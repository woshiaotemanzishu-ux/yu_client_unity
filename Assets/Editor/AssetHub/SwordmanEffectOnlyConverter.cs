using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Shenxiao.Editor.Laya3D;
using Shenxiao.Editor.LayaUI;
using Shenxiao.EditorTools.AddrSetup;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Shenxiao.Editor.AssetHub
{
    /// <summary>
    /// 临时排查入口:只转换剑士创角链路用到的特效。
    /// 问题确认后可删除本文件,不要把它发展成第二套资产管理工具。
    /// </summary>
    public static class SwordmanEffectOnlyConverter
    {
        private const int SWORDMAN_CAREER = 1;
        private const int SWORDMAN_SEX = 1;
        private const int GUNNER_CAREER = 3;
        private const int GUNNER_SEX = 1;

        private sealed class EffectTarget
        {
            public string Dir;
            public string Name;
            public string Reason;
            public string LhPath;
        }

        [MenuItem("神霄/临时/只转剑士创角特效", priority = 901)]
        public static void ConvertSwordmanCreateRoleEffects()
        {
            if (!EnsureTempConvertCanBeVerified("Convert swordman create-role effects"))
                return;

            if (!LayaUISettings.ValidateClientRoot(out string error))
            {
                EditorUtility.DisplayDialog("只转剑士创角特效", error, "好");
                return;
            }

            List<EffectTarget> targets;
            try
            {
                targets = CollectTargets();
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("只转剑士创角特效", "收集清单失败:\n" + e.Message, "好");
                return;
            }

            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("只转剑士创角特效", "没有找到剑士创角链路特效。", "好");
                return;
            }

            string summary = string.Join("\n", targets.Select(t => $"{t.Dir}/{t.Name}  ({t.Reason})"));
            if (!EditorUtility.DisplayDialog("只转剑士创角特效",
                    $"即将只转换以下 {targets.Count} 个特效:\n\n{summary}", "转换", "取消"))
                return;

            var failed = new List<string>();
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    EffectTarget t = targets[i];
                    if (EditorUtility.DisplayCancelableProgressBar("只转剑士创角特效",
                            $"({i + 1}/{targets.Count}) {t.Dir}/{t.Name}",
                            targets.Count <= 1 ? 0f : (float)i / targets.Count))
                        break;

                    if (!File.Exists(t.LhPath))
                    {
                        failed.Add($"{t.Dir}/{t.Name}: 源 .lh 不存在");
                        continue;
                    }
                    if (AssetHubDomains.IsLfsPlaceholder(t.LhPath))
                    {
                        failed.Add($"{t.Dir}/{t.Name}: 源是 LFS 占位");
                        continue;
                    }

                    if (!LayaEffectImporter.Convert(t.LhPath).Ok)
                        failed.Add($"{t.Dir}/{t.Name}: 转换失败,看 Console");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (LayaUISettings.AutoGroupAfterConvert)
            {
                try { AddressableSetup.AutoGroupAll(); }
                catch (System.Exception e) { Debug.LogWarning("[SwordmanEffectOnly] Addressable 分组失败: " + e.Message); }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = failed.Count == 0
                ? $"完成 {targets.Count} 个剑士创角特效转换。"
                : $"完成 {targets.Count - failed.Count}/{targets.Count},失败:\n" + string.Join("\n", failed);
            EditorUtility.DisplayDialog("只转剑士创角特效", msg, "好");
        }

        [MenuItem("神霄/临时/只转枪使创角特效", priority = 902)]
        public static void ConvertGunnerCreateRoleEffects()
        {
            if (!EnsureTempConvertCanBeVerified("Convert gunner create-role effects"))
                return;

            if (!LayaUISettings.ValidateClientRoot(out string error))
            {
                EditorUtility.DisplayDialog("只转枪使创角特效", error, "好");
                return;
            }

            List<EffectTarget> targets;
            try
            {
                targets = CollectTargets(GUNNER_CAREER, GUNNER_SEX);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("只转枪使创角特效", "收集清单失败:\n" + e.Message, "好");
                return;
            }

            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("只转枪使创角特效", "没有找到枪使创角链路特效。", "好");
                return;
            }

            string summary = string.Join("\n", targets.Select(t => $"{t.Dir}/{t.Name}  ({t.Reason})"));
            if (!EditorUtility.DisplayDialog("只转枪使创角特效",
                    $"即将只转换以下 {targets.Count} 个特效:\n\n{summary}", "转换", "取消"))
                return;

            var failed = new List<string>();
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    EffectTarget t = targets[i];
                    if (EditorUtility.DisplayCancelableProgressBar("只转枪使创角特效",
                            $"({i + 1}/{targets.Count}) {t.Dir}/{t.Name}",
                            targets.Count <= 1 ? 0f : (float)i / targets.Count))
                        break;

                    if (!File.Exists(t.LhPath))
                    {
                        failed.Add($"{t.Dir}/{t.Name}: 源 .lh 不存在");
                        continue;
                    }
                    if (AssetHubDomains.IsLfsPlaceholder(t.LhPath))
                    {
                        failed.Add($"{t.Dir}/{t.Name}: 源是 LFS 占位");
                        continue;
                    }

                    if (!LayaEffectImporter.Convert(t.LhPath).Ok)
                        failed.Add($"{t.Dir}/{t.Name}: 转换失败,看 Console");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (LayaUISettings.AutoGroupAfterConvert)
            {
                try { AddressableSetup.AutoGroupAll(); }
                catch (System.Exception e) { Debug.LogWarning("[GunnerEffectOnly] Addressable 分组失败: " + e.Message); }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = failed.Count == 0
                ? $"完成 {targets.Count} 个枪使创角特效转换。"
                : $"完成 {targets.Count - failed.Count}/{targets.Count},失败:\n" + string.Join("\n", failed);
            EditorUtility.DisplayDialog("只转枪使创角特效", msg, "好");
        }

        private static bool EnsureTempConvertCanBeVerified(string title)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(title,
                    "Stop Play Mode before converting effects. Runtime-loaded Addressables assets and material instances are not refreshed by AssetDatabase.Refresh().",
                    "OK");
                return false;
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return true;

            int index = settings.ActivePlayModeDataBuilderIndex;
            var builder = settings.GetDataBuilder(index);
            string builderName = builder != null ? builder.Name : "<none>";
            string builderType = builder != null ? builder.GetType().Name : "<none>";
            if (builderType == "BuildScriptFastMode") return true;

            string message =
                "Addressables Play Mode is '" + builderName + "'.\n\n" +
                "For immediate visual verification after this temp conversion, switch to 'Use Asset Database (fastest)' or rebuild Addressables before pressing Play.\n\n" +
                "Continue conversion?";
            return EditorUtility.DisplayDialog(title, message, "Convert", "Cancel");
        }

        private static List<EffectTarget> CollectTargets(int career = SWORDMAN_CAREER, int sex = SWORDMAN_SEX)
        {
            string cfgRoot = Path.Combine(LayaUISettings.CdnResourceRoot, "config", "client");
            JObject login = JObject.Parse(File.ReadAllText(Path.Combine(cfgRoot, "ConfigLogin.json")));
            JObject particle = JObject.Parse(File.ReadAllText(Path.Combine(cfgRoot, "SceneObjectParticle.json")));

            string key = $"{career}@{sex}";
            JToken res = login["CreateRole"]?["Res"]?[key];
            if (res == null) throw new FileNotFoundException("ConfigLogin.CreateRole.Res 缺 " + key);

            string roleRes = res.Value<string>("role_res");
            string weaponRes = res.Value<string>("weapon_res");
            var result = new Dictionary<string, EffectTarget>();

            JToken createEffect = login["CreateRole"]?["Effect"]?[key]?["clothe"];
            CollectConfigLoginEffects(createEffect as JObject, key, result);

            string fallbackCreateFx = $"cj_1{career}00";
            Add(result, "skills_effect", fallbackCreateFx, "老客户端 LoginCreateRoleView.ts bone_effect fallback");

            CollectSceneObjectParticle(particle["Body"]?[roleRes] as JObject, "role_effect",
                "SceneObjectParticle.Body[" + roleRes + "]", result);
            CollectAlways(particle["Weapon"]?[weaponRes]?["always"] as JObject, "weapon_effect",
                "SceneObjectParticle.Weapon[" + weaponRes + "].always", result);

            return result.Values.OrderBy(t => t.Dir).ThenBy(t => t.Name).ToList();
        }

        private static void CollectConfigLoginEffects(JObject boneMap, string key, Dictionary<string, EffectTarget> output)
        {
            if (boneMap == null) return;
            foreach (KeyValuePair<string, JToken> kv in boneMap)
            {
                string name = kv.Value?.Value<string>();
                if (!string.IsNullOrEmpty(name))
                    Add(output, "skills_effect", name, "ConfigLogin.CreateRole.Effect[" + key + "].clothe." + kv.Key);
            }
        }

        private static void CollectSceneObjectParticle(JObject entry, string dir, string reason,
            Dictionary<string, EffectTarget> output)
        {
            if (entry == null) return;
            CollectAlways(entry["always"] as JObject, dir, reason + ".always", output);
            if (entry["action"] is JObject actions)
            {
                foreach (KeyValuePair<string, JToken> kv in actions)
                    CollectAlways(kv.Value as JObject, dir, reason + ".action." + kv.Key, output);
            }
        }

        private static void CollectAlways(JObject boneMap, string dir, string reason,
            Dictionary<string, EffectTarget> output)
        {
            if (boneMap == null) return;
            foreach (KeyValuePair<string, JToken> kv in boneMap)
            {
                IEnumerable<JToken> list = kv.Value is JArray arr ? arr : new[] { kv.Value };
                foreach (JToken item in list)
                {
                    string name = (item as JObject)?.Value<string>("name");
                    if (!string.IsNullOrEmpty(name))
                        Add(output, dir, name, reason + "." + kv.Key);
                }
            }
        }

        private static void Add(Dictionary<string, EffectTarget> output, string dir, string name, string reason)
        {
            string id = dir + "/" + name;
            if (output.ContainsKey(id)) return;
            output[id] = new EffectTarget
            {
                Dir = dir,
                Name = name,
                Reason = reason,
                LhPath = Path.Combine(LayaUISettings.CdnResourceRoot, "effect", "objs", dir, name + ".lh"),
            };
        }
    }
}
