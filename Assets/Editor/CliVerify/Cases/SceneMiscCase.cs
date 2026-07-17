using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene;
using Shenxiao.Module.Core.Scene.Vo;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// Scene 散件(120xx 补全,pt_120.erl)验收(自动循环 轮18 PK5 实现)。反射喂 SceneController 私有
    /// On12xxx handler + 断言 SceneManager/SceneMiscModel/RoleModel 落地状态,并对 12017/12088/12092
    /// (变长数组/嵌套结构重点)、12024(自空)做游标(NetReader.Remaining==0)探针,对 12089/12091
    /// 做"禁注册"反射断言(NetManager._handlers 不含这两个 key)。
    /// 日志前缀 "CLIVERIFY scenemisc"。⚠与并行会话的 SceneMixDriverCase(角色模型混合驱动器验收)
    /// 是两回事,勿混改。m9:本文件已由主控收口挂钩(CliVerify.cs 内 `SceneMiscCase.Run` 已接入
    /// RenderAll/Run 调用链),独立可调用一节留档不再是待办。
    /// </summary>
    public static class SceneMiscCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static Task<int> Run()
        {
            try
            {
                return Task.FromResult(RunSync());
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY scenemisc EXCEPTION " + e);
                return Task.FromResult(3); // m10:异常路径返回值与其余 Case 口径统一(3,非1)
            }
        }

        private static int RunSync()
        {
            SceneController ctrl = SceneController.Instance;
            ctrl.Init(); // BaseController.Init 内部已判重,可安全重复调用

            // ---- 0. 死号禁注册反射断言(12089/12091,r18_server_scene §重大发现:双端事实死) ----
            FieldInfo fHandlers = typeof(NetManager).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Static);
            if (fHandlers == null) { Debug.LogError("CLIVERIFY scenemisc NetManager._handlers 反射目标缺失"); return 3; }
            var handlers = (IDictionary)fHandlers.GetValue(null);
            bool has12089 = handlers.Contains(12089);
            bool has12091 = handlers.Contains(12091);
            bool deadOk = !has12089 && !has12091;
            Debug.Log("CLIVERIFY scenemisc 0 死号禁注册: 12089注册=" + has12089 + " 12091注册=" + has12091 + " ok=" + deadOk);

            Type t = ctrl.GetType();
            MethodInfo M(string name)
            {
                MethodInfo m = t.GetMethod(name, F);
                if (m == null) Debug.LogError("CLIVERIFY scenemisc 反射目标缺失: " + name);
                return m;
            }

            MethodInfo m12015 = M("On12015"), m12017 = M("On12017"), m12022 = M("On12022"), m12023 = M("On12023"),
                m12024 = M("On12024"), m12025 = M("On12025"), m12026 = M("On12026"), m12027 = M("On12027"),
                m12028 = M("On12028"), m12030 = M("On12030"), m12036 = M("On12036"), m12043 = M("On12043"),
                m12044 = M("On12044"), m12045 = M("On12045"), m12078 = M("On12078"), m12080 = M("On12080"),
                m12083 = M("On12083"), m12085 = M("On12085"), m12087 = M("On12087"), m12088 = M("On12088"),
                m12090 = M("On12090"), m12092 = M("On12092");
            if (m12015 == null || m12017 == null || m12022 == null || m12023 == null || m12024 == null
                || m12025 == null || m12026 == null || m12027 == null || m12028 == null || m12030 == null
                || m12036 == null || m12043 == null || m12044 == null || m12045 == null || m12078 == null
                || m12080 == null || m12083 == null || m12085 == null || m12087 == null || m12088 == null
                || m12090 == null || m12092 == null)
            {
                return 3;
            }

            NetReader Feed(MethodInfo m, byte[] pkt)
            {
                var r = new NetReader(pkt, 0, pkt.Length);
                m.Invoke(ctrl, new object[] { r });
                return r;
            }

            bool allPass = deadOk;
            void Check(string tag, bool ok)
            {
                Debug.Log("CLIVERIFY scenemisc " + tag + " ok=" + ok);
                if (!ok) allPass = false;
            }

            SceneManager sm = SceneManager.Instance;
            SceneMiscModel misc = SceneMiscModel.Instance;
            sm.Clear();
            misc.Clear();

            long origRoleId = RoleModel.Instance.RoleId;
            var origFigure = RoleModel.Instance.Figure;
            try
            {
                RoleModel.Instance.RoleId = 999999;

                // ---- 1. 12015 假人进场(整条塞进 SceneManager 角色表,复用 RoleVo/AddRole) ----
                byte[] p12015 = new CliVerify.Pkt()
                    .I(90001).H(0).H(7).H(1) // Id, 保留0, SerId, SerNum
                    .AppendFigure("Dummy", 9)
                    .H(100).H(200).L(500).L(1000).H(150).C(0).C(0).L(42)
                    .Bytes();
                NetReader r12015 = Feed(m12015, p12015);
                RoleVo dummy = sm.GetRole(90001);
                Check("1 12015", dummy != null && dummy.Figure != null && dummy.Figure.name == "Dummy"
                    && dummy.X == 100 && dummy.Y == 200 && dummy.Hp == 500 && dummy.HpLim == 1000
                    && dummy.Group == 42 && r12015.Remaining == 0);

                // ---- 2. 12017 掉落生成(游标探针重点;17字段元素+外层MonId/X/Y回填DropVo.MonId/MonPosX/Y) ----
                byte[] p12017 = new CliVerify.Pkt()
                    .I(555).H(3).I(1001).H(2)
                        .L(7001).C(1).I(2001).I(5).L(0).H(0).L(0).H(0).L(0).H(10).H(20).S("eff").S("icon").I(1000).I(9000).C(1).C(7)
                        .L(7002).C(2).I(2002).I(3).L(0).H(0).L(0).H(0).L(0).H(11).H(21).S("").S("").I(500).I(8000).C(0).C(0)
                    .H(300).H(400).C(1)
                    .Bytes();
                NetReader r12017 = Feed(m12017, p12017);
                DropVo drop1 = sm.GetDrop(7001);
                DropVo drop2 = sm.GetDrop(7002);
                Check("2 12017", drop1 != null && drop1.TypeId == 2001 && drop1.DropNum == 5 && drop1.DropEffect == "eff"
                    && drop1.PutIcon == "icon" && drop1.MonId == 555 && drop1.MonPosX == 300 && drop1.MonPosY == 400
                    && drop2 != null && drop2.TypeId == 2002 && r12017.Remaining == 0);

                // ---- 3. 12022 Boss归属(复用既有 RoleVo.BossOwner) ----
                sm.AddRole(new RoleVo { RoleId = 555001, X = 1, Y = 1 });
                byte[] p12022 = new CliVerify.Pkt().L(555001).C(1).Bytes();
                Feed(m12022, p12022);
                Check("3 12022", sm.GetRole(555001)?.BossOwner == 1);

                // ---- 4. 12023 怪物喊话(落 LastMonsterTalk) ----
                byte[] p12023 = new CliVerify.Pkt().I(444).S("Grr").Bytes();
                Feed(m12023, p12023);
                Check("4 12023", misc.LastMonsterTalk.AutoId == 444 && misc.LastMonsterTalk.Msg == "Grr");

                // ---- 5. 12024 自空(游标探针+零消费断言) ----
                byte[] p12024 = new CliVerify.Pkt().L(1).L(2).L(3).Bytes();
                NetReader r12024 = Feed(m12024, p12024);
                Check("5 12024", r12024.Remaining == 0 && sm.GetDrop(1) == null); // 只读完保游标,不落任何 Model

                // ---- 6. 12025 Boss伤害榜全量 ----
                byte[] p12025 = new CliVerify.Pkt().I(1).I(2)
                    .H(2)
                        .L(801).S("Alice").H(1).H(1).S("S1").L(0).C(1).L(1000).L(0)
                        .L(802).S("Bob").H(1).H(1).S("S1").L(0).C(2).L(500).L(0)
                    .Bytes();
                Feed(m12025, p12025);
                Check("6 12025", misc.BossHurtAutoId == 1 && misc.BossHurtConfigId == 2 && misc.BossHurtList.Count == 2
                    && misc.BossHurtList.Find(e => e.RoleId == 801)?.Hurt == 1000);

                // ---- 7. 12026 增量新增/更新(upsert:801 更新 Hurt,803 新增) ----
                byte[] p12026Update = new CliVerify.Pkt().I(1).I(2).L(801).S("Alice").H(1).H(1).S("S1").L(0).C(1).L(9999).L(0).Bytes();
                Feed(m12026, p12026Update);
                byte[] p12026Add = new CliVerify.Pkt().I(1).I(2).L(803).S("Carl").H(1).H(1).S("S1").L(0).C(3).L(300).L(0).Bytes();
                Feed(m12026, p12026Add);
                Check("7 12026", misc.BossHurtList.Count == 3 && misc.BossHurtList.Find(e => e.RoleId == 801)?.Hurt == 9999
                    && misc.BossHurtList.Find(e => e.RoleId == 803) != null);

                // ---- 8. 12027 移除(去掉 802) ----
                byte[] p12027 = new CliVerify.Pkt().I(1).I(2).H(1).L(802).Bytes();
                Feed(m12027, p12027);
                Check("8 12027", misc.BossHurtList.Count == 2 && misc.BossHurtList.Find(e => e.RoleId == 802) == null);

                // ---- 9. 12028 协助id更改(801 的 AssistId 改成 555) ----
                byte[] p12028 = new CliVerify.Pkt().I(1).I(2).H(1).L(801).L(555).Bytes();
                Feed(m12028, p12028);
                Check("9 12028", misc.BossHurtList.Find(e => e.RoleId == 801)?.AssistId == 555);

                // ---- 10. 12030 动态区域标记(独立推送,整表替换) ----
                byte[] p12030A = new CliVerify.Pkt().H(2).C(1).C(1).C(2).C(2).Bytes();
                Feed(m12030, p12030A);
                bool marksFirst = misc.AreaMarks.Count == 2;
                byte[] p12030B = new CliVerify.Pkt().H(1).C(9).C(9).Bytes();
                Feed(m12030, p12030B);
                Check("10 12030", marksFirst && misc.AreaMarks.Count == 1 && misc.AreaMarks[0].AreaId == 9);

                // ---- 11. 12036 HP变化(核心;复用 ApplyHp 落 RoleVo.Hp,表现字段落 LastHpChange) ----
                byte[] p12036 = new CliVerify.Pkt().C(2).L(555001).L(800).L(1000).C(1).L(200).H(5).C(1).L(90999).Bytes();
                Feed(m12036, p12036);
                Check("11 12036", sm.GetRole(555001)?.Hp == 800 && sm.GetRole(555001)?.HpLim == 1000
                    && misc.LastHpChange.Change == 200 && misc.LastHpChange.BuffId == 5 && misc.LastHpChange.SourceId == 90999);

                // ---- 12. 12043 求助列表全量 ----
                byte[] p12043 = new CliVerify.Pkt().I(11).I(22)
                    .H(2)
                        .L(901).L(555001).S("Alice").H(1).H(1).S("S1")
                        .L(902).L(555002).S("Bob").H(1).H(1).S("S1")
                    .Bytes();
                Feed(m12043, p12043);
                Check("12 12043", misc.AssistAutoId == 11 && misc.AssistConfigId == 22 && misc.AssistList.Count == 2);

                // ---- 13. 12044 求助新增(upsert:903 新增) ----
                byte[] p12044 = new CliVerify.Pkt().I(11).I(22).L(903).L(555003).S("Carl").H(1).H(1).S("S1").Bytes();
                Feed(m12044, p12044);
                Check("13 12044", misc.AssistList.Count == 3 && misc.AssistList.Find(e => e.AssistId == 903) != null);

                // ---- 14. 12045 求助删除(删 901) ----
                byte[] p12045 = new CliVerify.Pkt().I(11).I(22).L(901).Bytes();
                Feed(m12045, p12045);
                Check("14 12045", misc.AssistList.Count == 2 && misc.AssistList.Find(e => e.AssistId == 901) == null);

                // ---- 15. 12078 Figure变更(他人分支+主角分支,整块替换 Figure) ----
                byte[] p12078Other = new CliVerify.Pkt().L(555001).AppendFigure("NewName", 5).Bytes();
                Feed(m12078, p12078Other);
                byte[] p12078Main = new CliVerify.Pkt().L(999999).AppendFigure("MainNew", 7).Bytes();
                Feed(m12078, p12078Main);
                Check("15 12078", sm.GetRole(555001)?.Figure?.name == "NewName"
                    && RoleModel.Instance.Figure?.name == "MainNew" && RoleModel.Instance.Career == 7);

                // ---- 16. 12080 怪物属性(Type==3→CanAttack,复用既有 MonsterVo 字段;未映射type不炸) ----
                sm.AddMonster(new MonsterVo { InstanceId = 60001, X = 1, Y = 1 });
                byte[] p12080 = new CliVerify.Pkt().I(60001).H(2).C(3).I(1).C(9).I(42).Bytes();
                Feed(m12080, p12080);
                Check("16 12080", sm.GetMonster(60001)?.CanAttack == 1);

                // ---- 17. 12083 复活完成(落 LastRevive,不联动 Relive/不改 RoleModel 位置) ----
                byte[] p12083 = new CliVerify.Pkt().C(2).I(1002).H(50).H(60).S("野外").L(3000).I(10).I(20).H(5000).Bytes();
                Feed(m12083, p12083);
                Check("17 12083", misc.LastRevive.ReviveType == 2 && misc.LastRevive.SceneId == 1002
                    && misc.LastRevive.SceneName == "野外" && misc.LastRevive.Hp == 3000 && misc.LastRevive.Gold == 10
                    && misc.LastRevive.BGold == 20 && misc.LastRevive.AttProtectedTime == 5000);

                // ---- 18. 12085 安全区状态(区域广播,PlayerId 不一定是自己) ----
                byte[] p12085Main = new CliVerify.Pkt().L(999999).C(2).Bytes();
                Feed(m12085, p12085Main);
                byte[] p12085Other = new CliVerify.Pkt().L(555001).C(3).Bytes();
                Feed(m12085, p12085Other);
                Check("18 12085", misc.MainRoleSafeAreaState == 2 && misc.SafeAreaStateByPlayer[555001] == 3);

                // ---- 19. 12087 场景玩家计数 ----
                byte[] p12087 = new CliVerify.Pkt().H(1001).H(37).Bytes();
                Feed(m12087, p12087);
                Check("19 12087", misc.PlayerCountSceneId == 1001 && misc.PlayerCountNum == 37);

                // ---- 20. 12088 场景内简单用户列表(游标探针重点;防御接收保留) ----
                byte[] p12088 = new CliVerify.Pkt().H(1)
                    .S("h5").H(1).L(555001).C(1).C(0).C(3).H(80).S("Alice").S("pic").I(5)
                    .Bytes();
                NetReader r12088 = Feed(m12088, p12088);
                Check("20 12088", misc.SimpleUsers.Count == 1 && misc.SimpleUsers[0].Id == 555001
                    && misc.SimpleUsers[0].Name == "Alice" && misc.SimpleUsers[0].Career == 3
                    && r12088.Remaining == 0);

                // ---- B7裁决:RequestSimpleUserList 发送方法已删除(老端全仓零引用,照 17241-43/33903
                // 先例)——改反射断言"无公开发送方法",On12088 只保留防御接收。 ----
                bool noSimpleUserSend = t.GetMethod("RequestSimpleUserList") == null;
                Check("B7 12088 RequestSimpleUserList已删除(仅防御recv)", noSimpleUserSend);

                // ---- 21. 12090 公会id变更(Monster分支复用 MonsterVo.GuildId;Role分支落 GuildIdByRole) ----
                byte[] p12090Mon = new CliVerify.Pkt().C(1).L(60001).L(777).Bytes();
                Feed(m12090, p12090Mon);
                byte[] p12090Role = new CliVerify.Pkt().C(2).L(555001).L(888).Bytes();
                Feed(m12090, p12090Role);
                Check("21 12090", sm.GetMonster(60001)?.GuildId == 777 && misc.GuildIdByRole[555001] == 888);

                // ---- 22. 12092 怪物Buff批量(游标探针重点;buff结构 hhiccIIl 复用 FightVo.BuffInfo) ----
                byte[] p12092 = new CliVerify.Pkt().H(1)
                    .L(60001).H(2)
                        .H(10).H(20).I(30001).C(1).C(2).I(100).I(-5).L(99999999)
                        .H(11).H(21).I(30002).C(3).C(0).I(200).I(0).L(0)
                    .Bytes();
                NetReader r12092 = Feed(m12092, p12092);
                bool has12092 = misc.MonsterBuffs.TryGetValue(60001, out var buffs) && buffs.Count == 2;
                Check("22 12092", has12092 && buffs[0].IconType == 10 && buffs[0].BuffEffectId == 20
                    && buffs[0].Id == 30001 && buffs[0].Level == 1 && buffs[0].Diejia == 2
                    && buffs[0].Integer == 100 && buffs[0].Decimals == -5 && buffs[0].Period == 99999999
                    && r12092.Remaining == 0);

                // ---- 23. Request* 方法零参烟雾(未连网仅应 Warn,不应抛异常)
                //          + B5:12092 真发送(动态"h"+"l"×N)/null·空列表守卫不发 ----
                bool requestNoThrow = true;
                var reqLogs = new List<string>();
                Application.LogCallback reqCb = (msg, stack, type) => reqLogs.Add(msg);
                Application.logMessageReceived += reqCb;
                try
                {
                    ctrl.RequestBossHurtList(1);
                    ctrl.RequestAssistList(1);
                    ctrl.RequestSafeAreaState(0);
                    ctrl.RequestPlayerCount(1001);
                    ctrl.RequestMonsterBuffList(new long[] { 1, 2, 3 }); // 应真发一次 12092
                    ctrl.RequestMonsterBuffList(null);                  // B5 守卫:不应发送
                    ctrl.RequestMonsterBuffList(Array.Empty<long>());   // B5 守卫:不应发送
                }
                catch (Exception e)
                {
                    requestNoThrow = false;
                    Debug.LogError("CLIVERIFY scenemisc Request* threw: " + e);
                }
                finally
                {
                    Application.logMessageReceived -= reqCb;
                }
                int send12092Count = reqLogs.FindAll(l => l.Contains("proto=" + Proto.SC_MONSTER_BUFF_BATCH)).Count;
                bool b12092Send = send12092Count == 1; // 仅 {1,2,3} 那次真发,null/空列表被守卫拦截
                Check("23 Request*(含B5 12092真发送+空列表守卫)", requestNoThrow && b12092Send);
            }
            finally
            {
                RoleModel.Instance.RoleId = origRoleId;
                RoleModel.Instance.Figure = origFigure;
                sm.Clear();
                misc.Clear();
            }

            Debug.Log("CLIVERIFY scenemisc VERDICT allPass=" + allPass);
            return allPass ? 0 : 3;
        }
    }

    /// <summary>CliVerify.Pkt 的 FigureProto 专用扩展:按 FigureProto.SCHEMA(Common/Proto/FigureProto.cs)
    /// 逐字段顺序拼一份"空壳+可选 name/career"的合法 Figure 块,供 12015/12078 测试包复用,避免
    /// 46 个字段散落各处手抄出错。字段顺序/格式字符与 SCHEMA 数组逐条对应,改 schema 两边一起改。</summary>
    internal static class CliVerifyPktFigureExt
    {
        public static CliVerify.Pkt AppendFigure(this CliVerify.Pkt p, string name, int career)
        {
            return p
                .S(name).C(0).C(0).C(career).H(0).C(0).C(0).C(0).C(0) // name,sex,realm,career,level,GM,vip_flag,is_hide_vip,touxian
                .H(0)                                                  // level_model_list(空)
                .H(0)                                                  // fashion_model_list(空)
                .S("").I(0).L(0).S("").C(0).S("")                      // picture,prcture_ver,guild_id,guild_name,position,position_name
                .I(0).I(0).C(0).C(0).C(0).C(0).L(0).S("")              // dsgt_id,liveness_id,turn,turn_stage,grade_id,is_marriage,marriage_id,marriage_name
                .I(0).I(0).I(0).H(0)                                   // escort_state,block_id,house_id,house_lv
                .H(0)                                                  // figure_list(空)
                .H(0)                                                  // figure_ride_list(空)
                .H(0).H(0).I(0)                                        // achv_lv,medal_id,fazhen_id
                .H(0)                                                  // dress_list(空)
                .I(0).I(0).I(0).C(0).I(0).C(0).C(0).C(0).C(0).C(0).C(0); // god_id..collect_state
        }
    }
}
