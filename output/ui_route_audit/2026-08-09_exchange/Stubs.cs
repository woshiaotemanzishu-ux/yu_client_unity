using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine
{
    public class Object { }
    public class GameObject : Object { public void SetActive(bool active) { } }
    public class Component : Object
    {
        public GameObject gameObject = new GameObject();
        public T GetComponentInChildren<T>(bool includeInactive) where T : Component => null;
    }
    public class MonoBehaviour : Component { }
}

namespace UnityEngine.UI
{
    public class Image : UnityEngine.Component { public bool raycastTarget; }
}

namespace TMPro
{
    public class TMP_InputField : UnityEngine.Component { public string text; }
    public class TextMeshProUGUI : UnityEngine.Component { public string text; }
}

namespace Shenxiao.Framework.UI
{
    public abstract class BaseView : UnityEngine.MonoBehaviour
    {
        public bool IsShown { get; protected set; }
        protected virtual void OnInit() { }
        protected virtual void OnShow(object args) { }
        protected virtual void OnHide() { }
        protected virtual void OnDispose() { }
    }
}

namespace Shenxiao.Generated.UI.Exchange
{
    public class ExchangeGiftViewBind : Shenxiao.Framework.UI.BaseView
    {
        public TMPro.TextMeshProUGUI _lb_error;
        public TMPro.TextMeshProUGUI _lb_url;
        public UnityEngine.Component _btn_receive;
        public TMPro.TMP_InputField _input_text;
    }
}

namespace Shenxiao.Common.Tips
{
    public static class TipsManager { public static void Toast(string text) { } }
}

namespace Shenxiao.Framework.Event
{
    public static class GlobalEvent
    {
        public const string EVT_GIFT_CARD_RESULT = "EVT_GIFT_CARD_RESULT";
    }

    public static class EventDispatcher
    {
        public static void On<T1, T2>(string evt, Action<T1, T2> handler) { }
        public static void Off<T1, T2>(string evt, Action<T1, T2> handler) { }
    }
}

namespace Shenxiao.Framework.Util
{
    public static class UIUtil
    {
        public static void AddClick(UnityEngine.UI.Image target, Action action) { }
    }

    public static class TimeUtil
    {
        public static Task Delay(int milliseconds) => Task.CompletedTask;
    }
}

namespace Shenxiao.Module.Core.Bag
{
    public sealed class BagController
    {
        public static readonly BagController Instance = new BagController();
        public void SendGiftCard(string cardNo) { }
    }
}
