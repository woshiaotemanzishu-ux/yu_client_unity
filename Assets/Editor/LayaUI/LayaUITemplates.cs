using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// LayaUI 模板 prefab:转换器只负责实例化 + 套布局/内容,所有样式(字体、描边、过渡、
    /// raycast 默认值)都在模板上调,改完重转自动继承,不改代码。幂等:已存在的模板不覆盖。
    /// </summary>
    public static class LayaUITemplates
    {
        public static void BuildAll()
        {
            Directory.CreateDirectory(LayaUISettings.TEMPLATE_ROOT);

            BuildIfMissing("Box", () => NewRect("Box"));
            BuildIfMissing("Image", () =>
            {
                GameObject go = NewRect("Image");
                Image img = go.AddComponent<Image>();
                img.raycastTarget = false;
                return go;
            });
            BuildIfMissing("Label", () =>
            {
                GameObject go = NewRect("Label");
                TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
                t.font = LayaUISettings.LoadFont();
                t.fontSize = 24;
                t.color = Color.white;
                t.alignment = TextAlignmentOptions.TopLeft;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.overflowMode = TextOverflowModes.Overflow;
                t.raycastTarget = false;
                return go;
            });
            BuildIfMissing("TextInput", () =>
            {
                GameObject go = NewRect("TextInput");
                Image bg = go.AddComponent<Image>();
                bg.raycastTarget = true;
                TMP_InputField input = go.AddComponent<TMP_InputField>();

                RectTransform area = NewChildRect(go, "Text Area", true);
                area.gameObject.AddComponent<RectMask2D>();

                RectTransform placeholder = NewChildRect(area.gameObject, "Placeholder", true);
                TextMeshProUGUI ph = placeholder.gameObject.AddComponent<TextMeshProUGUI>();
                ph.font = LayaUISettings.LoadFont();
                ph.fontSize = 24;
                ph.color = new Color(0.6f, 0.6f, 0.6f, 0.75f);
                ph.alignment = TextAlignmentOptions.Left;
                ph.raycastTarget = false;

                RectTransform text = NewChildRect(area.gameObject, "Text", true);
                TextMeshProUGUI txt = text.gameObject.AddComponent<TextMeshProUGUI>();
                txt.font = LayaUISettings.LoadFont();
                txt.fontSize = 24;
                txt.color = Color.white;
                txt.alignment = TextAlignmentOptions.Left;
                txt.raycastTarget = false;

                input.textViewport = area;
                input.textComponent = txt;
                input.placeholder = ph;
                return go;
            });
            BuildIfMissing("List", () =>
            {
                GameObject go = NewRect("List");
                ScrollRect sr = go.AddComponent<ScrollRect>();
                RectTransform viewport = NewChildRect(go, "Viewport", true);
                viewport.gameObject.AddComponent<RectMask2D>();
                RectTransform content = NewChildRect(viewport.gameObject, "Content", false);
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(0f, 1f);
                content.pivot = new Vector2(0f, 1f);
                content.anchoredPosition = Vector2.zero;
                sr.viewport = viewport;
                sr.content = content;
                sr.horizontal = false;
                sr.vertical = true;
                sr.movementType = ScrollRect.MovementType.Elastic;
                sr.scrollSensitivity = 30f;
                return go;
            });
            BuildIfMissing("Panel", () =>
            {
                // Laya Panel = 裁剪容器(可滚动)。子节点统一塞进 Content。
                GameObject go = NewRect("Panel");
                go.AddComponent<RectMask2D>();
                ScrollRect sr = go.AddComponent<ScrollRect>();
                RectTransform content = NewChildRect(go, "Content", true);
                sr.viewport = (RectTransform)go.transform;
                sr.content = content;
                sr.horizontal = false;
                sr.vertical = true;
                sr.movementType = ScrollRect.MovementType.Elastic;
                return go;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LayaUI] 模板就绪: " + LayaUISettings.TEMPLATE_ROOT);
        }

        public static GameObject Load(string name)
        {
            string path = LayaUISettings.TEMPLATE_ROOT + "/" + name + ".prefab";
            GameObject t = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (t == null)
            {
                BuildAll();
                t = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return t;
        }

        /// <summary>实例化模板并完全解包,返回普通节点树。</summary>
        public static GameObject Spawn(string name, Transform parent)
        {
            GameObject template = Load(name);
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(template);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void BuildIfMissing(string name, System.Func<GameObject> builder)
        {
            string path = LayaUISettings.TEMPLATE_ROOT + "/" + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject go = builder();
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static GameObject NewRect(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(100f, 100f);
            return go;
        }

        private static RectTransform NewChildRect(GameObject parent, string name, bool stretch)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent.transform, false);
            if (stretch)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            return rt;
        }
    }
}
