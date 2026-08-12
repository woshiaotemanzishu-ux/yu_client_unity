using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.FairyWish;
using UnityEngine;

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

        public enum EntryRedState
        {
            Bubble = 1,
            RedDot = 2,
            Hidden = 3,
        }

        public readonly struct EntryTouchResult
        {
            public readonly int FairyId;
            public readonly bool Send51302;
            public readonly EntryRedState State;
            public EntryTouchResult(int fairyId, bool send51302, EntryRedState state)
            {
                FairyId = fairyId;
                Send51302 = send51302;
                State = state;
            }
        }

        public enum OperateKind
        {
            Loading,
            PurchaseRequired,
            ActivateNode,
            NodeConditionBlocked,
            Maxed,
        }

        public readonly struct OperateState
        {
            public readonly OperateKind Kind;
            public readonly int NodeId;
            public bool CanSend51301 => Kind == OperateKind.ActivateNode;
            public OperateState(OperateKind kind, int nodeId) { Kind = kind; NodeId = nodeId; }
        }

        private readonly Dictionary<int, FairyEntry> _fairies = new Dictionary<int, FairyEntry>();

        /// <summary>51303 recv-only 点击次数推送落地(对标老端 red_info,fairyId → times)。红点态本身
        /// (2/3 三值语义)耦合 OutWardBaseModel.UpdateOutWardStrongerRed(fairy_id-1000),本轮不实现该耦合,
        /// TODO(PK2 遗留,可检索 "FairyWish 红点耦合"):UI 落地时对接 Pet/OutWard 系统的红点管理。</summary>
        private readonly Dictionary<int, int> _clickTimes = new Dictionary<int, int>();
        private readonly Dictionary<int, EntryRedState> _entryRed = new Dictionary<int, EntryRedState>();

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
            if (!_entryRed.ContainsKey(fairyId)) _entryRed[fairyId] = EntryRedState.RedDot;
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
            foreach ((int fairyId, int times) in clickList)
            {
                _clickTimes[fairyId] = times;
                if (GetEntryRedState(fairyId) != EntryRedState.Hidden) _entryRed[fairyId] = EntryRedState.RedDot;
            }
        }

        public FairyEntry GetFairy(int fairyId) => _fairies.TryGetValue(fairyId, out FairyEntry e) ? e : null;

        /// <summary>缺数据一律返回 0(对标老端 click_list 未含该 fairy_id 时的未定义态降级)。</summary>
        public int GetClickTimes(int fairyId) => _clickTimes.TryGetValue(fairyId, out int t) ? t : 0;

        public EntryRedState GetEntryRedState(int fairyId)
            => _entryRed.TryGetValue(fairyId, out EntryRedState state) ? state : EntryRedState.RedDot;

        /// <summary>
        /// 对标老端 OutWardBaseView.OnEnterFairyWish：仅 Bubble(1) 首次触碰发 51302 并转 RedDot(2)；
        /// 其余入口触碰不发 51302，直接转 Hidden(3)。购买/充值与 51301 节点激活均不在此接口。
        /// </summary>
        public EntryTouchResult ConfirmEntryTouch(int fairyId)
        {
            EntryRedState before = GetEntryRedState(fairyId);
            bool send = before == EntryRedState.Bubble;
            EntryRedState after = send ? EntryRedState.RedDot : EntryRedState.Hidden;
            _entryRed[fairyId] = after;
            return new EntryTouchResult(fairyId, send, after);
        }

        public void SetEntryRedStateForAuthority(int fairyId, EntryRedState state)
        {
            if (fairyId > 0) _entryRed[fairyId] = state;
        }

        public OperateState GetOperateState(int fairyId, int nodeId, int roleLevel)
        {
            FairyEntry entry = GetFairy(fairyId);
            if (entry == null) return new OperateState(OperateKind.Loading, nodeId);
            if (entry.IsBuy == 0) return new OperateState(OperateKind.PurchaseRequired, nodeId);
            int target = nodeId > 0 ? nodeId : GetFirstInactiveNode(entry);
            if (target <= 0) return new OperateState(OperateKind.Maxed, 0);
            int needLevel = FairyWishConfigs.GetNodeOpenLevel(fairyId, target);
            return new OperateState(needLevel <= 0 || roleLevel >= needLevel
                ? OperateKind.ActivateNode : OperateKind.NodeConditionBlocked, target);
        }

        private static int GetFirstInactiveNode(FairyEntry entry)
        {
            int candidate = 0;
            for (int i = 0; i < entry.NodeList.Count; i++)
                if (entry.NodeList[i].IsActivate == 0 && (candidate == 0 || entry.NodeList[i].NodeId < candidate))
                    candidate = entry.NodeList[i].NodeId;
            return candidate;
        }

        public void Reset()
        {
            _fairies.Clear();
            _clickTimes.Clear();
            _entryRed.Clear();
        }
    }

    /// <summary>
    /// config_fairy(主键=fairy_id 字符串,5 条)+ config_fairy_node(主键 "fairy_id@node_id",250 条)读取器。
    /// ⚠仓库这两张表此前已存在,与"148精灵系统"无关,是 fairy_buy(仙灵直购/许愿)的真正数据源
    /// (r18 A组侦察已证实)。P0 已按 CDN 原件 MD5 核对同步,表经 ClientConfigSync 从 yu_client cdn 同步。
    /// </summary>
    public static class FairyWishConfigs
    {
        public sealed class FairyRow
        {
            public int Id;
            public string Name;
            public int Shape;
            public int OpenLevel;
            public int OpenDay;
        }

        private static JObject _fairy;
        private static JObject _node;

        public static bool IsLoaded => _fairy != null && _node != null;
        public static int FairyCount => _fairy?.Count ?? 0;
        public static int NodeCount => _node?.Count ?? 0;

        public static FairyRow GetFairy(int fairyId)
        {
            JObject row = _fairy?[fairyId.ToString()] as JObject;
            return row == null ? null : new FairyRow
            {
                Id = row.Value<int?>("id") ?? fairyId,
                Name = row.Value<string>("name") ?? string.Empty,
                Shape = row.Value<int?>("shape") ?? 0,
                OpenLevel = row.Value<int?>("open_lv") ?? 0,
                OpenDay = row.Value<int?>("open_day") ?? 0,
            };
        }

        public static int GetNodeOpenLevel(int fairyId, int nodeId)
        {
            JObject row = _node?[fairyId + "@" + nodeId] as JObject;
            string raw = row?.Value<string>("condition");
            if (string.IsNullOrEmpty(raw) || raw == "[]") return 0;
            try
            {
                Shenxiao.Framework.Net.ErlangTerm term = Shenxiao.Framework.Net.ErlangParser.Parse(raw);
                if (term?.Items == null) return 0;
                foreach (Shenxiao.Framework.Net.ErlangTerm tuple in term.Items)
                    if (tuple.IsCollection && tuple.Items != null && tuple.Items.Count >= 2
                        && tuple.Items[0].As<string>() == "lv") return tuple.Items[1].As<int>();
            }
            catch (System.Exception e) { GameLog.Warn("FairyWish", "node condition parse failed fairy={0} node={1}: {2}", fairyId, nodeId, e.Message); }
            return 0;
        }

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

    /// <summary>复用现有 FairyWishModule.prefab，负责 51300 查询与弹窗生命周期。</summary>
    public static class FairyWishFlow
    {
        private static GameObject _root;
        private static FairyWishViewBind _view;
        private static int _epoch;
        private static bool _loading;

        public static void Open(int fairyId)
        {
            if (fairyId <= 0) return;
            _ = OpenAsync(fairyId, ++_epoch);
        }

        public static void Close()
        {
            _epoch++;
            if (_view != null && _view.IsShown) _view.Hide();
            if (_root != null) _root.SetActive(false);
        }

        private static async Task OpenAsync(int fairyId, int epoch)
        {
            await FairyWishConfigs.EnsureLoaded();
            FairyWishController.Instance.Init();
            FairyWishController.Instance.RequestInfo(fairyId);
            if (!await EnsureViewAsync() || epoch != _epoch) return;
            _root.SetActive(true);
            _view.Show(fairyId);
            _view.transform.SetAsLastSibling();
        }

        private static async Task<bool> EnsureViewAsync()
        {
            if (_root != null && _view != null) return true;
            if (_loading) return false;
            _loading = true;
            try
            {
                string key = GameResPath.GetUIPrefab("fairyWish", "FairyWishModule");
                _root = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Popup));
                if (_root == null)
                {
                    GameLog.Error("FairyWish", "FairyWishModule load failed: {0}", key);
                    return false;
                }

                _root.name = "FairyWishModule(Runtime)";
                foreach (BaseView child in _root.GetComponentsInChildren<BaseView>(true))
                    child.gameObject.SetActive(false);
                _view = _root.GetComponentInChildren<FairyWishViewBind>(true);
                if (_view == null)
                {
                    GameLog.Error("FairyWish", "FairyWishModule missing FairyWishViewBind");
                    ResManager.ReleaseInstance(_root);
                    _root = null;
                    return false;
                }

                _root.SetActive(false);
                return true;
            }
            finally
            {
                _loading = false;
            }
        }
    }
}
