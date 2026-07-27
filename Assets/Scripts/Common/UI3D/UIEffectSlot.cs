using System.Threading.Tasks;
using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    /// <summary>UI effect slot backed by an effect/objs runtime prefab.</summary>
    public sealed class UIEffectSlot : UIDynamicResourceSlot
    {
        [SerializeField] private string _effectName = "";
        [SerializeField] private Vector2 _position;
        [SerializeField] private Vector3 _scale = Vector3.one;
        [SerializeField] private float _rotationY;
        [SerializeField, Tooltip("可选差异表 ID；留空时按 effectName 匹配，最终回退到 default。")]
        private string _profileId = "";
        // 配置化自动挂载:开启后 OnEnable 自己挂特效、OnDisable 自己收。已有专属业务代码手动消费本槽
        // (如 MainUITaskItem 完成光效、MainUIGuideManager 引导手指/选中框)的场景必须保持 false,防止双挂。
        [SerializeField] private bool _autoPlay;

        public string EffectName => _effectName;
        public Vector2 Position => _position;
        public Vector3 Scale => _scale == default ? Vector3.one : _scale;
        public float RotationY => _rotationY;
        public string ProfileId => _profileId;
        public bool AutoPlay => _autoPlay;

        // 自动挂载状态:参照 MainUIDownView.AddExpEffectAsync 的防重入/防脏写法(loading 标志 + 版本号自弃)。
        private UIEffectStage.Handle _autoHandle;
        private bool _autoLoading;
        private int _autoVersion;

        public void ConfigureEffect(string slotId, string effectName, string addressKey, string source, string note,
            Vector2 position, Vector3 scale, float rotationY, bool autoPlay = false, string profileId = "")
        {
            Configure(UIDynamicResourceKind.UIEffect, slotId, addressKey, source, note);
            _effectName = effectName ?? "";
            _position = position;
            _scale = scale == default ? Vector3.one : scale;
            _rotationY = rotationY;
            _autoPlay = autoPlay;
            _profileId = profileId ?? "";
        }

        private void OnEnable()
        {
            // 编辑态(含 Filler 在 LoadPrefabContents/UnloadPrefabContents 期间的激活/反激活)不跑,只在运行时自动挂。
            if (!Application.isPlaying || !_autoPlay) return;
            _autoVersion++;
            _ = AutoPlayAsync(_autoVersion);
        }

        private void OnDisable()
        {
            _autoVersion++;
            _autoLoading = false;
            DisposeAutoHandle();
        }

        private void OnDestroy()
        {
            DisposeAutoHandle();
        }

        private async Task AutoPlayAsync(int version)
        {
            if (_autoHandle != null || _autoLoading) return;
            _autoLoading = true;
            RectTransform parent = (transform.parent as RectTransform) ?? (transform as RectTransform);
            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(this, parent);
            _autoLoading = false;
            // 挂起等待期间可能已被 OnDisable/OnDestroy 打断(版本号不再匹配)或组件已销毁,此时自弃,不留悬挂 Handle。
            if (this == null || version != _autoVersion)
            {
                handle?.Dispose();
                return;
            }
            _autoHandle = handle;
        }

        private void DisposeAutoHandle()
        {
            if (_autoHandle == null) return;
            _autoHandle.Dispose();
            _autoHandle = null;
        }
    }
}
