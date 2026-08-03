using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator.Dress
{
    /// <summary>
    /// 设置头像路线的资源闭包预检。依照当前配置枚举装扮三子页和发饰页会实际加载的图片，
    /// 要求文件在运行前已存在并只对缺失条目做 Addressables 定向登记。
    /// </summary>
    public static class DressAssetPreflight
    {
        private const string DressConfig = "Assets/GameRes/resource/config/server/config_dress_up_cfg.json";
        private const string GoodsConfig = "Assets/GameRes/resource/config/server/config_goods.json";
        private const string FashionModelConfig = "Assets/GameRes/resource/config/server/config_fashion_model.json";
        private const string ResourceGroup = "Remote_resource";
        private const string GoodsLabel = "pack_resource_game_goodsicon";
        private const string ChatLabel = "pack_resource_game_chat";
        private const string BigBgLabel = "pack_resource_game_bigbg";
        // 现有 head/texture 条目使用该历史小资源桶；定向预检不得顺带重排全仓 PackLabeler。
        private const string HeadLabel = "pack_resource_game_m1";

        public static bool EnsureAddressables()
        {
            try
            {
                HashSet<string> paths = BuildRequiredPaths();
                string[] missing = paths.Where(path => !File.Exists(path)).OrderBy(path => path).ToArray();
                if (missing.Length > 0)
                {
                    Debug.LogError("[DressAssetPreflight] 缺少配置依赖资源 " + missing.Length + " 个:\n" + string.Join("\n", missing));
                    return false;
                }

                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                AddressableAssetGroup group = settings?.FindGroup(ResourceGroup);
                if (settings == null || group == null)
                {
                    Debug.LogError("[DressAssetPreflight] Addressables settings/Remote_resource 不存在");
                    return false;
                }

                int added = 0;
                foreach (string path in paths.OrderBy(path => path, StringComparer.Ordinal))
                {
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    if (string.IsNullOrEmpty(guid))
                    {
                        Debug.LogError("[DressAssetPreflight] 资源没有有效 meta GUID: " + path);
                        return false;
                    }

                    AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                    if (entry == null)
                    {
                        entry = settings.CreateOrMoveEntry(guid, group, false, false);
                        added++;
                        entry.SetLabel(LabelFor(path), true, true, false);
                    }
                    string expected = AddressFor(path);
                    if (entry.address != expected) entry.address = expected;
                }

                settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log("[DressAssetPreflight] required=" + paths.Count + " added=" + added + " missing=0");
                return Verify(paths, settings);
            }
            catch (Exception exception)
            {
                Debug.LogError("[DressAssetPreflight] 异常: " + exception);
                return false;
            }
        }

        private static HashSet<string> BuildRequiredPaths()
        {
            JObject dress = JObject.Parse(File.ReadAllText(DressConfig));
            JObject goods = JObject.Parse(File.ReadAllText(GoodsConfig));
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            paths.Add("Assets/GameRes/resource/game/bigBg/ui_role_bg3.jpg");
            paths.Add("Assets/GameRes/resource/game/bigBg/ui_role_bg4.jpg");
            paths.Add("Assets/GameRes/resource/game/bigBg/ui_role_bg7.jpg");

            foreach (JObject row in dress.Properties().Select(property => property.Value).OfType<JObject>())
            {
                int level = ReadInt(row["level"]);
                int type = ReadInt(row["type"]);
                int id = ReadInt(row["id"]);
                if (type == 1 || type == 2 || type == 5)
                {
                    int skill = ReadInt(row["skill"]);
                    if (skill > 0)
                        paths.Add("Assets/GameRes/resource/game/skillicon/" + skill + ".png");
                }
                if (level != 1 || (type != 1 && type != 2 && type != 5)) continue;

                if (type == 1)
                {
                    AddCostIcon(paths, goods, row);
                    paths.Add("Assets/GameRes/resource/game/chat/texture/1_" + id + "_0.png");
                }
                else if (type == 2)
                {
                    AddCostIcon(paths, goods, row);
                    paths.Add("Assets/GameRes/resource/game/head/texture/2_" + id + ".png");
                }
                else
                {
                    foreach (JObject pair in ParseArray(row["screen"]?.ToString()).OfType<JObject>())
                    {
                        string icon = pair["1"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(icon))
                            paths.Add("Assets/GameRes/resource/game/head/texture/" + icon + ".png");
                    }
                }
            }

            // 发饰页只保留有 config_fashion_model 展示行的 ID，与 FashionConfigs 的运行时过滤一致。
            JObject fashionModel = JObject.Parse(File.ReadAllText(FashionModelConfig));
            foreach (string id in fashionModel.Properties()
                         .Select(property => property.Name.Split('@'))
                         .Where(parts => parts.Length > 1 && parts[0] == "3")
                         .Select(parts => parts[1])
                         .Distinct(StringComparer.Ordinal))
            {
                string path = "Assets/GameRes/resource/game/goodsicon/" + id + ".png";
                if (File.Exists(path)) paths.Add(path);
            }
            return paths;
        }

        private static void AddCostIcon(HashSet<string> paths, JObject goods, JObject row)
        {
            JObject cost = ParseArray(row["cost"]?.ToString()).OfType<JObject>().FirstOrDefault();
            if (cost == null || ReadInt(cost["0"]) != 0) return;
            string typeId = cost["1"]?.ToString();
            if (string.IsNullOrEmpty(typeId) || !(goods[typeId] is JObject goodsRow)) return;
            string icon = goodsRow["14"]?.ToString();
            if (!string.IsNullOrWhiteSpace(icon))
                paths.Add("Assets/GameRes/resource/game/goodsicon/" + icon + ".png");
        }

        private static JArray ParseArray(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? new JArray() : JArray.Parse(value);
        }

        private static int ReadInt(JToken value) => int.TryParse(value?.ToString(), out int result) ? result : 0;

        private static string AddressFor(string path)
        {
            const string prefix = "Assets/GameRes/";
            string relative = path.StartsWith(prefix, StringComparison.Ordinal) ? path.Substring(prefix.Length) : path;
            return Path.ChangeExtension(relative, null).Replace('\\', '/').ToLowerInvariant();
        }

        private static string LabelFor(string path)
        {
            if (path.IndexOf("/goodsicon/", StringComparison.OrdinalIgnoreCase) >= 0) return GoodsLabel;
            if (path.IndexOf("/chat/", StringComparison.OrdinalIgnoreCase) >= 0) return ChatLabel;
            if (path.IndexOf("/bigbg/", StringComparison.OrdinalIgnoreCase) >= 0) return BigBgLabel;
            return HeadLabel;
        }

        private static bool Verify(IEnumerable<string> paths, AddressableAssetSettings settings)
        {
            foreach (string path in paths)
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                if (entry == null || entry.address != AddressFor(path))
                {
                    Debug.LogError("[DressAssetPreflight] Addressable 校验失败: " + path);
                    return false;
                }
            }
            return true;
        }

        public static void EnsureAddressablesBatch()
        {
            EditorApplication.Exit(EnsureAddressables() ? 0 : 1);
        }
    }
}
