using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Scene3D.Map;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    ///   · damage==0 且非闪避 → 不飘(:421;engage 帧 damage=0/flag=0、免疫与 flag=7 None 均静默)。
    ///   · flag 语义:0普通 1闪避 2暴击 3免疫 4会心 5护盾免伤 6格挡 7无伤害 8卓越 9暴击会心 10卓越会心。
    ///   · 主角攻击 → 数字飘在受击者头顶(AniOne 普通 / AniTwo 暴击系放大回弹);主角被击 → 红字飘自己头顶(AniThree)。
    ///   · 位置出生即定格(老端 defender_pos 快照),横向随机散布(end_pos_offset=75)防重叠。
    ///
    /// 字形严格使用老端 fight_font_*.fnt/png：a/b/c 是图集中“闪避/暴击/卓越/会心”等整块美术字，
    /// 不是要显示的拉丁字母。协议 flag 只选择字体和占位字符，不再拼普通中文或用 TMP 颜色模拟。
    /// 同一协议中的 attack_trigger_skill_list / 20028 则使用 skillName/{skillId}.png 单图美术字。
    /// 层级/坐标口径与 MonsterRenderer 名牌一致(UILayer.Scene 屏幕跟随件)。
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

        private sealed class PendingDamage
        {
            public int WorldX, WorldY, Flag;
            public long Damage;
            public bool MainRoleIsDefender;
        }

        private sealed class SkillImageItem
        {
            public Image Image;
            public RectTransform Rt;
            public float WorldX, WorldY;
            public float Age;
            public bool Active;
        }

        // 头顶偏移与 MonsterRenderer.NAMEPLATE_HEAD_OFFSET 同参考系:飘字从名牌上方一点出生。
        private const float HEAD_OFFSET = 165f;
        private const float SCATTER_RANGE = 75f;   // 对标老端 end_pos_offset=75
        private const int MAX_ACTIVE = 40;         // 极端 AOE 刷屏兜底:超限复用最老的一条

        private static readonly List<FloatItem> _items = new List<FloatItem>(); // 池(Active 标记复用)
        private static readonly List<SkillImageItem> _skillItems = new List<SkillImageItem>();
        private static readonly Queue<PendingDamage> _pendingDamage = new Queue<PendingDamage>();
        private static readonly Dictionary<string, TMP_FontAsset> _bitmapFonts =
            new Dictionary<string, TMP_FontAsset>(StringComparer.Ordinal);
        private static readonly Dictionary<int, Sprite> _skillSprites = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Task<Sprite>> _skillLoads = new Dictionary<int, Task<Sprite>>();
        private static readonly string[] CombatFontNames =
        {
            "fight_font_attack", "fight_font_beattack", "fight_font_baoji", "fight_font_huixin",
            "fight_font_zhuoyue", "fight_font_shenwu", "fight_font_gedang", "fight_font_fantan",
            "fight_font_liuxue", "fight_font_huifu",
        };
        private static RectTransform _root;
        private static GameObject _driverGo;
        private static Task _fontLoadTask;
        private static bool _fontLoadFinished;
        private static HashSet<int> _skillNameWhitelist;
        private static Task _skillConfigTask;

        /// <summary>
        /// 单条伤害飘字。<paramref name="worldX"/>/<paramref name="worldY"/> = 受击者世界像素(怪取场景 vo,
        /// 主角取 RoleModel);<paramref name="damage"/>/<paramref name="flag"/> 来自 20001 defender 原始字段;
        /// <paramref name="mainRoleIsDefender"/> = 受击者是主角(红字被击样式)。显示门槛由调用方
        /// (FightController,主角相关才调)+ 本方法 damage==0 规则共同把守。
        /// </summary>
        public static void ShowDamage(int worldX, int worldY, long damage, int flag, bool mainRoleIsDefender)
        {
            if (!CanShowCombatFloat()) return;
            if (flag == FLAG_NONE) return;                                        // 7=无伤害帧,老端不飘
            if (damage <= 0 && flag != FLAG_DODGE) return; // 老端只有闪避允许 value=0；免疫 0 同样静默

            if (!_fontLoadFinished)
            {
                if (_pendingDamage.Count >= MAX_ACTIVE) _pendingDamage.Dequeue();
                _pendingDamage.Enqueue(new PendingDamage
                {
                    WorldX = worldX,
                    WorldY = worldY,
                    Damage = damage,
                    Flag = flag,
                    MainRoleIsDefender = mainRoleIsDefender,
                });
                _ = PreloadAsync();
                return;
            }

            ShowDamageReady(worldX, worldY, damage, flag, mainRoleIsDefender);
        }

        /// <summary>游戏开始预热全部战斗 BMFont；首个协议早到时 ShowDamage 会排队，绝不回退普通文字。</summary>
        public static Task PreloadAsync()
        {
            if (_fontLoadTask == null || _fontLoadTask.IsFaulted) _fontLoadTask = LoadFontsAndFlushAsync();
            return _fontLoadTask;
        }

        private static async Task LoadFontsAndFlushAsync()
        {
            var loads = new Task<TMP_FontAsset>[CombatFontNames.Length];
            for (int i = 0; i < CombatFontNames.Length; i++)
                loads[i] = ResManager.LoadAsync<TMP_FontAsset>("fonts/bitmap/" + CombatFontNames[i]);

            TMP_FontAsset[] fonts = await Task.WhenAll(loads);
            for (int i = 0; i < CombatFontNames.Length; i++)
            {
                if (fonts[i] != null) _bitmapFonts[CombatFontNames[i]] = fonts[i];
            }
            _fontLoadFinished = true;

            if (_bitmapFonts.Count != CombatFontNames.Length)
                GameLog.Error("BitmapFont", "战斗位图字体预热不完整: {0}/{1}", _bitmapFonts.Count, CombatFontNames.Length);

            while (_pendingDamage.Count > 0)
            {
                PendingDamage pending = _pendingDamage.Dequeue();
                ShowDamageReady(pending.WorldX, pending.WorldY, pending.Damage, pending.Flag,
                    pending.MainRoleIsDefender);
            }
        }

        private static void ShowDamageReady(int worldX, int worldY, long damage, int flag, bool mainRoleIsDefender)
        {
            if (!CanShowCombatFloat()) return;
            (string text, string fontName, float fontSize, Ani ani) = ResolveStyle(damage, flag, mainRoleIsDefender);
            if (string.IsNullOrEmpty(text) || !_bitmapFonts.TryGetValue(fontName, out TMP_FontAsset font)
                || font == null) return;

            EnsureRoot();
            if (_root == null) return; // Scene 层未就绪(headless/未装配):静默降级,不缓存补飘

            FloatItem item = ObtainItem();
            if (item == null) return;

            item.WorldX = worldX;
            item.WorldY = worldY;
            item.ScatterX = UnityEngine.Random.Range(-SCATTER_RANGE, SCATTER_RANGE);
            item.Age = 0f;
            item.Ani = ani;
            item.Active = true;
            item.Text.text = text;
            item.Text.font = font;
            item.Text.fontSharedMaterial = font.material;
            item.Text.color = Color.white;
            item.Text.fontSize = fontSize;
            item.Rt.gameObject.SetActive(true);
            UpdateItem(item); // 立即摆到出生位,避免首帧闪在旧位置
        }

        /// <summary>flag → (占位字符, 位图字体, 字号, 动画)，逐项对标 FightDamageManager.InitFightFontData。</summary>
        private static (string, string, float, Ani) ResolveStyle(long damage, int flag, bool mainRoleIsDefender)
        {
            if (mainRoleIsDefender)
            {
                switch (flag)
                {
                    case FLAG_CRIT:
                    case FLAG_CRIT_HUIXIN:
                        return ("b" + damage, "fight_font_baoji", 36f, Ani.MainRoleHit);
                    case FLAG_ZHUOYUE:
                        return ("b" + damage, "fight_font_zhuoyue", 36f, Ani.MainRoleHit);
                    case FLAG_ZHUOYUE_HUIXIN: // 老端该组合明确复用会心被击字形
                    case FLAG_HUIXIN:
                        return ("b" + damage, "fight_font_huixin", 36f, Ani.MainRoleHit);
                    case FLAG_PARRY:
                        return ("b" + damage, "fight_font_gedang", 36f, Ani.MainRoleHit);
                    default:
                        return (damage == 0 ? "a" : damage.ToString(), "fight_font_beattack", 36f, Ani.MainRoleHit);
                }
            }

            switch (flag)
            {
                case FLAG_CRIT:
                case FLAG_CRIT_HUIXIN:
                    return ("a" + damage, "fight_font_baoji", 36f, Ani.Crit);
                case FLAG_ZHUOYUE:
                case FLAG_ZHUOYUE_HUIXIN:
                    return ("a" + damage, "fight_font_zhuoyue", 36f, Ani.Crit);
                case FLAG_HUIXIN:
                    return ("a" + damage, "fight_font_huixin", 36f, Ani.Crit);
                case FLAG_PARRY:
                    return ("a" + damage, "fight_font_gedang", 36f, Ani.Normal);
                default:
                    return (damage == 0 ? "a" : damage.ToString(), "fight_font_attack", 36f, Ani.Normal);
            }
        }

        /// <summary>
        /// 显示触发技能的单图美术字。老端只允许 config_key_value[20001] 白名单内的技能，
        /// 图片按 resource/game/skillName/{skillId}.png 异步加载；加载完成后才出现，不生成文字替身。
        /// </summary>
        public static void ShowSkillImage(int skillId, int worldX, int worldY)
        {
            if (!CanShowCombatFloat()) return;
            _ = ShowSkillImageAsync(skillId, worldX, worldY);
        }

        private static async Task ShowSkillImageAsync(int skillId, int worldX, int worldY)
        {
            try
            {
                await EnsureSkillWhitelistAsync();
                if (_skillNameWhitelist == null || !_skillNameWhitelist.Contains(skillId)) return;

                Sprite sprite = await GetSkillSpriteAsync(skillId);
                if (sprite == null || !CanShowCombatFloat()) return;
                EnsureRoot();
                if (_root == null) return;

                SkillImageItem item = ObtainSkillItem();
                item.WorldX = worldX;
                item.WorldY = worldY;
                item.Age = 0f;
                item.Active = true;
                item.Image.sprite = sprite;
                item.Image.SetNativeSize();
                item.Rt.gameObject.SetActive(true);
                UpdateSkillItem(item);
            }
            catch (Exception e)
            {
                GameLog.Error("BitmapFont", "技能名图片显示失败 skill={0}: {1}", skillId, e.Message);
            }
        }

        private static Task EnsureSkillWhitelistAsync()
        {
            if (_skillConfigTask == null || _skillConfigTask.IsFaulted)
                _skillConfigTask = LoadSkillWhitelistAsync();
            return _skillConfigTask;
        }

        private static async Task LoadSkillWhitelistAsync()
        {
            await KeyValueConfigs.EnsureLoaded();
            var ids = new HashSet<int>();
            string raw = KeyValueConfigs.GetRaw(20001);
            if (!string.IsNullOrWhiteSpace(raw))
            {
                foreach (JToken token in JArray.Parse(raw))
                {
                    if (token.Type == JTokenType.Integer) ids.Add(token.Value<int>());
                }
            }
            _skillNameWhitelist = ids;
        }

        private static Task<Sprite> GetSkillSpriteAsync(int skillId)
        {
            if (_skillSprites.TryGetValue(skillId, out Sprite sprite) && sprite != null)
                return Task.FromResult(sprite);
            if (_skillLoads.TryGetValue(skillId, out Task<Sprite> pending)) return pending;

            Task<Sprite> load = LoadSkillSpriteAsync(skillId);
            _skillLoads[skillId] = load;
            return load;
        }

        private static async Task<Sprite> LoadSkillSpriteAsync(int skillId)
        {
            try
            {
                Sprite sprite = await ResManager.LoadAsync<Sprite>(GameResPath.GetSkillNamePath(skillId));
                if (sprite != null) _skillSprites[skillId] = sprite;
                return sprite;
            }
            finally
            {
                _skillLoads.Remove(skillId);
            }
        }

        // ── 动画驱动(driver 每帧推进;时间轴对标老端 FightFontManager.fight_font_ani_data 简化) ──

        private const float NORMAL_TOTAL = 0.70f;   // AniOne:0.2 弹出 + 0.15 停 + 0.35 上飘淡出
        private const float CRIT_TOTAL = 0.90f;     // AniTwo:0.15 放大回弹 + 0.125 回缩 + 0.3 停 + 0.325 上飘淡出
        private const float HIT_TOTAL = 0.95f;      // AniThree:主角被击

        internal static void Tick()
        {
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
            for (int i = 0; i < _skillItems.Count; i++)
            {
                SkillImageItem item = _skillItems[i];
                if (!item.Active) continue;
                item.Age += dt;
                if (!UpdateSkillItem(item))
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

        /// <summary>对标老端 FightFontFiveAni：0.75→2→1，停留后淡出。</summary>
        private static bool UpdateSkillItem(SkillImageItem item)
        {
            const float enlargeEnd = 0.05f;
            const float narrowEnd = 0.20f;
            const float stayEnd = 0.75f;
            const float total = 0.95f;
            float t = item.Age;
            if (t >= total) return false;

            float scale;
            float alpha = 1f;
            if (t < enlargeEnd) scale = Mathf.Lerp(0.75f, 2f, t / enlargeEnd);
            else if (t < narrowEnd) scale = Mathf.Lerp(2f, 1f, (t - enlargeEnd) / (narrowEnd - enlargeEnd));
            else scale = 1f;
            if (t > stayEnd) alpha = 1f - (t - stayEnd) / (total - stayEnd);

            Vector2 cam = SceneMapView.CameraPos;
            item.Rt.anchoredPosition = new Vector2(item.WorldX - cam.x + 10f,
                -(item.WorldY - cam.y) + HEAD_OFFSET + 200f);
            item.Rt.localScale = new Vector3(scale, scale, 1f);
            Color c = item.Image.color;
            c.a = Mathf.Clamp01(alpha);
            item.Image.color = c;
            return true;
        }

        private static float EaseOutBack(float p)
        {
            const float s = 1.70158f;
            p -= 1f;
            return p * p * ((s + 1f) * p + s) + 1f;
        }

        private static bool CanShowCombatFloat()
        {
            if (Time.timeScale <= 0f) return false;
            Transform windowLayer = ViewManager.GetLayer(UILayer.Window);
            if (windowLayer == null) return true;
            for (int i = 0; i < windowLayer.childCount; i++)
            {
                if (windowLayer.GetChild(i).gameObject.activeInHierarchy) return false;
            }
            return true;
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

            var item = new FloatItem { Text = t, Rt = rt };
            _items.Add(item);
            return item;
        }

        private static SkillImageItem ObtainSkillItem()
        {
            SkillImageItem oldest = null;
            for (int i = 0; i < _skillItems.Count; i++)
            {
                SkillImageItem item = _skillItems[i];
                if (!item.Active) return item;
                if (oldest == null || item.Age > oldest.Age) oldest = item;
            }
            if (_skillItems.Count >= 12) return oldest;
            return CreateSkillItem();
        }

        private static SkillImageItem CreateSkillItem()
        {
            var go = new GameObject("SkillNameImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_root, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.color = Color.white;
            var item = new SkillImageItem { Image = image, Rt = rt };
            _skillItems.Add(item);
            return item;
        }

        private static void EnsureRoot()
        {
            if (_root != null) return;
            _items.RemoveAll(item => item == null || item.Rt == null);
            _skillItems.RemoveAll(item => item == null || item.Rt == null);
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

        private static void EnsureDriver()
        {
            if (_driverGo != null) return;
            _driverGo = new GameObject("__DamageFontDriver");
            UnityEngine.Object.DontDestroyOnLoad(_driverGo);
            _driverGo.AddComponent<DamageFontDriver>();
        }
    }

    /// <summary>飘字每帧驱动(同 MonsterRendererDriver 约定)。</summary>
    public sealed class DamageFontDriver : MonoBehaviour
    {
        private void Update() => DamageFontRenderer.Tick();
    }
}
