using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Skill
{
    /// <summary>
    /// Client-side fight movie config for role skills.
    /// Mirrors the old client's ConfigCareerSkillMovies lookup used before sending/playing 20001.
    /// </summary>
    public static class SkillMovieConfigs
    {
        private const string CareerMovieConfig = "configcareerskillmovies";
        private const string MonsterMovieConfig = "configmonsterskillmovies";

        private static JObject _careerMovies;
        private static JObject _monsterMovies;

        public static bool IsLoaded => _careerMovies != null && _monsterMovies != null;

        public static async Task EnsureLoaded()
        {
            if (_careerMovies != null && _monsterMovies != null) return;

            _careerMovies = await LoadConfig(CareerMovieConfig, "ConfigCareerSkillMovies");
            _monsterMovies = await LoadConfig(MonsterMovieConfig, "ConfigMonsterSkillMovies");
        }

        public static bool Has(int skillId) => GetMovie(skillId) != null;

        public static string GetActionName(int skillId)
            => GetMovie(skillId)?.Value<string>("anim") ?? "";

        /// <summary>老端 FightMovieInfo 直接使用的 skill/{sound_res}，空值表示该动作没有主音效。</summary>
        public static string GetSoundRes(int skillId)
            => GetMovie(skillId)?.Value<string>("sound_res") ?? "";

        public static float GetSoundStartTimeSeconds(int skillId)
            => Mathf.Max(0f, GetMovie(skillId)?.Value<float?>("sound_start_time") ?? 0f);

        public static bool SoundRequiresHiter(int skillId)
            => GetMovie(skillId)?.Value<bool?>("sound_need_hiter") ?? false;

        public static float GetConfiguredDurationSeconds(int skillId)
        {
            JObject movie = GetMovie(skillId);
            if (movie == null) return 0f;
            float pre = movie.Value<float?>("pre_swing") ?? 0f;
            float spell = movie.Value<float?>("spell_time") ?? 0f;
            float back = movie.Value<float?>("back_swing") ?? 0f;
            return Mathf.Max(0f, pre + spell + back);
        }

        public static IReadOnlyList<SkillMovieParticle> GetParticles(int skillId)
        {
            JObject movie = GetMovie(skillId);
            JArray arr = movie?["particles"] as JArray;
            if (arr == null || arr.Count == 0)
                return System.Array.Empty<SkillMovieParticle>();

            var list = new List<SkillMovieParticle>(arr.Count);
            foreach (JToken token in arr)
            {
                JObject o = token as JObject;
                if (o == null) continue;
                string res = o.Value<string>("res");
                if (string.IsNullOrEmpty(res)) continue;
                list.Add(new SkillMovieParticle
                {
                    Res = res,
                    PosType = o.Value<int?>("pos_type") ?? 0,
                    DirType = o.Value<int?>("dir_type") ?? 0,
                    StartTime = Mathf.Max(0f, o.Value<float?>("start_time") ?? 0f),
                    PlayTimeLen = Mathf.Max(0f, o.Value<float?>("play_time_len") ?? 0f),
                    Scale = o.Value<float?>("scale") ?? 1f,
                });
            }
            return list;
        }

        /// <summary>
        /// 从当前职业的真实技能栏配置生成首战表现预热清单。这里只收集动作、粒子和声音资源，
        /// 不硬编码职业技能 ID；配置新增/替换技能后，预热范围会自动跟随。
        /// </summary>
        public static async Task<SkillVisualWarmupPlan> BuildCareerWarmupPlanAsync(int career)
        {
            await EnsureLoaded();
            await SkillUIConfigs.EnsureLoaded();

            var actions = new List<string>();
            var effectKeys = new List<string>();
            var soundNames = new List<string>();
            var seenActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenEffects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenSounds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            List<SkillUIConfigs.CareerSkill> skills = SkillUIConfigs.GetCareerSkills(career);
            for (int i = 0; i < skills.Count; i++)
            {
                int skillId = skills[i].SkillId;
                if (skillId <= 0 || GetMovie(skillId) == null) continue;

                string action = GetActionName(skillId);
                if (!string.IsNullOrEmpty(action) && seenActions.Add(action))
                    actions.Add(action);

                string sound = GetSoundRes(skillId);
                if (!string.IsNullOrEmpty(sound) && seenSounds.Add(sound))
                    soundNames.Add(sound);

                IReadOnlyList<SkillMovieParticle> particles = GetParticles(skillId);
                for (int j = 0; j < particles.Count; j++)
                {
                    string res = particles[j]?.Res;
                    if (string.IsNullOrEmpty(res)) continue;
                    string key = GameResPath.GetEffectPrefabPath("skills_effect", res);
                    if (!string.IsNullOrEmpty(key) && seenEffects.Add(key))
                        effectKeys.Add(key);
                }
            }

            return new SkillVisualWarmupPlan(
                actions.ToArray(), effectKeys.ToArray(), soundNames.ToArray());
        }

        private static JObject GetMovie(int skillId)
        {
            if (_careerMovies == null || _monsterMovies == null)
            {
                GameLog.Warn("Skill", "SkillMovieConfigs used before EnsureLoaded skill={0}", skillId);
                return null;
            }
            string key = skillId.ToString();
            return _careerMovies[key] as JObject
                ?? _monsterMovies[key] as JObject;
        }

        private static async Task<JObject> LoadConfig(string cfg, string displayName)
        {
            string key = GameResPath.GetClientConfigPath(cfg);
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Skill", "missing {0}: {1}", displayName, key);
                return new JObject();
            }

            JObject json = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return json;
        }
    }

    public static class OtherFightConfigs
    {
        private const string OtherFightConfig = "configotherfightinfo";

        private static JObject _cfg;

        public static bool IsLoaded => _cfg != null;

        public static async Task EnsureLoaded()
        {
            if (_cfg != null) return;

            string key = GameResPath.GetClientConfigPath(OtherFightConfig);
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Skill", "missing ConfigOtherFightInfo: {0}", key);
                _cfg = new JObject();
                return;
            }

            _cfg = JObject.Parse(asset.text);
            ResManager.Release(asset);
        }

        public static IReadOnlyList<string> GetJumpActionList(int career, int sex)
        {
            JArray arr = _cfg?["jump_name_list"]?[career + "@" + sex] as JArray;
            if (arr == null || arr.Count == 0) return System.Array.Empty<string>();

            var list = new List<string>(arr.Count);
            foreach (JToken token in arr)
            {
                string action = token.Value<string>();
                if (!string.IsNullOrEmpty(action)) list.Add(action);
            }
            return list;
        }

        public static float GetJumpHSpeed(int jumpType, float fallback)
            => GetFloat("jump_hspeed", jumpType.ToString(), fallback);

        public static float GetJumpVSpeed(int jumpType, float fallback)
            => GetFloat("jump_vspeed", jumpType.ToString(), fallback);

        public static float GetJumpGravitySpeed(int jumpType, float fallback)
            => GetFloat("jump_gravity_speed", jumpType.ToString(), fallback);

        public static float GetJumpFallStayTime(int jumpType, int career, float fallback)
            => GetFloat("jump_fall_stay_time", jumpType + "@" + career, fallback);

        public static bool TryGetJumpMotion(int jumpType, int career,
            out float hspeed, out float vspeed, out float gravity, out float fallStay)
        {
            hspeed = 0f;
            vspeed = 0f;
            gravity = 0f;
            fallStay = 0f;

            string key = jumpType.ToString();
            if (!TryGetFloat("jump_hspeed", key, out hspeed)) return false;
            if (!TryGetFloat("jump_vspeed", key, out vspeed)) return false;
            if (!TryGetFloat("jump_gravity_speed", key, out gravity)) return false;
            if (!TryGetFloat("jump_fall_stay_time", jumpType + "@" + career, out fallStay)) return false;
            return true;
        }

        public static float GetTaskJumpMovieSpeedOff(int career, int sex, string action)
        {
            if (string.IsNullOrEmpty(action)) return 0f;
            string actorKey = career + "@" + sex;
            JToken token = _cfg?["task_jump_move_movie_speed_off"]?[actorKey]?[action];
            return token != null ? token.Value<float>() : 0f;
        }

        private static float GetFloat(string section, string key, float fallback)
        {
            if (_cfg?[section]?[key] == null) return fallback;
            return _cfg[section][key].Value<float>();
        }

        private static bool TryGetFloat(string section, string key, out float value)
        {
            JToken token = _cfg?[section]?[key];
            if (token == null)
            {
                value = 0f;
                return false;
            }

            value = token.Value<float>();
            return true;
        }
    }

    public sealed class SkillMovieParticle
    {
        public string Res;
        public int PosType;
        public int DirType;
        public float StartTime;
        public float PlayTimeLen;
        public float Scale = 1f;
    }

    /// <summary>当前职业首战所需的最小表现资源集合。</summary>
    public sealed class SkillVisualWarmupPlan
    {
        public static readonly SkillVisualWarmupPlan Empty =
            new SkillVisualWarmupPlan(
                Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());

        public SkillVisualWarmupPlan(string[] actions, string[] effectKeys, string[] soundNames)
        {
            Actions = actions ?? Array.Empty<string>();
            EffectKeys = effectKeys ?? Array.Empty<string>();
            SoundNames = soundNames ?? Array.Empty<string>();
        }

        public string[] Actions { get; }
        public string[] EffectKeys { get; }
        public string[] SoundNames { get; }
    }
}
