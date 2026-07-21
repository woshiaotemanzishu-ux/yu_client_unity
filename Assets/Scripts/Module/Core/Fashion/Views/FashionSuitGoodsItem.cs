using Shenxiao.Generated.UI.Fashion;
using Shenxiao.Module.Core.Common;
using TMPro;
using UnityEngine;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>套装四件条件格。时装类显示真实物品图标，其余幻化类显示明确的类型/配置 id。</summary>
    public sealed class FashionSuitGoodsItem : FashionSuitGoodsItemBind
    {
        private BaseAwardItem _award;
        private TextMeshProUGUI _label;

        public void SetData(FashionConfigs.SuitCondition condition, GameObject awardTemplate)
        {
            // 先取模板自身文案；BaseAwardItem 内也有 TMP 数量文本，实例化后再泛查会误把它当条件名。
            if (_label == null) _label = GetComponentInChildren<TextMeshProUGUI>(true);
            bool isFashion = condition.Type == 1 && condition.TypeId > 0;
            if (isFashion && _award == null && awardTemplate != null && _box_con != null)
            {
                GameObject go = Instantiate(awardTemplate, _box_con);
                go.SetActive(true);
                _award = go.GetComponent<BaseAwardItem>();
                if (_award != null) _award.SetScale(0.62f);
            }
            if (_award != null)
            {
                _award.gameObject.SetActive(isFashion);
                if (isFashion) _award.SetData(condition.TypeId, 1);
            }

            if (_label == null && _box_con != null)
            {
                GameObject go = new GameObject("ConditionLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(_box_con, false);
                RectTransform rt = (RectTransform)go.transform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 2f);
                rt.sizeDelta = new Vector2(0f, 25f);
                _label = go.GetComponent<TextMeshProUGUI>();
                _label.alignment = TextAlignmentOptions.Center;
                _label.fontSize = 17f;
                _label.color = new Color32(102, 57, 21, 255);
            }
            if (_label != null) _label.text = ConditionText(condition);
        }

        private static string ConditionText(FashionConfigs.SuitCondition c)
        {
            if (c.Type == 1)
            {
                string name = GoodsModel.GetGoodsName(c.TypeId);
                return string.IsNullOrEmpty(name) ? (c.SubType == 3 ? "发饰" : "时装") : name;
            }
            string kind = c.SubType switch
            {
                1 => "坐骑",
                2 => "精灵",
                3 => "羽翼",
                4 => "圣器",
                5 => "神兵",
                12 => "背饰",
                _ => "幻化",
            };
            return kind + " " + c.TypeId;
        }
    }
}
