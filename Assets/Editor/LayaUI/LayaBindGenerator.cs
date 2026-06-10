using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// 为转换出来的 View prefab 生成 {Name}Bind.cs(Assets/Scripts/Generated/UI/{Module}/)。
    /// 收集规则:节点名以 "_" 开头的(Laya 项目约定的代码引用节点)+ __Templates 下的模板
    /// (字段名 _tpl_xxx)。字段类型按节点组件取最具体的。
    /// 生成后需要 Unity 编译一轮,再跑『回填 Bind 引用』把字段填进 prefab。
    /// </summary>
    public static class LayaBindGenerator
    {
        public static void Generate(LayaUIManifest.SceneEntry entry, LayaUIManifest manifest, string prefabPath, LayaUIReport report)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            List<FieldInfo> fields = new List<FieldInfo>();
            HashSet<string> used = new HashSet<string>();
            Collect(prefab.transform, prefab.transform, fields, used, report, entry.Name);

            string moduleDir = manifest.ModuleDir(entry.Module);
            string className = SanitizeType(entry.Name) + "Bind";
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// 由 LayaUI 转换器自动生成,不要手改。重转会覆盖。");
            sb.AppendLine("// 来源: " + entry.Json);
            sb.AppendLine("using TMPro;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UI;");
            sb.AppendLine("using Shenxiao.Framework.UI;");
            sb.AppendLine();
            sb.AppendLine("namespace Shenxiao.Generated.UI." + moduleDir);
            sb.AppendLine("{");
            sb.AppendLine("    public partial class " + className + " : BaseView");
            sb.AppendLine("    {");
            foreach (FieldInfo f in fields)
            {
                sb.AppendLine("        public " + f.TypeName + " " + f.FieldName + ";");
            }
            sb.AppendLine();
            sb.AppendLine("        protected override void BindNodes()");
            sb.AppendLine("        {");
            foreach (FieldInfo f in fields)
            {
                sb.AppendLine("            EnsureBound(nameof(" + f.FieldName + "), " + f.FieldName + ");");
            }
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            string dir = LayaUISettings.BIND_ROOT + "/" + moduleDir;
            Directory.CreateDirectory(dir);
            File.WriteAllText(dir + "/" + className + ".cs", sb.ToString());
        }

        public struct FieldInfo
        {
            public string FieldName;
            public string TypeName;
            public string NodePath; // 回填用:prefab 内相对路径
        }

        public static void Collect(Transform root, Transform node, List<FieldInfo> fields, HashSet<string> used,
            LayaUIReport report, string sceneName)
        {
            for (int i = 0; i < node.childCount; i++)
            {
                Transform c = node.GetChild(i);
                bool isTplRoot = c.name == "__Templates";
                if (isTplRoot)
                {
                    for (int j = 0; j < c.childCount; j++)
                    {
                        Transform t = c.GetChild(j);
                        AddField(fields, used, "_tpl_" + SanitizeField(t.name), "GameObject", Path(root, t), report, sceneName);
                    }
                    continue; // 模板内部节点不进 Bind
                }
                if (c.name.StartsWith("_"))
                {
                    AddField(fields, used, SanitizeField(c.name), TypeOf(c), Path(root, c), report, sceneName);
                }
                Collect(root, c, fields, used, report, sceneName);
            }
        }

        private static void AddField(List<FieldInfo> fields, HashSet<string> used, string fieldName, string typeName,
            string path, LayaUIReport report, string sceneName)
        {
            if (!used.Add(fieldName))
            {
                report.Note(sceneName + " 节点名重复,跳过 Bind 字段: " + fieldName + " (" + path + ")");
                return;
            }
            FieldInfo f;
            f.FieldName = fieldName;
            f.TypeName = typeName;
            f.NodePath = path;
            fields.Add(f);
        }

        public static string TypeOf(Transform t)
        {
            if (t.GetComponent<TMP_InputField>() != null) return "TMP_InputField";
            if (t.GetComponent<TextMeshProUGUI>() != null) return "TextMeshProUGUI";
            if (t.GetComponent<ScrollRect>() != null) return "ScrollRect";
            if (t.GetComponent<Image>() != null) return "Image";
            return "RectTransform";
        }

        public static string Path(Transform root, Transform t)
        {
            List<string> parts = new List<string>();
            while (t != null && t != root)
            {
                parts.Insert(0, t.name);
                t = t.parent;
            }
            return string.Join("/", parts.ToArray());
        }

        private static string SanitizeField(string name)
        {
            string s = Regex.Replace(name, @"[^\w]", "_");
            if (s.Length > 0 && char.IsDigit(s[0])) s = "_" + s;
            return s;
        }

        private static string SanitizeType(string name)
        {
            string s = Regex.Replace(name, @"[^\w]", "");
            if (s.Length == 0) s = "View";
            if (char.IsDigit(s[0])) s = "V" + s;
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
