using System.Collections.Generic;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Login;
using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 选角页:左侧角色列表(_tpl_LoginSelectRoleItem,数据=10000 回包),
    /// 「踏入仙界」用选中角色进入游戏(10004)。中央 _gp_model 是 3D 模型位,待 .lh 转换线。
    /// </summary>
    public sealed class LoginSelectRoleView : LoginSelectRoleViewBind
    {
        private const float ITEM_SPACING = 12f;

        private readonly List<GameObject> _items = new List<GameObject>();
        private long _selectedRoleId;

        protected override void OnInit()
        {
            UIUtil.AddClick(_img_enter, OnClickEnter);
            UIUtil.AddClick(_img_return, OnClickReturn);
        }

        protected override void OnShow(object args)
        {
            BuildList();
        }

        private void BuildList()
        {
            foreach (GameObject go in _items) Destroy(go);
            _items.Clear();

            IReadOnlyList<GameRoleInfo> roles = LoginModel.Instance.Roles;
            if (roles.Count > 0) _selectedRoleId = roles[0].roleId;

            RectTransform content = _panel_item.content;
            RectTransform tplRect = (RectTransform)_tpl_LoginSelectRoleItem.transform;
            float itemHeight = tplRect.sizeDelta.y;

            for (int i = 0; i < roles.Count; i++)
            {
                GameRoleInfo role = roles[i];
                GameObject item = Instantiate(_tpl_LoginSelectRoleItem, content);
                item.SetActive(true);
                ((RectTransform)item.transform).anchoredPosition = new Vector2(0f, -i * (itemHeight + ITEM_SPACING));

                var bind = item.GetComponent<LoginSelectRoleItemBind>();
                if (bind == null)
                {
                    GameLog.Error("Login", "选角行缺 LoginSelectRoleItemBind,重跑回填");
                    continue;
                }
                bind._lb_name.text = role.DisplayName;
                bind._lb_lv.text = role.Level + "级";
                bind._lb_turn.text = role.Turn + "转";
                long captured = role.roleId;
                UIUtil.AddClick(bind._img_bg, () => OnClickRole(captured));
                UIUtil.AddClick(bind._img_bg2, () => OnClickRole(captured));
                _items.Add(item);
            }
            // 列表尾部追加「+创建角色」入口(老客户端在角色列表末尾,复用行模板)
            GameObject createItem = Instantiate(_tpl_LoginSelectRoleItem, content);
            createItem.SetActive(true);
            ((RectTransform)createItem.transform).anchoredPosition =
                new Vector2(0f, -roles.Count * (itemHeight + ITEM_SPACING));
            var createBind = createItem.GetComponent<LoginSelectRoleItemBind>();
            if (createBind != null)
            {
                createBind._lb_name.text = "+ 创建角色";
                createBind._lb_turn.text = string.Empty;
                createBind._lb_lv.text = string.Empty;
                createBind._img_sc.enabled = false;
                UIUtil.AddClick(createBind._img_bg, LoginFlow.ShowCreateRole);
                UIUtil.AddClick(createBind._img_bg2, LoginFlow.ShowCreateRole);
            }
            _items.Add(createItem);

            content.sizeDelta = new Vector2(content.sizeDelta.x, (roles.Count + 1) * (itemHeight + ITEM_SPACING));
        }

        private void OnClickRole(long roleId)
        {
            _selectedRoleId = roleId;
            GameLog.Info("Login", "选中角色 role_id={0}", roleId);
        }

        private void OnClickEnter()
        {
            if (_selectedRoleId == 0)
            {
                GameLog.Warn("Login", "未选中角色");
                return;
            }
            LoginController.Instance.EnterGameWithRole(_selectedRoleId);
        }

        private void OnClickReturn()
        {
            LoginFlow.BackToEnter();
        }
    }
}
