// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/team/TeamHallItem.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.Team
{
    public partial class TeamHallItemBind : BaseView
    {
        public Image _Image1;
        public RectTransform role_head;
        public Image _img_frame;
        public Image _img_role_head;
        public RectTransform head_click_group;
        public Image name_bg;
        public TextMeshProUGUI role_name;
        public TextMeshProUGUI level;
        public TextMeshProUGUI role_num;
        public TextMeshProUGUI online_state;
        public RectTransform applyBtn;
        public Image _Image2;
        public TextMeshProUGUI _Label1;
        public Image leader_tag;
        public GameObject _tpl_CustomHeadItem;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_Image1), _Image1);
            EnsureBound(nameof(role_head), role_head);
            EnsureBound(nameof(_img_frame), _img_frame);
            EnsureBound(nameof(_img_role_head), _img_role_head);
            EnsureBound(nameof(head_click_group), head_click_group);
            EnsureBound(nameof(name_bg), name_bg);
            EnsureBound(nameof(role_name), role_name);
            EnsureBound(nameof(level), level);
            EnsureBound(nameof(role_num), role_num);
            EnsureBound(nameof(online_state), online_state);
            EnsureBound(nameof(applyBtn), applyBtn);
            EnsureBound(nameof(_Image2), _Image2);
            EnsureBound(nameof(_Label1), _Label1);
            EnsureBound(nameof(leader_tag), leader_tag);
            EnsureBound(nameof(_tpl_CustomHeadItem), _tpl_CustomHeadItem);
        }
    }
}
