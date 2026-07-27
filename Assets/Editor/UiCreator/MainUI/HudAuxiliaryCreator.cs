using Shenxiao.Module.Core.MainUI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.MainUI
{
    /// <summary>
    /// 原 HudSecondary 的职责拆分生成器。
    /// 每个 prefab 都是可在 Scene/Prefab 编辑器中直接选中的有界区域；坐标只在 Creator/prefab 中维护。
    /// 活动 loc4/5 已归 HudActivity，降神模板已归 HudSkillBar，本文件不再复制活动模板或技能模板。
    /// </summary>
    public static class HudAuxiliaryCreator
    {
        private const string NotificationPath = "Assets/Prefabs/UI/MainUI/Regions/HudNotification.prefab";
        private const string OnHookPath = "Assets/Prefabs/UI/MainUI/Regions/HudOnHook.prefab";
        private const string SceneAssistPath = "Assets/Prefabs/UI/MainUI/Regions/HudSceneAssist.prefab";

        private const string IMG_HELP = "resource/game/mainUI/texture/guild_help.png";
        private const string IMG_HELP_TIPS = "resource/game/mainUI/texture/uigh_124.png";
        private const string IMG_NOTICE_EMAIL = "resource/game/mainUI/texture/ui_notice_3.png";
        private const string IMG_RED_DOT = "resource/game/mainUI/texture/com_red_point.png";
        private const string IMG_RED_NUM_BG = "resource/game/mainUI/texture/ui_bq_03.png";
        private const string IMG_EXP_TITLE_BG = "resource/game/mainUI/texture/com_title_bg1.png";
        private const string IMG_EXP_BG = "resource/game/mainUI/texture/ui_gj_12.png";
        private const string IMG_EXP_ADD = "resource/game/common/texture/ui_gj_27.png";
        private const string IMG_EXP_BTN = "resource/game/mainUI/texture/ui_gj_13.png";
        private const string IMG_PLEASE = "resource/game/mainUI/texture/please.png";
        private const string IMG_RED_PACKET_RAIN = "resource/game/mainUI/texture/uihby_014.png";

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudNotification(临时通知区)",
                Note = "有界消息/邀请/公会协助区；活动入口不得在这里硬编码",
                Order = 60,
                Generate = GenerateNotification,
                PrefabPath = NotificationPath,
            });
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudOnHook(挂机经验区)",
                Note = "有界挂机经验丸区域；显隐等待权威数据，不在 MainUI 写等级兜底",
                Order = 61,
                Generate = GenerateOnHook,
                PrefabPath = OnHookPath,
            });
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "MainUI",
                Name = "HudSceneAssist(场景临时挂点)",
                Note = "有界场景态特效/提示挂点；默认隐藏，由对应系统事件按需点亮",
                Order = 62,
                Generate = GenerateSceneAssist,
                PrefabPath = SceneAssistPath,
            });
        }

        public static void GenerateNotification()
        {
            // 原节点屏幕范围约 x=168..509、底部 y=256..394；外框留出提示气泡空间。
            RectTransform root = NewBottomCenterRoot("HudNotification", 540f, 160f, 250f);
            var view = root.gameObject.AddComponent<MainUINotificationView>();

            RectTransform help = UiCreatorKit.NewNode("GuildHelpButtonBox", root);
            PlaceTopLeft(help, 78f, 99f, 55f, 55f);
            view._box_help = help;
            Image helpIcon = UiCreatorKit.NewImage("GuildHelpIcon", help);
            UiCreatorKit.Stretch(helpIcon.rectTransform);
            UiCreatorKit.TrySetSprite(helpIcon, IMG_HELP, UiCreatorKit.Palette.BtnSecond);
            view._img_help = helpIcon;
            help.gameObject.SetActive(false);

            RectTransform helpTips = UiCreatorKit.NewNode("GuildHelpTipBubble", help);
            UiCreatorKit.Place(helpTips, 90f, 18f, 175f, 59f);
            view._box_help_tips = helpTips;
            Image helpTipsBg = UiCreatorKit.NewImage("GuildHelpTipBubbleBg", helpTips);
            UiCreatorKit.Stretch(helpTipsBg.rectTransform);
            helpTipsBg.type = Image.Type.Sliced;
            UiCreatorKit.TrySetSprite(helpTipsBg, IMG_HELP_TIPS, UiCreatorKit.Palette.Panel);
            view._img_help_tips = helpTipsBg;
            TextMeshProUGUI helpLabel = UiCreatorKit.NewText("GuildHelpTipLabel", helpTips, "收到新的协助请求");
            UiCreatorKit.Place(helpLabel.rectTransform, 6.5f, 1.5f, 144f, 18f);
            helpLabel.fontSize = 18f;
            helpLabel.color = new Color32(0x4a, 0x3a, 0x32, 0xff);
            helpLabel.alignment = TextAlignmentOptions.TopLeft;
            view._lb_help = helpLabel;
            helpTips.gameObject.SetActive(false);

            RectTransform bar = UiCreatorKit.NewNode("NotificationBar", root);
            // 老端按当前消息状态动态排布；这里保留完整九类通知的可用宽度，
            // 不再用四枚 55px 图标反推固定 300px 容器。
            PlaceTopLeft(bar, 20f, 16f, 500f, 55f);
            view._box_notification_bar = bar;

            RectTransform content = UiCreatorKit.NewNode("NotificationContent", bar);
            UiCreatorKit.Stretch(content);
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            view.NotificationContent = content;

            RectTransform templates = UiCreatorKit.NewNode("__Templates", root);
            UiCreatorKit.Place(templates, 0f, 0f, 55f, 55f);
            MainUINotificationItem template = BuildNotificationTemplate(templates);
            templates.gameObject.SetActive(false);
            view.NotificationItemTemplate = template;

            Save(root, NotificationPath, "HudNotification");
        }

        public static void GenerateOnHook()
        {
            RectTransform root = NewBottomCenterRoot("HudOnHook", 415f, 118f, 352f);
            var view = root.gameObject.AddComponent<MainUIOnHookView>();

            RectTransform box = UiCreatorKit.NewNode("OnHookExpOrbBox", root);
            PlaceTopLeft(box, 79f, 0f, 257f, 61f);
            view._box_outline_exp = box;

            Image bg1 = UiCreatorKit.NewImage("ExpOrbBaseBg", box);
            UiCreatorKit.Place(bg1.rectTransform, 0f, -3.5f, 351f, 40f);
            bg1.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg1, IMG_EXP_TITLE_BG, UiCreatorKit.Palette.Panel);
            view._img_outline_exp_bg1 = bg1;

            Image bg = UiCreatorKit.NewImage("ExpOrbRewardBg", box);
            UiCreatorKit.Place(bg.rectTransform, 0.5f, 30.5f, 270f, 118f);
            bg.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bg, IMG_EXP_BG, UiCreatorKit.Palette.Panel);
            bg.gameObject.SetActive(false);
            view._img_outline_exp_bg = bg;

            RectTransform expShow = UiCreatorKit.NewNode("ExpRewardEffectSlot", box);
            UiCreatorKit.Place(expShow, 0f, 45.5f, 415f, 100f);
            expShow.gameObject.SetActive(false);
            view.exp_show = expShow;

            RectTransform row = UiCreatorKit.NewNode("ExpInfoRow", box);
            UiCreatorKit.Place(row, 0f, 0f, 219f, 31f);
            TextMeshProUGUI label = UiCreatorKit.NewText("ExpRateLabel", row, "<color=#00fa64>0</color>经验/分");
            UiCreatorKit.Place(label.rectTransform, -49.5f, -3.5f, 120f, 18f);
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.Left;
            view._lb_outline_exp = label;

            RectTransform addButton = UiCreatorKit.NewNode("ExpBoostButton", row);
            UiCreatorKit.Place(addButton, 75f, 0f, 69f, 31f);
            view.add_btn = addButton;
            RectTransform addAnchor = UiCreatorKit.NewNode("ExpBoostEffectAnchor", addButton);
            UiCreatorKit.Stretch(addAnchor);
            view.add = addAnchor;
            Image addIcon = UiCreatorKit.NewImage("ExpBoostIcon", addButton);
            UiCreatorKit.Place(addIcon.rectTransform, -9.5f, -2.5f, 50f, 30f);
            UiCreatorKit.TrySetSprite(addIcon, IMG_EXP_ADD, UiCreatorKit.Palette.BtnPrimary);
            addIcon.gameObject.SetActive(false);
            view._img_add = addIcon;

            RectTransform rewardButton = UiCreatorKit.NewNode("ExpRewardButtonBox", box);
            UiCreatorKit.Place(rewardButton, 0.5f, 46.5f, 66f, 70f);
            view._box_exp_btn = rewardButton;
            Image rewardIcon = UiCreatorKit.NewImage("ExpRewardButtonIcon", rewardButton);
            UiCreatorKit.Stretch(rewardIcon.rectTransform);
            UiCreatorKit.TrySetSprite(rewardIcon, IMG_EXP_BTN, UiCreatorKit.Palette.BtnPrimary);
            view.exp_btn = rewardIcon;
            rewardButton.gameObject.SetActive(false);

            RectTransform legacy = UiCreatorKit.NewNode("LegacyExpOrbBox", root);
            PlaceTopLeft(legacy, 79f, 0f, 257f, 61f);
            view._box_old_outline_exp = legacy;
            Image legacyBg = UiCreatorKit.NewImage("LegacyExpOrbBg", legacy);
            UiCreatorKit.Place(legacyBg.rectTransform, 0f, -0.5f, 351f, 40f);
            UiCreatorKit.TrySetSprite(legacyBg, IMG_EXP_TITLE_BG, UiCreatorKit.Palette.Panel);
            view._img_old_outline_exp_bg = legacyBg;
            TextMeshProUGUI legacyLabel = UiCreatorKit.NewText("LegacyExpRateLabel", legacy, string.Empty);
            UiCreatorKit.Place(legacyLabel.rectTransform, 0.5f, -0.5f, 200f, 40f);
            legacyLabel.fontSize = 22f;
            legacyLabel.color = new Color32(0x71, 0xe1, 0x5b, 0xff);
            view._lb_old_outline_exp = legacyLabel;

            box.gameObject.SetActive(false);
            legacy.gameObject.SetActive(false);
            Save(root, OnHookPath, "HudOnHook");
        }

        public static void GenerateSceneAssist()
        {
            // 原散布点统一收进 x=75..645、屏幕底部 y=250..650 的有界编辑区域。
            RectTransform root = NewBottomCenterRoot("HudSceneAssist", 570f, 400f, 250f);
            var view = root.gameObject.AddComponent<MainUISceneAssistView>();

            RectTransform autoEffect = UiCreatorKit.NewNode("AutoStateEffectSlot", root);
            PlaceTopLeft(autoEffect, 160f, 10f, 250f, 200f);
            view._box_auto_effect = autoEffect;

            RectTransform treasure = UiCreatorKit.NewNode("TreasureMapEffectSlot", root);
            PlaceTopLeft(treasure, 77f, 89f, 416f, 100f);
            view._gp_t_map = treasure;

            RectTransform please = UiCreatorKit.NewNode("MarriageGiftHintBox", root);
            PlaceTopLeft(please, 343f, 109f, 70f, 70f);
            view._box_please = please;
            Image pleaseIcon = UiCreatorKit.NewImage("MarriageGiftHintIcon", please);
            UiCreatorKit.Place(pleaseIcon.rectTransform, 0.5f, 0f, 65f, 58f);
            UiCreatorKit.TrySetSprite(pleaseIcon, IMG_PLEASE, UiCreatorKit.Palette.BtnPrimary);
            view._img_please = pleaseIcon;

            RectTransform progress = UiCreatorKit.NewNode("RedPacketRainEntryBox", root);
            PlaceTopLeft(progress, 246f, 6f, 78f, 78f);
            view._gp_pro = progress;
            Image redPacketRain = UiCreatorKit.NewImage("RedPacketRainIcon", progress);
            UiCreatorKit.Stretch(redPacketRain.rectTransform);
            UiCreatorKit.TrySetSprite(redPacketRain, IMG_RED_PACKET_RAIN, UiCreatorKit.Palette.BtnPrimary);
            view._img_rpr = redPacketRain;

            Image record = UiCreatorKit.NewImage("TtRecordButton", root);
            PlaceTopLeft(record.rectTransform, 463f, 339f, 55f, 55f);
            record.color = UiCreatorKit.Palette.BtnNeutral;
            view._img_tt_record = record;

            autoEffect.gameObject.SetActive(false);
            treasure.gameObject.SetActive(false);
            please.gameObject.SetActive(false);
            progress.gameObject.SetActive(false);
            record.gameObject.SetActive(false);
            Save(root, SceneAssistPath, "HudSceneAssist");
        }

        private static MainUINotificationItem BuildNotificationTemplate(Transform parent)
        {
            RectTransform root = UiCreatorKit.NewNode("NotificationItemTemplate", parent);
            UiCreatorKit.Place(root, 0f, 0f, 55f, 55f);
            var item = root.gameObject.AddComponent<MainUINotificationItem>();
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 55f;
            layout.preferredHeight = 55f;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;

            Image icon = UiCreatorKit.NewImage("NoticeIcon", root);
            UiCreatorKit.Stretch(icon.rectTransform);
            UiCreatorKit.TrySetSprite(icon, IMG_NOTICE_EMAIL, UiCreatorKit.Palette.BtnSecond);
            item.Icon = icon;

            RectTransform effect = UiCreatorKit.NewNode("NoticeEffectAnchor", root);
            UiCreatorKit.Stretch(effect);
            effect.gameObject.SetActive(false);
            item.EffectAnchor = effect;

            Image redDot = UiCreatorKit.NewImage("NoticeRedDot", root);
            UiCreatorKit.Place(redDot.rectTransform, 19f, 19f, 24f, 24f);
            UiCreatorKit.TrySetSprite(redDot, IMG_RED_DOT, UiCreatorKit.Palette.Mark);
            redDot.gameObject.SetActive(false);
            item.RedDot = redDot;

            Image countBadge = UiCreatorKit.NewImage("NoticeCountBadge", root);
            UiCreatorKit.Place(countBadge.rectTransform, 18f, 18f, 31f, 31f);
            UiCreatorKit.TrySetSprite(countBadge, IMG_RED_NUM_BG, UiCreatorKit.Palette.Mark);
            item.CountBadge = countBadge;

            TextMeshProUGUI count = UiCreatorKit.NewText("NoticeCountLabel", countBadge.transform, string.Empty);
            UiCreatorKit.Stretch(count.rectTransform);
            count.fontSize = 18f;
            count.alignment = TextAlignmentOptions.Center;
            item.CountLabel = count;
            countBadge.gameObject.SetActive(false);

            return item;
        }

        private static RectTransform NewBottomCenterRoot(string name, float width, float height, float bottom)
        {
            RectTransform root = UiCreatorKit.NewRoot(name);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(width, height);
            root.anchoredPosition = new Vector2(0f, bottom);
            root.gameObject.SetActive(false);
            return root;
        }

        private static void PlaceTopLeft(RectTransform rt, float x, float y, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(x, -y);
        }

        private static void Save(RectTransform root, string path, string label)
        {
            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, path);
            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] " + label + ".prefab 已生成：" + path + "（有界区域，布局只在 prefab/Creator）");
        }
    }
}
