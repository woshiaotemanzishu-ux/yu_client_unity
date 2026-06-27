using System.Threading.Tasks;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Res;
using Shenxiao.Common.UI3D;
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

        // 图标特效(对标老端 ActivityIcon.SetIconEffect:cfg.effect_name 有值时 AddUIEffect 到 _box_effect2)。
        private UIEffectStage.Handle _effect;
        private string _effectName;

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
            // 【最小验证措施】带特效的图标拒绝被隐藏:活动区当前是「烤制静态图 + 动态图标被折叠/重建隐藏」的坏混合,
            // 动态图标(带特效)会被 HideAllIcons/折叠盖掉 → 看不到特效。这里让带特效的图标常显,先肉眼确认环特效在真UI上成立。
            // 注:这是验证期权宜,正解是修活动视图(动态图标接管 + 清掉烤出的静态图),确认后移除本特判。
            if (!visible && _effect != null) return;
            gameObject.SetActive(visible);
        }

        private async Task RefreshAsync()
        {
            if (string.IsNullOrEmpty(_iconType)) return;
            await MainUIConfigs.EnsureLoaded();
            _cfg = MainUIConfigs.GetFunctionIconCfg(_iconType);
            _info = ActivityIconManager.Instance.GetIconInfo(_iconType);
            UIEffectStage.Note(string.Format("ActivityIcon[{0}] cfgFound={1} effectName='{2}' box2={3}",
                _iconType, _cfg != null, _cfg != null ? _cfg.EffectName : "<no-cfg>", _box_effect2 != null));
            if (_cfg == null) return;

            await SetIconImgAsync();
            SetIconText(_info != null ? _info.IconTxt : "");
            HideOptionalState();
            await RefreshEffectAsync();
        }

        /// <summary>
        /// 对标老端 ActivityIcon.SetIconEffect:cfg.effect_name 非空时把特效挂到 _box_effect2(持久循环,如超值礼包高亮)。
        /// scale 取老端的「框相对」算法(参考 MainUITaskItem 工作样板:1280/框高)。如显示偏大/偏小,这是该类特效的统一校准点。
        /// </summary>
        private async Task RefreshEffectAsync()
        {
            string eff = _cfg != null ? _cfg.EffectName : null;
            if (string.IsNullOrEmpty(eff)) { ClearEffect(); return; }
            RectTransform host = EnsureEffectBox(); // 烤制把 inactive 的 box_effect2 裁掉了→克隆出的图标字段为 null,这里按名找/缺则建
            if (host == null) { ClearEffect(); return; }
            if (_effectName == eff && _effect != null)
            {
                // 真因:RefreshAsync 每次都先调 HideOptionalState 把 _box_effect2 SetActive(false)。
                // 同特效已挂时本方法早退→盒被关着不再激活→特效盒 inactive→RawImage 不渲染(「单独正常、放UI上全无」的真根因)。
                // 这里每次刷新都把盒/图标补激活回来。
                host.gameObject.SetActive(true);
                gameObject.SetActive(true);
                return;
            }
            ClearEffect();

            host.gameObject.SetActive(true);
            _effectName = eff;
            // 对标老端 ActivityIcon.SetIconEffect:scale = 12.8 * cfg.effect_scale(定值,与框尺寸无关;
            // 相机框定 12.8 世界单位、RT 取框尺寸,与老端一致)。之前的 1280/box*0.96 漏乘 effect_scale 且多了 0.96,偏小约 25%。
            float effectScale = _cfg != null && _cfg.EffectScale > 0f ? _cfg.EffectScale : 1f;
            float s = 12.8f * effectScale;
            UIEffectStage.Note(string.Format("ActivityIcon[{0}] SPAWN eff='{1}' scale={2:0.##} (effScale={3:0.##})", _iconType, eff, s, effectScale));
            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(eff, host, Vector2.zero, new Vector3(s, s, s));
            if (this == null || _effectName != eff)
            {
                UIEffectStage.Note(string.Format("ActivityIcon[{0}] spawned-then-DISPOSED (thisNull={1} effChanged={2})",
                    _iconType, this == null, _effectName != eff));
                handle?.Dispose();
                return;
            }
            _effect = handle;
            // 【最小验证】带特效的动态图标强制现身,盖过烤制静态图,让环特效在真UI上可见。
            gameObject.SetActive(true);
            UIEffectStage.Note(string.Format("ActivityIcon[{0}] FORCE-SHOWN (effect attached, overriding fold/baked hide)", _iconType));
        }

        private void ClearEffect()
        {
            if (_effect != null) { _effect.Dispose(); _effect = null; }
            _effectName = null;
        }

        /// <summary>
        /// 取/兜底建图标特效盒(对标老端 _box_effect2)。烤制器把快照里 inactive 的 box_effect2 裁掉了,
        /// 克隆出的图标 _box_effect2 字段恒为 null(同 [[baked-list-items-need-per-item-bind]] 家族),
        /// 导致环特效(超值礼包等 ui_zhujiemianzhuanquan)压根不 spawn。故:① 字段非空直接用;② 按名递归找回;
        /// ③ 仍无则按预制体几何(100×100、左上锚、(-14,14))新建一个,保证特效永远有挂点,不依赖烤制把它绑对。
        /// </summary>
        private RectTransform EnsureEffectBox()
        {
            if (_box_effect2 != null) return _box_effect2;

            Transform found = FindDeep(transform, "_box_effect2") ?? FindDeep(transform, "box_effect2");
            if (found is RectTransform existing)
            {
                _box_effect2 = existing;
                return existing;
            }

            var go = new GameObject("_box_effect2", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(100f, 100f);
            rt.anchoredPosition = new Vector2(-14f, 14f);
            rt.localScale = Vector3.one;
            _box_effect2 = rt;
            return rt;
        }

        private static Transform FindDeep(Transform root, string childName)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName) return child;
                Transform deeper = FindDeep(child, childName);
                if (deeper != null) return deeper;
            }
            return null;
        }

        private void OnDestroy()
        {
            ClearEffect();
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
            // 挂着特效时别把特效盒关掉(否则每次刷新都隐藏→RawImage 不渲染)。
            if (_box_effect2 != null && _effect == null) _box_effect2.gameObject.SetActive(false);
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
