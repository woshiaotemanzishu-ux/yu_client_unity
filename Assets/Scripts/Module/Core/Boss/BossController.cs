using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Boss
{
    /// <summary>
    /// BOSS(节日大妖)控制器。大 BOSS 系统只提取"节日大妖"主界面图标 51,本服/跨服 BOSS 玩法协议
    /// (46xxx/47xxx:进副本/BOSS信息/掉落/复活/幻域/圣兽岭…)全部不做。
    ///
    /// 图标 51 的驱动 = 自定义活动列表 33101(FEASTBOSS,base_type=51)——老端 BossModel.FeastBossActivity
    /// 读该活动 condition 的时间窗,处于窗内则 addIcon("51", 结束时间戳)(带倒计时),当天有下一场则
    /// addIcon("51", "HH:MM开启")(预告),否则 deleteIcon("51")。老端也没有专属 BOSS 协议驱动该图标,
    /// 纯粹是自定义活动 + 客户端时间窗计算。
    ///
    /// ★不在此重注册 33101★:Unity NetManager 每协议仅一处理器(RegisterProtocal 覆盖式),33101 已由
    /// CustomActivityController 独占并解析(它进游戏 RequestActivityList() 发过、回包驱动全部自定义活动图标);
    /// 若此处再注册会把所有自定义活动图标逻辑覆盖掉。理想接线是订阅 CustomActivity 侧解析 FEASTBOSS(51)
    /// 时间窗后广播的事件(对标老端 AddVipServiceController 订阅首充 EVT_FIRST_RECHARGE_UPDATE 的做法),
    /// 但当前 CustomActivityController 既不广播活动更新事件、也不暴露 act info,暂无可订阅信号 →
    /// 图标默认隐藏(等价"当前无节日BOSS活动开启"),这是非破坏、与"无活动"一致的安全态。
    /// 接线口子已留:CustomActivity 侧(或每秒时间窗计时器)算出 FEASTBOSS 时间窗后调 NotifyFeastBossActivity 即可点亮。
    ///
    /// 等级变化(EVT_ROLE_INFO_UPDATE,去抖)复评图标以对齐模板;节日BOSS图标无等级门(靠活动时间窗),此处仅作复检钩子。
    /// 本期只做图标;节日BOSS玩法/面板(采集/掉落/结算/入口)未移植,待用户验收。
    /// </summary>
    public sealed class BossController : BaseController
    {
        public static readonly BossController Instance = new BossController();
        private BossController() { }

        public const string ICON_TYPE = BossModel.ICON_TYPE;

        // 复评图标的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时复评。
        private int _lastLevel = -1;

        protected override void Register()
        {
            // 无专属 BOSS 图标协议可注册:驱动源 33101 已由 CustomActivityController 独占(见类注释,不可重注册)。
            // 模板对齐:等级变化复评(节日BOSS图标无等级门,仅复检钩子)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            BossModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>
        /// 进游戏钩子(GameStartController.RequestStartupPackets 调用)。驱动协议 33101 已由
        /// CustomActivityController.RequestActivityList() 请求,此处无需发包,仅按当前状态复评一次图标
        /// (此刻多半无节日BOSS活动、复评为空;真正驱动在 NotifyFeastBossActivity 被接线后)。
        /// </summary>
        public void RequestStartup()
        {
            RefreshIcon("startup");
        }

        /// <summary>
        /// 节日BOSS图标驱动入口(对标老端 CustomActivityModel base_type==FEASTBOSS → BossModel.FeastBossActivity)。
        /// 由 CustomActivityController 在 33101 列表刷新/复评时调用:
        ///   · hasActivity=false(列表里没有 FEASTBOSS 活动)→ 清图标;
        ///   · hasActivity=true → 按活动 condition 的每日时间窗算三态(窗内倒计时 / 当天下一场预告 / 都无则删)。
        /// 注:未接每秒定时器,窗口边界的自动切换靠 33101 刷新或等级/任务复评时重算(icon-first 阶段够用);
        /// 严格边界即时切换(老端 StartFeastTimer 每秒轮询)待后续接全局定时器基建后补。
        /// </summary>
        public void EvaluateFeastBoss(bool hasActivity, string condition, int actStartTime, int actEndTime)
        {
            if (!hasActivity)
            {
                NotifyFeastBossActivity(false, 0, null);
                return;
            }
            (bool active, int endTime, string foreshadow) =
                BossModel.ComputeFeastWindow(condition, actStartTime, actEndTime, TimeUtil.NowSec());
            NotifyFeastBossActivity(active, endTime, foreshadow);
        }

        /// <summary>
        /// 节日BOSS图标接线入口(对标老端 BossModel.FeastBossActivity 的结果三态):
        ///   · active=true  → 处于活动时间窗内,endTime=活动结束时间戳(倒计时);
        ///   · active=false 且 foreshadow 非空 → 当天有下一场未开始,显示 "HH:MM开启" 预告;
        ///   · 都不满足(foreshadow 传 null) → 删除图标 51。
        /// </summary>
        public void NotifyFeastBossActivity(bool active, int endTime, string foreshadow = null)
        {
            BossModel.Instance.SetFeastBossActivity(active, endTime, foreshadow);
            RefreshIcon("feastBoss");
        }

        private void RefreshIcon(string from)
        {
            BossModel m = BossModel.Instance;
            if (m.FeastBossActive)
            {
                // 活动时间窗内:带倒计时(time=结束时间戳),对标老端 addIcon("51", left_time)。
                _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE, m.FeastBossEndTime);
            }
            else if (!string.IsNullOrEmpty(m.FeastBossForeshadow))
            {
                // 当天有下一场未开始:预告文本,对标老端 addIcon("51", null, null, "HH:MM开启")。
                _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE, 0, m.FeastBossForeshadow);
            }
            else
            {
                ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            }

            GameLog.Info("Boss", "{0} 节日大妖: active={1} endTime={2} foreshadow={3} open={4}",
                from, m.FeastBossActive, m.FeastBossEndTime, m.FeastBossForeshadow, m.GetEntranceOpenState());
        }

        // 模板对齐:主角等级变化(去抖)复评图标(节日BOSS图标无等级门,复检无害)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RefreshIcon("levelChange");
        }
    }
}
