/* petview_capture.cjs — 用测试账号直登老客户端,进主城关弹窗后打开 MountPetView(坐骑/剑魄同修页签),
 * 采【舞台快照】(UiSnapshot 格式:扁平 nodes + gx/gy,喂 PetCreator 当 1:1 几何事实源)+ 截图。
 * 用法: node petview_capture.cjs <账号> <密码> <输出目录>
 * 产物: oldclient_pet_20_horse_stage.json / oldclient_pet_21_partner_stage.json (+ 同名 .png)
 */
const fs = require('fs');
const path = require('path');
const { chromium } = require('e:/GitProject/yu_client_unity/output/node_modules/playwright');

const ACC = process.argv[2] || '123123';
const PWD = process.argv[3] || '123123';
const OUT = process.argv[4] || 'e:/GitProject/yu_client_unity/output/manual_round';

// 常驻 HUD / 无害视图白名单(可见也不算"挡路弹窗");MountPetView 是目标视图也入白
const WHITELIST = /^(MainUI|NameBoard|MessageItem|FirstRechargeBubble|FunctionOpenIcon|UIJoyStick|WaitforOpenViewLoading|FightingUpView|LoginBgView|ActivityIcon|FuncBoardView|MountPetView|BaseWindowSkin)/;

// 舞台快照采集(同 oldclient_mobile_round.js collectStage,UiSnapshot.Load 消费此格式)
async function collectStage(page) {
  return await page.evaluate(() => {
    const out = [];
    const L = window.Laya;
    function globalOf(node) {
      try {
        const p = node.localToGlobal ? node.localToGlobal(new L.Point(0, 0)) : { x: node.x || 0, y: node.y || 0 };
        return { x: p.x, y: p.y };
      } catch {
        return { x: node.x || 0, y: node.y || 0 };
      }
    }
    function walk(node, depth, parentVisible) {
      if (!node || depth > 16 || out.length > 4000) return;
      const children = node._children || node._childs || [];
      const visible = parentVisible && node.visible !== false;
      const g = globalOf(node);
      const name = node.name ? String(node.name) : '';
      const textValue = node.text != null ? node.text : node.label;
      const text = textValue != null && typeof textValue !== 'object' ? String(textValue) : '';
      const skin = node.skin != null && typeof node.skin !== 'object' ? String(node.skin) : '';
      const cls = (node.constructor && node.constructor.name) || '';
      if (visible && (name || text || skin || depth < 2)) {
        out.push({
          depth, cls, name,
          x: Number(node.x) || 0, y: Number(node.y) || 0,
          gx: Number(g.x) || 0, gy: Number(g.y) || 0,
          w: Number(node.width) || 0, h: Number(node.height) || 0,
          text, skin,
          child: children.length,
          mouseEnabled: node.mouseEnabled !== false,
        });
      }
      for (const child of Array.prototype.slice.call(children, 0, 200)) {
        walk(child, depth + 1, visible);
      }
    }
    walk(L.stage, 0, true);
    return {
      stage: { width: L.stage.width, height: L.stage.height, scaleX: L.stage.scaleX, scaleY: L.stage.scaleY },
      nodes: out,
    };
  });
}

