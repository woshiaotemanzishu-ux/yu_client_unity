using System.Collections.Generic;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Login;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 创角页(对标老客户端 LoginCreateRoleView.ts)。
    ///
    /// Phase 2:结构(背景/飘带/职业项/tips 布局)由快照烤进 prefab,这里【只绑数据】——
    /// 不再运行时 new 节点、不再摆位置(老的 ApplyRuntimeLayout/EnsureCreateBg/BuildCareers 已删)。
    /// 职业列表/图标/介绍图/随机名/默认装模型来自 ConfigLogin+ConfigModelAni+ConfigRandomName。
    /// 进入按 random_weight 加权随机预选;返回:有角色回选角,无角色断线回踏入仙界。
    /// </summary>
    public sealed class LoginCreateRoleView : LoginCreateRoleViewBind
    {
        private const float MODEL_SCALE = 0.5f; // 老客户端字面量 show_model_data.scale

        /// <summary>烤进 prefab 的职业项:绑数据时缓存它内部的底图/图标/名字。</summary>
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
            UIUtil.AddClick(_img_enter, OnClickEnter);
            UIUtil.AddClick(_img_return, OnClickReturn);
            UIUtil.AddClick(_img_random, OnClickRandomName);
            EventDispatcher.On<int>(GlobalEvent.EVT_GAME_CREATE_ROLE_RESULT, OnCreateResult);
        }

        protected override void OnShow(object args)
        {
            _creating = false;
            InitAsync();
        }

        protected override void OnHide()
        {
            UIModelStage.Clear();
        }

        protected override void OnDispose()
        {
            EventDispatcher.Off<int>(GlobalEvent.EVT_GAME_CREATE_ROLE_RESULT, OnCreateResult);
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
            BindBakedCareers();
            SelectCareer(WeightedRandomIndex()); // 老客户端 GetRandomIndex:按 random_weight 加权
            OnClickRandomName();
        }

        /// <summary>把数据绑到烤进 prefab 的职业项上(不再 Instantiate 模板)。</summary>
        private void BindBakedCareers()
        {
            _slots.Clear();
            RectTransform head = HeadCon();
            if (head == null)
            {
                GameLog.Error("Login", "创角缺 _gp_head_con(烤制 prefab 结构异常?)");
                return;
            }

            // 收集烤进 _gp_head_con 的职业项(节点名 LoginCreateRoleItem),按层级序
            var items = new List<Transform>();
            for (int i = 0; i < head.childCount; i++)
            {
                Transform c = head.GetChild(i);
                if (c.name.StartsWith("LoginCreateRoleItem")) items.Add(c);
            }
            if (items.Count < _options.Count)
            {
                GameLog.Warn("Login", "烤制职业项 {0} 少于配置职业 {1}(配置变了需重烤)", items.Count, _options.Count);
            }

            for (int i = 0; i < items.Count; i++)
            {
                Transform item = items[i];
                bool used = i < _options.Count;
                item.gameObject.SetActive(used);
                if (!used) continue;

                var slot = new CareerSlot
                {
                    root = item.gameObject,
                    bg = FindImage(item, "_box_head/_img_bg"),
                    icon = FindImage(item, "_box_head/_img_icon"),
                    label = FindText(item, "_box_head/_lb_career"),
                };
                if (slot.label != null) slot.label.text = _options[i].Name;
                int captured = i;
                if (slot.bg != null)
                {
                    slot.bg.raycastTarget = true;
                    UIUtil.ClearClicks(slot.bg); // BindBakedCareers 每次 OnShow 重跑,先清再加防监听叠加
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
            RefreshTips();
            ShowCareerModel();
        }

        private void RefreshCareerStates()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                bool selected = i == _selectedIndex;
                // 对标 LoginCreateRoleItem.ts:选中底图 ui_Login_02,未选 ui_Login_03;头像换 a 版。
                string bg = selected ? "ui_Login_02" : "ui_Login_03";
                string icon = selected ? _options[i].SelectIcon : _options[i].UnselectIcon;
                if (_slots[i].bg != null)
                    _ = ResManager.SetImageAsync(_slots[i].bg, $"resource/game/login/texture/{bg}.png", nativeSize: false);
                if (_slots[i].icon != null)
                    _ = ResManager.SetImageAsync(_slots[i].icon, $"resource/game/login/texture/{icon}.png", nativeSize: false);
            }
        }

        /// <summary>右侧职业介绍三连图:换图 + tips2/3 的 centerY 随职业微调(诗句错位,对标 SelectCareer)。</summary>
        private void RefreshTips()
        {
            var o = _options[_selectedIndex];
            int career = o.Career;
            float poemA = career == 2 ? 20f : 0f;
            float poemBbase = (career == 2 || career == 4) ? -30f : -50f;
            float poemBoff = career == 2 ? 60f : 0f;
            SetCenter(_img_tips2.rectTransform, 280f, -270f + poemA);
            SetCenter(_img_tips3.rectTransform, 335f, poemBbase + poemBoff);
            _ = ResManager.SetImageAsync(_img_tips, $"resource/game/login/other/{o.Img1}.png", nativeSize: false);
            _ = ResManager.SetImageAsync(_img_tips2, $"resource/game/login/other/{o.Img2}.png", nativeSize: false);
            _ = ResManager.SetImageAsync(_img_tips3, $"resource/game/login/other/{o.Img3}.png", nativeSize: false);
        }

        /// <summary>中央 3D 模型:默认装(衣+头饰+武器)+ ConfigModelAni 的 create 动作序列(真·运行时)。</summary>
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
                GameObject effect = await Shenxiao.Common.UI3D.EffectBinder.AttachOne(
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
                Shenxiao.Common.UI3D.EffectBinder.PlayOneShot(effect);
            }
        }

        private void OnClickRandomName()
        {
            _lb_random_name.text = LoginConfigs.RandomRoleName(_options.Count > 0 ? _options[_selectedIndex].Sex : 1);
        }

        private void OnClickEnter()
        {
            if (_creating) return;
            string roleName = (_lb_random_name.text ?? string.Empty).Trim();
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

        // ——— 容器字段没绑上时按名兜底(烤制 prefab 里容器节点名与老端一致)———
        private RectTransform HeadCon() => _gp_head_con != null ? _gp_head_con : transform.Find("_gp_head_con") as RectTransform;
        private RectTransform ModelCon() => _gp_model_con != null ? _gp_model_con : transform.Find("_gp_model_con") as RectTransform;

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
