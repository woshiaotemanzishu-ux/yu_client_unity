using System.Collections.Generic;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.AutoBrush;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Dialogue;
using Shenxiao.Module.Core.FirstRecharge;
using Shenxiao.Module.Core.FunctionOpen;
using Shenxiao.Module.Core.Gm;
using Shenxiao.Module.Core.Mail;
using Shenxiao.Module.Core.Notice;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene;
using Shenxiao.Module.Core.SurpriseGift;
using Shenxiao.Module.Core.Tasks;
using Shenxiao.Module.Core.WeekCard;

namespace Shenxiao.Module.Core.Game
{
    /// <summary>
    /// 游戏内控制器注册中心:进游戏统一 Init、断线/登出统一 Dispose。
    /// 新增游戏内模块 = 在 ALL 里加一行(对标老客户端各 commonController 的集中注册)。
    /// 登录控制器(LoginController)不在此列——它在登录链就绪、跨越进游戏边界,单独管理。
    /// </summary>
    public static class ControllerHub
    {
        private static readonly BaseController[] ALL =
        {
            GameStartController.Instance,
            RoleController.Instance,
            SceneController.Instance,
            TaskController.Instance,
            DialogueController.Instance,
            AutoBrushController.Instance,
            GmCheatController.Instance,
            NoticeController.Instance,
            MailController.Instance,
            FunctionOpenController.Instance,
            FirstRechargeController.Instance,
            WeekCardController.Instance,
            SurpriseGiftController.Instance,
            BagController.Instance,
            // 后续:EquipController / ChatController ...
        };

        public static bool Initialized { get; private set; }

        public static void InitAll()
        {
            if (Initialized) return;

            foreach (BaseController c in ALL) c.Init();
            Initialized = true;
            GameLog.Info("Game", "游戏内控制器已注册: {0} 个", ALL.Length);
        }

        public static void DisposeAll()
        {
            foreach (BaseController c in ALL) c.Dispose();
            Initialized = false;
            GameLog.Info("Game", "游戏内控制器已注销(断线/登出)");
        }
    }
}
