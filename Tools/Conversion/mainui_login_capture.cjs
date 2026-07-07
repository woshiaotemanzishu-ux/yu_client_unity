/* mainui_login_capture.cjs — 用测试账号直登老客户端,进主城,关弹窗,采 MainUI 全量快照
 * 用法: node mainui_login_capture.cjs <账号> <密码> <输出目录>
 */
const fs = require('fs');
const path = require('path');
const os = require('os');
const { chromium } = require('e:/GitProject/yu_client_unity/output/node_modules/playwright');

const ACC = process.argv[2] || '123123';
const PWD = process.argv[3] || '123123';
const OUT = process.argv[4] || __dirname;
const SHOTS = path.join(OUT, '_shots');

// 常驻 HUD / 无害视图白名单(可见也不算"挡路弹窗")
const WHITELIST = /^(MainUI|NameBoard|MessageItem|FirstRechargeBubble|FunctionOpenIcon|UIJoyStick|WaitforOpenViewLoading|FightingUpView|LoginBgView|ActivityIcon|FuncBoardView)/;

// 只导出 UI 视图(NameBoard/MessageItem 是场景名牌/飘字,不进烤制)
const EXPORT = /^(MainUI|UIJoyStick|FunctionOpenIcon|FirstRechargeBubble|ActivityIcon|FuncBoardView)/;

// 去环序列化:只丢真正的祖先环,不丢共享引用
function safeStringify(root) {
  const stack = new Set();
  const helper = (v) => {
    if (v === null || typeof v !== 'object') return v;
    if (stack.has(v)) return undefined;
    stack.add(v);
    let out;
    if (Array.isArray(v)) out = v.map(helper);
    else { out = {}; for (const k of Object.keys(v)) { const h = helper(v[k]); if (h !== undefined) out[k] = h; } }
    stack.delete(v);
    return out;
  };
  return JSON.stringify(helper(root));
}

