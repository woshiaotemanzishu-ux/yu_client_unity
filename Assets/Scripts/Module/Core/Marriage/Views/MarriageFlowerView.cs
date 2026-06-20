using Shenxiao.Generated.UI.Marriage;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 伴侣-送花界面(对标老客户端 marriage/MarriageFlowerView.ts):
    /// 婚恋主面板「姻缘」页按钮打开的二级弹窗。头像位(_head_con 克隆 CustomHeadItem)+
    /// 好友下拉(_drop_btn_con 克隆 DownDropBtn/MarriageDropBtn,GetFriendList 取列表,无好友时降级「无可赠送对象」)+
    /// 结社名(_lb_guild)+ 亲密度展示(_gp_intimacy/_lb_intimacy,与「快加好友」提示 _gp_tips 互斥)+
    /// 送花列表(_gp_con 走 LoopScrowViewMgr 铺 MarriageFlowerItem,按背包数量/goods_id 排序)+
    /// 说明(_btn_help → OPEN_INSTRUCTION_VIEW 223)+ 关闭(_btn_close → Close 关闭返回主面板)。
    ///
    /// 降级:MarriageModel(GetFriendList/GetFlowerList/GIVE_GIFT/GET_FLOWER/GIVE_GIFT_SUCCESS 等)、
    /// FriendModel(getFriendById/REQUEST_FRIEND_DATA/FRIEND_DATA_UPDATE)、GoodsModel/DressModel/RoleManager、
    /// 送花协议(22301)、CustomHeadItem 头像、DownDropBtn 下拉、MarriageFlowerItem 列表项与 LoopScrowViewMgr 循环列表均未移植 →
    /// _tpl_* 模板隐藏;按钮点击打日志「待对接」;_btn_close 走 Hide 关闭返回主面板;OnShow 列表空/默认降级。
    /// 二级弹窗,默认关闭、不进 FirstPass。Null-guard 每处访问。
    /// (Bind 中无红点字段,故无红点隐藏。)
    /// </summary>
    public sealed class MarriageFlowerView : MarriageFlowerViewBind
    {
        protected override void OnInit()
        {
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 Local_Open/LoadSuccess → UpdateView/UpdateItems:渲染头像 + 好友下拉 + 送花列表/亲密度。
            // MarriageModel/FriendModel/协议/列表项均未移植 → 列表空、默认降级。
            GameLog.Info("Marriage", "MarriageFlowerView 打开 → 待对接 MarriageModel(列表空/默认降级)");
        }

        /// <summary>列表项/下拉/头像模板,由各 Item View 克隆,数据未移植先隐藏。</summary>
        private void HideTemplates()
        {
            if (_tpl_MarriageFlowerItem != null) _tpl_MarriageFlowerItem.SetActive(false);
            if (_tpl_CustomHeadItem != null) _tpl_CustomHeadItem.SetActive(false);
            if (_tpl_DownDropBtn != null) _tpl_DownDropBtn.SetActive(false);
            if (_tpl_MarriageDropBtn != null) _tpl_MarriageDropBtn.SetActive(false);
        }

        private void BindButtons()
        {
            // _btn_close → 关闭返回婚恋主面板(BaseView.Hide)。
            BindClose(_btn_close);
            BindBtn(_btn_help, "说明 OPEN_INSTRUCTION_VIEW(223)");
        }

        /// <summary>关闭按钮:挂点击 → Hide(继承自 BaseView,关闭本二级弹窗返回主面板)。</summary>
        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/子窗待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Marriage", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
