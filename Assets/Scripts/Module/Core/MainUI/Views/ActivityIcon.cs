using System.Collections;
using System.Threading.Tasks;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
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

        // 宽横幅上下浮动(当前用于首充新号形态，对标老端 FirstRechargeBubble._box_icon 的 POSY pingpong)。
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

        // 图标描述/倒计时(对标老端 ActivityIcon.SetCountTimer):Time 是服务器下发的绝对 Unix 秒。
        private static readonly Color DescriptionRunColor = new Color(0.533f, 1f, 0.263f); // #88ff43
        private static readonly Color DescriptionEndColor = new Color(0.996f, 0.102f, 0.102f); // #fe1a1a
        private Coroutine _countdown;
        private long _countdownEndTime;
        private bool _countdownEnabled;

        // 首充气泡复合体只建一次(159 图标:把通用单图标替换成 bg+横幅)。
        private bool _bubbleBuilt;
        private RectTransform _bubbleRoot;
        private TextMeshProUGUI _bubbleTimeLabel;
        private Coroutine _bubbleCountdown;

        // 图标特效(对标老端 ActivityIcon.SetIconEffect:cfg.effect_name 有值时 AddUIEffect 到 _box_effect2)。
        private UIEffectStage.Handle _effect;
        private string _effectName;
        private bool _effectClicked; // 对标老端 is_clicked:effect_need_delete 特效点过一次后本局不再挂

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
            SetFloatEnabled(false);
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
            // 特效跟随图标显隐(对标老端 SetLayer 的显隐代理不变量:图标隐→特效必隐)。
            // 此前"带特效图标拒绝隐藏"的验证期 hack 已拆:它让特效图标滞留旧槽位盖到别的图标上,
            // 是满解锁号"特效乱/两账号不一样"观感的放大器。
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
            ((RectTransform)transform).anchoredPosition = _floatBase; // 归位到布局基准位
        }

        private void SetFloatEnabled(bool enabled)
        {
            if (_floatEnabled == enabled)
            {
                if (enabled) StartFloat();
                return;
            }

            StopFloat();
            _floatEnabled = enabled;
            if (enabled) StartFloat();
        }

        // 折叠(_gp_con 失活)时 Unity 会停掉浮动协程,但 _float 引用还在;展开(SetActive true)时若不清引用,
        // StartFloat 会被 _float!=null 挡住 → 首充气泡(159)折叠一次后就不再浮动。用 OnDisable 清引用、OnEnable 按需重启,
        // 与"显隐走 SetVisible 还是 SetActive"无关地兜住(槽位式下图标是靠 gameObject.SetActive 复活的)。
        private void OnEnable()
        {
            if (_floatEnabled) StartFloat();
            if (_countdownEnabled && _countdown == null) ResumeCountdown();
            if (IsFirstRechargeBubble && _bubbleCountdown == null) StartBubbleCountdown();
        }

        private void OnDisable()
        {
            _float = null; // 失活时协程已被 Unity 停,只清残留引用(无需 StopCoroutine)
            _countdown = null; // 同上;保留结束时间,展开/复用时 OnEnable 会按服务器时间续跑
            _bubbleCountdown = null;
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

            // 入口形态来自业务状态，不再把 icon_type=159 永久解释成横幅。
            SetFloatEnabled(IsFirstRechargeBubble);
            SetBubbleVisible(IsFirstRechargeBubble);
            if (IsFirstRechargeBubble)
            {
                await BuildFirstRechargeBubbleAsync();
                StartBubbleCountdown();
                return;
            }

            StopBubbleCountdown();
            HideOptionalState();
            if (_img_icon != null) _img_icon.gameObject.SetActive(true);
            await SetIconImgAsync();
            if (this == null) return;
            RefreshDescription();
            RefreshBadges();
            await RefreshEffectAsync();
        }

        private bool IsFirstRechargeBubble =>
            _info != null && _info.Presentation == ActivityIconManager.IconPresentation.WideBanner;

        private void SetBubbleVisible(bool visible)
        {
            if (_bubbleRoot != null) _bubbleRoot.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 渲染首充气泡复合体(对标老端 FirstRechargeBubble._box_icon:_img_bg + “领取新服优势”横幅图)。
        /// 老端把它做成独立 view;Unity 这里复用 159 图标的位置/浮动/收放/生命周期,只把单图标替换成气泡复合体。
        /// 贴图已预拷进 Assets/GameRes/resource/game/recharge/texture(mainui_ui_56|57,Sprite 类型);打真机包前需跑 Addressable 自动分组登记这两个 key。
        /// 绿色倒计时直接消费 IconInfo.Time（由 13001.reg_time + 老端 30 分钟展示窗得出）。
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

            if (_bubbleBuilt)
            {
                SetBubbleVisible(true);
                return;
            }
            _bubbleBuilt = true;

            // _box_icon 容器:老端 260×129,左上锚,从图标左上角向右下展开(浮动/布局作用在本图标根,复合体随之移动)。
            // 老端 x:15 偏移(FirstRechargeBubble.scene _box_icon),这里照搬。
            RectTransform box = NewBubbleChild("_box_icon", transform, new Vector2(260f, 129f), new Vector2(15f, 0f));
            _bubbleRoot = box;
            // 老端 RefViewPos 把整个 view scale(0.9)。缩放放在 _box_icon 容器上,而不是图标根——
            // 因为 MainUISecondaryView.RefreshIconPos 每帧对根 SetScale(1f),放根上会被复位。
            box.localScale = new Vector3(0.9f, 0.9f, 1f);

            // _img_bg:mainui_ui_56(蓝底横幅 + “首充”徽章),260×88,老端 y=41。点击气泡 → OnClick(老端 _img_bg 点击打开首充)。
            Image bg = NewBubbleImage("_img_bg", box, new Vector2(260f, 88f), new Vector2(0f, -41f));
            bg.raycastTarget = true;
            UIUtil.AddClick(bg, OnClick);

            // 横幅:mainui_ui_57(“领取新服优势”烤字图),原生尺寸,老端 x=78 y=54。
            Image banner = NewBubbleImage("_img_banner", box, Vector2.zero, new Vector2(78f, -54f));

            // 老端 _lb_time:x=135,y=92,anchorX=.5,fontSize=20,color=#b3ff48，格式 mm:ss.ms。
            _bubbleTimeLabel = NewBubbleText("_lb_time", box, new Vector2(120f, 26f), new Vector2(135f, -92f));
            _bubbleTimeLabel.fontSize = 20f;
            _bubbleTimeLabel.color = new Color32(0xB3, 0xFF, 0x48, 0xFF);
            _bubbleTimeLabel.alignment = TextAlignmentOptions.Center;
            if (_lb_desc != null && _lb_desc.font != null)
            {
                _bubbleTimeLabel.font = _lb_desc.font;
                _bubbleTimeLabel.fontSharedMaterial = _lb_desc.fontSharedMaterial;
            }

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

        private static TextMeshProUGUI NewBubbleText(string name, Transform parent, Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            rt.localScale = Vector3.one;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            return text;
        }

        private void StartBubbleCountdown()
        {
            StopBubbleCountdown();
            if (_bubbleTimeLabel == null || _info == null || _info.Time <= 0 || !gameObject.activeInHierarchy) return;
            _bubbleCountdown = StartCoroutine(BubbleCountdownRoutine(_info.Time));
        }

        private void StopBubbleCountdown()
        {
            if (_bubbleCountdown == null) return;
            StopCoroutine(_bubbleCountdown);
            _bubbleCountdown = null;
        }

        private IEnumerator BubbleCountdownRoutine(long endTime)
        {
            while (true)
            {
                long leftMs = endTime * 1000L - TimeUtil.NowMs();
                if (leftMs <= 0)
                {
                    _bubbleTimeLabel.text = "00:00.00";
                    _bubbleCountdown = null;
                    yield break;
                }

                long minutes = leftMs / 60000L % 60L;
                long seconds = leftMs / 1000L % 60L;
                long hundredths = leftMs % 1000L / 10L;
                _bubbleTimeLabel.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, hundredths);
                yield return null;
            }
        }

        /// <summary>
        /// 对标老端 ActivityIcon.SetIconEffect:cfg.effect_name 非空时把特效挂到 _box_effect2(持久循环,如超值礼包高亮)。
        /// scale 取老端的「框相对」算法(参考 MainUITaskItem 工作样板:1280/框高)。如显示偏大/偏小,这是该类特效的统一校准点。
        /// </summary>
        private async Task RefreshEffectAsync()
        {
            string eff = _cfg != null ? _cfg.EffectName : null;
            if (string.IsNullOrEmpty(eff)) { ClearEffect(); return; }
            // 对标老端 ActivityIcon.RefreshIcon 的 !is_clicked 闸:effect_need_delete 的特效点过一次后本局不再挂。
            if (_effectClicked && _cfg != null && _cfg.EffectNeedDelete) { ClearEffect(); return; }
            RectTransform host = EnsureEffectBox(); // 烤制把 inactive 的 box_effect2 裁掉了→克隆出的图标字段为 null,这里按名找/缺则建
            if (host == null) { ClearEffect(); return; }
            if (_effectName == eff && _effect != null)
            {
                // RefreshAsync 每次都先 HideOptionalState 把 _box_effect2 关掉,同特效已挂时这里要把盒补激活
                // (否则「单独正常、放UI上全无」)。只激活特效盒,不动图标自身的显隐(那归 SetVisible/槽位管)。
                host.gameObject.SetActive(true);
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
            // 不再强制 SetActive(true):特效可见性完全跟随图标本身的显隐(SetVisible/槽位/折叠),
            // 对标老端"图标隐→特效必隐"不变量。此前的 FORCE-SHOWN hack 会盖过折叠/槽位隐藏,
            // 让特效滞留陈旧位置叠到别的图标上。
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
            StopBubbleCountdown();
            StopCountdown();
            ClearEffect();
        }

        private async Task SetIconImgAsync()
        {
            string iconName = !string.IsNullOrEmpty(_info?.IconImg) ? _info.IconImg : _cfg.IconName;
            if (string.IsNullOrEmpty(iconName)) return;

            string path = iconName.StartsWith("resource/") ? iconName : GameResPath.GetIcon("icon", iconName);
            await ResManager.SetImageAsync(_img_icon, path, nativeSize: false);
        }

        private void RefreshDescription()
        {
            // 老端 SetIconType 先开倒计时、再 SetIconText;有业务状态文字时由文字覆盖并取消倒计时。
            string text = _info != null ? _info.IconTxt : "";
            if (!string.IsNullOrEmpty(text))
            {
                SetIconText(text, DescriptionRunColor);
                return;
            }

            if (_cfg != null && _cfg.CountDownTime && _info != null && _info.Time > 0)
                StartCountdown(_info.Time);
        }

        private void StartCountdown(long endTime)
        {
            StopCountdown();
            _countdownEndTime = endTime;
            _countdownEnabled = true;
            ResumeCountdown();
        }

        private void ResumeCountdown()
        {
            if (!_countdownEnabled || !gameObject.activeInHierarchy) return;
            TickCountdown();
            if (_countdownEnabled && _countdown == null)
                _countdown = StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            var wait = new WaitForSecondsRealtime(1f);
            while (_countdownEnabled)
            {
                yield return wait;
                TickCountdown();
            }
            _countdown = null;
        }

        private void TickCountdown()
        {
            long left = _countdownEndTime - TimeUtil.NowSec();
            if (left > 0)
            {
                SetIconText(FormatCountdown(left), DescriptionRunColor);
                return;
            }

            _countdownEnabled = false;
            _countdownEndTime = 0;
            SetIconText("已结束", DescriptionEndColor);

            // 对标老端 OnTimer:倒计时结束后,not_delete=false 的入口由 ActivityIconManager 回收。
            if (_cfg != null && !_cfg.NotDelete && !string.IsNullOrEmpty(_iconType))
                ActivityIconManager.Instance.DeleteIcon(_iconType);
        }

        private void StopCountdown()
        {
            _countdownEnabled = false;
            _countdownEndTime = 0;
            if (_countdown == null) return;
            StopCoroutine(_countdown);
            _countdown = null;
        }

        // 对标老端 TimeUtil.convertTimeColor:有天数时显示“D天H时”,不足一天显示 HH:mm:ss。
        private static string FormatCountdown(long seconds)
        {
            long days = seconds / 86400;
            long hours = seconds / 3600 % 24;
            if (days > 0) return hours > 0 ? days + "天" + hours + "时" : days + "天";

            long minutes = seconds / 60 % 60;
            long secs = seconds % 60;
            return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, secs);
        }

        private void SetIconText(string text, Color? color = null)
        {
            bool show = !string.IsNullOrEmpty(text);
            if (_lb_desc != null)
            {
                _lb_desc.text = show ? text : "";
                if (color.HasValue) _lb_desc.color = color.Value;
                _lb_desc.gameObject.SetActive(show);
            }
            if (_img_desc_bg != null) _img_desc_bg.gameObject.SetActive(show);
        }

        /// <summary>
        /// 红点/数字角标统一消费 ActivityIconManager.IconInfo，不在 View 里按活动类型写判断。
        /// 具体业务只需在收到服务端状态后调用 SetIconRedDot/SetIconBadge。
        /// </summary>
        private void RefreshBadges()
        {
            int count = _info != null ? _info.BadgeCount : 0;
            bool showNumber = count > 0;
            bool showRed = !showNumber && _info != null && _info.RedDot;

            SetGraphicVisible(_img_red, showRed);
            SetGraphicVisible(_img_red_num, showNumber);
            if (_lb_num != null)
            {
                _lb_num.text = showNumber ? count.ToString() : "";
                _lb_num.gameObject.SetActive(showNumber);
            }
        }

        private void HideOptionalState()
        {
            StopCountdown();
            SetIconText("");
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
            // 对标老端 ClickEvent 的 effect_need_delete 分支:点过一次即清特效并本局不再挂(is_clicked)。
            // 未移植时"超值礼包转圈"等特效永不消失,是特效观感乱的成因之一。
            if (_effect != null && _cfg != null && _cfg.EffectNeedDelete)
            {
                _effectClicked = true;
                ClearEffect();
            }

            // 盒入口图标(is_box,如 100 福利大厅 / 101 寻宝):点开收纳盒弹窗铺出里面的成员,而非路由到某个功能面板
            // (对标老端 ActivityIcon.ClickEvent 的 cfg.is_box 分支:OPEN_VIEW 'MainUIIconBoxView', icon_type, global_pos)。
            if (_cfg != null && _cfg.IsBox)
            {
                string boxType = !string.IsNullOrEmpty(_cfg.IconType) ? _cfg.IconType : _iconType;
                MainUIIconBoxView.OpenFor(boxType, transform.position);
                return;
            }

            string key = _cfg != null && !string.IsNullOrEmpty(_cfg.IconType) ? _cfg.IconType : _iconType;
            MainUIRouter.Open(key);
        }
    }
}
