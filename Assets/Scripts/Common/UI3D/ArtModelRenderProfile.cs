using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// 导入的美术成品模型(创角整模等)的渲染档案,由 ArtImport 导入工具写到 prefab 根上。
    ///
    /// 背景:成品模型的特效用 PandaShader,采样 SceneDepth(软粒子)与 SceneColor(扭曲)。
    /// Mobile_RPAsset 的 Depth/Opaque Texture 是关的,直接渲会黑掉/没效果;又不能为此改全局管线
    /// 设置动到老模型的渲染。所以走"新开一条渲染"的路:UIModelStage 上台时读本档案,只对
    /// 展示相机做三件事——切到独立 renderer、按相机粒度强制 Depth/Opaque Texture;
    /// 下台或换回不带档案的老模型时全部还原,老模型渲染路径零改动。
    /// </summary>
    public sealed class ArtModelRenderProfile : MonoBehaviour
    {
        [Tooltip("切到独立的 ArtFx renderer(由导入工具追加进 RP Asset 的 renderer list)")]
        public bool useDedicatedRenderer = true;

        [Tooltip("独立 renderer 在 RP Asset renderer list 中的下标,-1=不切(导入工具写入)")]
        public int rendererIndex = -1;

        [Tooltip("按相机粒度强制生成 Depth Texture(PandaShader 软粒子需要)")]
        public bool forceDepthTexture = true;

        [Tooltip("按相机粒度强制生成 Opaque Texture(PandaShader 扭曲/SceneColor 需要)")]
        public bool forceOpaqueTexture = true;

        // —— 落点(导入工具用 SampleAnimation 把 create3 待机采样到末帧、BakeMesh 精确量出并烤入;
        //    运行时把它平移到展示台占位点。静态包围盒是绑定姿势+披风/武器的松盒,不能用来猜,
        //    1213 曾被它骗成"已归零"导致巨腿怼镜头,实锤教训)——
        [Tooltip("导入时是否成功采样了落点(false=旧版导入,建议重新导入)")]
        public bool hasLanding;

        [Tooltip("create3 待机末帧的精确落点(美术场景坐标:脚底中心)")]
        public Vector3 landingOffset;

        [Tooltip("体量归一系数(采样身高→老角色身高 2.33)")]
        public float landingScale = 1f;

        [Tooltip("角色本体的标准附件空间倍率。身体体型保持不变，头饰/武器/翅膀/背饰在挂接时统一乘此值；非角色或未标准化角色为 1")]
        public float attachmentSpaceScale = 1f;

        [Tooltip("需要【半透渐变混合】的材质名(导入时分析贴图 alpha 直方图:有中间值=轻纱/雾状渐变," +
                 "运行时设 Transparent+ZWrite;其余身体材质走 Alpha Clipping 镂空)")]
        public string[] blendMaterials;

        /// <summary>把档案应用到指定相机；UI 台、场景台和编辑器预览共用，避免渲染口径分叉。</summary>
        public static void ApplyToCamera(Camera camera, ArtModelRenderProfile profile)
        {
            if (camera == null) return;
            UniversalAdditionalCameraData camData = camera.GetUniversalAdditionalCameraData();
            if (camData == null) return;
            camData.SetRenderer(profile != null && profile.useDedicatedRenderer && profile.rendererIndex >= 0
                ? profile.rendererIndex : -1);
            camData.requiresDepthOption = profile != null && profile.forceDepthTexture
                ? CameraOverrideOption.On : CameraOverrideOption.UsePipelineSettings;
            camData.requiresColorOption = profile != null && profile.forceOpaqueTexture
                ? CameraOverrideOption.On : CameraOverrideOption.UsePipelineSettings;
        }
    }
}
