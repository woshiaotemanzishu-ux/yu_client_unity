using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 场景级指针捕获(对标老客户端 Scene.ts 在 stage 上挂的 MOUSE_DOWN/MOVE/UP):
    /// 一张铺满屏幕的透明 raycast 接收板,挂在所有 UI 之后(canvas 最底层),通过 UGUI EventSystem
    /// 收指针事件写入 <see cref="SceneInput"/>。
    ///
    /// 为什么走 EventSystem 而不是 UnityEngine.Input:本工程 Active Input Handling = Input System Package,
    /// 直接读旧 Input 会每帧抛 InvalidOperationException 刷屏;EventSystem 用当前生效的输入模块派发,
    /// 与输入后端无关。"落在 UI 按钮上的点击不进摇杆"也自然成立——上层 UI 先吃掉射线,空白处才落到本板。
    ///
    /// 由 MainRoleFlow 在主角创建时 EnsureInstalled,清理主角时 Remove(无主角无移动)。
    /// </summary>
    public sealed class SceneInputDriver : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private static SceneInputDriver _instance;

        public static void EnsureInstalled()
        {
            if (_instance != null) return;

            Transform scene = ViewManager.GetLayer(UILayer.Scene);
            Transform canvas = scene != null ? scene.parent : null;
            if (canvas == null)
            {
                GameLog.Warn("Scene", "skip scene input: UI canvas is not ready");
                return;
            }

            var go = new GameObject("__SceneInputCatcher", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling(); // 置于所有 UI 层之后,只接没人要的空白区点击

            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f); // 全透明,只作 raycast 接收(raycastTarget 与 alpha 无关)
            img.raycastTarget = true;

            _instance = go.AddComponent<SceneInputDriver>();
        }

        public static void Remove()
        {
            if (_instance == null) return;
            SceneInput.End();
            Destroy(_instance.gameObject);
            _instance = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            SceneInput.Begin(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            SceneInput.Move(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SceneInput.End();
        }
    }
}
