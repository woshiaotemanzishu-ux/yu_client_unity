using System.Collections.Generic;
using System.Linq;
using Shenxiao.Common.Prefs;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Login;
using UnityEngine;
using UnityEngine.UI;

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

            // content 锚到顶(转换产物 pivot 可能在中心:改 sizeDelta 会向上下两头长,
            // 把前几行顶出视口——「4 个角色只见 2 个」的根因)。行坐标从 content 顶部往下排。
            RectTransform content = _panel_item.content;
            content.anchorMin = new Vector2(content.anchorMin.x, 1f);
            content.anchorMax = new Vector2(content.anchorMax.x, 1f);
            content.pivot = new Vector2(content.pivot.x, 1f);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
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
        private async void FillSlot(LoginSelectRoleItemBind bind, int index, bool selected)
        {
            bool isRole = index < _roles.Count;
            bind._img_bg2.enabled = isRole;
            bind._lb_name.enabled = isRole;
            bind._lb_lv.enabled = isRole;
            bind._lb_turn.enabled = isRole;
            bind._img_sc.enabled = false;
            Image head = EnsureHeadIcon(bind);
            head.gameObject.SetActive(isRole);

            if (!isRole)
            {
                _ = Shenxiao.Framework.Res.ResManager.SetImageAsync(bind._img_bg,
                        "resource/game/login/texture/ui_Login_04.png");
                return;
            }

            GameRoleInfo role = _roles[index];
            bind._lb_name.text = role.DisplayName;
            bind._lb_turn.text = role.Turn + "转";
            bool isSc = role.Level > SC_LEVEL;
            bind._img_sc.enabled = isSc;
            bind._lb_lv.text = (isSc ? role.Level - SC_LEVEL : role.Level) + "级";

            string bg = selected ? "ui_Login_02" : "ui_Login_03";
            string bg2 = selected ? "ui_Login_05" : "ui_Login_06";
            _ = Shenxiao.Framework.Res.ResManager.SetImageAsync(bind._img_bg, $"resource/game/login/texture/{bg}.png");
            // 内框等图就位(SetNativeSize 落定)后再对齐头像,否则头像按旧矩形摆会偏
            await Shenxiao.Framework.Res.ResManager.SetImageAsync(bind._img_bg2, $"resource/game/login/texture/{bg2}.png");
            if (bind == null || head == null) return;
            SyncHeadRect(head, (RectTransform)bind._img_bg2.transform);

            // 头像:config_dress_up_cfg(按转生数选装扮,screen 按职业给图标);自定义头像 picture 待头像线。
            // 直接赋 sprite,不走 SetImageAsync——SetNativeSize 会把头像撑回原图大小盖出框外
            string headIcon = LoginConfigs.HeadIconPath(role.Career, role.Turn);
            if (!string.IsNullOrEmpty(headIcon))
            {
                Sprite s = await Shenxiao.Framework.Res.ResManager.LoadAsync<Sprite>(headIcon);
                if (s != null && head != null)
                {
                    head.sprite = s;
                    head.enabled = true;
                }
            }
        }

        /// <summary>槽位头像(对标 CustomHeadItem,贴在内框 _img_bg2 下层;通用头像组件出来后替换)。</summary>
        private Image EnsureHeadIcon(LoginSelectRoleItemBind bind)
        {
            RectTransform frame = (RectTransform)bind._img_bg2.transform;
            Transform exist = frame.parent.Find("__head");
            if (exist != null) return exist.GetComponent<Image>();

            var go = new GameObject("__head", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(frame.parent, false);
            rt.SetSiblingIndex(frame.GetSiblingIndex()); // 插到内框下层,框压在头像上
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.enabled = false; // 图就位前不显示白块
            return img;
        }

        /// <summary>头像矩形对齐内框中心(内框图 SetNativeSize 落定后调用)。</summary>
        private static void SyncHeadRect(Image head, RectTransform frame)
        {
            var rt = (RectTransform)head.transform;
            Vector2 size = frame.sizeDelta;
            rt.anchorMin = frame.anchorMin;
            rt.anchorMax = frame.anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            // frame.anchoredPosition 是其 pivot 点的位置 → 换算到中心点
            rt.anchoredPosition = frame.anchoredPosition
                + new Vector2((0.5f - frame.pivot.x) * size.x, (0.5f - frame.pivot.y) * size.y);
            rt.sizeDelta = size * 0.88f;
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

        /// <summary>
        /// 中央 3D 模型:角色 10000 形象数据换装(衣/头饰/武器/翅膀/背饰,对标 Util.GetRoleClotheId 系列),
        /// 字段为 0 时回退职业默认装。天启/史诗套装/神殿觉醒 overrides + 时装贴图待形象线。
        /// </summary>
        private async void ShowRoleModel()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _roles.Count) return;
            GameRoleInfo role = _roles[_selectedIndex];
            var option = LoginConfigs.CreateRoleOptions().FirstOrDefault(o => o.Career == role.Career);
            if (option == null) return;
            LoginConfigs.CareerRes res = LoginConfigs.GetCreateRes(option.Career, option.Sex);
            if (res == null) return;
            var figure = role.figure;

            int selectedAtRequest = _selectedIndex;
            GameObject model = await RoleModelAssembler.BuildAsync(new RoleModelSpec
            {
                Career = option.Career,
                ClotheRes = figure != null && figure.ClotheModelId > 0 ? figure.ClotheModelId : res.RoleRes,
                HeadRes = figure != null && figure.HeadModelId > 0 ? figure.HeadModelId : res.HeadRes,
                WeaponRes = figure != null && figure.WeaponModelId > 0 ? figure.WeaponModelId : res.WeaponRes,
                WingId = figure != null ? figure.WingId : 0,
                BackOrnamentId = figure != null ? figure.BackOrnamentId : 0,
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
