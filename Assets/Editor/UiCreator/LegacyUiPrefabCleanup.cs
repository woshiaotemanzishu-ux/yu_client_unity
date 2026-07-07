using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Shenxiao.Editor.UiCreator
{
    /// <summary>
    /// 清理旧 Laya 转换器(.scene 镜像)产出的、已被 UICreator 重构版取代的死 prefab。
    ///
    /// 删除清单由 2026-07-06 的全库反向引用扫描得出(6517 个 prefab/unity/asset 逐 GUID 核对):
    /// 清单内文件除 Addressable 分组条目外【零】资产引用,且代码侧无按名/按地址加载。
    /// 明确保留(有活引用,勿加进来):
    ///   MainUIModule/ArrowComponent/CollectBarView/FightingUpView/MainUIBuffView/MainUIFightModeView
    ///     —— 运行时按地址加载,已由 UICreator 原地覆盖(GUID/地址不变);
    ///   ActivityIcon(新 bundle 嵌套)、TopPlayerTipItem(ActivityIcon 嵌套)、
    ///   MainUISkillItemGod(Kf1vnModule 嵌套)、GiftPushIcon(DungeonRune/Equip/Eudaemon/Pet 嵌套)。
    /// 删除动作:先移除 Addressable 条目再删资产,git 可整体回退。
    /// </summary>
    public static class LegacyUiPrefabCleanup
    {
        private static readonly string[] DeadPrefabs =
        {
            // ---- MainUI:老的碎片化视图 prefab(已由 Regions/Overlays + 新总装取代) ----
            "Assets/Prefabs/UI/MainUI/MainUIActivityView.prefab",
            "Assets/Prefabs/UI/MainUI/MainUIAutoBrushView.prefab",
            "Assets/Prefabs/UI/MainUI/MainUIChatView.prefab",
            "Assets/Prefabs/UI/MainUI/MainUIDownView.prefab",
            "Assets/Prefabs/UI/MainUI/MainUIEffectPartnerSkillView.prefab",
            "Assets/Prefabs/UI/MainUI/MainUIEffectView.prefab",
            "Assets/Prefabs/UI/MainUI/MainUIHiterBigBloodView.prefab",
            "Assets/Prefabs/UI/MainUI/MainUIIconBoxView.prefab",
            "Assets/Prefabs/UI/MainUI/MainUIMarriageItem.prefab",
            "Assets/Prefabs/UI/MainUI/MainUIReliveView.prefab",
            "Assets/Prefabs/UI/MainUI/MainUISecondaryView.prefab",
            "Assets/Prefabs/UI/MainUI/MainUISkillView.prefab",
            "Assets/Prefabs/UI/MainUI/MainUITaskTeamView.prefab",
            "Assets/Prefabs/UI/MainUI/MainUITopView.prefab",
            "Assets/Prefabs/UI/MainUI/UIJoyStick.prefab",
            "Assets/Prefabs/UI/MainUI/OpenDestinyView.prefab",
            "Assets/Prefabs/UI/MainUI/DropProgress.prefab",
            "Assets/Prefabs/UI/MainUI/EyouThAdultItem.prefab",
            "Assets/Prefabs/UI/MainUI/FlowerEffectView.prefab",
            "Assets/Prefabs/UI/MainUI/FuncBoardView.prefab",
            // ---- Login:老转换器残留(LoginFlow 只加载 6 个 UICreator prefab) ----
            "Assets/Prefabs/UI/Login/AccountView.prefab",
            "Assets/Prefabs/UI/Login/LoginCreatRoleVideoView.prefab",
            "Assets/Prefabs/UI/Login/LoginTipsView.prefab",
            "Assets/Prefabs/UI/Login/LoginUserAgreementView.prefab",
            "Assets/Prefabs/UI/Login/WaitforOpenViewLoading.prefab",
        };

        [MenuItem("神霄/清理·旧转换器UI预制体(Login+MainUI)", priority = 9)]
        public static void Run()
        {
            var existing = new List<string>();
            foreach (string p in DeadPrefabs)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(p) != null) existing.Add(p);
            }

            if (existing.Count == 0)
            {
                EditorUtility.DisplayDialog("清理旧UI预制体", "清单里的 25 个文件都已不存在,无需清理。", "好");
                return;
            }

            bool ok = EditorUtility.DisplayDialog("清理旧UI预制体",
                "将删除 " + existing.Count + " 个已被 UICreator 重构版取代的旧转换器 prefab\n" +
                "(先移除 Addressable 条目,再删资产;git 可回退)。\n\n" +
                "清单见 Console(点确定后逐条打印)。确定删除?",
                "确定删除", "取消");
            if (!ok) return;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            int deleted = 0, addrRemoved = 0;
            var failed = new List<string>();

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string p in existing)
                {
                    string guid = AssetDatabase.AssetPathToGUID(p);
                    if (settings != null && !string.IsNullOrEmpty(guid))
                    {
                        AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                        if (entry != null)
                        {
                            settings.RemoveAssetEntry(guid, postEvent: false);
                            addrRemoved++;
                        }
                    }

                    if (AssetDatabase.DeleteAsset(p))
                    {
                        deleted++;
                        Debug.Log("[清理] 已删除 " + p);
                    }
                    else
                    {
                        failed.Add(p);
                        Debug.LogError("[清理] 删除失败 " + p);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                if (settings != null)
                {
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }
                AssetDatabase.Refresh();
            }

            string msg = "删除 " + deleted + " 个 prefab,移除 " + addrRemoved + " 条 Addressable 条目。"
                + (failed.Count > 0 ? "\n失败 " + failed.Count + " 个(见 Console)。" : "");
            Debug.Log("[清理] 完成:" + msg);
            EditorUtility.DisplayDialog("清理旧UI预制体", msg, "好");
        }
    }
}
