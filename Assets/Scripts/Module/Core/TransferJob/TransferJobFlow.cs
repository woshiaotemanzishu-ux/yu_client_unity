using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.TransferJob
{
    /// <summary>
    /// 转职卡窗口编排(对标 <see cref="Shenxiao.Module.Core.MainUI.MainUIFlow"/>.ShowReliveAsync 的独立
    /// prefab 按需加载模式):事件驱动(道具使用触发)按需实例化 TransferJobCardView.prefab、挂 Window 层,
    /// 缓存复用。prefab 由 <c>Assets/Editor/UiCreator/TransferJob/TransferJobCreator.cs</c> 生成
    /// (无死树可嫁接的全新纯代码建树,真机包前需跑一次「神霄/资源/Addressable 自动分组」注册地址)。
    /// </summary>
    public static class TransferJobFlow
    {
        private const string MODULE = "TransferJob";
        private const string PREFAB = "TransferJobCardView";

        private static TransferJobCardView _view;
        private static bool _loading;

        /// <summary>打开转职卡界面(对标老端 Fire(OPEN_VIEW,"TransferJobCardView"))。</summary>
        public static void Show() => _ = ShowAsync();

        private static async Task ShowAsync()
        {
            if (_view != null) { _view.Show(); return; }
            if (_loading) return;

            _loading = true;
            string key = GameResPath.GetUIPrefab(MODULE, PREFAB);
            GameObject go = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            _loading = false;

            if (go == null)
            {
                GameLog.Warn("TransferJob", "TransferJobCardView 预制加载失败: {0}(先跑 TransferJobCreator 生成 + Addressable 自动分组)", key);
                return;
            }
            _view = go.GetComponent<TransferJobCardView>();
            if (_view == null)
            {
                GameLog.Warn("TransferJob", "TransferJobCardView 预制缺组件(重跑 TransferJobCreator)");
                ResManager.ReleaseInstance(go);
                return;
            }
            _view.Show();
        }

        public static void Close() => _view?.Hide();

        internal static void Reset()
        {
            if (_view != null) ResManager.ReleaseInstance(_view.gameObject);
            _view = null;
            _loading = false;
        }
    }
}
