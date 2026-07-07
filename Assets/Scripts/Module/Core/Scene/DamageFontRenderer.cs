using System.Collections.Generic;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.UI;
using TMPro;
using UnityEngine;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 伤害飘字(对标老端 scene/fight 的 FightDamageManager + FightFontManager + FightFont 三件套)。
    /// 数据源 = 20001 S2C 攻击结果广播的 defender.damage / damage_flag(<see cref="Vo.FightVo"/>),由
    /// <see cref="FightController"/> 在应用血量前逐 defender 调 <see cref="ShowDamage"/> —— 只消费真实协议
    /// 字段,不本地算伤、不造数字。
    ///
    /// 对标口径(FightDamageManager.ts):
    ///   · 显示门槛 = 攻击者是主角 或 受击者是主角(ShowFont 前置判断 :426-440;其他人打架不飘)。
    ///   · damage==0 且非 闪避/免疫 → 不飘(:421;engage 帧 damage=0/flag=0、flag=7 None 均静默)。
    ///   · flag 语义:0普通 1闪避 2暴击 3免疫 4会心 5护盾免伤 6格挡 7无伤害 8卓越 9暴击会心 10卓越会心。
    ///   · 主角攻击 → 数字飘在受击者头顶(AniOne 普通 / AniTwo 暴击系放大回弹);主角被击 → 红字飘自己头顶(AniThree)。
    ///   · 位置出生即定格(老端 defender_pos 快照),横向随机散布(end_pos_offset=75)防重叠。
    ///
    /// 呈现降级(精确记录,不冒充):老端用位图字体 fight_font_*.fnt(bin/assets/resource/font/,未转换成
    /// Unity 字体资产),暴击/格挡等徽标是字体图内嵌图形。本类用场景内已有 TMP 字体 + 按 flag 配色/字号近似
    /// (配色取自 fight_font_attack 淡金、fight_font_baoji 橙金、fight_font_beattack 红,见资产 png);
    /// 位图字体转换后可替换 <see cref="ApplyFont"/> 换回美术字。层级/坐标口径与 MonsterRenderer 名牌一致
    /// (UILayer.Scene 屏幕跟随件,anchored = 世界像素 - 相机像素)。
    /// </summary>
    public static class DamageFontRenderer
    {
        // 老端 FightDamageFlag(FightDamageManager.ts:13-71;服务端 0-10,客户端追加 1001+ 本轮不涉及)。
        private const int FLAG_NORMAL = 0;
        private const int FLAG_DODGE = 1;       // 闪避
        private const int FLAG_CRIT = 2;        // 暴击
        private const int FLAG_IMMUNE = 3;      // 免疫/无敌
        private const int FLAG_HUIXIN = 4;      // 会心
        private const int FLAG_SHIELD = 5;      // 护盾免伤
        private const int FLAG_PARRY = 6;       // 格挡
        private const int FLAG_NONE = 7;        // 无伤害(不飘)
        private const int FLAG_ZHUOYUE = 8;     // 卓越
        private const int FLAG_CRIT_HUIXIN = 9; // 暴击会心
        private const int FLAG_ZHUOYUE_HUIXIN = 10;

        /// <summary>动画风格(对标老端 FightFontAniType 的三种主用形态)。</summary>
        private enum Ani
        {
            Normal,     // AniOne:弹出→短停→上飘缩小淡出(主角普攻)
            Crit,       // AniTwo:0.5→2 回弹放大→停→上飘淡出(暴击/卓越/会心系)
            MainRoleHit // AniThree:主角被击,红字自己头顶快缩落定→上飘淡出
        }

        private sealed class FloatItem
        {
            public TextMeshProUGUI Text;
            public RectTransform Rt;
            public float WorldX, WorldY; // 出生定格的世界像素(老端 defender_pos 快照,不追踪目标)
            public float ScatterX;       // 横向随机散布(对标 end_pos_offset=75)
            public float Age;
            public Ani Ani;
            public bool Active;
        }

        // 头顶偏移与 MonsterRenderer.NAMEPLATE_HEAD_OFFSET 同参考系:飘字从名牌上方一点出生。
        private const float HEAD_OFFSET = 165f;
        private const float SCATTER_RANGE = 75f;   // 对标老端 end_pos_offset=75
        private const int MAX_ACTIVE = 40;         // 极端 AOE 刷屏兜底:超限复用最老的一条

        private static readonly List<FloatItem> _items = new List<FloatItem>(); // 池(Active 标记复用)
        private static RectTransform _root;
        private static GameObject _driverGo;
        private static TMP_FontAsset _font;
        private static Material _fontMat;

        /// <summary>
        /// 单条伤害飘字。<paramref name="worldX"/>/<paramref name="worldY"/> = 受击者世界像素(怪取场景 vo,
        /// 主角取 RoleModel);<paramref name="damage"/>/<paramref name="flag"/> 来自 20001 defender 原始字段;
        /// <paramref name="mainRoleIsDefender"/> = 受击者是主角(红字被击样式)。显示门槛由调用方
        /// (FightController,主角相关才调)+ 本方法 damage==0 规则共同把守。
        /// </summary>
        public static void ShowDamage(int worldX, int worldY, long damage, int flag, bool mainRoleIsDefender)
        {
            if (flag == FLAG_NONE) return;                                        // 7=无伤害帧,老端不飘
            if (damage <= 0 && flag != FLAG_DODGE && flag != FLAG_IMMUNE) return; // 0 伤害仅 闪避/免疫 有字可飘

            (string text, Color color, float fontSize, Ani ani) = ResolveStyle(damage, flag, mainRoleIsDefender);
            if (string.IsNullOrEmpty(text)) return;

            EnsureRoot();
            if (_root == null) return; // Scene 层未就绪(headless/未装配):静默降级,不缓存补飘

            FloatItem item = ObtainItem();
            if (item == null) return;

            item.WorldX = worldX;
            item.WorldY = worldY;
            item.ScatterX = Random.Range(-SCATTER_RANGE, SCATTER_RANGE);
            item.Age = 0f;
            item.Ani = ani;
            item.Active = true;
            item.Text.text = text;
            item.Text.color = color;
            item.Text.fontSize = fontSize;
            item.Rt.gameObject.SetActive(true);
            UpdateItem(item); // 立即摆到出生位,避免首帧闪在旧位置
        }

        /// <summary>flag → (文案, 颜色, 字号, 动画)。配色近似老端位图字体(见类头说明)。</summary>
        private static (string, Color, float, Ani) ResolveStyle(long damage, int flag, bool mainRoleIsDefender)
        {
            // 主角被击:一律红系飘自己头顶(对标老端 be-attack 组 AniThree,fight_font_beattack 红字)。
            if (mainRoleIsDefender)
            {
                if (flag == FLAG_DODGE) return ("闪避", new Color(0.85f, 0.85f, 0.85f), 34f, Ani.MainRoleHit);
                if (flag == FLAG_IMMUNE) return ("免疫", new Color(0.85f, 0.85f, 0.85f), 34f, Ani.MainRoleHit);
                string prefix = flag == FLAG_CRIT || flag == FLAG_CRIT_HUIXIN ? "暴击 -" : "-";
                return (prefix + damage, new Color(1f, 0.29f, 0.24f), 38f, Ani.MainRoleHit);
            }

            // 主角(或其伙伴)攻击:飘在受击者头顶。
            switch (flag)
            {
                case FLAG_DODGE: return ("闪避", new Color(1f, 0.62f, 0.30f), 34f, Ani.Normal);
                case FLAG_IMMUNE: return ("免疫", new Color(0.85f, 0.85f, 0.85f), 34f, Ani.Normal);
                case FLAG_CRIT:
                case FLAG_CRIT_HUIXIN:
                    return ("暴击 " + damage, new Color(1f, 0.66f, 0.22f), 46f, Ani.Crit);   // fight_font_baoji 橙金
                case FLAG_ZHUOYUE:
                case FLAG_ZHUOYUE_HUIXIN:
                    return ("卓越 " + damage, new Color(0.79f, 0.55f, 1f), 46f, Ani.Crit);   // fight_font_zhuoyue 紫
                case FLAG_HUIXIN:
                    return ("会心 " + damage, new Color(0.42f, 0.82f, 1f), 44f, Ani.Crit);   // fight_font_huixin 蓝
                case FLAG_PARRY:
                    return ("格挡 " + damage, new Color(0.75f, 0.75f, 0.75f), 34f, Ani.Normal);
                case FLAG_SHIELD:
                    return (damage.ToString(), new Color(0.62f, 0.78f, 1f), 36f, Ani.Normal);
                case FLAG_NORMAL:
                default:
                    return (damage.ToString(), new Color(1f, 0.88f, 0.54f), 38f, Ani.Normal); // fight_font_attack 淡金
            }
        }

        // ── 动画驱动(driver 每帧推进;时间轴对标老端 FightFontManager.fight_font_ani_data 简化) ──

        private const float NORMAL_TOTAL = 0.70f;   // AniOne:0.2 弹出 + 0.15 停 + 0.35 上飘淡出
        private const float CRIT_TOTAL = 0.90f;     // AniTwo:0.15 放大回弹 + 0.125 回缩 + 0.3 停 + 0.325 上飘淡出
        private const float HIT_TOTAL = 0.95f;      // AniThree:主角被击

        internal static void Tick()
        {
            if (_items.Count == 0) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < _items.Count; i++)
            {
                FloatItem item = _items[i];
                if (!item.Active) continue;
                item.Age += dt;
                if (!UpdateItem(item))
                {
                    item.Active = false;
                    item.Rt.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>推进一条飘字;返回 false 表示寿命结束。位置每帧按(世界像素-相机像素)重算,相机移动不漂移。</summary>
        private static bool UpdateItem(FloatItem item)
        {
            float t = item.Age;
            float rise, scale, alpha = 1f;
            float total;

            switch (item.Ani)
            {
                case Ani.Crit:
                    total = CRIT_TOTAL;
                    if (t < 0.15f) { float p = t / 0.15f; scale = Mathf.LerpUnclamped(0.5f, 2.0f, EaseOutBack(p)); rise = 0f; }
                    else if (t < 0.275f) { float p = (t - 0.15f) / 0.125f; scale = Mathf.Lerp(2.0f, 1.4f, p); rise = 0f; }
                    else if (t < 0.575f) { scale = 1.4f; rise = 0f; }
                    else { float p = (t - 0.575f) / 0.325f; scale = 1.4f; rise = 20f * p; alpha = 1f - p; }
                    break;
                case Ani.MainRoleHit:
                    total = HIT_TOTAL;
                    if (t < 0.12f) { float p = t / 0.12f; scale = Mathf.Lerp(1.9f, 1f, p); rise = 0f; }
                    else if (t < 0.55f) { float p = (t - 0.12f) / 0.43f; scale = 1f; rise = 25f * p; }
                    else { float p = (t - 0.55f) / 0.40f; scale = 1f; rise = 25f + 45f * p; alpha = 1f - p; }
                    break;
                case Ani.Normal:
                default:
                    total = NORMAL_TOTAL;
                    if (t < 0.20f) { float p = t / 0.20f; scale = Mathf.Lerp(0.8f, 1.2f, p); rise = 18f * p; }
                    else if (t < 0.35f) { scale = 1.2f; rise = 18f; }
                    else { float p = (t - 0.35f) / 0.35f; scale = Mathf.Lerp(1.2f, 0.6f, p); rise = 18f + 55f * p; alpha = 1f - p; }
                    break;
            }
            if (t >= total) return false;

            Vector2 cam = SceneMapView.CameraPos;
            float sx = item.WorldX - cam.x + item.ScatterX;
            float sy = -(item.WorldY - cam.y) + HEAD_OFFSET + rise;
            item.Rt.anchoredPosition = new Vector2(sx, sy);
            item.Rt.localScale = new Vector3(scale, scale, 1f);
            Color c = item.Text.color;
            c.a = Mathf.Clamp01(alpha);
            item.Text.color = c;
            return true;
        }

        private static float EaseOutBack(float p)
        {
            const float s = 1.70158f;
            p -= 1f;
            return p * p * ((s + 1f) * p + s) + 1f;
        }

        // ── 池 / 根节点 ──────────────────────────────────────────────────────────

        private static FloatItem ObtainItem()
        {
            FloatItem oldest = null;
            int activeCount = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                FloatItem it = _items[i];
                if (!it.Active) return it;
                activeCount++;
                if (oldest == null || it.Age > oldest.Age) oldest = it;
            }
            if (activeCount >= MAX_ACTIVE) return oldest; // 刷屏兜底:复用最老一条
            return CreateItem();
        }

        private static FloatItem CreateItem()
        {
            var go = new GameObject("DamageFont", typeof(RectTransform));
            go.transform.SetParent(_root, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); // 与名牌同口径:锚屏幕中心,anchored=世界-相机
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(320f, 60f);

            var t = go.AddComponent<TextMeshProUGUI>();
            t.alignment = TextAlignmentOptions.Bottom;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            ApplyFont(t);

            var item = new FloatItem { Text = t, Rt = rt };
            _items.Add(item);
            return item;
        }

        private static void EnsureRoot()
        {
            if (_root != null) return;
            Transform sceneLayer = ViewManager.GetLayer(UILayer.Scene);
            if (sceneLayer == null) return;

            var go = new GameObject("__DamageFonts", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(sceneLayer, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var canvas = go.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = -30; // 名牌(-40)之上、HUD(0)之下(对标老端独立 DamageFont 层)
            _root = rt;

            EnsureDriver();
        }

        // 复用场景里已有 TMP 字体(含中文字形),同 MonsterRenderer/NpcRenderer 名牌约定,避免豆腐块。
        private static void ApplyFont(TextMeshProUGUI t)
        {
            if (_font == null)
            {
                TextMeshProUGUI src = Object.FindAnyObjectByType<TextMeshProUGUI>();
                if (src != null) { _font = src.font; _fontMat = src.fontSharedMaterial; }
            }
            if (_font != null) t.font = _font;
            if (_fontMat != null) t.fontSharedMaterial = _fontMat;
        }

        private static void EnsureDriver()
        {
            if (_driverGo != null) return;
            _driverGo = new GameObject("__DamageFontDriver");
            Object.DontDestroyOnLoad(_driverGo);
            _driverGo.AddComponent<DamageFontDriver>();
        }
    }

    /// <summary>飘字每帧驱动(同 MonsterRendererDriver 约定)。</summary>
    public sealed class DamageFontDriver : MonoBehaviour
    {
        private void Update() => DamageFontRenderer.Tick();
    }
}
