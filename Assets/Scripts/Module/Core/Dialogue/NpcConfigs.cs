using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Dialogue
{
    /// <summary>
    /// 服务端 config_npc 访问器(对标老端 DialogueModel.GetNpcCfg / Config.GetConfigVo("config_npc", npc_id))。
    /// 表由 ClientConfigSync 从 yu_client cdn/resource/config/server/config_npc.json 同步进
    /// Assets/GameRes/resource/config/server/config_npc.json(地址 resource/config/server/config_npc)。
    /// 字段为具名键(非 config_task 的数字索引):id/title/name/talk/icon/icon_type/icon_scale/brith_rot/...
    ///
    /// 用途:① 对话——name(名牌)、talk(NPC 默认对话 id → config_talk);② 场景 NPC 渲染层名牌/缩放/朝向
    /// (此前 NpcRenderer 标注的 "config_npc 未导入" blocker,本表导入后即可接)。
    /// </summary>
    public static class NpcConfigs
    {
        public sealed class NpcCfg
        {
            public int Id;
            public string Name;
            public string Title;
            public int Talk;        // 默认对话 id(查 config_talk)
            public string Icon;     // 模型资源 id(多数 == npc id)
            public int IconType;
            public float IconScale;
            public string Image;    // 头像 id
            public int ArmId;       // 武器
            public float TalkScale; // 对话立绘缩放
            public int BrithRot;    // 出生朝向(-1 表示默认)
        }

        private static JObject _npc;
        private static readonly Dictionary<int, NpcCfg> _cache = new Dictionary<int, NpcCfg>();

        public static bool IsLoaded => _npc != null;

        public static async Task EnsureLoaded()
        {
            if (_npc != null) return;

            string key = GameResPath.GetServerConfigPath("config_npc");
            TextAssetWrap asset = await LoadText(key);
            if (asset == null)
            {
                GameLog.Error("Dialogue", "missing config_npc: {0}", key);
                _npc = new JObject();
                return;
            }

            _npc = JObject.Parse(asset.Text);
            _cache.Clear();
            asset.Release();
            GameLog.Info("Dialogue", "config_npc loaded: {0} npc", _npc.Count);
        }

        public static NpcCfg Get(int npcId)
        {
            if (npcId <= 0 || _npc == null) return null;
            if (_cache.TryGetValue(npcId, out NpcCfg cached)) return cached;
            if (!(_npc[npcId.ToString()] is JObject obj)) return null;

            NpcCfg cfg = new NpcCfg
            {
                Id = ReadInt(obj, "id"),
                Name = ReadString(obj, "name"),
                Title = ReadString(obj, "title"),
                Talk = ReadInt(obj, "talk"),
                Icon = ReadString(obj, "icon"),
                IconType = ReadInt(obj, "icon_type"),
                IconScale = ReadFloat(obj, "icon_scale", 1f),
                Image = ReadString(obj, "image"),
                ArmId = ReadInt(obj, "arm_id"),
                TalkScale = ReadFloat(obj, "talk_scale", 1f),
                BrithRot = ReadInt(obj, "brith_rot"),
            };
            _cache[npcId] = cfg;
            return cfg;
        }

        /// <summary>NPC 默认对话 id(对标 DialogueModel.GetDialogueId:config_npc[npc_id].talk)。</summary>
        public static int GetDialogueId(int npcId) => Get(npcId)?.Talk ?? 0;

        public static string GetModelKey(int npcId, NpcCfg cfg, out string module, out string resId)
        {
            int iconType = cfg?.IconType ?? 0;
            module = GetModelModuleName(iconType);
            resId = GetModelResourceId(npcId, cfg);
            string resName = GetModelPrefix(iconType) + resId;
            return $"object/{module}/{resName}/{resName}";
        }

        public static string GetActionKey(string module, string resId, string actionName)
        {
            return $"object/{module}/action/{resId}/{actionName}";
        }

        private static string GetModelResourceId(int npcId, NpcCfg cfg)
        {
            if (cfg != null && !string.IsNullOrEmpty(cfg.Icon) && cfg.Icon != "0") return cfg.Icon;
            return npcId.ToString();
        }

        private static string GetModelModuleName(int iconType)
        {
            switch (iconType)
            {
                case 1: return "monster";
                case 2: return "pet";
                case 3: return "spirit";
                case 4: return "mount";
                case 5: return "littlepet";
                case 6: return "god";
                case 7: return "child";
                case 8: return "fairy";
                case 0:
                default: return "npc";
            }
        }

        private static string GetModelPrefix(int iconType)
        {
            switch (iconType)
            {
                case 2: return "model_pet_";
                case 3: return "model_spirit_";
                case 4: return "model_mount_";
                case 5: return "model_littlepet_";
                case 6: return "model_god_";
                case 7: return "model_child_";
                case 8: return "model_fairy_";
                case 0:
                case 1:
                default: return "model_clothe_";
            }
        }

        // —— 读取小工具:具名键容错(字符串/数字混排)——
        private static int ReadInt(JObject obj, string key)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            return token.Type == JTokenType.Integer ? token.Value<int>()
                : int.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        private static float ReadFloat(JObject obj, string key, float fallback)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return fallback;
            if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer) return token.Value<float>();
            return float.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float v) ? v : fallback;
        }

        private static string ReadString(JObject obj, string key)
        {
            JToken token = obj[key];
            return token == null || token.Type == JTokenType.Null ? "" : token.ToString();
        }

        // ResManager.LoadAsync 返回 TextAsset 并要 Release,这里包一层避免业务直接拿 Unity 句柄。
        private sealed class TextAssetWrap
        {
            public string Text;
            private UnityEngine.TextAsset _asset;
            public TextAssetWrap(UnityEngine.TextAsset a) { _asset = a; Text = a.text; }
            public void Release() { ResManager.Release(_asset); _asset = null; }
        }

        private static async Task<TextAssetWrap> LoadText(string key)
        {
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            return asset == null ? null : new TextAssetWrap(asset);
        }
    }
}
