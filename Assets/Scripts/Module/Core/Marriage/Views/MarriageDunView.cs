using Shenxiao.Generated.UI.Marriage;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 婚恋-副本页(对标老客户端 marriage/MarriageDunView.ts,婚恋 5 个 Tab 页之一:大厅/姻缘/指环/锦囊/副本):
    /// 背景(bg ui_marry_bg4)+ 角标(img4 uijh_123)+ 副本次数(_lb_count)+ 顶部说明(_lb_tips「与异性/伴侣一起进入副本…」)+
    /// 奖励横排(scroll/_group_item 克隆 EquipmentItem,按 config_dungeon_ui_content 铺奖励)+
    /// 队友下拉(_drop_btn_con 克隆 DownDropBtn,数据来自 MarriageModel.GetCanDunFriendList)+ 伴侣标识(_img_mate,选中伴侣时显示)+
    /// 自动匹配(_btn_match → REQUEST_PROTO 17245)+ 前往挑战(_btn_go → 61046,离线伴侣走镜像 Alert 确认)+
    /// 增加次数(_btn_add → 61021/17295,结婚后可买,Alert 二次确认)+ 说明(_btn_help → OPEN_INSTRUCTION_VIEW 1721)。
    ///
    /// 降级:MarriageModel(GetInstance/GetDunInfo/GetCanDunFriendList/MarriageDefine.DUN_ID)、FriendModel/RoleManager/VipModel、
    /// BaseDungeonModel(SCMD_REQUEST 61020/61021/61046、次数事件)、GoodsModel/Config/ErlangParser、DownDropBtn 下拉、
    /// EquipmentItem 奖励项、Alert 确认、各 MarriageEvent/FriendEvent 均未移植 →
    /// 红点(本页无)无需隐藏;_tpl_*(本页无)无需隐藏;按钮点击打日志「待对接」;OnShow 列表空/次数默认降级。
    /// 作为 Tab 子页随婚恋主窗驱动。Null-guard 每处访问。
    /// </summary>
    public sealed class MarriageDunView : MarriageDunViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 load_callback/LoadSuccess → 请求好友数据 + 副本信息(61020),刷次数/奖励/队友下拉。
            // MarriageModel/BaseDungeonModel/协议/配置/各 Item 均未移植 → 列表空、次数/默认降级。
            GameLog.Info("Marriage", "MarriageDunView 打开 → 待对接 MarriageModel(列表空/默认降级)");
        }

        /// <summary>红点:本页 Bind 无红点字段,无需隐藏。</summary>
        private void HideReds()
        {
        }

        /// <summary>列表项模板:本页 Bind 无 _tpl_* 模板字段(奖励项/下拉运行时克隆),无需隐藏。</summary>
        private void HideTemplates()
        {
        }

        private void BindButtons()
        {
            BindBtn(_btn_match, "自动匹配 REQUEST_PROTO(17245)");
            BindBtn(_btn_go, "前往挑战 SCMD_REQUEST(61046)");
            BindBtn(_btn_add, "增加次数 SCMD_REQUEST(61021)/REQUEST_PROTO(17295)");
            BindBtn(_btn_help, "说明 OPEN_INSTRUCTION_VIEW(1721)");
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
