using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 通用奖励飞行动画。语义对齐老客户端 MainUIController.REWARD_FLY：
    /// 普通物品按 120ms 间隔飞向底部功能入口；元宝、绑元和金币散开后飞向顶部货币槽。
    /// 调用方只负责在权威成功回包后传入奖励与起点，不持有动画节点。
    /// </summary>
    public static class RewardFlyService
    {
        private const int ItemStaggerMs = 120;
        private const float ItemMoveSeconds = 0.75f;
        private const float MoneyScatterSeconds = 0.4f;
        private const float MoneyMoveSeconds = 0.7f;
        private const float MoneyEndScale = 0.4f;
        private static readonly Dictionary<int, ArrivalState> ArrivalStates =
            new Dictionary<int, ArrivalState>();

        private sealed class ArrivalState
        {
            public Vector3 BaseScale;
            public int Epoch;
        }

        public readonly struct Reward
        {
            public readonly int Style;
            public readonly int TypeId;
            public readonly long Count;

            public Reward(int style, int typeId, long count)
            {
                Style = style;
                TypeId = typeId;
                Count = count;
            }
        }

        public sealed class Handle : IDisposable
        {
            private readonly List<GameObject> _objects = new List<GameObject>();
            private readonly List<LegacyRgbaSequencePlayback> _sequences =
                new List<LegacyRgbaSequencePlayback>();

            public bool IsDisposed { get; private set; }
            public bool IsCompleted { get; private set; }

            internal void MarkCompleted() => IsCompleted = true;

            internal bool Track(GameObject value)
            {
                if (value == null) return false;
                if (IsDisposed)
                {
                    UnityEngine.Object.Destroy(value);
                    return false;
                }
                _objects.Add(value);
                return true;
            }

            internal bool Track(LegacyRgbaSequencePlayback value)
            {
                if (value == null) return false;
                if (IsDisposed)
                {
                    value.Dispose();
                    return false;
                }
                _sequences.Add(value);
                return true;
            }

            internal void Untrack(GameObject value) => _objects.Remove(value);
            internal void Untrack(LegacyRgbaSequencePlayback value) => _sequences.Remove(value);

            public void Dispose()
            {
                if (IsDisposed) return;
                IsDisposed = true;

                LegacyRgbaSequencePlayback[] sequences = _sequences.ToArray();
                _sequences.Clear();
                for (int i = 0; i < sequences.Length; i++) sequences[i]?.Dispose();

                GameObject[] objects = _objects.ToArray();
                _objects.Clear();
                for (int i = 0; i < objects.Length; i++)
                    if (objects[i] != null) UnityEngine.Object.Destroy(objects[i]);
            }
        }

        private readonly struct MoneyRoute
        {
            public readonly int MoneyType;
            public readonly string EffectName;
            public readonly string IconPath;

            public MoneyRoute(int moneyType, string effectName, string iconPath)
            {
                MoneyType = moneyType;
                EffectName = effectName;
                IconPath = iconPath;
            }
        }

        private readonly struct LegacyRgbaSequenceSpec
        {
            public readonly string Address;
            public readonly int FrameWidth;
            public readonly int FrameHeight;
            public readonly int FrameCount;
            public readonly int Columns;
            public readonly int Padding;
            public readonly int AtlasWidth;
            public readonly int AtlasHeight;
            public readonly float FrameRate;

            public LegacyRgbaSequenceSpec(string address)
            {
                Address = address;
                FrameWidth = 100;
                FrameHeight = 100;
                FrameCount = 120;
                Columns = 12;
                Padding = 2;
                AtlasWidth = 1248;
                AtlasHeight = 1040;
                FrameRate = 60f;
            }

            public int CellWidth => FrameWidth + Padding * 2;
            public int CellHeight => FrameHeight + Padding * 2;
        }

        /// <summary>
        /// 复播老 Laya UIEffect 的透明 RenderTexture 序列。一批奖励只加载并推进一份 source，
        /// 所有飞行物仅持有 RawImage presenter，与老端“一份 100x100 RT -> N 个 Image”拓扑一致。
        /// </summary>
        internal sealed class LegacyRgbaSequencePlayback : IDisposable
        {
            private readonly LegacyRgbaSequenceSpec _spec;
            private readonly Texture2D _atlas;
            private readonly List<RawImage> _presenters = new List<RawImage>();
            private readonly float _startedAt;
            private readonly Task _runner;
            private int _currentFrame;
            private bool _disposed;

            private LegacyRgbaSequencePlayback(LegacyRgbaSequenceSpec spec, Texture2D atlas)
            {
                _spec = spec;
                _atlas = atlas;
                _startedAt = Time.unscaledTime;
                _currentFrame = 0;
                _runner = RunAsync();
            }

            public static async Task<LegacyRgbaSequencePlayback> CreateAsync(string effectName)
            {
                if (!TryResolveSequence(effectName, out LegacyRgbaSequenceSpec spec)) return null;
                // LoadAsync keeps the Editor AssetDatabase fallback available when the
                // current Play Mode catalog predates this newly captured atlas.
                Texture2D atlas = await ResManager.LoadAsync<Texture2D>(spec.Address);
                if (atlas == null)
                {
                    GameLog.Warn("RewardFly", "legacy RGBA atlas missing: effect={0} key={1}",
                        effectName, spec.Address);
                    return null;
                }
                if (atlas.width != spec.AtlasWidth || atlas.height != spec.AtlasHeight)
                {
                    GameLog.Warn("RewardFly",
                        "legacy RGBA atlas size mismatch: effect={0} actual={1}x{2} expected={3}x{4}",
                        effectName, atlas.width, atlas.height, spec.AtlasWidth, spec.AtlasHeight);
                    ResManager.Release(atlas);
                    return null;
                }
                return new LegacyRgbaSequencePlayback(spec, atlas);
            }

            public RawImage AddPresenter(RectTransform rect)
            {
                if (_disposed || rect == null) return null;
                RawImage image = rect.gameObject.AddComponent<RawImage>();
                image.texture = _atlas;
                image.color = Color.white;
                image.raycastTarget = false;
                image.maskable = false;
                image.uvRect = GetFrameUv(_currentFrame);
                _presenters.Add(image);
                return image;
            }

            public void RemovePresenter(RawImage image)
            {
                if (image != null) image.texture = null;
                _presenters.Remove(image);
            }

            private async Task RunAsync()
            {
                try
                {
                    while (!_disposed)
                    {
                        await Task.Yield();
                        if (_disposed) break;
                        float elapsed = Mathf.Max(0f, Time.unscaledTime - _startedAt);
                        int frame = Mathf.FloorToInt(elapsed * _spec.FrameRate) % _spec.FrameCount;
                        if (frame != _currentFrame) ApplyFrame(frame);
                    }
                }
                catch (Exception ex)
                {
                    if (!_disposed)
                        GameLog.Warn("RewardFly", "legacy RGBA playback failed: {0}", ex.Message);
                }
            }

            private void ApplyFrame(int frame)
            {
                _currentFrame = Mathf.Clamp(frame, 0, _spec.FrameCount - 1);
                Rect uv = GetFrameUv(_currentFrame);
                for (int i = _presenters.Count - 1; i >= 0; i--)
                {
                    RawImage image = _presenters[i];
                    if (image == null)
                    {
                        _presenters.RemoveAt(i);
                        continue;
                    }
                    image.uvRect = uv;
                }
            }

            private Rect GetFrameUv(int frame)
            {
                int column = frame % _spec.Columns;
                int rowFromTop = frame / _spec.Columns;
                float x = (column * _spec.CellWidth + _spec.Padding) / (float)_spec.AtlasWidth;
                float y = 1f - (rowFromTop * _spec.CellHeight + _spec.Padding
                                + _spec.FrameHeight) / (float)_spec.AtlasHeight;
                return new Rect(x, y,
                    _spec.FrameWidth / (float)_spec.AtlasWidth,
                    _spec.FrameHeight / (float)_spec.AtlasHeight);
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                for (int i = 0; i < _presenters.Count; i++)
                    if (_presenters[i] != null) _presenters[i].texture = null;
                _presenters.Clear();
                ResManager.Release(_atlas);
                _ = _runner;
            }

            private static bool TryResolveSequence(string effectName,
                out LegacyRgbaSequenceSpec spec)
            {
                switch (effectName)
                {
                    case "ui_bangyu_1":
                        spec = new LegacyRgbaSequenceSpec(
                            "effect/legacy_rgba/reward_fly/ui_bangyu_1_atlas");
                        return true;
                    case "ui_bangyu_2":
                        spec = new LegacyRgbaSequenceSpec(
                            "effect/legacy_rgba/reward_fly/ui_bangyu_2_atlas");
                        return true;
                    default:
                        spec = default;
                        return false;
                }
            }
        }

        /// <summary>
        /// 播放一次奖励飞行。返回句柄可由页面关闭流程取消，取消和自然结束都会清理全部临时节点与特效。
        /// </summary>
        public static Handle Play(IReadOnlyList<Reward> rewards, Vector3 sourceWorld,
            string normalTargetRes = "bag")
        {
            var handle = new Handle();
            if (rewards == null || rewards.Count == 0)
            {
                handle.MarkCompleted();
                return handle;
            }
            _ = PlayInternalAsync(handle, rewards, sourceWorld, normalTargetRes);
            return handle;
        }

        private static async Task PlayInternalAsync(Handle handle, IReadOnlyList<Reward> rewards,
            Vector3 sourceWorld, string normalTargetRes)
        {
            try
            {
                RectTransform top = ViewManager.GetLayer(UILayer.Top) as RectTransform;
                if (top == null || handle.IsDisposed) return;

                Vector2 start = sourceWorld == Vector3.zero
                    ? Vector2.zero
                    : (Vector2)top.InverseTransformPoint(sourceWorld);
                var tasks = new List<Task>();
                var normalRewards = new List<(Reward reward, int index)>();

                for (int i = 0; i < rewards.Count; i++)
                {
                    Reward reward = rewards[i];
                    if (reward.Count <= 0) continue;
                    (int goodsId, _) = GoodsModel.GetMappingTypeId(reward.Style, reward.TypeId);
                    if (TryGetMoneyRoute(goodsId, out MoneyRoute route))
                        tasks.Add(PlayMoneyAsync(handle, top, reward.Count, start, route));
                    else
                        normalRewards.Add((reward, i));
                }

                if (normalRewards.Count > 0)
                    tasks.Add(PlayItemsAsync(handle, top, normalRewards, start, normalTargetRes));
                if (tasks.Count > 0) await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                GameLog.Warn("RewardFly", "reward fly failed: {0}", ex.Message);
                handle.Dispose();
            }
            finally
            {
                handle.MarkCompleted();
            }
        }

        private static async Task PlayItemsAsync(Handle handle, RectTransform top,
            IReadOnlyList<(Reward reward, int index)> rewards, Vector2 start, string targetRes)
        {
            MainFuncIconItem targetItem = UnityEngine.Object.FindObjectsByType<MainFuncIconItem>(
                    FindObjectsSortMode.None)
                .FirstOrDefault(item => item != null && item.isActiveAndEnabled && item.Res == targetRes);
            RectTransform target = targetItem != null ? targetItem.RewardFlyTarget : null;
            if (target == null)
            {
                GameLog.Warn("RewardFly", "normal reward target missing: {0}", targetRes);
                return;
            }

            Vector2 end = ToLocalPoint(top, target);
            var flights = new List<Task>(rewards.Count);
            for (int i = 0; i < rewards.Count; i++)
            {
                (Reward reward, int sourceIndex) = rewards[i];
                flights.Add(PlayItemAsync(handle, top, reward, sourceIndex, start, end));
            }
            await Task.WhenAll(flights);
            if (!handle.IsDisposed) await PlayArrivalAsync(handle, target);
        }

        private static async Task PlayItemAsync(Handle handle, RectTransform top, Reward reward,
            int sourceIndex, Vector2 start, Vector2 end)
        {
            if (sourceIndex > 0) await TimeUtil.Delay(sourceIndex * ItemStaggerMs);
            if (handle.IsDisposed || top == null) return;

            (int goodsId, _) = GoodsModel.GetMappingTypeId(reward.Style, reward.TypeId);
            string icon = GoodsModel.GetGoodsIcon(goodsId);
            if (string.IsNullOrEmpty(icon))
            {
                GameLog.Warn("RewardFly", "normal reward icon missing: goods={0}", goodsId);
                return;
            }

            GameObject go = CreateImageObject("RewardFlyItem", top, new Vector2(84f, 84f), out Image image);
            if (!handle.Track(go)) return;
            RectTransform rect = go.transform as RectTransform;
            rect.anchoredPosition = start;
            try
            {
                bool ready = await ResManager.SetImageAsync(
                    image, GameResPath.GetGoodsIconPath(icon), nativeSize: false);
                if (!ready || handle.IsDisposed || rect == null) return;
                await MoveAsync(handle, rect, start, end, ItemMoveSeconds,
                    Vector3.one, Vector3.one, rotateDegrees: 0f, easeInOut: false);
            }
            finally
            {
                handle.Untrack(go);
                if (go != null) UnityEngine.Object.Destroy(go);
            }
        }

        private static async Task PlayMoneyAsync(Handle handle, RectTransform top, long count,
            Vector2 start, MoneyRoute route)
        {
            MainUIMoneyItem moneyItem = UnityEngine.Object.FindObjectsByType<MainUIMoneyItem>(
                    FindObjectsSortMode.None)
                .FirstOrDefault(item => item != null && item.isActiveAndEnabled
                    && item.MoneyType == route.MoneyType);
            RectTransform target = moneyItem != null ? moneyItem.RewardFlyTarget : null;
            if (target == null)
            {
                GameLog.Warn("RewardFly", "money reward target missing: type={0}", route.MoneyType);
                return;
            }

            int showCount = count > 15L
                ? (int)Math.Min((count + 2L) / 3L, 20L)
                : 5;
            Vector2 end = ToLocalPoint(top, target);
            LegacyRgbaSequencePlayback sequence = null;
            try
            {
                if (!string.IsNullOrEmpty(route.EffectName))
                {
                    sequence = await LegacyRgbaSequencePlayback.CreateAsync(route.EffectName);
                    if (sequence == null || !handle.Track(sequence)) return;
                }

                var particles = new List<Task>(showCount);
                for (int i = 0; i < showCount; i++)
                    particles.Add(PlayMoneyParticleAsync(
                        handle, top, start, end, route, sequence, i));
                await Task.WhenAll(particles);
                if (!handle.IsDisposed) await PlayArrivalAsync(handle, target);
            }
            finally
            {
                if (sequence != null)
                {
                    handle.Untrack(sequence);
                    sequence.Dispose();
                }
            }
        }

        private static async Task PlayMoneyParticleAsync(Handle handle, RectTransform top,
            Vector2 start, Vector2 end, MoneyRoute route,
            LegacyRgbaSequencePlayback sequence, int index)
        {
            if (handle.IsDisposed || top == null) return;

            bool useEffect = sequence != null;
            Vector2 size = new Vector2(100f, 100f);
            GameObject go = CreateRectObject(
                useEffect ? "RewardFlyMoneyEffect" : "RewardFlyMoneyIcon", top, size);
            if (!handle.Track(go)) return;

            RectTransform rect = go.transform as RectTransform;
            rect.anchoredPosition = start;
            float initialScale = useEffect
                ? UnityEngine.Random.Range(0.7f, 1.2f)
                : UnityEngine.Random.Range(0.3f, 0.4f);
            GameObject visualObject = CreateRectObject("Visual", rect, size);
            RectTransform visual = visualObject.transform as RectTransform;
            visual.localScale = Vector3.one * initialScale;
            visual.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            RawImage sequenceImage = null;
            Image iconImage = null;
            try
            {
                if (useEffect)
                {
                    sequenceImage = sequence.AddPresenter(visual);
                    if (sequenceImage == null) return;
                }
                else
                {
                    iconImage = visualObject.AddComponent<Image>();
                    iconImage.raycastTarget = false;
                    iconImage.preserveAspect = true;
                    iconImage.color = Color.white;
                    bool ready = await ResManager.SetImageAsync(
                        iconImage, route.IconPath, nativeSize: false);
                    if (!ready) return;
                }
                if (handle.IsDisposed || rect == null) return;

                Rect bounds = top.rect;
                Vector2 scatter = new Vector2(
                    Mathf.Clamp(start.x + UnityEngine.Random.Range(-100f, 100f), bounds.xMin, bounds.xMax),
                    Mathf.Clamp(start.y + UnityEngine.Random.Range(-100f, 100f), bounds.yMin, bounds.yMax));
                float scaleDuration = MoneyMoveSeconds + index * 0.05f;
                Task rotation = useEffect
                    ? Task.CompletedTask
                    : RotateAsync(handle, visual, 720f, MoneyScatterSeconds + scaleDuration);
                await MoveAsync(handle, rect, start, scatter, MoneyScatterSeconds,
                    Vector3.one, Vector3.one, rotateDegrees: 0f, easeInOut: false);
                if (handle.IsDisposed) return;
                await MoveMoneySecondAsync(handle, rect, scatter, end,
                    scaleDuration + 0.1f, scaleDuration);
                await rotation;
            }
            finally
            {
                if (sequenceImage != null) sequence.RemovePresenter(sequenceImage);
                handle.Untrack(go);
                if (go != null) UnityEngine.Object.Destroy(go);
            }
        }

        private static async Task MoveMoneySecondAsync(Handle handle, RectTransform rect,
            Vector2 from, Vector2 to, float moveDuration, float scaleDuration)
        {
            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                await Task.Yield();
                if (handle.IsDisposed || rect == null) return;
                elapsed += Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : 0.016f;
                float moveT = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, moveDuration));
                float moveEase = moveT < 0.5f
                    ? 4f * moveT * moveT * moveT
                    : 1f + 4f * Mathf.Pow(moveT - 1f, 3f);
                float scaleT = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, scaleDuration));
                rect.anchoredPosition = Vector2.LerpUnclamped(from, to, moveEase);
                rect.localScale = Vector3.LerpUnclamped(
                    Vector3.one, Vector3.one * MoneyEndScale, scaleT);
            }
            if (rect != null)
            {
                rect.anchoredPosition = to;
                rect.localScale = Vector3.one * MoneyEndScale;
            }
        }

        private static async Task RotateAsync(Handle handle, RectTransform rect,
            float degrees, float duration)
        {
            if (rect == null) return;
            Quaternion start = rect.localRotation;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                await Task.Yield();
                if (handle.IsDisposed || rect == null) return;
                elapsed += Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : 0.016f;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                rect.localRotation = start * Quaternion.Euler(0f, 0f, degrees * t);
            }
        }

        private static async Task MoveAsync(Handle handle, RectTransform rect,
            Vector2 from, Vector2 to, float duration, Vector3 fromScale, Vector3 toScale,
            float rotateDegrees, bool easeInOut)
        {
            float elapsed = 0f;
            Quaternion startRotation = rect.localRotation;
            while (elapsed < duration)
            {
                await Task.Yield();
                if (handle.IsDisposed || rect == null) return;
                elapsed += Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : 0.016f;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                float eased = easeInOut
                    ? t * t * (3f - 2f * t)
                    : 1f - Mathf.Pow(1f - t, 3f);
                rect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
                rect.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
                if (Mathf.Abs(rotateDegrees) > 0.01f)
                    rect.localRotation = startRotation * Quaternion.Euler(0f, 0f, rotateDegrees * t);
            }
            if (rect != null)
            {
                rect.anchoredPosition = to;
                rect.localScale = toScale;
            }
        }

        private static async Task PlayArrivalAsync(Handle handle, RectTransform target)
        {
            if (handle.IsDisposed || target == null) return;
            int targetId = target.GetInstanceID();
            if (!ArrivalStates.TryGetValue(targetId, out ArrivalState state))
            {
                state = new ArrivalState { BaseScale = target.localScale };
                ArrivalStates[targetId] = state;
            }
            int epoch = ++state.Epoch;
            Vector3 baseScale = state.BaseScale;
            try
            {
                await ScaleAsync(handle, target, state, epoch,
                    baseScale, baseScale * 1.18f, 0.11f);
                await ScaleAsync(handle, target, state, epoch,
                    baseScale * 1.18f, baseScale, 0.16f);
            }
            finally
            {
                if (state.Epoch == epoch)
                {
                    if (target != null) target.localScale = baseScale;
                    ArrivalStates.Remove(targetId);
                }
            }
        }

        private static async Task ScaleAsync(Handle handle, RectTransform target,
            ArrivalState state, int epoch, Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                await Task.Yield();
                if (handle.IsDisposed || target == null || state.Epoch != epoch) return;
                elapsed += Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : 0.016f;
                float t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.LerpUnclamped(from, to, t * t * (3f - 2f * t));
            }
        }

        private static bool TryGetMoneyRoute(int goodsId, out MoneyRoute route)
        {
            switch (goodsId)
            {
                case 34:
                    route = new MoneyRoute(MainUIMoneyItem.TYPE_GOLD, "ui_bangyu_2", null);
                    return true;
                case 35:
                case 36020001:
                    route = new MoneyRoute(MainUIMoneyItem.TYPE_BGOLD, "ui_bangyu_1", null);
                    return true;
                case 31:
                    route = new MoneyRoute(MainUIMoneyItem.TYPE_COIN, null,
                        GameResPath.GetIcon("common", "com_gold"));
                    return true;
                default:
                    route = default;
                    return false;
            }
        }

        private static Vector2 ToLocalPoint(RectTransform root, RectTransform target)
        {
            Vector3 world = target.TransformPoint(target.rect.center);
            return root.InverseTransformPoint(world);
        }

        private static GameObject CreateRectObject(string name, RectTransform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.transform as RectTransform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.SetAsLastSibling();
            return go;
        }

        private static GameObject CreateImageObject(string name, RectTransform parent,
            Vector2 size, out Image image)
        {
            GameObject go = CreateRectObject(name, parent, size);
            image = go.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.color = Color.white;
            return go;
        }
    }
}
