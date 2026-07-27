using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    public enum UIEffectChannelOverride
    {
        Auto = 0,
        Scene = 1,
        Main = 2,
        Window = 3,
        Popup = 4,
        Tip = 5,
        Loading = 6,
        Top = 7
    }

    public enum UIEffectBand
    {
        Underlay = 0,
        Overlay = 1
    }

    [Serializable]
    public sealed class UIEffectProfile
    {
        [Tooltip("规则名；UIEffectSlot 可通过 profileId 显式选择。default 是公共默认项。")]
        public string id = "default";

        [Tooltip("可选的资源名匹配。为空时只通过 profileId 使用。")]
        public string effectName = "";

        [Tooltip("Auto 表示跟随宿主所属 UILayer。")]
        public UIEffectChannelOverride channel = UIEffectChannelOverride.Auto;

        [Tooltip("同一 UILayer 内的固定渲染带。大多数粒子使用 Overlay。")]
        public UIEffectBand band = UIEffectBand.Overlay;

        [Tooltip("在调用参数之外追加的旧端坐标偏移。")]
        public Vector2 positionOffset;

        [Tooltip("在调用参数之外追加的资源缩放差异。")]
        public Vector3 scaleMultiplier = Vector3.one;

        [Tooltip("在调用参数之外追加的 Y 轴旋转。")]
        public float rotationYOffset;

        [Tooltip("仅翻转本资源，不改变共享通道的老端全局镜像规则。")]
        public bool mirrorX;

        internal Vector3 SafeScaleMultiplier => scaleMultiplier == default ? Vector3.one : scaleMultiplier;
    }

    /// <summary>
    /// UI 特效公共参数和资源差异表。运行时从 Assets/Resources/UIEffectProfileCatalog.asset 加载；
    /// 缺少资产时使用同样的内存默认值，不能阻断 UI。
    /// </summary>
    [CreateAssetMenu(fileName = "UIEffectProfileCatalog", menuName = "Shenxiao/UI Effect Profile Catalog", order = 41)]
    public sealed class UIEffectProfileCatalog : ScriptableObject
    {
        public const string RESOURCE_PATH = "UIEffectProfileCatalog";

        [Header("Shared render channels")]
        [Range(0.25f, 1f)]
        [Tooltip("共享 RenderTexture 相对 UI 逻辑尺寸的倍率。1 为像素等宽。")]
        public float renderScale = 1f;

        [Min(16)] public int minRenderTextureSize = 16;
        [Min(64)] public int maxRenderTextureSize = 2048;

        [Min(0f)]
        [Tooltip("通道没有实例后保留 RT 的秒数。Camera 会立即停止。")]
        public float idleReleaseSeconds = 10f;

        [Header("Profiles")]
        public List<UIEffectProfile> profiles = new List<UIEffectProfile>
        {
            new UIEffectProfile()
        };

        private static UIEffectProfileCatalog s_runtime;
        private static UIEffectProfile s_fallbackProfile;

        public static UIEffectProfileCatalog Runtime
        {
            get
            {
                if (s_runtime != null) return s_runtime;
                s_runtime = Resources.Load<UIEffectProfileCatalog>(RESOURCE_PATH);
                if (s_runtime == null)
                {
                    s_runtime = CreateInstance<UIEffectProfileCatalog>();
                    s_runtime.hideFlags = HideFlags.HideAndDontSave;
                }
                s_runtime.Normalize();
                return s_runtime;
            }
        }

        public UIEffectProfile Resolve(string effectName, string profileId = null)
        {
            Normalize();

            if (!string.IsNullOrWhiteSpace(profileId))
            {
                for (int i = 0; i < profiles.Count; i++)
                {
                    UIEffectProfile profile = profiles[i];
                    if (profile != null && string.Equals(profile.id, profileId, StringComparison.OrdinalIgnoreCase))
                        return profile;
                }
            }

            if (!string.IsNullOrWhiteSpace(effectName))
            {
                for (int i = 0; i < profiles.Count; i++)
                {
                    UIEffectProfile profile = profiles[i];
                    if (profile != null && !string.IsNullOrWhiteSpace(profile.effectName) &&
                        string.Equals(profile.effectName, effectName, StringComparison.OrdinalIgnoreCase))
                        return profile;
                }
            }

            for (int i = 0; i < profiles.Count; i++)
            {
                UIEffectProfile profile = profiles[i];
                if (profile != null && string.Equals(profile.id, "default", StringComparison.OrdinalIgnoreCase))
                    return profile;
            }

            return GetFallbackProfile();
        }

        private void Normalize()
        {
            renderScale = Mathf.Clamp(renderScale, 0.25f, 1f);
            minRenderTextureSize = Mathf.Max(16, minRenderTextureSize);
            maxRenderTextureSize = Mathf.Max(minRenderTextureSize, maxRenderTextureSize);
            idleReleaseSeconds = Mathf.Max(0f, idleReleaseSeconds);
            if (profiles == null) profiles = new List<UIEffectProfile>();
            if (profiles.Count == 0) profiles.Add(new UIEffectProfile());
        }

        private static UIEffectProfile GetFallbackProfile()
        {
            return s_fallbackProfile ??= new UIEffectProfile();
        }

#if UNITY_EDITOR
        internal static void ClearRuntimeCache()
        {
            s_runtime = null;
        }
#endif
    }
}
