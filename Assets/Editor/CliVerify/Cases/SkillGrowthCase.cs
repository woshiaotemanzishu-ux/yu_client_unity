using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 技能成长线(自动循环 轮3)实证:纯逻辑用例,不建 Stage/不渲染(仿 ReliveCase/GoodsProtoCase 套路:
    /// 手工按服务端权威字节序拼合成包,反射喂 SkillController/FightController 私有 On2xxxx handler,
    /// 断言模型状态 + GlobalEvent 触发。日志前缀统一 "CLIVERIFY skillgrowth"。
    /// 覆盖:21001 成/败、21010 全量(尾哨兵)、21011 成功(LessPoint减+模型等级变)/失败码、21012 成功、
    /// 13008/13010 State、12093 列表、18401 key2+key6 解析、20006 AssistVo 全结构读包(尾哨兵)+防御方血量同步。
    /// </summary>
    public static class SkillGrowthCase
    {
        public static async Task<int> Run()
        {
            object skillCtrl = Shenxiao.Module.Core.Skill.SkillController.Instance;
            object fightCtrl = Shenxiao.Module.Core.Scene.FightController.Instance;
            const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

            MethodInfo GetM(object target, string name)
            {
                MethodInfo m = target.GetType().GetMethod(name, F);
                if (m == null) Debug.LogError("CLIVERIFY skillgrowth handler missing(reflection): " + name);
                return m;
            }

            MethodInfo m21001 = GetM(skillCtrl, "On21001");
            MethodInfo m21010 = GetM(skillCtrl, "On21010");
            MethodInfo m21011 = GetM(skillCtrl, "On21011");
            MethodInfo m21012 = GetM(skillCtrl, "On21012");
            MethodInfo m13008 = GetM(skillCtrl, "On13008");
            MethodInfo m13010 = GetM(skillCtrl, "On13010");
            MethodInfo m12093 = GetM(skillCtrl, "On12093");
            MethodInfo m18401 = GetM(skillCtrl, "On18401");
            MethodInfo m20006 = GetM(fightCtrl, "On20006");
            if (m21001 == null || m21010 == null || m21011 == null || m21012 == null || m13008 == null
                || m13010 == null || m12093 == null || m18401 == null || m20006 == null)
            {
                return 3;
            }

            void Feed(MethodInfo m, object target, Shenxiao.Framework.Net.NetReader reader) =>
                m.Invoke(target, new object[] { reader });
            Shenxiao.Framework.Net.NetReader Reader(byte[] pkt) => new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length);

            Shenxiao.Module.Core.Skill.SkillManager.Instance.Clear();
            Shenxiao.Module.Core.Skill.SkillTalentModel.Instance.Clear();

            bool upgradeOk = Test21001(m21001, skillCtrl, Feed, Reader);
            bool talentInfoOk = Test21010(m21010, skillCtrl, Feed, Reader);
            bool talentLearnOk = Test21011(m21011, skillCtrl, Feed, Reader);
            bool talentResetOk = Test21012(m21012, skillCtrl, Feed, Reader);
            bool quickbarSaveOk = Test13008(m13008, skillCtrl, Feed, Reader);
            bool quickbarSwapOk = Test13010(m13010, skillCtrl, Feed, Reader);
            bool careerBuffOk = Test12093(m12093, skillCtrl, Feed, Reader);
            bool moduleBuffOk = Test18401(m18401, skillCtrl, Feed, Reader);
            bool assistOk = Test20006(m20006, fightCtrl, Feed, Reader);

            Shenxiao.Module.Core.Skill.SkillManager.Instance.Clear();
            Shenxiao.Module.Core.Skill.SkillTalentModel.Instance.Clear();

            bool pass = upgradeOk && talentInfoOk && talentLearnOk && talentResetOk && quickbarSaveOk
                && quickbarSwapOk && careerBuffOk && moduleBuffOk && assistOk;
            Debug.Log("CLIVERIFY skillgrowth VERDICT upgrade=" + upgradeOk + " talentInfo=" + talentInfoOk
                + " talentLearn=" + talentLearnOk + " talentReset=" + talentResetOk + " quickbarSave=" + quickbarSaveOk
                + " quickbarSwap=" + quickbarSwapOk + " careerBuff=" + careerBuffOk + " moduleBuff=" + moduleBuffOk
                + " assist=" + assistOk + " pass=" + pass);
            await Task.CompletedTask;
            return pass ? 0 : 3;
        }

        // ---- 21001:[1,skillId]→EVT_SKILL_LEVEL_UP;[2100004,skillId]→toast「升级失败(2100004)」不炸 ----
        private static bool Test21001(MethodInfo m, object ctrl, Action<MethodInfo, object, Shenxiao.Framework.Net.NetReader> feed,
            Func<byte[], Shenxiao.Framework.Net.NetReader> reader)
        {
            (int skillId, bool fired) emitted = (-1, false);
            Action<int> onLevelUp = id => emitted = (id, true);
            EventDispatcher.On(GlobalEvent.EVT_SKILL_LEVEL_UP, onLevelUp);
            feed(m, ctrl, reader(new CliVerify.Pkt().I(1).I(59100021).Bytes()));
            EventDispatcher.Off(GlobalEvent.EVT_SKILL_LEVEL_UP, onLevelUp);
            bool okSuccess = emitted.fired && emitted.skillId == 59100021;

            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            bool noThrow = true;
            try { feed(m, ctrl, reader(new CliVerify.Pkt().I(2100004).I(59100021).Bytes())); }
            catch (Exception e) { noThrow = false; Debug.LogError("CLIVERIFY skillgrowth 21001 fail threw: " + e); }
            finally { Application.logMessageReceived -= cb; }
            bool toastOk = logs.Exists(l => l.Contains("升级失败"));

            bool ok = okSuccess && noThrow && toastOk;
            Debug.Log("CLIVERIFY skillgrowth 21001 okSuccess=" + okSuccess + " noThrow=" + noThrow + " toastOk=" + toastOk + " ok=" + ok);
            return ok;
        }

        // ---- 21010:LessPoint=16 + 2 组(5/3/[100:2,101:1], 6/1/[200:1]) + 尾哨兵 ----
        private static bool Test21010(MethodInfo m, object ctrl, Action<MethodInfo, object, Shenxiao.Framework.Net.NetReader> feed,
            Func<byte[], Shenxiao.Framework.Net.NetReader> reader)
        {
            const int sentinel = 0xBEEF;
            byte[] pkt = new CliVerify.Pkt()
                .H(16).H(2)
                    .C(5).H(3).H(2).I(100).H(2).I(101).H(1)
                    .C(6).H(1).H(1).I(200).H(1)
                .H(sentinel)
                .Bytes();

            var r = reader(pkt);
            bool fired = false;
            Action onInfo = () => fired = true;
            EventDispatcher.On(GlobalEvent.EVT_TALENT_INFO, onInfo);
            feed(m, ctrl, r);
            EventDispatcher.Off(GlobalEvent.EVT_TALENT_INFO, onInfo);

            int tail = r.ReadU16();
            bool sentinelOk = tail == sentinel;

            var model = Shenxiao.Module.Core.Skill.SkillTalentModel.Instance;
            bool fieldsOk = model.HasTalentInfo && model.LessPoint == 16
                && model.GetGroup(5)?.Point == 3 && model.GetTalentLevel(100) == 2 && model.GetTalentLevel(101) == 1
                && model.GetGroup(6)?.Point == 1 && model.GetTalentLevel(200) == 1;

            bool ok = fired && sentinelOk && fieldsOk;
            Debug.Log("CLIVERIFY skillgrowth 21010 fired=" + fired + " sentinelOk=" + sentinelOk + " fieldsOk=" + fieldsOk + " ok=" + ok);
            return ok;
        }

        // ---- 21011:成功([1,100,3,13])→LessPoint=13+GetTalentLevel(100)=3+EVT_TALENT_LEARNED;失败码不炸 ----
        private static bool Test21011(MethodInfo m, object ctrl, Action<MethodInfo, object, Shenxiao.Framework.Net.NetReader> feed,
            Func<byte[], Shenxiao.Framework.Net.NetReader> reader)
        {
            var model = Shenxiao.Module.Core.Skill.SkillTalentModel.Instance;
            (int skillId, int skillLv, bool fired) emitted = (-1, -1, false);
            Action<int, int> onLearned = (id, lv) => emitted = (id, lv, true);
            EventDispatcher.On(GlobalEvent.EVT_TALENT_LEARNED, onLearned);
            feed(m, ctrl, reader(new CliVerify.Pkt().I(1).I(100).H(3).H(13).Bytes()));
            EventDispatcher.Off(GlobalEvent.EVT_TALENT_LEARNED, onLearned);

            bool okSuccess = emitted.fired && emitted.skillId == 100 && emitted.skillLv == 3
                && model.LessPoint == 13 && model.GetTalentLevel(100) == 3;

            bool noThrow = true;
            try { feed(m, ctrl, reader(new CliVerify.Pkt().I(2100007).I(100).H(3).H(0).Bytes())); }
            catch (Exception e) { noThrow = false; Debug.LogError("CLIVERIFY skillgrowth 21011 fail threw: " + e); }

            bool ok = okSuccess && noThrow;
            Debug.Log("CLIVERIFY skillgrowth 21011 okSuccess=" + okSuccess + " lessPoint=" + model.LessPoint
                + " talentLv100=" + model.GetTalentLevel(100) + " noThrow=" + noThrow + " ok=" + ok);
            return ok;
        }

        // ---- 21012:成功([1,7])→toast「天赋重置成功」+EVT_TALENT_RESET;失败码不炸 ----
        private static bool Test21012(MethodInfo m, object ctrl, Action<MethodInfo, object, Shenxiao.Framework.Net.NetReader> feed,
            Func<byte[], Shenxiao.Framework.Net.NetReader> reader)
        {
            bool fired = false;
            Action onReset = () => fired = true;
            EventDispatcher.On(GlobalEvent.EVT_TALENT_RESET, onReset);

            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try { feed(m, ctrl, reader(new CliVerify.Pkt().I(1).H(7).Bytes())); }
            finally { Application.logMessageReceived -= cb; }
            EventDispatcher.Off(GlobalEvent.EVT_TALENT_RESET, onReset);
            bool toastOk = logs.Exists(l => l.Contains("天赋重置成功"));

            bool noThrow = true;
            try { feed(m, ctrl, reader(new CliVerify.Pkt().I(2100009).H(0).Bytes())); }
            catch (Exception e) { noThrow = false; Debug.LogError("CLIVERIFY skillgrowth 21012 fail threw: " + e); }

            bool ok = fired && toastOk && noThrow;
            Debug.Log("CLIVERIFY skillgrowth 21012 fired=" + fired + " toastOk=" + toastOk + " noThrow=" + noThrow + " ok=" + ok);
            return ok;
        }

        // ---- 13008:State=1→toast「保存成功」+EVT_QUICKBAR_SAVED(false) ----
        private static bool Test13008(MethodInfo m, object ctrl, Action<MethodInfo, object, Shenxiao.Framework.Net.NetReader> feed,
            Func<byte[], Shenxiao.Framework.Net.NetReader> reader)
        {
            (bool isSwap, bool fired) emitted = (true, false);
            Action<bool> onSaved = swap => emitted = (swap, true);
            EventDispatcher.On(GlobalEvent.EVT_QUICKBAR_SAVED, onSaved);
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try { feed(m, ctrl, reader(new CliVerify.Pkt().C(1).Bytes())); }
            finally { Application.logMessageReceived -= cb; }
            EventDispatcher.Off(GlobalEvent.EVT_QUICKBAR_SAVED, onSaved);

            bool toastOk = logs.Exists(l => l.Contains("保存成功"));
            bool ok = emitted.fired && !emitted.isSwap && toastOk;
            Debug.Log("CLIVERIFY skillgrowth 13008 fired=" + emitted.fired + " isSwap=" + emitted.isSwap + " toastOk=" + toastOk + " ok=" + ok);
            return ok;
        }

        // ---- 13010:State=1→toast「替换成功」+EVT_QUICKBAR_SAVED(true) ----
        private static bool Test13010(MethodInfo m, object ctrl, Action<MethodInfo, object, Shenxiao.Framework.Net.NetReader> feed,
            Func<byte[], Shenxiao.Framework.Net.NetReader> reader)
        {
            (bool isSwap, bool fired) emitted = (false, false);
            Action<bool> onSaved = swap => emitted = (swap, true);
            EventDispatcher.On(GlobalEvent.EVT_QUICKBAR_SAVED, onSaved);
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try { feed(m, ctrl, reader(new CliVerify.Pkt().C(1).Bytes())); }
            finally { Application.logMessageReceived -= cb; }
            EventDispatcher.Off(GlobalEvent.EVT_QUICKBAR_SAVED, onSaved);

            bool toastOk = logs.Exists(l => l.Contains("替换成功"));
            bool ok = emitted.fired && emitted.isSwap && toastOk;
            Debug.Log("CLIVERIFY skillgrowth 13010 fired=" + emitted.fired + " isSwap=" + emitted.isSwap + " toastOk=" + toastOk + " ok=" + ok);
            return ok;
        }

        // ---- 12093:2 项 → SkillTalentModel.CareerSkillBuffList + EVT_CAREER_SKILL_BUFF ----
        private static bool Test12093(MethodInfo m, object ctrl, Action<MethodInfo, object, Shenxiao.Framework.Net.NetReader> feed,
            Func<byte[], Shenxiao.Framework.Net.NetReader> reader)
        {
            bool fired = false;
            Action onBuff = () => fired = true;
            EventDispatcher.On(GlobalEvent.EVT_CAREER_SKILL_BUFF, onBuff);
            feed(m, ctrl, reader(new CliVerify.Pkt().H(2).I(59100021).H(3).I(59100022).H(1).Bytes()));
            EventDispatcher.Off(GlobalEvent.EVT_CAREER_SKILL_BUFF, onBuff);

            var list = Shenxiao.Module.Core.Skill.SkillTalentModel.Instance.CareerSkillBuffList;
            bool ok = fired && list.Count == 2 && list[0].skillId == 59100021 && list[0].skillLv == 3
                && list[1].skillId == 59100022 && list[1].skillLv == 1;
            Debug.Log("CLIVERIFY skillgrowth 12093 fired=" + fired + " count=" + list.Count + " ok=" + ok);
            return ok;
        }

        // ---- 18401:key=2 Erlang term onhook_time=7200 → OnhookMaxTimeSec=79200;key=6 "1.5" → LifeSkillAdd=1.5 ----
        private static bool Test18401(MethodInfo m, object ctrl, Action<MethodInfo, object, Shenxiao.Framework.Net.NetReader> feed,
            Func<byte[], Shenxiao.Framework.Net.NetReader> reader)
        {
            bool fired = false;
            Action onModuleBuff = () => fired = true;
            EventDispatcher.On(GlobalEvent.EVT_MODULE_BUFF_LIST, onModuleBuff);
            byte[] pkt = new CliVerify.Pkt()
                .H(2)
                    .I(2).S("[{onhook_time,7200}]")
                    .I(6).S("1.5")
                .Bytes();
            feed(m, ctrl, reader(pkt));
            EventDispatcher.Off(GlobalEvent.EVT_MODULE_BUFF_LIST, onModuleBuff);

            var model = Shenxiao.Module.Core.Skill.SkillTalentModel.Instance;
            bool onhookOk = model.OnhookMaxTimeSec == 20 * 3600 + 7200;
            bool lifeSkillOk = System.Math.Abs(model.LifeSkillAdd - 1.5) < 0.0001;
            bool onhookControllerOk = Shenxiao.Module.Core.OnHook.OnHookController.MaxOnlineTimeSec == model.OnhookMaxTimeSec;

            bool ok = fired && onhookOk && lifeSkillOk && onhookControllerOk;
            Debug.Log("CLIVERIFY skillgrowth 18401 fired=" + fired + " onhookMaxSec=" + model.OnhookMaxTimeSec
                + " lifeSkillAdd=" + model.LifeSkillAdd + " onhookControllerOk=" + onhookControllerOk + " ok=" + ok);
            return ok;
        }

        // ---- 20006:AssistVo 全结构(尾哨兵)+ 防御方(怪888)血量 1000→500 同步 ----
        private static bool Test20006(MethodInfo m, object ctrl, Action<MethodInfo, object, Shenxiao.Framework.Net.NetReader> feed,
            Func<byte[], Shenxiao.Framework.Net.NetReader> reader)
        {
            const long attackerRole = 777L;
            const int attackerType = 2; // OBJ_ROLE
            const int skillId = 59200001;
            const int skillLevel = 1;
            const long defenderMonsterIns = 888L;
            const int sentinel = 0xBEEF;

            var mgr = Shenxiao.Module.Core.Scene.SceneManager.Instance;
            mgr.Clear();
            mgr.AddMonster(new Shenxiao.Module.Core.Scene.Vo.MonsterVo { InstanceId = (int)defenderMonsterIns, Hp = 1000, HpLim = 1000 });

            byte[] pkt = new CliVerify.Pkt()
                .L(attackerRole).C(attackerType).I(skillId).C(skillLevel)
                .H(1)
                    .C(1).L(defenderMonsterIns).L(500).H(0) // type_flag=1怪, hp=500, buff_num=0
                .H(sentinel)
                .Bytes();

            var r = reader(pkt);
            Shenxiao.Module.Core.Scene.Vo.AssistVo captured = null;
            Action<Shenxiao.Module.Core.Scene.Vo.AssistVo> onAssist = vo => captured = vo;
            EventDispatcher.On(GlobalEvent.EVT_ASSIST_SKILL, onAssist);
            feed(m, ctrl, r);
            EventDispatcher.Off(GlobalEvent.EVT_ASSIST_SKILL, onAssist);

            int tail = r.ReadU16();
            bool sentinelOk = tail == sentinel;

            bool voOk = captured != null && captured.RoleId == attackerRole && captured.AttackerType == attackerType
                && captured.SkillId == skillId && captured.SkillLevel == skillLevel
                && captured.DefenseList.Count == 1 && captured.DefenseList[0].RoleId == defenderMonsterIns
                && captured.DefenseList[0].Hp == 500;

            bool hpSyncOk = mgr.GetMonster((int)defenderMonsterIns)?.Hp == 500;

            mgr.Clear();

            bool ok = sentinelOk && voOk && hpSyncOk;
            Debug.Log("CLIVERIFY skillgrowth 20006 sentinelOk=" + sentinelOk + " voOk=" + voOk + " hpSyncOk=" + hpSyncOk + " ok=" + ok);
            return ok;
        }
    }
}
