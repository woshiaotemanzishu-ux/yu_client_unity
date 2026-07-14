// 无头加载 WebGL 包,采集控制台/页面错误/失败请求 + 定时截图。
// 用法: node capture_boot.cjs [url] [秒数]
const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const URL = process.argv[2] || 'http://127.0.0.1:8090/';
const DURATION_S = Number(process.argv[3] || 60);
const OUT = path.join(__dirname, 'out');
fs.mkdirSync(OUT, { recursive: true });

(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    args: ['--no-sandbox', '--enable-webgl', '--use-gl=angle', '--window-size=720,1280'],
  });
  const page = await browser.newPage();
  await page.setViewport({ width: 720, height: 1280 });

  const logPath = path.join(OUT, 'console.log');
  const log = fs.createWriteStream(logPath);
  const stamp = () => new Date().toISOString().slice(11, 23);

  page.on('console', (m) => log.write(`[${stamp()}][${m.type()}] ${m.text()}\n`));
  page.on('pageerror', (e) => log.write(`[${stamp()}][PAGEERROR] ${e.message}\n`));
  page.on('requestfailed', (r) =>
    log.write(`[${stamp()}][REQFAIL] ${r.url()} :: ${r.failure() && r.failure().errorText}\n`));
  page.on('response', (r) => {
    if (r.status() >= 400) log.write(`[${stamp()}][HTTP${r.status()}] ${r.url()}\n`);
  });

  console.log(`loading ${URL}, capturing ${DURATION_S}s -> ${OUT}`);
  await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 30000 }).catch((e) => {
    log.write(`[GOTO-FAIL] ${e.message}\n`);
  });

  const shots = [5, 15, 30, DURATION_S - 2].filter((t) => t > 0 && t <= DURATION_S);
  let elapsed = 0;
  for (const t of shots) {
    await new Promise((res) => setTimeout(res, (t - elapsed) * 1000));
    elapsed = t;
    await page.screenshot({ path: path.join(OUT, `shot_${t}s.png`) }).catch(() => {});
    console.log(`shot at ${t}s`);
  }

  log.end();
  await browser.close();
  console.log('done');
})();
