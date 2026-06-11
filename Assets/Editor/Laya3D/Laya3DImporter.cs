using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.Laya3D
{
    /// <summary>
    /// Laya 3D → Unity 原生资产装配器(MVP):
    /// .lh(层级/引用)+ .lm(网格/蒙皮)+ .lmat(贴图)+ .lani(动画)
    /// → Mesh.asset + .anim(Legacy)+ .mat(URP)+ .prefab,产物进 GameRes。
    /// 解析逻辑 1:1 对标 Electron 工具 python 参考实现(已被 3D 预览渲染验证)。
    /// mirrorX:坐标系翻转开关(若首渲左右镜像/法线翻面,切换重转,MVP 验收期定死后固化)。
    /// </summary>
    public static class Laya3DImporter
    {
        public sealed class Result
        {
            public bool Ok;
            public string PrefabPath = "";
            public readonly StringBuilder Log = new StringBuilder();
        }

        public static Result Convert(string lhPath, List<string> laniPaths, bool mirrorX)
        {
            var r = new Result();
            try
            {
                ConvertInner(lhPath, laniPaths ?? new List<string>(), mirrorX, r);
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

        private static void ConvertInner(string lhPath, List<string> laniPaths, bool mirrorX, Result r)
        {
            string modelName = Path.GetFileNameWithoutExtension(lhPath);
            string outDir = "Assets/GameRes/object/role/" + modelName;
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
            Material mat = BuildMaterial(lh, modelName, outDir, r);

            // ---------- ⑥ SkinnedMeshRenderer ----------
            var meshGo = new GameObject(lm.Name == "" ? "mesh" : lm.Name);
            meshGo.transform.SetParent(rootGo.transform, false);
            if (lh.MeshNodeRotation != null)
            {
                meshGo.transform.localRotation = Rot(lh.MeshNodeRotation, mirrorX);
            }
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

            // ---------- ⑦ 动画(Legacy) ----------
            var clips = new List<AnimationClip>();
            foreach (string laniPath in laniPaths)
            {
                AnimationClip clip = BuildClip(laniPath, mirrorX, rootGo.transform, r);
                if (clip != null)
                {
                    string clipAsset = outDir + "/" + clip.name + ".anim";
                    AssetDatabase.CreateAsset(clip, clipAsset);
                    clips.Add(AssetDatabase.LoadAssetAtPath<AnimationClip>(clipAsset));
                }
            }
            if (clips.Count > 0)
            {
                var anim = rootGo.AddComponent<Animation>();
                AnimationUtility.SetAnimationClips(anim, clips.ToArray());
                anim.clip = clips[0];
                anim.playAutomatically = true;
                r.Log.AppendLine($"⑦ 动画 {clips.Count} 个,默认自动播放: {clips[0].name}");
            }

            // ---------- ⑧ Prefab ----------
            string prefabPath = outDir + "/" + modelName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(rootGo, prefabPath);
            UnityEngine.Object.DestroyImmediate(rootGo);
            AssetDatabase.SaveAssets();
            r.PrefabPath = prefabPath;
            r.Log.AppendLine("✅ 完成: " + prefabPath);
        }

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

        private static Material BuildMaterial(LhDocument lh, string modelName, string outDir, Result r)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit")
                            ?? Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader) { name = modelName + "_mat" };

            if (lh.MaterialPaths.Count > 0)
            {
                string lmatPath = lh.ResolveAssetPath(lh.MaterialPaths[0]);
                if (lmatPath != null)
                {
                    List<string> textures = LhDocument.ExtractTextures(lmatPath);
                    r.Log.AppendLine($"⑤ 材质: {Path.GetFileName(lmatPath)},贴图 {textures.Count} 张");
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
                    r.Log.AppendLine("⑤ 材质: .lmat 未找到 " + lh.MaterialPaths[0]);
                }
            }
            string matAsset = outDir + "/" + modelName + ".mat";
            AssetDatabase.CreateAsset(mat, matAsset);
            return AssetDatabase.LoadAssetAtPath<Material>(matAsset);
        }

        private static AnimationClip BuildClip(string laniPath, bool mirrorX, Transform root, Result r)
        {
            LaniClip lani = LaniParser.Parse(File.ReadAllBytes(laniPath));
            var clip = new AnimationClip
            {
                name = string.IsNullOrEmpty(lani.Name) ? Path.GetFileNameWithoutExtension(laniPath) : lani.Name,
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
