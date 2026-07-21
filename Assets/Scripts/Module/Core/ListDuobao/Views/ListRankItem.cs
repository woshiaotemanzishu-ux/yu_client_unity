using System.Collections.Generic;
using Shenxiao.Generated.UI.ListDuobao;
using UnityEngine;

namespace Shenxiao.Module.Core.ListDuobao
{
    public sealed class ListRankItem : ListRankItemBind
    {
        public void SetData(int rank, string name, long score, int needScore,
            IReadOnlyList<ListDuobaoConfigs.RewardEntry> rewards, GameObject goodsTemplate)
        {
            bool vacant = string.IsNullOrEmpty(name);
            if (_lb_rank != null) _lb_rank.text = rank > 3 ? rank.ToString() : "";
            if (_lb_name != null) _lb_name.text = vacant ? "虚位以待" : name;
            if (_lb_score != null)
                _lb_score.text = vacant ? "上榜条件：" + needScore + "积分" : "积分：" + score;
            if (_img_rank != null) _img_rank.gameObject.SetActive(rank > 0 && rank <= 3);

            if (_gp_reward == null || goodsTemplate == null) return;
            Transform parent = _gp_reward.content != null ? _gp_reward.content : _gp_reward.transform;
            GameObject go = Instantiate(goodsTemplate, parent);
            go.SetActive(true);
            go.GetComponent<ListGoodsItem>()?.SetData(rewards);
        }
    }
}
