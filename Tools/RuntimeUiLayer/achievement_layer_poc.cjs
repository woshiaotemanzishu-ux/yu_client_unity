/*
 * Independent legacy-Laya UI layer proof for Achievement.
 *
 * It keeps a real Unity WebGL page alive underneath a real legacy Laya page.
 * The legacy page is hidden while it logs in, becomes the top interactive layer
 * only after AchvMainView is visible, and is atomically hidden again when the
 * legacy return button closes Achievement. No Unity Editor automation is used.
 *
 * Usage:
 *   node Tools/RuntimeUiLayer/achievement_layer_poc.cjs \
 *     [legacyAccount] [legacyPassword] [outputDir] [legacyUrl] [unityUrl]
 */

const crypto = require('crypto');
const fs = require('fs');
const http = require('http');
const os = require('os');
const path = require('path');
const { chromium } = require('e:/GitProject/yu_client_unity/output/node_modules/playwright');

const LEGACY_ACCOUNT = process.argv[2] || '123123';
const LEGACY_PASSWORD = process.argv[3] || '123123';
const OUTPUT = process.argv[4] || path.join(
  __dirname,
  '../../output/ui_layer_poc/2026-08-08_achievement/' +
    new Date().toISOString().replace(/[-:]/g, '').replace(/\..+/, '').replace('T', '_'),
);
const LEGACY_URL = process.argv[5] || 'http://127.0.0.1:8091/index.html';
const UNITY_URL = process.argv[6] || 'http://223.109.142.26:89/web/';
const VIEWPORT = { width: 720, height: 1280 };
const WHITELIST = /^(MainUI|NameBoard|MessageItem|FirstRechargeBubble|FunctionOpenIcon|UIJoyStick|WaitforOpenViewLoading|FightingUpView|LoginBgView|ActivityIcon|FuncBoardView)/;

// A tool runner can detach while the proof is still active. Keep evidence
// generation alive if that closes only the inherited stdout/stderr pipe.
for (const stream of [process.stdout, process.stderr]) {
  stream.on('error', (error) => {
    if (!error || error.code !== 'EPIPE') throw error;
  });
}

function ensureEmptyOutput() {
  if (fs.existsSync(OUTPUT) && fs.readdirSync(OUTPUT).length) {
    throw new Error(`immutable evidence directory already exists and is non-empty: ${OUTPUT}`);
  }
  fs.mkdirSync(OUTPUT, { recursive: true });
}

function sha256(file) {
  return crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
}

