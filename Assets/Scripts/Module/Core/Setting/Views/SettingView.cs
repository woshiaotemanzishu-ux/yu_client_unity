using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Prefs;
using Shenxiao.Common.Audio;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
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
    /// 设置主界面(对标老客户端 setting/SettingView.ts):顶部头像+角色信息(改头像/改名/复制ID)、
    /// 双页签(基础设置/屏蔽列表)、四滑条(同屏人数/特效数量/音乐/音效)、自动拾取(17/18/19)、
    /// 御风云骑(202)/降神(201)/自动任务(21) 勾选、底部五钮(换角色/返回登录/还原默认/脱离卡死/修复异常)。
    ///
    /// 数据面:服务器权威 —— 值读 SettingModel(10202 全量),写走 SettingController(10203);
    /// 音量滑条实时改 AudioManager,数值在面板关闭时统一上报(对标老端 close_callback → SendSetSliderNum);
    /// 勾选项点击即单条上报(对标 SendProtocal)。文案/默认值读 SettingConfigs(config_setting.json)。
    /// 拾取项 150 级前禁取消(对标 CheckClickFun);极简模式下改屏蔽项先确认退出(对标老端 Alert)。
    /// 改头像/改名子窗仍为壳(13080/13083/42601 未移植),经 SettingFlow.OpenSub 打开。
    /// </summary>
    public sealed class SettingView : SettingViewBind
    {
        private const float SliderWidth = 225f;
        private const float DefaultSameScreenRoleCount = 5f;
        private const float DefaultEffectCount = 8f;
        private const float DefaultSoundVolume = 50f;
        private const float DefaultMusicVolume = 50f;
        private const float SettingShieldItemWidth = 250f;
        private const float SettingShieldItemHeight = 50f;
        private const string PrefLastRoleId = "login.lastRoleId";
        private const string CheckedSkin = "resource/game/common/texture/com_ui_gx1.png";
        private const string UncheckedSkin = "resource/game/common/texture/com_ui_gx.png";

        // 对标老端 ShowInShieldDic(SettingView.ts:44;顺序即展示顺序;微信推送 24 仅微信端,本端跳过)。
        private static readonly int[] ShieldSubtypes =
        {
            SettingModel.SUB_SPRITE, SettingModel.SUB_WING, SettingModel.SUB_SHENGQI,
            SettingModel.SUB_WEAPON, SettingModel.SUB_DEMON, SettingModel.SUB_PARTNER,
            SettingModel.SUB_SHIELD_CHANNEL, SettingModel.SUB_BACK,
            SettingModel.SUB_LIVENESS, SettingModel.SUB_SHOCK_SCREEN,
        };

        private static readonly int[] AutoPickSubtypes =
        {
            SettingModel.SUB_AUTO_BLUE, SettingModel.SUB_AUTO_PURPLE, SettingModel.SUB_AUTO_ORANGE,
        };

        private WithBtnHSlider _sameScreenSlider;
        private WithBtnHSlider _effectSlider;
        private WithBtnHSlider _soundSlider;
        private WithBtnHSlider _musicSlider;
        private bool _showingBaseTab = true;
        private CustomHeadItem _roleHeadItem;
        private bool _loadingRoleHead;
        private readonly List<SettingShieldItem> _autoPickItems = new List<SettingShieldItem>();
        private readonly List<SettingShieldItem> _shieldItems = new List<SettingShieldItem>();

        protected override void OnInit()
        {
            HideTemplates();
            BindClose(_img_close);
            BindClose(_img_empty);
            // 模态底点击关闭(对标老端 click_bg_toClose;SettingCreator 按名约定生成 ModalDim,旧转换 prefab 无此节点则跳过)。
            Transform dim = transform.Find("ModalDim");
            if (dim != null) BindClose(dim);
            BindButtons();
            EventDispatcher.On(GlobalEvent.EVT_SETTING_UPDATED, OnSettingUpdated);
        }

        protected override void OnDispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_SETTING_UPDATED, OnSettingUpdated);
        }

        private void OnDestroy()
        {
            EventDispatcher.Off(GlobalEvent.EVT_SETTING_UPDATED, OnSettingUpdated);
        }

        protected override void OnShow(object args)
        {
            BackfillRuntimeSkins();
            PopulateRoleInfo();
            RefreshHeadIcon();
            SelectTab(_showingBaseTab);
            _ = RefreshAsync();
        }

        /// <summary>面板关闭时统一上报四滑条(对标老端 close_callback → SendSetSliderNum)。</summary>
        protected override void OnHide()
        {
            SendSliderValues();
        }

        private async Task RefreshAsync()
        {
            await SettingConfigs.EnsureLoaded();
            await FuncOpenConfig.EnsureLoaded();
            if (this == null || !IsShown) return;

            BuildSliders();
            BuildAutoPickList();
            BuildShieldList();
            RefreshToggleBlocks();
        }

        /// <summary>设置数据变化(10202 到达/10203 写回成功)→ 刷新滑条值+勾选态+列表
        /// (对标老端 UPDATE_SETTING_INFO → OpenCallback 全量重铺;「还原默认设置」后滑条必须回位,
        /// 否则关面板时会把旧滑条值再写回去覆盖默认)。</summary>
        private void OnSettingUpdated()
        {
            if (!IsShown || !SettingConfigs.IsLoaded) return;
            BuildSliders();
            BuildAutoPickList();
            BuildShieldList();
            RefreshToggleBlocks();
        }

        // ---------------------------------------------------------------- 皮肤回填

        /// <summary>克隆模板节点(滑条/屏蔽项/拾取项/自定义头像)在运行时由列表/容器复制,先全部隐藏。</summary>
        private void BackfillRuntimeSkins()
        {
            SetSkin(_img_bg, "resource/game/setting/other/bg_03.png");
            SetSkin(_Image1, "resource/game/setting/texture/com_title_bg_1_.png");
            SetSkin(_Image4, "resource/game/setting/texture/ui_button_rect8.png");
            SetSkin(_Image6, "resource/game/setting/texture/ui_button_rect9.png");
            SetSkin(_btn_changename, "resource/game/setting/texture/ui_guild_07.png");
            SetSkin(_Image15, "resource/game/setting/texture/uixxsz_006.png");
            SetSkin(_Image16, "resource/game/setting/texture/uixxsz_007.png");
            SetSkin(simple_mode_bg, "resource/game/setting/texture/uixxsz_005.png");
            SetSkin(_Image14, "resource/game/setting/texture/uixxsz_009.png");
            SetSkin(_Image141, "resource/game/setting/texture/uixxsz_008.png");
            SetSkin(_img_tab_setting, "resource/game/setting/texture/ui_button_rect5.png");
            SetSkin(_img_tab_shield, "resource/game/setting/texture/ui_button_mid_1.png");
            SetSkin(_img_close, "resource/game/common/texture/uity_016k.png");
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

        // ---------------------------------------------------------------- 按钮接线

        private void BindButtons()
        {
            BindTab(_box_tab_base_setting, true);
            BindTab(_box_tab_shield_setting, false);

            // 顶部头像/角色信息:改头像/改名 → 打开子窗(SettingFlow.OpenSub 叠在主面板上);复制ID → 系统剪贴板。
            BindOpen(change_head_btn, "SettingChangeHeadView", "更换头像");
            BindOpen(_btn_changename, "SettingChangeNameView", "修改名字");
            BindClick(_btn_copy, CopyRoleId);

            // 勾选项(降神/御风云骑/自动任务;对标老端 change_god_box/change_horse_box/task_check 点击)。
            BindClick(_img_god_check, () => ToggleSysSetting(SettingModel.SUB_GODBEFALL));
            BindClick(_img_horse_check, OnToggleHorse);
            BindClick(_img_task_check1, () => SetAutoTask(1));
            BindClick(_img_task_check2, () => SetAutoTask(0));

            // 底部操作(对标老端 SettingView.ts:288-346,均带二次确认)。
            BindClick(change_role, () => TipsManager.Confirm("是否返回角色选择界面？", SettingFlow.BackToRoleSelect));
            BindClick(return_login, () => TipsManager.Confirm("是否返回登录界面？", SettingFlow.BackToLogin));
            BindClick(simple_mode_btn, () => TipsManager.Confirm("是否还原默认设置", SendDefaultMode));
            BindClick(confirm_flee, () => TipsManager.Confirm("是否脱离卡死？", () =>
                SettingController.Instance.SendFlee(RoleModel.Instance.SceneId)));
            BindClick(confirm_res, () => TipsManager.Confirm("修复异常后，需要重新连接游戏", SettingFlow.ReconnectRepair));
        }

        private void CopyRoleId()
        {
            long roleId = RoleModel.Instance.RoleId;
            if (roleId <= 0 && id_number != null) long.TryParse(id_number.text, out roleId);
            if (roleId <= 0) return;
            GUIUtility.systemCopyBuffer = roleId.ToString();
            TipsManager.Toast("已复制");
        }

        /// <summary>「还原默认设置」(对标老端 SimpleModeFun):按 ClientBlockConfig.DefaultMode 批量下发 10203。</summary>
        private void SendDefaultMode()
        {
            List<KeyValuePair<int, int>> defaults = SettingConfigs.GetDefaultModeList();
            if (defaults.Count == 0)
            {
                GameLog.Warn("Setting", "ClientBlockConfig.DefaultMode 为空,还原默认设置未发送");
                return;
            }
            SettingController.Instance.SendSettingList(SettingModel.TYPE_SYS_SETTING, defaults);
        }

        // ---------------------------------------------------------------- 滑条(同屏/特效/音乐/音效)

        private void BuildSliders()
        {
            float maxNum = SettingConfigs.MaxRoleNum;
            float same = SettingModel.Get(SettingModel.TYPE_SYS_SETTING, SettingModel.SUB_SAME_SCREEN_ROLE_NUM, (int)DefaultSameScreenRoleCount);
            float effect = SettingModel.Get(SettingModel.TYPE_SYS_SETTING, SettingModel.SUB_EFFECT_NUM, (int)DefaultEffectCount);
            float sound = SettingModel.Get(SettingModel.TYPE_SYS_SETTING, SettingModel.SUB_SOUND_OPEN, (int)DefaultSoundVolume);
            float music = SettingModel.Get(SettingModel.TYPE_SYS_SETTING, SettingModel.SUB_SOUND_EFFECT_OPEN, (int)DefaultMusicVolume);

            EnsureSlider(ref _sameScreenSlider, slider_conta1, same, 0f, maxNum, v => SetNumber(_screen_value, v));
            EnsureSlider(ref _effectSlider, slider_conta2, effect, 0f, maxNum, v => SetNumber(_effect_value, v));
            EnsureSlider(ref _soundSlider, slider_audio, sound, 0f, 100f, v =>
            {
                SetNumber(_sound_value, v);
                AudioManager.SetVolume(AudioManager.Category.Sfx, v / 100f); // 「音效」滑条(subtype 9,老端 ChangeVolumeEffect)
            });
            EnsureSlider(ref _musicSlider, slider_music, music, 0f, 100f, v =>
            {
                SetNumber(_music_value, v);
                AudioManager.SetVolume(AudioManager.Category.Music, v / 100f); // 「音乐」滑条(subtype 12,老端 ChangeVolume)
            });
        }

        /// <summary>关闭时把四滑条当前值批量写回服务器(subtype 6/7/9/12,对标 SendSetSliderNum)。</summary>
        private void SendSliderValues()
        {
            if (_sameScreenSlider == null && _effectSlider == null && _soundSlider == null && _musicSlider == null) return;

            var list = new List<KeyValuePair<int, int>>(4);
            AppendSlider(list, SettingModel.SUB_SAME_SCREEN_ROLE_NUM, _sameScreenSlider);
            AppendSlider(list, SettingModel.SUB_EFFECT_NUM, _effectSlider);
            AppendSlider(list, SettingModel.SUB_SOUND_OPEN, _soundSlider);
            AppendSlider(list, SettingModel.SUB_SOUND_EFFECT_OPEN, _musicSlider);
            if (list.Count > 0)
            {
                SettingController.Instance.SendSettingList(SettingModel.TYPE_SYS_SETTING, list);
            }
        }

        private static void AppendSlider(List<KeyValuePair<int, int>> list, int subtype, WithBtnHSlider slider)
        {
            if (slider == null) return;
            list.Add(new KeyValuePair<int, int>(subtype, Mathf.RoundToInt(slider.GetValue())));
        }

        private void EnsureSlider(ref WithBtnHSlider slider, RectTransform parent, float value, float min, float max, Action<float> onChange)
        {
            onChange?.Invoke(value);
            if (slider == null)
            {
                if (_tpl_WithBtnHSlider == null || parent == null) return;
                // SettingCreator 烤的静态预览滑条(纯视觉件)在真滑条就位时移除(编辑期 harness 无 Play 态,走 Immediate)。
                Transform preview = parent.Find("__PreviewSlider");
                if (preview != null)
                {
                    if (Application.isPlaying) Destroy(preview.gameObject);
                    else DestroyImmediate(preview.gameObject);
                }
                GameObject go = Instantiate(_tpl_WithBtnHSlider, parent);
                go.name = _tpl_WithBtnHSlider.name + "_" + parent.name;
                go.SetActive(true);

                RectTransform rt = go.transform as RectTransform;
                if (rt != null)
                {
                    // 老端 new WithBtnHSlider(parent) 落在容器 (0,0):左上锚+左上枢轴(快照里滑条克隆即容器左上角)。
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
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
                slider.SetData(value, 1f, min, max, v => onChange?.Invoke(v));
            }
        }

        private static void SetNumber(TextMeshProUGUI label, float value)
        {
            if (label != null) label.text = Mathf.RoundToInt(value).ToString();
        }

        // ---------------------------------------------------------------- 角色信息/头像

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
                // 优先收编 SettingCreator 烤进 prefab 的 CustomHeadItem 静态预览实例。
                _roleHeadItem = _role_head.GetComponentInChildren<CustomHeadItem>(true);
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
                }

                _roleHeadItem.gameObject.SetActive(true);
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

        // ---------------------------------------------------------------- 自动拾取 / 屏蔽列表

        /// <summary>自动拾取三项(17/18/19,对标 UpdateAutoPickBlockCheck):服务器有数据且配置有文案才显示。</summary>
        private void BuildAutoPickList()
        {
            RebuildShieldItems(_autoPickItems, _list_pick, AutoPickSubtypes, OnToggleAutoPick);
        }

        /// <summary>屏蔽列表页(对标 SetBlockCheck + ShowInShieldDic)。</summary>
        private void BuildShieldList()
        {
            RebuildShieldItems(_shieldItems, _list_shield, ShieldSubtypes, OnToggleShield);
        }

        private void RebuildShieldItems(List<SettingShieldItem> items, ScrollRect listRoot, int[] subtypes, Action<int> onToggle)
        {
            if (_tpl_SettingShieldItem == null || listRoot == null) return;

            RectTransform parent = listRoot.content;
            if (parent == null) parent = listRoot.transform as RectTransform;
            if (parent == null) return;

            // 收编 SettingCreator 烤进 prefab 的静态预览项(同顺序),避免克隆一份重复的。
            if (items.Count == 0)
            {
                foreach (SettingShieldItem baked in parent.GetComponentsInChildren<SettingShieldItem>(true))
                {
                    baked.Show();
                    items.Add(baked);
                }
            }

            int used = 0;
            foreach (int subtype in subtypes)
            {
                // 老端容错:配置表查不到不显示;服务器行缺失时退配置默认值(点击切换会经 10203 建行)。
                SettingConfigs.ItemCfg cfg = SettingConfigs.GetItem(SettingModel.TYPE_SYS_SETTING, subtype);
                if (cfg == null || string.IsNullOrEmpty(cfg.Name)) continue;
                int isOpen = SettingModel.Get(SettingModel.TYPE_SYS_SETTING, subtype, cfg.DefaultOpen);

                SettingShieldItem item = GetOrCreateShieldItem(items, parent, used);
                if (item == null) continue;

                item.gameObject.SetActive(true);
                PlaceShieldItem(item, used);
                int captured = subtype;
                item.SetData(cfg.Name, isOpen == 1, () => onToggle(captured));
                used++;
            }
            for (int i = used; i < items.Count; i++)
            {
                if (items[i] != null) items[i].gameObject.SetActive(false);
            }

            parent.sizeDelta = new Vector2(
                Mathf.Max(parent.sizeDelta.x, SettingShieldItemWidth * 2f),
                Mathf.Max((used + 1) / 2 * SettingShieldItemHeight, SettingShieldItemHeight));
        }

        private SettingShieldItem GetOrCreateShieldItem(List<SettingShieldItem> items, RectTransform parent, int index)
        {
            while (items.Count <= index) items.Add(null);
            if (items[index] != null) return items[index];

            GameObject go = Instantiate(_tpl_SettingShieldItem, parent, false);
            go.name = "SettingShieldItem_" + parent.name + "_" + index;
            SettingShieldItem item = go.GetComponent<SettingShieldItem>();
            if (item == null) item = go.GetComponentInChildren<SettingShieldItem>(true);
            if (item == null)
            {
                Destroy(go);
                return null;
            }
            item.Show();
            items[index] = item;
            return item;
        }

        private static void PlaceShieldItem(SettingShieldItem item, int index)
        {
            RectTransform rt = item.transform as RectTransform;
            if (rt == null) return;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(index % 2 * SettingShieldItemWidth, -(index / 2) * SettingShieldItemHeight);
            rt.sizeDelta = new Vector2(SettingShieldItemWidth, SettingShieldItemHeight);
            rt.localScale = Vector3.one;
        }

        /// <summary>自动拾取项点击:150 级前禁取消(对标老端 CheckClickFun 的等级闸),其余走通用切换。</summary>
        private void OnToggleAutoPick(int subtype)
        {
            if (SettingModel.Get(SettingModel.TYPE_SYS_SETTING, subtype, 1) == 1 && RoleModel.Instance.Level < 150)
            {
                TipsManager.Toast("150级后可取消选项");
                return;
            }
            ToggleWithSimpleModeGuard(subtype);
        }

        private void OnToggleShield(int subtype)
        {
            ToggleWithSimpleModeGuard(subtype);
        }

        /// <summary>极简模式下改屏蔽项先确认退出该模式(对标老端 CheckClickFun 的 simple_mode 分支)。</summary>
        private void ToggleWithSimpleModeGuard(int subtype)
        {
            bool inSimpleMode = SettingModel.Get(SettingModel.TYPE_SYS_SETTING, SettingModel.SUB_SIMPLE_MODE, 0) == 1;
            if (inSimpleMode && subtype != SettingModel.SUB_GODBEFALL)
            {
                TipsManager.Confirm("您当前处于极简模式，是否确定退出该模式？", () =>
                {
                    SettingController.Instance.SendSetting(SettingModel.TYPE_SYS_SETTING, SettingModel.SUB_SIMPLE_MODE, 0);
                    ToggleSysSetting(subtype);
                });
                return;
            }
            ToggleSysSetting(subtype);
        }

        /// <summary>读设置值:服务器行缺失退配置默认(config_setting.json is_open),再缺退 1。</summary>
        private static int GetSettingValue(int subtype)
        {
            SettingConfigs.ItemCfg cfg = SettingConfigs.GetItem(SettingModel.TYPE_SYS_SETTING, subtype);
            return SettingModel.Get(SettingModel.TYPE_SYS_SETTING, subtype, cfg?.DefaultOpen ?? 1);
        }

        /// <summary>通用切换:取反当前值发 10203(对标老端 SendProtocal(subtype, open))。</summary>
        private void ToggleSysSetting(int subtype)
        {
            SettingController.Instance.SendSetting(SettingModel.TYPE_SYS_SETTING, subtype, GetSettingValue(subtype) == 1 ? 0 : 1);
        }

        // ---------------------------------------------------------------- 御风云骑/降神/自动任务

        private void OnToggleHorse()
        {
            int open = GetSettingValue(SettingModel.SUB_AUTO_HORSE) == 1 ? 0 : 1;
            SettingController.Instance.SendSetting(SettingModel.TYPE_SYS_SETTING, SettingModel.SUB_AUTO_HORSE, open);
            if (open == 0)
            {
                TipsManager.Toast("场景中上下轻滑即可上下御风云骑~"); // 对标老端 change_horse_box
            }
        }

        /// <summary>自动任务双勾:check1=自动(1)/check2=手动(0),点已选中的一侧不重发(对标老端 task_check 点击)。</summary>
        private void SetAutoTask(int open)
        {
            if (GetSettingValue(SettingModel.SUB_AUTO_TASK) == open) return;
            SettingController.Instance.SendSetting(SettingModel.TYPE_SYS_SETTING, SettingModel.SUB_AUTO_TASK, open);
        }

        /// <summary>三块勾选区显隐 + 勾选态(对标 UpdateGodSetting/UpdateHorseSetting/UpdateTaskSetting):
        /// 降神/坐骑走功能开放门禁,自动任务看服务器是否下发;隐藏项之后做纵向紧排消空位。</summary>
        private void RefreshToggleBlocks()
        {
            bool hasHorse = SettingConfigs.GetItem(SettingModel.TYPE_SYS_SETTING, SettingModel.SUB_AUTO_HORSE) != null;
            bool hasGod = SettingConfigs.GetItem(SettingModel.TYPE_SYS_SETTING, SettingModel.SUB_GODBEFALL) != null;
            bool hasTask = SettingConfigs.GetItem(SettingModel.TYPE_SYS_SETTING, SettingModel.SUB_AUTO_TASK) != null;
            int horseOpen = GetSettingValue(SettingModel.SUB_AUTO_HORSE);
            int godOpen = GetSettingValue(SettingModel.SUB_GODBEFALL);
            int taskOpen = GetSettingValue(SettingModel.SUB_AUTO_TASK);

            bool showHorse = hasHorse && FuncOpenConfig.CheckFuncOpenState("HorseComponentView");
            bool showGod = hasGod && FuncOpenConfig.CheckFuncOpenState("GodBefallMainView");
            bool showTask = hasTask;

            SetNodeVisible(_box_horse, showHorse);
            SetNodeVisible(_box_god, showGod);
            SetNodeVisible(_box_task, showTask);

            if (showHorse) SetCheckSkin(_img_horse_check, horseOpen == 1);
            if (showGod) SetCheckSkin(_img_god_check, godOpen == 1);
            if (showTask)
            {
                SetCheckSkin(_img_task_check1, taskOpen == 1);
                SetCheckSkin(_img_task_check2, taskOpen != 1);
            }

            ReflowBaseBlocks();
        }

        /// <summary>基础设置页各块纵向紧排(对标老端 UpdateBaseSettingPanelHeight):隐藏块不占位。
        /// 依赖各块 anchor(0,1)/pivot(0,1)(SettingCreator 保证),只改 y、保留各自 x。</summary>
        private void ReflowBaseBlocks()
        {
            float y = 10f;
            ReflowBlock(_box_slider, ref y);
            ReflowBlock(_box_pick, ref y);
            ReflowBlock(_box_horse, ref y);
            ReflowBlock(_box_god, ref y);
            ReflowBlock(_box_task, ref y);
        }

        private static void ReflowBlock(RectTransform block, ref float y)
        {
            if (block == null || !block.gameObject.activeSelf) return;
            block.anchoredPosition = new Vector2(block.anchoredPosition.x, -y);
            y += block.sizeDelta.y + 10f;
        }

        private static void SetCheckSkin(Image img, bool isChecked)
        {
            if (img == null) return;
            img.raycastTarget = true;
            _ = ResManager.SetImageAsync(img, isChecked ? CheckedSkin : UncheckedSkin, nativeSize: false);
        }

        // ---------------------------------------------------------------- 通用绑定/页签

        /// <summary>按钮 → 打开设置模块内子窗(SettingFlow.OpenSub 按 View 子类名查找并叠在主面板上)。</summary>
        private void BindOpen(Component target, string viewType, string label)
        {
            BindClick(target, () =>
            {
                GameLog.Info("Setting", "点击[{0}] → 打开 {1}", label, viewType);
                SettingFlow.OpenSub(viewType);
            });
        }

        /// <summary>关闭按钮(Image 或含 Image 容器)→ Hide(关闭本窗)。</summary>
        private void BindClose(Component target)
        {
            BindClick(target, Hide);
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击。</summary>
        private static void BindClick(Component target, Action onClick)
        {
            if (target == null || onClick == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }

        private void BindTab(Component target, bool baseTab)
        {
            BindClick(target, () => SelectTab(baseTab));
        }

        private void SelectTab(bool baseTab)
        {
            _showingBaseTab = baseTab;
            if (_box_base_setting != null) _box_base_setting.gameObject.SetActive(baseTab);
            if (_box_shield_setting != null) _box_shield_setting.gameObject.SetActive(!baseTab);
            if (_lb_tab_setting != null) _lb_tab_setting.color = baseTab ? Color.white : new Color(0.58f, 0.36f, 0.25f);
            if (_lb_tab_shield != null) _lb_tab_shield.color = !baseTab ? Color.white : new Color(0.58f, 0.36f, 0.25f);
        }

        private static void SetNodeVisible(Component c, bool visible)
        {
            if (c != null) c.gameObject.SetActive(visible);
        }
    }
}
