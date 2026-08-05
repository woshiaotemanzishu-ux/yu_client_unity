using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Skill;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// Read-only talent-page verification using the same three-tab common window shell as RoleFlow.
    /// </summary>
    public static class InnateViewCase
    {
        public static async Task<int> Run()
        {
            ResManager.EditorPreferFallback = true;
            CliVerify.Stage stage = CliVerify.Stage.Create();
            GameObject frameRoot = null;
            GameObject roleRoot = null;
            RoleModel role = RoleModel.Instance;
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await SkillConfigs.EnsureLoaded();
                await SkillUIConfigs.EnsureLoaded();
                if (!SkillUIConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY innateview FAIL ConfigSkillUI not loaded");
                    return 3;
                }

                role.Reset();
                role.RoleId = 4294967524L;
                role.Level = 630;
                role.Figure = new FigureProto
                {
                    name = "Talent verification role",
                    career = 1,
                    sex = 1,
                    level = 630,
                    turn = 4,
                };
                role.MarkBaseInfoReady();

                object skillController = SkillController.Instance;
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo on21010 = skillController.GetType().GetMethod("On21010", flags);
                if (on21010 == null)
                {
                    Debug.LogError("CLIVERIFY innateview handler missing (reflection): On21010");
                    return 3;
                }

                SkillManager.Instance.Clear();
                SkillTalentModel.Instance.Clear();

                const int lessPoint = 10;
                const int type5 = 5;
                const int point5 = 3;
                const int skillId = 59340001;
                const int skillLv = 2;
                byte[] packet = new CliVerify.Pkt()
                    .H(lessPoint).H(1)
                    .C(type5).H(point5).H(1).I(skillId).H(skillLv)
                    .Bytes();
                on21010.Invoke(skillController, new object[]
                {
                    new Shenxiao.Framework.Net.NetReader(packet, 0, packet.Length),
                });

                SkillTalentModel model = SkillTalentModel.Instance;
                bool dataOk = model.HasTalentInfo && model.LessPoint == lessPoint
                    && model.GetGroup(type5)?.Point == point5
                    && model.GetTalentLevel(skillId) == skillLv;
                Debug.Log("CLIVERIFY innateview 21010 dataOk=" + dataOk
                    + " lessPoint=" + model.LessPoint
                    + " point5=" + (model.GetGroup(type5)?.Point ?? -1)
                    + " skillLv=" + model.GetTalentLevel(skillId));

                GameObject framePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/UI/Common/BaseWindowSkin.prefab");
                GameObject rolePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/UI/Role/RoleModule.prefab");
                if (framePrefab == null || rolePrefab == null)
                {
                    Debug.LogError("CLIVERIFY innateview common shell or RoleModule.prefab missing");
                    return 3;
                }

                frameRoot = Object.Instantiate(framePrefab, ViewManager.GetLayer(UILayer.Window));
                roleRoot = Object.Instantiate(rolePrefab, ViewManager.GetLayer(UILayer.Window));
                foreach (Transform child in roleRoot.transform)
                    child.gameObject.SetActive(false);

                BaseWindowSkinView window = frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
                SkillInitiativeSubItem active = roleRoot.transform.Find("SkillInitiativeSubItem")
                    ?.GetComponent<SkillInitiativeSubItem>();
                SkillPassiveSubItem passive = roleRoot.transform.Find("SkillPassiveSubItem")
                    ?.GetComponent<SkillPassiveSubItem>();
                InnateSkillView talent = roleRoot.transform.Find("InnateSkillView")
                    ?.GetComponent<InnateSkillView>();
                if (window == null || active == null || passive == null || talent == null)
                {
                    Debug.LogError("CLIVERIFY innateview skill shell/pages incomplete");
                    return 3;
                }

                window.Show();
                window.Configure(new[]
                {
                    SkillTab("主动技能", active),
                    SkillTab("被动技能", passive),
                    SkillTab("天赋", talent),
                }, 2);
                await WaitUntil(() =>
                {
                    InnateInfoItem readyInfo = talent.GetComponentInChildren<InnateInfoItem>(true);
                    return readyInfo != null && readyInfo.Icon != null && readyInfo.Icon.sprite != null;
                }, 5d);
                await Task.Delay(500);
                stage.ForceCjkFont();

                TMPro.TextMeshProUGUI pointLabel = talent._lb_point;
                bool pointLabelOk = pointLabel != null && pointLabel.text == lessPoint.ToString();
                int expectedSlots = SkillUIConfigs.GetInnateSlots(type5, role.Career).Count;
                InnateSkillItem[] items = talent.GetComponentsInChildren<InnateSkillItem>(false);
                bool itemCountOk = expectedSlots > 0 && items.Length == expectedSlots;
                bool isolationOk = !active.gameObject.activeSelf && !passive.gameObject.activeSelf
                    && talent.gameObject.activeSelf;
                InnateTypeItemRenderer[] typeTabs = talent.GetComponentsInChildren<InnateTypeItemRenderer>(false);
                InnateTypeItemRenderer type8Tab = System.Array.Find(typeTabs, tab => tab.SkillType == 8);
                bool type8LabelOk = type8Tab != null && type8Tab.typeLb != null
                    && type8Tab.typeLb.text == "绝对";
                InnateInfoItem info = talent.GetComponentInChildren<InnateInfoItem>(true);
                RectTransform detailContent = info != null ? info.DecContainer : null;
                RectTransform detailViewport = detailContent != null ? detailContent.parent as RectTransform : null;
                ScrollRect detailScroll = detailViewport != null ? detailViewport.GetComponent<ScrollRect>() : null;
                bool detailScrollOk = detailContent != null && detailViewport != null
                    && detailViewport.GetComponent<RectMask2D>() != null
                    && detailContent.GetComponent<VerticalLayoutGroup>() != null
                    && detailContent.GetComponent<ContentSizeFitter>() != null
                    && detailScroll != null && detailScroll.content == detailContent
                    && detailScroll.viewport == detailViewport && detailScroll.vertical && !detailScroll.horizontal;
                bool detailIconOk = info != null && info.Icon != null && info.Icon.sprite != null
                    && (info.Mask == null || !info.Mask.enabled || info.Mask.color.a <= 0.001f);

                string png = stage.Capture(
                    "output/ui_route_audit/2026-08-04_role_web_round2/cli_innate_shell_20260804_2411/innateview_type5.png");
                Debug.Log("CLIVERIFY innateview render pointLabelOk=" + pointLabelOk
                    + "(text=" + (pointLabel?.text ?? "<null>") + ")"
                    + " itemCountOk=" + itemCountOk + " items=" + items.Length + "/" + expectedSlots
                    + " isolationOk=" + isolationOk + " type8LabelOk=" + type8LabelOk
                    + " detailScrollOk=" + detailScrollOk
                    + " detailIconOk=" + detailIconOk
                    + " shot=" + png);

                bool pass = dataOk && pointLabelOk && itemCountOk && isolationOk && detailScrollOk
                    && type8LabelOk && detailIconOk;
                Debug.Log("CLIVERIFY innateview VERDICT dataOk=" + dataOk
                    + " pointLabelOk=" + pointLabelOk
                    + " itemCountOk=" + itemCountOk
                    + " isolationOk=" + isolationOk
                    + " type8LabelOk=" + type8LabelOk
                    + " detailScrollOk=" + detailScrollOk
                    + " detailIconOk=" + detailIconOk
                    + " pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                if (frameRoot != null) Object.DestroyImmediate(frameRoot);
                if (roleRoot != null) Object.DestroyImmediate(roleRoot);
                SkillManager.Instance.Clear();
                SkillTalentModel.Instance.Clear();
                role.Reset();
                stage.Dispose();
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

        private static async Task<bool> WaitUntil(System.Func<bool> predicate, double timeoutSeconds)
        {
            double deadline = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                if (predicate()) return true;
                await Task.Delay(50);
            }
            return predicate();
        }
    }
}
