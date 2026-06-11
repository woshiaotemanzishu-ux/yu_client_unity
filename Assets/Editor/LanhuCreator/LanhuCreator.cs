using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;

namespace Shenxiao.EditorTools.Lanhu
{
    public static class LanhuCreator
    {
        private const string ManifestName = "lanhu_manifest.json";
        private const string DefaultAssetRoot = "assets";
        private const string RemotePrefabRoot = "Assets/Prefabs/UI";
        private const string LocalPrefabRoot = "Assets/_App/UI";
        private const string LocalTextureRoot = "Assets/_App/UI/Textures";
        private const string GeneratedRoot = "Assets/Scripts/Generated/UI";
        private const string GameResRoot = "Assets/GameRes";

        [MenuItem("神霄/工具/蓝湖(备用路线)/导入 Package...", priority = 85)]
        public static void ImportPackageMenu()
        {
            var dir = EditorUtility.OpenFolderPanel("选择蓝湖导入包目录", "", "");
            if (string.IsNullOrEmpty(dir)) return;

            var report = ImportPackage(dir);
            EditorUtility.DisplayDialog("蓝湖导入完成", BuildDialogSummary(report), "OK");
        }

        [MenuItem("神霄/工具/蓝湖(备用路线)/校验 Package...", priority = 86)]
        public static void ValidatePackageMenu()
        {
            var dir = EditorUtility.OpenFolderPanel("选择蓝湖导入包目录", "", "");
            if (string.IsNullOrEmpty(dir)) return;

            var report = ImportPackageInternal(dir, false);
            EditorUtility.DisplayDialog("蓝湖校验完成", BuildDialogSummary(report), "OK");
        }

        public static LanhuImportReport ImportPackage(string packageDir)
        {
            return ImportPackageInternal(packageDir, true);
        }

        private static LanhuImportReport ImportPackageInternal(string packageDir, bool writeAssets)
        {
            var report = new LanhuImportReport();
            var manifestPath = FindManifest(packageDir);
            if (string.IsNullOrEmpty(manifestPath))
            {
                report.warnings.Add("package missing lanhu_manifest.json");
                WriteReport(packageDir, "unknown", report);
                return report;
            }

            var package = LoadPackage(manifestPath);
            NormalizePackage(package);

            foreach (var view in package.views)
            {
                if (string.IsNullOrEmpty(view.name))
                {
                    report.warnings.Add("view missing name");
                    continue;
                }

                report.views++;
                var fields = CollectBindFields(view);
                if (writeAssets)
                {
                    GenerateBindScript(package, view, fields);
                    report.bindScripts++;
                    GeneratePrefab(packageDir, package, view, fields, report);
                }
                else
                {
                    ValidateImages(packageDir, package, view, report);
                }
            }

            WriteReport(packageDir, package.module, report);
            if (writeAssets)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            return report;
        }

        private static LanhuPackage LoadPackage(string manifestPath)
        {
            var json = File.ReadAllText(manifestPath, Encoding.UTF8);
            var package = JsonConvert.DeserializeObject<LanhuPackage>(json);
            if (package == null) throw new InvalidOperationException("lanhu manifest is empty");
            return package;
        }

        private static void NormalizePackage(LanhuPackage package)
        {
            if (string.IsNullOrEmpty(package.module)) package.module = "login";
            if (string.IsNullOrEmpty(package.assetRoot)) package.assetRoot = DefaultAssetRoot;
            if (package.views == null) package.views = new List<LanhuView>();
            foreach (var view in package.views)
            {
                if (string.IsNullOrEmpty(view.layer)) view.layer = view.local ? "Loading" : "Window";
                if (view.width <= 0) view.width = 720;
                if (view.height <= 0) view.height = 1280;
                if (view.nodes == null) view.nodes = new List<LanhuNode>();
            }
        }

