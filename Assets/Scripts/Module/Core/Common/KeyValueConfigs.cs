using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// config_key_value 全局杂项 KV 配置读取器(轮20 P5,spec_serverclock_round20.md 裁决7)。
    /// 表结构:数字键(如 "1"/"11"/"20001")→{"key":同键,"name":中文名,"value":字符串,"desc":说明}。
    /// 老端多个模块共用同一张表(grep "config_key_value" 命中 yu_client\h5\src 下 common/CommonManager.ts、
    /// common/Config.ts、commonController/WelfareController.ts、commonModel/WelfareModel.ts、
    /// halo/HaloMainView.ts、deposit/DepositView.ts、scene/fight/FightDamageManager.ts、
    /// suitActivity/SuitActivityView.ts、topPlayer/TopPlayerRankItem.ts 共9处引用,各自按需读不同 key,
    /// 具体行为未逐个核对),故本读取器落在 Common 而非 Welfare,供全仓按需消费(本轮仅 41708 一个真实消费点
    /// 接线,其余 key 留待各自模块的后续轮次接入)。
    ///
    /// ⚠ value 字段本身是字符串,但其"内容语法"逐 key 不统一,**不提供统一解析器**,只给 <see cref="GetRaw"/>
    /// 返回原始字符串,各消费点自行按已知形态解析:
    ///   · key 1 (?KEY_DOWNLOAD_GIFT,yu_server\include\kv.hrl:9):合法 JSON 数组,
    ///     如 "[{\"0\":0,\"1\":35,\"2\":200},...]"(可直接 JToken.Parse)。
    ///   · key 11 / 20001:合法 JSON 数组,但元素是裸整数,如 "[119,26,200,96]"。
    ///   · key 15 / 18:**单引号**字符串数组,如 "['jzy_sh921_wx_...','...']"——这不是合法 JSON
    ///     (JSON 要求双引号),JObject/JArray.Parse 会抛异常,需要专用解析或正则。
    ///   · 其余 key 多为裸数字/裸字符串(如 "30"/"0"/"大吉大利"),直接当字符串/数字用即可。
    /// 严禁对本表任何 value 走 <see cref="Shenxiao.Framework.Net.ErlangParser"/>——老端 41708 就是这么喂 JSON
    /// 给面向 Erlang term 语法的解析器,产出 1000 个空串垃圾对象(老端存量 bug,WelfareController.cs 已订正)。
    /// </summary>
    public static class KeyValueConfigs
    {
        private static JObject _kv;
        private static Task _loading;

        public static bool IsLoaded => _kv != null;
        public static int Count => _kv?.Count ?? 0;

        // 单飞 + 帧级重试(对标 FuncOpenConfig.cs:34-38 的单飞与 MainUIConfigs.cs:110-125 的重试循环)。
        // 旧实现失败即把 _kv 写成空 JObject,之后 `if (_kv != null) return;` 永久短路、再也不重试
        // ——本读取器唯一的调用时机(41708 跨天/整点消费点)正好落在 Addressables 常未就绪的开局窗口,
        // 一旦命中即整局无数据。改为:失败分支不写 _kv,留 null 让下次 EnsureLoaded 重新发起加载。
        public static Task EnsureLoaded()
        {
            if (_kv != null) return Task.CompletedTask;
            if (_loading == null || _loading.IsCompleted) _loading = LoadWithRetryAsync();
            return _loading;
        }

        private static async Task LoadWithRetryAsync()
        {
            string key = GameResPath.GetServerConfigPath("config_key_value");
            for (int attempt = 0; attempt < 600 && _kv == null; attempt++)
            {
                UnityEngine.TextAsset asset = await ResManager.LoadOptionalAsync<UnityEngine.TextAsset>(key);
                if (asset != null)
                {
                    _kv = JObject.Parse(asset.text);
                    ResManager.Release(asset);
                    GameLog.Info("KeyValueConfigs", "KeyValueConfigs 加载: count={0}", _kv.Count);
                    return;
                }
                await Task.Yield();
            }
            if (_kv == null)
                GameLog.Error("KeyValueConfigs", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝),重试 600 帧仍加载失败", key);
        }

        /// <summary>原始 value 字符串(未解析,见类注释的形态清单)。key 不存在或表未加载返回 null。</summary>
        public static string GetRaw(int key)
        {
            return _kv?[key.ToString(CultureInfo.InvariantCulture)] is JObject obj ? obj.Value<string>("value") : null;
        }

        /// <summary>name 字段(配置项中文名,调试/日志用)。</summary>
        public static string GetName(int key)
        {
            return _kv?[key.ToString(CultureInfo.InvariantCulture)] is JObject obj ? obj.Value<string>("name") : null;
        }
    }
}
