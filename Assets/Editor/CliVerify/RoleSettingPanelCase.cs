using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Setting;
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

            bool pass = settingLayoutOk && settingSpritesOk && rolePrefabOk
                && roleAssetsOk && roleRuntimeOk;
            Debug.Log("CLIVERIFY rolesetting VERDICT settingLayout=" + settingLayoutOk
                + " settingSprites=" + settingSpritesOk + " rolePrefab=" + rolePrefabOk
                + " roleAssets=" + roleAssetsOk + " roleRuntime=" + roleRuntimeOk
                + " pass=" + pass);
            return pass ? 0 : 3;
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
            bool modelBackground = view.model_bg != null && view.model_bg.sprite == null;
            Debug.Log("CLIVERIFY rolesetting rolePrefab grid=" + gridOk + " fitter=" + fitter
                + " fight=" + fight + " title=" + title + " modelBackground=" + modelBackground);
            return gridOk && fitter && fight && title && modelBackground;
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
            bool popupPrefabs =
                HasComponent(
                    "Assets/Prefabs/UI/Common/CommonModule.prefab",
                    "InstructionViewBind")
                && HasComponent(
                    "Assets/Prefabs/UI/Marriage/MarriageModule.prefab",
                    "MarriageHonourViewBind")
                && HasComponent(
                    "Assets/Prefabs/UI/Dsgt/DsgtModule.prefab",
                    "DsgtViewBind");
            Debug.Log("CLIVERIFY rolesetting roleAssets backgrounds=" + backgrounds
                + " configs=" + configs + " designationIcons=" + designationIconCount
                + " popupPrefabs=" + popupPrefabs);
            return backgrounds && configs && designationIconCount == 103 && popupPrefabs;
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
            role.Level = 260;
            role.Exp = 2070000000000L;
            role.ExpLim = 11750000000000L;
            role.CombatPower = 22868566L;
            // 编辑器截图不创建 UIModelStage；人物模型链路只在 PlayMode/真机中装配。
            // 这样验收图不会混入 DontDestroyOnLoad 的编辑器伪影（右下黑块）。
            role.Figure = null;
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
                        ContentFactory = parent =>
                        {
                            equipment.transform.SetParent(parent, false);
                            equipment.gameObject.SetActive(true);
                            return equipment;
                        },
                    },
                }, 0);

                await Task.Delay(400);
                equipment._lb_name.text = "广海嘉";
                equipment.top_levelLb.text = "Lv.260 广海嘉";
                int baseCount = equipment._Scroller1.content.GetComponentsInChildren<RolePropertyItemRenderer>(false).Length;
                RolePropertyItemRenderer[] baseItems = equipment._Scroller1.content.GetComponentsInChildren<RolePropertyItemRenderer>(false);
                bool rawValue = baseItems.Length > 0 && baseItems[0].property_value.text == "57263";
                stage.ForceCjkFont();
                string baseShot = stage.Capture("Temp/role_panel_base.png");

                MethodInfo showPage = typeof(EquipmentView).GetMethod("ShowAttributePage", PrivateInstance);
                showPage.Invoke(equipment, new object[] { false });
                LayoutRebuilder.ForceRebuildLayoutImmediate(equipment._Scroller1.content);
                await Task.Delay(200);
                RolePropertyItemRenderer[] specialItems = equipment._Scroller1.content.GetComponentsInChildren<RolePropertyItemRenderer>(false);
                int specialCount = specialItems.Length;
                bool percentValue = specialItems.Length > 0 && specialItems[0].property_value.text == "12.34%";
                bool titles = !equipment._img_title_base.gameObject.activeSelf && equipment._img_title_best.gameObject.activeSelf;
                stage.ForceCjkFont();
                string specialShot = stage.Capture("Temp/role_panel_special.png");

                bool ok = baseCount == 13 && specialCount == 31 && rawValue && percentValue && titles;
                Debug.Log("CLIVERIFY rolesetting roleRuntime base=" + baseCount + " special=" + specialCount
                    + " raw=" + rawValue + " percent=" + percentValue + " titles=" + titles
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
