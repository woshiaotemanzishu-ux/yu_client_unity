using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 装备强化实证:15204(查询)/15205(强化)合成包反射喂 EquipStrenController 私有 handler,
    /// 断言 EquipStrenModel 数据套值(GetStren/TotalStren)与 res!=1 失败不抛异常。
    /// 无专属壳(EquipStrenView 是既有 EquipFlow 六标签窗内容,非独立弹窗)→ 渲染断言跳过,纯逻辑用例。
    /// 独立用例文件(避免多代理改 CliVerify.cs 冲突),复用 CliVerify.Stage/Pkt(均已 public)。
    /// 日志前缀统一 "CLIVERIFY equipstren"。
    /// </summary>
    public static class EquipStrenCase
    {
        public static async Task<int> Run()
        {
            CliVerify.Stage stage = CliVerify.Stage.Create();
            try
            {
                object ctrl = Shenxiao.Module.Core.Equip.EquipStrenController.Instance;
                const System.Reflection.BindingFlags F =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                System.Reflection.MethodInfo m15204 = ctrl.GetType().GetMethod("On15204", F);
                System.Reflection.MethodInfo m15205 = ctrl.GetType().GetMethod("On15205", F);
                if (m15204 == null || m15205 == null)
                {
                    Debug.LogError("CLIVERIFY equipstren handlers missing (reflection)");
                    return 3;
                }
                void Feed(System.Reflection.MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                Shenxiao.Module.Core.Equip.EquipStrenModel model = Shenxiao.Module.Core.Equip.EquipStrenModel.Instance;
                model.Clear();

                // 15204 查询回包:res:i, equip_type:c, stren:h。res=1(成功) equip_type=1(武器) stren=3。
                byte[] p15204 = new CliVerify.Pkt().I(1).C(1).H(3).Bytes();
                Feed(m15204, p15204);
                bool queryOk = model.GetStren(1) == 3;
                Debug.Log("CLIVERIFY equipstren 15204 equipType1Stren=" + model.GetStren(1) + " ok=" + queryOk);

                // 15205 强化成功:res:i, res1:c, type:c, stren_info[u16×{equip_type:c, stren:h}]。
                // res=1(成功) res1=0 type=2(一键) stren_info 2项:{equip_type=1,stren=4}(覆盖) {equip_type=2,stren=1}(新增)。
                byte[] p15205Ok = new CliVerify.Pkt()
                    .I(1).C(0).C(2)
                    .H(2)          // stren_info 计数
                        .C(1).H(4)     // equip_type=1 stren=4
                        .C(2).H(1)     // equip_type=2 stren=1
                    .Bytes();
                Feed(m15205, p15205Ok);
                bool strenOk = model.GetStren(1) == 4 && model.TotalStren() == 5;
                Debug.Log("CLIVERIFY equipstren 15205 ok equipType1Stren=" + model.GetStren(1)
                    + " total=" + model.TotalStren() + " ok=" + strenOk);

                // 15205 强化失败:res=1500(常见=铜币不足/已到上限),只要不抛异常即过,数据不应回退。
                byte[] p15205Fail = new CliVerify.Pkt().I(1500).C(0).C(1).H(0).Bytes();
                bool failNoThrow = true;
                try { Feed(m15205, p15205Fail); }
                catch (System.Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY equipstren 15205 fail threw: " + e); }
                bool dataUnchanged = model.GetStren(1) == 4 && model.TotalStren() == 5;
                Debug.Log("CLIVERIFY equipstren 15205 fail noThrow=" + failNoThrow
                    + " dataUnchanged=" + dataUnchanged + " total=" + model.TotalStren());

                model.Clear();
                bool clearOk = !model.HasData && model.TotalStren() == 0;
                Debug.Log("CLIVERIFY equipstren clear hasData=" + model.HasData + " ok=" + clearOk);

                bool pass = queryOk && strenOk && failNoThrow && dataUnchanged && clearOk;
                Debug.Log("CLIVERIFY equipstren VERDICT queryOk=" + queryOk + " strenOk=" + strenOk
                    + " failNoThrow=" + failNoThrow + " dataUnchanged=" + dataUnchanged + " clearOk=" + clearOk + " pass=" + pass);

                await Task.CompletedTask;
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }
    }
}
