// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。
// 来源: cdn/resource/game/mainUI/MainUIChatView.json
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUIChatViewBind : BaseView
    {
        public Image _img_bg;
        public ScrollRect _panel_chat;
        public RectTransform _box_chat_con;
        public ScrollRect _panel_sys;
        public RectTransform _box_sys_con;
        public RectTransform _box_setting;
        public Image _img_setting;
        public RectTransform _box_friend;
        public Image _img_friend;
        public Image _img_friend_red;
        public RectTransform _box_shop;
        public Image _img_shop;
        public Image _img_shop_red;
        public RectTransform _box_shop_effect;
        public RectTransform _box_strengthen;
        public GameObject _tpl_MainUIChatItem;
        public GameObject _tpl_ActivityIcon;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_bg), _img_bg);
            EnsureBound(nameof(_panel_chat), _panel_chat);
            EnsureBound(nameof(_box_chat_con), _box_chat_con);
            EnsureBound(nameof(_panel_sys), _panel_sys);
            EnsureBound(nameof(_box_sys_con), _box_sys_con);
            EnsureBound(nameof(_box_setting), _box_setting);
            EnsureBound(nameof(_img_setting), _img_setting);
            EnsureBound(nameof(_box_friend), _box_friend);
            EnsureBound(nameof(_img_friend), _img_friend);
            EnsureBound(nameof(_img_friend_red), _img_friend_red);
            EnsureBound(nameof(_box_shop), _box_shop);
            EnsureBound(nameof(_img_shop), _img_shop);
            EnsureBound(nameof(_img_shop_red), _img_shop_red);
            EnsureBound(nameof(_box_shop_effect), _box_shop_effect);
            EnsureBound(nameof(_box_strengthen), _box_strengthen);
            EnsureBound(nameof(_tpl_MainUIChatItem), _tpl_MainUIChatItem);
            EnsureBound(nameof(_tpl_ActivityIcon), _tpl_ActivityIcon);
        }
    }
}
