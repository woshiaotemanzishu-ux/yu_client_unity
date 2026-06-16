// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/MainUISkillView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUISkillViewBind : BaseView
    {
        public RectTransform _box_partner_skill;
        public Image _img_partner_lock;
        public Image _img_auto_fight;
        public RectTransform skill_box;
        public Image _img_bg;
        public RectTransform _box_skill_con;
        public GameObject _tpl_MainUISkillItem;
        public GameObject _tpl_CirCleCdView;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_box_partner_skill), _box_partner_skill);
            EnsureBound(nameof(_img_partner_lock), _img_partner_lock);
            EnsureBound(nameof(_img_auto_fight), _img_auto_fight);
            EnsureBound(nameof(skill_box), skill_box);
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_box_skill_con), _box_skill_con);
            EnsureBound(nameof(_tpl_MainUISkillItem), _tpl_MainUISkillItem);
            EnsureBound(nameof(_tpl_CirCleCdView), _tpl_CirCleCdView);
        }
    }
}
