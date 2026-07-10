using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 复活链(自动循环 队列#2 轮2)实证:纯逻辑用例,不建 Stage/不渲染(仿 GoodsProtoCase 套路:
    /// 手工按服务端权威字节序拼合成包,反射喂 FightController/ReliveController 私有 On2xxxx handler,
    /// 断言 ReliveModel 状态 + GlobalEvent 触发。日志前缀统一 "CLIVERIFY relive"。
    /// 覆盖:20013 死亡广播(含尾哨兵字节游标校验)、20004 复活结果(成功/失败/12特殊成功)、
    /// 20009 复活信息、20017 疲劳查询、20022 模拟死亡(他人)、20027 技能CD结束(单条)、
    /// RequestRelive 1秒节流。
    /// </summary>
    public static class ReliveCase
    {
        public static async Task<int> Run()
        {
            object fightCtrl = Shenxiao.Module.Core.Scene.FightController.Instance;
            object reliveCtrl = Shenxiao.Module.Core.Relive.ReliveController.Instance;
            const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

            MethodInfo GetM(object target, string name)
            {
                MethodInfo m = target.GetType().GetMethod(name, F);
                if (m == null) Debug.LogError("CLIVERIFY relive handler missing(reflection): " + name);
                return m;
            }

            MethodInfo m20013 = GetM(fightCtrl, "On20013");
            MethodInfo m20022 = GetM(fightCtrl, "On20022");
            MethodInfo m20027 = GetM(fightCtrl, "On20027");
            MethodInfo m20004 = GetM(reliveCtrl, "On20004");
            MethodInfo m20009 = GetM(reliveCtrl, "On20009");
            MethodInfo m20017 = GetM(reliveCtrl, "On20017");
            if (m20013 == null || m20022 == null || m20027 == null || m20004 == null || m20009 == null || m20017 == null)
            {
                return 3;
            }

            void Feed(MethodInfo m, object target, Shenxiao.Framework.Net.NetReader reader) =>
                m.Invoke(target, new object[] { reader });
            void FeedBytes(MethodInfo m, object target, byte[] pkt) =>
                Feed(m, target, new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length));

            Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId = 999; // 20022"他人"分支需要一个确定的"自己" id
            Shenxiao.Module.Core.Relive.ReliveModel.Instance.Clear();

            bool killerOk = Test20013(m20013, fightCtrl);
            bool reliveResultOk = Test20004(m20004, reliveCtrl, FeedBytes);
            bool reviveInfoOk = Test20009(m20009, reliveCtrl, FeedBytes);
            bool tiredOk = Test20017(m20017, reliveCtrl, FeedBytes);
            bool simulateOk = Test20022Other(m20022, fightCtrl, FeedBytes);
            bool cdEndOk = Test20027(m20027, fightCtrl, FeedBytes);
            bool throttleOk = TestRequestReliveThrottle();

            Shenxiao.Module.Core.Relive.ReliveModel.Instance.Clear();

            bool pass = killerOk && reliveResultOk && reviveInfoOk && tiredOk && simulateOk && cdEndOk && throttleOk;
            Debug.Log("CLIVERIFY relive VERDICT killer=" + killerOk + " reliveResult=" + reliveResultOk
                + " reviveInfo=" + reviveInfoOk + " tired=" + tiredOk + " simulate=" + simulateOk
                + " cdEnd=" + cdEndOk + " throttle=" + throttleOk + " pass=" + pass);
            await Task.CompletedTask;
            return pass ? 0 : 3;
        }

        // ---- 20013:死亡广播(含尾哨兵字节游标校验)→ ReliveModel killer 字段 + EVT_ROLE_DEAD + IsDead ----
        private static bool Test20013(MethodInfo m20013, object fightCtrl)
        {
            const int killerType = 1;      // 被怪杀死
            const string killerName = "哨兵怪"; // killerId=8888 不是任何真实 config_mon 模板id(6位数),3级fallback 不会覆盖它
            const long killerId = 8888L;
            const int sentinel = 0xBEEF;

            byte[] packet = new CliVerify.Pkt()
                .C(killerType).S(killerName)
                .H(999).C(5).H(88).C(2)   // 罪恶值/扣除元宝/玩家等级/几转,老端读后即弃
                .L(killerId)
                .H(sentinel)              // 尾哨兵(不属于 20013 schema)
                .Bytes();

            var reader = new Shenxiao.Framework.Net.NetReader(packet, 0, packet.Length);
            bool deadFired = false;
            Action onDead = () => deadFired = true;
            EventDispatcher.On(GlobalEvent.EVT_ROLE_DEAD, onDead);
            m20013.Invoke(fightCtrl, new object[] { reader });
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_DEAD, onDead);

            int tail = reader.ReadU16();
            bool sentinelOk = tail == sentinel;

            Shenxiao.Module.Core.Relive.ReliveModel model = Shenxiao.Module.Core.Relive.ReliveModel.Instance;
            bool fieldsOk = model.IsDead && model.KillerType == killerType && model.KillerId == killerId
                && model.KillerName == killerName;

            bool ok = sentinelOk && fieldsOk && deadFired;
            Debug.Log("CLIVERIFY relive 20013 sentinelOk=" + sentinelOk + " fieldsOk=" + fieldsOk
                + " deadFired=" + deadFired + " killerName=" + model.KillerName + " ok=" + ok);
            return ok;
        }

        // ---- 20004:[22,1]成功清死亡态;[22,6]失败(铜币不足)不清;[13,12]特殊成功(REVIVE_BOSS/ASHES)按成功路径 ----
        private static bool Test20004(MethodInfo m20004, object reliveCtrl, Action<MethodInfo, object, byte[]> feedBytes)
        {
            var model = Shenxiao.Module.Core.Relive.ReliveModel.Instance;

            // [22,1] 成功
            model.SetKiller(1, 1, "test");
            (int type, bool fired) success = (-1, false);
            Action<int> onSuccess1 = t => success = (t, true);
            EventDispatcher.On(GlobalEvent.EVT_RELIVE_SUCCESS, onSuccess1);
            feedBytes(m20004, reliveCtrl, new CliVerify.Pkt().C(22).C(1).Bytes());
            EventDispatcher.Off(GlobalEvent.EVT_RELIVE_SUCCESS, onSuccess1);
            bool okSuccess = success.fired && success.type == 22 && !model.IsDead;

            // [22,6] 失败(铜币不足):toast 走到(log 断言)+ IsDead 不清
            model.SetKiller(1, 1, "test"); // 重新置死亡态
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try { feedBytes(m20004, reliveCtrl, new CliVerify.Pkt().C(22).C(6).Bytes()); }
            finally { Application.logMessageReceived -= cb; }
            bool toastOk = logs.Exists(l => l.Contains("铜币不足"));
            bool stillDead = model.IsDead;

            // [13,12] 特殊成功(REVIVE_BOSS/ASHES 服务端把 Res 改写成 12,按成功路径)
            model.SetKiller(1, 1, "test");
            (int type, bool fired) success2 = (-1, false);
            Action<int> onSuccess2 = t => success2 = (t, true);
            EventDispatcher.On(GlobalEvent.EVT_RELIVE_SUCCESS, onSuccess2);
            feedBytes(m20004, reliveCtrl, new CliVerify.Pkt().C(13).C(12).Bytes());
            EventDispatcher.Off(GlobalEvent.EVT_RELIVE_SUCCESS, onSuccess2);
            bool ok12 = success2.fired && success2.type == 13 && !model.IsDead;

            bool ok = okSuccess && toastOk && stillDead && ok12;
            Debug.Log("CLIVERIFY relive 20004 okSuccess=" + okSuccess + " toastOk=" + toastOk
                + " stillDeadAfterFail=" + stillDead + " ok12=" + ok12 + " ok=" + ok);
            return ok;
        }

        // ---- 20009:can_relive=1,next_time=1234567 → model + EVT_RELIVE_INFO ----
        private static bool Test20009(MethodInfo m20009, object reliveCtrl, Action<MethodInfo, object, byte[]> feedBytes)
        {
            long emitted = -1;
            Action<long> onInfo = t => emitted = t;
            EventDispatcher.On(GlobalEvent.EVT_RELIVE_INFO, onInfo);
            feedBytes(m20009, reliveCtrl, new CliVerify.Pkt().C(1).I(1234567).Bytes());
            EventDispatcher.Off(GlobalEvent.EVT_RELIVE_INFO, onInfo);

            var model = Shenxiao.Module.Core.Relive.ReliveModel.Instance;
            bool ok = model.HasReviveInfo && model.CanRelive && model.NextReviveTime == 1234567 && emitted == 1234567;
            Debug.Log("CLIVERIFY relive 20009 canRelive=" + model.CanRelive + " nextReviveTime=" + model.NextReviveTime
                + " emitted=" + emitted + " ok=" + ok);
            return ok;
        }

        // ---- 20017:[2,999] → model tired + EVT_RELIVE_TIRED ----
        private static bool Test20017(MethodInfo m20017, object reliveCtrl, Action<MethodInfo, object, byte[]> feedBytes)
        {
            (int num, long end, bool fired) emitted = (-1, -1, false);
            Action<int, long> onTired = (num, end) => emitted = (num, end, true);
            EventDispatcher.On(GlobalEvent.EVT_RELIVE_TIRED, onTired);
            feedBytes(m20017, reliveCtrl, new CliVerify.Pkt().H(2).I(999).Bytes());
            EventDispatcher.Off(GlobalEvent.EVT_RELIVE_TIRED, onTired);

            var model = Shenxiao.Module.Core.Relive.ReliveModel.Instance;
            bool ok = model.TiredCount == 2 && model.TiredEndTime == 999 && emitted.fired && emitted.num == 2 && emitted.end == 999;
            Debug.Log("CLIVERIFY relive 20017 tiredCount=" + model.TiredCount + " tiredEndTime=" + model.TiredEndTime + " ok=" + ok);
            return ok;
        }

        // ---- 20022(died=他人,不在 SceneManager 视野内)→ 不炸 + EVT_SIMULATE_FIGHT ----
        private static bool Test20022Other(MethodInfo m20022, object fightCtrl, Action<MethodInfo, object, byte[]> feedBytes)
        {
            const long killerId = 1L;
            const long diedId = 555L; // != RoleModel.RoleId(999),且未在 SceneManager 注册 → 走"未在视野"警告分支

            (long killer, long died, bool fired) emitted = (-1, -1, false);
            Action<long, long> onSimulate = (k, d) => emitted = (k, d, true);
            EventDispatcher.On(GlobalEvent.EVT_SIMULATE_FIGHT, onSimulate);

            bool noThrow = true;
            try
            {
                feedBytes(m20022, fightCtrl, new CliVerify.Pkt().L(killerId).L(diedId).L(50).L(100).Bytes());
            }
            catch (Exception e)
            {
                noThrow = false;
                Debug.LogError("CLIVERIFY relive 20022 threw: " + e);
            }
            EventDispatcher.Off(GlobalEvent.EVT_SIMULATE_FIGHT, onSimulate);

            bool ok = noThrow && emitted.fired && emitted.killer == killerId && emitted.died == diedId;
            Debug.Log("CLIVERIFY relive 20022 noThrow=" + noThrow + " emitted=" + emitted.fired
                + " killer=" + emitted.killer + " died=" + emitted.died + " ok=" + ok);
            return ok;
        }

        // ---- 20027:单条 skill_id=42,end_time=99999 → EVT_SKILL_CD_END(不是数组循环)----
        private static bool Test20027(MethodInfo m20027, object fightCtrl, Action<MethodInfo, object, byte[]> feedBytes)
        {
            (int skillId, long endTime, bool fired) emitted = (-1, -1, false);
            Action<int, long> onCdEnd = (id, end) => emitted = (id, end, true);
            EventDispatcher.On(GlobalEvent.EVT_SKILL_CD_END, onCdEnd);
            feedBytes(m20027, fightCtrl, new CliVerify.Pkt().I(42).L(99999).Bytes());
            EventDispatcher.Off(GlobalEvent.EVT_SKILL_CD_END, onCdEnd);

            bool ok = emitted.fired && emitted.skillId == 42 && emitted.endTime == 99999;
            Debug.Log("CLIVERIFY relive 20027 skillId=" + emitted.skillId + " endTime=" + emitted.endTime + " ok=" + ok);
            return ok;
        }

        // ---- RequestRelive 节流:连调两次只应发一次(离线 SendFmt 不炸,数日志)----
        private static bool TestRequestReliveThrottle()
        {
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            bool noThrow = true;
            try
            {
                Shenxiao.Module.Core.Relive.ReliveController.Instance.RequestRelive(22);
                Shenxiao.Module.Core.Relive.ReliveController.Instance.RequestRelive(22);
            }
            catch (Exception e)
            {
                noThrow = false;
                Debug.LogError("CLIVERIFY relive RequestRelive threw: " + e);
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }

            bool throttledLogged = logs.Exists(l => l.Contains("1秒节流内跳过"));
            bool ok = noThrow && throttledLogged;
            Debug.Log("CLIVERIFY relive throttle noThrow=" + noThrow + " throttledLogged=" + throttledLogged + " ok=" + ok);
            return ok;
        }
    }
}