(async () => {
  fs.mkdirSync(OUT, { recursive: true });
  const tmp = path.join(require('os').tmpdir(), 'ps_' + process.pid + '.mjs');
  fs.copyFileSync('e:/GitProject/yu_client/tools/yu-resource-tool/frontend/src/utils/pageSnapshot.js', tmp);
  const SNAP = (await import('file:///' + tmp.split(path.sep).join('/'))).PAGE_SNAPSHOT_SCRIPT;

  const browser = await chromium.launch({ headless: true, channel: 'msedge' });
  const page = await browser.newPage({ viewport: { width: 720, height: 1280 } });
  await page.goto('http://127.0.0.1:8090/index.html', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForFunction(() => !!(window.Laya && window.Laya.stage), { timeout: 30000 });
  await page.waitForTimeout(9000);

  const inject = async () => { await page.evaluate(SNAP + '; void 0'); };
  const shot = async (n) => { await page.screenshot({ path: path.join(OUT, n) }); console.log('SHOT', n); };
  const saveStage = async (label) => {
    const stage = await collectStage(page);
    fs.writeFileSync(path.join(OUT, `${label}_stage.json`), JSON.stringify(stage, null, 2));
    await shot(`${label}.png`);
    console.log(`STAGE ${label} nodes=${stage.nodes.length}`);
  };
  await inject();

  const getState = async () => page.evaluate(() => {
    try {
      const l = window.__sxListLoadedPages__();
      const names = (l.views || []).map(v => v.name);
      const s = window.__sxExportPageSnapshots__(names);
      return (s.views || []).map(v => ({ name: v.meta.name, visible: v.meta.visible !== false, nodes: v.nodeCount }));
    } catch (e) { return []; }
  });

  // 用舞台快照(gx/gy=真全局坐标)找最顶层的 close 类节点——嵌套树 x 累加对负锚点/居中容器会算错(挂机收益 X 实测 920>720 越界)
  const findClose = async () => {
    const stage = await collectStage(page);
    let best = null;
    for (const n of stage.nodes) {
      const nm = (n.name || '').toLowerCase();
      if (!/close|guanbi|btn_quit/.test(nm)) continue;
      if (!(n.w > 0 && n.h > 0)) continue;
      const cx = Math.round(n.gx + n.w / 2), cy = Math.round(n.gy + n.h / 2);
      if (cx < 1 || cx > 719 || cy < 1 || cy > 1279) continue;
      best = { name: n.name, cx, cy };   // DFS 靠后 ≈ 渲染更靠上,取最后命中
    }
    return best;
  };

  const typeInto = async (x, y, text) => {
    await page.mouse.click(x, y); await page.waitForTimeout(500);
    await page.keyboard.press('Control+a'); await page.keyboard.press('Backspace');
    await page.keyboard.type(text, { delay: 25 }); await page.waitForTimeout(300);
  };
  await typeInto(408, 525, ACC);
  await typeInto(408, 590, PWD);
  await page.mouse.click(490, 718);   // 登录
  console.log('LOGIN fired acc=' + ACC);
  await page.waitForTimeout(8000);
  await inject();

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
      const c = await findClose();
      if (c) { console.log(`close ${b.name} via ${c.name} @(${c.cx},${c.cy})`); await page.mouse.click(c.cx, c.cy); }
      else { console.log(`no close btn for ${b.name}`); await page.mouse.click(360, 100); }
    }
    else if (inCity) clean++;
    await page.waitForTimeout(4000);
    await inject();
  }
  console.log('CITY CLEAN');

  // 打开 MountPetView:老端事件 SWITCH_MOUNT_PET_VIEW(index) → OutWardController.OpenMountPetView1。
  // index 0=御风云骑(坐骑,type_id=1) 1=剑魄同修(侍魂,type_id=2)。
  const openTab = async (idx) => {
    await page.evaluate((i) => {
      try {
        const G = window.GlobalEventSystem;
        if (G && G.Fire) G.Fire('SWITCH_MOUNT_PET_VIEW', i);
      } catch (e) {}
    }, idx);
    await page.waitForTimeout(6000);   // 等 16002/16028 回包 + 模型/特效入场
    await inject();
    const st = await getState();
    console.log(`openTab(${idx}) loaded=`, st.filter(v => v.visible).map(v => v.name).join(', '));
  };

  await openTab(0);
  await saveStage('oldclient_pet_20_horse');

  await openTab(1);
  await saveStage('oldclient_pet_21_partner');

  // 参考件:同时导出 MountPetView 的 page_snapshot(嵌套树,查 skin/层级用,不喂 UiSnapshot)
  try {
    const snap = await page.evaluate(() => window.__sxExportPageSnapshots__(['MountPetView']));
    const v = (snap.views || [])[0];
    if (v) {
      fs.writeFileSync(path.join(OUT, 'page_snapshot_MountPetView_pet.json'),
        JSON.stringify({ version: snap.version, stage: snap.stage, views: [v] }));
      console.log('SAVED page_snapshot_MountPetView_pet.json nodes=' + v.nodeCount);
    }
  } catch (e) { console.log('page_snapshot export skip: ' + String(e).slice(0, 120)); }

  await browser.close();
  console.log('DONE');
})().catch(e => { console.error('ERR:', String(e)); process.exit(1); });
