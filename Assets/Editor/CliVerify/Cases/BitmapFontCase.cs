using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Shenxiao.Editor.LayaUI;
using Shenxiao.Editor.UiCreator;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Scene;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>老端 BMFont、单图技能字、Prefab 绑定与 Addressables 闭包的统一可视化验收。</summary>
    public static class BitmapFontCase
    {
        private const string FontFolder = "Assets/GameRes/Fonts/Bitmap";
        private const string SkillNameFolder = "Assets/GameRes/resource/game/skillName";
        private const string OutputFolder = "output/ui_route_audit/2026-08-06_bitmap-font-remediation";
        // 每轮最终证据写入新目录，避免桌面查看器/报告仍映射旧 PNG 时被覆盖后误读。
        private const string EvidenceFolder = OutputFolder + "/evidence-r2";
        private const string InventoryPath = OutputFolder + "/bitmap-font-inventory.json";
        private static readonly JObject RenderedNonBackgroundPixels = new JObject();
        private static readonly string[] RequiredCombatFonts =
        {
            "fight_font_attack", "fight_font_beattack", "fight_font_baoji", "fight_font_huixin",
            "fight_font_zhuoyue", "fight_font_shenwu", "fight_font_gedang", "fight_font_fantan",
            "fight_font_liuxue", "fight_font_huifu",
        };

        public static void RunBatch()
        {
            int code = 0;
            try
            {
                BitmapFontPrefabUpgrader.SyncBuildAndApply();
                Run();
                Debug.Log("CLIVERIFY bitmap-font PASS");
            }
            catch (Exception e)
            {
                code = 3;
                Debug.LogError("CLIVERIFY bitmap-font FAIL\n" + e);
            }
            EditorApplication.Exit(code);
        }

        /// <summary>
        /// 2026-08-06 用户复查专项：只读两个 HUD Prefab、FightingUpView 与十套战斗字体，
        /// 不同步源目录、不重建 66 套字体、不改 Addressables，也不扫描全部 Prefab。
        /// </summary>
        public static void RunRemediationBatch()
        {
            int code = 0;
            try
            {
                RunRemediation();
                Debug.Log("CLIVERIFY bitmap-font-remediation PASS");
            }
            catch (Exception e)
            {
                code = 3;
                Debug.LogError("CLIVERIFY bitmap-font-remediation FAIL\n" + e);
            }
            EditorApplication.Exit(code);
        }

        [MenuItem("神霄/验证/位图字体复查专项（定向）", priority = 120)]
        public static void RunRemediationInEditor()
        {
            try
            {
                RunRemediation();
                Debug.Log("CLIVERIFY bitmap-font-remediation PASS");
            }
            catch (Exception e)
            {
                Debug.LogError("CLIVERIFY bitmap-font-remediation FAIL\n" + e);
                throw;
            }
        }

        private static void RunRemediation()
        {
            Require(SystemInfo.graphicsDeviceType.ToString() != "Null",
                "位图字体复查专项必须使用真实图形设备，禁止 -nographics");
            RenderedNonBackgroundPixels.RemoveAll();
            Directory.CreateDirectory(EvidenceFolder);

            var combatFonts = new List<TMP_FontAsset>();
            foreach (string name in RequiredCombatFonts)
            {
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontFolder + "/" + name + ".asset");
                Require(font != null && font.material != null && font.atlasTextures?.Length == 1,
                    "战斗位图字体资产不完整: " + name);
                combatFonts.Add(font);
            }

            VerifyCommonPrefabBindings();
            VerifyCombatMappings();
            var captures = new JArray
            {
                RenderTargetBindingSheet(),
                RenderCombatSheet(combatFonts),
            };
            var report = new JObject
            {
                ["status"] = "pass",
                ["scope"] = "HudTop._lb_fighting + HudTaskTeam._lb_open_awaken_progress + " +
                            "FightingUpView renamed binds + 10 combat bitmap fonts + runtime float bounds",
                ["resourceMutation"] = "none",
                ["captures"] = captures,
                ["renderedNonBackgroundPixels"] = RenderedNonBackgroundPixels,
            };
            File.WriteAllText(EvidenceFolder + "/bitmap-font-remediation-verification.json", report.ToString());
        }

        private static void Run()
        {
            Require(SystemInfo.graphicsDeviceType.ToString() != "Null", "位图字体视觉验收必须使用真实图形设备，禁止 -nographics");
            RenderedNonBackgroundPixels.RemoveAll();
            string[] fntPaths = Directory.GetFiles(FontFolder, "*.fnt").Select(Normalize).OrderBy(v => v).ToArray();
            string[] assetPaths = Directory.GetFiles(FontFolder, "*.asset").Select(Normalize).OrderBy(v => v).ToArray();
            string[] skillPaths = Directory.GetFiles(SkillNameFolder, "*.png").Select(Normalize).OrderBy(v => v).ToArray();
            Require(fntPaths.Length == 66, "BMFont 源文件应为 66，实际 " + fntPaths.Length);
            Require(assetPaths.Length == 66, "TMP 位图字体资产应为 66，实际 " + assetPaths.Length);
            Require(skillPaths.Length == 41, "技能名字图应为 41，实际 " + skillPaths.Length);

            var fonts = new List<TMP_FontAsset>();
            foreach (string path in assetPaths)
            {
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                Require(font != null, "字体资产加载失败: " + path);
                Require(font.glyphTable != null && font.glyphTable.Count > 0, "字体没有 glyph: " + path);
                Require(font.characterTable != null && font.characterTable.Count > 0, "字体没有 character: " + path);
                Require(font.atlasTextures != null && font.atlasTextures.Length == 1 && font.atlasTextures[0] != null,
                    "字体 atlas 未绑定: " + path);
                Require(font.material != null && font.material.shader != null
                    && font.material.shader.name == "TextMeshPro/Bitmap Custom Atlas", "字体材质/Shader 错误: " + path);
                fonts.Add(font);
            }

            foreach (string name in RequiredCombatFonts)
            {
                TMP_FontAsset font = fonts.FirstOrDefault(v => v.name == name);
                Require(font != null, "缺战斗字体: " + name);
                Require(HasCharacters(font, "0123456789a"), "战斗字体缺数字或图形字 a: " + name);
            }
            Require(HasCharacters(fonts.First(v => v.name == "fight_font_baoji"), "b"), "暴击字体缺图形字 b");
            Require(HasCharacters(fonts.First(v => v.name == "fight_font_shenwu"), "c"), "神武字体缺图形字 c");

            VerifyAddressables(assetPaths, skillPaths, fntPaths);
            VerifyTmpSettings();
            VerifySkillSprites(skillPaths);
            VerifySkillWhitelist(skillPaths);
            VerifyCommonPrefabBindings();
            VerifyCombatMappings();
            JObject firstInventory = JObject.Parse(File.ReadAllText(InventoryPath));
            Require(firstInventory.Value<int>("discoveredBindings") == 105,
                "老端位图字体绑定盘点数漂移: " + firstInventory.Value<int>("discoveredBindings"));
            Require(firstInventory.Value<int>("matchedCurrentPrefabBindings") == 68,
                "当前 Prefab 位图字体命中数漂移: " + firstInventory.Value<int>("matchedCurrentPrefabBindings"));

            Directory.CreateDirectory(EvidenceFolder);
            var captures = new JArray();
            const int perPage = 22;
            for (int start = 0, page = 1; start < fonts.Count; start += perPage, page++)
            {
                string path = RenderContactSheet(fonts.Skip(start).Take(perPage).ToList(), page);
                captures.Add(path);
            }
            captures.Add(RenderCombatSheet(fonts));
            captures.Add(RenderSkillNameSheet(skillPaths));

            // 第二次跑完整同步必须不再拷文件或改 Prefab，防止每次构建持续制造资源漂移。
            BitmapFontPrefabUpgrader.SyncBuildAndApply();
            JObject inventory = JObject.Parse(File.ReadAllText(InventoryPath));
            Require(inventory.Value<int>("copiedThisRun") == 0, "第二次字体预检仍在复制文件");
            Require(inventory.Value<int>("changedPrefabs") == 0 && inventory.Value<int>("changedLabels") == 0,
                "第二次字体预检仍在修改 Prefab");

            var report = new JObject
            {
                ["status"] = "pass",
                ["fontCount"] = fonts.Count,
                ["skillNameImageCount"] = skillPaths.Length,
                ["combatFonts"] = new JArray(RequiredCombatFonts),
                ["captures"] = captures,
                ["renderedNonBackgroundPixels"] = RenderedNonBackgroundPixels,
            };
            File.WriteAllText(EvidenceFolder + "/bitmap-font-verification.json", report.ToString());
        }

        private static void VerifyAddressables(string[] assetPaths, string[] skillPaths, string[] fntPaths)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Require(settings != null, "Addressables Settings 不存在");
            foreach (string path in assetPaths)
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                string expected = path.Substring("Assets/GameRes/".Length);
                expected = expected.Substring(0, expected.Length - Path.GetExtension(expected).Length)
                    .Replace('\\', '/').ToLowerInvariant();
                Require(entry != null && entry.address == expected, "字体 Addressable 错误: " + path);
                Require(entry.labels.Count(v => v.StartsWith("pack_", StringComparison.Ordinal)) == 1
                        && entry.labels.Contains("pack_fonts"), "字体 pack 标签错误: " + path);
            }
            foreach (string path in skillPaths)
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                string expected = path.Substring("Assets/GameRes/".Length);
                expected = expected.Substring(0, expected.Length - Path.GetExtension(expected).Length)
                    .Replace('\\', '/').ToLowerInvariant();
                Require(entry != null && entry.address == expected, "技能字图 Addressable 错误: " + path);
                Require(entry.labels.Count(v => v.StartsWith("pack_", StringComparison.Ordinal)) == 1
                        && entry.labels.Contains("pack_resource_game_skillname"), "技能字图 pack 标签错误: " + path);
            }
            foreach (string fnt in fntPaths)
                Require(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(fnt)) == null,
                    "FNT 原文件不应拥有 Addressable 地址: " + fnt);
            foreach (string png in Directory.GetFiles(FontFolder, "*.png").Select(Normalize))
                Require(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(png)) == null,
                    "字体 atlas 原图不应拥有 Addressable 地址: " + png);
        }

        private static void VerifyTmpSettings()
        {
            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/Resources/TMP Settings.asset");
            Require(settings != null, "TMP Settings 缺失");
            var serialized = new SerializedObject(settings);
            Require(serialized.FindProperty("m_defaultFontAsset")?.objectReferenceValue is TMP_FontAsset primary
                    && primary.name == "DFPYuanW7 SDF", "TMP Settings 主字体错误");
            SerializedProperty fallbacks = serialized.FindProperty("m_fallbackFontAssets");
            Require(fallbacks != null && fallbacks.arraySize == 1
                    && fallbacks.GetArrayElementAtIndex(0).objectReferenceValue is TMP_FontAsset fallback
                    && fallback.name == "FZYHJW SDF", "TMP Settings 后备字体错误");
            Require(Mathf.Approximately(serialized.FindProperty("m_defaultFontSize")?.floatValue ?? 0f, 36f),
                "TMP Settings 默认字号错误");
            Require(serialized.FindProperty("m_defaultTextMeshProUITextContainerSize")?.vector2Value
                    == new Vector2(200f, 50f), "TMP Settings 默认 UI 容器尺寸错误");
        }

        private static void VerifySkillSprites(IEnumerable<string> skillPaths)
        {
            foreach (string path in skillPaths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Require(importer != null && importer.textureType == TextureImporterType.Sprite
                    && importer.spriteImportMode == SpriteImportMode.Single, "技能字图不是单 Sprite: " + path);
                Require(AssetDatabase.LoadAssetAtPath<Sprite>(path) != null, "技能字图 Sprite 加载失败: " + path);
            }
        }

        private static void VerifySkillWhitelist(IReadOnlyCollection<string> skillPaths)
        {
            string configPath = Path.Combine(LayaUISettings.ClientRoot, "cdn", "resource", "config", "server",
                "config_key_value.json");
            JObject table = JObject.Parse(File.ReadAllText(configPath));
            string raw = table["20001"]?["value"]?.Value<string>();
            JArray ids = JArray.Parse(raw ?? "[]");
            Require(ids.Count == 34, "战斗技能字图白名单应为 34，实际 " + ids.Count);
            var fileNames = new HashSet<string>(skillPaths.Select(Path.GetFileNameWithoutExtension), StringComparer.Ordinal);
            foreach (JToken id in ids)
                Require(fileNames.Contains(id.ToString()), "白名单技能缺字图: " + id);
        }

        private static void VerifyCommonPrefabBindings()
        {
            VerifyPrefabFont("Assets/Prefabs/UI/Common/FightingShowSmallItem.prefab", "_lb_fighting", "num_new");
            VerifyPrefabFont("Assets/Prefabs/UI/Common/FightingUpItem.prefab", "_lb_fighting", "num_new_green");
            VerifyBoundPrefabFont("Assets/Prefabs/UI/MainUI/Regions/HudTop.prefab", "MainUITopView",
                "_lb_fighting", "num_new", 22f);
            VerifyBoundPrefabFont("Assets/Prefabs/UI/MainUI/Regions/HudTaskTeam.prefab", "MainUITaskTeamView",
                "_lb_open_awaken_progress", "temple_awaken_font", 22f * 15f / 14f);
            VerifyBoundPrefabFont("Assets/Prefabs/UI/MainUI/FightingUpView.prefab", "FightingUpView",
                "_lb_fight", "fight_up", 50f);
            VerifyBoundPrefabFont("Assets/Prefabs/UI/MainUI/FightingUpView.prefab", "FightingUpView",
                "_lb_add_fight", "fight_up2", 50f);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Common/FightingUpItem.prefab");
            FightingUpItem item = prefab != null ? prefab.GetComponent<FightingUpItem>() : null;
            Require(item != null, "FightingUpItem 脚本绑定缺失");
            var serialized = new SerializedObject(item);
            Require(serialized.FindProperty("_style1Font")?.objectReferenceValue is TMP_FontAsset f1 && f1.name == "num_new_green",
                "FightingUpItem style1 字体绑定错误");
            Require(serialized.FindProperty("_style2Font")?.objectReferenceValue is TMP_FontAsset f2 && f2.name == "view_fight_up",
                "FightingUpItem style2 字体绑定错误");
        }

        private static void VerifyCombatMappings()
        {
            MethodInfo resolve = typeof(DamageFontRenderer).GetMethod("ResolveStyle",
                BindingFlags.NonPublic | BindingFlags.Static);
            Require(resolve != null, "DamageFontRenderer.ResolveStyle 不存在");
            RequireStyle(resolve, 123, 0, false, "123", "fight_font_attack");
            RequireStyle(resolve, 123, 0, true, "123", "fight_font_beattack");
            RequireStyle(resolve, 123, 2, false, "a123", "fight_font_baoji");
            RequireStyle(resolve, 123, 2, true, "b123", "fight_font_baoji");
            RequireStyle(resolve, 123, 10, false, "a123", "fight_font_zhuoyue");
            RequireStyle(resolve, 123, 10, true, "b123", "fight_font_huixin");
            RequireStyle(resolve, 123, 6, false, "a123", "fight_font_gedang");
            RequireStyle(resolve, 123, 6, true, "b123", "fight_font_gedang");

            MethodInfo resolveSize = typeof(DamageFontRenderer).GetMethod("ResolveRenderedFontSize",
                BindingFlags.NonPublic | BindingFlags.Static);
            Require(resolveSize != null, "DamageFontRenderer.ResolveRenderedFontSize 不存在");
            TMP_FontAsset attackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                FontFolder + "/fight_font_attack.asset");
            Require(attackFont != null, "fight_font_attack 资产不存在");
            float renderedSize = (float)resolveSize.Invoke(null, new object[] { attackFont, 36f });
            Require(Mathf.Approximately(renderedSize, attackFont.faceInfo.pointSize),
                $"战斗位图字体没有按老端 1:1 原生尺寸绘制: actual={renderedSize}, native={attackFont.faceInfo.pointSize}");

            MethodInfo prepareLayout = typeof(DamageFontRenderer).GetMethod("PrepareTextLayout",
                BindingFlags.NonPublic | BindingFlags.Static);
            Require(prepareLayout != null, "DamageFontRenderer.PrepareTextLayout 不存在");
            RequireRuntimeCombatLayout(prepareLayout, attackFont, "1234567890123456789", renderedSize);

            TMP_FontAsset critFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                FontFolder + "/fight_font_baoji.asset");
            Require(critFont != null, "fight_font_baoji 资产不存在");
            float critSize = (float)resolveSize.Invoke(null, new object[] { critFont, 36f });
            RequireRuntimeCombatLayout(prepareLayout, critFont, "a1234567890123456789", critSize);
        }

        private static void RequireRuntimeCombatLayout(MethodInfo prepareLayout, TMP_FontAsset font, string text,
            float fontSize)
        {
            var canvasGo = new GameObject("CombatLayoutCheckCanvas", typeof(RectTransform), typeof(Canvas));
            var labelGo = new GameObject("DamageFont", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(canvasGo.transform, false);
            try
            {
                var rect = (RectTransform)labelGo.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0f);
                var label = labelGo.GetComponent<TextMeshProUGUI>();
                label.font = font;
                label.fontSharedMaterial = font.material;
                label.fontSize = fontSize;
                label.color = Color.white;
                label.text = text;

                prepareLayout.Invoke(null, new object[] { label, rect });
                label.ForceMeshUpdate(true, true);

                Require(label.mesh != null && label.mesh.vertexCount > 0,
                    $"战斗飘字没有生成网格: font={font.name}, text={text}");
                Bounds bounds = label.textBounds;
                Rect box = rect.rect;
                const float tolerance = 1f;
                Require(bounds.min.x >= box.xMin - tolerance && bounds.max.x <= box.xMax + tolerance
                        && bounds.min.y >= box.yMin - tolerance && bounds.max.y <= box.yMax + tolerance,
                    $"战斗飘字网格仍越出容器: font={font.name}, text={text}, " +
                    $"bounds=({bounds.min.x:F1},{bounds.min.y:F1})-({bounds.max.x:F1},{bounds.max.y:F1}), " +
                    $"rect=({box.xMin:F1},{box.yMin:F1})-({box.xMax:F1},{box.yMax:F1})");
                Require(rect.rect.width >= 480f && rect.rect.height >= 180f,
                    $"战斗飘字安全容器过小: font={font.name}, size={rect.rect.size}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasGo);
            }
        }

        private static void RequireStyle(MethodInfo resolve, long damage, int flag, bool defender,
            string expectedText, string expectedFont)
        {
            object result = resolve.Invoke(null, new object[] { damage, flag, defender });
            Type type = result.GetType();
            string text = (string)type.GetField("Item1")?.GetValue(result);
            string font = (string)type.GetField("Item2")?.GetValue(result);
            Require(text == expectedText && font == expectedFont,
                $"战斗图形字映射错误: damage={damage}, flag={flag}, defender={defender}, actual={text}/{font}");
        }

        private static void VerifyPrefabFont(string prefabPath, string nodeName, string fontName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Require(prefab != null, "Prefab 不存在: " + prefabPath);
            TextMeshProUGUI label = prefab.GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(v => v.name == nodeName);
            Require(label != null && label.font != null && label.font.name == fontName,
                $"Prefab 字体绑定错误: {prefabPath}/{nodeName}, expected={fontName}");
            Require(label.fontSharedMaterial == label.font.material && label.color == Color.white,
                "Prefab 位图字体材质或颜色错误: " + prefabPath + "/" + nodeName);
        }

        private static void VerifyBoundPrefabFont(string prefabPath, string viewTypeName, string fieldName,
            string fontName, float expectedSize)
        {
            TextMeshProUGUI label = FindBoundPrefabLabel(prefabPath, viewTypeName, fieldName);
            Require(label.font != null && label.font.name == fontName,
                $"Prefab Bind 字段字体错误: {prefabPath}/{viewTypeName}.{fieldName}, expected={fontName}");
            Require(Mathf.Abs(label.fontSize - expectedSize) <= 0.01f,
                $"Prefab Bind 字段位图字号错误: {prefabPath}/{viewTypeName}.{fieldName}, " +
                $"actual={label.fontSize}, expected={expectedSize}");
            Require(label.fontSharedMaterial == label.font.material && label.color == Color.white,
                $"Prefab Bind 字段位图材质或颜色错误: {prefabPath}/{viewTypeName}.{fieldName}");
        }

        private static TextMeshProUGUI FindBoundPrefabLabel(string prefabPath, string viewTypeName, string fieldName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Require(prefab != null, "Prefab 不存在: " + prefabPath);
            foreach (MonoBehaviour component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().Name != viewTypeName) continue;
                var serialized = new SerializedObject(component);
                SerializedProperty property = serialized.FindProperty(fieldName);
                if (property?.objectReferenceValue is TextMeshProUGUI label) return label;
            }
            throw new InvalidOperationException(
                $"Prefab Bind 字段不存在或不是 TMP: {prefabPath}/{viewTypeName}.{fieldName}");
        }

        private static string RenderTargetBindingSheet()
        {
            const int width = 900;
            const int height = 420;
            CreateStage(width, height, out RenderTexture rt, out Camera camera, out GameObject canvasGo);
            TMP_FontAsset labelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_App/Fonts/FZYHJW SDF.asset");
            AddText(canvasGo.transform, "2026-08-06 位图字体遗漏复查", labelFont, 30f,
                new Vector2(18f, -15f), new Vector2(width - 36f, 48f), new Color(1f, 0.88f, 0.35f, 1f));

            TextMeshProUGUI power = FindBoundPrefabLabel(
                "Assets/Prefabs/UI/MainUI/Regions/HudTop.prefab", "MainUITopView", "_lb_fighting");
            AddText(canvasGo.transform, "MainUITopView._lb_fighting → num_new / native 22px", labelFont, 20f,
                new Vector2(22f, -90f), new Vector2(540f, 46f), new Color(0.72f, 0.78f, 0.88f, 1f));
            AddText(canvasGo.transform, "293342", power.font, power.fontSize,
                new Vector2(590f, -88f), new Vector2(270f, 52f), Color.white);

            TextMeshProUGUI awaken = FindBoundPrefabLabel(
                "Assets/Prefabs/UI/MainUI/Regions/HudTaskTeam.prefab", "MainUITaskTeamView",
                "_lb_open_awaken_progress");
            AddText(canvasGo.transform, "MainUITaskTeamView._lb_open_awaken_progress → temple_awaken_font / 22×15÷14",
                labelFont, 20f, new Vector2(22f, -190f), new Vector2(650f, 52f),
                new Color(0.72f, 0.78f, 0.88f, 1f));
            AddText(canvasGo.transform, "10/35", awaken.font, awaken.fontSize,
                new Vector2(700f, -186f), new Vector2(170f, 58f), Color.white);

            return CaptureAndDestroy(rt, camera, canvasGo,
                EvidenceFolder + "/hud-bitmap-font-remediation.png");
        }

        private static string RenderContactSheet(IReadOnlyList<TMP_FontAsset> fonts, int page)
        {
            const int width = 900;
            const int height = 1280;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var cameraGo = new GameObject("BitmapFontEvidenceCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.065f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.targetTexture = rt;

            var canvasGo = new GameObject("BitmapFontEvidenceCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            TMP_FontAsset labelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_App/Fonts/FZYHJW SDF.asset");

            AddText(canvasGo.transform, $"旧端位图字体视觉证据  {page}/3", labelFont, 28f,
                new Vector2(18f, -15f), new Vector2(width - 36f, 42f), new Color(1f, 0.88f, 0.35f, 1f));
            for (int i = 0; i < fonts.Count; i++)
            {
                TMP_FontAsset font = fonts[i];
                float y = -68f - i * 53f;
                AddText(canvasGo.transform, font.name, labelFont, 18f, new Vector2(18f, y),
                    new Vector2(330f, 46f), new Color(0.72f, 0.78f, 0.88f, 1f));
                TextMeshProUGUI sample = AddText(canvasGo.transform, BuildSample(font), font, 36f,
                    new Vector2(345f, y - 1f), new Vector2(535f, 48f), Color.white);
                sample.ForceMeshUpdate();
                Require(sample.textInfo.characterCount > 0 && sample.mesh != null && sample.mesh.vertexCount > 0,
                    "字体未生成可绘制网格: " + font.name);
            }

            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            string path = EvidenceFolder + $"/bitmap-font-contact-sheet-{page}.png";
            ValidateRenderedPixels(texture, path);
            File.WriteAllBytes(path, texture.EncodeToPNG());

            UnityEngine.Object.DestroyImmediate(texture);
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(canvasGo);
            UnityEngine.Object.DestroyImmediate(cameraGo);
            UnityEngine.Object.DestroyImmediate(rt);
            return path.Replace('\\', '/');
        }

        private static string RenderCombatSheet(IReadOnlyCollection<TMP_FontAsset> fonts)
        {
            var rows = new[]
            {
                new[] { "主角普通攻击", "fight_font_attack", "123456" },
                new[] { "主角闪避字形", "fight_font_attack", "a" },
                new[] { "主角暴击", "fight_font_baoji", "a123456" },
                new[] { "主角卓越", "fight_font_zhuoyue", "a123456" },
                new[] { "主角会心", "fight_font_huixin", "a123456" },
                new[] { "主角攻击格挡", "fight_font_gedang", "a123456" },
                new[] { "主角普通受击", "fight_font_beattack", "123456" },
                new[] { "主角暴击受击", "fight_font_baoji", "b123456" },
                new[] { "主角卓越会心受击", "fight_font_huixin", "b123456" },
                new[] { "伙伴神武", "fight_font_shenwu", "a123456" },
                new[] { "反弹", "fight_font_fantan", "a123456" },
                new[] { "回血", "fight_font_huifu", "a123456" },
            };
            const int width = 900;
            const int height = 1280;
            CreateStage(width, height, out RenderTexture rt, out Camera camera, out GameObject canvasGo);
            TMP_FontAsset labelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_App/Fonts/FZYHJW SDF.asset");
            AddText(canvasGo.transform, "战斗图片字真实映射", labelFont, 30f,
                new Vector2(18f, -15f), new Vector2(width - 36f, 48f), new Color(1f, 0.88f, 0.35f, 1f));
            for (int i = 0; i < rows.Length; i++)
            {
                float y = -85f - i * 92f;
                AddText(canvasGo.transform, rows[i][0] + "  " + rows[i][1], labelFont, 20f,
                    new Vector2(22f, y), new Vector2(390f, 55f), new Color(0.72f, 0.78f, 0.88f, 1f));
                TMP_FontAsset font = fonts.First(v => v.name == rows[i][1]);
                TextMeshProUGUI sample = AddText(canvasGo.transform, rows[i][2], font, font.faceInfo.pointSize,
                    new Vector2(420f, y - 3f), new Vector2(455f, 120f), Color.white);
                sample.ForceMeshUpdate();
                Require(sample.mesh != null && sample.mesh.vertexCount > 0, "战斗图片字没有实际网格: " + rows[i][0]);
            }
            return CaptureAndDestroy(rt, camera, canvasGo, EvidenceFolder + "/combat-bitmap-font-mapping.png");
        }

        private static string RenderSkillNameSheet(IReadOnlyList<string> skillPaths)
        {
            const int width = 900;
            const int height = 1280;
            CreateStage(width, height, out RenderTexture rt, out Camera camera, out GameObject canvasGo);
            TMP_FontAsset labelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_App/Fonts/FZYHJW SDF.asset");
            AddText(canvasGo.transform, "单图技能名字图（41 张，白名单 34 张）", labelFont, 28f,
                new Vector2(18f, -15f), new Vector2(width - 36f, 44f), new Color(1f, 0.88f, 0.35f, 1f));
            const int columns = 4;
            const float cellW = 220f;
            const float cellH = 105f;
            for (int i = 0; i < skillPaths.Count; i++)
            {
                int column = i % columns;
                int row = i / columns;
                float x = 10f + column * cellW;
                float y = -68f - row * cellH;
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(skillPaths[i]);
                Require(sprite != null, "技能名字图加载失败: " + skillPaths[i]);
                AddText(canvasGo.transform, Path.GetFileNameWithoutExtension(skillPaths[i]), labelFont, 15f,
                    new Vector2(x + 4f, y), new Vector2(cellW - 8f, 22f), new Color(0.72f, 0.78f, 0.88f, 1f));
                var go = new GameObject("SkillName", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(canvasGo.transform, false);
                var imageRt = (RectTransform)go.transform;
                imageRt.anchorMin = imageRt.anchorMax = new Vector2(0f, 1f);
                imageRt.pivot = new Vector2(0.5f, 0.5f);
                float scale = Mathf.Min((cellW - 16f) / sprite.rect.width, 70f / sprite.rect.height);
                imageRt.sizeDelta = new Vector2(sprite.rect.width * scale, sprite.rect.height * scale);
                imageRt.anchoredPosition = new Vector2(x + cellW * 0.5f, y - 62f);
                Image image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            return CaptureAndDestroy(rt, camera, canvasGo, EvidenceFolder + "/skill-name-image-sheet.png");
        }

        private static void CreateStage(int width, int height, out RenderTexture rt, out Camera camera,
            out GameObject canvasGo)
        {
            rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var cameraGo = new GameObject("BitmapFontEvidenceCamera");
            camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.065f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.targetTexture = rt;
            canvasGo = new GameObject("BitmapFontEvidenceCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvasGo.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        }

        private static string CaptureAndDestroy(RenderTexture rt, Camera camera, GameObject canvasGo, string path)
        {
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            ValidateRenderedPixels(texture, path);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(canvasGo);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(rt);
            return path.Replace('\\', '/');
        }

        private static void ValidateRenderedPixels(Texture2D texture, string path)
        {
            Color32[] pixels = texture.GetPixels32();
            var histogram = new Dictionary<uint, int>();
            int dominant = 0;
            foreach (Color32 pixel in pixels)
            {
                uint key = ((uint)pixel.r << 24) | ((uint)pixel.g << 16) | ((uint)pixel.b << 8) | pixel.a;
                int count = histogram.TryGetValue(key, out int current) ? current + 1 : 1;
                histogram[key] = count;
                if (count > dominant) dominant = count;
            }
            int nonBackground = pixels.Length - dominant;
            Require(nonBackground >= 1000,
                $"渲染证据没有真实内容像素: {path}, nonBackground={nonBackground}");
            RenderedNonBackgroundPixels[Path.GetFileName(path)] = nonBackground;
        }

        private static TextMeshProUGUI AddText(Transform parent, string text, TMP_FontAsset font, float size,
            Vector2 anchoredPosition, Vector2 dimensions, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = dimensions;
            var label = go.GetComponent<TextMeshProUGUI>();
            label.font = font;
            if (font != null) label.fontSharedMaterial = font.material;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        private static string BuildSample(TMP_FontAsset font)
        {
            return string.Concat(font.characterTable
                .Select(v => v.unicode)
                .Where(v => v >= 32 && v <= 0x10FFFF)
                .Distinct()
                .OrderBy(v => v)
                .Take(24)
                .Select(v => char.ConvertFromUtf32((int)v)));
        }

        private static bool HasCharacters(TMP_FontAsset font, string chars)
        {
            return font != null && chars.All(c => font.HasCharacter(c));
        }

        private static string Normalize(string path) => path.Replace('\\', '/');

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
