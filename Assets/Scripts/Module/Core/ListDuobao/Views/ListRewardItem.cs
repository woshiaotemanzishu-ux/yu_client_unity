using Shenxiao.Generated.UI.ListDuobao;
using UnityEngine;

namespace Shenxiao.Module.Core.ListDuobao
{
    public sealed class ListRewardItem : ListRewardItemBind
    {
        public void SetData(ListDuobaoConfigs.StageRow row, int gotType, long score, GameObject goodsTemplate)
        {
            if (row == null) return;
            if (_lb_msg != null) _lb_msg.text = "积分达到" + row.NeedValue + "可领取";
            if (_lb_score != null) _lb_score.text = "(" + score + "/" + row.NeedValue + ")";
            if (_img_have != null) _img_have.gameObject.SetActive(gotType == 2);

            if (_gp_item == null || goodsTemplate == null) return;
            Transform parent = _gp_item.content != null ? _gp_item.content : _gp_item.transform;
            GameObject go = Instantiate(goodsTemplate, parent);
            go.SetActive(true);
            go.GetComponent<ListGoodsItem>()?.SetData(row.Reward);
        }
    }
}
