using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Shenxiao.Editor.LayaUI;
using UnityEngine;

namespace Shenxiao.Editor.AssetHub
{
    public sealed class AssetAssemblySet
    {
        public string ProfileId;
        public string Label;
        public string BusinessKey;
        public string ActionGroup;
        public int Career;
        public int Sex;
        public int RoleRes;
        public int HeadRes;
        public int WeaponRes;
        public string ModelKey;
        public string WeaponKey;
        public string DefaultActionContext;
        public readonly Dictionary<string, List<string>> ActionContexts = new Dictionary<string, List<string>>();
        public AssetEntry Entry;

        public List<string> DefaultActions()
        {
            if (!string.IsNullOrEmpty(DefaultActionContext)
                && ActionContexts.TryGetValue(DefaultActionContext, out List<string> actions))
            {
                return actions;
            }
            return new List<string>();
        }
    }

    public static class AssetAssemblyCatalog
    {
        private const string DEFAULT_CONTEXT = "LoginCreateRoleView/default";

        public static List<AssetAssemblySet> BuildDefaultRoleSets()
        {
            JObject login = LoadClientConfig("configlogin", "ConfigLogin");
            JObject modelAni = LoadClientConfig("configmodelani", "ConfigModelAni");
            var contexts = ParseRoleActionContexts(modelAni);
            var list = new List<AssetAssemblySet>();
            JObject create = login?["CreateRole"] as JObject;
            if (!(create?["Res"] is JObject res) || !(create["UI"] is JArray uiList)) return list;

            foreach (JObject ui in uiList.OfType<JObject>())
            {
                int career = ui.Value<int?>("career") ?? 0;
                int sex = ui.Value<int?>("sex") ?? 0;
                string businessKey = career + "@" + sex;
                if (!(res[businessKey] is JObject roleRes)) continue;

                int clotheRes = roleRes.Value<int?>("role_res") ?? 0;
                int weaponRes = roleRes.Value<int?>("weapon_res") ?? 0;
                int headRes = roleRes.Value<int?>("head_res") ?? 0;
                string actionGroup = (1000 + career * 100).ToString();
                string label = (ui.Value<string>("name") ?? businessKey) + "·默认装";

                var set = new AssetAssemblySet
                {
                    ProfileId = "role/model_clothe_" + clotheRes,
                    Label = label,
                    BusinessKey = businessKey,
                    ActionGroup = actionGroup,
                    Career = career,
                    Sex = sex,
                    RoleRes = clotheRes,
                    HeadRes = headRes,
                    WeaponRes = weaponRes,
                    ModelKey = ObjectKey("role", "model_clothe_" + clotheRes),
                    WeaponKey = weaponRes > 0 ? ObjectKey("weapon", "model_weapon_r_" + weaponRes) : "",
                    DefaultActionContext = contexts.ContainsKey(DEFAULT_CONTEXT)
                        ? DEFAULT_CONTEXT
                        : contexts.Keys.OrderBy(k => k).FirstOrDefault(),
                };
                foreach (KeyValuePair<string, List<string>> kv in contexts)
                    set.ActionContexts[kv.Key] = new List<string>(kv.Value);
                set.Entry = BuildRoleEntry(set);
                list.Add(set);
            }
            return list.OrderBy(s => s.ActionGroup).ThenBy(s => s.BusinessKey).ToList();
        }

        private static JObject LoadClientConfig(string unityName, string oldClientName)
        {
            string unityPath = Path.Combine("Assets", "GameRes", "resource", "config", "client", unityName + ".json");
            if (File.Exists(unityPath)) return JObject.Parse(File.ReadAllText(unityPath));

            string oldPath = Path.Combine(LayaUISettings.CdnResourceRoot, "config", "client", oldClientName + ".json");
            if (File.Exists(oldPath)) return JObject.Parse(File.ReadAllText(oldPath));

            Debug.LogWarning("[AssetAssembly] 配置缺失: " + unityName + " / " + oldClientName);
            return null;
        }

        private static Dictionary<string, List<string>> ParseRoleActionContexts(JObject modelAni)
        {
            var contexts = new Dictionary<string, List<string>>();
            if (!(modelAni?["role"] is JObject role)) return contexts;
            foreach (KeyValuePair<string, JToken> section in role)
            {
                if (section.Value is JArray direct)
                {
                    contexts[section.Key + "/default"] = ToActionList(direct);
                    continue;
                }
                if (!(section.Value is JObject byKey)) continue;
                foreach (KeyValuePair<string, JToken> kv in byKey)
                {
                    if (kv.Value is JArray actions)
                        contexts[section.Key + "/" + kv.Key] = ToActionList(actions);
                }
            }
            return contexts;
        }

        private static List<string> ToActionList(JArray array)
        {
            return array
                .Select(v => v.Value<string>())
                .Where(v => !string.IsNullOrEmpty(v))
                .Distinct()
                .ToList();
        }

        private static AssetEntry BuildRoleEntry(AssetAssemblySet set)
        {
            string name = "model_clothe_" + set.RoleRes;
            string root = Path.Combine(LayaUISettings.CdnResourceRoot, "object", "role");
            return new AssetEntry
            {
                Id = set.RoleRes.ToString(),
                DisplayName = set.Label,
                Career = set.Career,
                Sex = set.Sex,
                Note = "ConfigLogin.CreateRole.Res[" + set.BusinessKey + "]",
                LhPath = Path.Combine(root, "objs", name + ".lh"),
                ActionDir = Path.Combine(root, "action", set.ActionGroup),
                OutDir = "Assets/GameRes/object/role/" + name,
                PrefabPath = "Assets/GameRes/object/role/" + name + "/" + name + ".prefab",
                Kind = AssetKind.Model,
                SearchText = (set.ProfileId + " " + set.Label + " " + set.BusinessKey + " " + set.ActionGroup)
                    .ToLowerInvariant(),
            };
        }

        private static string ObjectKey(string module, string name)
        {
            return "object/" + module + "/" + name + "/" + name;
        }
    }
}
