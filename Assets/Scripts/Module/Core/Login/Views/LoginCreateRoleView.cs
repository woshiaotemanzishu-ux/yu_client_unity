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
        // 临时:ConfigLogin.CreateRole.UI 的镜像(career, sex, 名称)
        private static readonly (int career, int sex, string name)[] CAREERS =
        {
            (1, 1, "剑士"), (2, 2, "武姬"), (3, 1, "枪使"), (4, 2, "弓手"),
        };

        private static readonly string[] RANDOM_NAMES =
        {
            "凌霄", "青岚", "破晓", "听雪", "惊鸿", "御风", "星河", "暮云",
        };

        private const float ITEM_SPACING = 10f;

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

            RectTransform tplRect = (RectTransform)_tpl_LoginCreateRoleItem.transform;
            float itemWidth = tplRect.sizeDelta.x;

            for (int i = 0; i < CAREERS.Length; i++)
            {
                GameObject item = Instantiate(_tpl_LoginCreateRoleItem, _gp_head_con);
                item.SetActive(true);
                ((RectTransform)item.transform).anchoredPosition = new Vector2(i * (itemWidth + ITEM_SPACING), 0f);

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
                bind._img_bg.color = i == _selectedIndex ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            }
        }

        private void OnClickCareer(int index)
        {
            _selectedIndex = index;
            RefreshCareerStates();
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
            (int career, int sex, string _) = CAREERS[_selectedIndex];
            LoginController.Instance.SendCreateRole(roleName, career, sex);
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
