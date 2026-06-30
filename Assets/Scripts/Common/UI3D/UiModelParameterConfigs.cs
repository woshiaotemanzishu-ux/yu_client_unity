using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// uimodelparameter.json(对标老端 Config.PRELOAD_CLIENT_CONFIG.UIModelParameter)。
    /// 按 [layout_file][show_id] 给 UI 内 3D 模型的取景参数:scale / position{x,y} / rotate(yaw) / action(动画名,取 action_name_list[0])。
    /// 缺 show_id 回退 [layout_file]["defaut"]。文件在 Assets/GameRes/resource/config/client/uimodelparameter.json(已含 MainUIActivityView 块)。
    /// </summary>
    public static class UiModelParameterConfigs
    {
        public sealed class ModelParam
        {
            public float Scale = 1f;
            public Vector2 Position;
            public float Rotate = 180f;   // 默认转身面向相机
            public string Action = "idle"; // 动画名(神兵等会被 action_name_list 覆盖成 "show")
        }

        private static JObject _root;

        public static async Task EnsureLoaded()
        {
            if (_root != null) return;
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(GameResPath.GetClientConfigPath("uimodelparameter"));
            if (asset == null)
            {
                _root = new JObject();
                GameLog.Info("UIModel", "uimodelparameter 未导入,UI 模型取景用默认参数");
                return;
            }
            _root = JObject.Parse(asset.text);
            ResManager.Release(asset);
        }

        /// <summary>取 [layoutFile][showId];缺则 [layoutFile]["defaut"];再缺给默认 ModelParam。</summary>
        public static ModelParam Get(string layoutFile, string showId)
        {
            JObject layout = _root?[layoutFile] as JObject;
            JToken node = layout?[showId] ?? layout?["defaut"];
            return Parse(node);
        }

        private static ModelParam Parse(JToken node)
        {
            var p = new ModelParam();
            if (!(node is JObject o)) return p;
            if (o["scale"] != null) p.Scale = (float)o["scale"];
            if (o["position"] is JObject pos)
                p.Position = new Vector2(pos["x"] != null ? (float)pos["x"] : 0f, pos["y"] != null ? (float)pos["y"] : 0f);
            if (o["rotate"] != null) p.Rotate = (float)o["rotate"];
            if (o["action_name_list"] is JArray arr && arr.Count > 0 && arr[0] != null) p.Action = (string)arr[0];
            return p;
        }
    }
}
