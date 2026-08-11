/*
 * FashionMain(pos=1) Unity WebGL 真实 Canvas 只读验收。
 *
 * 用法：
 *   node Tools/headless/fashion_main_pos1_web.cjs <url> <account> <password> <new-output-dir>
 *
 * 本用例只点击时装页的只读入口、列表、颜色预览、等级弹窗和返回链；
 * 41301/41302/41303/41304/41305/41306/41316 任一发送都会失败。
 */
'use strict';

const crypto = require('crypto');
const fs = require('fs');
const path = require('path');
const puppeteer = require('puppeteer');

const URL = process.argv[2] || 'http://127.0.0.1:18090/?cdn=http%3A%2F%2F127.0.0.1%3A18090%2Fres';
const ACCOUNT = process.argv[3] || '111111';
const PASSWORD = process.argv[4] || '111111';
const OUT = path.resolve(process.env.FASHION_AUDIT_OUT
  || process.argv[5] || 'output/ui_route_audit/fashion-main-pos1-web');
const BASE_WIDTH = 720;
const BASE_HEIGHT = 1280;

if (fs.existsSync(OUT) && fs.readdirSync(OUT).length) {
  throw new Error(`IMMUTABLE_EVIDENCE_EXISTS: ${OUT}`);
}
fs.mkdirSync(OUT, { recursive: true });

const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
const sha256File = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');

async function waitUntil(predicate, timeoutMs, label) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    if (predicate()) return;
    await sleep(100);
  }
  throw new Error(`WAIT_TIMEOUT: ${label}`);
}

