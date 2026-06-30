using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.AutoFight;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene.Vo;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 大妖(副本 BOSS)入场「大妖来袭」表现(对标老端 DungeonFightSceneMaskView +
    /// BaseDungeonController.ShowBossBornEffect,见 yu_client/h5/src/dungeon/DungeonFightSceneMaskView.ts /
    /// commonController/BaseDungeonController.ts:441-457,2184)。
    ///
    /// 触发:<see cref="MonsterRenderer"/> 收到怪入场调 <see cref="NotifyMonsterAdded"/>;BOSS(<see cref="MonsterVo.IsBoss"/>)
    /// 且在副本内(<see cref="RoleModel.DunId"/>!=0,野外世界 boss 不播)才播,每实例只播一次(切场景/清场重置)。
    ///
    /// 表现(完全复现老端):上下两条真·遮罩贴图(resource/game/dungeoncommon/texture/mask & mask1,黑色+alpha 渐变暗角)
    /// 从屏外滑入做电影留白 → 中央播 effect_ui_dayaolaixi 横幅("大妖来袭" 金色火焰横幅,纯粒子特效经 UIEffectStage 离屏合成)
    /// → 约 3s 后滑出收尾;期间临时暂停自动战斗。滑动由 <see cref="BossBornEffectPlayer"/> 逐帧驱动(Time.deltaTime),
    /// 不再用 Task.Delay 轮询(那会卡顿)。
    ///
    /// 关键修复(之前横幅不显示的根因):UIEffectStage 的 position 是【世界单位】(localPosition=-position),
    /// 之前传 (0,90) 把横幅顶到相机框(±6.4 单位)外 90 单位 → 看不见;且离屏相机框定 12.8 世界单位,横幅自然宽 ~15 单位,
    /// 之前 scale 1.5 过大溢出。现 position=(0,0)、scale≈0.7(实测 0.55~0.75 都对,0.7 火焰延展到屏幕两侧,贴合老端)。
    /// </summary>
    public static class BossBornEffectFlow
    {
        private static readonly HashSet<int> _shown = new HashSet<int>();
        private static bool _playing;
        private static bool _installed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (_installed) return;
            _installed = true;
            EventDispatcher.On(GlobalEvent.EVT_SCENE_OBJECTS_CLEARED, Reset);
            EventDispatcher.On(GlobalEvent.EVT_NET_DISCONNECTED, Reset);
        }

        /// <summary>怪入场回调(由 <see cref="MonsterRenderer"/> 在 vo 修正后调)。BOSS + 在副本内 + 未播过 → 播一次。</summary>
        public static void NotifyMonsterAdded(MonsterVo vo)
        {
            if (vo == null || !vo.IsBoss) return;
            if (RoleModel.Instance.DunId == 0) return; // 仅副本内(对标老端 IsShowBornEffectDungeonType)
            if (!_shown.Add(vo.InstanceId)) return;     // 已播过该实例
            Play(vo);
        }

        /// <summary>切场景/清场/断线:重置去重集(对标老端 SceneManager.START 时 show_born_effect_dic_=undefined)。</summary>
        public static void Reset() => _shown.Clear();

        internal static void NotifyPlayerFinished() => _playing = false;

        private static void Play(MonsterVo vo)
        {
            if (_playing) return; // 同帧多 BOSS 只播一次(对标老端 CURR_OPEN_VIEW 非空不再开)
            if (!(ViewManager.GetLayer(UILayer.Top) is RectTransform topLayer))
            {
                GameLog.Warn("Scene", "大妖来袭:Top 层不可用,跳过入场表现 ins={0}", vo.InstanceId);
                return;
            }

            _playing = true;
            var go = new GameObject("__BossBornEffect", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(topLayer, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            BossBornEffectPlayer player = go.AddComponent<BossBornEffectPlayer>();
            player.Begin(topLayer, vo);
            GameLog.Info("Scene", "大妖来袭:入场表现 play ins={0} type={1} name=\"{2}\"", vo.InstanceId, vo.TypeId, vo.Name);
        }
    }

    /// <summary>
    /// 「大妖来袭」单次播放器(挂在 Top 层一个满屏容器上,逐帧驱动黑幕滑入→横幅→滑出,放完自毁)。
    /// 逐帧动画(Update + Time.deltaTime)取代 Task.Delay 轮询,消除卡顿。
    /// </summary>
    public sealed class BossBornEffectPlayer : MonoBehaviour
    {
        // —— 可调常量(实测基线,待真机微调)——
        private const string BANNER_EFFECT = "effect_ui_dayaolaixi";
        private const string MASK_TOP = "resource/game/dungeoncommon/texture/mask";    // 黑色+alpha 渐变暗角(上)
        private const string MASK_BOTTOM = "resource/game/dungeoncommon/texture/mask1"; // 黑色+alpha 渐变暗角(下)
        private const float BANNER_SCALE = 0.7f;   // 离屏相机框 12.8 单位下,0.7 让火焰横幅延展到屏幕两侧(贴合老端)
        private const float BAR_ALPHA = 0.8f;      // 对标老端 mask 0.8
        private const float SLIDE_IN = 0.18f;      // 滑入(对标老端 0.15s)
        private const float HOLD = 2.6f;           // 横幅停留(对标老端 ~3000ms 窗口)
        private const float SLIDE_OUT = 0.18f;     // 滑出

        private enum Phase { Slidein, Hold, Slideout, Done }

        private RectTransform _topBar;
        private RectTransform _bottomBar;
        private RectTransform _host;
        private float _barH;
        private float _topHidden, _topShown, _botHidden, _botShown;
        private UIEffectStage.Handle _banner;
        private Phase _phase = Phase.Slidein;
        private float _t;
        private bool _ready;        // 遮罩贴图加载完、可以开始滑动
        private bool _bannerStarted;
        private bool _finished;

        private static Sprite _white;

        public void Begin(RectTransform topLayer, MonsterVo vo)
        {
            // 冻结演出:玩家攻击+移动寻路、怪跟位+朝向全停(对标老端 STOPAUTOFIGHT,横幅期间全停,结束才开打)。
            AutoFightModel.Instance.SetCombatFreeze(true);
            _ = SetupAsync(topLayer);
        }

        private async Task SetupAsync(RectTransform topLayer)
        {
            var self = (RectTransform)transform;
            float h = topLayer.rect.height > 1f ? topLayer.rect.height : 1280f;
            _barH = h * 0.5f + 30f; // 对标老端 mask_height = stageH*0.5+30(上下各半屏多一点)
            _topShown = 0f; _topHidden = _barH;
            _botShown = 0f; _botHidden = -_barH;

            _topBar = CreateBar(self, true);
            _bottomBar = CreateBar(self, false);
            SetBarY(_topBar, _topHidden);
            SetBarY(_bottomBar, _botHidden);

            _host = CreateHost(self); // 横幅满屏承载(最后建 → 叠在黑幕之上,火焰盖过暗角)

            // 先把真·遮罩贴图加载好再开始滑动,保证全程是渐变暗角而非纯黑块(加载失败则保留纯黑兜底)。
            // try/catch 保证即使加载抛异常 _ready 也会置真 → 动画照常推进 → Finish 解冻,绝不让冻结卡死。
            try
            {
                await Task.WhenAll(ApplyMask(_topBar, MASK_TOP), ApplyMask(_bottomBar, MASK_BOTTOM));
            }
            catch (System.Exception e)
            {
                GameLog.Warn("Scene", "大妖来袭:遮罩贴图加载异常(保留纯黑兜底):{0}", e.Message);
            }
            if (_finished || this == null) return;
            _ready = true;
        }

        private void Update()
        {
            if (!_ready || _finished) return;
            _t += Time.deltaTime;

            switch (_phase)
            {
                case Phase.Slidein:
                {
                    float p = Mathf.Clamp01(_t / SLIDE_IN);
                    float e = Mathf.SmoothStep(0f, 1f, p);
                    SetBarY(_topBar, Mathf.Lerp(_topHidden, _topShown, e));
                    SetBarY(_bottomBar, Mathf.Lerp(_botHidden, _botShown, e));
                    if (p >= 1f) { _phase = Phase.Hold; _t = 0f; StartBanner(); }
                    break;
                }
                case Phase.Hold:
                {
                    if (!_bannerStarted) StartBanner();
                    if (_t >= HOLD) { _phase = Phase.Slideout; _t = 0f; }
                    break;
                }
                case Phase.Slideout:
                {
                    float p = Mathf.Clamp01(_t / SLIDE_OUT);
                    float e = Mathf.SmoothStep(0f, 1f, p);
                    SetBarY(_topBar, Mathf.Lerp(_topShown, _topHidden, e));
                    SetBarY(_bottomBar, Mathf.Lerp(_botShown, _botHidden, e));
                    if (p >= 1f) { _phase = Phase.Done; Finish(); }
                    break;
                }
            }
        }

        private async void StartBanner()
        {
            if (_bannerStarted) return;
            _bannerStarted = true;

            // 关键:position=(0,0)(UIEffectStage 把它当世界单位:之前传 90 直接顶出相机框);scale≈0.7 贴合老端。
            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(
                BANNER_EFFECT, _host, Vector2.zero, new Vector3(BANNER_SCALE, BANNER_SCALE, BANNER_SCALE));
            if (handle == null)
            {
                GameLog.Warn("Scene", "大妖来袭:横幅特效加载失败 effect={0}", BANNER_EFFECT);
                return;
            }
            if (_finished || this == null) { handle.Dispose(); return; } // 加载途中已收尾
            _banner = handle;
        }

        private void Finish()
        {
            if (_finished) return;
            _finished = true;
            _banner?.Dispose();
            _banner = null;
            AutoFightModel.Instance.SetCombatFreeze(false);   // 解冻:恢复战斗(玩家在 BOSS 正面、互相面向)
            BossBornEffectFlow.NotifyPlayerFinished();
            if (this != null) Object.Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // 兜底:被外部销毁(切场景等)也要恢复自动战斗 + 解锁播放标志 + 释放横幅。
            if (_finished) return;
            _finished = true;
            _banner?.Dispose();
            _banner = null;
            AutoFightModel.Instance.SetCombatFreeze(false); // 兜底解冻(切场景/断线也要恢复,否则下一场卡死不动)
            BossBornEffectFlow.NotifyPlayerFinished();
        }

        private RectTransform CreateBar(RectTransform parent, bool top)
        {
            var go = new GameObject(top ? "MaskTop" : "MaskBottom",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            // 横向拉满(+150 对标老端 mask_width=stageW+150),纵向固定 _barH,锚在对应边做滑动。
            rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rt.sizeDelta = new Vector2(150f, _barH);
            rt.anchoredPosition = new Vector2(0f, top ? _topHidden : _botHidden);

            var img = go.GetComponent<Image>();
            img.sprite = EnsureWhite();
            img.color = new Color(0f, 0f, 0f, BAR_ALPHA); // 贴图加载前的纯黑兜底
            img.raycastTarget = false;
            return rt;
        }

        private static RectTransform CreateHost(RectTransform parent)
        {
            var go = new GameObject("BannerHost", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            return rt;
        }

        private static async Task ApplyMask(RectTransform bar, string addr)
        {
            if (bar == null) return;
            Image img = bar.GetComponent<Image>();
            if (img == null) return;
            // 保留我们设的尺寸(coverScreen=false, nativeSize=false);贴图是黑色+alpha 渐变。
            bool ok = await ResManager.SetImageAsync(img, addr, false, false);
            if (img == null) return;
            // SetImageAsync 会把 alpha 顶到 1,这里复位到 0.8(贴图本身黑色,白 tint 不改黑,只用其 alpha 渐变)。
            img.color = ok ? new Color(1f, 1f, 1f, BAR_ALPHA) : new Color(0f, 0f, 0f, BAR_ALPHA);
        }

        private static void SetBarY(RectTransform rt, float y)
        {
            if (rt == null) return;
            Vector2 p = rt.anchoredPosition;
            p.y = y;
            rt.anchoredPosition = p;
        }

        private static Sprite EnsureWhite()
        {
            if (_white != null) return _white;
            Texture2D tex = Texture2D.whiteTexture;
            _white = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return _white;
        }
    }
}
