using Shenxiao.Common.Tips;
using Shenxiao.Generated.UI.Equip;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Bag;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备列表格(自动循环 轮4 队列#4,补写:Bind 已烤,老端无对应手写 View;对标老客户端 equipWash/EquipWashItem.ts)。
    /// 吞天洗魄面板左侧穿戴装备列表(EquipWashView.Content/_Scroller1)与神屠九炼装备列表(EquipRefinementView.Content)
    /// 共用同一模板 _tpl_EquipWashItem。图标(icon_bg/award_group)+ 部位角标(_img_pos_icon)+ 选中框(select_img)+
    /// 4 格洗魄进度(wash0~wash3,老端按属性 color 换图)+ 神炼等级角标(_bit_refine_lv)+ 名称/段位文本
    /// (_lb_name/_lb_level)+ 未解锁遮罩/提示(_img_mask/tips)+ 免费标(_img_free)+ 红点(red_dot)。
    ///
    /// 点击 _gp(对标老端 InitEvent):未解锁 → 提示「部位未解锁」;已解锁但未穿戴(无数据)→ 提示「未穿戴装备」;
    /// 否则回调选中(对标 SELECT_WASH_EQUIP,供父面板切换"当前查看装备")——照抄老端三段判定,真实行为而非日志占位。
    ///
    /// 降级:EquipModel(解锁配置完整表/红点判定)、GoodsDynamicModel 驱动的洗魄进度(wash_attr 颜色)/神炼等级、
    /// EquipmentItem 图标克隆均未移植 → 默认按「未解锁」态(is_lock=true,同老端构造函数默认值)显示遮罩+提示占位;
    /// 红点/免费标/洗魄进度先隐藏;SetData 仅填名称(走 Common.GoodsModel 真配置取名)+ 段位文本。
    /// 列表项,由父面板克隆铺设(本轮父面板暂未铺格,故本项当前无实例被创建;点击三段判定/选中回调已就绪待接)。
    /// </summary>
    public sealed class EquipWashItem : EquipWashItemBind
    {
        private int _equipType;
        private long _goodsId;
        /// <summary>对标老端构造函数 this.is_lock = true(默认锁定,等 SetUnlocked 才解锁)。</summary>
        private bool _isLock = true;
        private bool _hasData;
        private EquipmentItem _equipmentItem;
        private BaseAwardItem _awardItem;
        private System.Action<int, long> _onSelect;

        public int EquipType => _equipType;

        protected override void OnInit()
        {
            if (red_dot != null) red_dot.gameObject.SetActive(false);
            if (select_img != null) select_img.gameObject.SetActive(false);
            if (_img_free != null) _img_free.gameObject.SetActive(false);
            HideWashProgress();
            ApplyLockVisual();

            BindClick(_gp, () =>
            {
                if (_isLock)
                {
                    TipsManager.Toast("部位未解锁");
                    return;
                }
                if (!_hasData)
                {
                    TipsManager.Toast("未穿戴装备");
                    return;
                }
                SetSelect(true);
                _onSelect?.Invoke(_equipType, _goodsId);
                GameLog.Info("Equip", "选中装备列表格 equip_type={0} goods_id={1}(SELECT_WASH_EQUIP)", _equipType, _goodsId);
            });
        }

        private void HideWashProgress()
        {
            if (wash0 != null) wash0.gameObject.SetActive(false);
            if (wash1 != null) wash1.gameObject.SetActive(false);
            if (wash2 != null) wash2.gameObject.SetActive(false);
            if (wash3 != null) wash3.gameObject.SetActive(false);
        }

        /// <summary>设置该格部位(对标 SetEquipPos);部位图标依赖 equipCom 图集(未移植)不设图。</summary>
        public void SetEquipPos(int equipType) => _equipType = equipType;

        /// <summary>设置选中回调(对标 SELECT_WASH_EQUIP 事件消费方,由父面板铺格时挂)。</summary>
        public void SetSelectCallback(System.Action<int, long> onSelect) => _onSelect = onSelect;

        /// <summary>解锁态(对标 SetOpenData/OpenFunc:等级达标 → 解锁,遮罩/提示隐藏;否则显遮罩+"N级解锁"提示)。</summary>
        public void SetUnlocked(bool unlocked, int unlockLv)
        {
            _isLock = !unlocked;
            if (tips != null)
            {
                tips.gameObject.SetActive(_isLock);
                if (_isLock) tips.text = unlockLv + "级\n解锁";
            }
            if (_img_mask != null) _img_mask.gameObject.SetActive(_isLock);
        }

        /// <summary>填最小数据(对标 SetData):名称走 Common.GoodsModel 真配置(GetGoodsBasicByTypeId)、段位文本直填。
        /// 洗魄进度(wash_attr 颜色)/神炼等级/红点/免费标依赖 EquipModel+config_equip_wash/attr(未移植)→ 仅占位。</summary>
        public void SetData(int equipType, long goodsId, int typeId, int division,
            GameObject equipmentTemplate = null, GoodsDetailVo detail = null)
        {
            _equipType = equipType;
            _goodsId = goodsId;
            _hasData = goodsId > 0;

            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
            if (_lb_name != null) _lb_name.text = basic != null ? basic.Name : "";
            if (_lb_level != null) _lb_level.text = "洗魄段位:" + division + "段";
            if (_bit_refine_lv != null) _bit_refine_lv.text = "";
            EnsureEquipmentItem(equipmentTemplate);
            if (_equipmentItem != null)
            {
                if (_hasData)
                {
                    if (!_equipmentItem.IsShown) _equipmentItem.Show();
                    _equipmentItem.SetData(typeId, 1);
                    _equipmentItem.SetClickCallBack(() =>
                    {
                        BagGoods worn = EquipAutoWear.GetWorn(_equipType);
                        if (worn != null) ItemTipsView.ShowEquipped(worn);
                    });
                }
                else if (_equipmentItem.IsShown) _equipmentItem.Hide();
            }
            else if (_awardItem != null)
            {
                if (_hasData)
                {
                    if (!_awardItem.IsShown) _awardItem.Show();
                    _awardItem.SetData(typeId, 1);
                    _awardItem.SetClickCallBack(() =>
                    {
                        BagGoods worn = EquipAutoWear.GetWorn(_equipType);
                        if (worn != null) ItemTipsView.ShowEquipped(worn);
                    });
                }
                else if (_awardItem.IsShown) _awardItem.Hide();
            }
            RefreshWashProgress(detail?.WashAttrs?.Count ?? 0);
            SetSelect(false);
        }

        public void SetRefinementLevel(int level)
        {
            if (_bit_refine_lv != null)
            {
                _bit_refine_lv.gameObject.SetActive(level > 0);
                _bit_refine_lv.text = level > 0 ? ("神炼" + level + "级") : string.Empty;
            }
        }

        public void SetSelect(bool selected)
        {
            if (select_img != null) select_img.gameObject.SetActive(selected);
        }

        private void ApplyLockVisual()
        {
            if (tips != null) tips.gameObject.SetActive(_isLock);
            if (_img_mask != null) _img_mask.gameObject.SetActive(_isLock);
        }

        private void EnsureEquipmentItem(GameObject template)
        {
            if (_equipmentItem != null || _awardItem != null || template == null || award_group == null) return;
            GameObject go = Instantiate(template, award_group, false);
            go.name = "EquipmentItem_Runtime";
            go.SetActive(false);
            if (go.transform is RectTransform rt)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one * 0.82f;
            }
            _equipmentItem = go.GetComponent<EquipmentItem>();
            if (_equipmentItem == null) _awardItem = go.GetComponent<BaseAwardItem>();
            if (_equipmentItem == null && _awardItem == null) Destroy(go);
        }

        private void RefreshWashProgress(int count)
        {
            Image[] progress = { wash0, wash1, wash2, wash3 };
            for (int i = 0; i < progress.Length; i++)
                if (progress[i] != null) progress[i].gameObject.SetActive(i < count);
        }

        protected override void OnHide()
        {
            if (_equipmentItem != null && _equipmentItem.IsShown) _equipmentItem.Hide();
            if (_awardItem != null && _awardItem.IsShown) _awardItem.Hide();
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
