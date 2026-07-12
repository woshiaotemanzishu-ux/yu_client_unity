using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 公会核心一期(自动循环 轮13a)实证:GuildController 33 活号合成包反射喂 handler,断言 GuildModel 落值 +
    /// 关键裁决点(40009 订正删单条/40021 权限 Contains 修正/40013 双错误通道/40018 广播recv/40012 等级门
    /// 失败码/40006 大列表尾哨兵完整性),尾段拉起 GuildMainFlow 渲染断言信息/成员两页(编辑期不可加载则
    /// 优雅降级,不计入通过判定——同 DungeonFamilyCase 先例)。
    /// 独立用例文件(避免多代理改 CliVerify.cs 冲突),复用 CliVerify.Stage/Pkt/FindDeep(均已 public)。
    /// 日志前缀统一 "CLIVERIFY guildcore"。
    /// </summary>
    public static class GuildCoreCase
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
                    if (m == null) Debug.LogError("CLIVERIFY guildcore handler missing: " + name);
                    return m;
                }
                void Feed(MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                MethodInfo m40000 = M("On40000"), m40005 = M("On40005"), m40006 = M("On40006"), m40007 = M("On40007"),
                    m40008 = M("On40008"), m40009 = M("On40009"), m40010 = M("On40010"), m40011 = M("On40011"),
                    m40012 = M("On40012"), m40013 = M("On40013"), m40014 = M("On40014"), m40015 = M("On40015"),
                    m40016 = M("On40016"), m40017 = M("On40017"), m40018 = M("On40018"), m40019 = M("On40019"),
                    m40020 = M("On40020"), m40021 = M("On40021"), m40023 = M("On40023"), m40027 = M("On40027"),
                    m40028 = M("On40028"), m40030 = M("On40030"), m40031 = M("On40031"), m40039 = M("On40039"),
                    m40040 = M("On40040"), m40042 = M("On40042"), m40043 = M("On40043"), m40044 = M("On40044"),
                    m40060 = M("On40060"), m40061 = M("On40061"), m40062 = M("On40062"), m40063 = M("On40063");
                if (new object[] { m40000, m40005, m40006, m40007, m40008, m40009, m40010, m40011, m40012, m40013,
                        m40014, m40015, m40016, m40017, m40018, m40019, m40020, m40021, m40023, m40027, m40028,
                        m40030, m40031, m40039, m40040, m40042, m40043, m40044, m40060, m40061, m40062, m40063 }
                    .Any(m => m == null))
                {
                    return 3;
                }

                model.Reset();
                const long SELF_ROLE_ID = 9001;
                role.RoleId = SELF_ROLE_ID;
                role.GuildId = 0;
                role.GuildPosition = 0;

                var logs = new List<string>();
                Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
                Application.logMessageReceived += cb;

                bool infoOk, membersOk, applyApproveOk, permissionOk, level12Ok, mutexDualChannelOk,
                    renameChainOk, upgradeBroadcastOk, boundaryOk, selfInfoOk;
                try
                {
                    // ---- 40015 自身信息(先行,驱动 RoleModel 落值 + position==1 补发40008 联动) ----
                    Feed(m40015, new CliVerify.Pkt().L(5001).S("天穹盟").H(5).C(1).S("会长").Bytes());
                    selfInfoOk = role.GuildId == 5001 && role.GuildName == "天穹盟" && role.GuildPosition == 1
                        && role.GuildPositionName == "会长" && Shenxiao.Module.Core.Guild.GuildModel.IsGuildMaster();
                    Debug.Log("CLIVERIFY guildcore 40015 guildId=" + role.GuildId + " position=" + role.GuildPosition + " ok=" + selfInfoOk);

                    // ---- 40005 基础信息批量链(position_list 内嵌 Figure,GetTopMember(1) 取会长名) ----
                    var p40005 = new CliVerify.Pkt().L(5001).S("天穹盟").S("欢迎大家踊跃发言").H(1).C(1).L(SELF_ROLE_ID);
                    AppendFigure(p40005, "盟主甲", 1, 88, 2);
                    p40005.H(5).I(1000).I(200).I(50).H(10).H(45).L(99999).H(3).I(0).C(0).C(0).I(1700000000).C(0);
                    Feed(m40005, p40005.Bytes());
                    Shenxiao.Module.Core.Guild.GuildModel.GuildInfo info = model.Info;
                    infoOk = info != null && info.GuildId == 5001 && info.GuildName == "天穹盟" && info.GuildLv == 5
                        && info.MemberCapacity == 45 && info.CombatPower == 99999
                        && model.GetTopMember(1)?.Name == "盟主甲";
                    Debug.Log("CLIVERIFY guildcore 40005 guildId=" + info?.GuildId + " lv=" + info?.GuildLv
                        + " chief=" + model.GetTopMember(1)?.Name + " ok=" + infoOk);

                    // ---- 40006 大列表(40条,近 member_capacity=45 上限)+ 尾哨兵完整性(末条字段不因长列表偏移损坏) ----
                    const int MEMBER_COUNT = 40;
                    var p40006 = new CliVerify.Pkt().H(MEMBER_COUNT);
                    for (int i = 0; i < MEMBER_COUNT; i++)
                    {
                        bool isLast = i == MEMBER_COUNT - 1;
                        long roleId = isLast ? 29999 : (i == 0 ? SELF_ROLE_ID : 20000 + i);
                        string name = isLast ? "哨兵尾" : "member" + i;
                        long combat = isLast ? 88888 : 1000 + i;
                        p40006.L(roleId);
                        AppendFigure(p40006, name, 1, 50 + i % 50, 0);
                        p40006.C(i == 0 ? 1 : 3).I(0).L(combat).C(i % 2 == 0 ? 1 : 0).I(3600 * i).I(1700000000 + i);
                    }
                    Feed(m40006, p40006.Bytes());
                    Shenxiao.Module.Core.Guild.GuildModel.MemberEntry sentinel =
                        model.Members.FirstOrDefault(mm => mm.RoleId == 29999);
                    membersOk = model.HasMembers && model.Members.Count == MEMBER_COUNT
                        && sentinel != null && sentinel.Name == "哨兵尾" && sentinel.CombatPower == 88888
                        && model.Members[0].RoleId == SELF_ROLE_ID; // 自己置顶(SetMembers 排序对标老端)
                    Debug.Log("CLIVERIFY guildcore 40006 count=" + model.Members.Count + " sentinelOk="
                        + (sentinel != null && sentinel.Name == "哨兵尾" && sentinel.CombatPower == 88888)
                        + " selfTopOk=" + (model.Members.Count > 0 && model.Members[0].RoleId == SELF_ROLE_ID) + " ok=" + membersOk);

                    // ---- 40008 申请列表 → 40009 审批(**订正删单条**,rule10;非老端 splice(i,2) 双删) ----
                    var p40008 = new CliVerify.Pkt().H(3);
                    p40008.L(101); AppendFigure(p40008, "甲", 1, 30, 0); p40008.L(500);
                    p40008.L(102); AppendFigure(p40008, "乙", 2, 31, 0); p40008.L(600);
                    p40008.L(103); AppendFigure(p40008, "丙", 3, 32, 0); p40008.L(700);
                    Feed(m40008, p40008.Bytes());
                    bool applyListOk = model.Applies.Count == 3;
                    Feed(m40009, new CliVerify.Pkt().I(1).C(1).L(102).Bytes()); // errorCode=1,type=1,roleId=102
                    applyApproveOk = applyListOk && model.Applies.Count == 2
                        && model.Applies.Any(a => a.RoleId == 101) && model.Applies.Any(a => a.RoleId == 103)
                        && !model.Applies.Any(a => a.RoleId == 102);
                    Debug.Log("CLIVERIFY guildcore 40008->40009 before=3 after=" + model.Applies.Count
                        + " singleDeleteOk=" + applyApproveOk);
                    // 边界:审批一个不存在的申请人(服务端静默场景,客户端本地删除应无副作用不炸)
                    bool noThrowMissingApprove = true;
                    try { Feed(m40009, new CliVerify.Pkt().I(1).C(1).L(9999).Bytes()); }
                    catch (System.Exception e) { noThrowMissingApprove = false; Debug.LogError("CLIVERIFY guildcore 40009 missing role threw: " + e); }
                    bool missingApproveNoSideEffect = model.Applies.Count == 2; // 未误删其它条目

                    // ---- 40021 权限列表:Contains 语义修正验证(id=1 在下标0——老端 indexOf truthy bug 会误判为false) ----
                    Feed(m40021, new CliVerify.Pkt().H(3).C(1).C(5).C(7).Bytes());
                    bool permIndex0Ok = model.HasPermission(1);   // 关键:下标0 也必须为 true(修正点)
                    bool permAbsentOk = !model.HasPermission(2);
                    bool permPresentOk = model.HasPermission(5) && model.HasPermission(7);
                    // 边界:空权限列表(不在公会时的真实回包形态,非静默非报错)
                    Feed(m40021, new CliVerify.Pkt().H(0).Bytes());
                    bool permEmptyOk = model.HasPermissionInfo && !model.HasPermission(1);
                    permissionOk = permIndex0Ok && permAbsentOk && permPresentOk && permEmptyOk;
                    Debug.Log("CLIVERIFY guildcore 40021 index0Fix=" + permIndex0Ok + " absent=" + permAbsentOk
                        + " present=" + permPresentOk + " emptyBoundary=" + permEmptyOk + " ok=" + permissionOk);

                    // ---- 40012 编辑公告:唯一真等级门(公会lv<4拒)+ **订正**成功路径(cast层无条件回包,errorCode==1
                    // 才是真成功——此前"成功静默/到达即失败"是对 pp_guild 前置层的误读,mod_guild_cast.erl
                    // 'modify_announce' 结尾无条件 write(40012,[ErrorCode])) ----
                    logs.Clear();
                    bool level12FailNoThrow = true;
                    try { Feed(m40012, new CliVerify.Pkt().I(41204).Bytes()); } // 占位等级门错误码(具体数值材料未给出)
                    catch (System.Exception e) { level12FailNoThrow = false; Debug.LogError("CLIVERIFY guildcore 40012 fail threw: " + e); }
                    bool level12FailOk = level12FailNoThrow && logs.Any(l => l.Contains("40012") && l.Contains("失败"));
                    logs.Clear();
                    bool level12SuccessNoThrow = true;
                    try { Feed(m40012, new CliVerify.Pkt().I(1).Bytes()); } // 真成功:errorCode==1
                    catch (System.Exception e) { level12SuccessNoThrow = false; Debug.LogError("CLIVERIFY guildcore 40012 success threw: " + e); }
                    bool level12SuccessOk = level12SuccessNoThrow && logs.Any(l => l.Contains("40012") && l.Contains("成功"))
                        && !logs.Any(l => l.Contains("40012") && l.Contains("失败"));
                    level12Ok = level12FailOk && level12SuccessOk;
                    Debug.Log("CLIVERIFY guildcore 40012 failOk=" + level12FailOk + " successOk=" + level12SuccessOk + " ok=" + level12Ok);

                    // ---- 40013 双错误通道:前置mutex失败共享40000壳 / 业务层成功走自己的号 ----
                    int capturedErr = -1;
                    void OnErr(int code) => capturedErr = code;
                    Shenxiao.Framework.Event.EventDispatcher.On<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_GUILD_ERROR, OnErr);
                    Feed(m40000, new CliVerify.Pkt().I(7001).Bytes()); // 模拟40013 mutex前置失败走共享壳
                    Shenxiao.Framework.Event.EventDispatcher.Off<int>(Shenxiao.Framework.Event.GlobalEvent.EVT_GUILD_ERROR, OnErr);
                    bool sharedChannelOk = capturedErr == 7001;
                    logs.Clear();
                    Feed(m40013, new CliVerify.Pkt().I(1).L(9002).C(2).Bytes()); // 业务层成功(补发40006)
                    bool ownChannelSuccessOk = logs.Any(l => l.Contains("40013") && l.Contains("成功"));
                    mutexDualChannelOk = sharedChannelOk && ownChannelSuccessOk;
                    Debug.Log("CLIVERIFY guildcore 40013 sharedChannel(40000)=" + sharedChannelOk
                        + " ownChannelSuccess=" + ownChannelSuccessOk + " ok=" + mutexDualChannelOk);

                    // ---- 改名链:40043(成功→patch GuildName)+ 40044(改名信息;next_rename_time 是剩余秒数
                    // 倒计时,非墙钟时间戳——喂值用明显的秒数量级 3600,不用 unix 时间戳量级,避免误导语义) ----
                    Feed(m40043, new CliVerify.Pkt().I(1).S("天穹新盟").Bytes());
                    bool renameNameOk = model.Info != null && model.Info.GuildName == "天穹新盟";
                    Feed(m40044, new CliVerify.Pkt().C(1).I(3600).Bytes());
                    bool renameInfoOk = model.HasRenameInfo && model.RenameIsFree && model.NextRenameTime == 3600;
                    renameChainOk = renameNameOk && renameInfoOk;
                    Debug.Log("CLIVERIFY guildcore 改名链 nameOk=" + renameNameOk + " infoOk=" + renameInfoOk + " ok=" + renameChainOk);

                    // ---- 40018 广播recv:操作者私有确认 + 公会全员广播字段shape相同,按"到达即刷新"处理,不辨来源 ----
                    logs.Clear();
                    bool upg1NoThrow = true, upg2NoThrow = true;
                    try { Feed(m40018, new CliVerify.Pkt().I(1).Bytes()); } catch { upg1NoThrow = false; }
                    try { Feed(m40018, new CliVerify.Pkt().I(1).Bytes()); } catch { upg2NoThrow = false; }
                    int upgradeLogHits = logs.Count(l => l.Contains("40018"));
                    bool upgFailNoThrow = true;
                    try { Feed(m40018, new CliVerify.Pkt().I(999).Bytes()); }
                    catch (System.Exception e) { upgFailNoThrow = false; Debug.LogError("CLIVERIFY guildcore 40018 fail threw: " + e); }
                    upgradeBroadcastOk = upg1NoThrow && upg2NoThrow && upgradeLogHits >= 2 && upgFailNoThrow;
                    Debug.Log("CLIVERIFY guildcore 40018 twiceNoThrow=" + (upg1NoThrow && upg2NoThrow)
                        + " logHits=" + upgradeLogHits + " failNoThrow=" + upgFailNoThrow + " ok=" + upgradeBroadcastOk);

                    // ---- 边界各一发(其余号:仅验证 no-throw + 关键字段落值,防止字段序回归) ----
                    bool b1 = true, b2 = true, b3 = true, b4 = true, b5 = true, b6 = true, b7 = true, b8 = true,
                        b9 = true, b10 = true, b11 = true, b12 = true, b13 = true, b14 = true, b15 = true, b16 = true;
                    try { Feed(m40007, new CliVerify.Pkt().I(1).Bytes()); b1 = role.GuildId == 0; } catch { b1 = false; }
                    // On40007 成功已内部调用 GuildModel.Reset(),此处不再重复;后续各号互不依赖 Info,直接顺发即可。
                    try { Feed(m40010, new CliVerify.Pkt().C(1).H(10).I(100000).Bytes()); b2 = model.ApproveType == 1 && model.AutoApproveLv == 10; } catch { b2 = false; }
                    // ---- 40011 **订正**(同40012):errorCode==1 才是真成功,并非"到达即失败" ----
                    try
                    {
                        logs.Clear();
                        Feed(m40011, new CliVerify.Pkt().I(555).Bytes()); // 失败码
                        bool b3Fail = logs.Any(l => l.Contains("40011") && l.Contains("失败"));
                        logs.Clear();
                        Feed(m40011, new CliVerify.Pkt().I(1).Bytes()); // 真成功
                        bool b3Success = logs.Any(l => l.Contains("40011") && l.Contains("成功"));
                        b3 = b3Fail && b3Success;
                    }
                    catch { b3 = false; }
                    try { Feed(m40014, new CliVerify.Pkt().I(1).L(20001).Bytes()); } catch { b4 = false; }
                    try
                    {
                        var p40016 = new CliVerify.Pkt().I(1).C(2);
                        model.SetApplies(new List<Shenxiao.Module.Core.Guild.GuildModel.ApplyEntry> { new Shenxiao.Module.Core.Guild.GuildModel.ApplyEntry { RoleId = 1 } });
                        Feed(m40016, p40016.Bytes());
                        b5 = model.Applies.Count == 0; // 成功→本地清空申请列表
                    }
                    catch { b5 = false; }
                    try { Feed(m40017, new CliVerify.Pkt().L(30001).L(5001).S("天穹盟").C(2).S("副会长").Bytes()); } catch { b6 = false; }
                    try { Feed(m40019, new CliVerify.Pkt().C(3).C(1).Bytes()); } catch { b7 = false; } // 纯死号 no-op,不炸即过
                    try { Feed(m40020, new CliVerify.Pkt().I(1).Bytes()); } catch { b8 = false; }
                    try
                    {
                        var p40023 = new CliVerify.Pkt().I(50).C(3).H(0).H(1);
                        p40023.I(1).L(9001).S("盟主甲").C(1).C(1).H(10).H(5).H(3).I(1700000000);
                        Feed(m40023, p40023.Bytes());
                        b9 = model.HasDonateInfo && model.DonateRecords.Count == 1;
                    }
                    catch { b9 = false; }
                    try { Feed(m40027, new CliVerify.Pkt().I(1).Bytes()); } catch { b10 = false; }
                    try { Feed(m40028, new CliVerify.Pkt().I(77).Bytes()); b11 = true; } catch { b11 = false; }
                    try { Feed(m40030, new CliVerify.Pkt().I(100).I(2).I(20).I(200).Bytes()); b12 = model.TitleId == 2; } catch { b12 = false; }
                    try { Feed(m40031, new CliVerify.Pkt().I(120).I(5).I(30).Bytes()); b13 = model.PrestigeDay == 5; } catch { b13 = false; }
                    try { Feed(m40039, new CliVerify.Pkt().I(888).Bytes()); b14 = model.Donate == 888; } catch { b14 = false; }
                    try
                    {
                        var p40040 = new CliVerify.Pkt().I(900).H(1).I(11).C(2).C(1).L(100).L(200);
                        Feed(m40040, p40040.Bytes());
                        b15 = model.Skills.Count == 1 && model.Skills[0].SkillId == 11;
                    }
                    catch { b15 = false; }
                    try
                    {
                        Feed(m40042, new CliVerify.Pkt().I(1).I(11).C(3).I(870).L(150).L(250).Bytes());
                        b16 = model.Donate == 870 && model.Skills.FirstOrDefault(s => s.SkillId == 11)?.LearnLv == 3;
                    }
                    catch { b16 = false; }
                    boundaryOk = b1 && b2 && b3 && b4 && b5 && b6 && b7 && b8 && b9 && b10 && b11 && b12 && b13 && b14 && b15 && b16;
                    Debug.Log("CLIVERIFY guildcore 边界各一发 40007=" + b1 + " 40010=" + b2 + " 40011=" + b3 + " 40014=" + b4
                        + " 40016=" + b5 + " 40017=" + b6 + " 40019=" + b7 + " 40020=" + b8 + " 40023=" + b9
                        + " 40027=" + b10 + " 40028=" + b11 + " 40030=" + b12 + " 40031=" + b13 + " 40039=" + b14
                        + " 40040=" + b15 + " 40042=" + b16 + " ok=" + boundaryOk);

                    // ---- 40060/61/62/63(仙宗召援/合并族):仅烟测 no-throw + 关键字段 ----
                    bool m60ok = true, m61ok = true, m62ok = true, m63ok = true;
                    try
                    {
                        var p60 = new CliVerify.Pkt().L(30002).S("路人乙").H(80).C(1).C(1).S("").I(0).H(1).S("世界boss").I(500).C(1).I(100).H(10).H(20);
                        Feed(m40060, p60.Bytes());
                        m60ok = model.LastBossCall != null && model.LastBossCall.RoleName == "路人乙";
                    }
                    catch { m60ok = false; }
                    try
                    {
                        var p61 = new CliVerify.Pkt().H(1);
                        p61.L(6001).S("邻盟").H(3).I(500).L(30003).S("邻盟盟主").H(20).H(30).C(0).I(0).L(30000).C(0).C(1);
                        Feed(m40061, p61.Bytes());
                        m61ok = model.MergeCandidates.Count == 1 && model.MergeCandidates[0].GuildName == "邻盟" && model.MergeCandidates[0].MergeRel == 1;
                    }
                    catch { m61ok = false; }
                    try { Feed(m40062, new CliVerify.Pkt().I(1).L(6001).Bytes()); } catch { m62ok = false; }
                    try { Feed(m40063, new CliVerify.Pkt().I(1).L(6001).Bytes()); } catch { m63ok = false; }
                    boundaryOk = boundaryOk && m60ok && m61ok && m62ok && m63ok
                        && noThrowMissingApprove && missingApproveNoSideEffect;
                    Debug.Log("CLIVERIFY guildcore 40060=" + m60ok + " 40061=" + m61ok + " 40062=" + m62ok + " 40063=" + m63ok
                        + " missingApprove=" + (noThrowMissingApprove && missingApproveNoSideEffect));
                }
                finally
                {
                    Application.logMessageReceived -= cb;
                }

                bool logicPass = infoOk && membersOk && applyApproveOk && permissionOk && level12Ok
                    && mutexDualChannelOk && renameChainOk && upgradeBroadcastOk && boundaryOk && selfInfoOk;

                // ---- 渲染段(信息/成员两页,编辑期不可加载则优雅降级,不计入通过判定——同 DungeonFamilyCase 先例) ----
                bool renderAttempted = false, renderOk = false;
                try
                {
                    model.Reset();
                    role.GuildId = 5001; role.GuildPosition = 1; role.GuildPositionName = "会长";
                    Feed(m40005, p40005AfterReset());
                    Shenxiao.Module.Core.Guild.GuildMainFlow.Open();
                    await Task.Delay(800);
                    renderAttempted = true;
                    stage.ForceCjkFont();
                    string png = stage.Capture("Temp/round13a_guild_main.png");
                    foreach (TMP_Text txt in stage.CanvasRoot.GetComponentsInChildren<TMP_Text>(true))
                        if (txt.text != null && txt.text.Contains("天穹盟")) { renderOk = true; break; }
                    Debug.Log("CLIVERIFY guildcore render attempted shot=" + png + " renderOk=" + renderOk);
                    Shenxiao.Module.Core.Guild.GuildMainFlow.Reset();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("CLIVERIFY guildcore render 优雅降级(编辑期未必可加载 GuildModule.prefab): " + e.Message);
                }

                bool pass = logicPass;
                Debug.Log("CLIVERIFY guildcore VERDICT selfInfo=" + selfInfoOk + " info=" + infoOk + " members=" + membersOk
                    + " applyApprove=" + applyApproveOk + " permission=" + permissionOk + " level12=" + level12Ok
                    + " mutexDual=" + mutexDualChannelOk + " renameChain=" + renameChainOk + " upgradeBroadcast=" + upgradeBroadcastOk
                    + " boundary=" + boundaryOk + " renderAttempted=" + renderAttempted + " renderOk=" + renderOk + " pass=" + pass);

                model.Reset();
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        /// <summary>渲染段单独重建一份最小 40005 包(复用同一份"天穹盟"命名,供文本断言)。</summary>
        private static byte[] p40005AfterReset()
        {
            var p = new CliVerify.Pkt().L(5001).S("天穹盟").S("欢迎大家踊跃发言").H(1).C(1).L(9001);
            AppendFigure(p, "盟主甲", 1, 88, 2);
            p.H(5).I(1000).I(200).I(50).H(10).H(45).L(99999).H(3).I(0).C(0).C(0).I(1700000000).C(0);
            return p.Bytes();
        }

        /// <summary>按 FigureProto.SCHEMA 精确顺序手工拼字节(46字段,4个 u16计数子列表全传0——
        /// 对标 Shenxiao.Common.Proto.FigureProto.Read,改 schema 必须两处同步)。</summary>
        private static void AppendFigure(CliVerify.Pkt p, string name, int career, int level, int turn)
        {
            p.S(name)      // name
             .C(0)         // sex
             .C(0)         // realm
             .C(career)    // career
             .H(level)     // level
             .C(0)         // GM
             .C(0)         // vip_flag
             .C(0)         // is_hide_vip
             .C(0)         // touxian
             .H(0)         // level_model_list 计数
             .H(0)         // fashion_model_list 计数
             .S("")        // picture
             .I(0)         // prcture_ver
             .L(0)         // guild_id
             .S("")        // guild_name
             .C(0)         // position
             .S("")        // position_name
             .I(0)         // dsgt_id
             .I(0)         // liveness_id
             .C(turn)      // turn
             .C(0)         // turn_stage
             .C(0)         // grade_id
             .C(0)         // is_marriage
             .L(0)         // marriage_id
             .S("")        // marriage_name
             .I(0)         // escort_state
             .I(0)         // block_id
             .I(0)         // house_id
             .H(0)         // house_lv
             .H(0)         // figure_list 计数
             .H(0)         // figure_ride_list 计数
             .H(0)         // achv_lv
             .H(0)         // medal_id
             .I(0)         // fazhen_id
             .H(0)         // dress_list 计数
             .I(0)         // god_id
             .I(0)         // revelation_suit
             .I(0)         // demon_id
             .C(0)         // supreme_vip
             .I(0)         // title_id
             .C(0)         // mask_id
             .C(0)         // seaCamp
             .C(0)         // brick_id
             .C(0)         // dummy_type
             .C(0)         // suit_fashion_id
             .C(0);        // collect_state
        }
    }
}
