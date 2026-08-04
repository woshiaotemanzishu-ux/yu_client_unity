using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator.Fashion
{
    /// <summary>
    /// 时装/发饰完整资源闭包预检。把配置可达的物品图标与非基础色材质在构建前从老客户端镜像
    /// 一次性导入并登记 Addressables，禁止玩家第一次点击颜色/条目时再临时拷资源造成数秒卡顿。
    /// </summary>
    public static class FashionAssetPreflight
    {
        private const string FashionModelConfig =
            "Assets/GameRes/resource/config/server/config_fashion_model.json";
        private const string FashionColorConfig =
            "Assets/GameRes/resource/config/server/config_fashion_color.json";
        private const string GoodsConfig = "Assets/GameRes/resource/config/server/config_goods.json";
        private const string FashionSuitConfig =
            "Assets/GameRes/resource/config/server/config_fashion_suit.json";
        private const string MountFigureConfig =
            "Assets/GameRes/resource/config/server/config_mount_figure.json";
        private const string IllusionModelConfig =
            "Assets/GameRes/resource/config/client/configillusionmodel.json";
        private const string ResourceGroup = "Remote_resource";
        private const string ObjectGroup = "Remote_object";
        private const string GoodsLabel = "pack_resource_game_goodsicon";
        private const string Common4Label = "pack_resource_game_common4";
        private const string FashionLabel = "pack_object_fashion";
        private const string ConfigLabel = "pack_resource_config";

        public static bool EnsureAddressables()
        {
            try
            {
                JObject models = JObject.Parse(File.ReadAllText(FashionModelConfig));
                JObject colors = JObject.Parse(File.ReadAllText(FashionColorConfig));
                JObject goods = JObject.Parse(File.ReadAllText(GoodsConfig));
                JObject suits = JObject.Parse(File.ReadAllText(FashionSuitConfig));
                JObject mountFigures = JObject.Parse(File.ReadAllText(MountFigureConfig));
                HashSet<string> paths = BuildRequiredPaths(models, colors, goods, suits, mountFigures);
                paths.Add(IllusionModelConfig);
                int imported = 0;
                int configured = 0;
                foreach (string path in paths.OrderBy(value => value, StringComparer.Ordinal))
                {
                    if (!File.Exists(path))
                    {
                        if (!CopyFromOldClient(path))
                        {
                            Debug.LogError("[FashionAssetPreflight] 老端也缺少配置依赖资源: " + path);
                            return false;
                        }
                        imported++;
                    }
                    if (ConfigureImporter(path)) configured++;
                }

                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                AddressableAssetGroup resourceGroup = settings?.FindGroup(ResourceGroup);
                AddressableAssetGroup objectGroup = settings?.FindGroup(ObjectGroup);
                if (settings == null || resourceGroup == null || objectGroup == null)
                {
                    Debug.LogError("[FashionAssetPreflight] Addressables Remote_resource/Remote_object 不存在");
                    return false;
                }

                int added = 0;
                foreach (string path in paths.OrderBy(value => value, StringComparer.Ordinal))
                {
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    if (string.IsNullOrEmpty(guid))
                    {
                        Debug.LogError("[FashionAssetPreflight] 资源没有有效 meta GUID: " + path);
                        return false;
                    }
                    bool isFashionTexture = IsFashionTexture(path);
                    AddressableAssetGroup group = isFashionTexture ? objectGroup : resourceGroup;
                    AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                    if (entry == null)
                    {
                        entry = settings.CreateOrMoveEntry(guid, group, false, false);
                        added++;
                    }
                    else if (entry.parentGroup != group)
                    {
                        entry = settings.CreateOrMoveEntry(guid, group, false, false);
                    }
                    entry.SetLabel(isFashionTexture ? FashionLabel : IsConfig(path) ? ConfigLabel
                            : IsIllusionTipsBackground(path) ? Common4Label : GoodsLabel,
                        true, true, false);
                    string address = AddressFor(path);
                    if (entry.address != address) entry.address = address;
                }

                settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log("[FashionAssetPreflight] required=" + paths.Count + " imported=" + imported
                    + " configured=" + configured + " added=" + added + " missing=0");
                return Verify(paths, settings);
            }
            catch (Exception exception)
            {
                Debug.LogError("[FashionAssetPreflight] 异常: " + exception);
                return false;
            }
        }

        private static HashSet<string> BuildRequiredPaths(
            JObject models, JObject colors, JObject goods, JObject suits, JObject mountFigures)
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // IllusionTips 的品质大底图是运行时按 goods.color 赋值，scene/prefab 本身没有 skin。
            // 七档必须和页面一起预检，禁止第一次点套装条件时再从老端复制 400KB 级图片。
            for (int color = 1; color <= 7; color++)
                paths.Add("Assets/GameRes/resource/game/common4/other/ui_tips_pzbg_" + color + ".png");
            var fashionIds = new HashSet<int>();
            foreach (JObject row in models.Properties().Select(property => property.Value).OfType<JObject>())
            {
                int pos = ReadInt(row["pos_id"]);
                int fashionId = ReadInt(row["fashion_id"]);
                int modelId = ReadInt(row["model_id"]);
                int colorId = ReadInt(row["color_id"]);
                if (fashionId > 0) fashionIds.Add(fashionId);
                if ((pos != 1 && pos != 3) || modelId <= 0 || colorId <= 0) continue;
                string part = pos == 3 ? "head" : "clothe";
                paths.Add("Assets/GameRes/resource/object/fashion/model_" + part + "_"
                    + modelId + "_" + colorId + ".jpg");
            }

            foreach (int fashionId in fashionIds)
            {
                AddGoodsIcon(paths, goods, fashionId);
            }

            // 染色页的激活/升星材料只有选中具体颜色后才会进入 BaseAwardItem。
            // 如果不把这些配置引用计入闭包，第一次点颜色仍会在运行时复制 PNG/.meta。
            foreach (JObject row in colors.Properties().Select(property => property.Value).OfType<JObject>())
            {
                AddCostGoodsIcons(paths, goods, row["active_cost"]);
                AddCostGoodsIcons(paths, goods, row["star_cost"]);
            }

            // 套装条件里的羽翼/神兵/坐骑也会在第一次打开套装页显示物品格；它们必须与
            // 时装图标一起成为构建前资源闭包，不能等玩家点到第四页再从老端临时复制。
            foreach (JObject suit in suits.Properties().Select(property => property.Value).OfType<JObject>())
            {
                JArray conditions;
                try { conditions = JArray.Parse(suit["condition"]?.ToString() ?? "[]"); }
                catch { continue; }
                foreach (JObject wrapper in conditions.OfType<JObject>())
                {
                    if (!(wrapper["1"] is JObject condition) || ReadInt(condition["0"]) != 2) continue;
                    int typeId = ReadInt(condition["1"]);
                    int figureId = ReadInt(condition["2"]);
                    for (int career = 1; career <= 4; career++)
                    {
                        JObject figure = mountFigures[typeId + "@" + figureId + "@" + career] as JObject
                            ?? mountFigures[typeId + "@" + figureId + "@1"] as JObject;
                        int goodsId = ReadInt(figure?["goods_id"]);
                        string value = goodsId.ToString();
                        if (value.Length >= 3 && value[value.Length - 3] == '1') goodsId -= 100;
                        AddGoodsIcon(paths, goods, goodsId);
                    }
                }
            }
            return paths;
        }

        private static void AddGoodsIcon(HashSet<string> paths, JObject goods, int goodsId)
        {
            if (goodsId <= 0 || !(goods[goodsId.ToString()] is JObject row)) return;
            string icon = row["14"]?.ToString();
            if (!string.IsNullOrWhiteSpace(icon))
                paths.Add("Assets/GameRes/resource/game/goodsicon/" + icon + ".png");
        }

        private static void AddCostGoodsIcons(HashSet<string> paths, JObject goods, JToken value)
        {
            JArray costs;
            try { costs = JArray.Parse(value?.ToString() ?? "[]"); }
            catch { return; }
            foreach (JObject cost in costs.OfType<JObject>())
            {
                int goodsId = ReadInt(cost["1"]);
                AddGoodsIcon(paths, goods, goodsId);
            }
        }

        private static bool CopyFromOldClient(string assetPath)
        {
            const string prefix = "Assets/GameRes/";
            string relative = assetPath.StartsWith(prefix, StringComparison.Ordinal)
                ? assetPath.Substring(prefix.Length)
                : assetPath;
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string defaultClient = Path.GetFullPath(Path.Combine(projectRoot, "..", "yu_client"));
            string clientRoot = EditorPrefs.GetString(
                "Shenxiao.LayaUI.ClientRoot:" + Application.dataPath.GetHashCode(), defaultClient);
            string[] roots =
            {
                Path.Combine(clientRoot, "h5", "laya", "assets"),
                Path.Combine(clientRoot, "cdn"),
            };
            foreach (string root in roots)
            {
                string source = Path.Combine(root, relative);
                if (!File.Exists(source) || IsLfsPointer(source)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                File.Copy(source, assetPath, true);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                return true;
            }
            return false;
        }

        private static bool IsLfsPointer(string path)
        {
            var info = new FileInfo(path);
            if (info.Length >= 300) return false;
            string head = File.ReadAllText(path);
            return head.StartsWith("version https://git-lfs", StringComparison.Ordinal);
        }

        private static bool ConfigureImporter(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return false;
            bool material = IsFashionTexture(path);
            TextureImporterType type = material ? TextureImporterType.Default : TextureImporterType.Sprite;
            bool dirty = importer.textureType != type || importer.mipmapEnabled
                || (material && importer.npotScale != TextureImporterNPOTScale.None);
            if (dirty)
            {
                importer.textureType = type;
                importer.mipmapEnabled = false;
                if (material) importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
            return dirty;
        }

        private static bool IsFashionTexture(string path) =>
            path.IndexOf("/resource/object/fashion/", StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool IsConfig(string path) =>
            path.IndexOf("/resource/config/", StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool IsIllusionTipsBackground(string path) =>
            path.IndexOf("/resource/game/common4/other/ui_tips_pzbg_", StringComparison.OrdinalIgnoreCase) >= 0;

        private static string AddressFor(string path)
        {
            const string prefix = "Assets/GameRes/";
            string relative = path.StartsWith(prefix, StringComparison.Ordinal) ? path.Substring(prefix.Length) : path;
            return Path.ChangeExtension(relative, null).Replace('\\', '/').ToLowerInvariant();
        }

        private static int ReadInt(JToken value) => int.TryParse(value?.ToString(), out int result) ? result : 0;

        private static bool Verify(IEnumerable<string> paths, AddressableAssetSettings settings)
        {
            foreach (string path in paths)
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                if (entry == null || entry.address != AddressFor(path))
                {
                    Debug.LogError("[FashionAssetPreflight] Addressable 校验失败: " + path);
                    return false;
                }
            }
            return true;
        }

        [MenuItem("神霄/重构UI 生成器/设置/补齐时装资源闭包")]
        public static void EnsureAddressablesMenu() => EnsureAddressables();

        public static void EnsureAddressablesBatch() =>
            EditorApplication.Exit(EnsureAddressables() ? 0 : 1);
    }
}