        private static string FindManifest(string packageDir)
        {
            var direct = Path.Combine(packageDir, ManifestName);
            if (File.Exists(direct)) return direct;
            var files = Directory.GetFiles(packageDir, ManifestName, SearchOption.AllDirectories);
            return files.Length > 0 ? files[0] : null;
        }

        private static List<LanhuBindField> CollectBindFields(LanhuView view)
        {
            var fields = new List<LanhuBindField>();
            foreach (var node in view.nodes)
            {
                CollectBindFieldsRecursive(node, node.name, fields);
            }
            return fields;
        }

        private static void CollectBindFieldsRecursive(LanhuNode node, string path, List<LanhuBindField> fields)
        {
            if (node == null) return;
            var fieldName = MakeFieldName(node.name);
            if (IsBindCandidate(fieldName))
            {
                var componentType = ResolveBindComponentType(fieldName, node.type);
                fields.Add(new LanhuBindField
                {
                    fieldName = fieldName,
                    nodePath = path,
                    componentType = componentType,
                    componentTypeName = GetGeneratedTypeName(componentType),
                });
            }

            if (node.children == null) return;
            foreach (var child in node.children)
            {
                var childPath = string.IsNullOrEmpty(path) ? child.name : path + "/" + child.name;
                CollectBindFieldsRecursive(child, childPath, fields);
            }
        }

        private static void GenerateBindScript(LanhuPackage package, LanhuView view, List<LanhuBindField> fields)
        {
            var moduleName = view.local ? "App" : ToPascal(package.module);
            var outputDir = $"{GeneratedRoot}/{moduleName}";
            EnsureAssetFolder(outputDir);

            var className = view.name + "Bind";
            var ns = "Shenxiao.Generated.UI." + moduleName;
            var path = $"{outputDir}/{className}.cs";
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("//   This file is generated by LanhuCreator.");
            sb.AppendLine("//   DO NOT EDIT. Re-run the tool to regenerate.");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine();
            sb.AppendLine("using TMPro;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UI;");
            sb.AppendLine("using Shenxiao.Framework.UI;");
            sb.AppendLine();
            sb.AppendLine("namespace " + ns);
            sb.AppendLine("{");
            sb.AppendLine("    [UIView(\"" + GetViewAddress(package, view) + "\")]");
            sb.AppendLine("    public partial class " + className + " : BaseView");
            sb.AppendLine("    {");
            sb.AppendLine("        public override UILayer Layer => UILayer." + NormalizeLayer(view.layer) + ";");
            sb.AppendLine();
            foreach (var field in fields)
            {
                sb.AppendLine("        public " + field.componentTypeName + " " + field.fieldName + ";");
            }
            sb.AppendLine();
            sb.AppendLine("        protected override void BindNodes()");
            sb.AppendLine("        {");
            foreach (var field in fields)
            {
                sb.AppendLine("            EnsureBound(nameof(" + field.fieldName + "), " + field.fieldName + ");");
            }
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private static void EnsureBound(string fieldName, UnityEngine.Object value)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (value == null) throw new MissingReferenceException(fieldName + \" is not wired by LanhuCreator.\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(ToAbsolutePath(path), sb.ToString(), Encoding.UTF8);
        }

        private static void GeneratePrefab(string packageDir, LanhuPackage package, LanhuView view, List<LanhuBindField> fields, LanhuImportReport report)
        {
            var prefabDir = view.local ? LocalPrefabRoot : $"{RemotePrefabRoot}/{ToPascal(package.module)}";
            EnsureAssetFolder(prefabDir);

            var prefabPath = $"{prefabDir}/{view.name}.prefab";
            var root = new GameObject(view.name, typeof(RectTransform));
            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.sizeDelta = new Vector2(view.width, view.height);
            rootRt.anchoredPosition = Vector2.zero;

            var wired = new Dictionary<string, Component>();
            foreach (var node in view.nodes)
            {
                CreateNode(packageDir, package, view.local, node, root.transform, node.name, wired, report);
            }

            var viewType = ResolveViewType(package, view);
            if (viewType != null)
            {
                var component = root.AddComponent(viewType);
                WireSerializedFields(component, fields, wired, report);
            }
            else
            {
                report.pendingComponents.Add($"{view.name}: generated Bind type not compiled yet; rerun Lanhu import after Unity recompiles.");
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            report.prefabs++;
        }

        private static void CreateNode(string packageDir, LanhuPackage package, bool localView, LanhuNode node, Transform parent, string nodePath, Dictionary<string, Component> wired, LanhuImportReport report)
        {
            if (node == null) return;

            var name = string.IsNullOrEmpty(node.name) ? "Node" : node.name;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            if (node.visible.HasValue && !node.visible.Value) go.SetActive(false);

            var rt = (RectTransform)go.transform;
            ConfigureRect(rt, node);

            var bindType = ResolveBindComponentType(MakeFieldName(node.name), node.type);
            var component = AddNodeComponent(go, bindType, packageDir, package, localView, node, report);

            var fieldName = MakeFieldName(node.name);
            if (IsBindCandidate(fieldName))
            {
                wired[fieldName] = component;
            }

            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    var childPath = string.IsNullOrEmpty(nodePath) ? child.name : nodePath + "/" + child.name;
                    CreateNode(packageDir, package, localView, child, go.transform, childPath, wired, report);
                }
            }
        }

