'use strict';

const fs = require('fs');
const os = require('os');
const path = require('path');
const { pathToFileURL } = require('url');
const { createRequire } = require('module');
const { findEdgeExecutable } = require('./preflight.cjs');
const { collectRuntimeSnapshot, findNodes } = require('./runtime-tree.cjs');
const { decidePopup, assertSafePopupDecision, popupCloseStability, observePopupStack } = require('./popup-policy.cjs');
const { clickRuntimeTarget } = require('./canvas-input.cjs');
const { createPopupInstanceRef, observePopupLifecycle } = require('./popup-lifecycle.cjs');
const { buildSelectorDiagnostic, SelectorIdentityError } = require('./selector-diagnostic.cjs');
const { runtimeOverlayDecisions, runtimeOverlayViews } = require('./runtime-overlay.cjs');

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

function startupBlockers(snapshot, preset = LEGACY_720_LOGIN, runtimeOverlayPolicy = null) {
  const passive = new RegExp(preset.passiveViewPattern);
  const names = [
    ...(snapshot && snapshot.visibleViews || []),
    ...(runtimeOverlayPolicy ? runtimeOverlayViews(snapshot, runtimeOverlayPolicy).map(item => item.view) : []),
  ];
  return [...new Set(names)].filter(name => !passive.test(name)
    && name !== preset.mainView && !/^LoginLoadingView$/.test(name));
}

