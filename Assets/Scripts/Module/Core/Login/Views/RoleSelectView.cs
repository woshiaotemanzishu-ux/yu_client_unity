using System.Collections.Generic;
using System.Linq;
using Shenxiao.Common.Prefs;
using Shenxiao.Common.Tips;
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
    /// 结构(由 RoleSelectCreator 纯代码建树):全屏背景 / 3D 模型容器 modelCon(内含编辑器占位 modelPlaceholder)/
    /// 4 张固定角色卡 roleCards(每张位置各自手调,不再用模板克隆)/ 踏入仙界 enterBtn / 返回 returnBtn。
    /// 每张卡内直接持有子控件引用(bg 底图 / selectedFrame 选中胶囊 / head 头像 / nameLabel / turnLabel /
    /// ascendMark 升仙角标 / levelLabel),由 Creator 建树时回填,运行时不再按名查找。
    ///
    /// 本类只做:① 数据绑定(把角色数据填进 4 张卡,空卡=创建入口) ② 功能性状态切换(选中态换图 / 显示 3D 模型)。
    /// 不写颜色/字号/尺寸样式(建树期 Creator + 用户手调的事)。
    /// 卡片数量现由 prefab 上的 roleCards 决定(默认 4);要增减槽位就在 prefab 上加/删卡(或改 Creator 重生成)。
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

        /// <summary>一张固定角色卡的子控件引用(Creator 建树时回填;对标老 LoginSelectRoleItemBind)。</summary>
        [System.Serializable]
        public class RoleCard
        {
            public Image bg;              // 底图:未选/选中/空槽创建图;点击命中体
            public Image selectedFrame;   // 选中胶囊 / 名牌底
            public Image ascendMark;      // 升仙角标
            public Image head;            // 头像
            public TextMeshProUGUI nameLabel;
            public TextMeshProUGUI turnLabel;
            public TextMeshProUGUI levelLabel;
        }

        [Header("3D 模型容器")]
        public RectTransform modelCon;
        // 编辑器占位色块(建树期可见,便于直接调 modelCon 的位置/大小);运行时隐藏,不遮真模型。
        public Image modelPlaceholder;

        [Header("角色卡(固定 4 张,位置各自手调)")]
        public RoleCard[] roleCards;

        [Header("按钮")]
        public Image enterBtn;            // 踏入仙界
        public Image returnBtn;           // 返回

        private List<GameRoleInfo> _roles = new List<GameRoleInfo>();
        private int _selectedIndex = -1;

        protected override void OnInit()
        {
            BindStaticClicks();
            BindCardClicks();
        }

        protected override void OnShow(object args)
        {
            // 隐藏编辑器占位:真模型由 UIModelStage 贴进 modelCon,占位只是编辑期看位置/大小用。
            if (modelPlaceholder != null) modelPlaceholder.gameObject.SetActive(false);
            UIModelStage.SetDragRotate(true); // 对标老端:模型区横向拖动=左右转身(仅本页开启)
            Refresh();
        }

        protected override void OnHide()
        {
            UIModelStage.SetDragRotate(false);
            UIModelStage.Clear();
        }

        protected override void OnDispose()
        {
            _roles.Clear();
            _selectedIndex = -1;
        }

        private void BindStaticClicks()
        {
            ClearAndAddClick(enterBtn, OnClickEnter);
            ClearAndAddClick(returnBtn, OnClickReturn);
        }

        /// <summary>给每张卡的底图绑点击(捕获卡下标;固定卡只需绑一次)。</summary>
        private void BindCardClicks()
        {
            if (roleCards == null) return;
            for (int i = 0; i < roleCards.Length; i++)
            {
                RoleCard card = roleCards[i];
                if (card?.bg == null) continue;
                card.bg.raycastTarget = true;
                int captured = i;
                UIUtil.ClearClicks(card.bg);
                UIUtil.AddClick(card.bg, () => OnClickSlot(captured));
            }
        }

        // ---------------------------------------------------------------- 数据绑定(主入口)

        /// <summary>从角色列表填 4 张卡、默认选中上次登录角色、显示其 3D 模型(供 LoginFlow 调)。</summary>
        public async void Refresh()
        {
            await LoginConfigs.EnsureLoaded();
            _roles = LoginModel.Instance.Roles.OrderBy(r => r.roleId).ToList();

            if (roleCards == null || roleCards.Length == 0)
            {
                GameLog.Error("Login", "选角缺角色卡(roleCards 未绑,重新生成 prefab)");
                return;
            }

            // 先全部按未选态填一遍(超出角色数的卡=空槽创建入口),选中态由 SelectSlot 触发。
            for (int i = 0; i < roleCards.Length; i++) FillCard(roleCards[i], i, selected: false);

            // 默认选中上次登录角色,找不到选第一个(对标老端 UpdateView 的 select_index)
            long lastRoleId = long.TryParse(PrefsManager.GetString(PREF_LAST_ROLE_ID, ""), out long v) ? v : 0;
            int selectIndex = 0;
            for (int i = 0; i < _roles.Count && i < roleCards.Length; i++)
            {
                if (_roles[i].roleId == lastRoleId) { selectIndex = i; break; }
            }
            _selectedIndex = -1;
            if (_roles.Count > 0) SelectSlot(Mathf.Min(selectIndex, roleCards.Length - 1));
        }

        /// <summary>对标 LoginSelectRoleItem.UpdateItem/UpdateState:空卡=创建入口,角色卡按选中态换图。</summary>
        private async void FillCard(RoleCard card, int index, bool selected)
        {
            if (card == null) return;
            bool isRole = index < _roles.Count;
            if (card.selectedFrame != null) card.selectedFrame.enabled = isRole;
            if (card.nameLabel != null) card.nameLabel.enabled = isRole;
            if (card.levelLabel != null) card.levelLabel.enabled = isRole;
            if (card.turnLabel != null) card.turnLabel.enabled = isRole;
            if (card.ascendMark != null) card.ascendMark.enabled = false;
            if (card.head != null) card.head.enabled = isRole;

            if (!isRole)
            {
                // 换肤一律 nativeSize:false:保留卡片宽高(对标 Laya skin=),否则 SetNativeSize 撑回原图大小。
                if (card.bg != null)
                    _ = ResManager.SetImageAsync(card.bg,
                            "resource/game/login/texture/ui_Login_04.png", nativeSize: false);
                return;
            }

            GameRoleInfo role = _roles[index];
            if (card.nameLabel != null) card.nameLabel.text = role.DisplayName;
            if (card.turnLabel != null) card.turnLabel.text = role.Turn + "转";
            bool isSc = role.Level > SC_LEVEL;
            if (card.ascendMark != null) card.ascendMark.enabled = isSc;
            if (card.levelLabel != null) card.levelLabel.text = (isSc ? role.Level - SC_LEVEL : role.Level) + "级";

            string bg = selected ? "ui_Login_02" : "ui_Login_03";
            string bg2 = selected ? "ui_Login_05" : "ui_Login_06";
            if (card.bg != null)
                _ = ResManager.SetImageAsync(card.bg, $"resource/game/login/texture/{bg}.png", nativeSize: false);
            if (card.selectedFrame != null)
                _ = ResManager.SetImageAsync(card.selectedFrame, $"resource/game/login/texture/{bg2}.png", nativeSize: false);

            // 头像:config_dress_up_cfg(按转生数选装扮,screen 按职业给图标);自定义头像 picture 待头像线。
            Image head = card.head;
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
                LoginFlow.ShowCreateRole(); // 空卡=创建角色入口(对标老客户端 data.none 分支)
                return;
            }
            SelectSlot(index);
        }

        private void SelectSlot(int index)
        {
            if (_selectedIndex == index) return;
            _selectedIndex = index;
            for (int i = 0; i < roleCards.Length; i++)
            {
                FillCard(roleCards[i], i, selected: i == index);
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
            Vector2 pos = LoginConfigs.GetModelPos("SelectRole", option.Career, option.Sex);
            float yaw = UIModelStage.MODEL_YAW;
            // 新模型(激活中的实例带渲染档案)吃单独的展示旋钮:老 ModelPos 是按老模型调的构图,
            // 新模型的构图/朝向在 configlogin SelectRole.NewModel { x, y, yaw } 里所见即所得拧
            if (model.GetComponentInChildren<ArtModelRenderProfile>(false) != null)
            {
                Vector3 tuning = LoginConfigs.GetNewModelTuning("SelectRole");
                pos += new Vector2(tuning.x, tuning.y);
                yaw += tuning.z;
            }
            UIModelStage.ShowInstance(modelCon, model, MODEL_SCALE, pos, yaw);
        }

        private void OnClickEnter()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _roles.Count)
            {
                TipsManager.Toast("未选择角色");   // 对标老端 LoginSelectRoleView:183
                return;
            }
            LoginController.Instance.EnterGameWithRole(_roles[_selectedIndex].roleId);
        }

        private void OnClickReturn()
        {
            LoginFlow.BackToEnter();
        }

        // ---------------------------------------------------------------- 工具

        private static void ClearAndAddClick(Graphic target, System.Action onClick)
        {
            if (target == null) return;
            UIUtil.ClearClicks(target);
            UIUtil.AddClick(target, onClick);
        }
    }
}
