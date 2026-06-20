using Shenxiao.Generated.UI.Rune;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>
    /// 符文属性查看(对标老客户端 rune/RunePropertyView.ts):从九霄劫魄(符文)主面板按钮再点打开的二级子窗。
    /// 标题「罡魄天威」+ 属性列表(scroll/Content 克隆 _tpl_RunePropertyItem,战力/属性汇总+觉醒加成)+
    /// 空态(none_conta,无属性时显示)。无关闭按钮:老端 click_bg_toClose,Unity 由主面板按钮再点 ToggleSub 关闭,故不绑 close。
    ///
    /// 降级:RuneModel(GetFightAndProList/GetAllProShow/rune_info/GetWakeUpCfg 等)、RunePropertyItem 列表项均未移植 →
    /// 列表空(显 none_conta、scroll 隐含为空)、_tpl_RunePropertyItem 模板隐藏;无任何动作/关闭/红点按钮可绑。
    /// </summary>
    public sealed class RunePropertyView : RunePropertyViewBind
    {
        protected override void OnInit()
        {
            // 列表项模板隐藏(运行时克隆,不直接显示)。
            if (_tpl_RunePropertyItem != null) _tpl_RunePropertyItem.SetActive(false);
            // 无关闭按钮/无动作按钮/无红点:由主面板按钮 ToggleSub 关闭,本子窗不绑任何点击。
        }

        protected override void OnShow(object args)
        {
            // 老端 open → OpenCallback:GetFightAndProList 有数据则铺属性列表、否则显空态。RuneModel 未移植 → 列表空、显空态。
            if (none_conta != null) none_conta.gameObject.SetActive(true);
            GameLog.Info("Rune", "RunePropertyView 打开 → 待对接 RuneModel(列表空/默认降级)");
        }
    }
}
