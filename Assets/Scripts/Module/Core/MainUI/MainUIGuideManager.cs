using System;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    public sealed class MainUIGuideManager
    {
        private const string GUIDE_SELECT_SLOT = "main_ui_guide_select";
        private const string GUIDE_FINGER_SLOT = "main_ui_guide_finger";

        public static readonly MainUIGuideManager Instance = new MainUIGuideManager();

        private object _owner;
        private RectTransform _target;
        private ArrowComponent _arrow;
        private UIEffectStage.Handle _selectEffect;
        private UIEffectStage.Handle _fingerEffect;
        private RectTransform _selectHolder;
        private RectTransform _fingerHolder;
        private bool _arrowLoading;
        private bool _effectLoading;
        private int _version;

        private MainUIGuideManager() { }

        public void ShowMainUiFinger(object owner, RectTransform target, ArrowData data, Action autoAction = null,
            bool isTaskItem = false)
        {
            if (owner == null || target == null || data == null)
            {
                HideMainUiFinger(owner);
                return;
            }

            if (_target != null && _target != target)
            {
                ClearVisuals();
            }

            _owner = owner;
            _target = target;
            data.Target = target;

            if (target.gameObject != null) target.gameObject.SetActive(true);
            _version++;
            int version = _version;
            _ = ShowMainUiFingerAsync(version, data, autoAction, isTaskItem);
        }

        public void HideMainUiFinger(object owner)
        {
            if (owner != null && _owner != null && !ReferenceEquals(owner, _owner)) return;
            _version++;
            ClearVisuals();
            _owner = null;
            _target = null;
        }

        private async Task ShowMainUiFingerAsync(int version, ArrowData data, Action autoAction, bool isTaskItem)
        {
            while (_arrowLoading)
            {
                await Task.Yield();
                if (!IsCurrent(version)) return;
            }

            if (_arrow == null)
            {
                _arrowLoading = true;
                Transform parent = _target.parent != null ? _target.parent : _target;
                GameObject go = await ResManager.InstantiateAsync(
                    GameResPath.GetUIPrefab("mainUI", "ArrowComponent"), parent);
                _arrowLoading = false;
                if (go == null) return;
                if (!IsCurrent(version))
                {
                    ResManager.ReleaseInstance(go);
                    return;
                }

                _arrow = go.GetComponent<ArrowComponent>();
                if (_arrow == null)
                {
                    GameLog.Warn("MainUI", "ArrowComponent prefab missing business component. Run mainUI convert + bind backfill.");
                    ResManager.ReleaseInstance(go);
                    return;
                }
            }

            if (data.NotEffect) ClearEffects();
            else await EnsureEffectsAsync(version, isTaskItem);
            if (!IsCurrent(version)) return;

            _arrow.Show();
            _arrow.transform.SetAsLastSibling();
            _arrow.SetData(data, autoAction);
        }

        private async Task EnsureEffectsAsync(int version, bool isTaskItem)
        {
            while (_effectLoading)
            {
                await Task.Yield();
                if (!IsCurrent(version)) return;
            }

            if (_selectEffect != null && _fingerEffect != null) return;

            _effectLoading = true;
            try
            {
                if (_selectEffect == null)
                {
                    _selectHolder = EnsureEffectHolder(_selectHolder, "__main_ui_guide_select_holder");
                    UIEffectSlot slot = FindEffectSlot(GUIDE_SELECT_SLOT);
                    if (slot == null)
                    {
                        GameLog.Warn("MainUI", "Guide select effect slot missing: {0}", GUIDE_SELECT_SLOT);
                    }
                    else
                    {
                        _selectEffect = await UIEffectStage.AddByKeyAsync(slot.EffectName, slot.AddressKey,
                            _selectHolder, slot.Position, ScaleSlot(slot, GetSelectEffectScale(isTaskItem)),
                            slot.RotationY);
                    }
                    if (!IsCurrent(version))
                    {
                        ClearEffects();
                        return;
                    }
                }

                if (_fingerEffect == null)
                {
                    _fingerHolder = EnsureEffectHolder(_fingerHolder, "__main_ui_guide_finger_holder");
                    UIEffectSlot slot = FindEffectSlot(GUIDE_FINGER_SLOT);
                    if (slot == null)
                    {
                        GameLog.Warn("MainUI", "Guide finger effect slot missing: {0}", GUIDE_FINGER_SLOT);
                    }
                    else
                    {
                        _fingerEffect = await UIEffectStage.AddAsync(slot, _fingerHolder);
                    }
                    if (!IsCurrent(version))
                    {
                        ClearEffects();
                    }
                }
            }
            finally
            {
                _effectLoading = false;
            }
        }

        private bool IsCurrent(int version)
        {
            return version == _version
                && _target != null
                && _target.gameObject.activeInHierarchy;
        }

        private RectTransform EnsureEffectHolder(RectTransform holder, string name)
        {
            if (holder != null) return holder;
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(_target, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(720f, 1280f);
            rt.SetAsLastSibling();
            return rt;
        }

        private Vector3 GetSelectEffectScale(bool isTaskItem)
        {
            if (!isTaskItem || _target == null) return Vector3.one;
            return _target.rect.height <= 55f
                ? Vector3.one
                : new Vector3(1.02f, 1.4f, 1f);
        }

        private UIEffectSlot FindEffectSlot(string slotId)
        {
            if (_target == null || string.IsNullOrEmpty(slotId)) return null;
            UIEffectSlot[] slots = _target.GetComponentsInChildren<UIEffectSlot>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].SlotId == slotId) return slots[i];
            }
            return null;
        }

        private static Vector3 ScaleSlot(UIEffectSlot slot, Vector3 multiplier)
        {
            Vector3 baseScale = slot != null ? slot.Scale : Vector3.one;
            return new Vector3(baseScale.x * multiplier.x, baseScale.y * multiplier.y, baseScale.z * multiplier.z);
        }

        private void ClearVisuals()
        {
            if (_arrow != null)
            {
                _arrow.Hide();
                ResManager.ReleaseInstance(_arrow.gameObject);
                _arrow = null;
            }
            ClearEffects();
            _arrowLoading = false;
        }

        private void ClearEffects()
        {
            if (_selectEffect != null)
            {
                _selectEffect.Dispose();
                _selectEffect = null;
            }
            if (_fingerEffect != null)
            {
                _fingerEffect.Dispose();
                _fingerEffect = null;
            }
            if (_selectHolder != null)
            {
                DestroyObject(_selectHolder.gameObject);
                _selectHolder = null;
            }
            if (_fingerHolder != null)
            {
                DestroyObject(_fingerHolder.gameObject);
                _fingerHolder = null;
            }
            _effectLoading = false;
        }

        private static void DestroyObject(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
