/* mainui_login_capture.cjs — 用测试账号直登老客户端,进主城,关弹窗,采 MainUI 全量快照
 * 用法: node mainui_login_capture.cjs <账号> <密码> <输出目录>
 */
const fs = require('fs');
const path = require('path');
const os = require('os');
const crypto = require('crypto');
const zlib = require('zlib');
const { chromium } = require('e:/GitProject/yu_client_unity/output/node_modules/playwright');

const ACC = process.argv[2] || '123123';
const PWD = process.argv[3] || '123123';
const OUT = process.argv[4] || __dirname;
const URL = process.argv[5] || 'http://127.0.0.1:8090/index.html';
const ROUTE = process.argv[6] || '';
const SHOTS = path.join(OUT, '_shots');
const FASHION_CAPTURE_WIDE = process.env.FASHION_CAPTURE_WIDE === '1';

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

let crcTable;
function crc32(buffer) {
  if (!crcTable) {
    crcTable = new Uint32Array(256);
    for (let n = 0; n < 256; n++) {
      let c = n;
      for (let k = 0; k < 8; k++) c = (c & 1) ? (0xedb88320 ^ (c >>> 1)) : (c >>> 1);
      crcTable[n] = c >>> 0;
    }
  }
  let c = 0xffffffff;
  for (const value of buffer) c = crcTable[(c ^ value) & 0xff] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
}

function pngChunk(type, data) {
  const tag = Buffer.from(type, 'ascii');
  const body = data || Buffer.alloc(0);
  const chunk = Buffer.concat([tag, body]);
  const out = Buffer.alloc(12 + body.length);
  out.writeUInt32BE(body.length, 0);
  chunk.copy(out, 4);
  out.writeUInt32BE(crc32(chunk), 8 + body.length);
  return out;
}

function encodeRgbaPng(width, height, rgba) {
  if (rgba.length !== width * height * 4) throw new Error(`bad RGBA byte count: ${rgba.length}`);
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8;
  ihdr[9] = 6;
  const stride = width * 4;
  const filtered = Buffer.alloc((stride + 1) * height);
  for (let y = 0; y < height; y++) rgba.copy(filtered, y * (stride + 1) + 1, y * stride, (y + 1) * stride);
  return Buffer.concat([
    Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    pngChunk('IHDR', ihdr),
    pngChunk('IDAT', zlib.deflateSync(filtered, { level: 9 })),
    pngChunk('IEND'),
  ]);
}

function sha256(buffer) {
  return crypto.createHash('sha256').update(buffer).digest('hex');
}

function packLegacyEffectCapture(capture, outputDir) {
  const frameWidth = capture.width;
  const frameHeight = capture.height;
  const frameCount = capture.frames.length;
  const columns = 12;
  const padding = 2;
  const cellWidth = frameWidth + padding * 2;
  const cellHeight = frameHeight + padding * 2;
  const rows = Math.ceil(frameCount / columns);
  const atlasWidth = columns * cellWidth;
  const atlasHeight = rows * cellHeight;
  const atlas = Buffer.alloc(atlasWidth * atlasHeight * 4);
  const rawFrames = capture.frames.map(frame => Buffer.from(frame, 'base64'));

  const copyPixel = (source, sx, sy, dx, dy) => {
    const si = (sy * frameWidth + sx) * 4;
    const di = (dy * atlasWidth + dx) * 4;
    source.copy(atlas, di, si, si + 4);
  };

  rawFrames.forEach((frame, index) => {
    if (frame.length !== frameWidth * frameHeight * 4) {
      throw new Error(`${capture.effectName} frame ${index} has ${frame.length} bytes`);
    }
    const cellX = (index % columns) * cellWidth;
    const cellY = Math.floor(index / columns) * cellHeight;
    for (let y = 0; y < frameHeight; y++) {
      // RenderTexture.getData follows the WebGL framebuffer origin (bottom-left).
      // PNG rows are top-left, so each isolated 100x100 frame is flipped once here.
      const sourceY = frameHeight - 1 - y;
      for (let x = 0; x < frameWidth; x++) copyPixel(frame, x, sourceY, cellX + padding + x, cellY + padding + y);
      for (let p = 0; p < padding; p++) {
        copyPixel(frame, 0, sourceY, cellX + p, cellY + padding + y);
        copyPixel(frame, frameWidth - 1, sourceY, cellX + padding + frameWidth + p, cellY + padding + y);
      }
    }
    const paddedRowBytes = cellWidth * 4;
    const firstRow = (cellY + padding) * atlasWidth * 4 + cellX * 4;
    const lastRow = (cellY + padding + frameHeight - 1) * atlasWidth * 4 + cellX * 4;
    for (let p = 0; p < padding; p++) {
      atlas.copy(atlas, (cellY + p) * atlasWidth * 4 + cellX * 4, firstRow, firstRow + paddedRowBytes);
      atlas.copy(atlas, (cellY + padding + frameHeight + p) * atlasWidth * 4 + cellX * 4,
        lastRow, lastRow + paddedRowBytes);
    }
  });

  const rgba = Buffer.concat(rawFrames);
  const png = encodeRgbaPng(atlasWidth, atlasHeight, atlas);
  const base = path.join(outputDir, capture.effectName);
  fs.writeFileSync(base + '.rgba', rgba);
  fs.writeFileSync(base + '_atlas.png', png);

  const previewIndex = Math.min(30, frameCount - 1);
  const preview = Buffer.alloc(frameWidth * frameHeight * 4);
  const previewSource = rawFrames[previewIndex];
  for (let y = 0; y < frameHeight; y++) {
    const sourceY = frameHeight - 1 - y;
    previewSource.copy(preview, y * frameWidth * 4, sourceY * frameWidth * 4, (sourceY + 1) * frameWidth * 4);
  }
  fs.writeFileSync(base + `_frame_${String(previewIndex).padStart(3, '0')}.png`,
    encodeRgbaPng(frameWidth, frameHeight, preview));

  let alphaNonZero = 0;
  let transparentRgbNonZero = 0;
  for (let i = 0; i < rgba.length; i += 4) {
    if (rgba[i + 3] !== 0) alphaNonZero++;
    else if (rgba[i] !== 0 || rgba[i + 1] !== 0 || rgba[i + 2] !== 0) transparentRgbNonZero++;
  }
  const metadata = {
    schema: 1,
    effectName: capture.effectName,
    source: 'old-laya-runtime-rendertexture-rgba',
    sourceOrigin: 'bottom-left',
    width: frameWidth,
    height: frameHeight,
    frameCount,
    frameRate: 60,
    durationSeconds: 2,
    frameTimesMs: capture.frameTimesMs,
    atlas: { width: atlasWidth, height: atlasHeight, columns, rows, padding, cellWidth, cellHeight },
    rgbaSha256: sha256(rgba),
    atlasPngSha256: sha256(png),
    alphaNonZeroPixels: alphaNonZero,
    transparentRgbNonZeroPixels: transparentRgbNonZero,
  };
  fs.writeFileSync(base + '_atlas.json', JSON.stringify(metadata, null, 2) + '\n', 'utf8');
  return metadata;
}

