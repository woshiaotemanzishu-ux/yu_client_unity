// 高级号点怪验证:登录 111111 → 进世界 → 等 8s → 单击一只怪 → 40s 纯观察。
// 判定:单击后是否持续攻击(连续 send 20001 对,~600ms/刀)直到怪死;死后主角是否回站立。
const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const URL = process.argv[2] || 'http://127.0.0.1:8090/';
const ACC = process.argv[3] || '111111';
const PWD = process.argv[4] || '111111';
const OUT = path.join(__dirname, 'out_click');
fs.mkdirSync(OUT, { recursive: true });

(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    args: ['--no-sandbox', '--enable-webgl', '--use-gl=angle', '--window-size=720,1280'],
  });
  const page = await browser.newPage();
  await page.setViewport({ width: 720, height: 1280 });

  const log = fs.createWriteStream(path.join(OUT, 'console.log'));
  const stamp = () => new Date().toISOString().slice(11, 23);
  const keep = /\[Fight\]|\[Combat\]|\[AutoFight\]|click monster|判定死亡|attack blocked|attack unblocked|PAGEERROR|PAGE-CRASH/;
  page.on('console', (m) => { const t = m.text(); if (keep.test(t)) log.write(`[${stamp()}] ${t}\n`); });

  const shot = (n) => page.screenshot({ path: path.join(OUT, n) }).catch(() => {});
  const sleep = (ms) => new Promise(r => setTimeout(r, ms));

  await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await sleep(28000);
  await page.evaluate(() => { const c = document.querySelector('#unity-canvas'); if (c) c.focus(); });
  await page.mouse.click(375, 543); await sleep(400);
  await page.keyboard.down('Control'); await page.keyboard.press('KeyA'); await page.keyboard.up('Control');
  await page.keyboard.type(ACC, { delay: 40 });
  await page.mouse.click(375, 605); await sleep(400);
  await page.keyboard.type(PWD, { delay: 40 });
  await page.mouse.click(490, 745); await sleep(7000);
  await page.mouse.click(490, 855); await sleep(3000);   // 同意(如有)
  await page.mouse.click(360, 1022); await sleep(9000);  // 踏八仙界→选角
  await page.mouse.click(170, 100); await sleep(2000);   // 选第1个角色
  await page.mouse.click(360, 1192); await sleep(14000); // 进世界
  await shot('1_world.png');

  // 单击画面中部偏上找怪(多点几个位置提高命中率,间隔>2s 只算"一次性点击"不构成连点)
  await page.mouse.click(360, 500);
  await sleep(2500);
  await shot('2_after_click.png');

  // 40 秒纯观察:不再点击
  await sleep(15000);
  await shot('3_obs_15s.png');
  await sleep(15000);
  await shot('4_obs_30s.png');
  await sleep(10000);
  await shot('5_final.png');

  log.end();
  await browser.close();
  console.log('done');
})();