function overlayDiagnostic(snapshot, decision, code, elapsedMs = 0) {
  const overlay = decision && decision.overlay;
  const runtimeName = overlay && overlay.node && overlay.node.runtimeName || '';
  const selector = { source: 'laya-stage', runtimeName, expectedCount: 1 };
  const actualCount = runtimeName ? findNodes(snapshot, selector).length : 0;
  return new SelectorIdentityError(code, `${code}: ${decision && decision.reason || ''}`, buildSelectorDiagnostic(snapshot, selector, {
    expectedCount: 1,
    actualCount,
    context: { kind: 'runtime-overlay', elapsedMs, decision },
  }));
}

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

  async closeAllowlistedPopup(viewName, popupPolicy, runtime = {}) {
    const decision = decidePopup(popupPolicy, viewName);
    const close = assertSafePopupDecision(decision);
    const maxClicks = Number(close.maxClicks || 1);
    const selector = close.kind === 'view-node' ? {
      source: 'laya-stage', ownerView: viewName, boundField: close.node, expectedCount: 1,
    } : { source: 'laya-stage', ownerView: viewName, expectedCount: 1 };
    let instanceRef = null;
    let clickedTarget = null;
    let clickSnapshot = null;
    let clickInput = null;
    for (let clickIndex = 0; clickIndex < maxClicks; clickIndex++) {
      const before = await this.snapshot();
      if (!before.visibleViews.includes(viewName)) return { closed: true, clicks: clickIndex, decision };
      if (close.requiresCurrentView && runtime.preset) {
        const blockers = startupBlockers(before, runtime.preset, runtime.runtimeOverlayPolicy);
        const stack = observePopupStack(before, blockers, runtime.runtimeOverlayPolicy);
        const unresolved = stack.filter(item => !item.resolved);
        if (unresolved.length || !stack.length || stack[0].view !== viewName) {
          if (runtime.runtimeOverlayPolicy && !unresolved.length && stack.length && stack[0].view !== viewName) {
            this.note('popup-close-deferred', { requestedView: viewName, observedTopFirst: stack });
            return { closed: false, deferred: true, clicks: clickIndex, decision, observedTopFirst: stack };
          }
          const diagnostic = buildSelectorDiagnostic(before, selector, {
            expectedCount: 1,
            actualCount: findNodes(before, selector).length,
            context: {
              kind: 'popup-runtime-stack',
              requestedView: viewName,
              observedTopFirst: stack,
              unresolved: unresolved.map(item => item.view),
            },
          });
          throw new SelectorIdentityError(
            'POPUP_RUNTIME_STACK_CHANGED',
            `POPUP_RUNTIME_STACK_CHANGED requested=${viewName} observed=${JSON.stringify(stack)} diagnosticSha256=${diagnostic.sha256}`,
            diagnostic,
          );
        }
      }
      if (close.kind === 'view-node') {
        const click = await clickRuntimeTarget(this.page, before, selector, {
          point: close.point ? { x: Number(close.point.x), y: Number(close.point.y) } : undefined,
        });
        clickedTarget = click.target;
        clickInput = click.input;
        clickSnapshot = clickSnapshot || before;
        instanceRef = instanceRef || createPopupInstanceRef(before, viewName, selector, click.target);
      } else if (close.kind === 'shared-background') {
        clickSnapshot = clickSnapshot || before;
        instanceRef = instanceRef || createPopupInstanceRef(before, viewName, selector);
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
      this.note('popup-close-click', {
        view: viewName, clickIndex, selector, instance: instanceRef,
        target: clickedTarget ? { path: clickedTarget.path, indexPath: clickedTarget.indexPath } : null,
        input: clickInput,
      });
      if (!close.stability) await sleep(close.mode && close.mode.includes('tween') ? 900 : 300);
    }
    if (close.stability) {
      const samples = [];
      const deadline = Date.now() + Number(close.stability.timeoutMs);
      let stability = popupCloseStability(samples, viewName, close.stability, { input: clickInput });
      let lastSnapshot = null;
      while (Date.now() <= deadline && !stability.pass) {
        lastSnapshot = await this.snapshot();
        const lifecycle = observePopupLifecycle(lastSnapshot, instanceRef);
        samples.push({
          capturedAt: lastSnapshot.capturedAt,
          visibleViews: lastSnapshot.visibleViews,
          stage: lastSnapshot.stage,
          lifecycle,
        });
        stability = popupCloseStability(samples, viewName, close.stability, { input: clickInput });
        if (!stability.pass) await sleep(Number(close.stability.pollMs));
      }
      this.note('popup-close-stability', { view: viewName, stability });
      if (!stability.pass) {
        const actualCount = lastSnapshot ? findNodes(lastSnapshot, selector).length : 0;
        const initialDiagnostic = clickSnapshot ? buildSelectorDiagnostic(clickSnapshot, selector, {
          expectedCount: 1,
          actualCount: findNodes(clickSnapshot, selector).length,
        }) : null;
        const diagnostic = buildSelectorDiagnostic(lastSnapshot, selector, {
          expectedCount: 1,
          actualCount,
          context: {
            kind: 'popup-close-lifecycle',
            instance: instanceRef,
            evaluation: stability,
            input: clickInput,
            samples,
            initialSelector: initialDiagnostic ? {
              capturedAt: initialDiagnostic.capturedAt,
              runtimeSources: initialDiagnostic.runtimeSources,
              stage: initialDiagnostic.stage,
              subtree: initialDiagnostic.subtree,
              candidates: initialDiagnostic.candidates,
              sha256: initialDiagnostic.sha256,
            } : null,
          },
        });
        throw new SelectorIdentityError(
          'POPUP_CLOSE_NOT_STABLE',
          `POPUP_CLOSE_NOT_STABLE: ${JSON.stringify(stability)} diagnosticSha256=${diagnostic.sha256}`,
          diagnostic,
        );
      }
      return { closed: true, clicks: maxClicks, decision, stability, input: clickInput };
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

    let clean = 0;
    const runtimeGateStartedAt = new Map();
    const popupWaitStartedAt = new Map();
    for (let iteration = 0; iteration < (options.maxIterations || 45) && clean < 3; iteration++) {
      const snapshot = await this.snapshot();
      const visible = snapshot.visibleViews;
      const inMain = visible.includes(preset.mainView);
      const runtimeDecisions = options.runtimeOverlayPolicy
        ? runtimeOverlayDecisions(snapshot, options.runtimeOverlayPolicy) : [];
      const unknownOverlay = runtimeDecisions.find(decision => decision.action === 'unknown-hard-stop');
      if (unknownOverlay) throw overlayDiagnostic(snapshot, unknownOverlay, 'RUNTIME_OVERLAY_UNKNOWN');
      const waitingGate = runtimeDecisions.find(decision => decision.action === 'wait-for-release');
      if (waitingGate) {
        const key = waitingGate.overlay.id;
        const startedAt = runtimeGateStartedAt.get(key) || Date.now();
        runtimeGateStartedAt.set(key, startedAt);
        const elapsedMs = Date.now() - startedAt;
        this.note('runtime-overlay-wait', { iteration, elapsedMs, decision: waitingGate });
        if (elapsedMs > waitingGate.timeoutMs) throw overlayDiagnostic(snapshot, waitingGate, 'RUNTIME_INPUT_GATE_TIMEOUT', elapsedMs);
        clean = 0;
        await sleep(options.pollMs || 1000);
        continue;
      }
      runtimeGateStartedAt.clear();
      const blockers = startupBlockers(snapshot, preset, options.runtimeOverlayPolicy);
      const popupStack = inMain && blockers.length
        ? observePopupStack(snapshot, blockers, options.runtimeOverlayPolicy) : [];
      this.note('login-state', { iteration, inMain, blockers, popupStack, runtimeDecisions });
      const transition = Object.entries(preset.transitions).find(([name]) => visible.includes(name));
      if (transition && (transition[0] !== 'DialogueView' || !inMain)) {
        clean = 0;
        await this.page.mouse.click(transition[1].x, transition[1].y);
      } else if (inMain && blockers.length) {
        clean = 0;
        const unresolved = popupStack.filter(item => !item.resolved);
        if (unresolved.length) throw new Error(`POPUP_RUNTIME_STACK_UNRESOLVED: ${unresolved.map(item => item.view).join(',')}`);
        const top = popupStack[0] && popupStack[0].view;
        if (!top) throw new Error(`POPUP_RUNTIME_STACK_EMPTY: ${JSON.stringify(blockers)}`);
        if (!startupBlockers({ visibleViews: [top] }, preset).length) {
          this.note('popup-drain-deferred-by-passive-overlay', {
            iteration,
            view: top,
            overlay: popupStack[0].overlay || null,
          });
          await sleep(options.pollMs || 1000);
          continue;
        }
        if (top === 'ItemUseView') {
          if (typeof options.itemUseHandler !== 'function') {
            if (options.allowBlockedReadOnly === true) {
              this.note('login-read-only-blocked-snapshot', {
                iteration,
                view: top,
                reason: 'ItemUseView requires a controlled, type-specific handler',
              });
              return {
                ...snapshot,
                readOnlyBlockedBy: {
                  view: top,
                  reason: 'ItemUseView requires a controlled, type-specific handler',
                },
              };
            }
            throw new Error('ITEM_USE_REQUIRES_CONTROLLED_HANDLER');
          }
          await options.itemUseHandler(this.page, snapshot);
        } else {
          const topPolicy = decidePopup(options.popupPolicy, top);
          if (topPolicy.action === 'wait') {
            const currentView = popupStack[0].overlay && popupStack[0].overlay.currentView;
            const waitKey = `${top}:${currentView && currentView.hashCode || popupStack[0].rootPath || 'visible'}`;
            const startedAt = popupWaitStartedAt.get(waitKey) || Date.now();
            popupWaitStartedAt.set(waitKey, startedAt);
            const elapsedMs = Date.now() - startedAt;
            const timeoutMs = Number(topPolicy.entry.waitForRelease.timeoutMs);
            this.note('popup-wait-for-natural-release', {
              iteration, view: top, elapsedMs, timeoutMs, policy: topPolicy.entry.waitForRelease,
            });
            if (elapsedMs > timeoutMs) throw new Error(`POPUP_NATURAL_RELEASE_TIMEOUT: ${top}`);
            await sleep(options.pollMs || 1000);
            continue;
          }
          if (topPolicy.action !== 'allow' && popupStack[0].overlay) {
            throw overlayDiagnostic(snapshot, {
              ...topPolicy,
              overlay: popupStack[0].overlay,
              reason: `popup policy ${topPolicy.action}: ${topPolicy.reason || top}`,
            }, 'POPUP_POLICY_HARD_STOP');
          }
          await this.closeAllowlistedPopup(top, options.popupPolicy, {
            preset, runtimeOverlayPolicy: options.runtimeOverlayPolicy,
          });
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
  startupBlockers,
  overlayDiagnostic,
  HeadlessUiSession,
};
