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
    /// 降级:EquipModel/GoodsModel/config_equip_wash + EquipWashGoodsItem 渲染均未移植 →
    /// 模板先隐藏、列表空(Content 不铺项);_img_close/btn_unload 点击打日志「待对接」;OnShow 打 TODO。
    /// 事件驱动弹窗,默认关闭、不进 FirstPass。
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
            // _img_close:老端 InitEvent 里 AddClickEvent 关窗(虽不以 btn 命名,但功能即按钮)。
            BindBtn(_img_close, "关闭");
            // btn_unload:老端取消所有材料选中(UnLoadSelect)+ select_callBack_(-2,0) 后关窗。
            BindBtn(btn_unload, "卸下/取消选择 btn_unload");
        }

        /// <summary>给按钮 Image 挂点击 → 打日志(降级:逻辑/回调待对接)。</summary>
        private void BindBtn(Image img, string label)
        {
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Equip", "点击[{0}] → 待对接", label));
        }
    }
}
