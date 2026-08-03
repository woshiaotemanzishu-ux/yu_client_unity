using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Setting;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 设置面板 + PK(战斗)模式链路 CLI 实证(无服务器,合成包驱动):
    ///  ① 直接加载当前人工维护的 SettingModule.prefab，禁止测试重建覆盖视觉事实源;
    ///  ② config_scene requirement.pkstate_list 解析(7001→[1] / 10103→[0,1,2]);
    ///  ③ 13012 回包 → RoleModel.PkStatus / 和平冷却 / 错误码不炸;
    ///  ④ 10202 合成包 → SettingModel 落库;
    ///  ⑤ 渲染 SettingView:四滑条克隆、自动拾取 3 项、屏蔽列表 10 项、任务勾选块、页签切换,双截图;
    ///  ⑥ 渲染 MainUIFightModeView:场景 10103 → 3 个模式项 + 当前模式高亮,截图。
    /// 入口:CliVerify.SettingPk(-executeMethod Shenxiao.EditorTools.CliVerify.SettingPk)。
    /// </summary>
    public static class SettingPkCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            Shenxiao.Framework.Res.ResManager.EditorPreferFallback = true;

            // ① 当前 Prefab 是唯一视觉事实源；验收只读加载，不调用历史 Creator。
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Setting/SettingModule.prefab");
            bool creatorOk = prefab != null && prefab.GetComponentInChildren<SettingView>(true) != null
                && prefab.GetComponentInChildren<SettingChangeHeadView>(true) != null
                && prefab.GetComponentInChildren<SettingChangeNameView>(true) != null;
            Debug.Log("CLIVERIFY settingpk creator ok=" + creatorOk);

            // ② 场景 pkstate_list 解析
            await MainUIConfigs.EnsureSceneLoaded();
            MainUIConfigs.SceneCfg s7001 = MainUIConfigs.GetSceneCfg(7001);
            MainUIConfigs.SceneCfg s10103 = MainUIConfigs.GetSceneCfg(10103);
            bool pkCfgOk = s7001 != null && s7001.PkStateList.Length == 1 && s7001.PkStateList[0] == 1
                && s10103 != null && s10103.PkStateList.Length == 3
                && s10103.PkStateList[0] == 0 && s10103.PkStateList[2] == 2;
            Debug.Log("CLIVERIFY settingpk pkCfg 7001=[" + string.Join(",", s7001?.PkStateList ?? new int[0])
                + "] 10103=[" + string.Join(",", s10103?.PkStateList ?? new int[0]) + "] ok=" + pkCfgOk);

            // ③ 13012 回包(ici):成功切到 2 / 冷却 30s / 错误码 5 不炸
            RoleModel.Instance.Reset();
            MethodInfo m13012 = PkStatusController.Instance.GetType().GetMethod("On13012", F);
            void FeedPk(byte[] pkt) => m13012.Invoke(PkStatusController.Instance,
                new object[] { new NetReader(pkt, 0, pkt.Length) });
            FeedPk(new CliVerify.Pkt().I(1).C(2).I(0).Bytes());
            bool pkChangeOk = RoleModel.Instance.PkStatus == 2 && !RoleModel.Instance.PeaceCdActive;
            FeedPk(new CliVerify.Pkt().I(1).C(0).I(30).Bytes());
            bool pkCdOk = RoleModel.Instance.PeaceCdActive && RoleModel.Instance.PkStatus == 2;
            bool pkErrNoThrow = true;
            try { FeedPk(new CliVerify.Pkt().I(5).C(0).I(0).Bytes()); }
            catch (System.Exception e) { pkErrNoThrow = false; Debug.LogError("CLIVERIFY settingpk 13012 err threw: " + e); }
            RoleModel.Instance.SetPeaceCd(0);
            Debug.Log("CLIVERIFY settingpk 13012 change=" + pkChangeOk + " cd=" + pkCdOk + " errNoThrow=" + pkErrNoThrow);

            // ④ 10202 合成包 → SettingModel(滑条 6/7/9/12 + 拾取 17/18/19 + 任务/降神/坐骑 + 屏蔽 10 项)
            SettingModel.Reset();
            (int sub, int val)[] entries =
            {
                (6, 5), (7, 8), (9, 50), (12, 50),
                (17, 1), (18, 1), (19, 1),
                (21, 1), (201, 1), (202, 1), (8, 0),
                (1, 0), (2, 1), (3, 1), (10, 0), (14, 1), (20, 0), (22, 0), (25, 1), (5, 0), (26, 0),
            };
            var pkt10202 = new CliVerify.Pkt().C(3).H(entries.Length);
            foreach ((int sub, int val) e in entries) pkt10202.H(e.sub).C(e.val); // 服务端 item_to_bin_4:Subtype:16, IsOpen:8
            byte[] b10202 = pkt10202.Bytes();
            MethodInfo m10202 = GameStartController.Instance.GetType().GetMethod("On10202", F);
            m10202.Invoke(GameStartController.Instance, new object[] { new NetReader(b10202, 0, b10202.Length) });
            bool settingDataOk = SettingModel.Get(3, 9, -1) == 50 && SettingModel.Get(3, 6, -1) == 5
                && SettingModel.Get(3, 17, -1) == 1 && SettingModel.Get(3, 21, -1) == 1;
            Debug.Log("CLIVERIFY settingpk 10202 dataOk=" + settingDataOk);

            // ⑤ 渲染设置面板
            CliVerify.Stage stage = CliVerify.Stage.Create();
            bool slidersOk, pickOk, shieldOk, taskOk;
            try
            {
                await SettingConfigs.EnsureLoaded();
                GameObject root = Object.Instantiate(prefab, ViewManager.GetLayer(UILayer.Window));
                foreach (Transform c in root.transform) c.gameObject.SetActive(false);
                SettingView main = root.GetComponentInChildren<SettingView>(true);
                main.Show();
                await Task.Delay(2500); // RefreshAsync(配置加载)+ 克隆滑条 + 异步贴图

                var sliders = root.GetComponentsInChildren<WithBtnHSlider>(false);
                slidersOk = sliders.Length == 4;

                int pickCount = CountActiveShieldItems(main._list_pick != null ? main._list_pick.content : null);
                pickOk = pickCount == 3;
                taskOk = main._box_task != null && main._box_task.gameObject.activeInHierarchy;

                stage.ForceCjkFont();
                string shot1 = stage.Capture("Temp/settingpk_base.png");

                MethodInfo selectTab = main.GetType().GetMethod("SelectTab", F);
                selectTab.Invoke(main, new object[] { false });
                await Task.Delay(300);
                int shieldCount = CountActiveShieldItems(main._list_shield != null ? main._list_shield.content : null);
                shieldOk = main._box_shield_setting.gameObject.activeInHierarchy && shieldCount == 10;

                stage.ForceCjkFont();
                string shot2 = stage.Capture("Temp/settingpk_shield.png");
                Debug.Log("CLIVERIFY settingpk view sliders=" + sliders.Length + " pick=" + pickCount
                    + " shield=" + shieldCount + " task=" + taskOk + " shots=" + shot1 + " | " + shot2);

                main.Hide(); // 触发 OnHide 上报路径(未连接只 warn,不应抛)
                Object.DestroyImmediate(root);
            }
            finally
            {
                stage.Dispose();
            }

            // ⑥ 渲染 PK 模式弹窗(场景 10103 → 0/1/2 三项,当前 2 高亮)
            CliVerify.Stage stage2 = CliVerify.Stage.Create();
            bool fightModeOk;
            try
            {
                RoleModel.Instance.SceneId = 10103;
                RoleModel.Instance.PkStatus = 2;
                GameObject fmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MainUI/MainUIFightModeView.prefab");
                GameObject fmRoot = Object.Instantiate(fmPrefab, ViewManager.GetLayer(UILayer.Window));
                MainUIFightModeView fm = fmRoot.GetComponentInChildren<MainUIFightModeView>(true);
                fm.Show();
                await Task.Delay(1200);

                int items = 0, selected = 0;
                foreach (MainUIFightModeItem it in fmRoot.GetComponentsInChildren<MainUIFightModeItem>(false))
                {
                    items++;
                    var bind = (Shenxiao.Generated.UI.MainUI.MainUIFightModeItemBind)it;
                    if (bind._img_select != null && bind._img_select.gameObject.activeSelf) selected++;
                }
                fightModeOk = items == 3 && selected == 1;
                stage2.ForceCjkFont();
                string shot3 = stage2.Capture("Temp/settingpk_fightmode.png");
                Debug.Log("CLIVERIFY settingpk fightmode items=" + items + " selected=" + selected
                    + " ok=" + fightModeOk + " shot=" + shot3);
                Object.DestroyImmediate(fmRoot);
            }
            finally
            {
                stage2.Dispose();
            }

            SettingModel.Reset();
            RoleModel.Instance.Reset();

            bool pass = creatorOk && pkCfgOk && pkChangeOk && pkCdOk && pkErrNoThrow && settingDataOk
                && slidersOk && pickOk && shieldOk && taskOk && fightModeOk;
            Debug.Log("CLIVERIFY settingpk VERDICT creator=" + creatorOk + " pkCfg=" + pkCfgOk
                + " pkChange=" + pkChangeOk + " pkCd=" + pkCdOk + " pkErr=" + pkErrNoThrow
                + " data=" + settingDataOk + " sliders=" + slidersOk + " pick=" + pickOk
                + " shield=" + shieldOk + " task=" + taskOk + " fightmode=" + fightModeOk + " pass=" + pass);
            return pass ? 0 : 3;
        }

        private static int CountActiveShieldItems(Transform content)
        {
            if (content == null) return -1;
            int n = 0;
            foreach (SettingShieldItem it in content.GetComponentsInChildren<SettingShieldItem>(false))
            {
                if (it.gameObject.activeSelf) n++;
            }
            return n;
        }
    }
}
