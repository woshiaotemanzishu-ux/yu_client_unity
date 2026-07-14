// 内存探针:全链进世界挂机,分阶段采 wasm 堆(SxUnity.Module.HEAPU8)与 JS 堆,观察增长趋势。
// 用法: node memory_probe.cjs [url] [账号] [密码]
const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const URL = process.argv[2] || 'http://127.0.0.1:8090/';
const ACCOUNT = process.argv[3] || '123123';
const PASSWORD = process.argv[4] || '123123';
const OUT = path.join(__dirname, 'out_memory');
fs.mkdirSync(OUT, { recursive: true });

const POS = {
  account: { x: 375, y: 543 },
  password: { x: 375, y: 605 },
  loginBtn: { x: 490, y: 745 },
};

(async () => {
  // MEM_PROFILE_DIR 指定持久化浏览器档案 → IndexedDB/UnityCache 跨次保留,可测"热缓存二次进入"
  // PUPPETEER_EXEC 指定浏览器可执行文件(如系统 Edge;CfT 对 jzy:80 有 ERR_BLOCKED_BY_CLIENT 怪癖)
  const browser = await puppeteer.launch({
    headless: 'new',
    executablePath: process.env.PUPPETEER_EXEC || undefined,
    userDataDir: process.env.MEM_PROFILE_DIR || undefined,
    args: ['--no-sandbox', '--enable-webgl', '--use-gl=angle', '--window-size=720,1280', '--enable-precise-memory-info'],
  });
  const page = await browser.newPage();
  await page.setViewport({ width: 720, height: 1280 });

  const log = fs.createWriteStream(path.join(OUT, 'console.log'));
  const memlog = fs.createWriteStream(path.join(OUT, 'memory.log'));
  const stamp = () => new Date().toISOString().slice(11, 23);
  page.on('console', (m) => {
    const t = m.text();
    if (t.includes('AudioContext')) return;
    log.write(`[${stamp()}][${m.type()}] ${t}\n`);
  });
  page.on('error', (e) => { log.write(`[${stamp()}][PAGE-CRASH] ${e.message}\n`); memlog.write(`[PAGE-CRASH] ${e.message}\n`); });
  page.on('pageerror', (e) => log.write(`[${stamp()}][PAGEERROR] ${e.message}\n`));

  const shot = (name) => page.screenshot({ path: path.join(OUT, name) }).catch(() => {});
  const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
  const mb = (b) => (b == null ? 'n/a' : (b / 1048576).toFixed(1) + 'MB');

  async function sample(tag) {
    let m = {};
    try { m = await page.metrics(); } catch (e) {}
    let r = { wasmHeap: null, jsUsed: null, jsTotal: null };
    try {
      r = await page.evaluate(() => {
        const o = { wasmHeap: null, jsUsed: null, jsTotal: null };
        try { const u = window.SxUnity; if (u && u.Module && u.Module.HEAPU8) o.wasmHeap = u.Module.HEAPU8.length; } catch (e) {}
        try { if (performance.memory) { o.jsUsed = performance.memory.usedJSHeapSize; o.jsTotal = performance.memory.totalJSHeapSize; } } catch (e) {}
        return o;
      });
    } catch (e) {}
    const line = `[${stamp()}][${tag}] wasm堆=${mb(r.wasmHeap)} JS堆=${mb(r.jsUsed)}/${mb(r.jsTotal)} DOM节点=${m.Nodes || 'n/a'} 监听器=${m.JSEventListeners || 'n/a'}`;
    console.log(line);
    memlog.write(line + '\n');
  }

  console.log(`loading ${URL}`);
  await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 30000 });

  await sleep(8000); await sample('boot_8s');
  await sleep(17000); await sample('login_page_25s'); await shot('1_login.png');

  await page.evaluate(() => { const c = document.querySelector('#unity-canvas'); if (c) c.focus(); });
  await page.mouse.click(POS.account.x, POS.account.y);
  await sleep(500);
  await page.keyboard.down('Control'); await page.keyboard.press('KeyA'); await page.keyboard.up('Control');
  await page.keyboard.type(ACCOUNT, { delay: 50 });
  await sleep(300);
  await page.mouse.click(POS.password.x, POS.password.y);
  await sleep(500);
  await page.keyboard.type(PASSWORD, { delay: 50 });
  await sleep(300);
  await page.mouse.click(POS.loginBtn.x, POS.loginBtn.y);
  await sleep(8000);
  await page.mouse.click(490, 855); // 隐私同意
  await sleep(3000);
  await page.mouse.click(360, 1022); // 选服进入
  await sleep(10000); await sample('roleselect'); await shot('2_roleselect.png');

  await page.mouse.click(360, 1192); // 踏八仙界进世界
  await sleep(15000); await sample('world_enter_15s'); await shot('3_world.png');

  await page.mouse.click(660, 955); // 开启挂机
  for (let i = 1; i <= 5; i++) {
    await sleep(30000);
    await sample(`battle_${i * 30}s`);
  }
  await shot('4_battle_end.png');

  // shader 警告核对:控制台里不应再出现 additive UI shader missing
  log.end(); memlog.end();
  await sleep(500);
  const consoleTxt = fs.readFileSync(path.join(OUT, 'console.log'), 'utf8');
  console.log('shader_missing_warn=' + (consoleTxt.includes('additive UI shader missing') ? 'STILL_PRESENT' : 'GONE'));
  console.log('tpl_bind_errors=' + (consoleTxt.match(/Bind 字段未绑定/g) || []).length);
  await browser.close();
  console.log('done');
})();
