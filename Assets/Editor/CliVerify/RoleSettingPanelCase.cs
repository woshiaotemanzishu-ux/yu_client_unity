using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Dungeon;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Setting;
using Shenxiao.Module.Core.Skill;
using Shenxiao.Module.Core.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>人物/设置面板的 Prefab 几何与人物属性运行态验收。</summary>
    public static class RoleSettingPanelCase
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            ResManager.EditorPreferFallback = true;

            bool settingLayoutOk = VerifySettingPrefab();
            bool settingSpritesOk = VerifySettingSprites();
            bool rolePrefabOk = VerifyRolePrefab();
            bool roleAssetsOk = VerifyRoleAssets();
            bool roleRuntimeOk = await VerifyRoleRuntime();
            bool skillRuntimeOk = await VerifySkillRuntime();

            bool pass = settingLayoutOk && settingSpritesOk && rolePrefabOk
                && roleAssetsOk && roleRuntimeOk && skillRuntimeOk;
            Debug.Log("CLIVERIFY rolesetting VERDICT settingLayout=" + settingLayoutOk
                + " settingSprites=" + settingSpritesOk + " rolePrefab=" + rolePrefabOk
                + " roleAssets=" + roleAssetsOk + " roleRuntime=" + roleRuntimeOk
                + " skillRuntime=" + skillRuntimeOk
                + " pass=" + pass);
            return pass ? 0 : 3;
        }

        [MenuItem("神霄/验收/人物页视觉")]
        public static async void RunFromEditorMenu()
        {
            ResManager.EditorPreferFallback = true;
            bool prefab = VerifyRolePrefab();
            bool runtime = await VerifyRoleRuntime();
            if (prefab && runtime)
            {
                Debug.Log("CLIVERIFY rolevisual EDITOR PASS prefab=True runtime=True");
            }
            else
            {
                Debug.LogError("CLIVERIFY rolevisual EDITOR FAIL prefab=" + prefab + " runtime=" + runtime);
            }
        }

        private static bool VerifySettingPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Setting/SettingModule.prefab");
            SettingView view = prefab != null ? prefab.GetComponentInChildren<SettingView>(true) : null;
            if (view == null) return false;

            bool head = Near(view.change_head_btn, 125f, 37f) && view._Label1 != null && view._Label1.gameObject.activeSelf;
            bool copy = Near(view._btn_copy, 100f, 34f) && view._Label41 != null && view._Label41.gameObject.activeSelf;
            bool flow = view._box_base_setting != null && view._box_base_setting.content != null
                && view._box_base_setting.content.GetComponent<VerticalLayoutGroup>() != null
                && view._box_base_setting.content.GetComponent<ContentSizeFitter>() != null;
            bool grids = view._list_pick != null && view._list_pick.content.GetComponent<GridLayoutGroup>() != null
                && view._list_shield != null && view._list_shield.content.GetComponent<GridLayoutGroup>() != null;
            Debug.Log("CLIVERIFY rolesetting settingPrefab head=" + head + " copy=" + copy
                + " flow=" + flow + " grids=" + grids);
            return head && copy && flow && grids;
        }

        private static bool VerifySettingSprites()
        {
            bool changeHead = SpriteSize("Assets/GameRes/resource/game/setting/texture/ui_button_rect8.png", 98, 28);
            bool copy = SpriteSize("Assets/GameRes/resource/game/setting/texture/ui_button_rect9.png", 98, 28);
            bool baseTab = SpriteSize("Assets/GameRes/resource/game/setting/texture/ui_button_rect5.png", 150, 57);
            bool shieldTab = SpriteSize("Assets/GameRes/resource/game/setting/texture/ui_button_mid_1.png", 124, 52);
            bool header = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/GameRes/resource/game/setting/texture/ui_friends_25.png") != null;
            Debug.Log("CLIVERIFY rolesetting settingSprites head=" + changeHead + " copy=" + copy
                + " baseTab=" + baseTab + " shieldTab=" + shieldTab + " header=" + header);
            return changeHead && copy && baseTab && shieldTab && header;
        }

        private static bool VerifyRolePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Role/RoleModule.prefab");
            EquipmentView view = prefab != null ? prefab.GetComponentInChildren<EquipmentView>(true) : null;
            if (view == null || view._Scroller1 == null || view._Scroller1.content == null) return false;
            GridLayoutGroup grid = view._Scroller1.content.GetComponent<GridLayoutGroup>();
            bool gridOk = grid != null && grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                && grid.constraintCount == 2 && Near(grid.cellSize.x, 300f) && Near(grid.cellSize.y, 38f);
            bool fitter = view._Scroller1.content.GetComponent<ContentSizeFitter>() != null;
            bool fight = view._gp_fight != null && view._gp_fight.GetComponent<HorizontalOrVerticalLayoutGroup>() != null;
            bool title = view._img_title_base != null && view._img_title_base.gameObject.activeSelf
                && view._img_title_best != null && !view._img_title_best.gameObject.activeSelf;
            bool modelBackground = view.model_bg != null
                && view.model_bg.sprite == null
                && !view.model_bg.enabled;
            bool skillPages = VerifySkillPrefab(prefab);
            Debug.Log("CLIVERIFY rolesetting rolePrefab grid=" + gridOk + " fitter=" + fitter
                + " fight=" + fight + " title=" + title + " modelBackground=" + modelBackground
                + " skillPages=" + skillPages);
            return gridOk && fitter && fight && title && modelBackground && skillPages;
        }

        private static bool VerifySkillPrefab(GameObject prefab)
        {
            Transform active = prefab.transform.Find("SkillInitiativeSubItem");
            Transform passive = prefab.transform.Find("SkillPassiveSubItem");
            Transform talent = prefab.transform.Find("InnateSkillView");
            SkillPassiveSubItem passiveView = passive != null
                ? passive.GetComponent<SkillPassiveSubItem>()
                : null;
            ScrollRect scroll = passiveView != null ? passiveView._Scroller1 : null;
            RectTransform content = scroll != null ? scroll.content : null;
            RectTransform viewport = scroll != null ? scroll.viewport : null;
            GridLayoutGroup grid = content != null ? content.GetComponent<GridLayoutGroup>() : null;
            ContentSizeFitter fitter = content != null ? content.GetComponent<ContentSizeFitter>() : null;
            Transform template = content != null ? content.Find("SkillPassiveItemTemplate") : null;

            bool directPages = active != null && passive != null && talent != null;
            bool passiveSize = Near(passive as RectTransform, 720f, 997f);
            bool scrollTree = scroll != null && !scroll.horizontal && scroll.vertical
                && viewport != null && viewport.GetComponent<RectMask2D>() != null
                && content != null && scroll.content == content;
            bool gridOk = grid != null
                && grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                && grid.constraintCount == 4
                && Near(grid.cellSize.x, 148f) && Near(grid.cellSize.y, 173f)
                && Near(grid.spacing.x, 0f) && Near(grid.spacing.y, 0f)
                && fitter != null
                && fitter.verticalFit == ContentSizeFitter.FitMode.PreferredSize;
            bool templateOk = template != null && !template.gameObject.activeSelf
                && template.GetComponent<SkillPassiveItem>() != null;
            Debug.Log("CLIVERIFY rolesetting skillPrefab direct=" + directPages
                + " passiveSize=" + passiveSize + " scrollTree=" + scrollTree
                + " grid=" + gridOk + " template=" + templateOk);
            return directPages && passiveSize && scrollTree && gridOk && templateOk;
        }

        private static bool VerifyRoleAssets()
        {
            Texture2D roleBackground = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/GameRes/resource/game/bigBg/ui_role_new_bg_1.jpg");
            Texture2D designationBackground = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/GameRes/resource/game/bigBg/ui_role_bg5.jpg");
            bool backgrounds = roleBackground != null
                && roleBackground.width == 720 && roleBackground.height == 1200
                && designationBackground != null;
            bool configs =
                AssetDatabase.LoadAssetAtPath<TextAsset>(
                    "Assets/GameRes/resource/config/client/configinstruction.json") != null
                && AssetDatabase.LoadAssetAtPath<TextAsset>(
                    "Assets/GameRes/resource/config/server/config_dsgt.json") != null
                && AssetDatabase.LoadAssetAtPath<TextAsset>(
                    "Assets/GameRes/resource/config/server/config_fame_lv.json") != null;
            int designationIconCount = AssetDatabase.FindAssets(
                "t:Sprite",
                new[] { "Assets/GameRes/resource/game/dsgtIcon" }).Length;
            bool roleDesignationPrefab =
                HasComponent(
                    "Assets/Prefabs/UI/Role/RoleModule.prefab",
                    "DsgtViewBind")
                && HasComponent(
                    "Assets/Prefabs/UI/Dsgt/DsgtModule.prefab",
                    "DsgtItemRendererBind")
                && HasComponent(
                    "Assets/Prefabs/UI/Dsgt/DsgtModule.prefab",
                    "DsgtDetailsItemBind");
            bool popupPrefabs =
                HasComponent(
                    "Assets/Prefabs/UI/Common/CommonModule.prefab",
                    "InstructionViewBind")
                && HasComponent(
                    "Assets/Prefabs/UI/Marriage/MarriageModule.prefab",
                    "MarriageHonourViewBind")
                && HasComponent(
                    "Assets/Prefabs/UI/Dsgt/DsgtModule.prefab",
                    "GetDsgtViewBind");
            Debug.Log("CLIVERIFY rolesetting roleAssets backgrounds=" + backgrounds
                + " configs=" + configs + " designationIcons=" + designationIconCount
                + " roleDesignationPrefab=" + roleDesignationPrefab
                + " popupPrefabs=" + popupPrefabs);
            return backgrounds && configs && designationIconCount == 103
                && roleDesignationPrefab && popupPrefabs;
        }

        private static bool HasComponent(string prefabPath, string typeName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return false;
            foreach (MonoBehaviour component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component != null && component.GetType().Name == typeName) return true;
            }
            return false;
        }

        private static async Task<bool> VerifyRoleRuntime()
        {
            RoleModel role = RoleModel.Instance;
            role.Reset();
            role.RoleId = 4294967524L;
            role.ServerName = "1-2区";
            role.Level = 630;
            role.Exp = 2070000000000L;
            role.ExpLim = 11750000000000L;
            role.CombatPower = 22868566L;
            role.Figure = new FigureProto
            {
                name = "111111",
                career = 1,
                sex = 1,
                level = 630,
                turn = 4,
            };
            role.BattleAttr = new BattleAttrProto { Hp = 1868655, HpLim = 1868655, Speed = 250 };
            role.BattleAttr.Attrs["att"] = 57263;
            role.BattleAttr.Attrs["wreck"] = 24067;
            role.BattleAttr.Attrs["def"] = 36438;
            role.BattleAttr.Attrs["hit"] = 1006;
            role.BattleAttr.Attrs["dodge"] = 1649;
            role.BattleAttr.Attrs["crit"] = 1016;
            role.BattleAttr.Attrs["ten"] = 1686;
            role.BattleAttr.Attrs["hurt_add_ratio"] = 1234;
            role.MarkBaseInfoReady();

            CliVerify.Stage stage = CliVerify.Stage.Create();
            GameObject frameRoot = null;
            GameObject roleRoot = null;
            try
            {
                GameObject framePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Common/BaseWindowSkin.prefab");
                GameObject rolePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Role/RoleModule.prefab");
                frameRoot = Object.Instantiate(framePrefab, ViewManager.GetLayer(UILayer.Window));
                roleRoot = Object.Instantiate(rolePrefab, ViewManager.GetLayer(UILayer.Window));
                foreach (Transform child in roleRoot.transform) child.gameObject.SetActive(false);

                BaseWindowSkinView window = frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
                EquipmentView equipment = roleRoot.GetComponentInChildren<EquipmentView>(true);
                window.Show();
                window.Configure(new[]
                {
                    new TabSpec
                    {
                        Enabled = true,
                        Label = "人物",
                        TitleImagePath = GameResPath.GetIcon("role", "title_name"),
                        BackgroundImagePath = GameResPath.GetBigBgPath("ui_role_new_bg_1.jpg"),
                        ContentFactory = parent =>
                        {
                            equipment.transform.SetParent(parent, false);
                            equipment.gameObject.SetActive(true);
                            return equipment;
                        },
                    },
                }, 0);

                int modelPixels = 0;
                string modelEvidence =
                    "output/ui_route_audit/2026-08-04_role_web_round2/cli_rolesetting_20260804_2415/role_model_rt.png";
                double modelDeadline = EditorApplication.timeSinceStartup + 12d;
                while (EditorApplication.timeSinceStartup < modelDeadline)
                {
                    await Task.Delay(100);
                    RawImage modelImage = equipment.model_gp != null
                        ? equipment.model_gp.GetComponentInChildren<RawImage>(true)
                        : null;
                    modelPixels = CaptureRenderedPixels(modelImage, modelEvidence);
                    if (modelPixels >= 64) break;
                }
                bool levelPresentation = equipment.levelLb != null && equipment.levelLb.text == "260级"
                    && equipment.destiny_img != null && equipment.destiny_img.gameObject.activeSelf
                    && equipment.top_levelLb != null && !equipment.top_levelLb.gameObject.activeSelf;
                int baseCount = equipment._Scroller1.content.GetComponentsInChildren<RolePropertyItemRenderer>(false).Length;
                RolePropertyItemRenderer[] baseItems = equipment._Scroller1.content.GetComponentsInChildren<RolePropertyItemRenderer>(false);
                bool rawValue = baseItems.Length > 0 && baseItems[0].property_value.text == "57263";
                stage.ForceCjkFont();
                string baseShot = stage.Capture("output/ui_route_audit/2026-08-04_role_web_round2/cli_rolesetting_20260804_2415/role_panel_base.png");

                MethodInfo showPage = typeof(EquipmentView).GetMethod("ShowAttributePage", PrivateInstance);
                showPage.Invoke(equipment, new object[] { false });
                LayoutRebuilder.ForceRebuildLayoutImmediate(equipment._Scroller1.content);
                await Task.Delay(200);
                RolePropertyItemRenderer[] specialItems = equipment._Scroller1.content.GetComponentsInChildren<RolePropertyItemRenderer>(false);
                int specialCount = specialItems.Length;
                bool percentValue = specialItems.Length > 0 && specialItems[0].property_value.text == "12.34%";
                bool titles = !equipment._img_title_base.gameObject.activeSelf && equipment._img_title_best.gameObject.activeSelf;
                stage.ForceCjkFont();
                string specialShot = stage.Capture("output/ui_route_audit/2026-08-04_role_web_round2/cli_rolesetting_20260804_2415/role_panel_special.png");

                bool ok = baseCount == 13 && specialCount == 31 && rawValue && percentValue
                    && titles && levelPresentation && modelPixels >= 64;
                Debug.Log("CLIVERIFY rolesetting roleRuntime base=" + baseCount + " special=" + specialCount
                    + " raw=" + rawValue + " percent=" + percentValue + " titles=" + titles
                    + " levelPresentation=" + levelPresentation + " modelPixels=" + modelPixels
                    + " shots=" + baseShot + " | " + specialShot + " ok=" + ok);
                return ok;
            }
            finally
            {
                if (frameRoot != null) Object.DestroyImmediate(frameRoot);
                if (roleRoot != null) Object.DestroyImmediate(roleRoot);
                stage.Dispose();
                role.Reset();
            }
        }

        private static int CaptureRenderedPixels(RawImage image, string evidencePath)
        {
            if (image == null || !image.gameObject.activeInHierarchy
                || !(image.texture is RenderTexture renderTexture) || !renderTexture.IsCreated())
                return 0;

            UIModelStage.RenderNow();
            RenderTexture previous = RenderTexture.active;
            Texture2D copy = null;
            try
            {
                RenderTexture.active = renderTexture;
                copy = new Texture2D(renderTexture.width, renderTexture.height,
                    TextureFormat.RGBA32, false, true);
                copy.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0, false);
                copy.Apply(false, false);
                Color32[] pixels = copy.GetPixels32();
                int count = 0;
                for (int i = 0; i < pixels.Length; i++)
                    if (pixels[i].a >= 8) count++;

                if (count > 0 && !string.IsNullOrEmpty(evidencePath))
                {
                    string fullPath = Path.GetFullPath(evidencePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? "Temp");
                    File.WriteAllBytes(fullPath, copy.EncodeToPNG());
                }
                return count;
            }
            finally
            {
                RenderTexture.active = previous;
                if (copy != null) Object.DestroyImmediate(copy);
            }
        }

        private static async Task<bool> VerifySkillRuntime()
        {
            await SkillConfigs.EnsureLoaded();
            await SkillUIConfigs.EnsureLoaded();
            await SkillPassiveConfigs.EnsureLoaded();
            await TaskConfigs.EnsureLoaded();

            RoleModel role = RoleModel.Instance;
            role.Reset();
            role.RoleId = 4294967524L;
            role.Level = 630;
            role.Figure = new FigureProto
            {
                name = "技能验收角色",
                career = 1,
                sex = 1,
                level = 630,
                turn = 4,
            };
            role.MarkBaseInfoReady();

            List<SkillUIConfigs.CareerSkill> configured = SkillUIConfigs.GetCareerSkills(1);
            var packet = new CliVerify.Pkt().H(configured.Count);
            for (int i = 0; i < configured.Count; i++)
                packet.I(configured[i].SkillId).H(i == configured.Count - 1 ? 1 : 2);
            byte[] bytes = packet.Bytes();
            SkillManager.Instance.Clear();
            SkillManager.Instance.CreateSkillList(new NetReader(bytes, 0, bytes.Length));
            List<SkillPassiveConfigs.PassiveSkillCfg> passiveConfigured =
                SkillPassiveConfigs.GetForCareer(1);
            var heartSkills = new List<DungeonModel.HeartSkillInfoEntry>();
            for (int i = 0; i < passiveConfigured.Count; i++)
            {
                heartSkills.Add(new DungeonModel.HeartSkillInfoEntry
                {
                    SkillId = (uint)passiveConfigured[i].SkillId,
                    SkillLv = 0,
                });
            }
            DungeonModel.Instance.ApplyHeartSkillInfo(heartSkills);

            CliVerify.Stage stage = CliVerify.Stage.Create();
            GameObject frameRoot = null;
            GameObject roleRoot = null;
            try
            {
                GameObject framePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/UI/Common/BaseWindowSkin.prefab");
                GameObject rolePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/UI/Role/RoleModule.prefab");
                frameRoot = Object.Instantiate(framePrefab, ViewManager.GetLayer(UILayer.Window));
                roleRoot = Object.Instantiate(rolePrefab, ViewManager.GetLayer(UILayer.Window));
                foreach (Transform child in roleRoot.transform) child.gameObject.SetActive(false);

                BaseWindowSkinView window = frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
                SkillInitiativeSubItem active = roleRoot.transform.Find("SkillInitiativeSubItem")
                    ?.GetComponent<SkillInitiativeSubItem>();
                SkillPassiveSubItem passive = roleRoot.transform.Find("SkillPassiveSubItem")
                    ?.GetComponent<SkillPassiveSubItem>();
                InnateSkillView talent = roleRoot.transform.Find("InnateSkillView")
                    ?.GetComponent<InnateSkillView>();
                if (window == null || active == null || passive == null || talent == null) return false;

                window.Show();
                window.Configure(new[]
                {
                    SkillTab("主动技能", active),
                    SkillTab("被动技能", passive),
                    SkillTab("天赋", talent),
                }, 0);

                SkillInitiativeItem[] activeItems = null;
                bool activeCount = false;
                bool activeDetail = false;
                double activeDeadline = EditorApplication.timeSinceStartup + 8d;
                while (EditorApplication.timeSinceStartup < activeDeadline)
                {
                    await Task.Delay(100);
                    activeItems = active.GetComponentsInChildren<SkillInitiativeItem>(false);
                    activeCount = activeItems.Length == 6;
                    activeDetail = active._lb_level != null && active._lb_level.text == "[2级]"
                        && active._lb_name != null && !string.IsNullOrEmpty(active._lb_name.text)
                        && active._img_boy != null && active._img_boy.enabled && active._img_boy.sprite != null;
                    if (activeCount && activeDetail) break;
                }
                stage.ForceCjkFont();
                string activeShot = stage.Capture("output/ui_route_audit/2026-08-04_role_web_round2/cli_rolesetting_20260804_2415/role_skill_active.png");

                window.SelectTab(1);
                await Task.Delay(500);
                SkillPassiveItem[] passiveItems = passive._Scroller1.content
                    .GetComponentsInChildren<SkillPassiveItem>(false);
                bool passiveList = passiveConfigured.Count == 6
                    && passiveItems.Length == passiveConfigured.Count;
                bool passiveDetail = passive._gp_desc3 != null && passive._gp_desc3.gameObject.activeSelf
                    && passive._gp_level_up != null && !passive._gp_level_up.gameObject.activeSelf
                    && passive._lb_open3 != null && passive._lb_open3.text.Contains("达到140级")
                    && passive._lb_open3.text.Contains("(未激活)");
                stage.ForceCjkFont();
                string passiveShot = stage.Capture("output/ui_route_audit/2026-08-04_role_web_round2/cli_rolesetting_20260804_2415/role_skill_passive.png");

                bool ok = configured.Count == 6 && activeCount && activeDetail
                    && passiveList && passiveDetail;
                Debug.Log("CLIVERIFY rolesetting skillRuntime config=" + configured.Count
                    + " activeCount=" + activeItems.Length + " activeDetail=" + activeDetail
                    + " passiveCount=" + passiveItems.Length + " passiveDetail=" + passiveDetail
                    + " shots=" + activeShot + " | " + passiveShot + " ok=" + ok);
                return ok;
            }
            finally
            {
                if (frameRoot != null) Object.DestroyImmediate(frameRoot);
                if (roleRoot != null) Object.DestroyImmediate(roleRoot);
                stage.Dispose();
                SkillManager.Instance.Clear();
                DungeonModel.Instance.Clear();
                role.Reset();
            }
        }

        private static TabSpec SkillTab(string label, BaseView view)
        {
            return new TabSpec
            {
                Enabled = true,
                Label = label,
                TitleImagePath = GameResPath.GetIcon("role", "title_name"),
                BackgroundImagePath = GameResPath.GetIconJpgOtherPath("role", "uijn_001"),
                ContentFactory = parent =>
                {
                    view.transform.SetParent(parent, false);
                    view.gameObject.SetActive(true);
                    return view;
                },
            };
        }

        private static bool SpriteSize(string path, int width, int height)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return texture != null && sprite != null && texture.width == width && texture.height == height;
        }

        private static bool Near(Component component, float width, float height)
        {
            RectTransform rect = component != null ? component.transform as RectTransform : null;
            return rect != null && Near(rect.rect.width, width) && Near(rect.rect.height, height);
        }

        private static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.1f;
    }
}