(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    executablePath: process.env.PUPPETEER_EXEC || undefined,
    args: [
      '--no-sandbox', '--enable-webgl', '--use-gl=angle',
      '--window-size=720,1280', '--disable-background-timer-throttling',
    ],
  });
  const page = await browser.newPage();
  await page.setViewport({ width: 720, height: 1280, deviceScaleFactor: 1 });
  await page.evaluateOnNewDocument(() => {
    window.__fashionWebAudioStarts = 0;
    const install = () => {
      const proto = window.AudioBufferSourceNode && window.AudioBufferSourceNode.prototype;
      if (!proto || proto.__fashionAuditWrapped) return;
      const original = proto.start;
      proto.start = function wrappedStart(...args) {
        window.__fashionWebAudioStarts += 1;
        return original.apply(this, args);
      };
      Object.defineProperty(proto, '__fashionAuditWrapped', { value: true });
    };
    install();
    const timer = setInterval(install, 50);
    window.addEventListener('beforeunload', () => clearInterval(timer));
  });

  const report = {
    schema: 1,
    authority: 'current-unity-web-real-canvas',
    page: 'FashionMain(pos=1)',
    url: URL,
    account: ACCOUNT,
    passwordRecorded: false,
    startedAt: new Date().toISOString(),
    viewports: ['720x1280', '1920x1080'],
    fingerprints: {
      playerWasmSha256: sha256File('Builds/WebGL/Build/WebGL.wasm.gz'),
      embeddedCatalogSha256: sha256File('Builds/WebGL/StreamingAssets/aa/catalog.bin'),
      serverCatalogSha256: sha256File('ServerData/WebGL/catalog_live.bin'),
    },
    actions: [],
    assertions: [],
    console: [],
    requests: [],
    failures: [],
    writeTransactions: { authorized: false, sent: [] },
  };
  const consoleText = [];
  let lastClickAt = 0;

  page.on('console', message => {
    const text = message.text();
    consoleText.push(text);
    if (/\[Fashion\]|\[Net\]|\[UI3D\]|role ready|main role ready|exception|error/i.test(text)) {
      report.console.push({ type: message.type(), text, at: new Date().toISOString() });
    }
  });
  page.on('pageerror', error => report.failures.push({ kind: 'pageerror', text: error.message }));
  page.on('requestfailed', request => {
    const text = request.failure() && request.failure().errorText;
    if (text !== 'net::ERR_ABORTED') report.failures.push({ kind: 'requestfailed', url: request.url(), text });
  });
  page.on('response', response => {
    const url = response.url();
    if (/WebGL\.(wasm|data)|catalog(_live)?\.(bin|hash)|\.bundle(?:\?|$)/i.test(url)) {
      report.requests.push({ status: response.status(), url });
    }
    if (response.status() >= 400 && !/favicon\.ico/i.test(url)) {
      report.failures.push({ kind: `http-${response.status()}`, url });
    }
  });

  const contentRect = async () => page.evaluate(({ bw, bh }) => {
    const canvas = document.querySelector('#unity-canvas');
    const rect = canvas ? canvas.getBoundingClientRect() : { left: 0, top: 0, width: innerWidth, height: innerHeight };
    const designAspect = bw / bh;
    const canvasAspect = rect.width / rect.height;
    let x = rect.left;
    let y = rect.top;
    let width = rect.width;
    let height = rect.height;
    if (canvasAspect > designAspect) {
      width = rect.height * designAspect;
      x += (rect.width - width) * 0.5;
    } else if (canvasAspect < designAspect) {
      height = rect.width / designAspect;
      y += (rect.height - height) * 0.5;
    }
    return { x, y, width, height };
  }, { bw: BASE_WIDTH, bh: BASE_HEIGHT });

  const canvasPoint = async (x, y) => {
    const rect = await contentRect();
    return {
      x: rect.x + x * rect.width / BASE_WIDTH,
      y: rect.y + y * rect.height / BASE_HEIGHT,
      contentRect: rect,
    };
  };

  const audioStarts = async () => page.evaluate(() => Number(window.__fashionWebAudioStarts || 0));
  const shot = async (name, extra = null) => {
    const file = path.join(OUT, name);
    await page.screenshot({ path: file });
    report.actions.push({
      action: 'screenshot', file: name, sha256: sha256File(file),
      viewport: `${(await page.viewport()).width}x${(await page.viewport()).height}`,
      at: new Date().toISOString(), extra,
    });
  };
  const click = async (label, x, y, waitMs = 0) => {
    const actual = await canvasPoint(x, y);
    const beforeAudio = await audioStarts();
    await page.mouse.click(actual.x, actual.y);
    lastClickAt = Date.now();
    if (waitMs) await sleep(waitMs);
    const afterAudio = await audioStarts();
    report.actions.push({
      action: 'click', label, logical: [x, y], actual,
      audioStartsDelta: afterAudio - beforeAudio, at: new Date().toISOString(),
    });
  };
  const drag = async (label, from, to, waitMs = 1000, settleBeforeUpMs = 0) => {
    const actualFrom = await canvasPoint(from[0], from[1]);
    const actualTo = await canvasPoint(to[0], to[1]);
    const beforeAudio = await audioStarts();
    await page.mouse.move(actualFrom.x, actualFrom.y);
    await page.mouse.down();
    await page.mouse.move(actualTo.x, actualTo.y, { steps: 16 });
    if (settleBeforeUpMs) await sleep(settleBeforeUpMs);
    await page.mouse.up();
    await sleep(waitMs);
    const afterAudio = await audioStarts();
    report.actions.push({
      action: 'drag', label, from, to, actualFrom, actualTo,
      audioStartsDelta: afterAudio - beforeAudio, at: new Date().toISOString(),
    });
    return afterAudio - beforeAudio;
  };
  const timedShot = async (name, targetMs) => {
    const remaining = lastClickAt + targetMs - Date.now();
    if (remaining > 0) await sleep(remaining);
    const actualMs = Date.now() - lastClickAt;
    await shot(name, { targetMs, actualMs, errorMs: actualMs - targetMs });
  };
  const assert = (name, passed, evidence) => {
    report.assertions.push({ name, passed: !!passed, evidence, at: new Date().toISOString() });
    if (!passed) throw new Error(`ASSERTION_FAILED: ${name}: ${evidence}`);
  };
  const waitLog = async (pattern, timeoutMs, label) => {
    await waitUntil(() => consoleText.some(text => pattern.test(text)), timeoutMs, label);
    return consoleText.filter(text => pattern.test(text)).slice(-1)[0];
  };
  const sinceConsole = () => consoleText.length;
  const consoleAfter = index => consoleText.slice(index);

  try {
    await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await waitLog(/framework ready/i, 90000, 'Unity framework ready');
    await sleep(7000);
    await shot('00_login_720x1280.png');
    await page.evaluate(() => { const canvas = document.querySelector('#unity-canvas'); if (canvas) canvas.focus(); });

    await click('account-field', 375, 564, 350);
    await page.keyboard.down('Control'); await page.keyboard.press('KeyA'); await page.keyboard.up('Control');
    await page.keyboard.type(ACCOUNT, { delay: 25 });
    await click('password-field', 375, 630, 350);
    await page.keyboard.down('Control'); await page.keyboard.press('KeyA'); await page.keyboard.up('Control');
    await page.keyboard.type(PASSWORD, { delay: 25 });
    await shot('01_login_filled_720x1280.png');
    await click('login-submit', 500, 765, 10000);
    await shot('02_privacy_guide_720x1280.png');
    await click('agreement-check', 174, 1244, 400);
    await click('agreement-ok', 490, 870, 1200);
    await shot('03_server_enter_720x1280.png');
    await click('server-enter', 360, 840, 7000);
    await shot('04_role_select_720x1280.png');
    await click('first-role', 170, 100, 900);
    const roleLogStart = sinceConsole();
    await click('enter-world', 360, 1192);
    const roleReady = await waitLog(/\[Game\].*role ready:\s*111111\b/i, 90000, 'role 111111 ready');
    const mainRoleReady = await waitLog(/\[Scene\].*main role ready/i, 90000, 'main role ready');
    assert('real account 111111 asserted from runtime', /111111/.test(roleReady), roleReady);
    assert('real world main role ready', consoleAfter(roleLogStart).some(text => /main role ready/i.test(text)), mainRoleReady);
    await sleep(25000);
    await shot('05_world_mainui_720x1280.png');

    await click('mainui-role', 74, 1212, 2200);
    await shot('06_role_person_720x1280.png');
    await click('role-fashion-entry-cold', 70, 520);
    await timedShot('10_cold_350ms.png', 350);
    await timedShot('10_cold_1000ms.png', 1000);
    await sleep(4500);
    await shot('10_cold_ready_a.png');
    await sleep(350);
    await shot('10_cold_ready_b.png');
    const coldConsole = consoleText.slice();
    assert('cold Fashion model staged', coldConsole.some(text => /\[UI3D\].*model_clothe_.*709x522/i.test(text)), 'UI3D model_clothe in 709x522 Fashion preview');
    assert('cold Fashion default model preloaded', coldConsole.some(text => /FashionMain.*key=1\|12010001/i.test(text)), 'FashionMain preload key=1|12010001 observed');

    await click('current-fashion-tab-readonly', 95, 1115, 900);
    await shot('11_current_tab_720x1280.png');
    await click('fashion-list-visible-1', 143, 778, 900);
    await shot('12_list_item_1_720x1280.png');
    await click('fashion-list-visible-2', 253, 778, 900);
    await shot('13_list_item_2_720x1280.png');
    for (const [index, y] of [168, 283, 398, 513].entries()) {
      await click(`fashion-color-${index}-readonly`, 117, y, 650);
      await shot(`14_color_${index}_720x1280.png`);
    }
    const dragStart = sinceConsole();
    const dragAudioDelta = await drag(
      'fashion-list-horizontal-to-deep', [610, 778], [280, 778], 1200, 650);
    const dragLogs = consoleAfter(dragStart);
    assert('horizontal drag release is silent',
      !dragLogs.some(text => /\[Fashion\].*PointerClick|\[Fashion\].*41312.*fashion=/i.test(text)),
      dragLogs.join('\n') || 'no Fashion click/protocol log');
    assert('horizontal drag release plays no click sound', dragAudioDelta === 0,
      `audioStartsDelta=${dragAudioDelta}`);
    await shot('17_list_dragged_720x1280.png');
    const deepStart = sinceConsole();
    await click('fashion-list-deep-sweetheart-center', 594, 778, 1800);
    await waitLog(/\[Fashion\].*PointerClick.*fashion=12010008/i, 10000, 'Sweetheart PointerClick');
    const powerReply = await waitLog(/\[Fashion\].*41312.*pos=1 fashion=12010008/i, 15000, 'Sweetheart 41312 reply');
    const deepLogs = consoleAfter(deepStart);
    assert('deep item click keeps FashionId 12010008', deepLogs.some(text => /PointerClick.*fashion=12010008/i.test(text)), deepLogs.join('\n'));
    assert('Sweetheart power read 41312 returned', /pos=1 fashion=12010008/i.test(powerReply), powerReply);
    await shot('18_sweetheart_selected_720x1280.png');

    await shot('19_attribute_before_720x1280.png');
    await drag('fashion-attribute-vertical', [620, 675], [620, 505], 850);
    await shot('19_attribute_after_720x1280.png');
    await click('fashion-level-entry-readonly', 650, 164);
    await timedShot('20_level_350ms.png', 350);
    await timedShot('20_level_1000ms.png', 1000);
    await sleep(1000);
    await shot('20_level_ready.png');
    await click('fashion-level-modal-background-close', 20, 200, 800);
    await shot('21_level_background_closed_720x1280.png');
    await click('fashion-level-reopen', 650, 164, 900);
    await shot('22_level_reopen_720x1280.png');
    await click('fashion-level-close-button', 650, 333, 800);
    await shot('23_level_button_closed_720x1280.png');

    await click('fashion-return-to-role', 670, 1115, 900);
    await shot('24_return_role_720x1280.png');
    const warmStart = sinceConsole();
    await click('role-fashion-entry-warm', 70, 520);
    await timedShot('30_warm_350ms.png', 350);
    await timedShot('30_warm_1000ms.png', 1000);
    await sleep(2500);
    await shot('30_warm_ready_a.png');
    await sleep(350);
    await shot('30_warm_ready_b.png');
    assert('warm reopen stages Fashion model', consoleAfter(warmStart).some(text => /\[UI3D\].*model_clothe_.*709x522/i.test(text)), consoleAfter(warmStart).join('\n'));
    await click('fashion-return-before-wide', 670, 1115, 800);

    await page.setViewport({ width: 1920, height: 1080, deviceScaleFactor: 1 });
    await sleep(600);
    await shot('40_role_1920x1080.png');
    await click('role-fashion-entry-wide', 70, 520);
    await timedShot('41_wide_350ms.png', 350);
    await timedShot('41_wide_1000ms.png', 1000);
    await sleep(2500);
    await shot('41_wide_ready.png');
    const wideDragStart = sinceConsole();
    const wideDragAudioDelta = await drag(
      'wide-fashion-list-horizontal', [610, 778], [280, 778], 1200, 650);
    const wideDragLogs = consoleAfter(wideDragStart);
    assert('wide horizontal drag release is silent',
      !wideDragLogs.some(text => /\[Fashion\].*PointerClick|\[Fashion\].*41312.*fashion=/i.test(text)),
      wideDragLogs.join('\n') || 'no Fashion click/protocol log');
    assert('wide horizontal drag release plays no click sound', wideDragAudioDelta === 0,
      `audioStartsDelta=${wideDragAudioDelta}`);
    await shot('42_wide_list_dragged.png');
    const wideDeepStart = sinceConsole();
    await click('wide-fashion-sweetheart', 594, 778, 1800);
    assert('wide Sweetheart identity remains 12010008', consoleAfter(wideDeepStart).some(text => /PointerClick.*fashion=12010008/i.test(text)), consoleAfter(wideDeepStart).join('\n'));
    await shot('43_wide_sweetheart.png');
    await drag('wide-fashion-attribute-vertical', [620, 675], [620, 505], 850);
    await shot('44_wide_attribute_after.png');
    await click('wide-fashion-level-entry-readonly', 650, 164, 1000);
    await shot('45_wide_level.png');
    await click('wide-fashion-level-close-button', 650, 333, 800);
    await click('wide-fashion-return-role', 670, 1115, 900);
    await shot('46_wide_return_role.png');

    const sentWrites = consoleText.filter(text => /\[Net\].*sent proto=(41301|41302|41303|41304|41305|41306|41316)\b/i.test(text));
    report.writeTransactions.sent = sentWrites;
    assert('no unauthorized Fashion write transaction', sentWrites.length === 0, sentWrites.length ? sentWrites.join('\n') : 'none');
    const catalogMatch = report.fingerprints.embeddedCatalogSha256 === report.fingerprints.serverCatalogSha256;
    assert('embedded and ServerData catalog are same batch', catalogMatch, JSON.stringify(report.fingerprints));
    report.unitySessionValid = true;
  } catch (error) {
    report.unitySessionValid = false;
    report.failures.push({ kind: 'exception', text: error && error.stack ? error.stack : String(error) });
    try { await shot('99_failure.png'); } catch (_) { /* keep primary error */ }
  } finally {
    report.finishedAt = new Date().toISOString();
    report.performance = await page.metrics().catch(() => null);
    fs.writeFileSync(path.join(OUT, 'headless-report.json'), JSON.stringify(report, null, 2) + '\n');
    await browser.close();
  }

  if (report.failures.length || report.assertions.some(assertion => !assertion.passed)) {
    console.error(`fashion-main-pos1 failures=${report.failures.length}`);
    process.exitCode = 2;
  } else {
    console.log(`fashion-main-pos1 screenshots=${report.actions.filter(action => action.action === 'screenshot').length}`);
  }
})().catch(error => {
  console.error(error && error.stack ? error.stack : String(error));
  process.exit(2);
});
