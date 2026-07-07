using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.UI3D;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.DynamicResources
{
    public static class UIDynamicResourceSlotFiller
    {
        private const string ConfigPath = "Schemas/DynamicResources/ui_dynamic_resources.json";
        private const string SlotContainerName = "__DynamicResources";

        [MenuItem("神霄/资源/回填 UI 动态资源 Slot", priority = 24)]
        public static void FillAllFromMenu()
        {
            int expected = CountConfiguredSlots(null);
            int count = FillModules(null);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("UI 动态资源 Slot", "回填完成: " + count + "/" + expected + " 个 slot。", "好");
        }

        public static int FillModules(IEnumerable<string> modules)
        {
            if (!File.Exists(ConfigPath))
            {
                Debug.LogWarning("[UIDynamicResourceSlot] config missing: " + ConfigPath);
                return 0;
            }

            HashSet<string> moduleSet = modules == null
                ? null
                : new HashSet<string>(modules.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()));

            JObject root = JObject.Parse(File.ReadAllText(ConfigPath));
            JArray entries = root["entries"] as JArray;
            if (entries == null) return 0;

            int total = 0;
            foreach (JObject entry in entries.OfType<JObject>())
            {
                string module = entry.Value<string>("module") ?? "";
                if (moduleSet != null && !moduleSet.Contains(module)) continue;
                total += ApplyEntry(entry);
            }
            return total;
        }

        private static int CountConfiguredSlots(IEnumerable<string> modules)
        {
            if (!File.Exists(ConfigPath)) return 0;

            HashSet<string> moduleSet = modules == null
                ? null
                : new HashSet<string>(modules.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()));

            JObject root = JObject.Parse(File.ReadAllText(ConfigPath));
            JArray entries = root["entries"] as JArray;
            if (entries == null) return 0;

            int total = 0;
            foreach (JObject entry in entries.OfType<JObject>())
            {
                string module = entry.Value<string>("module") ?? "";
                if (moduleSet != null && !moduleSet.Contains(module)) continue;
                if (entry["slots"] is JArray slots) total += slots.Count;
            }
            return total;
        }

        private static int ApplyEntry(JObject entry)
        {
            string prefabPath = entry.Value<string>("prefab");
            if (string.IsNullOrEmpty(prefabPath) || !File.Exists(prefabPath))
            {
                Debug.LogWarning("[UIDynamicResourceSlot] prefab missing: " + prefabPath);
                return 0;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            int count = 0;
            try
            {
                List<Transform> hosts = FindHosts(root.transform, entry);
                if (hosts.Count == 0)
                {
                    Debug.LogWarning("[UIDynamicResourceSlot] host missing in " + prefabPath);
                    return 0;
                }

                foreach (Transform host in hosts)
                {
                    count += ApplySlots(host, entry);
                }

                if (count > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    Debug.Log("[UIDynamicResourceSlot] " + prefabPath + " slots=" + count);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return count;
        }

        private static List<Transform> FindHosts(Transform root, JObject entry)
        {
            List<Transform> boundHosts = FindHostsByBoundField(root, entry);
            if (boundHosts.Count > 0) return boundHosts;

            string hostPath = entry.Value<string>("hostPath");
            if (!string.IsNullOrEmpty(hostPath))
            {
                Transform direct = root.Find(hostPath);
                return direct != null ? new List<Transform> { direct } : new List<Transform>();
            }

            string hostName = entry.Value<string>("hostName");
            string ancestorName = entry.Value<string>("ancestorName");
            if (string.IsNullOrEmpty(hostName)) return new List<Transform>();

            var result = new List<Transform>();
            foreach (Transform t in Enumerate(root))
            {
                if (t.name != hostName) continue;
                if (!string.IsNullOrEmpty(ancestorName) && !HasAncestor(t, ancestorName)) continue;
                result.Add(t);
            }
            return result;
        }

        private static List<Transform> FindHostsByBoundField(Transform root, JObject entry)
        {
            string ownerComponent = entry.Value<string>("ownerComponent");
            string bindField = entry.Value<string>("bindField");
            if (string.IsNullOrEmpty(ownerComponent) || string.IsNullOrEmpty(bindField))
                return new List<Transform>();

            var result = new List<Transform>();
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null) continue;
                System.Type type = behaviour.GetType();
                if (type.FullName != ownerComponent && type.Name != ownerComponent) continue;

                SerializedObject so = new SerializedObject(behaviour);
                SerializedProperty prop = so.FindProperty(bindField);
                if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference)
                {
                    Debug.LogWarning("[UIDynamicResourceSlot] bind field missing: " + ownerComponent + "." + bindField);
                    continue;
                }

                Transform host = ResolveTransform(prop.objectReferenceValue);
                if (host != null && !result.Contains(host)) result.Add(host);
            }

            if (result.Count == 0)
            {
                Debug.LogWarning("[UIDynamicResourceSlot] bound host missing: " + ownerComponent + "." + bindField);
            }
            return result;
        }

        private static Transform ResolveTransform(Object reference)
        {
            if (reference == null) return null;
            if (reference is Transform transform) return transform;
            if (reference is Component component) return component.transform;
            if (reference is GameObject go) return go.transform;
            return null;
        }

        private static int ApplySlots(Transform host, JObject entry)
        {
            JArray slots = entry["slots"] as JArray;
            if (slots == null) return 0;

            int count = 0;
            string source = entry.Value<string>("source") ?? "";
            Transform container = EnsureSlotContainer(host);
            foreach (JObject slot in slots.OfType<JObject>())
            {
                string type = slot.Value<string>("type") ?? "";
                if (type != "ui_effect")
                {
                    Debug.LogWarning("[UIDynamicResourceSlot] unsupported slot type: " + type);
                    continue;
                }

                string slotId = slot.Value<string>("slotId") ?? "";
                UIEffectSlot effectSlot = FindEffectSlot(container, slotId);
                if (effectSlot == null)
                {
                    GameObject slotGo = new GameObject(slotId, typeof(RectTransform));
                    slotGo.transform.SetParent(container, false);
                    effectSlot = slotGo.AddComponent<UIEffectSlot>();
                }

                Vector2 position = ReadVector2(slot["position"] as JArray, Vector2.zero);
                effectSlot.ConfigureEffect(
                    slotId,
                    slot.Value<string>("effectName") ?? "",
                    slot.Value<string>("addressKey") ?? "",
                    source,
                    slot.Value<string>("note") ?? "",
                    position,
                    ReadVector3(slot["scale"] as JArray, Vector3.one),
                    slot.Value<float?>("rotationY") ?? 0f,
                    slot.Value<bool?>("autoPlay") ?? false);
                ConfigureSlotTransform(effectSlot.transform as RectTransform, position);
                EditorUtility.SetDirty(effectSlot);
                count++;
            }
            RemoveLegacyHostSlots(host);
            return count;
        }

        private static UIEffectSlot FindEffectSlot(Transform host, string slotId)
        {
            UIEffectSlot[] slots = host.GetComponentsInChildren<UIEffectSlot>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].SlotId == slotId) return slots[i];
            }
            return null;
        }

        private static Transform EnsureSlotContainer(Transform host)
        {
            Transform existing = host.Find(SlotContainerName);
            if (existing != null) return existing;

            GameObject go = new GameObject(SlotContainerName, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(host, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            return rt;
        }

        private static void ConfigureSlotTransform(RectTransform rt, Vector2 position)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(32f, 32f);
            rt.localScale = Vector3.one;
        }

        private static void RemoveLegacyHostSlots(Transform host)
        {
            UIEffectSlot[] legacySlots = host.GetComponents<UIEffectSlot>();
            for (int i = 0; i < legacySlots.Length; i++)
            {
                if (legacySlots[i] != null) Object.DestroyImmediate(legacySlots[i], true);
            }
        }

        private static IEnumerable<Transform> Enumerate(Transform root)
        {
            yield return root;
            for (int i = 0; i < root.childCount; i++)
            {
                foreach (Transform child in Enumerate(root.GetChild(i)))
                    yield return child;
            }
        }

        private static bool HasAncestor(Transform t, string name)
        {
            Transform p = t.parent;
            while (p != null)
            {
                if (p.name == name) return true;
                p = p.parent;
            }
            return false;
        }

        private static Vector2 ReadVector2(JArray arr, Vector2 fallback)
        {
            if (arr == null || arr.Count < 2) return fallback;
            return new Vector2((float)arr[0], (float)arr[1]);
        }

        private static Vector3 ReadVector3(JArray arr, Vector3 fallback)
        {
            if (arr == null || arr.Count < 3) return fallback;
            return new Vector3((float)arr[0], (float)arr[1], (float)arr[2]);
        }
    }
}
