/*
 * Read-only old-client evidence capture for Role -> Achievement.
 * It navigates every top/sub category and scrolls the read-only lists.
 * It never clicks active_btn or receiveBtn (40902/40905 write transactions).
 */
const fs = require('fs');
const os = require('os');
const path = require('path');
const { chromium } = require('e:/GitProject/yu_client_unity/output/node_modules/playwright');

const ACCOUNT = process.argv[2] || '111111';
const PASSWORD = process.argv[3] || '111111';
const OUTPUT = process.argv[4] || path.join(__dirname,
  '../../output/ui_route_audit/2026-08-08_role_achievement/old_full_achievement_v1');
const URL = process.argv[5] || 'http://127.0.0.1:8091/index.html';
const SHOTS = path.join(OUTPUT, 'shots');
const STATES = path.join(OUTPUT, 'states');
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
  if (fs.existsSync(OUTPUT) && fs.readdirSync(OUTPUT).length) {
    throw new Error(`immutable evidence directory already exists and is non-empty: ${OUTPUT}`);
  }
  fs.mkdirSync(SHOTS, { recursive: true });
  fs.mkdirSync(STATES, { recursive: true });
  const tempModule = path.join(os.tmpdir(), `achievement_snapshot_${process.pid}.mjs`);
  fs.copyFileSync('e:/GitProject/yu_client/tools/yu-resource-tool/frontend/src/utils/pageSnapshot.js', tempModule);
  const snapshotScript = (await import('file:///' + tempModule.split(path.sep).join('/'))).PAGE_SNAPSHOT_SCRIPT;

  const browser = await chromium.launch({ headless: true, channel: 'msedge' });
  const page = await browser.newPage({ viewport: { width: 720, height: 1280 } });
  const inject = async () => page.evaluate(snapshotScript + '; void 0');
  const shot = async (name) => {
    await page.screenshot({ path: path.join(SHOTS, name) });
    console.log('SHOT', name);
  };
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
  const queryNodes = async (criteria) => page.evaluate((query) => {
    try {
      const names = (window.__sxListLoadedPages__().views || []).map((view) => view.name);
      const exported = window.__sxExportPageSnapshots__(names);
      const matches = [];
      for (const view of exported.views || []) {
        if (query.viewPattern && !(new RegExp(query.viewPattern, 'i')).test(view.meta.name)) continue;
        const walk = (node, ancestors) => {
          const text = node.textProps && node.textProps.text ? String(node.textProps.text) : '';
          const skin = String(node.skin || '');
          const ancestry = ancestors.map((item) => String(item.name || '')).join('/');
          const ok = node.effectiveVisible !== false
            && (!query.name || node.name === query.name)
            && (!query.type || node.type === query.type)
            && (!query.path || JSON.stringify(node.path) === JSON.stringify(query.path))
            && (!query.ancestorPattern || (new RegExp(query.ancestorPattern, 'i')).test(ancestry))
            && (!query.textPattern || (new RegExp(query.textPattern, 'i')).test(text))
            && (!query.skinPattern || (new RegExp(query.skinPattern, 'i')).test(skin))
            && node.globalBounds;
          if (ok) {
            const b = node.globalBounds;
            matches.push({
              view: view.meta.name, name: node.name, type: node.type, path: node.path,
              text, skin, mouseEnabled: !!node.mouseEnabled,
              x: b.x, y: b.y, width: b.width, height: b.height,
              cx: Math.round(b.x + b.width / 2), cy: Math.round(b.y + b.height / 2),
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
      const found = [];
      const walk = (node) => {
        const n = String(node.name || '').toLowerCase();
        if (node.effectiveVisible !== false
          && /close|guanbi|_btn_x\b|btn_quit|_img_x\b|_btn_close\b/.test(n)
          && node.globalBounds) {
          const b = node.globalBounds;
          found.push({ name: node.name, cx: Math.round(b.x + b.width / 2), cy: Math.round(b.y + b.height / 2) });
        }
        (node.children || []).forEach(walk);
      };
      walk(view.nodeTree);
      return found.find((item) => item.cx >= 0 && item.cx <= 720 && item.cy >= 0 && item.cy <= 1280) || null;
    } catch (_) { return null; }
  }, viewName);
  const captureState = async (label) => {
    await inject();
    const names = (await listedViews()).map((view) => view.name);
    const exported = await page.evaluate((viewNames) => window.__sxExportPageSnapshots__(viewNames), names);
    const file = path.join(STATES, `${label}.json`);
    fs.writeFileSync(file, safeStringify(exported), 'utf8');
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
      stable = 0; await page.mouse.click(460, 840);
    } else if (visible('LoginEnterView')) {
      stable = 0; await page.mouse.click(360, 930); await page.waitForTimeout(3000);
    } else if (visible('LoginSelectRoleView') || visible('LoginCreateRoleView')) {
      stable = 0; await page.mouse.click(360, 1120); await page.waitForTimeout(5000);
    } else if (visible('DialogueView')) {
      stable = 0; await page.mouse.click(45, 565);
    } else if (inCity && blockers.length) {
      stable = 0;
      const blocker = blockers[blockers.length - 1];
      const close = await findClose(blocker.name);
      if (!close) throw new Error(`startup blocker has no safe close: ${blocker.name}`);
      console.log(`close ${blocker.name} via ${close.name} @(${close.cx},${close.cy})`);
      await page.mouse.click(close.cx, close.cy);
    } else if (inCity) stable++;
    await page.waitForTimeout(3500);
    await inject();
  }
  if (stable < 3) throw new Error('old client did not reach a stable city state');

  await shot('10_city_ready.png');
  await page.mouse.click(120, 1218);
  await page.waitForTimeout(3500);
  await inject();
  await shot('20_role_person.png');
  const achievementEntries = await queryNodes({
    ancestorPattern: 'EquipmentView', name: '_Group2', skinPattern: 'role_achv_btn'
  });
  const entry = achievementEntries[0] || (await queryNodes({ ancestorPattern: 'EquipmentView', name: '_Group2' }))[0];
  if (!entry) throw new Error('EquipmentView achievement entry _Group2 not found');
  await page.mouse.click(entry.cx, entry.cy);
  await page.waitForTimeout(350);
  await shot('30_achievement_350ms.png');
  await page.waitForTimeout(650);
  await shot('31_achievement_1000ms.png');

  let ready = false;
  for (let attempt = 0; attempt < 30; attempt++) {
    await inject();
    ready = (await queryNodes({ ancestorPattern: 'AchvMainView', name: 'tabScroller' })).length > 0;
    if (ready) break;
    await page.waitForTimeout(400);
  }
  if (!ready) throw new Error('achievement page did not reach snapshot-ready state');
  await page.waitForTimeout(500);
  await shot('32_achievement_ready.png');
  await captureState('overview_ready');

  const destructiveClicks = [];
  const recordWriteButtons = async (state) => {
    const writeButtons = [
      ...(await queryNodes({ ancestorPattern: 'AchvMainView', name: 'active_btn' })),
      ...(await queryNodes({ ancestorPattern: '(AchvTotalItem|achvSubItem)', name: 'receiveBtn' })),
    ];
    for (const button of writeButtons) {
      destructiveClicks.push({ state, view: button.view, path: button.path,
        control: button.name, protocol: button.name === 'active_btn' ? 40902 : 40905,
        clicked: false });
    }
  };
  await recordWriteButtons('overview_ready');

  let topLabels = await queryNodes({ ancestorPattern: 'AchvTabBtn', name: 'tab_txt' });
  topLabels = topLabels.sort((a, b) => a.x - b.x);
  if (topLabels.length !== 7) throw new Error(`expected seven top achievement tabs, got ${topLabels.length}`);
  const topTexts = topLabels.map((node) => node.text);
  const route = { account: ACCOUNT, url: URL, topTabs: [], destructiveClicks };

  const findTopLabel = async (labelText) => (await queryNodes({
    ancestorPattern: 'AchvTabBtn', name: 'tab_txt'
  })).find((node) => node.text === labelText);

  const bringIntoViewport = async (labelText) => {
    for (let attempt = 0; attempt < 8; attempt++) {
      await inject();
      const label = await findTopLabel(labelText);
      if (!label) throw new Error(`top label disappeared: ${labelText}`);
      const tab = (await queryNodes({ ancestorPattern: 'AchvTabBtn', name: 'tab' }))
        .sort((a, b) => Math.abs(a.cx - label.cx) - Math.abs(b.cx - label.cx))[0];
      if (tab && tab.cx >= 40 && tab.cx <= 680) return tab;
      const bars = await queryNodes({ ancestorPattern: 'AchvTabBar', name: 'scroll' });
      const bar = bars[0] || { x: 20, y: 930, width: 680, height: 220 };
      const y = Math.max(80, Math.min(1200, Math.round(bar.y + bar.height / 2)));
      if (label.cx > 680) {
        await page.mouse.move(640, y); await page.mouse.down(); await page.mouse.move(120, y, { steps: 12 }); await page.mouse.up();
      } else {
        await page.mouse.move(120, y); await page.mouse.down(); await page.mouse.move(640, y, { steps: 12 }); await page.mouse.up();
      }
      await page.waitForTimeout(350);
    }
    throw new Error(`cannot bring top tab into viewport: ${labelText}`);
  };

  const centerSelectedTop = async (labelText) => {
    await inject();
    const label = await findTopLabel(labelText);
    if (!label || (label.cx >= 210 && label.cx <= 510)) return;
    const bars = await queryNodes({ ancestorPattern: 'AchvTabBar', name: 'scroll' });
    const bar = bars[0] || { y: 880, height: 220 };
    const y = Math.max(900, Math.min(1060, Math.round(bar.y + bar.height * 0.72)));
    const fromX = Math.max(90, Math.min(630, label.cx));
    const toX = Math.max(90, Math.min(630, fromX + (360 - label.cx)));
    await page.mouse.move(fromX, y);
    await page.mouse.down();
    await page.mouse.move(toX, y, { steps: 12 });
    await page.mouse.up();
    await page.waitForTimeout(1200);
    await inject();
  };

  const pathStartsWith = (pathValue, prefix) => prefix.every((value, index) => pathValue[index] === value);
  const expectedSubCounts = [0, 4, 1, 3, 2, 2, 3];

  for (let topIndex = 0; topIndex < topTexts.length; topIndex++) {
    const tab = await bringIntoViewport(topTexts[topIndex]);
    await page.mouse.click(tab.cx, tab.cy);
    await page.waitForTimeout(650);
    await centerSelectedTop(topTexts[topIndex]);
    await inject();
    const label = await findTopLabel(topTexts[topIndex]);
    const topResult = { index: topIndex, label: label ? label.text : '', subTabs: [] };
    route.topTabs.push(topResult);
    await shot(`40_top_${topIndex + 1}.png`);
    await captureState(`top_${topIndex + 1}`);
    await recordWriteButtons(`top_${topIndex + 1}`);

    const topRootPath = label.path.slice(0, -3);
    let subs = (await queryNodes({ ancestorPattern: 'AchvTabSubBtn', name: 'sub_conta' }))
      .filter((node) => pathStartsWith(node.path, topRootPath))
      .sort((a, b) => a.path[a.path.length - 2] - b.path[b.path.length - 2]);
    if (subs.length !== expectedSubCounts[topIndex]) {
      throw new Error(`top ${topIndex + 1} expected ${expectedSubCounts[topIndex]} sub tabs, got ${subs.length}`);
    }
    const subPaths = subs.map((node) => node.path);
    for (let subIndex = 0; subIndex < subPaths.length; subIndex++) {
      const currentSub = (await queryNodes({ ancestorPattern: 'AchvTabSubBtn', path: subPaths[subIndex] }))[0];
      if (!currentSub || currentSub.cx < 0 || currentSub.cx > 720 || currentSub.cy < 0 || currentSub.cy > 1280) {
        throw new Error(`top ${topIndex + 1} sub ${subIndex + 1} is outside the viewport after centering`);
      }
      await page.mouse.click(currentSub.cx, currentSub.cy);
      await page.waitForTimeout(500);
      await inject();
      const subLabels = (await queryNodes({ ancestorPattern: 'AchvTabSubBtn', name: 'btn_text' }))
        .filter((node) => pathStartsWith(node.path, topRootPath)
          && Math.abs(node.cx - currentSub.cx) < 45 && Math.abs(node.cy - currentSub.cy) < 50);
      const rowCount = (await queryNodes({ ancestorPattern: 'achvSubItem', name: '_Image1' })).length;
      topResult.subTabs.push({ index: subIndex, label: subLabels[0] ? subLabels[0].text : '', rowCount });
      await shot(`50_top_${topIndex + 1}_sub_${subIndex + 1}.png`);
      await captureState(`top_${topIndex + 1}_sub_${subIndex + 1}`);
      await recordWriteButtons(`top_${topIndex + 1}_sub_${subIndex + 1}`);

      const detailScroll = (await queryNodes({ ancestorPattern: 'AchvMainView', name: '_Scroller3' }))[0];
      if (detailScroll && rowCount > 5) {
        const x = Math.max(80, Math.min(640, detailScroll.cx));
        const fromY = Math.min(1100, detailScroll.y + detailScroll.height - 80);
        const toY = Math.max(180, detailScroll.y + 100);
        await page.mouse.move(x, fromY); await page.mouse.down();
        await page.mouse.move(x, toY, { steps: 12 }); await page.mouse.up();
        await page.waitForTimeout(450);
        await shot(`51_top_${topIndex + 1}_sub_${subIndex + 1}_scrolled.png`);
      }
    }
  }

  const returns = [
    ...(await queryNodes({ ancestorPattern: 'BaseWindowSkin', name: '_img_return' })),
    ...(await queryNodes({ ancestorPattern: 'BaseWindowSkin', name: '_img_return0' })),
  ];
  if (!returns.length) throw new Error('achievement return button not found');
  await page.mouse.click(returns[0].cx, returns[0].cy);
  await page.waitForTimeout(800);
  await shot('60_return_to_role.png');
  await inject();
  const warmEntry = (await queryNodes({ ancestorPattern: 'EquipmentView', name: '_Group2' }))[0];
  if (!warmEntry) throw new Error('achievement entry missing after return');
  const warmStart = Date.now();
  await page.mouse.click(warmEntry.cx, warmEntry.cy);
  for (let attempt = 0; attempt < 40; attempt++) {
    await page.waitForTimeout(50);
    await inject();
    if ((await queryNodes({ ancestorPattern: 'AchvMainView', name: 'tabScroller' })).length > 0) break;
  }
  route.warmOpenMs = Date.now() - warmStart;
  await shot('61_warm_reopen.png');
  const final = await captureState('warm_reopen');
  await recordWriteButtons('warm_reopen');

  const inventory = [];
  for (const view of final.views || []) {
    const walk = (node) => {
      const text = node.textProps && node.textProps.text ? String(node.textProps.text) : '';
      if (node.effectiveVisible !== false
        && (node.mouseEnabled || text || /tab|btn|list|scroller|box|img|lab/i.test(node.name || ''))) {
        inventory.push({ view: view.meta.name, name: node.name, path: node.path,
          type: node.type, text, mouseEnabled: !!node.mouseEnabled,
          bounds: node.globalBounds || null, skin: node.skin || '' });
      }
      (node.children || []).forEach(walk);
    };
    walk(view.nodeTree);
  }
  fs.writeFileSync(path.join(OUTPUT, 'route_summary.json'), safeStringify(route), 'utf8');
  fs.writeFileSync(path.join(OUTPUT, 'visible_inventory.json'), safeStringify(inventory), 'utf8');
  console.log('SUMMARY', JSON.stringify({ topTabs: route.topTabs.length,
    subTabs: route.topTabs.reduce((sum, item) => sum + item.subTabs.length, 0),
    writesClicked: route.destructiveClicks.filter((item) => item.clicked).length,
    warmOpenMs: route.warmOpenMs }));
  await browser.close();
  try { fs.unlinkSync(tempModule); } catch (_) {}
  console.log('DONE');
})().catch((error) => {
  console.error('ERR', error && error.stack ? error.stack : String(error));
  process.exit(1);
});
