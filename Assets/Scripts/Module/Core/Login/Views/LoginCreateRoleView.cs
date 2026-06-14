using System.Collections.Generic;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Login;
using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 创角页(对标老客户端 LoginCreateRoleView.ts):
    /// 职业列表/图标/介绍图/随机名/默认装模型全部来自 ConfigLogin+ConfigModelAni+ConfigRandomName
    /// (LoginConfigs 运行时读 JSON,无硬编码镜像)。
    /// 进入时按 random_weight 加权随机预选职业;返回:有角色回选角页,无角色断线回踏入仙界页。
    /// </summary>
    public sealed class LoginCreateRoleView : LoginCreateRoleViewBind
    {
        // 老客户端视图代码字面量(非配置):item.SetPosition(0, career*133)、show_model_data.scale=0.5
        private const float ITEM_STEP_Y = 133f;
        private const float MODEL_SCALE = 0.5f;

        private readonly List<GameObject> _items = new List<GameObject>();
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
            BuildCareers();
            SelectCareer(WeightedRandomIndex()); // 老客户端 GetRandomIndex:按 random_weight 加权
            OnClickRandomName();
        }

        private void BuildCareers()
        {
            foreach (GameObject go in _items) Destroy(go);
            _items.Clear();

            for (int i = 0; i < _options.Count; i++)
            {
                GameObject item = Instantiate(_tpl_LoginCreateRoleItem, _gp_head_con);
                item.SetActive(true);
                // y = career*133(Laya y 向下 → Unity 取负)
                ((RectTransform)item.transform).anchoredPosition =
                    new Vector2(0f, -_options[i].Career * ITEM_STEP_Y);

                var bind = item.GetComponent<LoginCreateRoleItemBind>();
                if (bind == null)
                {
                    GameLog.Error("Login", "职业项缺 LoginCreateRoleItemBind,重跑回填");
                    continue;
                }
                bind._lb_career.text = _options[i].Name;
                int captured = i;
                UIUtil.AddClick(bind._img_bg, () => SelectCareer(captured));
                _items.Add(item);
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
            _selectedIndex = Mathf.Clamp(index, 0, _options.Count - 1);
            RefreshCareerStates();
            RefreshTips();
            ShowCareerModel();
        }

        private void RefreshCareerStates()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var bind = _items[i].GetComponent<LoginCreateRoleItemBind>();
                if (bind == null) continue;
                bool selected = i == _selectedIndex;
                // 对标 LoginCreateRoleItem.ts:选中底图 ui_Login_02,未选 ui_Login_03;头像换 a 版。
                // 换肤 nativeSize:false 保留场景尺寸(对标 Laya skin=)
                string bg = selected ? "ui_Login_02" : "ui_Login_03";
                string icon = selected ? _options[i].SelectIcon : _options[i].UnselectIcon;
                _ = Shenxiao.Framework.Res.ResManager.SetImageAsync(bind._img_bg,
                        $"resource/game/login/texture/{bg}.png", nativeSize: false);
                _ = Shenxiao.Framework.Res.ResManager.SetImageAsync(bind._img_icon,
                        $"resource/game/login/texture/{icon}.png", nativeSize: false);
            }
        }

        /// <summary>右侧职业介绍三连图(老客户端 SetOutsideImageSprite(GetIconOtherPath)),保留场景尺寸。</summary>
        private void RefreshTips()
        {
            var o = _options[_selectedIndex];
            _ = Shenxiao.Framework.Res.ResManager.SetImageAsync(_img_tips, $"resource/game/login/other/{o.Img1}.png", nativeSize: false);
            _ = Shenxiao.Framework.Res.ResManager.SetImageAsync(_img_tips2, $"resource/game/login/other/{o.Img2}.png", nativeSize: false);
            _ = Shenxiao.Framework.Res.ResManager.SetImageAsync(_img_tips3, $"resource/game/login/other/{o.Img3}.png", nativeSize: false);
        }

        /// <summary>中央 3D 模型:默认装(衣+头饰+武器)+ ConfigModelAni 的 create 动作序列。</summary>
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
            // 创角骨骼特效(ConfigLogin.CreateRole.Effect,如 cj_1100 脚下漩涡,skills_effect 目录)
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
            UIModelStage.ShowInstance(_gp_model_con, model,
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
    }
}
