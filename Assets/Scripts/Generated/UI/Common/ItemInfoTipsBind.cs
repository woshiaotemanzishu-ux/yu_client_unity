// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/common/ItemInfoTips.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Common
{
    public partial class ItemInfoTipsBind : BaseView
    {
        public Image bgImg;
        public Image quaImg;
        public TextMeshProUGUI goods_name;
        public RectTransform iconBox;
        public TextMeshProUGUI type_text;
        public TextMeshProUGUI quantity_text;
        public TextMeshProUGUI level_text;
        public ScrollRect detail_scroller;
        public RectTransform Content;
        public RectTransform _Group1;
        public Image _Image1;
        public TextMeshProUGUI label1;
        public TextMeshProUGUI intro;
        public ScrollRect rewardsList;
        public RectTransform useBtn;
        public TextMeshProUGUI descLab;
        public TextMeshProUGUI descHtml;
        public Image titleImg;
        public RectTransform modelBox;
        public Image lImg;
        public Image rImg;
        public Image iconImg;
        public Image probabilityImg;
        public Image jackpotImg;
        public GameObject _tpl_BaseAwardItem;
        public GameObject _tpl_ItemInfoItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(bgImg), bgImg);
            EnsureBound(nameof(quaImg), quaImg);
            EnsureBound(nameof(goods_name), goods_name);
            EnsureBound(nameof(iconBox), iconBox);
            EnsureBound(nameof(type_text), type_text);
            EnsureBound(nameof(quantity_text), quantity_text);
            EnsureBound(nameof(level_text), level_text);
            EnsureBound(nameof(detail_scroller), detail_scroller);
            EnsureBound(nameof(Content), Content);
            EnsureBound(nameof(_Group1), _Group1);
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(label1), label1);
            EnsureBound(nameof(intro), intro);
            EnsureBound(nameof(rewardsList), rewardsList);
            EnsureBound(nameof(useBtn), useBtn);
            EnsureBound(nameof(descLab), descLab);
            EnsureBound(nameof(descHtml), descHtml);
            EnsureBound(nameof(titleImg), titleImg);
            EnsureBound(nameof(modelBox), modelBox);
            EnsureBound(nameof(lImg), lImg);
            EnsureBound(nameof(rImg), rImg);
            EnsureBound(nameof(iconImg), iconImg);
            EnsureBound(nameof(probabilityImg), probabilityImg);
            EnsureBound(nameof(jackpotImg), jackpotImg);
            EnsureBound(nameof(_tpl_BaseAwardItem), _tpl_BaseAwardItem);
            EnsureBound(nameof(_tpl_ItemInfoItem), _tpl_ItemInfoItem);
        }
    }
}
