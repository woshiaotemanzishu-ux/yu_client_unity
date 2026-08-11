using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Rune;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Dungeon;
using Shenxiao.Module.Core.RuneTreasure;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>灵魄背包；复用 RuneModule.prefab 中现有 RuneBagView/RuneBagItem 模板。</summary>
    public sealed class RuneBagView : RuneBagViewBind
    {
        public readonly struct OpenArgs
        {
            public int Position { get; }
            public bool Replace { get; }

            public OpenArgs(int position, bool replace)
            {
                Position = position;
                Replace = replace;
            }
        }

        private readonly List<GameObject> _rows = new List<GameObject>();
        private readonly Dictionary<Image, Sprite> _ownedSprites = new Dictionary<Image, Sprite>();
        private int _position = 1;
        private bool _replace;
        private int _renderEpoch;
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_RuneBagItem != null) _tpl_RuneBagItem.SetActive(false);
            BindClick(closeBtn, Hide);
            BindClick(getBtn, OpenObtainRoute);
        }

        protected override void OnShow(object args)
        {
            if (args is OpenArgs value)
            {
                _position = Mathf.Clamp(value.Position, 1, 10);
                _replace = value.Replace;
            }
            Subscribe();
            RuneController.Instance.RequestRuneBag();
            Render();
        }

        protected override void OnHide() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            RuneModel.Instance.BagChanged += OnBagChanged;
            RuneModel.Instance.Changed += Render;
            RuneModel.Instance.WearSucceeded += OnWearSucceeded;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            RuneModel.Instance.BagChanged -= OnBagChanged;
            RuneModel.Instance.Changed -= Render;
            RuneModel.Instance.WearSucceeded -= OnWearSucceeded;
            _subscribed = false;
        }

        private void OnBagChanged(bool _) => Render();

        private void OnWearSucceeded(int position)
        {
            if (position == _position) Hide();
        }

        private void Render()
        {
            int epoch = ++_renderEpoch;
            ClearRows();
            RuneModel model = RuneModel.Instance;
            List<RuneModel.BagGoodsVo> values = model.RuneBagGoods
                .Where(item => item != null && item.Num > 0)
                .OrderByDescending(item => RuneMainUIView.IsCompatible(_position, item.TypeId))
                .ThenByDescending(MaxAwake)
                .ThenByDescending(item => item.Color > 0 ? item.Color : GoodsModel.GetColor(item.TypeId))
                .ThenByDescending(item => GoodsModel.GetGoodsBasicByTypeId(item.TypeId)?.Subtype ?? 0)
                .ThenByDescending(item => item.Level)
                .ToList();

            if (none_conta != null) none_conta.gameObject.SetActive(model.HasRuneBag && values.Count == 0);
            if (label1 != null) label1.text = "万魄藏";
            if (tips != null)
            {
                tips.text = values.Count == 0
                    ? (_position >= 9 ? "参与劫天觅宝，可猎更多劫魄~" : "踏破九劫塔，可猎更多劫魄~")
                    : (_replace ? "请选择要替换的灵魄" : "请选择要镶嵌的灵魄");
            }
            if (bag_scroll == null || _tpl_RuneBagItem == null) return;
            RectTransform content = bag_scroll.content;
            if (content == null) return;
            for (int i = 0; i < values.Count; i++) CreateRow(values[i], content, epoch);
            bag_scroll.StopMovement();
            bag_scroll.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        private void CreateRow(RuneModel.BagGoodsVo value, RectTransform content, int epoch)
        {
            GameObject clone = Instantiate(_tpl_RuneBagItem, content, false);
            clone.name = "RuneBagItem_" + value.GoodsId;
            clone.SetActive(true);
            RuneBagItemBind row = clone.GetComponent<RuneBagItemBind>()
                ?? clone.GetComponentInChildren<RuneBagItemBind>(true);
            if (row == null) { Destroy(clone); return; }
            row.Show();
            _rows.Add(clone);

            bool compatible = RuneMainUIView.IsCompatible(_position, value.TypeId);
            if (row.goods_name != null) row.goods_name.text = GoodsName(value.TypeId);
            if (row.goods_lv != null) row.goods_lv.text = value.Level + "级";
            if (row.pro != null) row.pro.text = BuildAttributeText(value);
            if (row.not_suit != null) row.not_suit.gameObject.SetActive(!compatible);
            if (row.not_state != null) row.not_state.gameObject.SetActive(false);
            if (row.awakeIcon != null) row.awakeIcon.gameObject.SetActive(MaxAwake(value) > 0);
            if (row.insertBtn != null) row.insertBtn.gameObject.SetActive(compatible);
            if (row.labelDisplay != null) row.labelDisplay.text = _replace ? "替换" : "镶嵌";
            BindClick(row.insertBtn, () =>
            {
                if (!compatible) return;
                RuneController.Instance.Wear(_position, value.GoodsId);
            });

            int color = value.Color > 0 ? value.Color : GoodsModel.GetColor(value.TypeId);
            color = (value.TypeId == 26260005 || value.TypeId == 26270005) ? 6 : Mathf.Clamp(color, 1, 6);
            _ = SetSpriteAsync(row._img_icon, "resource/game/runeCard/" + value.TypeId + ".png", epoch);
            _ = SetSpriteAsync(row._img_kuang, "resource/game/runeCard/icon_kp_0" + color + ".png", epoch);
            _ = SetSpriteAsync(row._img_iconbg, "resource/game/runeCard/icon_kpbg_0" + color + ".png", epoch);
        }

        private void OpenObtainRoute()
        {
            Hide();
            if (_position >= 9) RuneTreasureFlow.Open();
            else DungeonRuneShellView.Show();
        }

        private static string BuildAttributeText(RuneModel.BagGoodsVo value)
        {
            if (value.AwakeList.Count > 0)
            {
                RuneModel.BagAwakeAttrVo first = value.AwakeList[0];
                return GoodsModel.GetAttrName(first.AttrType) + " 觉醒" + first.AwakeLv + "级";
            }
            if (value.ExtraAttrs.Count > 0)
            {
                RuneModel.BagExtraAttrVo first = value.ExtraAttrs[0];
                return GoodsModel.GetAttrName(first.AttrId) + " +" + first.AttrVal;
            }
            return "战力 " + value.CombatPower;
        }

        private static int MaxAwake(RuneModel.BagGoodsVo value)
        {
            int level = 0;
            for (int i = 0; i < value.AwakeList.Count; i++) level = Math.Max(level, value.AwakeList[i].AwakeLv);
            return level;
        }

        private static string GoodsName(int typeId)
        {
            string name = GoodsModel.GetGoodsName(typeId);
            return string.IsNullOrEmpty(name) ? typeId.ToString() : name;
        }

        private async Task SetSpriteAsync(Image image, string address, int epoch)
        {
            if (image == null) return;
            Sprite sprite = await ResManager.LoadAsync<Sprite>(address);
            if (sprite == null) return;
            if (epoch != _renderEpoch || image == null)
            {
                ResManager.Release(sprite);
                return;
            }
            if (_ownedSprites.TryGetValue(image, out Sprite old) && old != null && old != sprite) ResManager.Release(old);
            _ownedSprites[image] = sprite;
            image.sprite = sprite;
            image.enabled = true;
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++) DestroyRuntimeRow(_rows[i]);
            _rows.Clear();
            foreach (Sprite sprite in _ownedSprites.Values) if (sprite != null) ResManager.Release(sprite);
            _ownedSprites.Clear();
        }

        private static void DestroyRuntimeRow(GameObject row)
        {
            if (row == null) return;
            BaseView[] views = row.GetComponentsInChildren<BaseView>(true);
            for (int i = views.Length - 1; i >= 0; i--)
                if (views[i] != null && views[i].IsShown) views[i].Hide();
            Destroy(row);
        }

        private static void BindClick(Component target, Action action)
        {
            if (target == null || action == null) return;
            Image image = target as Image ?? target.GetComponent<Image>() ?? target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _renderEpoch++;
            ClearRows();
        }
    }
}
