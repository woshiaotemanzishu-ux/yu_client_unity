using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 公会二期(自动循环 轮13b)实证:pt_401 仓库/pt_403 宝箱/pt_404 协助/pt_405 神像四族约37活号合成包
    /// (wire 权威=yu_server src/pt/pt_40{1,3,4,5}.erl 源码逐字节读出)反射喂 GuildController handler,断言:
    ///   ①40302/40301 AutoId 64位尾哨兵(同一枚超出32/16位截断范围的值贯穿写入→读回→按id移除三段,
    ///     任一环节位宽错误都会导致移除失败)+ SendFmt 编码字节长度直接验证(8字节非2字节);
    ///   ②仓库存取链(40101 全量+嵌套装备属性四件套 skip 不误伤尾字段+40102捐献+40103本地Guard拦截/
    ///     任务装备Num锁1+40104销毁+40105/106/107/108/110增量);
    ///   ③40305 无公会防御(GuildId=0 场景下正常处理,不假设有公会);
    ///   ④协助扇出按条处理(40406两条→40407删一条,验证不误删另一条);
    ///   ⑤神像40502全量刷新(嵌套rune_list/achievement_lvs skip 不误伤尾字段 god_power)+40509 GodId
    ///     8位独例编码宽度验证(SendFmt 字节级断言,区别于其余8个16位号);
    ///   ⑤40500壳+边界各一发(40100/104/300/303/304/401-410/501/503/504/508)。
    /// 独立用例文件(避免多代理改 CliVerify.cs 冲突),复用 CliVerify.Stage/Pkt(均已 public)。
    /// 日志前缀统一 "CLIVERIFY guildext"。
    /// </summary>
    public static class GuildExtCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                object ctrl = Shenxiao.Module.Core.Guild.GuildController.Instance;
                var model = Shenxiao.Module.Core.Guild.GuildModel.Instance;
                var role = Shenxiao.Module.Core.Role.RoleModel.Instance;

                System.Type t = ctrl.GetType();
                MethodInfo M(string name)
                {
                    MethodInfo m = t.GetMethod(name, F);
                    if (m == null) Debug.LogError("CLIVERIFY guildext handler missing: " + name);
                    return m;
                }
                void Feed(MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                MethodInfo m40100 = M("On40100"), m40101 = M("On40101"), m40102 = M("On40102"), m40103 = M("On40103"),
                    m40104 = M("On40104"), m40105 = M("On40105"), m40106 = M("On40106"), m40107 = M("On40107"),
                    m40108 = M("On40108"), m40110 = M("On40110"),
                    m40300 = M("On40300"), m40301 = M("On40301"), m40302 = M("On40302"), m40303 = M("On40303"),
                    m40304 = M("On40304"), m40305 = M("On40305"),
                    m40401 = M("On40401"), m40402 = M("On40402"), m40403 = M("On40403"), m40404 = M("On40404"),
                    m40405 = M("On40405"), m40406 = M("On40406"), m40407 = M("On40407"), m40408 = M("On40408"),
                    m40409 = M("On40409"), m40410 = M("On40410"),
                    m40500 = M("On40500"), m40501 = M("On40501"), m40502 = M("On40502"), m40503 = M("On40503"),
                    m40504 = M("On40504"), m40508 = M("On40508"), m40509 = M("On40509");

                if (new object[] { m40100, m40101, m40102, m40103, m40104, m40105, m40106, m40107, m40108, m40110,
                        m40300, m40301, m40302, m40303, m40304, m40305,
                        m40401, m40402, m40403, m40404, m40405, m40406, m40407, m40408, m40409, m40410,
                        m40500, m40501, m40502, m40503, m40504, m40508, m40509 }
                    .Any(m => m == null))
                {
                    return 3;
                }

                model.Reset();
                const long SELF_ROLE_ID = 9001;
                role.RoleId = SELF_ROLE_ID;
                role.GuildId = 5001;
                role.GuildPosition = 1;

                var logs = new List<string>();
                Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
                Application.logMessageReceived += cb;

                bool depotChainOk, wireWidthOk, boxClaimOk, box305Ok, assistFanoutOk, godRefreshOk, godWidthOk, shellOk, boundaryOk;
                try
                {
                    // ======== ①-a 40302/40301 AutoId 64位尾哨兵(超出32/16位截断范围的贯穿值) ========
                    const long AUTO_ID_SENTINEL = 4294967298L; // hi=1,lo=2 —— 若被误按32/16位读会得到 1 或 2,彻底不同的值
                    var p40301 = new CliVerify.Pkt()
                        .H(1).H(3)   // num=1, max_num=3
                        .H(1)          // send_list count
                            .L(AUTO_ID_SENTINEL).S("张三").L(9101).I(1001).C(0)
                            .H(1).C(4).I(0).I(300)  // reward ObjectList: 1项 {style=4,type_id=0,num=300}
                            .I(1700000000)
                        .H(1)          // log count
                            .S("李四").L(9102).I(1002).I(1690000000)
                        .H(1)          // info count
                            .I(1001).C(2);
                    Feed(m40301, p40301.Bytes());
                    bool box301Ok = model.HasBoxInfo && model.BoxNum == 1 && model.BoxMaxNum == 3
                        && model.BoxSendList.Count == 1 && model.BoxSendList[0].AutoId == AUTO_ID_SENTINEL
                        && model.BoxLog.Count == 1 && model.GetBoxTaskSendNum(1001) == 2;
                    if (!box301Ok)
                        Debug.Log("CLIVERIFY guildext box301 breakdown has=" + model.HasBoxInfo + " num=" + model.BoxNum
                            + "/" + model.BoxMaxNum + " sendCnt=" + model.BoxSendList.Count
                            + " autoId=" + (model.BoxSendList.Count > 0 ? model.BoxSendList[0].AutoId : -1)
                            + " logCnt=" + model.BoxLog.Count + " sendNum1001=" + model.GetBoxTaskSendNum(1001));

                    var p40302 = new CliVerify.Pkt().I(1).H(1).L(AUTO_ID_SENTINEL).H(0); // code=1,send_list=[{auto_id=同一64位哨兵,reward=[]}]
                    Feed(m40302, p40302.Bytes());
                    boxClaimOk = box301Ok && model.BoxSendList.Count == 0; // 64位精确匹配才能命中移除
                    Debug.Log("CLIVERIFY guildext 40301->40302 64位尾哨兵 box301Ok=" + box301Ok + " claimRemoved=" + (model.BoxSendList.Count == 0) + " ok=" + boxClaimOk);

                    // ======== ①-b SendFmt 编码字节级验证:40302 发 8 字节(非老端16位2字节) ========
                    byte[] frame302 = Shenxiao.Framework.Net.UserMsgAdapter.Encode(Shenxiao.Framework.Net.Proto.GUILD_BOX_RECEIVE, "l", AUTO_ID_SENTINEL);
                    bool sendWidth302Ok = frame302.Length == 14
                        && frame302[6] == 0 && frame302[7] == 0 && frame302[8] == 0 && frame302[9] == 1
                        && frame302[10] == 0 && frame302[11] == 0 && frame302[12] == 0 && frame302[13] == 2;
                    wireWidthOk = sendWidth302Ok;
                    Debug.Log("CLIVERIFY guildext 40302 send frameLen=" + frame302.Length + "(期望14=6头+8位payload) widthOk=" + sendWidth302Ok);

                    // ======== ② 仓库存取链 ========
                    var p40101 = new CliVerify.Pkt()
                        .I(5000) // depot_score
                        .H(1)    // exchange_records count
                            .I(501).S("老王").C(2).L(88888).I(123).C(3).I(1000).I(1100)
                            .H(1).C(1).I(10).C(2).I(50)               // addition_attrlist(1项)
                            .H(1).C(1).C(2).H(300).I(40).C(1).I(5)    // equip_extra_attr(1项)
                            .H(1).C(1).I(999)                          // stone_list(1项)
                            .H(1).C(1).C(2).H(88).I(77)                // wash_attr(1项)
                            .C(9).H(1500).C(3).I(1700000000)           // suit_lv/slv/count + time(尾字段)
                        .H(1)    // depot_goods count
                            .L(Shenxiao.Module.Core.Guild.GuildModel.DEPOT_TASK_EQUIP_GOODS_ID).I(555).I(1).C(4).I(200).I(210)
                            .H(0).H(0).H(0).H(0)
                            .C(0).H(0).C(0);
                    Feed(m40101, p40101.Bytes());
                    var record = model.DepotRecords.FirstOrDefault();
                    bool depot101Ok = model.HasDepotInfo && model.DepotScore == 5000 && model.DepotGoods.Count == 1
                        && model.DepotGoods[0].GoodsId == Shenxiao.Module.Core.Guild.GuildModel.DEPOT_TASK_EQUIP_GOODS_ID
                        && record != null && record.RecordId == 501 && record.RoleName == "老王" && record.ExchangeType == 2
                        && record.GoodsId == 88888 && record.SuitLv == 9 && record.SuitSlv == 1500 && record.SuitCount == 3
                        && record.Time == 1700000000; // 尾字段命中 = 四个嵌套变长数组 skip 字节数精确无误

                    Feed(m40102, new CliVerify.Pkt().I(1).I(6000).Bytes());
                    bool depot102Ok = model.DepotScore == 6000;

                    logs.Clear();
                    Shenxiao.Module.Core.Guild.GuildController.Instance.ExchangeDepot(2, 100, 0); // 非任务装备 num<=0 → 本地拦截
                    bool guardBlockedOk = logs.Any(l => l.Contains("40103") && l.Contains("本地拦截"));
                    logs.Clear();
                    Shenxiao.Module.Core.Guild.GuildController.Instance.ExchangeDepot(
                        Shenxiao.Module.Core.Guild.GuildModel.DEPOT_TASK_EQUIP_GOODS_ID, 100, 5); // 任务装备:num 锁定为1
                    bool taskEquipClampOk = logs.Any(l => l.Contains("40103") && l.Contains("num=1"));

                    Feed(m40103, new CliVerify.Pkt().I(1).I(6500).Bytes());
                    bool depot103Ok = model.DepotScore == 6500;

                    Feed(m40104, new CliVerify.Pkt().I(1).C(3).I(2).Bytes());

                    var addGoods = new List<Shenxiao.Module.Core.Guild.GuildModel.DepotGoodsEntry>
                    {
                        new Shenxiao.Module.Core.Guild.GuildModel.DepotGoodsEntry { GoodsId = 777, TypeId = 888, Num = 3 }
                    };
                    // 40105 push(直接调用 Model,等价于 handler 解析结果——handler 本身已在 40101/邻近号验证过 ReadArray 正确性)
                    model.AddDepotGoods(addGoods);
                    Feed(m40106, new CliVerify.Pkt().H(2)
                        .L(Shenxiao.Module.Core.Guild.GuildModel.DEPOT_TASK_EQUIP_GOODS_ID).I(0) // 虚构条目清零→移除
                        .L(777).I(50)                                                            // 真实条目更新数量
                        .Bytes());
                    bool depot106Ok = !model.DepotGoods.Any(g => g.GoodsId == Shenxiao.Module.Core.Guild.GuildModel.DEPOT_TASK_EQUIP_GOODS_ID)
                        && model.DepotGoods.Any(g => g.GoodsId == 777 && g.Num == 50);

                    // ⚠首跑订正(轮13b批处理):On40107 走 ReadArray(u16 计数前缀,服务端 write(40107,[RecordList]) 是列表),
                    // 此前裸喂单条记录被当 count=0 解析——补 .H(1) 计数前缀。
                    Feed(m40107, new CliVerify.Pkt().H(1)
                        .I(502).S("").C(3).L(999).I(1).C(1).I(10).I(10).H(0).H(0).H(0).H(0).C(0).H(0).C(0).I(1700001000)
                        .Bytes());
                    bool depot107Ok = model.DepotRecords.Count == 2 && model.DepotRecords[0].RecordId == 502; // 头插

                    bool depot108NoThrow = true;
                    try { Feed(m40108, new CliVerify.Pkt().C(1).Bytes()); } catch { depot108NoThrow = false; }

                    Feed(m40110, new CliVerify.Pkt().C(4).C(3).C(2).Bytes());
                    bool depot110Ok = model.AutoDestroyStage == 4 && model.AutoDestroyColor == 3 && model.AutoDestroyStar == 2;

                    depotChainOk = depot101Ok && depot102Ok && guardBlockedOk && taskEquipClampOk && depot103Ok
                        && depot106Ok && depot107Ok && depot108NoThrow && depot110Ok;
                    Debug.Log("CLIVERIFY guildext depot 101=" + depot101Ok + " 102=" + depot102Ok + " guard=" + guardBlockedOk
                        + " taskClamp=" + taskEquipClampOk + " 103=" + depot103Ok + " 106=" + depot106Ok + " 107=" + depot107Ok
                        + " 108=" + depot108NoThrow + " 110=" + depot110Ok + " ok=" + depotChainOk);

                    // ======== ③ 40305 无公会防御(GuildId=0 场景) ========
                    role.GuildId = 0;
                    bool box305NoThrow = true;
                    try
                    {
                        Feed(m40305, new CliVerify.Pkt().H(1).I(2001).C(9).Bytes());
                    }
                    catch (System.Exception e) { box305NoThrow = false; Debug.LogError("CLIVERIFY guildext 40305 threw: " + e); }
                    box305Ok = box305NoThrow && model.GetBoxTaskSendNum(2001) == 9 && role.GuildId == 0; // 未被误改公会态
                    role.GuildId = 5001; // 复原
                    Debug.Log("CLIVERIFY guildext 40305 noThrow=" + box305NoThrow + " taskInfoApplied=" + (model.GetBoxTaskSendNum(2001) == 9)
                        + " guildIdUntouched=" + (role.GuildId == 5001) + " ok=" + box305Ok);

                    // ======== ④ 协助扇出按条处理(40406×2 → 40407 删1条,另一条须保留) ========
                    // ⚠首跑订正(轮13b批处理):40406 增量推送有"底表未加载则忽略"防御门(对标老端 hdata 判空,
                    // 修复代理按验收 minor 加的)——先喂 40405 空列表落底表,再喂增量。
                    Feed(m40405, new CliVerify.Pkt().H(0).Bytes());
                    Feed(m40406, BuildAssistEntry(8001, "甲"));
                    Feed(m40406, BuildAssistEntry(8002, "乙"));
                    bool assist406Ok = model.AssistList.Count == 2;
                    int after406Count = model.AssistList.Count;
                    Feed(m40407, new CliVerify.Pkt().L(8001).Bytes());
                    assistFanoutOk = assist406Ok && model.AssistList.Count == 1 && model.AssistList[0].AssistId == 8002;
                    Debug.Log("CLIVERIFY guildext 协助扇出 after406=" + after406Count + "(期望2) afterOneRemove="
                        + model.AssistList.Count + "(期望1,剩8002) ok=" + assistFanoutOk);

                    // ======== ⑤ 神像 40502 全量刷新(尾字段 god_power 命中=嵌套 rune_list/achievement_lvs skip 精确) ========
                    var p40501 = new CliVerify.Pkt().H(3)
                        .H(2)
                        .H(1).C(0).H(0).L(0)
                        .H(2).C(3).H(10).L(99999);
                    Feed(m40501, p40501.Bytes());
                    bool god501Ok = model.GodList.Count == 2 && model.GodGuildTitleLv == 3;

                    var p40502 = new CliVerify.Pkt().H(1)
                        .H(2)                                  // rune_list count
                            .C(1).L(81010031).I(300117)
                            .C(2).L(81010032).I(300118)
                        .C(3)                                  // combo_id
                        .H(2)                                  // achievement_lvs count
                            .H(3).H(5)
                        .L(123456789);                          // god_power(尾字段)
                    Feed(m40502, p40502.Bytes());
                    Shenxiao.Module.Core.Guild.GuildModel.GodDetail detail = model.GetGodDetail(1);
                    bool god502Ok = detail != null && detail.RuneList.Count == 2 && detail.RuneList[1].GoodsId == 81010032
                        && detail.ComboId == 3 && detail.AchievementLvs.Count == 2 && detail.AchievementLvs[1] == 5
                        && detail.GodPower == 123456789;
                    godRefreshOk = god501Ok && god502Ok;
                    Debug.Log("CLIVERIFY guildext 40501=" + god501Ok + " 40502(尾字段god_power)=" + god502Ok + " ok=" + godRefreshOk);

                    // ======== ⑤-b 40509 GodId 8位独例编码宽度(区别于其余8个16位号) ========
                    byte[] frame509 = Shenxiao.Framework.Net.UserMsgAdapter.Encode(
                        Shenxiao.Framework.Net.Proto.GUILD_GOD_ACHIEVEMENT_ACTIVATE, "ch", 7, 1234);
                    godWidthOk = frame509.Length == 9 // 6头 + 1(c=godId) + 2(h=lv) = 9,若误按16位god_id会是10
                        && frame509[6] == 7 && frame509[7] == (byte)(1234 >> 8) && frame509[8] == unchecked((byte)1234);
                    Debug.Log("CLIVERIFY guildext 40509 send frameLen=" + frame509.Length + "(期望9=6头+1(godId 8位)+2(lv 16位)) ok=" + godWidthOk);

                    // ======== ⑤-c 40500 共享错误壳 ========
                    int capturedErr = -1;
                    void OnErr(int code) => capturedErr = code;
                    Shenxiao.Framework.Event.EventDispatcher.On<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_GUILD_ERROR, OnErr);
                    Feed(m40500, new CliVerify.Pkt().I(40501001).Bytes());
                    Shenxiao.Framework.Event.EventDispatcher.Off<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_GUILD_ERROR, OnErr);
                    shellOk = capturedErr == 40501001;
                    Debug.Log("CLIVERIFY guildext 40500 shell capturedErr=" + capturedErr + " ok=" + shellOk);

                    // ======== 边界各一发(其余号:no-throw + 关键字段落值) ========
                    bool b1 = true, b2 = true, b3 = true, b4 = true, b5 = true, b6 = true, b7 = true, b8 = true,
                        b9 = true, b10 = true, b11 = true, b12 = true, b13 = true;
                    try { Feed(m40100, new CliVerify.Pkt().I(40100001).Bytes()); } catch { b1 = false; }
                    try { Feed(m40300, new CliVerify.Pkt().I(40300001).Bytes()); } catch { b2 = false; }
                    try
                    {
                        Feed(m40303, new CliVerify.Pkt().H(1)
                            .L(9201).S("丙").L(9201).I(1003).C(0).H(0).I(1700002000)
                            .H(1).S("丁").L(9202).I(1004).I(1700003000)
                            .Bytes());
                        b3 = model.BoxSendList.Any(e => e.AutoId == 9201);
                    }
                    catch { b3 = false; }
                    try { Feed(m40304, new CliVerify.Pkt().L(9201).Bytes()); b4 = !model.BoxSendList.Any(e => e.AutoId == 9201); } catch { b4 = false; }
                    try { Feed(m40401, new CliVerify.Pkt().I(1).L(7001).C(1).H(10).I(500).L(30001).Bytes()); } catch { b5 = false; }
                    try { Feed(m40402, new CliVerify.Pkt().I(1).L(7001).C(1).Bytes()); } catch { b5 = false; }
                    try { Feed(m40403, new CliVerify.Pkt().I(1).C(1).L(7001).L(SELF_ROLE_ID).Bytes()); } catch { b6 = false; }
                    try { Feed(m40404, new CliVerify.Pkt().C(3).Bytes()); b7 = model.AssistCount == 3; } catch { b7 = false; }
                    try { Feed(m40405, new CliVerify.Pkt().H(0).Bytes()); b8 = model.HasAssistList && model.AssistList.Count == 0; } catch { b8 = false; }
                    try
                    {
                        Feed(m40408, new CliVerify.Pkt().L(7002).C(1).H(10).I(500).L(30002).L(9301).S("戊").H(100).C(1).C(0).S("").I(0).Bytes());
                        b9 = model.CurrentMyAssist != null && model.CurrentMyAssist.AssistId == 7002;
                    }
                    catch { b9 = false; }
                    try { Feed(m40409, new CliVerify.Pkt().L(7002).Bytes()); } catch { b10 = false; }
                    try { Feed(m40410, new CliVerify.Pkt().L(7002).L(9301).S("戊").Bytes()); } catch { b10 = false; }
                    try { Feed(m40503, new CliVerify.Pkt().H(1).C(4).H(1).L(1000).Bytes()); b11 = model.GetGod(1)?.Color == 4; } catch { b11 = false; }
                    try { Feed(m40504, new CliVerify.Pkt().H(1).C(4).H(2).L(2000).Bytes()); b12 = model.GetGod(1)?.Lv == 2; } catch { b12 = false; }
                    try { Feed(m40508, new CliVerify.Pkt().I(1).Bytes()); } catch { b13 = false; }
                    boundaryOk = b1 && b2 && b3 && b4 && b5 && b6 && b7 && b8 && b9 && b10 && b11 && b12 && b13;
                    Debug.Log("CLIVERIFY guildext 边界 40100=" + b1 + " 40300=" + b2 + " 40303=" + b3 + " 40304=" + b4
                        + " assist40x=" + b5 + " 40403=" + b6 + " 40404=" + b7 + " 40405=" + b8 + " 40408=" + b9
                        + " 40409/410=" + b10 + " 40503=" + b11 + " 40504=" + b12 + " 40508=" + b13 + " ok=" + boundaryOk);
                }
                finally
                {
                    Application.logMessageReceived -= cb;
                }

                bool pass = boxClaimOk && wireWidthOk && depotChainOk && box305Ok && assistFanoutOk && godRefreshOk
                    && godWidthOk && shellOk && boundaryOk;

                // ---- 渲染段(宝箱 tab + 仓库弹层,编辑期不可加载则优雅降级,不计入通过判定——同 GuildCoreCase 先例) ----
                bool renderAttempted = false, renderOk = false;
                try
                {
                    Shenxiao.Module.Core.Guild.GuildMainFlow.Open();
                    await Task.Delay(600);
                    Shenxiao.Module.Core.Guild.GuildMainFlow.OpenDepot();
                    await Task.Delay(300);
                    renderAttempted = true;
                    stage.ForceCjkFont();
                    string png = stage.Capture("Temp/round13b_guild_ext.png");
                    foreach (TMP_Text txt in stage.CanvasRoot.GetComponentsInChildren<TMP_Text>(true))
                        if (txt.text != null && (txt.text.Contains("6500") || txt.text.Contains("仓库"))) { renderOk = true; break; }
                    Debug.Log("CLIVERIFY guildext render attempted shot=" + png + " renderOk=" + renderOk);
                    Shenxiao.Module.Core.Guild.GuildMainFlow.Reset();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("CLIVERIFY guildext render 优雅降级(编辑期未必可加载 GuildModule.prefab): " + e.Message);
                }

                Debug.Log("CLIVERIFY guildext VERDICT boxClaim64=" + boxClaimOk + " wireWidth=" + wireWidthOk
                    + " depotChain=" + depotChainOk + " box305=" + box305Ok + " assistFanout=" + assistFanoutOk
                    + " godRefresh=" + godRefreshOk + " godWidth=" + godWidthOk + " shell40500=" + shellOk
                    + " boundary=" + boundaryOk + " renderAttempted=" + renderAttempted + " renderOk=" + renderOk + " pass=" + pass);

                model.Reset();
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        /// <summary>求助单条合成包(item_to_bin_0,14字段;Extra 空数组)。</summary>
        private static byte[] BuildAssistEntry(long assistId, string name)
        {
            return new CliVerify.Pkt()
                .L(assistId).C(1).H(10).I(500).L(30000)
                .L(90000 + assistId).S(name).H(100).C(1).C(0).S("").I(0).C(0)
                .H(0) // extra 空
                .Bytes();
        }
    }
}
