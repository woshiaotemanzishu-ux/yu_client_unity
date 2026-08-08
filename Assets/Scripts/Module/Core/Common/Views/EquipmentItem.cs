using System;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 通用装备格子(对标老客户端 common/EquipmentItem.ts):比 BaseAwardItem 更重的装备件 —— 底板+图标 外加
    /// 强化等级/品阶/星级/觉醒/技能/星装/宠物装/神装/龙魂/限时/红点/特效 等一大票覆盖层。被装备/背包/角色等复用。
    ///
    /// 公开 API 对标:SetData(typeId,num,lock,select)/SetCount/SetSelect/SetLock/SetScale/SetClickCallBack。
    /// 静态阶数/星级/劣质标/限时/品质流光由 config_goods + config_equip_attr 自动呈现；强化等实例态由调用方
    /// 通过公开 setter 覆盖。套装流光和物品品质流光使用独立句柄，避免跨页面互相清除。
    /// </summary>
    public sealed class EquipmentItem : EquipmentItemBind
    {
        // 对标老端 BaseAwardItem.SetItemEffect：14 是资源语义倍率，越界动画由品质特效 profile
        // 按当前共享槽 effect_con 的真实尺寸逐实例裁切。
        private const float ItemQualityEffectScale = 14f;

        // 仅用于 127px 共享装备槽的 ui_shenzhuang 流光宿主；页面主展示/预览必须单独映射，
        // 不能因为资源名相同就复用本倍率。最终玩家画面仍需在装备槽宿主上做抽样复验。
        private const float SuitSlotEffectScale = 10f;

        private Action _clickCb;
        private int _typeId;
        private int? _displayColorOverride;
        private int _refreshEpoch;
        private UIEffectStage.Handle _itemEffect;
        private string _itemEffectName = "";
        private int _itemEffectEpoch;
        private UIEffectStage.Handle _suitEffect;
        private string _suitEffectName = "";
        private byte _suitEffectTier;
        private int _suitEffectEpoch;
        private bool _inited;

        protected override void OnInit()
        {
            EnsureInit();
        }

        private void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            // 所有动态覆盖层默认隐藏(数据接上后按需开),保留底板 item_bg + 图标 icon
            HideAll(@lock, select_image, stren, _img_grade_bg, grade, num_text, red_dot, up_image,
                lung_con, star_bg, _Group1, lung_star_num, skillImg, _bad_icon, _img_awaken, _img_tips,
                starEquipGp, _pet_equip_gp, god_icon, star_group, effect_con, _lb_name,
                effBox, effBox1, effBox2, time_limit);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            BindClick();
        }

        /// <summary>填装备/物品(对标 SetData 核心:type_id + 数量 + 锁 + 选中)。</summary>
        public void SetData(int typeId, long num, bool isLock = false, bool select = false)
        {
            EnsureInit();
            ClearSuitEffect();
            ClearItemEffect();
            ResetDynamicPresentation();
            _typeId = typeId;
            _displayColorOverride = null;
            SetCount(num);
            SetLock(isLock);
            SetSelect(select);
            ApplyStaticPresentation(typeId);
            RefreshIcon();
        }

        /// <summary>
        /// 装备实例包携带的 color 优先于 config_goods 静态品质；用于同 type_id 的实例品质展示。
        /// 递增 epoch 可阻止 SetData 先发起的异步静态底板晚到后覆盖实例底板。
        /// </summary>
        public void SetDisplayColor(int color)
        {
            _displayColorOverride = Mathf.Clamp(color, 0, 8);
            RefreshIcon();
        }

        /// <summary>数量(>1 才显示)。大数缩写对标 BaseAwardItem/老端 FormatNumber2(20000→"2W"),避免长数字溢出格子。</summary>
        public void SetCount(long num)
        {
            if (num_text == null) return;
            bool show = num > 1;
            num_text.gameObject.SetActive(show);
            if (show) num_text.text = GoodsModel.FormatCountNum(num);
        }

        /// <summary>选中态。</summary>
        public void SetSelect(bool select)
        {
            if (select_image != null) select_image.gameObject.SetActive(select);
        }

        /// <summary>锁定态。</summary>
        public void SetLock(bool locked)
        {
            if (@lock != null) @lock.gameObject.SetActive(locked);
        }

        public void SetStrengthen(int level, bool forceShow = false)
        {
            if (stren == null) return;
            bool show = level > 0 || forceShow;
            stren.gameObject.SetActive(show);
            stren.text = show ? "+" + Mathf.Max(0, level) : "";
        }

        public void SetGrade(int stage)
        {
            if (_img_grade_bg != null) _img_grade_bg.gameObject.SetActive(false);
            if (grade == null) return;
            bool show = stage > 0;
            grade.gameObject.SetActive(show);
            grade.text = show ? stage + "阶" : "";
        }

        public void SetStar(int count)
        {
            count = Mathf.Clamp(count, 0, 4);
            if (star_group != null) star_group.gameObject.SetActive(count > 0);
            SetActive(star_0, count >= 1);
            SetActive(star_1, count >= 2);
            SetActive(star_2, count >= 3);
            SetActive(star_3, count >= 4);
        }

        public void SetBadIcon(bool visible) => SetActive(_bad_icon, visible);
        public void SetUpMark(bool visible) => SetActive(up_image, visible);
        public void SetRedDot(bool visible) => SetActive(red_dot, visible);
        public void SetTimeLimit(bool visible) => SetActive(time_limit, visible);

        /// <summary>整体缩放(基准 127px 格子)。</summary>
        public void SetScale(float scale)
        {
            transform.localScale = Vector3.one * scale;
        }

        /// <summary>点击回调;未设则默认装备 tips(待移植)。</summary>
        public void SetClickCallBack(Action callback)
        {
            _clickCb = callback;
        }

        private void ResetDynamicPresentation()
        {
            HideAll(stren, _img_grade_bg, grade, red_dot, up_image, lung_con, star_bg, _Group1,
                lung_star_num, skillImg, _bad_icon, _img_awaken, _img_tips, starEquipGp,
                _pet_equip_gp, god_icon, star_group, effect_con, _lb_name, time_limit);
        }

        private void ApplyStaticPresentation(int typeId)
        {
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
            if (basic == null) return;

            SetTimeLimit(GoodsModel.HasConfigExpiry(typeId));
            if (basic.Type == 10)
            {
                GoodsModel.EquipAttr equip = GoodsModel.GetEquipAttr(typeId);
                SetGrade(equip?.Stage ?? 0);
                SetStar(equip?.Star ?? 0);
                SetBadIcon(equip?.ClassType == 1);
            }

            string effectName = GoodsModel.GetItemEffectName(basic.EffectId);
            if (!string.IsNullOrEmpty(effectName)) SetItemEffect(effectName);
        }

        private void SetItemEffect(string effectName)
        {
            _itemEffectName = effectName?.Trim() ?? "";
            RestartItemEffect();
        }

        private void RestartItemEffect()
        {
            ReleaseItemEffect();
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy || effect_con == null ||
                string.IsNullOrEmpty(_itemEffectName)) return;
            effect_con.gameObject.SetActive(true);
            int epoch = _itemEffectEpoch;
            _ = AttachItemEffectAsync(_itemEffectName, epoch);
        }

        public void ClearItemEffect()
        {
            _itemEffectName = "";
            ReleaseItemEffect();
        }

        private void ReleaseItemEffect()
        {
            ++_itemEffectEpoch;
            _itemEffect?.Dispose();
            _itemEffect = null;
            SetActive(effect_con, false);
        }

        private async Task AttachItemEffectAsync(string effectName, int epoch)
        {
            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(
                effectName, effect_con, Vector2.zero, Vector3.one * ItemQualityEffectScale, 0f);
            if (this == null || effect_con == null || epoch != _itemEffectEpoch || _typeId <= 0)
            {
                handle?.Dispose();
                return;
            }

            _itemEffect = handle;
            effect_con.gameObject.SetActive(handle != null);
        }

        /// <summary>
        /// 共享装备槽的套装/共鸣流光入口。仅明确 opt-in 的已穿戴槽调用；材料、奖励、详情、
        /// 共鸣中央当前/下一阶展示均不得调用。宿主选择、槽位缩放和生命周期由组件负责。
        /// </summary>
        public void SetSuitEffect(string effectName, byte tier)
        {
            EnsureInit();
            ClearSuitEffect();
            if (string.IsNullOrWhiteSpace(effectName)) return;

            _suitEffectName = effectName.Trim();
            _suitEffectTier = tier;
            RestartSuitEffect();
        }

        private void RestartSuitEffect()
        {
            ReleaseSuitEffect();
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy || string.IsNullOrEmpty(_suitEffectName))
                return;

            RectTransform host;
            Vector2 position = Vector2.zero;
            Vector3 scale;
            switch (_suitEffectTier)
            {
                case 1:
                    host = effBox;
                    scale = Vector3.one * SuitSlotEffectScale;
                    break;
                case 2:
                    host = effBox1;
                    position.x = 0.5f;
                    scale = new Vector3(1.3f, 1f, 1f) * SuitSlotEffectScale;
                    break;
                case 3:
                    host = effBox2;
                    scale = Vector3.one * (1.3f * SuitSlotEffectScale);
                    break;
                default:
                    return;
            }

            if (host == null) return;
            host.gameObject.SetActive(true);
            int epoch = _suitEffectEpoch;
            _ = AttachSuitEffectAsync(_suitEffectName, host, position, scale, epoch);
        }

        public void ClearSuitEffect()
        {
            _suitEffectName = "";
            _suitEffectTier = 0;
            ReleaseSuitEffect();
        }

        private void ReleaseSuitEffect()
        {
            ++_suitEffectEpoch;
            _suitEffect?.Dispose();
            _suitEffect = null;
            HideAll(effBox, effBox1, effBox2);
        }

        protected override void OnDispose()
        {
            ClearItemEffect();
            ClearSuitEffect();
            base.OnDispose();
        }

        private async Task AttachSuitEffectAsync(string effectName, RectTransform host,
            Vector2 position, Vector3 scale, int epoch)
        {
            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(
                effectName, host, position, scale, 0f, new Vector2(140f, 140f));
            if (this == null || host == null || epoch != _suitEffectEpoch)
            {
                handle?.Dispose();
                return;
            }

            _suitEffect = handle;
            host.gameObject.SetActive(handle != null);
        }

        private void OnDestroy()
        {
            ClearItemEffect();
            ClearSuitEffect();
        }

        private void OnDisable()
        {
            // 共享格可能只随父页面失活而不会单独走 BaseView.Hide：释放句柄避免累积，
            // 但保留期望状态，让同一数据随父页面重开时自动恢复两类流光。
            ReleaseItemEffect();
            ReleaseSuitEffect();
        }

        private void OnEnable()
        {
            if (!_inited || _typeId <= 0) return;
            if (!string.IsNullOrEmpty(_itemEffectName)) RestartItemEffect();
            if (!string.IsNullOrEmpty(_suitEffectName)) RestartSuitEffect();
        }

        private void RefreshIcon()
        {
            RefreshIconAsync();
        }

        private async void RefreshIconAsync()
        {
            int epoch = ++_refreshEpoch;
            int typeId = _typeId;
            if (icon == null) return;
            if (typeId <= 0)
            {
                icon.enabled = false;
                return;
            }

            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
            if (basic == null)
            {
                icon.enabled = false;
                GameLog.Warn("Common", "EquipmentItem typeId={0} 不在 config_goods(或未加载)→ 图标降级隐藏", typeId);
                return;
            }

            if (item_bg != null)
            {
                int displayColor = _displayColorOverride ?? GoodsModel.GetDisplayColor(typeId);
                string plateKey = GameResPath.GetIcon("common", "com_goods_plate_" + displayColor);
                await ResManager.SetImageAsync(item_bg, plateKey, false, false);
                if (_typeId != typeId || _refreshEpoch != epoch) return;
            }

            string iconPath = GameResPath.GetGoodsIconPath(basic.Icon);
            bool ok = await ResManager.SetImageAsync(icon, iconPath, false, false);
            if (_typeId != typeId || _refreshEpoch != epoch) return;
            icon.enabled = ok;
            if (!ok)
            {
                GameLog.Warn("Common", "EquipmentItem 物品[{0}]{1} 图标未加载: key={2}", typeId, basic.Name, iconPath);
            }
        }

        private void BindClick()
        {
            if (click_group == null) return;
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            Image img = click_group.GetComponent<Image>();
            // 共鸣材料会异步替换 icon；图标的启停不能顺带让整个物品格失去点击。
            // 优先让稳定的整格底板成为唯一命中面，再兼容没有 item_bg 的旧模板。
            if (img == null && item_bg != null && item_bg.transform.IsChildOf(click_group)) img = item_bg;
            if (img == null) img = click_group.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, OnClick);
        }

        private void OnClick()
        {
            if (_clickCb != null) { _clickCb(); return; }
            if (_typeId > 0) ItemTipsView.Show(_typeId, 1);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }

        private static void HideAll(params Component[] comps)
        {
            foreach (var c in comps)
                if (c != null) c.gameObject.SetActive(false);
        }
    }
}
