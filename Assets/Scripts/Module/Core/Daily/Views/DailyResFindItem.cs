using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 资源找回·行项(对标老客户端 daily/DailyResFindItem.ts):标题(title)+ 奖励格列表(_Scroller1/Content)+
    /// 找回按钮(findBtn,带价格 price/icon)/已完成(doneBtn)/免费提示(free_lb)。
    ///
    /// 老端 findBtn → Fire(OPEN_VIEW, "DailyResFindTipsView") 弹出找回确认;此处把选中行数据经
    /// <see cref="DailyResFindTipsView.Pending"/> 交给确认弹窗,再 DailyFlow.OpenSub("DailyResFindTipsView")。
    /// 降级:奖励格图标预览(config_res_act 未导入)/config_res_act 关联展示未接线,先只落次数文案。
    /// </summary>
    public sealed class DailyResFindItem : DailyResFindItemBind
    {
        private DailyModel.ResFindVo _vo;

        protected override void OnInit()
        {
            BindClick(findBtn, () =>
            {
                DailyResFindTipsView.Pending = _vo;
                DailyFlow.OpenSub("DailyResFindTipsView");
            });
            BindClick(doneBtn, () => GameLog.Info("Daily", "点击[资源找回·已完成] → 无剩余次数"));
        }

        /// <summary>填资源找回行(对标 SetData):次数文案(41900 回包);config_res_act 未导入,标题降级为 act 号。</summary>
        public void SetData(DailyModel.ResFindVo vo)
        {
            _vo = vo;
            if (title != null) title.text = "资源找回 " + vo.ActId + "@" + vo.ActSub;
            bool canFind = vo.Lefttimes + vo.LefttimesVip > 0;
            if (findBtn != null) findBtn.gameObject.SetActive(canFind);
            if (doneBtn != null) doneBtn.gameObject.SetActive(!canFind);
            if (price != null) price.text = vo.Lefttimes + "+" + vo.LefttimesVip;
            if (free_lb != null) free_lb.gameObject.SetActive(false);
        }

        /// <summary>兼容旧签名(标题占位,老端调用方尚未补齐前的降级路径)。</summary>
        public void SetData(string titleText)
        {
            if (title != null) title.text = titleText;
        }

        private static void BindClick(Component target, System.Action onClick)
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
