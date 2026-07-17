using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 市场/交易行(151xx补全)实证(自动循环 轮19)。反射喂包 MarketController 全 16 号新增协议
    /// (15100/15101/15102/15106/15108/15109/15111/15112/15114/15115-15119/15120/15122;既有 15121
    /// 图标逻辑仅做一次轻量回归),断言 MarketModel 落地字段/嵌套数组(15102/15109/15112 EquipExtraAttr
    /// 二层嵌套探针,15118 ServerNum:64 独例探针)、EVT_MARKET_UPDATE/EVT_MARKET_RESULT 事件、死号
    /// (15103/15104/15105/15107/15110/15113)禁注册反射断言、注册线核实(NetManager._handlers 直查,
    /// 同 CustomActCoreCase/WelfareCase 先例)。15106/15108/15111 成功后重发镜像(15109/15109/15114)
    /// 用 "send while disconnected: proto=X" 日志计数验证(CliVerify 全程不建立真实连接,NetManager.
    /// IsConnected 恒 false,此日志是发送确已发生的可靠副作用探针,同 FbFestivalCase/WelfareCase 先例)。
    /// 15117 vs 15116 的成功分支摘除列表数量不对称(15117 只摘 All,15116 两个都摘)、15122 成功分支
    /// 为空(老端 ts:299-307 无数据处理)均按 TS 原文逐字镜像并显式断言,防止"伪quirk"臆造。本轮不搬
    /// config(config_goods_sell* 4 张表留 UI 尾包),故无 config 计数段。日志前缀 "CLIVERIFY market"。
    /// UI 层(24个 view 文件)未移植,本轮数据层only。
    /// 自动循环 轮19 minor 清尾追加两条数据层行为探针:①15102 wire 故意逆序发(id=1002 排 1001 前)断言
    /// 落地后仍按 id 升序排[镜像老端 ts:143-146 table.sort];②SetSellGoodsInfo 写前清空整个
    /// _sellGoodsDic[镜像老端 ts:139 单桶语义]断言陈旧桶被清空。15106/15108/15111/15115/15116/15117
    /// 新增成功 toast(TipsManager.Toast)不加断言——CliVerify 全程无头无 UI,toast 只落 GameLog,
    /// 与既有 ShowError 显码探针同理不重复断言。
    /// </summary>
    public static class MarketCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                Shenxiao.Module.Core.Market.MarketModel model = Shenxiao.Module.Core.Market.MarketModel.Instance;
                model.Reset();
                object ctrl = Shenxiao.Module.Core.Market.MarketController.Instance;
                var typed = (Shenxiao.Module.Core.Market.MarketController)ctrl;
                System.Type type = ctrl.GetType();

                void Feed(string method, byte[] pkt)
                {
                    MethodInfo m = type.GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY market handler missing: " + method); return; }
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });
                }
                List<string> CaptureLogs(System.Action body)
                {
                    var l = new List<string>();
                    Application.LogCallback lcb = (msg, stack, t) => l.Add(msg);
                    Application.logMessageReceived += lcb;
                    try { body(); }
                    finally { Application.logMessageReceived -= lcb; }
                    return l;
                }
                int CountProto(List<string> l, int protoId)
                {
                    string needle = "proto=" + protoId;
                    int n = 0;
                    foreach (string s in l) if (s.Contains(needle)) n++;
                    return n;
                }

                // ---- 0. 死号(15103/15104/15105/15107/15110/15113)禁注册反射断言 ----
                int[] deadNums = { 15103, 15104, 15105, 15107, 15110, 15113 };
                bool deadOk = true;
                var deadHits = new List<int>();
                foreach (int num in deadNums)
                {
                    if (type.GetMethod("On" + num, F) != null) { deadOk = false; deadHits.Add(num); }
                }
                Debug.Log("CLIVERIFY market 死号禁注册 checked=" + deadNums.Length + " hits=[" + string.Join(",", deadHits) + "] ok=" + deadOk);

                // ---- 1. 注册线核实(Init() 后必须真的挂进 NetManager._handlers;17 活号在,6 死号不在) ----
                var baseCtrl = (Shenxiao.Framework.Net.BaseController)ctrl;
                if (!baseCtrl.IsInitialized) baseCtrl.Init();
                FieldInfo handlersField = typeof(Shenxiao.Framework.Net.NetManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Static);
                var handlers = handlersField?.GetValue(null) as System.Collections.IDictionary;
                int[] mustBeRegistered =
                {
                    Shenxiao.Framework.Net.Proto.MARKET_ICON_INFO, Shenxiao.Framework.Net.Proto.MARKET_ERROR_PUSH,
                    Shenxiao.Framework.Net.Proto.MARKET_LEVEL1_LIST, Shenxiao.Framework.Net.Proto.MARKET_GOODS_LIST,
                    Shenxiao.Framework.Net.Proto.MARKET_SELL_UP, Shenxiao.Framework.Net.Proto.MARKET_SELL_DOWN,
                    Shenxiao.Framework.Net.Proto.MARKET_SHELF_LIST, Shenxiao.Framework.Net.Proto.MARKET_BUY,
                    Shenxiao.Framework.Net.Proto.MARKET_RECORD_LIST, Shenxiao.Framework.Net.Proto.MARKET_BUY_TIMES,
                    Shenxiao.Framework.Net.Proto.MARKET_PLZ_CREATE, Shenxiao.Framework.Net.Proto.MARKET_PLZ_CANCEL,
                    Shenxiao.Framework.Net.Proto.MARKET_PLZ_SELL, Shenxiao.Framework.Net.Proto.MARKET_PLZ_LIST_ALL,
                    Shenxiao.Framework.Net.Proto.MARKET_PLZ_LIST_MINE, Shenxiao.Framework.Net.Proto.MARKET_SELL_DELETE_PUSH,
                    Shenxiao.Framework.Net.Proto.MARKET_SHOUT,
                };
                bool regOk = true;
                var missingReg = new List<int>();
                if (handlers != null)
                {
                    foreach (int id in mustBeRegistered) if (!handlers.Contains(id)) { regOk = false; missingReg.Add(id); }
                    foreach (int id in deadNums) if (handlers.Contains(id)) { regOk = false; missingReg.Add(-id); } // 负数标记"不该在却在"
                }
                else regOk = false;
                Debug.Log("CLIVERIFY market 注册线核实(NetManager._handlers) missing/unexpected=[" + string.Join(",", missingReg) + "] ok=" + regOk);

                // ---- 2. 15121 既有图标逻辑轻量回归(一行未改,仅验证未被本轮扩展破坏) ----
                byte[] p15121 = new CliVerify.Pkt().I(0).Bytes();
                Feed("On15121", p15121);
                bool b15121 = !model.KfOpen && model.GetShowIconType() == Shenxiao.Module.Core.Market.MarketController.ICON_TYPE_LOCAL;
                Debug.Log("CLIVERIFY market 15121 回归 kfOpen=" + model.KfOpen + " ok=" + b15121);

                // ---- 3. 15101 一级分类挂单数量 ----
                byte[] p15101 = new CliVerify.Pkt().I(5).H(2).I(100).I(3).I(200).I(7).Bytes();
                Feed("On15101", p15101);
                var goodsDic5 = model.GetGoodsDic(5);
                bool b15101 = goodsDic5 != null && goodsDic5.Count == 2
                    && goodsDic5[0].Subtype == 100 && goodsDic5[0].SellNum == 3
                    && goodsDic5[1].Subtype == 200 && goodsDic5[1].SellNum == 7;
                Debug.Log("CLIVERIFY market 15101 一级分类挂单数量 listN=" + (goodsDic5?.Count ?? -1) + " ok=" + b15101);

                // ---- 4. 15102 二级列表商品(9字段+EquipExtraAttr二层嵌套探针:1条2元素+1条0元素;
                //      wire 故意把 id 更大的 1002 排在 1001 前面发送,断言排序后 1001 仍落在 [0]——
                //      证明 On15102 落 Model 前确实按 id 升序排序[镜像老端 ts:143-146 table.sort],
                //      而非单纯"wire 顺序恰好就是升序") ----
                byte[] p15102 = new CliVerify.Pkt().I(5).I(100)
                    .H(2)
                        .L(1002).L(9002).I(38010002).I(2).I(101).I(201).I(5001).C(0)
                            .H(0)
                        .L(1001).L(9001).I(38010001).I(1).I(100).I(200).I(5000).C(1)
                            .H(2)
                                .C(1).C(2).H(300).I(400).C(5).I(600)
                                .C(2).C(3).H(301).I(401).C(6).I(601)
                    .Bytes();
                Feed("On15102", p15102);
                var goodsList5_100 = model.GetSellGoodsInfo(5, 100);
                bool b15102Sorted = goodsList5_100 != null && goodsList5_100.Count == 2
                    && goodsList5_100[0].Id < goodsList5_100[1].Id; // wire 逆序进,落地后须已排正
                bool b15102 = goodsList5_100 != null && goodsList5_100.Count == 2
                    && goodsList5_100[0].Id == 1001 && goodsList5_100[0].PlayerId == 9001 && goodsList5_100[0].TypeId == 38010001
                    && goodsList5_100[0].UnitPrice == 5000 && goodsList5_100[0].SellType == 1
                    && goodsList5_100[0].EquipExtraAttr.Count == 2
                    && goodsList5_100[0].EquipExtraAttr[0].Color == 1 && goodsList5_100[0].EquipExtraAttr[0].AttrId == 300
                    && goodsList5_100[0].EquipExtraAttr[0].AttrVal == 400 && goodsList5_100[0].EquipExtraAttr[0].PlusUnit == 600
                    && goodsList5_100[0].EquipExtraAttr[1].AttrId == 301
                    && goodsList5_100[1].Id == 1002 && goodsList5_100[1].EquipExtraAttr.Count == 0 // 0元素嵌套游标不炸
                    && b15102Sorted;
                Debug.Log("CLIVERIFY market 15102 二级列表商品(嵌套+排序探针) listN=" + (goodsList5_100?.Count ?? -1)
                    + " attrN0=" + (goodsList5_100?[0].EquipExtraAttr.Count ?? -1) + " sorted(wire逆序进)=" + b15102Sorted + " ok=" + b15102);

                // ---- 4b. SetSellGoodsInfo 写前清空整桶(镜像老端 ts:139 sell_goods_dic_=[] 单桶语义) ----
                // 先在另一个 subtype(999)桶垫一条陈旧数据,再对 (5,100) 触发一次新的 15102 写入,
                // 断言陈旧桶 (5,999) 被整体清空为 null、当前桶 (5,100) 正常写入——本轮既有断言
                // (b15102 系列)只覆盖单一桶写入场景,不与本条新增的清空语义冲突,故无需回退订正。
                model.SetSellGoodsInfo(5, 999, new List<Shenxiao.Module.Core.Market.MarketModel.GoodsEntry>
                {
                    new Shenxiao.Module.Core.Market.MarketModel.GoodsEntry { Id = 424242, EquipExtraAttr = new List<Shenxiao.Module.Core.Market.MarketModel.EquipExtraAttrEntry>() },
                });
                Feed("On15102", p15102); // 复用同一个包再喂一次,只关心清空副作用
                bool b15102ClearWholeDict = model.GetSellGoodsInfo(5, 999) == null && model.GetSellGoodsInfo(5, 100) != null
                    && model.GetSellGoodsInfo(5, 100).Count == 2;
                Debug.Log("CLIVERIFY market 15102 SetSellGoodsInfo写前清空整桶(单桶语义探针) staleBucketCleared="
                    + (model.GetSellGoodsInfo(5, 999) == null) + " ok=" + b15102ClearWholeDict);

                // ---- 5. 15106 上架:成功重发15109镜像 / 失败(1500001)只显码不重发 ----
                int marketResultCount = 0; int lastResultProto = -1; int lastResultCode = -1;
                var resultProtoIds = new List<int>();
                System.Action<int, int> onMarketResult = (proto, code) => { marketResultCount++; lastResultProto = proto; lastResultCode = code; resultProtoIds.Add(proto); };
                Shenxiao.Framework.Event.EventDispatcher.On<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_MARKET_RESULT, onMarketResult);

                byte[] p15106ok = new CliVerify.Pkt().I(1).Bytes();
                List<string> logs15106ok = CaptureLogs(() => Feed("On15106", p15106ok));
                bool b15106Mirror = CountProto(logs15106ok, Shenxiao.Framework.Net.Proto.MARKET_SHELF_LIST) == 1;
                byte[] p15106fail = new CliVerify.Pkt().I(1500001).Bytes();
                List<string> logs15106fail = CaptureLogs(() => Feed("On15106", p15106fail));
                bool b15106FailNoMirror = CountProto(logs15106fail, Shenxiao.Framework.Net.Proto.MARKET_SHELF_LIST) == 0
                    && logs15106fail.Exists(l => l.Contains("错误(1500001)"));
                bool b15106 = b15106Mirror && b15106FailNoMirror
                    && marketResultCount == 2 && resultProtoIds[0] == Shenxiao.Framework.Net.Proto.MARKET_SELL_UP
                    && resultProtoIds[1] == Shenxiao.Framework.Net.Proto.MARKET_SELL_UP;
                Debug.Log("CLIVERIFY market 15106 上架 mirror15109=" + b15106Mirror + " failNoMirrorShowErr=" + b15106FailNoMirror + " ok=" + b15106);

                // ---- 6. 15108 下架:成功重发15109镜像 / 失败只显码不重发 ----
                byte[] p15108ok = new CliVerify.Pkt().I(1).Bytes();
                List<string> logs15108ok = CaptureLogs(() => Feed("On15108", p15108ok));
                bool b15108Mirror = CountProto(logs15108ok, Shenxiao.Framework.Net.Proto.MARKET_SHELF_LIST) == 1;
                byte[] p15108fail = new CliVerify.Pkt().I(99).Bytes();
                List<string> logs15108fail = CaptureLogs(() => Feed("On15108", p15108fail));
                bool b15108FailNoMirror = CountProto(logs15108fail, Shenxiao.Framework.Net.Proto.MARKET_SHELF_LIST) == 0;
                bool b15108 = b15108Mirror && b15108FailNoMirror;
                Debug.Log("CLIVERIFY market 15108 下架 mirror15109=" + b15108Mirror + " failNoMirror=" + b15108FailNoMirror + " ok=" + b15108);

                // ---- 7. 15109 我的上架列表(同 15102 元素形状,无 type/subtype 前缀) ----
                byte[] p15109 = new CliVerify.Pkt()
                    .H(1)
                        .L(2001).L(9101).I(38030001).I(1).I(50).I(60).I(700).C(1)
                            .H(0)
                    .Bytes();
                Feed("On15109", p15109);
                bool b15109 = model.ShelfGoodsInfo != null && model.ShelfGoodsInfo.Count == 1
                    && model.ShelfGoodsInfo[0].Id == 2001 && model.ShelfGoodsInfo[0].UnitPrice == 700
                    && model.ShelfGoodsInfo[0].EquipExtraAttr.Count == 0;
                Debug.Log("CLIVERIFY market 15109 我的上架列表 listN=" + (model.ShelfGoodsInfo?.Count ?? -1) + " ok=" + b15109);

                // ---- 8. 15111 购买:成功摘除挂单缓存+重发15114镜像 / 1510006摘除挂单不重发 / 1510014摘除上架不重发 ----
                model.SetSellGoodsInfo(5, 100, new List<Shenxiao.Module.Core.Market.MarketModel.GoodsEntry>
                {
                    new Shenxiao.Module.Core.Market.MarketModel.GoodsEntry { Id = 2001, EquipExtraAttr = new List<Shenxiao.Module.Core.Market.MarketModel.EquipExtraAttrEntry>() },
                    new Shenxiao.Module.Core.Market.MarketModel.GoodsEntry { Id = 2002, EquipExtraAttr = new List<Shenxiao.Module.Core.Market.MarketModel.EquipExtraAttrEntry>() },
                });
                model.SetShelfGoodsInfo(new List<Shenxiao.Module.Core.Market.MarketModel.GoodsEntry>
                {
                    new Shenxiao.Module.Core.Market.MarketModel.GoodsEntry { Id = 3001, EquipExtraAttr = new List<Shenxiao.Module.Core.Market.MarketModel.EquipExtraAttrEntry>() },
                });
                byte[] p15111ok = new CliVerify.Pkt().I(1).C(1).L(2001).I(5).I(100).Bytes();
                List<string> logs15111ok = CaptureLogs(() => Feed("On15111", p15111ok));
                bool b15111Removed = model.GetSellGoodsInfo(5, 100).Count == 1 && model.GetSellGoodsInfo(5, 100)[0].Id == 2002;
                bool b15111Mirror = CountProto(logs15111ok, Shenxiao.Framework.Net.Proto.MARKET_BUY_TIMES) == 1;

                byte[] p151110006 = new CliVerify.Pkt().I(1510006).C(1).L(2002).I(5).I(100).Bytes();
                List<string> logs1510006 = CaptureLogs(() => Feed("On15111", p151110006));
                bool b1510006Removed = model.GetSellGoodsInfo(5, 100).Count == 0;
                bool b1510006NoMirror = CountProto(logs1510006, Shenxiao.Framework.Net.Proto.MARKET_BUY_TIMES) == 0;

                byte[] p151110014 = new CliVerify.Pkt().I(1510014).C(1).L(3001).I(5).I(100).Bytes();
                Feed("On15111", p151110014);
                bool b1510014Removed = model.ShelfGoodsInfo.Count == 0;

                bool b15111 = b15111Removed && b15111Mirror && b1510006Removed && b1510006NoMirror && b1510014Removed;
                Debug.Log("CLIVERIFY market 15111 购买 removed=" + b15111Removed + " mirror15114=" + b15111Mirror
                    + " 1510006removed=" + b1510006Removed + " 1510014removed=" + b1510014Removed + " ok=" + b15111);

                // ---- 9. 15112 交易记录(9字段,与15102不同形状) ----
                byte[] p15112 = new CliVerify.Pkt()
                    .H(1)
                        .I(38040001).I(1).I(10).I(20).C(1).I(50).I(1000).I(1700000000)
                            .H(1)
                                .C(1).C(2).H(300).I(400).C(5).I(600)
                    .Bytes();
                Feed("On15112", p15112);
                bool b15112 = model.RecordList != null && model.RecordList.Count == 1
                    && model.RecordList[0].TypeId == 38040001 && model.RecordList[0].Tax == 50 && model.RecordList[0].Price == 1000
                    && model.RecordList[0].Time == 1700000000 && model.RecordList[0].EquipExtraAttr.Count == 1;
                Debug.Log("CLIVERIFY market 15112 交易记录 listN=" + (model.RecordList?.Count ?? -1) + " ok=" + b15112);

                // ---- 10. 15114 购买次数(裸,三字段列表) ----
                byte[] p15114 = new CliVerify.Pkt().H(2).C(1).C(3).C(5).C(2).C(0).C(0).Bytes();
                Feed("On15114", p15114);
                bool b15114 = model.BuyTimesList != null && model.BuyTimesList.Count == 2
                    && model.BuyTimesList[0].Type == 1 && model.BuyTimesList[0].Times == 3 && model.BuyTimesList[0].TimesLimit == 5
                    && model.BuyTimesList[1].Type == 2 && model.BuyTimesList[1].Times == 0 && model.BuyTimesList[1].TimesLimit == 0;
                Debug.Log("CLIVERIFY market 15114 购买次数 listN=" + (model.BuyTimesList?.Count ?? -1) + " ok=" + b15114);

                // ---- 11. 15115 发起求购:未拉取(null)守卫 + 已拉取头插 + 失败不落地 ----
                model.SetSeekAllInfo(1, 1, 20, new List<Shenxiao.Module.Core.Market.MarketModel.SeekEntry>()); // All 已拉取(空列表)
                // Mine 保持 null(未拉取)——验证 AddPlzGoodInfoMine 的 null 守卫(ts:473-480)
                byte[] p15115ok = new CliVerify.Pkt().I(1).L(3001).L(9001).S("甲").I(38010001).H(5).I(100).I(1700000000).Bytes();
                Feed("On15115", p15115ok);
                bool b15115AllAdded = model.SeekListAll.Count == 1 && model.SeekListAll[0].Id == 3001 && model.SeekListAll[0].RoleName == "甲"
                    && model.SeekListAll[0].GoodsNum == 5;
                bool b15115MineGuarded = model.SeekListMine == null; // 未拉取过,Add 应被 null 守卫拦下
                // 现在补拉 Mine(模拟 15119 已回包),再验证头插命中两侧
                model.SetSeekMineInfo(new List<Shenxiao.Module.Core.Market.MarketModel.SeekEntry>());
                byte[] p15115ok2 = new CliVerify.Pkt().I(1).L(3002).L(9002).S("乙").I(38010002).H(6).I(200).I(1700000001).Bytes();
                Feed("On15115", p15115ok2);
                bool b15115BothAdded = model.SeekListAll.Count == 2 && model.SeekListAll[0].Id == 3002 // 头插,新条目在最前
                    && model.SeekListMine.Count == 1 && model.SeekListMine[0].Id == 3002;
                byte[] p15115fail = new CliVerify.Pkt().I(99).L(0).L(0).S("").I(0).H(0).I(0).I(0).Bytes();
                List<string> logs15115fail = CaptureLogs(() => Feed("On15115", p15115fail));
                bool b15115FailNoAdd = model.SeekListAll.Count == 2 && model.SeekListMine.Count == 1
                    && logs15115fail.Exists(l => l.Contains("错误(99)"));
                bool b15115 = b15115AllAdded && b15115MineGuarded && b15115BothAdded && b15115FailNoAdd;
                Debug.Log("CLIVERIFY market 15115 发起求购 allAdded=" + b15115AllAdded + " mineGuarded(null守卫)=" + b15115MineGuarded
                    + " bothAdded=" + b15115BothAdded + " failNoAdd=" + b15115FailNoAdd + " ok=" + b15115);

                // ---- 12. 15116 撤销求购:成功两侧都摘 / 1510023两侧都摘 / 其它失败码不摘 ----
                model.SetSeekAllInfo(1, 1, 20, new List<Shenxiao.Module.Core.Market.MarketModel.SeekEntry>
                {
                    new Shenxiao.Module.Core.Market.MarketModel.SeekEntry { Id = 7001 },
                    new Shenxiao.Module.Core.Market.MarketModel.SeekEntry { Id = 7002 },
                    new Shenxiao.Module.Core.Market.MarketModel.SeekEntry { Id = 7003 },
                });
                model.SetSeekMineInfo(new List<Shenxiao.Module.Core.Market.MarketModel.SeekEntry>
                {
                    new Shenxiao.Module.Core.Market.MarketModel.SeekEntry { Id = 7001 },
                    new Shenxiao.Module.Core.Market.MarketModel.SeekEntry { Id = 7002 },
                    new Shenxiao.Module.Core.Market.MarketModel.SeekEntry { Id = 7003 },
                });
                Feed("On15116", new CliVerify.Pkt().I(1).L(7001).Bytes());
                bool b15116Success = !model.SeekListAll.Exists(e => e.Id == 7001) && !model.SeekListMine.Exists(e => e.Id == 7001);
                Feed("On15116", new CliVerify.Pkt().I(1510023).L(7002).Bytes());
                bool b151160023 = !model.SeekListAll.Exists(e => e.Id == 7002) && !model.SeekListMine.Exists(e => e.Id == 7002);
                Feed("On15116", new CliVerify.Pkt().I(5).L(7003).Bytes());
                bool b15116OtherFailKeeps = model.SeekListAll.Exists(e => e.Id == 7003) && model.SeekListMine.Exists(e => e.Id == 7003);
                bool b15116 = b15116Success && b151160023 && b15116OtherFailKeeps;
                Debug.Log("CLIVERIFY market 15116 撤销求购 success=" + b15116Success + " code1510023=" + b151160023
                    + " otherFailKeeps=" + b15116OtherFailKeeps + " ok=" + b15116);

                // ---- 13. 15117 出售给求购单:成功只摘All(不摘Mine,与15116不对称,按TS原文镜像) / 1510023两侧都摘 ----
                model.SetSeekAllInfo(1, 1, 20, new List<Shenxiao.Module.Core.Market.MarketModel.SeekEntry>
                {
                    new Shenxiao.Module.Core.Market.MarketModel.SeekEntry { Id = 8001 },
                    new Shenxiao.Module.Core.Market.MarketModel.SeekEntry { Id = 8002 },
                });
                model.SetSeekMineInfo(new List<Shenxiao.Module.Core.Market.MarketModel.SeekEntry>
                {
                    new Shenxiao.Module.Core.Market.MarketModel.SeekEntry { Id = 8001 },
                    new Shenxiao.Module.Core.Market.MarketModel.SeekEntry { Id = 8002 },
                });
                Feed("On15117", new CliVerify.Pkt().I(1).L(8001).I(5).Bytes());
                bool b15117AsymOnlyAll = !model.SeekListAll.Exists(e => e.Id == 8001) && model.SeekListMine.Exists(e => e.Id == 8001);
                Feed("On15117", new CliVerify.Pkt().I(1510023).L(8002).I(3).Bytes());
                bool b151170023 = !model.SeekListAll.Exists(e => e.Id == 8002) && !model.SeekListMine.Exists(e => e.Id == 8002);
                bool b15117 = b15117AsymOnlyAll && b151170023;
                Debug.Log("CLIVERIFY market 15117 求购出售(不对称摘除探针) asymOnlyAll=" + b15117AsymOnlyAll + " code1510023=" + b151170023 + " ok=" + b15117);

                // ---- 14. 15118 求购列表(全服,分页,ServerNum:64独例探针) ----
                byte[] p15118 = new CliVerify.Pkt().H(3).H(1).H(20)
                    .H(1)
                        .L(5001).L(1).L(1234567890123L).L(9001).S("乙").I(38020001).H(2).I(300).I(1700000001)
                    .Bytes();
                Feed("On15118", p15118);
                bool b15118 = model.SeekAllPageTotal == 3 && model.SeekAllPageNo == 1 && model.SeekAllPageSize == 20
                    && model.SeekListAll.Count == 1 && model.SeekListAll[0].Id == 5001 && model.SeekListAll[0].SerId == 1
                    && model.SeekListAll[0].ServerNum == 1234567890123L && model.SeekListAll[0].RoleName == "乙"
                    && model.SeekListAll[0].GoodsNum == 2;
                Debug.Log("CLIVERIFY market 15118 求购列表(全服,ServerNum:64探针) serverNum=" + (model.SeekListAll?[0].ServerNum ?? -1) + " ok=" + b15118);

                // ---- 15. 15119 我的求购列表(5字段,无SerId/ServerNum/RoleName) ----
                byte[] p15119 = new CliVerify.Pkt().H(1)
                        .L(5002).I(38020002).H(3).I(301).I(1700000002)
                    .Bytes();
                Feed("On15119", p15119);
                bool b15119 = model.SeekListMine.Count == 1 && model.SeekListMine[0].Id == 5002 && model.SeekListMine[0].TypeId == 38020002
                    && model.SeekListMine[0].GoodsNum == 3 && model.SeekListMine[0].SerId == 0 && model.SeekListMine[0].RoleName == "";
                Debug.Log("CLIVERIFY market 15119 我的求购列表(缺省字段探针) serId默认0=" + (model.SeekListMine?[0].SerId == 0) + " ok=" + b15119);

                // ---- 16. 15120 删除推送(S2C only):sellType==1摘挂单+上架 / sellType==3摘两个求购 ----
                model.SetSellGoodsInfo(5, 100, new List<Shenxiao.Module.Core.Market.MarketModel.GoodsEntry>
                {
                    new Shenxiao.Module.Core.Market.MarketModel.GoodsEntry { Id = 9001, EquipExtraAttr = new List<Shenxiao.Module.Core.Market.MarketModel.EquipExtraAttrEntry>() },
                });
                model.SetShelfGoodsInfo(new List<Shenxiao.Module.Core.Market.MarketModel.GoodsEntry>
                {
                    new Shenxiao.Module.Core.Market.MarketModel.GoodsEntry { Id = 9001, EquipExtraAttr = new List<Shenxiao.Module.Core.Market.MarketModel.EquipExtraAttrEntry>() },
                });
                Feed("On15120", new CliVerify.Pkt().C(1).I(5).I(100).L(9001).Bytes());
                bool b15120SellType1 = model.GetSellGoodsInfo(5, 100).Count == 0 && model.ShelfGoodsInfo.Count == 0;

                model.SetSeekAllInfo(1, 1, 20, new List<Shenxiao.Module.Core.Market.MarketModel.SeekEntry> { new Shenxiao.Module.Core.Market.MarketModel.SeekEntry { Id = 9002 } });
                model.SetSeekMineInfo(new List<Shenxiao.Module.Core.Market.MarketModel.SeekEntry> { new Shenxiao.Module.Core.Market.MarketModel.SeekEntry { Id = 9002 } });
                Feed("On15120", new CliVerify.Pkt().C(3).I(0).I(0).L(9002).Bytes());
                bool b15120SellType3 = !model.SeekListAll.Exists(e => e.Id == 9002) && !model.SeekListMine.Exists(e => e.Id == 9002);
                bool b15120 = b15120SellType1 && b15120SellType3;
                Debug.Log("CLIVERIFY market 15120 删除推送 sellType1=" + b15120SellType1 + " sellType3=" + b15120SellType3 + " ok=" + b15120);

                // ---- 17. 15122 喊话:成功分支为空(不臆造数据处理,只显式断言"无错误提示"),失败显码 ----
                List<string> logs15122ok = CaptureLogs(() => Feed("On15122", new CliVerify.Pkt().I(1).L(6001).I(30).Bytes()));
                bool b15122OkNoShowError = !logs15122ok.Exists(l => l.Contains("错误("));
                List<string> logs15122fail = CaptureLogs(() => Feed("On15122", new CliVerify.Pkt().I(7).L(6002).I(0).Bytes()));
                bool b15122FailShowError = logs15122fail.Exists(l => l.Contains("错误(7)"));
                bool b15122 = b15122OkNoShowError && b15122FailShowError;
                Debug.Log("CLIVERIFY market 15122 喊话(成功分支为空探针) okNoShowError=" + b15122OkNoShowError + " failShowError=" + b15122FailShowError + " ok=" + b15122);

                Shenxiao.Framework.Event.EventDispatcher.Off<int, int>(Shenxiao.Framework.Event.GlobalEvent.EVT_MARKET_RESULT, onMarketResult);

                // ---- 18. 15100 通用错误码推送(无条件显码,不判断code==1,按TS原文逐字镜像) ----
                int updateCount = 0; var updateProtoIds = new List<int>();
                System.Action<int> onMarketUpdate = proto => { updateCount++; updateProtoIds.Add(proto); };
                Shenxiao.Framework.Event.EventDispatcher.On<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_MARKET_UPDATE, onMarketUpdate);
                List<string> logs15100 = CaptureLogs(() => Feed("On15100", new CliVerify.Pkt().I(1).S("ok_code_still_shows").Bytes()));
                bool b15100 = logs15100.Exists(l => l.Contains("错误(1)")) && updateProtoIds.Contains(Shenxiao.Framework.Net.Proto.MARKET_ERROR_PUSH);
                Shenxiao.Framework.Event.EventDispatcher.Off<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_MARKET_UPDATE, onMarketUpdate);
                Debug.Log("CLIVERIFY market 15100 错误推送(无条件显码探针) ok=" + b15100);

                // ---- 19. 发送方法 no-throw(全部 Request* 一遍) ----
                bool sendNoThrow = true;
                try
                {
                    typed.RequestLevel1List(5);
                    typed.RequestGoodsList(5, 100, 99, 99, 99);
                    typed.RequestSellUp(1001, 1, 100, 0);
                    typed.RequestSellDown(1, 1001, 38010001, 1);
                    typed.RequestShelfList();
                    typed.RequestBuy(1, 2001, 5, 100, 9001, 38010001, 1, 100);
                    typed.RequestRecordList();
                    typed.RequestBuyTimes();
                    typed.RequestCreatePlz(38010001, 1, 100);
                    typed.RequestCancelPlz(3001);
                    typed.RequestSellToPlz(3001, 9001, 38010001, 1, 100);
                    typed.RequestPlzListAll(1, 20);
                    typed.RequestPlzListMine();
                    typed.RequestShout(2001);
                }
                catch (System.Exception e) { sendNoThrow = false; Debug.LogError("CLIVERIFY market 发送方法 threw: " + e); }
                Debug.Log("CLIVERIFY market 发送方法 noThrow=" + sendNoThrow);

                // ---- 20. Reset() 清空核实 ----
                model.Reset();
                bool resetOk = model.GetGoodsDic(5) == null && model.GetSellGoodsInfo(5, 100) == null
                    && model.ShelfGoodsInfo == null && model.RecordList == null && model.BuyTimesList == null
                    && model.SeekListAll == null && model.SeekListMine == null && model.KfOpenTime == 0 && !model.KfOpen;
                Debug.Log("CLIVERIFY market Reset() 清空核实 ok=" + resetOk);

                bool pass = deadOk && regOk && b15121 && b15101 && b15102 && b15102ClearWholeDict && b15106 && b15108 && b15109 && b15111
                    && b15112 && b15114 && b15115 && b15116 && b15117 && b15118 && b15119 && b15120 && b15122
                    && b15100 && sendNoThrow && resetOk;

                Debug.Log("CLIVERIFY market VERDICT dead=" + deadOk + " reg=" + regOk + " icon15121=" + b15121
                    + " list(101/102/109)=" + (b15101 && b15102 && b15109) + " sellDicClear=" + b15102ClearWholeDict
                    + " ops(106/108/111)=" + (b15106 && b15108 && b15111)
                    + " record112=" + b15112 + " times114=" + b15114 + " plz(115-119)=" + (b15115 && b15116 && b15117 && b15118 && b15119)
                    + " deletePush120=" + b15120 + " shout122=" + b15122 + " errPush100=" + b15100
                    + " sendNoThrow=" + sendNoThrow + " resetOk=" + resetOk + " pass=" + pass);

                baseCtrl.Dispose();
                return pass ? 0 : 3;
            }
            catch (System.Exception e)
            {
                Debug.LogError("CLIVERIFY market EXCEPTION " + e);
                return 3;
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }
        }
    }
}
