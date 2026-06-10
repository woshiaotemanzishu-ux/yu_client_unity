using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.EditorTools.UICreator
{
    public static class LoginServerSelectUICreator
    {
        private const string ModuleName = "Login";
        private const string ViewName = "LoginServerSelectView";
        private const string PrefabDir = "Assets/Prefabs/UI/Login";
        private const string PrefabPath = PrefabDir + "/" + ViewName + ".prefab";
        private const string GeneratedDir = "Assets/Scripts/Generated/UI/Login";
        private const string BindPath = GeneratedDir + "/" + ViewName + "Bind.cs";

        [MenuItem("Shenxiao/UI/UICreator/Create Login Server Select View", priority = 81)]
        public static void CreateLoginServerSelectViewMenu()
        {
            string result = CreateLoginServerSelectView();
            EditorUtility.DisplayDialog("UICreator", result, "OK");
        }

        public static void CreateLoginServerSelectViewBatch()
        {
            Debug.Log("[UICreator] " + CreateLoginServerSelectView());
        }

        public static string CreateLoginServerSelectView()
        {
            EnsureAssetFolder(PrefabDir);
            EnsureAssetFolder(GeneratedDir);

            List<BindField> fields = CreateBindFields();
            GenerateBindScript(fields);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            GameObject root = new GameObject(ViewName, typeof(RectTransform), typeof(CanvasGroup));
            Stretch((RectTransform)root.transform, 0f);

            Dictionary<string, Component> wired = new Dictionary<string, Component>();
            CreateView(root.transform, wired);

            string pending = AttachViewComponent(root, fields, wired);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return string.IsNullOrEmpty(pending)
                ? "LoginServerSelectView generated and wired."
                : "LoginServerSelectView generated. " + pending;
        }

        private static void CreateView(Transform parent, Dictionary<string, Component> wired)
        {
            RectTransform panel = CreateBox(parent, "_panel_root", StretchSpec(), wired);
            CreateImage(panel, "_img_bg", StretchSpec(), new Color(0.04f, 0.05f, 0.06f, 1f), wired);

            CreateText(panel, "_txt_title", "Server", 44f, CenterSpec(new Vector2(620f, 72f), new Vector2(0f, 420f)), wired);
            CreateText(panel, "_txt_account", "Account", 24f, CenterSpec(new Vector2(620f, 40f), new Vector2(0f, 358f)), wired);
            CreateText(panel, "_txt_selected_server", "No server", 34f, CenterSpec(new Vector2(620f, 56f), new Vector2(0f, 230f)), wired);
            CreateDropdown(panel, "_dd_server", CenterSpec(new Vector2(520f, 58f), new Vector2(0f, 132f)), wired);
            CreateButton(panel, "_btn_enter", "Enter", CenterSpec(new Vector2(220f, 64f), new Vector2(125f, 20f)), wired);
            CreateButton(panel, "_btn_back", "Back", CenterSpec(new Vector2(220f, 64f), new Vector2(-125f, 20f)), wired);
            CreateText(panel, "_txt_message", string.Empty, 22f, CenterSpec(new Vector2(620f, 80f), new Vector2(0f, -80f)), wired);

            RectTransform waiting = CreateBox(panel, "_panel_waiting", StretchSpec(), wired);
            waiting.gameObject.SetActive(false);
            CreateImage(waiting, "_img_waiting_dim", StretchSpec(), new Color(0f, 0f, 0f, 0.45f), wired);
            CreateText(waiting, "_txt_waiting", "Loading", 26f, CenterSpec(new Vector2(220f, 48f), Vector2.zero), wired);
        }

        private static RectTransform CreateBox(Transform parent, string name, RectSpec spec, Dictionary<string, Component> wired)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            ApplyRect(rt, spec);
            wired[name] = rt;
            return rt;
        }

        private static Image CreateImage(Transform parent, string name, RectSpec spec, Color color, Dictionary<string, Component> wired)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            ApplyRect(rt, spec);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            wired[name] = image;
            return image;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string textValue, float fontSize, RectSpec spec, Dictionary<string, Component> wired)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            ApplyRect(rt, spec);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = textValue;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            wired[name] = text;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, RectSpec spec, Dictionary<string, Component> wired)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            ApplyRect(rt, spec);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.93f, 0.68f, 0.34f, 1f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI text = CreateText(go.transform, "Label", label, 26f, StretchSpec(), new Dictionary<string, Component>());
            text.color = new Color(0.28f, 0.16f, 0.08f, 1f);
            wired[name] = button;
            return button;
        }

        private static TMP_Dropdown CreateDropdown(Transform parent, string name, RectSpec spec, Dictionary<string, Component> wired)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
            go.transform.SetParent(parent, false);
            ApplyRect((RectTransform)go.transform, spec);

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.82f, 0.73f, 0.58f, 1f);

            TextMeshProUGUI label = CreateText(go.transform, "Label", "Select server", 24f, new RectSpec(Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(-54f, 0f)), new Dictionary<string, Component>());
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = new Color(0.16f, 0.1f, 0.06f, 1f);
            TextMeshProUGUI arrow = CreateText(go.transform, "Arrow", "v", 22f, new RectSpec(new Vector2(1f, 0f), Vector2.one, new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(40f, 0f)), new Dictionary<string, Component>());
            arrow.color = new Color(0.16f, 0.1f, 0.06f, 1f);

            RectTransform template = CreateDropdownTemplate(go.transform);
            TMP_Dropdown dropdown = go.GetComponent<TMP_Dropdown>();
            dropdown.targetGraphic = image;
            dropdown.captionText = label;
            dropdown.template = template;
            dropdown.itemText = template.GetComponentInChildren<TextMeshProUGUI>(true);
            dropdown.options.Clear();
            dropdown.options.Add(new TMP_Dropdown.OptionData("Select server"));
            wired[name] = dropdown;
            return dropdown;
        }

        private static RectTransform CreateDropdownTemplate(Transform parent)
        {
            GameObject templateGo = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateGo.transform.SetParent(parent, false);
            RectTransform template = (RectTransform)templateGo.transform;
            template.anchorMin = new Vector2(0f, 0f);
            template.anchorMax = new Vector2(1f, 0f);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = new Vector2(0f, -4f);
            template.sizeDelta = new Vector2(0f, 220f);
            templateGo.SetActive(false);

            Image templateImage = templateGo.GetComponent<Image>();
            templateImage.color = new Color(0.16f, 0.14f, 0.12f, 1f);

            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(templateGo.transform, false);
            RectTransform viewport = (RectTransform)viewportGo.transform;
            Stretch(viewport, 4f);
            Image viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(0.16f, 0.14f, 0.12f, 1f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform content = (RectTransform)contentGo.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 44f);

            GameObject itemGo = new GameObject("Item", typeof(RectTransform), typeof(Toggle), typeof(Image));
            itemGo.transform.SetParent(contentGo.transform, false);
            RectTransform item = (RectTransform)itemGo.transform;
            item.anchorMin = new Vector2(0f, 1f);
            item.anchorMax = new Vector2(1f, 1f);
            item.pivot = new Vector2(0.5f, 1f);
            item.anchoredPosition = Vector2.zero;
            item.sizeDelta = new Vector2(0f, 44f);
            itemGo.GetComponent<Image>().color = new Color(0.28f, 0.23f, 0.18f, 1f);

            GameObject checkGo = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
            checkGo.transform.SetParent(itemGo.transform, false);
            RectTransform check = (RectTransform)checkGo.transform;
            check.anchorMin = new Vector2(0f, 0.5f);
            check.anchorMax = new Vector2(0f, 0.5f);
            check.pivot = new Vector2(0.5f, 0.5f);
            check.anchoredPosition = new Vector2(18f, 0f);
            check.sizeDelta = new Vector2(16f, 16f);
            checkGo.GetComponent<Image>().color = new Color(0.93f, 0.68f, 0.34f, 1f);

            TextMeshProUGUI itemLabel = CreateText(itemGo.transform, "Item Label", "Option", 22f, new RectSpec(Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(-42f, 0f)), new Dictionary<string, Component>());
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;

            Toggle toggle = itemGo.GetComponent<Toggle>();
            toggle.targetGraphic = itemGo.GetComponent<Image>();
            toggle.graphic = checkGo.GetComponent<Image>();

            ScrollRect scrollRect = templateGo.GetComponent<ScrollRect>();
            scrollRect.content = content;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            return template;
        }

        private static string AttachViewComponent(GameObject root, List<BindField> fields, Dictionary<string, Component> wired)
        {
            Type viewType = Type.GetType("Shenxiao.Module.Core.Login." + ViewName + ", Shenxiao.Module.Core")
                ?? Type.GetType("Shenxiao.Generated.UI.Login." + ViewName + "Bind, Shenxiao.Generated");
            if (viewType == null)
            {
                return "View type is not compiled yet; run this menu once more after Unity recompiles.";
            }

            Component component = root.AddComponent(viewType);
            SerializedObject so = new SerializedObject(component);
            List<string> missing = new List<string>();
            foreach (BindField field in fields)
            {
                SerializedProperty prop = so.FindProperty(field.name);
                if (prop == null || !wired.TryGetValue(field.name, out Component value))
                {
                    missing.Add(field.name);
                    continue;
                }
                prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return missing.Count == 0 ? string.Empty : "Missing fields: " + string.Join(", ", missing);
        }

        private static List<BindField> CreateBindFields()
        {
            return new List<BindField>
            {
                new BindField("_panel_root", "RectTransform"),
                new BindField("_img_bg", "Image"),
                new BindField("_txt_title", "TextMeshProUGUI"),
                new BindField("_txt_account", "TextMeshProUGUI"),
                new BindField("_txt_selected_server", "TextMeshProUGUI"),
                new BindField("_dd_server", "TMP_Dropdown"),
                new BindField("_btn_enter", "Button"),
                new BindField("_btn_back", "Button"),
                new BindField("_txt_message", "TextMeshProUGUI"),
                new BindField("_panel_waiting", "RectTransform"),
                new BindField("_img_waiting_dim", "Image"),
                new BindField("_txt_waiting", "TextMeshProUGUI"),
            };
        }

        private static void GenerateBindScript(List<BindField> fields)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("//   This file is generated by LoginServerSelectUICreator.");
            sb.AppendLine("//   DO NOT EDIT. Re-run the tool to regenerate.");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine();
            sb.AppendLine("using TMPro;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UI;");
            sb.AppendLine("using Shenxiao.Framework.UI;");
            sb.AppendLine();
            sb.AppendLine("namespace Shenxiao.Generated.UI." + ModuleName);
            sb.AppendLine("{");
            sb.AppendLine("    [UIView(\"prefabs/ui/login/serverselectview\")]");
            sb.AppendLine("    public partial class " + ViewName + "Bind : BaseView");
            sb.AppendLine("    {");
            sb.AppendLine("        public override UILayer Layer => UILayer.Top;");
            sb.AppendLine();
            foreach (BindField field in fields)
            {
                sb.AppendLine("        public " + field.typeName + " " + field.name + ";");
            }
            sb.AppendLine();
            sb.AppendLine("        protected override void BindNodes()");
            sb.AppendLine("        {");
            foreach (BindField field in fields)
            {
                sb.AppendLine("            EnsureBound(nameof(" + field.name + "), " + field.name + ");");
            }
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private static void EnsureBound(string fieldName, UnityEngine.Object value)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (value == null) throw new MissingReferenceException(fieldName + \" is not wired by LoginServerSelectUICreator.\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(ToAbsolutePath(BindPath), sb.ToString(), Encoding.UTF8);
        }

        private static RectSpec CenterSpec(Vector2 size, Vector2 anchoredPosition)
        {
            return new RectSpec(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size);
        }

        private static RectSpec StretchSpec()
        {
            return new RectSpec(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private static void ApplyRect(RectTransform rt, RectSpec spec)
        {
            rt.anchorMin = spec.anchorMin;
            rt.anchorMax = spec.anchorMax;
            rt.pivot = spec.pivot;
            rt.anchoredPosition = spec.anchoredPosition;
            rt.sizeDelta = spec.sizeDelta;
        }

        private static void Stretch(RectTransform rt, float padding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            string[] parts = assetFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, assetPath).Replace('\\', '/');
        }

        private readonly struct RectSpec
        {
            public readonly Vector2 anchorMin;
            public readonly Vector2 anchorMax;
            public readonly Vector2 pivot;
            public readonly Vector2 anchoredPosition;
            public readonly Vector2 sizeDelta;

            public RectSpec(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
            {
                this.anchorMin = anchorMin;
                this.anchorMax = anchorMax;
                this.pivot = pivot;
                this.anchoredPosition = anchoredPosition;
                this.sizeDelta = sizeDelta;
            }
        }

        private readonly struct BindField
        {
            public readonly string name;
            public readonly string typeName;

            public BindField(string name, string typeName)
            {
                this.name = name;
                this.typeName = typeName;
            }
        }
    }
}
