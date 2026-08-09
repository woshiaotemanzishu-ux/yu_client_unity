/*
 * Read-only old-client evidence capture for Role -> SecretTreasure(Unreal).
 *
 * Usage:
 *   node Tools/Conversion/unreal_login_capture.cjs [account] [password] [output] [url]
 *
 * The script uses only real pointer navigation. It captures the bag, strengthen
 * and decompose views, but never clicks 14903/14905 or GoodsModel.RESOLVE_GOODS
 * transaction buttons.
 */
const fs = require('fs');
const os = require('os');
const path = require('path');
const { chromium } = require('e:/GitProject/yu_client_unity/output/node_modules/playwright');

const ACCOUNT = process.argv[2] || '111111';
const PASSWORD = process.argv[3] || '111111';
const OUTPUT = process.argv[4] || path.join(__dirname,
  '../../output/ui_route_audit/2026-08-09_role_unreal/old_readonly_v1');
const URL = process.argv[5] || 'http://127.0.0.1:8091/index.html';
const SHOTS = path.join(OUTPUT, 'shots');
const STATES = path.join(OUTPUT, 'states');
const SNAPSHOTS = path.join(OUTPUT, 'snapshots');
const WHITELIST = /^(MainUI|NameBoard|MessageItem|FirstRechargeBubble|FunctionOpenIcon|UIJoyStick|WaitforOpenViewLoading|FightingUpView|LoginBgView|LoginLoadingView|ActivityIcon|FuncBoardView)/;

function safeStringify(root) {
  const stack = new Set();
  const visit = (value) => {
    if (value === null || typeof value !== 'object') return value;
    if (stack.has(value)) return undefined;
    stack.add(value);
    let result;
    if (Array.isArray(value)) result = value.map(visit);
    else {
      result = {};
      for (const key of Object.keys(value)) {
        const child = visit(value[key]);
        if (child !== undefined) result[key] = child;
      }
    }
    stack.delete(value);
    return result;
  };
  return JSON.stringify(visit(root), null, 2);
}

