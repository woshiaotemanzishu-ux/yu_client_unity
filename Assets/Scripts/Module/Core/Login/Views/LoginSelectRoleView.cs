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
    /// 选角页(对标老客户端 LoginSelectRoleView.ts + LoginSelectRoleItem.ts)。
    ///
    /// Phase 2 / data-only:结构(模型台/飘带 ui_role_01_select/4 个交错扇形角色项/进入返回按钮)
    /// 已由快照烤进 prefab,这里【只绑数据】——不再运行时 Instantiate 角色项、不再新建飘带、不再摆位置
    /// (老的 BuildList/EnsureRoleRibbon/EnsureHeadIcon/SetPosition 已删)。
    ///
    /// 列表:遍历烤进 _panel_item/Content/Sprite 下的 LoginSelectRoleItem 节点,按 role_id 升序填充;
    /// 角色项超出角色数=空槽(创建入口,底图 ui_Login_04,点击切创角页),选中态换图(02/05),未选(03/06)。
    /// 默认选中上次登录角色(Prefs login.lastRoleId,对标 cookie LAST_LOGIN_ROLE_ID)。
    /// 中央 3D 模型 = 选中角色形象数据换装,字段为 0 回退职业默认装(真·运行时,保留原样)。
    /// </summary>
    public sealed class LoginSelectRoleView : LoginSelectRoleViewBind
    {
        public const string PREF_LAST_ROLE_ID = "login.lastRoleId";

        // 老客户端视图代码字面量:show_model_data.scale=0.5;等级>370 显示「升仙」角标且显示 level-370。
        private const int SC_LEVEL = 370;
        private const float MODEL_SCALE = 0.5f;

        /// <summary>烤进 prefab 的角色项:绑数据时缓存它内部的底图/胶囊/头像/文字(对标 LoginSelectRoleItemBind)。</summary>
        private struct RoleSlot
        {
            public GameObject root;
            public LoginSelectRoleItemBind bind; // 烤好的内部组件引用(_img_bg/_img_bg2/_lb_name/_hbox_lv/...)
            public Image head;                   // icon_sys_head:实际显示的系统头像图(不是 icon——那是自定义头像,烤制时空且 inactive)
            public GameObject headBlock;         // CustomHeadItem 整块:空槽时整块隐藏(连等级浮层 _level_gp 一起)
        }

        private readonly List<RoleSlot> _slots = new List<RoleSlot>();
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

        protected override void OnDispose()
        {
            _slots.Clear();
            _roles.Clear();
            _selectedIndex = -1;
        }

        private async void InitAsync()
        {
            await LoginConfigs.EnsureLoaded();
            _roles = LoginModel.Instance.Roles.OrderBy(r => r.roleId).ToList();
            BindBakedSlots();

            // 默认选中上次登录角色,找不到选第一个(对标老端 UpdateView 的 select_index)
            long lastRoleId = long.TryParse(PrefsManager.GetString(PREF_LAST_ROLE_ID, ""), out long v) ? v : 0;
            int selectIndex = 0;
            for (int i = 0; i < _roles.Count; i++)
            {
                if (_roles[i].roleId == lastRoleId) { selectIndex = i; break; }
            }
            _selectedIndex = -1;
            if (_roles.Count > 0) SelectSlot(selectIndex);
        }

        /// <summary>把数据绑到烤进 prefab 的角色项上(不再 Instantiate 模板,不再摆位置)。</summary>
        private void BindBakedSlots()
        {
            _slots.Clear();
            RectTransform content = ItemContainer();
            if (content == null)
            {
                GameLog.Error("Login", "选角缺 _panel_item 容器(烤制 prefab 结构异常?)");
                return;
            }

            // 收集烤进容器(_panel_item/Content/Sprite)的角色项(节点名 LoginSelectRoleItem),按层级序。
            var items = new List<Transform>();
            CollectItems(content, items);
            int slotCount = Mathf.Max(LoginConfigs.SelectRoleTotalCount(), _roles.Count);
            if (items.Count < slotCount)
            {
                GameLog.Warn("Login", "烤制角色项 {0} 少于需要槽位 {1}(TotalCount/角色数变了需重烤)", items.Count, slotCount);
            }

            for (int i = 0; i < items.Count; i++)
            {
                Transform item = items[i];
                bool used = i < slotCount;
                item.gameObject.SetActive(used);
                if (!used) continue;

                var bind = item.GetComponent<LoginSelectRoleItemBind>();
                if (bind == null)
                {
                    GameLog.Error("Login", "选角行缺 LoginSelectRoleItemBind,重跑回填");
                    continue;
                }

                // CustomHeadItem 在 _box_con 下,老的固定路径 "CustomHeadItem/..." 找不到(返回 null→头像不受控、
                // 烤制头像漏出)。改递归按名找;头像取 icon_sys_head(系统头像,实际显示的那个)。
                Transform headBlock = FindDeep(item, "CustomHeadItem");
                var slot = new RoleSlot
                {
                    root = item.gameObject,
                    bind = bind,
                    head = headBlock != null ? FindImageDeep(headBlock, "icon_sys_head") : null,
                    headBlock = headBlock != null ? headBlock.gameObject : null,
                };

                // 老客户端点击区是 _box_con(空容器);Unity 射线需要 Graphic,挂在烤好的底图/胶囊上。
                int captured = i;
                if (bind._img_bg != null)
                {
                    bind._img_bg.raycastTarget = true;
                    UIUtil.ClearClicks(bind._img_bg);   // BindBakedSlots 每次 OnShow 重跑,先清再加防监听叠加
                    UIUtil.AddClick(bind._img_bg, () => OnClickSlot(captured));
                }
                if (bind._img_bg2 != null)
                {
                    bind._img_bg2.raycastTarget = true;
                    UIUtil.ClearClicks(bind._img_bg2);
                    UIUtil.AddClick(bind._img_bg2, () => OnClickSlot(captured));
                }
                _slots.Add(slot);
            }

            // 先全部按未选态填一遍(空槽=创建入口),选中态由 SelectSlot 触发。
            for (int i = 0; i < _slots.Count; i++)
            {
                FillSlot(_slots[i], i, selected: false);
            }
        }

        /// <summary>对标 LoginSelectRoleItem.UpdateItem/UpdateState:空槽=创建入口,角色槽按选中态换图。</summary>
        private async void FillSlot(RoleSlot slot, int index, bool selected)
        {
            LoginSelectRoleItemBind bind = slot.bind;
            if (bind == null) return;

            bool isRole = index < _roles.Count;
            if (bind._img_bg2 != null) bind._img_bg2.enabled = isRole;
            if (bind._lb_name != null) bind._lb_name.enabled = isRole;
            if (bind._lb_lv != null) bind._lb_lv.enabled = isRole;
            if (bind._lb_turn != null) bind._lb_turn.enabled = isRole;
            if (bind._img_sc != null) bind._img_sc.enabled = false;
            // 整块头像显隐(SetActive 连带隐藏烤进的 icon_sys_head + 等级浮层,空槽不漏烤制头像)
            if (slot.headBlock != null) slot.headBlock.SetActive(isRole);

            if (!isRole)
            {
                // 换肤一律 nativeSize:false:保留烤进 prefab 的宽高(对标 Laya skin=),
                // SetNativeSize 会把行底图撑成原图大小。
                if (bind._img_bg != null)
                    _ = Shenxiao.Framework.Res.ResManager.SetImageAsync(bind._img_bg,
                            "resource/game/login/texture/ui_Login_04.png", nativeSize: false);
                return;
            }

            GameRoleInfo role = _roles[index];
            if (bind._lb_name != null) bind._lb_name.text = role.DisplayName;
            if (bind._lb_turn != null) bind._lb_turn.text = role.Turn + "转";
            bool isSc = role.Level > SC_LEVEL;
            if (bind._img_sc != null) bind._img_sc.enabled = isSc;
            if (bind._lb_lv != null) bind._lb_lv.text = (isSc ? role.Level - SC_LEVEL : role.Level) + "级";

            string bg = selected ? "ui_Login_02" : "ui_Login_03";
            string bg2 = selected ? "ui_Login_05" : "ui_Login_06";
            if (bind._img_bg != null)
                _ = Shenxiao.Framework.Res.ResManager.SetImageAsync(bind._img_bg,
                        $"resource/game/login/texture/{bg}.png", nativeSize: false);
            if (bind._img_bg2 != null)
                _ = Shenxiao.Framework.Res.ResManager.SetImageAsync(bind._img_bg2,
                        $"resource/game/login/texture/{bg2}.png", nativeSize: false);

            // 头像:config_dress_up_cfg(按转生数选装扮,screen 按职业给图标);自定义头像 picture 待头像线。
            // 直接赋 sprite 到烤好的 CustomHeadItem/icon 节点,不改其矩形。
            Image head = slot.head;
            if (head != null)
            {
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
            for (int i = 0; i < _slots.Count; i++)
            {
                FillSlot(_slots[i], i, selected: i == index);
            }
            ShowRoleModel();
        }

        /// <summary>
        /// 中央 3D 模型:角色 10000 形象数据换装(衣/头饰/武器/翅膀/背饰,对标 Util.GetRoleClotheId 系列),
        /// 字段为 0 时回退职业默认装。天启/史诗套装/神殿觉醒 overrides + 时装贴图待形象线。(真·运行时,保留原样)
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

        // ——— 角色项容器没绑上时按名兜底(烤制 prefab 容器路径 _panel_item/Content/Sprite)———
        private RectTransform ItemContainer()
        {
            // _panel_item 是 ScrollRect;角色项烤在它的 content 下(Content/Sprite)。
            if (_panel_item != null && _panel_item.content != null)
            {
                Transform sprite = _panel_item.content.Find("Sprite");
                if (sprite != null) return sprite as RectTransform;
                return _panel_item.content;
            }
            Transform t = transform.Find("_panel_item/Content/Sprite");
            if (t != null) return t as RectTransform;
            t = transform.Find("_panel_item/Content");
            return t as RectTransform;
        }

        /// <summary>递归收集容器下的角色项(节点名 LoginSelectRoleItem),按层级序;只取直接子层。</summary>
        private static void CollectItems(Transform container, List<Transform> outList)
        {
            for (int i = 0; i < container.childCount; i++)
            {
                Transform c = container.GetChild(i);
                if (c.name.StartsWith("LoginSelectRoleItem")) outList.Add(c);
            }
        }

        private static Image FindImage(Transform root, string path)
        {
            Transform t = root.Find(path);
            return t != null ? t.GetComponent<Image>() : null;
        }

        /// <summary>递归按名找后代节点(烤制结构里 CustomHeadItem 嵌在 _box_con 下,固定路径不稳)。</summary>
        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t != root && t.name == name) return t;
            return null;
        }

        private static Image FindImageDeep(Transform root, string name)
        {
            Transform t = FindDeep(root, name);
            return t != null ? t.GetComponent<Image>() : null;
        }
    }
}
