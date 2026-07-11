using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 宝石(骸珀镶嵌,自动循环 轮4 下半/4b)实证:15210(雕刻信息)/15211(雕刻)/15215(宝石升级)/15216(宝石合成,
    /// 服务端语义自循环)合成包反射喂 <see cref="Shenxiao.Module.Core.Equip.EquipJewelController"/> 私有 handler,
    /// 断言模型套值 + 尾哨兵字节不被多吃/少吃(GameLog "remaining=NB" 行)+ 失败码包不炸 + 15216 一键序列真的能停
    /// (对标老端 on15216 model.one_key_mark 语义);渲染段:实例化 JewelModule.prefab,拉起 EquipJewelView(主页签)+
    /// EquipJewelCraveView(雕刻子窗),克隆一枚 EquipJewelCraveSubItem 喂真实 15210 数据断言雕刻等级文本(照
    /// InnateViewCase 套路+截图)。独立用例文件(避免多代理改 CliVerify.cs 冲突),复用 CliVerify.Stage/Pkt。
    /// 日志前缀统一 "CLIVERIFY jewel"。
    /// </summary>
    public static class JewelCase
    {
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

        public static async Task<int> Run()
        {
            // 0) 装配(幂等,照 InnateViewCase 先跑 Creator 的套路):JewelModule.prefab 烤入产物只有基类 *Bind,
            // 且 EquipJewelView 子树被烤进 CommonModule/__Templates —— 先嫁接 + 升级运行时子类(详见 JewelBindUpgrader)。
            if (!Shenxiao.Editor.UiCreator.Equip.JewelBindUpgrader.Generate())
            {
                Debug.LogError("CLIVERIFY jewel FAIL JewelBindUpgrader.Generate()(嫁接/升级失败,看前面 [UiCreator] 日志)");
                return 3;
            }

            CliVerify.Stage stage = CliVerify.Stage.Create();
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                bool protoOk = RunProto(logs);
                bool renderOk = await RunRenderAsync(stage, logs);

                bool pass = protoOk && renderOk;
                Debug.Log("CLIVERIFY jewel VERDICT proto=" + protoOk + " render=" + renderOk + " pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                Application.logMessageReceived -= cb;
                stage.Dispose();
            }
        }

        private static void Feed(object ctrl, MethodInfo m, byte[] pkt) =>
            m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

        // ---- 协议(15210/15211/15215/15216) ----

        private static bool RunProto(List<string> logs)
        {
            object ctrl = Shenxiao.Module.Core.Equip.EquipJewelController.Instance;
            MethodInfo m10 = ctrl.GetType().GetMethod("On15210", F);
            MethodInfo m11 = ctrl.GetType().GetMethod("On15211", F);
            MethodInfo m15 = ctrl.GetType().GetMethod("On15215", F);
            MethodInfo m16 = ctrl.GetType().GetMethod("On15216", F);
            if (m10 == null || m11 == null || m15 == null || m16 == null)
            {
                Debug.LogError("CLIVERIFY jewel handlers missing (reflection)");
                return false;
            }

            var model = Shenxiao.Module.Core.Equip.EquipJewelModel.Instance;
            model.Clear();

            // 15210 查询(尾哨兵 2 字节 0xEE 0xEE):res=1 equip_pos=1 refine_lv=3 exp=120,1 项属性 {attr_id=1,attr_val=50}。
            // refine_lv/attr_id 均是 1 字节(服务端 pt_152.erl 实证,规格草稿的 refine_lv:h 有误,详见控制器注释)。
            logs.Clear();
            byte[] p10 = new CliVerify.Pkt().I(1).C(1).C(3).I(120).H(1).C(1).I(50).C(0xEE).C(0xEE).Bytes();
            Feed(ctrl, m10, p10);
            Shenxiao.Module.Core.Equip.EquipJewelModel.CraveInfo? crave = model.GetCrave(1);
            bool tail10 = logs.Exists(l => l.Contains("remaining=2B"));
            bool queryOk = crave.HasValue && crave.Value.RefineLv == 3 && crave.Value.Exp == 120
                && crave.Value.Attrs.Count == 1 && crave.Value.Attrs[0].AttrId == 1 && crave.Value.Attrs[0].AttrVal == 50 && tail10;
            Debug.Log("CLIVERIFY jewel 15210 refineLv=" + (crave?.RefineLv ?? -1) + " exp=" + (crave?.Exp ?? -1)
                + " tail=" + tail10 + " ok=" + queryOk);

            // 15211 雕刻成功(res=1 equip_pos=1 is_up=1 one_key=0)→ 对标老端 on15211 自动重发 15210(不抛异常即过,
            // 反射直调无法窃听"是否真的调了 SendFmt",但 SendFmt 在无连接编辑期环境已验证安全 no-op,详见
            // EquipGrowthCase 同类先例 OpenSlot 调用)。
            logs.Clear();
            byte[] p11Ok = new CliVerify.Pkt().I(1).C(1).C(1).C(0).Bytes();
            bool crave11NoThrow = true;
            try { Feed(ctrl, m11, p11Ok); }
            catch (Exception e) { crave11NoThrow = false; Debug.LogError("CLIVERIFY jewel 15211 ok threw: " + e); }
            Debug.Log("CLIVERIFY jewel 15211 ok noThrow=" + crave11NoThrow);

            // 15211 失败(res=1500)→ toast 显码,不抛异常。
            logs.Clear();
            bool crave11FailNoThrow = true;
            try { Feed(ctrl, m11, new CliVerify.Pkt().I(1500).C(1).C(0).C(0).Bytes()); }
            catch (Exception e) { crave11FailNoThrow = false; Debug.LogError("CLIVERIFY jewel 15211 fail threw: " + e); }
            bool crave11FailToast = logs.Exists(l => l.Contains("toast: 雕刻失败(1500)"));
            Debug.Log("CLIVERIFY jewel 15211 fail noThrow=" + crave11FailNoThrow + " toast=" + crave11FailToast);

            // 15215 三种 upgrade_type 各一发(0普通/1一键低级/2直升丹),均走成功分支(res=1),断言不抛异常 + toast。
            bool upgrade15Ok = true;
            for (int upgradeType = 0; upgradeType <= 2; upgradeType++)
            {
                logs.Clear();
                byte[] p15 = new CliVerify.Pkt().I(1).C(1).C(1).I(200 + upgradeType).Bytes();
                try { Feed(ctrl, m15, p15); }
                catch (Exception e) { upgrade15Ok = false; Debug.LogError("CLIVERIFY jewel 15215 type=" + upgradeType + " threw: " + e); }
                bool toastOk = logs.Exists(l => l.Contains("toast: 升级宝石成功"));
                if (!toastOk) upgrade15Ok = false;
                Debug.Log("CLIVERIFY jewel 15215 type=" + upgradeType + " toastOk=" + toastOk);
            }
            // 15215 失败码包(res=1500),只要不抛异常即过。
            bool upgrade15FailNoThrow = true;
            try { Feed(ctrl, m15, new CliVerify.Pkt().I(1500).C(1).C(1).I(0).Bytes()); }
            catch (Exception e) { upgrade15FailNoThrow = false; Debug.LogError("CLIVERIFY jewel 15215 fail threw: " + e); }

            // 15216 一键自循环:成功包(res=1 type_id=500 is_one_key=1)→ 服务端语义要求自循环续发(内部再次
            // SendFmt,断言不抛异常);随后喂失败码包(res=1500,is_one_key=1)→ 序列自然终止,toast「合成宝石成功」
            // (对标老端 model.one_key_mark 语义,非真失败);再喂一次失败码包 → 此时 mark 已清,应走真失败分支
            // (toast 显码),证明序列真的"停了"而非无限重试。
            logs.Clear();
            byte[] p16Ok = new CliVerify.Pkt().I(1).I(500).C(1).Bytes();
            bool combine16NoThrow = true;
            try { Feed(ctrl, m16, p16Ok); }
            catch (Exception e) { combine16NoThrow = false; Debug.LogError("CLIVERIFY jewel 15216 ok threw: " + e); }
            Debug.Log("CLIVERIFY jewel 15216 ok(one_key=1) noThrow=" + combine16NoThrow);

            logs.Clear();
            byte[] p16Fail1 = new CliVerify.Pkt().I(1500).I(500).C(1).Bytes();
            bool combine16Fail1NoThrow = true;
            try { Feed(ctrl, m16, p16Fail1); }
            catch (Exception e) { combine16Fail1NoThrow = false; Debug.LogError("CLIVERIFY jewel 15216 fail1 threw: " + e); }
            bool combine16Fail1Toast = logs.Exists(l => l.Contains("toast: 合成宝石成功"));
            Debug.Log("CLIVERIFY jewel 15216 fail1(序列终止) noThrow=" + combine16Fail1NoThrow + " toast=" + combine16Fail1Toast);

            logs.Clear();
            byte[] p16Fail2 = new CliVerify.Pkt().I(1500).I(500).C(1).Bytes();
            bool combine16Fail2NoThrow = true;
            try { Feed(ctrl, m16, p16Fail2); }
            catch (Exception e) { combine16Fail2NoThrow = false; Debug.LogError("CLIVERIFY jewel 15216 fail2 threw: " + e); }
            bool combine16Fail2RealFail = logs.Exists(l => l.Contains("toast: 合成宝石失败(1500)"));
            Debug.Log("CLIVERIFY jewel 15216 fail2(真失败,序列已停) noThrow=" + combine16Fail2NoThrow + " realFail=" + combine16Fail2RealFail);

            model.Clear();
            bool pass = queryOk && crave11NoThrow && crave11FailNoThrow && crave11FailToast
                && upgrade15Ok && upgrade15FailNoThrow
                && combine16NoThrow && combine16Fail1NoThrow && combine16Fail1Toast
                && combine16Fail2NoThrow && combine16Fail2RealFail;
            Debug.Log("CLIVERIFY jewel proto VERDICT pass=" + pass);
            return pass;
        }

        // ---- 渲染:JewelModule.prefab → EquipJewelView + EquipJewelCraveView → 雕刻等级文本 ----

        private static async Task<bool> RunRenderAsync(CliVerify.Stage stage, List<string> logs)
        {
            // 独立喂一份 15210(equip_pos=1,refine_lv=7),与协议段互不干扰(protoAssertions 已 Clear 过模型)。
            object ctrl = Shenxiao.Module.Core.Equip.EquipJewelController.Instance;
            MethodInfo m10 = ctrl.GetType().GetMethod("On15210", F);
            byte[] p10 = new CliVerify.Pkt().I(1).C(1).C(7).I(0).H(0).Bytes();
            Feed(ctrl, m10, p10);

            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Jewel/JewelModule.prefab");
            if (prefab == null)
            {
                Debug.LogError("CLIVERIFY jewel JewelModule.prefab missing");
                return false;
            }
            GameObject go = UnityEngine.Object.Instantiate(prefab, stage.CanvasRoot);
            try
            {
                var jewelView = go.GetComponentInChildren<Shenxiao.Module.Core.Equip.EquipJewelView>(true);
                if (jewelView == null)
                {
                    Debug.LogError("CLIVERIFY jewel EquipJewelView missing in JewelModule.prefab");
                    return false;
                }
                jewelView.gameObject.SetActive(true);
                jewelView.Show();

                var craveView = go.GetComponentInChildren<Shenxiao.Module.Core.Equip.EquipJewelCraveView>(true);
                if (craveView == null)
                {
                    Debug.LogError("CLIVERIFY jewel EquipJewelCraveView missing in JewelModule.prefab");
                    return false;
                }
                craveView.gameObject.SetActive(true);
                craveView.Show();

                GameObject subTemplate = craveView._tpl_EquipJewelCraveSubItem;
                if (subTemplate == null)
                {
                    Debug.LogError("CLIVERIFY jewel _tpl_EquipJewelCraveSubItem missing");
                    return false;
                }
                GameObject rowGo = UnityEngine.Object.Instantiate(subTemplate, subTemplate.transform.parent);
                rowGo.SetActive(true);
                var row = rowGo.GetComponent<Shenxiao.Module.Core.Equip.EquipJewelCraveSubItem>();
                if (row == null)
                {
                    Debug.LogError("CLIVERIFY jewel EquipJewelCraveSubItem component missing on template clone");
                    return false;
                }
                row.Show();       // Bind 子组件须父 Show() 触发 EnsureBound(轮3 三坑规避)
                row.SetData(1);   // equip_pos=1,对应上面喂的 15210(refine_lv=7)

                await Task.Delay(300);
                stage.ForceCjkFont();

                TMPro.TextMeshProUGUI levelLabel = row.level_label;
                bool levelTextOk = levelLabel != null && levelLabel.text == "Lv.7";
                string png = stage.Capture("Temp/jewel_crave_level.png");
                Debug.Log("CLIVERIFY jewel render levelTextOk=" + levelTextOk + "(text=" + (levelLabel != null ? levelLabel.text : "<null>")
                    + ") shot=" + png);

                bool pass = levelTextOk;
                Debug.Log("CLIVERIFY jewel render VERDICT pass=" + pass);
                return pass;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                Shenxiao.Module.Core.Equip.EquipJewelModel.Instance.Clear();
            }
        }
    }
}
