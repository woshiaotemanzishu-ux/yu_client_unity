using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.LimitLevelShop;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.LimitLevelShop
{
    public sealed class LimitLevelShopView : LimitLevelShopViewBind
    {
        private static readonly Vector2[] TabPositions =
        {
            new Vector2(90f, -550f), new Vector2(290f, -580f), new Vector2(500f, -550f),
        };

        private readonly List<GameObject> _tabObjects = new List<GameObject>();
        private readonly List<LimitLevelShopTabItem> _tabs = new List<LimitLevelShopTabItem>();
        private readonly List<GameObject> _rewardObjects = new List<GameObject>();
        private LimitLevelShopTabItem _tabTemplate;
        private LimitLevelShopReward _rewardTemplate;
        private LimitLevelShopModel.GiftEntry _gift;
        private LimitLevelShopModel.GiftConfigSnapshot _config;
        private int _selectedIndex;
        private bool _eventsBound;

        public string IconType { get; private set; }

        protected override void OnInit()
        {
            Transform module = transform.parent;
            if (module != null)
            {
                _tabTemplate = module.GetComponentInChildren<LimitLevelShopTabItem>(true);
                _rewardTemplate = module.GetComponentInChildren<LimitLevelShopReward>(true);
            }
            if (_tabTemplate != null) _tabTemplate.gameObject.SetActive(false);
            if (_rewardTemplate != null) _rewardTemplate.gameObject.SetActive(false);
            if (_lable_tips != null) _lable_tips.gameObject.SetActive(false);
            BindClick(_img_close, LimitLevelShopFlow.Close);
            BindClick(_gp_buy, OnBlockedPurchase);
            if (_img_bg != null)
                _ = ResManager.SetImageAsync(_img_bg,
                    GameResPath.GetIcon("limitLevelShop", "uidjqg_001"), false, false);
        }

        protected override void OnShow(object args)
        {
            IconType = args as string ?? LimitLevelShopModel.ICON_TYPE;
            BindEvents();
            RefreshData();
        }

        protected override void OnHide()
        {
            UnbindEvents();
            ClearDynamic();
            _gift = null;
            _config = null;
        }

        private void Update()
        {
            if (!IsShown || _gift == null || _lb_left_time == null) return;
            long left = Math.Max(0L, _gift.EndTime - TimeUtil.NowSec());
            _lb_left_time.text = "购买剩余时间:" + FormatCountdown(left);
        }

        private void RefreshData()
        {
            if (!IsShown) return;
            _gift = LimitLevelShopModel.Instance.FindByIcon(IconType);
            if (_gift == null)
            {
                TipsManager.Toast("活动未开启");
                LimitLevelShopFlow.Close();
                return;
            }
            if (!LimitLevelShopModel.Instance.TryGetGiftConfig(_gift.Type, _gift.Subtype, out _config))
            {
                if (_lb_btn != null) _lb_btn.text = "数据加载中";
                return;
            }
            RefreshHeader();
            RefreshTabs();
            SelectTab(FindDefaultIndex());
        }

        private void RefreshHeader()
        {
            IReadOnlyList<ErlangTerm> title = FindTuple(_gift.ActCondition, "title");
            if (title != null && title.Count >= 2) LoadOther(_img_title, title[1].As<string>());
            Update();
        }

        private void RefreshTabs()
        {
            ClearDynamic();
            if (_tabTemplate == null || _gp_tab_con == null || _config == null) return;
            for (int i = 0; i < _config.Entries.Count; i++)
            {
                LimitLevelShopModel.GiftConfigEntry cfg = _config.Entries[i];
                GameObject go = Instantiate(_tabTemplate.gameObject, _gp_tab_con, false);
                LimitLevelShopTabItem item = go.GetComponent<LimitLevelShopTabItem>();
                int index = i;
                Vector2 pos = i < TabPositions.Length ? TabPositions[i] : new Vector2(90f + i * 200f, -550f);
                if (item != null)
                {
                    item.SetData(_gift, cfg, pos, () => SelectTab(index));
                    _tabs.Add(item);
                }
                else go.SetActive(true);
                _tabObjects.Add(go);
            }
        }

        private int FindDefaultIndex()
        {
            if (_config == null) return 0;
            for (int i = 0; i < _config.Entries.Count; i++)
                if (_gift.GetState(_config.Entries[i].Grade) < 1) return i;
            return 0;
        }

        private void SelectTab(int index)
        {
            if (_config == null || index < 0 || index >= _config.Entries.Count) return;
            _selectedIndex = index;
            for (int i = 0; i < _tabs.Count; i++) _tabs[i].SetSelected(i == index);
            RefreshSelected(_config.Entries[index]);
        }

        private void RefreshSelected(LimitLevelShopModel.GiftConfigEntry cfg)
        {
            byte state = _gift.GetState(cfg.Grade);
            if (_gp_price != null) _gp_price.gameObject.SetActive(state == 0);
            if (_lb_btn != null) _lb_btn.text = state == 0 ? "立即抢购" : "已购买";
            RefreshPrice(cfg);
            RefreshRewards(cfg.Reward);
            RefreshPresentation(cfg.Show);
        }

        private void RefreshPrice(LimitLevelShopModel.GiftConfigEntry cfg)
        {
            int costType = ReadTupleInt(cfg.Condition, "cost_type");
            bool currency = costType == 1 || costType == 2;
            SetActive(_img_last_price, currency);
            SetActive(_img_now_price, currency);
            SetActive(_img_now_price0, currency);
            if (_lb_last_price != null) _lb_last_price.text = currency ? cfg.NormalCost : ToYuan(cfg.NormalCost);
            if (_lb_now_price != null) _lb_now_price.text = currency ? cfg.Cost : ToYuan(cfg.Cost);
            if (_lb_now_price0 != null) _lb_now_price0.text = currency ? cfg.Cost : "仅售" + ToYuan(cfg.Cost);
            if (currency)
            {
                string icon = costType == 1 ? "com_diamond" : "com_bind_diamond";
                LoadCommon(_img_last_price, icon);
                LoadCommon(_img_now_price, icon);
                LoadCommon(_img_now_price0, icon);
            }
        }

        private void RefreshRewards(string raw)
        {
            for (int i = 0; i < _rewardObjects.Count; i++) if (_rewardObjects[i] != null) Destroy(_rewardObjects[i]);
            _rewardObjects.Clear();
            if (_rewardTemplate == null || _Scroller1 == null) return;
            Transform parent = _Scroller1.content != null ? _Scroller1.content : _Scroller1.transform;
            ErlangTerm root;
            try { root = ErlangParser.Parse(string.IsNullOrEmpty(raw) ? "[]" : raw); }
            catch { return; }
            if (root?.Items == null) return;
            for (int i = 0; i < root.Items.Count; i++)
            {
                IReadOnlyList<ErlangTerm> tuple = root.Items[i]?.Items;
                if (tuple == null || tuple.Count < 3) continue;
                GameObject go = Instantiate(_rewardTemplate.gameObject, parent, false);
                LimitLevelShopReward reward = go.GetComponent<LimitLevelShopReward>();
                if (reward != null) reward.SetData(tuple[0].As<int>(), tuple[1].As<int>(), tuple[2].As<int>());
                else go.SetActive(true);
                _rewardObjects.Add(go);
            }
        }

        private void RefreshPresentation(string raw)
        {
            IReadOnlyList<ErlangTerm> tips = FindTuple(raw, "tips_show");
            bool hasTips = tips != null && tips.Count >= 2;
            SetActive(_img_tips, hasTips);
            if (hasTips) LoadOther(_img_tips, tips[1].As<string>());

            IReadOnlyList<ErlangTerm> mark = FindTuple(raw, "tab_mark");
            bool hasMark = mark != null && mark.Count >= 2;
            SetActive(_img_tab, hasMark);
            if (hasMark && _img_tab != null)
                _ = ResManager.SetImageAsync(_img_tab,
                    GameResPath.GetIcon("limitLevelShop", mark[1].As<string>()), false, false);

            IReadOnlyList<ErlangTerm> show = FindTuple(raw, "show");
            string mode = show != null && show.Count >= 2 ? show[1].As<string>() : "";
            SetActive(_gp_model, mode == "show_model");
            SetActive(_gp_effect, mode == "show_effect");
            SetActive(_img_show, mode == "show_icon");
            SetActive(_gp_rune, mode == "goods_id");
            if (mode == "show_icon" && show.Count >= 3) LoadOther(_img_show, show[2].As<string>());
            else if (mode == "goods_id" && show.Count >= 3)
                LoadOther(_img_rune, "card_" + show[2].As<int>());
            else if (mode == "show_model" || mode == "show_effect")
                GameLog.Warn("LimitLevelShop", "{0} 动态展示需真实运行态补模型/特效证据", mode);
        }

        private void OnBlockedPurchase()
        {
            LimitLevelShopModel.GiftConfigEntry cfg = _config != null && _selectedIndex >= 0 && _selectedIndex < _config.Entries.Count
                ? _config.Entries[_selectedIndex] : null;
            if (cfg != null && _gift.GetState(cfg.Grade) != 0)
                TipsManager.Toast("礼包限购一次，无法再次购买");
            else
                TipsManager.Toast("购买功能尚未完成安全接入");
            // 明确不发送 61201/15804；购买叶在 schema 6 台账保持 blocked。
        }

        private void BindEvents()
        {
            if (_eventsBound) return;
            LimitLevelShopModel.Instance.DataChanged += OnDataChanged;
            _eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!_eventsBound) return;
            LimitLevelShopModel.Instance.DataChanged -= OnDataChanged;
            _eventsBound = false;
        }

        private void OnDataChanged() => RefreshData();

        private void ClearDynamic()
        {
            for (int i = 0; i < _tabObjects.Count; i++) if (_tabObjects[i] != null) Destroy(_tabObjects[i]);
            for (int i = 0; i < _rewardObjects.Count; i++) if (_rewardObjects[i] != null) Destroy(_rewardObjects[i]);
            _tabObjects.Clear();
            _tabs.Clear();
            _rewardObjects.Clear();
        }

        internal static IReadOnlyList<ErlangTerm> FindTuple(string raw, string key)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            ErlangTerm root;
            try { root = ErlangParser.Parse(raw); }
            catch { return null; }
            if (root?.Items == null) return null;
            for (int i = 0; i < root.Items.Count; i++)
            {
                IReadOnlyList<ErlangTerm> tuple = root.Items[i]?.Items;
                if (tuple != null && tuple.Count >= 2 && tuple[0].As<string>() == key) return tuple;
            }
            return null;
        }

        private static int ReadTupleInt(string raw, string key)
        {
            IReadOnlyList<ErlangTerm> tuple = FindTuple(raw, key);
            if (tuple == null || tuple.Count < 2) return 0;
            try { return tuple[1].As<int>(); }
            catch { return 0; }
        }

        private static string ToYuan(string raw) => decimal.TryParse(raw, out decimal value)
            ? (value / 10m).ToString("0.##") + "元" : raw;

        private static string FormatCountdown(long seconds)
        {
            TimeSpan span = TimeSpan.FromSeconds(Math.Max(0L, seconds));
            if (span.TotalDays >= 1) return ((int)span.TotalDays) + "天" + span.Hours.ToString("00") + "时";
            if (span.TotalHours >= 1) return ((int)span.TotalHours).ToString("00") + ":" + span.Minutes.ToString("00");
            return span.Minutes.ToString("00") + ":" + span.Seconds.ToString("00");
        }

        private static void LoadOther(Image image, string name)
        {
            if (image == null || string.IsNullOrEmpty(name)) return;
            image.gameObject.SetActive(true);
            _ = ResManager.SetImageAsync(image, GameResPath.GetIconOtherPath("limitLevelShop", name), false, false);
        }

        private static void LoadCommon(Image image, string name)
        {
            if (image == null || string.IsNullOrEmpty(name)) return;
            image.gameObject.SetActive(true);
            _ = ResManager.SetImageAsync(image, GameResPath.GetIcon("common", name), false, false);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }

        private static void BindClick(Component target, Action action)
        {
            if (target == null) return;
            Graphic graphic = target as Graphic ?? target.GetComponent<Graphic>() ?? target.GetComponentInChildren<Graphic>(true);
            if (graphic != null) UIUtil.ClearClicks(graphic);
            UIUtil.AddClick(target, action);
        }
    }
}
