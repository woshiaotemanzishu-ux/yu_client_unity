using Shenxiao.Generated.UI.Boss;

namespace Shenxiao.Module.Core.Boss.Views.BossMain
{
    public sealed class BossDamageSubItemView : BossDamageSubItemBind
    {
        public void SetData(int rank, string roleName, long damage)
        {
            if (_lb_name != null) _lb_name.text = roleName ?? string.Empty;
            if (_lb_hurt != null) _lb_hurt.text = damage.ToString("N0");
            bool medalRank = rank > 0 && rank <= 3;
            if (_img_rank != null) _img_rank.gameObject.SetActive(medalRank);
            if (_lb_rank != null)
            {
                _lb_rank.gameObject.SetActive(!medalRank);
                _lb_rank.text = rank > 0 ? rank.ToString() : "--";
            }
        }
    }
}
