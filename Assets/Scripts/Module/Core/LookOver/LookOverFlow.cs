using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Friend;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.LookOver
{
    /// <summary>
    /// 他人资料卡窗口编排(轮21 module 1 基础卡 + 轮28 module 2-12 扩展资料——对标老客户端各处
    /// requestPlayerMessage/ChatMenuView"查看信息" → 15011/19501 → PlayerMessageView 的最小可用形态)。
    ///
    /// 数据链复用 Friend 模块：19501 统一请求走
    /// <see cref="FriendController.RequestPlayerCard"/>(wire "hlh" 与 pt_195.erl:8-12 read 子句一致);
    /// 19502 落地走 <see cref="FriendModel.SetPlayerCard"/> → <c>GlobalEvent.EVT_PLAYER_CARD</c>；
    /// 19503-19512 走结构化扩展快照 → <c>GlobalEvent.EVT_LOOKOVER_MODULE</c>，由同一窗口切换消费。
    ///
    /// ⚠**陷阱③自查**(侦察 r21_lookover.md §1.3/§8.6,服务端 lib_player_look_over.erl:89/:56
    /// 两处"自己查自己"子句顺序遮蔽 → 零回包):FriendController.RequestPlayerCard 本身**没有**
    /// 自查(它只是薄封装,照抄 wire 直发),所以必须在这一层(Unity 侧唯一入口收口点)拦截,
    /// 否则点自己头像会一直停在"加载中"永远等不到 19502。<see cref="Show"/> 是全仓调用
    /// LookOver 的唯一入口,别绕过它直接调 FriendController.RequestPlayerCard。
    ///
    /// 19501 本身对我方发出后:成功路径服务端不回包(参数非法才回错误码,pt_195.erl:13/:29),
    /// 真正数据由服务器随后经 19502(module=1)推送 → EventDispatcher 回调本类打开的面板。
    /// </summary>
    public static class LookOverFlow
    {
        private const string MODULE = "LookOver";
        private const string PREFAB = "LookOverCardView";

        private static Views.LookOverCardView _view;
        private static bool _loading;
        private static int _generation;

        public readonly struct Target
        {
            public readonly long RoleId;
            public readonly int ServerId;

            public Target(long roleId, int serverId)
            {
                RoleId = roleId;
                ServerId = serverId;
            }
        }

        /// <summary>打开资料卡(默认 module 1，可在窗口内切换 module 2-12)。serverId=0 表示同服/合服;跨服传对方 server_id。
        /// 自己/无效 role_id 直接拦截,不发包也不开面板(陷阱③,见类注释)。</summary>
        public static void Show(long roleId, int serverId = 0)
        {
            if (roleId == 0 || roleId == RoleModel.Instance.RoleId)
            {
                TipsManager.Toast("无法查看自己的资料卡");
                GameLog.Info("LookOver", "Show 拦截:自己/无效 roleId={0}", roleId);
                return;
            }
            _ = ShowAsync(roleId, serverId);
        }

        public static void Close() => _view?.Hide();

        private static async Task ShowAsync(long roleId, int serverId)
        {
            if (_loading) return;
            int generation = _generation;

            if (_view == null)
            {
                _loading = true;
                string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
                GameObject go = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
                if (generation != _generation)
                {
                    if (go != null) ResManager.ReleaseInstance(go);
                    return;
                }
                _loading = false;

                if (go == null)
                {
                    GameLog.Warn("LookOver", "LookOverCardView 预制加载失败: {0}(先跑 LookOverCardCreator 生成 + Addressable 自动分组)", key);
                    return;
                }
                _view = go.GetComponent<Views.LookOverCardView>();
                if (_view == null)
                {
                    GameLog.Warn("LookOver", "LookOverCardView 预制缺组件(重跑 LookOverCardCreator)");
                    ResManager.ReleaseInstance(go);
                    return;
                }
            }

            _view.Show(new Target(roleId, serverId));
        }

        /// <summary>断线(非游戏内自动重连)清面板,对标 FriendFlow/TransferJobFlow 同款 Reset。</summary>
        internal static void Reset()
        {
            _generation++;
            if (_view != null)
            {
                _view.Hide(); // 先触发 View 的配对 Off/目标清理，再释放实例
                ResManager.ReleaseInstance(_view.gameObject);
            }
            _view = null;
            _loading = false;
        }
    }
}
