'use strict';

const fs = require('fs');
const os = require('os');
const path = require('path');
const { pathToFileURL } = require('url');
const { createRequire } = require('module');
const { findEdgeExecutable } = require('./preflight.cjs');
const { collectRuntimeSnapshot, findNodes } = require('./runtime-tree.cjs');
const { decidePopup, assertSafePopupDecision, popupCloseStability } = require('./popup-policy.cjs');
const { clickRuntimeTarget } = require('./canvas-input.cjs');

const LEGACY_720_LOGIN = Object.freeze({
  viewport: { width: 720, height: 1280 },
  accountPoint: { x: 408, y: 525 },
  passwordPoint: { x: 408, y: 590 },
  submitPoint: { x: 490, y: 718 },
  transitions: {
    LoginAlertView: { x: 460, y: 840 },
    LoginEnterView: { x: 360, y: 930 },
    LoginSelectRoleView: { x: 360, y: 1120 },
    LoginCreateRoleView: { x: 360, y: 1120 },
    DialogueView: { x: 45, y: 565 },
  },
  mainView: 'MainUITopView',
  passiveViewPattern: '^(MainUI|NameBoard|MessageItem|FirstRechargeBubble|FunctionOpenIcon|UIJoyStick|WaitforOpenViewLoading|FightingUpView|ActivityIcon|FuncBoardView|ChuanwenItem)',
});

function sleep(ms) { return new Promise(resolve => setTimeout(resolve, ms)); }

function loadPuppeteer(repoRoot) {
  const packagePath = path.join(repoRoot, 'Tools', 'headless', 'node_modules', 'puppeteer', 'package.json');
  return createRequire(packagePath)('puppeteer');
}

async function loadPageSnapshotScript(sourcePath) {
  const temp = path.join(os.tmpdir(), `ui-audit-page-snapshot-${process.pid}-${Date.now()}.mjs`);
  fs.copyFileSync(sourcePath, temp);
  try {
    const module = await import(`${pathToFileURL(temp).href}?v=${Date.now()}`);
    if (typeof module.PAGE_SNAPSHOT_SCRIPT !== 'string') throw new Error('PAGE_SNAPSHOT_SCRIPT export missing');
    return module.PAGE_SNAPSHOT_SCRIPT;
  } finally {
    try { fs.unlinkSync(temp); } catch (_) {}
  }
}

class HeadlessUiSession {
  constructor(options) {
    this.options = options;
    this.browser = null;
    this.page = null;
    this.events = [];
    this.snapshotScript = null;
    this.sessionId = `ui-audit-${Date.now()}-${process.pid}`;
  }

  note(kind, detail = {}) {
    const event = { at: new Date().toISOString(), kind, ...detail };
    this.events.push(event);
    return event;
  }

  async start() {
    const repoRoot = path.resolve(this.options.repoRoot);
    const puppeteer = loadPuppeteer(repoRoot);
    const executablePath = this.options.edgeExecutable || findEdgeExecutable();
    if (!executablePath) throw new Error('HEADLESS_EDGE_NOT_FOUND');
    this.browser = await puppeteer.launch({
      headless: true,
      executablePath,
      args: ['--disable-background-timer-throttling', '--disable-renderer-backgrounding'],
    });
    this.page = await this.browser.newPage();
    await this.page.setViewport(this.options.viewport || LEGACY_720_LOGIN.viewport);
    this.page.on('console', message => this.note('browser-console', { level: message.type(), text: message.text() }));
    this.page.on('pageerror', error => this.note('browser-pageerror', { text: String(error && error.stack || error) }));
    await this.page.goto(this.options.url, { waitUntil: 'domcontentloaded', timeout: this.options.navigationTimeoutMs || 30000 });
    await this.page.waitForFunction(() => !!(window.Laya && window.Laya.stage), { timeout: this.options.runtimeTimeoutMs || 30000 });
    if (this.options.snapshotSource) {
      this.snapshotScript = await loadPageSnapshotScript(this.options.snapshotSource);
      await this.injectSnapshotRuntime();
    }
    this.note('session-started', { sessionId: this.sessionId, url: this.options.url, executablePath });
    return this;
  }

