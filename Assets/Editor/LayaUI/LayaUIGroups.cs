using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// 模块合并分组规则。默认:一个模块的全部窗口合并成一个 {ModuleDir}Module.prefab。
    /// 想把大模块拆成几个大 Panel,建 Schemas/LayaUI/ui_groups.json 覆盖,格式:
    /// {
    ///   "login": [
    ///     { "name": "LoginEntry",  "scenes": ["login/LoginBgView", "login/LoginView", "login/RegisterView"] },
    ///     { "name": "LoginSelect", "scenes": ["login/LoginSelectServerView", "login/LoginSelectRoleView"] }
    ///   ]
    /// }
    /// 模块里没被任何组列到的窗口,仍按每窗口一个 prefab 转。
    /// </summary>
    public static class LayaUIGroups
    {
        public const string CONFIG_PATH = "Schemas/LayaUI/ui_groups.json";

        public class Group
        {
            [JsonProperty("name")] public string Name = "";
            [JsonProperty("scenes")] public List<string> Scenes = new List<string>();
        }

        /// <summary>
        /// 取模块的分组方案。返回组列表;leftovers = 不属于任何组、按单窗口转的 scene key。
        /// 只有 view-prefab / standalone-prefab 决策的 scene 参与(shared 等照旧)。
        /// </summary>
        public static List<Group> ForModule(string module, LayaUIManifest manifest, out List<string> leftovers)
        {
            List<string> eligible = new List<string>();
            foreach (KeyValuePair<string, LayaUIManifest.SceneEntry> kv in manifest.Scenes)
            {
                LayaUIManifest.SceneEntry e = kv.Value;
                if (e.Module != module) continue;
                if (e.Decision == "view-prefab" || e.Decision == "standalone-prefab") eligible.Add(kv.Key);
            }
            eligible.Sort();

            Dictionary<string, List<Group>> config = LoadConfig();
            List<Group> groups;
            if (config != null && config.TryGetValue(module, out groups) && groups != null && groups.Count > 0)
            {
                HashSet<string> covered = new HashSet<string>();
                foreach (Group g in groups)
                {
                    g.Scenes.RemoveAll(s =>
                    {
                        bool bad = !eligible.Contains(s);
                        if (bad) Debug.LogWarning("[LayaUI] ui_groups.json 组 " + g.Name + " 里的 " + s +
                                                  " 不是本模块可合并窗口,忽略");
                        return bad;
                    });
                    foreach (string s in g.Scenes) covered.Add(s);
                }
                leftovers = eligible.FindAll(s => !covered.Contains(s));
                return groups;
            }

            // 默认:整个模块一组
            Group all = new Group();
            all.Name = manifest.ModuleDir(module) + "Module";
            all.Scenes = eligible;
            leftovers = new List<string>();
            return new List<Group> { all };
        }

        private static Dictionary<string, List<Group>> LoadConfig()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), CONFIG_PATH);
            if (!File.Exists(path)) return null;
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, List<Group>>>(File.ReadAllText(path));
            }
            catch (System.Exception e)
            {
                Debug.LogError("[LayaUI] ui_groups.json 解析失败,按默认整模块一组: " + e.Message);
                return null;
            }
        }
    }
}
