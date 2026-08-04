using Shenxiao.Generated.UI.Fashion;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.OutWard;
using Newtonsoft.Json.Linq;
using System.Linq;
using UnityEngine;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>套装四件条件格。时装类显示真实物品图标，其余幻化类显示明确的类型/配置 id。</summary>
    public sealed class FashionSuitGoodsItem : FashionSuitGoodsItemBind
    {
        private BaseAwardItem _award;

        public BaseAwardItem AwardItem => _award;

        public void SetData(FashionConfigs.SuitCondition condition, GameObject awardTemplate)
        {
            if (_award == null && awardTemplate != null && _box_con != null)
            {
                GameObject go = Instantiate(awardTemplate, _box_con);
                go.SetActive(true);
                _award = go.GetComponent<BaseAwardItem>();
                if (_award != null) _award.SetScale(0.62f);
            }
            int goodsId = ResolveGoodsId(condition);
            if (_award != null)
            {
                _award.gameObject.SetActive(goodsId > 0);
                if (goodsId > 0)
                {
                    _award.SetData(goodsId, 1);
                    _award.SetGray(GetStage(condition) < 0);
                    int fashionPos = condition != null && condition.Type == 1 ? condition.SubType : 0;
                    _award.SetClickCallBack(() => IllusionTipsFlow.Show(goodsId, fashionPos));
                }
            }
        }

        private static int GetStage(FashionConfigs.SuitCondition condition)
        {
            if (condition == null) return -1;
            if (condition.Type == 1)
            {
                FashionModel.FashionEntry fashion = FashionModel.Instance.GetActive(condition.SubType, condition.TypeId);
                return fashion?.StarLv ?? -1;
            }
            if (condition.Type != 2) return -1;
            OutWardModel.IllusionListVo illusion = OutWardModel.Instance.GetIllusionList(condition.SubType);
            OutWardModel.FigureBriefVo figure = illusion?.FigureList?.FirstOrDefault(
                value => value != null && value.Id == condition.TypeId);
            return figure?.Stage ?? -1;
        }

        private static int ResolveGoodsId(FashionConfigs.SuitCondition condition)
        {
            if (condition == null) return 0;
            if (condition.Type == 1) return condition.TypeId;
            if (condition.Type != 2) return 0;
            int career = Mathf.Max(1, Shenxiao.Module.Core.Role.RoleModel.Instance.Career);
            JObject row = OutWardConfigs.GetFigureRow(condition.SubType, condition.TypeId, career);
            int goodsId = row?.Value<int?>("goods_id") ?? 0;
            // 老端约定：碎片图标映射到实际物品图标（倒数第三位为 1 时减 100）。
            string value = goodsId.ToString();
            if (value.Length >= 3 && value[value.Length - 3] == '1') goodsId -= 100;
            return goodsId;
        }
    }
}
