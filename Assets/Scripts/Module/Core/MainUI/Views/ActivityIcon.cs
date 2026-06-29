using System.Collections;
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

        // 首充气泡上下浮动(对标老端 FirstRechargeBubble._box_icon 的 POSY pingpong:向上 10px、单程 1.5s、循环)。
        // 老端只有首充气泡这一个图标会浮动,故按图标类型自启;布局每帧改 anchoredPosition,浮动只在布局基准位上叠加偏移。
        private const string FloatIconType = "159";
        private const float FloatDistance = 10f;
        private const float FloatDuration = 1.5f;

        private string _iconType;
        private MainUIConfigs.FunctionIconCfg _cfg;
        private ActivityIconManager.IconInfo _info;
        private CanvasGroup _canvasGroup;
        private bool _clickBound;

        // 浮动状态:_floatBase 是布局给的基准 anchoredPosition,协程在其上叠加上下偏移。
        private Coroutine _float;
        private Vector2 _floatBase;
        private bool _floatEnabled;

        // 首充气泡复合体只建一次(159 图标:把通用单图标替换成 bg+横幅)。
        private bool _bubbleBuilt;

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
            _floatEnabled = iconType == FloatIconType;
            _ = RefreshAsync();
        }

        public void Refresh()
        {
            _ = RefreshAsync();
        }

        public void SetPosition(float x, float y)
        {
            RectTransform rt = (RectTransform)transform;
            // 记录布局基准位:浮动协程读取它叠加偏移。布局刷新改基准位时,正在跑的协程下一帧自动跟随。
            _floatBase = new Vector2(x, -y);
            // 浮动中不直接写 anchoredPosition——交给协程(否则布局刷新会把当帧偏移抹掉,产生一帧抖动)。
            if (_float == null) rt.anchoredPosition = _floatBase;
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
            // 首充气泡:显示时启浮动,隐藏时停并归位(GameObject 失活也会停协程,这里显式收尾保证归位)。
            if (visible) StartFloat();
            else StopFloat();
        }

        // 上下浮动协程(对标老端 FirstRechargeBubble._box_icon 的 POSY pingpong),沿用 ArrowComponent.BobRoutine 同款三角波。
        private void StartFloat()
        {
            if (!_floatEnabled || _float != null) return;
            if (!gameObject.activeInHierarchy) return; // 失活物体不能起协程
            _float = StartCoroutine(FloatRoutine());
        }

        private void StopFloat()
        {
            if (_float != null) { StopCoroutine(_float); _float = null; }
            if (_floatEnabled) ((RectTransform)transform).anchoredPosition = _floatBase; // 归位到布局基准位
        }

        private IEnumerator FloatRoutine()
        {
            RectTransform rt = (RectTransform)transform;
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime / FloatDuration;
                float ping = Mathf.PingPong(t, 1f); // 0→1→0 三角波,单程 FloatDuration 秒
                rt.anchoredPosition = _floatBase + new Vector2(0f, FloatDistance * ping); // 向上浮 10px
                yield return null;
            }
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

            // 首充气泡(159):不走通用单图标,渲染老端 FirstRechargeBubble 的复合气泡(bg+横幅)。
            if (IsFirstRechargeBubble)
            {
                await BuildFirstRechargeBubbleAsync();
                return;
            }

            await SetIconImgAsync();
            SetIconText(_info != null ? _info.IconTxt : "");
            HideOptionalState();
            await RefreshEffectAsync();
        }

        // “159” = 首充气泡,与 FloatIconType 同一个图标:用作渲染分支判断(_floatEnabled 仅表达浮动语义)。
        private bool IsFirstRechargeBubble => _iconType == FloatIconType;

        /// <summary>
        /// 渲染首充气泡复合体(对标老端 FirstRechargeBubble._box_icon:_img_bg + “领取新服优势”横幅图)。
        /// 老端把它做成独立 view;Unity 这里复用 159 图标的位置/浮动/收放/生命周期,只把单图标替换成气泡复合体。
        /// 贴图已预拷进 Assets/GameRes/resource/game/recharge/texture(mainui_ui_56|57,Sprite 类型);打真机包前需跑 Addressable 自动分组登记这两个 key。
        /// 绿色倒计时(老端 _lb_time)依赖 bubble_time(创角+30min),Unity 暂无该数据源,留作后续。
        /// </summary>
        private async Task BuildFirstRechargeBubbleAsync()
        {
            // 隐藏通用单图标视觉(72px 图标 / 描述 / 红点 / 箭头),改用气泡复合体。
            // 先跑 HideOptionalState(它会把 _box_arrow 重新激活),再统一关掉,避免被它复位。
            HideOptionalState();
            if (_img_icon != null) _img_icon.gameObject.SetActive(false);
            if (_lb_desc != null) _lb_desc.gameObject.SetActive(false);
            if (_img_desc_bg != null) _img_desc_bg.gameObject.SetActive(false);
            if (_box_arrow != null) _box_arrow.gameObject.SetActive(false);

            if (_bubbleBuilt) return;
            _bubbleBuilt = true;

            // _box_icon 容器:老端 260×129,左上锚,从图标左上角向右下展开(浮动/布局作用在本图标根,复合体随之移动)。
            // 老端 x:15 偏移(FirstRechargeBubble.scene _box_icon),这里照搬。
            RectTransform box = NewBubbleChild("_box_icon", transform, new Vector2(260f, 129f), new Vector2(15f, 0f));
            // 老端 RefViewPos 把整个 view scale(0.9)。缩放放在 _box_icon 容器上,而不是图标根——
            // 因为 MainUISecondaryView.RefreshIconPos 每帧对根 SetScale(1f),放根上会被复位。
            box.localScale = new Vector3(0.9f, 0.9f, 1f);

            // _img_bg:mainui_ui_56(蓝底横幅 + “首充”徽章),260×88,老端 y=41。点击气泡 → OnClick(老端 _img_bg 点击打开首充)。
            Image bg = NewBubbleImage("_img_bg", box, new Vector2(260f, 88f), new Vector2(0f, -41f));
            bg.raycastTarget = true;
            UIUtil.AddClick(bg, OnClick);

            // 横幅:mainui_ui_57(“领取新服优势”烤字图),原生尺寸,老端 x=78 y=54。
            Image banner = NewBubbleImage("_img_banner", box, Vector2.zero, new Vector2(78f, -54f));

            // 贴图已预拷进 GameRes/recharge/texture(并随包跑 Addressable 自动分组);编辑器下未分组时 ResManager 兜底直接从工程/yu_client 加载。
            // await 是为了被销毁(折叠/重建)时 SetImageAsync 内部的 image==null 守卫能早退,不对已销毁节点赋图。
            await ResManager.SetImageAsync(bg, GameResPath.GetIcon("recharge", "mainui_ui_56"), false, false);
            if (this == null) return;
            await ResManager.SetImageAsync(banner, GameResPath.GetIcon("recharge", "mainui_ui_57"), false, true);
        }

        // 气泡子节点统一用左上锚(对标 Laya 顶左原点、y 向下 → Unity anchoredPosition.y 取负)。
        private static RectTransform NewBubbleChild(string name, Transform parent, Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            rt.localScale = Vector3.one;
            return rt;
        }

        private static Image NewBubbleImage(string name, Transform parent, Vector2 size, Vector2 anchoredPos)
        {
            RectTransform rt = NewBubbleChild(name, parent, size, anchoredPos);
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            return img;
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
