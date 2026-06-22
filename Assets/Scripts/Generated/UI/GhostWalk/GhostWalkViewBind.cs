// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/ghostWalk/GhostWalkView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.GhostWalk
{
    public partial class GhostWalkViewBind : BaseView
    {
        public Image imgRewardBg;
        public Image btnShop;
        public Image _red_shop;
        public Image btnLast;
        public Image imgBoss;
        public Image btnProcess;
        public TextMeshProUGUI lblProcess;
        public Image btnGo;
        public TextMeshProUGUI lblGo;
        public Image btnTeam;
        public TextMeshProUGUI lblTeam;
        public ScrollRect panelRule;
        public TextMeshProUGUI htmlRuleDesc;
        public ScrollRect listReward;
        public TextMeshProUGUI htmlMode;
        public GameObject _tpl_CommonRewardItem;
        public GameObject _tpl_EquipmentItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(imgRewardBg), imgRewardBg);
            EnsureBound(nameof(btnShop), btnShop);
            EnsureBound(nameof(_red_shop), _red_shop);
            EnsureBound(nameof(btnLast), btnLast);
            EnsureBound(nameof(imgBoss), imgBoss);
            EnsureBound(nameof(btnProcess), btnProcess);
            EnsureBound(nameof(lblProcess), lblProcess);
            EnsureBound(nameof(btnGo), btnGo);
            EnsureBound(nameof(lblGo), lblGo);
            EnsureBound(nameof(btnTeam), btnTeam);
            EnsureBound(nameof(lblTeam), lblTeam);
            EnsureBound(nameof(panelRule), panelRule);
            EnsureBound(nameof(htmlRuleDesc), htmlRuleDesc);
            EnsureBound(nameof(listReward), listReward);
            EnsureBound(nameof(htmlMode), htmlMode);
            EnsureBound(nameof(_tpl_CommonRewardItem), _tpl_CommonRewardItem);
            EnsureBound(nameof(_tpl_EquipmentItem), _tpl_EquipmentItem);
        }
    }
}
