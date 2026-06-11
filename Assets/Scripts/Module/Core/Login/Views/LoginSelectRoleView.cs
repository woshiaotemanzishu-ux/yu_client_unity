using System.Collections.Generic;
using System.Linq;
using Shenxiao.Common.Prefs;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Login;
using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 选角页(对标老客户端 LoginSelectRoleView.ts + LoginSelectRoleItem.ts):
    /// 固定 SelectRole.TotalCount 个槽位,角色按 role_id 升序填充,空槽=「创建角色」入口
    /// (底图 ui_Login_04,点击切创角页)。选中态换底图(02/05),未选(03/06)。
    /// 默认选中上次登录角色(Prefs login.lastRoleId,对标 cookie LAST_LOGIN_ROLE_ID)。
    /// 中央模型 = 选中角色职业默认装 + idle(TODO 形象线:时装/套装/觉醒按 figure 数据换装)。
    /// </summary>
    public sealed class LoginSelectRoleView : LoginSelectRoleViewBind
    {
        public const string PREF_LAST_ROLE_ID = "login.lastRoleId";

        // 老客户端视图代码字面量:item.SetPosition(50, index*136)、show_model_data.scale=0.5、
        // 等级>370 显示「升仙」角标且显示 level-370
        private const float ITEM_X = 50f;
        private const float ITEM_STEP_Y = 136f;
        private const int SC_LEVEL = 370;
        private const float MODEL_SCALE = 0.5f;

        private readonly List<GameObject> _items = new List<GameObject>();
        private List<GameRoleInfo> _roles = new List<GameRoleInfo>();
        private int _selectedIndex = -1;

        protected override void OnInit()
        {
            UIUtil.AddClick(_img_enter, OnClickEnter);
            UIUtil.AddClick(_img_return, OnClickReturn);
        }

        protected override void OnShow(object args)
        {
            InitAsync();
        }

        protected override void OnHide()
        {
            UIModelStage.Clear();
        }

        private async void InitAsync()
        {
            await LoginConfigs.EnsureLoaded();
            BuildList();
        }

        private void BuildList()
        {
            foreach (GameObject go in _items) Destroy(go);
            _items.Clear();
            _selectedIndex = -1;

            _roles = LoginModel.Instance.Roles.OrderBy(r => r.roleId).ToList();
            int slotCount = Mathf.Max(LoginConfigs.SelectRoleTotalCount(), _roles.Count);

            RectTransform content = _panel_item.content;
            for (int i = 0; i < slotCount; i++)
            {
                GameObject item = Instantiate(_tpl_LoginSelectRoleItem, content);
                item.SetActive(true);
                ((RectTransform)item.transform).anchoredPosition = new Vector2(ITEM_X, -i * ITEM_STEP_Y);
                _items.Add(item);

                var bind = item.GetComponent<LoginSelectRoleItemBind>();
                if (bind == null)
                {
                    GameLog.Error("Login", "选角行缺 LoginSelectRoleItemBind,重跑回填");
                    continue;
                }
                // 老客户端点击区是 _box_con(空容器);Unity 射线需要 Graphic,挂行底图+内框
                int captured = i;
                UIUtil.AddClick(bind._img_bg, () => OnClickSlot(captured));
                UIUtil.AddClick(bind._img_bg2, () => OnClickSlot(captured));
                FillSlot(bind, i, selected: false);
            }
            content.sizeDelta = new Vector2(content.sizeDelta.x, slotCount * ITEM_STEP_Y);

            // 默认选中上次登录角色,找不到选第一个
            long lastRoleId = long.TryParse(PrefsManager.GetString(PREF_LAST_ROLE_ID, ""), out long v) ? v : 0;
            int selectIndex = 0;
            for (int i = 0; i < _roles.Count; i++)
            {
                if (_roles[i].roleId == lastRoleId) { selectIndex = i; break; }
            }
            if (_roles.Count > 0) SelectSlot(selectIndex);
        }

        /// <summary>对标 LoginSelectRoleItem.UpdateItem/UpdateState:空槽=创建入口,角色槽按选中态换图。</summary>
        private void FillSlot(LoginSelectRoleItemBind bind, int index, bool selected)
        {
            bool isRole = index < _roles.Count;
            bind._img_bg2.enabled = isRole;
            bind._lb_name.enabled = isRole;
            bind._lb_lv.enabled = isRole;
            bind._lb_turn.enabled = isRole;
            bind._img_sc.enabled = false;

            if (!isRole)
            {
                _ = Shenxiao.Framework.Res.ResManager.SetImageAsync(bind._img_bg,
                        "resource/game/login/texture/ui_Login_04.png");
                return;
            }

            GameRoleInfo role = _roles[index];
            string bg = selected ? "ui_Login_02" : "ui_Login_03";
            string bg2 = selected ? "ui_Login_05" : "ui_Login_06";
            _ = Shenxiao.Framework.Res.ResManager.SetImageAsync(bind._img_bg, $"resource/game/login/texture/{bg}.png");
            _ = Shenxiao.Framework.Res.ResManager.SetImageAsync(bind._img_bg2, $"resource/game/login/texture/{bg2}.png");

            bind._lb_name.text = role.DisplayName;
            bind._lb_turn.text = role.Turn + "转";
            bool isSc = role.Level > SC_LEVEL;
            bind._img_sc.enabled = isSc;
            bind._lb_lv.text = (isSc ? role.Level - SC_LEVEL : role.Level) + "级";
            // TODO(头像线):老客户端槽位里有 CustomHeadItem 角色头像,等通用头像组件再补
        }

        private void OnClickSlot(int index)
        {
            if (index >= _roles.Count)
            {
                LoginFlow.ShowCreateRole(); // 空槽=创建角色入口(对标老客户端 data.none 分支)
                return;
            }
            SelectSlot(index);
        }

        private void SelectSlot(int index)
        {
            if (_selectedIndex == index) return;
            _selectedIndex = index;
            for (int i = 0; i < _items.Count; i++)
            {
                var bind = _items[i].GetComponent<LoginSelectRoleItemBind>();
                if (bind != null) FillSlot(bind, i, selected: i == index);
            }
            ShowRoleModel();
        }

        /// <summary>中央 3D 模型:选中角色职业默认装 + ConfigModelAni 的 idle(TODO 形象线换装)。</summary>
        private async void ShowRoleModel()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _roles.Count) return;
            GameRoleInfo role = _roles[_selectedIndex];
            var option = LoginConfigs.CreateRoleOptions().FirstOrDefault(o => o.Career == role.Career);
            if (option == null) return;
            LoginConfigs.CareerRes res = LoginConfigs.GetCreateRes(option.Career, option.Sex);
            if (res == null) return;

            int selectedAtRequest = _selectedIndex;
            GameObject model = await RoleModelAssembler.BuildAsync(new RoleModelSpec
            {
                Career = option.Career,
                ClotheRes = res.RoleRes,
                WeaponRes = res.WeaponRes,
                HeadRes = res.HeadRes,
                Actions = LoginConfigs.RoleUIActions("LoginSelectRoleView"),
            });
            if (model == null) return;
            if (selectedAtRequest != _selectedIndex || !gameObject.activeInHierarchy)
            {
                Destroy(model);
                return;
            }
            UIModelStage.ShowInstance(_gp_model, model,
                MODEL_SCALE, LoginConfigs.GetModelPos("SelectRole", option.Career, option.Sex));
        }

        private void OnClickEnter()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _roles.Count)
            {
                GameLog.Warn("Login", "未选择角色");
                return;
            }
            LoginController.Instance.EnterGameWithRole(_roles[_selectedIndex].roleId);
        }

        private void OnClickReturn()
        {
            LoginFlow.BackToEnter();
        }
    }
}
