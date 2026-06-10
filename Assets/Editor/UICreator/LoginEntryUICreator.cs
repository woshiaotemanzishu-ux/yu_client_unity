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
    public static class LoginEntryUICreator
    {
        private const string ModuleName = "Login";
        private const string ViewName = "LoginEntryView";
        private const string PrefabDir = "Assets/Prefabs/UI/Login";
        private const string PrefabPath = PrefabDir + "/" + ViewName + ".prefab";
        private const string GeneratedDir = "Assets/Scripts/Generated/UI/Login";
        private const string BindPath = GeneratedDir + "/" + ViewName + "Bind.cs";

        [MenuItem("Shenxiao/UI/UICreator/Create Login Entry View", priority = 80)]
        public static void CreateLoginEntryViewMenu()
        {
            string result = CreateLoginEntryView();
            EditorUtility.DisplayDialog("UICreator", result, "OK");
        }

        public static void CreateLoginEntryViewBatch()
        {
            Debug.Log("[UICreator] " + CreateLoginEntryView());
        }

        public static string CreateLoginEntryView()
        {
            EnsureAssetFolder(PrefabDir);
            EnsureAssetFolder(GeneratedDir);

            List<BindField> fields = CreateBindFields();
            GenerateBindScript(fields);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            GameObject root = new GameObject(ViewName, typeof(RectTransform), typeof(CanvasGroup));
            RectTransform rootRt = (RectTransform)root.transform;
            Stretch(rootRt, 0f);

            Dictionary<string, Component> wired = new Dictionary<string, Component>();
            CreateLoadingPanel(root.transform, wired);
            CreateAccountPanel(root.transform, wired);
            CreateRegisterPanel(root.transform, wired);
            CreateWaitingPanel(root.transform, wired);

            string pending = AttachBindComponent(root, fields, wired);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return string.IsNullOrEmpty(pending)
                ? "LoginEntryView generated and wired."
                : "LoginEntryView generated. " + pending;
        }

        private static void CreateLoadingPanel(Transform parent, Dictionary<string, Component> wired)
        {
            RectTransform panel = CreatePanel(parent, "_panel_loading", true, wired);
            CreateImage(panel, "_img_loading_bg", new RectSpec(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), new Color(0.08f, 0.09f, 0.1f, 1f), wired);

            RectTransform bottom = CreateBox(panel, "_panel_loading_bottom", new RectSpec(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(720f, 260f)), wired);
            CreateText(bottom, "_txt_first_load", "First load may take a while.", 24f, new RectSpec(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 176f), new Vector2(670f, 32f)), wired);
            CreateProgress(bottom, "_bar_load_progress", new RectSpec(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 124f), new Vector2(635f, 24f)), wired);
            CreateImage(bottom, "_img_progress_end", new RectSpec(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-318f, 124f), new Vector2(65f, 43f)), new Color(0.96f, 0.73f, 0.28f, 1f), wired);
            CreateText(bottom, "_txt_load_progress", "loading...0%", 24f, new RectSpec(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 78f), new Vector2(670f, 32f)), wired);
            CreateText(bottom, "_txt_version_tip", "Healthy play reminder and version text placeholder.", 18f, new RectSpec(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(680f, 48f)), wired);
        }

        private static void CreateAccountPanel(Transform parent, Dictionary<string, Component> wired)
        {
            RectTransform panel = CreatePanel(parent, "_panel_account", false, wired);
            CreateImage(panel, "_img_account_bg", new RectSpec(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), new Color(0.04f, 0.05f, 0.06f, 1f), wired);

            RectTransform form = CreateBox(panel, "_panel_account_form", CenterSpec(new Vector2(720f, 567f), new Vector2(0f, -10f)), wired);
            CreateImage(form, "_img_login_panel", CenterSpec(new Vector2(651f, 363f), new Vector2(0f, -40f)), new Color(0.82f, 0.73f, 0.58f, 1f), wired);
            CreateImage(form, "_img_logo", CenterSpec(new Vector2(518f, 159f), new Vector2(0f, 210f)), new Color(0.64f, 0.2f, 0.18f, 1f), wired);

            CreateText(form, "_txt_account_label", "Account", 30f, CenterSpec(new Vector2(150f, 42f), new Vector2(-190f, 42f)), wired);
            CreateText(form, "_txt_password_label", "Password", 30f, CenterSpec(new Vector2(150f, 42f), new Vector2(-190f, -28f)), wired);
            CreateInput(form, "_input_account", "Enter account", false, CenterSpec(new Vector2(350f, 46f), new Vector2(70f, 42f)), wired);
            CreateInput(form, "_input_password", "Enter password", true, CenterSpec(new Vector2(350f, 46f), new Vector2(70f, -28f)), wired);
            CreateToggle(form, "_chk_remember", "Remember", CenterSpec(new Vector2(170f, 46f), new Vector2(0f, -96f)), wired);
            CreateButton(form, "_btn_register", "Register", CenterSpec(new Vector2(146f, 52f), new Vector2(-82f, -154f)), wired);
            CreateButton(form, "_btn_login", "Login", CenterSpec(new Vector2(146f, 52f), new Vector2(82f, -154f)), wired);
        }

        private static void CreateRegisterPanel(Transform parent, Dictionary<string, Component> wired)
        {
            RectTransform panel = CreatePanel(parent, "_panel_register", false, wired);
            CreateImage(panel, "_img_register_bg", new RectSpec(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), new Color(0.04f, 0.05f, 0.06f, 1f), wired);

            RectTransform form = CreateBox(panel, "_panel_register_form", CenterSpec(new Vector2(720f, 567f), new Vector2(0f, -10f)), wired);
            CreateImage(form, "_img_register_panel", CenterSpec(new Vector2(651f, 363f), new Vector2(0f, -40f)), new Color(0.82f, 0.73f, 0.58f, 1f), wired);
            CreateImage(form, "_img_register_logo", CenterSpec(new Vector2(518f, 159f), new Vector2(0f, 210f)), new Color(0.64f, 0.2f, 0.18f, 1f), wired);

            CreateText(form, "_txt_register_account_label", "Account", 30f, CenterSpec(new Vector2(150f, 42f), new Vector2(-190f, 42f)), wired);
            CreateText(form, "_txt_register_password_label", "Password", 30f, CenterSpec(new Vector2(150f, 42f), new Vector2(-190f, -28f)), wired);
            CreateInput(form, "_input_register_account", "Account", false, CenterSpec(new Vector2(350f, 46f), new Vector2(70f, 42f)), wired);
            CreateInput(form, "_input_register_password", "Password", true, CenterSpec(new Vector2(350f, 46f), new Vector2(70f, -28f)), wired);
            CreateButton(form, "_btn_register_confirm", "Confirm", CenterSpec(new Vector2(146f, 52f), new Vector2(-82f, -154f)), wired);
            CreateButton(form, "_btn_register_return", "Back", CenterSpec(new Vector2(146f, 52f), new Vector2(82f, -154f)), wired);
        }

        private static void CreateWaitingPanel(Transform parent, Dictionary<string, Component> wired)
        {
            RectTransform panel = CreatePanel(parent, "_panel_waiting", false, wired);
            CreateImage(panel, "_img_waiting_dim", new RectSpec(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), new Color(0f, 0f, 0f, 0.45f), wired);
            RectTransform box = CreateBox(panel, "_panel_waiting_box", CenterSpec(new Vector2(180f, 180f), Vector2.zero), wired);
            CreateImage(box, "_img_waiting_circle", CenterSpec(new Vector2(123f, 123f), new Vector2(0f, 18f)), new Color(0.96f, 0.73f, 0.28f, 1f), wired);
            CreateText(box, "_txt_waiting", "Loading", 24f, CenterSpec(new Vector2(160f, 34f), new Vector2(0f, -58f)), wired);
        }

        private static RectTransform CreatePanel(Transform parent, string name, bool active, Dictionary<string, Component> wired)
        {
            RectTransform rt = CreateBox(parent, name, new RectSpec(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), wired);
            rt.gameObject.SetActive(active);
            return rt;
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

        private static TMP_InputField CreateInput(Transform parent, string name, string placeholderValue, bool password, RectSpec spec, Dictionary<string, Component> wired)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            ApplyRect(rt, spec);

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.96f, 0.91f, 0.78f, 1f);

            GameObject viewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(go.transform, false);
            Stretch((RectTransform)viewport.transform, 8f);

            TextMeshProUGUI text = CreateText(viewport.transform, "Text", string.Empty, 24f, new RectSpec(Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero), new Dictionary<string, Component>());
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = Color.black;
            TextMeshProUGUI placeholder = CreateText(viewport.transform, "Placeholder", placeholderValue, 24f, new RectSpec(Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero), new Dictionary<string, Component>());
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.color = new Color(0.2f, 0.2f, 0.2f, 0.55f);

            TMP_InputField input = go.GetComponent<TMP_InputField>();
            input.textViewport = (RectTransform)viewport.transform;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            input.targetGraphic = image;
            wired[name] = input;
            return input;
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

            TextMeshProUGUI text = CreateText(go.transform, "Label", label, 24f, new RectSpec(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), new Dictionary<string, Component>());
            text.color = new Color(0.28f, 0.16f, 0.08f, 1f);
            wired[name] = button;
            return button;
        }

        private static Toggle CreateToggle(Transform parent, string name, string label, RectSpec spec, Dictionary<string, Component> wired)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            ApplyRect(rt, spec);

            Image bg = CreateImage(go.transform, "Background", new RectSpec(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(34f, 34f)), new Color(0.83f, 0.72f, 0.58f, 1f), new Dictionary<string, Component>());
            Image checkmark = CreateImage(bg.transform, "Checkmark", new RectSpec(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f)), new Color(0.22f, 0.53f, 0.18f, 1f), new Dictionary<string, Component>());
            TextMeshProUGUI text = CreateText(go.transform, "Label", label, 22f, new RectSpec(new Vector2(0f, 0f), Vector2.one, new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(-50f, 0f)), new Dictionary<string, Component>());
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = new Color(0.28f, 0.16f, 0.08f, 1f);

            Toggle toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = bg;
            toggle.graphic = checkmark;
            toggle.isOn = true;
            wired[name] = toggle;
            return toggle;
        }

        private static Slider CreateProgress(Transform parent, string name, RectSpec spec, Dictionary<string, Component> wired)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            ApplyRect(rt, spec);

            Image background = CreateImage(go.transform, "Background", new RectSpec(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), new Color(0.18f, 0.14f, 0.12f, 1f), new Dictionary<string, Component>());
            background.raycastTarget = false;
            RectTransform fillArea = CreateBox(go.transform, "Fill Area", new RectSpec(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), new Dictionary<string, Component>());
            Image fill = CreateImage(fillArea, "Fill", new RectSpec(Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero), new Color(0.98f, 0.58f, 0.16f, 1f), new Dictionary<string, Component>());

            Slider slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.fillRect = (RectTransform)fill.transform;
            slider.targetGraphic = fill;
            wired[name] = slider;
            return slider;
        }

        private static string AttachBindComponent(GameObject root, List<BindField> fields, Dictionary<string, Component> wired)
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
                new BindField("_panel_loading", "RectTransform"),
                new BindField("_img_loading_bg", "Image"),
                new BindField("_panel_loading_bottom", "RectTransform"),
                new BindField("_txt_first_load", "TextMeshProUGUI"),
                new BindField("_bar_load_progress", "Slider"),
                new BindField("_img_progress_end", "Image"),
                new BindField("_txt_load_progress", "TextMeshProUGUI"),
                new BindField("_txt_version_tip", "TextMeshProUGUI"),
                new BindField("_panel_account", "RectTransform"),
                new BindField("_img_account_bg", "Image"),
                new BindField("_panel_account_form", "RectTransform"),
                new BindField("_img_login_panel", "Image"),
                new BindField("_img_logo", "Image"),
                new BindField("_txt_account_label", "TextMeshProUGUI"),
                new BindField("_txt_password_label", "TextMeshProUGUI"),
                new BindField("_input_account", "TMP_InputField"),
                new BindField("_input_password", "TMP_InputField"),
                new BindField("_chk_remember", "Toggle"),
                new BindField("_btn_register", "Button"),
                new BindField("_btn_login", "Button"),
                new BindField("_panel_register", "RectTransform"),
                new BindField("_img_register_bg", "Image"),
                new BindField("_panel_register_form", "RectTransform"),
                new BindField("_img_register_panel", "Image"),
                new BindField("_img_register_logo", "Image"),
                new BindField("_txt_register_account_label", "TextMeshProUGUI"),
                new BindField("_txt_register_password_label", "TextMeshProUGUI"),
                new BindField("_input_register_account", "TMP_InputField"),
                new BindField("_input_register_password", "TMP_InputField"),
                new BindField("_btn_register_confirm", "Button"),
                new BindField("_btn_register_return", "Button"),
                new BindField("_panel_waiting", "RectTransform"),
                new BindField("_img_waiting_dim", "Image"),
                new BindField("_panel_waiting_box", "RectTransform"),
                new BindField("_img_waiting_circle", "Image"),
                new BindField("_txt_waiting", "TextMeshProUGUI"),
            };
        }

        private static void GenerateBindScript(List<BindField> fields)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("//   This file is generated by LoginEntryUICreator.");
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
            sb.AppendLine("    [UIView(\"prefabs/ui/login/loginentryview\")]");
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
            sb.AppendLine("            if (value == null) throw new MissingReferenceException(fieldName + \" is not wired by LoginEntryUICreator.\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(ToAbsolutePath(BindPath), sb.ToString(), Encoding.UTF8);
        }

        private static RectSpec CenterSpec(Vector2 size, Vector2 anchoredPosition)
        {
            return new RectSpec(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size);
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
