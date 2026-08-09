using System;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.BossField;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Boss.Views.BossField
{
    public sealed class BossFieldRoomItem : BossFieldRoomItemBind
    {
        public sealed class Args
        {
            public readonly int BossType;
            public readonly int BossId;
            public Args(int bossType, int bossId) { BossType = bossType; BossId = bossId; }
        }

        private int _bossType;
        private int _bossId;

        protected override void OnInit()
        {
            BindClick(_box_go, Enter);
            BindClick(_btn_add, BossFieldFlow.OpenTired);
            BindClick(_lb_t2, BossFieldFlow.OpenVitBuy);
        }

        protected override void OnShow(object args)
        {
            Args data = args as Args;
            if (data != null) { _bossType = data.BossType; _bossId = data.BossId; }
            BossModel.BossTypeState state = BossModel.Instance.GetBossState(_bossType);
            BossModel.VitInfo vit = BossModel.Instance.GetVit(BossModel.BossType.Field);
            int current = vit?.Vit ?? state?.Vit ?? 0;
            int max = vit?.MaxVit ?? state?.AllTired ?? 0;
            if (_html_vit != null) _html_vit.text = string.Format("体力 {0}/{1}", current, max);
            if (_lb_t1 != null) _lb_t1.text = "体力恢复";
            if (_lb_t2 != null) _lb_t2.text = vit != null && vit.BackVit > 0 ? "可找回 " + vit.BackVit : "";
            if (_img_up != null) _img_up.gameObject.SetActive(false);
            if (_img_down != null) _img_down.gameObject.SetActive(false);
        }

        private void Enter() { if (_bossId > 0) BossController.Instance.EnterBoss(_bossType, _bossId); }

        private static void BindClick(Component target, Action action)
        {
            if (target == null) return;
            Image image = target as Image ?? target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
        }
    }
}
