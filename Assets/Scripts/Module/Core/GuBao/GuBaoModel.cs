using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.GuBao
{
    /// <summary>
    /// 古宝(妖物结界守护 soap)数据层(对标老端 commonModel/MonsterModel.ts 的 guBao 段,服务端 pt_133)。
    /// 主线 100811(ctype89)=soap 10001「幽瞳」全部 2 片碎片(1105010011/1105010012)激活。
    /// 13320 全量落此(soap_id → 已激活 debris_id 集合);13321 激活成功后套单个 soap 的碎片集合。
    /// </summary>
    public sealed class GuBaoModel
    {
        public static readonly GuBaoModel Instance = new GuBaoModel();
        private GuBaoModel() { }

        /// <summary>soap_id → 已激活 debris_id 集合。</summary>
        private readonly Dictionary<int, HashSet<int>> _activeDebris = new Dictionary<int, HashSet<int>>();

        public long Combat { get; private set; }
        public bool HasData { get; private set; }

        /// <summary>13320 全量状态(对标 On13320:combat:l + soap_list[u16×{soap_id:h, debris_list[u16×{debris_id:h}]}]);清空重建。</summary>
        public void ApplyFull(long combat, List<(int soapId, List<int> debrisIds)> soapList)
        {
            _activeDebris.Clear();
            if (soapList != null)
            {
                foreach ((int soapId, List<int> debrisIds) in soapList)
                {
                    var set = new HashSet<int>();
                    if (debrisIds != null)
                        foreach (int d in debrisIds) set.Add(d);
                    _activeDebris[soapId] = set;
                }
            }
            Combat = combat;
            HasData = true;
        }

        /// <summary>13321 激活成功套值(对标 On13321 errcode==1:单个 soap 的碎片集合 + 战力)。</summary>
        public void ApplyActive(int soapId, List<int> debrisIds, long combat)
        {
            var set = new HashSet<int>();
            if (debrisIds != null)
                foreach (int d in debrisIds) set.Add(d);
            _activeDebris[soapId] = set;
            Combat = combat;
            HasData = true;
        }

        /// <summary>某 soap 的某碎片是否已激活;缺数据/缺 soap 一律 false(不臆造)。</summary>
        public bool IsDebrisActive(int soapId, int debrisId)
        {
            return _activeDebris.TryGetValue(soapId, out HashSet<int> set) && set.Contains(debrisId);
        }

        /// <summary>某 soap 已激活的碎片集合只读快照(供壳渲染;无数据返回空集合)。</summary>
        public IReadOnlyCollection<int> GetActiveDebris(int soapId)
        {
            return _activeDebris.TryGetValue(soapId, out HashSet<int> set) ? set : System.Array.Empty<int>();
        }

        public void Clear()
        {
            _activeDebris.Clear();
            Combat = 0;
            HasData = false;
        }
    }

    /// <summary>
    /// config_enchantment_guard_soap(具名键 soap_id/soap_name/condition,主键=soap_id 字符串)与
    /// config_enchantment_guard_soap_debris(主键 "soap_id@debris_id",字段 debris_name/cost/attr)读取器。
    /// ⚠仓库已有 config_enchantment_guard_boss/stage_reward,本表是另外两张,勿混淆。表经 ClientConfigSync 从 yu_client cdn 同步。
    /// </summary>
    public static class GuBaoConfigs
    {
        private static JObject _soap;
        private static JObject _debris;

        public static bool IsLoaded => _soap != null && _debris != null;

        public static async Task EnsureLoaded()
        {
            if (_soap != null && _debris != null) return;
            await LoadSoap();
            await LoadDebris();
        }

        private static async Task LoadSoap()
        {
            if (_soap != null) return;
            string key = GameResPath.GetServerConfigPath("config_enchantment_guard_soap");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("GuBao", "missing config_enchantment_guard_soap: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                _soap = new JObject();
                return;
            }
            _soap = JObject.Parse(asset.text);
            ResManager.Release(asset);
            GameLog.Info("GuBao", "config_enchantment_guard_soap={0}", _soap.Count);
        }

        private static async Task LoadDebris()
        {
            if (_debris != null) return;
            string key = GameResPath.GetServerConfigPath("config_enchantment_guard_soap_debris");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("GuBao", "missing config_enchantment_guard_soap_debris: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                _debris = new JObject();
                return;
            }
            _debris = JObject.Parse(asset.text);
            ResManager.Release(asset);
            GameLog.Info("GuBao", "config_enchantment_guard_soap_debris={0}", _debris.Count);
        }

        /// <summary>古宝真名(具名键 soap_name);缺表/缺项降级 "古宝{id}"(标出而非臆造)。</summary>
        public static string GetSoapName(int soapId)
        {
            if (_soap?[soapId.ToString()] is JObject obj)
            {
                string name = obj.Value<string>("soap_name");
                if (!string.IsNullOrEmpty(name)) return name;
            }
            return "古宝" + soapId;
        }

        /// <summary>碎片名(主键 "soap@debris",字段 debris_name);缺表/缺项降级 "碎片{soap}-{debris}"。</summary>
        public static string GetDebrisName(int soapId, int debrisId)
        {
            string key = soapId + "@" + debrisId;
            if (_debris?[key] is JObject obj)
            {
                string name = obj.Value<string>("debris_name");
                if (!string.IsNullOrEmpty(name)) return name;
            }
            return "碎片" + soapId + "-" + debrisId;
        }

        /// <summary>消耗(原样字符串,例 "[{0,1105010011,1}]");缺表/缺项返回 null(壳层自行判断是否显示)。</summary>
        public static string GetDebrisCost(int soapId, int debrisId)
        {
            string key = soapId + "@" + debrisId;
            if (_debris?[key] is JObject obj)
            {
                return obj.Value<string>("cost");
            }
            return null;
        }
    }
}