(async () => {
  if (fs.existsSync(OUTPUT) && fs.readdirSync(OUTPUT).length) {
    throw new Error(`immutable evidence directory already exists and is non-empty: ${OUTPUT}`);
  }
  fs.mkdirSync(SHOTS, { recursive: true });
  fs.mkdirSync(STATES, { recursive: true });
  fs.mkdirSync(SNAPSHOTS, { recursive: true });

  const tempModule = path.join(os.tmpdir(), `unreal_snapshot_${process.pid}.mjs`);
  fs.copyFileSync('e:/GitProject/yu_client/tools/yu-resource-tool/frontend/src/utils/pageSnapshot.js', tempModule);
  const snapshotScript = (await import('file:///' + tempModule.split(path.sep).join('/'))).PAGE_SNAPSHOT_SCRIPT;

  const browser = await chromium.launch({ headless: true, channel: 'msedge' });
  const page = await browser.newPage({ viewport: { width: 720, height: 1280 } });
  const actions = [];
  const captures = [];

  const inject = async () => page.evaluate(snapshotScript + '; void 0');
  const listedViews = async () => page.evaluate(() => {
    try {
      return (window.__sxListLoadedPages__().views || []).map((view) => ({
        name: view.name,
        visible: view.visible !== false,
        nodes: view.nodeCount || 0,
      }));
    } catch (_) { return []; }
  });
  const visibleNames = async () => (await listedViews())
    .filter((view) => view.visible).map((view) => view.name);
  const shot = async (name) => {
    await page.screenshot({ path: path.join(SHOTS, name) });
    captures.push({ kind: 'screenshot', file: `shots/${name}` });
    console.log('SHOT', name);
  };
  const queryNodes = async (criteria) => page.evaluate((query) => {
    try {
      const names = (window.__sxListLoadedPages__().views || []).map((view) => view.name);
      const exported = window.__sxExportPageSnapshots__(names);
      const matches = [];
      for (const view of exported.views || []) {
        if (query.viewPattern && !(new RegExp(query.viewPattern, 'i')).test(view.meta.name)) continue;
        const walk = (node, ancestors) => {
          const ancestry = ancestors.map((item) => String(item.name || '')).join('/');
          const text = node.textProps && node.textProps.text ? String(node.textProps.text) : '';
          const ok = (query.includeHidden || node.effectiveVisible !== false)
            && (!query.name || node.name === query.name)
            && (!query.type || node.type === query.type)
            && (!query.ancestorPattern || (new RegExp(query.ancestorPattern, 'i')).test(ancestry))
            && (!query.textPattern || (new RegExp(query.textPattern, 'i')).test(text))
            && node.globalBounds;
          if (ok) {
            const b = node.globalBounds;
            matches.push({
              view: view.meta.name,
              name: node.name,
              type: node.type,
              path: node.path,
              text,
              x: b.x,
              y: b.y,
              width: b.width,
              height: b.height,
              cx: Math.round(b.x + b.width / 2),
              cy: Math.round(b.y + b.height / 2),
            });
          }
          (node.children || []).forEach((child) => walk(child, ancestors.concat(node)));
        };
        walk(view.nodeTree, []);
      }
      return matches;
    } catch (_) { return []; }
  }, criteria);
  const findClose = async (viewName) => page.evaluate((name) => {
    try {
      const view = (window.__sxExportPageSnapshots__([name]).views || [])[0];
      if (!view) return null;
      const candidates = [];
      const walk = (node) => {
        const n = String(node.name || '').toLowerCase();
        if (node.effectiveVisible !== false
          && /close|return|guanbi|_btn_x\b|btn_quit|_img_x\b|_img_return\b/.test(n)
          && node.globalBounds) {
          const b = node.globalBounds;
          candidates.push({
            name: node.name,
            cx: Math.round(b.x + b.width / 2),
            cy: Math.round(b.y + b.height / 2),
          });
        }
        (node.children || []).forEach(walk);
      };
      walk(view.nodeTree);
      return candidates.find((item) => item.cx >= 0 && item.cx <= 720
        && item.cy >= 0 && item.cy <= 1280) || null;
    } catch (_) { return null; }
  }, viewName);
  const clickNode = async (criteria, label) => {
    await inject();
    const nodes = await queryNodes(criteria);
    if (!nodes.length) throw new Error(`${label}: node not found ${JSON.stringify(criteria)}`);
    const node = nodes[0];
    await page.mouse.click(node.cx, node.cy);
    actions.push({ label, view: node.view, control: node.name, path: node.path, clicked: true });
    return node;
  };
  const waitVisible = async (name, attempts = 30) => {
    for (let i = 0; i < attempts; i++) {
      await inject();
      if ((await visibleNames()).includes(name)) return true;
      await page.waitForTimeout(400);
    }
    return false;
  };
  const captureState = async (label, wantedViews = []) => {
    await inject();
    const names = (await listedViews()).map((view) => view.name);
    const exported = await page.evaluate((viewNames) => window.__sxExportPageSnapshots__(viewNames), names);
    const stateFile = path.join(STATES, `${label}.json`);
    fs.writeFileSync(stateFile, safeStringify(exported), 'utf8');
    captures.push({ kind: 'state', label, file: `states/${label}.json`, views: names });

    const selected = (exported.views || []).filter((view) => wantedViews.includes(view.meta.name));
    for (const view of selected) {
      const one = { version: exported.version, stage: exported.stage, views: [view] };
      const fileName = `page_snapshot_${view.meta.name}_${label}.json`;
      fs.writeFileSync(path.join(SNAPSHOTS, fileName), safeStringify(one), 'utf8');
      captures.push({ kind: 'page-snapshot', label, view: view.meta.name,
        file: `snapshots/${fileName}`, nodes: view.nodeCount || 0 });
    }
    return exported;
  };
  const typeInto = async (x, y, value) => {
    await page.mouse.click(x, y);
    await page.keyboard.press('Control+a');
    await page.keyboard.press('Backspace');
    await page.keyboard.type(value, { delay: 20 });
  };

  await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForFunction(() => !!(window.Laya && window.Laya.stage), { timeout: 30000 });
  await page.waitForTimeout(9000);
  await inject();
  await typeInto(408, 525, ACCOUNT);
  await typeInto(408, 590, PASSWORD);
  await page.mouse.click(490, 718);
  actions.push({ label: 'login', account: ACCOUNT, clicked: true });
  console.log('LOGIN fired account=' + ACCOUNT);
  await page.waitForTimeout(8000);
  await inject();

  let stable = 0;
  for (let attempt = 0; attempt < 45 && stable < 3; attempt++) {
    const current = await listedViews();
    const visible = (name) => current.some((view) => view.name === name && view.visible);
    const inCity = visible('MainUITopView');
    const blockers = current.filter((view) => view.visible && !WHITELIST.test(view.name));
    console.log(`[${attempt}] city=${inCity} blockers=${blockers.map((view) => view.name).join(',') || '-'}`);
    if (visible('LoginAlertView')) {
      stable = 0;
      await page.mouse.click(460, 840);
    } else if (visible('LoginEnterView')) {
      stable = 0;
      await page.mouse.click(360, 930);
      await page.waitForTimeout(3000);
    } else if (visible('LoginSelectRoleView') || visible('LoginCreateRoleView')) {
      stable = 0;
      await page.mouse.click(360, 1120);
      await page.waitForTimeout(5000);
    } else if (visible('DialogueView')) {
      stable = 0;
      await page.mouse.click(45, 565);
    } else if (inCity && blockers.length) {
      stable = 0;
      const blocker = blockers[blockers.length - 1];
      const close = await findClose(blocker.name);
      if (!close) throw new Error(`startup blocker has no safe close: ${blocker.name}`);
      console.log(`close ${blocker.name} via ${close.name} @(${close.cx},${close.cy})`);
      await page.mouse.click(close.cx, close.cy);
    } else if (inCity) {
      stable++;
    }
    await page.waitForTimeout(3500);
    await inject();
  }
  if (stable < 3) throw new Error('old client did not reach a stable city state');

  await shot('10_city_ready.png');
  await page.mouse.click(120, 1218);
  actions.push({ label: 'open role', control: 'MainUIDown.role', clicked: true });
  await page.waitForTimeout(3500);
  await inject();
  await shot('20_role_person.png');
  await captureState('role_person', ['EquipmentView']);

  const allUnrealEntries = await queryNodes({
    ancestorPattern: 'EquipmentView', name: '_Group6', includeHidden: true,
  });
  let unrealEntries = await queryNodes({ ancestorPattern: 'EquipmentView', name: '_Group6' });
  if (!unrealEntries.length && allUnrealEntries.length) {
    const levelNodes = await queryNodes({ name: 'levelLb' });
    const blockedReport = {
      schema: 1,
      route: 'role.equipment.unreal-entry',
      account: ACCOUNT,
      url: URL,
      viewport: { width: 720, height: 1280 },
      captured_at: new Date().toISOString(),
      read_only: true,
      outcome: 'blocked-precondition',
      blocker: {
        code: 'old-client-unreal-entry-hidden',
        control: allUnrealEntries[0],
        observed_levels: levelNodes.map((node) => node.text).filter(Boolean),
        required_level: 360,
        evidence: [
          'ConfigFuncOpenCondition.UnrealBagView.open_lv=360',
          'current EquipmentView.json serializes _Group6 visible=false',
        ],
      },
      actions,
      captures,
      skipped_transactions: [14901, 14902, 14903, 14905, 'GoodsModel.RESOLVE_GOODS'],
    };
    fs.writeFileSync(path.join(OUTPUT, 'blocked.json'), safeStringify(blockedReport), 'utf8');
    console.log('BLOCKED old-client Unreal entry is hidden; no transaction was attempted');
    await browser.close();
    return;
  }
  if (!unrealEntries.length) {
    const menu = (await queryNodes({ ancestorPattern: 'EquipmentView', name: 'secondary_menu' }))[0];
    if (!menu) throw new Error('EquipmentView secondary_menu not found');
    const y = Math.round(menu.y + menu.height / 2);
    for (let i = 0; i < 3 && !unrealEntries.length; i++) {
      const fromX = Math.round(menu.x + menu.width - 25);
      const toX = Math.round(menu.x + 25);
      await page.mouse.move(fromX, y);
      await page.mouse.down();
      await page.mouse.move(toX, y, { steps: 16 });
      await page.mouse.up();
      await page.waitForTimeout(500);
      await inject();
      unrealEntries = await queryNodes({ ancestorPattern: 'EquipmentView', name: '_Group6' });
    }
    actions.push({ label: 'reveal role secondary unreal entry', control: 'secondary_menu',
      clicked: false, drag: true, found: unrealEntries.length > 0 });
    await shot('21_role_secondary_scrolled.png');
    await captureState('role_secondary_scrolled', ['EquipmentView']);
  }

  await clickNode({ ancestorPattern: 'EquipmentView', name: '_Group6' },
    'role person -> unreal');
  await page.waitForTimeout(350);
  await shot('30_unreal_bag_350ms.png');
  await page.waitForTimeout(650);
  await shot('31_unreal_bag_1000ms.png');
  if (!await waitVisible('UnrealBagView')) throw new Error('UnrealBagView did not become visible');
  await page.waitForTimeout(500);
  await shot('32_unreal_bag_ready.png');
  await captureState('unreal_bag_ready', ['SecretTreasureMainView', 'UnrealBagView']);

  const bagScroller = (await queryNodes({ viewPattern: '^UnrealBagView$', name: '_scroll_bag_con' }))[0];
  if (bagScroller) {
    await page.mouse.move(bagScroller.cx, bagScroller.cy);
    await page.mouse.wheel(0, 900);
    await page.waitForTimeout(700);
    actions.push({ label: 'scroll unreal bag', control: '_scroll_bag_con', write: false });
    await shot('33_unreal_bag_scrolled.png');
    await captureState('unreal_bag_scrolled', ['UnrealBagView']);
  }

  await clickNode({ viewPattern: '^UnrealBagView$', name: '_gp_stren' },
    'open unreal strengthen');
  await page.waitForTimeout(350);
  await shot('40_unreal_strengthen_350ms.png');
  await page.waitForTimeout(650);
  await shot('41_unreal_strengthen_1000ms.png');
  if (!await waitVisible('UnrealStrengthenView'))
    throw new Error('UnrealStrengthenView did not become visible');
  await page.waitForTimeout(500);
  await shot('42_unreal_strengthen_ready.png');
  await captureState('unreal_strengthen_ready', ['UnrealEnterView', 'UnrealStrengthenView']);
  actions.push({ label: '14903 stage', control: '_gp_level_up', clicked: false,
    reason: 'destructive write skipped' });
  actions.push({ label: '14905 strengthen', control: '_gp_level_up', clicked: false,
    reason: 'destructive write skipped' });

  const strengthenClose = await findClose('UnrealEnterView');
  if (!strengthenClose) throw new Error('UnrealEnterView close/return control not found');
  await page.mouse.click(strengthenClose.cx, strengthenClose.cy);
  actions.push({ label: 'return from strengthen', view: 'UnrealEnterView',
    control: strengthenClose.name, clicked: true });
  await page.waitForTimeout(1500);
  if (!await waitVisible('UnrealBagView', 20))
    throw new Error('UnrealBagView did not restore after strengthen return');

  await clickNode({ viewPattern: '^UnrealBagView$', name: '_gp_decompose' },
    'open unreal decompose');
  if (!await waitVisible('UnrealDecomposeView'))
    throw new Error('UnrealDecomposeView did not become visible');
  await page.waitForTimeout(500);
  await shot('50_unreal_decompose_ready.png');
  await captureState('unreal_decompose_ready', ['UnrealDecomposeView']);
  actions.push({ label: 'resolve unreal goods', control: '_gp_decompose', clicked: false,
    reason: 'destructive write skipped' });

  const decomposeClose = await findClose('UnrealDecomposeView');
  if (!decomposeClose) throw new Error('UnrealDecomposeView close control not found');
  await page.mouse.click(decomposeClose.cx, decomposeClose.cy);
  actions.push({ label: 'close decompose', view: 'UnrealDecomposeView',
    control: decomposeClose.name, clicked: true });
  await page.waitForTimeout(700);
  await captureState('unreal_bag_after_modal_close', ['SecretTreasureMainView', 'UnrealBagView']);

  const finalViews = await listedViews();
  const report = {
    schema: 1,
    route: 'mainui.role.person.unreal-via-secret-treasure',
    account: ACCOUNT,
    url: URL,
    viewport: { width: 720, height: 1280 },
    captured_at: new Date().toISOString(),
    read_only: true,
    actions,
    captures,
    final_views: finalViews,
    skipped_transactions: [14901, 14902, 14903, 14905, 'GoodsModel.RESOLVE_GOODS'],
  };
  fs.writeFileSync(path.join(OUTPUT, 'capture-manifest.json'),
    JSON.stringify(report, null, 2), 'utf8');
  console.log('DONE captures=' + captures.length + ' output=' + OUTPUT);
  await browser.close();
})().catch((error) => {
  console.error('ERR:', error && error.stack ? error.stack : String(error));
  process.exit(1);
});
