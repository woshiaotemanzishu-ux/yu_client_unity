using System.Collections.Generic;
using Shenxiao.Framework.Res;
using Shenxiao.Generated.UI.ListDuobao;
using UnityEngine;

namespace Shenxiao.Module.Core.ListDuobao
{
    public sealed class ListRankItem : ListRankItemBind
    {
        public void SetData(int rank, string name, long score, int needScore,
            IReadOnlyList<ListDuobaoConfigs.RewardEntry> rewards, GameObject goodsTemplate)
        {
            if (!IsInitialized) Show();
            bool vacant = string.IsNullOrEmpty(name);
            if (_lb_rank != null) _lb_rank.text = rank > 3 ? rank.ToString() : "";
            if (_lb_name != null) _lb_name.text = vacant ? "虚位以待" : name;
            if (_lb_score != null)
                _lb_score.text = vacant ? "上榜条件：<color=#0a953e>" + needScore + "</color>积分" : "积分：<color=#0a953e>" + score + "</color>";
            if (_lb_name != null) _lb_name.color = vacant ? new Color32(102, 57, 21, 255) : new Color32(209, 94, 0, 255);
            if (_img_rank != null)
            {
                _img_rank.gameObject.SetActive(true);
                _ = ResManager.SetImageAsync(_img_rank,
                    GameResPath.GetIcon("listDuobao", rank > 3 ? "ui_rank4" : "ui_rank" + rank), false, false);
            }

            if (_gp_reward == null || goodsTemplate == null) return;
            Transform parent = _gp_reward.content != null ? _gp_reward.content : _gp_reward.transform;
            GameObject go = Instantiate(goodsTemplate, parent);
            ListGoodsItem goods = go.GetComponent<ListGoodsItem>();
            if (goods != null) goods.SetData(rewards);
            else go.SetActive(true);
        }
    }
}
