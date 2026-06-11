using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Login;
using UnityEngine;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 创角页:职业头像列表(_tpl_LoginCreateRoleItem)+ 名字输入 + 随机名 + 进入(10003)。
    /// TODO(配表线):职业定义与随机名词库应来自 ConfigLogin(CreateRole.UI / 名字库),
    /// ConfigManager 接入 yu_client 配表后替换下面的临时常量。
    /// 中央 _gp_model_con 是 3D 模型位,待 .lh 转换线。
    /// </summary>
    public sealed class LoginCreateRoleView : LoginCreateRoleViewBind
    {
        // 临时:ConfigLogin.CreateRole.UI/Res 的镜像(career/sex/名称/头像/默认装 role_res/模型位偏移 PosOffset)
        // TODO(配表线):ConfigManager 接入 ConfigLogin 后替换
        private static readonly (int career, int sex, string name, string selectIcon, string unselectIcon, int roleRes, float posOffsetY)[] CAREERS =
        {
            (1, 1, "剑士", "ui_Login_10", "ui_Login_10a", 1111, -0.53f),
            (2, 2, "武姬", "ui_Login_12", "ui_Login_12a", 1213, 0f),
            (3, 1, "枪使", "ui_Login_11", "ui_Login_11a", 1300, 0f),
            (4, 2, "弓手", "ui_Login_13", "ui_Login_13a", 1400, 0f),
        };

        // ConfigLogin.CreateRole.ModelPos 与 show_model_data.scale=0.5 的镜像
        private static readonly Vector2 MODEL_POS = new Vector2(0f, 1.53f);
        private const float MODEL_SCALE = 0.5f;

        private static readonly string[] RANDOM_NAMES =
        {
            "凌霄", "青岚", "破晓", "听雪", "惊鸿", "御风", "星河", "暮云",
        };

        // 对标老客户端 LoginCreateRoleView.ts:item.SetPosition(0, career*133) —— 左侧竖排
        private const float ITEM_STEP_Y = 133f;

        private readonly List<GameObject> _items = new List<GameObject>();
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
            BuildCareers();
            OnClickRandomName();
            ShowCareerModel();
        }

        protected override void OnHide()
        {
            Shenxiao.Common.UI3D.UIModelStage.Clear();
        }

        protected override void OnDispose()
        {
            EventDispatcher.Off<int>(GlobalEvent.EVT_GAME_CREATE_ROLE_RESULT, OnCreateResult);
        }

        private void BuildCareers()
        {
            foreach (GameObject go in _items) Destroy(go);
            _items.Clear();
            _selectedIndex = 0;

            for (int i = 0; i < CAREERS.Length; i++)
            {
                GameObject item = Instantiate(_tpl_LoginCreateRoleItem, _gp_head_con);
                item.SetActive(true);
                // y = career*133(Laya y 向下 → Unity 取负),x 固定 0
                ((RectTransform)item.transform).anchoredPosition =
                    new Vector2(0f, -CAREERS[i].career * ITEM_STEP_Y);

                var bind = item.GetComponent<LoginCreateRoleItemBind>();
                if (bind == null)
                {
                    GameLog.Error("Login", "职业项缺 LoginCreateRoleItemBind,重跑回填");
                    continue;
                }
                bind._lb_career.text = CAREERS[i].name;
                int captured = i;
                UIUtil.AddClick(bind._img_bg, () => OnClickCareer(captured));
                _items.Add(item);
            }
            RefreshCareerStates();
        }

        private void RefreshCareerStates()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var bind = _items[i].GetComponent<LoginCreateRoleItemBind>();
                if (bind == null) continue;
                bool selected = i == _selectedIndex;
                // 对标 LoginCreateRoleItem.ts:选中底图 ui_Login_02,未选 ui_Login_03;头像换 a 版
                string bg = selected ? "ui_Login_02" : "ui_Login_03";
                string icon = selected ? CAREERS[i].selectIcon : CAREERS[i].unselectIcon;
                _ = Shenxiao.Framework.Res.ResManager.SetImageAsync(bind._img_bg,
                        $"resource/game/login/texture/{bg}.png");
                _ = Shenxiao.Framework.Res.ResManager.SetImageAsync(bind._img_icon,
                        $"resource/game/login/texture/{icon}.png");
            }
        }

        private void OnClickCareer(int index)
        {
            _selectedIndex = index;
            RefreshCareerStates();
            ShowCareerModel();
        }

        /// <summary>中央 3D 模型(Laya SetRoleModel 对等):按职业默认装加载转换产物。</summary>
        private async void ShowCareerModel()
        {
            var career = CAREERS[_selectedIndex];
            string key = $"object/role/model_clothe_{career.roleRes}/model_clothe_{career.roleRes}";
            GameObject prefab = await Shenxiao.Framework.Res.ResManager.LoadAsync<GameObject>(key);
            if (prefab == null)
            {
                GameLog.Warn("Login", "职业模型未找到(先用 Laya3D 转换器生成):{0}", key);
                return;
            }
            Shenxiao.Common.UI3D.UIModelStage.Show(_gp_model_con, prefab,
                MODEL_SCALE, MODEL_POS + new Vector2(0f, career.posOffsetY));
        }

        private void OnClickRandomName()
        {
            string baseName = RANDOM_NAMES[Random.Range(0, RANDOM_NAMES.Length)];
            _lb_random_name.text = baseName + Random.Range(100, 999);
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
            var selected = CAREERS[_selectedIndex];
            LoginController.Instance.SendCreateRole(roleName, selected.career, selected.sex);
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

        private void OnClickReturn()
        {
            LoginFlow.BackToEnter();
        }
    }
}
