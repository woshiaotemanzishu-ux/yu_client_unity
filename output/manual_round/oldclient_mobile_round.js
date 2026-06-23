const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright');

const OUT_DIR = path.resolve(__dirname);
const URL = 'http://127.0.0.1:8090/index.html';
const PASSWORD = 'zxczxc';
const account = process.env.OLDCLIENT_ACCOUNT || `zxc${Date.now().toString().slice(-9)}`;
const RUN_LABEL = process.env.RUN_LABEL || 'oldclient_fresh';
const TARGET = process.env.OLDCLIENT_TARGET || 'all';

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function waitForLaya(page, timeoutMs = 30000) {
  await page.waitForFunction(() => window.Laya && window.Laya.stage, null, { timeout: timeoutMs });
}

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
      if (!node || depth > 16 || out.length > 2500) return;
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
          depth,
          cls,
          name,
          x: Number(node.x) || 0,
          y: Number(node.y) || 0,
          gx: Number(g.x) || 0,
          gy: Number(g.y) || 0,
          w: Number(node.width) || 0,
          h: Number(node.height) || 0,
          text,
          skin,
          child: children.length,
          mouseEnabled: node.mouseEnabled !== false,
        });
      }
      for (const child of Array.prototype.slice.call(children, 0, 140)) {
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

async function snapshot(page, label) {
  const png = await page.screenshot({ path: path.join(OUT_DIR, `${RUN_LABEL}_${label}.png`), fullPage: false });
  const stage = await collectStage(page);
  fs.writeFileSync(path.join(OUT_DIR, `${RUN_LABEL}_${label}_stage.json`), JSON.stringify(stage, null, 2));
  return png;
}

function isClickablePoint(node) {
  return Number.isFinite(node.gx) && Number.isFinite(node.gy) && node.w > 0 && node.h > 0;
}

async function getNode(page, name, options = {}) {
  const stage = await collectStage(page);
  let nodes = stage.nodes.filter(n => n.name === name && isClickablePoint(n));
  if (options.text) nodes = nodes.filter(n => n.text && n.text.includes(options.text));
  if (options.skin) nodes = nodes.filter(n => n.skin && n.skin.includes(options.skin));
  if (!nodes.length) {
    const sample = stage.nodes
      .filter(n => n.name || n.text || n.skin)
      .slice(-80)
      .map(n => `${n.name || n.text || n.skin}@${Math.round(n.gx)},${Math.round(n.gy)} ${Math.round(n.w)}x${Math.round(n.h)}`);
    throw new Error(`node not found: ${name}\nRecent nodes:\n${sample.join('\n')}`);
  }
  nodes.sort((a, b) => (a.depth - b.depth) || (a.gy - b.gy));
  return options.first ? nodes[0] : nodes[nodes.length - 1];
}

async function clickNode(page, name, options = {}) {
  const node = await getNode(page, name, options);
  const x = Math.max(1, Math.min(719, node.gx + node.w / 2));
  const y = Math.max(1, Math.min(1279, node.gy + node.h / 2));
  if (options.touch) {
    await page.touchscreen.tap(x, y).catch(() => {});
    await sleep(120);
  }
  await page.mouse.click(x, y);
  return { name, x, y, node };
}

async function fillNode(page, name, value) {
  const hit = await clickNode(page, name);
  await page.evaluate(({ nodeName, nodeValue }) => {
    const L = window.Laya;
    const nodes = [];
    function walk(node, parentVisible) {
      if (!node) return;
      const visible = parentVisible && node.visible !== false;
      if (visible && node.name === nodeName) nodes.push(node);
      const children = node._children || node._childs || [];
      for (const child of Array.prototype.slice.call(children, 0, 140)) walk(child, visible);
    }
    walk(L.stage, true);
    const node = nodes[nodes.length - 1];
    if (!node) throw new Error(`input node missing: ${nodeName}`);
    node.text = nodeValue;
    if (node.event) {
      node.event(L.Event.INPUT);
      node.event(L.Event.CHANGE);
    }
  }, { nodeName: name, nodeValue: value });
  return hit;
}

async function hasNode(page, name) {
  const stage = await collectStage(page);
  return stage.nodes.some(n => n.name === name && isClickablePoint(n));
}

async function clickIfVisible(page, name, options = {}) {
  if (!(await hasNode(page, name))) return false;
  await clickNode(page, name, options);
  return true;
}

async function waitForAny(page, names, timeoutMs = 30000) {
  const end = Date.now() + timeoutMs;
  while (Date.now() < end) {
    const stage = await collectStage(page);
    for (const name of names) {
      if (stage.nodes.some(n => n.name === name && isClickablePoint(n))) return name;
    }
    await sleep(500);
  }
  throw new Error(`timeout waiting for any of: ${names.join(', ')}`);
}

async function clickMainFunc(page, funcName) {
  const pos = await page.evaluate((name) => {
    const model = window.MainUIModel && window.MainUIModel.GetInstance && window.MainUIModel.GetInstance();
    const mainFunc = window.MainFunc && window.MainFunc[name];
    if (!model || !mainFunc || !model.GetIconPosInMainUIView) return null;
    return model.GetIconPosInMainUIView(2, mainFunc, { x: 50, y: 50 }, { x: 0, y: 0 });
  }, funcName);
  if (!pos || !Number.isFinite(pos.x) || !Number.isFinite(pos.y)) {
    throw new Error(`main func position missing: ${funcName}`);
  }
  await page.mouse.click(Math.max(1, Math.min(719, pos.x)), Math.max(1, Math.min(1279, pos.y)));
  return pos;
}

async function leaveRoleGate(page) {
  const gateVisible = await hasNode(page, 'LoginCreateRoleView') || await hasNode(page, 'LoginSelectRoleView') || await hasNode(page, '_img_enter');
  if (!gateVisible) return;

  for (let i = 0; i < 4; i += 1) {
    if (!(await hasNode(page, '_img_enter'))) break;
    await clickNode(page, '_img_enter', { touch: true });
    await sleep(3500);
    if (!(await hasNode(page, 'LoginCreateRoleView')) && !(await hasNode(page, 'LoginSelectRoleView'))) break;
  }
}

async function closeTopWindow(page) {
  const names = ['_img_close', '_btn_close', '_img_return', '_btn_return', '_img_back', '_btn_back'];
  for (const name of names) {
    if (await clickIfVisible(page, name)) {
      await sleep(1200);
      return true;
    }
  }
  await page.keyboard.press('Escape');
  await sleep(1200);
  return false;
}

async function openTarget(page, target) {
  const settleMs = target === 'setting' ? 2500 : target === 'chat' ? 2500 : 3500;
  if (target === 'setting') {
    await clickNode(page, '_box_setting');
  } else if (target === 'chat') {
    await clickNode(page, '_panel_chat');
  } else if (target === 'role') {
    await clickMainFunc(page, 'Role');
  } else if (target === 'bag') {
    await clickMainFunc(page, 'Bag');
  } else {
    throw new Error(`unknown target: ${target}`);
  }
  await sleep(settleMs);
  await snapshot(page, `20_${target}`);
}

(async () => {
  fs.mkdirSync(OUT_DIR, { recursive: true });
  const chromePath = 'C:/Program Files/Google/Chrome/Application/chrome.exe';
  const browser = await chromium.launch({
    headless: true,
    executablePath: fs.existsSync(chromePath) ? chromePath : undefined,
  });
  const context = await browser.newContext({
    viewport: { width: 720, height: 1280 },
    deviceScaleFactor: 1,
    isMobile: true,
    hasTouch: true,
  });
  const page = await context.newPage();
  const consoleLines = [];
  page.on('console', msg => consoleLines.push(`[${msg.type()}] ${msg.text()}`.slice(0, 500)));

  try {
    await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 30000 });
    await waitForLaya(page);
    await sleep(6000);
    await snapshot(page, '00_login');

    await clickNode(page, 'registerBtn');
    await sleep(1000);
    await snapshot(page, '01_register');

    await fillNode(page, 'account', account);
    await fillNode(page, 'password', PASSWORD);
    await clickNode(page, 'confirmBtn');
    await sleep(8000);
    await snapshot(page, '02_after_register');

    if (await hasNode(page, '_box_ok')) {
      await clickNode(page, '_box_ok');
      await sleep(3000);
      await snapshot(page, '03_after_agree');
    }

    await waitForAny(page, ['_img_enter', 'LoginCreateRoleView', 'LoginSelectRoleView'], 30000).catch(() => {});
    await leaveRoleGate(page);
    await sleep(10000);
    await snapshot(page, '04_after_enter');

    if (await hasNode(page, '_img_enter')) {
      await leaveRoleGate(page);
      await sleep(20000);
      await snapshot(page, '05_after_create_or_select');
    }

    if (await hasNode(page, '_box_skip')) {
      await clickNode(page, '_box_skip');
      await sleep(15000);
      await snapshot(page, '06_after_video_skip');
    }

    await waitForAny(page, ['_box_setting', '_panel_chat', '_gp_icon_con'], 45000);
    await sleep(1200);
    await snapshot(page, '10_main');

    if (TARGET !== 'all') {
      await openTarget(page, TARGET);
      fs.writeFileSync(path.join(OUT_DIR, `${RUN_LABEL}_console.log`), consoleLines.join('\n'));
      fs.writeFileSync(path.join(OUT_DIR, `${RUN_LABEL}_account.txt`), `account=${account}\npassword=${PASSWORD}\n`);
      console.log(JSON.stringify({ ok: true, account, target: TARGET, outDir: OUT_DIR, label: RUN_LABEL }, null, 2));
      return;
    }

    await clickMainFunc(page, 'Role');
    await sleep(3500);
    await snapshot(page, '20_role');
    await closeTopWindow(page);
    await snapshot(page, '21_after_role_close');

    await clickMainFunc(page, 'Bag');
    await sleep(3500);
    await snapshot(page, '30_bag');
    await closeTopWindow(page);
    await snapshot(page, '31_after_bag_close');

    await clickNode(page, '_panel_chat');
    await sleep(2500);
    await snapshot(page, '40_chat');
    await closeTopWindow(page);
    await snapshot(page, '41_after_chat_close');

    await clickNode(page, '_box_setting');
    await sleep(2500);
    await snapshot(page, '50_setting');

    fs.writeFileSync(path.join(OUT_DIR, `${RUN_LABEL}_console.log`), consoleLines.join('\n'));
    fs.writeFileSync(path.join(OUT_DIR, `${RUN_LABEL}_account.txt`), `account=${account}\npassword=${PASSWORD}\n`);
    console.log(JSON.stringify({ ok: true, account, outDir: OUT_DIR, label: RUN_LABEL }, null, 2));
  } catch (error) {
    fs.writeFileSync(path.join(OUT_DIR, `${RUN_LABEL}_console.log`), consoleLines.join('\n'));
    try { await snapshot(page, 'error'); } catch {}
    console.error(error && error.stack ? error.stack : String(error));
    process.exitCode = 1;
  } finally {
    await browser.close();
  }
})();
