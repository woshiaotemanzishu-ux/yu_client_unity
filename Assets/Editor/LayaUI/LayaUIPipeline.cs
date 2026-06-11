using Shenxiao.EditorTools.AddrSetup;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// 模块一键流水线:散图导入 → 模板补齐 → 合并转换(产 prefab + Bind cs)
    /// → Unity 编译 → [DidReloadScripts] 自动回填 Bind → (可选)Addressable 分组。
    /// 消除「转完等编译再手点回填」的两步操作;验收过的模块重转前弹确认。
    /// </summary>
    public static class LayaUIPipeline
    {
        private static string PendingKey => "Shenxiao.LayaUI.PendingFill:" + Application.dataPath.GetHashCode();
        private static string MissingKey(string module) => "Shenxiao.LayaUI.Missing." + module + ":" + Application.dataPath.GetHashCode();

        public static int GetLastMissingCount(string module)
        {
            return EditorPrefs.GetInt(MissingKey(module), -1);
        }

        public static void RunModule(string module)
        {
            string err;
            if (!LayaUISettings.ValidateClientRoot(out err))
            {
                EditorUtility.DisplayDialog("LayaUI", err + "\n\n先在设置里配置 yu_client 目录。", "好");
                return;
            }
            if (LayaUIAcceptance.IsAccepted(module) &&
                !EditorUtility.DisplayDialog("LayaUI",
                    "模块 " + module + " 已标记验收 ✅。\n重转会重建该模块全部窗口(prefab 上的手调会丢)。\n确定重转?",
                    "重转", "取消"))
            {
                return;
            }

            // ① 散图(动态换图用,幂等) + 模板
            var spriteReport = new LayaUIReport(module + "_sprites");
            int imported = LayaSpriteImporter.ImportModuleAll(module, spriteReport);
            if (imported > 0) spriteReport.Save();
            LayaUITemplates.BuildAll();

            // ② 转换(写 prefab + Bind cs)
            int missing = LayaSceneConverter.ConvertModuleCombined(module);
            if (missing < 0) return;
            EditorPrefs.SetInt(MissingKey(module), missing);

            // ③ 排队回填:Bind cs 触发编译则 DidReloadScripts 续跑;没触发则直接补
            EditorPrefs.SetString(PendingKey, module);
            AssetDatabase.Refresh();
            EditorApplication.delayCall += TryFillPending;
            GameLog("模块 " + module + " 转换完成(缺图 " + missing + "),等编译后自动回填 Bind ...");
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += TryFillPending;
        }

        private static void TryFillPending()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            string module = EditorPrefs.GetString(PendingKey, "");
            if (string.IsNullOrEmpty(module)) return;
            EditorPrefs.DeleteKey(PendingKey);

            LayaBindFiller.FillModule(module);
            if (LayaUISettings.AutoGroupAfterConvert)
            {
                AddressableSetup.AutoGroupAll();
            }
            GameLog("模块 " + module + " 流水线完成 ✅(转换 → 回填" +
                    (LayaUISettings.AutoGroupAfterConvert ? " → Addressable 分组" : "") + ")");
        }

        private static void GameLog(string msg)
        {
            Debug.Log("[LayaUI] " + msg);
        }
    }
}
