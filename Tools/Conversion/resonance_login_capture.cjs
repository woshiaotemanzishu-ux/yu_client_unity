/*
 * Read-only old-client evidence capture for Role -> Resonance.
 * Usage:
 *   node resonance_login_capture.cjs [account] [password] [output] [url]
 *
 * The script only performs real pointer navigation and exports snapshots.
 * It never clicks build/return confirmation buttons.
 */
const fs = require('fs');
const os = require('os');
const path = require('path');
const { chromium } = require('e:/GitProject/yu_client_unity/output/node_modules/playwright');

const ACCOUNT = process.argv[2] || '111111';
const PASSWORD = process.argv[3] || '111111';
const OUTPUT = process.argv[4] || path.join(__dirname, '../../output/ui_route_audit/2026-08-07_resonance/old');
const URL = process.argv[5] || 'http://127.0.0.1:8091/index.html';
const SHOTS = path.join(OUTPUT, 'shots');
const WHITELIST = /^(MainUI|NameBoard|MessageItem|FirstRechargeBubble|FunctionOpenIcon|UIJoyStick|WaitforOpenViewLoading|FightingUpView|LoginBgView|ActivityIcon|FuncBoardView)/;

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
  fs.mkdirSync(SHOTS, { recursive: true });
  const tempModule = path.join(os.tmpdir(), `resonance_snapshot_${process.pid}.mjs`);
  fs.copyFileSync('e:/GitProject/yu_client/tools/yu-resource-tool/frontend/src/utils/pageSnapshot.js', tempModule);
  const snapshotScript = (await import('file:///' + tempModule.split(path.sep).join('/'))).PAGE_SNAPSHOT_SCRIPT;

  const browser = await chromium.launch({ headless: true, channel: 'msedge' });
  const page = await browser.newPage({ viewport: { width: 720, height: 1280 } });
  const inject = async () => page.evaluate(snapshotScript + '; void 0');
  const shot = async (name) => {
    await page.screenshot({ path: path.join(SHOTS, name) });
    console.log('SHOT', name);
  };
  const state = async () => page.evaluate(() => {
    try {
      const listed = window.__sxListLoadedPages__();
      const names = (listed.views || []).map((view) => view.name);
      const exported = window.__sxExportPageSnapshots__(names);
      return (exported.views || []).map((view) => ({
        name: view.meta.name,
        visible: view.meta.visible !== false,
        nodes: view.nodeCount,
      }));
    } catch (_) {
      return [];
    }
  });
  const findClose = async (viewName) => page.evaluate((name) => {
    try {
      const exported = window.__sxExportPageSnapshots__([name]);
      const view = (exported.views || [])[0];
      if (!view) return null;
      let best = null;
      const walk = (node) => {
        const nodeName = String(node.name || '').toLowerCase();
        if (node.effectiveVisible !== false && /close|guanbi|_btn_x\b|btn_quit|_img_x\b/.test(nodeName)) {
          const bounds = node.globalBounds;
          if (bounds) {
            best = {
              name: node.name,
              cx: Math.round(bounds.x + bounds.width / 2),
              cy: Math.round(bounds.y + bounds.height / 2),
            };
          }
        }
        (node.children || []).forEach(walk);
      };
      walk(view.nodeTree);
      return best;
    } catch (_) {
      return null;
    }
  }, viewName);
  const findNodes = async (nodeName) => page.evaluate((target) => {
    try {
      const listed = window.__sxListLoadedPages__();
      const names = (listed.views || []).map((view) => view.name);
      const exported = window.__sxExportPageSnapshots__(names);
      const matches = [];
      for (const view of exported.views || []) {
        const walk = (node) => {
          if (node.effectiveVisible !== false && node.name === target && node.globalBounds) {
            const bounds = node.globalBounds;
            matches.push({
              view: view.meta.name,
              name: node.name,
              path: node.path,
              x: bounds.x,
              y: bounds.y,
              width: bounds.width,
              height: bounds.height,
              cx: Math.round(bounds.x + bounds.width / 2),
              cy: Math.round(bounds.y + bounds.height / 2),
            });
          }
          (node.children || []).forEach(walk);
        };
        walk(view.nodeTree);
      }
      return matches;
    } catch (_) {
      return [];
    }
  }, nodeName);
  const queryNodes = async (criteria) => page.evaluate((query) => {
    try {
      const listed = window.__sxListLoadedPages__();
      const names = (listed.views || []).map((view) => view.name);
      const exported = window.__sxExportPageSnapshots__(names);
      const matches = [];
      for (const view of exported.views || []) {
        if (query.viewPattern && !(new RegExp(query.viewPattern, 'i')).test(view.meta.name)) continue;
        const walk = (node) => {
          const text = node.textProps && node.textProps.text ? String(node.textProps.text) : '';
          const nameMatches = !query.name || node.name === query.name;
          const typeMatches = !query.type || node.type === query.type;
          const textMatches = !query.textPattern || (new RegExp(query.textPattern, 'i')).test(text);
          if (node.effectiveVisible !== false && nameMatches && typeMatches && textMatches && node.globalBounds) {
            const bounds = node.globalBounds;
            matches.push({
              view: view.meta.name,
              name: node.name,
              type: node.type,
              path: node.path,
              text,
              mouseEnabled: !!node.mouseEnabled,
              x: bounds.x,
              y: bounds.y,
              width: bounds.width,
              height: bounds.height,
              cx: Math.round(bounds.x + bounds.width / 2),
              cy: Math.round(bounds.y + bounds.height / 2),
            });
          }
          (node.children || []).forEach(walk);
        };
        walk(view.nodeTree);
      }
      return matches;
    } catch (_) {
      return [];
    }
  }, criteria);
  const visibleViewNames = async () => (await state()).filter((view) => view.visible).map((view) => view.name);
  const closeOverlay = async (beforeNames, preferredNode) => {
    await inject();
    const afterNames = await visibleViewNames();
    const added = afterNames.filter((name) => !beforeNames.includes(name) && name !== 'EquipSuitBaseView');
    for (const name of added.reverse()) {
      if (preferredNode) {
        const preferred = await queryNodes({ viewPattern: `^${name}$`, name: preferredNode });
        if (preferred.length) {
          await page.mouse.click(preferred[0].cx, preferred[0].cy);
          await page.waitForTimeout(500);
          return name;
        }
      }
      const close = await findClose(name);
      if (close) {
        await page.mouse.click(close.cx, close.cy);
        await page.waitForTimeout(500);
        return name;
      }
    }
    return null;
  };
  const stateSnapshots = [];
  const captureState = async (label, extra = {}) => {
    await inject();
    const listed = await page.evaluate(() => window.__sxListLoadedPages__());
    const names = (listed.views || []).map((view) => view.name);
    const exported = await page.evaluate((viewNames) => window.__sxExportPageSnapshots__(viewNames), names);
    const file = path.join(OUTPUT, 'states', `${label}.json`);
    fs.mkdirSync(path.dirname(file), { recursive: true });
    fs.writeFileSync(file, safeStringify(exported), 'utf8');
    stateSnapshots.push({ label, file: path.relative(OUTPUT, file).split(path.sep).join('/'), ...extra });
    return exported;
  };
  const typeInto = async (x, y, value) => {
    await page.mouse.click(x, y);
    await page.keyboard.press('Control+a');
    await page.keyboard.press('Backspace');
    await page.keyboard.type(value, { delay: 25 });
  };

  await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForFunction(() => !!(window.Laya && window.Laya.stage), { timeout: 30000 });
  await page.waitForTimeout(9000);
  await inject();
  await shot('00_login.png');
  await typeInto(408, 525, ACCOUNT);
  await typeInto(408, 590, PASSWORD);
  await page.mouse.click(490, 718);
  console.log('LOGIN fired account=' + ACCOUNT);
  await page.waitForTimeout(8000);
  await inject();

  let stable = 0;
  for (let attempt = 0; attempt < 40 && stable < 3; attempt++) {
    const current = await state();
    const visible = (name) => current.some((view) => view.name === name && view.visible);
    const inCity = visible('MainUITopView');
    const blockers = current.filter((view) => view.visible && !WHITELIST.test(view.name));
    if (attempt % 3 === 0 || blockers.length) {
      console.log(`[${attempt}] city=${inCity} blockers=${blockers.map((view) => view.name).join(',') || '-'}`);
    }
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
      const blocker = blockers[0];
      const close = await findClose(blocker.name);
      if (close && close.cx >= 0 && close.cx <= 720 && close.cy >= 0 && close.cy <= 1280) {
        console.log(`close ${blocker.name} via ${close.name} @(${close.cx},${close.cy})`);
        await page.mouse.click(close.cx, close.cy);
      } else {
        await shot(`startup_${attempt}_${blocker.name}.png`);
        await page.mouse.click(360, 640);
      }
    } else if (inCity) {
      stable++;
    }
    await page.waitForTimeout(4000);
    await inject();
  }
  if (stable < 3) throw new Error('old client did not reach a stable city state');

  await shot('10_city_ready.png');
  await page.mouse.click(120, 1218);
  await page.waitForTimeout(5000);
  await inject();
  await shot('20_role_person.png');

  const entries = await findNodes('_Group5');
  console.log('RESONANCE_ENTRIES', JSON.stringify(entries));
  if (!entries.length) throw new Error('EquipmentView._Group5 resonance entry was not found');
  await page.mouse.click(entries[0].cx, entries[0].cy);
  await page.waitForTimeout(350);
  await shot('30_resonance_350ms.png');
  await page.waitForTimeout(650);
  await shot('31_resonance_1000ms.png');

  let ready = false;
  for (let attempt = 0; attempt < 20; attempt++) {
    await inject();
    const current = await state();
    ready = current.some((view) => /EquipSuit(BaseView|MianView)/i.test(view.name) && view.visible && view.nodes > 20);
    if (ready) break;
    await page.waitForTimeout(500);
  }
  if (!ready) throw new Error('EquipSuitBaseView/EquipSuitMianView did not reach snapshot-ready state');
  await page.waitForTimeout(500);
  await inject();
  await shot('32_resonance_ready.png');

  const tabLabels = ['demon-soul', 'war-soul', 'all-things', 'ornament'];
  const routeSummary = { account: ACCOUNT, url: URL, tabs: [], destructiveClicks: [] };
  let tabs = await queryNodes({ viewPattern: '^EquipSuitBaseView$', type: 'WindowComponentTabButtonOne' });
  tabs = tabs.sort((a, b) => a.x - b.x);
  if (tabs.length !== 4) throw new Error(`expected four resonance tabs, got ${tabs.length}`);

  for (let tabIndex = 0; tabIndex < tabs.length; tabIndex++) {
    await page.mouse.click(tabs[tabIndex].cx, tabs[tabIndex].cy);
    await page.waitForTimeout(800);
    await inject();
    const tabLabel = tabLabels[tabIndex];
    const tabResult = { index: tabIndex, label: tabLabel, positions: [], attributeStages: [] };
    routeSummary.tabs.push(tabResult);
    await shot(`40_tab_${tabIndex + 1}_${tabLabel}.png`);
    await captureState(`tab_${tabIndex + 1}_${tabLabel}`);

    let positions = await queryNodes({ viewPattern: '^EquipSuitBaseView$', type: 'EquipSuitPosItem' });
    positions = positions.sort((a, b) => (a.y - b.y) || (a.x - b.x));
    for (let positionIndex = 0; positionIndex < positions.length; positionIndex++) {
      await page.mouse.click(positions[positionIndex].cx, positions[positionIndex].cy);
      await page.waitForTimeout(350);
      await inject();
      const names = await queryNodes({ viewPattern: '^EquipSuitBaseView$', name: 'nameSLab' });
      const currentName = names.length ? names[0].text : '';
      tabResult.positions.push({ index: positionIndex, currentName, bounds: positions[positionIndex] });
      await captureState(`tab_${tabIndex + 1}_${tabLabel}_position_${positionIndex + 1}`, { currentName });
    }
    await shot(`41_tab_${tabIndex + 1}_${tabLabel}_last_position.png`);

    const rightButtons = await queryNodes({ viewPattern: '^EquipSuitBaseView$', name: 'rImg' });
    const leftButtons = await queryNodes({ viewPattern: '^EquipSuitBaseView$', name: 'lImg' });
    if (rightButtons.length && leftButtons.length) {
      let previous = '';
      for (let step = 0; step < 40; step++) {
        const stageLabels = await queryNodes({ viewPattern: '^EquipSuitBaseView$', name: 'nameLab' });
        const stage = stageLabels
          .filter((node) => node.y > 750 && /【.*】/.test(node.text))
          .sort((a, b) => a.y - b.y)[0];
        const stageText = stage ? stage.text : '';
        if (!tabResult.attributeStages.includes(stageText)) tabResult.attributeStages.push(stageText);
        if (step > 0 && stageText === previous) break;
        previous = stageText;
        await page.mouse.click(rightButtons[0].cx, rightButtons[0].cy);
        await page.waitForTimeout(220);
        await inject();
      }
      await shot(`42_tab_${tabIndex + 1}_${tabLabel}_attribute_max.png`);
      for (let step = 0; step < 40; step++) {
        const before = await queryNodes({ viewPattern: '^EquipSuitBaseView$', name: 'nameLab' });
        const beforeText = (before.filter((node) => node.y > 750 && /【.*】/.test(node.text)).sort((a, b) => a.y - b.y)[0] || {}).text || '';
        await page.mouse.click(leftButtons[0].cx, leftButtons[0].cy);
        await page.waitForTimeout(120);
        await inject();
        const after = await queryNodes({ viewPattern: '^EquipSuitBaseView$', name: 'nameLab' });
        const afterText = (after.filter((node) => node.y > 750 && /【.*】/.test(node.text)).sort((a, b) => a.y - b.y)[0] || {}).text || '';
        if (afterText === beforeText) break;
      }
    }

    const preview = await queryNodes({ viewPattern: '^EquipSuitBaseView$', name: 'previewBox' });
    if (preview.length) {
      const before = await visibleViewNames();
      await page.mouse.click(preview[0].cx, preview[0].cy);
      await page.waitForTimeout(1200);
      await inject();
      const opened = await visibleViewNames();
      tabResult.previewView = opened.find((name) => !before.includes(name)) || '';
      await shot(`43_tab_${tabIndex + 1}_${tabLabel}_effect_preview.png`);
      await captureState(`tab_${tabIndex + 1}_${tabLabel}_effect_preview`);
      await closeOverlay(before);
    }

    const returnButtons = await queryNodes({ viewPattern: '^EquipSuitBaseView$', name: '_btn_back' });
    tabResult.returnAvailable = returnButtons.length > 0;
    if (returnButtons.length) {
      const before = await visibleViewNames();
      await page.mouse.click(returnButtons[0].cx, returnButtons[0].cy);
      await page.waitForTimeout(1200);
      await inject();
      const opened = await visibleViewNames();
      tabResult.returnView = opened.find((name) => !before.includes(name)) || '';
      await shot(`44_tab_${tabIndex + 1}_${tabLabel}_return_preview.png`);
      await captureState(`tab_${tabIndex + 1}_${tabLabel}_return_preview`);
      await closeOverlay(before, '_gp_cancel');
    }

    const buildButtons = await queryNodes({ viewPattern: '^EquipSuitBaseView$', name: 'upBtn' });
    if (buildButtons.length) {
      routeSummary.destructiveClicks.push({ tab: tabLabel, control: 'upBtn', action: '15221 build', clicked: false });
    }
  }

  tabs = (await queryNodes({ viewPattern: '^EquipSuitBaseView$', type: 'WindowComponentTabButtonOne' })).sort((a, b) => a.x - b.x);
  await page.mouse.click(tabs[0].cx, tabs[0].cy);
  await page.waitForTimeout(500);
  const infoButtons = await queryNodes({ viewPattern: '^EquipSuitBaseView$', name: 'infoBox' });
  if (infoButtons.length) {
    const before = await visibleViewNames();
    await page.mouse.click(infoButtons[0].cx, infoButtons[0].cy);
    await page.waitForTimeout(900);
    await inject();
    const opened = await visibleViewNames();
    routeSummary.instructionView = opened.find((name) => !before.includes(name)) || '';
    await shot('50_instruction.png');
    await captureState('instruction');
    await closeOverlay(before);
  }

  const returns = await queryNodes({ viewPattern: '^EquipSuitBaseView$', name: '_img_return' });
  if (!returns.length) throw new Error('resonance page return button not found');
  await page.mouse.click(returns[0].cx, returns[0].cy);
  await page.waitForTimeout(900);
  await inject();
  await shot('60_resonance_closed.png');
  const warmEntry = await findNodes('_Group5');
  if (!warmEntry.length) throw new Error('resonance entry missing after close');
  const warmStart = Date.now();
  await page.mouse.click(warmEntry[0].cx, warmEntry[0].cy);
  let warmReady = false;
  for (let attempt = 0; attempt < 40; attempt++) {
    await page.waitForTimeout(50);
    await inject();
    const current = await state();
    warmReady = current.some((view) => /EquipSuitBaseView/i.test(view.name) && view.visible && view.nodes > 20);
    if (warmReady) break;
  }
  routeSummary.warmOpenMs = Date.now() - warmStart;
  if (!warmReady) throw new Error('warm reopen did not become ready');
  await shot('61_resonance_warm_reopen.png');
  await captureState('warm_reopen');
  fs.writeFileSync(path.join(OUTPUT, 'route_summary.json'), safeStringify(routeSummary), 'utf8');
  fs.writeFileSync(path.join(OUTPUT, 'state_index.json'), safeStringify(stateSnapshots), 'utf8');

  const listed = await page.evaluate(() => window.__sxListLoadedPages__());
  const names = (listed.views || []).map((view) => view.name);
  const exported = await page.evaluate((viewNames) => window.__sxExportPageSnapshots__(viewNames), names);
  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  fs.writeFileSync(path.join(OUTPUT, `all_loaded_${stamp}.json`), safeStringify(exported), 'utf8');

  const inventory = [];
  for (const view of exported.views || []) {
    const walk = (node) => {
      const text = node.textProps && node.textProps.text ? String(node.textProps.text) : '';
      if (node.effectiveVisible !== false && (node.mouseEnabled || text || /tab|btn|list|box|img|html|lab/i.test(node.name || ''))) {
        inventory.push({
          view: view.meta.name,
          name: node.name,
          path: node.path,
          type: node.type,
          text,
          mouseEnabled: !!node.mouseEnabled,
          bounds: node.globalBounds || null,
          skin: node.skin || '',
        });
      }
      (node.children || []).forEach(walk);
    };
    walk(view.nodeTree);
  }
  fs.writeFileSync(path.join(OUTPUT, `visible_inventory_${stamp}.json`), safeStringify(inventory), 'utf8');
  console.log('FINAL_LOADED', names.join(','));
  console.log('INVENTORY', inventory.length);
  await browser.close();
  try { fs.unlinkSync(tempModule); } catch (_) {}
  console.log('DONE');
})().catch((error) => {
  console.error('ERR', error && error.stack ? error.stack : String(error));
  process.exit(1);
});
