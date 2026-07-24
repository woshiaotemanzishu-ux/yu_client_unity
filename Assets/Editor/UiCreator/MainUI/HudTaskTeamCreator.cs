using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Team;
using Shenxiao.Module.Core.MainUI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 主界面左侧【任务·组队面板】(MainUITaskTeamView)纯代码建树生成器。
    ///
    /// 结构对标运行时快照(mainui_123123,270 节点,含真实任务列表)+ 老端场景 JSON
    /// resource/game/mainUI/MainUITaskTeamView.json(40 个设计节点 + 2 个未落盘的模板字段
    /// _tpl_MainUITaskItem/_tpl_TeamMainRoleItem,与 Bind 42 个字段一一对应):
    ///   _box_con(224x314 主体)
    ///     _box_task_tab / _box_team_tab —— 左缘竖排"任务/组队"两个 tab(点击见 BindButtons)
    ///     _box_temple_awaken —— 神殿觉醒条(运行时按 42901 快照 + config_temple_awaken* 配置显隐/填充)
    ///     _box_task —— 任务页内容:_panel_task(ScrollRect,任务列表)+ _box_main_line(主线引导条槽位)
    ///     _box_team —— 组队页"已有队伍"内容:_list_team(ScrollRect,克隆 _tpl_TeamMainRoleItem)
    ///     _box_non_team —— 组队页"无队伍"内容:创建队伍/查找队伍两个按钮
    ///   _img_arrow(+_img_arrow_icon) —— 面板左上角收起箭头(点击见 ToggleTaskPanel)
    /// 三个内容盒(_box_task/_box_team/_box_non_team)由业务代码 SetActive 互斥切换,对标 Login 的
    /// "两个子面板 SetActive 切换"同一模式;默认只显 _box_task(对齐 MainUITaskTeamView.OnInit)。
    ///
    /// root="HudTaskTeam" 收成面板实际占位:AnchorBottomLeft 锚左下(对标老端 view 在主界面里的位置
    /// x=10,y=586,255x314 —— 底边距 = 1280-(586+314) = 380),不再全屏;子根 "MainUITaskTeamView"
    /// 挂业务类,Stretch 填满 root。挪整块面板直接拖 root(所见即所得)。
    /// 任务列表排版归 prefab:TaskScrollView 的 Content 挂 VerticalLayoutGroup(间距 2)
    /// + ContentSizeFitter;任务条高度和主线条占位由运行时按真实文本行数同步,对标老端
    /// MainUITaskItem.SetTipsMsg / MainUITaskTeamView.SetTaskConSize。
    ///
    /// 两个模板(未激活模板子节点,回填各自 Bind 字段):
    ///   _tpl_MainUITaskItem —— 对标 resource/game/mainUI/MainUITaskItem.json(210x54),挂已写好的业务类
    ///     MainUITaskItem(MainUITaskTeamView.GetOrCreateItem / EnsureMainLineItem 都用它 Instantiate 克隆)。
    ///   _tpl_TeamMainRoleItem —— 对标 resource/game/team/TeamMainRoleItem.json(team 模块,222.74x78.24;
    ///     _list_team 的 itemRender 在老端 TS 里 import 自 "./TeamMainRoleItem",不是 mainUI 模块自己的节点,
    ///     旧镜像 prefab 里这个字段一直是 fileID: 0 从未绑过)。自动循环 轮8 起挂已写好的业务类
    ///     Shenxiao.Module.Core.Team.TeamMainRoleItem(照 MainUITaskItem 先例,直接在 Creator 里
    ///     AddComponent 业务子类,不走运行时嫁接/回填升级器),MainUITaskTeamView.RefreshTeamPanel
    ///     用它 Instantiate 克隆喂 TeamModel 真实成员。
    ///
    /// 组队相关另外两个老端场景 MainUITeamView.json(720x229,底部悬浮"小队"按钮部件)与
    /// MainUITeamHeadItem.json(83x93,该部件内的头像子项)经核对与本视图的 42 个 Bind 字段【无任何引用
    /// 关系】(字段里没有 _con_widget/teamBtn/scroll/role_head 这类对应名),是主界面另一个独立浮标部件,
    /// 不属于 MainUITaskTeamView 的组队子面板——按 prompt 的条件跳过,不并入本 prefab。
    ///
    /// 元素已尽量贴老端真实源图(TrySetSprite),贴不到回退占位色;sizeGrid 九宫图仅设 Image.Type=Sliced,
    /// 具体边框像素留给美术/自行在编辑器上调(不改 Sprite 资产)。尺寸/位置为起步值,自行手调。
    /// 入口在「神霄/重构UI 生成器」面板。
    /// </summary>
    // 命名对照(Laya风格 -> 语义化英文):
    //   _box_con                       -> MainBody
    //   _box_task_tab                  -> TaskTab
    //   _img_task_bg                   -> TaskTabBackground
    //   _lb_task_desc                  -> TaskTabLabel
    //   _lb_task_desc_en               -> TaskTabLabelEn
    //   _box_team_tab                  -> TeamTab
    //   _img_team_bg                   -> TeamTabBackground
    //   _lb_team_desc                  -> TeamTabLabel
    //   _lb_team_desc_en               -> TeamTabLabelEn
    //   _img_team_red                  -> TeamRedDot
    //   _img_team_role_count_bg        -> TeamMemberCountBg
    //   _lb_team_role_count            -> TeamMemberCountLabel
    //   _box_temple_awaken             -> TempleAwakenBar
    //   _img_temple_awaken_bg          -> TempleAwakenBackground
    //   _box_unopen_awaken             -> AwakenLockedState
    //   _img_unopen_awaken_icon        -> AwakenLockedIcon
    //   _img_awaken                    -> AwakenTitleGraphic
    //   _html_open_lv_bg               -> AwakenUnlockLevelBg
    //   _html_open_lv                  -> AwakenUnlockLevelLabel
    //   _box_open_awaken               -> AwakenUnlockedState
    //   _img_goods_icon                -> AwakenItemIcon
    //   _lb_open_awaken_progress       -> AwakenProgressLabel
    //   _lb_open_awaken_task_desc_bg   -> AwakenTaskDescBg
    //   _lb_open_awaken_task_desc      -> AwakenTaskDescLabel
    //   _img_goods_name                -> AwakenItemNameImage
    //   _img_awaken_red                -> AwakenRedDot
    //   _box_awaken_effect             -> AwakenEffectSlot
    //   _box_task                      -> TaskPage
    //   _panel_task                    -> TaskScrollView
    //   _box_main_line                 -> MainLineTaskSlot
    //   _box_team                      -> TeamPage
    //   _list_team                     -> TeamMemberList
    //   _box_non_team                  -> NoTeamPage
    //   _img_not_team_bg               -> NoTeamBackground
    //   _img_create_team               -> CreateTeamButton
    //   _lb_create_team                -> CreateTeamLabel
    //   _img_search_team               -> SearchTeamButton
    //   _lb_search_team                -> SearchTeamLabel
    //   _img_arrow                     -> CollapseArrowButton
    //   _img_arrow_icon                -> CollapseArrowIcon
    //   —— MainUITaskItem 模板(根节点名按类名保持不变)——
    //   _img_bg                        -> TaskItemBackground
    //   _img_done                      -> TaskDoneMark
    //   _img_select                    -> TaskSelectedHighlight
    //   lblTaskTitle                   -> TaskTitleLabel
    //   lblTaskTitle2                  -> TaskSubTitleLabel
    //   _box_finger_con                -> GuideFingerSlot
    //   _box_effect                    -> CompletionEffectSlot
    //   (新增,无老端对应节点)          -> TaskTipsLabel(任务描述,老端/旧实现为运行时动态造节点)
    //   —— TeamMainRoleItem 模板(根节点名按类名保持不变)——
    //   _Image1                        -> TeamItemBackground
    //   non_role                       -> InviteSlot
    //   Image                          -> InviteIcon
    //   lbl_tips                       -> InviteTipsLabel
    //   red_point                      -> TeamApplyRedDot
    //   role_group                     -> RoleInfoGroup
    //   online_img                     -> OnlineStateIcon
    //   role_head                      -> RoleHeadSlot
    //   leader_tag                     -> LeaderTag
    //   head_click_group               -> HeadClickArea
    //   role_name                      -> RoleNameLabel
    //   _Group1                        -> RoleExtraSlot(老端 TS/JSON 里从未被实际引用,用途不明,按位置起名)
    //   destiny_img                    -> AscendMarkIcon
    //   level                          -> LevelLabel
    //   quitBtn                        -> QuitTeamButton
    //   _Image2                        -> QuitTeamButtonBg
    //   _Label1                        -> QuitTeamButtonLabel
    //   pleaseLeaveBtn                 -> KickMemberButton
    //   _Image3                        -> KickMemberButtonBg
    //   _Label2                        -> KickMemberButtonLabel
    public static class HudTaskTeamCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/MainUI/Regions/HudTaskTeam.prefab";

        // ---------------------------------------------------------------- 老端源图(均已确认在 Assets/GameRes 下)
        private const string IMG_TAB_BG_NORMAL = "resource/game/mainUI/texture/mainui_taskbtn_normal.png";
        private const string IMG_TAB_BG_LIGHT = "resource/game/mainUI/texture/mainui_taskbtn_light.png";
        private const string IMG_RED_POINT_COMMON = "resource/game/common/texture/com_red_point.png"; // _img_team_red / _img_awaken_red
        private const string IMG_TEAM_MEM_NUM_BG = "resource/game/mainUI/texture/mainui_team_mem_num_bg.png";
        private const string IMG_TEMPLE_AWAKEN_BG = "resource/game/mainUI/texture/mainui_adventure_item_bg.png";
        private const string IMG_UNOPEN_AWAKEN_ICON = "resource/game/mainUI/texture/uijxzlzjm_003.png";
        private const string IMG_AWAKEN = "resource/game/mainUI/texture/jxzl.png";
        private const string IMG_OPEN_LV_BG = "resource/game/mainUI/texture/uijxzlzjm_001.png"; // html_open_lv_bg / lb_open_awaken_task_desc_bg 共用
        private const string IMG_COMMON_EMPTY = "resource/game/common/texture/com_empty.png";   // 老端"空图"占位(goods_name/online_img 运行时换真图前用它)
        private const string IMG_TASK_PANEL_BG = "resource/game/mainUI/texture/task_bg.png";    // sizeGrid 37,87,43,68
        private const string IMG_CREATE_TEAM_BTN = "resource/game/mainUI/texture/uirwl_008a.png";
        private const string IMG_SEARCH_TEAM_BTN = "resource/game/mainUI/texture/uirwl_008b.png";
        private const string IMG_ARROW = "resource/game/mainUI/texture/mainui_btn_task_back.png";
        private const string IMG_ARROW_ICON = "resource/game/mainUI/texture/UIrwl_002.png";

        // MainUITaskItem 模板
        private const string IMG_TASKITEM_BG = "resource/game/mainUI/texture/monster_bg.png";       // sizeGrid 10,5,10,5
        private const string IMG_TASKITEM_DONE = "resource/game/mainUI/texture/UIrwl_006.png";
        private const string IMG_TASKITEM_SELECT = "resource/game/mainUI/texture/mainui_ui_select.png"; // sizeGrid 15,15,15,15

        // TeamMainRoleItem 模板(team 模块,注意部分图和 mainUI 模块同名但目录不同)
        private const string IMG_TEAMITEM_BG = "resource/game/mainUI/texture/monster_bg.png";        // sizeGrid 5,5,5,5(同一张图,不同九宫切法)
        private const string IMG_TEAMITEM_INVITE = "resource/game/mainUI/texture/uirwl_015.png";
        private const string IMG_TEAMITEM_RED_POINT = "resource/game/mainUI/texture/com_red_point.png"; // 注意:mainUI 目录下这份,和上面 common 目录那份是两张不同资产
        private const string IMG_TEAM_LEADER_TAG = "resource/game/team/texture/ui_dw_zdyq003.png";
        private const string IMG_ASCEND_MARK = "resource/game/mainUI/texture/uisc_006.png";
        private const string IMG_COMMON_BTN_BG = "resource/game/common/texture/uian_0120b.png"; // 退队/请离 按钮底图

        // 面板整体锚点(对标老端 view 在主界面里的位置 x=10,y=586,255x314)
        private const float RegionLeft = 10f;
        private const float RegionBottom = 380f; // 1280 - (586 + 314)
        private const float RegionW = 255f;
        private const float RegionH = 314f;

        private static GameObject _previewInstance;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudTaskTeam(任务·组队)",
                Note = "左下有界面板(10/380,255×314):任务/队伍 tab + 任务列表(布局组排版) + 组队入口,代码零布局",
                Order = 30,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建(与 Login/HudTop/HudActivity 一致的安全写法),建完再统一激活。
            // root 收成面板实际占位(左下 10/380,255×314),不再全屏 Stretch;并入总装时非全屏锚 → offset
            // 归零自动跳过,想挪整块面板直接在 prefab 里拖 root(所见即所得)。
            RectTransform root = UiCreatorKit.NewRoot("HudTaskTeam");
            AnchorBottomLeft(root, RegionLeft, RegionBottom, RegionW, RegionH);
            root.gameObject.SetActive(false);

            RectTransform viewRoot = UiCreatorKit.NewNode("MainUITaskTeamView", root);
            UiCreatorKit.Stretch(viewRoot); // 填满有界 root
            var view = viewRoot.gameObject.AddComponent<MainUITaskTeamView>();

            RectTransform boxCon = UiCreatorKit.NewNode("MainBody", viewRoot); // 老端: _box_con
            PlaceLaya(boxCon, 0f, 0f, 224f, 314f, RegionW, RegionH);
            view._box_con = boxCon;

            // 两个列表模板(MainUITaskItem/TeamMainRoleItem)统一挂在本 view 根下的 __Templates 里,
            // 不再混进各自 ScrollRect 的 Content(那里应该只有真实列表项)。
            RectTransform templatesRoot = NewTemplatesWrapper(viewRoot);

            BuildTaskTeamTabs(boxCon, view);
            BuildTempleAwaken(boxCon, view);
            BuildTaskBox(boxCon, view, templatesRoot);
            BuildTeamBox(boxCon, view, templatesRoot);
            BuildNonTeamBox(boxCon, view);
            BuildArrow(viewRoot, view);

            // 默认显任务页(对标 MainUITaskTeamView.OnInit:_box_task=true,_box_team=false,_box_non_team=false)。
            // 觉醒条默认隐藏，运行时收到 42901 后按真实等级/章节/阶段配置决定是否显示。
            view._box_task.gameObject.SetActive(true);
            view._box_team.gameObject.SetActive(false);
            view._box_non_team.gameObject.SetActive(false);
            view._box_temple_awaken.gameObject.SetActive(false);

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] HudTaskTeam.prefab 已生成: " + PrefabPath +
                      "(任务·组队面板;人工核对后再并入 MainUIModule.prefab)");
        }

        // ---------------------------------------------------------------- 任务/组队 两个 tab

        private static void BuildTaskTeamTabs(Transform boxCon, MainUITaskTeamView view)
        {
            const float conW = 224f, conH = 314f;

            // _box_task_tab: x=0,y=35,w=35,h=143
            RectTransform taskTab = UiCreatorKit.NewNode("TaskTab", boxCon); // 老端: _box_task_tab
            PlaceLaya(taskTab, 0f, 35f, 35f, 143f, conW, conH);
            view._box_task_tab = taskTab;
            const float taskTabW = 35f, taskTabH = 143f;

            // 起步贴运行时初始选中态(OnInit 会调 ApplyTaskTabState(true):任务=light,组队=normal)。
            Image taskBg = UiCreatorKit.NewImage("TaskTabBackground", taskTab); // 老端: _img_task_bg
            PlaceLaya(taskBg.rectTransform, 0f, 0f, 35f, 135f, taskTabW, taskTabH);
            taskBg.raycastTarget = false; // 点击绑在 _box_task_tab 容器上(UIUtil 会自动补透明命中图)
            UiCreatorKit.TrySetSprite(taskBg, IMG_TAB_BG_LIGHT, UiCreatorKit.Palette.BtnPrimary);
            view._img_task_bg = taskBg;

            // 老端运行时不是直接使用 30x100 / 18px:生成代码会把字号放大到 36 后将整个
            // Label 缩放为 0.5，最终可见 bounds 为 local(10,47) 15x50。Unity 直接照设计尺寸
            // 会让文字盒大一倍，页签看起来偏位、留白和内容缩进都不对；这里烤最终运行时几何。
            TextMeshProUGUI taskDesc = UiCreatorKit.NewText("TaskTabLabel", taskTab, "任\n务"); // 老端: _lb_task_desc
            PlaceLaya(taskDesc.rectTransform, 10f, 47f, 15f, 50f, taskTabW, taskTabH);
            taskDesc.fontSize = 18f;
            taskDesc.fontStyle = FontStyles.Bold;
            taskDesc.alignment = TextAlignmentOptions.Center;
            taskDesc.textWrappingMode = TextWrappingModes.NoWrap;
            taskDesc.color = ParseColor("#FFF7D6", Color.white);
            view._lb_task_desc = taskDesc;

            TextMeshProUGUI taskDescEn = UiCreatorKit.NewText("TaskTabLabelEn", taskTab, "Quests"); // 老端: _lb_task_desc_en
            PlaceLaya(taskDescEn.rectTransform, 0f, 0f, 96f, 34f, taskTabW, taskTabH);
            taskDescEn.fontSize = 18f;
            taskDescEn.gameObject.SetActive(false); // 设计默认隐藏 + OnInit 也强制隐藏
            view._lb_task_desc_en = taskDescEn;

            // _box_team_tab: x=2,y=171,w=35,h=135
            RectTransform teamTab = UiCreatorKit.NewNode("TeamTab", boxCon); // 老端: _box_team_tab
            PlaceLaya(teamTab, 2f, 171f, 35f, 135f, conW, conH);
            view._box_team_tab = teamTab;
            const float teamTabW = 35f, teamTabH = 135f;

            Image teamBg = UiCreatorKit.NewImage("TeamTabBackground", teamTab); // 老端: _img_team_bg
            PlaceLaya(teamBg.rectTransform, 0f, 0f, 35f, 143f, teamTabW, teamTabH);
            teamBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(teamBg, IMG_TAB_BG_NORMAL, UiCreatorKit.Palette.BtnNeutral);
            view._img_team_bg = teamBg;

            // 老端 LoadSuccess 最终文案为“组队”，同样是 36px + 0.5 整体缩放；运行时快照
            // bounds 为 local(9,32) 17.5x71.5。显式换行，避免 TMP 字体度量变化后又排回横向。
            TextMeshProUGUI teamDesc = UiCreatorKit.NewText("TeamTabLabel", teamTab, "组\n队"); // 老端: _lb_team_desc
            PlaceLaya(teamDesc.rectTransform, 9f, 32f, 17.5f, 71.5f, teamTabW, teamTabH);
            teamDesc.fontSize = 18f;
            teamDesc.fontStyle = FontStyles.Bold;
            teamDesc.alignment = TextAlignmentOptions.Center;
            teamDesc.textWrappingMode = TextWrappingModes.NoWrap;
            teamDesc.color = ParseColor("#6CFFD3", Color.cyan);
            view._lb_team_desc = teamDesc;

            TextMeshProUGUI teamDescEn = UiCreatorKit.NewText("TeamTabLabelEn", teamTab, "Team"); // 老端: _lb_team_desc_en
            PlaceLaya(teamDescEn.rectTransform, 0f, 0f, 96f, 34f, teamTabW, teamTabH);
            teamDescEn.fontSize = 18f;
            teamDescEn.gameObject.SetActive(false);
            view._lb_team_desc_en = teamDescEn;

            // 运行时快照实测尺寸 24x24(设计源没写 width/height,Laya 按贴图原始尺寸自适应)
            Image teamRed = UiCreatorKit.NewImage("TeamRedDot", teamTab); // 老端: _img_team_red
            PlaceLaya(teamRed.rectTransform, 70f, -10f, 24f, 24f, teamTabW, teamTabH);
            teamRed.raycastTarget = false;
            UiCreatorKit.TrySetSprite(teamRed, IMG_RED_POINT_COMMON, UiCreatorKit.Palette.Mark);
            teamRed.gameObject.SetActive(false);
            view._img_team_red = teamRed;

            Image teamCountBg = UiCreatorKit.NewImage("TeamMemberCountBg", teamTab); // 老端: _img_team_role_count_bg
            PlaceLaya(teamCountBg.rectTransform, 17.5f, -10f, 34f, 34f, teamTabW, teamTabH);
            teamCountBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(teamCountBg, IMG_TEAM_MEM_NUM_BG, UiCreatorKit.Palette.Panel);
            teamCountBg.gameObject.SetActive(false);
            view._img_team_role_count_bg = teamCountBg;

            TextMeshProUGUI teamRoleCount = UiCreatorKit.NewText("TeamMemberCountLabel", teamCountBg.transform, "1"); // 老端: _lb_team_role_count
            // 老端同样经过 0.5 整体缩放，烤最终可见 bounds。
            PlaceLaya(teamRoleCount.rectTransform, 12f, 9f, 8.5f, 18f, 34f, 34f);
            teamRoleCount.fontSize = 18f;
            view._lb_team_role_count = teamRoleCount;
        }

        // ---------------------------------------------------------------- 神殿觉醒条(结构完整，运行时数据驱动)

        private static void BuildTempleAwaken(Transform boxCon, MainUITaskTeamView view)
        {
            const float conW = 224f, conH = 314f;

            // Laya: y=39, right=-27 → x = 224 - 210 - (-27) = 41
            RectTransform templeAwaken = UiCreatorKit.NewNode("TempleAwakenBar", boxCon); // 老端: _box_temple_awaken
            PlaceLaya(templeAwaken, 41f, 39f, 210f, 59f, conW, conH);
            view._box_temple_awaken = templeAwaken;
            const float w = 210f, h = 59f;

            Image bg = UiCreatorKit.NewImage("TempleAwakenBackground", templeAwaken); // 老端: _img_temple_awaken_bg
            PlaceLaya(bg.rectTransform, 0f, 0f, 210f, 59f, w, h);
            bg.raycastTarget = false; // 点击绑在 _box_temple_awaken 容器上
            UiCreatorKit.TrySetSprite(bg, IMG_TEMPLE_AWAKEN_BG, UiCreatorKit.Palette.Panel);
            view._img_temple_awaken_bg = bg;

            // ---- AwakenLockedState(老端: _box_unopen_awaken,未开启态,设计默认隐藏)----
            RectTransform unopen = UiCreatorKit.NewNode("AwakenLockedState", templeAwaken); // 老端: _box_unopen_awaken
            PlaceLaya(unopen, 0f, 0f, 191f, 59f, w, h);
            unopen.gameObject.SetActive(false);
            view._box_unopen_awaken = unopen;
            const float unopenW = 191f, unopenH = 59f;

            Image unopenIcon = UiCreatorKit.NewImage("AwakenLockedIcon", unopen); // 老端: _img_unopen_awaken_icon
            PlaceLaya(unopenIcon.rectTransform, 0f, 7f, 60f, 45f, unopenW, unopenH);
            unopenIcon.raycastTarget = false;
            UiCreatorKit.TrySetSprite(unopenIcon, IMG_UNOPEN_AWAKEN_ICON, UiCreatorKit.Palette.Panel);
            view._img_unopen_awaken_icon = unopenIcon;

            // Laya centerX=44(相对父中心横向偏移),y=0(顶对齐)
            Image awaken = UiCreatorKit.NewImage("AwakenTitleGraphic", unopen); // 老端: _img_awaken
            UiCreatorKit.Place(awaken.rectTransform, 44f, 15f, 89f, 29f);
            awaken.raycastTarget = false;
            UiCreatorKit.TrySetSprite(awaken, IMG_AWAKEN, UiCreatorKit.Palette.BtnPrimary);
            view._img_awaken = awaken;

            // 运行时快照实测尺寸 131x24(设计源没写 width/height)
            Image openLvBg = UiCreatorKit.NewImage("AwakenUnlockLevelBg", unopen); // 老端: _html_open_lv_bg
            PlaceLaya(openLvBg.rectTransform, 68f, 34f, 131f, 24f, unopenW, unopenH);
            openLvBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(openLvBg, IMG_OPEN_LV_BG, UiCreatorKit.Palette.Panel);
            view._html_open_lv_bg = openLvBg;

            TextMeshProUGUI openLv = UiCreatorKit.NewText("AwakenUnlockLevelLabel", unopen, "999级开启"); // 老端: _html_open_lv
            PlaceLaya(openLv.rectTransform, 38f, 37f, 180f, 20f, unopenW, unopenH);
            openLv.fontSize = 18f;
            view._html_open_lv = openLv;

            // ---- AwakenUnlockedState(老端: _box_open_awaken,已开启态,设计默认显示)----
            RectTransform openAwaken = UiCreatorKit.NewNode("AwakenUnlockedState", templeAwaken); // 老端: _box_open_awaken
            PlaceLaya(openAwaken, 0f, 0f, 191f, 60f, w, h);
            view._box_open_awaken = openAwaken;
            const float openW = 191f, openH = 60f;

            // 无设计态贴图:该图标运行时按当前"侍魂"道具动态换图(_img_goods_icon 在源 JSON 里没有 skin),
            // 只留占位色,不瞎贴一张具体道具图误导。
            Image goodsIcon = UiCreatorKit.NewImage("AwakenItemIcon", openAwaken); // 老端: _img_goods_icon
            PlaceLaya(goodsIcon.rectTransform, 5f, 2f, 55f, 55f, openW, openH);
            goodsIcon.raycastTarget = false;
            goodsIcon.color = UiCreatorKit.Palette.Panel;
            view._img_goods_icon = goodsIcon;

            // Laya anchorX=1(x=205.82 是文本框右边界,不是左边界)+ scaleX/Y=0.75
            TextMeshProUGUI progress = UiCreatorKit.NewText("AwakenProgressLabel", openAwaken, "10/35"); // 老端: _lb_open_awaken_progress
            PlaceLaya(progress.rectTransform, 205.82f, 8f, 200f, 23f, openW, openH, 1f, 0f);
            progress.rectTransform.localScale = new Vector3(0.75f, 0.75f, 1f);
            progress.fontSize = 15f;
            progress.alignment = TextAlignmentOptions.Right;
            progress.color = Color.black;
            view._lb_open_awaken_progress = progress;

            Image openDescBg = UiCreatorKit.NewImage("AwakenTaskDescBg", openAwaken); // 老端: _lb_open_awaken_task_desc_bg
            PlaceLaya(openDescBg.rectTransform, 68f, 34f, 131f, 24f, openW, openH);
            openDescBg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(openDescBg, IMG_OPEN_LV_BG, UiCreatorKit.Palette.Panel);
            view._lb_open_awaken_task_desc_bg = openDescBg;

            TextMeshProUGUI openDesc = UiCreatorKit.NewText("AwakenTaskDescLabel", openAwaken, "侍魂提升"); // 老端: _lb_open_awaken_task_desc
            PlaceLaya(openDesc.rectTransform, 57f, 31f, 148.82f, 30f, openW, openH);
            openDesc.fontSize = 18f;
            view._lb_open_awaken_task_desc = openDesc;

            Image goodsName = UiCreatorKit.NewImage("AwakenItemNameImage", openAwaken); // 老端: _img_goods_name
            PlaceLaya(goodsName.rectTransform, 60f, 2f, 84f, 24f, openW, openH);
            goodsName.raycastTarget = false;
            UiCreatorKit.TrySetSprite(goodsName, IMG_COMMON_EMPTY, UiCreatorKit.Palette.Panel);
            view._img_goods_name = goodsName;

            // 运行时快照实测尺寸 24x24
            Image awakenRed = UiCreatorKit.NewImage("AwakenRedDot", openAwaken); // 老端: _img_awaken_red
            PlaceLaya(awakenRed.rectTransform, 179.82f, -16f, 24f, 24f, openW, openH);
            awakenRed.raycastTarget = false;
            UiCreatorKit.TrySetSprite(awakenRed, IMG_RED_POINT_COMMON, UiCreatorKit.Palette.Mark);
            awakenRed.gameObject.SetActive(false);
            view._img_awaken_red = awakenRed;

            // ---- AwakenEffectSlot(老端: _box_awaken_effect,特效挂点,纯布局盒)----
            RectTransform effect = UiCreatorKit.NewNode("AwakenEffectSlot", templeAwaken); // 老端: _box_awaken_effect
            PlaceLaya(effect, 0f, 0f, 193f, 60f, w, h);
            view._box_awaken_effect = effect;
        }

        // ---------------------------------------------------------------- 任务页内容

        private static void BuildTaskBox(Transform boxCon, MainUITaskTeamView view, Transform templatesRoot)
        {
            const float conW = 224f, conH = 314f;

            // _box_task: x=41,y=98,w=210,h=214
            RectTransform boxTask = UiCreatorKit.NewNode("TaskPage", boxCon); // 老端: _box_task
            PlaceLaya(boxTask, 41f, 98f, 210f, 214f, conW, conH);
            view._box_task = boxTask;
            const float w = 210f, h = 214f;

            // _panel_task(Laya Panel→ScrollRect):主线单行高 55 时 x=0,y=57,w=210,h=157。
            // 运行时若主线隐藏则恢复 y=0,h=214;主线多行时继续按其真实高度动态挤压。
            ScrollRect panelTask = NewScrollRect("TaskScrollView", boxTask, 157f, out RectTransform panelTaskContent); // 老端: _panel_task
            PlaceLaya((RectTransform)panelTask.transform, 0f, 57f, 210f, 157f, w, h);
            view._panel_task = panelTask;

            // 老端 UpdateItemSize 是「每项真实高度 + 2px」,并不是固定 78px 步进。
            // 每项高度由 MainUITaskItem 按描述文本行数改 RectTransform,这里仅负责 2px 间距和内容撑高。
            var taskLayout = panelTaskContent.gameObject.AddComponent<VerticalLayoutGroup>();
            taskLayout.spacing = 2f;
            taskLayout.childAlignment = TextAnchor.UpperLeft;
            taskLayout.childControlWidth = false;
            taskLayout.childControlHeight = false;
            taskLayout.childForceExpandWidth = false;
            taskLayout.childForceExpandHeight = false;
            var taskFitter = panelTaskContent.gameObject.AddComponent<ContentSizeFitter>();
            taskFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 未激活模板,挂已写好的业务类 MainUITaskItem;放进本 view 的 __Templates 下(不再混进
            // ScrollRect 的 Content——那里应该只有 MainUITaskTeamView.RefreshTaskItems 克隆出的真实列表项)。
            GameObject tplTaskItem = BuildTaskItemTemplate(templatesRoot);
            view._tpl_MainUITaskItem = tplTaskItem;

            // _box_main_line: x=0,y=0,w=210,h=1(主线引导条槽位,运行时按需克隆 _tpl_MainUITaskItem 进来)
            RectTransform mainLine = UiCreatorKit.NewNode("MainLineTaskSlot", boxTask); // 老端: _box_main_line
            PlaceLaya(mainLine, 0f, 0f, 210f, 1f, w, h);
            view._box_main_line = mainLine;
        }

        // ---------------------------------------------------------------- 组队页内容(已有队伍)

        private static void BuildTeamBox(Transform boxCon, MainUITaskTeamView view, Transform templatesRoot)
        {
            const float conW = 224f, conH = 314f;

            // _box_team: x=40,y=98,w=210,h=214(设计默认 visible=false)
            RectTransform boxTeam = UiCreatorKit.NewNode("TeamPage", boxCon); // 老端: _box_team
            PlaceLaya(boxTeam, 40f, 98f, 210f, 214f, conW, conH);
            view._box_team = boxTeam;
            const float w = 210f, h = 214f;

            // _list_team(Laya List→ScrollRect): x=0,y=0,w=210,h=214
            ScrollRect listTeam = NewScrollRect("TeamMemberList", boxTeam, 214f, out RectTransform listTeamContent); // 老端: _list_team
            PlaceLaya((RectTransform)listTeam.transform, 0f, 0f, 210f, 214f, w, h);
            view._list_team = listTeam;

            // _tpl_TeamMainRoleItem:业务类 TeamMainRoleItem 尚未移植(见类注释),先挂生成器已产出的
            // TeamMainRoleItemBind 把结构烤出来、回填全部字段;挂在本 view 的 __Templates 下,不再混进
            // ScrollRect 的 Content。
            GameObject tplTeamItem = BuildTeamMainRoleItemTemplate(templatesRoot);
            view._tpl_TeamMainRoleItem = tplTeamItem;
        }

        // ---------------------------------------------------------------- 组队页内容(无队伍)

        private static void BuildNonTeamBox(Transform boxCon, MainUITaskTeamView view)
        {
            const float conW = 224f, conH = 314f;

            // _box_non_team: x=39,y=98,w=210,h=214
            RectTransform boxNonTeam = UiCreatorKit.NewNode("NoTeamPage", boxCon); // 老端: _box_non_team
            PlaceLaya(boxNonTeam, 39f, 98f, 210f, 214f, conW, conH);
            view._box_non_team = boxNonTeam;
            const float w = 210f, h = 214f;

            Image notTeamBg = UiCreatorKit.NewImage("NoTeamBackground", boxNonTeam); // 老端: _img_not_team_bg
            PlaceLaya(notTeamBg.rectTransform, 0f, 0f, 210f, 214f, w, h);
            notTeamBg.raycastTarget = false;
            notTeamBg.type = Image.Type.Sliced; // sizeGrid 37,87,43,68
            UiCreatorKit.TrySetSprite(notTeamBg, IMG_TASK_PANEL_BG, UiCreatorKit.Palette.Panel);
            view._img_not_team_bg = notTeamBg;

            Image createTeam = UiCreatorKit.NewImage("CreateTeamButton", boxNonTeam); // 老端: _img_create_team
            PlaceLaya(createTeam.rectTransform, 19f, 10f, 171f, 60f, w, h);
            createTeam.raycastTarget = true; // BindClick(_img_create_team, ...) 直接命中体
            UiCreatorKit.TrySetSprite(createTeam, IMG_CREATE_TEAM_BTN, UiCreatorKit.Palette.BtnPrimary);
            view._img_create_team = createTeam;

            // 运行时快照实测尺寸 160x40(设计源没写 width/height)
            TextMeshProUGUI createLabel = UiCreatorKit.NewText("CreateTeamLabel", createTeam.transform, "创建队伍"); // 老端: _lb_create_team
            // Laya 生成代码将 40px 文本整体缩放 0.5，最终为 80x20 / 20px。
            PlaceLaya(createLabel.rectTransform, 56f, 20f, 80f, 20f, 171f, 60f);
            createLabel.fontSize = 20f;
            createLabel.fontStyle = FontStyles.Italic;
            view._lb_create_team = createLabel;

            Image searchTeam = UiCreatorKit.NewImage("SearchTeamButton", boxNonTeam); // 老端: _img_search_team
            PlaceLaya(searchTeam.rectTransform, 19f, 107f, 171f, 60f, w, h);
            searchTeam.raycastTarget = true; // BindClick(_img_search_team, ...) 直接命中体
            UiCreatorKit.TrySetSprite(searchTeam, IMG_SEARCH_TEAM_BTN, UiCreatorKit.Palette.BtnSecond);
            view._img_search_team = searchTeam;

            TextMeshProUGUI searchLabel = UiCreatorKit.NewText("SearchTeamLabel", searchTeam.transform, "查找队伍"); // 老端: _lb_search_team
            PlaceLaya(searchLabel.rectTransform, 56f, 20f, 80f, 20f, 171f, 60f);
            searchLabel.fontSize = 20f;
            searchLabel.fontStyle = FontStyles.Italic;
            view._lb_search_team = searchLabel;
        }

        // ---------------------------------------------------------------- 收起箭头(面板左上角)

        private static void BuildArrow(Transform viewRoot, MainUITaskTeamView view)
        {
            // _img_arrow: x=0,y=0,w=32,h=34(相对 MainUITaskTeamView 自身 255x314,和 _box_con 同级)
            Image arrow = UiCreatorKit.NewImage("CollapseArrowButton", viewRoot); // 老端: _img_arrow
            PlaceLaya(arrow.rectTransform, 0f, 0f, 32f, 34f, RegionW, RegionH);
            arrow.raycastTarget = true; // BindClick(_img_arrow, ToggleTaskPanel) 直接命中体
            UiCreatorKit.TrySetSprite(arrow, IMG_ARROW, UiCreatorKit.Palette.BtnNeutral);
            view._img_arrow = arrow;

            Image arrowIcon = UiCreatorKit.NewImage("CollapseArrowIcon", arrow.transform); // 老端: _img_arrow_icon
            UiCreatorKit.Place(arrowIcon.rectTransform, 0f, 0f, 27f, 33f); // centerX=0/centerY=0
            arrowIcon.raycastTarget = false;
            UiCreatorKit.TrySetSprite(arrowIcon, IMG_ARROW_ICON, UiCreatorKit.Palette.BtnNeutral);
            view._img_arrow_icon = arrowIcon;
        }

        // ---------------------------------------------------------------- MainUITaskItem 模板(挂业务类)

        /// <summary>
        /// 对标 resource/game/mainUI/MainUITaskItem.json,画布 210x54。未激活模板,挂已写好的业务类
        /// MainUITaskItem 并回填其 7 个 Bind 字段。MainUITaskTeamView.GetOrCreateItem / EnsureMainLineItem
        /// 都用 Instantiate(_tpl_MainUITaskItem, parent) 克隆它。
        /// </summary>
        private static GameObject BuildTaskItemTemplate(Transform parent)
        {
            const float w = 210f, h = 54f;
            RectTransform root = UiCreatorKit.NewNode("MainUITaskItem", parent);
            UiCreatorKit.Place(root, 0f, 0f, w, h);
            var item = root.gameObject.AddComponent<MainUITaskItem>();

            Image bg = UiCreatorKit.NewImage("TaskItemBackground", root); // 老端: _img_bg
            PlaceLaya(bg.rectTransform, 0f, 0f, 210f, 54f, w, h);
            bg.raycastTarget = true; // MainUITaskItem.OnInit: UIUtil.AddClick(_img_bg, OnClick)
            bg.type = Image.Type.Sliced; // sizeGrid 10,5,10,5
            UiCreatorKit.TrySetSprite(bg, IMG_TASKITEM_BG, UiCreatorKit.Palette.Panel);
            item._img_bg = bg;

            // 运行时快照实测尺寸 21x20(设计源没写 width/height)
            Image done = UiCreatorKit.NewImage("TaskDoneMark", root); // 老端: _img_done
            AnchorTopLeft(done.rectTransform, 186f, 0f, 21f, 20f);
            done.raycastTarget = false;
            UiCreatorKit.TrySetSprite(done, IMG_TASKITEM_DONE, UiCreatorKit.Palette.Mark);
            done.gameObject.SetActive(false); // OnInit 强制隐藏
            item._img_done = done;

            Image select = UiCreatorKit.NewImage("TaskSelectedHighlight", root); // 老端: _img_select
            PlaceLaya(select.rectTransform, 0f, 0f, 210f, 54f, w, h);
            select.raycastTarget = false;
            select.type = Image.Type.Sliced; // sizeGrid 15,15,15,15
            UiCreatorKit.TrySetSprite(select, IMG_TASKITEM_SELECT, UiCreatorKit.Palette.BtnSecond);
            select.gameObject.SetActive(false); // OnInit 强制隐藏
            item._img_select = select;

            // 运行时快照实测高度 16(设计源没写 height)
            TextMeshProUGUI title = UiCreatorKit.NewText("TaskTitleLabel", root, "[主] 任务标题"); // 老端: lblTaskTitle
            AnchorTopLeft(title.rectTransform, 6f, 6f, 180f, 16f);
            title.fontSize = 16f;
            title.alignment = TextAlignmentOptions.Left;
            title.color = ParseColor("#54d5ff", Color.cyan);
            item.lblTaskTitle = title;

            TextMeshProUGUI title2 = UiCreatorKit.NewText("TaskSubTitleLabel", root, ""); // 老端: lblTaskTitle2
            AnchorTopLeft(title2.rectTransform, 6f, 6f, 180f, 16f);
            title2.fontSize = 16f;
            title2.alignment = TextAlignmentOptions.Left;
            title2.color = ParseColor("#54d5ff", Color.cyan);
            item.lblTaskTitle2 = title2;

            // 任务描述 tips:老端从 (6,32) 起按 215px 行宽换行;模板烤静态起点/字号,
            // 运行时按真实行数把文本框和任务条从单行 55px 向下增高。
            TextMeshProUGUI taskTips = UiCreatorKit.NewText("TaskTipsLabel", root, "任务描述内容");
            taskTips.rectTransform.anchorMin = taskTips.rectTransform.anchorMax = new Vector2(0f, 1f);
            taskTips.rectTransform.pivot = new Vector2(0f, 1f);
            taskTips.rectTransform.anchoredPosition = new Vector2(6f, -32f);
            taskTips.rectTransform.sizeDelta = new Vector2(215f, 18f);
            taskTips.fontSize = 18f;
            taskTips.alignment = TextAlignmentOptions.TopLeft;
            taskTips.textWrappingMode = TextWrappingModes.Normal;
            taskTips.gameObject.SetActive(false); // 数据到达时 SetTips 现身
            item.taskTips = taskTips;

            RectTransform fingerCon = UiCreatorKit.NewNode("GuideFingerSlot", root); // 老端: _box_finger_con
            UiCreatorKit.Place(fingerCon, 0f, 0f, 197f, 54f); // centerX=0/centerY=0
            fingerCon.gameObject.SetActive(false); // OnInit 强制隐藏
            item._box_finger_con = fingerCon;

            RectTransform effect = UiCreatorKit.NewNode("CompletionEffectSlot", root); // 老端: _box_effect
            PlaceLaya(effect, 0f, -1f, 192f, 57f, w, h);
            item._box_effect = effect;

            root.gameObject.SetActive(false); // 未激活模板
            return root.gameObject;
        }

        // ---------------------------------------------------------------- TeamMainRoleItem 模板(挂生成的 Bind)

        /// <summary>
        /// 对标 resource/game/team/TeamMainRoleItem.json,画布 222.74x78.24。挂已写好的业务类
        /// Shenxiao.Module.Core.Team.TeamMainRoleItem(照 MainUITaskItem 先例),回填全部 20 个 Bind 字段。
        /// MainUITaskTeamView.RefreshTeamPanel 用它 Instantiate 克隆。未激活模板。
        /// </summary>
        private static GameObject BuildTeamMainRoleItemTemplate(Transform parent)
        {
            const float w = 222.74f, h = 78.24f;
            RectTransform root = UiCreatorKit.NewNode("TeamMainRoleItem", parent);
            UiCreatorKit.Place(root, 0f, 0f, w, h);
            var bind = root.gameObject.AddComponent<Shenxiao.Module.Core.Team.TeamMainRoleItem>();

            Image image1 = UiCreatorKit.NewImage("TeamItemBackground", root); // 老端: _Image1
            PlaceLaya(image1.rectTransform, 0f, 1f, 222.3f, 77.6f, w, h);
            image1.raycastTarget = false;
            image1.type = Image.Type.Sliced; // sizeGrid 5,5,5,5
            UiCreatorKit.TrySetSprite(image1, IMG_TEAMITEM_BG, UiCreatorKit.Palette.Panel);
            bind._Image1 = image1;

            // ---- InviteSlot(老端: non_role,邀请队员占位,设计默认隐藏)----
            RectTransform nonRole = UiCreatorKit.NewNode("InviteSlot", root); // 老端: non_role
            PlaceLaya(nonRole, 0f, 0f, 222.74f, 86.58f, w, h);
            nonRole.gameObject.SetActive(false);
            bind.non_role = nonRole;
            const float nonRoleW = 222.74f, nonRoleH = 86.58f;

            Image inviteIcon = UiCreatorKit.NewImage("InviteIcon", nonRole); // 老端: Image
            UiCreatorKit.Place(inviteIcon.rectTransform, -79f, 2f, 58f, 58f); // centerX=-79/centerY=-2
            inviteIcon.raycastTarget = true;
            UiCreatorKit.TrySetSprite(inviteIcon, IMG_TEAMITEM_INVITE, UiCreatorKit.Palette.BtnSecond);
            bind.Image = inviteIcon;

            // lbl_tips/role_name/level 三处设计源没写 width/height,且组队面板未在快照里展开过、无实测值
            // 可核对,给个够用的起步框(自行在编辑器调)。
            TextMeshProUGUI tips = UiCreatorKit.NewText("InviteTipsLabel", nonRole, "邀请队员"); // 老端: lbl_tips
            PlaceLaya(tips.rectTransform, 61f, 31f, 120f, 24f, nonRoleW, nonRoleH);
            tips.fontSize = 18f;
            tips.alignment = TextAlignmentOptions.Left;
            tips.color = ParseColor("#9ade4d", Color.green);
            bind.lbl_tips = tips;

            Image redPoint = UiCreatorKit.NewImage("TeamApplyRedDot", nonRole); // 老端: red_point
            UiCreatorKit.Place(redPoint.rectTransform, -60f, 20f, 28f, 29f); // centerX=-60/centerY=-20
            redPoint.raycastTarget = false;
            UiCreatorKit.TrySetSprite(redPoint, IMG_TEAMITEM_RED_POINT, UiCreatorKit.Palette.Mark);
            redPoint.gameObject.SetActive(false);
            bind.red_point = redPoint;

            // ---- RoleInfoGroup(老端: role_group,有队友时的正式内容)----
            RectTransform roleGroup = UiCreatorKit.NewNode("RoleInfoGroup", root); // 老端: role_group
            PlaceLaya(roleGroup, 0f, 0f, 222.74f, 86.58f, w, h);
            bind.role_group = roleGroup;
            const float rgW = 222.74f, rgH = 86.58f;

            Image onlineImg = UiCreatorKit.NewImage("OnlineStateIcon", roleGroup); // 老端: online_img
            PlaceLaya(onlineImg.rectTransform, 0f, 0f, 38f, 16f, rgW, rgH);
            onlineImg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(onlineImg, IMG_COMMON_EMPTY, UiCreatorKit.Palette.Panel);
            bind.online_img = onlineImg;

            // role_head 是纯容器(Bind 字段类型 RectTransform),运行时挂真实头像/模型,这里只搭位置。
            RectTransform roleHead = UiCreatorKit.NewNode("RoleHeadSlot", roleGroup); // 老端: role_head
            UiCreatorKit.Place(roleHead, -80f, 0f, 93f, 87f); // centerX=-80/centerY=0
            roleHead.localScale = new Vector3(0.5f, 0.5f, 1f);
            bind.role_head = roleHead;

            Image leaderTag = UiCreatorKit.NewImage("LeaderTag", roleGroup); // 老端: leader_tag
            PlaceLaya(leaderTag.rectTransform, 173f, 13f, 23f, 25f, rgW, rgH); // 23x25 取自贴图原始尺寸(设计源没写)
            leaderTag.raycastTarget = false;
            UiCreatorKit.TrySetSprite(leaderTag, IMG_TEAM_LEADER_TAG, UiCreatorKit.Palette.Mark);
            bind.leader_tag = leaderTag;

            RectTransform headClick = UiCreatorKit.NewNode("HeadClickArea", roleGroup); // 老端: head_click_group
            PlaceLaya(headClick, 0f, 1f, 62.74f, 76f, rgW, rgH);
            bind.head_click_group = headClick;

            TextMeshProUGUI roleName = UiCreatorKit.NewText("RoleNameLabel", roleGroup, "玩家名字"); // 老端: role_name
            PlaceLaya(roleName.rectTransform, 61f, 16f, 140f, 24f, rgW, rgH);
            roleName.fontSize = 20f;
            roleName.alignment = TextAlignmentOptions.Left;
            bind.role_name = roleName;

            // 老端: _Group1 —— 空容器,老端 TS/JSON 里从未被实际引用/赋图,用途不明,按其位置(挨着
            // destiny_img 左侧)起个保守名,不瞎猜语义。
            RectTransform group1 = UiCreatorKit.NewNode("RoleExtraSlot", roleGroup); // 老端: _Group1
            PlaceLaya(group1, 44f, 45f, 21.98f, 32.1f, rgW, rgH);
            bind._Group1 = group1;

            Image destinyImg = UiCreatorKit.NewImage("AscendMarkIcon", roleGroup); // 老端: destiny_img
            PlaceLaya(destinyImg.rectTransform, 60f, 49f, 35f, 33f, rgW, rgH);
            destinyImg.raycastTarget = false;
            destinyImg.rectTransform.localScale = new Vector3(0.6f, 0.6f, 1f);
            UiCreatorKit.TrySetSprite(destinyImg, IMG_ASCEND_MARK, UiCreatorKit.Palette.Mark);
            bind.destiny_img = destinyImg;

            TextMeshProUGUI level = UiCreatorKit.NewText("LevelLabel", roleGroup, "1级"); // 老端: level
            PlaceLaya(level.rectTransform, 82f, 51f, 80f, 24f, rgW, rgH);
            level.fontSize = 18f;
            level.alignment = TextAlignmentOptions.Left;
            level.color = ParseColor("#bbb64d", Color.yellow);
            bind.level = level;

            RectTransform quitBtn = UiCreatorKit.NewNode("QuitTeamButton", roleGroup); // 老端: quitBtn
            PlaceLaya(quitBtn, 140f, 46f, 73.414f, 29f, rgW, rgH);
            bind.quitBtn = quitBtn;

            Image image2 = UiCreatorKit.NewImage("QuitTeamButtonBg", quitBtn); // 老端: _Image2
            UiCreatorKit.Place(image2.rectTransform, 0f, 0f, 73.4f, 28.71f); // 铺满(<1px 偏移已简化为 0)
            UiCreatorKit.TrySetSprite(image2, IMG_COMMON_BTN_BG, UiCreatorKit.Palette.BtnNeutral);
            bind._Image2 = image2;

            TextMeshProUGUI label1 = UiCreatorKit.NewText("QuitTeamButtonLabel", quitBtn, "退队"); // 老端: _Label1
            UiCreatorKit.Place(label1.rectTransform, 0f, 0f, 73.387f, 28.852f);
            label1.fontSize = 18f;
            bind._Label1 = label1;

            RectTransform pleaseLeaveBtn = UiCreatorKit.NewNode("KickMemberButton", roleGroup); // 老端: pleaseLeaveBtn
            PlaceLaya(pleaseLeaveBtn, 141f, 46f, 71f, 28f, rgW, rgH);
            bind.pleaseLeaveBtn = pleaseLeaveBtn;

            Image image3 = UiCreatorKit.NewImage("KickMemberButtonBg", pleaseLeaveBtn); // 老端: _Image3
            PlaceLaya(image3.rectTransform, -1f, 0f, 73f, 29f, 71f, 28f);
            UiCreatorKit.TrySetSprite(image3, IMG_COMMON_BTN_BG, UiCreatorKit.Palette.BtnNeutral);
            bind._Image3 = image3;

            TextMeshProUGUI label2 = UiCreatorKit.NewText("KickMemberButtonLabel", pleaseLeaveBtn, "请离"); // 老端: _Label2
            PlaceLaya(label2.rectTransform, 0f, 1f, 71f, 27f, 71f, 28f);
            label2.fontSize = 18f;
            bind._Label2 = label2;

            root.gameObject.SetActive(false); // 未激活模板
            return root.gameObject;
        }

        // ---------------------------------------------------------------- 模板容器 helper(对标 HudOverlayPopupsCreator.NewTemplatesWrapper)

        private static RectTransform NewTemplatesWrapper(Transform parent)
        {
            RectTransform wrapper = UiCreatorKit.NewNode("__Templates", parent);
            UiCreatorKit.Place(wrapper, 0f, 0f, 100f, 100f);
            wrapper.gameObject.SetActive(false);
            return wrapper;
        }

        // ---------------------------------------------------------------- ScrollRect helper(UiCreatorKit 没有,本文件自建)

        /// <summary>
        /// 建一个 ScrollRect:Viewport(RectMask2D 裁切)+ Content(顶部对齐,业务代码手动摆子项位置)+
        /// 一张透明命中图铺在根节点上,保证列表空白区域也能拖拽(对标 UIUtil 给纯容器补命中图的思路)。
        /// contentHeight 显式传入(而不是读 viewport.rect.height)——调用时根节点还没 Place,读 rect 会是 0。
        /// </summary>
        private static ScrollRect NewScrollRect(string name, Transform parent, float contentHeight, out RectTransform content)
        {
            RectTransform root = UiCreatorKit.NewNode(name, parent);
            Image hit = root.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            var scroll = root.gameObject.AddComponent<ScrollRect>();

            RectTransform viewport = UiCreatorKit.NewNode("Viewport", root);
            UiCreatorKit.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = UiCreatorKit.NewNode("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, contentHeight);

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }

        // ---------------------------------------------------------------- 布局换算 helper(本文件私有,对标 HudTopCreator.PlaceLaya)

        /// <summary>面板整体锚左下(对标老端 view 在主界面里的绝对位置,而非父层中心)。</summary>
        private static void AnchorBottomLeft(RectTransform rt, float left, float bottom, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(left, bottom);
        }

        /// <summary>左上锚定:x 向右、y 向下为正。任务条增高时标题/完成标记保持贴顶。</summary>
        private static void AnchorTopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -y);
        }

        /// <summary>
        /// 把 Laya 节点的 (x,y,w,h,anchorX,anchorY)(父左上原点,y 向下;anchorX/anchorY 是节点自身锚点
        /// 在 0~1 的位置,0=以左/上边为基准即 x/y 就是左上角,1=以右/下边为基准)换算成 UiCreatorKit.Place
        /// 的父中心锚坐标并摆位。parentW/parentH 用该层级子节点公用的设计画布宽高。
        /// 注意:与此不同,Laya 的 centerX/centerY(相对父中心的像素偏移)不走这个 helper——直接
        /// UiCreatorKit.Place(rt, centerX, -centerY, w, h)(centerY 向下为正,取负翻到 Unity 向上为正)。
        /// </summary>
        private static void PlaceLaya(RectTransform rt, float x, float y, float w, float h,
            float parentW, float parentH, float anchorX = 0f, float anchorY = 0f)
        {
            float centerTopLeftX = x + (0.5f - anchorX) * w;
            float centerTopLeftY = y + (0.5f - anchorY) * h;
            float cx = centerTopLeftX - parentW / 2f;
            float cy = -(centerTopLeftY - parentH / 2f);
            UiCreatorKit.Place(rt, cx, cy, w, h);
        }

        private static Color ParseColor(string hex, Color fallback)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : fallback;
        }

        // ---------------------------------------------------------------- 预览

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 HudTaskTeam",
                    "请先进入 Play 模式(主界面已起、UI 层已初始化)再点预览。\n\n" +
                    "MainUITaskTeamView 是 MainUIModule 内「模块合并 prefab」子窗口(BaseView.Show 由业务流程" +
                    "直接管理),没有 [UIView] 特性、不走 ViewManager.Open。预览改为:直接从最新 prefab " +
                    "实例化到 Main 层并调用 view.Show(),仅用于看结构/试交互,不代表真机的加载路径。",
                    "好");
                return;
            }

            if (_previewInstance != null)
            {
                Object.Destroy(_previewInstance);
                _previewInstance = null;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[UiCreator] 预览失败,找不到 " + PrefabPath + "(请先点生成)");
                return;
            }

            Transform layer = ViewManager.GetLayer(UILayer.Main);
            _previewInstance = Object.Instantiate(prefab, layer);
            var view = _previewInstance.GetComponentInChildren<MainUITaskTeamView>(true);
            if (view == null)
            {
                Debug.LogError("[UiCreator] 预览失败,prefab 缺 MainUITaskTeamView 组件");
                return;
            }
            view.Show();
            Selection.activeObject = _previewInstance;
        }
    }
}
