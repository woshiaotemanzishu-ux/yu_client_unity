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
    /// Toast/Float 对标老端 sysInfo 链(Message.show → APPEND_MSG → SysInfoType.MINI → SysInfoMiniMgr 滚动条):
    /// 屏中偏上浮动文字,多条向上顶推,~2.2s 上浮渐隐;UI 层未就绪(headless/启动早期)自动退回 log-only。
    /// 始终写 GameLog(供 CLI/无头验证断言)。
    /// Confirm 仍是 Phase 0 壳(log + 直接走 onYes)——老端 Alert.Show 双按钮确认框未移植,调用方知悉此语义。
    /// </summary>
    public static class TipsManager
    {
        private const int MaxLines = 5;      // 同屏上限,超出顶掉最旧(对标 SysInfoMiniMgr 队列从简)
        private const float LifeSec = 2.2f;  // 单条存活
        private const float RisePx = 90f;    // 存活期内总上浮
        private const float LineGap = 46f;   // 新条入场时旧条整体上移一档

        private static readonly List<RectTransform> _live = new List<RectTransform>();
        private static TMP_FontAsset _font;
        private static Material _fontMat;

        public static void Toast(string text)
        {
            GameLog.Info("Tip", "toast: {0}", text);
            ShowFloating(text);
        }

        public static void Float(string text)
        {
            GameLog.Info("Tip", "float: {0}", text);
            ShowFloating(text);
        }

        public static void Confirm(string text, System.Action onYes, System.Action onNo = null)
        {
            GameLog.Info("Tip", "confirm: {0}", text);
            onYes?.Invoke();
        }

        // ---- 浮动条实现(对标 SysInfoMiniMgr 的 mini 消息;结构代码建、样式从简)----

        private static void ShowFloating(string text)
        {
            Transform layer = ViewManager.GetLayer(UILayer.Tip);
            if (layer == null) return;   // UI 层未 Init(headless/启动早期):log-only

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
