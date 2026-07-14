// 大妖副本专项:进世界→点左侧[斩妖]→尝试进副本,验证同图无感切换(reuse tiles/main role reused)。
const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const URL = process.argv[2] || 'http://127.0.0.1:8090/';
const OUT = path.join(__dirname, 'out_boss');
fs.mkdirSync(OUT, { recursive: true });

const POS = {
  account: { x: 375, y: 543 },
  password: { x: 375, y: 605 },
  loginBtn: { x: 490, y: 745 },
};

(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    userDataDir: process.env.MEM_PROFILE_DIR || undefined,
    args: ['--no-sandbox', '--enable-webgl', '--use-gl=angle', '--window-size=720,1280'],
  });
  const page = await browser.newPage();
  await page.setViewport({ width: 720, height: 1280 });
  const log = fs.createWriteStream(path.join(OUT, 'console.log'));
  const stamp = () => new Date().toISOString().slice(11, 23);
  page.on('console', (m) => { const t = m.text(); if (!t.includes('AudioContext')) log.write(`[${stamp()}][${m.type()}] ${t}\n`); });
  const shot = (n) => page.screenshot({ path: path.join(OUT, n) }).catch(() => {});
  const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

  await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await sleep(25000);
  await page.evaluate(() => { const c = document.querySelector('#unity-canvas'); if (c) c.focus(); });
  await page.mouse.click(POS.account.x, POS.account.y); await sleep(500);
  await page.keyboard.down('Control'); await page.keyboard.press('KeyA'); await page.keyboard.up('Control');
  await page.keyboard.type('123123', { delay: 50 }); await sleep(300);
  await page.mouse.click(POS.password.x, POS.password.y); await sleep(500);
  await page.keyboard.type('123123', { delay: 50 }); await sleep(300);
  await page.mouse.click(POS.loginBtn.x, POS.loginBtn.y); await sleep(8000);
  await page.mouse.click(490, 855); await sleep(3000);   // 隐私同意
  await page.mouse.click(360, 1022); await sleep(10000); // 选服
  await page.mouse.click(360, 1192); await sleep(15000); // 进世界
  await shot('1_world.png');

  // 左侧[斩妖]入口(用户截图 466x849 中约 (47,622) → 720x1280 约 (73,938));点开后中央可能有挑战按钮,盲点几处常见位置
  await page.mouse.click(73, 938); await sleep(4000); await shot('2_after_zhanyao.png');
  await page.mouse.click(360, 900); await sleep(3000); await shot('3_try_a.png');   // 面板下方按钮位A
  await page.mouse.click(360, 1000); await sleep(3000); await shot('4_try_b.png');  // 面板下方按钮位B
  await sleep(10000); await shot('5_final.png');

  log.end(); await sleep(300);
  const txt = fs.readFileSync(path.join(OUT, 'console.log'), 'utf8');
  const dun = txt.match(/12005 ok: sceneId=\d+ dunId=(\d+)/g) || [];
  console.log('12005_events=' + JSON.stringify(dun));
  console.log('reuse_tiles=' + (txt.match(/reuse tiles/g) || []).length);
  console.log('role_reused=' + (txt.match(/main role reused/g) || []).length);
  console.log('mask_shown=' + (txt.match(/SceneTransitionMask/g) || []).length);
  await browser.close();
  console.log('done');
})();
