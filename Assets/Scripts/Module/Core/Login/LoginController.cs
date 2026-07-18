using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.Prefs;
using Shenxiao.Common.Proto;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Config;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Game;

namespace Shenxiao.Module.Core.Login
{
    public sealed class LoginController : BaseController
    {
        private const string PREF_ACCOUNT = "login.account";
        private const string PREF_PASSWORD = "login.password";
        private const string PREF_REMEMBER = "login.remember";
        private const string PREF_DEVICE_ID = "login.device_id";
        private const float RECONNECT_BASE_DELAY_SEC = 2f;
        private const float RECONNECT_MAX_DELAY_SEC = 15f;
        /// <summary>
        /// 链路判死窗口(秒):整条连接连续无【任何】下行数据才判死,不只看 10006。
        /// 实测本服务端会整体静默 30~60s 后自行恢复(恢复首包 rtt 8s+),这类可恢复停顿必须扛住不断线
        /// (对标老端:心跳超时只显示"无信号"图标、从不主动断开);而彻底死链(实录 48 连超时零回包)
        /// 要能自愈,所以保留一个宽松的静默 watchdog。
        /// </summary>
        private const float DEAD_LINK_SILENCE_SEC = 120f;
        /// <summary>连接建立后到进入游戏前(10000/10004 无回包)的判死窗口:此阶段服务端应秒回,给短些。</summary>
        private const float ENTERING_SILENCE_SEC = 45f;
        /// <summary>
        /// 链路整体静默期间心跳探测的最小间距(秒)。不能按 12s 超时密集补发:停顿期的心跳会在服务端
        /// 邮箱里积压,恢复瞬间被一次性处理,而 yu_server pp_login.erl handle(10006) 有反外挂检测——
        /// 连续 10 次到包间隔 &lt;4.8s 直接按外挂踢线(LOGOUT_LOG_WAIGUA)。30s 间距下 120s 停顿最多积压
        /// 4~5 个,安全。
        /// </summary>
        private const float STALL_PROBE_SPACING_SEC = 30f;
        private static readonly LoginController _instance = new LoginController();

        private AppConfig _config;
        private long _pendingEnterRoleId;
        private long _activeRoleId;
        private bool _connectingGame;
        private bool _inGameEntered;
        private int _reconnectAttempt;
        private CancellationTokenSource _heartbeatDelayCts;
        private CancellationTokenSource _heartbeatTimeoutCts;
        private CancellationTokenSource _linkWatchdogCts;
        private int _heartbeatSerial;
        private bool _heartbeatWaitingResponse;
        private bool _stallNotified;
        private DateTime _lastHeartbeatSentAt;
        private DateTime _lastHeartbeatResponseAt;
        private CancellationTokenSource _autoReconnectCts;

        public static LoginController Instance => _instance;
        public LoginModel Model => LoginModel.Instance;
        public bool CanAutoReconnectInGame => _activeRoleId > 0;

        private LoginController()
        {
        }

        public void Setup(AppConfig config)
        {
            _config = config;
            if (_config == null) return;

            GmApi.BaseUrl = _config.gmApiUrl;
            GmApi.LoginKey = _config.gmLoginKey;
            Init();
        }

        public SavedLoginInput LoadSavedInput()
        {
            bool remember = PrefsManager.GetBool(PREF_REMEMBER, true);
            string account = PrefsManager.GetString(PREF_ACCOUNT, string.Empty);
            string password = remember ? PrefsManager.GetString(PREF_PASSWORD, string.Empty) : string.Empty;

            if (string.IsNullOrEmpty(account) && _config != null)
            {
                account = _config.devAccount ?? string.Empty;
            }

            return new SavedLoginInput(account, password, remember);
        }

        public async Task<LoginRequestResult> LoginAsync(string account, string password, bool rememberPassword)
        {
            string trimmedAccount = (account ?? string.Empty).Trim();
            string trimmedPassword = (password ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmedAccount) || string.IsNullOrEmpty(trimmedPassword))
            {
                return LoginRequestResult.Fail("请输入账号密码");
            }

            JObject checkInfo = await GmApi.PlayerCheckLogin(trimmedAccount, trimmedPassword);
            if (!IsOk(checkInfo, out string checkError))
            {
                return LoginRequestResult.Fail(string.IsNullOrEmpty(checkError) ? "账号或密码错误" : checkError);
            }

