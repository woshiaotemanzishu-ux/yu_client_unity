using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.GrowthBenefits
{
    /// <summary>
    /// 成长福利控制器(对标老客户端 GrowthBenefitsController)。进游戏请求 41720 拿成长任务领取态;
    /// 回包据 GetEntranceOpenState(等级 ≥135 且任务未全领)增删主界面图标 41720。
    /// 41721 为服务端推送的任务进度变更,合并后同样刷新图标。等级变化(EVT_ROLE_INFO_UPDATE)复请求 41720
    /// (对标老端 CHANGE_LEVEL→refreshIcon/复算),让升到 135 级开启成长福利后图标及时出现。
    /// 本期只做图标;41722(领取回包)属面板动作、41723/41724(战力福利)未移植,均待用户验收。
    /// </summary>
    public sealed class GrowthBenefitsController : BaseController
    {
        public static readonly GrowthBenefitsController Instance = new GrowthBenefitsController();
        private GrowthBenefitsController() { }

        public const string ICON_TYPE = GrowthBenefitsModel.ICON_TYPE;

        // 复请求 41720 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.GROWTHBENEFITS_INFO, On41720);
            RegisterProtocal(Proto.GROWTHBENEFITS_TASK_UPDATE, On41721);
            // 对标老端 CHANGE_LEVEL→刷新图标:等级变化时复请求(135级开启成长福利)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            GrowthBenefitsModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START 发 41720)。</summary>
        public void RequestStartup()
        {
            // read(41720,_)->{ok,[]}:请求无字段,裸发。
            SendFmt(Proto.GROWTHBENEFITS_INFO);
        }

        // 41720: task_list[u16 count × { task_id:h, process:h, status:c }](登录/换天全量下发)
        private void On41720(NetReader r)
        {
            List<KeyValuePair<int, int>> tasks = ReadTaskList(r);
            GrowthBenefitsModel.Instance.SetTaskList(tasks);
            RefreshIcon("41720");
        }

        // 41721: task_list[u16 count × { task_id:h, process:h, status:c }](服务端推送任务进度变更,增量)
        private void On41721(NetReader r)
        {
            List<KeyValuePair<int, int>> tasks = ReadTaskList(r);
            GrowthBenefitsModel.Instance.UpsertTasks(tasks);
            RefreshIcon("41721");
        }

        // item_to_bin_8/9 一致:task_id:16, process:16, status:8。process 面板进度用,本期图标不需,读掉即可。
        private static List<KeyValuePair<int, int>> ReadTaskList(NetReader r)
        {
            int count = r.ReadU16();
            var list = new List<KeyValuePair<int, int>>(count);
            for (int i = 0; i < count; i++)
            {
                int taskId = r.ReadU16();
                r.ReadU16();               // process(面板进度,本期不存)
                int status = r.ReadU8();
                list.Add(new KeyValuePair<int, int>(taskId, status));
            }
            return list;
        }

        private void RefreshIcon(string from)
        {
            GrowthBenefitsModel m = GrowthBenefitsModel.Instance;
            bool open = m.GetEntranceOpenState();
            // 41720 图标配置 open_lv/open_day 均为 0(门槛在本控制器 GetEntranceOpenState 里把控),
            // 走 AddIconAsync 过一遍图标配置门(与老端 addIcon 一致);controll_by_own_fun=true 仅表示通用扫描
            // (RefreshDefaultIconsCoreAsync)会跳过它,由本控制器负责增删。
            if (open) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE);
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);

            GameLog.Info("GrowthBenefits", "{0} 成长福利: allReceive={1} open={2}",
                from, m.GetTaskIsAllReceive(), open);
        }

        // 对标老端:主角等级变化复请求 41720(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }
    }
}
