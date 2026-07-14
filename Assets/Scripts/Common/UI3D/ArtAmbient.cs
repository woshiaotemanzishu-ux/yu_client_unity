using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// 新美术模型环境光(引用计数):Lit 材质在无灯场景整体偏黑,有新模型在台上/场景里时把环境光
    /// 切成亮平光,全部下台自动恢复。老模型/场景全是 unlit shader 不吃光照,切环境光对它们零影响
    /// ——真正吃光的只有新模型(用户定案:场景内也用这套光,只有新模型吃)。
    /// (原 UIModelStage.SetArtAmbient 收编:UI 台 + 场景台多方共用,布尔开关会互相踩,改引用计数。)
    /// </summary>
    public static class ArtAmbient
    {
        private static readonly Color ART_AMBIENT = Color.white;
        private static int _count;
        private static UnityEngine.Rendering.AmbientMode _savedMode;
        private static Color _savedColor;

        public static void Retain()
        {
            if (_count++ != 0) return;
            _savedMode = RenderSettings.ambientMode;
            _savedColor = RenderSettings.ambientLight;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ART_AMBIENT;
        }

        public static void Release()
        {
            if (_count <= 0) return;
            if (--_count != 0) return;
            RenderSettings.ambientMode = _savedMode;
            RenderSettings.ambientLight = _savedColor;
        }
    }
}
