using Shenxiao.Generated.UI.Equip;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 洗练材料选择弹窗(对标老客户端 equipWash/EquipWashGoodsView.ts):按洗练位 pos_info + 锁定条数 lock_num
    /// 读 config_equip_wash → EquipModel.GetWashInfo 取「必得红/橙属性」额外消耗,在 Content 里铺
    /// EquipWashGoodsItem(材料图标 + 拥有/所需数量 + 描述 + 选中态),点材料回调选择、btn_unload 取消选择并关闭。
    ///
    /// 关闭动作(自动循环 轮4 队列#4)已接线为真调用(Hide());取材/选中回调仍降级(见下)。
    /// 降级:EquipModel/GoodsModel/config_equip_wash + EquipWashGoodsItem 渲染均未移植 →
    /// 模板先隐藏、列表空(Content 不铺项,故本轮无选中态可清)。事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class EquipWashGoodsView : EquipWashGoodsViewBind
    {
        protected override void OnInit()
        {
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 open_callback → InitView:按 pos_info/lock_num 读洗练消耗配置铺材料项。数据未移植 → 列表空。
            GameLog.Info("Equip", "EquipWashGoodsView 打开 → 待对接 EquipModel/config_equip_wash(洗练材料列表空/属性默认降级)");
        }

        private void HideTemplates()
        {
            if (_tpl_EquipWashGoodsItem != null) _tpl_EquipWashGoodsItem.SetActive(false);
        }

        private void BindButtons()
        {
            // _img_close:老端 InitEvent 里 AddClickEvent 关窗(虽不以 btn 命名,但功能即按钮)→ 真关闭。
            BindBtn(_img_close, () => Hide());
            // btn_unload:老端取消所有材料选中(UnLoadSelect)+ select_callBack_(-2,0) 后关窗;列表本轮未铺格
            // (无子项可清选中),关窗动作先接真(对标老端此按钮的最终效果)。
            BindBtn(btn_unload, () => Hide());
        }

        /// <summary>给按钮 Image 挂点击回调。</summary>
        private void BindBtn(Image img, System.Action onClick)
        {
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
