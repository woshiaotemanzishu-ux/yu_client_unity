using Shenxiao.Generated.UI.GodBefall;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 神祇降临-洗练标签按钮项(对标老客户端 godBefall/GodBefallRecoveryBtnItem.ts):
    /// 洗练页左侧的一个分类标签按钮,内含背景(img_bg)+ 选中高亮(img_select)+ 标签文字(lable_btn_name)+ 红点(img_red)。
    /// 老端 data:data[0]=按钮名,data[2].color=颜色类型(查 GodBefallModel.recovery_red_dot_list 决定红点),
    /// SelectBtnState 按 selected 切 img_select 显隐 + 文字主色/描边色,SetRedDotState 由红点列表决定 img_red 显隐。
    ///
    /// 降级:GodBefallModel(recovery_red_dot_list/UPDATE_RECOVERY_RED 事件)未移植 →
    /// OnInit 隐藏红点 img_red + 选中高亮 img_select;SetData 仅落标签文字(颜色/描边切换待对接),红点恒隐;
    /// SetSelect 仅切 img_select 显隐。无 _tpl_* 模板、无独立按钮字段(整项即按钮,点击由列表/父面板挂)。
    /// 列表项,由洗练面板克隆铺设。
    /// </summary>
    public sealed class GodBefallRecoveryBtnItem : GodBefallRecoveryBtnItemBind
    {
        protected override void OnInit()
        {
            // 红点:对标 SetRedDotState,依赖 GodBefallModel.recovery_red_dot_list(未移植)→ 隐藏。
            HideNode(img_red);
            // 选中高亮:对标 SelectBtnState 的 img_select.visible = selected,默认未选 → 隐藏。
            HideNode(img_select);
        }

        /// <summary>
        /// 填标签数据(对标 dataChanged → SelectBtnState/SetRedDotState)。
        /// 老端 data[0]=按钮名;颜色/描边随 selected 切换、红点查 model 列表 —— 数据层未移植 → 仅落文字。
        /// </summary>
        public void SetData(string name, bool selected)
        {
            if (lable_btn_name != null) lable_btn_name.text = name ?? "";
            // 红点恒隐(待对接 GodBefallModel.recovery_red_dot_list[color_type])。
            HideNode(img_red);
            SetSelect(selected);
            GameLog.Info("GodBefall", "GodBefallRecoveryBtnItem.SetData(name={0}, selected={1}) → 待对接 GodBefallModel(recovery_red_dot_list/红点+文字配色)", name, selected);
        }

        /// <summary>设置选中高亮(对标 SelectBtnState 的 img_select.visible = selected)。文字主色/描边色切换待对接。</summary>
        public void SetSelect(bool selected)
        {
            if (img_select != null) img_select.gameObject.SetActive(selected);
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
