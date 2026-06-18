using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Shenxiao.Editor.MapTools
{
    /// <summary>
    /// 把场景标注点写回 yu_server data_scene.erl(对标 electron map_editor.py 的 save_scene_fields + _replace_*/_format_*)。
    /// 只改 #ets_scene{...} 记录体里的 mon/npc/elem/reborn_xys/x/y 字段文本,正则定位、保留其余原样;
    /// 写前自动备份 .bak_时间戳。怪(mon)= 普通怪 + 采集怪合并成一个列表回写(采集只是 kind 不同的 mon)。
    /// </summary>
    public static class MapSceneWriter
    {
        public struct Result
        {
            public bool Ok;
            public string Message;
            public string BackupPath;
        }

        /// <summary>
        /// 保存一张场景的标注点。monsters/collects 会合并成 mon 列表;doors→elem;reborns→reborn_xys;birth→x,y。
        /// </summary>
        public static Result SaveScene(int sceneId, SceneEntities e, int birthX, int birthY, string timestamp)
        {
            string path = MapServerData.SceneErlPath;
            if (!File.Exists(path))
                return new Result { Ok = false, Message = "找不到 data_scene.erl: " + path };

            string text;
            try { text = File.ReadAllText(path); }
            catch (Exception ex) { return new Result { Ok = false, Message = "读取失败: " + ex.Message }; }

            // 定位场景块:(get(ID) -> #ets_scene{)(body)(};)
            var blockRe = new Regex(@"(get\(" + sceneId + @"\)\s*->\s*#ets_scene\{)([^;]+)(\};)");
            Match m = blockRe.Match(text);
            if (!m.Success)
                return new Result { Ok = false, Message = $"场景 {sceneId} 在 data_scene.erl 中不存在" };

            string prefix = m.Groups[1].Value;
            string body = m.Groups[2].Value;
            string suffix = m.Groups[3].Value;

            try
            {
                var mon = new List<MapEntity>(e.Monsters);
                mon.AddRange(e.Collects); // 采集与普通怪同属 mon 列表
                body = ReplaceListField(body, "mon", FormatMonList(mon));
                body = ReplaceListField(body, "npc", FormatNpcList(e.Npcs));
                body = ReplaceListField(body, "elem", FormatElemList(e.Doors));
                body = ReplaceListField(body, "reborn_xys", FormatRebornList(e.Reborns));
                body = ReplaceIntField(body, "x", birthX);
                body = ReplaceIntField(body, "y", birthY);
            }
            catch (Exception ex)
            {
                return new Result { Ok = false, Message = "字段替换失败: " + ex.Message };
            }

            // 备份(时间戳由调用方传入,Editor 脚本里 DateTime 可用,这里只拼路径)
            string backup = path + ".bak_" + timestamp;
            try
            {
                File.Copy(path, backup, true);
                string newText = text.Substring(0, m.Index) + prefix + body + suffix + text.Substring(m.Index + m.Length);
                File.WriteAllText(path, newText, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                return new Result { Ok = false, Message = "写入失败: " + ex.Message };
            }

            return new Result { Ok = true, Message = "已保存", BackupPath = backup };
        }

        // ---------- 字段替换(对标 python _replace_*) ----------

        private static string ReplaceIntField(string body, string field, int value)
        {
            var re = new Regex(@"(\b" + field + @"\s*=\s*)\d+");
            return re.Replace(body, mm => mm.Groups[1].Value + value, 1);
        }

        private static string ReplaceListField(string body, string field, string newListText)
        {
            string old = ExtractListText(body, field, out int start);
            if (old == null) throw new InvalidOperationException("字段不存在: " + field);
            return body.Substring(0, start) + newListText + body.Substring(start + old.Length);
        }

        /// <summary>取 field=[...] 的完整文本(方括号平衡),并输出 [ 的起始下标。</summary>
        private static string ExtractListText(string body, string field, out int listStart)
        {
            listStart = -1;
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
                    if (depth == 0) { listStart = start; return body.Substring(start, i - start + 1); }
                }
            }
            return null;
        }

        // ---------- 格式化(对标 python _format_*,Erlang 文本格式逐字一致) ----------

        private static string FormatMonList(List<MapEntity> items)
        {
            if (items == null || items.Count == 0) return "[]";
            var parts = new List<string>(items.Count);
            foreach (MapEntity e in items)
                parts.Add($"[{e.Id}, {e.X}, {e.Y}, {e.Type}, {e.Group}]");
            return "[" + string.Join(" ,", parts) + " ]";
        }

        private static string FormatNpcList(List<MapEntity> items)
        {
            if (items == null || items.Count == 0) return "[]";
            var parts = new List<string>(items.Count);
            foreach (MapEntity e in items)
                parts.Add($"[{e.Id},{e.X},{e.Y},\"{e.Action}\"]");
            return "[" + string.Join(",", parts) + "]";
        }

        private static string FormatElemList(List<MapEntity> items)
        {
            if (items == null || items.Count == 0) return "[[]]";
            var parts = new List<string>(items.Count);
            foreach (MapEntity e in items)
                parts.Add($"{{{e.Id},{e.X},{e.Y},{e.P1},{e.P2}}}");
            return "[" + string.Join(",", parts) + "]";
        }

        private static string FormatRebornList(List<MapEntity> items)
        {
            if (items == null || items.Count == 0) return "[]";
            var parts = new List<string>(items.Count);
            foreach (MapEntity e in items)
                parts.Add($"{{{e.X},{e.Y}}}");
            return "[" + string.Join(",", parts) + "]";
        }
    }
}
