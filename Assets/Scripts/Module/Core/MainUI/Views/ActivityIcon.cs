using System.Threading.Tasks;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Res;
using Shenxiao.Generated.UI.MainUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// MainUI activity icon item. Click routing, bubbles, and effects are separate system slices.
    /// </summary>
    public sealed class ActivityIcon : ActivityIconBind
    {
        public const float WIDTH = 72f;
        public const float HEIGHT = 72f;

        private string _iconType;
        private MainUIConfigs.FunctionIconCfg _cfg;
        private ActivityIconManager.IconInfo _info;
        private CanvasGroup _canvasGroup;
        private bool _clickBound;

        public string IconType => _iconType;
        public MainUIConfigs.FunctionIconCfg Cfg => _cfg;

        protected override void OnInit()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            HideOptionalState();
            BindClick();
        }

        public void SetIconType(string iconType)
        {
            _iconType = iconType;
            _ = RefreshAsync();
        }

        public void Refresh()
        {
            _ = RefreshAsync();
        }

        public void SetPosition(float x, float y)
        {
            RectTransform rt = (RectTransform)transform;
            rt.anchoredPosition = new Vector2(x, -y);
        }

        public void SetAlpha(float alpha)
        {
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = alpha;
        }

        public void SetScale(float scale)
        {
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private async Task RefreshAsync()
        {
            if (string.IsNullOrEmpty(_iconType)) return;
            await MainUIConfigs.EnsureLoaded();
            _cfg = MainUIConfigs.GetFunctionIconCfg(_iconType);
            _info = ActivityIconManager.Instance.GetIconInfo(_iconType);
            if (_cfg == null) return;

            await SetIconImgAsync();
            SetIconText(_info != null ? _info.IconTxt : "");
            HideOptionalState();
        }

        private async Task SetIconImgAsync()
        {
            string iconName = !string.IsNullOrEmpty(_info?.IconImg) ? _info.IconImg : _cfg.IconName;
            if (string.IsNullOrEmpty(iconName)) return;

            string path = iconName.StartsWith("resource/") ? iconName : GameResPath.GetIcon("icon", iconName);
            await ResManager.SetImageAsync(_img_icon, path, nativeSize: false);
        }

        private void SetIconText(string text)
        {
            bool show = !string.IsNullOrEmpty(text);
            if (_lb_desc != null)
            {
                _lb_desc.text = show ? text : "";
                _lb_desc.gameObject.SetActive(show);
            }
            if (_img_desc_bg != null) _img_desc_bg.gameObject.SetActive(show);
        }

        private void HideOptionalState()
        {
            SetGraphicVisible(_img_red, false);
            SetGraphicVisible(_img_red_num, false);
            SetTextVisible(_lb_num, false);
            if (_box_effect != null) _box_effect.gameObject.SetActive(false);
            if (_box_effect2 != null) _box_effect2.gameObject.SetActive(false);
            if (_box_arrow != null) _box_arrow.gameObject.SetActive(true);
        }

        private static void SetGraphicVisible(Graphic graphic, bool visible)
        {
            if (graphic == null) return;
            graphic.gameObject.SetActive(visible);
        }

        private static void SetTextVisible(TextMeshProUGUI text, bool visible)
        {
            if (text == null) return;
            if (!visible) text.text = "";
            text.gameObject.SetActive(visible);
        }

        private void BindClick()
        {
            if (_clickBound || _img_icon == null) return;
            UIUtil.AddClick(_img_icon, OnClick);
            _clickBound = true;
        }

        private void OnClick()
        {
            string key = _cfg != null && !string.IsNullOrEmpty(_cfg.IconType) ? _cfg.IconType : _iconType;
            MainUIRouter.Open(key);
        }
    }
}
