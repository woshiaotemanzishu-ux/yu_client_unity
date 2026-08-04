using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>老端 ConfigIllusionModel 的只读运行时门面，供 IllusionTips 还原模型构图。</summary>
    public static class IllusionModelConfigs
    {
        public sealed class Entry
        {
            public int TypeId;
            public int ModelType;
            public int ModelRes;
            public long Fight;
            public float Scale = 0.75f;
            public Vector2 Position;
            public float Rotate;
            public string[] Actions = Array.Empty<string>();
        }

        private static JObject _root;
        private static Task _loading;

        public static Task EnsureLoaded()
        {
            if (_root != null) return Task.CompletedTask;
            return _loading ?? (_loading = LoadAsync());
        }

        public static Entry Get(int typeId, int career, int sex)
        {
            if (_root == null || !(_root[typeId.ToString()] is JObject row)) return null;
            bool careerKey = ReadInt(row["use_career_key"]) != 0;
            int key = Mathf.Max(1, careerKey ? career : sex);
            JToken modelRes = Pick(row["model_res"], key);
            if (modelRes == null || !int.TryParse(modelRes.ToString(), out int model) || model <= 0) return null;
            JToken actionToken = Pick(row["action_list"], key);
            return new Entry
            {
                TypeId = typeId,
                ModelType = ReadInt(row["model_type"]),
                ModelRes = model,
                Fight = ReadLong(row["fight"]),
                Scale = ReadFloat(Pick(row["scale"], key), 0.75f),
                Position = ReadPosition(Pick(row["pos"], key)),
                Rotate = ReadFloat(row["rotate"], 0f),
                Actions = actionToken is JArray actions ? actions.ToObject<string[]>() ?? Array.Empty<string>()
                    : Array.Empty<string>(),
            };
        }

        private static async Task LoadAsync()
        {
            string key = GameResPath.GetClientConfigPath("configillusionmodel");
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                _root = new JObject();
                GameLog.Error("IllusionTips", "missing ConfigIllusionModel: {0}", key);
                return;
            }
            _root = JObject.Parse(asset.text);
            ResManager.Release(asset);
        }

        private static JToken Pick(JToken token, int key)
        {
            if (!(token is JObject obj)) return token;
            return obj[key.ToString()] ?? obj["1"] ?? obj.Properties().FirstOrDefault()?.Value;
        }

        private static Vector2 ReadPosition(JToken token)
        {
            if (!(token is JObject obj)) return Vector2.zero;
            return new Vector2(ReadFloat(obj["x"], 0f), ReadFloat(obj["y"], 0f));
        }

        private static int ReadInt(JToken token) =>
            int.TryParse(token?.ToString(), out int value) ? value : 0;

        private static long ReadLong(JToken token) =>
            long.TryParse(token?.ToString(), out long value) ? value : 0L;

        private static float ReadFloat(JToken token, float fallback) =>
            float.TryParse(token?.ToString(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float value) ? value : fallback;
    }
}
