using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Shenxiao.Editor.Laya3D;
using Shenxiao.Editor.LayaUI;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.DynamicResources
{
    public static class UIDynamicResourceRuntimeReset
    {
        private const string ConfigPath = "Schemas/DynamicResources/ui_dynamic_resources.json";

        [MenuItem("神霄/资源/重置 UI 动态资源 Runtime Editable", priority = 25)]
        public static void ResetFromMenu()
        {
            if (!LayaUISettings.ValidateClientRoot(out string error))
            {
                EditorUtility.DisplayDialog("UI 动态资源", error, "好");
                return;
            }

            List<string> lhPaths = CollectEffectSourcePaths();
            if (lhPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("UI 动态资源", "没有找到可重置的 UI 特效 Slot。", "好");
                return;
            }

            bool ok = EditorUtility.DisplayDialog(
                "重置 UI 动态资源 Runtime Editable",
                "将覆盖配置里 UI 动态特效对应的 Runtime Editable Asset。\n\n" +
                string.Join("\n", lhPaths.Select(p => Path.GetFileName(p))) +
                "\n\n这只用于明确刷新运行时可编辑层;普通转换仍会保留人工修改。",
                "重置",
                "取消");
            if (!ok) return;

            int success = 0;
            foreach (string lhPath in lhPaths)
            {
                LayaEffectImporter.Result result = LayaEffectImporter.ResetRuntimeEditable(lhPath);
                if (result.Ok) success++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("UI 动态资源", "重置完成: " + success + "/" + lhPaths.Count, "好");
        }

        private static List<string> CollectEffectSourcePaths()
        {
            var result = new List<string>();
            if (!File.Exists(ConfigPath))
            {
                Debug.LogWarning("[UIDynamicResourceRuntimeReset] config missing: " + ConfigPath);
                return result;
            }

            JObject root = JObject.Parse(File.ReadAllText(ConfigPath));
            JArray entries = root["entries"] as JArray;
            if (entries == null) return result;

            var seen = new HashSet<string>();
            foreach (JObject entry in entries.OfType<JObject>())
            {
                JArray slots = entry["slots"] as JArray;
                if (slots == null) continue;
                foreach (JObject slot in slots.OfType<JObject>())
                {
                    if ((slot.Value<string>("type") ?? "") != "ui_effect") continue;
                    string lhPath = ResolveEffectLhPath(slot);
                    if (string.IsNullOrEmpty(lhPath) || seen.Contains(lhPath)) continue;
                    seen.Add(lhPath);
                    if (File.Exists(lhPath)) result.Add(lhPath);
                    else Debug.LogWarning("[UIDynamicResourceRuntimeReset] source missing: " + lhPath);
                }
            }
            return result;
        }

        private static string ResolveEffectLhPath(JObject slot)
        {
            string effectName = slot.Value<string>("effectName") ?? "";
            string addressKey = (slot.Value<string>("addressKey") ?? "").Replace('\\', '/');
            string effectDir = "ui_effect";
            if (!string.IsNullOrEmpty(addressKey))
            {
                string[] parts = addressKey.Split('/');
                if (parts.Length >= 3 && parts[0] == "effect" && parts[1] == "objs")
                {
                    effectDir = parts[2];
                    if (string.IsNullOrEmpty(effectName)) effectName = parts[parts.Length - 1];
                }
            }
            if (string.IsNullOrEmpty(effectName)) return "";
            return Path.Combine(LayaUISettings.CdnResourceRoot, "effect", "objs", effectDir, effectName + ".lh");
        }
    }
}
