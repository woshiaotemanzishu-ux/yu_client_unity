using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.EditorTools.ConfigGen
{
    /// <summary>
    /// Reads JSON schemas under {ProjectRoot}/Schemas/configs and generates:
    /// - Assets/Scripts/Generated/Config/{Name}Cfg.cs
    /// - Assets/Scripts/Generated/Config/Config{Name}.cs
    /// </summary>
    public static class ConfigGenerator
    {
        private const string SchemaDir = "Schemas/configs";
        private const string OutputDir = "Assets/Scripts/Generated/Config";
        private const string DefaultSourcePrefix = "resource/config/server/";

        [MenuItem("神霄/配表/全部生成", priority = 20)]
        public static void GenerateAll()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var schemaRoot = Path.Combine(projectRoot, SchemaDir);
            if (!Directory.Exists(schemaRoot))
            {
                Directory.CreateDirectory(schemaRoot);
                Debug.LogWarning($"[ConfigGenerator] Created empty schema dir: {schemaRoot}. Add .schema.json files and rerun.");
                return;
            }

            var outputAbs = Path.Combine(projectRoot, OutputDir);
            Directory.CreateDirectory(outputAbs);

            var files = Directory.GetFiles(schemaRoot, "*.schema.json", SearchOption.AllDirectories);
            int ok = 0, fail = 0;
            foreach (var file in files)
            {
                try
                {
                    var schema = LoadSchema(file);
                    GenerateForSchema(schema, outputAbs);
                    ok++;
                }
                catch (Exception e)
                {
                    fail++;
                    Debug.LogError($"[ConfigGenerator] {Path.GetFileName(file)} failed: {e.Message}\n{e.StackTrace}");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[ConfigGenerator] Done. ok={ok}, fail={fail}, total={files.Length}");
        }

        [MenuItem("神霄/配表/打开 Schema 目录", priority = 21)]
        public static void OpenSchemaFolder()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var schemaRoot = Path.Combine(projectRoot, SchemaDir);
            Directory.CreateDirectory(schemaRoot);
            EditorUtility.RevealInFinder(schemaRoot);
        }

        private static ConfigSchema LoadSchema(string path)
        {
            var text = File.ReadAllText(path, Encoding.UTF8);
            var schema = JsonConvert.DeserializeObject<ConfigSchema>(text);
            if (schema == null) throw new Exception("schema is null");
            if (string.IsNullOrEmpty(schema.table)) throw new Exception("missing 'table'");
            if (string.IsNullOrEmpty(schema.key_field)) throw new Exception("missing 'key_field'");
            if (string.IsNullOrEmpty(schema.key_type)) schema.key_type = "int";
            if (string.IsNullOrEmpty(schema.source)) schema.source = DefaultSourcePrefix + schema.table;
            if (string.IsNullOrEmpty(schema.vo_name)) schema.vo_name = DeriveVoName(schema.table);
            return schema;
        }

        private static string DeriveVoName(string table)
        {
            // config_skill -> SkillCfg ; chrono_rift_castle_kv -> ChronoRiftCastleKvCfg
            var t = table.StartsWith("config_") ? table.Substring("config_".Length) : table;
            var parts = t.Split('_');
            var sb = new StringBuilder();
            foreach (var p in parts)
            {
                if (p.Length == 0) continue;
                sb.Append(char.ToUpperInvariant(p[0]));
                sb.Append(p.Substring(1));
            }
            sb.Append("Cfg");
            return sb.ToString();
        }

        private static string DeriveConfigName(string table)
        {
            var t = table.StartsWith("config_") ? table.Substring("config_".Length) : table;
            var parts = t.Split('_');
            var sb = new StringBuilder("Config");
            foreach (var p in parts)
            {
                if (p.Length == 0) continue;
                sb.Append(char.ToUpperInvariant(p[0]));
                sb.Append(p.Substring(1));
            }
            return sb.ToString();
        }

        private static void GenerateForSchema(ConfigSchema schema, string outputAbs)
        {
            var voName = schema.vo_name;
            var configName = DeriveConfigName(schema.table);
            var keyCs = MapType(schema.key_type);

            var voCode = BuildVoCode(schema, voName);
            File.WriteAllText(Path.Combine(outputAbs, voName + ".cs"), voCode, new UTF8Encoding(false));

            var configCode = BuildConfigCode(schema, voName, configName, keyCs);
            File.WriteAllText(Path.Combine(outputAbs, configName + ".cs"), configCode, new UTF8Encoding(false));
        }

        private static string BuildVoCode(ConfigSchema schema, string voName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("// Generated by Shenxiao ConfigGenerator. Do not edit by hand.");
            sb.AppendLine($"// Schema: {schema.table}");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using Shenxiao.Framework.Config;");
            sb.AppendLine();
            sb.AppendLine("namespace Shenxiao.Generated.Config");
            sb.AppendLine("{");

            EmitClass(sb, voName, schema.fields, baseClass: "BaseVo", indent: 1, comment: schema.comment);

            if (schema.nested_types != null)
            {
                foreach (var kv in schema.nested_types)
                {
                    sb.AppendLine();
                    EmitClass(sb, kv.Key, kv.Value, baseClass: null, indent: 1, comment: null);
                }
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void EmitClass(StringBuilder sb, string className, List<FieldDef> fields, string baseClass, int indent, string comment)
        {
            var pad = new string(' ', indent * 4);
            if (!string.IsNullOrEmpty(comment))
            {
                sb.Append(pad).AppendLine("/// <summary>" + EscapeXml(comment) + "</summary>");
            }
            sb.Append(pad).AppendLine("[Serializable]");
            sb.Append(pad).Append("public class ").Append(className);
            if (!string.IsNullOrEmpty(baseClass)) sb.Append(" : ").Append(baseClass);
            sb.AppendLine();
            sb.Append(pad).AppendLine("{");
            var inner = pad + "    ";
            foreach (var f in fields ?? new List<FieldDef>())
            {
                if (!string.IsNullOrEmpty(f.comment))
                {
                    sb.Append(inner).AppendLine("/// <summary>" + EscapeXml(f.comment) + "</summary>");
                }
                var cs = MapType(f.type);
                sb.Append(inner).Append("public ").Append(cs).Append(' ').Append(SafeIdent(f.name));
                if (!string.IsNullOrEmpty(f.@default))
                {
                    sb.Append(" = ").Append(FormatDefault(cs, f.@default));
                }
                sb.AppendLine(";");
            }
            sb.Append(pad).AppendLine("}");
        }

        private static string BuildConfigCode(ConfigSchema schema, string voName, string configName, string keyCs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("// Generated by Shenxiao ConfigGenerator. Do not edit by hand.");
            sb.AppendLine($"// Schema: {schema.table}");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using Newtonsoft.Json;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using Shenxiao.Framework.Res;");
            sb.AppendLine("using Shenxiao.Framework.Util;");
            sb.AppendLine();
            sb.AppendLine("namespace Shenxiao.Generated.Config");
            sb.AppendLine("{");
            sb.AppendLine($"    public static class {configName}");
            sb.AppendLine("    {");
            sb.AppendLine($"        public const string AddrKey = \"{schema.source}\";");
            sb.AppendLine($"        private static Dictionary<{keyCs}, {voName}> _data;");
            sb.AppendLine($"        public static bool IsLoaded => _data != null;");
            sb.AppendLine();
            sb.AppendLine("        public static async Task LoadAsync()");
            sb.AppendLine("        {");
            sb.AppendLine("            var asset = await ResManager.LoadAsync<TextAsset>(AddrKey);");
            sb.AppendLine("            if (asset == null)");
            sb.AppendLine("            {");
            sb.AppendLine($"                GameLog.Error(\"Config\", \"missing config asset: {{0}}\", AddrKey);");
            sb.AppendLine($"                _data = new Dictionary<{keyCs}, {voName}>();");
            sb.AppendLine("                return;");
            sb.AppendLine("            }");
            sb.AppendLine($"            _data = JsonConvert.DeserializeObject<Dictionary<{keyCs}, {voName}>>(asset.text)");
            sb.AppendLine($"                    ?? new Dictionary<{keyCs}, {voName}>();");
            sb.AppendLine("            ResManager.Release(asset);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        public static {voName} Get({keyCs} key)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (_data == null) return null;");
            sb.AppendLine("            return _data.TryGetValue(key, out var v) ? v : null;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        public static IEnumerable<{voName}> All()");
            sb.AppendLine("        {");
            sb.AppendLine($"            return _data != null ? _data.Values : System.Linq.Enumerable.Empty<{voName}>();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public static int Count => _data != null ? _data.Count : 0;");
            sb.AppendLine();
            sb.AppendLine("        public static void Unload() { _data = null; }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string MapType(string t)
        {
            if (string.IsNullOrEmpty(t)) return "string";
            if (t.EndsWith("[]"))
            {
                var inner = t.Substring(0, t.Length - 2);
                return MapType(inner) + "[]";
            }
            switch (t)
            {
                case "int": return "int";
                case "long": return "long";
                case "float": return "float";
                case "double": return "double";
                case "bool": return "bool";
                case "string": return "string";
                default: return t; // nested type name passthrough
            }
        }

        private static string FormatDefault(string cs, string raw)
        {
            switch (cs)
            {
                case "string": return "\"" + raw.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
                case "bool": return raw.ToLowerInvariant() == "true" ? "true" : "false";
                case "float": return raw + "f";
                default: return raw;
            }
        }

        private static string SafeIdent(string name)
        {
            // C# keyword guard
            switch (name)
            {
                case "class":
                case "namespace":
                case "default":
                case "event":
                case "string":
                case "int":
                case "long":
                case "float":
                case "double":
                case "bool":
                case "object":
                case "ref":
                case "out":
                case "in":
                case "params":
                case "lock":
                case "new":
                case "base":
                case "this":
                    return "@" + name;
                default:
                    return name;
            }
        }

        private static string EscapeXml(string s)
        {
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
