using Shenxiao.Generated.UI.Marriage;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 婚戒设计图/称号弹窗(对标老客户端 marriage/MarriageDsgtView.ts):伴侣-称号界面,从婚恋姻缘页按钮打开的二级弹窗。
    /// 称号列表(_list/Content 克隆 _tpl_MarriageDsgtItem)+ 爱意进度(_img_progress 进度条 + _lb_progress 进度文本)+
    /// 关闭(_btn_close 关闭返回)。
    ///
    /// 降级:MarriageModel(伴侣信息 GetMateInfo、爱意值 love_num、MarriageEvent UPDATE_DSGT/CHANGE_KEY_VALUE)、
    /// DsgtModel(称号解锁状态 GetDsgtInfoVo)、config_love_dsgt_cfg 配置、MarriageDsgtItem 列表项、
    /// LoopScrowViewMgr 均未移植 → 列表空、_tpl_MarriageDsgtItem 模板隐藏、进度走默认(_img_progress/_lb_progress 不改);
    /// _btn_close → Hide(老端 Close,关闭返回上一页)。
    /// </summary>
    public sealed class MarriageDsgtView : MarriageDsgtViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_MarriageDsgtItem != null) _tpl_MarriageDsgtItem.SetActive(false);

            BindClose(_btn_close);
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess → SetDsgtDataList/UpdateView:GetMateInfo 取爱意值铺进度+列表。
            // MarriageModel/DsgtModel/config_love_dsgt_cfg 未移植 → 列表空、进度走默认值。
            GameLog.Info("Marriage", "MarriageDsgtView 打开 → 待对接 MarriageModel(列表空/默认降级)");
        }

        /// <summary>关闭按钮 → Hide(关闭返回上一页)。</summary>
        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }

        /// <summary>动作按钮 → 日志降级(待对接 MarriageModel 协议)。</summary>
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
