// 启动计时探针:自适应等待(轮询控制台标志),量 页面→引擎就绪→登录页→进世界 的真实耗时。
// 用法: [PUPPETEER_EXEC=...] [MEM_PROFILE_DIR=...] node boot_timing.cjs [url]
const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const URL = process.argv[2] || 'http://223.109.142.26/';
const OUT = path.join(__dirname, 'out_timing');
fs.mkdirSync(OUT, { recursive: true });

(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    executablePath: process.env.PUPPETEER_EXEC || undefined,
    userDataDir: process.env.MEM_PROFILE_DIR || undefined,
    args: ['--no-sandbox', '--enable-webgl', '--use-gl=angle', '--window-size=720,1280'],
  });
  const page = await browser.newPage();
  await page.setViewport({ width: 720, height: 1280 });

  const marks = {};
  const t0 = Date.now();
  const sec = () => ((Date.now() - t0) / 1000).toFixed(1);
  const waitMark = (name) => new Promise((resolve) => { marks[name] = resolve; });
  page.on('console', (m) => {
    const t = m.text();
    if (t.includes('framework ready') && marks.fw) { console.log(`引擎+框架就绪: ${sec()}s`); marks.fw(); marks.fw = null; }
    if (t.includes('进入游戏成功') && marks.entered) { console.log(`登录成功(10004): ${sec()}s`); marks.entered(); marks.entered = null; }
    if (t.includes('GAME_START:登录模块退下') && marks.gs) { console.log(`GAME_START: ${sec()}s`); marks.gs(); marks.gs = null; }
    if (t.includes('login views released') && marks.world) { console.log(`进世界揭幕(实体就绪): ${sec()}s`); marks.world(); marks.world = null; }
  });
  const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
  const withTimeout = (p, ms, tag) => Promise.race([p, sleep(ms).then(() => console.log(`${tag} 超时(${ms / 1000}s)未到`))]);

  const fwWait = waitMark('fw'); marks.fw = fwWait ? marks.fw : marks.fw; // placeholder
  const pFw = new Promise((r) => (marks.fw = r));
  const pEntered = new Promise((r) => (marks.entered = r));
  const pGs = new Promise((r) => (marks.gs = r));
  const pWorld = new Promise((r) => (marks.world = r));

  console.log(`加载 ${URL}(${process.env.MEM_PROFILE_DIR ? '热缓存' : '冷缓存'})`);
  await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 60000 });
  console.log(`页面就绪: ${sec()}s`);

  await withTimeout(pFw, 120000, '框架就绪');
  await sleep(12000); // 登录模块预载+登录页渲染
  await page.screenshot({ path: path.join(OUT, '1_login.png') });

  await page.evaluate(() => { const c = document.querySelector('#unity-canvas'); if (c) c.focus(); });
  await page.mouse.click(375, 543); await sleep(600);
  await page.keyboard.down('Control'); await page.keyboard.press('KeyA'); await page.keyboard.up('Control');
  await page.keyboard.type('123123', { delay: 50 }); await sleep(300);
  await page.mouse.click(375, 605); await sleep(600);
  await page.keyboard.type('123123', { delay: 50 }); await sleep(300);
  await page.mouse.click(490, 745); await sleep(8000);
  await page.mouse.click(490, 855); await sleep(3000);   // 隐私同意
  await page.mouse.click(360, 1022);                      // 选服进入
  await withTimeout(pEntered, 60000, '登录');
  await sleep(15000); // 选角页预载(冷网下载模型)
  await page.screenshot({ path: path.join(OUT, '2_roleselect.png') });
  await page.mouse.click(360, 1192);                      // 踏八仙界
  await withTimeout(pGs, 60000, 'GAME_START');
  await withTimeout(pWorld, 60000, '进世界');
  await sleep(3000);
  await page.screenshot({ path: path.join(OUT, '3_world.png') });
  console.log('done');
  await browser.close();
})();