            string token = LoginModel.ReadString(checkInfo, "token", string.Empty);
            LoginRequestResult result = await PlayerLoginAsync(trimmedAccount, token);
            if (result.success)
            {
                SaveAccount(trimmedAccount, trimmedPassword, rememberPassword);
            }
            return result;
        }

        public async Task<LoginRequestResult> RegisterAsync(string account, string password, bool rememberPassword)
        {
            string trimmedAccount = (account ?? string.Empty).Trim();
            string trimmedPassword = (password ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmedAccount) || string.IsNullOrEmpty(trimmedPassword))
            {
                return LoginRequestResult.Fail("请输入账号密码");
            }

            JObject registerInfo = await GmApi.PlayerRegister(trimmedAccount, trimmedPassword, GetPlatName());
            if (!IsOk(registerInfo, out string registerError))
            {
                return LoginRequestResult.Fail(string.IsNullOrEmpty(registerError) ? "注册失败" : registerError);
            }

            string token = LoginModel.ReadString(registerInfo, "token", string.Empty);
            LoginRequestResult result = await PlayerLoginAsync(trimmedAccount, token);
            if (result.success)
            {
                SaveAccount(trimmedAccount, trimmedPassword, rememberPassword);
            }
            return result;
        }

        public Task<LoginRequestResult> SelectServerAsync(LoginServerInfo server)
        {
            if (server == null || server.id <= 0)
            {
                return Task.FromResult(LoginRequestResult.Fail("请选择服务器"));
            }

            if (server.IsClosed)
            {
                string message = string.IsNullOrEmpty(server.closeDesc) ? "服务器维护中" : server.closeDesc;
                return Task.FromResult(LoginRequestResult.Fail(message));
            }

            Model.SelectServer(server);
            EventDispatcher.Emit(GlobalEvent.EVT_LOGIN_SERVER_SELECTED, server.id);
            GameLog.Info("Login", "selected server id={0}", server.id);
            return Task.FromResult(LoginRequestResult.Ok());
        }

        /// <summary>
        /// 开发/冒烟登录:跳过账号密码校验,直接走 player_login(yu_gm 侧账号不存在会自动注册)。
        /// 对应 Laya 平台 SDK 登录后的同一条路。
        /// </summary>
        public async Task<LoginRequestResult> DevLoginAsync(string account)
        {
            string trimmedAccount = (account ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmedAccount))
            {
                return LoginRequestResult.Fail("请输入账号");
            }
            return await PlayerLoginAsync(trimmedAccount, string.Empty);
        }

        /// <summary>
        /// 连接已解析好入口的游戏服并发送账号登录协议(10000)。
        /// 角色列表回包由 OnAccountLogin 处理,完成后发 EVT_GAME_ROLE_LIST。
        /// </summary>
        public async Task<LoginRequestResult> ConnectGameAsync()
        {
            LoginServerInfo server = Model.SelectedServer;
            if (server == null || string.IsNullOrEmpty(server.host) || server.port <= 0)
            {
                return LoginRequestResult.Fail("服务器入口未解析,先调 ResolveSelectedServerEndpointAsync");
            }

            string url = $"ws://{server.host}:{server.port}";
            try
            {
                _connectingGame = true;
                await NetManager.ConnectAsync(url);
                ControllerHub.InitAll();
            }
            catch (Exception e)
            {
                GameLog.Error("Login", "连接游戏服失败 {0}: {1}", url, e.Message);
                return LoginRequestResult.Fail("连接游戏服失败: " + e.Message);
            }

            finally
            {
                _connectingGame = false;
            }

            // Old client drives 10006 from LoginController: send once after 10000,
            // then schedule the next heartbeat only after receiving a 10006 reply.
            NetManager.ConfigureHeartbeat(0, 0f);
            ResetHeartbeatState();
            _inGameEntered = false;
            StartLinkWatchdog();

            // 对标 Laya GAME_CONNECT:SendFmtToGame(10000, "iiss", pid, time_stamp, account_id, plat_name)。
            // 关键:account_id = get_server_info 的 accname(游戏服按它认账号,发 player_id 会被当成
            // 另一个空账号 → 满角色的号进了创角页);time_stamp 同样优先用服务器下发的 time。
            int pid = server.pid > 0 ? server.pid : 1;
            string accountId = !string.IsNullOrEmpty(server.accname)
                ? server.accname
                : Model.PlayerId.ToString();
            long timeStamp = long.TryParse(server.time, out long svrTime) && svrTime > 0
                ? svrTime
                : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await NetManager.SendFmtAsync(Proto.ACCOUNT_LOGIN, "iiss", pid, timeStamp, accountId, Model.PlatName);
            GameLog.Info("Login", "已发送账号登录协议 pid={0} accname={1} time={2} plat={3}",
                pid, accountId, timeStamp, Model.PlatName);
            return LoginRequestResult.Ok();
        }

        public async Task<LoginRequestResult> ResolveSelectedServerEndpointAsync()
        {
            LoginServerInfo server = Model.SelectedServer;
            if (server == null || server.id <= 0)
            {
                return LoginRequestResult.Fail("请选择服务器");
            }

            JObject info = await GmApi.GetServerInfo(Model.PlayerId, server.id, Model.Token);
            if (!IsOk(info, out string error))
            {
                return LoginRequestResult.Fail(string.IsNullOrEmpty(error) ? "获取服务器入口失败" : error);
            }

            Model.ApplySelectedServerInfo(info);
            GameLog.Info("Login", "resolved server endpoint id={0} host={1} port={2}", server.id, server.host, server.port);
            return LoginRequestResult.Ok();
        }

        protected override void Register()
        {
            RegisterProtocal(Proto.ACCOUNT_LOGIN, OnAccountLogin);
            RegisterProtocal(Proto.CREATE_ROLE, OnCreateRole);
            RegisterProtocal(Proto.ENTER_GAME, OnEnterGame);
            RegisterProtocal(Proto.HEARTBEAT, OnHeartbeat);
            RegisterProtocal(Proto.NAME_VERIFY, OnNameVerify);
            RegisterProtocal(Proto.LOGIN_KICK_REASON, OnKickReason);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnNetDisconnected);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_NET_DISCONNECTED, OnNetDisconnected);
            ResetHeartbeatState();
            StopLinkWatchdog();
            CancelAutoReconnect();
            base.Dispose();
        }

        /// <summary>
        /// 10000 回包(对标 yu_client LoginController.On10000):
        /// "clihi" = career, 服务器时间(毫秒), 开服时间, 角色数, 注册数;
        /// 逐角色 "l" role_id + "c" state + FigureProto 外观块 + "c" reward_id。
        /// </summary>
        private void OnAccountLogin(NetReader reader)
        {
            object[] head = reader.ReadFmt("clihi");
            long serverTimeMs = (long)head[1];
            long openTimeSec = (uint)head[2];
            int roleCount = (ushort)head[3];

            // 裁决5(spec_serverclock_round20.md §1):补对时 + 落 open_time,对标老端 On10000
            // (LoginController.ts:275-276)只做这两件事——Time:64 是毫秒、OpenTime:32 是秒,不得混淆单位;
            // 严禁在此调 ServerTimeModel.TryFireEvent(老端同一位置也不调,10201 才是唯一驱动点)。
            // 只想改 open_time,merge_time/merge_start_time/merge_count 原样传回当前值(不建第二个局部
            // setter,避免 ServerTimeModel 出现双写入口)。
            TimeUtil.SyncServerTime(serverTimeMs);
            ServerTimeModel.ApplyServerInfo(openTimeSec, ServerTimeModel.MergeTime, ServerTimeModel.MergeStartTime, ServerTimeModel.MergeCount);

            var roles = new List<GameRoleInfo>(roleCount);
            for (int i = 0; i < roleCount; i++)
            {
                var role = new GameRoleInfo();
                role.roleId = reader.ReadU64();
                role.state = reader.ReadU8();
                role.figure = FigureProto.Read(reader);
                role.rewardId = reader.ReadU8();
                roles.Add(role);
            }
            Model.SetRoles(roles);

            GameLog.Info("Login", "★ 10000 回包: 角色数={0} 服务器时间={1}", roleCount, serverTimeMs);
            for (int i = 0; i < roles.Count; i++)
            {
                GameLog.Info("Login", "  角色[{0}] id={1} {2} 职业={3} 等级={4} {5}转",
                    i, roles[i].roleId, roles[i].DisplayName, roles[i].Career, roles[i].Level, roles[i].Turn);
            }
            SendHeartbeatNow();
            if (_pendingEnterRoleId > 0)
            {
                long roleId = _pendingEnterRoleId;
                _pendingEnterRoleId = 0;
                GameLog.Info("Login", "select-role reconnect received 10000, continue 10004 role_id={0}", roleId);
                SendEnterGamePacket(roleId);
                return;
            }

            EventDispatcher.Emit(GlobalEvent.EVT_GAME_ROLE_LIST, roleCount);
        }

        /// <summary>
        /// 创角请求(对标 TRY_CREATE_ROLE 的 10003 "cccsslsscscc")。
        /// 打点/邀请/模拟器等渠道字段按首登默认值发送。
        /// </summary>
        public void SendCreateRole(string roleName, int career, int sex)
        {
            SendFmt(Proto.CREATE_ROLE, "cccsslsscscc",
                0, career, sex, roleName, Model.PlatName,
                0L,        // inviter_id
                "",        // plat_account
                "",        // ta_distinct_id
                0,         // is_simulator
                "",        // ta_device_id
                0, 0);     // create_role_change_career / change_name
            GameLog.Info("Login", "发送创角: name={0} career={1} sex={2}", roleName, career, sex);
        }

        /// <summary>选角进入游戏(对标 TRY_LOGIN_GAME 的 10004 "lsisisscscsh")。</summary>
        public void EnterGameWithRole(long roleId, bool isNewCareer = false)
        {
            // 对标老客户端 cookie LAST_LOGIN_ROLE_ID:选角页下次默认选中它
            Shenxiao.Common.Prefs.PrefsManager.SetString(RoleSelectView.PREF_LAST_ROLE_ID, roleId.ToString());
            Model.SetNewCareer(isNewCareer);
            _activeRoleId = roleId;
            if (!NetManager.IsConnected)
            {
                _pendingEnterRoleId = roleId;
                GameLog.Warn("Login", "enter-game requested while disconnected, reconnect before 10004 role_id={0}", roleId);
                _ = ReconnectThenEnterPendingRoleAsync();
                return;
            }

            SendEnterGamePacket(roleId);
        }

        private async Task ReconnectThenEnterPendingRoleAsync()
        {
            LoginRequestResult result = await ConnectGameAsync();
            if (!result.success)
            {
                GameLog.Warn("Login", "select-role reconnect failed: {0}", result.message);
                _pendingEnterRoleId = 0;
            }
        }

        private void SendEnterGamePacket(long roleId)
        {
            long timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            SendFmt(Proto.ENTER_GAME, "lsisisscscsh",
                roleId, "开发", timeStamp, "", 1, Model.PlatName,
                "",   // plat_account
                0,    // is_whitelist
                "",   // ta_distinct_id
                0,    // is_simulator
                "",   // ta_device_id
                0);   // scene_id(重连续接用,首登 0)
            GameLog.Info("Login", "发送进入游戏: role_id={0}", roleId);
        }

        /// <summary>10003 回包 "cl":result + role_id。result==1 直接进游戏(对标 On10003)。</summary>
        private void OnCreateRole(NetReader reader)
        {
            object[] data = reader.ReadFmt("cl");
            int result = (byte)data[0];
            long roleId = (long)data[1];
            GameLog.Info("Login", "创角结果 result={0} role_id={1}", result, roleId);
            EventDispatcher.Emit(GlobalEvent.EVT_GAME_CREATE_ROLE_RESULT, result);
            if (result == 1)
            {
                EnterGameWithRole(roleId, true);
            }
        }

        /// <summary>10004 回包 "c":result==1 进入游戏成功(对标 On10004,后续 GAME_START 主城接管)。</summary>
        private void OnEnterGame(NetReader reader)
        {
            int result = reader.ReadU8();
            if (result == 1)
            {
                _inGameEntered = true;
                bool wasReconnect = _reconnectAttempt > 0;
                _reconnectAttempt = 0;
                if (wasReconnect) TipsManager.Toast("重连成功");
                GameLog.Info("Login", "🎉 进入游戏成功(10004),主城/场景流程待接");
                EventDispatcher.Emit(GlobalEvent.EVT_GAME_ENTERED);
            }
            else
            {
                GameLog.Warn("Login", "进入游戏失败 result={0}", result);
                TipsManager.Toast("进入游戏失败(" + result + ")");   // 错误码表(Util.ErrorCodeShow)未移植,显码降级
            }
        }

        private void OnHeartbeat(NetReader reader)
        {
            _heartbeatWaitingResponse = false;
            if (_stallNotified)
            {
                _stallNotified = false;
                TipsManager.Toast("网络已恢复");
            }
            _lastHeartbeatResponseAt = DateTime.UtcNow;
            CancelHeartbeatTimeout();
            ScheduleNextHeartbeat();
        }

        private void OnKickReason(NetReader reader)
        {
            int code = reader.Remaining >= 2 ? reader.ReadU16() : -1;
            GameLog.Warn("Login", "59004 server kick reason code={0}", code);
            TipsManager.Toast("已被服务器断开(" + code + ")");   // 踢线原因文案表未移植,显码降级
        }

        private void SendHeartbeatNow()
        {
            CancelHeartbeatDelay();
            CancelHeartbeatTimeout();
            if (!NetManager.IsConnected)
            {
                _heartbeatWaitingResponse = false;
                return;
            }

            int serial = ++_heartbeatSerial;
            _heartbeatWaitingResponse = true;
            _lastHeartbeatSentAt = DateTime.UtcNow;
            SendFmt(Proto.HEARTBEAT, "");
            ScheduleHeartbeatTimeout(serial);
            GameLog.Debug("Login", "10006 heartbeat sent serial={0}", serial);
        }

        private void ScheduleNextHeartbeat()
        {
            CancelHeartbeatDelay();
            if (_config != null && _config.heartbeatIntervalSec <= 0f) return;
            float delaySec = GetHeartbeatIntervalSec();
            _heartbeatDelayCts = new CancellationTokenSource();
            CancellationToken token = _heartbeatDelayCts.Token;
            _ = SendHeartbeatAfterDelayAsync(delaySec, token);
            double rttMs = _lastHeartbeatSentAt == default(DateTime)
                ? -1
                : (_lastHeartbeatResponseAt - _lastHeartbeatSentAt).TotalMilliseconds;
            GameLog.Debug("Login", "10006 heartbeat response rtt={0:0}ms, next in {1:0.###}s", rttMs, delaySec);
        }

        private async Task SendHeartbeatAfterDelayAsync(float delaySec, CancellationToken token)
        {
            try
            {
                await Shenxiao.Framework.Util.TimeUtil.Delay((int)(delaySec * 1000f), token); // Task.Delay 在 WebGL 永不醒:曾致心跳只发首跳→服务端超时踢线
                if (!token.IsCancellationRequested) SendHeartbeatNow();
            }
            catch (OperationCanceledException)
            {
                // Replaced by a newer heartbeat schedule or connection lifecycle reset.
            }
        }

        private void ScheduleHeartbeatTimeout(int serial)
        {
            CancelHeartbeatTimeout();
            float timeoutSec = Math.Max(GetHeartbeatIntervalSec() * 2f, 10f);
            _heartbeatTimeoutCts = new CancellationTokenSource();
            _ = WatchHeartbeatTimeoutAsync(serial, timeoutSec, _heartbeatTimeoutCts);
        }

        private async Task WatchHeartbeatTimeoutAsync(int serial, float timeoutSec, CancellationTokenSource cts)
        {
            bool timeout = false;
            try
            {
                await Shenxiao.Framework.Util.TimeUtil.Delay((int)(timeoutSec * 1000f), cts.Token);
                timeout = !cts.IsCancellationRequested;
            }
            catch (OperationCanceledException)
            {
                // Response arrived or connection lifecycle reset.
            }
            finally
            {
                if (_heartbeatTimeoutCts == cts)
                {
                    _heartbeatTimeoutCts = null;
                    cts.Dispose();
                }
            }

            if (!timeout) return;
            if (serial != _heartbeatSerial || !_heartbeatWaitingResponse || !NetManager.IsConnected) return;

            double sinceMs = (DateTime.UtcNow - _lastHeartbeatSentAt).TotalMilliseconds;
            float silenceSec = NetManager.SecondsSinceLastInbound;

            // 超时分两种,处置完全不同:
            // ① 链路有下行、只是 10006 回包丢/慢 → 立即补发一次(12s 间距,不触发服务端反外挂)。
            // ② 整条链路静默(服务端停顿/网络抖动)→ 不密集补发,改 30s 间距稀疏探测;
            //    判死不在这里做——交给链路静默 watchdog(RunLinkWatchdogAsync,看任意下行、窗口 120s),
            //    可恢复停顿(实测 30~60s)扛住不断线,对标老端"心跳超时只亮无信号图标、从不主动断"。
            if (silenceSec < timeoutSec)
            {
                GameLog.Warn("Login", "10006 heartbeat timeout serial={0} elapsed={1:0}ms,但链路 {2:0.#}s 内有下行(回包丢/慢)→ 立即补发",
                    serial, sinceMs, silenceSec);
                SendHeartbeatNow();
                return;
            }

            if (!_stallNotified)
            {
                _stallNotified = true;
                if (_inGameEntered) TipsManager.Toast("网络波动,等待服务器响应…");
            }
            GameLog.Warn("Login", "10006 heartbeat timeout serial={0} elapsed={1:0}ms,链路已整体静默 {2:0.#}s → {3:0}s 后稀疏探测(判死由静默 watchdog 负责,窗口 {4:0}s)",
                serial, sinceMs, silenceSec, STALL_PROBE_SPACING_SEC, _inGameEntered ? DEAD_LINK_SILENCE_SEC : ENTERING_SILENCE_SEC);
            _heartbeatWaitingResponse = false;
            ScheduleProbeAfter(STALL_PROBE_SPACING_SEC);
        }

        /// <summary>链路静默期的稀疏心跳探测(间距见 <see cref="STALL_PROBE_SPACING_SEC"/>)。</summary>
        private void ScheduleProbeAfter(float delaySec)
        {
            CancelHeartbeatDelay();
            _heartbeatDelayCts = new CancellationTokenSource();
            _ = SendHeartbeatAfterDelayAsync(delaySec, _heartbeatDelayCts.Token);
        }

        private float GetHeartbeatIntervalSec()
        {
            if (_config == null || _config.heartbeatIntervalSec <= 0f) return 5f;
            return _config.heartbeatIntervalSec;
        }

        private void CancelHeartbeatDelay()
        {
            if (_heartbeatDelayCts == null) return;
            _heartbeatDelayCts.Cancel();
            _heartbeatDelayCts.Dispose();
            _heartbeatDelayCts = null;
        }

        private void CancelHeartbeatTimeout()
        {
            if (_heartbeatTimeoutCts == null) return;
            CancellationTokenSource cts = _heartbeatTimeoutCts;
            _heartbeatTimeoutCts = null;
            cts.Cancel();
            cts.Dispose();
        }

        private void ResetHeartbeatState()
        {
            CancelHeartbeatDelay();
            CancelHeartbeatTimeout();
            _heartbeatWaitingResponse = false;
            _stallNotified = false;
            _lastHeartbeatSentAt = default(DateTime);
            _lastHeartbeatResponseAt = default(DateTime);
        }

        /// <summary>
        /// 链路静默 watchdog:唯一的"判死链路"出口。看的是整条连接最近一次任意下行(NetManager
        /// 收包线程记录),进游戏后窗口 <see cref="DEAD_LINK_SILENCE_SEC"/>,进游戏前(等 10000/10004
        /// 回包)窗口 <see cref="ENTERING_SILENCE_SEC"/>——重连到仍在停顿的服务端时,10000 可能永远
        /// 不回,没有这个短窗口整条重连链会僵死。
        /// </summary>
        private void StartLinkWatchdog()
        {
            StopLinkWatchdog();
            _linkWatchdogCts = new CancellationTokenSource();
            _ = RunLinkWatchdogAsync(_linkWatchdogCts);
        }

        private async Task RunLinkWatchdogAsync(CancellationTokenSource cts)
        {
            try
            {
                while (!cts.Token.IsCancellationRequested && NetManager.IsConnected)
                {
                    await Shenxiao.Framework.Util.TimeUtil.Delay(5000, cts.Token); // 判死 watchdog 也不能用 Task.Delay(WebGL 永不醒)
                    if (cts.Token.IsCancellationRequested || !NetManager.IsConnected) break;

                    float silenceSec = NetManager.SecondsSinceLastInbound;
                    float limitSec = _inGameEntered ? DEAD_LINK_SILENCE_SEC : ENTERING_SILENCE_SEC;
                    if (silenceSec < limitSec) continue;

                    GameLog.Warn("Login", "链路连续 {0:0}s 无任何下行(≥{1:0}s,阶段={2})→ 判定链路已死,主动断开走自动重连",
                        silenceSec, limitSec, _inGameEntered ? "in-game" : "entering");
                    if (_activeRoleId > 0) TipsManager.Toast("连接中断,正在自动重连…");
                    _ = NetManager.DisconnectAsync();
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                // Connection lifecycle reset.
            }
            finally
            {
                if (_linkWatchdogCts == cts)
                {
                    _linkWatchdogCts = null;
                    cts.Dispose();
                }
            }
        }

        private void StopLinkWatchdog()
        {
            if (_linkWatchdogCts == null) return;
            // 同 CancelAutoReconnect:先置 null 再 Cancel,避免 Cancel 同步唤起 finally 后二次 Dispose。
            CancellationTokenSource cts = _linkWatchdogCts;
            _linkWatchdogCts = null;
            cts.Cancel();
            cts.Dispose();
        }

        private void OnNetDisconnected()
        {
            ResetHeartbeatState();
            StopLinkWatchdog();
            _inGameEntered = false;
            if (_connectingGame || _activeRoleId <= 0)
            {
                return;
            }

            ScheduleInGameAutoReconnect();
        }

        /// <summary>玩家主动登出/回登录页时清空游戏内重连状态,停掉重连与链路 watchdog。</summary>
        public void ClearInGameReconnectState()
        {
            _activeRoleId = 0;
            _pendingEnterRoleId = 0;
            _reconnectAttempt = 0;
            _inGameEntered = false;
            CancelAutoReconnect();
            StopLinkWatchdog();
        }

        /// <summary>
        /// 游戏内自动重连:不限次数、指数退避(2s→4s→…→15s 封顶),进入游戏成功(10004)才清零计数。
        /// 此前只给 2 次额度且重连成功不补——跨多次网络停顿的长会话必然耗尽额度,之后再断线连尝试都
        /// 不尝试,玩家看到的就是"突然断线后永远愣住"。服务端停顿期间重连本身也会失败(消耗额度),
        /// 所以额度制在这个服务器环境下不成立,改为持续退避重试直到成功或玩家主动退出。
        /// </summary>
        private void ScheduleInGameAutoReconnect()
        {
            if (_autoReconnectCts != null || _activeRoleId <= 0) return;
            long roleId = _activeRoleId;
            _reconnectAttempt++;
            float delaySec = Math.Min(
                RECONNECT_BASE_DELAY_SEC * (float)Math.Pow(2, _reconnectAttempt - 1),
                RECONNECT_MAX_DELAY_SEC);
            _autoReconnectCts = new CancellationTokenSource();
            GameLog.Warn("Login", "game socket disconnected, auto reconnect #{0} in {1:0.#}s role_id={2}",
                _reconnectAttempt, delaySec, roleId);
            _ = ReconnectInGameAfterDelayAsync(roleId, delaySec, _autoReconnectCts);
        }

        private async Task ReconnectInGameAfterDelayAsync(long roleId, float delaySec, CancellationTokenSource cts)
        {
            try
            {
                await Shenxiao.Framework.Util.TimeUtil.Delay((int)(delaySec * 1000f), cts.Token);
                if (cts.Token.IsCancellationRequested || roleId <= 0 || roleId != _activeRoleId) return;

                if (_autoReconnectCts == cts)
                {
                    _autoReconnectCts = null;
                }
                cts.Dispose();

                _pendingEnterRoleId = roleId;
                LoginRequestResult result = await ConnectGameAsync();
                if (!result.success)
                {
                    GameLog.Warn("Login", "in-game auto reconnect #{0} failed: {1}", _reconnectAttempt, result.message);
                    ScheduleInGameAutoReconnect();
                }
                // 连接成功但服务端仍停顿(10000/10004 不回)的情况由 entering 阶段静默 watchdog 兜底:
                // 45s 无下行 → 断开 → OnNetDisconnected → 下一轮重连。
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (_autoReconnectCts == cts)
                {
                    _autoReconnectCts = null;
                    cts.Dispose();
                }
            }
        }

        private void CancelAutoReconnect()
        {
            if (_autoReconnectCts == null) return;
            // 先取局部 + 先置 null,再 Cancel/Dispose:Cancel() 会同步唤起 ReconnectInGameAfterDelayAsync 的 finally
            // (`if (_autoReconnectCts == cts) { _autoReconnectCts = null; cts.Dispose(); }`),若不先置 null,
            // 字段会在 Cancel 内被清空,回到这里再 Dispose 就对 null 触发 NRE(与 AutoFightController.StopLoop 同类 bug)。
            CancellationTokenSource cts = _autoReconnectCts;
            _autoReconnectCts = null;
            cts.Cancel();
            cts.Dispose();
        }

        private void OnNameVerify(NetReader reader)
        {
            int code = reader.ReadU8();
            string msg = code switch
            {
                1 => "验证成功",
                2 => "验证失败",
                4 => "角色名称已被使用",
                5 => "含非法字符",
                6 => "名称长度需 1~5",
                _ => "未知结果 " + code,
            };
            GameLog.Info("Login", "角色名验证(10007): {0}", msg);
            if (code != 1) TipsManager.Toast(msg);   // 失败码给玩家提示(文案对标老端)
        }

        private async Task<LoginRequestResult> PlayerLoginAsync(string account, string token)
        {
            string deviceId = GetOrCreateDeviceId();
            string platName = GetPlatName();
            Model.ResetSession(account, platName, token);

            JObject loginInfo = await GmApi.PlayerLogin(account, platName, deviceId, token);
            if (!IsOk(loginInfo, out string loginError))
            {
                return LoginRequestResult.Fail(string.IsNullOrEmpty(loginError) ? "登录失败" : loginError);
            }

            Model.ApplyPlayerLoginInfo(loginInfo);
            if (Model.Servers.Count == 0)
            {
                JObject serverListInfo = await GmApi.GetServerList();
                if (!IsOk(serverListInfo, out string serverListError))
                {
                    return LoginRequestResult.Fail(string.IsNullOrEmpty(serverListError) ? "获取服务器列表失败" : serverListError);
                }
                Model.ApplyServerListInfo(serverListInfo);
            }

            if (Model.Servers.Count == 0)
            {
                return LoginRequestResult.Fail("没有可用服务器");
            }

            EventDispatcher.Emit(GlobalEvent.EVT_LOGIN_SUCCESS);
            return LoginRequestResult.Ok();
        }

        private string GetPlatName()
        {
            if (_config == null || string.IsNullOrEmpty(_config.platName)) return "unity";
            return _config.platName;
        }

        private static string GetOrCreateDeviceId()
        {
            string deviceId = PrefsManager.GetString(PREF_DEVICE_ID, string.Empty);
            if (!string.IsNullOrEmpty(deviceId)) return deviceId;

            deviceId = Guid.NewGuid().ToString("N");
            PrefsManager.SetString(PREF_DEVICE_ID, deviceId);
            return deviceId;
        }

        private static void SaveAccount(string account, string password, bool rememberPassword)
        {
            PrefsManager.SetString(PREF_ACCOUNT, account);
            PrefsManager.SetBool(PREF_REMEMBER, rememberPassword);
            if (rememberPassword) PrefsManager.SetString(PREF_PASSWORD, password);
            else PrefsManager.Remove(PREF_PASSWORD);
        }

        private static bool IsOk(JObject info, out string message)
        {
            message = string.Empty;
            if (info == null)
            {
                message = "网络请求失败";
                return false;
            }

            int ret = LoginModel.ReadInt(info, "ret", -1);
            message = LoginModel.ReadString(info, "msg", string.Empty);
            return ret == 0;
        }
    }

    public readonly struct SavedLoginInput
    {
        public readonly string account;
        public readonly string password;
        public readonly bool remember;

        public SavedLoginInput(string account, string password, bool remember)
        {
            this.account = account;
            this.password = password;
            this.remember = remember;
        }
    }

    public readonly struct LoginRequestResult
    {
        public readonly bool success;
        public readonly string message;

        private LoginRequestResult(bool success, string message)
        {
            this.success = success;
            this.message = message;
        }

        public static LoginRequestResult Ok()
        {
            return new LoginRequestResult(true, string.Empty);
        }

        public static LoginRequestResult Fail(string message)
        {
            return new LoginRequestResult(false, message);
        }
    }
}
