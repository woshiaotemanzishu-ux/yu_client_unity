using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>模块验收状态(Schemas/LayaUI/ui_acceptance.json,进 git)。验收过的模块重转需确认。</summary>
    public static class LayaUIAcceptance
    {
        private const string PATH = "Schemas/LayaUI/ui_acceptance.json";
        private static Dictionary<string, bool> _data;

        private static Dictionary<string, bool> Load()
        {
            if (_data != null) return _data;
            _data = File.Exists(PATH)
                ? JsonConvert.DeserializeObject<Dictionary<string, bool>>(File.ReadAllText(PATH))
                : new Dictionary<string, bool>();
            return _data ?? (_data = new Dictionary<string, bool>());
        }

        public static bool IsAccepted(string module)
        {
            return Load().TryGetValue(module, out bool v) && v;
        }

        public static void SetAccepted(string module, bool accepted)
        {
            Load()[module] = accepted;
            File.WriteAllText(PATH, JsonConvert.SerializeObject(_data, Formatting.Indented));
            Debug.Log("[LayaUI] 模块 " + module + (accepted ? " 标记验收 ✅(重转将弹确认)" : " 取消验收"));
        }
    }
}