(async () => {
  fs.mkdirSync(OUT, { recursive: true });
  fs.mkdirSync(SHOTS, { recursive: true });
  const tmp = path.join(os.tmpdir(), 'ps_' + process.pid + '.mjs');
  fs.copyFileSync('e:/GitProject/yu_client/tools/yu-resource-tool/frontend/src/utils/pageSnapshot.js', tmp);
  const SNAP = (await import('file:///' + tmp.split(path.sep).join('/'))).PAGE_SNAPSHOT_SCRIPT;

  const browser = await chromium.launch({ headless: true, channel: 'msedge' });
  const page = await browser.newPage({ viewport: { width: 720, height: 1280 } });
  await page.goto('http://127.0.0.1:8090/index.html', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForFunction(() => !!(window.Laya && window.Laya.stage), { timeout: 30000 });
  await page.waitForTimeout(9000);

  const inject = async () => { await page.evaluate(SNAP + '; void 0'); };
  const shot = async (n) => { await page.screenshot({ path: path.join(SHOTS, n) }); console.log('SHOT', n); };
  await inject();

  // 状态导出:所有已加载视图的 meta(name/visible)
  const getState = async () => page.evaluate(() => {
    try {
      const l = window.__sxListLoadedPages__();
      const names = (l.views || []).map(v => v.name);
      const s = window.__sxExportPageSnapshots__(names);
      return (s.views || []).map(v => ({ name: v.meta.name, visible: v.meta.visible !== false, nodes: v.nodeCount }));
    } catch (e) { return []; }
  });

  // 在指定视图节点树里找 close 类节点,返回累计坐标中心
  const findClose = async (viewName) => page.evaluate((vn) => {
    try {
      const s = window.__sxExportPageSnapshots__([vn]);
      const v = (s.views || [])[0];
      if (!v) return null;
      let best = null;
      const walk = (n, ax, ay) => {
        const x = ax + (n.x || 0), y = ay + (n.y || 0);
        const nm = (n.name || '').toLowerCase();
        if (n.effectiveVisible !== false && /close|guanbi|_btn_x\b|btn_quit|_img_x\b/.test(nm)) {
          best = { name: n.name, cx: Math.round(x + (n.width || 40) / 2), cy: Math.round(y + (n.height || 40) / 2) };
        }
        (n.children || []).forEach(c => walk(c, x, y));
      };
      const t = v.nodeTree;
      // 根偏移:视图 root 的 x,y 已是舞台坐标
      walk(t, 0, 0);
      return best;
    } catch (e) { return null; }
  }, viewName);

  // 登录:清空输入框再输入
  const typeInto = async (x, y, text) => {
    await page.mouse.click(x, y); await page.waitForTimeout(500);
    await page.keyboard.press('Control+a'); await page.keyboard.press('Backspace');
    await page.keyboard.type(text, { delay: 25 }); await page.waitForTimeout(300);
  };
  await shot('00_login.png');
  await typeInto(408, 525, ACC);
  await typeInto(408, 590, PWD);
  await page.mouse.click(490, 718);   // 登录
  console.log('LOGIN fired acc=' + ACC);
  await page.waitForTimeout(8000);
  await inject();
  await shot('01_after_login.png');

  // 状态机:同意协议→踏入仙界→选角进入→跳对话→关弹窗→干净主城
  let clean = 0;
  for (let k = 0; k < 40 && clean < 3; k++) {
    const st = await getState();
    const vis = (nm) => st.some(v => v.name === nm && v.visible);
    const inCity = vis('MainUITopView');
    const blockers = st.filter(v => v.visible && !WHITELIST.test(v.name));
    if (k % 3 === 0 || blockers.length) console.log(`[${k}] city=${inCity} blockers=${blockers.map(b => b.name).join(',') || '-'}`);

    if (vis('LoginAlertView')) { clean = 0; await page.mouse.click(460, 840); }
    else if (vis('LoginEnterView')) { clean = 0; await page.mouse.click(360, 930); await page.waitForTimeout(3000); }
    else if (vis('LoginSelectRoleView') || vis('LoginCreateRoleView')) { clean = 0; await page.mouse.click(360, 1120); await page.waitForTimeout(5000); }
    else if (vis('DialogueView')) { clean = 0; await page.mouse.click(45, 565); }
    else if (inCity && blockers.length) {
      clean = 0;
      const b = blockers[0];
      const c = await findClose(b.name);
      if (c) { console.log(`close ${b.name} via ${c.name} @(${c.cx},${c.cy})`); await page.mouse.click(c.cx, c.cy); }
      else { console.log(`no close btn in ${b.name}, screenshot`); await shot(`popup_${b.name}.png`); await page.mouse.click(360, 100); } // 点空白尝试关闭
    }
    else if (inCity) clean++;
    await page.waitForTimeout(4000);
    await inject();
  }

  await page.waitForTimeout(5000);
  await shot('50_city.png');

  // 导出全部已加载视图,一视图一文件(烤制器格式)
  const list = await page.evaluate(() => window.__sxListLoadedPages__());
  const names = (list.views || []).map(v => v.name);
  console.log('FINAL LOADED:', names.join(', '));
  const exportNames = names.filter(n => EXPORT.test(n));
  const snap = await page.evaluate(ns => window.__sxExportPageSnapshots__(ns), exportNames);
  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  for (const v of snap.views || []) {
    const one = { version: snap.version, stage: snap.stage, views: [v] };
    const file = path.join(OUT, `page_snapshot_${v.meta.name}_${stamp}.json`);
    try {
      fs.writeFileSync(file, safeStringify(one), 'utf8');
      console.log(`SAVED ${v.meta.name} nodes=${v.nodeCount} visible=${v.meta.visible}`);
    } catch (e) { console.log(`SAVEFAIL ${v.meta.name}: ${String(e).slice(0, 120)}`); }
  }
  await browser.close();
  console.log('DONE');
})().catch(e => { console.error('ERR:', String(e)); process.exit(1); });
