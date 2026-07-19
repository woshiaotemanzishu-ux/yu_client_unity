using System;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Net;
using Shenxiao.Module.Core.StarEquip;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 星宿锻造(chc,pt_232 兜底转发段,轮23 PK2)实证:12 个协议(23210-23213/23220-23221/23230-23233/
    /// 23240-23241)合成包反射喂 StarForgeController 私有 handler,断言 StarForgeModel 数据套值
    /// (TypeInfo.ByPos/MasterInfo.MasterList)与失败分支不抛异常、不覆盖已存数据。
    /// 无专属壳(chc UI 本轮不接线,#23b 尾包再做 port-view-bindings)→ 渲染断言跳过,纯逻辑用例。
    /// 独立用例文件(避免多代理改 CliVerify.cs 冲突,同 EquipStrenCase/GodBefallCase 惯例),复用
    /// CliVerify.Stage/Pkt(均已 public)。
    /// ⚠本文件由 PK2 产出但不挂进 CliVerify.cs 主列表(spec_round23.md 裁决表#5:StarForgeCase 由主控
    /// 收口挂,避免并行编译期跨包依赖)。
    ///
    /// 重点覆盖:23210/23230 两个"入口数据"形状对比(23230 比 23210 少 Buff 字段,断言恒为0不误读);
    /// 23211/23231 成功后局部更新 EquipStatus.Lv,失败分支不抛异常且不覆盖已存数据(B3边界);
    /// 23231 响应 Type 字段是 32 位(与请求侧 8 位不对称)照 32 位读;23213/23233 点亮大师后
    /// MasterList 按 MasterLv&lt;=下发的 MasterLv 整表覆盖 Status(不是"只升不降"的增量更新,是
    /// lib_constellation_forge.erl 的老端已知写法);23220/23221 EVO 的 code==1 但 is_success==0
    /// (随机判定失败)分支不应用 Lv;23240/23241 SOUL 用 IsSpirit 不用 Lv。
    /// 日志前缀统一 "CLIVERIFY starforge"。
    /// </summary>
    public static class StarForgeCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                object ctrl = StarForgeController.Instance;
                Type t = ctrl.GetType();
                MethodInfo m23210 = t.GetMethod("On23210", F);
                MethodInfo m23211 = t.GetMethod("On23211", F);
                MethodInfo m23212 = t.GetMethod("On23212", F);
                MethodInfo m23213 = t.GetMethod("On23213", F);
                MethodInfo m23220 = t.GetMethod("On23220", F);
                MethodInfo m23221 = t.GetMethod("On23221", F);
                MethodInfo m23230 = t.GetMethod("On23230", F);
                MethodInfo m23231 = t.GetMethod("On23231", F);
                MethodInfo m23232 = t.GetMethod("On23232", F);
                MethodInfo m23233 = t.GetMethod("On23233", F);
                MethodInfo m23240 = t.GetMethod("On23240", F);
                MethodInfo m23241 = t.GetMethod("On23241", F);
                if (m23210 == null || m23211 == null || m23212 == null || m23213 == null ||
                    m23220 == null || m23221 == null || m23230 == null || m23231 == null ||
                    m23232 == null || m23233 == null || m23240 == null || m23241 == null)
                {
                    Debug.LogError("CLIVERIFY starforge handlers missing (reflection)");
                    return 3;
                }

                void Feed(MethodInfo m, byte[] pkt) => m.Invoke(ctrl, new object[] { new NetReader(pkt, 0, pkt.Length) });
                // 尾哨兵专用:返回 reader 供断言 Remaining/哨兵值(spec 第8条,照 StarEquipCase.FeedReader 范式)
                NetReader FeedReader(MethodInfo m, byte[] pkt)
                {
                    var r = new NetReader(pkt, 0, pkt.Length);
                    m.Invoke(ctrl, new object[] { r });
                    return r;
                }

                StarForgeModel model = StarForgeModel.Instance;
                model.Clear();

                // ---- 23210 强化界面:Code:32,TypeId:8,Stage:32,IsMax:8,Buff:16,EquipList[u16计数]{EquipId:64,Pos:8,Lv:32}
                // stype=1,Stage=5,IsMax=0,Buff=100,2项:{equip=1001,pos=1,lv=3}{equip=1002,pos=2,lv=0}
                byte[] p23210 = new CliVerify.Pkt().I(1).C(1).I(5).C(0).H(100)
                    .H(2)
                        .L(1001).C(1).I(3)
                        .L(1002).C(2).I(0)
                    .I(777888999) // 尾哨兵:EquipList 嵌套数组读完后字节游标必须恰好停在此(spec 第8条)
                    .Bytes();
                NetReader r23210 = FeedReader(m23210, p23210);
                StarForgeModel.TypeInfo stren1 = model.GetInfo(StarForgeModel.TYPE_STREN, 1);
                bool sentinel210 = r23210.Remaining == 4 && r23210.ReadU32() == 777888999;
                bool info210Ok = stren1 != null && stren1.EquipList.Count == 2 && stren1.NextMasterLv == 5 && stren1.Buff == 100
                    && model.GetByPos(StarForgeModel.TYPE_STREN, 1, 1).Lv == 3 && sentinel210;
                Debug.Log("CLIVERIFY starforge 23210 stype1 count=" + (stren1?.EquipList.Count ?? -1)
                    + " sentinelOk=" + sentinel210 + " ok=" + info210Ok);

                // ---- 23211 强化结果成功:Code:32=1,TypeId:8=1,Pos:8=1,Type:8=0,Buff:16=100,Lv:32=4
                byte[] p23211Ok = new CliVerify.Pkt().I(1).C(1).C(1).C(0).H(100).I(4).Bytes();
                Feed(m23211, p23211Ok);
                bool ok211 = model.GetByPos(StarForgeModel.TYPE_STREN, 1, 1).Lv == 4;
                Debug.Log("CLIVERIFY starforge 23211 ok lv=" + model.GetByPos(StarForgeModel.TYPE_STREN, 1, 1).Lv + " ok=" + ok211);

                // ---- 23211 失败(裁决2的容忍性场景:字段照单收,不抛异常,不覆盖已存 lv)
                byte[] p23211Fail = new CliVerify.Pkt().I(1500).C(1).C(1).C(0).H(0).I(0).Bytes();
                bool fail211NoThrow = true;
                try { Feed(m23211, p23211Fail); }
                catch (Exception e) { fail211NoThrow = false; Debug.LogError("CLIVERIFY starforge 23211 fail threw: " + e); }
                bool data211Unchanged = model.GetByPos(StarForgeModel.TYPE_STREN, 1, 1).Lv == 4;
                Debug.Log("CLIVERIFY starforge 23211 fail noThrow=" + fail211NoThrow + " unchanged=" + data211Unchanged);

                // ---- 23212 强化大师:Code:32,TypeId:8,MasterList[u16计数]{MasterLv:32,Status:8} 2项
                byte[] p23212 = new CliVerify.Pkt().I(1).C(1)
                    .H(2)
                        .I(3).C(1)  // MASTER_ACTIVE
                        .I(7).C(0)  // MASTER_NOACT
                    .I(666555444) // 尾哨兵:MasterList 嵌套数组读完后游标核对(spec 第8条)
                    .Bytes();
                NetReader r23212 = FeedReader(m23212, p23212);
                StarForgeModel.MasterInfo master1 = model.GetMaster(StarForgeModel.TYPE_STREN, 1);
                bool sentinel212 = r23212.Remaining == 4 && r23212.ReadU32() == 666555444;
                bool ok212 = master1 != null && master1.MasterList.Count == 2 && sentinel212;
                Debug.Log("CLIVERIFY starforge 23212 count=" + (master1?.MasterList.Count ?? -1)
                    + " sentinelOk=" + sentinel212 + " ok=" + ok212);

                // ---- 23213 点亮强化大师成功:Code:32=1,TypeId:8=1,MasterLv:32=3 → lv<=3 变 ACTIVED(2),否则 NOACT(0)
                byte[] p23213 = new CliVerify.Pkt().I(1).C(1).I(3).Bytes();
                Feed(m23213, p23213);
                master1 = model.GetMaster(StarForgeModel.TYPE_STREN, 1);
                bool ok213 = master1.MasterList[0].Status == 2 && master1.MasterList[1].Status == 0;
                Debug.Log("CLIVERIFY starforge 23213 status0=" + master1.MasterList[0].Status + " status1=" + master1.MasterList[1].Status + " ok=" + ok213);

                // ---- 23220 进化界面:Code:32,TypeId:8,EquipList[u16计数]{EquipId:64,Pos:8,Lv:32,AttrNum:16} 1项
                byte[] p23220 = new CliVerify.Pkt().I(1).C(2)
                    .H(1)
                        .L(2001).C(1).I(2).H(1)
                    .Bytes();
                Feed(m23220, p23220);
                StarForgeModel.TypeInfo evo2 = model.GetInfo(StarForgeModel.TYPE_EVO, 2);
                bool ok220 = evo2 != null && evo2.EquipList.Count == 1 && evo2.EquipList[0].AttrNum == 1;
                Debug.Log("CLIVERIFY starforge 23220 attrNum=" + (evo2?.EquipList[0].AttrNum ?? -1) + " ok=" + ok220);

                // ---- 23221 进化结果成功(code=1,is_success=1):Lv 32=1→3 更新落地
                byte[] p23221Ok = new CliVerify.Pkt().I(1).C(1).C(2).L(2001).C(1).I(3).I(19).Bytes();
                Feed(m23221, p23221Ok);
                bool ok221 = model.GetByPos(StarForgeModel.TYPE_EVO, 2, 1).Lv == 3;
                Debug.Log("CLIVERIFY starforge 23221 ok lv=" + model.GetByPos(StarForgeModel.TYPE_EVO, 2, 1).Lv + " ok=" + ok221);

                // ---- 23221(code=1,is_success=0,随机判定失败):不应用 Lv,断言仍是上一步的 3(B3边界)
                byte[] p23221Fail = new CliVerify.Pkt().I(1).C(0).C(2).L(2001).C(1).I(2).I(0).Bytes();
                Feed(m23221, p23221Fail);
                bool ok221Fail = model.GetByPos(StarForgeModel.TYPE_EVO, 2, 1).Lv == 3;
                Debug.Log("CLIVERIFY starforge 23221 fail-roll lvUnchanged=" + ok221Fail);

                // ---- 23230 附魔(觉醒)界面:比 23210 少 Buff 字段——Code:32,TypeId:8,Stage:32,IsMax:8,EquipList{EquipId:64,Pos:8,Lv:32}
                byte[] p23230 = new CliVerify.Pkt().I(1).C(1).I(2).C(0)
                    .H(1)
                        .L(3001).C(1).I(1)
                    .Bytes();
                Feed(m23230, p23230);
                StarForgeModel.TypeInfo magic1 = model.GetInfo(StarForgeModel.TYPE_MAGIC, 1);
                bool ok230 = magic1 != null && magic1.EquipList.Count == 1 && magic1.Buff == 0; // 恒0,无该字段
                Debug.Log("CLIVERIFY starforge 23230 buff=" + (magic1?.Buff ?? -1) + " ok=" + ok230);

                // ---- 23231 附魔结果:⚠响应 Type 是 32 位(与请求侧 "ccc" 8 位不对称)
                byte[] p23231 = new CliVerify.Pkt().I(1).C(1).C(1).I(0).I(2).Bytes();
                Feed(m23231, p23231);
                bool ok231 = model.GetByPos(StarForgeModel.TYPE_MAGIC, 1, 1).Lv == 2;
                Debug.Log("CLIVERIFY starforge 23231 lv=" + model.GetByPos(StarForgeModel.TYPE_MAGIC, 1, 1).Lv + " ok=" + ok231);

                // ---- 23232 附魔大师:同 23212 形状
                byte[] p23232 = new CliVerify.Pkt().I(1).C(1)
                    .H(1)
                        .I(1).C(1)
                    .Bytes();
                Feed(m23232, p23232);
                bool ok232 = model.GetMaster(StarForgeModel.TYPE_MAGIC, 1)?.MasterList.Count == 1;
                Debug.Log("CLIVERIFY starforge 23232 ok=" + ok232);

                // ---- 23233 点亮附魔大师成功
                byte[] p23233 = new CliVerify.Pkt().I(1).C(1).I(1).Bytes();
                Feed(m23233, p23233);
                bool ok233 = model.GetMaster(StarForgeModel.TYPE_MAGIC, 1).MasterList[0].Status == 2;
                Debug.Log("CLIVERIFY starforge 23233 ok=" + ok233);

                // ---- 23240 启灵界面:Code:32,TypeId:8,EquipList[u16计数]{EquipId:64,Pos:8,IsSpirit:8}
                byte[] p23240 = new CliVerify.Pkt().I(1).C(1)
                    .H(1)
                        .L(4001).C(1).C(0)
                    .Bytes();
                Feed(m23240, p23240);
                StarForgeModel.TypeInfo soul1 = model.GetInfo(StarForgeModel.TYPE_SOUL, 1);
                bool ok240 = soul1 != null && soul1.EquipList.Count == 1 && soul1.EquipList[0].IsSpirit == 0;
                Debug.Log("CLIVERIFY starforge 23240 ok=" + ok240);

                // ---- 23241 启灵结果成功:Code:32=1,TypeId:8=1,Pos:8=1,IsSpirit:8=1
                byte[] p23241 = new CliVerify.Pkt().I(1).C(1).C(1).C(1).Bytes();
                Feed(m23241, p23241);
                bool ok241 = model.GetByPos(StarForgeModel.TYPE_SOUL, 1, 1).IsSpirit == 1;
                Debug.Log("CLIVERIFY starforge 23241 ok=" + ok241);

                model.Clear();
                bool clearOk = model.GetInfo(StarForgeModel.TYPE_STREN, 1) == null && model.GetMaster(StarForgeModel.TYPE_MAGIC, 1) == null;
                Debug.Log("CLIVERIFY starforge clear ok=" + clearOk);

                // ---- 尾哨兵:StarForgeConfigs 9 张表加载(不依赖具体条数——目标资产可能尚未同步,只求不抛异常)
                bool configsNoThrow = true;
                try { await StarForgeConfigs.EnsureLoaded(); }
                catch (Exception e) { configsNoThrow = false; Debug.LogError("CLIVERIFY starforge configs threw: " + e); }
                Debug.Log("CLIVERIFY starforge configs strength=" + StarForgeConfigs.StrengthCount
                    + " evolutionRateEmpty=" + StarForgeConfigs.IsEvolutionRateEmpty + " noThrow=" + configsNoThrow);

                bool pass = info210Ok && ok211 && fail211NoThrow && data211Unchanged && ok212 && ok213
                    && ok220 && ok221 && ok221Fail && ok230 && ok231 && ok232 && ok233 && ok240 && ok241
                    && clearOk && configsNoThrow;
                Debug.Log("CLIVERIFY starforge VERDICT pass=" + pass);

                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }
    }
}
