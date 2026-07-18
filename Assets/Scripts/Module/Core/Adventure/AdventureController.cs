using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Adventure
{
    /// <summary>
    /// 天天冒险控制器(对标老客户端 AdventureController)。进游戏请求 42700;回包据活动时间窗
    /// (stage/start_time/end_time)+ 神装功能开放态,增删主界面活动图标(42701/42702)。
    /// 对标老端 SetTimeInfo:先删两个图标,再在开启时加当天那一个(默认 42701)。
    /// 等级变化(EVT_ROLE_INFO_UPDATE)时复请求 42700——对标老端 CHANGE_LEVEL 复算开启态
    /// (神装门槛依赖等级),让升级达标后图标及时出现。
    /// 本期只做图标,明细/投掷/商店等协议(42701-42706)与面板待用户验收。
    /// </summary>
    public sealed class AdventureController : BaseController
    {
        public static readonly AdventureController Instance = new AdventureController();
        private AdventureController() { }

        // 复请求 42700 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.ADVENTURE_INFO, On42700);
            // 对标老端 CHANGE_LEVEL→复算开启态:等级变化时复请求 42700(神装功能等级门槛)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            // 对标老端 AdventureController.ts:52:DAY_CHANGE→ref_func 发 42700,并在 model.shop_data 已加载时
            // 延迟发 42704(商店刷新)。42704/商店(shop_data)本期未移植(面板/协议 42701-42706 待用户验收),
            // 故只复请求 42700,42704 留 todos_left,不发明数据流。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, RequestStartup);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, RequestStartup);
            ActivityIconManager.Instance.DeleteIcon(AdventureModel.ICON_TYPE_A);
            ActivityIconManager.Instance.DeleteIcon(AdventureModel.ICON_TYPE_B);
            AdventureModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START 发 42700)。</summary>
        public void RequestStartup()
        {
            SendFmt(Proto.ADVENTURE_INFO);
        }

        // 42700: stage:h, start_time:i, end_time:i(read(42700,_)->{ok,[]} 无参请求)
        private void On42700(NetReader r)
        {
            int stage = r.ReadU16();
            long startTime = r.ReadU32();
            long endTime = r.ReadU32();

            AdventureModel.Instance.SetTimeInfo(stage, startTime, endTime);
            _ = RefreshIconAsync();

            GameLog.Info("Adventure", "42700 天天冒险: stage={0} start={1} end={2}",
                stage, startTime, endTime);
        }

        // 对标老端 SetTimeInfo:先删两个图标,开启时只加当天那一个(默认 42701)。
        // 开启态依赖神装功能开放(FuncOpenConfig),故先确保配置加载;加图标走 AddIconAsync
        // (含图标配置门 open_lv/open_day,与老端 addIcon 一致)。
        private async Task RefreshIconAsync()
        {
            await FuncOpenConfig.EnsureLoaded();

            AdventureModel m = AdventureModel.Instance;
            if (m.IsActivityOpen())
            {
                string cur = m.GetCurIconType();
                string other = cur == AdventureModel.ICON_TYPE_A
                    ? AdventureModel.ICON_TYPE_B
                    : AdventureModel.ICON_TYPE_A;
                ActivityIconManager.Instance.DeleteIcon(other);
                await ActivityIconManager.Instance.AddIconAsync(cur);
            }
            else
            {
                ActivityIconManager.Instance.DeleteIcon(AdventureModel.ICON_TYPE_A);
                ActivityIconManager.Instance.DeleteIcon(AdventureModel.ICON_TYPE_B);
            }
        }

        // 对标老端:主角等级变化复请求 42700(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
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
