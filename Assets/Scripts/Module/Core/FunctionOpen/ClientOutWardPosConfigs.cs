using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.FunctionOpen
{
    /// <summary>
    /// ClientOutWardPos.json(对标老端 Config.PRELOAD_CLIENT_CONFIG.ClientOutWardPos)的取景参数。
    /// 功能预告入口框 3D 模型用 key = "FunctionOpenIcon_{pre_x}_{model}"(缺则回退 default_horse/wing/sprite/weapon/artifact/back_ornament)。
    /// 结构与 uimodelparameter 同(scale/position{x,y}/rotate),故复用 <see cref="UiModelParameterConfigs.ModelParam"/>。
    /// </summary>
    public static class ClientOutWardPosConfigs
    {
        private static JObject _root;

        public static async Task EnsureLoaded()
        {
            if (_root != null) return;
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(GameResPath.GetClientConfigPath("ClientOutWardPos"));
            if (asset == null) { _root = new JObject(); GameLog.Info("FuncOpen", "ClientOutWardPos 未导入,预告模型取景用默认"); return; }
            _root = JObject.Parse(asset.text);
            ResManager.Release(asset);
        }

        /// <summary>取 key,缺则 fallbackKey,再缺给默认。预告模型默认不转身(Rotate=0,与角色台的 180 不同)。</summary>
        public static UiModelParameterConfigs.ModelParam Get(string key, string fallbackKey)
        {
            JToken node = _root?[key] ?? (fallbackKey != null ? _root?[fallbackKey] : null);
            var p = new UiModelParameterConfigs.ModelParam { Scale = 1f, Rotate = 0f };
            if (node is JObject o)
            {
                if (o["scale"] != null) p.Scale = (float)o["scale"];
                if (o["position"] is JObject pos)
                    p.Position = new Vector2(pos["x"] != null ? (float)pos["x"] : 0f, pos["y"] != null ? (float)pos["y"] : 0f);
                if (o["rotate"] != null) p.Rotate = (float)o["rotate"];
            }
            return p;
        }
    }
}
