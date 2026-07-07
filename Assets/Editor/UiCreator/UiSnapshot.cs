using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator
{
    /// <summary>
    /// 老客户端【运行时快照】读取器(output/manual_round/*.json / Tools/ModuleManifest/snapshots/*.json),
    /// 给各 *Creator 当"1:1 几何事实源"用。
    ///
    /// 动机(2026-07-06 教训):.scene 设计值 ≠ 运行时结算值——HBox/centerX/centerY/缩放/代码改位
    /// 都在运行时才折算(例:SettingView 的 _btn_copy 设计 142×45 pill,运行时是 33×33 圆钮)。
    /// 手抄设计值建出来的 prefab 必然跑偏;快照里是 Laya 已结算的最终布局,照抄即 1:1。
    ///
    /// 坐标约定:节点的 gx/gy 是【全局左上角】,对 anchorX/pivot 语义免疫;
    /// 求"相对某祖先的局部左上角"一律用 gx/gy 差值(LocalTo),不要用 x/y(x/y 是 Laya 锚点坐标,
    /// 遇 anchorX=1/centerX 会错位)。w/h 是结算尺寸(含缩放的例外:负缩放节点 w/h 不可信,自行兜底)。
    ///
    /// 结构约定:快照是 DFS 扁平数组 + depth/child 字段;【不可见节点不入快照】(隐藏页/门禁块要用
    /// 设计值兜底),且中间层可能被略过(depth 跳档)——重建树时挂到最近的浅层祖先,gx/gy 差值仍正确。
    /// </summary>
    public sealed class UiSnapshot
    {
        public sealed class Node
        {
            public string Name;
            public string Cls;
            public float X, Y;    // Laya 锚点坐标(慎用,见类注释)
            public float Gx, Gy;  // 全局左上角
            public float W, H;
            public string Text;
            public string Skin;
            public Node Parent;
            public readonly List<Node> Children = new List<Node>();

            /// <summary>相对某祖先节点的局部左上角(gx/gy 差值,对锚点语义免疫)。</summary>
            public Vector2 LocalTo(Node ancestor)
            {
                return new Vector2(Gx - ancestor.Gx, Gy - ancestor.Gy);
            }
        }

        private readonly List<Node> _all = new List<Node>();

        public static UiSnapshot Load(string projectRelJsonPath)
        {
            string full = Path.GetFullPath(projectRelJsonPath);
            if (!File.Exists(full))
            {
                Debug.LogWarning("[UiSnapshot] 快照不存在: " + projectRelJsonPath);
                return null;
            }

            var jo = JObject.Parse(File.ReadAllText(full));
            if (!(jo["nodes"] is JArray nodes)) return null;

            var snap = new UiSnapshot();
            var stack = new List<(int depth, Node node)>();
            foreach (JToken t in nodes)
            {
                if (!(t is JObject o)) continue;
                int depth = o.Value<int?>("depth") ?? 0;
                var n = new Node
                {
                    Name = o.Value<string>("name") ?? "",
                    Cls = o.Value<string>("cls") ?? "",
                    X = o.Value<float?>("x") ?? 0f,
                    Y = o.Value<float?>("y") ?? 0f,
                    Gx = o.Value<float?>("gx") ?? 0f,
                    Gy = o.Value<float?>("gy") ?? 0f,
                    W = o.Value<float?>("w") ?? 0f,
                    H = o.Value<float?>("h") ?? 0f,
                    Text = o.Value<string>("text") ?? "",
                    Skin = o.Value<string>("skin") ?? "",
                };

                // 挂到最近的浅层祖先(容忍 depth 跳档:被略过的中间层不影响 gx/gy 差值)。
                while (stack.Count > 0 && stack[stack.Count - 1].depth >= depth) stack.RemoveAt(stack.Count - 1);
                if (stack.Count > 0)
                {
                    n.Parent = stack[stack.Count - 1].node;
                    n.Parent.Children.Add(n);
                }
                stack.Add((depth, n));
                snap._all.Add(n);
            }
            return snap;
        }

        /// <summary>全树按名找第一个(快照里业务节点名基本唯一;克隆件如 View_2/item0 不要用名查)。</summary>
        public Node Find(string name)
        {
            foreach (Node n in _all)
            {
                if (n.Name == name) return n;
            }
            return null;
        }

        /// <summary>在某子树范围内按名找第一个。</summary>
        public static Node FindIn(Node root, string name)
        {
            if (root == null) return null;
            if (root.Name == name) return root;
            foreach (Node c in root.Children)
            {
                Node hit = FindIn(c, name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
