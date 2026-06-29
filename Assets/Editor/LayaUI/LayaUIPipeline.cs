using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Shenxiao.Editor.DynamicResources;
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
        private const string NAMES_PATH = "Schemas/LayaUI/module_names_cn.json";
        private const string MainUIEntryAutoRunRequestPath = "Temp/ShenxiaoRunMainUIEntryModules.request";
        private const string MainUIEntryTitle = "MainUI Entry Modules";
        private const double MainUIEntryQueuePollSeconds = 2.0;
        private static readonly string[] FreshMachineModules = { "login", "mainUI" };
        private static readonly string[] MainUIEntryModules =
        {
            "vip", "pet", "redPacket", "rune", "marriage", "godBefall", "shop", "common"
        };
        private static bool mainUIEntryAutoRunQueued;
        private static double nextMainUIEntryQueuePollTime;

        private static string PendingKey => "Shenxiao.LayaUI.PendingFill:" + Application.dataPath.GetHashCode();
        private static string MissingKey(string module) => "Shenxiao.LayaUI.Missing." + module + ":" + Application.dataPath.GetHashCode();

        [MenuItem("神霄/LayaUI/新机一键转换(登录+主界面)", priority = 10)]
        public static void RunFreshMachineModules()
        {
            if (!EditorUtility.DisplayDialog("新机一键转换",
                    "将重建本地忽略的核心 UI 产物:\n\n- login 登录/选角/创角\n- mainUI 主界面\n\n流程:散图导入 → 模板补齐 → prefab/Bind 转换 → 编译后回填 → Addressable 分组。\n这会覆盖对应模块的生成 prefab。",
                    "开始转换", "取消"))
            {
                return;
            }

            RunModules(FreshMachineModules, "新机一键转换", false);
        }

        [MenuItem("神霄/LayaUI/重转主界面(MainUI)", priority = 20)]
        public static void RunMainUI()
        {
            RunModule("mainUI");
        }

        [MenuItem("神霄/LayaUI/重转主界面入口模块", priority = 22)]
        public static void RunMainUIEntryModules()
        {
            RunModules(MainUIEntryModules, MainUIEntryTitle, true);
        }

        public static void RunMainUIEntryModulesNoConfirm()
        {
            RunModules(MainUIEntryModules, MainUIEntryTitle, false);
        }

        [InitializeOnLoadMethod]
        private static void RegisterQueuedMainUIEntryModules()
        {
            EditorApplication.update -= PollQueuedMainUIEntryModules;
            EditorApplication.update += PollQueuedMainUIEntryModules;
            ScheduleQueuedMainUIEntryModules();
        }

        private static void PollQueuedMainUIEntryModules()
        {
            if (mainUIEntryAutoRunQueued) return;
            if (EditorApplication.timeSinceStartup < nextMainUIEntryQueuePollTime) return;

            nextMainUIEntryQueuePollTime = EditorApplication.timeSinceStartup + MainUIEntryQueuePollSeconds;
            ScheduleQueuedMainUIEntryModules();
        }

        private static void ScheduleQueuedMainUIEntryModules()
        {
            if (mainUIEntryAutoRunQueued) return;
            if (!File.Exists(GetMainUIEntryAutoRunRequestPath())) return;

            mainUIEntryAutoRunQueued = true;
            EditorApplication.delayCall += RunQueuedMainUIEntryModules;
        }

        private static void RunQueuedMainUIEntryModules()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RunQueuedMainUIEntryModules;
                return;
            }

            string path = GetMainUIEntryAutoRunRequestPath();
            if (!File.Exists(path))
            {
                mainUIEntryAutoRunQueued = false;
                return;
            }

            try
            {
                File.Delete(path);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[LayaUI] Failed to delete MainUI entry rebuild request: " + e.Message);
            }

            mainUIEntryAutoRunQueued = false;
            GameLog("Auto-running queued MainUI entry module rebuild.");
            RunMainUIEntryModulesNoConfirm();
        }

        [MenuItem("神霄/LayaUI/重转任务(Task)", priority = 21)]
        public static void RunTask()
        {
            RunModule("task");
        }

        [MenuItem("神霄/LayaUI/高级/一键转换全部模块", priority = 120)]
        public static void RunAllModules()
        {
            string[] modules = LoadKnownModules();
            if (modules.Length == 0)
            {
                EditorUtility.DisplayDialog("LayaUI 全量转换", "没有读到模块列表: " + NAMES_PATH, "好");
                return;
            }

            if (!EditorUtility.DisplayDialog("LayaUI 全量转换",
                    "将重建全部 " + modules.Length + " 个 LayaUI 模块的生成 prefab/Bind。\n\n这个操作耗时较长,适合新机完整初始化或大规模工具规则变更后使用。",
                    "开始全量转换", "取消"))
            {
                return;
            }

            RunModules(modules, "LayaUI 全量转换", false);
        }

        public static int GetLastMissingCount(string module)
        {
            return EditorPrefs.GetInt(MissingKey(module), -1);
        }

        public static void RunModule(string module)
        {
            RunModules(new[] { module }, "LayaUI", true);
        }

        public static void RunModules(IEnumerable<string> modules, string title, bool confirmAcceptedModules)
        {
            string err;
            if (!LayaUISettings.ValidateClientRoot(out err))
            {
                EditorUtility.DisplayDialog("LayaUI", err + "\n\n先在设置里配置 yu_client 目录。", "好");
                return;
            }

            List<string> targets = modules
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => m.Trim())
                .Distinct()
                .ToList();
            if (targets.Count == 0) return;

            string[] accepted = targets.Where(LayaUIAcceptance.IsAccepted).ToArray();
            if (confirmAcceptedModules && accepted.Length > 0 &&
                !EditorUtility.DisplayDialog("LayaUI",
                    "以下模块已标记验收 ✅:\n" + string.Join(", ", accepted) +
                    "\n\n重转会重建这些模块的生成 prefab, prefab 上的手调会丢。\n确定重转?",
                    "重转", "取消"))
            {
                return;
            }

            var completed = new List<string>();
            bool canceled = false;
            try
            {
                // ① 散图(动态换图用,幂等)。
                for (int i = 0; i < targets.Count; i++)
                {
                    string module = targets[i];
                    if (EditorUtility.DisplayCancelableProgressBar(title,
                            "导入散图 " + (i + 1) + "/" + targets.Count + "  " + module,
                            (float)i / targets.Count))
                    {
                        canceled = true;
                        break;
                    }

                    var spriteReport = new LayaUIReport(module + "_sprites");
                    int imported = LayaSpriteImporter.ImportModuleAll(module, spriteReport);
                    if (imported > 0) spriteReport.Save();
                }

                if (canceled) return;

                // ② 模板补齐只需做一次,再逐模块转换(写 prefab + Bind cs)。
                LayaUITemplates.BuildAll();
                for (int i = 0; i < targets.Count; i++)
                {
                    string module = targets[i];
                    if (EditorUtility.DisplayCancelableProgressBar(title,
                            "转换 prefab/Bind " + (i + 1) + "/" + targets.Count + "  " + module,
                            (float)i / targets.Count))
                    {
                        canceled = true;
                        break;
                    }

                    int missing = LayaSceneConverter.ConvertModuleCombined(module);
                    if (missing < 0) continue;
                    EditorPrefs.SetInt(MissingKey(module), missing);
                    completed.Add(module);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (completed.Count == 0) return;

            // ③ 排队回填:Bind cs 触发编译则 DidReloadScripts 续跑;没触发则直接补。
            QueuePendingModules(completed);
            AssetDatabase.Refresh();
            EditorApplication.delayCall += TryFillPending;

            string suffix = canceled ? "(用户中止,已完成 " + completed.Count + "/" + targets.Count + ")" : "";
            GameLog(title + " 转换完成 " + completed.Count + "/" + targets.Count + suffix +
                    ",等编译后自动回填 Bind ...");
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += TryFillPending;
        }

        private static void TryFillPending()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            List<string> modules = GetPendingModules();
            if (modules.Count == 0) return;
            EditorPrefs.DeleteKey(PendingKey);

            foreach (string module in modules)
            {
                LayaBindFiller.FillModule(module);
            }

            int slotCount = UIDynamicResourceSlotFiller.FillModules(modules);

            if (LayaUISettings.AutoGroupAfterConvert)
            {
                AddressableSetup.AutoGroupAll();
            }
            GameLog("模块 " + string.Join(", ", modules) + " 流水线完成 ✅(转换 → 回填" +
                    (slotCount > 0 ? " → 动态资源Slot " + slotCount : "") +
                    (LayaUISettings.AutoGroupAfterConvert ? " → Addressable 分组" : "") + ")");
        }

        private static void QueuePendingModules(IEnumerable<string> modules)
        {
            List<string> pending = GetPendingModules();
            foreach (string module in modules)
            {
                if (!pending.Contains(module)) pending.Add(module);
            }
            EditorPrefs.SetString(PendingKey, string.Join("|", pending));
        }

        private static List<string> GetPendingModules()
        {
            string raw = EditorPrefs.GetString(PendingKey, "");
            return raw.Split('|')
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => m.Trim())
                .Distinct()
                .ToList();
        }

        private static string[] LoadKnownModules()
        {
            if (!File.Exists(NAMES_PATH)) return new string[0];
            JObject names = JObject.Parse(File.ReadAllText(NAMES_PATH));
            return names.Properties()
                .Where(p => !p.Name.StartsWith("_"))
                .Select(p => p.Name)
                .OrderBy(m => m)
                .ToArray();
        }

        private static string GetMainUIEntryAutoRunRequestPath()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
            {
                projectRoot = Directory.GetCurrentDirectory();
            }
            return Path.Combine(projectRoot, MainUIEntryAutoRunRequestPath);
        }

        private static void GameLog(string msg)
        {
            Debug.Log("[LayaUI] " + msg);
        }
    }
}
