using System;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.BossField;
using Shenxiao.Module.Core.Relive;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Boss.Views.BossField
{
    public sealed class BossFieldReliveView : BossFieldReliveViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_CustomHeadItem != null) _tpl_CustomHeadItem.SetActive(false);
            BindClick(_box_free_relive, () => ReliveController.Instance.RequestRelive(ReliveModel.DEFAULT_RELIVE_TYPE));
            BindClick(_box_relive, BlockSceneMode);
            BindClick(_box_relive_and_snatch, BlockSceneMode);
            BindClick(_btn_help, () => GameLog.Info("BossField", "公会求助 40060 为跨模块 blocker"));
            BindClick(_img_auto_relive, () => GameLog.Info("BossField", "自动复活依赖场景/货币状态，当前 blocker"));
            BindClick(_img_goods_icon, () => GameLog.Info("BossField", "复活药详情由 Common blocker 承载"));
        }

        protected override void OnShow(object args)
        {
            ReliveModel model = ReliveModel.Instance;
            if (_lb_killer_name != null) _lb_killer_name.text = model.KillerName ?? "";
            if (_lb_count_down != null) _lb_count_down.text = model.CanRelive ? "可复活" : "等待复活";
            if (_lb_to_safe != null) _lb_to_safe.text = "";
            if (_box_role_monster != null) _box_role_monster.gameObject.SetActive(model.KillerType == ReliveModel.KILLER_TYPE_MONSTER);
            if (_box_role_head != null) _box_role_head.gameObject.SetActive(model.KillerType == ReliveModel.KILLER_TYPE_ROLE);
        }

        private static void BlockSceneMode() =>
            GameLog.Info("BossField", "场景专属复活 mode 矩阵尚无权威 SceneType API，拒绝猜测发送");

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
