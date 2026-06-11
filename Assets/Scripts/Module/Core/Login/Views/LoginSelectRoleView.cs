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

        // ConfigLogin.SelectRole.ModelPos 与 show_model_data.scale=0.5 的镜像
        // TODO(配表线):ConfigManager 接入 ConfigLogin 后替换
        private static readonly Vector2 MODEL_POS = new Vector2(0f, 2.3f);
        private const float MODEL_SCALE = 0.5f;

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
            ShowRoleModel();
        }

        protected override void OnHide()
        {
            Shenxiao.Common.UI3D.UIModelStage.Clear();
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
            ShowRoleModel();
        }

        /// <summary>中央 3D 模型:按选中角色职业的默认装展示(时装外观待配表线)。</summary>
        private async void ShowRoleModel()
        {
            int career = 0;
            foreach (GameRoleInfo role in LoginModel.Instance.Roles)
            {
                if (role.roleId == _selectedRoleId) { career = role.Career; break; }
            }
            // TODO(配表线):ConfigLogin.CreateRole.Res.role_res;此处镜像 剑士1111/武姬1213/枪使1300/弓手1400
            int roleRes = career == 1 ? 1111 : career == 2 ? 1213 : career == 3 ? 1300 : career == 4 ? 1400 : 0;
            if (roleRes == 0) return;
            string key = $"object/role/model_clothe_{roleRes}/model_clothe_{roleRes}";
            GameObject prefab = await Shenxiao.Framework.Res.ResManager.LoadAsync<GameObject>(key);
            if (prefab == null)
            {
                GameLog.Warn("Login", "角色模型未找到(先用 Laya3D 转换器生成):{0}", key);
                return;
            }
            Shenxiao.Common.UI3D.UIModelStage.Show(_gp_model, prefab, MODEL_SCALE, MODEL_POS);
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
