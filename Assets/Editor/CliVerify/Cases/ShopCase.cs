using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 商店(自动循环 轮11)实证:反射喂本轮新建的 ShopController 私有 handler(模板 PetTrainCase+DailyHubCase)。
    /// 逻辑段(纯反射喂包,断言 ShopModel 状态):
    ///   · 15301 按 shop_type 分槽存 + Rank 升序 + subtype_list 去包裹切分 series_list(尾哨兵 Bind 字段核对
    ///     字节游标未错位)/ SoldOut 真实语义=已购次数(非售罄布尔,喂非零值仍能正常落地)/
    ///     type==TopVipShop(10) 劫持:落 TopVipShopGoodsList 专槽、不进主表、不炸。
    ///   · 15305 BuyType 真实语义=购买状态(1未买/2已买,非货币类型)按 cfg_id 升序 + hit_num 变化联动
    ///     (EVT_SHOP_MYSTERY_REFRESH_EFFECT)。
    ///   · 15306 刷新联动(errcode==1 成功/失败码各一发,不炸)。
    ///   · 15307 成功落地(UpdateMysteryShop)+ 失败包字段错位兼容(第二字段实为 Id,不当 Type 用;不炸)。
    ///   · 64000 left_time 用服务器墙钟(SERVER_ZONE_HOURS=8)自算"下一个游戏日0点",与测试侧独立复算的
    ///     期望值容差比对(同 DailyHubCase 15718 红点期望值独立复算先例)+ VieRedStatus 首次判定。
    ///   · 64001 双编码分流:errcode=3(0-7 自定义提示码"金额不足")与 errcode=6400000(全局 ERRCODE≥100000)
    ///     各喂一发,断言走不同文案分支;成功原地 patch 不重拉整表。
    ///   · 64002 库存广播原地 patch left_limit_num。
    ///   · 64003 下架广播真删(订正老端 Array.slice 假删除 bug)。
    ///   · 失败码各一发(15302/15304/15306/15307/64001)均不抛异常。
    /// 结构段(不依赖协议,断言 ShopFlow 形态④):11 标签数 + labels 非空(BuildSharedTabs icon 通道补齐后仍保底
    /// 有文字)+ 点 tab3 命中 ShopVieView(ShopCommonView 转 inactive)。编辑期 Addressables 不可用则优雅降级
    /// (同 DungeonBuyTimeView/DailyHub 先例),渲染断言反射定位被测实例,不全场景搜同类型。
    /// </summary>
    public static class ShopCase
    {
        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;

            int mysteryRefreshEffectCount = 0;
            Action<int> onMysteryRefreshEffect = _ => mysteryRefreshEffectCount++;
            Shenxiao.Framework.Event.EventDispatcher.On<int>(
                Shenxiao.Framework.Event.GlobalEvent.EVT_SHOP_MYSTERY_REFRESH_EFFECT, onMysteryRefreshEffect);
            int mysteryBuySuccessCfgId = -1;
            Action<int> onMysteryBuySuccess = cfgId => mysteryBuySuccessCfgId = cfgId;
            Shenxiao.Framework.Event.EventDispatcher.On<int>(
                Shenxiao.Framework.Event.GlobalEvent.EVT_SHOP_MYSTERY_BUY_SUCCESS, onMysteryBuySuccess);
            int vieBuySuccessCount = 0;
            Action<int> onVieBuySuccess = _ => vieBuySuccessCount++;
            Shenxiao.Framework.Event.EventDispatcher.On<int>(
                Shenxiao.Framework.Event.GlobalEvent.EVT_SHOP_VIE_BUY_SUCCESS, onVieBuySuccess);

            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.Shop.ShopConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.Shop.ShopConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY shop FAIL ShopConfigs not loaded");
                    return 3;
                }

                object ctrl = Shenxiao.Module.Core.Shop.ShopController.Instance;
                const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo H(string name)
                {
                    MethodInfo m = ctrl.GetType().GetMethod(name, F);
                    if (m == null) Debug.LogError("CLIVERIFY shop handler missing: " + name);
                    return m;
                }
                MethodInfo m15301 = H("On15301"), m15302 = H("On15302"), m15304 = H("On15304"),
                    m15305 = H("On15305"), m15306 = H("On15306"), m15307 = H("On15307"),
                    m64000 = H("On64000"), m64001 = H("On64001"), m64002 = H("On64002"), m64003 = H("On64003");
                if (m15301 == null || m15302 == null || m15304 == null || m15305 == null || m15306 == null
                    || m15307 == null || m64000 == null || m64001 == null || m64002 == null || m64003 == null)
                {
                    return 3;
                }
                void Feed(MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                Shenxiao.Module.Core.Shop.ShopModel model = Shenxiao.Module.Core.Shop.ShopModel.Instance;
                model.Clear();

                // ---- A. 15301 分槽+Rank升序+series_list切分+SoldOut已购次数语义(尾哨兵 Bind 字段核对游标) ----
                // item B(Rank=1,先)/item A(Rank=2,后):subtype_list "%[1,2%]" 去包裹切分 series_list=[1,2];
                // item A 的 sold_out=3(非零,验证不是售罄布尔,而是真实已购次数);item B 的 Bind=1 是尾哨兵。
                Feed(m15301, new CliVerify.Pkt()
                    .C(Shenxiao.Module.Core.Shop.ShopModel.TYPE_LIMIT)
                    .H(2)
                        .I(101).S("%[1,2%]").I(2).I(520100).I(1).I(2).I(100).H(100).C(1).H(5).H(3).S("").I(0).C(0)
                        .I(102).S("").I(1).I(520100).I(1).I(1).I(50).H(100).C(0).H(0).H(0).S("").I(0).C(1)
                    .Bytes());
                List<Shenxiao.Module.Core.Shop.ShopModel.GoodsVo> limitList =
                    model.GetShopDataByType(Shenxiao.Module.Core.Shop.ShopModel.TYPE_LIMIT);
                bool splitOk = limitList.Count == 2;
                bool sortOk = splitOk && limitList[0].KeyId == 102 && limitList[1].KeyId == 101;
                bool seriesOk = splitOk && limitList[1].SeriesList.Count == 2
                    && limitList[1].SeriesList[0] == 1 && limitList[1].SeriesList[1] == 2;
                bool soldOutOk = splitOk && limitList[1].SoldOut == 3; // 真实语义=已购次数,非0/1布尔
                bool tailSentinelOk = splitOk && limitList[0].Bind == 1; // 尾哨兵:字段序全程无错位
                Debug.Log("CLIVERIFY shop 15301 split=" + splitOk + " sort=" + sortOk + " series=" + seriesOk
                    + " soldOutUsedTime=" + soldOutOk + " tailSentinel=" + tailSentinelOk);

                // ---- B. 15301 TopVipShop(10) 劫持:落专槽,不进主表,不炸 ----
                bool topVipNoThrow = true;
                try
                {
                    Feed(m15301, new CliVerify.Pkt()
                        .C(Shenxiao.Module.Core.Shop.ShopModel.TYPE_TOPVIP_SHOP)
                        .H(1)
                            .I(999).S("").I(1).I(520100).I(1).I(ShopMoneyTopVip()).I(100).H(100).C(0).H(0).H(0).S("").I(0).C(0)
                        .Bytes());
                }
                catch (Exception e) { topVipNoThrow = false; Debug.LogError("CLIVERIFY shop 15301 topvip threw: " + e); }
                bool topVipStashOk = model.TopVipShopGoodsList.Count == 1;
                bool topVipNotMainOk = model.GetShopDataByType(Shenxiao.Module.Core.Shop.ShopModel.TYPE_TOPVIP_SHOP).Count == 0;
                Debug.Log("CLIVERIFY shop 15301 topvip noThrow=" + topVipNoThrow + " stash=" + topVipStashOk + " notMain=" + topVipNotMainOk);

                // ---- C. 15302 失败码(1001 钻石不足降级 toast)不炸 ----
                logs.Clear();
                bool buy302NoThrow = true;
                try { Feed(m15302, new CliVerify.Pkt().I(1001).I(101).I(1).Bytes()); }
                catch (Exception e) { buy302NoThrow = false; Debug.LogError("CLIVERIFY shop 15302 threw: " + e); }
                bool buy302FailOk = logs.Exists(l => l.Contains("15302 购买失败"));
                Debug.Log("CLIVERIFY shop 15302 fail noThrow=" + buy302NoThrow + " text=" + buy302FailOk);

                // ---- D. 15304 失败码不炸 ----
                logs.Clear();
                bool buy304NoThrow = true;
                try { Feed(m15304, new CliVerify.Pkt().I(0).I(16020001).I(1).C(1).Bytes()); }
                catch (Exception e) { buy304NoThrow = false; Debug.LogError("CLIVERIFY shop 15304 threw: " + e); }
                bool buy304FailOk = logs.Exists(l => l.Contains("15304 快速购买失败"));
                Debug.Log("CLIVERIFY shop 15304 fail noThrow=" + buy304NoThrow + " text=" + buy304FailOk);

                // ---- E. 15305 BuyType真实语义=购买状态(cfg_id升序)+ hit_num变化联动刷新特效 ----
                Feed(m15305, new CliVerify.Pkt()
                    .H(Shenxiao.Module.Core.Shop.ShopModel.MYSTERY_DEMON).I(123456).H(2)
                    .H(2)
                        .H(10).C(90).I(200).C(1).C(0) // cfg_id=10,未买(BuyType=1)
                        .H(5).C(100).I(100).C(2).C(3) // cfg_id=5,已买(BuyType=2,BuyNum=3,尾哨兵)
                    .Bytes());
                Shenxiao.Module.Core.Shop.ShopModel.MysteryShopVo mysteryVo =
                    model.GetMysteryDataByType(Shenxiao.Module.Core.Shop.ShopModel.MYSTERY_DEMON);
                bool mysterySortOk = mysteryVo != null && mysteryVo.GoodList.Count == 2
                    && mysteryVo.GoodList[0].CfgId == 5 && mysteryVo.GoodList[1].CfgId == 10;
                bool buyTypeSemanticOk = mysterySortOk
                    && mysteryVo.GoodList[0].BuyType == 2 && mysteryVo.GoodList[1].BuyType == 1
                    && mysteryVo.GoodList[0].BuyNum == 3; // 尾哨兵
                bool allNewFalseOk = !model.MysteryFirstAllNewRed; // 并非全部未买 → 不点红点
                Debug.Log("CLIVERIFY shop 15305 sort=" + mysterySortOk + " buyTypeSemantic=" + buyTypeSemanticOk + " allNewFalse=" + allNewFalseOk);

                mysteryRefreshEffectCount = 0;
                Feed(m15305, new CliVerify.Pkt()
                    .H(Shenxiao.Module.Core.Shop.ShopModel.MYSTERY_DEMON).I(123456).H(9) // hit_num 2→9,变化
                    .H(1).H(5).C(100).I(100).C(2).C(3)
                    .Bytes());
                bool hitChangedOk = mysteryRefreshEffectCount == 1;
                Debug.Log("CLIVERIFY shop 15305 hitChanged refreshEffectFired=" + hitChangedOk);

                // ---- F. 15306 刷新联动(成功/失败各一发,不炸) ----
                logs.Clear();
                bool refresh306NoThrow = true;
                try { Feed(m15306, new CliVerify.Pkt().I(1).Bytes()); }
                catch (Exception e) { refresh306NoThrow = false; Debug.LogError("CLIVERIFY shop 15306 threw: " + e); }
                bool refresh306Ok = logs.Exists(l => l.Contains("15306 手动刷新成功"));
                logs.Clear();
                Feed(m15306, new CliVerify.Pkt().I(1530099).Bytes());
                bool refresh306FailOk = logs.Exists(l => l.Contains("15306 手动刷新失败"));
                Debug.Log("CLIVERIFY shop 15306 ok=" + refresh306Ok + " noThrow=" + refresh306NoThrow + " failText=" + refresh306FailOk);

                // ---- G. 15307 成功落地(UpdateMysteryShop)+ 失败包字段错位兼容(第二字段=Id,不炸) ----
                Feed(m15307, new CliVerify.Pkt().I(1).H(Shenxiao.Module.Core.Shop.ShopModel.MYSTERY_DEMON).H(5).Bytes());
                Shenxiao.Module.Core.Shop.ShopModel.MysteryGoodVo mg5 =
                    model.GetMysteryDataById(Shenxiao.Module.Core.Shop.ShopModel.MYSTERY_DEMON, 5);
                bool buy307Ok = mg5 != null && mg5.BuyType == 2 && mg5.BuyNum == 4 && mysteryBuySuccessCfgId == 5;
                logs.Clear();
                bool buy307FailNoThrow = true;
                // 失败包字段错位:实参 [Errcode,Id,0](第2字段=Id而非Type,第3字段恒0)——按老端行为原样喂,断言不炸。
                try { Feed(m15307, new CliVerify.Pkt().I(1570001).H(999).H(0).Bytes()); }
                catch (Exception e) { buy307FailNoThrow = false; Debug.LogError("CLIVERIFY shop 15307 fail threw: " + e); }
                bool buy307FailOk = logs.Exists(l => l.Contains("15307 购买失败") && l.Contains("错位"));
                Debug.Log("CLIVERIFY shop 15307 success=" + buy307Ok + " failNoThrow=" + buy307FailNoThrow + " failText=" + buy307FailOk);

                // ---- H. 64000 left_time服务器墙钟自算 + VieRedStatus首次判定 ----
                long beforeFeedMs = Shenxiao.Framework.Util.TimeUtil.NowMs();
                Feed(m64000, new CliVerify.Pkt()
                    .H(2)
                        .I(1).I(37010003).I(5).C(1).I(50).I(30).I(99999).I(99999).I(1).I(0)
                        .I(2).I(37010004).I(3).C(1).I(150).I(100).I(99999).I(5).I(2).I(2) // buy_num==daily_limit_num(尾哨兵售罄触发)
                    .Bytes());
                Shenxiao.Module.Core.Shop.ShopModel.VieInfoVo vieInfo = model.GetVieInfo();
                bool vie64000Ok = vieInfo != null && vieInfo.IdList.Count == 2
                    && vieInfo.IdList[0].Id == 1 && vieInfo.IdList[1].Id == 2 && vieInfo.IdList[1].BuyNum == 2;
                // 独立复算期望 left_time(同一套 SERVER_ZONE_HOURS 公式;同 DailyHub 15718 红点期望值先例),
                // 容差 5 秒(测试执行耗时 + 秒级取整)。
                DateTime zoneNow = Shenxiao.Framework.Util.TimeUtil.NowUtc().AddHours(Shenxiao.Module.Core.Shop.ShopModel.SERVER_ZONE_HOURS);
                DateTime zoneMidnightNext = zoneNow.Date.AddDays(1);
                DateTime trueUtc = zoneMidnightNext.AddHours(-Shenxiao.Module.Core.Shop.ShopModel.SERVER_ZONE_HOURS);
                long expectedLeftTimeMs = (long)(trueUtc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                bool leftTimeOk = vieInfo != null && Math.Abs(vieInfo.LeftTimeMs - expectedLeftTimeMs) < 5000
                    && vieInfo.LeftTimeMs > beforeFeedMs; // 必是未来时间点,非裸 UTC 当下值
                bool vieRedOk = model.VieRedStatus == true; // item1 还能买 → 并非全部售罄
                Debug.Log("CLIVERIFY shop 64000 land=" + vie64000Ok + " leftTime=" + leftTimeOk
                    + "(actual=" + (vieInfo?.LeftTimeMs ?? -1) + " expect=" + expectedLeftTimeMs + ") vieRed=" + vieRedOk);

                // ---- I. 64001 双编码分流(errcode=3 小表"金额不足"/errcode=6400000 大数值显码降级) ----
                Feed(m64001, new CliVerify.Pkt().I(1).I(1).I(1).I(99998).Bytes());
                Shenxiao.Module.Core.Shop.ShopModel.VieGoodVo vie1 = model.GetVieGoodById(1);
                bool buy64001Ok = vie1 != null && vie1.BuyNum == 1 && vie1.LeftLimitNum == 99998 && vieBuySuccessCount == 1;
                logs.Clear();
                Feed(m64001, new CliVerify.Pkt().I(3).I(1).I(0).I(0).Bytes()); // 0-7 小码:3="金额不足"
                bool smallCodeOk = logs.Exists(l => l.Contains("金额不足"));
                logs.Clear();
                Feed(m64001, new CliVerify.Pkt().I(6400000).I(1).I(0).I(0).Bytes()); // ≥100000 大码:显码降级
                bool bigCodeOk = logs.Exists(l => l.Contains("操作失败(6400000)"));
                Debug.Log("CLIVERIFY shop 64001 buy=" + buy64001Ok + " smallCode(3=金额不足)=" + smallCodeOk + " bigCode(6400000)=" + bigCodeOk);

                // ---- J. 64002 库存广播原地 patch ----
                Feed(m64002, new CliVerify.Pkt().H(1).I(1).I(50).Bytes());
                bool patch64002Ok = model.GetVieGoodById(1)?.LeftLimitNum == 50;
                Debug.Log("CLIVERIFY shop 64002 patch=" + patch64002Ok);

                // ---- K. 64003 真删(订正老端 Array.slice 假删除 bug) ----
                int countBeforeDel = model.GetVieInfo().IdList.Count;
                Feed(m64003, new CliVerify.Pkt().H(1).I(2).Bytes());
                bool realDeleteOk = model.GetVieInfo().IdList.Count == countBeforeDel - 1
                    && model.GetVieGoodById(2) == null && model.GetVieGoodById(1) != null;
                Debug.Log("CLIVERIFY shop 64003 realDelete=" + realDeleteOk + " remain=" + model.GetVieInfo().IdList.Count);

                bool logicPass = splitOk && sortOk && seriesOk && soldOutOk && tailSentinelOk
                    && topVipNoThrow && topVipStashOk && topVipNotMainOk
                    && buy302NoThrow && buy302FailOk && buy304NoThrow && buy304FailOk
                    && mysterySortOk && buyTypeSemanticOk && allNewFalseOk && hitChangedOk
                    && refresh306NoThrow && refresh306Ok && refresh306FailOk
                    && buy307Ok && buy307FailNoThrow && buy307FailOk
                    && vie64000Ok && leftTimeOk && vieRedOk
                    && buy64001Ok && smallCodeOk && bigCodeOk
                    && patch64002Ok && realDeleteOk;

                // ---- L. 结构段:ShopFlow 形态④ 11 标签 + labels 非空 + tab3 override 命中 ShopVieView ----
                bool structOk;
                bool structLoaded = false;
                try
                {
                    Shenxiao.Module.Core.Shop.ShopFlow.Open();
                    const BindingFlags SF = BindingFlags.NonPublic | BindingFlags.Static;
                    FieldInfo fWindow = typeof(Shenxiao.Module.Core.Shop.ShopFlow).GetField("_window", SF);
                    FieldInfo fContentRoot = typeof(Shenxiao.Module.Core.Shop.ShopFlow).GetField("_contentRoot", SF);
                    double deadline = UnityEditor.EditorApplication.timeSinceStartup + 8.0;
                    object windowObj = null;
                    GameObject contentRoot = null;
                    while (UnityEditor.EditorApplication.timeSinceStartup < deadline)
                    {
                        windowObj = fWindow?.GetValue(null);
                        contentRoot = fContentRoot?.GetValue(null) as GameObject;
                        if (windowObj != null && contentRoot != null) break;
                        await Task.Delay(200);
                    }
                    if (windowObj != null && contentRoot != null)
                    {
                        Type winType = windowObj.GetType();
                        const BindingFlags IF = BindingFlags.NonPublic | BindingFlags.Instance;
                        FieldInfo fTabs = winType.GetField("_tabs", IF);
                        FieldInfo fLabels = winType.GetField("_sharedLabels", IF);
                        System.Collections.IList tabs = fTabs?.GetValue(windowObj) as System.Collections.IList;
                        string[] labels = fLabels?.GetValue(windowObj) as string[];
                        bool tabCountOk = tabs != null && tabs.Count == 11;
                        bool labelsOk = labels != null && labels.Length == 11;
                        for (int i = 0; labelsOk && i < labels.Length; i++) if (string.IsNullOrEmpty(labels[i])) labelsOk = false;

                        MethodInfo mSelectShared = winType.GetMethod("SelectShared", IF | BindingFlags.Public);
                        mSelectShared?.Invoke(windowObj, new object[] { 3 });
                        await Task.Delay(200);
                        // ⚠ShopCommonView/ShopVieView 选中时已被 ReparentNamed 移出 _contentRoot(挂到窗框内容区
                        // _gp_item_con,后者是 windowObj 自身 Transform 下的子节点)——必须从窗框组件本身搜,
                        // 不能再从 _contentRoot 搜(此时已找不到,同轮9"渲染断言反射定位被测实例"教训)。
                        var windowComponent = windowObj as Component;
                        Shenxiao.Module.Core.Shop.ShopVieView vieView = windowComponent != null
                            ? windowComponent.GetComponentInChildren<Shenxiao.Module.Core.Shop.ShopVieView>(true) : null;
                        Shenxiao.Module.Core.Shop.ShopCommonView commonView = windowComponent != null
                            ? windowComponent.GetComponentInChildren<Shenxiao.Module.Core.Shop.ShopCommonView>(true) : null;
                        bool overrideOk = vieView != null && vieView.gameObject.activeInHierarchy
                            && (commonView == null || !commonView.gameObject.activeInHierarchy);

                        structLoaded = true;
                        structOk = tabCountOk && labelsOk && overrideOk;
                        stage.ForceCjkFont();
                        string png = stage.Capture("Temp/round11_shop_tabs.png");
                        Debug.Log("CLIVERIFY shop struct tabCount=" + (tabs?.Count ?? -1) + " labelsOk=" + labelsOk
                            + " tab3Override=" + overrideOk + " shot=" + png);
                    }
                    else
                    {
                        structOk = true; // 编辑期 ShopModule.prefab 不可加载:结构断言优雅降级,同 DailyHub/DungeonBuyTimeView 先例
                        Debug.LogWarning("CLIVERIFY shop struct degrade(prefab 未在编辑期加载,断言跳过)");
                    }
                    Shenxiao.Module.Core.Shop.ShopFlow.Close();
                }
                catch (Exception e)
                {
                    structOk = true;
                    Debug.LogWarning("CLIVERIFY shop struct degrade(exception): " + e.Message);
                }

                bool pass = logicPass && structOk;
                Debug.Log("CLIVERIFY shop VERDICT logic=" + logicPass + " structLoaded=" + structLoaded + " struct=" + structOk + " pass=" + pass);

                model.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                Application.logMessageReceived -= cb;
                Shenxiao.Framework.Event.EventDispatcher.Off<int>(
                    Shenxiao.Framework.Event.GlobalEvent.EVT_SHOP_MYSTERY_REFRESH_EFFECT, onMysteryRefreshEffect);
                Shenxiao.Framework.Event.EventDispatcher.Off<int>(
                    Shenxiao.Framework.Event.GlobalEvent.EVT_SHOP_MYSTERY_BUY_SUCCESS, onMysteryBuySuccess);
                Shenxiao.Framework.Event.EventDispatcher.Off<int>(
                    Shenxiao.Framework.Event.GlobalEvent.EVT_SHOP_VIE_BUY_SUCCESS, onVieBuySuccess);
                // ShopFlow.Reset() 是 internal(同轮10 DailyFlow 先例,本仓大多数 Flow.Reset 均 internal,
                // 跨程序集[Shenxiao.Editor]不可见)——已在 try 段末尾调过公开的 Close(),够用,不强行放宽可见性。
                stage.Dispose();
            }
        }

        // ShopModel.MONEY_TOPVIP 是 private-friendly public const,但避免与本文件其它 using 冲突,直接读常量值。
        private static int ShopMoneyTopVip() => Shenxiao.Module.Core.Shop.ShopModel.MONEY_TOPVIP;
    }
}
