using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.Laya3D
{
    /// <summary>
    /// Laya 特效 .lh → Unity Prefab(ParticleSystem/TrailRenderer/静态网格)。
    /// 规格书 = cdn/libs/laya.d3.js 的 ShuriKenParticle3D._parse / _parseModule /
    /// TrailSprite3D._parse / Material._parse(LAYAMATERIAL:01/02)——逐字段对标引擎解析,
    /// 模块数据布局:bases(标量)/vector2s/vector3s/vector4s(命名数组)/resources/bursts/
    /// gradientDataNumbers;渐变 {key,value} 点列;角度量一律弧度(Unity shape 角度需转度)。
    /// 产物:Assets/GameRes/effect/objs/{类型目录}/{名字}/{名字}.prefab;
    /// 贴图按 cdn 相对路径镜像进 GameRes(多特效共享)。
    /// </summary>
    public static class LayaEffectImporter
    {
        /// <summary>特效转换逻辑版本(独立于模型线 Laya3DImporter.TOOL_VERSION)。</summary>
        public const int TOOL_VERSION = 14; // v14: match Laya3D Shuriken shader color semantics.

        public sealed class Result
        {
            public bool Ok;
            public string PrefabPath = "";
            public readonly StringBuilder Log = new StringBuilder();
        }

        public static Result Convert(string lhPath)
        {
            var r = new Result();
            try
            {
                ConvertInner(lhPath, r);
                r.Ok = true;
            }
            catch (Exception e)
            {
                r.Log.AppendLine("❌ 失败: " + e.Message);
                r.Log.AppendLine(e.StackTrace);
                r.Ok = false;
            }
            Debug.Log("[LayaEffect] 转换日志\n" + r.Log);
            return r;
        }

        /// <summary>只读速览:解析 .lh 统计粒子/网格/拖尾节点数(不产资产),给 UI 转换前确认用。</summary>
        public static (int particles, int meshes, int trails, string error) Inspect(string lhPath)
        {
            try
            {
                JObject doc = JObject.Parse(File.ReadAllText(lhPath));
                JObject root = doc["data"] as JObject ?? doc;
                int p = 0, m = 0, t = 0;
                CountNodes(root, ref p, ref m, ref t);
                return (p, m, t, null);
            }
            catch (Exception e) { return (0, 0, 0, e.Message); }
        }

        private static void CountNodes(JObject node, ref int p, ref int m, ref int t)
        {
            switch ((string)node["type"])
            {
                case "ShuriKenParticle3D": p++; break;
                case "MeshSprite3D": m++; break;
                case "TrailSprite3D": t++; break;
            }
            if (node["child"] is JArray children)
                foreach (JToken c in children)
                    if (c is JObject child) CountNodes(child, ref p, ref m, ref t);
        }

        private static void ConvertInner(string lhPath, Result r)
        {
            string name = Path.GetFileNameWithoutExtension(lhPath);
            string dirName = Path.GetFileName(Path.GetDirectoryName(lhPath)); // skills_effect 等
            string outDir = $"Assets/GameRes/effect/objs/{dirName}/{name}";
            Directory.CreateDirectory(outDir);

            JObject doc = JObject.Parse(File.ReadAllText(lhPath));
            JObject rootNode = doc["data"] as JObject ?? doc; // 兼容 {data:{...}} 包装
            r.Log.AppendLine($"① .lh 解析: {name}(目录 {dirName})");

            var ctx = new Context
            {
                LhDir = Path.GetDirectoryName(lhPath),
                OutDir = outDir,
                Report = r,
            };

            GameObject rootGo = BuildNode(rootNode, null, ctx);
            if (rootGo == null) throw new Exception(".lh 根节点构建失败");
            rootGo.name = name;

            string prefabPath = outDir + "/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(rootGo, prefabPath);
            UnityEngine.Object.DestroyImmediate(rootGo);

            var meta = new JObject { ["tool"] = TOOL_VERSION, ["kind"] = "effect" };
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "..", outDir, name + ".import.json")),
                meta.ToString());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            r.PrefabPath = prefabPath;
            r.Log.AppendLine($"✅ 完成: {prefabPath}(粒子 {ctx.ParticleCount} / 网格 {ctx.MeshCount} / 拖尾 {ctx.TrailCount})");
        }

        private sealed class Context
        {
            public string LhDir;
            public string OutDir;
            public Result Report;
            public int ParticleCount, MeshCount, TrailCount;
            public readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();
            public readonly Dictionary<string, Mesh> MeshCache = new Dictionary<string, Mesh>();
        }

        // ================= 节点树 =================

        private static GameObject BuildNode(JObject node, Transform parent, Context ctx)
        {
            string type = (string)node["type"] ?? "Sprite3D";
            JObject props = node["props"] as JObject ?? new JObject();

            var go = new GameObject((string)props["name"] ?? type);
            if (parent != null) go.transform.SetParent(parent, false);
            ApplyTransform(go.transform, props);
            if (props["active"] != null && !props.Value<bool>("active")) go.SetActive(false);

            switch (type)
            {
                case "ShuriKenParticle3D":
                    BuildParticle(go, props, ctx);
                    ctx.ParticleCount++;
                    break;
                case "MeshSprite3D":
                    BuildMeshNode(go, props, ctx);
                    ctx.MeshCount++;
                    break;
                case "TrailSprite3D":
                    BuildTrail(go, props, ctx);
                    ctx.TrailCount++;
                    break;
                case "Sprite3D":
                case "Scene3D":
                    break; // 纯容器
                case "Camera":
                case "DirectionLight":
                case "PointLight":
                case "SpotLight":
                    ctx.Report.Log.AppendLine($"   跳过 {type} 节点(特效不带相机/灯)");
                    break;
                default:
                    ctx.Report.Log.AppendLine($"   ⚠ 未支持的节点类型 {type}(按空容器处理,如有显示缺失请回报)");
                    break;
            }

            if (node["child"] is JArray children)
            {
                foreach (JToken c in children)
                {
                    if (c is JObject child) BuildNode(child, go.transform, ctx);
                }
            }

            // Laya Animator 组件:子节点建完后接上(轨路径可指向子节点)。
            // 注意:components 挂在节点根(node)上,不在 props 里。
            ApplyAnimator(go, node, ctx);
            return go;
        }

        /// <summary>
        /// 节点上的 Laya Animator 组件 → 传统 Animation + .lani 转出的 AnimationClip 并自动播放。
        /// 缺这步:靠动画驱动的特效子节点(刀光挥砍等)会定格在作者预设姿态 —— 看着像"只显示最后一帧、
        /// 不按动作播放、也不消失",而纯粒子节点能自播(所以"部分特效正常")。
        /// .lani 轨路径相对该 Animator 节点(创角特效里多为空 = 动画器自身 transform)。
        /// </summary>
        private static void ApplyAnimator(GameObject go, JObject node, Context ctx)
        {
            if (!(node["components"] is JArray comps)) return;
            var clips = new List<AnimationClip>();
            foreach (JToken comp in comps)
            {
                if ((string)comp["type"] != "Animator" || !(comp["layers"] is JArray layers)) continue;
                foreach (JToken layer in layers)
                {
                    if (!(layer["states"] is JArray states)) continue;
                    foreach (JToken st in states)
                    {
                        string clipPath = (string)st["clipPath"];
                        if (string.IsNullOrEmpty(clipPath)) continue;
                        string laniAbs = Path.GetFullPath(Path.Combine(ctx.LhDir, clipPath));
                        if (!File.Exists(laniAbs))
                        {
                            ctx.Report.Log.AppendLine("   ⚠ .lani 不存在 " + clipPath);
                            continue;
                        }
                        string clipName = (string)st["name"] ?? Path.GetFileNameWithoutExtension(laniAbs);
                        AnimationClip clip = BuildEffectClip(laniAbs, clipName, ctx);
                        if (clip != null) clips.Add(clip);
                    }
                }
            }
            if (clips.Count == 0) return;
            var anim = go.AddComponent<Animation>();
            foreach (AnimationClip c in clips) anim.AddClip(c, c.name);
            anim.clip = clips[0];
            anim.playAutomatically = true;
            // 离屏 RT 舞台:别因渲染器在主相机视锥外而暂停动画(否则又定格)
            anim.cullingType = AnimationCullingType.AlwaysAnimate;
        }

        /// <summary>.lani → 传统 AnimationClip。轨:localPosition/localScale(3)、localRotation(四元数)、
        /// localRotationEuler(欧拉,度;烘成四元数轨以兼容传统动画)。</summary>
        private static AnimationClip BuildEffectClip(string laniAbs, string clipName, Context ctx)
        {
            LaniClip lani;
            try { lani = LaniParser.Parse(File.ReadAllBytes(laniAbs)); }
            catch (Exception e) { ctx.Report.Log.AppendLine($"   ⚠ .lani 解析失败 {clipName}: {e.Message}"); return null; }

            var clip = new AnimationClip
            {
                name = clipName,
                legacy = true,
                frameRate = lani.FrameRate > 0 ? lani.FrameRate : 30,
                wrapMode = lani.IsLooping ? WrapMode.Loop : WrapMode.Once,
            };
            foreach (LaniNode node in lani.Nodes)
            {
                string path = node.Path;
                string owner = node.PropertyOwner;
                string prop = node.PropertyName;
                if (owner == "transform" && node.Type == 2 && prop == "localRotation")
                    SetAxisCurves(clip, path, "localRotation", node, 4);
                else if (owner == "transform" && prop == "localPosition")
                    SetAxisCurves(clip, path, "localPosition", node, 3);
                else if (owner == "transform" && prop == "localScale")
                    SetAxisCurves(clip, path, "localScale", node, 3);
                else if (owner == "transform" && prop == "localRotationEuler")
                    SetEulerAsQuaternion(clip, path, node);
                else if (owner == "meshRenderer" && prop == "material")
                    SetMaterialCurve(clip, path, node, ctx);   // _TintColor 淡入淡出(决定特效消不消失)、_MainTex_ST UV 滚动
                else
                    ctx.Report.Log.AppendLine($"   ⚠ 跳过动画轨 {owner}.{string.Join(".", node.Properties)}");
            }
            clip.EnsureQuaternionContinuity();

            string clipAsset = ctx.OutDir + "/" + clipName + ".anim";
            AssetDatabase.CreateAsset(clip, clipAsset);
            ctx.Report.Log.AppendLine($"   动画 {clipName}: {lani.Duration:0.##}s 轨{lani.Nodes.Count} loop={lani.IsLooping}");
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(clipAsset);
        }

        private static readonly string[] AXIS = { "x", "y", "z", "w" };

        private static void SetAxisCurves(AnimationClip clip, string path, string property, LaniNode node, int components)
        {
            if (node.Keyframes.Count == 0) return;
            int comp = Mathf.Min(components, node.Keyframes[0].Value.Length);
            for (int c = 0; c < comp; c++)
            {
                var keys = new Keyframe[node.Keyframes.Count];
                for (int k = 0; k < keys.Length; k++)
                {
                    LaniKeyframe kf = node.Keyframes[k];
                    keys[k] = new Keyframe(kf.Time, kf.Value[c], kf.InTangent[c], kf.OutTangent[c]);
                }
                clip.SetCurve(path, typeof(Transform), property + "." + AXIS[c], new AnimationCurve(keys));
            }
        }

        /// <summary>材质属性轨(单分量,type 0):_TintColor→_BaseColor(x/y/z/w→r/g/b/a)、
        /// _MainTex_ST→_BaseMap_ST(同布局)。绑到 MeshRenderer 的 material.*,运行时自动实例化材质。</summary>
        private static void SetMaterialCurve(AnimationClip clip, string path, LaniNode node, Context ctx)
        {
            if (node.Properties.Length < 3 || node.Keyframes.Count == 0) return;
            string layaProp = node.Properties[1];
            string comp = node.Properties[2];
            string unityProp, unityComp;
            if (IsLayaColorComponent(layaProp))
            {
                unityProp = "_BaseColor";
                unityComp = comp == "x" ? "r" : comp == "y" ? "g" : comp == "z" ? "b" : "a";
            }
            else if (layaProp == "_MainTex_ST" || layaProp == "tilingOffset")
            {
                unityProp = "_BaseMap_ST";
                unityComp = comp; // x/y/z/w 同布局(tiling.xy / offset.zw)
            }
            else { ctx.Report.Log.AppendLine($"   ⚠ 跳过材质轨 {layaProp}.{comp}"); return; }

            float multiplier = IsLayaColorComponent(layaProp) && comp != "w" && comp != "a" ? 2f : 1f;
            var keys = new Keyframe[node.Keyframes.Count];
            for (int k = 0; k < keys.Length; k++)
            {
                LaniKeyframe kf = node.Keyframes[k];
                keys[k] = new Keyframe(kf.Time, kf.Value[0] * multiplier,
                    kf.InTangent[0] * multiplier, kf.OutTangent[0] * multiplier);
            }
            clip.SetCurve(path, typeof(MeshRenderer), "material." + unityProp + "." + unityComp, new AnimationCurve(keys));
        }

        /// <summary>欧拉角轨(Laya 度)烘成四元数 localRotation 轨(传统动画不认 localEulerAngles)。
        /// 关键帧少、按时间线性插值即可;切线交给 EnsureQuaternionContinuity 平滑。</summary>
        private static void SetEulerAsQuaternion(AnimationClip clip, string path, LaniNode node)
        {
            int n = node.Keyframes.Count;
            if (n == 0) return;
            var kx = new Keyframe[n]; var ky = new Keyframe[n]; var kz = new Keyframe[n]; var kw = new Keyframe[n];
            for (int k = 0; k < n; k++)
            {
                LaniKeyframe kf = node.Keyframes[k];
                Quaternion q = Quaternion.Euler(kf.Value[0], kf.Value[1], kf.Value[2]);
                kx[k] = new Keyframe(kf.Time, q.x); ky[k] = new Keyframe(kf.Time, q.y);
                kz[k] = new Keyframe(kf.Time, q.z); kw[k] = new Keyframe(kf.Time, q.w);
            }
            clip.SetCurve(path, typeof(Transform), "localRotation.x", new AnimationCurve(kx));
            clip.SetCurve(path, typeof(Transform), "localRotation.y", new AnimationCurve(ky));
            clip.SetCurve(path, typeof(Transform), "localRotation.z", new AnimationCurve(kz));
            clip.SetCurve(path, typeof(Transform), "localRotation.w", new AnimationCurve(kw));
        }

        /// <summary>Sprite3D._parse:position/rotationEuler/rotation/scale(数组)。</summary>
        private static void ApplyTransform(Transform t, JObject props)
        {
            if (props["position"] is JArray p && p.Count >= 3)
                t.localPosition = new Vector3((float)p[0], (float)p[1], (float)p[2]);
            if (props["rotationEuler"] is JArray re && re.Count >= 3)
                t.localRotation = Quaternion.Euler((float)re[0], (float)re[1], (float)re[2]);
            else if (props["rotation"] is JArray rq && rq.Count >= 4)
                t.localRotation = new Quaternion((float)rq[0], (float)rq[1], (float)rq[2], (float)rq[3]);
            if (props["scale"] is JArray sc && sc.Count >= 3)
                t.localScale = new Vector3((float)sc[0], (float)sc[1], (float)sc[2]);
        }

        // ================= 粒子 =================

        private static void BuildParticle(GameObject go, JObject props, Context ctx)
        {
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            var emission = ps.emission;
            emission.enabled = true;

            JObject mainData = props["main"] as JObject;
            JObject bases = mainData?["bases"] as JObject ?? new JObject();
            JObject vec3s = mainData?["vector3s"] as JObject;
            JObject vec4s = mainData?["vector4s"] as JObject;

            // ---- main(对标 ShurikenParticleSystem 属性名) ----
            main.duration = F(bases, "duration", 5f);
            main.loop = B(bases, "looping", true);
            main.prewarm = B(bases, "prewarm", false);
            main.maxParticles = (int)F(bases, "maxParticles", 1000f);
            main.simulationSpace = (int)F(bases, "simulationSpace", 0f) == 1
                ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            // scaleMode 枚举两边同序:0 Hierarchy / 1 Local / 2 Shape(Laya World 近似 Shape)
            main.scalingMode = (ParticleSystemScalingMode)Mathf.Clamp((int)F(bases, "scaleMode", 1f), 0, 2);
            main.playOnAwake = B(bases, "playOnAwake", true);
            main.gravityModifier = F(bases, "gravityModifier", 0f);
            // 特效只在离屏 RT 舞台(远离原点)里渲染,主相机视锥外。Automatic 剔除会暂停模拟
            // → 创角特效卡住/不动/不出现。强制 AlwaysSimulate,保证一直推进。
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            main.startDelay = (int)F(bases, "startDelayType", 0f) == 1
                ? new ParticleSystem.MinMaxCurve(F(bases, "startDelayMin", 0f), F(bases, "startDelayMax", 0f))
                : new ParticleSystem.MinMaxCurve(F(bases, "startDelay", 0f));

            main.startLifetime = NumberMinMax(bases, mainData,
                (int)F(bases, "startLifetimeType", 0f),
                "startLifetimeConstant", "startLifetimeConstantMin", "startLifetimeConstantMax",
                "startLifeTimeGradient", "startLifeTimeGradientMin", "startLifeTimeGradientMax", 5f, ctx);

            main.startSpeed = NumberMinMax(bases, mainData,
                (int)F(bases, "startSpeedType", 0f),
                "startSpeedConstant", "startSpeedConstantMin", "startSpeedConstantMax",
                null, null, null, 5f, ctx);

            // 尺寸(支持 3D 分轴)
            bool threeDSize = B(bases, "threeDStartSize", false);
            if (threeDSize && vec3s?["startSizeConstantSeparate"] is JArray sizeSep)
            {
                main.startSize3D = true;
                main.startSizeX = new ParticleSystem.MinMaxCurve((float)sizeSep[0]);
                main.startSizeY = new ParticleSystem.MinMaxCurve((float)sizeSep[1]);
                main.startSizeZ = new ParticleSystem.MinMaxCurve((float)sizeSep[2]);
            }
            else
            {
                main.startSize = NumberMinMax(bases, mainData,
                    (int)F(bases, "startSizeType", 0f),
                    "startSizeConstant", "startSizeConstantMin", "startSizeConstantMax",
                    null, null, null, 1f, ctx);
            }

            // 旋转(Laya 弧度;Unity MinMaxCurve 同为弧度)
            bool threeDRot = B(bases, "threeDStartRotation", false);
            if (threeDRot && vec3s?["startRotationConstantSeparate"] is JArray rotSep)
            {
                main.startRotation3D = true;
                main.startRotationX = new ParticleSystem.MinMaxCurve((float)rotSep[0]);
                main.startRotationY = new ParticleSystem.MinMaxCurve((float)rotSep[1]);
                main.startRotationZ = new ParticleSystem.MinMaxCurve((float)rotSep[2]);
            }
            else
            {
                int rotType = (int)F(bases, "startRotationType", 0f);
                main.startRotation = rotType == 2
                    ? new ParticleSystem.MinMaxCurve(F(bases, "startRotationConstantMin", 0f), F(bases, "startRotationConstantMax", 0f))
                    : new ParticleSystem.MinMaxCurve(F(bases, "startRotationConstant", 0f));
            }
            float flip = F(bases, "randomizeRotationDirection", 0f);
            if (flip > 0f) main.flipRotation = flip;

            // 起始颜色
            int colorType = (int)F(bases, "startColorType", 0f);
            if (colorType == 2 && vec4s?["startColorConstantMin"] is JArray cMin && vec4s["startColorConstantMax"] is JArray cMax)
                main.startColor = new ParticleSystem.MinMaxGradient(C4(cMin), C4(cMax));
            else if (colorType == 0 && vec4s?["startColorConstant"] is JArray c0)
                main.startColor = new ParticleSystem.MinMaxGradient(C4(c0));
            else if (colorType != 0)
                main.startColor = new ParticleSystem.MinMaxGradient(Color.white);

            if (bases["randomSeed"] != null && !B(bases, "autoRandomSeed", true))
            {
                ps.useAutoRandomSeed = false;
                ps.randomSeed = (uint)F(bases, "randomSeed", 0f);
            }

            // ---- emission ----
            if (props["emission"] is JObject emiData)
            {
                JObject emiBases = emiData["bases"] as JObject ?? new JObject();
                emission.enabled = B(emiBases, "enable", true);
                emission.rateOverTime = F(emiBases, "emissionRate", 10f);
                if (emiData["bursts"] is JArray bursts)
                {
                    var list = new List<ParticleSystem.Burst>();
                    foreach (JToken b in bursts)
                        list.Add(CreateBurst(b));
                    emission.SetBursts(list.ToArray());
                }
            }

            ApplyShape(ps, props["shape"] as JObject);
            ApplyVelocityOverLifetime(ps, props["velocityOverLifetime"] as JObject, ctx);
            ApplyColorOverLifetime(ps, props["colorOverLifetime"] as JObject);
            ApplySizeOverLifetime(ps, props["sizeOverLifetime"] as JObject, ctx);
            ApplyRotationOverLifetime(ps, props["rotationOverLifetime"] as JObject, ctx);
            ApplyTextureSheet(ps, props["textureSheetAnimation"] as JObject, ctx);

            // ---- renderer ----
            var psr = go.GetComponent<ParticleSystemRenderer>();
            JObject rd = props["renderer"] as JObject;
            JObject rdBases = rd?["bases"] as JObject ?? new JObject();
            int renderMode = (int)F(rdBases, "renderMode", 0f);
            switch (renderMode)
            {
                case 1:
                    psr.renderMode = ParticleSystemRenderMode.Stretch;
                    psr.lengthScale = F(rdBases, "stretchedBillboardLengthScale", 2f);
                    psr.velocityScale = F(rdBases, "stretchedBillboardSpeedScale", 0f);
                    break;
                case 2: psr.renderMode = ParticleSystemRenderMode.HorizontalBillboard; break;
                case 3: psr.renderMode = ParticleSystemRenderMode.VerticalBillboard; break;
                case 4:
                    Mesh particleMesh = LoadEffectMesh((string)(rd?["resources"]?["mesh"]), ctx);
                    if (particleMesh != null)
                    {
                        psr.renderMode = ParticleSystemRenderMode.Mesh;
                        psr.alignment = ParticleSystemRenderSpace.Local;
                        psr.mesh = particleMesh;
                        ctx.Report.Log.AppendLine($"   Mesh 粒子 {go.name}: {Path.GetFileName((string)(rd?["resources"]?["mesh"]))}");
                    }
                    else
                    {
                        psr.renderMode = ParticleSystemRenderMode.Billboard;
                        ctx.Report.Log.AppendLine($"   ⚠ {go.name}: Mesh 粒子缺少 mesh 资源,已回退 Billboard");
                    }
                    break;
                default: psr.renderMode = ParticleSystemRenderMode.Billboard; break;
            }
            psr.sortingFudge = F(rdBases, "sortingFudge", 0f);
            string matPath = (string)(rd?["resources"]?["material"]);
            psr.sharedMaterial = LoadMaterial(matPath, ctx) ?? DefaultParticleMaterial(ctx);
        }

        // ---- 各 over-lifetime 模块 ----

        private static void ApplyShape(ParticleSystem ps, JObject data)
        {
            var shape = ps.shape;
            if (data == null) { shape.enabled = false; return; }
            JObject bases = data["bases"] as JObject ?? new JObject();
            shape.enabled = B(bases, "enable", true);
            int shapeType = data["shapeType"] != null ? (int)data["shapeType"] : (int)F(bases, "shapeType", 0f);
            float randomDir = F(bases, "randomDirectionAmount", F(bases, "randomDirection", 0f));
            shape.randomDirectionAmount = randomDir;
            switch (shapeType)
            {
                case 0: // Sphere{radius, emitFromShell}
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = F(bases, "radius", 1f);
                    shape.radiusThickness = B(bases, "emitFromShell", false) ? 0f : 1f;
                    break;
                case 1: // Hemisphere
                    shape.shapeType = ParticleSystemShapeType.Hemisphere;
                    shape.radius = F(bases, "radius", 1f);
                    shape.radiusThickness = B(bases, "emitFromShell", false) ? 0f : 1f;
                    break;
                case 2: // Cone{angle(弧度), radius, length, emitType 0底面/1底面壳/2体积/3体积壳}
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = F(bases, "angle", 25f * Mathf.Deg2Rad) * Mathf.Rad2Deg;
                    shape.radius = F(bases, "radius", 1f);
                    shape.length = F(bases, "length", 5f);
                    int emitType = (int)F(bases, "emitType", 0f);
                    shape.shapeType = emitType >= 2 ? ParticleSystemShapeType.ConeVolume : ParticleSystemShapeType.Cone;
                    shape.radiusThickness = (emitType == 1 || emitType == 3) ? 0f : 1f;
                    break;
                case 3: // Box{x,y,z}
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(F(bases, "x", 1f), F(bases, "y", 1f), F(bases, "z", 1f));
                    break;
                case 7: // Circle{radius, arc(弧度), emitFromEdge}
                    shape.shapeType = ParticleSystemShapeType.Circle;
                    shape.radius = F(bases, "radius", 1f);
                    shape.arc = F(bases, "arc", Mathf.PI * 2f) * Mathf.Rad2Deg;
                    shape.radiusThickness = B(bases, "emitFromEdge", false) ? 0f : 1f;
                    break;
                default:
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    break;
            }
        }

        private static void ApplyVelocityOverLifetime(ParticleSystem ps, JObject data, Context ctx)
        {
            if (data == null) return;
            var vol = ps.velocityOverLifetime;
            JObject bases = data["bases"] as JObject ?? new JObject();
            vol.enabled = B(bases, "enable", true);
            JObject v = data["velocity"] as JObject;
            if (v == null) return;
            vol.space = (int)F(bases, "space", 0f) == 1
                ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            int type = v["type"] != null ? (int)v["type"] : 0;
            switch (type)
            {
                case 0:
                    Vector3 c = V3(v["constant"] as JArray, Vector3.zero);
                    vol.x = new ParticleSystem.MinMaxCurve(c.x);
                    vol.y = new ParticleSystem.MinMaxCurve(c.y);
                    vol.z = new ParticleSystem.MinMaxCurve(c.z);
                    break;
                case 1:
                    vol.x = CurveOf(v["gradientX"], "velocitys", 1f);
                    vol.y = CurveOf(v["gradientY"], "velocitys", 1f);
                    vol.z = CurveOf(v["gradientZ"], "velocitys", 1f);
                    break;
                case 2:
                    Vector3 mn = V3(v["constantMin"] as JArray, Vector3.zero);
                    Vector3 mx = V3(v["constantMax"] as JArray, Vector3.zero);
                    vol.x = new ParticleSystem.MinMaxCurve(mn.x, mx.x);
                    vol.y = new ParticleSystem.MinMaxCurve(mn.y, mx.y);
                    vol.z = new ParticleSystem.MinMaxCurve(mn.z, mx.z);
                    break;
                case 3:
                    vol.x = TwoCurves(v["gradientXMin"], v["gradientXMax"], "velocitys", 1f);
                    vol.y = TwoCurves(v["gradientYMin"], v["gradientYMax"], "velocitys", 1f);
                    vol.z = TwoCurves(v["gradientZMin"], v["gradientZMax"], "velocitys", 1f);
                    break;
            }
        }

        private static void ApplyColorOverLifetime(ParticleSystem ps, JObject data)
        {
            if (data == null) return;
            var col = ps.colorOverLifetime;
            JObject bases = data["bases"] as JObject ?? new JObject();
            col.enabled = B(bases, "enable", true);
            JObject c = data["color"] as JObject;
            if (c == null) return;
            int type = c["type"] != null ? (int)c["type"] : 1;
            switch (type)
            {
                case 0:
                    col.color = new ParticleSystem.MinMaxGradient(C4(c["constant"] as JArray));
                    break;
                case 1:
                    col.color = new ParticleSystem.MinMaxGradient(GradientOf(c["gradient"] as JObject));
                    break;
                case 2:
                    col.color = new ParticleSystem.MinMaxGradient(C4(c["constantMin"] as JArray), C4(c["constantMax"] as JArray));
                    break;
                case 3:
                    col.color = new ParticleSystem.MinMaxGradient(
                        GradientOf(c["gradientMin"] as JObject), GradientOf(c["gradientMax"] as JObject));
                    break;
            }
        }

        private static void ApplySizeOverLifetime(ParticleSystem ps, JObject data, Context ctx)
        {
            if (data == null) return;
            var sol = ps.sizeOverLifetime;
            JObject bases = data["bases"] as JObject ?? new JObject();
            sol.enabled = B(bases, "enable", true);
            JObject sz = data["size"] as JObject;
            if (sz == null) return;
            bool sep = sz.Value<bool>("separateAxes");
            int type = sz["type"] != null ? (int)sz["type"] : 0;
            switch (type)
            {
                case 0:
                    if (sep)
                    {
                        sol.separateAxes = true;
                        sol.x = CurveOf(sz["gradientX"], "sizes", 1f);
                        sol.y = CurveOf(sz["gradientY"], "sizes", 1f);
                        sol.z = CurveOf(sz["gradientZ"], "sizes", 1f);
                    }
                    else sol.size = CurveOf(sz["gradient"], "sizes", 1f);
                    break;
                case 1:
                    sol.size = new ParticleSystem.MinMaxCurve(
                        sz.Value<float?>("constantMin") ?? 0f, sz.Value<float?>("constantMax") ?? 0f);
                    break;
                case 2:
                    sol.size = TwoCurves(sz["gradientMin"], sz["gradientMax"], "sizes", 1f);
                    break;
            }
        }

        private static void ApplyRotationOverLifetime(ParticleSystem ps, JObject data, Context ctx)
        {
            if (data == null) return;
            var rol = ps.rotationOverLifetime;
            JObject bases = data["bases"] as JObject ?? new JObject();
            rol.enabled = B(bases, "enable", true);
            JObject av = data["angularVelocity"] as JObject;
            if (av == null) return;
            bool sep = av.Value<bool>("separateAxes");
            int type = av["type"] != null ? (int)av["type"] : 0;
            switch (type)
            {
                case 0:
                    if (sep)
                    {
                        Vector3 cs = V3(av["constantSeparate"] as JArray, new Vector3(0, 0, Mathf.PI / 4));
                        rol.separateAxes = true;
                        rol.x = new ParticleSystem.MinMaxCurve(cs.x);
                        rol.y = new ParticleSystem.MinMaxCurve(cs.y);
                        rol.z = new ParticleSystem.MinMaxCurve(cs.z);
                    }
                    else rol.z = new ParticleSystem.MinMaxCurve(av.Value<float?>("constant") ?? Mathf.PI / 4);
                    break;
                case 1:
                    rol.z = CurveOf(av["gradient"], "angularVelocitys", 1f);
                    break;
                case 2:
                    rol.z = new ParticleSystem.MinMaxCurve(
                        av.Value<float?>("constantMin") ?? 0f, av.Value<float?>("constantMax") ?? Mathf.PI / 4);
                    break;
                case 3:
                    rol.z = TwoCurves(av["gradientMin"], av["gradientMax"], "angularVelocitys", 1f);
                    break;
            }
        }

        private static void ApplyTextureSheet(ParticleSystem ps, JObject data, Context ctx)
        {
            if (data == null) return;
            var tsa = ps.textureSheetAnimation;
            JObject bases = data["bases"] as JObject ?? new JObject();
            tsa.enabled = B(bases, "enable", true);
            JArray tiles = (data["vector2s"] as JObject)?["tiles"] as JArray;
            int tilesX = tiles != null ? (int)(float)tiles[0] : 1;
            int tilesY = tiles != null ? (int)(float)tiles[1] : 1;
            tsa.numTilesX = Mathf.Max(tilesX, 1);
            tsa.numTilesY = Mathf.Max(tilesY, 1);
            tsa.cycleCount = Mathf.Max((int)F(bases, "cycles", 1f), 1);
            int totalFrames = Mathf.Max(tsa.numTilesX * tsa.numTilesY, 1);

            JObject frame = data["frame"] as JObject;
            if (frame != null)
            {
                int type = frame["type"] != null ? (int)frame["type"] : 1;
                switch (type)
                {
                    case 0:
                        tsa.frameOverTime = new ParticleSystem.MinMaxCurve((frame.Value<float?>("constant") ?? 0f) / totalFrames);
                        break;
                    case 1:
                        tsa.frameOverTime = CurveOf(frame["overTime"], "frames", 1f / totalFrames);
                        break;
                    case 2:
                        tsa.frameOverTime = new ParticleSystem.MinMaxCurve(
                            (frame.Value<float?>("constantMin") ?? 0f) / totalFrames,
                            (frame.Value<float?>("constantMax") ?? 0f) / totalFrames);
                        break;
                    case 3:
                        tsa.frameOverTime = TwoCurves(frame["overTimeMin"], frame["overTimeMax"], "frames", 1f / totalFrames);
                        break;
                }
            }
            JObject startFrame = data["startFrame"] as JObject;
            if (startFrame != null)
            {
                int type = startFrame["type"] != null ? (int)startFrame["type"] : 0;
                tsa.startFrame = type == 1
                    ? new ParticleSystem.MinMaxCurve(
                        (startFrame.Value<float?>("constantMin") ?? 0f) / totalFrames,
                        (startFrame.Value<float?>("constantMax") ?? 0f) / totalFrames)
                    : new ParticleSystem.MinMaxCurve((startFrame.Value<float?>("constant") ?? 0f) / totalFrames);
            }
        }

        // ================= 网格 / 拖尾 =================

        /// <summary>特效里的静态网格(光面片等):meshPath(.lm)+ materials[].path(.lmat)。</summary>
        private static void BuildMeshNode(GameObject go, JObject props, Context ctx)
        {
            string meshPath = (string)props["meshPath"];
            if (string.IsNullOrEmpty(meshPath))
            {
                ctx.Report.Log.AppendLine($"   ⚠ {go.name}: MeshSprite3D 无 meshPath");
                return;
            }
            string lmAbs = Path.GetFullPath(Path.Combine(ctx.LhDir, meshPath));
            if (!File.Exists(lmAbs))
            {
                ctx.Report.Log.AppendLine($"   ⚠ {go.name}: .lm 不存在 {meshPath}");
                return;
            }
            LmMesh lm = LmParser.Parse(File.ReadAllBytes(lmAbs));
            Mesh mesh = Laya3DImporter.BuildMeshForEffect(lm, new Laya3DImporter.Result());
            string meshAsset = ctx.OutDir + "/" + go.name + "_mesh.asset";
            AssetDatabase.CreateAsset(mesh, meshAsset);
            go.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAsset);

            Material mat = null;
            if (props["materials"] is JArray mats && mats.Count > 0)
                mat = LoadMaterial((string)mats[0]?["path"], ctx);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat ?? DefaultParticleMaterial(ctx);
        }

        private static Mesh LoadEffectMesh(string lmRel, Context ctx)
        {
            if (string.IsNullOrEmpty(lmRel)) return null;
            string lmAbs = Path.GetFullPath(Path.Combine(ctx.LhDir, lmRel));
            if (ctx.MeshCache.TryGetValue(lmAbs, out Mesh cached)) return cached;
            if (!File.Exists(lmAbs))
            {
                ctx.Report.Log.AppendLine("   ⚠ 粒子 .lm 不存在 " + lmRel);
                return null;
            }

            LmMesh lm = LmParser.Parse(File.ReadAllBytes(lmAbs));
            Mesh mesh = Laya3DImporter.BuildMeshForEffect(lm, new Laya3DImporter.Result());
            string meshAsset = ctx.OutDir + "/" + SafeAssetStem(lmAbs) + "_particle_mesh.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshAsset);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, meshAsset);
            }

            Mesh loaded = AssetDatabase.LoadAssetAtPath<Mesh>(meshAsset);
            ctx.MeshCache[lmAbs] = loaded;
            return loaded;
        }

        /// <summary>TrailSprite3D → TrailRenderer 近似(time/width/widthCurve/colorGradient)。</summary>
        private static void BuildTrail(GameObject go, JObject props, Context ctx)
        {
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = props.Value<float?>("time") ?? 0.5f;
            tr.minVertexDistance = props.Value<float?>("minVertexDistance") ?? 0.1f;
            float widthMul = props.Value<float?>("widthMultiplier") ?? 1f;
            if (props["widthCurve"] is JArray wc && wc.Count > 0)
            {
                var curve = new AnimationCurve();
                foreach (JToken k in wc)
                    curve.AddKey(new Keyframe(JF(k, "time", 0f), JF(k, "value", 1f),
                        JF(k, "inTangent", 0f), JF(k, "outTangent", 0f)));
                tr.widthCurve = curve;
            }
            tr.widthMultiplier = widthMul;
            if (props["colorGradient"] is JObject cg) tr.colorGradient = GradientOf(cg);
            Material mat = null;
            if (props["materials"] is JArray mats && mats.Count > 0)
                mat = LoadMaterial((string)mats[0]?["path"], ctx);
            tr.sharedMaterial = mat ?? DefaultParticleMaterial(ctx);
        }

        // ================= 材质 =================

        /// <summary>
        /// .lmat → URP 材质。ShurikenParticleMaterial(renderMode 0=AlphaBlend/1=Additive)、
        /// UnlitMaterial(0不透明/1裁切/2透明/3加色)。贴图按 cdn 相对路径镜像进 GameRes 共享。
        /// </summary>
        private static Material LoadMaterial(string lmatRel, Context ctx)
        {
            if (string.IsNullOrEmpty(lmatRel)) return null;
            string lmatAbs = Path.GetFullPath(Path.Combine(ctx.LhDir, lmatRel));
            if (ctx.MaterialCache.TryGetValue(lmatAbs, out Material cached)) return cached;
            if (!File.Exists(lmatAbs))
            {
                ctx.Report.Log.AppendLine("   ⚠ .lmat 不存在 " + lmatRel);
                return null;
            }
            JObject doc;
            try { doc = JObject.Parse(File.ReadAllText(lmatAbs)); }
            catch (Exception e)
            {
                ctx.Report.Log.AppendLine($"   ⚠ .lmat 解析失败 {lmatRel}: {e.Message}(LFS 占位?)");
                return null;
            }
            JObject props = doc["props"] as JObject ?? new JObject();
            string type = (string)props["type"] ?? "ShurikenParticleMaterial";

            // 混合状态:Laya 存在 props.renderStates[0]。srcBlend/dstBlend 是 WebGL 混合因子枚举
            // (770=SrcAlpha 771=OneMinusSrcAlpha 1=One 0=Zero),depthWrite/cull 同处;旧式材质退化读 props。
            // 关键修复(v3):此前只读 props["dstBlend"](恒为空,真实数据在 renderStates 里)+ 类型判等漏了
            // "Laya." 前缀,导致加色刀光/glow(dstBlend=One)全被当普通混合 → 黑底盖成黑块(发黑根因)。
            JArray rsArr = props["renderStates"] as JArray;
            JObject rs = rsArr != null && rsArr.Count > 0 ? rsArr[0] as JObject : null;
            int? srcGl = IntFrom(rs, props, "srcBlend");
            int? dstGl = IntFrom(rs, props, "dstBlend");

            bool additive;
            UnityEngine.Rendering.BlendMode srcBlend, dstBlend;
            if (srcGl.HasValue && dstGl.HasValue)
            {
                srcBlend = MapGlBlend(srcGl.Value, UnityEngine.Rendering.BlendMode.SrcAlpha);
                dstBlend = MapGlBlend(dstGl.Value, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                additive = dstBlend == UnityEngine.Rendering.BlendMode.One;
            }
            else
            {
                bool isParticle = type.EndsWith("ShurikenParticleMaterial"); // 兼容 "Laya." 命名空间前缀
                if (props["renderMode"] != null)
                {
                    int renderMode = (int)(float)props["renderMode"];
                    additive = isParticle ? renderMode != 0 : renderMode == 3;
                }
                else additive = isParticle; // 粒子无显式混合时默认加色
                srcBlend = UnityEngine.Rendering.BlendMode.SrcAlpha;
                dstBlend = additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
            }
            bool depthWrite = (rs ?? props).Value<bool?>("depthWrite") ?? false;
            ctx.Report.Log.AppendLine($"   材质 {Path.GetFileName(lmatAbs)}: type={type} src={srcBlend} dst={dstBlend} 混合={(additive ? "加色Additive" : "普通Alpha")}");

            var mat = CreateParticleMaterial();
            SetupTransparent(mat, srcBlend, dstBlend, additive, depthWrite);

            // vectors:color/_TintColor/albedoColor → _BaseColor;tilingOffset → _BaseMap_ST
            if (props["vectors"] is JArray vectors)
            {
                foreach (JToken v in vectors)
                {
                    string vName = (string)v["name"];
                    JArray val = v["value"] as JArray;
                    if (val == null) continue;
                    if (IsLayaColorComponent(vName) && val.Count >= 4)
                    {
                        Color color = LayaShaderColor(C4(val));
                        mat.SetColor("_BaseColor", color);
                        mat.SetColor("_Color", color);
                    }
                    else if (vName == "tilingOffset" && val.Count >= 4)
                        SetTextureScaleOffset(mat, val);
                }
            }

            bool hasTexture = false;
            int texDeclared = 0;
            if (props["textures"] is JArray textures)
            {
                foreach (JToken t in textures)
                {
                    string texRel = (string)t["path"];
                    if (string.IsNullOrEmpty(texRel)) continue;
                    texDeclared++;
                    Texture2D tex = ImportTexture(texRel, lmatAbs, ctx);
                    if (tex != null)
                    {
                        mat.SetTexture("_BaseMap", tex);
                        mat.SetTexture("_MainTex", tex);
                        hasTexture = true;
                        break;
                    }
                }
            }

            // 紫块诊断:shader 丢失/未编译 = 品红;贴图缺失 = 白(不是紫)。把"好像"变确定。
            string shaderName = mat.shader != null ? mat.shader.name : "<null>";
            bool shaderBad = mat.shader == null || !mat.shader.isSupported
                             || shaderName == "Hidden/InternalErrorShader";
            if (shaderBad)
                ctx.Report.Log.AppendLine($"   ❌紫块根因 {Path.GetFileName(lmatAbs)}: shader 不可用 = {shaderName}(品红即此)");
            if (texDeclared > 0 && !hasTexture)
                ctx.Report.Log.AppendLine($"   ⚠贴图缺失 {Path.GetFileName(lmatAbs)}: 声明 {texDeclared} 张但全部导入失败(LFS 占位?)");

            string matAsset = ctx.OutDir + "/" + Path.GetFileNameWithoutExtension(lmatAbs) + ".mat";
            AssetDatabase.CreateAsset(mat, matAsset);
            Material loaded = AssetDatabase.LoadAssetAtPath<Material>(matAsset);
            ctx.MaterialCache[lmatAbs] = loaded;
            return loaded;
        }

        /// <summary>
        /// URP Particles/Unlit 透明设置。src/dst = 解析自 Laya renderStates 的真实混合因子(GPU 实际生效);
        /// _Blend 枚举(0 Alpha / 2 Additive)仅作 Inspector 显示。均双面、透明队列;
        /// depthWrite 跟随源(特效一般不写深度)。_BaseColor 默认白,随后被 .lmat 的 tint 覆盖(若有)。
        /// </summary>
        private static void SetupTransparent(Material mat, UnityEngine.Rendering.BlendMode src,
            UnityEngine.Rendering.BlendMode dst, bool additive, bool depthWrite)
        {
            bool premultiply = src == UnityEngine.Rendering.BlendMode.One &&
                dst == UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
            bool multiply = IsMultiplyBlend(src, dst);

            mat.SetFloat("_Surface", 1f);                    // Transparent
            mat.SetFloat("_Blend", additive ? 2f : premultiply ? 1f : multiply ? 3f : 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_SrcBlend", (float)src);
            mat.SetFloat("_DstBlend", (float)dst);
            mat.SetFloat("_SrcBlendAlpha", (float)src);
            mat.SetFloat("_DstBlendAlpha", (float)dst);
            mat.SetFloat("_BlendOp", (float)UnityEngine.Rendering.BlendOp.Add);
            mat.SetFloat("_ZWrite", depthWrite ? 1f : 0f);
            mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetColor("_BaseColor", Color.white);         // 默认白,避免缺 tint 时发黑
            mat.SetShaderPassEnabled("ALWAYS", false);
            mat.SetShaderPassEnabled("DepthOnly", false);
            mat.SetShaderPassEnabled("SHADOWCASTER", false);
            if (mat.shader == null || mat.shader.name != "Shenxiao/Effect/LayaParticleUnlit")
            {
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.DisableKeyword("_ALPHAMODULATE_ON");
                if (premultiply) mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                else if (multiply) mat.EnableKeyword("_ALPHAMODULATE_ON");
            }
        }

        /// <summary>
        /// Laya burst 使用浮点 emitCount, for(i&lt;emitCount) 导致 0~1 的正随机值也会发 1 个;
        /// Unity Burst 是整数随机,直接 0~1 会有一半概率不发,创角刀光会整片丢失。
        /// </summary>
        private static ParticleSystem.Burst CreateBurst(JToken data)
        {
            float time = JF(data, "time", 0f);
            float layaMin = JF(data, "min", 0f);
            float layaMax = JF(data, "max", 0f);
            int max = Mathf.Max(0, Mathf.CeilToInt(layaMax));
            int min;
            if (layaMax > layaMin)
                min = Mathf.Max(1, Mathf.FloorToInt(layaMin) + 1);
            else
                min = Mathf.Max(0, Mathf.CeilToInt(layaMin));
            min = Mathf.Clamp(min, 0, max);
            return new ParticleSystem.Burst(time, (short)min, (short)max);
        }

        private static bool IsMultiplyBlend(UnityEngine.Rendering.BlendMode src, UnityEngine.Rendering.BlendMode dst)
        {
            return src == UnityEngine.Rendering.BlendMode.DstColor ||
                dst == UnityEngine.Rendering.BlendMode.SrcColor ||
                dst == UnityEngine.Rendering.BlendMode.DstColor ||
                (src == UnityEngine.Rendering.BlendMode.Zero && dst != UnityEngine.Rendering.BlendMode.One);
        }

        private static Shader FindParticleShader()
        {
            const string customShaderPath = "Assets/Shaders/LayaParticleUnlit.shader";
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(customShaderPath);
            if (shader != null) return shader;
            shader = Shader.Find("Shenxiao/Effect/LayaParticleUnlit");
            if (shader != null) return shader;
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null) return shader;
            shader = Shader.Find("Particles/Standard Unlit");
            if (shader != null) return shader;
            shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null) return shader;
            throw new InvalidOperationException("找不到可用的粒子透明 Shader。请确认 URP 包已导入。");
        }

        private static Material CreateParticleMaterial()
        {
            return new Material(FindParticleShader());
        }

        private static void SetTextureScaleOffset(Material mat, JArray val)
        {
            var scale = new Vector2((float)val[0], (float)val[1]);
            var offset = new Vector2((float)val[2], (float)val[3]);
            mat.SetTextureScale("_BaseMap", scale);
            mat.SetTextureOffset("_BaseMap", offset);
            mat.SetTextureScale("_MainTex", scale);
            mat.SetTextureOffset("_MainTex", offset);
            mat.SetVector("_BaseMap_ST", new Vector4(scale.x, scale.y, offset.x, offset.y));
            mat.SetVector("_MainTex_ST", new Vector4(scale.x, scale.y, offset.x, offset.y));
        }

        private static string SafeAssetStem(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        /// <summary>renderStates[0] 优先、退化到 props 的 int 取值(混合因子/枚举用)。</summary>
        private static int? IntFrom(JObject primary, JObject fallback, string key)
        {
            JToken t = primary?[key] ?? fallback?[key];
            return t == null || t.Type == JTokenType.Null ? (int?)null : (int)(float)t;
        }

        /// <summary>WebGL/Laya 混合因子枚举 → Unity BlendMode(770=SrcAlpha 771=OneMinusSrcAlpha 1=One …)。</summary>
        private static UnityEngine.Rendering.BlendMode MapGlBlend(int gl, UnityEngine.Rendering.BlendMode def)
        {
            switch (gl)
            {
                case 0: return UnityEngine.Rendering.BlendMode.Zero;
                case 1: return UnityEngine.Rendering.BlendMode.One;
                case 768: return UnityEngine.Rendering.BlendMode.SrcColor;
                case 769: return UnityEngine.Rendering.BlendMode.OneMinusSrcColor;
                case 770: return UnityEngine.Rendering.BlendMode.SrcAlpha;
                case 771: return UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                case 772: return UnityEngine.Rendering.BlendMode.DstAlpha;
                case 773: return UnityEngine.Rendering.BlendMode.OneMinusDstAlpha;
                case 774: return UnityEngine.Rendering.BlendMode.DstColor;
                case 775: return UnityEngine.Rendering.BlendMode.OneMinusDstColor;
                default: return def;
            }
        }

        private static bool IsLayaColorComponent(string name)
        {
            return name == "color" || name == "_TintColor" || name == "_Color" || name == "albedoColor";
        }

        private static Color LayaShaderColor(Color color)
        {
            return new Color(color.r * 2f, color.g * 2f, color.b * 2f, color.a);
        }

        /// <summary>贴图镜像进 GameRes(按 cdn/resource 相对路径,跨特效共享)。</summary>
        private static Texture2D ImportTexture(string texRel, string lmatAbs, Context ctx)
        {
            string texAbs = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(lmatAbs), texRel));
            if (!File.Exists(texAbs))
            {
                ctx.Report.Log.AppendLine("   ⚠ 贴图不存在 " + texRel);
                return null;
            }
            // 求相对 cdn/resource 的路径;不在其下则收进产物目录
            string norm = texAbs.Replace('\\', '/');
            int idx = norm.IndexOf("/resource/", StringComparison.Ordinal);
            string assetPath = idx >= 0
                ? "Assets/GameRes/resource/" + norm.Substring(idx + "/resource/".Length)
                : ctx.OutDir + "/" + Path.GetFileName(texAbs);
            string assetAbs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            if (!File.Exists(assetAbs))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(assetAbs));
                File.Copy(texAbs, assetAbs, true);
                AssetDatabase.ImportAsset(assetPath);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static Material DefaultParticleMaterial(Context ctx)
        {
            const string path = "Assets/GameRes/effect/__default_particle.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var mat = CreateParticleMaterial();
            SetupTransparent(mat, UnityEngine.Rendering.BlendMode.SrcAlpha,
                UnityEngine.Rendering.BlendMode.One, additive: true, depthWrite: false);
            Directory.CreateDirectory("Assets/GameRes/effect");
            AssetDatabase.CreateAsset(mat, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        // ================= 数据小工具 =================

        /// <summary>JToken 子项取 float(缺省安全;{key,value} 点列等小对象用)。</summary>
        private static float JF(JToken o, string key, float def)
        {
            JToken t = (o as JObject)?[key];
            return t == null || t.Type == JTokenType.Null ? def : (float)t;
        }

        private static float F(JObject o, string key, float def)
        {
            JToken t = o?[key];
            if (t == null) return def;
            if (t.Type == JTokenType.Boolean) return (bool)t ? 1f : 0f;
            return (float)t;
        }

        private static bool B(JObject o, string key, bool def)
        {
            JToken t = o?[key];
            if (t == null) return def;
            if (t.Type == JTokenType.Boolean) return (bool)t;
            return (float)t != 0f;
        }

        private static Vector3 V3(JArray a, Vector3 def)
        {
            return a != null && a.Count >= 3 ? new Vector3((float)a[0], (float)a[1], (float)a[2]) : def;
        }

        private static Color C4(JArray a)
        {
            return a != null && a.Count >= 4
                ? new Color((float)a[0], (float)a[1], (float)a[2], (float)a[3])
                : Color.white;
        }

        /// <summary>GradientDataNumber:{listKey:[{key,value}...]} → MinMaxCurve(曲线,值乘 scale)。</summary>
        private static ParticleSystem.MinMaxCurve CurveOf(JToken gradientData, string listKey, float scale)
        {
            var curve = new AnimationCurve();
            if ((gradientData as JObject)?[listKey] is JArray points && points.Count > 0)
            {
                foreach (JToken p in points)
                    curve.AddKey(JF(p, "key", 0f), JF(p, "value", 0f) * scale);
            }
            else
            {
                curve.AddKey(0f, 0f);
                curve.AddKey(1f, scale);
            }
            return new ParticleSystem.MinMaxCurve(1f, curve);
        }

        private static ParticleSystem.MinMaxCurve TwoCurves(JToken min, JToken max, string listKey, float scale)
        {
            ParticleSystem.MinMaxCurve a = CurveOf(min, listKey, scale);
            ParticleSystem.MinMaxCurve b = CurveOf(max, listKey, scale);
            return new ParticleSystem.MinMaxCurve(1f, a.curve, b.curve);
        }

        /// <summary>Laya Gradient:{alphas:[{key,value}], rgbs:[{key,value:[r,g,b]}]} → Unity Gradient。</summary>
        private static Gradient GradientOf(JObject data)
        {
            var g = new Gradient();
            var colors = new List<GradientColorKey>();
            var alphas = new List<GradientAlphaKey>();
            if (data?["rgbs"] is JArray rgbs)
            {
                foreach (JToken k in rgbs)
                {
                    JArray v = k["value"] as JArray;
                    if (v != null && v.Count >= 3)
                        colors.Add(new GradientColorKey(new Color((float)v[0], (float)v[1], (float)v[2]), JF(k, "key", 0f)));
                }
            }
            if (data?["alphas"] is JArray alphasArr)
            {
                foreach (JToken k in alphasArr)
                    alphas.Add(new GradientAlphaKey(JF(k, "value", 1f), JF(k, "key", 0f)));
            }
            if (colors.Count == 0) colors.Add(new GradientColorKey(Color.white, 0f));
            if (alphas.Count == 0) alphas.Add(new GradientAlphaKey(1f, 0f));
            // Unity 上限 8 键,Laya 4 键以内,安全
            g.SetKeys(colors.ToArray(), alphas.ToArray());
            return g;
        }

        /// <summary>数值型 MinMaxCurve(常量/双常量;渐变型在 main 模块少见,有样本再补)。</summary>
        private static ParticleSystem.MinMaxCurve NumberMinMax(JObject bases, JObject moduleData, int type,
            string constKey, string minKey, string maxKey,
            string gradKey, string gradMinKey, string gradMaxKey, float def, Context ctx)
        {
            switch (type)
            {
                case 2:
                    return new ParticleSystem.MinMaxCurve(F(bases, minKey, def), F(bases, maxKey, def));
                case 1:
                case 3:
                    if (gradKey != null)
                        ctx.Report.Log.AppendLine($"   ⚠ {constKey}: 渐变型起始值近似为常量(样本核对后补曲线)");
                    return new ParticleSystem.MinMaxCurve(F(bases, constKey, def));
                default:
                    return new ParticleSystem.MinMaxCurve(F(bases, constKey, def));
            }
        }
    }
}