function htmlAttr(value) {
  return String(value)
    .replace(/&/g, '&amp;')
    .replace(/"/g, '&quot;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');
}

function hostHtml() {
  return `<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no">
  <title>Achievement Independent Laya Layer POC</title>
  <style>
    html, body { width: 100%; height: 100%; margin: 0; overflow: hidden; background: #090d18; }
    #stage { position: fixed; inset: 0; width: 100%; height: 100%; overflow: hidden; }
    iframe { position: absolute; inset: 0; width: 100%; height: 100%; border: 0; }
    #unity-frame { z-index: 1; background: #090d18; }
    #laya-frame {
      z-index: 2;
      opacity: 0;
      visibility: hidden;
      pointer-events: none;
      background: transparent;
    }
    body[data-layer="laya-boot"] #laya-frame {
      opacity: 0;
      visibility: visible;
      pointer-events: auto;
    }
    body[data-layer="laya-open"] #laya-frame {
      opacity: 1;
      visibility: visible;
      pointer-events: auto;
    }
  </style>
</head>
<body data-layer="unity">
  <main id="stage">
    <iframe id="unity-frame" name="unity-layer" src="${htmlAttr(UNITY_URL)}" allow="autoplay; fullscreen"></iframe>
    <iframe id="laya-frame" name="laya-layer" src="${htmlAttr(LEGACY_URL)}" allow="autoplay; fullscreen"></iframe>
  </main>
  <script>
    (() => {
      const events = [];
      const push = (type, detail = {}) => events.push({ type, detail, at: new Date().toISOString() });
      const setLayer = (mode, reason = '') => {
        document.body.dataset.layer = mode;
        push('host-layer', { mode, reason });
      };
      window.__sxUiLayerHost = {
        setLayer,
        events,
        state() {
          const laya = document.getElementById('laya-frame');
          const style = getComputedStyle(laya);
          const center = document.elementFromPoint(innerWidth / 2, innerHeight / 2);
          return {
            mode: document.body.dataset.layer,
            laya: {
              opacity: style.opacity,
              visibility: style.visibility,
              pointerEvents: style.pointerEvents,
            },
            centerTarget: center ? center.id || center.tagName : null,
          };
        },
      };
      window.addEventListener('message', (event) => {
        const data = event.data || {};
        if (data.channel !== 'sx-laya-ui-layer') return;
        push(data.type || 'legacy-message', data.detail || {});
        if (data.type === 'achievement-open') setLayer('laya-open', 'legacy-open-event');
        if (data.type === 'achievement-close') setLayer('unity', 'legacy-close-event');
      });
      push('host-ready');
    })();
  </script>
</body>
</html>`;
}

function createHostServer() {
  const html = Buffer.from(hostHtml(), 'utf8');
  const server = http.createServer((request, response) => {
    if (request.url === '/' || request.url.startsWith('/index.html')) {
      response.writeHead(200, {
        'Content-Type': 'text/html; charset=utf-8',
        'Content-Length': html.length,
        'Cache-Control': 'no-store',
      });
      response.end(html);
      return;
    }
    if (request.url === '/health') {
      response.writeHead(200, { 'Content-Type': 'text/plain' });
      response.end('ok');
      return;
    }
    response.writeHead(404);
    response.end('not found');
  });
  return new Promise((resolve, reject) => {
    server.once('error', reject);
    server.listen(0, '127.0.0.1', () => {
      const address = server.address();
      resolve({ server, url: `http://127.0.0.1:${address.port}/` });
    });
  });
}

async function waitForFrame(page, test, timeoutMs) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    const frame = page.frames().find((candidate) => test(candidate.url()));
    if (frame) return frame;
    await page.waitForTimeout(100);
  }
  throw new Error('timed out waiting for iframe');
}

