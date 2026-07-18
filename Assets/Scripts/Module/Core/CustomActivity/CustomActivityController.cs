using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Boss;
using Shenxiao.Module.Core.FirstRecharge;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.CustomActivity
{
    /// <summary>
    /// 定制活动图标控制器(对标老端 commonController/CustomActivityController + commonModel/CustomActivityModel 的图标部分)。
    /// 只做主界面图标显隐(超值礼包/开服活动/每日直购/节日投资/红包返利/0元礼包/每日累充… 的 331@base@show 家族),
    /// 不移植任何面板。
    ///
    /// 【根因修复】老端 On33101 把列表缓存进 cache_scmd_33101_list,并在 CHANGE_LEVEL(去抖)/任务变化时用缓存
    /// 复评 UpdateActivityIcons。此前 Unity 端把列表用完即弃、只在收包那一刻按当时的等级/开服天数 add 一次;而
    /// 33101 与 13001(角色)/10201(开服时间)同批请求、回包顺序不定,收 33101 时等级常还是 0——绝大多数活动图标
    /// 受 open_lv 门禁(实测 configfunctionicon:满级 155/225 可显,等级 0 仅 14/225),被门掉后永不复评 → 整个
    /// 家族"看不到"。这里补回缓存 + 在 EVT_ROLE_INFO_UPDATE(等级真变)/EVT_TASK_LIST_UPDATED 时复评。
    ///
    /// 【专属通道】节日投资(62)与红包返利(117)老端不走通用 33101 图标路径(AddActivityIcons 里对
    /// 331@62@1 / 331@117@0 显式 continue),而是见到活动就请求 33211/33255,由回包按 etime/is_quality 直加/删图标。
    /// 本控制器同样:On33101 见到 62/117 即请求 33211/33255,并在通用路径跳过这两个图标,交给 On33211/On33255。
    /// </summary>
    public sealed partial class CustomActivityController : BaseController
    {
        public static readonly CustomActivityController Instance = new CustomActivityController();

        // 节日投资(FTVINVEST):base_type=62,图标 331@62@1,详情包 33211(对标老端 On33211)。
        private const int FTVINVEST_BASE_TYPE = 62;
        private const string FTVINVEST_ICON = "331@62@1";
        // 红包返利(RED_ENVELOPE_REBATE):base_type=117,图标 331@117@0,详情包 33255(对标老端 On33255)。
        private const int RED_ENVELOPE_REBATE_BASE_TYPE = 117;
        private const string RED_ENVELOPE_REBATE_ICON = "331@117@0";
        // 节日大妖(FEASTBOSS):base_type=51,图标 51,不走通用/详情包,由 BossController 按 condition 每日时间窗驱动
        // (对标老端 CustomActivityModel base_type==FEASTBOSS → BossModel.FeastBossActivity)。
        private const int FEAST_BOSS_BASE_TYPE = 51;
        // 累充有礼(TIRED_CHARGE_POLITE):base_type=121,On33101 扫描到即追发 33259(镜像老端 On33101:950-952
        // 双追发之二,自动循环 轮17 P1 新增;另一路 117→33255 追发已在 RequestDirectBranchDetails 里)。
        private const int TIRED_CHARGE_POLITE_BASE_TYPE = 121;

        private readonly HashSet<string> _ownedIcons = new HashSet<string>();
        // 缓存 33101 列表(对标老端 cache_scmd_33101_list),供等级/任务变化时复评。
        private readonly List<ActInfo> _cachedList = new List<ActInfo>();
        private int _applyVersion;
        // 复评去抖:EVT_ROLE_INFO_UPDATE 随经验/货币也会触发,只在等级真变时复评(对标老端 CHANGE_LEVEL)。
        private int _lastLevel = -1;

        private CustomActivityController() { }

        public struct ActInfo
        {
            public int BaseType;
            public int SubType;
            public int ActType;
            public int ShowId;
            public int Wlv;
            public string Name;
            public string Desc;
            public string Condition;
            public int StartTime;
            public int EndTime;
        }

        protected override void Register()
        {
            RegisterProtocal(Proto.CUSTOM_ACTIVITY_LIST, On33101);
            RegisterProtocal(Proto.CUSTOM_ACTIVITY_FTVINVEST, On33211);
            RegisterProtocal(Proto.CUSTOM_ACTIVITY_RED_ENVELOPE_REBATE, On33255);

            // 自动循环 轮17:框架核心(P1)注册在本方法直接调用;P2-P6 各自的注册收在自己 partial 文件的
            // RegisterXxx() 里(本轮 P1 预建空壳,调用点在此已就位),包代理落地时只需在自己文件里把方法体
            // 填上 RegisterProtocal 调用,不需要再回来改这个共享文件(对标 spec §0 "P2-P6 零共享文件改动")。
            RegisterCore();
            RegisterLotteryA();
            RegisterLotteryB();
            RegisterFestival();
            RegisterBiz();
            RegisterKf();
            // 轮21 PF 补漏批新增:LIST_DUOBAO=116 夺宝积分墙(33252/33253/33254),见 CustomActivityController.List.cs。
            RegisterList();

            // 等级/任务变化用缓存列表复评图标(对标老端 CHANGE_LEVEL→UpdateActivityIcons(cache) 与 taskChange)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, OnTaskListUpdated);

            // ServerClock(轮20 P4)补 DAY_CHANGE/HOUR_REFRESH 两个复拉钩子(对标老端
            // CustomActivityController.ts:207/225,老端函数体见 OnServerDayChange/OnServerHourRefresh 注释)。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            EventDispatcher.On<int>(GlobalEvent.EVT_SERVER_HOUR_REFRESH, OnServerHourRefresh);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, OnTaskListUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            EventDispatcher.Off<int>(GlobalEvent.EVT_SERVER_HOUR_REFRESH, OnServerHourRefresh);
            ClearOwnedIcons();
            _cachedList.Clear();
            _lastLevel = -1;
            CustomActivityModel.Instance.Clear();
            base.Dispose();
        }

        public void RequestActivityList()
        {
            SendFmt(Proto.CUSTOM_ACTIVITY_LIST);
        }

        private void On33101(NetReader r)
        {
            int count = r.ReadU16();
            _cachedList.Clear();
            var modelList = new List<CustomActivityModel.ActEntry>(count);
            for (int i = 0; i < count; i++)
            {
                var info = new ActInfo
                {
                    BaseType = r.ReadU16(),
                    SubType = r.ReadU16(),
                    ActType = r.ReadU8(),
                    ShowId = r.ReadU16(),
                    Wlv = r.ReadU16(),
                    Name = r.ReadString(),
                    Desc = r.ReadString(),
                    Condition = r.ReadString(),
                    StartTime = (int)r.ReadU32(),
                    EndTime = (int)r.ReadU32(),
                };
                _cachedList.Add(info);
                // 复用同一份解析结果落 Model,不重新读 NetReader(自动循环 轮17 P1 新增,字段与 ActEntry 完全一致)。
                modelList.Add(new CustomActivityModel.ActEntry
                {
                    BaseType = info.BaseType, SubType = info.SubType, ActType = info.ActType, ShowId = info.ShowId,
                    Wlv = info.Wlv, Name = info.Name, Desc = info.Desc, Condition = info.Condition,
                    Stime = info.StartTime, Etime = info.EndTime,
                });
            }

            // 对标老端 On33101 的 SaveActInfo(t:947)——轮17 P1 新增,清空重建整份列表 + 通知。
            CustomActivityModel.Instance.SaveActList(modelList);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_LIST_UPDATE);

            // 专属通道活动:见到即请求详情包,由 On33211/On33255/On33259(P5)决定图标/面板显隐
            // (镜像老端 On33101 两路追发:117→33255 在收包时立即追发【937行】,121→33259 在 SaveActInfo 之后追发【950-952行】;
            // 本端合并到一次遍历,时序上的先后差异不影响两者互不依赖的结果)。
            RequestDirectBranchDetails();
            // "见到即拉"特判(自动循环 轮17三镜头验收补,镜像老端 SaveActInfo switch-case,Model.ts:378-385):
            // MONEYTREE_SHOP(89)/LEVEL_WINDOW_REWARD(108)/DAILY_LOGIN(113)/ATLISTPURCHASE(114)/ATTENTION(70)。
            RequestSeeOnArrivalDetailsFull(modelList);
            ReapplyGenericIcons();
            EvaluateFeastBoss(); // 节日大妖(51)按时间窗驱动图标。
            GameLog.Info("CustomActivity", "33101 activity list: {0}", count);
        }

        // 节日投资(33211,→331@62@1):base_type:h, sub_type:h, investments[u16×{lv:c}], buy_time:i(对标 pt_332 write 33211)。
        private void On33211(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            int investCount = r.ReadU16();
            // 自动循环 轮17 P5 升级:此前只读游标丢弃,现落 Model(CustomActivityModel.Biz.cs FtvInvestInfo,
            // item_to_bin_5=单字段 Lv:8,pt_332.erl:1737-1743)供 P5 面板层消费,不影响下方既有图标/时间窗逻辑。
            var investments = new List<int>(investCount);
            for (int i = 0; i < investCount; i++) investments.Add(r.ReadU8()); // 每档投资等级(面板用,本期只做图标)
            int buyTime = (int)r.ReadU32();
            CustomActivityModel.Instance.SetFtvInvestInfo(new CustomActivityModel.FtvInvestInfo
            {
                BaseType = baseType, SubType = subType, BuyTime = buyTime,
            });
            CustomActivityModel.Instance.GetFtvInvestInfo(baseType, subType).Investments.AddRange(investments);

            // etime:有投资记录看活动结束时间,否则看购买截止时间(对标老端 On33211)。
            int etime = investCount > 0 && TryGetCachedEndTime(baseType, subType, out int actEnd) ? actEnd : buyTime;
            if (etime > TimeUtil.NowSec()) _ = ActivityIconManager.Instance.AddIconAsync(FTVINVEST_ICON, etime);
            else ActivityIconManager.Instance.DeleteIcon(FTVINVEST_ICON);

            // 追发镜像(ts:1841,自动循环 轮17三镜头验收补):老端在 buy_day/can_buy 判定与图标增删之后,函数末尾
            // 无条件追发 33104(b,s) 重拉通用详情(仅当 !info/!cond 提前 return,或"未达标且零投资"分支删除活动
            // 条目后 splice 命中提前 return 时才跳过——这两条早退路径涉及老端 cond/buy_day/act_key_dic 等
            // 本端未镜像的 UI 侧字段,本轮不复刻;仅镜像"函数正常跑到底就发 33104"这一条无条件追发)。
            SendFmt(Proto.CUSTOM_ACT_DETAIL, "hh", baseType, subType);

            GameLog.Info("CustomActivity", "33211 节日投资: base={0} sub={1} invest={2} etime={3}",
                baseType, subType, investCount, etime);
        }

        // 红包返利(33255,→331@117@0):type:h, subtype:h, is_quality:c, start_time:i, end_time:i, login_money:h,
        // recharge_money:h, login_status:c, recharge_status:c, login_withdrawal:c, recharge_withdrawal:c,
        // login_global_times:i, recharge_global_times:i(对标 pt_332 write 33255)。
        private void On33255(NetReader r)
        {
            int type = r.ReadU16();         // type
            int subtype = r.ReadU16();      // subtype
            int isQuality = r.ReadU8();     // 是否达标开启
            int startTime = (int)r.ReadU32(); // start_time
            int endTime = (int)r.ReadU32(); // end_time
            int loginMoney = r.ReadU16();      // login_money
            int rechargeMoney = r.ReadU16();   // recharge_money
            int loginStatus = r.ReadU8();      // login_status
            int rechargeStatus = r.ReadU8();   // recharge_status
            int loginWithdrawal = r.ReadU8();  // login_withdrawal
            int rechargeWithdrawal = r.ReadU8(); // recharge_withdrawal
            int loginGlobalTimes = (int)r.ReadU32(); // login_global_times
            int rechargeGlobalTimes = (int)r.ReadU32(); // recharge_global_times

            // 自动循环 轮17 P5 升级:此前 10 个字段读了即丢,现 12 字段全落 Model
            // (CustomActivityModel.Biz.cs RedEnvelopeRebateInfo,对标 pt_332.erl write(33255):1413-1443),
            // 不影响下方既有图标/时间窗逻辑。
            CustomActivityModel.Instance.SetRedEnvelopeRebateInfo(new CustomActivityModel.RedEnvelopeRebateInfo
            {
                Type = type, Subtype = subtype, IsQuality = isQuality, StartTime = startTime, EndTime = endTime,
                LoginMoney = loginMoney, RechargeMoney = rechargeMoney, LoginStatus = loginStatus, RechargeStatus = rechargeStatus,
                LoginWithdrawal = loginWithdrawal, RechargeWithdrawal = rechargeWithdrawal,
                LoginGlobalTimes = loginGlobalTimes, RechargeGlobalTimes = rechargeGlobalTimes,
            });

            // 对标老端 On33255:is_quality 且未过期即显示。老端还叠加渠道白名单(ClientRedBagOpen.channels)+
            // 非微信小游戏链的门禁——本端尚无对应平台基建,语义同 source_list 空=放行,先按"活跃即显示",
            // 接入平台后再补渠道过滤。
            if (isQuality != 0 && endTime > TimeUtil.NowSec())
                _ = ActivityIconManager.Instance.AddIconAsync(RED_ENVELOPE_REBATE_ICON, endTime);
            else
                ActivityIconManager.Instance.DeleteIcon(RED_ENVELOPE_REBATE_ICON);

            GameLog.Info("CustomActivity", "33255 红包返利: is_quality={0} end={1}", isQuality, endTime);
        }

        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            if (_cachedList.Count == 0) return;

            // 等级变化:重请求专属通道详情(对标老端 CHANGE_LEVEL 复跑 RequireActInfo)+ 通用图标复评。
            RequestDirectBranchDetails();
            ReapplyGenericIcons();
            EvaluateFeastBoss(); // 顺带按当前时间窗复评节日大妖(51),缓解无每秒定时器时的边界滞后。
        }

        private void OnTaskListUpdated()
        {
            if (_cachedList.Count == 0) return;
            ReapplyGenericIcons(); // 任务变化影响 open_task_id 门禁(对标老端 taskChange)。
        }

        /// <summary>跨天(对标老端 CustomActivityController.ts:207 绑定→change_day_func:179-183):
        /// ① show_super_gift_view=true + CookieWrapper.RemoveCookie(SUPER_GIFT_CHECK)——纯 UI 侧状态
        /// (驱动超值礼包弹窗首次自动开启),本仓无 CookieWrapper/SuperGiftView 落地,数据层轮不镜像
        /// (同 On33104 base_type==120 段"is_have_receive/product_id/130级弹窗等纯UI侧逻辑不镜像"先例,
        /// 见 CustomActivityController.Core.cs On33104 注释);
        /// ② activenss_time_limit()(ts:163-178):除 UI 倒计时字段 fvt_activeness_time/SetActRechargeDay(0)
        /// (同样纯 UI 状态,本仓无对应字段,不镜像)外,唯一网络副作用是无条件 Fire(SCMD_REQUEST,15959)——
        /// 本端对应 <see cref="RequestTodayRecharge"/>(15959 当天充值金额,On15959 内追发 CON_RECHARGE
        /// 详情,见 CustomActivityController.Biz.cs)。</summary>
        private void OnServerDayChange()
        {
            RequestTodayRecharge();
        }

        /// <summary>整点(对标老端 CustomActivityController.ts:225 绑定→匿名函数 209-224):hour==4 时遍历
        /// cache_scmd_33101_list(本端 _cachedList),三重过滤全过才发 33193——
        /// ① base_type==ACT_ID.FTVACTIVENESS(56,ConfigCustomActivity.json ACT_ID.FTVACTIVENESS 实测值,
        /// 本文件 ACT_ID_FTVACTIVENESS 常量,见 Core.cs);
        /// ② show_id!=10;
        /// ③ GetActData(base_type,sub_type) 非空——老端 GetActData 读的是 SetActData 落的缓存,SetActData
        /// 由众多 331 号收包处理器统一写入(含通用详情 33104),本端对应容器是
        /// <see cref="CustomActivityModel.GetDetail"/>(On33104 落地;On33196 追发 33193 时已用同一等价物,
        /// 见 CustomActivityController.Festival.cs On33196 注释)。三者全过 → 调
        /// <see cref="RequestFtvActivePanel"/>(33193,base_type,sub_type)。</summary>
        private void OnServerHourRefresh(int hour)
        {
            if (hour != 4) return;
            for (int i = 0; i < _cachedList.Count; i++)
            {
                ActInfo info = _cachedList[i];
                if (info.BaseType != ACT_ID_FTVACTIVENESS) continue;
                if (info.ShowId == 10) continue;
                if (CustomActivityModel.Instance.GetDetail(info.BaseType, info.SubType) == null) continue;
                RequestFtvActivePanel(info.BaseType, info.SubType);
            }
        }

        // 通用 33101 图标复评:用缓存快照重算应显图标集,增新去旧(专属通道图标 331@62@1/331@117@0 不在此列)。
        private void ReapplyGenericIcons()
        {
            _ = ApplyActivityIconsAsync(new List<ActInfo>(_cachedList), ++_applyVersion);
        }

        // 节日大妖(51):缓存列表里若有 FEASTBOSS 活动,交给 BossController 按其 condition 时间窗算图标;没有则清。
        // 对标老端 CustomActivityModel 收 33101 时对 base_type==FEASTBOSS → BossModel.FeastBossActivity(base,sub)。
        private void EvaluateFeastBoss()
        {
            for (int i = 0; i < _cachedList.Count; i++)
            {
                if (_cachedList[i].BaseType == FEAST_BOSS_BASE_TYPE)
                {
                    ActInfo a = _cachedList[i];
                    BossController.Instance.EvaluateFeastBoss(true, a.Condition, a.StartTime, a.EndTime);
                    return;
                }
            }
            BossController.Instance.EvaluateFeastBoss(false, null, 0, 0);
        }

        private void RequestDirectBranchDetails()
        {
            for (int i = 0; i < _cachedList.Count; i++)
            {
                ActInfo info = _cachedList[i];
                if (info.BaseType == FTVINVEST_BASE_TYPE)
                    SendFmt(Proto.CUSTOM_ACTIVITY_FTVINVEST, "hh", info.BaseType, info.SubType);
                else if (info.BaseType == RED_ENVELOPE_REBATE_BASE_TYPE)
                    SendFmt(Proto.CUSTOM_ACTIVITY_RED_ENVELOPE_REBATE, "hh", info.BaseType, info.SubType);
                else if (info.BaseType == TIRED_CHARGE_POLITE_BASE_TYPE)
                    // 自动循环 轮17 P1 新增:镜像老端 On33101 对 TiredChargePoliteModel.ACT_TYPE 的追发(ts:950-952)。
                    // 面板消费(P5)读 CustomActivityModel 的 detail 段(On33259 待 P5 落地)。
                    SendFmt(Proto.CUSTOM_ACT_TIRED_CHARGE_POLITE, "hh", info.BaseType, info.SubType);
            }
        }

        private bool TryGetCachedEndTime(int baseType, int subType, out int endTime)
        {
            for (int i = 0; i < _cachedList.Count; i++)
            {
                if (_cachedList[i].BaseType == baseType && _cachedList[i].SubType == subType)
                {
                    endTime = _cachedList[i].EndTime;
                    return true;
                }
            }
            endTime = 0;
            return false;
        }

        private async Task ApplyActivityIconsAsync(List<ActInfo> list, int version)
        {
            var next = new Dictionary<string, int>();
            for (int i = 0; i < list.Count; i++)
            {
                ActInfo info = list[i];
                // 节日大妖(51)由 BossController 时间窗驱动,通用路径跳过(对标老端 AddActivityIcons 对 FEASTBOSS 的 continue)。
                if (info.BaseType == FEAST_BOSS_BASE_TYPE) continue;
                string iconType = await CustomActivityConfigs.ResolveIconTypeAsync(info);
                if (string.IsNullOrEmpty(iconType)) continue;
                // 节日投资/红包返利图标由专属通道(On33211/On33255)管,通用路径跳过(对标老端 AddActivityIcons 的 continue)。
                if (iconType == FTVINVEST_ICON || iconType == RED_ENVELOPE_REBATE_ICON) continue;
                // 每日直购(331@115@0):未首充不显示(对标老端 AddActivityIcons:IsNoFirstRecharge → continue)。
                if (iconType == "331@115@0" && !FirstRechargeModel.Instance.IsDoneFirstRecharge()) continue;
                // 充值豪礼·开服累充(331@7@0):等级 < 40 不显示(对标老端 AddActivityIcons)。
                if (iconType == "331@7@0" && (!RoleModel.Instance.HasBaseInfo || RoleModel.Instance.Level < 40)) continue;
                next[iconType] = info.EndTime;
            }

            if (version != _applyVersion) return;

            var remove = new List<string>();
            foreach (string iconType in _ownedIcons)
            {
                if (!next.ContainsKey(iconType)) remove.Add(iconType);
            }
            for (int i = 0; i < remove.Count; i++)
            {
                _ownedIcons.Remove(remove[i]);
                ActivityIconManager.Instance.DeleteIcon(remove[i]);
            }

            foreach (KeyValuePair<string, int> kv in next)
            {
                _ownedIcons.Add(kv.Key);
                await ActivityIconManager.Instance.AddIconAsync(kv.Key, kv.Value);
            }
        }

        private void ClearOwnedIcons()
        {
            foreach (string iconType in _ownedIcons)
            {
                ActivityIconManager.Instance.DeleteIcon(iconType);
            }
            _ownedIcons.Clear();
            // 专属通道图标不入 _ownedIcons,单独清。
            ActivityIconManager.Instance.DeleteIcon(FTVINVEST_ICON);
            ActivityIconManager.Instance.DeleteIcon(RED_ENVELOPE_REBATE_ICON);
        }
    }
}
