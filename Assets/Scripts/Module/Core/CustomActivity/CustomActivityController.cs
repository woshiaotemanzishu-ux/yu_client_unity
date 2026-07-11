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
    public sealed class CustomActivityController : BaseController
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

            // 等级/任务变化用缓存列表复评图标(对标老端 CHANGE_LEVEL→UpdateActivityIcons(cache) 与 taskChange)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, OnTaskListUpdated);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, OnTaskListUpdated);
            ClearOwnedIcons();
            _cachedList.Clear();
            _lastLevel = -1;
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
            for (int i = 0; i < count; i++)
            {
                _cachedList.Add(new ActInfo
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
                });
            }

            // 专属通道活动:见到即请求详情包,由 On33211/On33255 决定图标显隐。
            RequestDirectBranchDetails();
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
            for (int i = 0; i < investCount; i++) r.ReadU8(); // 每档投资等级(面板用,本期只做图标)
            int buyTime = (int)r.ReadU32();

            // etime:有投资记录看活动结束时间,否则看购买截止时间(对标老端 On33211)。
            int etime = investCount > 0 && TryGetCachedEndTime(baseType, subType, out int actEnd) ? actEnd : buyTime;
            if (etime > TimeUtil.NowSec()) _ = ActivityIconManager.Instance.AddIconAsync(FTVINVEST_ICON, etime);
            else ActivityIconManager.Instance.DeleteIcon(FTVINVEST_ICON);

            GameLog.Info("CustomActivity", "33211 节日投资: base={0} sub={1} invest={2} etime={3}",
                baseType, subType, investCount, etime);
        }

        // 红包返利(33255,→331@117@0):type:h, subtype:h, is_quality:c, start_time:i, end_time:i, login_money:h,
        // recharge_money:h, login_status:c, recharge_status:c, login_withdrawal:c, recharge_withdrawal:c,
        // login_global_times:i, recharge_global_times:i(对标 pt_332 write 33255)。
        private void On33255(NetReader r)
        {
            r.ReadU16();                    // type
            r.ReadU16();                    // subtype
            int isQuality = r.ReadU8();     // 是否达标开启
            r.ReadU32();                    // start_time
            int endTime = (int)r.ReadU32(); // end_time
            r.ReadU16();                    // login_money
            r.ReadU16();                    // recharge_money
            r.ReadU8();                     // login_status
            r.ReadU8();                     // recharge_status
            r.ReadU8();                     // login_withdrawal
            r.ReadU8();                     // recharge_withdrawal
            r.ReadU32();                    // login_global_times
            r.ReadU32();                    // recharge_global_times

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
