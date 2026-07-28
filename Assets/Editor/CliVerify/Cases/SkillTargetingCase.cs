using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Module.Core.AutoFight;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene;
using Shenxiao.Module.Core.Scene.Vo;
using Shenxiao.Module.Core.Skill;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// MainUI 技能专项：普通自动战斗可手点、13017 托管态拦截；接敌距离和圆/直线/扇形目标收集对齐老端。
    /// </summary>
    public static class SkillTargetingCase
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;

        public static async Task<int> Run()
        {
            bool oldFallback = ResManager.EditorPreferFallback;
            ResManager.EditorPreferFallback = true;
            try
            {
                await SkillConfigs.EnsureLoaded();
                if (!SkillConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY skill-targeting FAIL config_skill 未加载");
                    return 3;
                }

                int failures = 0;
                VerifyConfiguredRanges(ref failures);
                VerifyClickGate(ref failures);
                VerifyGeometry(ref failures);
                Debug.Log("CLIVERIFY skill-targeting RESULT failures=" + failures);
                return failures == 0 ? 0 : 3;
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY skill-targeting EXCEPTION " + e);
                return 3;
            }
            finally
            {
                ResManager.EditorPreferFallback = oldFallback;
            }
        }

        private static void VerifyConfiguredRanges(ref int failures)
        {
            MethodInfo attackRange = typeof(SceneCombat).GetMethod("AttackRange", PrivateStatic);
            Check(attackRange != null, "存在 SceneCombat.AttackRange", ref failures);
            if (attackRange == null) return;

            CheckClose((float)attackRange.Invoke(null, new object[] { 59100001 }), 360f,
                "mode1 御剑一式=(distance100+area350)*0.8=360", ref failures);
            CheckClose((float)attackRange.Invoke(null, new object[] { 59100019 }), 440f,
                "mode1 追魂一剑=(distance缺省50+area500)*0.8=440", ref failures);
            CheckClose((float)attackRange.Invoke(null, new object[] { 59810001 }), 480f,
                "mode2 月华流辉=distance600*0.8=480", ref failures);
        }

        private static void VerifyClickGate(ref int failures)
        {
            AutoFightModel autoFight = AutoFightModel.Instance;
            RoleModel role = RoleModel.Instance;
            int oldWeight = autoFight.AutoFightWeight;
            bool oldTempMode = autoFight.TempMode;
            bool oldDeposit = role.DepositState;
            var go = new GameObject("SkillTargetingCase_Item");
            MainUISkillItem item = go.AddComponent<MainUISkillItem>();
            FieldInfo voField = typeof(MainUISkillItem).GetField("_vo", PrivateInstance);
            MethodInfo click = typeof(MainUISkillItem).GetMethod("OnClickSkill", PrivateInstance);
            int clickCount = 0;
            Action<int, int> onClick = (skillId, attackType) =>
            {
                if (skillId == 59100001 && attackType == SkillManager.ONLY_FIRE_ATTACK)
                    clickCount++;
            };

            try
            {
                SkillManager.Instance.Clear();
                voField?.SetValue(item, new SkillVo(59100001) { Level = 1 });
                EventDispatcher.On<int, int>(GlobalEvent.EVT_SKILL_SHORTCUT_CLICK, onClick);

                autoFight.SetAutoFight(true);
                role.DepositState = false;
                click?.Invoke(item, null);
                Check(clickCount == 1, "普通 AutoFight 开启时手点技能仍发释放事件", ref failures);

                role.DepositState = true;
                click?.Invoke(item, null);
                Check(clickCount == 1, "13017 deposit_state 托管态吞掉手点技能", ref failures);
            }
            finally
            {
                EventDispatcher.Off<int, int>(GlobalEvent.EVT_SKILL_SHORTCUT_CLICK, onClick);
                role.DepositState = oldDeposit;
                autoFight.SetAutoFightWeight(oldWeight);
                autoFight.SetTempMode(oldTempMode);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void VerifyGeometry(ref int failures)
        {
            SceneManager scene = SceneManager.Instance;
            RoleModel role = RoleModel.Instance;
            var savedMonsters = new List<MonsterVo>(scene.AllMonsters);
            int oldX = role.X;
            int oldY = role.Y;
            try
            {
                role.X = 0;
                role.Y = 0;

                scene.Clear();
                MonsterVo circlePrimary = Monster(101, 100, 0);
                scene.AddMonster(circlePrimary);
                scene.AddMonster(Monster(102, 440, 0));
                scene.AddMonster(Monster(103, 451, 0));
                List<int> circle = BuildPlanIds(59100001, circlePrimary);
                Check(SequenceEqual(circle, 101, 102),
                    "range1 圆形：主目标置首，半径350含340差值、排除351差值", ref failures);

                scene.Clear();
                MonsterVo linePrimary = Monster(201, 500, 0);
                scene.AddMonster(linePrimary);
                scene.AddMonster(Monster(202, 400, 140));
                scene.AddMonster(Monster(203, 400, 151));
                scene.AddMonster(Monster(204, -50, 0));
                List<int> line = BuildPlanIds(59810001, linePrimary);
                Check(SequenceEqual(line, 201, 204, 202),
                    "range2 直线：600长/300宽，主目标置首，100px近身目标免方向，排除半宽外目标", ref failures);

                scene.Clear();
                MonsterVo sectorPrimary = Monster(301, 500, 0);
                scene.AddMonster(sectorPrimary);
                scene.AddMonster(Monster(302, 500, 500));
                scene.AddMonster(Monster(303, -150, 0));
                scene.AddMonster(Monster(304, -50, 0));
                List<int> sector = BuildPlanIds(59830029, sectorPrimary);
                Check(SequenceEqual(sector, 301, 304, 302),
                    "range3 扇形：1000长/180度，主目标置首，100px近身目标免方向，排除身后远目标", ref failures);

                scene.Clear();
                scene.AddMonster(Monster(401, 1000, 0));
                object noTargetPlan = BuildPlan(59100001, null);
                Check(GetPlanPrimary(noTargetPlan) == null && GetPlanIds(noTargetPlan).Count == 0,
                    "手动无锁定目标只在 area 局部预选，不跨全场抓远怪", ref failures);
                CheckClose(GetPlanCenterY(noTargetPlan), 50f,
                    "无目标圆形圆心按老端椭圆修正：纵向 distance100→50", ref failures);
            }
            finally
            {
                scene.Clear();
                foreach (MonsterVo monster in savedMonsters) scene.AddMonster(monster);
                role.X = oldX;
                role.Y = oldY;
            }
        }

        private static object BuildPlan(int skillId, MonsterVo primary)
        {
            MethodInfo build = typeof(SceneCombat).GetMethod("BuildAttackPlan", PrivateStatic);
            if (build == null) throw new MissingMethodException(typeof(SceneCombat).FullName, "BuildAttackPlan");
            return build.Invoke(null, new object[] { skillId, primary });
        }

        private static List<int> BuildPlanIds(int skillId, MonsterVo primary)
        {
            return GetPlanIds(BuildPlan(skillId, primary));
        }

        private static List<int> GetPlanIds(object plan)
        {
            return (List<int>)plan.GetType().GetField("MonsterIds").GetValue(plan);
        }

        private static MonsterVo GetPlanPrimary(object plan)
        {
            return (MonsterVo)plan.GetType().GetField("Primary").GetValue(plan);
        }

        private static float GetPlanCenterY(object plan)
        {
            return (float)plan.GetType().GetField("CenterY").GetValue(plan);
        }

        private static MonsterVo Monster(int instanceId, int x, int y)
        {
            return new MonsterVo
            {
                InstanceId = instanceId,
                TypeId = 10001001,
                X = x,
                Y = y,
                Hp = 100,
                HpLim = 100,
                CanAttack = 1,
                Type = 0,
            };
        }

        private static bool SequenceEqual(List<int> actual, params int[] expected)
        {
            if (actual == null || actual.Count != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
                if (actual[i] != expected[i]) return false;
            return true;
        }

        private static void CheckClose(float actual, float expected, string label, ref int failures)
        {
            Check(Math.Abs(actual - expected) < 0.01f,
                label + " actual=" + actual + " expected=" + expected, ref failures);
        }

        private static void Check(bool ok, string label, ref int failures)
        {
            Debug.Log("CLIVERIFY skill-targeting " + label + " ok=" + ok);
            if (!ok) failures++;
        }
    }
}
