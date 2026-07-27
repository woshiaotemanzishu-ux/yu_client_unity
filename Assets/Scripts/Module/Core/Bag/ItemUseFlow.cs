using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Common.UI;
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
        // 对标老端 ClientConfigDefaultVo.ClientItemUse；当前 Unity 同步表不包含默认值文件，
        // 这里作为 clientitemuse 缺字段时的配置语义，而不是业务特判。
        private const int DefaultMinLevel = 1;
        private const int DefaultMaxLevel = 999;
        private const int DefaultAutoUseSeconds = 10;
        // 悬浮只表达演出位移，不参与布局：静止基准始终取 CommonModule.prefab 中 _gp_con 的位置。
        // 单程 0.8 秒、向上 12 单位，完整往返 1.6 秒；GameView 缩放到 0.5x 时仍能明确看见。
        private const float HoverDistance = 12f;
        private const float HoverHalfCycle = 0.8f;

        private sealed class ItemUseCfg
        {
            public bool FirstShow;
            public bool OnlyFirstShow;
            public int MinLevel = DefaultMinLevel;
            public int MaxLevel = DefaultMaxLevel;
            public int AutoUseSeconds = DefaultAutoUseSeconds;
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
        private static int _presentationVersion;
        private static Coroutine _presentationCoroutine;
        private static bool _isAnimating;
        private static Vector2 _restingPosition;
        private static bool _hasRestingPosition;
        private static string _actionLabel = string.Empty;

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
            _presentationVersion++;
            StopPresentation();
            _isAnimating = false;
            _hasRestingPosition = false;
            _restingPosition = default;
            _actionLabel = string.Empty;
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
                TryGetConfig(goods.TypeId, out ItemUseCfg cfg);
                int autoUseSeconds = cfg?.AutoUseSeconds ?? 0;
                StopPresentation();
                _presentationCoroutine = _bind.StartCoroutine(
                    PlayPresentation(++_presentationVersion, autoUseSeconds));
                GameLog.Info("ItemUse", "show typeId={0} goodsId={1} rating={2} autoUse={3}s",
                    goods.TypeId, goods.GoodsId, goods.Rating, autoUseSeconds);
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

            Transform parent = ViewManager.GetLayer(UILayer.Popup);
            if (parent == null)
            {
                GameLog.Error("ItemUse", "Popup layer missing");
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
            // 烤制快照来自关闭态：ItemUseView 根虽然会被 BaseView.Show 激活，真正承载全部视觉的
            // _gp_con 却被存成 inactive，结果日志显示 show、屏幕仍完全空白。它是窗口内容而非模板，
            // 每次装载都必须保持激活；具体位置/字号继续由 Prefab 节点负责。
            if (_bind._gp_con != null) _bind._gp_con.gameObject.SetActive(true);
            if (_bind._gp_con != null && !_hasRestingPosition)
            {
                _restingPosition = _bind._gp_con.anchoredPosition;
                _hasRestingPosition = true;
            }
            _moduleRoot.SetActive(false);
            return true;
        }

        private static void RefreshView(BagGoods goods)
        {
            if (_bind?._gp_con != null) _bind._gp_con.gameObject.SetActive(true);
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
            if (basic == null) return;

            if (_bind.name_label != null)
            {
                _bind.name_label.text = basic.Name;
                _bind.name_label.color = LegacyUiColor.GetColor(basic.Color, light: false);
            }
            _actionLabel = basic.Type == 10 ? "装备" : "使用";
            if (_bind.enter_btn_text != null) _bind.enter_btn_text.text = _actionLabel;
            if (_bind.bottom_label != null)
                _bind.bottom_label.text = basic.Type == 10 ? "合成可获更强装备！" : string.Empty;
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
            if (_isAnimating) return;
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
            if (_isAnimating) return;
            CloseCurrent(removeCandidate: true);
        }

        private static void CloseCurrent(bool removeCandidate)
        {
            long id = _current?.GoodsId ?? 0L;
            _presentationVersion++;
            StopPresentation();
            _isAnimating = false;
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
                MinLevel = ReadInt(obj, "min_lv", DefaultMinLevel),
                MaxLevel = ReadInt(obj, "max_lv", DefaultMaxLevel),
                AutoUseSeconds = ReadInt(obj, "auto_use_sec", DefaultAutoUseSeconds),
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

        private static IEnumerator PlayPresentation(int version, int autoUseSeconds)
        {
            if (_bind?._gp_con == null) yield break;

            RectTransform panel = _bind._gp_con;
            // UnityEngine.Object 的“缺失组件”使用重载 == null 判定；不能用 C# ??，否则会保留
            // MissingComponent 代理并在首次访问 alpha 时终止整个协程。
            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group == null) group = panel.gameObject.AddComponent<CanvasGroup>();
            Vector2 end = _hasRestingPosition ? _restingPosition : panel.anchoredPosition;
            Vector2 start = end + Vector2.down * 244f;
            panel.anchoredPosition = start;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            _isAnimating = true;

            const float duration = 0.3f;
            float startedAt = Time.unscaledTime;
            while (version == _presentationVersion && _current != null)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - startedAt) / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                panel.anchoredPosition = Vector2.LerpUnclamped(start, end, eased);
                group.alpha = eased;
                if (t >= 1f) break;
                yield return null;
            }

            if (version != _presentationVersion || _current == null) yield break;
            panel.anchoredPosition = end;
            group.alpha = 1f;
            group.blocksRaycasts = true;
            _isAnimating = false;

            float hoverStartedAt = Time.unscaledTime;
            int remaining = Mathf.Max(0, autoUseSeconds);
            float nextCountdownAt = Time.unscaledTime;
            while (version == _presentationVersion && _current != null && panel != null)
            {
                float elapsed = Time.unscaledTime - hoverStartedAt;
                float ping = Mathf.PingPong(elapsed / HoverHalfCycle, 1f);
                float eased = ping * ping * (3f - 2f * ping);
                panel.anchoredPosition = end + Vector2.up * (HoverDistance * eased);

                if (remaining > 0 && Time.unscaledTime >= nextCountdownAt)
                {
                    remaining--;
                    if (_bind?.enter_btn_text != null)
                        _bind.enter_btn_text.text = _actionLabel + "(" + remaining + "s)";
                    if (remaining <= 0)
                    {
                        _presentationCoroutine = null;
                        OnConfirm();
                        yield break;
                    }
                    nextCountdownAt += 1f;
                }

                yield return null;
            }

            _presentationCoroutine = null;
        }

        private static void StopPresentation()
        {
            if (_presentationCoroutine != null && _bind != null)
            {
                _bind.StopCoroutine(_presentationCoroutine);
            }
            _presentationCoroutine = null;
            if (_bind?._gp_con != null && _hasRestingPosition)
                _bind._gp_con.anchoredPosition = _restingPosition;
        }
    }
}
