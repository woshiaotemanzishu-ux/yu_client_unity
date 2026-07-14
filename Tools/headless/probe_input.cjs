// 键盘失灵显微探针:字段激活?焦点在哪?单字符能否进?
const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const URL = process.argv[2] || 'http://127.0.0.1:8090/index2.html';
const OUT = path.join(__dirname, 'out_probe');
fs.mkdirSync(OUT, { recursive: true });

(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    args: ['--no-sandbox', '--enable-webgl', '--use-gl=angle', '--window-size=720,1280'],
  });
  const page = await browser.newPage();
  await page.setViewport({ width: 720, height: 1280 });
  const log = [];
  page.on('console', (m) => { const t = m.text(); if (!t.includes('AudioContext') && !t.includes('UnityCache')) log.push(`[${m.type()}] ${t}`); });

  await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await new Promise(r => setTimeout(r, 26000)); // 等登录页

  const focus0 = await page.evaluate(() => (document.activeElement && (document.activeElement.id || document.activeElement.tagName)) || 'none');
  await page.mouse.click(375, 543); // 账号框
  await new Promise(r => setTimeout(r, 800));
  const focus1 = await page.evaluate(() => (document.activeElement && (document.activeElement.id || document.activeElement.tagName)) || 'none');
  await page.screenshot({ path: path.join(OUT, 'p1_after_field_click.png') });

  await page.keyboard.type('a');
  await new Promise(r => setTimeout(r, 800));
  await page.screenshot({ path: path.join(OUT, 'p2_after_one_char.png') });

  // 直接给 canvas 派发键盘事件(绕过 puppeteer 常规通道)
  await page.evaluate(() => {
    const c = document.querySelector('#unity-canvas') || document.body;
    for (const type of ['keydown', 'keypress', 'keyup']) {
      c.dispatchEvent(new KeyboardEvent(type, { key: 'b', code: 'KeyB', keyCode: 66, charCode: type === 'keypress' ? 98 : 0, which: 66, bubbles: true }));
    }
  });
  await new Promise(r => setTimeout(r, 800));
  await page.screenshot({ path: path.join(OUT, 'p3_after_dispatch.png') });

  console.log(JSON.stringify({ focus0, focus1, tail: log.slice(-12) }, null, 2));
  await browser.close();
})();
