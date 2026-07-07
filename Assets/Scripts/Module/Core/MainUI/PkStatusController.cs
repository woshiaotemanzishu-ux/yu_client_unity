using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// PK(战斗)模式切换控制器(对标老客户端 commonController/PkStatusController.ts)。
    /// 发 13012 "c"(目标 pk_status);收 13012 "ici"(error_code, pk_status, remain_time):
    ///   code==1 且 remain==0 → 主角 pk_status 落 RoleModel + EVT_PK_STATUS_CHANGED + EVT_PK_CHANGE_SUCCESS
    ///   (弹窗据此「切换成功」并关闭,HudTop 刷图标);
    ///   code==1 且 remain>0 → 记和平切换冷却(peace_cd_time),后续点击由弹窗提示「冷却中」(老端同为静默记录);
    ///   其余错误码 → 显码降级提示(老端 Util.ErrorCodeShow 错误码表未移植,同登录链做法)。
    /// 他人 pk 状态广播(12074)在 SceneController;主角初值来自进场自块(SceneController.ParseRole)。
    /// </summary>
    public sealed class PkStatusController : BaseController
    {
        public static readonly PkStatusController Instance = new PkStatusController();
        private PkStatusController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.PK_STATUS_CHANGE, On13012);
        }

        /// <summary>请求切换 PK 模式(对标老端 CHANGE_PK_STATUS → SendFmtToGame(13012,"c",type))。</summary>
        public void SendChangePkStatus(int pkStatus)
        {
            SendFmt(Proto.PK_STATUS_CHANGE, "c", pkStatus);
            GameLog.Info("MainUI", "request 13012 切换PK模式 → {0}", pkStatus);
        }

        private void On13012(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            int pkStatus = r.ReadU8();
            int remainTime = (int)r.ReadU32();

            if (errorCode != 1)
            {
                TipsManager.Toast("切换失败(" + errorCode + ")");
                return;
            }

            if (remainTime == 0)
            {
                RoleModel.Instance.PkStatus = pkStatus;
                EventDispatcher.Emit(GlobalEvent.EVT_PK_STATUS_CHANGED);
                EventDispatcher.Emit(GlobalEvent.EVT_PK_CHANGE_SUCCESS);
                GameLog.Info("MainUI", "13012 PK模式切换成功 → {0}", pkStatus);
            }
            else
            {
                RoleModel.Instance.SetPeaceCd(remainTime);
                GameLog.Info("MainUI", "13012 PK切换进入冷却 remain={0}s", remainTime);
            }
        }
    }
}
