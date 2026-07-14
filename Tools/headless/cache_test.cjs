// 缓存实测:同一持久化 Profile 连续两次加载,对比 框架就绪耗时 / 登录页耗时 / UnityCache 命中率。
const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const URL = process.argv[2] || 'http://127.0.0.1:8090/';
const PROFILE = path.join(__dirname, 'cache_profile');

async function oneRun(label) {
  const browser = await puppeteer.launch({
    headless: 'new',
    userDataDir: PROFILE,
    args: ['--no-sandbox', '--enable-webgl', '--use-gl=angle', '--window-size=720,1280'],
  });
  const page = await browser.newPage();
  await page.setViewport({ width: 720, height: 1280 });

  let stored = 0, cacheHit = 0, frameworkReadyAt = 0, bootDownloadAt = 0;
  const t0 = Date.now();
  page.on('console', (m) => {
    const t = m.text();
    if (t.includes('successfully downloaded and stored')) stored++;
    else if (t.includes('loaded from the browser cache') || t.includes('revalidated')) cacheHit++;
    if (t.includes('framework ready') && !frameworkReadyAt) frameworkReadyAt = Date.now() - t0;
    if (t.includes('Boot: download') && !bootDownloadAt) bootDownloadAt = Date.now() - t0;
  });

  await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await new Promise(r => setTimeout(r, 40000)); // 到登录页稳定
  const total = Date.now() - t0;
  await browser.close();
  console.log(`${label}: framework=${frameworkReadyAt}ms bootDL@${bootDownloadAt}ms total40s窗口 stored=${stored} cacheHit=${cacheHit}`);
  return { frameworkReadyAt, stored, cacheHit };
}

(async () => {
  fs.rmSync(PROFILE, { recursive: true, force: true }); // 干净起步
  await oneRun('第1次(冷)');
  await oneRun('第2次(应命中缓存)');
  await oneRun('第3次(应命中缓存)');
})();
