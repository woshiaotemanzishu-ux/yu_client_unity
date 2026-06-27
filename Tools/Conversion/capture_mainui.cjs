/*
 * capture_mainui.cjs — 无头驱动老客户端一路进游戏世界,采【主界面 HUD】运行时快照。
 *
 * 主界面只在进入游戏世界后存在(GAME_START → MainUIController.InitMainUI 建 HUD)。
 * 登录是个 6 态状态机(Register→Login→SimplifyServer→[选服]→ServerWin→ConnectGame),
 * Register→Login→SimplifyServer 自动链到 LoginEnterView;之后必须【选服】(设 cur_server_id)
 * 再 Fire LOGIN_STATE_CHANGE→ServerWin 才连游戏服。本脚本自适应分阶段驱动:
 *   强制 Register 态 → COMFIRM_REGISTER_BTN(自创号)→ 等 LoginEnterView(账号登录+服列表)
 *   → 从 ServerModel.server_data 选第一个开放服 SetCurServerId → Fire ServerWin(连游戏服)
 *   → 等 LoginCreateRoleView(10000 取角色列表,新号 role_num==0)→ 读 ui_cfg 合法 career/sex
 *   → Fire TRY_CREATE_ROLE → 10003 成功自动 TRY_LOGIN_GAME(10004)→ GAME_START → InitMainUI
 *   → 等 HUD 出现 → 导出全部已加载视图快照。
 *
 * 事件名一律从 window.LoginStateEvent 取(值有 typo,如 TRY_CREATE_ROLE='LST.CREATETRY_CREATE_ROLE_ROLE')。
 *
 * 用法: node capture_mainui.cjs --out <dir> [--url ...] [--name 测试角色]
 */
const fs = require('fs');
const path = require('path');
const os = require('os');
const PLAYWRIGHT = 'd:/git_res/yu_client_unity/output/node_modules/playwright';
const TOOL_SNAPSHOT_JS = 'd:/git_res/yu_client/tools/yu-resource-tool/frontend/src/utils/pageSnapshot.js';

const HUD_VIEWS = ['MainUITopView','MainUIActivityView','MainUISkillView','MainUIChatView',
  'MainUISecondaryView','MainUITaskTeamView','MainUIDownView','MainUIAutoBrushView','UIJoyStick'];

function arg(name, def) {
  const i = process.argv.indexOf('--' + name);
  return i >= 0 && i + 1 < process.argv.length ? process.argv[i + 1] : def;
}

