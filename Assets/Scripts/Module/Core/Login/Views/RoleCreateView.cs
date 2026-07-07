using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 创角页(重构版,自包含独立 prefab)——取代老端碎片化的 LoginCreateRoleView + LoginCreateRoleItem。
    ///
    /// 结构(对标截图 5/6):全屏背景 + 左侧职业选择列(4 张固定职业卡 careerItems) + 中央 3D 角色模型(ModelCon)
    /// + 职业名 + 职业描述三连图(诗句)+ 随机名(名字输入 + ⟳ 随机)+ 底部「踏入仙界」+ 返回。
    /// prefab 由 RoleCreateCreator 纯代码建树生成并回填下列 public 引用;职业卡固定 4 张(不用模板克隆),
    /// 每张卡内直接持有 bg/icon/label 引用,位置各自手调;职业数不足则运行时隐藏多余卡。
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
        public RectTransform modelCon;           // 3D 模型容器(对标老 _gp_model_con)

        [Header("职业卡(固定 4 张,位置各自手调)")]
        public CareerItem[] careerItems;

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

        /// <summary>一张固定职业卡的子控件引用(Creator 建树时回填;对标老 CareerSlot)。</summary>
        [System.Serializable]
        public class CareerItem
        {
            public GameObject root;   // 卡根节点(职业数不足时隐藏多余卡)
            public Image bg;          // 命中体 + 选中/未选底图
            public Image icon;        // 职业图标(选中/未选换图)
            public TMP_Text label;    // 职业名
        }

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

        /// <summary>把职业数据填进固定职业卡:填名字 + 绑点击;职业数不足则隐藏多余卡(对标老 BindBakedCareers)。</summary>
        private void BuildCareers()
        {
            if (careerItems == null || careerItems.Length == 0)
            {
                GameLog.Error("Login", "创角缺 careerItems(未绑,重新生成 prefab)");
                return;
            }
            if (careerItems.Length < _options.Count)
            {
                GameLog.Warn("Login", "职业数 {0} 超过职业卡数 {1},多出的职业不显示(prefab 加卡或改 Creator)",
                    _options.Count, careerItems.Length);
            }

            for (int i = 0; i < careerItems.Length; i++)
            {
                CareerItem item = careerItems[i];
                if (item == null) continue;
                bool used = i < _options.Count;
                if (item.root != null) item.root.SetActive(used);
                if (!used) continue;

                if (item.label != null) item.label.text = _options[i].Name;
                if (item.bg != null)
                {
                    item.bg.raycastTarget = true;
                    int captured = i;
                    UIUtil.ClearClicks(item.bg);
                    UIUtil.AddClick(item.bg, () => SelectCareer(captured));
                }
            }
        }

        /// <summary>当前可选职业数(职业卡数与配置职业数取小)。</summary>
        private int ActiveCareerCount()
        {
            return Mathf.Min(careerItems?.Length ?? 0, _options.Count);
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
            int count = ActiveCareerCount();
            if (count <= 0) return;
            _selectedIndex = Mathf.Clamp(index, 0, count - 1);
            RefreshCareerStates();
            RefreshInfo();
            ShowCareerModel();
        }

        /// <summary>选中态:选中底图 ui_Login_02,未选 ui_Login_03;头像换 a/b 版(对标 LoginCreateRoleItem.ts)。</summary>
        private void RefreshCareerStates()
        {
            int count = ActiveCareerCount();
            for (int i = 0; i < count; i++)
            {
                CareerItem item = careerItems[i];
                if (item == null) continue;
                bool selected = i == _selectedIndex;
                string bg = selected ? "ui_Login_02" : "ui_Login_03";
                string icon = selected ? _options[i].SelectIcon : _options[i].UnselectIcon;
                if (item.bg != null)
                    _ = ResManager.SetImageAsync(item.bg, $"resource/game/login/texture/{bg}.png", nativeSize: false);
                if (item.icon != null)
                    _ = ResManager.SetImageAsync(item.icon, $"resource/game/login/texture/{icon}.png", nativeSize: false);
            }
        }

        /// <summary>职业名 + 右侧介绍三连图换图。位置/尺寸一律以 prefab 为准,不在代码里摆位。</summary>
        private void RefreshInfo()
        {
            var o = _options[_selectedIndex];
            if (careerNameLabel != null) careerNameLabel.text = o.Name;

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

            // 整模优先:ArtImport 导入的创角成品 prefab(特效/动作自带,Timeline 自播),没有再走老拼装链
            if (await TryShowWholeModel(o, res, selectedAtRequest)) return;

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

        /// <summary>
        /// 整模路径(ArtImport 导入的创角成品 prefab):
        /// `object/role/model_create_{RoleRes}/{RoleRes}@create2`(出场,播完 Hold 停末帧)
        /// → `{RoleRes}@create3`(待机,循环)。特效/动作全在 prefab 里由 PlayableDirector 自播,
        /// 不走拼装链、不挂配置骨骼特效。资源不存在返回 false,由调用方走老拼装路径。
        /// </summary>
        private async Task<bool> TryShowWholeModel(
            LoginConfigs.CareerOption o, LoginConfigs.CareerRes res, int selectedAtRequest)
        {
            string baseKey = $"object/role/model_create_{res.RoleRes}/{res.RoleRes}";
            GameObject create2Prefab = await ResManager.LoadOptionalAsync<GameObject>(baseKey + "@create2");
            if (create2Prefab == null) return false;
            // 待机 prefab 提前拿:落点(=占位位置)从它的静态包围盒量,完全不碰出场动画的 Director
            GameObject create3Prefab = await ResManager.LoadOptionalAsync<GameObject>(baseKey + "@create3");
            // 加载期间切了职业/关了页:丢弃过期结果(新选中职业自会触发自己的 ShowCareerModel)
            if (selectedAtRequest != _selectedIndex || !gameObject.activeInHierarchy) return true;

            Vector2 modelPos = LoginConfigs.GetModelPos("CreateRole", o.Career, o.Sex);
            GameObject inst = Instantiate(create2Prefab);
            PlayableDirector director = inst.GetComponentInChildren<PlayableDirector>(true);
            if (director != null) director.extrapolationMode = DirectorWrapMode.Hold; // 播完停末帧等切待机
            UIModelStage.ShowInstance(ModelCon(), StageWrap(inst, create2Prefab), MODEL_SCALE, modelPos);

            // 等出场动画播完(inst 被销毁=页面关了/别人上台了,直接退)
            if (director != null)
            {
                double duration = director.duration;
                float safety = (float)duration + 5f; // director 意外停住时的兜底,防死循环
                while (inst != null && director.time + 0.001 < duration && safety > 0f
                       && selectedAtRequest == _selectedIndex && gameObject.activeInHierarchy)
                {
                    await Task.Yield();
                    safety -= Time.deltaTime;
                }
            }
            if (inst == null || selectedAtRequest != _selectedIndex || !gameObject.activeInHierarchy) return true;
            if (create3Prefab == null) return true; // 没交付待机就停在出场末帧

            GameObject idle = Instantiate(create3Prefab);
            PlayableDirector idleDirector = idle.GetComponentInChildren<PlayableDirector>(true);
            if (idleDirector != null) idleDirector.extrapolationMode = DirectorWrapMode.Loop; // 待机循环
            // 上台即销毁 create2 实例;两段档案里烤的是同一个落点,切换处位置无缝
            UIModelStage.ShowInstance(ModelCon(), StageWrap(idle, create3Prefab), MODEL_SCALE, modelPos);
            return true;
        }

        /// <summary>
        /// 落点计算——核心约定(与美术):【出场动画的落点 = 待机位置 = 占位位置】。
        /// 整模上台包装。落点**不在运行时猜**——导入工具用 SampleAnimation 把 create3 采样到末帧、
        /// BakeMesh 精确量出脚底中心与身高,烤在 ArtModelRenderProfile 里(静态包围盒是绑定姿势的
        /// 松盒,和动画停放点不是一回事,1213 曾被它骗成"已归零"导致巨腿怼镜头,实锤教训)。
        /// 这里只读档案:整体平移把落点对到占位点+体量归一(平移是相对量,不锁动画、不碰 Director,
        /// 空间位移原样播);create2/create3 档案里烤的是同一个落点,切换无缝。
        /// 页面级微调走 configlogin 的 CreateRole.ModelPos/PosOffset。
        /// </summary>
        private static GameObject StageWrap(GameObject inst, GameObject sourcePrefab)
        {
            ArtModelRenderProfile profile = inst.GetComponentInChildren<ArtModelRenderProfile>(true);
            if (profile == null)
            {
                profile = inst.AddComponent<ArtModelRenderProfile>();
                profile.useDedicatedRenderer = false; // 运行时补的档案不知道独立 renderer 下标:默认 renderer+强制贴图即可
            }
            foreach (ParticleSystem ps in inst.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            // 动画是 Generic,根位移依赖这个开关:Timeline 编辑器预览无视它,运行时不开=原地做动作
            foreach (Animator animator in inst.GetComponentsInChildren<Animator>(true))
            {
                animator.applyRootMotion = true;
            }
            // 透明镂空:美术把透明遮罩画在贴图 alpha 里(实测服饰.tga 有 3.2% 全透像素),但 FBX
            // 内嵌材质默认 Opaque 不读 alpha → 该透的地方渲成白块。给身体材质实例统一开
            // Alpha Clipping——没画 alpha 的贴图全 255,开了也一像素不少,安全;只改实例不动资产。
            foreach (Renderer r in inst.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                Material[] mats = r.materials;
                bool dirty = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    if (m == null || m.shader == null || !m.HasProperty("_AlphaClip")) continue;
                    m.SetFloat("_AlphaClip", 1f);
                    m.SetFloat("_Cutoff", 0.5f);
                    m.EnableKeyword("_ALPHATEST_ON");
                    m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    dirty = true;
                }
                if (dirty) r.materials = mats;
            }

            var pivot = new GameObject(inst.name + "_pivot");
            inst.transform.SetParent(pivot.transform, false);
            if (profile.hasLanding)
            {
                // 落点是美术场景坐标;换算成"相对本 prefab 根"的位移再缩放,把落点挪到 pivot 原点。
                // 根自身的缩放要保留相乘(美术可能用根缩放做整体包装),覆盖掉=体量爆炸
                Vector3 rootPos = sourcePrefab.transform.position;
                inst.transform.localScale =
                    sourcePrefab.transform.localScale * profile.landingScale;
                inst.transform.localPosition = -(profile.landingOffset - rootPos) * profile.landingScale;
            }
            else
            {
                GameLog.Warn("Login",
                    "整模 {0} 档案里没有烤落点(旧版导入),按原样上台——用资产管理[替换新模型]重导一次即可",
                    inst.name);
            }
            return pivot;
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
        private RectTransform ModelCon() => modelCon != null ? modelCon : transform.Find("ModelCon") as RectTransform;
    }
}
