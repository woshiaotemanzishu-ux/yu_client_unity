using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.BossField;

namespace Shenxiao.Module.Core.Boss.Views.BossField
{
    public sealed class BossFieldTiredNewView : BossFieldTiredNewViewBind
    {
        protected override void OnInit()
        {
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (close != null) UIUtil.AddClick(close, Hide);
            BindRect(use, "疲劳道具使用依赖 Goods 正式流程，当前 blocker");
            if (go != null) UIUtil.AddClick(go, () => GameLog.Info("BossField", "VIP跳转为跨模块 blocker"));
        }
        protected override void OnShow(object args)
        {
            BossModel.BossTypeState state = BossModel.Instance.GetBossState(BossModel.BossType.Field);
            BossModel.VitInfo vit = BossModel.Instance.GetVit(BossModel.BossType.Field);
            int current = vit?.Vit ?? state?.Vit ?? 0;
            int max = vit?.MaxVit ?? state?.AllTired ?? 0;
            if (tired != null) tired.text = current + "/" + max;
            if (tired_tip != null) tired_tip.text = "当前大妖体力";
            if (_lb_time != null) _lb_time.text = "恢复时间需运行态校准";
            if (btn_text != null) btn_text.text = "使用";
            if (tip != null) tip.text = "提升VIP可增加体力上限";
        }
        private static void BindRect(UnityEngine.Component target, string message)
        {
            if (target == null) return;
            UnityEngine.UI.Image image = target.GetComponentInChildren<UnityEngine.UI.Image>(true);
            if (image != null) UIUtil.AddClick(image, () => GameLog.Info("BossField", message));
        }
    }
}
