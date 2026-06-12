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
    /// Laya 3D → Unity 原生资产装配器(MVP):
    /// .lh(层级/引用)+ .lm(网格/蒙皮)+ .lmat(贴图)+ .lani(动画)
    /// → Mesh.asset + .anim(Legacy)+ .mat(URP)+ .prefab,产物进 GameRes。
    /// 解析逻辑 1:1 对标 Electron 工具 python 参考实现(已被 3D 预览渲染验证)。
    /// mirrorX:固化 false(v4)。几何镜像路径(v3)有蒙皮 bug 已撤回;朝向差异由
    /// UIModelStage 渲染层水平翻转补偿。场景线需要真镜像时用真实样本字节级核对后再启用。
    /// </summary>
    public static class Laya3DImporter
    {
        /// <summary>转换逻辑版本:改了产物结构/默认行为就 +1,资产管理会把旧版产物标 🔶 要求重转。
        /// v2 = 动作 clip 共享目录 + 默认全量动作 + 非蒙皮分支 + Unlit 默认。
        /// v3 = mirrorX=true 几何镜像(已撤回:蒙皮塌陷/扭曲,镜像数学需真实样本字节级核对)。
        /// v4 = 回退 mirrorX=false;镜像问题改在 UIModelStage 渲染层水平翻转解决
        ///      (与老客户端同向,见 UIModelStage.FLIP_HORIZONTAL;几何镜像留给场景线用真实样本再攻)。
        /// v5 = 角色动作目录公式修正:1000+career*100(此前 career*1000+100 只剑士碰巧对,
        ///      武姬/枪使/弓手转出无动作,需重转补动作)。</summary>
        public const int TOOL_VERSION = 5;
        /// <summary>材质模式:Unlit=贴图直出,对标老客户端(UIModelClass3D.ts 把角色材质按
        /// Laya.UnlitMaterial 处理,electron 工具 .lmat 也写 Laya.UnlitMaterial),不吃光照不会发黑;
        /// Lit=URP SimpleLit,受场景光照(留给后续真需要光照的资产)。
        /// 注:按 .lmat type/albedoColor 自动决策曾导致模型不可见,待真实 .lmat 样本核对后再启用。</summary>
        public enum MaterialMode { Lit, Unlit }

        public sealed class Result
        {
            public bool Ok;
            public string PrefabPath = "";
            public readonly StringBuilder Log = new StringBuilder();
        }

        public static Result Convert(string lhPath, List<string> laniPaths, bool mirrorX, MaterialMode materialMode = MaterialMode.Unlit)
        {
            var r = new Result();
            try
            {
                ConvertInner(lhPath, laniPaths ?? new List<string>(), mirrorX, materialMode, r);
                r.Ok = true;
            }
            catch (Exception e)
            {
                r.Log.AppendLine("❌ 失败: " + e.Message);
                r.Log.AppendLine(e.StackTrace);
                r.Ok = false;
            }
            Debug.Log("[Laya3D] 转换日志\n" + r.Log);
            return r;
        }

        private static void ConvertInner(string lhPath, List<string> laniPaths, bool mirrorX, MaterialMode materialMode, Result r)
        {
            string modelName = Path.GetFileNameWithoutExtension(lhPath);
            // 输出目录按源目录约定泛化:.../object/{module}/objs/x.lh → Assets/GameRes/object/{module}/x
            string module = DeriveModule(lhPath);
            string outDir = $"Assets/GameRes/object/{module}/{modelName}";
            Directory.CreateDirectory(outDir);

            // ---------- ① .lh ----------
            LhDocument lh = LhDocument.Load(lhPath);
            r.Log.AppendLine($"① .lh 解析: root={lh.RootName} 骨骼节点={lh.Bones.Count} meshPath={lh.MeshPath} 材质={lh.MaterialPaths.Count}");
            if (string.IsNullOrEmpty(lh.MeshPath)) throw new Exception(".lh 里找不到 SkinnedMeshSprite3D/MeshSprite3D 的 meshPath");

            string lmPath = lh.ResolveAssetPath(lh.MeshPath);
            if (lmPath == null) throw new Exception("找不到 .lm 文件: " + lh.MeshPath);
            r.Log.AppendLine("   .lm 实际路径: " + lmPath);

            // ---------- ② .lm ----------
            LmMesh lm = LmParser.Parse(File.ReadAllBytes(lmPath));
            r.Log.AppendLine($"② .lm 解析: name={lm.Name} 顶点={lm.VertexCount} 索引={lm.IndexData.Length} flag={lm.VertexFlag}");
            r.Log.AppendLine($"   骨骼={lm.BoneNames.Count} 逆绑定={lm.InverseBindPoses.Count} SubMesh={lm.SubMeshes.Count}");
            if (lm.BoundsMin.HasValue) r.Log.AppendLine($"   包围盒 {lm.BoundsMin} ~ {lm.BoundsMax}(应为米级,角色高 1~2)");

            // 校验:lm 骨骼名必须都在 lh 层级里
            var missingBones = new List<string>();
            foreach (string b in lm.BoneNames)
            {
                if (!lh.BoneNameToIndex.ContainsKey(b)) missingBones.Add(b);
            }
            if (missingBones.Count > 0)
            {
                r.Log.AppendLine("   ⚠ lm 骨骼在 lh 层级缺失: " + string.Join(",", missingBones));
            }

            // ---------- ③ 骨架 ----------
            var rootGo = new GameObject(modelName);
            var boneTransforms = new Transform[lh.Bones.Count];
            for (int i = 0; i < lh.Bones.Count; i++)
            {
                LhBone b = lh.Bones[i];
                var go = new GameObject(b.Name);
                Transform parent = b.ParentIndex >= 0 ? boneTransforms[b.ParentIndex] : rootGo.transform;
                go.transform.SetParent(parent, false);
                go.transform.localPosition = Pos(b.Position, mirrorX);
                go.transform.localRotation = Rot(b.Rotation, mirrorX);
                go.transform.localScale = new Vector3(b.Scale[0], b.Scale[1], b.Scale[2]);
                boneTransforms[i] = go.transform;
            }

            // ---------- ④ Mesh ----------
            Mesh mesh = BuildMesh(lm, mirrorX, r);
            string meshAsset = outDir + "/" + modelName + "_mesh.asset";
            AssetDatabase.CreateAsset(mesh, meshAsset);

            // ---------- ⑤ 材质 ----------
            Material mat = BuildMaterial(lh, modelName, outDir, materialMode, r);

            // ---------- ⑥ 渲染器(蒙皮=SkinnedMeshRenderer,静态=MeshFilter+MeshRenderer) ----------
            var meshGo = new GameObject(lm.Name == "" ? "mesh" : lm.Name);
            meshGo.transform.SetParent(rootGo.transform, false);
            if (lh.MeshNodeRotation != null)
            {
                meshGo.transform.localRotation = Rot(lh.MeshNodeRotation, mirrorX);
            }
            if (lm.BoneNames.Count > 0)
            {
                var smr = meshGo.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = mesh;
                smr.sharedMaterial = mat;

                var bones = new Transform[lm.BoneNames.Count];
                for (int i = 0; i < lm.BoneNames.Count; i++)
                {
                    bones[i] = lh.BoneNameToIndex.TryGetValue(lm.BoneNames[i], out int bi)
                        ? boneTransforms[bi]
                        : rootGo.transform;
                }
                smr.bones = bones;
                smr.rootBone = lh.Bones.Count > 0 ? boneTransforms[0] : rootGo.transform;
                smr.updateWhenOffscreen = true;
            }
            else
            {
                meshGo.AddComponent<MeshFilter>().sharedMesh = mesh;
                meshGo.AddComponent<MeshRenderer>().sharedMaterial = mat;
                r.Log.AppendLine("⑥ 静态网格(无骨骼,武器/部件常见)");
            }

            // ---------- ⑦ 动画(Legacy):clip 共享在 object/{module}/action/{dir}/,增量生成 ----------
            var clips = new List<AnimationClip>();
            foreach (string laniPath in laniPaths)
            {
                AnimationClip clip = BuildOrReuseClip(laniPath, mirrorX, module, rootGo.transform, r);
                if (clip != null) clips.Add(clip);
            }
            if (clips.Count > 0)
            {
                var anim = rootGo.AddComponent<Animation>();
                AnimationUtility.SetAnimationClips(anim, clips.ToArray());
                // 默认播放待机:stand > idle > 第一个(对标老客户端 UI 默认动作)
                anim.clip = clips.Find(c => c.name == "stand") ?? clips.Find(c => c.name == "idle") ?? clips[0];
                anim.playAutomatically = true;
                r.Log.AppendLine($"⑦ 动画 {clips.Count} 个,默认自动播放: {anim.clip.name}");
            }

            // ---------- ⑧ Prefab ----------
            string prefabPath = outDir + "/" + modelName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(rootGo, prefabPath);
            UnityEngine.Object.DestroyImmediate(rootGo);

            // 转换元数据:版本戳 + 实际转的动作,资产管理用它判断「转换器升级需重转」
            var meta = new JObject
            {
                ["tool"] = TOOL_VERSION,
                ["material"] = materialMode.ToString(),
                ["clips"] = new JArray(clips.ConvertAll(c => c.name)),
            };
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "..", outDir, modelName + ".import.json")),
                meta.ToString());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            r.PrefabPath = prefabPath;
            r.Log.AppendLine("✅ 完成: " + prefabPath);
        }

        /// <summary>特效线复用:.lm → Mesh(无镜像;特效网格均为静态面片)。</summary>
        internal static Mesh BuildMeshForEffect(LmMesh lm, Result r) => BuildMesh(lm, false, r);

        private static Mesh BuildMesh(LmMesh lm, bool mirrorX, Result r)
        {
            var mesh = new Mesh { name = lm.Name };
            if (lm.VertexCount > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            Vector3[] positions = LmParser.GetVec3(lm, "POSITION");
            if (positions == null) throw new Exception(".lm 无 POSITION 数据");
            if (mirrorX) for (int i = 0; i < positions.Length; i++) positions[i].x = -positions[i].x;
            mesh.vertices = positions;

            Vector3[] normals = LmParser.GetVec3(lm, "NORMAL");
            if (normals != null)
            {
                if (mirrorX) for (int i = 0; i < normals.Length; i++) normals[i].x = -normals[i].x;
                mesh.normals = normals;
            }
            Vector2[] uvs = LmParser.GetVec2(lm, "UV");
            if (uvs != null)
            {
                // Laya UV 原点左上(贴图坐标),Unity 左下:v 翻转
                for (int i = 0; i < uvs.Length; i++) uvs[i].y = 1f - uvs[i].y;
                mesh.uv = uvs;
            }

            int[] indices = (int[])lm.IndexData.Clone();
            if (mirrorX)
            {
                for (int i = 0; i + 2 < indices.Length; i += 3)
                {
                    (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
                }
            }
            mesh.triangles = indices;

            // 蒙皮:BLENDINDICES 是 draw call 局部下标,经 SubMesh 骨骼表映射到全局
            byte[][] blendIndices = LmParser.GetBlendIndices(lm);
            Vector4[] blendWeights = LmParser.GetVec4(lm, "BLENDWEIGHT");
            if (blendIndices != null && blendWeights != null)
            {
                var globalJoints = new int[lm.VertexCount, 4];
                for (int v = 0; v < lm.VertexCount; v++)
                    for (int c = 0; c < 4; c++) globalJoints[v, c] = blendIndices[v][c];

                if (lm.SubMeshes.Count > 0)
                {
                    LmSubMesh sub = lm.SubMeshes[0];
                    for (int d = 0; d < sub.BoneIndicesList.Count && d < sub.DrawCallRanges.Count; d++)
                    {
                        ushort[] boneArr = sub.BoneIndicesList[d];
                        (int start, int count) = sub.DrawCallRanges[d];
                        var seen = new HashSet<int>();
                        for (int i = start; i < start + count && i < lm.IndexData.Length; i++)
                        {
                            int v = lm.IndexData[i];
                            if (!seen.Add(v)) continue;
                            for (int c = 0; c < 4; c++)
                            {
                                int local = blendIndices[v][c];
                                if (local < boneArr.Length) globalJoints[v, c] = boneArr[local];
                            }
                        }
                    }
                }

                var weights = new BoneWeight[lm.VertexCount];
                for (int v = 0; v < lm.VertexCount; v++)
                {
                    weights[v] = new BoneWeight
                    {
                        boneIndex0 = globalJoints[v, 0], weight0 = blendWeights[v].x,
                        boneIndex1 = globalJoints[v, 1], weight1 = blendWeights[v].y,
                        boneIndex2 = globalJoints[v, 2], weight2 = blendWeights[v].z,
                        boneIndex3 = globalJoints[v, 3], weight3 = blendWeights[v].w,
                    };
                }
                mesh.boneWeights = weights;

                var bindposes = new Matrix4x4[lm.InverseBindPoses.Count];
                for (int i = 0; i < bindposes.Length; i++)
                {
                    bindposes[i] = mirrorX ? MirrorMatrix(lm.InverseBindPoses[i]) : lm.InverseBindPoses[i];
                }
                mesh.bindposes = bindposes;
                r.Log.AppendLine($"④ Mesh: 蒙皮顶点 {lm.VertexCount},绑定姿势 {bindposes.Length}");
            }
            else
            {
                r.Log.AppendLine("④ Mesh: 无蒙皮(静态模型)");
            }

            if (normals == null) mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material BuildMaterial(LhDocument lh, string modelName, string outDir,
            MaterialMode materialMode, Result r)
        {
            Shader shader = materialMode == MaterialMode.Unlit
                ? Shader.Find("Universal Render Pipeline/Unlit")
                : (Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Universal Render Pipeline/Lit"));
            var mat = new Material(shader) { name = modelName + "_mat" };
            // 对标 Laya 布料材质:双面渲染(裙摆/袖子是单层面片,单面剔除会"看穿"衣服内侧)
            mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            mat.doubleSidedGI = true;
            r.Log.AppendLine("⑤ 材质: " + (materialMode == MaterialMode.Unlit ? "URP Unlit(直出)" : "URP SimpleLit"));

            if (lh.MaterialPaths.Count > 0)
            {
                string lmatPath = lh.ResolveAssetPath(lh.MaterialPaths[0]);
                if (lmatPath != null)
                {
                    List<string> textures = LhDocument.ExtractTextures(lmatPath);
                    r.Log.AppendLine($"   {Path.GetFileName(lmatPath)} 贴图 {textures.Count} 张");
                    if (textures.Count > 0)
                    {
                        string texSrc = textures[0];
                        string texDst = outDir + "/" + Path.GetFileName(texSrc);
                        if (!File.Exists(texDst)) File.Copy(texSrc, texDst, true);
                        AssetDatabase.ImportAsset(texDst);
                        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDst);
                        mat.SetTexture("_BaseMap", tex);
                    }
                }
                else
                {
                    r.Log.AppendLine("   ⚠ .lmat 未找到 " + lh.MaterialPaths[0]);
                }
            }
            string matAsset = outDir + "/" + modelName + ".mat";
            AssetDatabase.CreateAsset(mat, matAsset);
            return AssetDatabase.LoadAssetAtPath<Material>(matAsset);
        }

        /// <summary>源约定 .../object/{module}/objs/x.lh → module;不符合约定回退 role。</summary>
        private static string DeriveModule(string lhPath)
        {
            var m = System.Text.RegularExpressions.Regex.Match(
                lhPath.Replace('\\', '/'), "/object/([^/]+)/objs/");
            return m.Success ? m.Groups[1].Value : "role";
        }

        /// <summary>
        /// 动作名:文件约定 "{动作名}-{动作名}.lani"(stand-stand、skill4_2-skill4_2),取首个 '-' 前段。
        /// 老客户端按动作名播放(CommonPlayAnim(animator,"stand")),用文件名保证确定性与增量比对。
        /// </summary>
        private static string ClipNameOf(string laniPath)
        {
            string baseName = Path.GetFileNameWithoutExtension(laniPath);
            int dash = baseName.IndexOf('-');
            return dash > 0 ? baseName.Substring(0, dash) : baseName;
        }

        /// <summary>
        /// clip 资产共享在 Assets/GameRes/object/{module}/action/{动作目录名}/(同职业多模型共用,
        /// 对标老客户端 action/{career*1000+100} 共用约定);已存在且比 .lani 新则直接复用。
        /// </summary>
        private static AnimationClip BuildOrReuseClip(string laniPath, bool mirrorX, string module, Transform root, Result r)
        {
            string clipName = ClipNameOf(laniPath);
            string clipDir = $"Assets/GameRes/object/{module}/action/{Path.GetFileName(Path.GetDirectoryName(laniPath))}";
            string clipAsset = clipDir + "/" + clipName + ".anim";
            string clipAbs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", clipAsset));
            if (File.Exists(clipAbs) && File.GetLastWriteTimeUtc(clipAbs) >= File.GetLastWriteTimeUtc(laniPath))
            {
                var cached = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipAsset);
                if (cached != null)
                {
                    r.Log.AppendLine($"⑥ 动画 {clipName}: 复用已有 clip");
                    return cached;
                }
            }
            Directory.CreateDirectory(clipDir);
            AnimationClip clip = BuildClip(laniPath, mirrorX, root, r);
            if (clip == null) return null;
            AssetDatabase.CreateAsset(clip, clipAsset);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(clipAsset);
        }

        private static AnimationClip BuildClip(string laniPath, bool mirrorX, Transform root, Result r)
        {
            LaniClip lani = LaniParser.Parse(File.ReadAllBytes(laniPath));
            var clip = new AnimationClip
            {
                name = ClipNameOf(laniPath),
                legacy = true,
                frameRate = lani.FrameRate,
                wrapMode = lani.IsLooping ? WrapMode.Loop : WrapMode.Once,
            };

            int missingPaths = 0;
            foreach (LaniNode node in lani.Nodes)
            {
                string path = node.Path;
                if (root.Find(path) == null) missingPaths++;

                string prop = node.PropertyName;
                if (node.Type == 2 && prop == "localRotation")
                {
                    SetCurves(clip, path, "localRotation", node, 4, mirrorX ? new[] { 1f, -1f, -1f, 1f } : null);
                }
                else if ((node.Type == 1 || node.Type == 3 || node.Type == 4) && prop == "localPosition")
                {
                    SetCurves(clip, path, "localPosition", node, 3, mirrorX ? new[] { -1f, 1f, 1f } : null);
                }
                else if ((node.Type == 1 || node.Type == 3 || node.Type == 4) && prop == "localScale")
                {
                    SetCurves(clip, path, "localScale", node, 3, null);
                }
                // 其他属性轨(材质/可见性等)MVP 跳过
            }
            clip.EnsureQuaternionContinuity();
            r.Log.AppendLine($"⑥ 动画 {clip.name}: 时长 {lani.Duration:0.##}s 轨 {lani.Nodes.Count}" +
                             (missingPaths > 0 ? $" ⚠ {missingPaths} 条轨的骨骼路径在层级中未找到" : ""));
            return clip;
        }

        private static readonly string[] AXIS = { "x", "y", "z", "w" };

        private static void SetCurves(AnimationClip clip, string path, string property,
            LaniNode node, int components, float[] sign)
        {
            for (int c = 0; c < components; c++)
            {
                float s = sign != null ? sign[c] : 1f;
                var keys = new Keyframe[node.Keyframes.Count];
                for (int k = 0; k < keys.Length; k++)
                {
                    LaniKeyframe kf = node.Keyframes[k];
                    keys[k] = new Keyframe(kf.Time, kf.Value[c] * s, kf.InTangent[c] * s, kf.OutTangent[c] * s);
                }
                clip.SetCurve(path, typeof(Transform), property + "." + AXIS[c], new AnimationCurve(keys));
            }
        }

        private static Vector3 Pos(float[] p, bool mirrorX)
        {
            return new Vector3(mirrorX ? -p[0] : p[0], p[1], p[2]);
        }

        private static Quaternion Rot(float[] q, bool mirrorX)
        {
            return mirrorX ? new Quaternion(q[0], -q[1], -q[2], q[3]) : new Quaternion(q[0], q[1], q[2], q[3]);
        }

        private static Matrix4x4 MirrorMatrix(Matrix4x4 m)
        {
            // S·M·S,S = diag(-1,1,1,1)
            Matrix4x4 s = Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));
            return s * m * s;
        }
    }
}
