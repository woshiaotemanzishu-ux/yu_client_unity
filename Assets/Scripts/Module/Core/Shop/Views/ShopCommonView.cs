using System.Collections.Generic;
using Shenxiao.Generated.UI.Shop;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>
    /// 常规商城(对标老客户端 shop/ShopCommonView.ts):左侧系列页签(_list_tab_con 克隆 ShopSeriesTab,仅灵玉/
    /// 善缘两类型有)+ 商品列表(scroll_group 克隆 ShopItem/ShopLimitItem,渲染类按 shop_type 分派,对标老端
    /// InitShopContent 的 itemRenderer 判定)+ 货币显示(money_conta/num、结社额外 money_conta2/num2)+
    /// 空态(none_conta)+ 荣誉/勋章提示(gloryLabel/_lb_medal_tips)。由 ShopFlow.OnShopTab 调 SetShopType 驱动。
    ///
    /// 简化(规格§0,不臆造):trigger_task_id 过滤走 TaskModel.IsReceivedTask;终生限购已售罄条目沉底
    /// (对标 InitShopContent lifelongList 合并);series 过滤走 HaveThisSeriesIdGoods。红点(结社
    /// RedDotController/善缘每日 cookie)、平台马甲分支(IsEyouEnPlatform 等)未移植,规格§0 已裁定跳过。
    /// </summary>
    public sealed class ShopCommonView : ShopCommonViewBind
    {
        // shop_type → 主货币类型(对标老端 InitEvents cur_money_type 分支;Limit/Diamond/Bind_Diamond/LonglangEx
        // 等常规货币购买类型不显示顶部货币位,对标老端最终 else 分支 money_conta.visible=false)。
        private static readonly Dictionary<int, int> PrimaryMoneyByShopType = new Dictionary<int, int>
        {
            { ShopModel.TYPE_HONER, ShopModel.MONEY_HONER },
            { ShopModel.TYPE_MEDAL_SHOP, ShopModel.MONEY_KF_HOLY_AREA },
            { ShopModel.TYPE_GHOST_WALK, ShopModel.MONEY_GHOST_WALK },
            { ShopModel.TYPE_EXPLOIT_SHOP, ShopModel.MONEY_CSPVP },
            { ShopModel.TYPE_SINGLE_RANK, ShopModel.MONEY_KF_SINGLE_RANK },
            { ShopModel.TYPE_LUCKY, ShopModel.MONEY_LUCKY },
            { ShopModel.TYPE_GOD_COURT, ShopModel.MONEY_GOD_CLIP },
            { ShopModel.TYPE_GUILD, ShopModel.MONEY_GUILD },
        };

        private static readonly Dictionary<int, string> GloryLabelTextByShopType = new Dictionary<int, string>
        {
            { ShopModel.TYPE_HONER, "获取荣耀" },
            { ShopModel.TYPE_EXPLOIT_SHOP, "获取功勋" },
            { ShopModel.TYPE_SINGLE_RANK, "获取试炼币" },
            { ShopModel.TYPE_LUCKY, "获取Boss幸运值" },
            { ShopModel.TYPE_GOD_COURT, "获取劫祈残辉" },
        };

        // Limit/Honer/MedalShop/GhostWalk/ExploitShop/SingleRank/Lucky/GodCourt/Guild 用 ShopLimitItem
        // (带折扣角标+三态限购文案);其余(灵玉/绑玉等常规货币购买)用 ShopItem(对标 InitShopContent itemRenderer 判定)。
        private static readonly HashSet<int> LimitItemShopTypes = new HashSet<int>
        {
            ShopModel.TYPE_LIMIT, ShopModel.TYPE_HONER, ShopModel.TYPE_MEDAL_SHOP, ShopModel.TYPE_GHOST_WALK,
            ShopModel.TYPE_EXPLOIT_SHOP, ShopModel.TYPE_SINGLE_RANK, ShopModel.TYPE_LUCKY, ShopModel.TYPE_GOD_COURT,
            ShopModel.TYPE_GUILD,
        };

        private int _shopType = -1;
        private int _curSeriesId;
        private readonly List<GameObject> _tabCells = new List<GameObject>();
        private readonly List<GameObject> _itemCells = new List<GameObject>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_ShopLimitItem != null) _tpl_ShopLimitItem.SetActive(false);
            if (_tpl_ShopSeriesTab != null) _tpl_ShopSeriesTab.SetActive(false);
            if (_tpl_ShopItem != null) _tpl_ShopItem.SetActive(false);
        }

        protected override void OnShow(object args) => Subscribe();
        protected override void OnHide() => Unsubscribe();
        protected override void OnDispose() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On<int>(GlobalEvent.EVT_SHOP_DATA_UPDATE, OnShopDataUpdate);
            EventDispatcher.On<int>(GlobalEvent.EVT_SHOP_ONE_UPDATE, OnShopOneUpdate);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, RefreshMoneyDisplay); // 钻石/绑钻/铜币(RoleModel)
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, RefreshMoneyDisplay); // 特殊积分/善缘/团结之证等(BagModel,对标老端 GoodsModel.UPDATE_SPECIAL_SCORE)
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off<int>(GlobalEvent.EVT_SHOP_DATA_UPDATE, OnShopDataUpdate);
            EventDispatcher.Off<int>(GlobalEvent.EVT_SHOP_ONE_UPDATE, OnShopOneUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, RefreshMoneyDisplay);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, RefreshMoneyDisplay);
            _subscribed = false;
        }

        private void OnShopDataUpdate(int shopType)
        {
            if (shopType != _shopType) return;
            BuildSeriesTabs();
            RefreshItemList();
        }

        private void OnShopOneUpdate(int keyId)
        {
            ShopModel.GoodsVo vo = ShopModel.Instance.GetShopDataByKeyId(keyId);
            if (vo != null && vo.ShopType == _shopType) RefreshItemList();
        }

        /// <summary>切标签入口(由 ShopFlow.OnShopTab 调,对标老端 CustomMehod → SetData)。</summary>
        public void SetShopType(int shopType)
        {
            _shopType = shopType;
            BuildSeriesTabs();
            RefreshMoneyDisplay();
            RefreshItemList();
        }

        private void BuildSeriesTabs()
        {
            foreach (GameObject go in _tabCells) if (go != null) Object.Destroy(go);
            _tabCells.Clear();

            List<(int id, string desc)> series = ShopConfigs.GetShopSeries(_shopType);
            var filtered = new List<(int id, string desc)>();
            foreach ((int id, string desc) s in series)
                if (ShopModel.Instance.HaveThisSeriesIdGoods(_shopType, s.id)) filtered.Add(s);

            bool hasSeries = filtered.Count > 0;
            if (_list_tab_con != null) _list_tab_con.gameObject.SetActive(hasSeries);
            _curSeriesId = hasSeries ? filtered[0].id : 0;
            if (!hasSeries || _tpl_ShopSeriesTab == null || _list_tab_con == null || _list_tab_con.content == null) return;

            for (int i = 0; i < filtered.Count; i++)
            {
                (int id, string desc) s = filtered[i];
                bool selected = i == 0;
                GameObject cellGo = Object.Instantiate(_tpl_ShopSeriesTab, _list_tab_con.content);
                cellGo.SetActive(true);
                ShopSeriesTab tab = cellGo.GetComponent<ShopSeriesTab>();
                if (tab != null) tab.SetData(s.id, s.desc, selected, () => OnSeriesTabClick(s.id));
                _tabCells.Add(cellGo);
            }
        }

        private void OnSeriesTabClick(int seriesId)
        {
            _curSeriesId = seriesId;
            RefreshItemList();
            foreach (GameObject go in _tabCells)
            {
                ShopSeriesTab tab = go != null ? go.GetComponent<ShopSeriesTab>() : null;
                if (tab != null) tab.SetSelected(tab.SeriesId == seriesId);
            }
        }

        /// <summary>商品列表(对标 InitShopContent):按 trigger_task_id 过滤未接取的任务专属商品 +
        /// 终生限购已售罄条目沉底 + 按 shop_type 选 ShopItem/ShopLimitItem 渲染类。</summary>
        private void RefreshItemList()
        {
            foreach (GameObject go in _itemCells) if (go != null) Object.Destroy(go);
            _itemCells.Clear();

            if (_shopType <= 0) return;
            List<ShopModel.GoodsVo> data = _curSeriesId > 0
                ? ShopModel.Instance.GetShopDataByTypeAndSeriesId(_shopType, _curSeriesId)
                : ShopModel.Instance.GetShopDataByType(_shopType);

            var display = new List<ShopModel.GoodsVo>();
            var lifelongSoldOut = new List<ShopModel.GoodsVo>();
            foreach (ShopModel.GoodsVo vo in data)
            {
                if (vo.TriggerTaskId != 0 && !Shenxiao.Module.Core.Tasks.TaskModel.Instance.IsReceivedTask(vo.TriggerTaskId)) continue;
                if (vo.QuotaType == 3 && vo.QuotaNum <= vo.SoldOut) lifelongSoldOut.Add(vo);
                else display.Add(vo);
            }
            display.AddRange(lifelongSoldOut);

            bool showNone = display.Count == 0;
            if (none_conta != null) none_conta.gameObject.SetActive(showNone);
            if (scroll != null) scroll.gameObject.SetActive(!showNone);

            bool useLimitItem = LimitItemShopTypes.Contains(_shopType);
            GameObject tpl = useLimitItem ? _tpl_ShopLimitItem : _tpl_ShopItem;
            if (tpl == null || scroll_group == null || scroll_group.content == null) return;

            foreach (ShopModel.GoodsVo vo in display)
            {
                GameObject cellGo = Object.Instantiate(tpl, scroll_group.content);
                cellGo.SetActive(true);
                if (useLimitItem) cellGo.GetComponent<ShopLimitItem>()?.SetData(vo);
                else cellGo.GetComponent<ShopItem>()?.SetData(vo);
                _itemCells.Add(cellGo);
            }
            GameLog.Info("Shop", "商城列表刷新 shop_type={0} series={1} count={2}", _shopType, _curSeriesId, display.Count);
        }

        /// <summary>货币显示区(对标 InitEvents cur_money_type 分支 + InitShopContent 尾段货币图标赋值)。</summary>
        private void RefreshMoneyDisplay()
        {
            bool guild = _shopType == ShopModel.TYPE_GUILD;
            if (money_conta2 != null) money_conta2.gameObject.SetActive(guild);
            if (guild)
            {
                if (num2 != null) num2.text = BagModel.Instance.GetTypeGoodsNum(ShopModel.MONEY_GUILD2).ToString();
                SetMoneyIcon(money_icon2, ShopModel.MONEY_GUILD2);
            }

            bool hasPrimary = PrimaryMoneyByShopType.TryGetValue(_shopType, out int moneyType);
            if (money_conta != null) money_conta.gameObject.SetActive(hasPrimary);
            if (hasPrimary)
            {
                if (num != null) num.text = BagModel.Instance.GetSpecialScore(moneyType).ToString();
                SetMoneyIcon(money_icon, moneyType);
            }

            bool showGlory = GloryLabelTextByShopType.TryGetValue(_shopType, out string gloryText);
            if (gloryLabel != null)
            {
                gloryLabel.gameObject.SetActive(showGlory);
                if (showGlory) gloryLabel.text = gloryText;
            }

            bool medalTips = _shopType == ShopModel.TYPE_MEDAL_SHOP || _shopType == ShopModel.TYPE_GHOST_WALK;
            if (_lb_medal_tips != null)
            {
                _lb_medal_tips.gameObject.SetActive(medalTips);
                if (_shopType == ShopModel.TYPE_MEDAL_SHOP) _lb_medal_tips.text = "商店每日0点刷新重置\n参与击杀神陨禁区首领可获得结印";
                else if (_shopType == ShopModel.TYPE_GHOST_WALK) _lb_medal_tips.text = "商城每日4点刷新重置\n参与百煞冲霄活动可获得百煞秘文";
            }
            if (_img_tips != null) _img_tips.gameObject.SetActive(medalTips);
            if (_gp_tips != null) _gp_tips.gameObject.SetActive(_shopType == ShopModel.TYPE_LIMIT);
        }

        private static void SetMoneyIcon(Image img, int moneyType)
        {
            if (img == null) return;
            (int goodsId, int _) = GoodsModel.GetMappingTypeId(-1, moneyType);
            string icon = GoodsModel.GetGoodsIcon(goodsId);
            if (string.IsNullOrEmpty(icon)) return;
            _ = ResManager.SetImageAsync(img, GameResPath.GetGoodsIconPath(icon), false, false);
        }
    }
}