        private static void ConfigureRect(RectTransform rt, LanhuNode node)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(node.width ?? 100f, node.height ?? 40f);
            rt.anchoredPosition = new Vector2(node.x ?? 0f, -(node.y ?? 0f));
        }

        private static Component AddNodeComponent(GameObject go, Type bindType, string packageDir, LanhuPackage package, bool localView, LanhuNode node, LanhuImportReport report)
        {
            if (bindType == typeof(Button))
            {
                var img = go.AddComponent<Image>();
                ConfigureImage(img, packageDir, package, localView, node, report);
                var btn = go.AddComponent<Button>();
                if (!string.IsNullOrEmpty(node.text)) CreateTextChild(go.transform, "Text", node.text, node.fontSize, "#FFFFFFFF");
                return btn;
            }

            if (bindType == typeof(Image))
            {
                var img = go.AddComponent<Image>();
                ConfigureImage(img, packageDir, package, localView, node, report);
                return img;
            }

            if (bindType == typeof(TextMeshProUGUI))
            {
                var text = go.AddComponent<TextMeshProUGUI>();
                text.text = node.text ?? string.Empty;
                text.fontSize = node.fontSize ?? 24f;
                text.color = ParseColor(node.color, Color.white, node.alpha);
                text.raycastTarget = node.raycast ?? false;
                return text;
            }

            if (bindType == typeof(TMP_InputField))
            {
                return CreateInput(go, node);
            }

            if (bindType == typeof(Slider))
            {
                return CreateSlider(go, node);
            }

            if (bindType == typeof(Toggle))
            {
                var img = go.AddComponent<Image>();
                ConfigureImage(img, packageDir, package, localView, node, report);
                return go.AddComponent<Toggle>();
            }

            if (bindType == typeof(TMP_Dropdown))
            {
                var img = go.AddComponent<Image>();
                ConfigureImage(img, packageDir, package, localView, node, report);
                return go.AddComponent<TMP_Dropdown>();
            }

            if (bindType == typeof(ScrollRect))
            {
                return CreateScrollRect(go);
            }

            if (bindType == typeof(RectMask2D))
            {
                return go.AddComponent<RectMask2D>();
            }

            if (bindType == typeof(ToggleGroup))
            {
                return go.AddComponent<ToggleGroup>();
            }

            if (!string.IsNullOrEmpty(node.image) || !string.IsNullOrEmpty(node.color))
            {
                var img = go.AddComponent<Image>();
                ConfigureImage(img, packageDir, package, localView, node, report);
            }
            return go.GetComponent<RectTransform>();
        }

