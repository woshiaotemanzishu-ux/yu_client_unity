using Shenxiao.Generated.UI.GodBefall;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 神祇降临-淬体顶部面板(对标老客户端 godBefall/GodBefallRecoveryTopView.ts,老端继承 BaseItem1):
    /// 展示当前选中神祇的战机图标(img_icon=ui_shape_{index})+ 称号图(img_title=ui_title_{index})+
    /// 当前淬体等级(lable_lv_cur_value)→ 下一级(lable_lv_next_value,满级显「已满级」并亮 img_full、隐 lable_lv_next_name)+
    /// 经验条(img_exp_bar 按 cur_exp/lv_up_need_exp 拉伸,html_exp_value 显「当前(+预览)/上限」,满级显 MAX)+
    /// 属性列表(box_property 内克隆 GodBefallRecoveryPropertyItem,当前/下一级数值对比)。
    ///
    /// 降级:GodBefallModel(选中神祇 god_data/index、is_max_level)、ResManager(图标纹理)、
    /// config_god_stren / config_god_equip(等级/经验/属性配置)、UPDATE_RECOVERT_TOP_VIEW / UPDATE_STRONG_GOD_VIEW /
    /// SELECTING_UPDATE_LEVEL_AND_EXP 事件、属性子项 GodBefallRecoveryPropertyItem 均未移植 →
    /// SetData 仅按老端结构落入参(等级/索引/经验文案直显),图标/称号纹理(走 ResManager)、属性子项铺设、
    /// 经验条比例与配置查表全部待对接;无红点 / 无模板 / 无按钮(纯展示项)。
    /// 列表/明细项,由上层面板克隆铺设。
    /// </summary>
    public sealed class GodBefallRecoveryTopView : GodBefallRecoveryTopViewBind
    {
        protected override void OnInit()
        {
            // 纯展示项 —— Bind 无红点字段 / 无 _tpl_* 模板 / 无按钮,无需隐藏件。
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSucess→InitView:UpdatePlaneImage(图标/称号) + UpdateStrengthLevel(当前/下一级) +
            // UpdateExpBar(经验条) + UpdateTopPropertyItem(属性列表)。数据未移植 → 文案空/默认。
            GameLog.Info("GodBefall", "GodBefallRecoveryTopView 打开 → 待对接 GodBefallModel/协议(列表空/等级/经验默认降级)");
        }

        /// <summary>
        /// 填顶部面板(对标老端 SetData(data, god_data, index) + InitView 系列刷新)。
        /// data=神祇等级/经验数据集,godData=当前选中品质 key,index=战机索引(决定 ui_shape_/ui_title_ 图标)。
        /// 降级:GodBefallModel/ResManager/config_god_stren 未移植 →
        /// 图标(img_icon/img_title 走 ResManager)、经验条比例(img_exp_bar)、属性子项(box_property)与满级判定均待对接,
        /// 这里仅把已知文案直显、其余落占位提示。
        /// </summary>
        public void SetData(object data, object godData, int index)
        {
            // 战机图标 / 称号图:老端 img_icon=ui_shape_{index}、img_title=ui_title_{index},经 ResManager 取纹理 → 待对接。
            // img_icon / img_title 保持预制体占位。

            // 当前 / 下一级等级:老端取 config_god_stren[key].stren_lv,未移植 → 占位。
            if (lable_lv_cur_value != null)
                lable_lv_cur_value.text = "—";
            if (lable_lv_next_value != null)
                lable_lv_next_value.text = "—";

            // 经验条文案:老端 html_exp_value = `${cur_exp}(+预览)/${up_level_exp}`,满级显 MAX → 待对接配置。
            if (html_exp_value != null)
                html_exp_value.text = "待对接 经验(config_god_stren)";

            // 满级提示:老端满级亮 img_full、隐 lable_lv_next_name。降级保持预制体默认显示状态。

            // 属性列表:老端 box_property 内克隆 GodBefallRecoveryPropertyItem 做当前/下一级对比 → 子项未移植,box_property 空铺。

            GameLog.Info("GodBefall", "GodBefallRecoveryTopView.SetData(index={0}) → 待对接 GodBefallModel/ResManager/config_god_stren 文案与属性铺设", index);
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
