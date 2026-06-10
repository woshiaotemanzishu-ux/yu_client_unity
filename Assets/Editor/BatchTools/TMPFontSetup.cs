using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Shenxiao.EditorTools.Fonts
{
    /// <summary>
    /// One-click TMP font setup for the project's CJK TTFs.
    /// Creates Dynamic OS / Dynamic SDF font assets so glyphs are rasterized at runtime
    /// (no need to bake the full CJK character set, keeping the build small).
    ///
    /// Convention:
    ///   Assets/_App/Fonts/DFPYuanW7.TTF  -> Assets/_App/Fonts/DFPYuanW7 SDF.asset  (default)
    ///   Assets/_App/Fonts/FZYHJW.TTF     -> Assets/_App/Fonts/FZYHJW SDF.asset    (fallback)
    /// </summary>
    public static class TMPFontSetup
    {
        private const string FontFolder = "Assets/_App/Fonts";
        private const int AtlasSize = 2048;

        [MenuItem("神霄/字体/默认 TMP 字体（按需生成）", priority = 30)]
        public static void Setup()
        {
            DoSetup(forceRebuild: false);
        }

        [MenuItem("神霄/字体/默认 TMP 字体（强制重建）", priority = 31)]
        public static void ForceSetup()
        {
            DoSetup(forceRebuild: true);
        }

        private static void DoSetup(bool forceRebuild)
        {
            var primary = CreateOrUpdate("DFPYuanW7.TTF", "DFPYuanW7 SDF", forceRebuild);
            var fallback = CreateOrUpdate("FZYHJW.TTF",   "FZYHJW SDF",    forceRebuild);
            if (primary == null)
            {
                EditorUtility.DisplayDialog("TMPFontSetup",
                    "DFPYuanW7.TTF not found under Assets/_App/Fonts/.", "OK");
                return;
            }

            // Hook fallback into primary, then assign primary as TMP default + fallback list.
            if (fallback != null)
            {
                if (primary.fallbackFontAssetTable == null)
                    primary.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                if (!primary.fallbackFontAssetTable.Contains(fallback))
                    primary.fallbackFontAssetTable.Add(fallback);
                EditorUtility.SetDirty(primary);
            }

            var tmpSettings = TMP_Settings.instance;
            if (tmpSettings != null)
            {
                var so = new SerializedObject(tmpSettings);
                SetObjectRefIfExists(so, primary, "m_defaultFontAsset");

                var globalFallback = so.FindProperty("m_fallbackFontAssets");
                if (globalFallback != null)
                {
                    globalFallback.ClearArray();
                    if (fallback != null)
                    {
                        globalFallback.InsertArrayElementAtIndex(0);
                        globalFallback.GetArrayElementAtIndex(0).objectReferenceValue = fallback;
                    }
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(tmpSettings);
            }
            else
            {
                Debug.LogWarning("[TMPFontSetup] TMP_Settings asset not found. Default font not auto-assigned. " +
                    "Open Project Settings -> TextMeshPro -> Settings to create one, then re-run.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TMPFontSetup] done. default={(primary ? primary.name : "null")}, fallback={(fallback ? fallback.name : "null")}");
        }

        private static void SetObjectRefIfExists(SerializedObject so, Object value, params string[] candidateNames)
        {
            foreach (var name in candidateNames)
            {
                var prop = so.FindProperty(name);
                if (prop != null) { prop.objectReferenceValue = value; return; }
            }
            Debug.LogWarning($"[TMPFontSetup] none of the SerializedProperty names found: {string.Join(", ", candidateNames)}");
        }

        private static TMP_FontAsset CreateOrUpdate(string ttfFileName, string assetName, bool forceRebuild)
        {
            var ttfPath = $"{FontFolder}/{ttfFileName}";
            if (!File.Exists(ttfPath))
            {
                Debug.LogWarning($"[TMPFontSetup] missing: {ttfPath}");
                return null;
            }
            var src = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (src == null)
            {
                AssetDatabase.ImportAsset(ttfPath);
                src = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            }
            if (src == null)
            {
                Debug.LogError($"[TMPFontSetup] failed to load Font from {ttfPath}");
                return null;
            }

            var assetPath = $"{FontFolder}/{assetName}.asset";
            if (forceRebuild)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
            else if (File.Exists(assetPath))
            {
                // Detect broken assets via sub-asset enumeration. A healthy SDF .asset has
                // both a Texture2D and a Material as sub-assets. If either is missing, rebuild.
                var subs = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                bool hasTex = false, hasMat = false, hasFa = false;
                foreach (var s in subs)
                {
                    if (s == null) continue;
                    if (s is TMP_FontAsset) hasFa = true;
                    else if (s is Texture2D) hasTex = true;
                    else if (s is Material) hasMat = true;
                }
                if (hasFa && hasTex && hasMat)
                {
                    return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
                }
                Debug.Log($"[TMPFontSetup] rebuilding broken asset: {assetPath} (tex={hasTex}, mat={hasMat})");
                AssetDatabase.DeleteAsset(assetPath);
            }

            var fa = TMP_FontAsset.CreateFontAsset(
                src,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: AtlasSize,
                atlasHeight: AtlasSize,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);
            fa.name = assetName;

            AssetDatabase.CreateAsset(fa, assetPath);

            // CreateFontAsset returns a font asset whose atlas Texture2D and Material live only in memory.
            // They MUST be added as sub-assets, otherwise serialization drops the references on reload.
            if (fa.atlasTextures != null)
            {
                foreach (var tex in fa.atlasTextures)
                {
                    if (tex != null && AssetDatabase.GetAssetPath(tex) == string.Empty)
                    {
                        tex.name = assetName + " Atlas";
                        AssetDatabase.AddObjectToAsset(tex, fa);
                    }
                }
            }
            if (fa.atlasTexture != null && AssetDatabase.GetAssetPath(fa.atlasTexture) == string.Empty)
            {
                fa.atlasTexture.name = assetName + " Atlas";
                AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
            }
            if (fa.material != null && AssetDatabase.GetAssetPath(fa.material) == string.Empty)
            {
                fa.material.name = assetName + " Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }

            // TMP's FontAsset Inspector reads the font weight table on draw and crashes
            // (NRE in DrawFont -> GUIContent ctor) when entries are null. Initialize a 10-slot
            // table with self references so the dropdown has valid options.
            EnsureFontWeightTable(fa);

            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        }

        private static void EnsureFontWeightTable(TMP_FontAsset fa)
        {
            var so = new SerializedObject(fa);
            var prop = so.FindProperty("m_FontWeightTable");
            if (prop == null || !prop.isArray) return;
            // TMP defines 10 weight slots (Thin..Black) plus regular/italic pairs.
            if (prop.arraySize < 10) prop.arraySize = 10;
            for (int i = 0; i < prop.arraySize; i++)
            {
                var pair = prop.GetArrayElementAtIndex(i);
                var reg = pair.FindPropertyRelative("regularTypeface");
                var ita = pair.FindPropertyRelative("italicTypeface");
                if (reg != null && reg.objectReferenceValue == null) reg.objectReferenceValue = fa;
                if (ita != null && ita.objectReferenceValue == null) ita.objectReferenceValue = fa;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