        private static TMP_InputField CreateInput(GameObject go, LanhuNode node)
        {
            go.AddComponent<Image>();
            var input = go.AddComponent<TMP_InputField>();
            var viewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(go.transform, false);
            Stretch((RectTransform)viewport.transform, 8f);

            var text = CreateTextChild(viewport.transform, "Text", node.text ?? string.Empty, node.fontSize, "#FFFFFFFF");
            var placeholder = CreateTextChild(viewport.transform, "Placeholder", string.Empty, node.fontSize, "#80FFFFFF");
            input.textViewport = (RectTransform)viewport.transform;
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        private static Slider CreateSlider(GameObject go, LanhuNode node)
        {
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            Stretch((RectTransform)bg.transform, 0f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            Stretch((RectTransform)fillArea.transform, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Stretch((RectTransform)fill.transform, 0f);
            slider.fillRect = (RectTransform)fill.transform;
            return slider;
        }

        private static ScrollRect CreateScrollRect(GameObject go)
        {
            var scroll = go.AddComponent<ScrollRect>();
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(go.transform, false);
            Stretch((RectTransform)viewport.transform, 0f);
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            Stretch((RectTransform)content.transform, 0f);
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = (RectTransform)content.transform;
            return scroll;
        }

        private static TextMeshProUGUI CreateTextChild(Transform parent, string name, string textValue, float? fontSize, string color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform, 0f);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = textValue ?? string.Empty;
            text.fontSize = fontSize ?? 24f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = ParseColor(color, Color.white, null);
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rt, float padding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        private static void ConfigureImage(Image img, string packageDir, LanhuPackage package, bool localView, LanhuNode node, LanhuImportReport report)
        {
            img.raycastTarget = node.raycast ?? false;
            img.color = ParseColor(node.color, Color.white, node.alpha);
            if (string.IsNullOrEmpty(node.image)) return;

            var sprite = ResolveSprite(packageDir, package, localView, node, report);
            if (sprite != null)
            {
                img.sprite = sprite;
            }
            else
            {
                img.color = new Color(1f, 0f, 1f, 0.35f);
            }
        }

        private static Sprite ResolveSprite(string packageDir, LanhuPackage package, bool localView, LanhuNode node, LanhuImportReport report)
        {
            var key = ResourcePath.Normalize(node.image);
            var existing = FindExistingSpriteAsset(key, localView);
            if (!string.IsNullOrEmpty(existing)) return AssetDatabase.LoadAssetAtPath<Sprite>(existing);

            var source = FindPackageImage(packageDir, package, node);
            if (string.IsNullOrEmpty(source))
            {
                report.missingImages.Add($"{node.name}: {node.image}");
                return null;
            }

            var ext = Path.GetExtension(source).ToLowerInvariant();
            var destAssetPath = localView ? $"{LocalTextureRoot}/{key}{ext}" : $"{GameResRoot}/{key}{ext}";
            var destAbsPath = ToAbsolutePath(destAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destAbsPath));
            File.Copy(source, destAbsPath, true);
            AssetDatabase.ImportAsset(destAssetPath, ImportAssetOptions.ForceUpdate);
            report.copiedImages++;
            return AssetDatabase.LoadAssetAtPath<Sprite>(destAssetPath);
        }

        private static string FindExistingSpriteAsset(string key, bool localView)
        {
            var root = localView ? LocalTextureRoot : GameResRoot;
            var candidates = new[]
            {
                $"{root}/{key}.png",
                $"{root}/{key}.jpg",
                $"{root}/{key}.jpeg",
            };
            foreach (var path in candidates)
            {
                if (File.Exists(ToAbsolutePath(path))) return path;
            }
            return null;
        }

        private static string FindPackageImage(string packageDir, LanhuPackage package, LanhuNode node)
        {
            var raw = string.IsNullOrEmpty(node.source) ? node.image : node.source;
            raw = (raw ?? string.Empty).Replace('\\', '/').TrimStart('/');
            var assetRoot = string.IsNullOrEmpty(package.assetRoot) ? DefaultAssetRoot : package.assetRoot;
            var fileName = Path.GetFileName(raw);
            var candidates = new List<string>
            {
                Path.Combine(packageDir, raw),
                Path.Combine(packageDir, assetRoot, raw),
                Path.Combine(packageDir, assetRoot, fileName),
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path)) return path;
            }
            return null;
        }

