using System.Collections.Generic;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 创角页(重构版,自包含独立 prefab)——取代老端碎片化的 LoginCreateRoleView + LoginCreateRoleItem。
    ///
    /// 结构(对标截图 5/6):全屏背景 + 左侧职业选择列(图标环) + 中央 3D 角色模型(_gp_model_con)
    /// + 职业名 + 职业描述三连图(诗句)+ 随机名(名字输入 + ⟳ 随机)+ 底部「踏入仙界」+ 返回。
    /// prefab 由 RoleCreateCreator 纯代码建树生成并回填下列 public 引用;职业项模板(careerItemTemplate)
    /// 在 prefab 内默认隐藏,运行时按职业数克隆。
    ///
    /// 本类只做:① 数据绑定(职业列表/3D 模型/随机名,逻辑原样搬自旧 LoginCreateRoleView.cs)
    ///          ② 功能性状态切换(选中职业换底图/头像、SetActive)。不写颜色/字号/尺寸等样式。
    ///
    /// ——— 对接说明(给主控 LoginFlow 接线用)———
    /// 暴露的 public 方法:
    ///   - void Refresh()                      // 建职业项 + 默认预选 + 显示模型/职业名/描述/随机名(OnShow 自动调一次)
    /// 调用的 LoginFlow 静态方法(返回按钮):
    ///   - LoginFlow.ShowSelectRole()          // 有角色时回选角页
    ///   - LoginFlow.BackToEnter()             // 无角色时回踏入仙界页
    /// 调用的数据层(原样搬自旧 view):
    ///   - LoginConfigs.EnsureLoaded / CreateRoleOptions / GetCreateRes / RoleUIActions /
    ///     CreateRoleEffects / GetModelPos / RandomRoleName
    ///   - RoleModelAssembler.BuildAsync / PlayActions, EffectBinder.AttachOne / PlayOneShot, UIModelStage
    ///   - LoginController.Instance.SendCreateRole(name, career, sex)
    ///   - 监听 GlobalEvent.EVT_GAME_CREATE_ROLE_RESULT
    /// 待主控/后续处理:本页职业「描述」配置里只有三连诗句图(Img1/2/3),无文字描述;
    ///   careerDescLabel 仅作占位文本节点(可由用户手填/隐藏),职业描述实际由 tips 三图呈现。
    /// </summary>
    [UIView("prefabs/ui/login/rolecreateview")]
    public sealed class RoleCreateView : BaseView
    {
        private const float MODEL_SCALE = 0.5f; // 老客户端字面量 show_model_data.scale

        [Header("背景 / 容器")]
        public Image bgImg;
        public RectTransform careerCon;          // 职业项的父容器(对标老 _gp_head_con)
        public GameObject careerItemTemplate;    // 职业项模板(prefab 内默认隐,克隆源)
        public RectTransform modelCon;           // 3D 模型容器(对标老 _gp_model_con)

        [Header("职业信息")]
        public TextMeshProUGUI careerNameLabel;  // 职业名("剑主"…)
        public TextMeshProUGUI careerDescLabel;  // 职业描述占位(无文字配置,实际描述用 tips 三图)
        public Image tipsImg;                    // 职业介绍三连图(诗句),对标老 _img_tips
        public Image tipsImg2;                   // 对标老 _img_tips2
        public Image tipsImg3;                   // 对标老 _img_tips3

        [Header("名字 / 按钮")]
        public Image nameBgImg;                  // 名字底框(对标老 _img_bg ui_Login_08)
        public TMP_InputField nameInput;         // 角色名输入(对标老 _lb_random_name TextInput)
        public Image randomBtn;                  // 随机名按钮 ⟳(对标老 _img_random)
        public Image enterBtn;                   // 踏入仙界(创建)按钮(对标老 _img_enter)
        public Image returnBtn;                  // 返回按钮(对标老 _img_return)

        /// <summary>运行时克隆出的职业项(缓存内部底图/图标/名字,对标老 CareerSlot)。</summary>
        private struct CareerSlot
        {
            public GameObject root;
            public Image bg;
            public Image icon;
            public TMP_Text label;
        }

        private readonly List<CareerSlot> _slots = new List<CareerSlot>();
        private List<LoginConfigs.CareerOption> _options = new List<LoginConfigs.CareerOption>();
        private int _selectedIndex;
        private bool _creating;

        protected override void OnInit()
        {
            BindClicks();
            EventDispatcher.On<int>(GlobalEvent.EVT_GAME_CREATE_ROLE_RESULT, OnCreateResult);
        }

        protected override void OnShow(object args)
        {
            _creating = false;
            Refresh();
        }

        protected override void OnHide()
        {
            UIModelStage.Clear();
        }

        protected override void OnDispose()
        {
            EventDispatcher.Off<int>(GlobalEvent.EVT_GAME_CREATE_ROLE_RESULT, OnCreateResult);
        }

        // ---------------------------------------------------------------- 点击绑定(功能性,允许)

        private void BindClicks()
        {
            ClearAndAddClick(enterBtn, OnClickEnter);
            ClearAndAddClick(randomBtn, OnClickRandomName);
            ClearAndAddClick(returnBtn, OnClickReturn);
        }

        private static void ClearAndAddClick(Graphic target, System.Action onClick)
        {
            if (target == null) return;
            UIUtil.ClearClicks(target);   // 重绑前先清,防监听叠加
            UIUtil.AddClick(target, onClick);
        }

        // ---------------------------------------------------------------- 数据绑定

        /// <summary>建职业项 + 默认预选 + 显示模型/职业名/描述/随机名。OnShow 自动调一次,亦可由 LoginFlow 主动刷新。</summary>
        public void Refresh()
        {
            InitAsync();
        }

        private async void InitAsync()
        {
            await LoginConfigs.EnsureLoaded();
            _options = LoginConfigs.CreateRoleOptions();
            if (_options.Count == 0)
            {
                GameLog.Error("Login", "ConfigLogin.CreateRole.UI 为空(配置未同步?)");
                return;
            }
            BuildCareers();
            SelectCareer(WeightedRandomIndex()); // 老客户端 GetRandomIndex:按 random_weight 加权
            OnClickRandomName();
        }

        /// <summary>按职业数克隆模板项,缓存内部底图/图标/名字并绑点击(对标老 BindBakedCareers,改成克隆模板)。</summary>
        private void BuildCareers()
        {
            // 清掉上次克隆(模板本身保留并隐藏)
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].root != null) Destroy(_slots[i].root);
            }
            _slots.Clear();

            if (careerItemTemplate == null || careerCon == null)
            {
                GameLog.Error("Login", "创角缺 careerItemTemplate / careerCon(建树异常?)");
                return;
            }
            careerItemTemplate.SetActive(false);

            for (int i = 0; i < _options.Count; i++)
            {
                GameObject item = Instantiate(careerItemTemplate, careerCon);
                item.name = "CareerItem_" + i;
                item.SetActive(true);

                var slot = new CareerSlot
                {
                    root = item,
                    bg = FindImage(item.transform, "Bg"),
                    icon = FindImage(item.transform, "Icon"),
                    label = FindText(item.transform, "Label"),
                };
                if (slot.label != null) slot.label.text = _options[i].Name;
                int captured = i;
                if (slot.bg != null)
                {
                    slot.bg.raycastTarget = true;
                    UIUtil.ClearClicks(slot.bg);
                    UIUtil.AddClick(slot.bg, () => SelectCareer(captured));
                }
                _slots.Add(slot);
            }
        }

        private int WeightedRandomIndex()
        {
            int total = 0;
            foreach (var o in _options) total += Mathf.Max(o.RandomWeight, 0);
            if (total <= 0) return 0;
            int roll = Random.Range(0, total);
            for (int i = 0; i < _options.Count; i++)
            {
                roll -= Mathf.Max(_options[i].RandomWeight, 0);
                if (roll < 0) return i;
            }
            return 0;
        }

        private void SelectCareer(int index)
        {
            int max = Mathf.Min(_options.Count, Mathf.Max(_slots.Count, 1)) - 1;
            _selectedIndex = Mathf.Clamp(index, 0, Mathf.Max(max, 0));
            RefreshCareerStates();
            RefreshInfo();
            ShowCareerModel();
        }

        /// <summary>选中态:选中底图 ui_Login_02,未选 ui_Login_03;头像换 a/b 版(对标 LoginCreateRoleItem.ts)。</summary>
        private void RefreshCareerStates()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                bool selected = i == _selectedIndex;
                string bg = selected ? "ui_Login_02" : "ui_Login_03";
                string icon = selected ? _options[i].SelectIcon : _options[i].UnselectIcon;
                if (_slots[i].bg != null)
                    _ = ResManager.SetImageAsync(_slots[i].bg, $"resource/game/login/texture/{bg}.png", nativeSize: false);
                if (_slots[i].icon != null)
                    _ = ResManager.SetImageAsync(_slots[i].icon, $"resource/game/login/texture/{icon}.png", nativeSize: false);
            }
        }

        /// <summary>职业名 + 右侧介绍三连图:换图 + tips2/3 的 centerY 随职业微调(诗句错位,对标老 SelectCareer)。</summary>
        private void RefreshInfo()
        {
            var o = _options[_selectedIndex];
            if (careerNameLabel != null) careerNameLabel.text = o.Name;

            int career = o.Career;
            float poemA = career == 2 ? 20f : 0f;
            float poemBbase = (career == 2 || career == 4) ? -30f : -50f;
            float poemBoff = career == 2 ? 60f : 0f;
            if (tipsImg2 != null) SetCenter(tipsImg2.rectTransform, 280f, -270f + poemA);
            if (tipsImg3 != null) SetCenter(tipsImg3.rectTransform, 335f, poemBbase + poemBoff);

            if (tipsImg != null)
                _ = ResManager.SetImageAsync(tipsImg, $"resource/game/login/other/{o.Img1}.png", nativeSize: false);
            if (tipsImg2 != null)
                _ = ResManager.SetImageAsync(tipsImg2, $"resource/game/login/other/{o.Img2}.png", nativeSize: false);
            if (tipsImg3 != null)
                _ = ResManager.SetImageAsync(tipsImg3, $"resource/game/login/other/{o.Img3}.png", nativeSize: false);
        }

        /// <summary>中央 3D 模型:默认装(衣+头饰+武器)+ ConfigModelAni 的 create 动作序列(真·运行时,原样搬)。</summary>
        private async void ShowCareerModel()
        {
            var o = _options[_selectedIndex];
            LoginConfigs.CareerRes res = LoginConfigs.GetCreateRes(o.Career, o.Sex);
            if (res == null)
            {
                GameLog.Warn("Login", "CreateRole.Res 缺 {0}@{1}", o.Career, o.Sex);
                return;
            }
            int selectedAtRequest = _selectedIndex;
            string[] actions = LoginConfigs.RoleUIActions("LoginCreateRoleView");
            GameObject model = await RoleModelAssembler.BuildAsync(new RoleModelSpec
            {
                Career = o.Career,
                ClotheRes = res.RoleRes,
                WeaponRes = res.WeaponRes,
                HeadRes = res.HeadRes,
                Actions = actions,
                AutoPlayActions = false,
            });
            if (model == null) return;
            if (selectedAtRequest != _selectedIndex || !gameObject.activeInHierarchy)
            {
                Destroy(model); // 加载期间切了职业/关了页:丢弃过期结果
                return;
            }
            var createEffects = new List<GameObject>();
            foreach ((string bone, string fx) in LoginConfigs.CreateRoleEffects(o.Career, o.Sex))
            {
                GameObject effect = await EffectBinder.AttachOne(
                    model, bone, "skills_effect", fx, "bone", playOnAttach: false);
                if (effect != null) createEffects.Add(effect);
            }
            if (selectedAtRequest != _selectedIndex || !gameObject.activeInHierarchy)
            {
                Destroy(model);
                return;
            }
            UIModelStage.ShowInstance(ModelCon(), model,
                MODEL_SCALE, LoginConfigs.GetModelPos("CreateRole", o.Career, o.Sex));
            RoleModelAssembler.PlayActions(model, actions);
            foreach (GameObject effect in createEffects)
            {
                EffectBinder.PlayOneShot(effect);
            }
        }

        // ---------------------------------------------------------------- 事件

        private void OnClickRandomName()
        {
            string name = LoginConfigs.RandomRoleName(_options.Count > 0 ? _options[_selectedIndex].Sex : 1);
            if (nameInput != null) nameInput.text = name;
        }

        private void OnClickEnter()
        {
            if (_creating) return;
            string roleName = (nameInput != null ? nameInput.text : string.Empty).Trim();
            if (string.IsNullOrEmpty(roleName))
            {
                GameLog.Warn("Login", "角色名为空");
                return;
            }
            _creating = true;
            var o = _options[_selectedIndex];
            LoginController.Instance.SendCreateRole(roleName, o.Career, o.Sex);
        }

        private void OnCreateResult(int result)
        {
            _creating = false;
            switch (result)
            {
                case 1: break; // 成功:LoginController 已自动 10004 进入游戏
                case 3: GameLog.Warn("Login", "创角失败:角色名称已被使用"); break;
                case 4: GameLog.Warn("Login", "创角失败:含敏感字符"); break;
                case 5: GameLog.Warn("Login", "创角失败:名称长度需 2~6 个汉字"); break;
                case 6: GameLog.Warn("Login", "创角失败:该账号已创建角色"); break;
                default: GameLog.Warn("Login", "创角失败:未知错误({0})", result); break;
            }
        }

        /// <summary>对标老客户端:有角色 → 回选角页;无角色 → 断线回踏入仙界页。</summary>
        private void OnClickReturn()
        {
            if (LoginModel.Instance.Roles.Count > 0) LoginFlow.ShowSelectRole();
            else LoginFlow.BackToEnter();
        }

        // ——— 容器字段没绑上时按名兜底 ———
        private RectTransform ModelCon() => modelCon != null ? modelCon : transform.Find("_gp_model_con") as RectTransform;

        private static Image FindImage(Transform root, string path)
        {
            Transform t = root.Find(path);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private static TMP_Text FindText(Transform root, string path)
        {
            Transform t = root.Find(path);
            if (t == null) return null;
            TMP_Text self = t.GetComponent<TMP_Text>();
            return self != null ? self : t.GetComponentInChildren<TMP_Text>(true);
        }

        /// <summary>tips 的每职业 centerY 微调用(数据驱动的小位移,非静态布局)。</summary>
        private static void SetCenter(RectTransform rt, float centerX, float centerY)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(centerX, -centerY);
        }
    }
}
