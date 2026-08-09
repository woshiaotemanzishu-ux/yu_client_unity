using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;

namespace Shenxiao.Common.Tips
{
    /// <summary>
    /// Toast / floating tip / confirmation dialog。
    /// Toast/Float 对标老端 sysInfo 链(Message.show → APPEND_MSG → SysInfoType.MINI → SysInfoMiniMgr):
    /// 首选 TipToastView.prefab(TipToastCreator 生成,样式可手调)排队播放;prefab 缺失回退代码建树样式;
    /// UI 层未就绪(headless/启动早期)自动退回 log-only。始终写 GameLog(供 CLI/无头验证断言)。
    /// Confirm 对标老端 Alert.Show 双按钮确认框(代码建树,置于 Tip 层):确认走 onYes、取消/点遮罩走 onNo;
    /// UI 层未就绪(headless/CLI)保持旧语义直接 onYes(无人可点,阻塞流程没有意义)。
    /// </summary>
    public static class TipsManager
    {
        public static void Toast(string text)
        {
            GameLog.Info("Tip", "toast: {0}", text);
            Show(text);
        }

        public static void Float(string text)
        {
            GameLog.Info("Tip", "float: {0}", text);
            Show(text);
        }

        public static void Confirm(string text, System.Action onYes, System.Action onNo = null,
            string yesLabel = "确认", string noLabel = "取消")
        {
            GameLog.Info("Tip", "confirm: {0}", text);
            if (ViewManager.GetLayer(UILayer.Tip) == null)
            {
                onYes?.Invoke(); // headless/启动早期:保持旧语义(CLI 验证依赖)
                return;
            }
            ConfirmDialog.Show(text, onYes, onNo, yesLabel, noLabel);
        }

        // ---- prefab 版(首选):TipToastView 排队/动画自管,这里只负责懒加载与转发 ----

        private static TipToastView _view;
        private static bool _loading;
        private static bool _prefabMissing;   // prefab 未生成:本次会话固定走代码建树兜底,不反复重试
        private static readonly Queue<string> _pendingWhileLoading = new Queue<string>();

        /// <summary>编辑器预览用:释放缓存实例,下次 Toast 从最新 prefab 重新实例化。</summary>
        public static void ReloadView()
        {
            ViewManager.Dispose<TipToastView>();
            _view = null;
            _loading = false;
            _prefabMissing = false;
            _pendingWhileLoading.Clear();
        }

        private static void Show(string text)
        {
            if (ViewManager.GetLayer(UILayer.Tip) == null) return;   // UI 层未 Init(headless/启动早期):log-only

            if (_view != null)
            {
                _view.Enqueue(text);
                return;
            }
            if (_prefabMissing)
            {
                ShowFloatingFallback(text);
                return;
            }
            _pendingWhileLoading.Enqueue(text);
            if (!_loading)
            {
                _loading = true;
                _ = LoadViewAsync();
            }
        }

        private static async Task LoadViewAsync()
        {
            TipToastView view = await ViewManager.Open<TipToastView>();
            if (view == null)
            {
                // prefab 缺失(未生成/未进组):降级代码建树样式,把加载期间积压的先补播
                _prefabMissing = true;
                _loading = false;
                GameLog.Warn("Tip", "TipToastView prefab 缺失(神霄/重构UI 生成器 → Common → TipToast 生成),降级代码建树样式");
                while (_pendingWhileLoading.Count > 0) ShowFloatingFallback(_pendingWhileLoading.Dequeue());
                return;
            }
            _view = view;
            while (_pendingWhileLoading.Count > 0) _view.Enqueue(_pendingWhileLoading.Dequeue());
        }

        // ---- 代码建树兜底(prefab 缺失时用;结构代码建、样式从简,行为与旧版一致)----

        private const int MaxLines = 5;      // 同屏上限,超出顶掉最旧
        private const float LifeSec = 2.2f;  // 单条存活
        private const float RisePx = 90f;    // 存活期内总上浮
        private const float LineGap = 46f;   // 新条入场时旧条整体上移一档

        private static readonly List<RectTransform> _live = new List<RectTransform>();
        private static TMP_FontAsset _font;
        private static Material _fontMat;

        private static void ShowFloatingFallback(string text)
        {
            Transform layer = ViewManager.GetLayer(UILayer.Tip);
            if (layer == null) return;

            while (_live.Count >= MaxLines)
            {
                RectTransform oldest = _live[0];
                _live.RemoveAt(0);
                DestroySafe(oldest);
            }
            foreach (RectTransform rt in _live)
                if (rt != null) rt.anchoredPosition += new Vector2(0f, LineGap);

            var go = new GameObject("Toast", typeof(RectTransform));
            go.transform.SetParent(layer, false);
            var self = (RectTransform)go.transform;
            self.anchorMin = self.anchorMax = new Vector2(0.5f, 0.55f);
            self.pivot = new Vector2(0.5f, 0.5f);
            self.sizeDelta = new Vector2(640f, 44f);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 26;
            label.alignment = TextAlignmentOptions.Center;
            label.richText = true;
            label.raycastTarget = false;
            ApplyFont(label);

            _live.Add(self);
            _ = FadeLoop(self, label);
        }

        private static async Task FadeLoop(RectTransform rt, TMP_Text label)
        {
            float t = 0f;
            while (t < LifeSec)
            {
                await Task.Yield();
                if (rt == null) return;
                float dt = Time.deltaTime > 0f ? Time.deltaTime : 0.016f;   // 编辑期 deltaTime 为 0 → 按 tick 估
                t += dt;
                rt.anchoredPosition += new Vector2(0f, RisePx * dt / LifeSec);
                float k = t / LifeSec;
                if (k > 0.6f && label != null) label.alpha = Mathf.Clamp01(1f - (k - 0.6f) / 0.4f);
            }
            _live.Remove(rt);
            DestroySafe(rt);
        }

        /// <summary>字体复用场景中已打开文本的 TMP 字体(含中文字形;同 ItemTipsView.ApplyFont 约定)。</summary>
        private static void ApplyFont(TextMeshProUGUI t)
        {
            if (_font == null)
            {
                foreach (TextMeshProUGUI candidate in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
                {
                    if (candidate != t) { _font = candidate.font; _fontMat = candidate.fontSharedMaterial; break; }
                }
            }
            if (_font != null) t.font = _font;
            if (_fontMat != null) t.fontSharedMaterial = _fontMat;
        }

        private static void DestroySafe(RectTransform rt)
        {
            if (rt == null) return;
            if (Application.isPlaying) Object.Destroy(rt.gameObject);
            else Object.DestroyImmediate(rt.gameObject);
        }
    }
}