(async () => {
  const { chromium } = require(PLAYWRIGHT);
  const url = arg('url', 'http://127.0.0.1:8090/index.html');
  const outDir = arg('out', '.');
  // 角色名:服务器拒数字/非法字符(10007),用纯中文随机名(4字×权重2=8,在 4~12 内)保证合法+唯一
  const NAME_POOL = '云风林雨山海天月星辰龙虎川峰然轩宇浩瀚青碧紫丹枫松柏鹤鹰雪霜烟霞清绝幽寒';
  const randName = () => Array.from({ length: 4 }, () => NAME_POOL[Math.floor(Math.random() * NAME_POOL.length)]).join('');
  const roleName = arg('name', randName());
  fs.mkdirSync(outDir, { recursive: true });

  const tmpMjs = path.join(os.tmpdir(), 'pageSnapshot_' + Date.now() + '.mjs');
  fs.copyFileSync(TOOL_SNAPSHOT_JS, tmpMjs);
  const mod = await import('file:///' + tmpMjs.replace(/\\/g, '/'));
  const SNAP = mod.PAGE_SNAPSHOT_SCRIPT;

  const browser = await chromium.launch({ headless: true, channel: 'msedge', args: [
    '--disable-background-timer-throttling', '--disable-backgrounding-occluded-windows',
    '--disable-renderer-backgrounding', '--disable-features=CalculateNativeWinOcclusion',
  ] });
  const ctx = await browser.newContext({ viewport: { width: 720, height: 1280 } });
  // 关键:无头页面 document.hidden=true 会让 Laya 暂停主循环/socket flush(进游戏服后 10000 发不出/收不到)。
  // 伪装成始终可见,保持游戏循环跑。
  await ctx.addInitScript(() => {
    try {
      Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => 'visible' });
      Object.defineProperty(document, 'hidden', { configurable: true, get: () => false });
      Object.defineProperty(document, 'webkitVisibilityState', { configurable: true, get: () => 'visible' });
      Object.defineProperty(document, 'webkitHidden', { configurable: true, get: () => false });
      document.hasFocus = () => true;
    } catch (e) { console.log('vis spoof err ' + e); }
  });
  // 在任何页面脚本之前钩住 WebSocket(Laya 会缓存构造器,必须 pre-boot 才抓得到游戏服 socket)
  await ctx.addInitScript(() => {
    const OldWS = window.WebSocket;
    function HookWS(url, proto) {
      console.log('WS-OPENING ' + url);
      const ws = proto !== undefined ? new OldWS(url, proto) : new OldWS(url);
      ws.addEventListener('open', () => console.log('WS-OPEN ' + url));
      ws.addEventListener('error', () => console.log('WS-ERROR ' + url));
      ws.addEventListener('close', e => console.log('WS-CLOSE ' + url + ' code=' + e.code + ' reason=' + e.reason));
      return ws;
    }
    HookWS.prototype = OldWS.prototype;
    HookWS.CONNECTING = OldWS.CONNECTING; HookWS.OPEN = OldWS.OPEN; HookWS.CLOSING = OldWS.CLOSING; HookWS.CLOSED = OldWS.CLOSED;
    window.WebSocket = HookWS;
  });
  const page = await ctx.newPage();
  page.on('console', m => { const t = m.text();
    if (/register|登录|注册|恭喜|失败|超时|server|On1000|GAME_START|role_num|TRY_LOGIN|InitMainUI|错误|敏感/i.test(t)) console.log('  [page]', t.slice(0, 200)); });

  const sleep = ms => page.waitForTimeout(ms);
  const loadedViews = () => page.evaluate(() => (window.__sxListLoadedPages__().views || []).map(v => v.name));
  async function waitFor(names, timeoutMs, label) {
    const dl = Date.now() + timeoutMs;
    while (Date.now() < dl) {
      const lv = await loadedViews();
      if (names.some(n => lv.includes(n))) return { ok: true, loaded: lv };
      await sleep(1500);
    }
    return { ok: false, loaded: await loadedViews() };
  }

  await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForFunction(() => !!(window.Laya && window.Laya.stage), { timeout: 45000 });
  await sleep(2000);
  await page.evaluate(SNAP + '; void 0');

  // 诊断:监听连接/视图开关事件
  await page.evaluate(() => {
    try {
      const G = window.GlobalEventSystem;
      G.Bind('GAME_CONNECT', () => console.log('EVT GAME_CONNECT (socket已连,发10000)'));
      G.Bind('GAME_START', () => console.log('EVT GAME_START (进世界)'));
      const bvName = window.EventName && window.EventName.BASEVIEW_OPEN_OR_CLOSE;
      if (bvName) G.Bind(bvName, (open, base, layer, layout) => console.log('VIEW ' + (open ? 'OPEN' : 'CLOSE') + ' ' + layout));
    } catch (e) { console.log('diag hook err ' + e); }
  });

  // 等登录 UI 起来(状态机已运行)+ 钩住 CreateRole 构造拿实例
  let st = await waitFor(['LoginView', 'RegisterView', 'LoginBgView', 'LoginEnterView'], 30000, 'login-ui');
  console.log('STAGE boot LOADED:', st.loaded.join(', ') || '(空)');
  await page.evaluate(() => {
    try {
      const Orig = window['LoginCreateRoleView'];
      if (Orig && !Orig.__hooked) {
        class Wrapped extends Orig { constructor() { super(...arguments); window.__crv__ = this; } } // ES5/ES6 兼容
        Wrapped.__hooked = true; window['LoginCreateRoleView'] = Wrapped;
      }
    } catch (e) { console.log('hook err ' + e); }
  });

  // 1) 登录账号:--acc 给定则【登录模式】(进 Login 态 + COMFIRN_LOGIN_BTN),否则【注册模式】自创号
  const argAcc = arg('acc', '');
  const argPwd = arg('pwd', argAcc);
  const loginMode = !!argAcc;
  const acc = loginMode ? argAcc : ('t' + Math.floor(Math.random() * 1000000));
  await page.evaluate((o) => {
    const lm = window.LoginManager.GetInstance(), E = window.LoginStateEvent, S = window.Enum_LoginState;
    if (o.login) { lm.Fire(E.LOGIN_STATE_CHANGE, S.Login); setTimeout(() => lm.Fire(E.COMFIRN_LOGIN_BTN, o.a, o.p), 600); }
    else { lm.Fire(E.LOGIN_STATE_CHANGE, S.Register); setTimeout(() => lm.Fire(E.COMFIRM_REGISTER_BTN, o.a, o.a), 600); }
  }, { a: acc, p: argPwd, login: loginMode });
  console.log((loginMode ? 'LOGIN' : 'REGISTER') + ' acc=' + acc + ' 等账号登录+服列表→LoginEnterView...');

  // 2) 等 LoginEnterView(= 账号登录成功 + 服列表就绪)
  st = await waitFor(['LoginEnterView'], 35000, 'enter');
  console.log('STAGE after-register LOADED:', st.loaded.join(', ') || '(空)');
  if (!st.ok) { console.log('✗ 没到 LoginEnterView(注册/账号登录失败,看 [page] 日志)'); await dump(); return end(2); }

  // 3) 选服:设 cur_server_id(从 server_data 选第一个开放服)→ Fire ServerWin 连游戏服
  const pick = await page.evaluate(() => {
    const lm = window.LoginManager.GetInstance(), E = window.LoginStateEvent, S = window.Enum_LoginState;
    let sid = lm.GetCurServerId();
    let src = 'cur';
    if (!sid || Number(sid) == 0) {
      src = 'server_data';
      const sm = window.ServerModel.GetInstance(); const sd = sm.server_data || {};
      let first = null, open = null;
      for (const area in sd) for (const id in sd[area]) {
        const s = sd[area][id]; const v = (s && s.id != null) ? s.id : id;
        if (v && Number(v) != 0) { if (!first) first = v; if (s && Number(s.closed) != 1 && !open) open = v; }
      }
      sid = open || first;
      if (sid) lm.SetCurServerId(sid);
    }
    if (sid && Number(sid) != 0) { lm.Fire(E.LOGIN_STATE_CHANGE, S.ServerWin); return { sid, src }; }
    return { err: 'no server', areas: Object.keys((window.ServerModel.GetInstance().server_data) || {}) };
  });
  console.log('SELECT-SERVER', pick);
  if (pick.err) { await dump(); return end(2); }

  // 探针:游戏服 socket 状态(诊断为何不进创角)
  await sleep(9000);
  const sock = await page.evaluate(() => {
    try {
      const ua = window.UserMsgAdapter.GetInstance();
      const s = ua.socket;
      const inner = s && (s._socket || s.socket || s._ws);
      const AC = window.AppConst || (window.LoginModel && {}) || {};
      return {
        url: ua.url_to_connected, is_game_connected: ua.is_game_connected,
        socket_connected: s && s.connected, inner_readyState: inner && inner.readyState,
        AppConst_addr: window.AppConst && window.AppConst.SocketAddress,
        AppConst_port: window.AppConst && window.AppConst.SocketPort,
        AppConst_ssl: window.AppConst && window.AppConst.SslSocketPort,
        is_http: window.HttpUtil && window.HttpUtil.is_http, proto: location.protocol,
        proto_cfg: !!(window.Config && window.Config.load_protocal_config_promise),
        recv_count: ua.curr_read_length,
      };
    } catch (e) { return { err: String(e) }; }
  });
  console.log('SOCKET-STATE', JSON.stringify(sock));

  // 4) 等 LoginCreateRoleView(游戏服连上 + 10000 角色列表,新号开创角)。期间记录 recv 字节增长。
  {
    const dl = Date.now() + 90000;
    while (Date.now() < dl) {
      const lv = await loadedViews();
      if (lv.includes('LoginCreateRoleView') || lv.includes('LoginSelectRoleView')) { st = { ok: true, loaded: lv }; break; }
      const rc = await page.evaluate(() => { try { return window.UserMsgAdapter.GetInstance().curr_read_length; } catch (e) { return -1; } });
      console.log('  waiting role... recv=' + rc + ' loaded=[' + lv.join(',') + ']');
      st = { ok: false, loaded: lv };
      await sleep(6000);
    }
  }
  console.log('STAGE after-connect LOADED:', st.loaded.join(', ') || '(空)');
  if (!st.ok) { console.log('✗ 没到创角/选角(游戏服未连上或 10000 未回,看 [page] 日志)'); await dump(); return end(2); }

  // 5) 进世界:已有角色→选第一个角色 TRY_LOGIN_GAME;新号→创角 TRY_CREATE_ROLE(成功后自动进)
  const cr = await page.evaluate((nm) => {
    const lm = window.LoginManager.GetInstance(), E = window.LoginStateEvent;
    const loaded = (window.__sxListLoadedPages__().views || []).map(v => v.name);
    if (loaded.includes('LoginSelectRoleView')) {
      // 已有角色:广撒找 role_id 再 TRY_LOGIN_GAME
      let rid = 0, from = 'none';
      try {
        const lmo = window.LoginModel.GetInstance();
        const cands = [];
        for (const k of ['account_data_10000', 'role_list', 'roles']) {
          const val = lmo[k];
          if (Array.isArray(val)) cands.push(val);
          else if (val && Array.isArray(val.role_list)) cands.push(val.role_list);
        }
        for (const arr of cands) { const r = arr && arr[0]; if (r) { rid = r.role_id || r.id || r.roleId || r.roleid || 0; if (rid) { from = 'model:' + Object.keys(r).slice(0, 8).join(','); break; } } }
      } catch (e) {}
      if (!rid) { try { rid = window.RoleManager.GetInstance().GetMainRoleId(); from = 'RoleMgr'; } catch (e) {} }
      if (rid) lm.Fire(E.TRY_LOGIN_GAME, rid);
      return { mode: 'select', rid, from };
    } else {
      const v = window.__crv__; let career = 1, sex = 1, src = 'default';
      if (v && v.ui_cfg && v.ui_cfg.length) {
        const c = v.ui_cfg[v.now_index != null ? v.now_index : 0] || v.ui_cfg[0];
        if (c) { career = c.career; sex = c.sex; src = 'ui_cfg'; }
      }
      lm.Fire(E.TRY_CREATE_ROLE, nm, career, sex);
      return { mode: 'create', career, sex, src, hasView: !!v };
    }
  }, roleName);
  console.log('ROLE-ACTION', cr);

  // 6) 等 HUD 出现(进了游戏世界)
  st = await waitFor(HUD_VIEWS, 45000, 'hud');
  console.log('STAGE after-create LOADED:', st.loaded.join(', ') || '(空)');
  const inWorld = st.ok;
  console.log(inWorld ? 'IN-WORLD ✓ 主界面 HUD 已出现' : '✗ HUD 未出现(看 [page] 日志诊断)');
  if (inWorld) await sleep(6000); // 让首批视图建全
  try { await page.screenshot({ path: path.join(outDir, 'oldclient_shot.png') }); console.log('SHOT → oldclient_shot.png'); } catch (e) { console.log('shot err ' + e); }
  await dump();
  return end(inWorld ? 0 : 2);

  async function dump() {
    const loaded = await loadedViews();
    console.log('FINAL LOADED:', loaded.join(', '));
    const snap = await page.evaluate(ns => window.__sxExportPageSnapshots__(ns), loaded);
    const stamp = new Date().toISOString().replace(/[:.]/g, '-');
    let n = 0;
    for (const v of snap.views || []) {
      const one = { version: snap.version, stage: snap.stage, views: [v] };
      fs.writeFileSync(path.join(outDir, `page_snapshot_${v.meta.name}_${stamp}.json`), JSON.stringify(one), 'utf8');
      console.log('SAVED', v.meta.name, '(' + v.nodeCount + 'n)'); n++;
    }
    console.log('DONE saved', n, 'snapshot(s) → ' + outDir);
  }
  async function end(code) { await browser.close(); process.exit(code); }
})().catch(e => { console.error('ERR:', String(e)); process.exit(1); });
