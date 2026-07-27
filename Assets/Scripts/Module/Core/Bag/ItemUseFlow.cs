using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Equip;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包新增物品推荐弹层。
    /// 数据入口只接受 15010/15017/15018 已落地的真实 BagGoods；是否进入候选由 ClientItemUse 配置决定，
    /// 装备再与 15010 pos=1 的当前穿戴评分比较。ItemUseView 的位置和皮肤继续使用转换产物 CommonModule。
    /// </summary>
    public static class ItemUseFlow
    {
        private sealed class ItemUseCfg
        {
            public bool FirstShow;
            public bool OnlyFirstShow;
            public int MinLevel;
            public int MaxLevel = int.MaxValue;
        }

        private static readonly Dictionary<long, BagGoods> Candidates = new Dictionary<long, BagGoods>();
        private static readonly List<long> CandidateOrder = new List<long>();

        private static JObject _config;
        private static Task _configTask;
        private static bool _initialized;
        private static bool _opening;
        private static GameObject _moduleRoot;
        private static ItemUseViewBind _bind;
        private static EquipmentItem _item;
        private static BagGoods _current;

        /// <summary>
        /// 新装备推荐存在时暂停自动任务的自动穿戴，避免 15201 抢先改变穿戴态，
        /// 导致 ItemUseView 刚创建便因“不再更优”而被关闭。
        /// </summary>
        public static bool HasPendingEquipment
        {
            get
            {
                if (_current != null && IsEquipment(_current)) return true;
                for (int i = 0; i < CandidateOrder.Count; i++)
                {
                    if (Candidates.TryGetValue(CandidateOrder[i], out BagGoods goods)
                        && IsEquipment(goods))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            EventDispatcher.On(GlobalEvent.EVT_SCENE_MAP_READY, OnEnvironmentChanged);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnEnvironmentChanged);
        }

        public static async Task EnsureConfigs()
        {
            if (_config != null) return;
            if (_configTask == null) _configTask = LoadConfigs();
            await _configTask;
        }

        /// <summary>15010 主背包全量：只接 ClientItemUse.first_show=true 的配置项，对齐老端登录语义。</summary>
        public static void OnInitialSnapshot(IReadOnlyList<BagGoods> goods)
        {
            AddReceived(goods, initialSnapshot: true);
        }

        /// <summary>15017/15018 正向数量变化：读取落地后的实例，不凭协议号或 typeId 写特殊分支。</summary>
        public static void OnReceived(IReadOnlyList<BagGoods> goods)
        {
            AddReceived(goods, initialSnapshot: false);
        }

        /// <summary>背包或穿戴状态变化后剔除已不存在、已不再更优的候选。</summary>
        public static void OnInventoryStateChanged()
        {
            RemoveInvalidCandidates();
            if (_current != null && !IsEligible(_current))
            {
                CloseCurrent(removeCandidate: true);
            }
            _ = TryShowNext();
        }

        public static void Reset()
        {
            if (_initialized)
            {
                EventDispatcher.Off(GlobalEvent.EVT_SCENE_MAP_READY, OnEnvironmentChanged);
                EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnEnvironmentChanged);
                _initialized = false;
            }

            Candidates.Clear();
            CandidateOrder.Clear();
            _current = null;
            _opening = false;
            _item = null;
            _bind = null;
            if (_moduleRoot != null)
            {
                ResManager.ReleaseInstance(_moduleRoot);
                _moduleRoot = null;
            }
        }

        private static async Task LoadConfigs()
        {
            string key = GameResPath.GetClientConfigPath("clientitemuse");
            TextAsset asset = await ResManager.LoadAsync<TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("ItemUse", "missing ClientItemUse config: {0}", key);
                _config = new JObject();
                return;
            }

            try
            {
                _config = JObject.Parse(asset.text);
                GameLog.Info("ItemUse", "ClientItemUse loaded: {0} rows", _config.Count);
            }
            catch (Exception ex)
            {
                _config = new JObject();
                GameLog.Error("ItemUse", "ClientItemUse parse failed: {0}", ex.Message);
            }
            finally
            {
                ResManager.Release(asset);
            }
        }

        private static void AddReceived(IReadOnlyList<BagGoods> goods, bool initialSnapshot)
        {
            if (goods == null || goods.Count == 0) return;
            _ = AddReceivedAsync(goods, initialSnapshot);
        }

        private static async Task AddReceivedAsync(IReadOnlyList<BagGoods> goods, bool initialSnapshot)
        {
            await Task.WhenAll(EnsureConfigs(), GoodsModel.EnsureLoaded(), MainUIConfigs.EnsureSceneLoaded());

            for (int i = 0; i < goods.Count; i++)
            {
                BagGoods goodsVo = goods[i];
                if (goodsVo == null || goodsVo.GoodsNum <= 0) continue;
                if (!TryGetConfig(goodsVo.TypeId, out ItemUseCfg cfg)) continue;
                if (initialSnapshot && !cfg.FirstShow) continue;
                if (!initialSnapshot && cfg.OnlyFirstShow) continue;
                if (!IsEligible(goodsVo, cfg)) continue;
                AddOrReplaceCandidate(goodsVo);
            }

            await TryShowNext();
        }

        private static void AddOrReplaceCandidate(BagGoods incoming)
        {
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(incoming.TypeId);
            if (basic != null && basic.Type == 10 && basic.EquipType > 0)
            {
                // 同一装备位只保留评分最高的候选，对齐老端 ItemUseModel.equip_list_ 的替换语义。
                for (int i = CandidateOrder.Count - 1; i >= 0; i--)
                {
                    long id = CandidateOrder[i];
                    if (!Candidates.TryGetValue(id, out BagGoods old)) continue;
                    GoodsModel.GoodsBasic oldBasic = GoodsModel.GetGoodsBasicByTypeId(old.TypeId);
                    if (oldBasic == null || oldBasic.Type != 10 || oldBasic.EquipType != basic.EquipType) continue;
                    if (old.Rating >= incoming.Rating) return;
                    Candidates.Remove(id);
                    CandidateOrder.RemoveAt(i);
                }
            }

            if (Candidates.ContainsKey(incoming.GoodsId))
            {
                Candidates[incoming.GoodsId] = incoming;
                return;
            }
            Candidates[incoming.GoodsId] = incoming;
            CandidateOrder.Add(incoming.GoodsId);
        }

        private static async Task TryShowNext()
        {
            if (_opening || _current != null || CandidateOrder.Count == 0) return;
            if (!RoleModel.Instance.HasBaseInfo) return;
            if (!MainUIConfigs.IsFieldScene(RoleModel.Instance.SceneId)) return;

            RemoveInvalidCandidates();
            if (CandidateOrder.Count == 0) return;

            _opening = true;
            try
            {
                if (!await EnsureViewLoaded()) return;
                if (_current != null || CandidateOrder.Count == 0) return;

                long id = CandidateOrder[0];
                if (!Candidates.TryGetValue(id, out BagGoods goods) || !IsEligible(goods))
                {
                    RemoveCandidate(id);
                    return;
                }

                _current = goods;
                _moduleRoot.SetActive(true);
                _bind.Show(goods);
                _bind.transform.SetAsLastSibling();
                RefreshView(goods);
                GameLog.Info("ItemUse", "show typeId={0} goodsId={1} rating={2}",
                    goods.TypeId, goods.GoodsId, goods.Rating);
            }
            catch (Exception ex)
            {
                GameLog.Error("ItemUse", "open failed: {0}\n{1}", ex.Message, ex.StackTrace);
            }
            finally
            {
                _opening = false;
            }
        }

        private static async Task<bool> EnsureViewLoaded()
        {
            if (_moduleRoot != null && _bind != null) return true;

            Transform parent = ViewManager.GetLayer(UILayer.Window);
            if (parent == null)
            {
                GameLog.Error("ItemUse", "Window layer missing");
                return false;
            }

            string key = GameResPath.GetUIPrefab("common", "CommonModule");
            _moduleRoot = await ResManager.InstantiateAsync(key, parent);
            if (_moduleRoot == null)
            {
                GameLog.Error("ItemUse", "CommonModule load failed: {0}", key);
                return false;
            }

            _moduleRoot.name = "CommonModule(ItemUse)";
            BaseView[] views = _moduleRoot.GetComponentsInChildren<BaseView>(true);
            for (int i = 0; i < views.Length; i++) views[i].gameObject.SetActive(false);

            _bind = _moduleRoot.GetComponentInChildren<ItemUseViewBind>(true);
            if (_bind == null)
            {
                GameLog.Error("ItemUse", "CommonModule missing ItemUseViewBind");
                ResManager.ReleaseInstance(_moduleRoot);
                _moduleRoot = null;
                return false;
            }

            if (_bind.enter_btn != null) UIUtil.AddClick(_bind.enter_btn, OnConfirm);
            if (_bind.close_btn != null) UIUtil.AddClick(_bind.close_btn, OnClose);
            _moduleRoot.SetActive(false);
            return true;
        }

        private static void RefreshView(BagGoods goods)
        {
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
            if (basic == null) return;

            if (_bind.name_label != null) _bind.name_label.text = basic.Name;
            if (_bind.enter_btn_text != null) _bind.enter_btn_text.text = basic.Type == 10 ? "装备" : "使用";
            if (_bind.bottom_label != null) _bind.bottom_label.text = string.Empty;
            if (_bind.up_arrow != null) _bind.up_arrow.gameObject.SetActive(basic.Type == 10);

            if (_item == null && _bind._tpl_EquipmentItem != null && _bind.item_group != null)
            {
                GameObject clone = UnityEngine.Object.Instantiate(_bind._tpl_EquipmentItem, _bind.item_group, false);
                clone.name = "EquipmentItem";
                clone.SetActive(true);
                _item = clone.GetComponent<EquipmentItem>();
                if (_item != null) _item.Show();
            }
            if (_bind._tpl_EquipmentItem != null) _bind._tpl_EquipmentItem.SetActive(false);
            if (_bind._tpl_CompositeRuneView != null) _bind._tpl_CompositeRuneView.SetActive(false);
            if (_item != null)
            {
                _item.SetData(goods.TypeId, goods.GoodsNum);
                _item.SetClickCallBack(() => ItemTipsView.Show(goods));
            }
        }

        private static void OnConfirm()
        {
            BagGoods goods = _current;
            if (goods == null) return;

            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
            CloseCurrent(removeCandidate: true);
            if (basic == null) return;

            if (basic.Type == 10)
                EquipWearController.Instance.Wear(goods.GoodsId);
            else
                BagController.Instance.UseGoods(goods.GoodsId, 1);
        }

        private static void OnClose()
        {
            CloseCurrent(removeCandidate: true);
        }

        private static void CloseCurrent(bool removeCandidate)
        {
            long id = _current?.GoodsId ?? 0L;
            _current = null;
            if (removeCandidate && id > 0) RemoveCandidate(id);
            if (_bind != null) _bind.Hide();
            if (_moduleRoot != null) _moduleRoot.SetActive(false);
            _ = TryShowNext();
        }

        private static void OnEnvironmentChanged()
        {
            _ = TryShowNext();
        }

        private static void RemoveInvalidCandidates()
        {
            for (int i = CandidateOrder.Count - 1; i >= 0; i--)
            {
                long id = CandidateOrder[i];
                if (!Candidates.TryGetValue(id, out BagGoods goods) || !IsEligible(goods))
                {
                    Candidates.Remove(id);
                    CandidateOrder.RemoveAt(i);
                }
            }
        }

        private static void RemoveCandidate(long goodsId)
        {
            Candidates.Remove(goodsId);
            CandidateOrder.Remove(goodsId);
        }

        private static bool IsEligible(BagGoods goods)
        {
            return TryGetConfig(goods?.TypeId ?? 0, out ItemUseCfg cfg) && IsEligible(goods, cfg);
        }

        private static bool IsEquipment(BagGoods goods)
        {
            return goods != null && GoodsModel.GetGoodsBasicByTypeId(goods.TypeId)?.Type == 10;
        }

        private static bool IsEligible(BagGoods goods, ItemUseCfg cfg)
        {
            if (goods == null || goods.GoodsNum <= 0) return false;
            BagGoods current = BagModel.Instance.FindContainerGoods(BagModel.POS_BAG, goods.GoodsId);
            if (current == null || current.GoodsNum <= 0) return false;

            RoleModel role = RoleModel.Instance;
            if (role.Level < cfg.MinLevel || role.Level > cfg.MaxLevel) return false;

            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
            if (basic == null || role.Level < basic.Level) return false;
            if (basic.CareerId != 0 && basic.CareerId != role.Career) return false;
            if (basic.Sex != 0 && basic.Sex != role.Sex) return false;
            if (basic.Turn > (role.Figure?.turn ?? 0)) return false;

            if (basic.Type != 10) return true;
            if (basic.EquipType <= 0) return false;
            BagGoods worn = EquipAutoWear.GetWorn(basic.EquipType);
            return worn == null || goods.Rating > worn.Rating;
        }

        private static bool TryGetConfig(int typeId, out ItemUseCfg cfg)
        {
            cfg = null;
            if (typeId <= 0 || _config == null || !(_config[typeId.ToString()] is JObject obj)) return false;
            cfg = new ItemUseCfg
            {
                FirstShow = ReadBool(obj, "first_show"),
                OnlyFirstShow = ReadBool(obj, "only_first_show"),
                MinLevel = ReadInt(obj, "min_lv", 0),
                MaxLevel = ReadInt(obj, "max_lv", int.MaxValue),
            };
            return true;
        }

        private static bool ReadBool(JObject obj, string key)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return false;
            if (token.Type == JTokenType.Boolean) return token.Value<bool>();
            if (token.Type == JTokenType.Integer) return token.Value<int>() != 0;
            return bool.TryParse(token.ToString(), out bool value) && value;
        }

        private static int ReadInt(JObject obj, string key, int fallback)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return fallback;
            return int.TryParse(token.ToString(), out int value) ? value : fallback;
        }
    }
}
