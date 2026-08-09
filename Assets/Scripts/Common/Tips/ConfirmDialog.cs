using System;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Alert;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Common.Tips
{
    /// <summary>
    /// 双按钮确认框。视觉唯一来源为 Alert/AlertModule.prefab 内的 AlertTypeTwo；
    /// 本类只负责懒加载、填文字、绑定点击和复用生命周期。
    /// </summary>
    public static class ConfirmDialog
    {
        private const string Module = "alert";
        private const string Prefab = "AlertModule";

        private static GameObject _moduleRoot;
        private static AlertTypeTwoBind _view;
        private static Action _onYes;
        private static Action _onNo;
        private static string _pendingText;
        private static string _pendingYesLabel = "确认";
        private static string _pendingNoLabel = "取消";
        private static bool _loading;

        public static void Show(string text, Action onYes, Action onNo,
            string yesLabel = "确认", string noLabel = "取消")
        {
            _pendingText = text ?? string.Empty;
            _onYes = onYes;
            _onNo = onNo;
            _pendingYesLabel = string.IsNullOrEmpty(yesLabel) ? "确认" : yesLabel;
            _pendingNoLabel = string.IsNullOrEmpty(noLabel) ? "取消" : noLabel;

            if (_view != null)
            {
                ShowLoaded();
                return;
            }

            if (!_loading)
            {
                _loading = true;
                _ = LoadAndShowAsync();
            }
        }

        private static async Task LoadAndShowAsync()
        {
            string key = GameResPath.GetUIPrefab(Module, Prefab);
            GameObject root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Tip));
            _loading = false;
            if (root == null)
            {
                GameLog.Error("Tip", "确认框 Prefab 加载失败: {0}", key);
                Action fallback = _onNo;
                ClearCallbacks();
                fallback?.Invoke();
                return;
            }

            _moduleRoot = root;
            _moduleRoot.name = "ConfirmDialog";
            Image blocker = _moduleRoot.GetComponent<Image>();
            if (blocker == null) blocker = _moduleRoot.AddComponent<Image>();
            blocker.color = Color.clear;
            Bind(blocker, () => Close(false));
            foreach (BaseView candidate in root.GetComponentsInChildren<BaseView>(true))
            {
                candidate.gameObject.SetActive(false);
                if (candidate is AlertTypeTwoBind alert)
                {
                    _view = alert;
                }
            }

            if (_view == null)
            {
                GameLog.Error("Tip", "确认框 Prefab 缺 AlertTypeTwoBind: {0}", key);
                ResManager.ReleaseInstance(root);
                _moduleRoot = null;
                Action fallback = _onNo;
                ClearCallbacks();
                fallback?.Invoke();
                return;
            }

            BindClicks();
            ShowLoaded();
        }

        private static void BindClicks()
        {
            Bind(_view._ok_btn, () => Close(true));
            Bind(_view._cancel_btn, () => Close(false));
            Bind(_view._close_btn, () => Close(false));
            Bind(_view.bg, () => Close(false));
        }

        private static void Bind(Image target, Action action)
        {
            if (target == null) return;
            target.raycastTarget = true;
            UIUtil.ClearClicks(target);
            UIUtil.AddClick(target, action);
        }

        private static void ShowLoaded()
        {
            if (_view == null) return;
            _moduleRoot.SetActive(true);
            _moduleRoot.transform.SetAsLastSibling();
            _view.Show();
            if (_view._content_html != null) _view._content_html.text = _pendingText;
            if (_view.ok_label != null) _view.ok_label.text = _pendingYesLabel;
            if (_view.cancel_label != null) _view.cancel_label.text = _pendingNoLabel;
            Canvas.ForceUpdateCanvases();
            if (_view.transform is RectTransform root) LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Canvas.ForceUpdateCanvases();
        }

        private static void Close(bool yes)
        {
            _view?.Hide();
            if (_moduleRoot != null) _moduleRoot.SetActive(false);
            Action callback = yes ? _onYes : _onNo;
            ClearCallbacks();
            callback?.Invoke();
        }

        private static void ClearCallbacks()
        {
            _onYes = null;
            _onNo = null;
            _pendingText = null;
            _pendingYesLabel = "确认";
            _pendingNoLabel = "取消";
        }

        /// <summary>编辑器预览或资源更新后释放缓存，下次从最新 Prefab 重载。</summary>
        public static void ReloadView()
        {
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
            _view = null;
            _loading = false;
            ClearCallbacks();
        }
    }
}
