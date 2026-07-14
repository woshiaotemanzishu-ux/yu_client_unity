// 进场揭幕专项:选角进世界后每 3s 连拍,验证加载页盖住"实体蹦出"窗口、揭幕时主角/怪/NPC 已齐。
const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const URL = process.argv[2] || 'http://127.0.0.1:8090/';
const OUT = path.join(__dirname, 'out_enter');
fs.mkdirSync(OUT, { recursive: true });

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
  await page.mouse.click(375, 543); await sleep(500);
  await page.keyboard.down('Control'); await page.keyboard.press('KeyA'); await page.keyboard.up('Control');
  await page.keyboard.type('123123', { delay: 50 }); await sleep(300);
  await page.mouse.click(375, 605); await sleep(500);
  await page.keyboard.type('123123', { delay: 50 }); await sleep(300);
  await page.mouse.click(490, 745); await sleep(8000);
  await page.mouse.click(490, 855); await sleep(3000);   // 隐私同意
  await page.mouse.click(360, 1022); await sleep(10000); // 选服
  await shot('0_roleselect.png');

  await page.mouse.click(360, 1192); // 踏八仙界:从这一刻开始连拍
  for (let i = 1; i <= 6; i++) {
    await sleep(3000);
    await shot(`enter_${i * 3}s.png`);
  }

  log.end(); await sleep(300);
  const txt = fs.readFileSync(path.join(OUT, 'console.log'), 'utf8');
  console.log('main_role_ready=' + (txt.match(/main role (ready|reused)/g) || []).length);
  console.log('snapshot=' + (txt.match(/12002 快照/g) || []).length);
  console.log('fallback_timeout=' + (txt.match(/首屏就绪事件超时/g) || []).length);
  await browser.close();
  console.log('done');
})();
