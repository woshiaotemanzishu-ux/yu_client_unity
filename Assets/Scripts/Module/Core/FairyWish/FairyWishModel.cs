using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.FairyWish
{
    /// <summary>
    /// 仙灵祝福(FairyWish)数据层(自动循环 轮18 PK2;对标老端 commonModel/FairyWishModel.ts,服务端 pt_513,
    /// 4 号全活)。51300 落某仙灵全部信息(IsBuy+NodeList);51301 强化节点结果(仅成功才落地);
    /// 51302 send-only 无回包(回执改走后续 51300 主动推送);51303 recv-only 点击次数推送。
    /// </summary>
    public sealed class FairyWishModel
    {
        public static readonly FairyWishModel Instance = new FairyWishModel();
        private FairyWishModel() { }

        public readonly struct NodeEntry
        {
            public readonly int NodeId;
            public readonly int IsActivate;
            public readonly int Combat;
            public NodeEntry(int nodeId, int isActivate, int combat) { NodeId = nodeId; IsActivate = isActivate; Combat = combat; }
        }

        public sealed class FairyEntry
        {
            public int FairyId;
            public int IsBuy;
            public readonly List<NodeEntry> NodeList = new List<NodeEntry>();
        }

        private readonly Dictionary<int, FairyEntry> _fairies = new Dictionary<int, FairyEntry>();

        /// <summary>51303 recv-only 点击次数推送落地(对标老端 red_info,fairyId → times)。红点态本身
        /// (2/3 三值语义)耦合 OutWardBaseModel.UpdateOutWardStrongerRed(fairy_id-1000),本轮不实现该耦合,
        /// TODO(PK2 遗留,可检索 "FairyWish 红点耦合"):UI 落地时对接 Pet/OutWard 系统的红点管理。</summary>
        private readonly Dictionary<int, int> _clickTimes = new Dictionary<int, int>();

        public IReadOnlyDictionary<int, FairyEntry> Fairies => _fairies;

        /// <summary>51300 某仙灵全部信息落地(对标 setFairyInfo):同 id 整条覆盖(node_list 一并整体替换)。</summary>
        public void ApplyInfo(int fairyId, int isBuy, List<(int NodeId, int IsActivate, int Combat)> nodeList)
        {
            var e = new FairyEntry { FairyId = fairyId, IsBuy = isBuy };
            if (nodeList != null)
            {
                foreach ((int nodeId, int isActivate, int combat) in nodeList) e.NodeList.Add(new NodeEntry(nodeId, isActivate, combat));
            }
            _fairies[fairyId] = e;
        }

        /// <summary>51301 强化节点成功后单条套值(对标 updateNodeInfo,调用方已确认 code==1,此处不再复判):
        /// 命中节点 is_activate 置 1。</summary>
        public void ApplyNodeActivate(int fairyId, int nodeId)
        {
            if (!_fairies.TryGetValue(fairyId, out FairyEntry e)) return;
            for (int i = 0; i < e.NodeList.Count; i++)
            {
                if (e.NodeList[i].NodeId == nodeId)
                {
                    e.NodeList[i] = new NodeEntry(nodeId, 1, e.NodeList[i].Combat);
                    break;
                }
            }
        }

        /// <summary>51303 点击次数推送落地(对标 updateRedInfo,仅记录 times,红点值语义留 UI 层,见类头 TODO)。</summary>
        public void ApplyClickPush(List<(int FairyId, int Times)> clickList)
        {
            if (clickList == null) return;
            foreach ((int fairyId, int times) in clickList) _clickTimes[fairyId] = times;
        }

        public FairyEntry GetFairy(int fairyId) => _fairies.TryGetValue(fairyId, out FairyEntry e) ? e : null;

        /// <summary>缺数据一律返回 0(对标老端 click_list 未含该 fairy_id 时的未定义态降级)。</summary>
        public int GetClickTimes(int fairyId) => _clickTimes.TryGetValue(fairyId, out int t) ? t : 0;

        public void Reset()
        {
            _fairies.Clear();
            _clickTimes.Clear();
        }
    }

    /// <summary>
    /// config_fairy(主键=fairy_id 字符串,5 条)+ config_fairy_node(主键 "fairy_id@node_id",250 条)读取器。
    /// ⚠仓库这两张表此前已存在,与"148精灵系统"无关,是 fairy_buy(仙灵直购/许愿)的真正数据源
    /// (r18 A组侦察已证实)。P0 已按 CDN 原件 MD5 核对同步,表经 ClientConfigSync 从 yu_client cdn 同步。
    /// </summary>
    public static class FairyWishConfigs
    {
        private static JObject _fairy;
        private static JObject _node;

        public static bool IsLoaded => _fairy != null && _node != null;
        public static int FairyCount => _fairy?.Count ?? 0;
        public static int NodeCount => _node?.Count ?? 0;

        public static async Task EnsureLoaded()
        {
            if (_fairy != null && _node != null) return;
            await LoadFairy();
            await LoadNode();
        }

        private static async Task LoadFairy()
        {
            if (_fairy != null) return;
            string key = GameResPath.GetServerConfigPath("config_fairy");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("FairyWish", "missing config_fairy: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                _fairy = new JObject();
                return;
            }
            _fairy = JObject.Parse(asset.text);
            ResManager.Release(asset);
            GameLog.Info("FairyWish", "config_fairy={0}", _fairy.Count);
        }

        private static async Task LoadNode()
        {
            if (_node != null) return;
            string key = GameResPath.GetServerConfigPath("config_fairy_node");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("FairyWish", "missing config_fairy_node: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                _node = new JObject();
                return;
            }
            _node = JObject.Parse(asset.text);
            ResManager.Release(asset);
            GameLog.Info("FairyWish", "config_fairy_node={0}", _node.Count);
        }
    }
}