  async injectSnapshotRuntime() {
    if (!this.snapshotScript) throw new Error('PAGE_SNAPSHOT_SCRIPT_NOT_CONFIGURED');
    await this.page.evaluate(`${this.snapshotScript}; void 0`);
  }

  async snapshot() {
    if (this.snapshotScript) await this.injectSnapshotRuntime();
    return collectRuntimeSnapshot(this.page);
  }

  async typeAt(point, value) {
    await this.page.mouse.click(point.x, point.y);
    await this.page.keyboard.down('Control');
    await this.page.keyboard.press('A');
    await this.page.keyboard.up('Control');
    await this.page.keyboard.press('Backspace');
    await this.page.keyboard.type(String(value), { delay: 20 });
  }

  async closeAllowlistedPopup(viewName, popupPolicy) {
    const decision = decidePopup(popupPolicy, viewName);
    const close = assertSafePopupDecision(decision);
    const maxClicks = Number(close.maxClicks || 1);
    for (let clickIndex = 0; clickIndex < maxClicks; clickIndex++) {
      const before = await this.snapshot();
      if (!before.visibleViews.includes(viewName)) return { closed: true, clicks: clickIndex, decision };
      if (close.kind === 'view-node') {
        await clickRuntimeTarget(this.page, before, {
          source: 'laya-stage', ownerView: viewName, boundField: close.node, expectedCount: 1,
        });
      } else if (close.kind === 'shared-background') {
        const candidate = await this.page.evaluate(targetView => {
          const Manager = window.ViewManager;
          const manager = Manager && Manager.GetInstance && Manager.GetInstance();
          const background = manager && manager.GetBackGround && manager.GetBackGround();
          const current = background && background.curr_view;
          const qualified = current && window.GetQualifiedClassName ? String(window.GetQualifiedClassName(current) || '') : '';
          const constructorName = current && current.constructor ? String(current.constructor.name || '') : '';
          if (!background || background.visible === false || (qualified !== targetView && constructorName !== targetView)) return null;
          const point = background.localToGlobal(new Laya.Point(Number(background.width || 0) / 2, Number(background.height || 0) / 2), true);
          return { x: Number(point.x), y: Number(point.y) };
        }, viewName);
        if (!candidate) throw new Error(`POPUP_SHARED_BACKGROUND_IDENTITY_MISMATCH: ${viewName}`);
        await this.page.mouse.click(candidate.x, candidate.y);
      } else {
        throw new Error(`POPUP_CLOSE_KIND_UNSUPPORTED: ${close.kind}`);
      }
      await sleep(close.mode && close.mode.includes('tween') ? 900 : 300);
    }
    if (close.stability) {
      const samples = [];
      const deadline = Date.now() + Number(close.stability.timeoutMs);
      let stability = popupCloseStability(samples, viewName, close.stability);
      while (Date.now() <= deadline && !stability.pass) {
        const snapshot = await this.snapshot();
        samples.push({ visibleViews: snapshot.visibleViews, stage: snapshot.stage });
        stability = popupCloseStability(samples, viewName, close.stability);
        if (!stability.pass) await sleep(Number(close.stability.pollMs));
      }
      if (!stability.pass) throw new Error(`POPUP_CLOSE_NOT_STABLE: ${JSON.stringify(stability)}`);
      return { closed: true, clicks: maxClicks, decision, stability };
    }
    const after = await this.snapshot();
    if (after.visibleViews.includes(viewName)) throw new Error(`POPUP_DID_NOT_CLOSE: ${viewName}`);
    return { closed: true, clicks: maxClicks, decision, stability: null };
  }

