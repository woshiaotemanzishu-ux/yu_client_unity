using System.Collections.Generic;
using System.Linq;
using Shenxiao.Common.Prefs;
using Shenxiao.Common.Proto;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 选角页(重构版,自包含独立 prefab)——取代老端碎片化的 LoginSelectRoleView + LoginSelectRoleItem。
    ///
    /// 结构(由 RoleSelectCreator 纯代码建树):全屏背景 / 角色卡容器 _box_items + 一张隐藏模板卡 _tpl_item
    /// (内部:底图 _img_bg / 选中胶囊 _img_bg2 / 名字 _lb_name / 转生 _lb_turn / 等级 _lb_lv / 升仙角标 _img_sc /
    /// 头像位 _img_head)/ 3D 模型容器 _gp_model / 踏入仙界按钮 _img_enter / 返回按钮 _img_return。
    ///
    /// 本类只做:① 数据绑定(克隆模板卡填角色数据、空槽=创建入口) ② 功能性状态切换(选中态换图 / 显示 3D 模型)。
    /// 不写颜色/字号/尺寸样式(建树期 Creator + 用户手调的事)。
    /// 列表/模型/头像逻辑从老 LoginSelectRoleView.cs 原样搬,只把"取烤进节点"换成"克隆代码模板卡 + 按名找子节点"。
    ///
    /// ── 对接(供主控改写 LoginFlow 时对照)─────────────────────────────────────────────
    /// 暴露给 LoginFlow 回调的 public 方法:
    ///   public void Refresh()  —— 从 LoginController.Instance.Model.Roles 建角色卡、默认选中、显示模型(本页主入口)。
    /// 调用的 LoginFlow 静态方法(与老 view 一致):
    ///   LoginFlow.ShowCreateRole()  —— 空槽 / 「创建角色」入口。
    ///   LoginFlow.BackToEnter()     —— 返回(回踏入仙界页,断开游戏服重选)。
    /// 调用的 LoginController:
    ///   LoginController.Instance.EnterGameWithRole(roleId) —— 踏入仙界(选中角色进游戏)。
    /// 保留常量:public const string PREF_LAST_ROLE_ID(与老 LoginSelectRoleView 同名同值,
    ///   LoginController.EnterGameWithRole 写它做"下次默认选中",避免编译/语义断裂)。
    /// OnShow 会自动 Refresh;LoginFlow 也可在 Show 后显式调 Refresh()(幂等)。
    /// </summary>
    [UIView("prefabs/ui/login/roleselectview")]
    public sealed class RoleSelectView : BaseView
    {
        // 对标老客户端 cookie LAST_LOGIN_ROLE_ID,LoginController.EnterGameWithRole 写它(同名同值保持不变)。
        public const string PREF_LAST_ROLE_ID = "login.lastRoleId";

        // 老客户端视图字面量:show_model_data.scale=0.5;等级>370 显示「升仙」角标且显示 level-370。
        private const int SC_LEVEL = 370;
        private const float MODEL_SCALE = 0.5f;

        [Header("3D 模型容器")]
        public RectTransform _gp_model;

        [Header("角色卡列表")]
        public RectTransform _box_items;   // 角色卡父容器(克隆模板填进这里)
        public RectTransform _tpl_item;    // 模板卡(prefab 内默认隐藏,作克隆源)

        [Header("按钮")]
        public Image _img_enter;           // 踏入仙界
        public Image _img_return;          // 返回

        /// <summary>克隆出来的角色卡:缓存它内部的底图/胶囊/头像/文字(对标老 LoginSelectRoleItemBind)。</summary>
        private struct RoleSlot
        {
            public GameObject root;
            public Image bg;       // _img_bg:底图(选中/未选 + 空槽创建图;点击命中体)
            public Image bg2;      // _img_bg2:选中胶囊
            public Image sc;       // _img_sc:升仙角标
            public Image head;     // _img_head:头像
            public TextMeshProUGUI name;   // _lb_name
            public TextMeshProUGUI turn;   // _lb_turn
            public TextMeshProUGUI lv;     // _lb_lv
        }

        private readonly List<RoleSlot> _slots = new List<RoleSlot>();
        private List<GameRoleInfo> _roles = new List<GameRoleInfo>();
        private int _selectedIndex = -1;

        protected override void OnShow(object args)
        {
            BindStaticClicks();
            Refresh();
        }

        protected override void OnHide()
        {
            UIModelStage.Clear();
        }

        protected override void OnDispose()
        {
            ClearSlots();
            _roles.Clear();
            _selectedIndex = -1;
        }

        private void BindStaticClicks()
        {
            ClearAndAddClick(_img_enter, OnClickEnter);
            ClearAndAddClick(_img_return, OnClickReturn);
        }

        // ---------------------------------------------------------------- 数据绑定(主入口)

        /// <summary>从角色列表建角色卡、默认选中上次登录角色、显示其 3D 模型(供 LoginFlow 调)。</summary>
        public async void Refresh()
        {
            await LoginConfigs.EnsureLoaded();
            _roles = LoginModel.Instance.Roles.OrderBy(r => r.roleId).ToList();
            RebuildSlots();

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

        /// <summary>按角色数 + 固定槽位数克隆模板卡(超出角色数=空槽创建入口),全部先按未选态填一遍。</summary>
        private void RebuildSlots()
        {
            ClearSlots();
            if (_box_items == null || _tpl_item == null)
            {
                GameLog.Error("Login", "选角缺角色卡容器/模板(_box_items 或 _tpl_item 未绑,重新生成 prefab)");
                return;
            }

            int slotCount = Mathf.Max(LoginConfigs.SelectRoleTotalCount(), _roles.Count);
            for (int i = 0; i < slotCount; i++)
            {
                GameObject clone = Instantiate(_tpl_item.gameObject, _box_items);
                clone.name = "RoleItem_" + i;
                clone.SetActive(true);

                var slot = new RoleSlot
                {
                    root = clone,
                    bg = FindImage(clone.transform, "_img_bg"),
                    bg2 = FindImage(clone.transform, "_img_bg2"),
                    sc = FindImage(clone.transform, "_img_sc"),
                    head = FindImage(clone.transform, "_img_head"),
                    name = FindText(clone.transform, "_lb_name"),
                    turn = FindText(clone.transform, "_lb_turn"),
                    lv = FindText(clone.transform, "_lb_lv"),
                };

                // 老客户端点击区是空容器;Unity 射线需要 Graphic,绑在底图上(每次重建先清再加防叠加)。
                int captured = i;
                if (slot.bg != null)
                {
                    slot.bg.raycastTarget = true;
                    UIUtil.ClearClicks(slot.bg);
                    UIUtil.AddClick(slot.bg, () => OnClickSlot(captured));
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
            bool isRole = index < _roles.Count;
            if (slot.bg2 != null) slot.bg2.enabled = isRole;
            if (slot.name != null) slot.name.enabled = isRole;
            if (slot.lv != null) slot.lv.enabled = isRole;
            if (slot.turn != null) slot.turn.enabled = isRole;
            if (slot.sc != null) slot.sc.enabled = false;
            if (slot.head != null) slot.head.enabled = isRole;

            if (!isRole)
            {
                // 换肤一律 nativeSize:false:保留模板卡宽高(对标 Laya skin=),否则 SetNativeSize 撑回原图大小。
                if (slot.bg != null)
                    _ = ResManager.SetImageAsync(slot.bg,
                            "resource/game/login/texture/ui_Login_04.png", nativeSize: false);
                return;
            }

            GameRoleInfo role = _roles[index];
            if (slot.name != null) slot.name.text = role.DisplayName;
            if (slot.turn != null) slot.turn.text = role.Turn + "转";
            bool isSc = role.Level > SC_LEVEL;
            if (slot.sc != null) slot.sc.enabled = isSc;
            if (slot.lv != null) slot.lv.text = (isSc ? role.Level - SC_LEVEL : role.Level) + "级";

            string bg = selected ? "ui_Login_02" : "ui_Login_03";
            string bg2 = selected ? "ui_Login_05" : "ui_Login_06";
            if (slot.bg != null)
                _ = ResManager.SetImageAsync(slot.bg, $"resource/game/login/texture/{bg}.png", nativeSize: false);
            if (slot.bg2 != null)
                _ = ResManager.SetImageAsync(slot.bg2, $"resource/game/login/texture/{bg2}.png", nativeSize: false);

            // 头像:config_dress_up_cfg(按转生数选装扮,screen 按职业给图标);自定义头像 picture 待头像线。
            Image head = slot.head;
            if (head != null)
            {
                string headIcon = LoginConfigs.HeadIconPath(role.Career, role.Turn);
                if (!string.IsNullOrEmpty(headIcon))
                {
                    Sprite s = await ResManager.LoadAsync<Sprite>(headIcon);
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
        /// 中央 3D 模型:角色形象数据换装(衣/头饰/武器/翅膀/背饰,对标 Util.GetRoleClotheId 系列),
        /// 字段为 0 回退职业默认装。天启/史诗套装/神殿觉醒 overrides + 时装贴图待形象线。(真·运行时,原样搬)
        /// </summary>
        private async void ShowRoleModel()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _roles.Count) return;
            GameRoleInfo role = _roles[_selectedIndex];
            var option = LoginConfigs.CreateRoleOptions().FirstOrDefault(o => o.Career == role.Career);
            if (option == null) return;
            LoginConfigs.CareerRes res = LoginConfigs.GetCreateRes(option.Career, option.Sex);
            if (res == null) return;
            FigureProto figure = role.figure;

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

        // ---------------------------------------------------------------- 工具

        private void ClearSlots()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].root != null) Destroy(_slots[i].root);
            }
            _slots.Clear();
        }

        private static void ClearAndAddClick(Graphic target, System.Action onClick)
        {
            if (target == null) return;
            UIUtil.ClearClicks(target);
            UIUtil.AddClick(target, onClick);
        }

        /// <summary>递归按名找子节点(模板卡内部节点名固定,克隆后结构一致)。</summary>
        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t != root && t.name == name) return t;
            return null;
        }

        private static Image FindImage(Transform root, string name)
        {
            Transform t = FindDeep(root, name);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private static TextMeshProUGUI FindText(Transform root, string name)
        {
            Transform t = FindDeep(root, name);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
