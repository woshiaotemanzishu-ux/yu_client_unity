using System;
using Shenxiao.Generated.UI.FairyWish;
using Shenxiao.Framework.Event;
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
    /// 51302 仅属于外层入口红点确认，不允许复用本页 _btn_operate。未购买购买/支付流程缺生产接入时明确阻断；
    /// 已购买且满足等级条件的节点操作走正式 51301。
    /// </summary>
    public sealed class FairyWishView : FairyWishViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_FairyWishAttrItem != null) _tpl_FairyWishAttrItem.SetActive(false);
            if (_tpl_FairyWishNodeItem != null) _tpl_FairyWishNodeItem.SetActive(false);
            if (_tpl_FairyWishNodeItem2 != null) _tpl_FairyWishNodeItem2.SetActive(false);
            if (_img_red != null) _img_red.gameObject.SetActive(false);
            BindBtn(_img_close, FairyWishFlow.Close);
            BindBtn(_img_back, FairyWishFlow.Close);
            BindBtn(_btn_operate, OnNodeOperate);
            EventDispatcher.On<int>(GlobalEvent.EVT_FAIRYWISH_UPDATE, OnFairyUpdated);
        }

        protected override void OnShow(object args)
        {
            _fairyId = args is int id ? id : 0;
            Render();
        }

        protected override void OnDispose()
        {
            EventDispatcher.Off<int>(GlobalEvent.EVT_FAIRYWISH_UPDATE, OnFairyUpdated);
        }

        private int _fairyId;

        private void OnFairyUpdated(int fairyId)
        {
            if (fairyId != 0 && fairyId != _fairyId) return;
            Render();
        }

        private void OnNodeOperate()
        {
            FairyWishModel.FairyEntry entry = FairyWishModel.Instance.GetFairy(_fairyId);
            if (entry == null) { FairyWishController.Instance.RequestInfo(_fairyId); return; }
            FairyWishModel.OperateState state = FairyWishModel.Instance.GetOperateState(
                _fairyId, 0, Shenxiao.Module.Core.Role.RoleModel.Instance.Level);
            if (state.CanSend51301)
            {
                FairyWishController.Instance.RequestNodeActivate(_fairyId, state.NodeId);
                return;
            }
            GameLog.Info("FairyWish", "operate blocked fairyId={0} state={1} node={2};51302 不属于此按钮",
                _fairyId, state.Kind, state.NodeId);
        }

        private void Render()
        {
            FairyWishConfigs.FairyRow cfg = FairyWishConfigs.GetFairy(_fairyId);
            FairyWishModel.FairyEntry entry = FairyWishModel.Instance.GetFairy(_fairyId);
            if (_fairy_name != null) _fairy_name.text = cfg?.Name ?? ("仙灵 " + _fairyId);
            int combat = 0;
            if (entry != null)
                for (int i = 0; i < entry.NodeList.Count; i++) combat += entry.NodeList[i].Combat;
            if (_lb_fighting != null) _lb_fighting.text = combat.ToString();
            FairyWishModel.OperateState operate = FairyWishModel.Instance.GetOperateState(
                _fairyId, 0, Shenxiao.Module.Core.Role.RoleModel.Instance.Level);
            if (btn_lb != null) btn_lb.text = operate.Kind == FairyWishModel.OperateKind.ActivateNode ? "激活"
                : operate.Kind == FairyWishModel.OperateKind.PurchaseRequired ? "需购买" : "未满足条件";
            if (_btn_operate != null) _btn_operate.gameObject.SetActive(operate.Kind != FairyWishModel.OperateKind.Maxed);
            if (_small_condition != null)
                _small_condition.text = entry == null ? "数据加载中" : "已激活节点 " + CountActivated(entry) + "/" + entry.NodeList.Count;
        }

        private static int CountActivated(FairyWishModel.FairyEntry entry)
        {
            int count = 0;
            for (int i = 0; i < entry.NodeList.Count; i++) if (entry.NodeList[i].IsActivate != 0) count++;
            return count;
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
