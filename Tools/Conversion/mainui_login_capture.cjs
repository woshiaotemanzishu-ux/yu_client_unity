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
const URL = process.argv[5] || 'http://127.0.0.1:8090/index.html';
const ROUTE = process.argv[6] || '';
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
  await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 30000 });
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
          const b = n.globalBounds;
          best = { name: n.name,
            cx: Math.round(b ? b.x + b.width / 2 : x + (n.width || 40) / 2),
            cy: Math.round(b ? b.y + b.height / 2 : y + (n.height || 40) / 2) };
        }
        (n.children || []).forEach(c => walk(c, x, y));
      };
      const t = v.nodeTree;
      // 根偏移:视图 root 的 x,y 已是舞台坐标
      walk(t, 0, 0);
      return best;
    } catch (e) { return null; }
  }, viewName);

  // 按视图名正则和节点名取运行态累计坐标；用于固化真实点击路线，避免页面居中或宽高比变化后猜坐标。
  const findNodes = async (viewPattern, nodeName) => page.evaluate(({ vp, nn }) => {
    try {
      const listed = window.__sxListLoadedPages__();
      const matcher = new RegExp(vp, 'i');
      const names = (listed.views || []).map(v => v.name);
      const matched = names.find(name => matcher.test(name));
      // EquipmentView 等页签 View 可能只是 RoleModule 顶层页面的嵌套子树，不会单独进入 loaded-pages。
      // 此时只读导出所有已加载页面并按精确节点名找所属根，点击仍由浏览器真实指针完成。
      const snap = window.__sxExportPageSnapshots__(matched ? [matched] : names);
      const views = snap.views || [];
      const nodes = [];
      let owner = matched || null;
      for (const view of views) {
        const before = nodes.length;
        const walk = (node, ax, ay) => {
          const x = ax + (node.x || 0), y = ay + (node.y || 0);
          if (node.effectiveVisible !== false && node.name === nn) {
            const b = node.globalBounds;
            nodes.push({
              name: node.name,
              x: b ? b.x : x,
              y: b ? b.y : y,
              width: b ? b.width : node.width || 0,
              height: b ? b.height : node.height || 0,
              cx: Math.round(b ? b.x + b.width / 2 : x + (node.width || 0) / 2),
              cy: Math.round(b ? b.y + b.height / 2 : y + (node.height || 0) / 2),
            });
          }
          (node.children || []).forEach(child => walk(child, x, y));
        };
        walk(view.nodeTree, 0, 0);
        if (!owner && nodes.length > before) owner = view.meta.name;
      }
      return { viewName: owner, nodes };
    } catch (e) {
      return { viewName: null, nodes: [], error: String(e) };
    }
  }, { vp: viewPattern, nn: nodeName });

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
      const closeVisible = c && c.cx >= 0 && c.cx <= 720 && c.cy >= 0 && c.cy <= 1280;
      if (closeVisible) { console.log(`close ${b.name} via ${c.name} @(${c.cx},${c.cy})`); await page.mouse.click(c.cx, c.cy); }
      else {
        console.log(`${c ? 'offscreen' : 'no'} close btn in ${b.name}, click visible center`);
        await shot(`popup_${b.name}.png`);
        await page.mouse.click(360, 640); // 奖励/公告类弹层普遍支持点击可视内容关闭。
      }
    }
    else if (inCity) clean++;
    await page.waitForTimeout(4000);
    await inject();
  }

  await page.waitForTimeout(5000);
  await shot('50_city.png');

  // 可选真实点击路线：主界面[角色] → 人物页[属性说明]，用于日常 UI 精修基线。
  // 坐标来自 720x1280 运行态快照；两次点击都由页面自己的命中链处理，不直接调用业务事件。
  if (ROUTE === 'role-instruction') {
    await page.mouse.click(120, 1218);
    await page.waitForTimeout(5000);
    await inject();
    await shot('60_role_person.png');

    await page.mouse.click(670, 858);
    await page.waitForTimeout(2500);
    await inject();
    await shot('61_instruction_top.png');

    // 在说明正文内真实拖动到底，保留末项可达证据，再回到顶部验证回滚。
    await page.mouse.move(360, 805);
    await page.mouse.down();
    await page.mouse.move(360, 500, { steps: 12 });
    await page.mouse.up();
    await page.waitForTimeout(1200);
    await shot('62_instruction_scrolled.png');

    await page.mouse.move(360, 805);
    await page.mouse.down();
    await page.mouse.move(360, 450, { steps: 12 });
    await page.mouse.up();
    await page.waitForTimeout(1200);
    await shot('62b_instruction_bottom.png');

    await page.mouse.click(653, 397);
    await page.waitForTimeout(1200);
    await shot('63_instruction_closed.png');

    await page.mouse.click(670, 858);
    await page.waitForTimeout(1200);
    await shot('64_instruction_reopen.png');
  }

  // 角色页[增强药剂]完整只读基线：真实入口、四档页签、列表弹性拖动、关闭和热重开。
  // 不点击“使用”，避免基线采集静默消耗真实账号道具。
  if (ROUTE === 'role-attribute-potion') {
    await page.mouse.click(120, 1218);
    await page.waitForTimeout(5000);
    await inject();
    await shot('60_role_person.png');

    const entry = await findNodes('EquipmentView', '_btn_attribute');
    console.log('ATTRIBUTE ENTRY', JSON.stringify(entry));
    if (!entry.nodes.length) throw new Error('EquipmentView._btn_attribute not found');
    await page.mouse.click(entry.nodes[0].cx, entry.nodes[0].cy);
    await page.waitForTimeout(3500);
    await inject();
    await shot('61_attribute_potion_tier1.png');

    const tabs = await findNodes('attributePotionView', 'attributePotionTab');
    console.log('ATTRIBUTE TABS', JSON.stringify(tabs));
    const orderedTabs = tabs.nodes.slice().sort((a, b) => a.cx - b.cx);
    for (let i = 0; i < orderedTabs.length; i++) {
      await page.mouse.click(orderedTabs[i].cx, orderedTabs[i].cy);
      await page.waitForTimeout(900);
      await inject();
      await shot(`62_attribute_potion_tier${i + 1}.png`);
    }

    // 对标老端 Content 的弹性拖动：内容被拖开后应自动回到 y=0，不把四行拉出视口。
    const rows = await findNodes('attributePotionView', 'attributePotionItem');
    console.log('ATTRIBUTE ROWS', JSON.stringify(rows));
    if (rows.nodes.length) {
      const first = rows.nodes.slice().sort((a, b) => a.cy - b.cy)[0];
      await page.mouse.move(first.cx, first.cy);
      await page.mouse.down();
      await page.mouse.move(first.cx, first.cy + 130, { steps: 10 });
      await page.mouse.up();
      await page.waitForTimeout(1200);
      await shot('63_attribute_potion_bounce_restored.png');
    }

    const potionView = tabs.viewName || 'attributePotionView';
    const close = await findClose(potionView);
    console.log('ATTRIBUTE CLOSE', JSON.stringify(close));
    if (!close) throw new Error('attributePotionView close not found');
    await page.mouse.click(close.cx, close.cy);
    await page.waitForTimeout(1200);
    await shot('64_attribute_potion_closed.png');

    await page.mouse.click(entry.nodes[0].cx, entry.nodes[0].cy);
    await page.waitForTimeout(1500);
    await inject();
    await shot('65_attribute_potion_reopen.png');
  }

  // 导出全部已加载视图,一视图一文件(烤制器格式)
  const list = await page.evaluate(() => window.__sxListLoadedPages__());
  const names = (list.views || []).map(v => v.name);
  console.log('FINAL LOADED:', names.join(', '));
  const exportNames = names.filter(n => EXPORT.test(n)
    || (ROUTE === 'role-instruction' && n === 'InstructionView')
    || (ROUTE === 'role-attribute-potion' && /EquipmentView|attributePotionView/i.test(n)));
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
