using System;
using Shenxiao.Generated.UI.FairyWish;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.FairyWish
{
    /// <summary>
    /// 仙缘许愿界面(对标老客户端 fairyWish/FairyWishView.ts):仙灵预览(preview_image/_fairy_name)+ 战力(_lb_fighting)+
    /// 节点列表(_node_list:FairyWishNodeItem/2)+ 技能/属性(max/big/small 组,FairyWishAttrItem)+ 许愿操作(_btn_operate)+ 关闭/返回。
    ///
    /// 降级:FairyWishModel/许愿协议未移植 → 3 个列表项模板隐藏、红点隐藏、close/back → Hide、btn_operate → TODO、OnShow TODO。
    /// 事件驱动弹窗,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class FairyWishView : FairyWishViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_FairyWishAttrItem != null) _tpl_FairyWishAttrItem.SetActive(false);
            if (_tpl_FairyWishNodeItem != null) _tpl_FairyWishNodeItem.SetActive(false);
            if (_tpl_FairyWishNodeItem2 != null) _tpl_FairyWishNodeItem2.SetActive(false);
            if (_img_red != null) _img_red.gameObject.SetActive(false);
            BindBtn(_img_close, () => Hide());
            BindBtn(_img_back, () => Hide());
            BindBtn(_btn_operate, () => GameLog.Info("FairyWish", "许愿操作 → 待对接 FairyWishModel 协议"));
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("FairyWish", "仙缘许愿打开 → 待对接 许愿数据(FairyWishModel)");
        }

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
