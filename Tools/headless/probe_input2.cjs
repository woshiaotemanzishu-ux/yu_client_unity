// 差分探针2:密码框(无CtrlA)单字符 / 账号框双击直输 / 记住密码勾选(验证 Toggle 点击)
const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const URL = process.argv[2] || 'http://127.0.0.1:8090/index2.html';
const OUT = path.join(__dirname, 'out_probe2');
fs.mkdirSync(OUT, { recursive: true });

(async () => {
  const HEADED = process.env.HEADED === '1';
  const browser = await puppeteer.launch({
    headless: HEADED ? false : 'new',
    args: ['--no-sandbox', '--enable-webgl', '--use-gl=angle', '--window-size=720,1280'],
  });
  const page = await browser.newPage();
  await page.setViewport({ width: 720, height: 1280 });
  page.on('console', (m) => { const t = m.text(); if (t.includes('[Login]') || t.includes('[Tip]') || t.includes('exception') || t.includes('Exception')) console.log('CONSOLE:', t.trim()); });

  await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await new Promise(r => setTimeout(r, 32000)); // 富余等登录页+完全就绪

  // A. 密码框:单击+单字符(无 CtrlA 干扰)
  await page.mouse.click(375, 605);
  await new Promise(r => setTimeout(r, 1000));
  await page.keyboard.type('1', { delay: 100 });
  await new Promise(r => setTimeout(r, 800));
  await page.screenshot({ path: path.join(OUT, 'a_pwd_one_char.png') });

  // B. 记住密码勾选框(约 155,660):Toggle 是否响应(UI 点击对照组)
  await page.mouse.click(155, 660);
  await new Promise(r => setTimeout(r, 800));
  await page.screenshot({ path: path.join(OUT, 'b_toggle.png') });

  // C. 账号框:双击 + 直接输入
  await page.mouse.click(375, 543, { clickCount: 2 });
  await new Promise(r => setTimeout(r, 1000));
  await page.keyboard.type('xy', { delay: 100 });
  await new Promise(r => setTimeout(r, 800));
  await page.screenshot({ path: path.join(OUT, 'c_account_dblclick.png') });

  await browser.close();
  console.log('done');
})();
