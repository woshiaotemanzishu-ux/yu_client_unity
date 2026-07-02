using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 结社加入实证:40001(列表)/40004(创建)合成包反射喂 GuildJoinController 私有 handler,
    /// 断言 GuildJoinModel 列表套值 + HasGuild 判定 + errcode 失败不抛异常;再拉起 GuildJoinShellView
    /// 渲染断言含「结社」文本与创建按钮。
    /// 独立用例文件(避免多代理改 CliVerify.cs 冲突),复用 CliVerify.Stage/Pkt/FindDeep(均已 public)。
    /// CliVerify.Pkt 无字符串写入方法(NetReader.ReadString 用 u16 长度+UTF8),故本文件自带 <see cref="AppendString"/>
    /// 手工拼 s 字段字节,不改 CliVerify.cs 本体。
    /// 日志前缀统一 "CLIVERIFY guildjoin"。
    /// </summary>
    public static class GuildJoinCase
    {
        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                object ctrl = Shenxiao.Module.Core.Guild.GuildJoinController.Instance;
                const System.Reflection.BindingFlags F =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                System.Reflection.MethodInfo m40001 = ctrl.GetType().GetMethod("On40001", F);
                System.Reflection.MethodInfo m40004 = ctrl.GetType().GetMethod("On40004", F);
                if (m40001 == null || m40004 == null)
                {
                    Debug.LogError("CLIVERIFY guildjoin handlers missing (reflection)");
                    return 3;
                }
                void Feed(System.Reflection.MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                Shenxiao.Module.Core.Guild.GuildJoinModel model = Shenxiao.Module.Core.Guild.GuildJoinModel.Instance;
                model.Clear();

                // 40001 列表:page_total:h, page_no:h, guild_list[u16×{guild_id:l, guild_name:s, guild_lv:h,
                // guild_exp:i, chief_id:l, chief_name:s, member_num:h, member_capacity:h, is_apply:c,
                // auto_approve_power:i, combat_power:l, merge_status:c, is_master:c}]。2 条:
                // {1001,"第一结社",lv2,5/30} {1002,"第二结社",lv1,3/30}。
                byte[] p40001 = new CliVerify.Pkt()
                    .H(1)          // page_total
                    .H(1)          // page_no
                    .H(2)          // guild_list 计数
                        .L(1001)
                        .Concat(S("第一结社"))
                        .H(2)          // guild_lv
                        .I(0)          // guild_exp
                        .L(0)          // chief_id
                        .Concat(S("盟主甲"))
                        .H(5)          // member_num
                        .H(30)         // member_capacity
                        .C(0)          // is_apply
                        .I(0)          // auto_approve_power
                        .L(0)          // combat_power
                        .C(0)          // merge_status
                        .C(0)          // is_master
                        .L(1002)
                        .Concat(S("第二结社"))
                        .H(1)          // guild_lv
                        .I(0)          // guild_exp
                        .L(0)          // chief_id
                        .Concat(S("盟主乙"))
                        .H(3)          // member_num
                        .H(30)         // member_capacity
                        .C(0)          // is_apply
                        .I(0)          // auto_approve_power
                        .L(0)          // combat_power
                        .C(0)          // merge_status
                        .C(0)          // is_master
                    .Bytes();
                Feed(m40001, p40001);
                bool listOk = model.HasData && model.List.Count == 2
                    && model.List[0].Name == "第一结社" && model.List[0].Lv == 2
                    && model.List[0].MemberNum == 5 && model.List[0].MemberCapacity == 30
                    && model.List[1].Name == "第二结社";
                Debug.Log("CLIVERIFY guildjoin 40001 count=" + model.List.Count
                    + " name0=" + (model.List.Count > 0 ? model.List[0].Name : "<none>")
                    + " lv0=" + (model.List.Count > 0 ? model.List[0].Lv : -1) + " ok=" + listOk);

                // 40004 创建成功:error_code:i=1, guild_id:l=2001。
                byte[] p40004Ok = new CliVerify.Pkt().I(1).L(2001).Bytes();
                Feed(m40004, p40004Ok);
                bool createOk = model.HasGuild;
                Debug.Log("CLIVERIFY guildjoin 40004 ok hasGuild=" + model.HasGuild + " ok=" + createOk);

                // 40004 创建失败:error_code:i=400(常见=消耗不足/等级不足),guild_id:l=0。只要不抛异常即过。
                byte[] p40004Fail = new CliVerify.Pkt().I(400).L(0).Bytes();
                bool failNoThrow = true;
                try { Feed(m40004, p40004Fail); }
                catch (System.Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY guildjoin 40004 fail threw: " + e); }
                Debug.Log("CLIVERIFY guildjoin 40004 fail noThrow=" + failNoThrow);

                Shenxiao.Module.Core.Guild.GuildJoinShellView.Show();
                await Task.Delay(400);
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round18_guild_shell.png");

                bool textOk = false;
                foreach (TMP_Text t in stage.CanvasRoot.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t.text != null && t.text.Contains("结社")) { textOk = true; break; }
                }
                Transform createBtn = CliVerify.FindDeep(stage.CanvasRoot, "Btn创建结社");
                bool createBtnOk = createBtn != null && createBtn.gameObject.activeInHierarchy;
                Debug.Log("CLIVERIFY guildjoin shell textOk=" + textOk + " createBtnOk=" + createBtnOk + " shot=" + png);

                bool pass = listOk && createOk && failNoThrow && textOk && createBtnOk;
                Debug.Log("CLIVERIFY guildjoin VERDICT listOk=" + listOk + " createOk=" + createOk
                    + " failNoThrow=" + failNoThrow + " textOk=" + textOk + " createBtnOk=" + createBtnOk + " pass=" + pass);

                Shenxiao.Module.Core.Guild.GuildJoinShellView.Close();
                Shenxiao.Module.Core.Guild.GuildJoinModel.Instance.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        /// <summary>手工拼 's' 字段字节(u16 长度 + UTF8),对标 NetReader.ReadString;CliVerify.Pkt 无此方法,
        /// 不改 CliVerify.cs 本体,故在本文件自带并经 <see cref="Concat"/> 接回 Pkt 链式调用。</summary>
        private static byte[] S(string s)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(s ?? string.Empty);
            var b = new byte[2 + utf8.Length];
            b[0] = (byte)(utf8.Length >> 8);
            b[1] = (byte)utf8.Length;
            System.Array.Copy(utf8, 0, b, 2, utf8.Length);
            return b;
        }

        /// <summary>把手工拼的原始字节数组接回 CliVerify.Pkt 链式调用(逐字节 C() 写入,顺序不变)。</summary>
        private static CliVerify.Pkt Concat(this CliVerify.Pkt pkt, byte[] raw)
        {
            foreach (byte b in raw) pkt.C(b);
            return pkt;
        }
    }
}
