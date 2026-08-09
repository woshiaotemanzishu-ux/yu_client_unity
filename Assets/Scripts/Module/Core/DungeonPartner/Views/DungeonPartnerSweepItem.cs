using Shenxiao.Generated.UI.DungeonPartner;

namespace Shenxiao.Module.Core.DungeonPartner.Views
{
    public sealed class DungeonPartnerSweepItem : DungeonPartnerSweepItemBind
    {
        public void SetProgress(int totalScore)
        {
            if (_lb_progress != null) _lb_progress.text = UnityEngine.Mathf.FloorToInt(UnityEngine.Mathf.Clamp01(totalScore / 27f) * 100f) + "%";
        }
    }
}
