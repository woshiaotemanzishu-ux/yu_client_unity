using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 灵宠培养页(PetModule/OutWardBaseView)实证:直接读取人工接管的现有 prefab → 16002 合成包喂数据 →
    /// 渲染断言(阶数/星级/祝福/一键提升按钮/引导特效槽/伙伴模型加载) → 16023 升星包 →
    /// 断言事件驱动刷新(1阶2星)。本 Case 禁止调用 Creator 或配置生成器覆盖人工 Prefab/共享配置。
    /// 对标主线卡点 100190「剑魄同修培养到1阶2星」的页面闭环;页内引导手指依赖 TaskModel 主线态,留活服实证。
    /// 独立文件复用 CliVerify.Stage/Pkt/FindDeep,不改 CliVerify.cs 本体(主控统一接 RenderAll)。
    /// </summary>
    public static class PetTrainCase
    {
        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                await Shenxiao.Module.Core.OutWard.OutWardConfigs.EnsureLoaded();

                object ctrl = Shenxiao.Module.Core.OutWard.OutWardController.Instance;
                const System.Reflection.BindingFlags F =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                System.Reflection.MethodInfo m16002 = ctrl.GetType().GetMethod("On16002", F);
                System.Reflection.MethodInfo m16023 = ctrl.GetType().GetMethod("On16023", F);
                if (m16002 == null || m16023 == null)
                {
                    Debug.LogError("CLIVERIFY pettrain handlers missing (reflection)");
                    return 3;
                }
                void Feed(System.Reflection.MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                // 2) 16002:剑魄同修(type_id=2) 1阶1星 blessing=5 combat=2333
                byte[] p16002 = new CliVerify.Pkt()
                    .C(2).C(1).H(1).I(5).C(0).I(2333).L(0).C(0).H(0).H(0).Bytes();
                Feed(m16002, p16002);

                // 3) 实例化人工接管后的 PetModule,拉起培养页并切到同修
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Pet/PetModule.prefab");
                if (prefab == null)
                {
                    Debug.LogError("CLIVERIFY pettrain PetModule.prefab missing");
                    return 3;
                }
                GameObject go = Object.Instantiate(prefab, stage.CanvasRoot);
                var view = go.GetComponentInChildren<Shenxiao.Module.Core.Pet.OutWardBaseView>(true);
                if (view == null)
                {
                    Debug.LogError("CLIVERIFY pettrain OutWardBaseView missing in prefab");
                    return 3;
                }
                view.gameObject.SetActive(true);
                view.Show();
                view.SetType(2);   // 内部 SendFmt 16002/16028 未连接仅 warn,无害
                await Task.Delay(1200);
                stage.ForceCjkFont();

                const System.Reflection.BindingFlags VF =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var modelKeyField = typeof(Shenxiao.Module.Core.Pet.OutWardBaseView).GetField("_modelKey", VF);
                var modelStageField = typeof(Shenxiao.Module.Core.Pet.OutWardBaseView).GetField("_modelStage", VF);
                string modelKey = modelKeyField?.GetValue(view) as string;
                var modelStage = modelStageField?.GetValue(view) as Shenxiao.Common.UI3D.UIModelStage;
                modelStage?.RenderStageNow();
                bool modelOk = modelKey == "object/spirit/model_spirit_1001/model_spirit_1001"
                    && modelStage != null
                    && view.res != null
                    && view.res.GetComponentInChildren<UnityEngine.UI.RawImage>(true)?.texture != null;
                string modelDiagnostics = modelStage?.GetRenderDiagnostics() ?? "stage=null";

                bool stageOk = view.res_stage != null && view.res_stage.text == "1阶";
                bool nameOk = view.res_name != null && !string.IsNullOrEmpty(view.res_name.text); // config_mount_stage 阶名
                bool starOk = view.shadow != null && view.shadow.gameObject.activeSelf
                    && view.shadow0 != null && !view.shadow0.gameObject.activeSelf; // 1星:第1颗亮第2颗灭
                bool blessOk = view.level_value != null && view.level_value.text.StartsWith("5");
                bool btnOk = view.lv_button != null && view.lv_button.gameObject.activeInHierarchy
                    && view.lv_button_text != null && view.lv_button_text.text.Contains("一键提升");
                bool slotOk = view.lv_button.GetComponentsInChildren<Shenxiao.Common.UI3D.UIEffectSlot>(true).Length >= 2;
                bool autoBuyClickBound = view.autoGp != null
                    && view.autoGp.GetComponent<UnityEngine.UI.Button>() != null;
                bool crystalClickBound = view.crystal_group != null;
                var crystalItems = view.crystal_group != null
                    ? view.crystal_group.GetComponentsInChildren<Shenxiao.Generated.UI.Pet.PetRoundItemBind>(true)
                    : new Shenxiao.Generated.UI.Pet.PetRoundItemBind[0];
                crystalClickBound &= crystalItems.Length >= 3;
                for (int i = 0; i < crystalItems.Length; i++)
                {
                    crystalClickBound &= crystalItems[i].click_group != null
                        && crystalItems[i].click_group.GetComponent<UnityEngine.UI.Button>() != null;
                }
                string png1 = stage.Capture("Temp/pettrain_partner_1stage1star.png");
                Debug.Log("CLIVERIFY pettrain render stage=" + stageOk + "(" + (view.res_stage?.text ?? "-")
                    + ") name=" + nameOk + "(" + (view.res_name?.text ?? "-") + ") star=" + starOk
                    + " bless=" + blessOk + "(" + (view.level_value?.text ?? "-") + ") btn=" + btnOk
                    + " guideSlots=" + slotOk + " autoBuyClick=" + autoBuyClickBound
                    + " crystalClicks=" + crystalClickBound + " model=" + modelOk + " key=" + (modelKey ?? "-")
                    + " diagnostics=" + modelDiagnostics + " shot=" + png1);

                // 4) 16023 升星成功 → 1阶2星:EVT_OUTWARD_UPDATE 驱动视图刷新
                byte[] p16023Ok = new CliVerify.Pkt().I(1).C(2).C(1).H(2).I(10).I(0).L(0).C(0).H(0).Bytes();
                Feed(m16023, p16023Ok);
                await Task.Delay(200);
                bool star2Ok = view.shadow0 != null && view.shadow0.gameObject.activeSelf
                    && view.shadow1 != null && !view.shadow1.gameObject.activeSelf;
                string png2 = stage.Capture("Temp/pettrain_partner_1stage2star.png");
                Debug.Log("CLIVERIFY pettrain starUp → 2星 lit=" + star2Ok + " shot=" + png2);

                view.Hide();
                await Task.Delay(50);
                bool modelClearedOnHide = modelStage != null
                    && modelStage.GetRenderDiagnostics().Contains("model=False");

                bool pass = stageOk && nameOk && starOk && blessOk && btnOk && slotOk
                    && autoBuyClickBound && crystalClickBound && modelOk
                    && star2Ok && modelClearedOnHide;
                Debug.Log("CLIVERIFY pettrain VERDICT stage=" + stageOk + " name=" + nameOk + " star=" + starOk
                    + " bless=" + blessOk + " btn=" + btnOk + " slots=" + slotOk
                    + " autoBuyClick=" + autoBuyClickBound + " crystalClicks=" + crystalClickBound
                    + " model=" + modelOk
                    + " star2=" + star2Ok + " modelClearedOnHide=" + modelClearedOnHide + " pass=" + pass);

                Object.DestroyImmediate(go);
                Shenxiao.Module.Core.OutWard.OutWardModel.Instance.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }
    }
}
