/*
 * capture_snapshots.cjs — 无头驱动老客户端(LayaAir web)采运行时快照。
 *
 * 老客户端跑在 http://127.0.0.1:8090/index.html(Laya 游戏)。本脚本用 Playwright + 系统 Edge
 * 打开它,注入 pageSnapshot.js 工具(__sxListLoadedPages__ / __sxExportPageSnapshots__),
 * 把【当前已加载】或【指定】的视图导出成 page_snapshot_<view>.json,喂给烤制器。
 *
 * 已验证:无头驱动 + 注入 + 列举/导出 全跑通,零人工。
 *
 * 用法:
 *   node capture_snapshots.cjs --out <dir> [--url <url>] [--views A,B,C] [--wait 8000]
 *
 * 注意(导航/状态):
 *   - 冷启动的无头实例 stage 是空的(游戏刚 boot 没开 view),需要把游戏驱动到目标屏。
 *     对登录这类屏:等游戏自走到登录页(--wait 调大),或后续补 OPEN_VIEW 驱动。
 *   - 数据驱动屏(选角需角色、选服需服列表)冷启动开出来是空结构;要真数据得驱动登录流程。
 *   - 最稳的是 attach 到你【已经开着、已导航过】的实例:用 --cdp ws://... (需该实例开 remote-debugging)。
 *   - 实在采不到的单屏,用 electron 工具(yu-resource-tool)现成的快照导出 UI 兜底。
 */
const fs = require('fs');
const path = require('path');
const os = require('os');
const PLAYWRIGHT = 'e:/GitProject/yu_client_unity/output/node_modules/playwright';
const TOOL_SNAPSHOT_JS = 'e:/GitProject/yu_client/tools/yu-resource-tool/frontend/src/utils/pageSnapshot.js';

function arg(name, def) {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : def;
}

(async () => {
  const { chromium } = require(PLAYWRIGHT);
  const url = arg('url', 'http://127.0.0.1:8090/index.html');
  const outDir = arg('out', '.');
  const wantViews = (arg('views', '') || '').split(',').map(s => s.trim()).filter(Boolean);
  const waitMs = parseInt(arg('wait', '8000'), 10);
  const cdp = arg('cdp', '');
  fs.mkdirSync(outDir, { recursive: true });

  // pageSnapshot.js 是 ESM(export const),拷成临时 .mjs 再 import 取出已求值的字符串
  const tmpMjs = path.join(os.tmpdir(), 'pageSnapshot_' + Date.now() + '.mjs');
  fs.copyFileSync(TOOL_SNAPSHOT_JS, tmpMjs);
  const mod = await import('file:///' + tmpMjs.replace(/\\/g, '/'));
  const SNAP = mod.PAGE_SNAPSHOT_SCRIPT;

  const browser = cdp
    ? await chromium.connectOverCDP(cdp)
    : await chromium.launch({ headless: true, channel: 'msedge' });
  const ctx = cdp ? browser.contexts()[0] : await browser.newContext();
  const page = cdp ? ctx.pages()[0] || await ctx.newPage() : await ctx.newPage();

  if (!cdp) await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForFunction(() => !!(window.Laya && window.Laya.stage), { timeout: 30000 });
  if (waitMs > 0) await page.waitForTimeout(waitMs); // 等游戏自走到有 view 的状态
  await page.evaluate(SNAP + '; void 0');             // 注入快照工具

  // 自己驱动注册(老端 RegisterView 点确定=随机账号+密码自创号+登录),拿到登录态/服务器列表,
  // 解决数据驱动屏(选服/公告)冷采空壳。不用人工给凭据。
  if (process.argv.includes('--register')) {
    const acc = 't' + Math.floor(Math.random() * 1000000);
    await page.evaluate((a) => {
      try { window.LoginManager.GetInstance().Fire('LST.COMFIRM_REGISTER_BTN', a, a); } catch (e) {}
    }, acc);
    console.log('REGISTERED acc=' + acc + ' (等账号服返回服务器列表...)');
    await page.waitForTimeout(parseInt(arg('regwait', '9000'), 10));
  }

  // 导航:自己驱动游戏开 view(GlobalEventSystem.Fire OPEN_VIEW),不用人工点
  const openViews = (arg('open', '') || '').split(',').map(s => s.trim()).filter(Boolean);
  for (const v of openViews) {
    await page.evaluate((name) => {
      try {
        const G = window.GlobalEventSystem;
        if (G && G.Fire) { G.Fire('OPEN_VIEW', name); G.Fire('LST.OPEN_VIEW', name); }
      } catch (e) {}
    }, v);
    await page.waitForTimeout(700);   // 等异步加载 + 入场
  }

  // 部分数据屏(如选服)OPEN_VIEW 只出壳,要再 Fire 它的刷新事件才建列表
  // (如 LST.UPDATE_SELECT_SERVER_VIEW)。--fire EVT1,EVT2 在采集前补发。
  const fireEvents = (arg('fire', '') || '').split(',').map(s => s.trim()).filter(Boolean);
  for (const ev of fireEvents) {
    await page.evaluate((e) => { try { window.LoginManager.GetInstance().Fire(e); } catch (x) {} }, ev);
    await page.waitForTimeout(1000);
  }

  const list = await page.evaluate(() => window.__sxListLoadedPages__());
  const loaded = (list.views || []).map(v => v.name);
  console.log('LOADED:', loaded.join(', ') || '(空——游戏还没开任何 view)');

  const names = wantViews.length ? wantViews : loaded;
  const snap = await page.evaluate(ns => window.__sxExportPageSnapshots__(ns), names);
  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  // 每个视图单独写一份(烤制器按 page_snapshot*.json 扫目录)
  let n = 0;
  for (const v of snap.views || []) {
    const one = { version: snap.version, stage: snap.stage, views: [v] };
    const file = path.join(outDir, `page_snapshot_${v.meta.name}_${stamp}.json`);
    fs.writeFileSync(file, JSON.stringify(one), 'utf8');
    console.log('SAVED', v.meta.name, '(' + v.nodeCount + 'n) →', file);
    n++;
  }
  if (snap.missing && snap.missing.length) console.log('MISSING:', snap.missing.join(', '));
  console.log('DONE saved', n, 'snapshot(s)');
  await browser.close();
})().catch(e => { console.error('ERR:', String(e)); process.exit(1); });
