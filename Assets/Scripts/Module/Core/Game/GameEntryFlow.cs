using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Game
{
    /// <summary>
    /// 进游戏编排(对标登录 LoginFlow,管的是"进游戏之后"):
    ///   10004 成功(EVT_GAME_ENTERED)→ 注册游戏内控制器(ControllerHub)→ 等服务端推 13001 主角全量
    ///   → 主角就绪(EVT_ROLE_READY),交给主城/场景接管。
    ///   断线(EVT_NET_DISCONNECTED)→ 注销控制器、重置主角数据。
    /// 进游戏后服务端会主动推几十条协议,未注册的在 NetManager 只记 Info(预期内),按模块逐步消化。
    /// </summary>
    public static class GameEntryFlow
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EventDispatcher.On(GlobalEvent.EVT_GAME_ENTERED, OnGameEntered);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, OnDisconnected);
        }

        private static void OnGameEntered()
        {
            GameLog.Info("Game", "—— 进入游戏:注册游戏内控制器,等待主角数据(13001)——");
            RoleModel.Instance.Reset();
            ControllerHub.InitAll();
            // 等首个 13001;到了再交接(只听一次)
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfo);
        }

        private static void OnRoleInfo()
        {
            if (!RoleModel.Instance.HasBaseInfo) return; // 13002/13006 可能先到,等 13001 全量
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfo);
            RoleModel m = RoleModel.Instance;
            GameLog.Info("Game",
                "—— 🎉 主角就绪:{0} Lv.{1} 战力{2} 铜币{3} 元宝{4} 场景{5}({6},{7})——交主城/场景接管(待接)",
                m.Name, m.Level, m.CombatPower, m.Coin, m.Gold, m.SceneId, m.X, m.Y);
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_READY);
        }

        private static void OnDisconnected()
        {
            if (!ControllerHub.Initialized) return;
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfo);
            ControllerHub.DisposeAll();
            RoleModel.Instance.Reset();
        }
    }
}
