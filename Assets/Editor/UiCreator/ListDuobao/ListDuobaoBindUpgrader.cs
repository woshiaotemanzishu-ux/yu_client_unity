using Shenxiao.Editor.LayaUI;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator.ListDuobao
{
    /// <summary>
    /// ListDuobao 运行时组件升级器。转换产物已挂完整 Bind 基类与序列化引用，本工具只把模块内窗口、
    /// 内联模板及独立 ListGoodsItem 源 prefab 升级为唯一业务子类，并复用 LayaBindFiller 重新回填引用。
    /// </summary>
    public static class ListDuobaoBindUpgrader
    {
        private const string ModulePath = "Assets/Prefabs/UI/ListDuobao/ListDuobaoModule.prefab";
        private const string GoodsItemPath = "Assets/Prefabs/UI/ListDuobao/ListGoodsItem.prefab";
        private const string StageConfigPath = "Assets/GameRes/resource/config/server/config_rush_treasure_stage_reward.json";
        private const string RankConfigPath = "Assets/GameRes/resource/config/server/config_rush_treasure_rank_reward.json";
        private const string StageConfigAddress = "resource/config/server/config_rush_treasure_stage_reward";
        private const string RankConfigAddress = "resource/config/server/config_rush_treasure_rank_reward";

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "ListDuobao",
                Name = "ListDuobaoModule(夺宝 Bind 升级)",
                Note = "把 ListDuobaoModule 与独立 ListGoodsItem 源 prefab 的 Bind 基类升级为业务子类" +
                       "(LayaBindFiller.FillPrefab,幂等可重跑)",
                Order = 98,
                Generate = () => Generate(),
                PrefabPath = ModulePath,
            });
        }

        /// <summary>定向升级两个源 prefab 并验证全部业务子类，成功返回 true。</summary>
        public static bool Generate()
        {
            if (!LayaBindFiller.FillPrefab(ModulePath))
            {
                Debug.LogError("[UiCreator] LayaBindFiller.FillPrefab(" + ModulePath + ") 失败(看 Console 前面的警告)");
                return false;
            }
            if (!LayaBindFiller.FillPrefab(GoodsItemPath))
            {
                Debug.LogError("[UiCreator] LayaBindFiller.FillPrefab(" + GoodsItemPath + ") 失败(看 Console 前面的警告)");
                return false;
            }
            if (!EnsureConfigAddressables()) return false;
            return Verify();
        }

        private static bool EnsureConfigAddressables()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup group = settings?.FindGroup("Remote_resource");
            if (settings == null || group == null)
            {
                Debug.LogError("[UiCreator] ListDuobao 配置登记失败: Addressables/Remote_resource 不存在");
                return false;
            }

            bool ok = EnsureEntry(settings, group, StageConfigPath, StageConfigAddress);
            ok &= EnsureEntry(settings, group, RankConfigPath, RankConfigAddress);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true);
            AssetDatabase.SaveAssets();
            return ok;
        }

        private static bool EnsureEntry(AddressableAssetSettings settings, AddressableAssetGroup group, string path, string address)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError("[UiCreator] ListDuobao 配置资源不存在: " + path);
                return false;
            }
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry == null) return false;
            entry.SetAddress(address, false);
            entry.SetLabel("pack_resource_config", true, true, false);
            return true;
        }

        /// <summary>验证模块内六个组件及独立源 prefab 的 ListGoodsItem 均已升级为业务子类。</summary>
        private static bool Verify()
        {
            GameObject module = AssetDatabase.LoadAssetAtPath<GameObject>(ModulePath);
            GameObject goodsItem = AssetDatabase.LoadAssetAtPath<GameObject>(GoodsItemPath);
            if (module == null || goodsItem == null)
            {
                Debug.LogError("[UiCreator] ListDuobaoBindUpgrader 验证失败:prefab 加载不到 module=" +
                               (module != null) + " goodsItem=" + (goodsItem != null));
                return false;
            }

            bool ok = true;
            ok &= Check<Shenxiao.Module.Core.ListDuobao.ListDuobaoView>(module, "ListDuobaoView");
            ok &= Check<Shenxiao.Module.Core.ListDuobao.ListDuobaoRecordView>(module, "ListDuobaoRecordView");
            ok &= Check<Shenxiao.Module.Core.ListDuobao.ListRewardView>(module, "ListRewardView");
            ok &= Check<Shenxiao.Module.Core.ListDuobao.ListRankView>(module, "ListRankView");
            ok &= Check<Shenxiao.Module.Core.ListDuobao.ListRewardItem>(module, "ListRewardItem(__Templates 内)");
            ok &= Check<Shenxiao.Module.Core.ListDuobao.ListRankItem>(module, "ListRankItem(__Templates 内)");
            ok &= Check<Shenxiao.Module.Core.ListDuobao.ListGoodsItem>(goodsItem, "ListGoodsItem(独立源 prefab)");
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            ok &= CheckAddress(settings, StageConfigPath, StageConfigAddress);
            ok &= CheckAddress(settings, RankConfigPath, RankConfigAddress);
            Debug.Log("[UiCreator] ListDuobaoBindUpgrader 验证 " + (ok ? "OK" : "FAILED") + " " + ModulePath);
            return ok;
        }

        private static bool CheckAddress(AddressableAssetSettings settings, string path, string expected)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            AddressableAssetEntry entry = settings?.FindAssetEntry(guid);
            bool ok = entry != null && entry.address == expected && entry.labels.Contains("pack_resource_config");
            if (!ok) Debug.LogError("[UiCreator] ListDuobao 配置未正确登记 Addressable: " + expected);
            return ok;
        }

        private static bool Check<T>(GameObject root, string label) where T : Component
        {
            if (root.GetComponentInChildren<T>(true) != null) return true;
            Debug.LogError("[UiCreator] 缺运行时组件 " + typeof(T).Name + "(" + label + ")");
            return false;
        }

        /// <summary>
        /// 批处理入口:
        /// Unity.exe -batchmode -projectPath . -executeMethod
        /// Shenxiao.Editor.UiCreator.ListDuobao.ListDuobaoBindUpgrader.GenerateBatch
        /// -logFile Temp/listduobao_bind_upgrader.log
        /// </summary>
        public static void GenerateBatch()
        {
            try
            {
                bool ok = Generate();
                Debug.Log("[UiCreator] ListDuobaoBindUpgrader.GenerateBatch " + (ok ? "OK " : "FAILED ") + ModulePath);
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UiCreator] ListDuobaoBindUpgrader.GenerateBatch 异常: " + e);
                EditorApplication.Exit(1);
            }
        }
    }
}
