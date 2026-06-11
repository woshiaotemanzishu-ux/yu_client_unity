using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Shenxiao.Editor.Laya3D
{
    /// <summary>
    /// .lh(JSON 场景/预制体)解析,对标 laya_to_glb.py 的 _extract_mesh_info /
    /// _extract_bone_hierarchy / _resolve_asset_path / _extract_textures_from_lmat。
    /// </summary>
    public sealed class LhBone
    {
        public string Name = "";
        public int ParentIndex = -1;
        public float[] Position = { 0, 0, 0 };
        public float[] Rotation = { 0, 0, 0, 1 };
        public float[] Scale = { 1, 1, 1 };
    }

    public sealed class LhDocument
    {
        public string LhPath = "";
        public string RootName = "";
        public string MeshPath = "";                       // .lm 相对路径
        public readonly List<string> MaterialPaths = new List<string>();   // .lmat 相对路径
        public float[] MeshNodeRotation;                   // 网格节点旋转(可空)
        public readonly List<LhBone> Bones = new List<LhBone>();
        public readonly Dictionary<string, int> BoneNameToIndex = new Dictionary<string, int>();

        public static LhDocument Load(string lhPath)
        {
            var doc = new LhDocument { LhPath = lhPath };
            JObject root = JObject.Parse(File.ReadAllText(lhPath));
            JObject data = root["data"] as JObject ?? root;
            doc.RootName = (string)data["props"]?["name"] ?? Path.GetFileNameWithoutExtension(lhPath);

            ExtractMeshInfo(data, doc);
            ExtractBones(data, doc);
            return doc;
        }

        private static void ExtractMeshInfo(JObject data, LhDocument doc)
        {
            bool found = false;
            void Walk(JObject node)
            {
                if (found) return;
                string type = (string)node["type"] ?? "";
                var props = node["props"] as JObject;
                if (type == "SkinnedMeshSprite3D" || type == "MeshSprite3D")
                {
                    doc.MeshPath = (string)props?["meshPath"] ?? "";
                    if (props?["materials"] is JArray mats)
                    {
                        foreach (JToken m in mats)
                        {
                            string p = (string)m["path"];
                            if (!string.IsNullOrEmpty(p)) doc.MaterialPaths.Add(p);
                        }
                    }
                    if (props?["rotation"] is JArray rot && rot.Count == 4)
                    {
                        doc.MeshNodeRotation = new[] { (float)rot[0], (float)rot[1], (float)rot[2], (float)rot[3] };
                    }
                    found = true;
                    return;
                }
                if (node["child"] is JArray children)
                {
                    foreach (JToken c in children)
                    {
                        Walk((JObject)c);
                        if (found) return;
                    }
                }
            }
            Walk(data);
        }

        private static void ExtractBones(JObject data, LhDocument doc)
        {
            void Collect(JObject node, int parentIdx)
            {
                string type = (string)node["type"] ?? "";
                if (type == "SkinnedMeshSprite3D" || type == "MeshSprite3D") return; // 跳过网格节点

                var props = node["props"] as JObject;
                var bone = new LhBone
                {
                    Name = (string)props?["name"] ?? "",
                    ParentIndex = parentIdx,
                };
                if (props?["position"] is JArray p && p.Count == 3)
                    bone.Position = new[] { (float)p[0], (float)p[1], (float)p[2] };
                if (props?["rotation"] is JArray r && r.Count == 4)
                    bone.Rotation = new[] { (float)r[0], (float)r[1], (float)r[2], (float)r[3] };
                if (props?["scale"] is JArray s && s.Count == 3)
                    bone.Scale = new[] { (float)s[0], (float)s[1], (float)s[2] };

                int idx = doc.Bones.Count;
                if (!doc.BoneNameToIndex.ContainsKey(bone.Name)) doc.BoneNameToIndex[bone.Name] = idx;
                doc.Bones.Add(bone);

                if (node["child"] is JArray children)
                {
                    foreach (JToken c in children) Collect((JObject)c, idx);
                }
            }

            if (data["child"] is JArray rootChildren)
            {
                foreach (JToken child in rootChildren)
                {
                    string type = (string)((JObject)child)["type"] ?? "";
                    if (type == "SkinnedMeshSprite3D" || type == "MeshSprite3D") continue;
                    Collect((JObject)child, -1);
                }
            }
        }

        /// <summary>解析 .lh 引用的资产路径(对标 _resolve_asset_path 的三级策略)。</summary>
        public string ResolveAssetPath(string relPath)
        {
            if (string.IsNullOrEmpty(relPath)) return null;
            string lhDir = Path.GetDirectoryName(LhPath);

            string p = Path.Combine(lhDir, relPath);
            if (File.Exists(p)) return p;

            string parent = Path.GetDirectoryName(lhDir);
            for (int i = 0; i < 2 && parent != null; i++)
            {
                p = Path.Combine(parent, relPath);
                if (File.Exists(p)) return p;
                parent = Path.GetDirectoryName(parent);
            }

            string current = lhDir;
            for (int i = 0; i < 10 && current != null; i++)
            {
                p = Path.Combine(current, "Conventional", relPath);
                if (File.Exists(p)) return p;
                p = Path.Combine(current, relPath);
                if (File.Exists(p)) return p;
                current = Path.GetDirectoryName(current);
            }
            return null;
        }

        /// <summary>.lmat → 贴图绝对路径列表(相对 .lmat 所在目录)。</summary>
        public static List<string> ExtractTextures(string lmatPath)
        {
            var result = new List<string>();
            JObject mat = JObject.Parse(File.ReadAllText(lmatPath));
            string matDir = Path.GetDirectoryName(lmatPath);
            if (mat["props"]?["textures"] is JArray textures)
            {
                foreach (JToken t in textures)
                {
                    string rel = (string)t["path"];
                    if (string.IsNullOrEmpty(rel)) continue;
                    string abs = Path.GetFullPath(Path.Combine(matDir, rel));
                    if (File.Exists(abs)) result.Add(abs);
                }
            }
            return result;
        }
    }
}