  async loginAndReachMainUi(options) {
    const preset = { ...LEGACY_720_LOGIN, ...(options.preset || {}) };
    const inputDeadline = Date.now() + (options.loginInputTimeoutMs || 30000);
    let loginReady = false;
    while (Date.now() < inputDeadline) {
      const current = await this.snapshot();
      const accountInputs = findNodes(current, { source: 'laya-stage', name: 'account' });
      const passwordInputs = findNodes(current, { source: 'laya-stage', name: 'password' });
      if (accountInputs.length === 1 && passwordInputs.length === 1) {
        loginReady = true;
        break;
      }
      await sleep(100);
    }
    if (!loginReady) throw new Error('LOGIN_INPUTS_NOT_READY');
    await this.typeAt(preset.accountPoint, options.account);
    await this.typeAt(preset.passwordPoint, options.password);
    const typed = await this.snapshot();
    const accountEcho = findNodes(typed, { source: 'laya-stage', name: 'account' })
      .some(node => node.text === String(options.account));
    if (!accountEcho) throw new Error(`ACCOUNT_ECHO_MISMATCH: ${options.account}`);
    await this.page.mouse.click(preset.submitPoint.x, preset.submitPoint.y);
    this.note('login-submit', { account: options.account, accountEcho: true });
    await sleep(options.postSubmitWaitMs || 8000);

    const passive = new RegExp(preset.passiveViewPattern);
    let clean = 0;
    for (let iteration = 0; iteration < (options.maxIterations || 45) && clean < 3; iteration++) {
      const snapshot = await this.snapshot();
      const visible = snapshot.visibleViews;
      const inMain = visible.includes(preset.mainView);
      const blockers = visible.filter(name => !passive.test(name) && name !== preset.mainView && !/^LoginLoadingView$/.test(name));
      this.note('login-state', { iteration, inMain, blockers });
      const transition = Object.entries(preset.transitions).find(([name]) => visible.includes(name));
      if (transition && (transition[0] !== 'DialogueView' || !inMain)) {
        clean = 0;
        await this.page.mouse.click(transition[1].x, transition[1].y);
      } else if (inMain && blockers.length) {
        clean = 0;
        const top = blockers[blockers.length - 1];
        if (top === 'ItemUseView') {
          if (typeof options.itemUseHandler !== 'function') throw new Error('ITEM_USE_REQUIRES_CONTROLLED_HANDLER');
          await options.itemUseHandler(this.page, snapshot);
        } else {
          await this.closeAllowlistedPopup(top, options.popupPolicy);
        }
      } else if (inMain) clean++;
      await sleep(options.pollMs || 1000);
    }
    const final = await this.snapshot();
    const pass = final.visibleViews.includes(preset.mainView)
      && !final.visibleViews.some(name => /^Login/.test(name));
    if (!pass) throw new Error(`MAIN_UI_NOT_READY: ${JSON.stringify(final.visibleViews)}`);
    this.note('main-ui-ready', { sessionId: this.sessionId, visibleViews: final.visibleViews });
    return final;
  }

  async assertHotSession(mainView = LEGACY_720_LOGIN.mainView) {
    if (!this.browser || !this.browser.connected || !this.page || this.page.isClosed()) {
      throw new Error('HOT_SESSION_LOST');
    }
    const snapshot = await this.snapshot();
    if (!snapshot.visibleViews.includes(mainView)) throw new Error(`HOT_SESSION_NOT_IN_MAIN_UI: ${mainView}`);
    return { sessionId: this.sessionId, mainView, snapshot };
  }

  async close() {
    if (this.browser) await this.browser.close();
    this.note('session-closed', { sessionId: this.sessionId });
    this.page = null;
    this.browser = null;
  }
}

module.exports = {
  LEGACY_720_LOGIN,
  sleep,
  loadPuppeteer,
  loadPageSnapshotScript,
  HeadlessUiSession,
};
