using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Dialogue;
using Shenxiao.Module.Core.Scene.Vo;
using Shenxiao.Module.Core.Skill;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 场景级指针捕获(对标老客户端 Scene.ts 在 stage 上挂的 MOUSE_DOWN/MOVE/UP):
    /// 一张铺满屏幕的透明 raycast 接收板,挂在所有 UI 之后(canvas 最底层),通过 UGUI EventSystem
    /// 收指针事件写入 <see cref="SceneInput"/>。
    ///
    /// 为什么走 EventSystem 而不是 UnityEngine.Input:本工程 Active Input Handling = Input System Package,
    /// 直接读旧 Input 会每帧抛 InvalidOperationException 刷屏;EventSystem 用当前生效的输入模块派发,
    /// 与输入后端无关。"落在 UI 按钮上的点击不进摇杆"也自然成立——上层 UI 先吃掉射线,空白处才落到本板。
    ///
    /// 由 MainRoleFlow 在主角创建时 EnsureInstalled,清理主角时 Remove(无主角无移动)。
    /// </summary>
    public sealed class SceneInputDriver : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private static SceneInputDriver _instance;

        public static void EnsureInstalled()
        {
            if (_instance != null) return;

            Transform scene = ViewManager.GetLayer(UILayer.Scene);
            Transform canvas = scene != null ? scene.parent : null;
            if (canvas == null)
            {
                GameLog.Warn("Scene", "skip scene input: UI canvas is not ready");
                return;
            }

            var go = new GameObject("__SceneInputCatcher", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling(); // 置于所有 UI 层之后,只接没人要的空白区点击

            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f); // 全透明,只作 raycast 接收(raycastTarget 与 alpha 无关)
            img.raycastTarget = true;

            _instance = go.AddComponent<SceneInputDriver>();
        }

        public static void Remove()
        {
            if (_instance == null) return;
            SceneInput.End();
            Destroy(_instance.gameObject);
            _instance = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            SceneInput.Begin(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            SceneInput.Move(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            float upDist = SceneInput.Active
                ? Vector2.Distance(eventData.position, SceneInput.StartScreen)
                : float.MaxValue;
            if (SceneInput.Active && SceneInput.MoveDist < SceneInput.MoveDeadZone && upDist < SceneInput.MoveDeadZone)
            {
                SceneObjectClickRouter.HandleTap(eventData.position);
            }
            SceneInput.End();
        }
    }

    /// <summary>
    /// Routes taps on the scene RT back to real scene objects.
    /// Scene NPCs/monsters are rendered into a RawImage, so they cannot receive
    /// collider or UGUI clicks directly.
    /// </summary>
    internal static class SceneObjectClickRouter
    {
        public static bool HandleTap(Vector2 screenPosition)
        {
            if (MonsterRenderer.TryHitMonster(screenPosition, out MonsterVo monster))
            {
                return HandleMonster(monster);
            }

            if (NpcRenderer.TryHitNpc(screenPosition, out NpcVo npc))
            {
                return HandleNpc(npc);
            }

            return false;
        }

        private static bool HandleNpc(NpcVo npc)
        {
            if (npc == null) return false;

            SceneCombat.Instance.SetClickTarget(0);
            MainRoleAgent agent = MainRoleAgent.Current;
            if (agent == null)
            {
                GameLog.Warn("Scene", "click npc {0}: no MainRoleAgent, open dialogue directly", npc.NpcId);
                DialogueController.Instance.ShowTask(npc.NpcId);
                return true;
            }

            GameLog.Info("Scene", "click npc {0} pos=({1},{2}) -> move and open dialogue", npc.NpcId, npc.X, npc.Y);
            agent.MoveToNpcStrict(npc.X, npc.Y, 0f, () => DialogueController.Instance.ShowTask(npc.NpcId));
            return true;
        }

        private static bool HandleMonster(MonsterVo monster)
        {
            if (monster == null) return false;

            if (monster.IsCollect)
            {
                // 点采集物 → 接近并采集(对标老端 Scene.MainRoleAttackMonster 采集物分支 → OPEN_COLLECT_VIEW)。
                GameLog.Info("Scene", "click collect target ins={0} type={1} → 接近并采集", monster.InstanceId, monster.TypeId);
                SceneCombat.Instance.SetClickTarget(monster.InstanceId);
                CollectController.Instance.CollectMonster(monster);
                return true;
            }

            bool started = SceneCombat.Instance.MainRoleAttackMonster(
                monster.InstanceId,
                0,
                SkillManager.ONLY_FIRE_ATTACK);

            // 对标老端 Scene.ts:592-594:点中可攻击怪即静默点亮自动战斗(Fire(STARTAUTOFIGHT, false, true),
            // 不弹提示、不写 cookie)→ 持续攻击环放行,打到目标死、再自动猎下一只。
            // 移植时漏掉这个边沿曾导致"点一次打一次"。SetAutoFight 内部同值早退,EnsureRunning 幂等。
            if (monster.CanAttack == 1)
            {
                AutoFight.AutoFightModel.Instance.SetAutoFight(true);
                AutoFight.AutoFightController.Instance.EnsureRunning();
            }

            GameLog.Info("Scene", "click monster ins={0} type={1} attackStarted={2}(可攻击则静默开自动,对标老端点怪持续打)",
                monster.InstanceId, monster.TypeId, started);
            return true;
        }
    }
}
