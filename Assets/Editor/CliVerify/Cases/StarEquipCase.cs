using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 星宿核心(pp_constellation_equip,pt_232 直接处理段,轮23 PK1)实证:17 号(23200-23209/23250-23257)
    /// 合成包驱动 StarEquipController 反射喂包,断言 StarEquipModel 落地字段/事件 + config 17 张表
    /// (16 服务端 config_constellation_* + 1 客户端 ConfigConstellation)计数与逐行核对(模板 GuildActivityCase,
    /// 纯逻辑段,同 15a/15b Boss 先例本轮无渲染)。
    ///
    /// 重点覆盖:23200 族错误出口(1500081 特判触发 COMPOSE_FAIL,同时仍发 ERROR)/23201 总览嵌套
    /// (PageItem.Attr attr_list)+**尾哨兵字节游标核对**/23202-23203 穿卸(均无 Code,仅成功回本号,
    /// 事件参数 goodsAutoId/goodsTypeId)/23206 自带真实失败码边界(失败不覆盖已升级数据)/23207→23208→23209
    /// 吞噬链:23209 成功后 **Color/Star 跨号保持不变**(wire 无该字段,对标 StarEquipModel.ApplyDevourResult
    /// 类注释)+23209 变长 C2S 发送侧(RequestDevour)不抛/23250 六层嵌套读取(SendDsgt→DsgtSuit/DsgtAttr,
    /// StarAttrCfg 6 字段独立结构)+**尾哨兵**/23251 星数被动推送(与23205 StarMaster 分槽不互相覆盖)/
    /// 23252 合成四出口(?SUCCESS/err150_compose_success 均落地,err150_compose_fail 不覆盖已有缓存,
    /// 未知失败码降级 ShowError 不抛)+变长 C2S 发送侧(RequestCompose)不抛/23253 解锁成功原地置位
    /// +内部联动重拉 23201(RequestOverview)不抛/23254 蜕变对比预览(比23250多前置 TargetGoodsAutoId)/
    /// 23255 精简类型 tips(GoodsTypeId wire 是 **32位**,比23250/23254少 SuitAttr/锻造四段属性共5个字段)/
    /// 23256 合成次数分桶/23257 蜕变执行(老端 on23257 是空 if 块未接动作,本端仍补发事件供尾包消费)。
    ///
    /// 死号断言:23204(裁决1 killlist)—— 无 On23204 handler 且 Proto 无 23204 常量,同 40218/40263 先例
    /// "永不触发的接收严禁注册"。
    ///
    /// UI 层:starEquip 20+ 个 scene 全部只有分类决策,连 prefab 都没烤(r23_starequip.md §三),
    /// 本轮数据层 only 无渲染段;星宿锻造(chc/StarForge,PK2,23210-23241)是本家族兜底转发的另一半,
    /// 分文件不分模块,单独 StarForgeCase.cs 覆盖(不挂本文件)。
    /// </summary>
    public static class StarEquipCase
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
                await Shenxiao.Module.Core.StarEquip.StarEquipConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.StarEquip.StarEquipConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY starequip FAIL StarEquipConfigs not loaded");
                    return 3;
                }

                // ---- 配置 17 表计数(源=r23_starequip.md §四 实测行数,与 yu_client cdn/resource/config/server
                // 法定同步源逐条核对一致;evolution_rate 实测确为 0 空表)+ 逐行核对 ----
                bool configCountOk = Shenxiao.Module.Core.StarEquip.StarEquipConfigs.EquipCount == 180
                    && Shenxiao.Module.Core.StarEquip.StarEquipConfigs.PageCount == 5
                    && Shenxiao.Module.Core.StarEquip.StarEquipConfigs.ComposeCount == 20
                    && Shenxiao.Module.Core.StarEquip.StarEquipConfigs.DecomposeCount == 2000
                    && Shenxiao.Module.Core.StarEquip.StarEquipConfigs.PosCount == 10
                    && Shenxiao.Module.Core.StarEquip.StarEquipConfigs.KvCount == 6
                    && Shenxiao.Module.Core.StarEquip.StarEquipConfigs.StrengthCount == 876
                    && Shenxiao.Module.Core.StarEquip.StarEquipConfigs.StrengthBuffCount == 15
                    && Shenxiao.Module.Core.StarEquip.StarEquipConfigs.StrengthMasterCount == 23
                    && Shenxiao.Module.Core.StarEquip.StarEquipConfigs.EnchantmentCount == 930
                    && Shenxiao.Module.Core.StarEquip.StarEquipConfigs.EnchantmentMasterCount == 55
                    && Shenxiao.Module.Core.StarEquip.StarEquipConfigs.EvolutionCount == 222
                    && Shenxiao.Module.Core.StarEquip.StarEquipConfigs.EvolutionPoolCount == 30
                    && Shenxiao.Module.Core.StarEquip.StarEquipConfigs.EvolutionRateCount == 0
                    && Shenxiao.Module.Core.StarEquip.StarEquipConfigs.SpiritCount == 30
                    && Shenxiao.Module.Core.StarEquip.StarEquipConfigs.ForgeKvCount == 10
                    && Shenxiao.Module.Core.StarEquip.StarEquipConfigs.StarPointCfgCount == 12; // 实测12条(侦察稿写8条,已订正)

                var equipRow = Shenxiao.Module.Core.StarEquip.StarEquipConfigs.GetEquipInfo(79010501);
                var posRow = Shenxiao.Module.Core.StarEquip.StarEquipConfigs.GetPos(1);
                var composeRow = Shenxiao.Module.Core.StarEquip.StarEquipConfigs.GetComposeInfo(79301);
                var kvRow = Shenxiao.Module.Core.StarEquip.StarEquipConfigs.GetKv("open_lv");
                var forgeKvRow = Shenxiao.Module.Core.StarEquip.StarEquipConfigs.GetForgeKv(6);
                var evoRow = Shenxiao.Module.Core.StarEquip.StarEquipConfigs.GetEvolution(1, 1, 0);
                bool configRowOk = equipRow != null && equipRow.Page == 1 && equipRow.ComposeInfo == 300 && equipRow.IsSuit == 1 && equipRow.DecomposeExp == 200
                    && posRow != null && posRow.Type == 1
                    && composeRow != null && composeRow.RatioType == 2 && composeRow.BindType == 3 && composeRow.TvType == 1
                    && kvRow != null && kvRow.Value == "560"
                    && forgeKvRow != null && forgeKvRow.Value == "1"
                    && evoRow != null && evoRow.EvPoint == 100 && evoRow.Rate == 6000;
                Debug.Log("CLIVERIFY starequip config countOk=" + configCountOk + " rowOk=" + configRowOk
                    + " equip=" + Shenxiao.Module.Core.StarEquip.StarEquipConfigs.EquipCount
                    + " decompose=" + Shenxiao.Module.Core.StarEquip.StarEquipConfigs.DecomposeCount
                    + " strength=" + Shenxiao.Module.Core.StarEquip.StarEquipConfigs.StrengthCount
                    + " evolutionRate=" + Shenxiao.Module.Core.StarEquip.StarEquipConfigs.EvolutionRateCount
                    + " starPointCfg=" + Shenxiao.Module.Core.StarEquip.StarEquipConfigs.StarPointCfgCount);

                Shenxiao.Module.Core.StarEquip.StarEquipModel model = Shenxiao.Module.Core.StarEquip.StarEquipModel.Instance;
                model.Clear();

                var ctrlTyped = Shenxiao.Module.Core.StarEquip.StarEquipController.Instance;
                object ctrl = ctrlTyped;
                Type t = ctrl.GetType();
                void Feed(string method, byte[] pkt)
                {
                    MethodInfo m = t.GetMethod(method, F);
                    if (m == null) { Debug.LogError("CLIVERIFY starequip handler missing: " + method); return; }
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });
                }
                Shenxiao.Framework.Net.NetReader FeedReader(string method, byte[] pkt)
                {
                    MethodInfo m = t.GetMethod(method, F);
                    var reader = new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length);
                    if (m == null) { Debug.LogError("CLIVERIFY starequip handler missing: " + method); return reader; }
                    m.Invoke(ctrl, new object[] { reader });
                    return reader;
                }

                // ---- A. 23204 死号断言(裁决1 killlist:无 handler + Proto 无常量,同40218/40263先例) ----
                bool dead23204NoRecv = t.GetMethod("On23204", F) == null;
                bool dead23204NoProtoConst = !typeof(Shenxiao.Framework.Net.Proto).GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Any(fi => fi.FieldType == typeof(int) && fi.IsLiteral && (int)fi.GetRawConstantValue() == 23204);
                Debug.Log("CLIVERIFY starequip 23204 killlist noRecv=" + dead23204NoRecv + " noProtoConst=" + dead23204NoProtoConst);

                // ---- B. 23200 族错误出口(1500081 特判触发 COMPOSE_FAIL,ERROR 事件两次都发) ----
                int errEventCount = 0; int lastErrCode = 0;
                Action<int, string> onErr = (code, args) => { errEventCount++; lastErrCode = code; };
                Shenxiao.Framework.Event.EventDispatcher.On<int, string>(Shenxiao.Framework.Event.GlobalEvent.EVT_STAREQUIP_ERROR, onErr);
                int composeFailCountB = 0;
                Action onComposeFailB = () => composeFailCountB++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_STAREQUIP_COMPOSE_FAIL, onComposeFailB);
                Feed("On23200", new CliVerify.Pkt().I(1500).S("test_args").Bytes());
                Feed("On23200", new CliVerify.Pkt().I(1500081).S("").Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off<int, string>(Shenxiao.Framework.Event.GlobalEvent.EVT_STAREQUIP_ERROR, onErr);
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_STAREQUIP_COMPOSE_FAIL, onComposeFailB);
                bool b23200 = errEventCount == 2 && lastErrCode == 1500081 && composeFailCountB == 1;
                Debug.Log("CLIVERIFY starequip 23200 族错误出口 errEvents=" + errEventCount + " composeFail=" + composeFailCountB + " ok=" + b23200);

                // ---- C. 23201 总览(u16 TotalStar + PageItem[Attr attr_list]) + **尾哨兵字节游标核对** ----
                byte[] p23201 = new CliVerify.Pkt().H(5)
                    .H(1)
                        .I(1).L(1000).C(2).C(1)
                            .H(1).H(10).I(200)
                        .C(0)
                    .I(777888999) // 尾哨兵:紧跟在 PageItem(内含 attr_list 嵌套数组)读完之后
                    .Bytes();
                bool feed23201NoThrow = true;
                Shenxiao.Framework.Net.NetReader reader23201 = null;
                try { reader23201 = FeedReader("On23201", p23201); }
                catch (Exception e) { feed23201NoThrow = false; Debug.LogError("CLIVERIFY starequip 23201 threw: " + e); }
                bool b23201Fields = model.HasOverview && model.TotalStar == 5 && model.PageInfo.Count == 1
                    && model.PageInfo[1].Power == 1000 && model.PageInfo[1].NormalNum == 2 && model.PageInfo[1].SpecialNum == 1
                    && model.PageInfo[1].Attr.Count == 1 && model.PageInfo[1].Attr[0].AttrId == 10 && model.PageInfo[1].Attr[0].AttrVal == 200
                    && model.PageInfo[1].IsActive == 0;
                bool b23201Sentinel = reader23201 != null && reader23201.Remaining == 4 && reader23201.ReadU32() == 777888999;
                bool b23201 = feed23201NoThrow && b23201Fields && b23201Sentinel;
                Debug.Log("CLIVERIFY starequip 23201 总览嵌套 totalStar=" + model.TotalStar + " attr0=" + model.PageInfo[1].Attr[0].AttrVal
                    + " sentinelOk=" + b23201Sentinel + " noThrow(内部补发23205)=" + feed23201NoThrow + " ok=" + b23201);

                // ---- D. 23202 穿戴 / 23203 卸下(均无 Code,仅成功回本号,事件参数 goodsAutoId/goodsTypeId) ----
                long wearAutoId = 0, wearTypeId = 0; int wearEvents = 0;
                Action<long, long> onWear = (a, b) => { wearEvents++; wearAutoId = a; wearTypeId = b; };
                Shenxiao.Framework.Event.EventDispatcher.On<long, long>(Shenxiao.Framework.Event.GlobalEvent.EVT_STAREQUIP_WEAR_RESULT, onWear);
                Feed("On23202", new CliVerify.Pkt().L(5001).I(79010501).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off<long, long>(Shenxiao.Framework.Event.GlobalEvent.EVT_STAREQUIP_WEAR_RESULT, onWear);
                bool b23202 = wearEvents == 1 && wearAutoId == 5001 && wearTypeId == 79010501;

                long unwearAutoId = 0, unwearTypeId = 0; int unwearEvents = 0;
                Action<long, long> onUnwear = (a, b) => { unwearEvents++; unwearAutoId = a; unwearTypeId = b; };
                Shenxiao.Framework.Event.EventDispatcher.On<long, long>(Shenxiao.Framework.Event.GlobalEvent.EVT_STAREQUIP_UNWEAR_RESULT, onUnwear);
                Feed("On23203", new CliVerify.Pkt().L(5002).I(79010502).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off<long, long>(Shenxiao.Framework.Event.GlobalEvent.EVT_STAREQUIP_UNWEAR_RESULT, onUnwear);
                bool b23203 = unwearEvents == 1 && unwearAutoId == 5002 && unwearTypeId == 79010502;

                bool wearSendNoThrow = true;
                try { ctrlTyped.RequestWear(5003, 1, 1); ctrlTyped.RequestUnwear(1, 1); }
                catch (Exception e) { wearSendNoThrow = false; Debug.LogError("CLIVERIFY starequip 23202/03 send threw: " + e); }
                Debug.Log("CLIVERIFY starequip 23202/23203 穿卸 wear=" + b23202 + " unwear=" + b23203 + " sendNoThrow=" + wearSendNoThrow);

                // ---- E. 23205 星级大师查询 / 23206 升级(自带真实失败码,失败不覆盖) / 23251 被动推送(与23205分槽) ----
                Feed("On23205", new CliVerify.Pkt().H(3).H(5).H(10).I(500).Bytes());
                bool b23205 = model.StarMaster != null && model.StarMaster.Level == 3 && model.StarMaster.MaxLevel == 5
                    && model.StarMaster.Star == 10 && model.StarMaster.Power == 500;

                Feed("On23206", new CliVerify.Pkt().I(1).H(4).I(600).Bytes());
                bool b23206Success = model.StarMaster.Level == 4 && model.StarMaster.Power == 600;
                bool fail206NoThrow = true;
                try { Feed("On23206", new CliVerify.Pkt().I(99).H(0).I(0).Bytes()); }
                catch (Exception e) { fail206NoThrow = false; Debug.LogError("CLIVERIFY starequip 23206 fail threw: " + e); }
                bool b23206FailUnchanged = model.StarMaster.Level == 4 && model.StarMaster.Power == 600;
                bool b23206 = b23206Success && fail206NoThrow && b23206FailUnchanged;
                Debug.Log("CLIVERIFY starequip 23205/23206 starMaster level=" + model.StarMaster.Level + " power=" + model.StarMaster.Power
                    + " b23205=" + b23205 + " b23206=" + b23206);

                Feed("On23251", new CliVerify.Pkt().H(6).H(6).H(12).I(700).Bytes());
                bool b23251 = model.StarPush != null && model.StarPush.Level == 6 && model.StarPush.Star == 12 && model.StarPush.Power == 700
                    && model.StarMaster.Level == 4; // StarMaster(23205/06) 与 StarPush(23251) 分槽,互不覆盖
                Debug.Log("CLIVERIFY starequip 23251 星数被动推送 pushLevel=" + model.StarPush.Level + " starMasterUnaffected=" + (model.StarMaster.Level == 4) + " ok=" + b23251);

                // ---- F. 23207 吞噬信息 / 23208 筛选(成功/失败边界) / 23209 执行(**Color/Star跨号保持**) ----
                Feed("On23207", new CliVerify.Pkt().H(2).I(300).I(150).C(1).C(0).Bytes());
                bool b23207 = model.HasDevourInfo && model.Devour.Level == 2 && model.Devour.Exp == 300 && model.Devour.Power == 150
                    && model.Devour.Color == 1 && model.Devour.Star == 0;

                Feed("On23208", new CliVerify.Pkt().C(2).C(1).I(1).Bytes());
                bool b23208Success = model.Devour.Color == 2 && model.Devour.Star == 1;
                bool fail208NoThrow = true;
                try { Feed("On23208", new CliVerify.Pkt().C(9).C(9).I(99).Bytes()); }
                catch (Exception e) { fail208NoThrow = false; Debug.LogError("CLIVERIFY starequip 23208 fail threw: " + e); }
                bool b23208FailUnchanged = model.Devour.Color == 2 && model.Devour.Star == 1; // 失败不应把Color/Star改成9/9
                bool b23208 = b23208Success && fail208NoThrow && b23208FailUnchanged;
                Debug.Log("CLIVERIFY starequip 23207/23208 吞噬信息+筛选 color=" + model.Devour.Color + " star=" + model.Devour.Star + " ok=" + b23208);

                Feed("On23209", new CliVerify.Pkt().H(3).I(999).I(200).Bytes());
                bool b23209 = model.Devour.Level == 3 && model.Devour.Exp == 999 && model.Devour.Power == 200
                    && model.Devour.Color == 2 && model.Devour.Star == 1; // ⚠核心断言:23209无Color/Star字段,应保持23208的值不被清零
                bool devourSendNoThrow = true;
                try { ctrlTyped.RequestDevour(new List<long> { 9101, 9102 }); }
                catch (Exception e) { devourSendNoThrow = false; Debug.LogError("CLIVERIFY starequip 23209 send threw: " + e); }
                Debug.Log("CLIVERIFY starequip 23209 吞噬执行(Color/Star跨号保持) level=" + model.Devour.Level
                    + " color=" + model.Devour.Color + " star=" + model.Devour.Star + " sendNoThrow=" + devourSendNoThrow + " ok=" + (b23209 && devourSendNoThrow));

                // ---- G. 23250 六层嵌套预览(SendDsgt→DsgtSuit/DsgtAttr,StarAttrCfg 6字段) + **尾哨兵** ----
                byte[] p23250 = new CliVerify.Pkt().L(9001).I(888)
                    .H(1)
                        .I(501).H(2)
                            .H(1).H(10).I(50)   // DsgtSuit: 1项 {AttrId=10,AttrVal=50}
                            .H(0)                // DsgtAttr: 0项
                    .H(1)
                        .H(20).I(300).C(5).I(700).C(3).C(2) // StarAttrCfg: {AttrId,AttrVal,PlusInterval,PlusUnit,Color,TypeId}
                    .H(0) // StarAttr
                    .H(3) // SuitNum
                    .H(0) // SuitAttr
                        .H(1).H(1).I(1000) // BaseAttr: 1项
                    .H(0) // ExtraAttr
                    .H(0).H(0).H(0).H(0) // StrenAttr/EvoluAttr/MasterAttr/SpiritAttr
                    .I(1234) // BaseRating
                    .I(24681357) // 尾哨兵
                    .Bytes();
                var reader23250 = FeedReader("On23250", p23250);
                var preview = model.LastPreview;
                bool b23250Fields = preview != null && preview.GoodsAutoId == 9001 && preview.Score == 888
                    && preview.SendDsgt.Count == 1 && preview.SendDsgt[0].DsgtId == 501 && preview.SendDsgt[0].DsgtNum == 2
                    && preview.SendDsgt[0].DsgtSuit.Count == 1 && preview.SendDsgt[0].DsgtSuit[0].AttrId == 10 && preview.SendDsgt[0].DsgtSuit[0].AttrVal == 50
                    && preview.SendDsgt[0].DsgtAttr.Count == 0
                    && preview.StarAttrCfg.Count == 1 && preview.StarAttrCfg[0].AttrId == 20 && preview.StarAttrCfg[0].PlusInterval == 5
                    && preview.StarAttrCfg[0].PlusUnit == 700 && preview.StarAttrCfg[0].Color == 3 && preview.StarAttrCfg[0].TypeId == 2
                    && preview.SuitNum == 3 && preview.BaseAttr.Count == 1 && preview.BaseAttr[0].AttrVal == 1000 && preview.BaseRating == 1234;
                bool b23250Sentinel = reader23250.Remaining == 4 && reader23250.ReadU32() == 24681357;
                bool b23250 = b23250Fields && b23250Sentinel;
                Debug.Log("CLIVERIFY starequip 23250 六层嵌套预览 dsgtSuitAttrVal=" + (preview?.SendDsgt[0].DsgtSuit[0].AttrVal ?? -1)
                    + " starAttrCfgColor=" + (preview?.StarAttrCfg[0].Color ?? -1) + " sentinelOk=" + b23250Sentinel + " ok=" + b23250);

                // ---- H. 23252 合成四出口(成功/altSuccess/compose_fail不覆盖/未知失败降级不抛)+ 变长C2S发送侧 ----
                int composeSuccessEvents = 0; int lastSuccessRuleId = 0;
                Action<int> onComposeSuccess = ruleId => { composeSuccessEvents++; lastSuccessRuleId = ruleId; };
                Shenxiao.Framework.Event.EventDispatcher.On<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_STAREQUIP_COMPOSE_SUCCESS, onComposeSuccess);
                int composeFailCountH = 0;
                Action onComposeFailH = () => composeFailCountH++;
                Shenxiao.Framework.Event.EventDispatcher.On(Shenxiao.Framework.Event.GlobalEvent.EVT_STAREQUIP_COMPOSE_FAIL, onComposeFailH);

                Feed("On23252", new CliVerify.Pkt().I(1).I(79301).H(1).L(9501).I(520100).Bytes()); // code=1(?SUCCESS)
                bool b23252Success = model.LastComposeRuleId == 79301 && model.LastComposeReward.Count == 1
                    && model.LastComposeReward[0].GoodsId == 9501 && model.LastComposeReward[0].GoodsTypeId == 520100 && composeSuccessEvents == 1;

                Feed("On23252", new CliVerify.Pkt().I(1500080).I(79302).H(0).Bytes()); // code=1500080(err150_compose_success),空SendList
                bool b23252AltSuccess = model.LastComposeRuleId == 79302 && model.LastComposeReward.Count == 0 && composeSuccessEvents == 2 && lastSuccessRuleId == 79302;

                Feed("On23252", new CliVerify.Pkt().I(1500081).I(79303).H(0).Bytes()); // code=1500081(err150_compose_fail),不应覆盖79302缓存
                bool b23252Fail = model.LastComposeRuleId == 79302 && composeFailCountH == 1;

                bool composeUnknownNoThrow = true;
                try { Feed("On23252", new CliVerify.Pkt().I(99999).I(79304).H(0).Bytes()); } // 未知失败码 → ShowError 降级,不抛
                catch (Exception e) { composeUnknownNoThrow = false; Debug.LogError("CLIVERIFY starequip 23252 unknown threw: " + e); }
                bool b23252UnknownUnchanged = model.LastComposeRuleId == 79302; // 未知失败同样不应覆盖缓存

                Shenxiao.Framework.Event.EventDispatcher.Off<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_STAREQUIP_COMPOSE_SUCCESS, onComposeSuccess);
                Shenxiao.Framework.Event.EventDispatcher.Off(Shenxiao.Framework.Event.GlobalEvent.EVT_STAREQUIP_COMPOSE_FAIL, onComposeFailH);

                bool composeSendNoThrow = true;
                try { ctrlTyped.RequestCompose(79301, new List<long> { 1 }, new List<long> { 2 }, new List<long> { 3 }); }
                catch (Exception e) { composeSendNoThrow = false; Debug.LogError("CLIVERIFY starequip 23252 send threw: " + e); }

                bool b23252 = b23252Success && b23252AltSuccess && b23252Fail && composeUnknownNoThrow && b23252UnknownUnchanged && composeSendNoThrow;
                Debug.Log("CLIVERIFY starequip 23252 合成四出口 ruleId=" + model.LastComposeRuleId + " successEvents=" + composeSuccessEvents
                    + " failEvents=" + composeFailCountH + " sendNoThrow=" + composeSendNoThrow + " ok=" + b23252);

                // ---- I. 23253 解锁星宿页(成功原地置位+内部联动重拉23201不抛;失败不抛) ----
                int unlockEvents = 0; int lastUnlockPage = -1;
                Action<int> onUnlock = page => { unlockEvents++; lastUnlockPage = page; };
                Shenxiao.Framework.Event.EventDispatcher.On<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_STAREQUIP_UNLOCK_PAGE_RESULT, onUnlock);
                bool unlockNoThrow = true;
                try { Feed("On23253", new CliVerify.Pkt().I(1).I(1).Bytes()); } // Page=1,Code=1 → PageInfo[1] 由段C的IsActive=0置1
                catch (Exception e) { unlockNoThrow = false; Debug.LogError("CLIVERIFY starequip 23253 threw: " + e); }
                bool b23253Success = model.PageInfo[1].IsActive == 1 && unlockEvents == 1 && lastUnlockPage == 1;
                bool unlockFailNoThrow = true;
                try { Feed("On23253", new CliVerify.Pkt().I(1).I(99).Bytes()); }
                catch (Exception e) { unlockFailNoThrow = false; Debug.LogError("CLIVERIFY starequip 23253 fail threw: " + e); }
                Shenxiao.Framework.Event.EventDispatcher.Off<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_STAREQUIP_UNLOCK_PAGE_RESULT, onUnlock);
                bool b23253 = unlockNoThrow && b23253Success && unlockFailNoThrow;
                Debug.Log("CLIVERIFY starequip 23253 解锁星宿页 isActive=" + model.PageInfo[1].IsActive + " noThrow(内部重拉23201)=" + unlockNoThrow + " ok=" + b23253);

                // ---- J. 23254 蜕变对比预览(比23250多前置TargetGoodsAutoId,其余同构) ----
                byte[] p23254 = new CliVerify.Pkt().L(9001).L(9002).I(777)
                    .H(0) // SendDsgt
                    .H(0) // StarAttrCfg
                    .H(0) // StarAttr
                    .H(0) // SuitNum
                    .H(0) // SuitAttr
                        .H(1).H(5).I(50) // BaseAttr: 1项
                    .H(0) // ExtraAttr
                    .H(0).H(0).H(0).H(0) // StrenAttr/EvoluAttr/MasterAttr/SpiritAttr
                    .I(321) // BaseRating
                    .Bytes();
                Feed("On23254", p23254);
                var transform = model.LastTransformPreview;
                bool b23254 = transform != null && transform.GoodsAutoId == 9001 && transform.TargetGoodsAutoId == 9002 && transform.Score == 777
                    && transform.BaseAttr.Count == 1 && transform.BaseAttr[0].AttrVal == 50 && transform.BaseRating == 321;
                Debug.Log("CLIVERIFY starequip 23254 蜕变对比预览 targetId=" + (transform?.TargetGoodsAutoId ?? -1) + " ok=" + b23254);

                // ---- K. 23255 精简类型tips(GoodsTypeId wire是32位,比23250/23254少SuitAttr/锻造四段属性) ----
                byte[] p23255 = new CliVerify.Pkt().I(520100).I(888)
                    .H(0) // SendDsgt
                    .H(0) // StarAttrCfg
                    .H(0) // StarAttr
                    .H(7) // SuitNum
                        .H(1).H(3).I(99) // BaseAttr: 1项
                    .H(0) // ExtraAttr
                    .I(456) // BaseRating
                    .Bytes();
                Feed("On23255", p23255);
                bool hasTypePreview = model.TypePreviewCache.TryGetValue(520100, out var typePreview);
                bool b23255 = hasTypePreview && typePreview.GoodsTypeId == 520100 && typePreview.Score == 888 && typePreview.SuitNum == 7
                    && typePreview.BaseAttr.Count == 1 && typePreview.BaseAttr[0].AttrVal == 99 && typePreview.BaseRating == 456;
                Debug.Log("CLIVERIFY starequip 23255 精简类型tips suitNum=" + (typePreview?.SuitNum ?? -1) + " ok=" + b23255);

                // ---- L. 23256 合成次数分桶 ----
                Feed("On23256", new CliVerify.Pkt().I(79301).H(3).H(8).H(5).Bytes());
                bool hasComposeTime = model.ComposeTime.TryGetValue(79301, out var composeTime);
                bool b23256 = hasComposeTime && composeTime.Times == 3 && composeTime.Index == 8 && composeTime.Num == 5;
                Debug.Log("CLIVERIFY starequip 23256 合成次数 times=" + (composeTime?.Times ?? -1) + " ok=" + b23256);

                // ---- M. 23257 蜕变执行(单字段Res,老端空if块,本端补发事件供尾包消费) ----
                int transformEvents = 0; bool lastTransformOk = false;
                Action<bool> onTransform = ok => { transformEvents++; lastTransformOk = ok; };
                Shenxiao.Framework.Event.EventDispatcher.On<bool>(Shenxiao.Framework.Event.GlobalEvent.EVT_STAREQUIP_TRANSFORM_RESULT, onTransform);
                Feed("On23257", new CliVerify.Pkt().I(1).Bytes());
                Feed("On23257", new CliVerify.Pkt().I(0).Bytes());
                Shenxiao.Framework.Event.EventDispatcher.Off<bool>(Shenxiao.Framework.Event.GlobalEvent.EVT_STAREQUIP_TRANSFORM_RESULT, onTransform);
                bool b23257 = transformEvents == 2 && lastTransformOk == false; // 第二次res=0 → ok=false
                bool transformSendNoThrow = true;
                try { ctrlTyped.RequestTransform(9001, 9002); }
                catch (Exception e) { transformSendNoThrow = false; Debug.LogError("CLIVERIFY starequip 23257 send threw: " + e); }
                Debug.Log("CLIVERIFY starequip 23257 蜕变执行 events=" + transformEvents + " lastOk=" + lastTransformOk + " sendNoThrow=" + transformSendNoThrow + " ok=" + (b23257 && transformSendNoThrow));

                model.Clear();
                bool clearOk = !model.HasOverview && model.PageInfo.Count == 0 && model.StarMaster == null && !model.HasDevourInfo;

                bool pass = configCountOk && configRowOk
                    && dead23204NoRecv && dead23204NoProtoConst
                    && b23200 && b23201 && b23202 && b23203 && wearSendNoThrow
                    && b23205 && b23206 && b23251
                    && b23207 && b23208 && b23209 && devourSendNoThrow
                    && b23250
                    && b23252
                    && b23253
                    && b23254 && b23255 && b23256
                    && b23257 && transformSendNoThrow
                    && clearOk;

                Debug.Log("CLIVERIFY starequip VERDICT config=" + (configCountOk && configRowOk)
                    + " dead23204=" + (dead23204NoRecv && dead23204NoProtoConst)
                    + " err200=" + b23200 + " overview201=" + b23201 + " wear202=" + b23202 + " unwear203=" + b23203
                    + " starmaster(205/06)=" + b23206 + " starpush251=" + b23251
                    + " devour(207/08/09)=" + (b23207 && b23208 && b23209)
                    + " preview250=" + b23250 + " compose252=" + b23252 + " unlock253=" + b23253
                    + " transformPreview254=" + b23254 + " typeTips255=" + b23255 + " composeTime256=" + b23256
                    + " transform257=" + b23257 + " clearOk=" + clearOk + " pass=" + pass);

                return pass ? 0 : 3;
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }
        }
    }
}