async function main() {
  ensureEmptyOutput();
  const report = {
    kind: 'independent-laya-ui-layer-poc',
    route: 'Unity host -> independent legacy Achievement UI',
    startedAt: new Date().toISOString(),
    viewport: VIEWPORT,
    legacy: { url: LEGACY_URL, account: LEGACY_ACCOUNT },
    unity: { url: UNITY_URL, loginPerformed: false },
    timingsMs: {},
    assertions: {},
    screenshots: {},
    limitations: [
      'This proof reuses the full legacy runtime and its own test login; the production data bridge is not implemented.',
      'Unity remains on its login/runtime page; this run validates browser-layer coexistence, input ownership, legacy visual reuse, and atomic cleanup.',
      'No Achievement reward/upgrade write transaction is clicked.',
    ],
  };

  const logFile = path.join(OUTPUT, 'console.log');
  const log = fs.createWriteStream(logFile, { flags: 'wx' });
  const stamp = () => new Date().toISOString();
  const say = (message) => {
    const line = `[${stamp()}] ${message}`;
    console.log(line);
    log.write(line + '\n');
  };

  const tempModule = path.join(os.tmpdir(), `achievement_layer_snapshot_${process.pid}.mjs`);
  fs.copyFileSync(
    'e:/GitProject/yu_client/tools/yu-resource-tool/frontend/src/utils/pageSnapshot.js',
    tempModule,
  );
  const snapshotScript = (await import('file:///' + tempModule.split(path.sep).join('/')))
    .PAGE_SNAPSHOT_SCRIPT;

  const { server, url: hostUrl } = await createHostServer();
  let browser;
  try {
    browser = await chromium.launch({
      headless: true,
      channel: 'msedge',
      args: [
        '--no-sandbox',
        '--enable-webgl',
        '--ignore-gpu-blocklist',
        '--use-gl=angle',
        `--window-size=${VIEWPORT.width},${VIEWPORT.height}`,
      ],
    });
    const context = await browser.newContext({ viewport: VIEWPORT });
    const legacyOrigin = new URL(LEGACY_URL).origin;
    await context.addInitScript(({ expectedOrigin }) => {
      if (location.origin !== expectedOrigin) return;
      const send = (type, detail = {}) => {
        try {
          parent.postMessage({ channel: 'sx-laya-ui-layer', type, detail }, '*');
        } catch (_) {}
      };
      const isEffectivelyVisible = (node) => {
        let current = node;
        while (current) {
          if (current.destroyed || current.visible === false || Number(current.alpha) === 0) return false;
          if (window.Laya && current === Laya.stage) return true;
          current = current.parent;
        }
        return false;
      };
      const findNamed = (root, name, depth = 0) => {
        if (!root || depth > 50) return null;
        if (root.name === name && isEffectivelyVisible(root)) return root;
        const children = root._children || [];
        for (let i = 0; i < children.length; i++) {
          const found = findNamed(children[i], name, depth + 1);
          if (found) return found;
        }
        return null;
      };
      const isAchievementOpen = () => {
        try {
          return !!(window.Laya && Laya.stage && findNamed(Laya.stage, 'tabScroller'));
        } catch (_) {
          return false;
        }
      };
      window.__sxIndependentUiLayer = {
        isAchievementOpen,
        openAchievement() {
          try {
            const type = window.achvModel;
            if (!type || !type.GetInstance) return { ok: false, reason: 'achvModel unavailable' };
            type.GetInstance().Fire(type.OP_V, 'AchvEnterView');
            return { ok: true };
          } catch (error) {
            return { ok: false, reason: String(error) };
          }
        },
      };
      let last = false;
      setInterval(() => {
        const current = isAchievementOpen();
        if (current !== last) {
          send(current ? 'achievement-open' : 'achievement-close', { runtime: 'legacy-laya' });
          last = current;
        }
      }, 120);
      send('legacy-bridge-installed', { origin: location.origin });
    }, { expectedOrigin: legacyOrigin });

    const page = await context.newPage();
    page.on('console', (message) => {
      const text = message.text();
      if (/AudioContext|WebGL: INVALID_ENUM/.test(text)) return;
      log.write(`[${stamp()}][browser:${message.type()}] ${text}\n`);
    });
    page.on('pageerror', (error) => log.write(`[${stamp()}][pageerror] ${error.message}\n`));
    page.on('requestfailed', (request) => {
      const failure = request.failure();
      log.write(`[${stamp()}][requestfailed] ${request.url()} :: ${failure ? failure.errorText : ''}\n`);
    });

    const shot = async (name) => {
      const file = path.join(OUTPUT, name);
      await page.screenshot({ path: file });
      report.screenshots[name] = { path: file, sha256: sha256(file) };
      say(`SHOT ${name}`);
    };
    const setLayer = (mode, reason) => page.evaluate(({ nextMode, why }) => {
      window.__sxUiLayerHost.setLayer(nextMode, why);
    }, { nextMode: mode, why: reason });

    say(`open host ${hostUrl}`);
    await page.goto(hostUrl, { waitUntil: 'domcontentloaded', timeout: 30000 });
    const unityFrame = await waitForFrame(page, (value) => value.startsWith(new URL(UNITY_URL).origin), 30000);
    const legacyFrame = await waitForFrame(page, (value) => value.startsWith(legacyOrigin), 30000);

    const legacyPoint = async (designX, designY) => {
      const iframe = await page.locator('#laya-frame').boundingBox();
      const canvas = await legacyFrame.evaluate(() => {
        const element = document.querySelector('canvas');
        if (!element) return null;
        const rect = element.getBoundingClientRect();
        return { x: rect.x, y: rect.y, width: rect.width, height: rect.height };
      });
      if (!iframe || !canvas || canvas.width <= 0 || canvas.height <= 0) {
        throw new Error('legacy iframe/canvas has no usable geometry');
      }
      return {
        x: iframe.x + canvas.x + designX * canvas.width / VIEWPORT.width,
        y: iframe.y + canvas.y + designY * canvas.height / VIEWPORT.height,
      };
    };
    const legacyClick = async (designX, designY) => {
      const point = await legacyPoint(designX, designY);
      await page.mouse.click(point.x, point.y);
    };

    const unityStarted = Date.now();
    const unityReady = (async () => {
      try {
        await unityFrame.waitForFunction(() => !!window.SxUnity, null, { timeout: 120000 });
        report.timingsMs.unityEngineReady = Date.now() - unityStarted;
        await unityFrame.waitForFunction(() => {
          const bar = document.querySelector('#unity-loading-bar');
          return !bar || getComputedStyle(bar).display === 'none';
        }, null, { timeout: 120000 });
        report.assertions.unityRuntimeLoaded = true;
        report.timingsMs.unityRuntimeReady = Date.now() - unityStarted;
        say(`Unity WebGL runtime visible in ${report.timingsMs.unityRuntimeReady}ms`);
        await page.waitForTimeout(750);
      } catch (error) {
        report.assertions.unityRuntimeLoaded = false;
        report.unity.loadError = String(error.message || error);
        say(`Unity runtime did not expose SxUnity: ${report.unity.loadError}`);
      }
    })();

    // Prepare the legacy session while Unity downloads/boots underneath. The
    // two waits are independent and must not be paid serially.
    await setLayer('laya-boot', 'prepare-legacy-session');
    const legacyStarted = Date.now();
    await legacyFrame.waitForFunction(() => !!(window.Laya && window.Laya.stage), null, { timeout: 30000 });
    report.assertions.layaRuntimeLoaded = true;
    await page.waitForTimeout(9000);
    await legacyFrame.evaluate(snapshotScript + '; void 0');

    const listedViews = async () => legacyFrame.evaluate(() => {
      try {
        return (window.__sxListLoadedPages__().views || []).map((view) => ({
          name: view.name,
          open: view.open !== false,
          visible: view.visible !== false,
        }));
      } catch (_) {
        return [];
      }
    });
    const queryNodes = async (criteria) => legacyFrame.evaluate((query) => {
      try {
        const names = (window.__sxListLoadedPages__().views || []).map((view) => view.name);
        const exported = window.__sxExportPageSnapshots__(names);
        const matches = [];
        for (const view of exported.views || []) {
          if (query.viewPattern && !(new RegExp(query.viewPattern, 'i')).test(view.meta.name)) continue;
          const walk = (node, ancestors) => {
            const text = node.textProps && node.textProps.text ? String(node.textProps.text) : '';
            const skin = String(node.skin || '');
            const ancestry = ancestors.map((item) => String(item.name || '')).join('/');
            const ok = node.effectiveVisible !== false
              && (!query.name || node.name === query.name)
              && (!query.ancestorPattern || (new RegExp(query.ancestorPattern, 'i')).test(ancestry))
              && (!query.textPattern || (new RegExp(query.textPattern, 'i')).test(text))
              && (!query.skinPattern || (new RegExp(query.skinPattern, 'i')).test(skin))
              && node.globalBounds;
            if (ok) {
              const b = node.globalBounds;
              matches.push({
                view: view.meta.name,
                name: node.name,
                text,
                skin,
                path: node.path,
                x: b.x,
                y: b.y,
                width: b.width,
                height: b.height,
                cx: Math.round(b.x + b.width / 2),
                cy: Math.round(b.y + b.height / 2),
              });
            }
            (node.children || []).forEach((child) => walk(child, ancestors.concat(node)));
          };
          walk(view.nodeTree, []);
        }
        return matches;
      } catch (_) {
        return [];
      }
    }, criteria);
    const findClose = async (viewName) => legacyFrame.evaluate((name) => {
      try {
        const view = (window.__sxExportPageSnapshots__([name]).views || [])[0];
        if (!view) return null;
        const found = [];
        const walk = (node) => {
          const lower = String(node.name || '').toLowerCase();
          // CongratulationObtainView intentionally has no close icon. Its
          // click_bg/left_bg/_gp_hit handlers only finish the reveal animation
          // or call Close(); they do not claim/use another item transaction.
          const isSourceVerifiedDismiss = name === 'CongratulationObtainView'
            && /^(click_bg|left_bg|_gp_hit)$/.test(lower);
          if (node.effectiveVisible !== false
            && (isSourceVerifiedDismiss
              || /close|guanbi|_btn_x\b|btn_quit|_img_x\b|_btn_close\b/.test(lower))
            && node.globalBounds) {
            const b = node.globalBounds;
            found.push({
              name: node.name,
              sourceVerifiedDismiss: isSourceVerifiedDismiss,
              cx: Math.round(b.x + b.width / 2),
              cy: Math.round(b.y + b.height / 2),
            });
          }
          (node.children || []).forEach(walk);
        };
        walk(view.nodeTree);
        const onStage = (item) => item.cx >= 0 && item.cx <= 720 && item.cy >= 0 && item.cy <= 1280;
        return found.find((item) => item.sourceVerifiedDismiss && onStage(item))
          || found.find(onStage)
          || null;
      } catch (_) {
        return null;
      }
    }, viewName);
    const typeInto = async (x, y, value) => {
      await legacyClick(x, y);
      await page.keyboard.press('Control+a');
      await page.keyboard.press('Backspace');
      await page.keyboard.type(value, { delay: 15 });
    };
    const escapeRegex = (value) => String(value).replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const requestIndependentAchievementOpen = async () => {
      const result = await legacyFrame.evaluate(() => {
        if (!window.__sxIndependentUiLayer) {
          return { ok: false, reason: 'independent layer bridge unavailable' };
        }
        return window.__sxIndependentUiLayer.openAchievement();
      });
      if (!result || !result.ok) {
        throw new Error(`independent Achievement open failed: ${result && result.reason}`);
      }
      return result;
    };

    await typeInto(408, 525, LEGACY_ACCOUNT);
    await typeInto(408, 590, LEGACY_PASSWORD);
    await legacyClick(490, 718);
    say(`legacy login fired account=${LEGACY_ACCOUNT}`);
    await page.waitForTimeout(8000);
    await legacyFrame.evaluate(snapshotScript + '; void 0');

    let stable = 0;
    for (let attempt = 0; attempt < 45 && stable < 3; attempt++) {
      const current = await listedViews();
      const visible = (name) => current.some((view) => view.name === name && view.visible && view.open);
      const inCity = visible('MainUITopView');
      const blockers = current.filter((view) => view.visible && view.open && !WHITELIST.test(view.name));
      say(`legacy settle ${attempt}: city=${inCity} blockers=${blockers.map((view) => view.name).join(',') || '-'}`);
      if (visible('LoginAlertView')) {
        stable = 0;
        await legacyClick(460, 840);
      } else if (visible('LoginEnterView')) {
        stable = 0;
        await legacyClick(360, 930);
        await page.waitForTimeout(3000);
      } else if (visible('LoginSelectRoleView') || visible('LoginCreateRoleView')) {
        stable = 0;
        await legacyClick(360, 1120);
        await page.waitForTimeout(5000);
      } else if (visible('DialogueView')) {
        stable = 0;
        await legacyClick(45, 565);
      } else if (inCity && blockers.length) {
        stable = 0;
        const blocker = blockers[blockers.length - 1];
        const close = await findClose(blocker.name);
        if (!close) throw new Error(`legacy startup blocker has no safe close: ${blocker.name}`);
        await legacyClick(close.cx, close.cy);
      } else if (inCity) {
        stable++;
      }
      await page.waitForTimeout(2500);
      await legacyFrame.evaluate(snapshotScript + '; void 0');
    }
    if (stable < 3) throw new Error('legacy client did not reach a stable city state');
    report.timingsMs.legacyCityReady = Date.now() - legacyStarted;
    report.assertions.legacyCityReady = true;

    await unityReady;
    await setLayer('unity', 'capture-runtime-underlay');
    await page.waitForTimeout(250);
    await shot('01_unity_runtime_underlay.png');
    await setLayer('laya-boot', 'open-achievement');

    const openStarted = Date.now();
    report.entry = {
      mode: 'host-to-laya-bridge',
      target: 'achvModel.OP_V -> AchvEnterView',
      result: await requestIndependentAchievementOpen(),
    };
    await page.waitForFunction(() => window.__sxUiLayerHost.state().mode === 'laya-open', null, { timeout: 10000 });
    for (let attempt = 0; attempt < 30; attempt++) {
      await legacyFrame.evaluate(snapshotScript + '; void 0');
      if ((await queryNodes({ ancestorPattern: 'AchvMainView', name: 'tabScroller' })).length) break;
      await page.waitForTimeout(200);
    }
    report.timingsMs.achievementColdOpen = Date.now() - openStarted;
    const achievementRoots = await queryNodes({
      ancestorPattern: 'AchvMainView',
      name: 'tabScroller',
    });
    report.assertions.achievementOpened = achievementRoots.length > 0;
    if (!report.assertions.achievementOpened) throw new Error('legacy Achievement did not become visible');
    report.achievementOwnerView = achievementRoots[0].view;
    const achievementOwnerPattern = `^${escapeRegex(report.achievementOwnerView)}$`;
    await page.waitForTimeout(500);
    await shot('02_laya_achievement_open.png');

    const personalLabel = (await queryNodes({
      ancestorPattern: 'AchvTabBtn',
      name: 'tab_txt',
      textPattern: '^个人成长$',
    }))[0];
    if (!personalLabel) throw new Error('legacy Personal Growth tab not found');
    const tabButtons = await queryNodes({ ancestorPattern: 'AchvTabBtn', name: 'tab' });
    const personalTab = tabButtons.sort((a, b) =>
      Math.abs(a.cx - personalLabel.cx) - Math.abs(b.cx - personalLabel.cx))[0];
    if (!personalTab) throw new Error('legacy Personal Growth click surface not found');
    await legacyClick(personalTab.cx, personalTab.cy);
    await page.waitForTimeout(800);
    await legacyFrame.evaluate(snapshotScript + '; void 0');
    const subTabs = await queryNodes({ ancestorPattern: 'AchvTabSubBtn', name: 'btn_text' });
    report.assertions.realLayaTabInteraction = subTabs.length === 4;
    report.interaction = { selected: '个人成长', visibleSubTabs: subTabs.map((item) => item.text) };
    await shot('03_laya_achievement_interacted.png');

    const returns = [
      ...(await queryNodes({ viewPattern: achievementOwnerPattern, ancestorPattern: 'BaseWindowSkin', name: '_img_return' })),
      ...(await queryNodes({ viewPattern: achievementOwnerPattern, ancestorPattern: 'BaseWindowSkin', name: '_img_return0' })),
    ];
    if (!returns.length) throw new Error('legacy Achievement return button not found');
    report.closeControls = { cold: returns };
    const closeStarted = Date.now();
    await legacyClick(returns[0].cx, returns[0].cy);
    await page.waitForFunction(() => window.__sxUiLayerHost.state().mode === 'unity', null, { timeout: 10000 });
    report.timingsMs.bridgeCloseToUnity = Date.now() - closeStarted;
    await page.waitForTimeout(300);
    await legacyFrame.evaluate(snapshotScript + '; void 0');
    const closedState = await page.evaluate(() => window.__sxUiLayerHost.state());
    report.closedState = closedState;
    report.assertions.legacyAchievementClosed = (await queryNodes({
      ancestorPattern: 'AchvMainView',
      name: 'tabScroller',
    })).length === 0;
    report.assertions.layerPixelsAtomicallyRemoved = closedState.mode === 'unity'
      && closedState.laya.opacity === '0'
      && closedState.laya.visibility === 'hidden';
    report.assertions.inputReturnedToUnity = closedState.centerTarget === 'unity-frame';
    await shot('04_unity_after_laya_close.png');

    await setLayer('laya-boot', 'warm-reopen');
    const warmStarted = Date.now();
    await requestIndependentAchievementOpen();
    await page.waitForFunction(() => window.__sxUiLayerHost.state().mode === 'laya-open', null, { timeout: 10000 });
    report.timingsMs.achievementWarmOpen = Date.now() - warmStarted;
    report.assertions.warmReopen = await legacyFrame.evaluate(() =>
      !!(window.__sxIndependentUiLayer && window.__sxIndependentUiLayer.isAchievementOpen()));
    await page.waitForTimeout(350);
    await shot('05_laya_achievement_warm_reopen.png');

    await legacyFrame.evaluate(snapshotScript + '; void 0');
    const warmAchievementRoots = await queryNodes({
      ancestorPattern: 'AchvMainView',
      name: 'tabScroller',
    });
    if (!warmAchievementRoots.length) throw new Error('legacy warm Achievement owner not found');
    const warmAchievementOwnerPattern = `^${escapeRegex(warmAchievementRoots[0].view)}$`;
    const warmReturns = [
      ...(await queryNodes({ viewPattern: warmAchievementOwnerPattern, ancestorPattern: 'BaseWindowSkin', name: '_img_return' })),
      ...(await queryNodes({ viewPattern: warmAchievementOwnerPattern, ancestorPattern: 'BaseWindowSkin', name: '_img_return0' })),
    ];
    if (!warmReturns.length) throw new Error('legacy warm return button not found');
    report.closeControls.warm = warmReturns;
    await legacyClick(warmReturns[0].cx, warmReturns[0].cy);
    await page.waitForFunction(() => window.__sxUiLayerHost.state().mode === 'unity', null, { timeout: 10000 });
    await page.waitForTimeout(250);
    report.finalState = await page.evaluate(() => window.__sxUiLayerHost.state());
    report.hostEvents = await page.evaluate(() => window.__sxUiLayerHost.events);
    report.assertions.secondCloseClean = report.finalState.mode === 'unity'
      && report.finalState.laya.opacity === '0'
      && report.finalState.laya.visibility === 'hidden'
      && report.finalState.centerTarget === 'unity-frame';
    await shot('06_unity_after_second_close.png');

    const requiredAssertions = [
      'layaRuntimeLoaded',
      'legacyCityReady',
      'achievementOpened',
      'realLayaTabInteraction',
      'legacyAchievementClosed',
      'layerPixelsAtomicallyRemoved',
      'inputReturnedToUnity',
      'warmReopen',
      'secondCloseClean',
    ];
    report.pass = requiredAssertions.every((key) => report.assertions[key] === true);
    report.finishedAt = new Date().toISOString();
    fs.writeFileSync(path.join(OUTPUT, 'report.json'), JSON.stringify(report, null, 2), 'utf8');
    say(`VERDICT pass=${report.pass}`);
    if (!report.pass) throw new Error('one or more required assertions failed');
  } finally {
    log.end();
    if (browser) await browser.close().catch(() => {});
    await new Promise((resolve) => server.close(resolve));
    try { fs.unlinkSync(tempModule); } catch (_) {}
  }
}

main().catch((error) => {
  const failure = error && error.stack ? error.stack : String(error);
  try {
    fs.mkdirSync(OUTPUT, { recursive: true });
    fs.writeFileSync(path.join(OUTPUT, 'failure.txt'), failure + '\n', 'utf8');
  } catch (_) {}
  console.error(failure);
  process.exitCode = 1;
});
