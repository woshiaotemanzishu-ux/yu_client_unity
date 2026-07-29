using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.TopVip
{
    /// <summary>至尊VIP 451xx 读侧控制器；45120 SVIP活动仍由独立 SvipController 持有。</summary>
    public sealed class TopVipController : BaseController
    {
        public static readonly TopVipController Instance = new TopVipController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private TopVipController() { }

        public const string ICON_TYPE = TopVipModel.ICON_TYPE;

        private int _lastLevel = -1;
        private int _lastVipFlag = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.TOPVIP_INFO, On45101);
            RegisterProtocal(Proto.TOPVIP_SKILL_TASKS, On45102);
            RegisterProtocal(Proto.TOPVIP_CURRENCY_TASKS, On45104);
            RegisterProtocal(Proto.TOPVIP_UPGRADE_NOTICE, On45109);
            RegisterProtocal(Proto.TOPVIP_SKILL_TASK_UPDATE, On45110);
            RegisterProtocal(Proto.TOPVIP_CURRENCY_TASK_UPDATE, On45111);
            RegisterProtocal(Proto.TOPVIP_FREE_PROTECT, On45112);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            TopVipModel.Instance.Reset();
            _lastLevel = -1;
            _lastVipFlag = -1;
            base.Dispose();
        }

        /// <summary>对标老端 GAME_START 的固定顺序：45101 → 45102 → 45104。</summary>
        public void RequestStartup()
        {
            RequestInfo();
            RequestSkillTasks();
            RequestCurrencyTasks();
        }

        public void RequestInfo() => SendEmpty(Proto.TOPVIP_INFO);
        public void RequestSkillTasks() => SendEmpty(Proto.TOPVIP_SKILL_TASKS);
        public void RequestCurrencyTasks() => SendEmpty(Proto.TOPVIP_CURRENCY_TASKS);

        private void SendEmpty(int protoId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId);
        }

        private void On45101(NetReader r)
        {
            byte supvipType = r.ReadU8();
            uint supvipTime = r.ReadU32();
            int count = r.ReadU16();
            var rights = new List<TopVipModel.RightEntry>(count);
            for (int i = 0; i < count; i++)
                rights.Add(new TopVipModel.RightEntry(r.ReadU8(), r.ReadString(), r.ReadU32()));
            byte chargeDay = r.ReadU8();
            uint todayGold = r.ReadU32();
            byte isFreeProtect = r.ReadU8();

            TopVipModel.Instance.ReplaceInfo(
                supvipType,
                supvipTime,
                rights,
                chargeDay,
                todayGold,
                isFreeProtect);
            GameLog.Info("TopVip", "45101 type={0} rights={1} chargeDay={2}", supvipType, rights.Count, chargeDay);
        }

        private void On45102(NetReader r)
        {
            byte stage = r.ReadU8();
            byte subStage = r.ReadU8();
            TopVipModel.Instance.ReplaceSkillTasks(stage, subStage, ReadTasks(r));
        }

        private void On45104(NetReader r)
        {
            TopVipModel.Instance.ReplaceCurrencyTasks(ReadTasks(r));
        }

        private void On45109(NetReader r)
        {
            RequestInfo();
        }

        private void On45110(NetReader r)
        {
            TopVipModel.Instance.ReplaceSkillTaskUpdate(ReadTasks(r));
            RequestSkillTasks();
        }

        private void On45111(NetReader r)
        {
            TopVipModel.Instance.ReplaceCurrencyTaskUpdate(ReadTasks(r));
            RequestCurrencyTasks();
        }

        private void On45112(NetReader r)
        {
            TopVipModel.Instance.ReplaceFreeProtectUpdate(r.ReadU8());
        }

        private static List<TopVipModel.TaskEntry> ReadTasks(NetReader r)
        {
            int count = r.ReadU16();
            var tasks = new List<TopVipModel.TaskEntry>(count);
            for (int i = 0; i < count; i++)
                tasks.Add(new TopVipModel.TaskEntry(r.ReadU16(), r.ReadU8(), r.ReadU8(), r.ReadString()));
            return tasks;
        }

        private void RefreshIcon()
        {
            if (IsEntranceOpen()) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE);
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
        }

        private static bool IsEntranceOpen()
        {
            RoleModel role = RoleModel.Instance;
            int level = role.HasBaseInfo ? role.Level : 0;
            return TopVipModel.Instance.GetEntranceOpenState(GetVipFlag(), level);
        }

        /// <summary>老端角色变化只复判入口，不额外重拉451xx；全量查询只由GAME_START或显式页面触发。</summary>
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            int vipFlag = GetVipFlag();
            if (role.Level == _lastLevel && vipFlag == _lastVipFlag) return;
            _lastLevel = role.Level;
            _lastVipFlag = vipFlag;
            RefreshIcon();
        }

        private static int GetVipFlag()
        {
            var figure = RoleModel.Instance.Figure;
            if (figure == null) return 0;
            return figure.Raw.TryGetValue("vip_flag", out object value) ? Convert.ToInt32(value) : 0;
        }
    }
}
