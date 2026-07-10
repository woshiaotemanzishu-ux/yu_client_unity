using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 天赋技能页(InnateSkillView)实证:InnateSkillCreator 装配 RoleModule.prefab → 21010 合成包驱动
    /// SkillTalentModel/SkillUIConfigs 真实数据 → 实例化 RoleModule → 找到 InnateSkillView 并 Show() →
    /// 断言 _lb_point 文本(剩余天赋点)与技能树 item 数(对标 type5 真实配置槽位数)→ 截图。
    /// 独立文件复用 CliVerify.Stage/Pkt(同 PetTrainCase 套路),不改 CliVerify.cs 本体(主控统一接 RenderAll)。
    /// </summary>
    public static class InnateViewCase
    {
        public static async Task<int> Run()
        {
            // 1) 装配(幂等:已提升过会自己跳过,见 InnateSkillCreator 注释)
            Shenxiao.Editor.UiCreator.Role.InnateSkillCreator.Generate();

            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.Skill.SkillConfigs.EnsureLoaded();
                await Shenxiao.Module.Core.Skill.SkillUIConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.Skill.SkillUIConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY innateview FAIL ConfigSkillUI not loaded");
                    return 3;
                }

                object skillCtrl = Shenxiao.Module.Core.Skill.SkillController.Instance;
                const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo m21010 = skillCtrl.GetType().GetMethod("On21010", F);
                if (m21010 == null)
                {
                    Debug.LogError("CLIVERIFY innateview handler missing (reflection): On21010");
                    return 3;
                }

                Shenxiao.Module.Core.Skill.SkillManager.Instance.Clear();
                Shenxiao.Module.Core.Skill.SkillTalentModel.Instance.Clear();

                // 2) 21010:剩余点10 + type5(攻击)分支 point=3,已学一枚真实配置技能(59340001 lv=2)
                const int lessPoint = 10;
                const int type5 = 5;
                const int point5 = 3;
                const int skillId = 59340001; // 真实 configskillui.json innateSkill["5"]["1"][0]
                const int skillLv = 2;
                byte[] pkt = new CliVerify.Pkt()
                    .H(lessPoint).H(1)
                        .C(type5).H(point5).H(1).I(skillId).H(skillLv)
                    .Bytes();
                m21010.Invoke(skillCtrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                var model = Shenxiao.Module.Core.Skill.SkillTalentModel.Instance;
                bool dataOk = model.HasTalentInfo && model.LessPoint == lessPoint
                    && model.GetGroup(type5)?.Point == point5 && model.GetTalentLevel(skillId) == skillLv;
                Debug.Log("CLIVERIFY innateview 21010 dataOk=" + dataOk + " lessPoint=" + model.LessPoint
                    + " point5=" + (model.GetGroup(type5)?.Point ?? -1) + " skillLv=" + model.GetTalentLevel(skillId));

                // 3) 实例化装配后的 RoleModule,拉起 InnateSkillView
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Role/RoleModule.prefab");
                if (prefab == null)
                {
                    Debug.LogError("CLIVERIFY innateview RoleModule.prefab missing");
                    return 3;
                }
                GameObject go = Object.Instantiate(prefab, stage.CanvasRoot);
                var view = go.GetComponentInChildren<Shenxiao.Module.Core.Role.InnateSkillView>(true);
                if (view == null)
                {
                    Debug.LogError("CLIVERIFY innateview InnateSkillView missing in RoleModule.prefab" +
                        "(InnateSkillCreator 未成功提升为顶层内容视图,先查该 Creator 日志)");
                    Object.DestroyImmediate(go);
                    return 3;
                }
                view.gameObject.SetActive(true);
                view.Show();
                await Task.Delay(300);
                stage.ForceCjkFont();

                TMPro.TextMeshProUGUI lbPoint = view._lb_point;
                bool pointLabelOk = lbPoint != null && lbPoint.text == lessPoint.ToString();

                int expectSlots = Shenxiao.Module.Core.Skill.SkillUIConfigs.GetInnateSlots(type5, RoleModel().Career).Count;
                var items = go.GetComponentsInChildren<Shenxiao.Module.Core.Role.InnateSkillItem>(false); // 只数激活的(隐藏模板不算)
                bool itemCountOk = expectSlots > 0 && items.Length == expectSlots;

                string png = stage.Capture("Temp/innateview_type5.png");
                Debug.Log("CLIVERIFY innateview render pointLabelOk=" + pointLabelOk + "(text=" + (lbPoint?.text ?? "<null>") + ")"
                    + " itemCountOk=" + itemCountOk + " items=" + items.Length + "/" + expectSlots + " shot=" + png);

                bool pass = dataOk && pointLabelOk && itemCountOk;
                Debug.Log("CLIVERIFY innateview VERDICT dataOk=" + dataOk + " pointLabelOk=" + pointLabelOk
                    + " itemCountOk=" + itemCountOk + " pass=" + pass);

                Object.DestroyImmediate(go);
                Shenxiao.Module.Core.Skill.SkillTalentModel.Instance.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        private static Shenxiao.Module.Core.Role.RoleModel RoleModel() => Shenxiao.Module.Core.Role.RoleModel.Instance;
    }
}