        private static void ValidateImages(string packageDir, LanhuPackage package, LanhuView view, LanhuImportReport report)
        {
            foreach (var node in view.nodes)
            {
                ValidateImagesRecursive(packageDir, package, view.local, node, report);
            }
        }

        private static void ValidateImagesRecursive(string packageDir, LanhuPackage package, bool localView, LanhuNode node, LanhuImportReport report)
        {
            if (node == null) return;
            if (!string.IsNullOrEmpty(node.image))
            {
                var key = ResourcePath.Normalize(node.image);
                if (string.IsNullOrEmpty(FindExistingSpriteAsset(key, localView)) && string.IsNullOrEmpty(FindPackageImage(packageDir, package, node)))
                {
                    report.missingImages.Add($"{node.name}: {node.image}");
                }
            }

            if (node.children == null) return;
            foreach (var child in node.children)
            {
                ValidateImagesRecursive(packageDir, package, localView, child, report);
            }
        }

        private static Type ResolveViewType(LanhuPackage package, LanhuView view)
        {
            var moduleName = view.local ? "App" : ToPascal(package.module);
            var businessType = Type.GetType($"Shenxiao.Module.Core.{moduleName}.{view.name}, Shenxiao.Module.Core");
            if (businessType != null) return businessType;
            return Type.GetType($"Shenxiao.Generated.UI.{moduleName}.{view.name}Bind, Shenxiao.Generated");
        }

        private static void WireSerializedFields(Component viewComponent, List<LanhuBindField> fields, Dictionary<string, Component> wired, LanhuImportReport report)
        {
            var so = new SerializedObject(viewComponent);
            foreach (var field in fields)
            {
                if (!wired.TryGetValue(field.fieldName, out var component))
                {
                    report.warnings.Add($"{viewComponent.GetType().Name}: missing generated node for {field.fieldName}");
                    continue;
                }

                var prop = so.FindProperty(field.fieldName);
                if (prop == null)
                {
                    report.pendingComponents.Add($"{viewComponent.GetType().Name}: field {field.fieldName} not found; rerun after scripts compile.");
                    continue;
                }
                prop.objectReferenceValue = component;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Type ResolveBindComponentType(string fieldName, string nodeType)
        {
            var normalizedType = (nodeType ?? string.Empty).Trim().ToLowerInvariant();
            if (fieldName.StartsWith("_btn_") || normalizedType == "button") return typeof(Button);
            if (fieldName.StartsWith("_img_") || normalizedType == "image") return typeof(Image);
            if (fieldName.StartsWith("_txt_") || fieldName.StartsWith("_lb_") || fieldName.StartsWith("_lab_") || normalizedType == "text" || normalizedType == "label") return typeof(TextMeshProUGUI);
            if (fieldName.StartsWith("_input_") || fieldName.StartsWith("_ti_") || normalizedType == "input" || normalizedType == "textinput") return typeof(TMP_InputField);
            if (fieldName.StartsWith("_list_") || normalizedType == "list" || normalizedType == "scroll") return typeof(ScrollRect);
            if (fieldName.StartsWith("_tab_") || normalizedType == "tab") return typeof(ToggleGroup);
            if (fieldName.StartsWith("_chk_") || normalizedType == "checkbox" || normalizedType == "toggle") return typeof(Toggle);
            if (fieldName.StartsWith("_bar_") || normalizedType == "progressbar" || normalizedType == "slider") return typeof(Slider);
            if (fieldName.StartsWith("_dd_") || normalizedType == "dropdown" || normalizedType == "combobox") return typeof(TMP_Dropdown);
            if (fieldName.StartsWith("_clip_") || normalizedType == "clip" || normalizedType == "mask") return typeof(RectMask2D);
            return typeof(RectTransform);
        }

        private static string GetGeneratedTypeName(Type type)
        {
            if (type == typeof(Button)) return "Button";
            if (type == typeof(Image)) return "Image";
            if (type == typeof(TextMeshProUGUI)) return "TextMeshProUGUI";
            if (type == typeof(TMP_InputField)) return "TMP_InputField";
            if (type == typeof(ScrollRect)) return "ScrollRect";
            if (type == typeof(ToggleGroup)) return "ToggleGroup";
            if (type == typeof(Toggle)) return "Toggle";
            if (type == typeof(Slider)) return "Slider";
            if (type == typeof(TMP_Dropdown)) return "TMP_Dropdown";
            if (type == typeof(RectMask2D)) return "RectMask2D";
            return "RectTransform";
        }

        private static string GetViewAddress(LanhuPackage package, LanhuView view)
        {
            if (view.local) return $"ui/{view.name}".ToLowerInvariant();
            return $"prefabs/ui/{ToPascal(package.module)}/{view.name}".ToLowerInvariant();
        }

        private static string NormalizeLayer(string layer)
        {
            if (Enum.TryParse(layer, true, out UILayer parsed)) return parsed.ToString();
            return UILayer.Window.ToString();
        }

        private static bool IsBindCandidate(string fieldName)
        {
            return !string.IsNullOrEmpty(fieldName) && fieldName[0] == '_';
        }

        private static string MakeFieldName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            var sb = new StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                var c = raw[i];
                sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            }
            if (sb.Length > 0 && char.IsDigit(sb[0])) sb.Insert(0, '_');
            return sb.ToString();
        }

