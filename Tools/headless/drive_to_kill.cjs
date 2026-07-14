// 登录已有号→进世界→自动推教学(定期点"完成任务/跳过")→盯到杀怪任务 100030,
// 判定:死循环是否消失(100030 是否 spam)/攻击是否发出(20001/attack unblocked)/任务是否推进。
const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const URL = process.argv[2] || 'http://127.0.0.1:8090/';
const OUT = path.join(__dirname, 'out_kill');
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
  const keep = /\[Task\]|\[AutoFight\]|\[Combat\]|\[Fight\]|\[Scene\] MoveToNpc|task target|auto task kill|attack|damage|20001|20002|hp=|升级|Level|经验|PAGEERROR|PAGE-CRASH/i;
  page.on('console', (m) => { const t = m.text(); if (keep.test(t)) log.write(`[${stamp()}] ${t}\n`); });
  page.on('pageerror', (e) => log.write(`[${stamp()}][PAGEERROR] ${e.message}\n`));

  const shot = (n) => page.screenshot({ path: path.join(OUT, n) }).catch(() => {});
  const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

  await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await sleep(26000);
  await page.evaluate(() => { const c = document.querySelector('#unity-canvas'); if (c) c.focus(); });
  await page.mouse.click(375, 543); await sleep(400);
  await page.keyboard.down('Control'); await page.keyboard.press('KeyA'); await page.keyboard.up('Control');
  await page.keyboard.type('123123', { delay: 40 });
  await page.mouse.click(375, 605); await sleep(400);
  await page.keyboard.type('123123', { delay: 40 });
  await page.mouse.click(490, 745); await sleep(7000); // 登录
  await page.mouse.click(490, 855); await sleep(3000); // 同意隐私
  await page.mouse.click(360, 1022); await sleep(9000); // 踏八仙界→选角
  // 选第 4 个角色(东方运生,3级,教学杀怪任务进行中)→踏八仙界进世界
  await page.mouse.click(620, 75); await sleep(3000);
  await shot('a_roleselect.png');
  await page.mouse.click(360, 1192); await sleep(14000);
  await shot('a_world.png');

  // 前 60 秒:只点"完成任务/跳过"推对话(别碰挂机按钮——那会把任务自动战斗切成手动挂机,污染观察);
  // 之后 5 分钟纯观察,任务链自驱(对标用户实测:自动任务无人值守自己跑)。
  for (let i = 0; i < 10; i++) {
    await page.mouse.click(590, 1140); await sleep(200);
    await page.mouse.click(80, 555); await sleep(200);
    await sleep(5500);
    if (i % 3 === 0) await shot(`t_${i * 6}s.png`);
  }
  for (let i = 0; i < 10; i++) {
    await sleep(30000);
    await shot(`obs_${60 + i * 30}s.png`);
  }
  await shot('z_final.png');

  log.end();
  await browser.close();
  console.log('done');
})();
