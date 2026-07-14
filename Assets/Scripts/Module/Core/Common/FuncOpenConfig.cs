using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Tasks;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 功能开放判定(对标老客户端 common/CommonManager.CheckFuncOpenState + ConfigFuncOpenCondition)。
    /// 三道门,任一不过即未开放:
    ///   - 开服天 open_day(ServerTimeModel.GetOpenServerDay)
    ///   - 等级   open_lv(RoleModel.Level)
    ///   - 前置任务 open_task(TaskModel.NewestFinishTaskId >= open_task 且非 0)
    /// 表里没有该 view 条目 → 默认开放(对标老端 open 初值 true)。
    /// 注:老端 CheckSpecialCondition 当前对所有功能都返回 false(特殊分支全注释),不移植;
    ///     DungeonPartnerView / BackOrnament* 的特殊任务分支也暂未移植(当前消费方不含这些 view,
    ///     需要时再按 ConfigPartnerAwake 等补)。
    /// </summary>
    public static class FuncOpenConfig
    {
        // 地址=小写约定,源表 yu_client cdn/resource/config/client/ConfigFuncOpenCondition.json
        private const string CONFIG_KEY = "resource/config/client/configfuncopencondition";

        private static JObject _cfg;
        private static Task _loading;

        public static bool IsLoaded => _cfg != null;

        // 单飞:GAME_START 一帧多个图标并发 EnsureLoaded 时只发一次真实加载,
        // 避免"首个用完即还→共乘者拿空"的竞态误报缺表。
        public static Task EnsureLoaded()
        {
            if (_cfg != null) return Task.CompletedTask;
            return _loading ?? (_loading = LoadCoreAsync());
        }

        private static async Task LoadCoreAsync()
        {
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(CONFIG_KEY);
            if (asset == null)
            {
                GameLog.Error("Config", "功能开放表缺失: {0}(跑「神霄/配表/同步客户端配置」)", CONFIG_KEY);
                _loading = null; // 允许下次重试
                return;
            }

            _cfg = JObject.Parse(asset.text);
            ResManager.Release(asset);
        }

        /// <summary>对标 CommonManager.CheckFuncOpenState。表未加载或无该条目按开放处理。</summary>
        public static bool CheckFuncOpenState(string viewClass)
        {
            if (string.IsNullOrEmpty(viewClass) || _cfg == null) return true;
            if (!(_cfg[viewClass] is JObject cond)) return true;

            int openDay = ReadInt(cond, "open_day");
            int openLv = ReadInt(cond, "open_lv");
            int openTask = ReadInt(cond, "open_task");

            if (openDay > 0 && ServerTimeModel.GetOpenServerDay() < openDay) return false;
            if (openLv > 0 && RoleModel.Instance.Level < openLv) return false;
            if (openTask > 0)
            {
                int newest = TaskModel.Instance.NewestFinishTaskId;
                if (newest == 0 || newest < openTask) return false;
            }
            return true;
        }

        private static int ReadInt(JObject obj, string key)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            if (token.Type == JTokenType.Float) return (int)token.Value<float>();
            string value = token.Value<string>();
            return int.TryParse(value, out int v) ? v : 0;
        }
    }
}
