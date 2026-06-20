using System;
using Shenxiao.Generated.UI.Equip;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 洗练消耗物品项(对标老客户端 equipWash/EquipWashGoodsItem.ts):一个洗练所需材料格,含奖励图标(_gp_awarditem 内
    /// 挂 BaseAwardItem)、描述(_lb_dec)、拥有数(lb_num)/需求数(lb_need),够数绿、不够红;选中态高亮(_img_select);
    /// 点击格子(_gp_gooditem)回调 touch_CallBack(good_type, have_num)。
    ///
    /// 降级:物品数据层(GoodsModel.GetTypeGoodsNum/UPDATE_GOODS_NUM)、BaseAwardItem 图标渲染均未移植 →
    /// SetData 仅填描述/需求数/选中态,have_num 暂取 0(待 GoodsModel);点击只打日志「待对接」;选中高亮可用。
    /// 列表项,由 EquipWashView 克隆。无红点/模板节点。
    /// </summary>
    public sealed class EquipWashGoodsItem : EquipWashGoodsItemBind
    {
        /// <summary>当前格对应的物品类型 id(待 GoodsModel 配置)。</summary>
        private int _goodType;
        /// <summary>需求数量(对标 need_num)。</summary>
        private int _needNum;
        /// <summary>拥有数量(对标 have_num;待 GoodsModel,降级为 0)。</summary>
        private int _haveNum;
        /// <summary>点击回调(对标 touch_CallBack(good_type, have_num))。</summary>
        private Action<int, int> _touchCallBack;

        protected override void OnInit()
        {
            // 对标 InitEvent:点击 _gp_gooditem → touch_CallBack(good_type, have_num)。回调未接 → 打日志「待对接」。
            BindBtn(_gp_gooditem, () =>
            {
                _touchCallBack?.Invoke(_goodType, _haveNum);
                GameLog.Info("Equip", "点击[{0}] → 待对接", "洗练材料格 _gp_gooditem");
            });
            // 初始不显选中态(对标 _img_select.visible=false)。
            if (_img_select != null) _img_select.gameObject.SetActive(false);
        }

        /// <summary>
        /// 填洗练材料项(对标 SetData(touchCallBack, goods_type, need_num, description, select_extra))。
        /// 图标(BaseAwardItem)、拥有数(GoodsModel)未移植 → have_num 降级 0;描述/需求数/选中态可用。
        /// </summary>
        public void SetData(Action<int, int> touchCallBack, int goodsType, int needNum, string description, int selectExtra)
        {
            _touchCallBack = touchCallBack;
            _goodType = goodsType;

            // 对标 have_num = GoodsModel.GetTypeGoodsNum(goods_type)(未移植 → 0),need_num/拥有数颜色刷新。
            _needNum = Mathf.Max(0, needNum);
            _haveNum = 0; // 待 GoodsModel
            RefreshNum();

            // 描述(对标 _lb_dec.text = description)。
            if (_lb_dec != null) _lb_dec.text = description ?? "";

            // 选中高亮(对标 select_extra == good_type ? 显 : 隐)。
            if (_img_select != null) _img_select.gameObject.SetActive(selectExtra == _goodType);

            // 图标(老端 new BaseAwardItem(_gp_awarditem).SetData(goods_type) + SetItemSize(84,84))依赖 goods 配置,未移植 → 暂不设。
        }

        /// <summary>取消选中(对标 UnLoadSelect)。</summary>
        public void UnLoadSelect()
        {
            if (_img_select != null) _img_select.gameObject.SetActive(false);
        }

        /// <summary>刷拥有/需求数文本与颜色(对标 lb_num/lb_need:够数绿 #b3ff48,不够红 #ff4f50)。</summary>
        private void RefreshNum()
        {
            if (lb_num != null)
            {
                lb_num.text = _haveNum.ToString();
                lb_num.color = _haveNum >= _needNum ? new Color32(0xb3, 0xff, 0x48, 0xff) : new Color32(0xff, 0x4f, 0x50, 0xff);
            }
            if (lb_need != null) lb_need.text = "/" + _needNum;
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击。</summary>
        private void BindBtn(Component target, Action onClick)
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
