using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.FriendInvite;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.FriendInvite
{
    /// <summary>
    /// 好友邀请只读主窗。现有 Prefab 负责视觉；本类只接关闭、只读刷新和明确降级入口。
    /// 34002/03/04/07/09、11301/02 均不在此发送。
    /// </summary>
    public sealed class FriendInviteMainView : FriendInviteViewBind
    {
        private const string TransactionBoundary = "blocked-no-transaction-send";
        private bool _listening;

        protected override void OnInit()
        {
            HideTemplate(_tpl_FriendInviteBoostView);
            HideTemplate(_tpl_FriendInviteHelpView);
            HideTemplate(_tpl_FriendInviteLevelUpView);
            HideTemplate(_tpl_FriendInviteRecourseView);
            HideTemplate(_tpl_FriendInviteRecourseItem);
            HideTemplate(_tpl_FriendInviteTabItem);

            BindClick(_img_close, Hide);
            BindClick(_lb_preview, () => GameLog.Info("FriendInvite", "奖励预览待真实配置/弹窗运行验收"));
            BindClick(ins, () => GameLog.Info("FriendInvite", "好友邀请说明入口待真实公共说明弹窗运行验收"));
            BindClick(_gp_open, () => GameLog.Info("FriendInvite", "{0}: 分享/领奖为未授权写事务，保持 blocked，不发送协议", TransactionBoundary));
        }

        protected override void OnShow(object args)
        {
            SetListening(true);
            RefreshSnapshot();
        }

        protected override void OnHide()
        {
            SetListening(false);
        }

        protected override void OnDispose()
        {
            SetListening(false);
        }

        internal void PrepareForRelease()
        {
            SetListening(false);
        }

        private void SetListening(bool listening)
        {
            if (_listening == listening) return;
            _listening = listening;
            if (listening) EventDispatcher.On(FriendInviteModel.EVENT_UPDATED, RefreshSnapshot);
            else EventDispatcher.Off(FriendInviteModel.EVENT_UPDATED, RefreshSnapshot);
        }

        private void RefreshSnapshot()
        {
            FriendInviteModel model = FriendInviteModel.Instance;
            if (_lb_count != null) _lb_count.text = "今日邀请 " + model.DailyCount + "  累计 " + model.TotalCount;
            if (_lb_count_down != null)
            {
                _lb_count_down.text = model.GetStatus == 2 ? "奖励可领取" : "分享与领奖待授权验证";
            }
            if (_lb_open_desc != null) _lb_open_desc.text = model.GetStatus == 2 ? "领取" : "分享";
            if (_img_red != null) _img_red.gameObject.SetActive(model.GetStatus == 2);
        }

        private static void HideTemplate(GameObject template)
        {
            if (template != null) template.SetActive(false);
        }

        private static void BindClick(Component target, System.Action action)
        {
            if (target == null || action == null) return;
            Graphic graphic = target as Graphic;
            if (graphic == null) graphic = target.GetComponentInChildren<Graphic>(true);
            if (graphic == null) return;
            graphic.raycastTarget = true;
            UIUtil.AddClick(graphic, action);
        }
    }
}
