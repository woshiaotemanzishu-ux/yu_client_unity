using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Tasks
{
    /// <summary>
    /// Reads old-client ConfigTaskArrow. Missing config means no guide; do not
    /// synthesize task guide text or directions in code.
    /// </summary>
    public static class TaskGuideConfigs
    {
        private const string CONFIG_NAME = "configtaskarrow";

        private const int TIP_KILL = 1;
        private const int TIP_FIN_DUN_TYPE = 9;
        private const int TIP_PASS_MAIN_DUNGEON = 10;
        private const int TIP_RED_EQUIP_COMBINE = 73;
        private const int TIP_MOUNT_LEVEL = 90;

        private static JObject _root;
        private static readonly HashSet<int> _missingLogged = new HashSet<int>();

        public static bool IsLoaded => _root != null;

        public static async Task EnsureLoaded()
        {
            if (_root != null) return;

            string key = GameResPath.GetClientConfigPath(CONFIG_NAME);
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Task", "missing ConfigTaskArrow: {0}", key);
                _root = new JObject();
                return;
            }

            _root = JObject.Parse(asset.text);
            _missingLogged.Clear();
            ResManager.Release(asset);
        }

        public static TaskModel.TaskGuideStep GetNowGuideCfg(bool isMainUiArrow, TaskVo task)
        {
            if (task == null || _root == null) return null;
            JObject nowCfg = ReadObj(_root[task.TaskTipsType.ToString(CultureInfo.InvariantCulture)]);
            if (nowCfg == null)
            {
                LogMissingOnce(task.TaskTipsType, "missing task_tips_type config");
                return null;
            }

            if (isMainUiArrow)
            {
                return ReadMainUiGuide(nowCfg, task);
            }

            return null;
        }

        public static bool HasVisibleMainUiTaskGuide(TaskVo task)
        {
            TaskModel.TaskGuideStep step = GetNowGuideCfg(true, task);
            return step != null && !step.NotShowInTaskItem;
        }

        private static TaskModel.TaskGuideStep ReadMainUiGuide(JObject nowCfg, TaskVo task)
        {
            JObject mainUiCfg = ReadObj(nowCfg["in_main_ui"]);
            if (mainUiCfg == null) return null;

            JObject arrowCfg = ReadIndexedMainUiGuide(mainUiCfg, "task_id_in_main_ui", task.TaskId, task.HasFinish == 1);
            if (task.TaskTipsType == TIP_FIN_DUN_TYPE || task.TaskTipsType == TIP_MOUNT_LEVEL)
            {
                arrowCfg ??= ReadIndexedMainUiGuide(mainUiCfg, "sub_dun_type_in_main_ui", task.Id, task.HasFinish == 1);
            }
            else if (task.TaskTipsType == TIP_PASS_MAIN_DUNGEON)
            {
                arrowCfg ??= ReadIndexedMainUiGuide(mainUiCfg, "sub_dun_type_in_main_ui", task.TaskId, task.HasFinish == 1);
            }
            else if (task.TaskTipsType == TIP_RED_EQUIP_COMBINE && task.HasFinish == 0)
            {
                arrowCfg ??= ReadObj(mainUiCfg["not_finish"]);
            }

            if (arrowCfg == null)
            {
                if (task.HasFinish == 1)
                {
                    arrowCfg = ReadObj(mainUiCfg["finish"]);
                }
                else if (task.TaskTipsType != TIP_KILL)
                {
                    arrowCfg = ReadObj(mainUiCfg["not_finish"]);
                }
            }

            TaskModel.TaskGuideStep step = ReadStep(arrowCfg);
            if (step == null) LogMissingOnce(task.TaskTipsType, "missing main-ui guide step");
            return step;
        }

        private static JObject ReadIndexedMainUiGuide(JObject mainUiCfg, string groupKey, int id, bool finish)
        {
            JObject sub = ReadObj(mainUiCfg[groupKey]);
            if (sub == null) return null;

            JObject item = ReadObj(sub[id.ToString(CultureInfo.InvariantCulture)]);
            if (item == null) return null;
            return ReadObj(item[finish ? "finish" : "not_finish"]);
        }

        private static TaskModel.TaskGuideStep ReadStep(JObject obj)
        {
            if (obj == null) return null;

            string text = ReadString(obj, "text");
            int direction = ReadInt(obj, "direction");
            if (string.IsNullOrWhiteSpace(text) || direction == 0) return null;

            int closeTime = ReadInt(obj, "close_time");
            return new TaskModel.TaskGuideStep
            {
                Direction = direction,
                Text = text,
                CloseTime = closeTime,
                AutoCountdown = closeTime > 0 && !ReadBool(obj, "not_auto_countdown"),
                NotShowInTaskItem = ReadBool(obj, "not_show_in_task_item"),
                NotEffect = ReadBool(obj, "not_effect"),
                OffsetX = ReadFloat(obj, "off_x"),
                OffsetY = ReadFloat(obj, "off_y"),
            };
        }

        private static JObject ReadObj(JToken token)
        {
            return token != null && token.Type == JTokenType.Object ? (JObject)token : null;
        }

        private static string ReadString(JObject obj, string key)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return "";
            return token.Type == JTokenType.String ? token.Value<string>() : token.ToString();
        }

        private static int ReadInt(JObject obj, string key, int fallback = 0)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return fallback;
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            if (token.Type == JTokenType.Float) return (int)token.Value<float>();
            if (token.Type == JTokenType.Boolean) return token.Value<bool>() ? 1 : 0;

            string value = token.Value<string>();
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            {
                return intValue;
            }
            if (float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
            {
                return (int)floatValue;
            }
            return fallback;
        }

        private static float ReadFloat(JObject obj, string key, float fallback = 0f)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return fallback;
            if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer) return token.Value<float>();
            string value = token.Value<string>();
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue)
                ? floatValue : fallback;
        }

        private static bool ReadBool(JObject obj, string key)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return false;
            if (token.Type == JTokenType.Boolean) return token.Value<bool>();
            if (token.Type == JTokenType.Integer) return token.Value<int>() != 0;

            string value = token.Value<string>();
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (bool.TryParse(value, out bool boolValue)) return boolValue;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue)
                && intValue != 0;
        }

        private static void LogMissingOnce(int tipsType, string reason)
        {
            if (!_missingLogged.Add(tipsType)) return;
            GameLog.Warn("Task", "ConfigTaskArrow {0}: tipsType={1}", reason, tipsType);
        }
    }
}
