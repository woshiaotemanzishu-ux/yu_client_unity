using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 场景 NPC/怪物共用的当前选中表现，对标老端 Scene.CreateClickTargetEffect / SetClickTarget。
    /// 真实资源固定为 other_effect/function_selection；选中切换、对象移除和切场景都会回收旧实例。
    /// </summary>
    public static class SceneTargetSelection
    {
        private enum TargetKind
        {
            None,
            Npc,
            Monster,
        }

        private const string EffectName = "function_selection";
        private const float EffectScale = 0.7f;
        // SceneCharacterStage 的角色容器为 -38°；老端把选中特效挂角色根后用 -StartRotate.x 抵消。
        private const float TiltCompensation = 38f;

        private static TargetKind _kind;
        private static int _targetId;
        private static int _epoch;
        private static bool _loading;
        private static GameObject _effect;

        public static void SelectNpc(int npcId) => Select(TargetKind.Npc, npcId);

        public static void SelectMonster(int instanceId) => Select(TargetKind.Monster, instanceId);

        public static void Clear()
        {
            _kind = TargetKind.None;
            _targetId = 0;
            _epoch++;
            _loading = false;
            DestroyEffect();
        }

        internal static void OnNpcReady(int npcId)
        {
            if (_kind == TargetKind.Npc && _targetId == npcId && _effect == null) BeginAttach();
        }

        internal static void OnMonsterReady(int instanceId)
        {
            if (_kind == TargetKind.Monster && _targetId == instanceId && _effect == null) BeginAttach();
        }

        internal static void OnNpcRemoved(int npcId)
        {
            if (_kind == TargetKind.Npc && _targetId == npcId) Clear();
        }

        internal static void OnMonsterRemoved(int instanceId)
        {
            if (_kind == TargetKind.Monster && _targetId == instanceId) Clear();
        }

        private static void Select(TargetKind kind, int targetId)
        {
            if (targetId <= 0)
            {
                Clear();
                return;
            }

            if (_kind == kind && _targetId == targetId && _effect != null)
            {
                if (!_effect.activeSelf) EffectBinder.PlayEffect(_effect);
                return;
            }

            _kind = kind;
            _targetId = targetId;
            _epoch++;
            _loading = false;
            DestroyEffect();
            BeginAttach();
        }

        private static void BeginAttach()
        {
            if (_loading || _effect != null) return;
            if (!TryGetTargetRoot(out Transform targetRoot)) return;

            _loading = true;
            int epoch = _epoch;
            _ = AttachAsync(targetRoot, epoch);
        }

        private static async Task AttachAsync(Transform targetRoot, int epoch)
        {
            GameObject effect = await EffectBinder.AttachOne(
                targetRoot.gameObject,
                "",
                "other_effect",
                EffectName,
                "target_selection",
                false);

            if (epoch != _epoch || targetRoot == null || effect == null || !IsCurrentTargetRoot(targetRoot))
            {
                if (effect != null) Object.Destroy(effect);
                if (epoch == _epoch) _loading = false;
                return;
            }

            effect.transform.SetParent(targetRoot, false);
            effect.transform.localPosition = Vector3.zero;
            effect.transform.localRotation = Quaternion.Euler(TiltCompensation, 0f, 0f);
            effect.transform.localScale = Vector3.one * EffectScale;
            _effect = effect;
            _loading = false;
            EffectBinder.PlayEffect(effect);
            GameLog.Info("Scene", "selection effect attached: kind={0} id={1} res={2}", _kind, _targetId, EffectName);
        }

        private static bool TryGetTargetRoot(out Transform root)
        {
            switch (_kind)
            {
                case TargetKind.Npc:
                    return NpcRenderer.TryGetSelectionRoot(_targetId, out root);
                case TargetKind.Monster:
                    return MonsterRenderer.TryGetSelectionRoot(_targetId, out root);
                default:
                    root = null;
                    return false;
            }
        }

        private static bool IsCurrentTargetRoot(Transform root)
        {
            return root != null && TryGetTargetRoot(out Transform current) && current == root;
        }

        private static void DestroyEffect()
        {
            if (_effect == null) return;
            _effect.SetActive(false);
            Object.Destroy(_effect);
            _effect = null;
        }
    }
}
