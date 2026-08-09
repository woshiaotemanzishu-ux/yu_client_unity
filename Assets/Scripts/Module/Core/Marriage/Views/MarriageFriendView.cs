using Shenxiao.Generated.UI.Marriage;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 婚恋-姻缘(缘分好友)页(对标老客户端 marriage/MarriageFriendView.ts),婚恋 5 个页签之一(大厅/姻缘/指环/锦囊/副本):
    /// 缘分候选列表(_list,克隆 MarriageFriendItem 循环列表,按 vip/vip_exp 排序)+ 翻页控件
    /// (_btn_top 首页 / _btn_left 上一页 / _btn_right 下一页 / _btn_bottom 末页,页码 _lb_page「当前/总页」)+
    /// 空态(_group_empty 列表为空时显示)+ 入口按钮(_btn_flower 送花记录 → MarriageRecordFlowerView /
    /// _btn_com_self 我的表白 → MarriageRecordComView(Self) / _btn_com_other 对方表白 → MarriageRecordComView(Other) /
    /// _btn_issue 发布心愿 → MarriageIssueView)。group1/group2/group3 为布局容器,imgN/labelN 为按钮内图标与文案。
    ///
    /// 当前:MarriageController/MarriageModel 已具备 17200 查询/收包地基；MarriageRecordType、MarriageFriendItem、
    /// LoopScrowViewMgr 等价列表表现、各子窗与翻页/排序消费尚未接线。OnShow 仅恢复第 1 页权威只读请求。
    /// </summary>
    public sealed class MarriageFriendView : MarriageFriendViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess → REQUEST_PROTO 17200 拉取缘分好友 + UPDATE_FRIEND_INFO → UpdateView:
            // 铺缘分候选列表 + 分页 + 空态。查询/收包地基已在，列表项、分页消费与空态表现尚未接线。
            MarriageController.Instance.RequestPersonalsList(1);
            GameLog.Info("Marriage", "MarriageFriendView 打开 → 已请求17200第1页权威快照,列表接线待运行态验证");
        }

        /// <summary>本页 Bind 无红点字段,占位保持与其它页签一致。</summary>
        private void HideReds()
        {
        }

        /// <summary>本页 Bind 无 _tpl_* 模板字段(列表项 MarriageFriendItem 由循环列表克隆),占位保持一致。</summary>
        private void HideTemplates()
        {
        }

        private void BindButtons()
        {
            BindBtn(_btn_top, "首页翻页 SwitchPage(0)");
            BindBtn(_btn_left, "上一页 SwitchPage(cur-1)");
            BindBtn(_btn_right, "下一页 SwitchPage(cur+1)");
            BindBtn(_btn_bottom, "末页翻页 SwitchPage(last)");
            BindBtn(_btn_flower, "送花记录 OPEN_VIEW(MarriageRecordFlowerView)");
            BindBtn(_btn_com_self, "我的表白 OPEN_VIEW(MarriageRecordComView, Self)");
            BindBtn(_btn_com_other, "对方表白 OPEN_VIEW(MarriageRecordComView, Other)");
            BindBtn(_btn_issue, "发布心愿 OPEN_VIEW(MarriageIssueView)");
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
