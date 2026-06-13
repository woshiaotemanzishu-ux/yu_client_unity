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
        public const int TOOL_VERSION = 1;

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
            return go;
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
            else if (vec4s?["startColorConstant"] is JArray c0)
                main.startColor = new ParticleSystem.MinMaxGradient(C4(c0));

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
                        list.Add(new ParticleSystem.Burst(JF(b, "time", 0f),
                            (short)JF(b, "min", 0f), (short)JF(b, "max", 0f)));
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
                    psr.renderMode = ParticleSystemRenderMode.Mesh;
                    ctx.Report.Log.AppendLine($"   ⚠ {go.name}: Mesh 渲染模式的网格引用待样本核对(暂按 Billboard 资源缺省)");
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

            // 混合模式判定(优先级:.lmat 显式 srcBlend/dstBlend > renderMode > 类型默认)。
            // Laya 加色 = dst 为 ONE(1);普通混合 = dst 为 ONE_MINUS_SRC_ALPHA。
            // 关键:仙侠发光特效默认加色——纹理黑底亮线,普通混合会把黑底盖成黑块(发黑根因)。
            bool additive;
            JToken dstTok = props["dstBlend"];
            if (dstTok != null)
            {
                int dst = (int)(float)dstTok;
                additive = dst == 1 || dst == 0x0001; // BLENDPARAM_ONE
            }
            else if (props["renderMode"] != null)
            {
                int renderMode = (int)(float)props["renderMode"];
                additive = type == "ShurikenParticleMaterial" ? renderMode != 0 : renderMode == 3;
            }
            else
            {
                additive = type == "ShurikenParticleMaterial"; // 粒子默认加色
            }
            ctx.Report.Log.AppendLine($"   材质 {Path.GetFileName(lmatAbs)}: type={type} 混合={(additive ? "加色Additive" : "普通Alpha")}");

            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            SetupTransparent(mat, additive);

            // vectors:color/_TintColor/albedoColor → _BaseColor;tilingOffset → _BaseMap_ST
            if (props["vectors"] is JArray vectors)
            {
                foreach (JToken v in vectors)
                {
                    string vName = (string)v["name"];
                    JArray val = v["value"] as JArray;
                    if (val == null) continue;
                    if ((vName == "color" || vName == "_TintColor" || vName == "albedoColor") && val.Count >= 4)
                        mat.SetColor("_BaseColor", C4(val));
                    else if (vName == "tilingOffset" && val.Count >= 4)
                        mat.SetVector("_BaseMap_ST", new Vector4((float)val[0], (float)val[1], (float)val[2], (float)val[3]));
                }
            }

            if (props["textures"] is JArray textures)
            {
                foreach (JToken t in textures)
                {
                    string texRel = (string)t["path"];
                    if (string.IsNullOrEmpty(texRel)) continue;
                    Texture2D tex = ImportTexture(texRel, lmatAbs, ctx);
                    if (tex != null) { mat.SetTexture("_BaseMap", tex); break; }
                }
            }

            string matAsset = ctx.OutDir + "/" + Path.GetFileNameWithoutExtension(lmatAbs) + ".mat";
            AssetDatabase.CreateAsset(mat, matAsset);
            Material loaded = AssetDatabase.LoadAssetAtPath<Material>(matAsset);
            ctx.MaterialCache[lmatAbs] = loaded;
            return loaded;
        }

        /// <summary>
        /// URP Particles/Unlit 透明设置。_Blend 枚举:0 Alpha / 1 Premultiply / 2 Additive / 3 Multiply。
        /// 加色 = SrcAlpha/One;普通 = SrcAlpha/OneMinusSrcAlpha;均双面、不写深度、透明队列。
        /// _BaseColor 强制白(缺 tint 时不发黑);加色还要关掉颜色 tint 影响。
        /// </summary>
        private static void SetupTransparent(Material mat, bool additive)
        {
            mat.SetFloat("_Surface", 1f);                    // Transparent
            mat.SetFloat("_Blend", additive ? 2f : 0f);      // 2=Additive / 0=Alpha
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", additive
                ? (float)UnityEngine.Rendering.BlendMode.One
                : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            mat.SetColor("_BaseColor", Color.white);         // 默认白,避免缺 tint 时发黑
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            // URP 粒子混合关键字:加色/普通都走 modulate,不要 premultiply(会压暗)
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.EnableKeyword("_ALPHAMODULATE_ON");
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
            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            SetupTransparent(mat, additive: true);
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
