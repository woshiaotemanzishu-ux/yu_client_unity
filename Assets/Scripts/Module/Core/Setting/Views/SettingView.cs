using System.Collections.Generic;
using Shenxiao.Common.Prefs;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Role;
using Shenxiao.Generated.UI.Setting;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Setting
{
    /// <summary>
    /// 设置主界面(大)(对标老客户端 setting/SettingView.ts):顶部头像(_role_head/change_head_btn 改头像)+ 角色信息
    /// (_lb_name 名/_btn_changename 改名/id_number 玩家ID/_btn_copy 复制/id_ser_name 区服)+ 底部操作
    /// (change_role 换角色/return_login 返回登录/simple_mode_btn 简洁模式/confirm_flee 确认逃跑(脱离卡死)/confirm_res 复活(修复))+
    /// 双页签(_box_tab_base_setting 基础设置 / _box_tab_shield_setting 屏蔽列表)+
    /// 基础设置页(_box_base_setting):音/效/屏 滑条(slider_audio/slider_music/slider_conta2/slider_conta1,克隆 _tpl_WithBtnHSlider)、
    /// 自动拾取(_list_pick)、御风云骑(_box_horse/_img_horse_check)、神祭(_box_god/_img_god_check)、自动任务(_box_task/_img_task_check1-2)+
    /// 屏蔽列表页(_box_shield_setting/_list_shield 克隆 _tpl_SettingShieldItem)+ 关闭(_img_close/_img_empty)。
    ///
    /// 降级:SettingModel/RoleManager/SoundManager/各协议(10203/10210/42602)、WithBtnHSlider 滑条组件、
    /// CustomHeadItem 头像、LoopScrowViewMgr 列表(SettingShieldItem/SettingSubscriptionItem)、OpenFun(203 改头像)、
    /// Alert 二次确认 均未移植 → 所有 _tpl_* 模板隐藏、屏蔽/拾取列表空、滑条无数值;头像/改名/换角色/返回登录/简洁模式/
    /// 确认逃跑/复活/页签 等按钮点击仅打日志「待对接」;change_head_btn / _btn_changename 暂打日志(父级后续接 OpenSub);
    /// _img_close / _img_empty 关闭可用。事件驱动窗口,默认关闭、不进 FirstPass。数据/列表后续 tick 补。
    /// </summary>
    public sealed class SettingView : SettingViewBind
    {
        private const float SliderWidth = 225f;
        private const float DefaultSameScreenRoleCount = 5f;
        private const float DefaultEffectCount = 8f;
        private const float DefaultSoundVolume = 50f;
        private const float DefaultMusicVolume = 50f;
        private const float MaxSameScreenRoleCount = 20f;
        private const float MaxEffectCount = 20f;
        private const float SettingShieldItemWidth = 250f;
        private const float SettingShieldItemHeight = 50f;
        private const string PrefLastRoleId = "login.lastRoleId";
        private static readonly string[] AutoPickLabels =
        {
            "\u84DD\u8272\u53CA\u4EE5\u4E0B\u88C5\u5907",
            "\u7D2B\u8272\u88C5\u5907",
            "\u6A59\u8272\u53CA\u4EE5\u4E0A\u7684\u4E00\u661F\u88C5\u5907",
        };

        private WithBtnHSlider _sameScreenSlider;
        private WithBtnHSlider _effectSlider;
        private WithBtnHSlider _soundSlider;
        private WithBtnHSlider _musicSlider;
        private bool _showingBaseTab = true;
        private CustomHeadItem _roleHeadItem;
        private bool _loadingRoleHead;
        private readonly List<GameObject> _autoPickItems = new List<GameObject>();

        protected override void OnInit()
        {
            HideTemplates();
            BindClose(_img_close);
            BindClose(_img_empty);
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            BackfillRuntimeSkins();
            PopulateRoleInfo();
            RefreshHeadIcon();
            BuildSliders();
            BuildAutoPickList();
            HideUnmigratedBaseBlocks();
            SelectTab(_showingBaseTab);
            // 老端 OpenCallback → SettingModel/RoleManager 铺角色信息 + 滑条/列表/页签。均未移植 → 列表空/默认降级。
            GameLog.Info("Setting", "SettingView 打开 → 待对接 SettingModel(列表空/默认降级)");
        }

        /// <summary>克隆模板节点(滑条/屏蔽项/拾取项/自定义头像/神祭)在运行时由列表/容器复制,先全部隐藏。</summary>
        private void BackfillRuntimeSkins()
        {
            SetSkin(_img_bg, "resource/game/setting/other/bg_03.png");
            SetSkin(_Image1, "resource/game/setting/texture/com_title_bg_1_.png");
            SetSkin(_Image4, "resource/game/setting/texture/ui_button_rect8.png");
            SetSkin(_Image6, "resource/game/setting/texture/ui_button_rect9.png");
            SetSkin(_btn_changename, "resource/game/setting/texture/ui_Guild_07.png");
            SetSkin(_Image15, "resource/game/setting/texture/uixxsz_006.png");
            SetSkin(_Image16, "resource/game/setting/texture/uixxsz_007.png");
            SetSkin(simple_mode_bg, "resource/game/setting/texture/uixxsz_005.png");
            SetSkin(_Image14, "resource/game/setting/texture/uixxsz_009.png");
            SetSkin(_Image141, "resource/game/setting/texture/uixxsz_008.png");
            SetSkin(_img_tab_setting, "resource/game/setting/texture/ui_button_rect5.png");
            SetSkin(_img_tab_shield, "resource/game/setting/texture/ui_button_mid_1.png");
            SetSkin(_img_close, "resource/game/common/texture/uity_016k.png");
            SetSkin(_img_horse_check, "resource/game/common/texture/uity_045d.png");
            SetSkin(_img_god_check, "resource/game/common/texture/com_ui_gx.png");
            SetSkin(_img_task_check1, "resource/game/common/texture/com_ui_gx.png");
            SetSkin(_img_task_check2, "resource/game/common/texture/com_ui_gx.png");
        }

        private static void SetSkin(Image target, string path)
        {
            if (target == null || string.IsNullOrEmpty(path)) return;
            _ = ResManager.SetImageAsync(target, path, nativeSize: false);
        }

        private void HideTemplates()
        {
            if (_tpl_SettingShieldItem != null) _tpl_SettingShieldItem.SetActive(false);
            if (_tpl_SettingSubscriptionItem != null) _tpl_SettingSubscriptionItem.SetActive(false);
            if (_tpl_CustomHeadItem != null) _tpl_CustomHeadItem.SetActive(false);
            if (_tpl_WithBtnHSlider != null) _tpl_WithBtnHSlider.SetActive(false);
            if (_tpl_GodBefallMainView != null) _tpl_GodBefallMainView.SetActive(false);
        }

        private void BindButtons()
        {
            BindTab(_box_tab_base_setting, true);
            BindTab(_box_tab_shield_setting, false);
            // 顶部头像/角色信息:改头像/改名 → 真打开子窗(经 SettingFlow.OpenSub 叠在主面板上,各子窗 _close_btn 返回)。
            BindOpen(change_head_btn, "SettingChangeHeadView", "更换头像");
            BindOpen(_btn_changename, "SettingChangeNameView", "修改名字");
            BindBtn(_btn_copy, "复制玩家ID");
            // 页签切换(基础设置 / 屏蔽列表)。
            BindBtn(_box_tab_base_setting, "页签-基础设置");
            BindBtn(_box_tab_shield_setting, "页签-屏蔽列表");
            // 屏蔽/勾选项(神祭 / 御风云骑 / 自动任务开关)。
            BindBtn(_img_god_check, "神祭开关");
            BindBtn(_img_horse_check, "御风云骑开关");
            BindBtn(_img_task_check1, "自动任务-开");
            BindBtn(_img_task_check2, "自动任务-关");
            // 底部操作(换角色 / 返回登录 / 简洁模式 / 确认逃跑 / 复活)。
            BindBtn(change_role, "更换角色");
            BindBtn(return_login, "返回登录");
            BindBtn(simple_mode_btn, "简洁模式");
            BindBtn(confirm_flee, "确认逃跑(脱离卡死)");
            BindBtn(confirm_res, "复活(修复异常)");
        }

        /// <summary>按钮 → 打开设置模块内子窗(SettingFlow.OpenSub 按 View 子类名查找并叠在主面板上)。</summary>
        private void BindOpen(Component target, string viewType, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                GameLog.Info("Setting", "点击[{0}] → 打开 {1}", label, viewType);
                SettingFlow.OpenSub(viewType);
            });
        }

        /// <summary>关闭按钮(Image 或含 Image 容器)→ Hide(关闭本窗)。</summary>
        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/子面板待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Setting", "点击[{0}] → 待对接", label));
        }

        private void BuildSliders()
        {
            EnsureSlider(ref _sameScreenSlider, slider_conta1, DefaultSameScreenRoleCount, 0f, MaxSameScreenRoleCount, _screen_value);
            EnsureSlider(ref _effectSlider, slider_conta2, DefaultEffectCount, 0f, MaxEffectCount, _effect_value);
            EnsureSlider(ref _soundSlider, slider_audio, DefaultSoundVolume, 0f, 100f, _sound_value);
            EnsureSlider(ref _musicSlider, slider_music, DefaultMusicVolume, 0f, 100f, _music_value);
        }

        private void EnsureSlider(ref WithBtnHSlider slider, RectTransform parent, float value, float min, float max, TextMeshProUGUI valueLabel)
        {
            SetNumber(valueLabel, value);
            if (slider == null)
            {
                if (_tpl_WithBtnHSlider == null || parent == null) return;
                GameObject go = Instantiate(_tpl_WithBtnHSlider, parent);
                go.name = _tpl_WithBtnHSlider.name + "_" + parent.name;
                go.SetActive(true);

                RectTransform rt = go.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 0.5f);
                    rt.anchorMax = new Vector2(0f, 0.5f);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.localScale = Vector3.one;
                }

                slider = go.GetComponent<WithBtnHSlider>();
                if (slider != null)
                {
                    slider.Show();
                    slider.HideNumBtnAndNumBg();
                    slider.SetCustomWidth(SliderWidth);
                }
            }

            if (slider != null)
            {
                slider.SetData(value, 1f, min, max, v => SetNumber(valueLabel, v));
            }
        }

        private static void SetNumber(TextMeshProUGUI label, float value)
        {
            if (label != null) label.text = Mathf.RoundToInt(value).ToString();
        }

        private void PopulateRoleInfo()
        {
            RoleModel role = RoleModel.Instance;
            if (role == null) return;

            long roleId = role.RoleId;
            string roleName = role.Name;
            string serverName = role.ServerName;
            GameRoleInfo loginRole = FindLoginRole(roleId);
            if (loginRole != null)
            {
                if (roleId <= 0) roleId = loginRole.roleId;
                if (string.IsNullOrEmpty(roleName) || role.RoleId <= 0) roleName = loginRole.DisplayName;
            }
            if (roleId <= 0 && long.TryParse(PrefsManager.GetString(PrefLastRoleId, string.Empty), out long prefRoleId))
            {
                roleId = prefRoleId;
            }

            LoginServerInfo server = LoginModel.Instance.SelectedServer;
            if (string.IsNullOrEmpty(serverName) && server != null) serverName = server.name;

            bool hasRole = role.HasBaseInfo || roleId > 0 || loginRole != null;
            if (!hasRole) return;
            if (_lb_name != null) _lb_name.text = roleName;
            if (id_number != null && roleId > 0) id_number.text = roleId.ToString();
            if (id_ser_name != null && !string.IsNullOrEmpty(serverName)) id_ser_name.text = serverName;
        }

        private async void RefreshHeadIcon()
        {
            RoleModel role = RoleModel.Instance;
            if (_role_head == null || role == null) return;

            int career = role.Career;
            int turn = role.Figure != null ? role.Figure.turn : 0;
            int level = role.Level;
            ApplyLoginRoleFallback(role, ref career, ref turn, ref level);
            if (career <= 0) career = 1;
            if (level < 0) level = 0;

            if (_roleHeadItem == null)
            {
                if (_loadingRoleHead) return;
                _loadingRoleHead = true;
                GameObject go = _tpl_CustomHeadItem != null
                    ? Instantiate(_tpl_CustomHeadItem, _role_head, false)
                    : await ResManager.InstantiateAsync(GameResPath.GetUIPrefab("common", "CustomHeadItem"), _role_head);
                _loadingRoleHead = false;
                if (go == null) return;

                go.name = "CustomHeadItem";
                go.SetActive(true);
                RectTransform rt = go.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.localScale = Vector3.one;
                }

                _roleHeadItem = go.GetComponent<CustomHeadItem>();
                if (_roleHeadItem == null) _roleHeadItem = go.GetComponentInChildren<CustomHeadItem>(true);
                if (_roleHeadItem == null) return;

                _roleHeadItem.Show();
                _roleHeadItem.SetActiveFrame(false);
                _roleHeadItem.SetActiveLevel(false);
                _roleHeadItem.SetActiveBg(false);
            }

            _roleHeadItem.SetRoleData(career, turn, level, showLevel: false);
        }

        private static GameRoleInfo FindLoginRole(long roleId)
        {
            IReadOnlyList<GameRoleInfo> roles = LoginModel.Instance.Roles;
            if (roles == null || roles.Count == 0) return null;
            if (roleId > 0)
            {
                for (int i = 0; i < roles.Count; i++)
                {
                    if (roles[i] != null && roles[i].roleId == roleId) return roles[i];
                }
            }
            return roles[0];
        }

        private static void ApplyLoginRoleFallback(RoleModel role, ref int career, ref int turn, ref int level)
        {
            long roleId = role != null ? role.RoleId : 0;
            GameRoleInfo loginRole = FindLoginRole(roleId);
            if (loginRole == null) return;

            if (career <= 0) career = loginRole.Career;
            if (turn <= 0) turn = loginRole.Turn;
            if (level <= 0) level = loginRole.Level;
        }

        private void BuildAutoPickList()
        {
            ClearRuntimeItems(_autoPickItems);
            if (_tpl_SettingShieldItem == null || _list_pick == null) return;

            RectTransform parent = _list_pick.content;
            if (parent == null) parent = _list_pick.transform as RectTransform;
            if (parent == null) return;

            for (int i = 0; i < AutoPickLabels.Length; i++)
            {
                GameObject go = Instantiate(_tpl_SettingShieldItem, parent, false);
                go.name = "SettingAutoPickItem_" + i;
                go.SetActive(true);
                _autoPickItems.Add(go);

                RectTransform rt = go.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2((i % 2) * SettingShieldItemWidth, -(i / 2) * SettingShieldItemHeight);
                    rt.sizeDelta = new Vector2(SettingShieldItemWidth, SettingShieldItemHeight);
                    rt.localScale = Vector3.one;
                }

                SettingShieldItem item = go.GetComponent<SettingShieldItem>();
                if (item == null) item = go.GetComponentInChildren<SettingShieldItem>(true);
                if (item == null) continue;
                item.Show();
                item.SetData(AutoPickLabels[i], true);
            }

            parent.sizeDelta = new Vector2(
                Mathf.Max(parent.sizeDelta.x, SettingShieldItemWidth * 2f),
                Mathf.Max(parent.sizeDelta.y, SettingShieldItemHeight * 2f));
        }

        private void HideUnmigratedBaseBlocks()
        {
            HideNode(_box_horse);
            HideNode(_box_god);
            HideNode(_box_task);
        }

        private void BindTab(Component target, bool baseTab)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => SelectTab(baseTab));
        }

        private void SelectTab(bool baseTab)
        {
            _showingBaseTab = baseTab;
            if (_box_base_setting != null) _box_base_setting.gameObject.SetActive(baseTab);
            if (_box_shield_setting != null) _box_shield_setting.gameObject.SetActive(!baseTab);
            if (_lb_tab_setting != null) _lb_tab_setting.color = baseTab ? Color.white : new Color(0.58f, 0.36f, 0.25f);
            if (_lb_tab_shield != null) _lb_tab_shield.color = !baseTab ? Color.white : new Color(0.58f, 0.36f, 0.25f);
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }

        private static void ClearRuntimeItems(List<GameObject> items)
        {
            if (items == null) return;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null) continue;
                Destroy(items[i]);
            }
            items.Clear();
        }
    }
}
