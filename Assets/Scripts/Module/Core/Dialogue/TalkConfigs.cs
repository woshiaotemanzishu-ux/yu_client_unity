using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Dialogue
{
    /// <summary>
    /// 服务端 config_talk 访问器(对标老端 DialogueModel.GetTalkCfg / Config.GetConfigVo("config_talk", talk_id))。
    /// 这是 NPC 对话文字的真实来源——绝不写死假对白。表由 ClientConfigSync 从 yu_client 同步。
    ///
    /// 行格式:{ id:"101", total_content:1, content:"&lt;JSON 串&gt;" }。content 必须二次 JSON.parse(对标
    /// NpcDialogVo.ts:53 talk_cfg.content = JSON.parse(...)),解析后:
    ///   content = { "1": { total_list, list: { "1": {type,text,other}, ..., id } }, "2": {...} }
    /// 即:content[内容块序号(1基)] → { total_list, list }, list[条目序号(1基)] → {type,text,other}。
    /// list 里可能夹一个 "id"(NPC 立绘覆盖 id,老端注"暂时未用到"),解析时跳过非数字键。
    /// </summary>
    public static class TalkConfigs
    {
        /// <summary>一个内容块(content["1"] 等):一组按序播放的对话节点。</summary>
        public sealed class TalkContentBlock
        {
            public int TotalList;
            public readonly List<DialogueNodeVo> Nodes = new List<DialogueNodeVo>();
        }

        /// <summary>一条对话配置(config_talk[talk_id]):若干内容块。</summary>
        public sealed class TalkCfg
        {
            public int Id;
            public int TotalContent;
            public readonly List<TalkContentBlock> Contents = new List<TalkContentBlock>(); // 0 基:Contents[0] == content["1"]

            public int ContentCount => TotalContent > 0 ? TotalContent : Contents.Count;

            /// <summary>取第 index 块(1 基,对标老端 GetTalkContentByIndex)。</summary>
            public TalkContentBlock GetContent(int index1Based)
            {
                int i = index1Based - 1;
                return i >= 0 && i < Contents.Count ? Contents[i] : null;
            }
        }

        private static JObject _talk;
        private static readonly Dictionary<int, TalkCfg> _cache = new Dictionary<int, TalkCfg>();

        public static bool IsLoaded => _talk != null;

        public static async Task EnsureLoaded()
        {
            if (_talk != null) return;

            string key = GameResPath.GetServerConfigPath("config_talk");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Dialogue", "missing config_talk: {0}", key);
                _talk = new JObject();
                return;
            }

            _talk = JObject.Parse(asset.text);
            _cache.Clear();
            ResManager.Release(asset);
            GameLog.Info("Dialogue", "config_talk loaded: {0} talk", _talk.Count);
        }

        /// <summary>按 talk_id 取已解析对话配置;不存在返回 null(老端则回退 DEFAULT_TALK,由 NpcDialogVo 决定)。</summary>
        public static TalkCfg Get(int talkId)
        {
            if (talkId <= 0 || _talk == null) return null;
            if (_cache.TryGetValue(talkId, out TalkCfg cached)) return cached;
            if (!(_talk[talkId.ToString()] is JObject row)) return null;

            TalkCfg cfg = Parse(row);
            if (cfg != null) _cache[talkId] = cfg;
            return cfg;
        }

        private static TalkCfg Parse(JObject row)
        {
            var cfg = new TalkCfg
            {
                Id = ReadInt(row["id"]),
                TotalContent = ReadInt(row["total_content"]),
            };

            string contentStr = row["content"]?.ToString();
            if (string.IsNullOrEmpty(contentStr)) return cfg;

            JObject content;
            try { content = JObject.Parse(contentStr); }
            catch (System.Exception e)
            {
                GameLog.Error("Dialogue", "config_talk content 解析失败 id={0}: {1}", cfg.Id, e.Message);
                return cfg;
            }

            int blockCount = cfg.ContentCount;
            for (int b = 1; b <= blockCount; b++)
            {
                if (!(content[b.ToString()] is JObject blockObj)) continue;
                var block = new TalkContentBlock { TotalList = ReadInt(blockObj["total_list"]) };

                if (blockObj["list"] is JObject listObj)
                {
                    int listCount = block.TotalList > 0 ? block.TotalList : listObj.Count;
                    for (int i = 1; i <= listCount; i++)
                    {
                        if (!(listObj[i.ToString()] is JObject node)) continue; // 跳过非数字键(如 "id")
                        block.Nodes.Add(new DialogueNodeVo(
                            ReadInt(node["type"]),
                            ReadText(node["text"]),
                            node["other"]?.ToString() ?? ""));
                    }
                }
                cfg.Contents.Add(block);
            }
            return cfg;
        }

        // text 多数是字符串;DEFAULT_TALK 等历史数据里偶为数组,取首元素兜底。
        private static string ReadText(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return "";
            if (token is JArray arr) return arr.Count > 0 ? arr[0].ToString() : "";
            return token.ToString();
        }

        private static int ReadInt(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return 0;
            return token.Type == JTokenType.Integer ? token.Value<int>()
                : int.TryParse(token.ToString(), out int v) ? v : 0;
        }
    }
}