        private static string ToPascal(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "App";
            var sb = new StringBuilder(raw.Length);
            var upper = true;
            for (int i = 0; i < raw.Length; i++)
            {
                var c = raw[i];
                if (!char.IsLetterOrDigit(c))
                {
                    upper = true;
                    continue;
                }
                sb.Append(upper ? char.ToUpperInvariant(c) : c);
                upper = false;
            }
            return sb.Length == 0 ? "App" : sb.ToString();
        }

        private static Color ParseColor(string text, Color fallback, float? alpha)
        {
            var color = fallback;
            if (!string.IsNullOrEmpty(text) && ColorUtility.TryParseHtmlString(text, out var parsed))
            {
                color = parsed;
            }
            if (alpha.HasValue) color.a = Mathf.Clamp01(alpha.Value);
            return color;
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            var parts = assetFolder.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, assetPath).Replace('\\', '/');
        }

        private static void WriteReport(string packageDir, string module, LanhuImportReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Lanhu Import Report");
            sb.AppendLine();
            sb.AppendLine($"module: {module}");
            sb.AppendLine($"views: {report.views}");
            sb.AppendLine($"prefabs: {report.prefabs}");
            sb.AppendLine($"bindScripts: {report.bindScripts}");
            sb.AppendLine($"copiedImages: {report.copiedImages}");
            AppendList(sb, "Missing Images", report.missingImages);
            AppendList(sb, "Pending Components", report.pendingComponents);
            AppendList(sb, "Warnings", report.warnings);
            File.WriteAllText(Path.Combine(packageDir, "_LanhuReport.md"), sb.ToString(), Encoding.UTF8);
        }

        private static void AppendList(StringBuilder sb, string title, List<string> items)
        {
            sb.AppendLine();
            sb.AppendLine("## " + title);
            if (items.Count == 0)
            {
                sb.AppendLine("- none");
                return;
            }
            foreach (var item in items)
            {
                sb.AppendLine("- " + item);
            }
        }

        private static string BuildDialogSummary(LanhuImportReport report)
        {
            return $"views={report.views}\nprefabs={report.prefabs}\nbindScripts={report.bindScripts}\ncopiedImages={report.copiedImages}\nmissingImages={report.missingImages.Count}\npendingComponents={report.pendingComponents.Count}";
        }
    }
}
