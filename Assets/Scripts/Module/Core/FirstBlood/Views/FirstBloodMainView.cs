using Shenxiao.Generated.UI.FirstBlood;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.FirstBlood
{
    /// <summary>
    /// 首杀主界面(对标老客户端 firstBlood/FirstBloodMainView.ts):说明(detail_text)+ 首杀目标模型(_gp_model_con)+
    /// 击杀记录列表(ScrollView/Content 克隆 FirstBloodItem,含头像 CustomHeadItem)+ 全服/个人奖励(all_reward/single_reward + 领取
    /// all_reward_get/single_reward_get)+ 红点(all_red_img/single_red_img/red_img)+ 前往(go_btn)+ 已击杀标(killed_img)。
    ///
    /// 降级:FirstBloodModel/首杀协议、FirstBloodItem/CustomHeadItem/EquipmentItem 列表项均未移植 → 红点/模板隐藏、列表空、模型默认;
    /// 前往/领取按钮打日志降级。无独立关闭按钮 → 二级 HUD 首杀按钮再点关闭(FirstBloodFlow.Toggle)。事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class FirstBloodMainView : FirstBloodMainViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindBtn(go_btn, "前往击杀");
            BindBtn(all_reward_get, "领取全服首杀奖励");
            BindBtn(single_reward_get, "领取个人首杀奖励");
        }

        protected override void OnShow(object args)
        {
            // 老端 open → 读 FirstBloodModel 铺击杀记录 + 奖励态。数据未移植 → 列表空、模型/奖励默认降级。
            GameLog.Info("FirstBlood", "首杀界面打开 → 待对接 FirstBloodModel(列表空/默认降级)");
        }

        private void HideReds()
        {
            HideNode(all_red_img);
            HideNode(single_red_img);
            HideNode(red_img);
        }

        private void HideTemplates()
        {
            if (_tpl_FirstBloodItem != null) _tpl_FirstBloodItem.SetActive(false);
            if (_tpl_CustomHeadItem != null) _tpl_CustomHeadItem.SetActive(false);
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/逻辑待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("FirstBlood", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
