/*
 * 人物→境界(地境/天境)→名誉，同批 Unity WebGL 真实 Canvas 路线。
 * 默认只跑到主界面，PHASE=route 时继续按可见 Graphic 坐标点击整条路线。
 * 用法：
 *   node Tools/headless/role_realm_honour_route.cjs <url> <account> <password> <output>
 */
const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const URL = process.argv[2] || 'http://127.0.0.1:8092/?cdn=http%3A%2F%2F127.0.0.1%3A8092%2Fres';
const ACCOUNT = process.argv[3] || '111111';
const PASSWORD = process.argv[4] || '111111';
const OUT = path.resolve(process.argv[5] || 'output/ui_route_audit/2026-08-09_role_person_realm_honour_web/headless');
const PHASE = process.env.PHASE || 'world';
const ROUTE_SEGMENT = process.env.ROUTE_SEGMENT || 'all';
const VIEWPORT_WIDTH = Number(process.env.VIEWPORT_WIDTH || 720);
const VIEWPORT_HEIGHT = Number(process.env.VIEWPORT_HEIGHT || 1280);
const BASE_WIDTH = 720;
const BASE_HEIGHT = 1280;
fs.mkdirSync(OUT, { recursive: true });

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    executablePath: process.env.PUPPETEER_EXEC || undefined,
    userDataDir: process.env.ROLE_UI_PROFILE || undefined,
    args: [
      '--no-sandbox', '--enable-webgl', '--use-gl=angle',
      `--window-size=${VIEWPORT_WIDTH},${VIEWPORT_HEIGHT}`, '--disable-background-timer-throttling',
    ],
  });
  const page = await browser.newPage();
  await page.setViewport({ width: VIEWPORT_WIDTH, height: VIEWPORT_HEIGHT, deviceScaleFactor: 1 });

  const report = {
    url: URL,
    account: ACCOUNT,
    viewport: `${VIEWPORT_WIDTH}x${VIEWPORT_HEIGHT}`,
    phase: PHASE,
    routeSegment: ROUTE_SEGMENT,
    startedAt: new Date().toISOString(),
    actions: [],
    console: [],
    failures: [],
  };
  page.on('console', (message) => {
    const text = message.text();
    if (/AudioContext|UnityCache|GmApi/.test(text)) return;
    if (/\[Medal\]|\[Title\]|\[Marriage\]|\[MainUIRouter\]|\[Role\]|\[Tip\]|1340|framework ready|GAME_START|login views released|exception|error/i.test(text)) {
      report.console.push({ type: message.type(), text });
    }
  });
  page.on('pageerror', (error) => report.failures.push({ kind: 'pageerror', text: error.message }));
  page.on('requestfailed', (request) => {
    const text = request.failure() && request.failure().errorText;
    // Chromium/UnityCache 在缓存重验证胜出时会主动 abort 原网络分支；后续缓存成功日志证明它不是加载失败。
    if (text === 'net::ERR_ABORTED') return;
    report.failures.push({ kind: 'requestfailed', url: request.url(), text });
  });
  page.on('response', (response) => {
    if (response.status() >= 400) report.failures.push({ kind: `http-${response.status()}`, url: response.url() });
  });

  const shot = async (name) => {
    const file = path.join(OUT, name);
    await page.screenshot({ path: file });
    report.actions.push({ action: 'screenshot', file: name, at: new Date().toISOString() });
  };
  const canvasPoint = async (x, y) => page.evaluate(({ x, y, bw, bh }) => {
    const canvas = document.querySelector('#unity-canvas');
    const rect = canvas ? canvas.getBoundingClientRect() : { left: 0, top: 0, width: innerWidth, height: innerHeight };
    const designAspect = bw / bh;
    const canvasAspect = rect.width / rect.height;
    let contentLeft = rect.left;
    let contentTop = rect.top;
    let contentWidth = rect.width;
    let contentHeight = rect.height;
    // Unity 的竖屏 GameView 在宽视口内保持 9:16，并把额外区域留在两侧；
    // 鼠标坐标必须映射到实际内容矩形，不能按整张 WebGL Canvas 非等比拉伸。
    if (canvasAspect > designAspect) {
      contentWidth = rect.height * designAspect;
      contentLeft += (rect.width - contentWidth) * 0.5;
    } else if (canvasAspect < designAspect) {
      contentHeight = rect.width / designAspect;
      contentTop += (rect.height - contentHeight) * 0.5;
    }
    return { x: contentLeft + x * contentWidth / bw, y: contentTop + y * contentHeight / bh };
  }, { x, y, bw: BASE_WIDTH, bh: BASE_HEIGHT });
  const click = async (label, x, y, waitMs = 900) => {
    const actual = await canvasPoint(x, y);
    await page.mouse.click(actual.x, actual.y);
    report.actions.push({ action: 'click', label, logical: [x, y], actual, at: new Date().toISOString() });
    await sleep(waitMs);
  };
  const drag = async (label, from, to, waitMs = 1000) => {
    const actualFrom = await canvasPoint(from[0], from[1]);
    const actualTo = await canvasPoint(to[0], to[1]);
    await page.mouse.move(actualFrom.x, actualFrom.y);
    await page.mouse.down();
    await page.mouse.move(actualTo.x, actualTo.y, { steps: 12 });
    await page.mouse.up();
    report.actions.push({ action: 'drag', label, from, to, actualFrom, actualTo });
    await sleep(waitMs);
  };

  try {
    await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await sleep(35000);
    await shot('00_login.png');
    await page.evaluate(() => { const canvas = document.querySelector('#unity-canvas'); if (canvas) canvas.focus(); });

    await click('account', 375, 564, 400);
    await page.keyboard.down('Control');
    await page.keyboard.press('KeyA');
    await page.keyboard.up('Control');
    await page.keyboard.type(ACCOUNT, { delay: 35 });
    await click('password', 375, 630, 400);
    await page.keyboard.down('Control');
    await page.keyboard.press('KeyA');
    await page.keyboard.up('Control');
    await page.keyboard.type(PASSWORD, { delay: 35 });
    await shot('01_filled.png');
    await click('login', 500, 765, 10000);

    // ServerEnterView 首次显示会自动打开《隐私保护指引》。先按玩家可见链点击
    // 底部协议勾选区，再点击弹层“同意”；弹层收起后才能点击“踏入仙界”。
    // 这三个检查点分别留图，禁止把仍被协议弹层遮挡的固定延时截图命名为 world。
    await shot('02_privacy_guide.png');
    await click('agreement-check', 174, 1244, 500);
    await shot('02a_privacy_after_check.png');
    await click('agreement-ok', 490, 870, 1200);
    await shot('02b_server_enter_agreed.png');
    await click('server-enter', 360, 840, 12000);
    await shot('02_role_select.png');
    await click('first-role', 170, 100, 1200);
    await click('enter-world', 360, 1192, 25000);
    await shot('03_world.png');

    if (PHASE === 'route') {
      // 主界面底部“角色”实际可见 Graphic。
      await click('mainui-role', 74, 1212, 2200);
      await shot('10_role_person.png');

      // 人物页左侧“境界”。
      if (ROUTE_SEGMENT === 'all' || ROUTE_SEGMENT === 'realm') {
      await click('realm-entry', 102, 390, 2200);
      await shot('11_realm_ground.png');
      await click('sky-tab', 220, 1115, 2400);
      await shot('12_realm_sky_a.png');
      await sleep(800);
      await shot('13_realm_sky_b.png');
        await drag('sky-title-list', [600, 900], [250, 900]);
      await click('sky-visible-later-title', 565, 900, 1800);
      await shot('14_realm_sky_scrolled.png');
        await click('ground-tab', 68, 1115, 1400);
        await click('realm-return', 670, 1115, 1800);
        if (ROUTE_SEGMENT === 'realm') return;
      }

      // 人物页经验条右侧“名”。
      await click('honour-entry', 580, 740, 1800);
      await shot('20_honour_top.png');
      await drag('honour-list', [360, 720], [360, 430]);
      await shot('21_honour_dragged.png');
      if (ROUTE_SEGMENT === 'honour-drag') return;
      await click('honour-close', 650, 270, 1200);
      await click('honour-reopen', 580, 740, 1500);
      await shot('22_honour_reopen.png');
      await click('honour-get', 360, 950, 1800);
      await shot('23_honour_get.png');
    }
  } catch (error) {
    report.failures.push({ kind: 'exception', text: error && error.stack ? error.stack : String(error) });
    try { await shot('99_failure.png'); } catch (_) { /* keep original error */ }
  } finally {
    report.finishedAt = new Date().toISOString();
    fs.writeFileSync(path.join(OUT, 'headless-report.json'), JSON.stringify(report, null, 2));
    await browser.close();
  }

  if (report.failures.length) {
    console.error(`role-realm-honour failures=${report.failures.length}`);
    process.exitCode = 2;
  } else {
    console.log(`role-realm-honour phase=${PHASE} screenshots=${report.actions.filter((x) => x.action === 'screenshot').length}`);
  }
})();
