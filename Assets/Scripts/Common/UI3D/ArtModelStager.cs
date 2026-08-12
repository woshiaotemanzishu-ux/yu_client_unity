using System.Collections.Generic;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.Playables;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// 新美术整模上台包装(原创角 RoleCreateView.StageWrap 原文收编为通用件,创角退役时曾随删;
    /// 血泪注释全保留)。核心约定(与美术):【动作停放点 = 落点 = 占位位置】。
    /// 落点**不在运行时猜**——导入工具用 SampleAnimation 把动作采样到末帧、BakeMesh 精确量出
    /// 脚底中心与身高,烤在 ArtModelRenderProfile 里(静态包围盒是绑定姿势的松盒,和动画停放点
    /// 不是一回事,1213 曾被它骗成"已归零"导致巨腿怼镜头,实锤教训)。
    /// 这里只读档案:整体平移把落点对到占位点+体量归一(平移是相对量,不锁动画、不碰 Director,
    /// 空间位移原样播);同一角色各动作档案里烤的是各自落点；带角色装配档案时体量统一采用
    /// canonical 动作，未标准化 legacy 资源才保留逐动作体量兼容。
    /// </summary>
    public static class ArtModelStager
    {
        private const string URP_UNLIT_SHADER = "Universal Render Pipeline/Unlit";

        /// <summary>
        /// 包装新模型实例:Timeline 循环模式 + 根位移 + 粒子缩放 + 透明分流 + 落点归一。
        /// 返回包着实例的 pivot(把它交给 UIModelStage/场景台摆放)。
        /// </summary>
        public static GameObject Stage(GameObject inst, GameObject sourcePrefab, DirectorWrapMode wrapMode)
        {
            PlayableDirector director = inst.GetComponentInChildren<PlayableDirector>(true);
            if (director != null) director.extrapolationMode = wrapMode;

            // 身体、头饰、武器、翅膀都可能是独立导入的美术 prefab，并各自带一份透明材质判定。
            // 根档案负责整模落点；部件档案只负责它自己的子树。不能用身体的 blendMaterials 扫整棵树，
            // 否则翅膀的渐变/Panda 特效会被误改成 AlphaTest，表现为白片、硬边和错误遮挡。
            ArtModelRenderProfile profile = inst.GetComponent<ArtModelRenderProfile>();
            if (profile == null)
            {
                profile = inst.AddComponent<ArtModelRenderProfile>();
                profile.useDedicatedRenderer = false; // 运行时补的档案不知道独立 renderer 下标:默认 renderer+强制贴图即可
            }
            foreach (ParticleSystem ps in inst.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            // 只有整模主体的 Generic 动画需要 Root Motion：Timeline 编辑器预览无视该开关，主体不开会
            // 原地做出场动作。独立部件必须保留 prefab 自己的设置；1005 翅膀有三个 Animator 且美术
            // 明确关闭 Root Motion。这里按最近的渲染档案分界，避免公共上台逻辑改写部件的运动设置。
            foreach (Animator animator in inst.GetComponentsInChildren<Animator>(true))
            {
                ArtModelRenderProfile owner = animator.GetComponentInParent<ArtModelRenderProfile>();
                if (owner == profile)
                    animator.applyRootMotion = true;
            }
            DisableSurfaceLighting(inst);
            // 透明处理:美术把透明信息画在贴图 alpha 里,但 FBX 内嵌材质默认 Opaque 不读 alpha
            // → 该透的地方渲成白块。按导入时烤好的判定分流(只改实例不动资产):
            //   缺口/破洞(alpha 二值)→ Alpha Clipping(硬边,无排序问题);
            //   轻纱/雾状渐变(alpha 有中间值,档案 blendMaterials 点名)→ Transparent 混合 +
            //   保留 ZWrite(自遮挡不乱)+ 低阈值裁剪(全透像素不写深度残影)。
            // 美术在 prefab 里已设 Transparent 的材质一律不碰。
            var blendSets = new Dictionary<ArtModelRenderProfile, HashSet<string>>();
            foreach (Renderer r in inst.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                ArtModelRenderProfile owner = r.GetComponentInParent<ArtModelRenderProfile>();
                if (owner == null) owner = profile;
                if (!blendSets.TryGetValue(owner, out HashSet<string> blendSet))
                {
                    blendSet = new HashSet<string>(
                        owner.blendMaterials ?? System.Array.Empty<string>());
                    blendSets.Add(owner, blendSet);
                }
                Material[] mats = r.materials;
                bool dirty = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    if (m == null || m.shader == null) continue;

                    // 美术已经声明为 Transparent 的 Shader/材质自带完整混合语义（Panda 的加法、
                    // Alpha、双通道 Blend 都在其中）。运行时再次按贴图 alpha 猜混合方式会破坏原表现。
                    // 这里只转换导入后仍为 Opaque 的 URP 材质，保证直接预览与 RT 上台使用同一份参数。
                    string renderType = m.GetTag("RenderType", false, string.Empty);
                    if (string.Equals(renderType, "Transparent", System.StringComparison.OrdinalIgnoreCase) ||
                        (m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f))
                        continue;

                    bool blend = false; // 实例名带"(Instance)"后缀,用 StartsWith 匹配
                    foreach (string n in blendSet)
                    {
                        if (m.name.StartsWith(n)) { blend = true; break; }
                    }

                    if (!m.HasProperty("_AlphaClip")) continue;
                    if (blend)
                    {
                        m.SetFloat("_Surface", 1f);
                        m.SetOverrideTag("RenderType", "Transparent");
                        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        m.SetFloat("_ZWrite", 1f);
                        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        m.SetFloat("_AlphaClip", 1f);
                        m.SetFloat("_Cutoff", 0.02f);
                        m.EnableKeyword("_ALPHATEST_ON");
                        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    }
                    else
                    {
                        m.SetFloat("_AlphaClip", 1f);
                        m.SetFloat("_Cutoff", 0.5f);
                        m.EnableKeyword("_ALPHATEST_ON");
                        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    }
                    dirty = true;
                }
                if (dirty) r.materials = mats;
            }

            var pivot = new GameObject(inst.name + "_pivot");
            inst.transform.SetParent(pivot.transform, false);
            if (profile.hasLanding)
            {
                // 落点是美术场景坐标；换算成“相对本 prefab 根”的位移，把落点挪到 pivot 原点。
                // 体量以源 Prefab 为准，不再读取历史 landingScale 做二次归一。
                Vector3 rootPos = sourcePrefab.transform.position;
                inst.transform.localScale = sourcePrefab.transform.localScale;
                inst.transform.localPosition = -(profile.landingOffset - rootPos);
            }
            else
            {
                GameLog.Warn("UI3D",
                    "新模型 {0} 档案里没有烤落点(旧版导入),按原样上台——资产管理[更新导入]重导一次即可",
                    inst.name);
            }
            return pivot;
        }

        /// <summary>关闭模型内置灯，并把常规 Lit 表面改为 URP Unlit；保留贴图、颜色、剪裁和混合参数。</summary>
        public static void DisableSurfaceLighting(GameObject root)
        {
            if (root == null) return;
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                if (light != null) light.enabled = false;
            }

            Shader unlit = Shader.Find(URP_UNLIT_SHADER);
            if (unlit == null)
            {
                GameLog.Error("UI3D", "找不到 {0},无法关闭模型表面光照", URP_UNLIT_SHADER);
                return;
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer) continue;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                bool needsConversion = false;
                foreach (Material shared in renderer.sharedMaterials)
                {
                    if (shared != null && shared.shader != null && UsesSurfaceLighting(shared.shader.name))
                    {
                        needsConversion = true;
                        break;
                    }
                }
                if (!needsConversion) continue;

                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null || source.shader == null || !UsesSurfaceLighting(source.shader.name))
                        continue;

                    Material material = new Material(source);
                    Texture baseMap = material.HasProperty("_BaseMap")
                        ? material.GetTexture("_BaseMap")
                        : material.mainTexture;
                    Vector2 textureScale = material.mainTextureScale;
                    Vector2 textureOffset = material.mainTextureOffset;
                    Color baseColor = material.HasProperty("_BaseColor")
                        ? material.GetColor("_BaseColor")
                        : material.color;
                    string renderType = material.GetTag("RenderType", false, string.Empty);
                    int renderQueue = material.renderQueue;
                    float standardMode = GetFloat(material, "_Mode");
                    float surface = material.HasProperty("_Surface")
                        ? material.GetFloat("_Surface")
                        : (standardMode >= 2f || string.Equals(renderType, "Transparent", System.StringComparison.OrdinalIgnoreCase) ? 1f : 0f);
                    bool hasBlend = material.HasProperty("_Blend");
                    float blend = GetFloat(material, "_Blend");
                    float alphaClip = material.HasProperty("_AlphaClip")
                        ? material.GetFloat("_AlphaClip")
                        : (standardMode == 1f || material.IsKeywordEnabled("_ALPHATEST_ON") ? 1f : 0f);
                    bool hasCutoff = material.HasProperty("_Cutoff");
                    float cutoff = GetFloat(material, "_Cutoff");
                    bool hasCull = material.HasProperty("_Cull");
                    float cull = GetFloat(material, "_Cull");
                    bool hasSrcBlend = material.HasProperty("_SrcBlend");
                    float srcBlend = GetFloat(material, "_SrcBlend");
                    bool hasDstBlend = material.HasProperty("_DstBlend");
                    float dstBlend = GetFloat(material, "_DstBlend");
                    bool hasSrcBlendAlpha = material.HasProperty("_SrcBlendAlpha");
                    float srcBlendAlpha = GetFloat(material, "_SrcBlendAlpha");
                    bool hasDstBlendAlpha = material.HasProperty("_DstBlendAlpha");
                    float dstBlendAlpha = GetFloat(material, "_DstBlendAlpha");
                    bool hasZWrite = material.HasProperty("_ZWrite");
                    float zWrite = GetFloat(material, "_ZWrite");
                    bool hasAlphaToMask = material.HasProperty("_AlphaToMask");
                    float alphaToMask = GetFloat(material, "_AlphaToMask");

                    material.shader = unlit;
                    if (baseMap != null && material.HasProperty("_BaseMap"))
                    {
                        material.SetTexture("_BaseMap", baseMap);
                        material.SetTextureScale("_BaseMap", textureScale);
                        material.SetTextureOffset("_BaseMap", textureOffset);
                    }
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
                    SetFloat(material, "_Surface", surface);
                    if (hasBlend) SetFloat(material, "_Blend", blend);
                    SetFloat(material, "_AlphaClip", alphaClip);
                    if (hasCutoff) SetFloat(material, "_Cutoff", cutoff);
                    if (hasCull) SetFloat(material, "_Cull", cull);
                    if (hasSrcBlend) SetFloat(material, "_SrcBlend", srcBlend);
                    if (hasDstBlend) SetFloat(material, "_DstBlend", dstBlend);
                    if (hasSrcBlendAlpha) SetFloat(material, "_SrcBlendAlpha", srcBlendAlpha);
                    if (hasDstBlendAlpha) SetFloat(material, "_DstBlendAlpha", dstBlendAlpha);
                    if (hasZWrite) SetFloat(material, "_ZWrite", zWrite);
                    if (hasAlphaToMask) SetFloat(material, "_AlphaToMask", alphaToMask);
                    material.SetOverrideTag("RenderType", renderType);
                    material.renderQueue = renderQueue;
                    if (surface > 0.5f) material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    else material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    if (alphaClip > 0.5f) material.EnableKeyword("_ALPHATEST_ON");
                    else material.DisableKeyword("_ALPHATEST_ON");
                    materials[i] = material;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = materials;
            }
        }

        private static bool UsesSurfaceLighting(string shaderName)
        {
            return string.Equals(shaderName, "Standard", System.StringComparison.Ordinal) ||
                   string.Equals(shaderName, "Standard (Specular setup)", System.StringComparison.Ordinal) ||
                   string.Equals(shaderName, "Universal Render Pipeline/Lit", System.StringComparison.Ordinal) ||
                   string.Equals(shaderName, "Universal Render Pipeline/Simple Lit", System.StringComparison.Ordinal) ||
                   string.Equals(shaderName, "Universal Render Pipeline/Complex Lit", System.StringComparison.Ordinal) ||
                   string.Equals(shaderName, "Universal Render Pipeline/Baked Lit", System.StringComparison.Ordinal);
        }

        private static float GetFloat(Material material, string property)
        {
            return material.HasProperty(property) ? material.GetFloat(property) : 0f;
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property)) material.SetFloat(property, value);
        }
    }
}
