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
    /// 空间位移原样播);同一角色各动作档案里烤的是各自落点,切换处均归一到原点,无缝。
    /// </summary>
    public static class ArtModelStager
    {
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
                    if (m == null || m.shader == null || !m.HasProperty("_AlphaClip")) continue;
                    if (m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f) continue; // 美术自设的透明,不动

                    bool blend = false; // 实例名带"(Instance)"后缀,用 StartsWith 匹配
                    foreach (string n in blendSet)
                    {
                        if (m.name.StartsWith(n)) { blend = true; break; }
                    }
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
                // 落点是美术场景坐标;换算成"相对本 prefab 根"的位移再缩放,把落点挪到 pivot 原点。
                // 根自身的缩放要保留相乘(美术可能用根缩放做整体包装),覆盖掉=体量爆炸
                Vector3 rootPos = sourcePrefab.transform.position;
                inst.transform.localScale =
                    sourcePrefab.transform.localScale * profile.landingScale;
                inst.transform.localPosition = -(profile.landingOffset - rootPos) * profile.landingScale;
            }
            else
            {
                GameLog.Warn("UI3D",
                    "新模型 {0} 档案里没有烤落点(旧版导入),按原样上台——资产管理[更新导入]重导一次即可",
                    inst.name);
            }
            return pivot;
        }
    }
}
