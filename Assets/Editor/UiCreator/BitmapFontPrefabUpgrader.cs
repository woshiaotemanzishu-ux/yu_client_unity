using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Shenxiao.Editor.LayaUI;
using Shenxiao.EditorTools.AddrSetup;
using Shenxiao.Module.Core.Common;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator
{
    /// <summary>
    /// 老端位图字体一次性同步/落袋工具。
    ///
    /// 事实源是 Electron「位图字体」工具默认指向的 yu_client/cdn/resource/font；先同步全部 FNT/PNG，
    /// 再读取当前 CDN scene 的 font 属性和 h5/src 里仍在执行的 LoadFont 调用，把对应 TMP_FontAsset
    /// 直接保存进现有 Prefab。这样人工接管后的 Prefab 仍是视觉事实源，运行时只更新字符串。
    /// </summary>
    public static class BitmapFontPrefabUpgrader
    {
        private const string FontAssetFolder = "Assets/GameRes/Fonts/Bitmap";
        private const string SkillNameAssetFolder = "Assets/GameRes/resource/game/skillName";
        private const string TmpSettingsPath = "Assets/Resources/TMP Settings.asset";

        private static readonly Regex ExportClassRegex = new Regex(
            @"export\s+class\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

        private static readonly Regex LoadFontRegex = new Regex(
            @"LoadFont\s*\(\s*this\s*,\s*[\""'](?<font>[^\""']+)[\""']\s*,\s*this\.(?<node>[A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.Compiled);

        [MenuItem("神霄/资源/同步并应用老端位图字体", priority = 21)]
        public static void SyncBuildAndApply()
        {
            if (!LayaUISettings.ValidateClientRoot(out string error))
                throw new InvalidOperationException(error);

            int copied = SyncSourceFiles();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureTmpSettings();
            ConfigureSkillNameSprites();
            BitmapFontAssetBuilder.BuildAllLegacyFonts();

            Dictionary<string, Dictionary<string, string>> bindings = CollectBindings();
            int changedPrefabs = 0;
            int changedLabels = 0;
            var matchedBindings = new Dictionary<string, string>(StringComparer.Ordinal);
            ApplyBindings(bindings, ref changedPrefabs, ref changedLabels, matchedBindings);

            AddressableSetup.SyncBitmapFontEntries();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int fontCount = Directory.GetFiles(FontAssetFolder, "*.fnt").Length;
            int assetCount = Directory.GetFiles(FontAssetFolder, "*.asset").Length;
            if (assetCount != fontCount)
                throw new InvalidOperationException($"位图字体资产数不完整: asset={assetCount}, fnt={fontCount}");

            WriteInventory(bindings, matchedBindings, copied, changedPrefabs, changedLabels, fontCount, assetCount);

            Debug.Log($"[BitmapFont] 同步/应用完成: copied={copied}, fonts={assetCount}, " +
                      $"bindings={bindings.Sum(v => v.Value.Count)}, matched={matchedBindings.Count}, " +
                      $"prefabs={changedPrefabs}, labels={changedLabels}");
        }

        /// <summary>batchmode 入口。成功/失败显式退出，避免脚本重载后 -quit 提前抢跑。</summary>
        public static void SyncBuildAndApplyBatch()
        {
            try
            {
                SyncBuildAndApply();
                Debug.Log("[BitmapFont] SyncBuildAndApplyBatch OK");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[BitmapFont] SyncBuildAndApplyBatch FAILED: " + e);
                EditorApplication.Exit(1);
            }
        }

        private static int SyncSourceFiles()
        {
            string clientRoot = LayaUISettings.ClientRoot;
            string fontSource = Path.Combine(clientRoot, "cdn", "resource", "font");
            string skillSource = Path.Combine(clientRoot, "cdn", "resource", "game", "skillName");
            if (!Directory.Exists(fontSource)) throw new DirectoryNotFoundException(fontSource);
            if (!Directory.Exists(skillSource)) throw new DirectoryNotFoundException(skillSource);

            Directory.CreateDirectory(FontAssetFolder);
            Directory.CreateDirectory(SkillNameAssetFolder);
            int copied = 0;
            foreach (string source in Directory.GetFiles(fontSource, "*", SearchOption.TopDirectoryOnly))
            {
                string ext = Path.GetExtension(source);
                if (!ext.Equals(".fnt", StringComparison.OrdinalIgnoreCase)
                    && !ext.Equals(".png", StringComparison.OrdinalIgnoreCase)) continue;
                copied += CopyIfDifferent(source, Path.Combine(FontAssetFolder, Path.GetFileName(source))) ? 1 : 0;
            }
            foreach (string source in Directory.GetFiles(skillSource, "*.png", SearchOption.TopDirectoryOnly))
                copied += CopyIfDifferent(source, Path.Combine(SkillNameAssetFolder, Path.GetFileName(source))) ? 1 : 0;
            return copied;
        }

        private static bool CopyIfDifferent(string source, string destination)
        {
            byte[] next = File.ReadAllBytes(source);
            if (File.Exists(destination) && File.ReadAllBytes(destination).SequenceEqual(next)) return false;
            File.WriteAllBytes(destination, next);
            return true;
        }

        private static void EnsureTmpSettings()
        {
            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            bool created = settings == null;
            if (settings == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                settings = ScriptableObject.CreateInstance<TMP_Settings>();
                AssetDatabase.CreateAsset(settings, TmpSettingsPath);
            }

            TMP_FontAsset primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_App/Fonts/DFPYuanW7 SDF.asset");
            TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_App/Fonts/FZYHJW SDF.asset");
            var serialized = new SerializedObject(settings);
            bool changed = false;
            if (created)
            {
                // 对齐 TMP Essential Resources 的通用默认值，避免“仅为位图字体补 Settings”反而
                // 让后续新建文本继承 0 字号、0 容器和 0 自动缩放比例。
                SetInt(serialized, "m_TextWrappingMode", 1);
                SetBool(serialized, "m_enableKerning", true);
                SetBool(serialized, "m_enableParseEscapeCharacters", true);
                SetFloat(serialized, "m_defaultFontSize", 36f);
                SetFloat(serialized, "m_defaultAutoSizeMinRatio", 0.5f);
                SetFloat(serialized, "m_defaultAutoSizeMaxRatio", 2f);
                SetVector2(serialized, "m_defaultTextMeshProTextContainerSize", new Vector2(20f, 5f));
                SetVector2(serialized, "m_defaultTextMeshProUITextContainerSize", new Vector2(200f, 50f));
                SetBool(serialized, "m_matchMaterialPreset", true);
                SetBool(serialized, "m_HideSubTextObjects", false);
                SetBool(serialized, "m_enableEmojiSupport", true);
                changed = true;
            }
            SerializedProperty version = serialized.FindProperty("assetVersion");
            if (version != null && version.stringValue != "2")
            {
                version.stringValue = "2";
                changed = true;
            }
            SerializedProperty defaultFont = serialized.FindProperty("m_defaultFontAsset");
            if (defaultFont != null && defaultFont.objectReferenceValue != primary)
            {
                defaultFont.objectReferenceValue = primary;
                changed = true;
            }
            SerializedProperty fallbacks = serialized.FindProperty("m_fallbackFontAssets");
            if (fallbacks != null && (fallbacks.arraySize != (fallback == null ? 0 : 1)
                                      || (fallback != null && fallbacks.GetArrayElementAtIndex(0).objectReferenceValue != fallback)))
            {
                fallbacks.ClearArray();
                if (fallback != null)
                {
                    fallbacks.InsertArrayElementAtIndex(0);
                    fallbacks.GetArrayElementAtIndex(0).objectReferenceValue = fallback;
                }
                changed = true;
            }
            if (!changed) return;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static void SetBool(SerializedObject serialized, string name, bool value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null) property.boolValue = value;
        }

        private static void SetInt(SerializedObject serialized, string name, int value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null) property.intValue = value;
        }

        private static void SetFloat(SerializedObject serialized, string name, float value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null) property.floatValue = value;
        }

        private static void SetVector2(SerializedObject serialized, string name, Vector2 value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null) property.vector2Value = value;
        }

        private static void ConfigureSkillNameSprites()
        {
            foreach (string rawPath in Directory.GetFiles(SkillNameAssetFolder, "*.png", SearchOption.TopDirectoryOnly))
            {
                string path = rawPath.Replace('\\', '/');
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                bool changed = importer.textureType != TextureImporterType.Sprite
                               || importer.spriteImportMode != SpriteImportMode.Single
                               || importer.mipmapEnabled
                               || importer.textureCompression != TextureImporterCompression.Uncompressed
                               || importer.wrapMode != TextureWrapMode.Clamp;
                if (!changed) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static Dictionary<string, Dictionary<string, string>> CollectBindings()
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            string gameRoot = Path.Combine(LayaUISettings.CdnResourceRoot, "game");
            foreach (string scenePath in Directory.GetFiles(gameRoot, "*.scene", SearchOption.AllDirectories))
            {
                JObject root;
                try { root = JObject.Parse(File.ReadAllText(scenePath)); }
                catch { continue; }
                string view = (string)root["props"]?["name"] ?? Path.GetFileNameWithoutExtension(scenePath);
                CollectSceneBindings(root, view, result);
            }

            // scene 里的 font 是编辑器初值；现行 TS 的 LoadFont 是运行时最终值，必须后写覆盖。
            string sourceRoot = Path.Combine(LayaUISettings.ClientRoot, "h5", "src");
            foreach (string tsPath in Directory.GetFiles(sourceRoot, "*.ts", SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(tsPath);
                Match classMatch = ExportClassRegex.Match(source);
                if (!classMatch.Success) continue;
                string view = classMatch.Groups["name"].Value;
                foreach (string rawLine in source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                {
                    string line = rawLine;
                    int comment = line.IndexOf("//", StringComparison.Ordinal);
                    if (comment >= 0) line = line.Substring(0, comment);
                    Match m = LoadFontRegex.Match(line);
                    if (!m.Success) continue;
                    AddBinding(result, view, m.Groups["node"].Value, m.Groups["font"].Value);
                }
            }

            // 两个公共件使用字段/分支决定字体，不能由字面量正则完整恢复。
            AddBinding(result, "FightingShowSmallItem", "_lb_fighting", "num_new");
            AddBinding(result, "FightingUpItem", "_lb_fighting", "num_new_green");
            // BindJageWishView 用 this[`_lab_multiple_${i}`] 动态索引，普通字面量正则无法展开。
            for (int i = 1; i <= 5; i++)
                AddBinding(result, "BindJageWishView", "_lab_multiple_" + i, "bind_jage_multiple");
            return result;
        }

        private static void CollectSceneBindings(JToken node, string view,
            Dictionary<string, Dictionary<string, string>> result)
        {
            if (!(node is JObject obj)) return;
            if (obj["props"] is JObject props)
            {
                string name = (string)props["name"];
                string font = (string)props["font"];
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(font)
                    && File.Exists(FontAssetFolder + "/" + font + ".asset"))
                    AddBinding(result, view, name, font);
            }
            if (!(obj["child"] is JArray children)) return;
            foreach (JToken child in children) CollectSceneBindings(child, view, result);
        }

        private static void AddBinding(Dictionary<string, Dictionary<string, string>> result,
            string view, string node, string font)
        {
            if (string.IsNullOrEmpty(view) || string.IsNullOrEmpty(node) || string.IsNullOrEmpty(font)) return;
            if (!File.Exists(FontAssetFolder + "/" + font + ".asset")) return;
            if (!result.TryGetValue(view, out Dictionary<string, string> nodes))
            {
                nodes = new Dictionary<string, string>(StringComparer.Ordinal);
                result.Add(view, nodes);
            }
            nodes[node] = font;
        }

        private static void ApplyBindings(Dictionary<string, Dictionary<string, string>> bindings,
            ref int changedPrefabs, ref int changedLabels, Dictionary<string, string> matchedBindings)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/UI" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool dirty = false;
                try
                {
                    Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                    foreach (Transform viewRoot in transforms)
                    {
                        if (!bindings.TryGetValue(viewRoot.name, out Dictionary<string, string> nodes)) continue;
                        if (PrefabUtility.IsPartOfPrefabInstance(viewRoot.gameObject)) continue;

                        TextMeshProUGUI[] labels = viewRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
                        foreach (TextMeshProUGUI label in labels)
                        {
                            if (!BelongsToView(label.transform, viewRoot, bindings)) continue;
                            if (!nodes.TryGetValue(label.name, out string fontName)) continue;
                            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                                FontAssetFolder + "/" + fontName + ".asset");
                            if (font == null) continue;
                            matchedBindings[viewRoot.name + "/" + label.name] = path;
                            var serialized = new SerializedObject(label);
                            SerializedProperty fontAsset = serialized.FindProperty("m_fontAsset");
                            SerializedProperty material = serialized.FindProperty("m_sharedMaterial");
                            SerializedProperty color = serialized.FindProperty("m_fontColor");
                            if (fontAsset == null || material == null || color == null) continue;
                            if (fontAsset.objectReferenceValue == font
                                && material.objectReferenceValue == font.material
                                && color.colorValue == Color.white) continue;
                            fontAsset.objectReferenceValue = font;
                            material.objectReferenceValue = font.material;
                            color.colorValue = Color.white;
                            serialized.ApplyModifiedPropertiesWithoutUndo();
                            EditorUtility.SetDirty(label);
                            dirty = true;
                            changedLabels++;
                        }

                        if (viewRoot.name == "FightingUpItem")
                            dirty |= BindFightingUpStyles(viewRoot);
                    }
                    if (dirty)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        changedPrefabs++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static bool BelongsToView(Transform label, Transform viewRoot,
            Dictionary<string, Dictionary<string, string>> bindings)
        {
            for (Transform cursor = label.parent; cursor != null && cursor != viewRoot; cursor = cursor.parent)
            {
                if (bindings.ContainsKey(cursor.name)) return false;
            }
            return label == viewRoot || label.IsChildOf(viewRoot);
        }

        private static void WriteInventory(Dictionary<string, Dictionary<string, string>> bindings,
            Dictionary<string, string> matchedBindings, int copied, int changedPrefabs, int changedLabels,
            int fontCount, int assetCount)
        {
            string reportPath = "output/ui_route_audit/2026-08-05_bitmap-fonts/bitmap-font-inventory.json";
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            var rows = new JArray();
            foreach (KeyValuePair<string, Dictionary<string, string>> view in bindings.OrderBy(v => v.Key, StringComparer.Ordinal))
            {
                foreach (KeyValuePair<string, string> node in view.Value.OrderBy(v => v.Key, StringComparer.Ordinal))
                {
                    string key = view.Key + "/" + node.Key;
                    bool applied = matchedBindings.TryGetValue(key, out string prefab);
                    rows.Add(new JObject
                    {
                        ["view"] = view.Key,
                        ["node"] = node.Key,
                        ["font"] = node.Value,
                        ["appliedToCurrentPrefab"] = applied,
                        ["prefab"] = prefab ?? string.Empty,
                    });
                }
            }

            var report = new JObject
            {
                ["oldClientFontFolder"] = Path.GetFullPath(Path.Combine(LayaUISettings.ClientRoot, "cdn", "resource", "font")),
                ["oldClientSkillNameFolder"] = Path.GetFullPath(Path.Combine(LayaUISettings.ClientRoot, "cdn", "resource", "game", "skillName")),
                ["copiedThisRun"] = copied,
                ["fntCount"] = fontCount,
                ["tmpFontAssetCount"] = assetCount,
                ["skillNameImageCount"] = Directory.GetFiles(SkillNameAssetFolder, "*.png").Length,
                ["discoveredBindings"] = rows.Count,
                ["matchedCurrentPrefabBindings"] = matchedBindings.Count,
                ["changedPrefabs"] = changedPrefabs,
                ["changedLabels"] = changedLabels,
                ["bindings"] = rows,
            };
            File.WriteAllText(reportPath, report.ToString());
        }

        private static bool BindFightingUpStyles(Transform root)
        {
            FightingUpItem item = root.GetComponent<FightingUpItem>();
            if (item == null) return false;
            var serialized = new SerializedObject(item);
            SerializedProperty style1 = serialized.FindProperty("_style1Font");
            SerializedProperty style2 = serialized.FindProperty("_style2Font");
            if (style1 == null || style2 == null) return false;
            TMP_FontAsset f1 = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetFolder + "/num_new_green.asset");
            TMP_FontAsset f2 = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetFolder + "/view_fight_up.asset");
            bool changed = style1.objectReferenceValue != f1 || style2.objectReferenceValue != f2;
            if (!changed) return false;
            style1.objectReferenceValue = f1;
            style2.objectReferenceValue = f2;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return true;
        }
    }
}
