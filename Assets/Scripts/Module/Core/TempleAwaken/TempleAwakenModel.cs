using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.TempleAwaken
{
    /// <summary>
    /// 天命觉醒(神殿觉醒之路)数据层(对标老端 TempleAwakenModel.ts;服务端 pt_429 lib_temple_awaken)。
    /// 主线卡点 #64:task 100590(ctype81 Open_function)要求发 42900 完成初始任务开启觉醒之路。
    /// 无轮询——42909 是服务端对前置任务(100580)完成态的推送;42900 是客户端唯一发起点,成功后
    /// 服务端 open_temple_awaken 推进 100590,由通用 30001 任务推送自动刷新(本层不重复维护任务态)。
    /// </summary>
    public sealed class TempleAwakenModel
    {
        public static readonly TempleAwakenModel Instance = new TempleAwakenModel();
        private TempleAwakenModel() { }

        /// <summary>前置任务(100580)是否已完成(42909 is_finish:c 推送)。</summary>
        public bool PreTaskFinished { get; private set; }

        /// <summary>觉醒之路是否已开启(42900 error_code==1 成功)。</summary>
        public bool Opened { get; private set; }

        public void SetPreTaskFinished(bool finished)
        {
            PreTaskFinished = finished;
        }

        public void SetOpened(bool opened)
        {
            Opened = opened;
        }

        public void Clear()
        {
            PreTaskFinished = false;
            Opened = false;
        }
    }

    /// <summary>
    /// config_temple_awaken_kv 读取器(具名键,主键为数字字符串:key/value/desc;对标老端 pre_ser_cfg.config_temple_awaken_kv)。
    /// KV(2)=前置任务对「{前置任务ID,觉醒之路任务ID}」={100580,100590};KV(6)=等级门槛「[{lv,48}]」——
    /// 均为 Erlang 字面量字符串,服务端二次校验用,本层只原样存注释不解析(避免臆造解析规则)。
    /// </summary>
    public static class TempleAwakenConfigs
    {
        private static JObject _kv;

        public static bool IsLoaded => _kv != null;

        public static async Task EnsureLoaded()
        {
            if (_kv != null) return;
            string key = GameResPath.GetServerConfigPath("config_temple_awaken_kv");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("TempleAwaken", "missing config_temple_awaken_kv: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                _kv = new JObject();
                return;
            }
            _kv = JObject.Parse(asset.text);
            ResManager.Release(asset);
            GameLog.Info("TempleAwaken", "config_temple_awaken_kv={0}", _kv.Count);
        }

        /// <summary>KV(2) 前置任务对原始字符串,如 "{100580, 100590}"(Erlang 字面量,不解析)。</summary>
        public static string GetPreTaskPairRaw()
        {
            return GetValueRaw(2);
        }

        /// <summary>KV(6) 等级门槛原始字符串,如 "[{lv, 48}]"(Erlang 字面量,不解析——服务端二次校验实际门槛)。</summary>
        public static string GetLevelGateRaw()
        {
            return GetValueRaw(6);
        }

        private static string GetValueRaw(int kvKey)
        {
            if (_kv?[kvKey.ToString()] is JObject obj)
            {
                string value = obj.Value<string>("value");
                if (!string.IsNullOrEmpty(value)) return value;
            }
            return null;
        }
    }
}
