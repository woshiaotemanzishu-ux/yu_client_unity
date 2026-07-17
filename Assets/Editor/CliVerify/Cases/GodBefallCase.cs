using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 谪仙临凡(GodBefall,自动循环 轮18 便宜活批 PK1)实证:pt_440.erl 全 16 号(44000-44018,44007-44009
    /// 空号,0 死号),合成包驱动 GodBefallController 反射喂包,断言 GodBefallModel 落地字段/事件 + config
    /// 8 表计数(模板 MarriageCase/KfBossCase,纯逻辑段,同 15a/15b Boss、轮16 Marriage 先例不接 View)。
    ///
    /// 重点覆盖:44000 二层嵌套(GodList[EquipList])3 只神格/不同装备数含 0 件边界;44001 单只推送
    /// 原地覆盖(已存在)与插入(不存在)两路径;44002/44005 成功后自动补发 44001(RequestItem 内部
    /// SendFmt,断言 noThrow);44003/44004/44005 局部字段更新不动其余字段 + 失败分支不覆盖(B3边界);
    /// 44005 Star **32位**(勿与 Lv/Grade 16位混淆);44006 出战整表 IsBattle 清0重置;44011 成功后自动
    /// 重发 44010;44012 成功分支镜像老端但服务端现状不可达(仅解码不崩溃);44013 成功=ack+44001双反馈
    /// (本用例只验证ack路径,44001联动由44001用例覆盖);44014/44018 恒记录结果(成功失败都覆盖,44018
    /// StrongGodDic 仅成功联动);44016 GoodsList 嵌套数组仅成功落地、失败不清空已有数据(B3边界);44015/
    /// 44017 无 Code 恒交付。注册线核实直接反射 NetManager._handlers 核对 16 号全部真实挂上(同
    /// CustomActCoreCase 先例,不仅仅反射能调到方法体)。
    /// </summary>
    public static class GodBefallCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.GodBefall.GodBefallConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.GodBefall.GodBefallConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY godbefall FAIL GodBefallConfigs not loaded");
                    return 3;
                }
                bool configOk = Shenxiao.Module.Core.GodBefall.GodBefallConfigs.GodCount == 10
                    && Shenxiao.Module.Core.GodBefall.GodBefallConfigs.EquipCount == 128
                    && Shenxiao.Module.Core.GodBefall.GodBefallConfigs.LvCount == 1010
                    && Shenxiao.Module.Core.GodBefall.GodBefallConfigs.StageCount == 110
                    && Shenxiao.Module.Core.GodBefall.GodBefallConfigs.StarCount == 59
                    && Shenxiao.Module.Core.GodBefall.GodBefallConfigs.KvCount == 6
                    && Shenxiao.Module.Core.GodBefall.GodBefallConfigs.StarUpLimitCount == 5
                    && Shenxiao.Module.Core.GodBefall.GodBefallConfigs.StrenCount == 2404;
                Debug.Log("CLIVERIFY godbefall config god=" + Shenxiao.Module.Core.GodBefall.GodBefallConfigs.GodCount
                    + " equip=" + Shenxiao.Module.Core.GodBefall.GodBefallConfigs.EquipCount
                    + " lv=" + Shenxiao.Module.Core.GodBefall.GodBefallConfigs.LvCount
                    + " stage=" + Shenxiao.Module.Core.GodBefall.GodBefallConfigs.StageCount
                    + " star=" + Shenxiao.Module.Core.GodBefall.GodBefallConfigs.StarCount
                    + " kv=" + Shenxiao.Module.Core.GodBefall.GodBefallConfigs.KvCount
                    + " starUpLimit=" + Shenxiao.Module.Core.GodBefall.GodBefallConfigs.StarUpLimitCount
                    + " stren=" + Shenxiao.Module.Core.GodBefall.GodBefallConfigs.StrenCount + " ok=" + configOk);

                Shenxiao.Module.Core.GodBefall.GodBefallModel model = Shenxiao.Module.Core.GodBefall.GodBefallModel.Instance;
                model.Reset();

                object ctrl = Shenxiao.Module.Core.GodBefall.GodBefallController.Instance;
                System.Type t = ctrl.GetType();
                bool anyThrew = false;
                void Feed(string method, byte[] pkt)
                {
                    MethodInfo m = t.GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY godbefall handler missing: " + method); anyThrew = true; return; }
                    try
                    {
                        m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });
                    }
                    catch (System.Exception e)
                    {
                        anyThrew = true;
                        Debug.LogError("CLIVERIFY godbefall " + method + " threw: " + e);
                    }
                }

                // ---- 0. 注册线核实:Init() 后 44000-44018(16号)必须真的挂进 NetManager(同 CustomActCoreCase
                // 先例,不仅仅反射能调到方法体)。 ----
                var baseCtrl = (Shenxiao.Framework.Net.BaseController)ctrl;
                if (!baseCtrl.IsInitialized) baseCtrl.Init();
                FieldInfo handlersField = typeof(Shenxiao.Framework.Net.NetManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Static);
                var handlers = handlersField?.GetValue(null) as System.Collections.IDictionary;
                int[] mustBeRegistered =
                {
                    Shenxiao.Framework.Net.Proto.GODBEFALL_LIST, Shenxiao.Framework.Net.Proto.GODBEFALL_ITEM_PUSH,
                    Shenxiao.Framework.Net.Proto.GODBEFALL_ACTIVATE, Shenxiao.Framework.Net.Proto.GODBEFALL_LEVEL_UP,
                    Shenxiao.Framework.Net.Proto.GODBEFALL_GRADE_UP, Shenxiao.Framework.Net.Proto.GODBEFALL_STAR_UP,
                    Shenxiao.Framework.Net.Proto.GODBEFALL_SET_BATTLE, Shenxiao.Framework.Net.Proto.GODBEFALL_SWITCH_CD,
                    Shenxiao.Framework.Net.Proto.GODBEFALL_SWITCH, Shenxiao.Framework.Net.Proto.GODBEFALL_EQUIP_WEAR,
                    Shenxiao.Framework.Net.Proto.GODBEFALL_EQUIP_TAKEOFF, Shenxiao.Framework.Net.Proto.GODBEFALL_QUICK_SYNTHESIS,
                    Shenxiao.Framework.Net.Proto.GODBEFALL_POWER_PREVIEW, Shenxiao.Framework.Net.Proto.GODBEFALL_SMART_SYNTHESIS,
                    Shenxiao.Framework.Net.Proto.GODBEFALL_TYPE_PANEL, Shenxiao.Framework.Net.Proto.GODBEFALL_TYPE_STRENGTHEN,
                };
                bool bRegistered = handlers != null;
                var missingReg = new List<int>();
                if (handlers != null)
                {
                    foreach (int id in mustBeRegistered)
                    {
                        if (!handlers.Contains(id)) { bRegistered = false; missingReg.Add(id); }
                    }
                }
                Debug.Log("CLIVERIFY godbefall 注册线核实(NetManager._handlers,16号) missing=[" + string.Join(",", missingReg) + "] ok=" + bRegistered);

                // ---- 结果事件采集(EVT_GODBEFALL_RESULT) ----
                var resultLog = new List<(int protoId, int code)>();
                System.Action<int, int> onResult = (pid, code) => resultLog.Add((pid, code));
                Shenxiao.Framework.Event.EventDispatcher.On<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_GODBEFALL_RESULT, onResult);
                var updateLog = new List<long>();
                System.Action<long> onUpdate = godId => updateLog.Add(godId);
                Shenxiao.Framework.Event.EventDispatcher.On<long>(Shenxiao.Framework.Event.GlobalEvent.EVT_GODBEFALL_UPDATE, onUpdate);

                // ---- A. 44000 边界:空列表(先测,避免污染后续正式数据) ----
                Feed("On44000", new CliVerify.Pkt().H(0).Bytes());
                bool b44000Empty = model.HasGodList && model.GodList.Count == 0 && model.CurrentBattleId == 0;
                Debug.Log("CLIVERIFY godbefall 44000 空列表边界 hasList=" + model.HasGodList + " count=" + model.GodList.Count + " ok=" + b44000Empty);

                // ---- B. 44000 正式:二层嵌套 3 只神格,装备数 1/0/2 三种边界 ----
                byte[] p44000 = new CliVerify.Pkt()
                    .H(3)
                        .C(1).I(3001).H(10).I(500).H(2).I(3).L(1000000).L(1100000).L(1200000).L(1300000)
                            .H(1).C(1).L(7001101)
                        .C(0).I(3002).H(5).I(100).H(1).I(1).L(200000).L(0).L(0).L(0)
                            .H(0)
                        .C(0).I(3003).H(1).I(0).H(0).I(0).L(1).L(0).L(0).L(0)
                            .H(2).C(1).L(100).C(2).L(200)
                    .Bytes();
                Feed("On44000", p44000);
                var g3001 = model.GetGod(3001);
                var g3002 = model.GetGod(3002);
                var g3003 = model.GetGod(3003);
                bool b44000 = model.HasGodList && model.GodList.Count == 3
                    && g3001 != null && g3001.IsBattle == 1 && g3001.Lv == 10 && g3001.Exp == 500 && g3001.Grade == 2
                    && g3001.Star == 3 && g3001.Power == 1000000 && g3001.NextLvPower == 1100000
                    && g3001.EquipList.Count == 1 && g3001.EquipList[0].Pos == 1 && g3001.EquipList[0].GoodsId == 7001101
                    && g3002 != null && g3002.IsBattle == 0 && g3002.EquipList.Count == 0
                    && g3003 != null && g3003.EquipList.Count == 2
                    && g3003.EquipList[0].Pos == 1 && g3003.EquipList[0].GoodsId == 100
                    && g3003.EquipList[1].Pos == 2 && g3003.EquipList[1].GoodsId == 200
                    && model.CurrentBattleId == 3001;
                Debug.Log("CLIVERIFY godbefall 44000 二层嵌套(3只/装备1-0-2) count=" + model.GodList.Count
                    + " g3001.equipN=" + (g3001?.EquipList.Count ?? -1) + " g3003.equipN=" + (g3003?.EquipList.Count ?? -1)
                    + " battleId=" + model.CurrentBattleId + " ok=" + b44000);

                // ---- C. 44001 单只推送:已存在原地覆盖 / 不存在插入 ----
                Feed("On44001", new CliVerify.Pkt().C(1).I(3001).H(11).I(600).H(2).I(3).L(1000001).L(0).L(0).L(0).H(0).Bytes());
                bool b44001Update = model.GetGod(3001).Lv == 11 && model.GetGod(3001).Exp == 600 && model.GodList.Count == 3;
                Feed("On44001", new CliVerify.Pkt().C(0).I(4001).H(1).I(0).H(0).I(0).L(1).L(0).L(0).L(0).H(0).Bytes());
                bool b44001Insert = model.GodList.Count == 4 && model.GetGod(4001) != null && model.GetGod(4001).Power == 1;
                bool b44001 = b44001Update && b44001Insert;
                Debug.Log("CLIVERIFY godbefall 44001 单只推送(覆盖+插入) lv3001=" + model.GetGod(3001).Lv
                    + " count=" + model.GodList.Count + " ok=" + b44001);

                // ---- D. 44002 激活(quirk 直写 CurrentBattleId + 自动补发44001,noThrow) ----
                bool b44002NoThrow = true;
                try { Feed("On44002", new CliVerify.Pkt().I(1).L(50000).I(5001).Bytes()); }
                catch (System.Exception e) { b44002NoThrow = false; Debug.LogError("CLIVERIFY godbefall 44002 threw: " + e); }
                bool b44002Success = model.CurrentBattleId == 5001;
                try { Feed("On44002", new CliVerify.Pkt().I(1720401).L(0).I(5002).Bytes()); }
                catch (System.Exception e) { b44002NoThrow = false; Debug.LogError("CLIVERIFY godbefall 44002 fail threw: " + e); }
                bool b44002FailNotOverwritten = model.CurrentBattleId == 5001;
                bool b44002 = b44002NoThrow && b44002Success && b44002FailNotOverwritten;
                Debug.Log("CLIVERIFY godbefall 44002 激活(quirk+自动44001) battleId=" + model.CurrentBattleId
                    + " noThrow=" + b44002NoThrow + " ok=" + b44002);

                // ---- E. 44003 升级(局部字段,Grade/Star/EquipList不动) + 失败边界 ----
                Feed("On44003", new CliVerify.Pkt().I(1).I(3001).H(12).I(700).L(2000000).L(2100000).L(2200000).L(2300000).Bytes());
                bool b44003 = model.GetGod(3001).Lv == 12 && model.GetGod(3001).Exp == 700 && model.GetGod(3001).Power == 2000000
                    && model.GetGod(3001).Grade == 2 && model.GetGod(3001).EquipList.Count == 0; // 未被44003触碰的字段仍是最后一次整记录写入(C段44001覆盖)的值:该包 EquipList=空,44001 整记录替换是正确镜像语义(首跑期望值订正:原断言==1 误取 B段44000 的旧值,产品代码零问题)
                Feed("On44003", new CliVerify.Pkt().I(1720003).I(3001).H(99).I(99).L(99).L(99).L(99).L(99).Bytes());
                bool b44003fail = model.GetGod(3001).Lv == 12;
                Debug.Log("CLIVERIFY godbefall 44003 升级(局部更新) lv=" + model.GetGod(3001).Lv + " ok=" + b44003 + " failNotOverwritten=" + b44003fail);

                // ---- F. 44004 升阶(局部字段) + 失败边界 ----
                Feed("On44004", new CliVerify.Pkt().I(1).I(3001).H(3).L(3000000).L(3100000).L(3200000).L(3300000).Bytes());
                bool b44004 = model.GetGod(3001).Grade == 3 && model.GetGod(3001).Power == 3000000 && model.GetGod(3001).Lv == 12;
                Feed("On44004", new CliVerify.Pkt().I(1720004).I(3001).H(99).L(99).L(99).L(99).L(99).Bytes());
                bool b44004fail = model.GetGod(3001).Grade == 3;
                Debug.Log("CLIVERIFY godbefall 44004 升阶(局部更新) grade=" + model.GetGod(3001).Grade + " ok=" + b44004 + " failNotOverwritten=" + b44004fail);

                // ---- G. 44005 升星(⚠Star恒32位,局部字段,自动补发44001 noThrow) + 失败边界 ----
                bool b44005NoThrow = true;
                try { Feed("On44005", new CliVerify.Pkt().I(1).I(3001).I(4).L(4000000).L(4100000).L(4200000).L(4300000).Bytes()); }
                catch (System.Exception e) { b44005NoThrow = false; Debug.LogError("CLIVERIFY godbefall 44005 threw: " + e); }
                bool b44005 = model.GetGod(3001).Star == 4 && model.GetGod(3001).Power == 4000000;
                Feed("On44005", new CliVerify.Pkt().I(1720005).I(3001).I(99).L(99).L(99).L(99).L(99).Bytes());
                bool b44005fail = model.GetGod(3001).Star == 4;
                Debug.Log("CLIVERIFY godbefall 44005 升星(Star32位+自动44001) star=" + model.GetGod(3001).Star
                    + " noThrow=" + b44005NoThrow + " ok=" + b44005 + " failNotOverwritten=" + b44005fail);

                // ---- H. 44006 出战(整表IsBattle清0重置) + 失败边界 ----
                Feed("On44006", new CliVerify.Pkt().I(1).I(3002).Bytes());
                bool b44006 = model.GetGod(3002).IsBattle == 1 && model.GetGod(3001).IsBattle == 0 && model.CurrentBattleId == 3002;
                Feed("On44006", new CliVerify.Pkt().I(1720006).I(9999).Bytes());
                bool b44006fail = model.CurrentBattleId == 3002 && model.GetGod(3002).IsBattle == 1;
                Debug.Log("CLIVERIFY godbefall 44006 出战(整表清0) battleId=" + model.CurrentBattleId + " ok=" + b44006 + " failNotOverwritten=" + b44006fail);

                // ---- I. 44010 变身CD(裸,无Code) ----
                Feed("On44010", new CliVerify.Pkt().I(123).I(1700000000).Bytes());
                bool b44010 = model.SwitchCd != null && model.SwitchCd.SwitchCd == 123 && model.SwitchCd.EndTime == 1700000000;
                Debug.Log("CLIVERIFY godbefall 44010 变身CD(裸) switchCd=" + (model.SwitchCd?.SwitchCd ?? -1) + " ok=" + b44010);

                // ---- J. 44011 切变身(B1修复:不再quirk直写battleId,仅补发44010,noThrow) + 失败边界 ----
                bool b44011NoThrow = true;
                try { Feed("On44011", new CliVerify.Pkt().I(1).I(3003).Bytes()); }
                catch (System.Exception e) { b44011NoThrow = false; Debug.LogError("CLIVERIFY godbefall 44011 threw: " + e); }
                bool b44011Success = model.CurrentBattleId == 3002; // B1:44011 不再写 battleId,应保持 H 段(44006)遗留值不变
                try { Feed("On44011", new CliVerify.Pkt().I(1720011).I(0).Bytes()); }
                catch (System.Exception e) { b44011NoThrow = false; Debug.LogError("CLIVERIFY godbefall 44011 fail threw: " + e); }
                bool b44011FailNotOverwritten = model.CurrentBattleId == 3002;
                bool b44011 = b44011NoThrow && b44011Success && b44011FailNotOverwritten;
                Debug.Log("CLIVERIFY godbefall 44011 切变身(不写battleId,保持H段值) battleId=" + model.CurrentBattleId + " ok=" + b44011);

                // ---- K. 44012 穿戴神装(Code单字段,成功分支老端镜像/服务端现状不可达但仍需能安全解码) ----
                Feed("On44012", new CliVerify.Pkt().I(1).Bytes());
                Feed("On44012", new CliVerify.Pkt().I(1720012).Bytes());
                bool b44012 = resultLog.FindAll(x => x.protoId == Shenxiao.Framework.Net.Proto.GODBEFALL_EQUIP_WEAR).Count == 2;
                Debug.Log("CLIVERIFY godbefall 44012 穿戴神装(单字段) resultEvents=" + b44012);

                // ---- L. 44013 卸下神装(Code单字段,成功=ack) ----
                Feed("On44013", new CliVerify.Pkt().I(1).Bytes());
                Feed("On44013", new CliVerify.Pkt().I(1720013).Bytes());
                bool b44013 = resultLog.FindAll(x => x.protoId == Shenxiao.Framework.Net.Proto.GODBEFALL_EQUIP_TAKEOFF).Count == 2;
                Debug.Log("CLIVERIFY godbefall 44013 卸下神装(单字段) resultEvents=" + b44013);

                // ---- M. 44014 快速合成(恒记录,成功失败都覆盖) ----
                Feed("On44014", new CliVerify.Pkt().I(1).I(81000101).L(555000123).Bytes());
                bool b44014 = model.LastQuickSynthesis != null && model.LastQuickSynthesis.Code == 1
                    && model.LastQuickSynthesis.RuleId == 81000101 && model.LastQuickSynthesis.GoodsId == 555000123;
                Feed("On44014", new CliVerify.Pkt().I(1720014).I(81000102).L(0).Bytes());
                bool b44014fail = model.LastQuickSynthesis.Code == 1720014 && model.LastQuickSynthesis.RuleId == 81000102;
                Debug.Log("CLIVERIFY godbefall 44014 快速合成(恒记录) code=" + model.LastQuickSynthesis.Code + " ok=" + b44014 + " overwrittenByFail=" + b44014fail);

                // ---- N. 44015 战力预览(无Code) ----
                Feed("On44015", new CliVerify.Pkt().I(5005).L(9999999).Bytes());
                bool b44015 = model.LastPowerPreview != null && model.LastPowerPreview.GodId == 5005 && model.LastPowerPreview.Power == 9999999;
                Debug.Log("CLIVERIFY godbefall 44015 战力预览(无Code) godId=" + (model.LastPowerPreview?.GodId ?? -1) + " ok=" + b44015);

                // ---- O. 44016 智能合成(嵌套数组探针,仅成功落地,失败不清空) ----
                byte[] p44016ok = new CliVerify.Pkt().I(1).H(2).C(1).L(7001101).C(3).C(2).L(7001102).C(1).Bytes();
                Feed("On44016", p44016ok);
                bool b44016 = model.LastSmartSynthesisRewards.Count == 2
                    && model.LastSmartSynthesisRewards[0].GoodsType == 1 && model.LastSmartSynthesisRewards[0].GoodsTypeId == 7001101 && model.LastSmartSynthesisRewards[0].GoodsNum == 3
                    && model.LastSmartSynthesisRewards[1].GoodsType == 2 && model.LastSmartSynthesisRewards[1].GoodsTypeId == 7001102 && model.LastSmartSynthesisRewards[1].GoodsNum == 1;
                Feed("On44016", new CliVerify.Pkt().I(1720016).H(0).Bytes());
                bool b44016fail = model.LastSmartSynthesisRewards.Count == 2; // 失败不清空
                Debug.Log("CLIVERIFY godbefall 44016 智能合成(嵌套数组) rewardN=" + model.LastSmartSynthesisRewards.Count + " ok=" + b44016 + " failNotCleared=" + b44016fail);

                // ---- P. 44017 神格强化界面(无Code) ----
                Feed("On44017", new CliVerify.Pkt().C(3).H(5).I(2000).Bytes());
                bool b44017 = model.GetStrongGod(3) != null && model.GetStrongGod(3).CurrentLv == 5 && model.GetStrongGod(3).CurrentExp == 2000;
                Debug.Log("CLIVERIFY godbefall 44017 神格强化界面(无Code) lv=" + (model.GetStrongGod(3)?.CurrentLv ?? -1) + " ok=" + b44017);

                // ---- Q. 44018 神格强化提交(恒记录结果,仅成功联动StrongGodDic) ----
                byte[] p44018ok = new CliVerify.Pkt().I(1).S("").C(3).H(6).I(2500).C(1).Bytes();
                Feed("On44018", p44018ok);
                bool b44018 = model.GetStrongGod(3).CurrentLv == 6 && model.GetStrongGod(3).CurrentExp == 2500
                    && model.LastTypeStrengthen.Code == 1 && model.LastTypeStrengthen.Args == "" && model.LastTypeStrengthen.IsDivide == 1;
                byte[] p44018fail = new CliVerify.Pkt().I(1720018).S("err_msg").C(3).H(0).I(0).C(0).Bytes();
                Feed("On44018", p44018fail);
                bool b44018fail = model.GetStrongGod(3).CurrentLv == 6 // 失败不联动StrongGodDic
                    && model.LastTypeStrengthen.Code == 1720018 && model.LastTypeStrengthen.Args == "err_msg" && model.LastTypeStrengthen.IsDivide == 0; // 结果恒覆盖
                Debug.Log("CLIVERIFY godbefall 44018 神格强化提交(恒记录+仅成功联动) lv=" + model.GetStrongGod(3).CurrentLv
                    + " ok=" + b44018 + " failNotLinked=" + b44018fail);

                Shenxiao.Framework.Event.EventDispatcher.Off<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_GODBEFALL_RESULT, onResult);
                Shenxiao.Framework.Event.EventDispatcher.Off<long>(Shenxiao.Framework.Event.GlobalEvent.EVT_GODBEFALL_UPDATE, onUpdate);

                // ---- R. C2S 发送侧全量 noThrow(含44016/44018两个自定义WriteFmt变长构包) ----
                bool sendNoThrow = true;
                try
                {
                    var c = (Shenxiao.Module.Core.GodBefall.GodBefallController)ctrl;
                    c.RequestGodList();
                    c.RequestItem(3001);
                    c.RequestActivate(3001);
                    c.RequestLevelUp(3001);
                    c.RequestGradeUp(3001);
                    c.RequestStarUp(3001);
                    c.RequestSetBattle(1, 3001);
                    c.RequestSwitchCd();
                    c.RequestSwitch();
                    c.RequestEquipWear(999888777, 3001);
                    c.RequestEquipTakeoff(3001, 1);
                    c.RequestQuickSynthesis(81000101, 999888777);
                    c.RequestPowerPreview(3001);
                    c.RequestSmartSynthesis(new List<(long ruleId, int count)> { (81000101, 1), (81000102, 2) });
                    c.RequestSmartSynthesis(null); // 空列表边界(h=0,不追加元素)
                    c.RequestTypePanel(3);
                    c.RequestTypeStrengthen(3, new List<(long goodsTypeId, int goodsNum)> { (7110001, 10) }, true);
                    c.RequestTypeStrengthen(4, null, false); // 空列表边界
                }
                catch (System.Exception e)
                {
                    sendNoThrow = false;
                    Debug.LogError("CLIVERIFY godbefall C2S send threw: " + e);
                }
                Debug.Log("CLIVERIFY godbefall C2S 全量发送(含44016/44018变长构包) noThrow=" + sendNoThrow);

                bool pass = configOk && bRegistered && !anyThrew
                    && b44000Empty && b44000 && b44001 && b44002
                    && b44003 && b44003fail && b44004 && b44004fail && b44005NoThrow && b44005 && b44005fail
                    && b44006 && b44006fail && b44010 && b44011
                    && b44012 && b44013 && b44014 && b44014fail && b44015
                    && b44016 && b44016fail && b44017 && b44018 && b44018fail
                    && sendNoThrow;

                Debug.Log("CLIVERIFY godbefall VERDICT config=" + configOk + " registered=" + bRegistered + " anyThrew=" + anyThrew
                    + " l44000=" + (b44000Empty && b44000) + " l44001=" + b44001 + " l44002=" + b44002
                    + " l44003=" + (b44003 && b44003fail) + " l44004=" + (b44004 && b44004fail) + " l44005=" + (b44005 && b44005fail)
                    + " l44006=" + (b44006 && b44006fail) + " l44010=" + b44010 + " l44011=" + b44011
                    + " l44012=" + b44012 + " l44013=" + b44013 + " l44014=" + (b44014 && b44014fail) + " l44015=" + b44015
                    + " l44016=" + (b44016 && b44016fail) + " l44017=" + b44017 + " l44018=" + (b44018 && b44018fail)
                    + " send=" + sendNoThrow + " pass=" + pass);

                model.Reset();
                return pass ? 0 : 3;
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }
        }
    }
}
