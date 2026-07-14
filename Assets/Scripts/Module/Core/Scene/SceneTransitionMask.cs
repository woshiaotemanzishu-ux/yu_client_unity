using System.Threading.Tasks;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 跨场景切换的全屏黑幕过渡(对标老端切场景 loading 遮罩)。
    /// 12005 场景确认后立即压黑(Show),地图+主角就绪(EVT_SCENE_MAP_READY 之后)渐隐(Hide)。
    /// 没有它时,退副本/飞鞋切场景表现为"角色瞬移 + 全场实体重刷",玩家会误读成断线重连
    /// (test.log 实证:副本 50000 与野外共用 mapResId=10000,连地图都不换,切换毫无过渡)。
    /// 兜底:Show 后 <see cref="AUTO_HIDE_SEC"/> 秒未 Hide 自动渐隐,防地图加载失败黑屏卡死。
    /// </summary>
    public static class SceneTransitionMask
    {
        private const float FADE_OUT_SEC = 0.35f;
        private const float AUTO_HIDE_SEC = 8f;

        private static CanvasGroup _group;
        private static int _serial;   // 每次 Show/Hide 递增,旧的淡出/兜底协程失效

        public static void Show()
        {
            if (!Application.isPlaying) return;
            Transform layer = ViewManager.GetLayer(UILayer.Loading);
            if (layer == null) return;   // headless/CLI:无 UI 层,只做数据流不做表现

            EnsureMask(layer);
            if (_group == null) return;
            int serial = ++_serial;
            _group.gameObject.SetActive(true);
            _group.alpha = 1f;
            _group.blocksRaycasts = true;   // 压黑期间挡输入,防切场景半程误点
            _ = AutoHideAsync(serial);
        }

        public static void Hide()
        {
            if (_group == null || !_group.gameObject.activeSelf) return;
            int serial = ++_serial;
            _group.blocksRaycasts = false;
            _ = FadeOutAsync(serial);
        }

        private static void EnsureMask(Transform layer)
        {
            if (_group != null) return;   // Unity fake-null:退 Play 销毁后自动重建

            var go = new GameObject("SceneTransitionMask", typeof(RectTransform));
            go.transform.SetParent(layer, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = go.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = true;

            _group = go.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            go.SetActive(false);
        }

        private static async Task FadeOutAsync(int serial)
        {
            float t = 0f;
            while (t < FADE_OUT_SEC)
            {
                await Task.Yield();
                if (_group == null || serial != _serial) return;
                float dt = Time.deltaTime > 0f ? Time.deltaTime : 0.016f;
                t += dt;
                _group.alpha = Mathf.Clamp01(1f - t / FADE_OUT_SEC);
            }
            if (_group != null && serial == _serial) _group.gameObject.SetActive(false);
        }

        private static async Task AutoHideAsync(int serial)
        {
            await Shenxiao.Framework.Util.TimeUtil.Delay((int)(AUTO_HIDE_SEC * 1000)); // Task.Delay 在 WebGL 永不醒
            if (_group == null || serial != _serial || !_group.gameObject.activeSelf) return;
            GameLog.Warn("Scene", "场景过渡黑幕 {0}s 未收到隐藏信号(地图加载失败/中断?)→ 兜底渐隐", AUTO_HIDE_SEC);
            Hide();
        }
    }
}