function readCurrentGmPassword() {
  if (process.env.SX_GM_PASSWORD) return process.env.SX_GM_PASSWORD;
  const configPath = 'E:/GitProject/yu_server/config/gsrv.config';
  const content = fs.readFileSync(configPath, 'utf8');
  const match = content.match(/\{\s*gm_password\s*,\s*"([^"]*)"\s*\}/);
  if (!match) throw new Error(`gm_password not found in ${configPath}`);
  return match[1];
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

  // BaseWindowComponent 内嵌的 BaseItem1（例如 MedalView/TitleMainView）不会独立进入
  // ViewManager/BASEVIEW_OPEN_OR_CLOSE 注册表。UI 精修不能因此退回静态 scene 坐标，
  // 这里直接从真实 Laya.stage 找目标根并保存页面根/全局矩形、pivot 与最终文本状态。
  const captureRuntimeSubtree = async (rootNames, fileName) => {
    const result = await page.evaluate((names) => {
      const wanted = new Set(names);
      const stage = window.Laya && Laya.stage;
      if (!stage) return { ok: false, reason: 'Laya.stage missing' };
      const childrenOf = node => node && (node._children || (node.numChildren
        ? Array.from({ length: node.numChildren }, (_, i) => node.getChildAt(i)) : [])) || [];
      const roots = [];
      const find = node => {
        if (!node) return;
        if (wanted.has(String(node.name || ''))) roots.push(node);
        for (const child of childrenOf(node)) find(child);
      };
      find(stage);
      const point = (node, x, y) => {
        try {
          const p = node.localToGlobal(new Laya.Point(x, y), true);
          return { x: Number(p.x.toFixed(3)), y: Number(p.y.toFixed(3)) };
        } catch (_) { return { x: 0, y: 0 }; }
      };
      const serialize = node => {
        const width = Number(node.width || 0), height = Number(node.height || 0);
        const corners = [point(node, 0, 0), point(node, width, 0), point(node, 0, height), point(node, width, height)];
        const xs = corners.map(p => p.x), ys = corners.map(p => p.y);
        const children = childrenOf(node).map(serialize);
        return {
          name: String(node.name || ''),
          type: node.constructor && node.constructor.name || '',
          localRect: { x: Number(node.x || 0), y: Number(node.y || 0), width, height },
          globalBounds: {
            x: Math.min(...xs), y: Math.min(...ys),
            width: Math.max(...xs) - Math.min(...xs), height: Math.max(...ys) - Math.min(...ys),
          },
          pivot: { x: Number(node.pivotX || 0), y: Number(node.pivotY || 0) },
          anchor: { x: Number(node.anchorX || 0), y: Number(node.anchorY || 0) },
          scale: { x: Number(node.scaleX == null ? 1 : node.scaleX), y: Number(node.scaleY == null ? 1 : node.scaleY) },
          visible: node.visible !== false,
          effectiveVisible: node.visible !== false && node.alpha !== 0 && !!node.parent,
          alpha: Number(node.alpha == null ? 1 : node.alpha),
          text: typeof node.text === 'string' ? node.text : undefined,
          html: typeof node.innerHTML === 'string' ? node.innerHTML : undefined,
          skin: typeof node.skin === 'string' ? node.skin : undefined,
          color: typeof node.color === 'string' ? node.color : undefined,
          fontSize: Number(node.fontSize || node.size || 0),
          children,
        };
      };
      return {
        ok: roots.length > 0,
        stage: { width: Number(stage.width || 0), height: Number(stage.height || 0) },
        requested: names,
        roots: roots.map(serialize),
      };
    }, rootNames);
    fs.writeFileSync(path.join(OUT, fileName), JSON.stringify({
      schema: 1,
      capturedAt: new Date().toISOString(),
      account: ACC,
      route: ROUTE,
      result,
    }, null, 2) + '\n', 'utf8');
    console.log('RUNTIME SUBTREE', fileName, JSON.stringify({ ok: result.ok, roots: result.roots && result.roots.map(v => v.name) }));
    if (!result.ok) throw new Error(`runtime subtree missing: ${rootNames.join(',')}`);
    return result;
  };

  // 把内嵌 BaseItem1 临时登记到快照注册表，再调用项目统一 pageSnapshot 序列化器。
  // 该文件可直接作为 LayaSceneConverter 的运行时烤制输入；登记只存在于本次无头页。
  const captureManagedRuntimePage = async (rootName, baseFile, fileName) => {
    const snapshot = await page.evaluate(({ rootName, baseFile }) => {
      const stage = window.Laya && Laya.stage;
      if (!stage || !window.__sxExportPageSnapshots__) return { error: 'snapshot runtime missing' };
      const childrenOf = node => node && (node._children || (node.numChildren
        ? Array.from({ length: node.numChildren }, (_, i) => node.getChildAt(i)) : [])) || [];
      let root = null;
      const walk = node => {
        if (!node || root) return;
        if (String(node.name || '') === rootName) { root = node; return; }
        for (const child of childrenOf(node)) walk(child);
      };
      walk(stage);
      if (!root) return { error: `${rootName} root missing` };
      window.__sxPageSnapshotRegistry__ = window.__sxPageSnapshotRegistry__ || {};
      const key = `managed_${rootName}`;
      window.__sxPageSnapshotRegistry__[key] = {
        name: rootName,
        view: {
          display_obj: root,
          base_file: baseFile,
          layout_file: rootName,
          is_loaded: true,
          HasOpen: () => root.visible !== false && !!root.parent,
        },
        source: 'ManagedNestedRuntime',
        open: true,
        seenAt: Date.now(),
      };
      return window.__sxExportPageSnapshots__([rootName]);
    }, { rootName, baseFile });
    if (snapshot.error || !snapshot.views || snapshot.views.length !== 1) {
      throw new Error(`managed runtime snapshot failed: ${snapshot.error || rootName}`);
    }
    fs.writeFileSync(path.join(OUT, fileName), safeStringify(snapshot) + '\n', 'utf8');
    console.log('MANAGED RUNTIME PAGE', fileName, `nodes=${snapshot.views[0].nodeCount}`);
    return snapshot;
  };

  const findVisibleText = async (text) => page.evaluate((target) => {
    const stage = window.Laya && Laya.stage;
    if (!stage) return null;
    const childrenOf = node => node && (node._children || (node.numChildren
      ? Array.from({ length: node.numChildren }, (_, i) => node.getChildAt(i)) : [])) || [];
    let match = null;
    const walk = node => {
      if (!node || match) return;
      if (node.visible !== false && String(node.text || '').trim() === target) {
        try {
          const p = node.localToGlobal(new Laya.Point((node.width || 0) / 2, (node.height || 0) / 2), true);
          match = { name: String(node.name || ''), cx: Number(p.x), cy: Number(p.y) };
          return;
        } catch (_) {}
      }
      for (const child of childrenOf(node)) walk(child);
    };
    walk(stage);
    return match;
  }, text);

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

  // 非写入的奖励飞行动画基线：直接调用老端公共表现入口，只用于逐帧核对
  // ui_bangyu 的单实例足迹、缓存复用、散开与收束；不发送领取协议、不改变账号。
  if (ROUTE === 'reward-fly-baseline') {
    const result = await page.evaluate(() => {
      const controllerClass = window.MainUIController;
      if (!controllerClass || typeof controllerClass.GetInstance !== 'function') {
        return { ok: false, reason: 'MainUIController missing' };
      }
      const controller = controllerClass.GetInstance();
      if (!controller || typeof controller.ShowDiamond !== 'function') {
        return { ok: false, reason: 'ShowDiamond missing' };
      }
      controller.ShowDiamond(2, 10, { x: 520, y: 300 });
      return { ok: true };
    });
    console.log('REWARD FLY BASELINE', JSON.stringify(result));
    if (!result.ok) throw new Error(result.reason);

    await shot('60_reward_fly_000ms.png');
    const frameDelays = [80, 170, 200, 300, 400, 350];
    const frameNames = ['080ms', '250ms', '450ms', '750ms', '1150ms', '1500ms'];
    for (let i = 0; i < frameDelays.length; i++) {
      await page.waitForTimeout(frameDelays[i]);
      await shot(`60_reward_fly_${frameNames[i]}.png`);
    }
  }

  // 旧端 UIEffect 的真实透明 RT 序列。这里隔离单个 100x100 source，不截整页、不反推粒子参数；
  // 产物供 Unity 复播同一份 RGBA，再由多个 2D presenter 复用，拓扑与老端一致。
  if (ROUTE === 'reward-fly-rgba') {
    const captureOne = async (effectName) => page.evaluate(async ({ effectName, frameCount }) => {
      const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
      const nextFrame = () => new Promise(resolve => requestAnimationFrame(resolve));
      const toBase64 = bytes => {
        let value = '';
        const chunk = 0x8000;
        for (let i = 0; i < bytes.length; i += chunk) {
          value += String.fromCharCode.apply(null, bytes.subarray(i, Math.min(bytes.length, i + chunk)));
        }
        return btoa(value);
      };

      if (!window.UIEffect || !window.Laya || !Laya.stage) {
        return { ok: false, reason: 'UIEffect/Laya.stage missing' };
      }

      const holder = new Laya.Box();
      holder.name = `__legacy_rgba_capture_${effectName}`;
      holder.width = holder.height = 100;
      holder.anchorX = holder.anchorY = 0.5;
      holder.pos(Laya.stage.width * 0.5, Laya.stage.height * 0.5);
      Laya.stage.addChild(holder);
      const effect = new window.UIEffect();
      effect.AddUIEffect(effectName, holder, null, 14);
      const key = `${effectName}@0@0@14@14@14@100@100`;

      let info = null;
      for (let i = 0; i < 300; i++) {
        info = window.UIEffect.ALL_UIEFFECT_DIC[key];
        if (info && info.loaded && info.render_texture && info.gameObject) break;
        await sleep(50);
      }
      if (!info || !info.loaded || !info.render_texture || !info.gameObject) {
        effect.ResetUIEffect();
        holder.removeSelf();
        holder.destroy(true);
        return { ok: false, reason: `effect source not ready: ${key}` };
      }

      const rt = info.render_texture;
      const width = Number(rt.width || rt._width || 0);
      const height = Number(rt.height || rt._height || 0);
      if (width !== 100 || height !== 100) {
        effect.ResetUIEffect();
        holder.removeSelf();
        holder.destroy(true);
        return { ok: false, reason: `unexpected RT size ${width}x${height}` };
      }

      // Laya 项目自身也用 active false->true 重播粒子；在此锁定 phase=0 后连续取 120 帧。
      info.gameObject.active = false;
      await nextFrame();
      info.gameObject.active = true;
      const frames = [];
      const frameTimesMs = [];
      const startedAt = performance.now();
      const read = time => {
        // This project uses Laya.RenderTexture (3D), whose getData overload requires
        // the caller-provided output buffer; RenderTexture2D's four-argument overload
        // would silently return an empty value here.
        const pixels = rt.getData(0, 0, width, height, new Uint8Array(width * height * 4));
        if (!pixels || pixels.length !== width * height * 4) {
          throw new Error(`RenderTexture.getData returned ${pixels ? pixels.length : 0} bytes`);
        }
        frames.push(toBase64(pixels));
        frameTimesMs.push(Number((time - startedAt).toFixed(3)));
      };
      read(startedAt);
      while (frames.length < frameCount) {
        const time = await nextFrame();
        // Headless Edge may expose the host's 120/144Hz rAF cadence, while this Laya
        // project declares 60fps. Sample the RT on the legacy game's 60Hz timeline.
        const targetTime = frames.length * (1000 / 60);
        if (time - startedAt + 0.25 >= targetTime) read(time);
      }

      effect.ResetUIEffect();
      holder.removeSelf();
      holder.destroy(true);
      return { ok: true, effectName, width, height, frames, frameTimesMs };
    }, { effectName, frameCount: 120 });

    const captureDir = path.join(OUT, 'legacy_rgba');
    fs.mkdirSync(captureDir, { recursive: true });
    const captures = [];
    for (const effectName of ['ui_bangyu_1', 'ui_bangyu_2']) {
      const capture = await captureOne(effectName);
      if (!capture.ok) throw new Error(capture.reason);
      const elapsed = capture.frameTimesMs[capture.frameTimesMs.length - 1];
      if (elapsed < 1800 || elapsed > 2300) {
        throw new Error(`${effectName} capture cadence drifted: 120 frames in ${elapsed}ms`);
      }
      const metadata = packLegacyEffectCapture(capture, captureDir);
      captures.push(metadata);
      console.log('LEGACY RGBA', JSON.stringify({
        effectName,
        frameCount: metadata.frameCount,
        elapsedMs: elapsed,
        atlasPngSha256: metadata.atlasPngSha256,
      }));
    }
    fs.writeFileSync(path.join(captureDir, 'capture-manifest.json'),
      JSON.stringify({ schema: 1, capturedAt: new Date().toISOString(), captures }, null, 2) + '\n', 'utf8');
  }

  // 为真实领取链准备一条“已达成、未领取”的成就。只完成一个当前未达成系列，
  // 不调用 clearachv、不代替玩家点击 40905，也不批量领取任何奖励。
  if (ROUTE === 'achievement-gm-ready') {
    const gmPassword = readCurrentGmPassword();
    const result = await page.evaluate(async ({ gmPassword }) => {
      const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
      if (!window.achvModel || !window.CheatModel) {
        return { ok: false, reason: 'achvModel/CheatModel missing' };
      }
      const model = window.achvModel.GetInstance();
      const cheat = window.CheatModel.GetInstance();
      const snapshot = () => {
        const source = model.GetAchvDataList && model.GetAchvDataList();
        const list = [];
        if (source) {
          for (const key of Object.keys(source)) {
            const value = source[key];
            if (!value || !Number.isFinite(Number(value.category))) continue;
            list.push({
              category: Number(value.category),
              id: Number(value.id || 0),
              status: Number(value.status || 0),
              progress: Number(value.progress || 0),
            });
          }
        }
        return list.sort((a, b) => a.category - b.category);
      };
      const send = command => cheat.Fire(window.CheatModel.SEND_CHEAT_TO_SERVER, command);

      for (let i = 0; i < 80; i++) {
        if (snapshot().length) break;
        await sleep(100);
      }
      const before = snapshot();
      if (!before.length) return { ok: false, reason: 'achievement summary not ready' };

      const categoryCfg = model.AcCategoryCfg || {};
      const candidates = before.filter(value => {
        const cfg = categoryCfg[value.category];
        return value.category > 0 && value.status === 0 && (!cfg || cfg.lv === undefined || Number(cfg.lv) <= 999999);
      });
      if (!candidates.length) {
        return {
          ok: false,
          reason: 'no unfinished achievement category on this role',
          before,
          existingClaimable: before.filter(value => value.status === 1),
        };
      }

      // 优先补当前编号最靠前的一条，控制改动面；领取仍留给 Unity 真实 UI。
      const selected = candidates[0];
      if (gmPassword) {
        send(`setgmpassword_${gmPassword}`);
        await sleep(250);
      }
      send(`completeachv_${selected.category}`);

      let after = snapshot();
      for (let i = 0; i < 80; i++) {
        const current = after.find(value => value.category === selected.category);
        if (current && current.status === 1) break;
        await sleep(100);
        after = snapshot();
      }
      const prepared = after.find(value => value.category === selected.category);
      return {
        ok: !!prepared && prepared.status === 1,
        selectedCategory: selected.category,
        before: selected,
        after: prepared || null,
        claimableCategories: after.filter(value => value.status === 1).map(value => value.category),
      };
    }, { gmPassword });

    const evidence = {
      schema: 1,
      account: ACC,
      command: result.selectedCategory ? `completeachv_${result.selectedCategory}` : null,
      destructiveResetUsed: false,
      claimProtocolSent: false,
      observedAt: new Date().toISOString(),
      result,
    };
    fs.writeFileSync(path.join(OUT, 'achievement-gm-ready.json'),
      JSON.stringify(evidence, null, 2) + '\n', 'utf8');
    console.log('ACHIEVEMENT GM READY', JSON.stringify({
      ok: result.ok,
      selectedCategory: result.selectedCategory || null,
      before: result.before || null,
      after: result.after || null,
      claimableCategories: result.claimableCategories || [],
      reason: result.reason || null,
    }));
    if (!result.ok) throw new Error(result.reason || 'achievement GM preparation failed');
  }

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

  // 人物页[境界]真实基线：从玩家可见入口打开地境/天境外窗。
  if (ROUTE === 'role-medal') {
    await page.mouse.click(120, 1218);
    await page.waitForTimeout(5000);
    await inject();
    await shot('60_role_person.png');

    const entry = await findNodes('EquipmentView', '_Group3');
    console.log('MEDAL ENTRY', JSON.stringify(entry));
    if (!entry.nodes.length) throw new Error('EquipmentView._Group3 not found');
    await page.mouse.click(entry.nodes[0].cx, entry.nodes[0].cy);
    await page.waitForTimeout(5000);
    await inject();
    await shot('61_medal_ground.png');
    await captureRuntimeSubtree(['MedalView'], 'runtime_subtree_MedalView_ground.json');
    await captureManagedRuntimePage('MedalView', 'medal', 'page_snapshot_MedalView_runtime.json');

    const skyTab = await findVisibleText('天境');
    console.log('MEDAL SKY TAB', JSON.stringify(skyTab));
    if (!skyTab) throw new Error('天境 tab not found');
    await page.mouse.click(skyTab.cx, skyTab.cy);
    await page.waitForTimeout(5000);
    await inject();
    await shot('62_medal_sky.png');

    // 同一个“如月”标题在主展示(scale=3.5)和列表项(scale=5)中各自拥有私有 RT。
    // 把两个源同时锁回 phase=0，再在 350/700ms 读取真实 RGBA；不截整页、不经过 Canvas 合成。
    const titleEffectInventory = await page.evaluate(() => {
      const all = window.UIEffect && window.UIEffect.ALL_UIEFFECT_DIC;
      if (!all) return [];
      return Object.keys(all).filter(key => /shenming|title/i.test(key)).map(key => {
        const info = all[key];
        const parts = key.split('@');
        const parents = info && (info.parent_list || info._parent_list) || [];
        return {
          key,
          effectName: parts[0],
          position: [Number(parts[1]), Number(parts[2])],
          scale: [Number(parts[3]), Number(parts[4]), Number(parts[5])],
          parentSize: [Number(parts[6]), Number(parts[7])],
          loaded: !!(info && info.loaded),
          rtSize: info && info.render_texture
            ? [Number(info.render_texture.width || info.render_texture._width || 0),
              Number(info.render_texture.height || info.render_texture._height || 0)]
            : null,
          parents: Array.from(parents).map(parent => ({
            name: String(parent && parent.name || ''),
            width: Number(parent && parent.width || 0),
            height: Number(parent && parent.height || 0),
          })),
        };
      });
    });
    console.log('TITLE EFFECT KEY INVENTORY', JSON.stringify(titleEffectInventory));
    const mainSource = titleEffectInventory.find(entry => entry.effectName === 'effect_shenmingjiemian_01'
      && entry.scale[0] === 3.5 && entry.scale[1] === 3.5 && entry.scale[2] === 3.5);
    const itemSource = titleEffectInventory.find(entry => entry.effectName === 'effect_shenmingjiemian_01'
      && entry.scale[0] === 5 && entry.scale[1] === 5 && entry.scale[2] === 5);
    if (!mainSource || !itemSource) {
      throw new Error(`title raw source selection failed: ${JSON.stringify(titleEffectInventory)}`);
    }

    const titleRaw = await page.evaluate(async ({ mainKey, itemKey }) => {
      const nextFrame = () => new Promise(resolve => requestAnimationFrame(resolve));
      const toBase64 = bytes => {
        let value = '';
        const chunk = 0x8000;
        for (let i = 0; i < bytes.length; i += chunk) {
          value += String.fromCharCode.apply(null, bytes.subarray(i, Math.min(bytes.length, i + chunk)));
        }
        return btoa(value);
      };
      const all = window.UIEffect && window.UIEffect.ALL_UIEFFECT_DIC;
      if (!all) return { ok: false, reason: 'UIEffect.ALL_UIEFFECT_DIC missing' };
      const main = { key: mainKey, info: all[mainKey] };
      const item = { key: itemKey, info: all[itemKey] };
      if (!main.info || !item.info || !main.info.loaded || !item.info.loaded
        || !main.info.render_texture || !item.info.render_texture
        || !main.info.gameObject || !item.info.gameObject) {
        return { ok: false, reason: `selected title sources not ready: ${mainKey}|${itemKey}` };
      }
      const selected = [
        { role: 'main', key: main.key, info: main.info, targetMs: 350 },
        { role: 'item', key: item.key, info: item.info, targetMs: 700 },
      ];
      for (const entry of selected) entry.info.gameObject.active = false;
      await nextFrame();
      for (const entry of selected) entry.info.gameObject.active = true;

      const startedAt = performance.now();
      const captures = [];
      for (const targetMs of [350, 700]) {
        let now = performance.now();
        while (now - startedAt < targetMs) now = await nextFrame();
        for (const entry of selected) {
          const rt = entry.info.render_texture;
          const width = Number(rt.width || rt._width || 0);
          const height = Number(rt.height || rt._height || 0);
          const pixels = rt.getData(0, 0, width, height, new Uint8Array(width * height * 4));
          if (!pixels || pixels.length !== width * height * 4) {
            return { ok: false, reason: `${entry.role} getData returned ${pixels ? pixels.length : 0}` };
          }
          captures.push({
            role: entry.role,
            key: entry.key,
            width,
            height,
            targetMs,
            observedMs: Number((now - startedAt).toFixed(3)),
            rgba: toBase64(pixels),
          });
        }
      }
      return { ok: true, effectName: 'effect_shenmingjiemian_01', sourceOrigin: 'bottom-left', captures };
    }, { mainKey: mainSource.key, itemKey: itemSource.key });
    if (!titleRaw.ok) throw new Error(`title raw capture failed: ${titleRaw.reason}`);

    const rawDir = path.join(OUT, 'old_raw');
    fs.mkdirSync(rawDir, { recursive: true });
    const rawManifest = [];
    for (const capture of titleRaw.captures) {
      const source = Buffer.from(capture.rgba, 'base64');
      const pngRgba = Buffer.alloc(source.length);
      for (let y = 0; y < capture.height; y++) {
        const sourceY = capture.height - 1 - y;
        source.copy(pngRgba, y * capture.width * 4,
          sourceY * capture.width * 4, (sourceY + 1) * capture.width * 4);
      }
      let alphaPixels = 0, highSaturationPixels = 0;
      let alphaSum = 0, saturationSum = 0, redSum = 0, greenSum = 0, blueSum = 0;
      let xMin = capture.width, yMin = capture.height, xMax = -1, yMax = -1;
      for (let i = 0; i < source.length; i += 4) {
        const r = source[i], g = source[i + 1], b = source[i + 2], a = source[i + 3];
        if (a <= 2) continue;
        alphaPixels++;
        alphaSum += a; redSum += r; greenSum += g; blueSum += b;
        const max = Math.max(r, g, b), min = Math.min(r, g, b);
        const saturation = max ? (max - min) / max : 0;
        saturationSum += saturation;
        if (saturation >= 0.4) highSaturationPixels++;
        const pixel = i / 4, x = pixel % capture.width, y = Math.floor(pixel / capture.width);
        xMin = Math.min(xMin, x); yMin = Math.min(yMin, y);
        xMax = Math.max(xMax, x); yMax = Math.max(yMax, y);
      }
      const base = `${capture.role}_${String(capture.targetMs).padStart(4, '0')}ms`;
      const png = encodeRgbaPng(capture.width, capture.height, pngRgba);
      fs.writeFileSync(path.join(rawDir, base + '.rgba'), source);
      fs.writeFileSync(path.join(rawDir, base + '.png'), png);
      rawManifest.push({
        role: capture.role,
        effectName: titleRaw.effectName,
        key: capture.key,
        width: capture.width,
        height: capture.height,
        targetMs: capture.targetMs,
        observedMs: capture.observedMs,
        sourceOrigin: titleRaw.sourceOrigin,
        rgbaSha256: sha256(source),
        pngSha256: sha256(png),
        alphaPixels,
        meanAlpha: alphaPixels ? alphaSum / alphaPixels : 0,
        meanRgb: alphaPixels ? [redSum / alphaPixels, greenSum / alphaPixels, blueSum / alphaPixels] : [0, 0, 0],
        meanSaturation: alphaPixels ? saturationSum / alphaPixels : 0,
        highSaturationShare: alphaPixels ? highSaturationPixels / alphaPixels : 0,
        bboxBottomLeft: alphaPixels ? { xMin, yMin, xMax, yMax } : null,
        rgbaFile: base + '.rgba',
        pngFile: base + '.png',
      });
    }
    fs.writeFileSync(path.join(rawDir, 'manifest.json'), JSON.stringify({
      schema: 1,
      capturedAt: new Date().toISOString(),
      account: ACC,
      route: ROUTE,
      effectName: titleRaw.effectName,
      captures: rawManifest,
    }, null, 2) + '\n', 'utf8');
    console.log('TITLE RAW', JSON.stringify(rawManifest.map(v => ({
      role: v.role, targetMs: v.targetMs, observedMs: v.observedMs,
      size: `${v.width}x${v.height}`, alphaPixels: v.alphaPixels,
      meanSaturation: v.meanSaturation, highSaturationShare: v.highSaturationShare,
    }))));

    await captureRuntimeSubtree(['TitleMainView'], 'runtime_subtree_TitleMainView_sky.json');
    await captureManagedRuntimePage('TitleMainView', 'title', 'page_snapshot_TitleMainView_runtime.json');
    await captureManagedRuntimePage('TitleItem', 'title', 'page_snapshot_TitleItem_runtime.json');
    await captureManagedRuntimePage('TitleAttrItem', 'title', 'page_snapshot_TitleAttrItem_runtime.json');
  }

  // 人物页[名誉]真实基线：打开名誉窗并记录列表、获取按钮和关闭链。
  if (ROUTE === 'role-honour') {
    await page.mouse.click(120, 1218);
    await page.waitForTimeout(5000);
    await inject();
    await shot('60_role_person.png');

    const entry = await findNodes('EquipmentView', '_btn_fame');
    console.log('HONOUR ENTRY', JSON.stringify(entry));
    if (!entry.nodes.length) throw new Error('EquipmentView._btn_fame not found');
    await page.mouse.click(entry.nodes[0].cx, entry.nodes[0].cy);
    await page.waitForTimeout(3500);
    await inject();
    await shot('61_honour.png');
  }

  // FashionMain(pos=1) 页面专用只读降级路线。登录、选角、进城、弹窗清理由上面的公共链复用；
  // 这里只保存老端真实 Canvas 的 cold/warm、列表滚动、指定条目、等级弹窗和返回链。
  if (ROUTE === 'fashion-main-pos1-current') {
    const routeStartedAt = new Date().toISOString();
    const steps = [];
    const clickRoute = async (label, x, y, waitMs) => {
      const before = Date.now();
      await page.mouse.click(x, y);
      await page.waitForTimeout(waitMs);
      steps.push({ action: 'click', label, point: { x, y }, elapsedMs: Date.now() - before });
    };
    const dragRoute = async (label, from, to, waitMs, settleBeforeUpMs = 0) => {
      const before = Date.now();
      await page.mouse.move(from.x, from.y);
      await page.mouse.down();
      await page.mouse.move(to.x, to.y, { steps: 16 });
      if (settleBeforeUpMs) await page.waitForTimeout(settleBeforeUpMs);
      await page.mouse.up();
      await page.waitForTimeout(waitMs);
      steps.push({ action: 'drag', label, from, to, elapsedMs: Date.now() - before });
    };

    await clickRoute('mainui-role', 110, 1219, 5000);
    await inject();
    await shot('60_fashion_role_person.png');

    const coldStart = Date.now();
    await page.mouse.click(75, 538);
    await page.waitForTimeout(350);
    await shot('61_fashion_cold_350ms.png');
    await page.waitForTimeout(650);
    await shot('61_fashion_cold_1000ms.png');
    await page.waitForTimeout(2500);
    await inject();
    await shot('61_fashion_cold_ready.png');
    steps.push({ action: 'open', label: 'fashion-cold', elapsedMs: Date.now() - coldStart });

    await clickRoute('fashion-tab-current', 75, 1121, 500);
    await clickRoute('fashion-list-visible-1', 143, 747, 700);
    await clickRoute('fashion-list-visible-2', 253, 747, 700);
    await dragRoute('fashion-list-horizontal', { x: 545, y: 760 }, { x: 193, y: 760 }, 1200);
    await shot('62_fashion_list_dragged.png');
    // 先保留真实惯性滚动证据，再用反向慢拖消除惯性并把 12010008 精确放回可见区。
    await dragRoute('fashion-list-reverse-to-sweetheart', { x: 193, y: 760 },
      { x: 523, y: 760 }, 900, 650);
    await shot('62b_fashion_sweetheart_visible.png');
    await clickRoute('fashion-list-sweetheart', 237, 747, 1800);
    const selectedFashionName = await page.evaluate(() => {
      const stage = window.Laya && Laya.stage;
      const childrenOf = node => node && (node._children || (node.numChildren
        ? Array.from({ length: node.numChildren }, (_, i) => node.getChildAt(i)) : [])) || [];
      const candidates = [];
      const walk = node => {
        if (!node) return;
        if (node.visible !== false && String(node.name || '') === '_lb_name') {
          try {
            const point = node.localToGlobal(new Laya.Point(0, 0), true);
            candidates.push({ text: String(node.text || ''), x: Number(point.x), y: Number(point.y) });
          } catch (_) {}
        }
        for (const child of childrenOf(node)) walk(child);
      };
      walk(stage);
      const detailName = candidates.find(value => value.x >= 80 && value.x <= 260
        && value.y >= 800 && value.y <= 880);
      return detailName ? detailName.text : null;
    });
    if (selectedFashionName !== '甜心宝贝') {
      throw new Error(`old Fashion identity mismatch: expected 甜心宝贝, got ${selectedFashionName}`);
    }
    await shot('63_fashion_sweetheart.png');
    await dragRoute('fashion-attributes-vertical', { x: 451, y: 1004 }, { x: 451, y: 921 }, 900);
    await shot('64_fashion_attributes_dragged.png');

    await page.mouse.click(652, 155);
    await page.waitForTimeout(350);
    await shot('65_fashion_level_350ms.png');
    await page.waitForTimeout(650);
    await shot('65_fashion_level_1000ms.png');
    await page.waitForTimeout(1700);
    await inject();
    await shot('65_fashion_level_ready.png');
    await clickRoute('fashion-level-close', 656, 325, 900);
    await shot('66_fashion_level_closed.png');
    await clickRoute('fashion-return-cold', 667, 1121, 1000);
    await shot('67_fashion_return_role.png');

    const warmStart = Date.now();
    await page.mouse.click(75, 538);
    await page.waitForTimeout(350);
    await shot('68_fashion_warm_350ms.png');
    await page.waitForTimeout(650);
    await shot('68_fashion_warm_1000ms.png');
    await page.waitForTimeout(1800);
    await inject();
    await shot('68_fashion_warm_ready.png');
    steps.push({ action: 'open', label: 'fashion-warm', elapsedMs: Date.now() - warmStart });
    if (FASHION_CAPTURE_WIDE) {
      await page.setViewportSize({ width: 1920, height: 1080 });
      await page.waitForTimeout(900);
      await shot('68_fashion_warm_ready_1920x1080.png');
      steps.push({ action: 'viewport', label: 'fashion-wide-ready',
        viewport: { width: 1920, height: 1080, deviceScaleFactor: 1 } });
      await page.setViewportSize({ width: 720, height: 1280 });
      await page.waitForTimeout(500);
    }
    await clickRoute('fashion-return-warm', 667, 1121, 900);
    await shot('69_fashion_warm_return_role.png');

    const state = await getState();
    fs.writeFileSync(path.join(OUT, 'fashion-main-pos1-current.json'), JSON.stringify({
      schema: 1,
      authority: 'old-h5-real-canvas-runtime',
      account: ACC,
      passwordRecorded: false,
      route: ROUTE,
      startedAt: routeStartedAt,
      finishedAt: new Date().toISOString(),
      viewport: { width: 720, height: 1280, deviceScaleFactor: 1 },
      viewports: FASHION_CAPTURE_WIDE ? ['720x1280', '1920x1080'] : ['720x1280'],
      steps,
      selectedFashionName,
      finalLoadedViews: state,
      writeTransactionsAuthorized: false,
    }, null, 2) + '\n', 'utf8');
  }

  // 导出全部已加载视图,一视图一文件(烤制器格式)
  const list = await page.evaluate(() => window.__sxListLoadedPages__());
  const names = (list.views || []).map(v => v.name);
  console.log('FINAL LOADED:', names.join(', '));
  const exportNames = names.filter(n => EXPORT.test(n)
    || (ROUTE === 'role-instruction' && n === 'InstructionView')
    || (ROUTE === 'role-attribute-potion' && /EquipmentView|attributePotionView/i.test(n))
     || (ROUTE === 'role-medal' && /EquipmentView|MedalEnterView|MedalView|TitleMainView/i.test(n))
     || (ROUTE === 'role-honour' && /EquipmentView|MarriageHonourView/i.test(n))
     || (ROUTE === 'fashion-main-pos1-current' && /EquipmentView|FashionMainView|FashionLevelView/i.test(n)));
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
