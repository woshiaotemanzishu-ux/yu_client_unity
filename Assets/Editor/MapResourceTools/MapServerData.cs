using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Shenxiao.Editor.LayaUI;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.MapTools
{
    public enum MapEntityKind { Monster, Collect, Npc, Door, Boss, Reborn }

    /// <summary>一个场景标注点(世界像素坐标,逻辑格 60×30)。对标 electron MapEditor 的实体。</summary>
    public struct MapEntity
    {
        public MapEntityKind Kind;
        public int Id;
        public int X;
        public int Y;
        public string Name;
        public string Info;   // 门→目标场景/坐标;Boss→类型名;NPC→动画(展示用)
        // 回写 data_scene.erl 需保留的原始字段:
        public int Type;      // 怪:战斗类型
        public int Group;     // 怪:分组
        public string Action; // NPC:动画 state
        public int P1;        // 门:目标 x
        public int P2;        // 门:目标 y
    }

    public sealed class SceneEntities
    {
        public readonly List<MapEntity> Monsters = new List<MapEntity>();
        public readonly List<MapEntity> Collects = new List<MapEntity>();
        public readonly List<MapEntity> Npcs = new List<MapEntity>();
        public readonly List<MapEntity> Doors = new List<MapEntity>();
        public readonly List<MapEntity> Bosses = new List<MapEntity>();
        public readonly List<MapEntity> Reborns = new List<MapEntity>();
        public int BirthX;   // data_scene.erl 里的出生点 x(编辑器编辑/回写的就是它)
        public int BirthY;
        public int Total => Monsters.Count + Collects.Count + Npcs.Count + Doors.Count + Bosses.Count + Reborns.Count;
    }

    /// <summary>
    /// 场景标注点数据(对标 electron map_editor.py):位置来自 yu_server data_scene.erl / data_boss.erl,
    /// 名字来自 yu_client config_mon.json / config_npc.json。纯编辑器期只读解析,正则与 python 一致。
    /// </summary>
    public static class MapServerData
    {
        private const string KEY_SERVER_ROOT = "Shenxiao.Map.ServerRoot";

        public static string ServerRoot
        {
            get
            {
                string def = Path.GetFullPath(Path.Combine(LayaUISettings.ClientRoot, "..", "yu_server"));
                return EditorPrefs.GetString(ProjectKey(KEY_SERVER_ROOT), def);
            }
            set { EditorPrefs.SetString(ProjectKey(KEY_SERVER_ROOT), value); Reload(); }
        }

        private static string ProjectKey(string key) => key + ":" + Application.dataPath.GetHashCode();

        private static string SceneErl => Path.Combine(ServerRoot, "src", "data", "create", "data_scene.erl");
        private static string BossErl => Path.Combine(ServerRoot, "src", "data", "create", "data_boss.erl");
        public static string SceneErlPath => SceneErl;
        public static bool Available => File.Exists(SceneErl);

        private static Dictionary<int, string> _sceneBodies;          // sceneId -> #ets_scene{...} body
        private static Dictionary<int, List<MapEntity>> _bossByScene;  // sceneId -> boss list
        private static Dictionary<int, string> _monNames;
        private static Dictionary<int, int> _monKind;
        private static Dictionary<int, string> _npcNames;

        public static void Reload()
        {
            _sceneBodies = null;
            _bossByScene = null;
            _monNames = null;
            _monKind = null;
            _npcNames = null;
        }

        public static SceneEntities GetEntities(int sceneId)
        {
            EnsureScenes();
            EnsureBosses();
            EnsureNames();

            var se = new SceneEntities();
            if (_sceneBodies != null && _sceneBodies.TryGetValue(sceneId, out string body))
            {
                se.BirthX = IntField(body, "x") ?? 0;
                se.BirthY = IntField(body, "y") ?? 0;
                ParseMon(body, se);
                ParseNpc(body, se);
                ParseElem(body, se);
                ParseReborn(body, se);
            }
            if (_bossByScene != null && _bossByScene.TryGetValue(sceneId, out List<MapEntity> bl))
                se.Bosses.AddRange(bl);
            return se;
        }

        // ---------- data_scene.erl ----------

        private static void EnsureScenes()
        {
            if (_sceneBodies != null) return;
            _sceneBodies = new Dictionary<int, string>();
            if (!File.Exists(SceneErl)) return;
            string text = File.ReadAllText(SceneErl);
            foreach (Match m in Regex.Matches(text, @"get\((\d+)\)\s*->\s*#ets_scene\{([^;]+)\};"))
            {
                int id = int.Parse(m.Groups[1].Value);
                _sceneBodies[id] = m.Groups[2].Value;
            }
        }

        private static void ParseMon(string body, SceneEntities se)
        {
            foreach (string item in ListItems(body, "mon"))
            {
                string[] p = SplitTop(item);
                if (p.Length < 5) continue;
                if (!TryInt(p[0], out int id) || !TryInt(p[1], out int x) || !TryInt(p[2], out int y)) continue;
                int kind = _monKind != null && _monKind.TryGetValue(id, out int k) ? k : 0;
                bool collect = kind == 1 || kind == 2 || kind == 8 || kind == 9;
                int type = p.Length >= 4 && TryInt(p[3], out int tp) ? tp : 0;
                int group = p.Length >= 5 && TryInt(p[4], out int gp) ? gp : 0;
                var e = new MapEntity
                {
                    Kind = collect ? MapEntityKind.Collect : MapEntityKind.Monster,
                    Id = id, X = x, Y = y,
                    Name = MonName(id),
                    Type = type, Group = group,
                };
                if (collect) se.Collects.Add(e); else se.Monsters.Add(e);
            }
        }

        private static void ParseNpc(string body, SceneEntities se)
        {
            foreach (string item in ListItems(body, "npc"))
            {
                string[] p = SplitTop(item);
                if (p.Length < 3) continue;
                if (!TryInt(p[0], out int id) || !TryInt(p[1], out int x) || !TryInt(p[2], out int y)) continue;
                string action = p.Length >= 4 ? p[3].Trim().Trim('"') : "";
                se.Npcs.Add(new MapEntity
                {
                    Kind = MapEntityKind.Npc, Id = id, X = x, Y = y,
                    Name = NpcName(id), Info = action, Action = action,
                });
            }
        }

        private static void ParseElem(string body, SceneEntities se)
        {
            string text = ListText(body, "elem");
            if (string.IsNullOrEmpty(text)) return;
            foreach (Match m in Regex.Matches(text, @"\{(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\}"))
            {
                int target = int.Parse(m.Groups[1].Value);
                int p1 = int.Parse(m.Groups[4].Value);
                int p2 = int.Parse(m.Groups[5].Value);
                se.Doors.Add(new MapEntity
                {
                    Kind = MapEntityKind.Door,
                    Id = target,
                    X = int.Parse(m.Groups[2].Value),
                    Y = int.Parse(m.Groups[3].Value),
                    Name = "传送门",
                    Info = $"→{target}({p1},{p2})",
                    P1 = p1, P2 = p2,
                });
            }
        }

        private static void ParseReborn(string body, SceneEntities se)
        {
            string text = ListText(body, "reborn_xys");
            if (string.IsNullOrEmpty(text)) return;
            foreach (Match m in Regex.Matches(text, @"\{(\d+)\s*,\s*(\d+)\}"))
            {
                se.Reborns.Add(new MapEntity
                {
                    Kind = MapEntityKind.Reborn,
                    X = int.Parse(m.Groups[1].Value),
                    Y = int.Parse(m.Groups[2].Value),
                    Name = "复活点",
                });
            }
        }

        // ---------- data_boss.erl ----------

        private static void EnsureBosses()
        {
            if (_bossByScene != null) return;
            _bossByScene = new Dictionary<int, List<MapEntity>>();
            if (!File.Exists(BossErl)) return;
            string text = File.ReadAllText(BossErl);

            var typeNames = new Dictionary<int, string>();
            foreach (Match m in Regex.Matches(text, "get_boss_type_name\\((\\d+)\\)\\s*->\\s*<<\"([^\"]*)\""))
                typeNames[int.Parse(m.Groups[1].Value)] = m.Groups[2].Value;

            foreach (Match m in Regex.Matches(text, @"get_boss_cfg\((\d+)\)\s*->\s*#boss_cfg\{([^;]+)\};"))
            {
                int bossId = int.Parse(m.Groups[1].Value);
                string body = m.Groups[2].Value;
                int scene = IntField(body, "scene") ?? 0;
                int x = IntField(body, "x") ?? 0;
                int y = IntField(body, "y") ?? 0;
                int type = IntField(body, "type") ?? 0;
                if (scene == 0 || (x == 0 && y == 0)) continue;
                string tn = typeNames.TryGetValue(type, out string s) ? s : "";
                if (!_bossByScene.TryGetValue(scene, out List<MapEntity> list))
                {
                    list = new List<MapEntity>();
                    _bossByScene[scene] = list;
                }
                list.Add(new MapEntity
                {
                    Kind = MapEntityKind.Boss, Id = bossId, X = x, Y = y,
                    Name = string.IsNullOrEmpty(tn) ? "Boss" : tn,
                    Info = "type=" + type,
                });
            }
        }

        // ---------- 名字(config_mon.json / config_npc.json) ----------

        private static void EnsureNames()
        {
            if (_monNames != null && _npcNames != null) return;
            _monNames = new Dictionary<int, string>();
            _monKind = new Dictionary<int, int>();
            _npcNames = new Dictionary<int, string>();

            string monPath = Path.Combine(LayaUISettings.CdnResourceRoot, "config", "server", "config_mon.json");
            if (File.Exists(monPath))
            {
                try
                {
                    JObject cfg = JObject.Parse(File.ReadAllText(monPath));
                    foreach (KeyValuePair<string, JToken> kv in cfg)
                    {
                        if (!int.TryParse(kv.Key, out int id) || !(kv.Value is JObject row)) continue;
                        _monNames[id] = row.Value<string>("1") ?? "";   // 字段 "1" = 怪物名
                        _monKind[id] = ToInt(row["2"]);                 // 字段 "2" = 类型(采集判定)
                    }
                }
                catch (System.Exception e) { Debug.LogWarning("[MapServerData] config_mon.json 解析失败: " + e.Message); }
            }

            string npcPath = Path.Combine(LayaUISettings.CdnResourceRoot, "config", "server", "config_npc.json");
            if (File.Exists(npcPath))
            {
                try
                {
                    JObject cfg = JObject.Parse(File.ReadAllText(npcPath));
                    foreach (KeyValuePair<string, JToken> kv in cfg)
                    {
                        if (!int.TryParse(kv.Key, out int id) || !(kv.Value is JObject row)) continue;
                        string name = row.Value<string>("name");
                        if (string.IsNullOrEmpty(name)) name = row.Value<string>("title");
                        _npcNames[id] = name ?? "";
                    }
                }
                catch (System.Exception e) { Debug.LogWarning("[MapServerData] config_npc.json 解析失败: " + e.Message); }
            }
        }

        private static string MonName(int id) => _monNames != null && _monNames.TryGetValue(id, out string n) && !string.IsNullOrEmpty(n) ? n : "怪" + id;
        private static string NpcName(int id) => _npcNames != null && _npcNames.TryGetValue(id, out string n) && !string.IsNullOrEmpty(n) ? n : "NPC" + id;

        // ---------- 解析工具(对标 python _extract_list_text 等) ----------

        /// <summary>取 field=[...] 的完整文本(方括号平衡)。</summary>
        private static string ListText(string body, string field)
        {
            Match m = Regex.Match(body, @"\b" + field + @"\s*=\s*");
            if (!m.Success) return null;
            int start = m.Index + m.Length;
            int depth = 0;
            for (int i = start; i < body.Length; i++)
            {
                char c = body[i];
                if (c == '[') depth++;
                else if (c == ']')
                {
                    depth--;
                    if (depth == 0) return body.Substring(start, i - start + 1);
                }
            }
            return null;
        }

        /// <summary>取 field 列表里的每个 [..] 子项内容(用于 mon/npc)。</summary>
        private static IEnumerable<string> ListItems(string body, string field)
        {
            string text = ListText(body, field);
            var result = new List<string>();
            if (string.IsNullOrEmpty(text)) return result;
            foreach (Match m in Regex.Matches(text, @"\[([^\[\]]+)\]"))
                result.Add(m.Groups[1].Value);
            return result;
        }

        private static string[] SplitTop(string item) => item.Split(',');

        private static int? IntField(string body, string field)
        {
            Match m = Regex.Match(body, @"\b" + field + @"\s*=\s*(\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value) : (int?)null;
        }

        private static bool TryInt(string s, out int v) =>
            int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v);

        private static int ToInt(JToken t)
        {
            if (t == null) return 0;
            if (t.Type == JTokenType.Integer) return t.Value<int>();
            return int.TryParse(t.ToString(), out int v) ? v : 0;
        }
    }
}
