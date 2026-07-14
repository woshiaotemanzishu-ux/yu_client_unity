// 无头回归:加载 WebGL 包 → 等登录页 → 填测试号 → 点登录 → 观察 WS 连接与选角。
// 用法: node capture_login.cjs [url] [账号] [密码]
const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const URL = process.argv[2] || 'http://127.0.0.1:8090/';
const ACCOUNT = process.argv[3] || '123123';
const PASSWORD = process.argv[4] || '123123';
const OUT = path.join(__dirname, 'out_login');
fs.mkdirSync(OUT, { recursive: true });

// 720x1280 视口下登录页控件坐标(以 shot_15s.png 实测)
const POS = {
  account: { x: 375, y: 543 },
  password: { x: 375, y: 605 },
  loginBtn: { x: 490, y: 745 },
};

(async () => {
  const browser = await puppeteer.launch({
    headless: 'new',
    args: ['--no-sandbox', '--enable-webgl', '--use-gl=angle', '--window-size=720,1280'],
  });
  const page = await browser.newPage();
  await page.setViewport({ width: 720, height: 1280 });

  const log = fs.createWriteStream(path.join(OUT, 'console.log'));
  const stamp = () => new Date().toISOString().slice(11, 23);
  page.on('console', (m) => {
    const t = m.text();
    if (t.includes('AudioContext')) return; // 无手势噪音
    log.write(`[${stamp()}][${m.type()}] ${t}\n`);
  });
  page.on('error', (e) => log.write(`[${stamp()}][PAGE-CRASH] ${e.message}\n`)); // 标签页崩溃(内存爆等)
  page.on('pageerror', (e) => log.write(`[${stamp()}][PAGEERROR] ${e.message}\n`));
  page.on('requestfailed', (r) => log.write(`[${stamp()}][REQFAIL] ${r.url()} :: ${r.failure() && r.failure().errorText}\n`));
  page.on('response', (r) => { if (r.status() >= 400) log.write(`[${stamp()}][HTTP${r.status()}] ${r.url()}\n`); });

  const shot = (name) => page.screenshot({ path: path.join(OUT, name) }).catch(() => {});
  const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

  console.log(`loading ${URL}`);
  await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 30000 });

  await sleep(3000);
  await shot('0a_boot_3s.png');   // 应显示 HTML 加载层(引擎下载段)
  await sleep(5000);
  await shot('0b_boot_8s.png');   // 应显示 85%+ (游戏资源段)或已交接游戏加载页
  await sleep(17000);
  await shot('1_login_page.png');

  // 填账号:点输入框→全选→输入(先确保画布持焦,否则键盘事件到不了 Unity)
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
  await shot('2_filled.png');

  await page.mouse.click(POS.loginBtn.x, POS.loginBtn.y);
  console.log('login clicked');
  await sleep(8000);
  await shot('3_after_login_8s.png');

  // 隐私保护指引弹窗:点[同意]
  await page.mouse.click(490, 855);
  console.log('agree clicked');
  await sleep(3000);
  await shot('4_after_agree.png');

  // 选服页:点[踏八仙界] → 连网关 → 选角页
  await page.mouse.click(360, 1022);
  console.log('enter clicked');
  await sleep(10000);
  await shot('5_roleselect.png');

  // 点右上"+"空位 → 创角页(验证 WebGL 视频 URL 播放)
  await page.mouse.click(620, 75);
  console.log('create-role slot clicked');
  await sleep(6000);
  await shot('5b_rolecreate_6s.png');
  await sleep(6000);
  await shot('5c_rolecreate_12s.png'); // 与 5b 对比:视频在播则画面不同帧

  // 选角页:点底部[踏八仙界]进世界
  await page.mouse.click(360, 1192);
  console.log('enter world clicked');
  await sleep(15000);
  await shot('6_world_15s.png');

  // 点[开启挂机]自动寻怪攻击,复现真实战斗链路
  await page.mouse.click(660, 955);
  console.log('auto-battle clicked');
  await sleep(10000);
  await shot('7_battle_10s.png');
  await sleep(15000);
  await shot('8_battle_25s.png');
  await sleep(20000);
  await shot('9_battle_45s.png');
  await sleep(20000);
  await shot('10_battle_65s.png');
  // 新号任务链观察窗:自动任务 10 秒一拍,拉长到 ~2 分钟看"杀怪死循环是否消失/攻击是否发出/任务是否推进"
  await sleep(30000);
  await shot('11_task_95s.png');
  await sleep(30000);
  await shot('12_task_125s.png');

  log.end();
  await browser.close();
  console.log('done');
})();
