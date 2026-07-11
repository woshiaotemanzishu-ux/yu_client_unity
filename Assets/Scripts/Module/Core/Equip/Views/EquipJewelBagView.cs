using Shenxiao.Common.Tips;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Jewel;
using Shenxiao.Module.Core.Bag;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 宝石背包弹窗(对标老客户端 jewel/EquipJewelBagView.ts):按 <see cref="Context"/>(equip_pos/stone_pos/
    /// 当前 type_id,由 <see cref="EquipJewelItem"/> 点击 _gp_click 时经 EquipFlow.OpenSub 带参传入)展示该镶嵌位
    /// 的操作 —— _btn_up→15215 type=0(升级当前宝石)、_btn_off→15209 拆除(既有 EquipStoneController)、直升丹
    /// 固定条目(EquipJewelBagItem 克隆 _tpl_EquipJewelBagItem)→15215 type=2(带二次确认,对标老端"该宝石等级
    /// 过低，使用收益较低，是否坚持使用")。
    ///
    /// 降级:常规"背包可镶嵌宝石列表"(对标 GetBagList 主体)依赖 config_equip_stone_inlay(部位→允许宝石子类)+
    /// config_equip_stone_lv(宝石等级/词条)两张未移植的表,且 Unity GoodsModel 未暴露"宝石子类型"字段 —— 无法
    /// 判定背包里哪些物品是该部位可镶嵌的宝石,本轮不臆造,列表留空(仅保留直升丹固定条目,数据源单一、无需配置表)。
    /// </summary>
    public sealed class EquipJewelBagView : EquipJewelBagViewBind
    {
        /// <summary>宝石直升丹(对标老端 EquipDefine.JEWEL_UP_GOODS)。</summary>
        private const int JewelUpGoodsTypeId = 14110007;

        /// <summary>打开上下文(对标老端 Local_Open 的 jewel_data_/dynamic_/cur_select_pos_ 三元组)。</summary>
        public readonly struct Context
        {
            public readonly int EquipPos;
            public readonly int StonePos;
            public readonly int CurrentTypeId;

            public Context(int equipPos, int stonePos, int currentTypeId)
            {
                EquipPos = equipPos;
                StonePos = stonePos;
                CurrentTypeId = currentTypeId;
            }
        }

        private int _equipPos;
        private int _stonePos;
        private int _currentTypeId;
        private GameObject _upOneInstance;

        protected override void OnInit()
        {
            if (_tpl_EquipJewelBagItem != null) _tpl_EquipJewelBagItem.SetActive(false);
            BindClick(closeBtn, Hide);
            BindClick(_btn_up, OnClickUp);
            BindClick(_btn_off, OnClickOff);
        }

        protected override void OnShow(object args)
        {
            if (args is Context ctx)
            {
                _equipPos = ctx.EquipPos;
                _stonePos = ctx.StonePos;
                _currentTypeId = ctx.CurrentTypeId;
            }
            RefreshHeader();
            RefreshUpOneEntry();
        }

        /// <summary>_btn_up/_btn_off 仅在该槽有宝石时才有意义(对标老端 jewel_data_ 真值判定);
        /// 红点(_reddot,是否可合成/升级)依赖 config_equip_stone_lv(未移植)→ 隐藏。</summary>
        private void RefreshHeader()
        {
            bool hasStone = _currentTypeId > 0;
            if (_btn_up != null) _btn_up.gameObject.SetActive(hasStone);
            if (_btn_off != null) _btn_off.gameObject.SetActive(hasStone);
            if (_reddot != null) _reddot.gameObject.SetActive(false);
        }

        /// <summary>直升丹固定条目:背包有货 + 当前槽有宝石(直升丹只能用于已镶嵌的宝石)才显示。</summary>
        private void RefreshUpOneEntry()
        {
            long haveNum = BagModel.Instance.GetTypeGoodsNum(JewelUpGoodsTypeId);
            bool show = haveNum > 0 && _currentTypeId > 0 && _tpl_EquipJewelBagItem != null;
            if (!show)
            {
                if (_upOneInstance != null) _upOneInstance.SetActive(false);
                GameLog.Info("Equip", "EquipJewelBagView 打开 equip_pos={0} stone_pos={1} type_id={2}(直升丹未显示:" +
                    "haveNum={3}) → 常规宝石列表待对接(config_equip_stone_inlay/config_equip_stone_lv 未移植)",
                    _equipPos, _stonePos, _currentTypeId, haveNum);
                return;
            }
            if (_upOneInstance == null)
            {
                _upOneInstance = Object.Instantiate(_tpl_EquipJewelBagItem, _tpl_EquipJewelBagItem.transform.parent);
            }
            _upOneInstance.SetActive(true);
            EquipJewelBagItem item = _upOneInstance.GetComponent<EquipJewelBagItem>();
            item?.Show();
            item?.SetUpOneData(JewelUpGoodsTypeId, haveNum, OnClickUpOne);
            GameLog.Info("Equip", "EquipJewelBagView 打开 equip_pos={0} stone_pos={1} type_id={2} → 直升丹条目已显示," +
                "常规宝石列表待对接(config_equip_stone_inlay/config_equip_stone_lv 未移植)", _equipPos, _stonePos, _currentTypeId);
        }

        /// <summary>_btn_up → 15215 type=0(升级当前宝石,对标老端 EquipJewelBagView.ts:77 _btn_up)。</summary>
        private void OnClickUp()
        {
            if (_currentTypeId <= 0)
            {
                TipsManager.Toast("该槽位暂无宝石");
                return;
            }
            GameLog.Info("Equip", "点击[升级] → UpgradeStone(equip_pos={0},stone_pos={1},type=0)", _equipPos, _stonePos);
            EquipJewelController.Instance.UpgradeStone(_equipPos, _stonePos, 0);
        }

        /// <summary>_btn_off → 15209 拆除(既有 EquipStoneController,对标老端 EquipJewelBagView.ts:90 _btn_off),
        /// 拆除后关闭弹窗(槽位已空,继续停留无意义)。</summary>
        private void OnClickOff()
        {
            if (_currentTypeId <= 0) return;
            GameLog.Info("Equip", "点击[拆除] → UnsetStone(equip_pos={0},stone_pos={1})", _equipPos, _stonePos);
            EquipStoneController.Instance.UnsetStone(_equipPos, _stonePos);
            Hide();
        }

        /// <summary>直升丹条目点击 → 15215 type=2,带二次确认(对标老端 EquipJewelBagItem.ts:56-60
        /// "该宝石等级过低，使用收益较低，是否坚持使用"。老端只在宝石等级&lt;6 时才提示,本轮无宝石等级数据源
        /// [config_equip_stone_lv 未移植]→ 保守起见一律确认,不直接执行)。</summary>
        private void OnClickUpOne()
        {
            if (_equipPos <= 0 || _stonePos <= 0) return;
            ConfirmDialog.Show("该宝石等级过低，使用收益较低，是否坚持使用", () =>
            {
                GameLog.Info("Equip", "点击[直升丹] → UpgradeStone(equip_pos={0},stone_pos={1},type=2)", _equipPos, _stonePos);
                EquipJewelController.Instance.UpgradeStone(_equipPos, _stonePos, 2);
            }, null);
        }

        private void BindClick(Component target, System.Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
