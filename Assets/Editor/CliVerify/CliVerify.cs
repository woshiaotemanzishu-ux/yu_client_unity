using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 批处理 CLI 验证通道:无需 Unity MCP,用
    ///   Unity.exe -batchmode -projectPath . -executeMethod Shenxiao.EditorTools.CliVerify.XXX -logFile Temp/cliverify.log
    /// 驱动「编辑期真机渲染截图法」(同第 8~10 轮 RunCommand harness:临时 Canvas(ScreenSpaceCamera)+RenderTexture
    /// +LayerManager/ViewManager 初始化+CJK 字体强挂),把断言与截图写进 Temp/ 供外部核对。
    /// 结果行统一以 "CLIVERIFY" 前缀写日志;结束经 EditorApplication.Exit 返回进程码(0 过 / 1 异常 / 2 超时 / 3 断言失败)。
    /// 注意:-batchmode 不能加 -nographics(渲染需要 GPU 设备),不加 -quit(由 Exit 收尾)。
    /// </summary>
    public static class CliVerify
    {
        private const string FontPath = "Assets/_App/Fonts/FZYHJW SDF.asset";

        /// <summary>编译探针:域加载成功即 0(编译错时 executeMethod 根本不会被调用)。</summary>
        public static void CompileCheck()
        {
            Debug.Log("CLIVERIFY COMPILE OK");
            EditorApplication.Exit(0);
        }

        /// <summary>P2a 实证:TaskFinishView 用真实 config_task 货币+物品奖励任务渲染,验货币走图标格(非文本)。</summary>
        public static void RenderTaskFinish()
        {
            Run(RenderTaskFinishAsync, 240.0);
        }

        /// <summary>P2b 实证:ItemTipsView 带背包实例渲染,验「使用」按钮按 config use 字段显隐。</summary>
        public static void RenderItemTips()
        {
            Run(RenderItemTipsAsync, 240.0);
        }

        /// <summary>P1 实证:背包增量协议 15017/15018/15008/15009 —— 按 ClientProtocol.json 手工组大端合成包,
        /// 反射调 BagController 私有 handler,断言 BagModel 增/改/删/积分语义(纯逻辑,不渲染)。</summary>
        public static void ProtoDelta()
        {
            Run(() => Task.FromResult(ProtoDeltaCase()), 60.0);
        }

        /// <summary>P1(13轮)实证:TipsManager.Toast 浮动条渲染(多条顶推)+ 生命周期消亡。</summary>
        public static void RenderToast()
        {
            Run(RenderToastAsync, 240.0);
        }

        /// <summary>P1(14轮)实证:DoTask 主线类型覆盖——真实主线任务 + 服务端权威 tipsType,断言各分支日志走向。</summary>
        public static void DoTaskCoverage()
        {
            Run(DoTaskCoverageAsync, 120.0);
        }

        /// <summary>剑魄同修实证:config_companion 同步 + 14202/14205 合成包驱动 PartnerModel + PartnerShellView 渲染。</summary>
        public static void PartnerCase()
        {
            Run(PartnerCaseAsync, 240.0);
        }

        /// <summary>设置面板 + PK 模式链路实证(SettingCreator 重建 + 10202/13012 合成包 + 双视图渲染断言)。</summary>
        public static void SettingPk()
        {
            Run(SettingPkCase.Run, 300.0);
        }

        /// <summary>灵宠培养页链路实证(PetCreator 快照重建 + 16002/16023 合成包 + 渲染断言)。</summary>
        public static void PetTrain()
        {
            Run(PetTrainCase.Run, 300.0);
        }

        /// <summary>Goods 协议扩容(自动循环 轮1)实证:15000/15002/15019/15026/15053/15055/15090 合成包驱动
        /// BagController 反射喂包,纯逻辑断言(详见 GoodsProtoCase 注释)。</summary>
        public static void GoodsProto()
        {
            Run(GoodsProtoCase.Run, 120.0);
        }

        /// <summary>复活链(自动循环 队列#2 轮2)实证:20013/20004/20009/20017/20022/20027 合成包驱动
        /// FightController/ReliveController 反射喂包,纯逻辑断言(详见 ReliveCase 注释)。</summary>
        public static void Relive()
        {
            Run(ReliveCase.Run, 120.0);
        }

        /// <summary>技能成长线(自动循环 轮3)实证:21001/21010/21011/21012/13008/13010/12093/18401/20006 合成包驱动
        /// SkillController/FightController 反射喂包,纯逻辑断言(详见 SkillGrowthCase 注释)。</summary>
        public static void SkillGrowth()
        {
            Run(SkillGrowthCase.Run, 120.0);
        }

        /// <summary>天赋技能页(技能成长线轮3 3b 单)实证:InnateSkillCreator 装配 + 21010 合成包 + 渲染断言
        /// (_lb_point 文本/技能树 item 数,详见 InnateViewCase 注释)。</summary>
        public static void InnateView()
        {
            Run(InnateViewCase.Run, 300.0);
        }

        /// <summary>全部用例(一次 Unity 启动跑完;任一失败进程码非 0)。</summary>
        public static void RenderAll()
        {
            Run(async () =>
            {
                int p = ProtoDeltaCase();
                int d = await DoTaskCoverageAsync();
                int e = await PartnerCaseAsync();
                int s = await SuitCollectCase.Run();
                int g = await RushGiftCase.Run();
                int o = await OutWardCase.Run();
                int t = await TempleAwakenCase.Run();
                int q = await EquipStrenCase.Run();
                int u = await GuBaoCase.Run();
                int j = await GuildJoinCase.Run();
                int n = await RuneCase.Run();
                int v = await DungeonCase.Run();
                int w = await ThinSliceCase.Run();
                int f = await FinalSliceCase.Run();
                int a = await RenderTaskFinishAsync();
                int b = await RenderItemTipsAsync();
                int c = await RenderToastAsync();
                int k = await SettingPkCase.Run();
                int m = await PetTrainCase.Run();
                int gp = await GoodsProtoCase.Run();
                int rl = await ReliveCase.Run();
                int sg = await SkillGrowthCase.Run();
                int iv = await InnateViewCase.Run();
                Debug.Log("CLIVERIFY ALL protoDelta=" + p + " dotask=" + d + " partner=" + e
                    + " suitclt=" + s + " rushgift=" + g + " outward=" + o
                    + " templeawaken=" + t + " equipstren=" + q + " gubao=" + u
                    + " guildjoin=" + j + " rune=" + n + " dungeon=" + v + " thinslice=" + w + " finalslice=" + f
                    + " taskfinish=" + a + " itemtips=" + b + " toast=" + c + " settingpk=" + k + " pettrain=" + m
                    + " goodsproto=" + gp + " relive=" + rl + " skillgrowth=" + sg + " innateview=" + iv);
                foreach (int r in new[] { p, d, e, s, g, o, t, q, u, j, n, v, w, f, a, b, c, k, m, gp, rl, sg, iv })
                    if (r != 0) return r;
                return 0;
            }, 1500.0);
        }

        private static async Task<int> DoTaskCoverageAsync()
        {
            await TaskConfigs.EnsureLoaded();
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                // (真实主线任务 id, 服务端权威 tipsType(data_task.erl get_content), 期望分支日志)
                (int taskId, int tips, string expect)[] cases =
                {
                    (100940, 27, "DoTask degrade"),   // LV 到达等级 → 升级提醒未移植降级
                    (100980, 9,  "DungeonRuneShellView"), // FinDunType → 已接真实入口(第19轮;编辑期无层时「无法构建」同含类名)
                    (100330, 23, "PetFlow"), // TrainMount → 真页 MountPet 页签窗(本轮起 23/25/90 → PetFlow,壳仅剩 24/92/41)
                    (100010, 37, "Welcome(37) 无动作"), // 对标老端空 case
                    (999999, 99, "未知类型"),          // 不在主线清单 → 未知 blocker
                };
                bool all = true;
                foreach ((int taskId, int tips, string expect) c in cases)
                {
                    logs.Clear();
                    var vo = new TaskVo(c.taskId, c.tips, "", 0, 0, 1, 0, 1, 0, 0, 0, 0);
                    vo.ApplyConfig(TaskConfigs.Get(c.taskId));
                    TaskModel.Instance.DoTask(vo);
                    bool hit = logs.Exists(l => l.Contains(c.expect));
                    Debug.Log("CLIVERIFY dotask task=" + c.taskId + " tips=" + c.tips + " expect=[" + c.expect + "] hit=" + hit);
                    if (!hit) all = false;
                }
                return all ? 0 : 3;
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }
        }

        /// <summary>剑魄同修实证:config_companion 同步 + 14202(全量列表)/14205(培养成功/失败)合成包
        /// 反射喂 PartnerController 私有 handler,断言 PartnerModel 数据;再拉起 PartnerShellView 渲染断言
        /// 培养按钮显隐 + 行文案含真实"N阶M星"。</summary>
        private static async Task<int> PartnerCaseAsync()
        {
            Stage stage = Stage.Create();
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.Partner.PartnerConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.Partner.PartnerConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY FAIL config_companion not loaded");
                    return 3;
                }

                object ctrl = Shenxiao.Module.Core.Partner.PartnerController.Instance;
                const System.Reflection.BindingFlags F =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                System.Reflection.MethodInfo m14202 = ctrl.GetType().GetMethod("On14202", F);
                System.Reflection.MethodInfo m14205 = ctrl.GetType().GetMethod("On14205", F);
                if (m14202 == null || m14205 == null)
                {
                    Debug.LogError("CLIVERIFY partner handlers missing (reflection)");
                    return 3;
                }
                void Feed(System.Reflection.MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                Shenxiao.Module.Core.Partner.PartnerModel model = Shenxiao.Module.Core.Partner.PartnerModel.Instance;

                // 14202 全量:fight_id + sum_attr[] + companion_list[单项]
                byte[] p14202 = new Pkt()
                    .I(0)      // fight_id
                    .H(0)      // sum_attr 计数
                    .H(1)      // companion_list 计数
                        .I(1)      // companion_id
                        .H(1)      // stage
                        .H(1)      // star
                        .H(0)      // biog_list 计数
                        .C(1)      // is_active
                        .C(0)      // is_fight
                        .I(1018)   // figure_id
                        .I(0)      // blessing
                        .I(0)      // train_num
                        .H(0)      // attr 计数
                        .L(100)    // combat
                    .Bytes();
                Feed(m14202, p14202);
                bool listOk = model.HasData && model.Companions.Count == 1
                    && model.Get(1) != null && model.Get(1).Star == 1 && model.Get(1).Combat == 100;
                Debug.Log("CLIVERIFY partner 14202 hasData=" + model.HasData + " count=" + model.Companions.Count
                    + " star=" + (model.Get(1)?.Star ?? -1) + " combat=" + (model.Get(1)?.Combat ?? -1) + " ok=" + listOk);

                // 14205 培养成功:errcode=1 + companion_id + stage + star=2 + blessing=10(升星→内部会 SendFmt 14201,未连接只 warn,无害)
                byte[] p14205Ok = new Pkt().I(1).I(1).H(1).H(2).I(10).Bytes();
                Feed(m14205, p14205Ok);
                bool trainOk = model.Get(1) != null && model.Get(1).Star == 2 && model.Get(1).Blessing == 10;
                Debug.Log("CLIVERIFY partner 14205 ok star=" + (model.Get(1)?.Star ?? -1)
                    + " blessing=" + (model.Get(1)?.Blessing ?? -1) + " ok=" + trainOk);

                // 14205 培养失败:errcode=5,只要不抛异常(走 toast log 分支)即过
                byte[] p14205Fail = new Pkt().I(5).I(1).H(1).H(2).I(0).Bytes();
                bool failNoThrow = true;
                try { Feed(m14205, p14205Fail); }
                catch (System.Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY partner 14205 fail threw: " + e); }
                Debug.Log("CLIVERIFY partner 14205 fail noThrow=" + failNoThrow);

                Shenxiao.Module.Core.Partner.PartnerShellView.Show();
                await Task.Delay(400);
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round15_partner_shell.png");

                Transform trainBtn = FindDeep(stage.CanvasRoot, "Btn培养");
                bool trainBtnOk = trainBtn != null && trainBtn.gameObject.activeInHierarchy;
                Transform row0 = FindDeep(stage.CanvasRoot, "Row0");
                TMP_Text rowLabel = row0 != null ? row0.GetComponentInChildren<TMP_Text>(true) : null;
                bool rowOk = rowLabel != null && !string.IsNullOrEmpty(rowLabel.text) && rowLabel.text.Contains("1阶2星");
                Debug.Log("CLIVERIFY partner shell rowOk=" + rowOk + " trainBtn=" + trainBtnOk + " shot=" + png);

                bool pass = listOk && trainOk && failNoThrow && trainBtnOk && rowOk;
                Debug.Log("CLIVERIFY partner VERDICT listOk=" + listOk + " trainOk=" + trainOk
                    + " failNoThrow=" + failNoThrow + " trainBtnOk=" + trainBtnOk + " rowOk=" + rowOk + " pass=" + pass);

                Shenxiao.Module.Core.Partner.PartnerShellView.Close();
                Shenxiao.Module.Core.Partner.PartnerModel.Instance.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        // ---- 渲染用例 ----

        private static async Task<int> RenderTaskFinishAsync()
        {
            Stage stage = Stage.Create();
            try
            {
                await TaskConfigs.EnsureLoaded();
                await GoodsModel.EnsureLoaded();
                if (!TaskConfigs.IsLoaded || !GoodsModel.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY FAIL config not loaded: task=" + TaskConfigs.IsLoaded + " goods=" + GoodsModel.IsLoaded);
                    return 3;
                }

                // 真实任务 100520:award_list=[{5,0,2500000},{3,0,10000},{0,17020001,1},{0,17020004,1}]
                //  → 经验(5→32)/金币(3→31)两个货币 + 两件真实物品,共 4 格。
                const int taskId = 100520;
                TaskConfigs.TaskCfg cfg = TaskConfigs.Get(taskId);
                if (cfg == null)
                {
                    Debug.LogError("CLIVERIFY FAIL config_task missing " + taskId);
                    return 3;
                }

                var vo = new TaskVo(taskId, 0, "", 1, 0, 1, 1, 1, 0, 0, 0, 0);
                vo.ApplyConfig(cfg);
                List<TaskReward.Entry> rewards = TaskReward.Build(vo.SpecialGoodsList, vo.AwardList, 1);
                Debug.Log("CLIVERIFY rewards=" + rewards.Count + " -> " + TaskReward.ToText(rewards, " / "));

                var view = new TaskFinishView();
                view.Open(vo);

                // 等 prefab 实例化 + 奖励格生成 + 图标异步加载(编辑期兜底导入可能较慢)。
                EquipmentItem[] cells = await WaitCells(stage.CanvasRoot, rewards.Count, 90.0);
                await Task.Delay(3000);   // 再等几拍:逐帧实例化的尾部格子/迟到图标(排除截图早于建格的竞态)
                cells = stage.CanvasRoot.GetComponentsInChildren<EquipmentItem>(false);
                // 按格子 GameObject 去重断言:格数 == 奖励数,且每格恰 1 个 EquipmentItem
                // (>1 = 嵌套 prefab 自带 + 回填 added-override 历史重复件,BindClick 双注册回归)。
                var roots = new Dictionary<GameObject, int>();
                foreach (EquipmentItem c in cells)
                    roots[c.gameObject] = roots.TryGetValue(c.gameObject, out int n) ? n + 1 : 1;
                int dupCells = 0, iconOk = 0;
                foreach (KeyValuePair<GameObject, int> kv in roots)
                {
                    if (kv.Value > 1) dupCells++;
                    var bind = (Shenxiao.Generated.UI.Common.EquipmentItemBind)kv.Key.GetComponent<EquipmentItem>();
                    bool ok = bind.icon != null && bind.icon.enabled && bind.icon.sprite != null;
                    if (ok) iconOk++;
                    Debug.Log("CLIVERIFY cell path=" + HierarchyPath(kv.Key.transform) + " comps=" + kv.Value
                        + " icon=" + (ok ? bind.icon.sprite.name : "<none>")
                        + " count=" + (bind.num_text != null && bind.num_text.gameObject.activeSelf ? bind.num_text.text : "-"));
                }

                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round11_taskfinish_currency.png");
                Debug.Log("CLIVERIFY shot=" + png);

                bool pass = roots.Count == rewards.Count && rewards.Count >= 4 && iconOk == roots.Count && dupCells == 0;
                Debug.Log("CLIVERIFY VERDICT cells=" + roots.Count + "/" + rewards.Count + " iconOk=" + iconOk
                    + " dupCompCells=" + dupCells + " pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        private static async Task<int> RenderItemTipsAsync()
        {
            Stage stage = Stage.Create();
            try
            {
                await GoodsModel.EnsureLoaded();
                if (!GoodsModel.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY FAIL config_goods not loaded");
                    return 3;
                }

                // 真实 config 物品 520100(type 52 / use=1 → 走默认 15050 分支,使用按钮应显示)。
                const int typeId = 520100;
                GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
                if (basic == null || basic.Use == 0)
                {
                    Debug.LogError("CLIVERIFY FAIL 520100 缺失或 use==0: " + (basic == null ? "null" : basic.Use.ToString()));
                    return 3;
                }

                // 合成背包实例仅供渲染断言(GoodsId 非服务端真值,不点使用、不发协议);typeId/数量走真实 config 展示链路。
                var goods = new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 1, TypeId = typeId, GoodsNum = 3, Color = basic.Color };
                ItemTipsView.Show(goods);
                await Task.Delay(1500);   // 图标/底板异步加载

                Transform useBtn = FindDeep(stage.CanvasRoot, "Use");
                bool useVisible = useBtn != null && useBtn.gameObject.activeInHierarchy;
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round11_itemtips_use.png");
                Debug.Log("CLIVERIFY tips item=" + basic.Name + " useBtnVisible=" + useVisible + " shot=" + png);

                ItemTipsView.Close();
                return useVisible ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        private static async Task<int> RenderToastAsync()
        {
            Stage stage = Stage.Create();
            try
            {
                // prefab 版 TipToastView 由 MonoBehaviour.Update 驱动,编辑期 batchmode 无 player loop 不 tick
                // (实测第一条永卡 Born、队列不放行):本用例强制走代码建树兜底(Task.Yield 驱动,无头可跑);
                // prefab 路径的动画/排队以 2026-07-06 用户实测手调为准,不在无头断言范围。
                typeof(Shenxiao.Common.Tips.TipsManager)
                    .GetField("_prefabMissing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.SetValue(null, true);

                Shenxiao.Common.Tips.TipsManager.Toast("使用成功");
                Shenxiao.Common.Tips.TipsManager.Toast("获得V1体验卡x1");

                // TipToast prefab 化(2026-07-06)后条目=TipToastItem 克隆挂 ViewManager 层(不在 stage.CanvasRoot 下),
                // 且排队"同刻仅一条 Born"→第二条晚 ~bornDuration 入场:改为全场景数活动条目,轮询等两条同时在场。
                // 兼容 prefab 缺失的代码建树兜底(节点名 Toast)。
                static (int live, bool textOk) CountToasts()
                {
                    int n = 0;
                    bool ok = true;
                    foreach (var item in UnityEngine.Object.FindObjectsByType<Shenxiao.Common.Tips.TipToastItem>(FindObjectsSortMode.None))
                    {
                        n++;
                        TMP_Text tx = item.GetComponentInChildren<TMP_Text>(false);
                        if (tx == null || string.IsNullOrEmpty(tx.text)) ok = false;
                    }
                    foreach (var t in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                        if (t.gameObject.name == "Toast") { n++; if (string.IsNullOrEmpty(t.text)) ok = false; }
                    return (n, ok);
                }

                int live = 0;
                bool textOk = false;
                double bothDeadline = EditorApplication.timeSinceStartup + 3.0;
                while (EditorApplication.timeSinceStartup < bothDeadline)
                {
                    (live, textOk) = CountToasts();
                    if (live >= 2) break;
                    await Task.Delay(100);
                }
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round13_toast.png");
                Debug.Log("CLIVERIFY toast live=" + live + " textOk=" + textOk + " shot=" + png);

                // 生命周期:轮询到全部消亡(编辑期 tick 慢,给足余量)
                double deadline = EditorApplication.timeSinceStartup + 30.0;
                int remain = live;
                while (EditorApplication.timeSinceStartup < deadline)
                {
                    (remain, _) = CountToasts();
                    if (remain == 0) break;
                    await Task.Delay(300);
                }
                Debug.Log("CLIVERIFY toast expiredRemain=" + remain);

                bool pass = live == 2 && textOk && remain == 0;
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        // ---- 协议增量用例(合成包驱动) ----

        private static int ProtoDeltaCase()
        {
            var bag = Shenxiao.Module.Core.Bag.BagModel.Instance;
            bag.Clear();
            object ctrl = Shenxiao.Module.Core.Bag.BagController.Instance;
            const System.Reflection.BindingFlags F =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            System.Reflection.MethodInfo m17 = ctrl.GetType().GetMethod("On15017", F);
            System.Reflection.MethodInfo m18 = ctrl.GetType().GetMethod("On15018", F);
            System.Reflection.MethodInfo m08 = ctrl.GetType().GetMethod("On15008", F);
            System.Reflection.MethodInfo m09 = ctrl.GetType().GetMethod("On15009", F);
            if (m17 == null || m18 == null || m08 == null || m09 == null)
            {
                Debug.LogError("CLIVERIFY proto handlers missing (reflection)");
                return 3;
            }
            void Feed(System.Reflection.MethodInfo m, byte[] pkt) =>
                m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

            // 15017 新增(全字段项,嵌套数组空)
            Feed(m17, Goods17(4, 9001, 520100, 5));
            bool add = bag.BagGoodsList.Count == 1 && bag.BagGoodsList[0].GoodsNum == 5
                       && bag.BagGoodsList[0].TypeId == 520100 && bag.BagGoodsList[0].Cell == 7;
            // 15018 数量 5→2
            Feed(m18, Num18(4, 9001, 2, 520100));
            bool chg = bag.BagGoodsList.Count == 1 && bag.BagGoodsList[0].GoodsNum == 2;
            // 15018 num=0 删除
            Feed(m18, Num18(4, 9001, 0, 520100));
            bool del = bag.BagGoodsList.Count == 0;
            // 15008 特殊积分单条
            Feed(m08, new Pkt().I(1001).I(777).Bytes());
            bool score = bag.GetSpecialScore(1001) == 777;
            // 15009 全量重建(旧 777 应被清)
            Feed(m09, new Pkt().H(2).I(1001).I(5).I(2002).I(9).Bytes());
            bool scoreList = bag.SpecialScores.Count == 2 && bag.GetSpecialScore(1001) == 5 && bag.GetSpecialScore(2002) == 9;
            // 15017 非背包 pos → 跳过不落
            Feed(m17, Goods17(1, 9002, 520100, 3));
            bool skip = bag.BagGoodsList.Count == 0;

            // 15021 出售回包读序烟测(res=1 + 1 项所得;UI 层未起 → toast 走 log-only,不炸即过)
            System.Reflection.MethodInfo m21 = ctrl.GetType().GetMethod("On15021", F);
            bool sell = m21 != null;
            if (sell) Feed(m21, new Pkt().I(1).H(1).I(520100).I(3).Bytes());

            Debug.Log("CLIVERIFY proto delta add=" + add + " chg=" + chg + " del=" + del
                + " score=" + score + " scoreList=" + scoreList + " skipPos=" + skip + " sell21=" + sell);
            bag.Clear();
            return (add && chg && del && score && scoreList && skip && sell) ? 0 : 3;
        }

        /// <summary>15017 包:pos:h + 1 项全字段(字段序照 ClientProtocol.json "15010"/"15017",嵌套数组计数 0)。</summary>
        private static byte[] Goods17(int pos, long gid, int typeId, long num)
        {
            return new Pkt().H(pos).H(1)
                .L(gid).I(typeId).C(0).H(7).I(num)      // goods_id/type_id/sub_pos/cell=7/goods_num
                .C(0).C(0).C(0).C(0).C(3)               // bind/trade/sell/is_drop/color
                .I(0).I(0).H(0).H(0).I(0).I(0)          // expire/combat/stren/level/rating/overall
                .H(0)                                   // addition_attrlist[]
                .H(0)                                   // equip_extra_attr[]
                .C(0).C(0).I(0).C(0)                    // equipStage/equipStar/skill_id/skill_lv
                .H(0)                                   // awake_list[]
                .Bytes();
        }

        /// <summary>15018 包:pos:h + 1 项 {goods_id:l, goods_num:i, type_id:i}。</summary>
        private static byte[] Num18(int pos, long gid, long num, int typeId)
        {
            return new Pkt().H(pos).H(1).L(gid).I(num).I(typeId).Bytes();
        }

        /// <summary>大端手工组包(与 NetReader 字节序一致:h=u16, i=u32, l=两个 u32 拼 64, c=u8)。</summary>
        public sealed class Pkt
        {
            private readonly List<byte> _b = new List<byte>();
            public Pkt C(int v) { _b.Add((byte)v); return this; }
            public Pkt H(int v) { _b.Add((byte)(v >> 8)); _b.Add((byte)v); return this; }
            public Pkt I(long v) { _b.Add((byte)(v >> 24)); _b.Add((byte)(v >> 16)); _b.Add((byte)(v >> 8)); _b.Add((byte)v); return this; }
            public Pkt L(long v) { I((v >> 32) & 0xFFFFFFFF); I(v & 0xFFFFFFFF); return this; }
            /// <summary>'s':u16 字节长 + UTF8(对标 NetReader.ReadString / UserMsgAdapter 's' 格式)。</summary>
            public Pkt S(string v)
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(v ?? string.Empty);
                H(bytes.Length);
                _b.AddRange(bytes);
                return this;
            }
            public byte[] Bytes() { return _b.ToArray(); }
        }

        public static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        public static string HierarchyPath(Transform t)
        {
            var sb = new System.Text.StringBuilder(t.name);
            while (t.parent != null) { t = t.parent; sb.Insert(0, t.name + "/"); }
            return sb.ToString();
        }

        /// <summary>轮询场景中激活的奖励格(TaskFinishView 内部逐帧实例化+异步图标)。</summary>
        private static async Task<EquipmentItem[]> WaitCells(Transform root, int expect, double timeoutSec)
        {
            double deadline = EditorApplication.timeSinceStartup + timeoutSec;
            EquipmentItem[] found = Array.Empty<EquipmentItem>();
            while (EditorApplication.timeSinceStartup < deadline)
            {
                found = root.GetComponentsInChildren<EquipmentItem>(false);
                if (found.Length >= expect)
                {
                    bool allIcons = true;
                    foreach (EquipmentItem c in found)
                    {
                        var bind = (Shenxiao.Generated.UI.Common.EquipmentItemBind)c;
                        if (bind.icon == null || !bind.icon.enabled || bind.icon.sprite == null) { allIcons = false; break; }
                    }
                    if (allIcons) return found;
                }
                await Task.Delay(200);
            }
            return found;
        }

        // ---- 驱动与舞台 ----

        /// <summary>batch 模式 async 驱动:EditorApplication.update 泵到任务完成/超时,经 Exit 返回进程码。</summary>
        private static void Run(Func<Task<int>> body, double timeoutSec)
        {
            Task<int> task = null;
            double deadline = EditorApplication.timeSinceStartup + timeoutSec;
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                try
                {
                    if (task == null) task = body();
                    if (task.IsCompleted)
                    {
                        EditorApplication.update -= tick;
                        int code = task.IsFaulted ? 1 : task.Result;
                        if (task.IsFaulted) Debug.LogError("CLIVERIFY EXCEPTION " + task.Exception);
                        Debug.Log("CLIVERIFY EXIT " + code);
                        EditorApplication.Exit(code);
                    }
                    else if (EditorApplication.timeSinceStartup > deadline)
                    {
                        EditorApplication.update -= tick;
                        Debug.LogError("CLIVERIFY TIMEOUT");
                        EditorApplication.Exit(2);
                    }
                }
                catch (Exception e)
                {
                    EditorApplication.update -= tick;
                    Debug.LogError("CLIVERIFY EXCEPTION " + e);
                    EditorApplication.Exit(1);
                }
            };
            EditorApplication.update += tick;
        }

        /// <summary>临时渲染舞台(空场景 + 720×1280 RenderTexture 相机 + ScreenSpaceCamera Canvas + 层/视图管理器)。</summary>
        public sealed class Stage : IDisposable
        {
            public Transform CanvasRoot => _canvas.transform;

            private Camera _cam;
            private Canvas _canvas;
            private RenderTexture _rt;

            public static Stage Create()
            {
                // batch 域 Addressables 操作不推进(KeyExists 挂死)→ 兜底优先(AssetDatabase 同步命中)。
                Shenxiao.Framework.Res.ResManager.EditorPreferFallback = true;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var s = new Stage();
                s._rt = new RenderTexture(720, 1280, 24);

                var camGo = new GameObject("CliVerifyCam");
                s._cam = camGo.AddComponent<Camera>();
                s._cam.clearFlags = CameraClearFlags.SolidColor;
                s._cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
                s._cam.targetTexture = s._rt;

                var canvasGo = new GameObject("CliVerifyCanvas");
                s._canvas = canvasGo.AddComponent<Canvas>();
                s._canvas.renderMode = RenderMode.ScreenSpaceCamera;
                s._canvas.worldCamera = s._cam;
                s._canvas.planeDistance = 1f;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(720, 1280);
                canvasGo.AddComponent<GraphicRaycaster>();

                var lm = new LayerManager();
                lm.Init(s._canvas);
                ViewManager.Init(lm);
                return s;
            }

            /// <summary>batch 域没有场景字体环境,渲染前把 CJK SDF 强挂到全部 TMP 文本(同第 8~10 轮做法)。</summary>
            public void ForceCjkFont()
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                if (font == null)
                {
                    Debug.LogWarning("CLIVERIFY font missing: " + FontPath);
                    return;
                }
                foreach (TMP_Text t in _canvas.GetComponentsInChildren<TMP_Text>(true))
                {
                    t.font = font;
                    t.ForceMeshUpdate();
                }
            }

            public string Capture(string projectRelativePng)
            {
                Canvas.ForceUpdateCanvases();
                _cam.Render();
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = _rt;
                var tex = new Texture2D(_rt.width, _rt.height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;

                string full = Path.GetFullPath(projectRelativePng);
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllBytes(full, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                return full;
            }

            public void Dispose()
            {
                ViewManager.Init(null);
                if (_cam != null) { _cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(_cam.gameObject); }
                if (_canvas != null) UnityEngine.Object.DestroyImmediate(_canvas.gameObject);
                if (_rt != null) UnityEngine.Object.DestroyImmediate(_rt);
            }
        }
    }
}
