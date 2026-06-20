using Shenxiao.Generated.UI.Marriage;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 寻缘求婚列表弹窗(对标老客户端 marriage/MarriageAskListView.ts):从婚恋姻缘页按钮打开的二级弹窗。
    /// 寻缘列表(已结缘玩家/可求婚对象,_list 克隆 _tpl_MarriageAskListItem)+ 空态(_group_empty,无对象时显示)+
    /// 前往按钮(_btn_go,老端 Fire OPEN_VIEW→MarriageBaseView)+ 关闭(_btn_close 关闭返回婚恋姻缘页)。
    ///
    /// 降级:MarriageModel(GetCanMarriageFriendListOutIntimacy 可求婚好友)、FriendModel(REQUEST_FRIEND_DATA/
    /// FRIEND_DATA_UPDATE 协议)、MarriageAskListItem 列表项、LoopScrowViewMgr 均未移植 → 列表空(显 _group_empty)、
    /// _tpl_MarriageAskListItem 模板隐藏;_btn_go 仅日志降级(老端 Fire OPEN_VIEW 打开 MarriageBaseView);
    /// _btn_close → Hide(老端 Close,关闭返回婚恋姻缘页)。
    /// </summary>
    public sealed class MarriageAskListView : MarriageAskListViewBind
    {
        protected override void OnInit()
        {
            // 模板项隐藏(老端 itemRenderer 克隆,降级期不铺列表)。
            if (_tpl_MarriageAskListItem != null) _tpl_MarriageAskListItem.SetActive(false);

            BindClose(_btn_close);
            BindBtn(_btn_go, "前往");
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess → 请求好友数据,FRIEND_DATA_UPDATE 回来按 list 长度铺列表/显空态。
            // MarriageModel/FriendModel 未移植 → 列表空、显空态。
            if (_group_empty != null) _group_empty.gameObject.SetActive(true);
            GameLog.Info("Marriage", "MarriageAskListView 打开 → 待对接 MarriageModel(列表空/默认降级)");
        }

        /// <summary>关闭按钮 → Hide(关闭返回婚恋姻缘页)。</summary>
        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }

        /// <summary>动作按钮 → 日志降级(待对接 OPEN_VIEW → MarriageBaseView)。</summary>
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
