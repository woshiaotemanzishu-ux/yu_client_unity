using System.Collections.Generic;
using Shenxiao.Generated.UI.Bag;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 主背包面板(对标老客户端 bag/BagComponentView.ts):角色模型展示 + 物品格子滚动列表(bag_con) +
    /// 一键使用/熔炼/红装/扩展/使用 等按钮 + 守护/龙珠入口 + 各红点。
    ///
    /// 第 8 轮 P1:物品格子落地 —— <see cref="OnShow"/> 用 <see cref="BagModel"/>(满背包 15010 回包)铺真实物品格
    /// (克隆 <see cref="BagItemRenderer"/> 模板进 bag_con.content,每格走 BaseAwardItem 真实图标 + 品质底板 + 数量,
    /// 对标老端 BagModel.GetBagList → LoopScrowViewMgr 铺 bagItemRenderer)。渲染模板 bagItemRenderer 是 BagModule 顶层兄弟
    /// (非本视图 Bind 字段)→ 由 <see cref="BagFlow"/> 经 <see cref="SetItemTemplate"/> 注入。背包数据收到(EVT_BAG_UPDATE)即重铺。
    /// 无活服回 15010 时 BagModel 无数据 → 空铺(不造假背包);格子数量/红点/角色模型/子窗路由按既有降级。
    /// 子窗(一键使用/熔炼/扩展)经 <see cref="BagFlow.ToggleSub"/> 打开。事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class BagComponentView : BagComponentViewBind
    {
        private BagItemRenderer _itemTemplate;
        private readonly List<GameObject> _cells = new List<GameObject>();

        // 格子布局(对标 bagItemRenderer 127×127;bag_con viewport ≈580 宽 → 自动算列数)。
        private const float CELL = 127f;
        private const float GAP = 6f;

        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        /// <summary>由 <see cref="BagFlow"/> 注入背包格渲染模板(bagItemRenderer,是 BagModule 顶层兄弟,非本视图 Bind 字段)。</summary>
        public void SetItemTemplate(BagItemRenderer template) => _itemTemplate = template;

        protected override void OnShow(object args)
        {
            BuildGrid();
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, BuildGrid);
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, BuildGrid);
        }

        // ===================== 物品格(真实背包,复用 BaseAwardItem)=====================

        /// <summary>用 BagModel 铺真实物品格(克隆 BagItemRenderer 模板,每格 SetData → 真实图标 + 品质底板 + 数量)。</summary>
        private void BuildGrid()
        {
            ClearCells();

            if (_itemTemplate == null)
            {
                GameLog.Warn("Bag", "背包格模板未注入(BagFlow 未调 SetItemTemplate?)→ 无法铺格");
                return;
            }
            if (bag_con == null || bag_con.content == null)
            {
                GameLog.Warn("Bag", "bag_con/content 缺失,无法铺背包格");
                return;
            }

            RectTransform content = bag_con.content;
            List<BagGoods> goods = BagModel.Instance.BagGoodsList;
            if (!BagModel.Instance.HasData)
            {
                GameLog.Info("Bag", "主背包打开:BagModel 暂无数据(待活服回满背包 15010)→ 空铺;协议链路已就绪(BagController 发 15010 pos=bag)");
                content.sizeDelta = new Vector2(content.sizeDelta.x, 0f);
                return;
            }

            float viewW = bag_con.viewport != null ? bag_con.viewport.rect.width : 580f;
            if (viewW <= 1f) viewW = 580f;
            int cols = Mathf.Max(1, Mathf.FloorToInt((viewW + GAP) / (CELL + GAP)));

            for (int i = 0; i < goods.Count; i++)
            {
                BagGoods vo = goods[i];
                GameObject cellGo = Instantiate(_itemTemplate.gameObject, content);
                cellGo.SetActive(true);
                var rt = (RectTransform)cellGo.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                int col = i % cols, row = i / cols;
                rt.anchoredPosition = new Vector2(col * (CELL + GAP), -row * (CELL + GAP));

                var renderer = cellGo.GetComponent<BagItemRenderer>();
                if (renderer != null) renderer.SetData(new BagItemData { TypeId = vo.TypeId, Count = vo.GoodsNum });
                _cells.Add(cellGo);
            }

            int rows = Mathf.CeilToInt(goods.Count / (float)cols);
            content.sizeDelta = new Vector2(content.sizeDelta.x, rows * (CELL + GAP) + GAP);
            GameLog.Info("Bag", "背包铺格: {0} 件(cols={1} rows={2}),真实图标+品质底板+数量(复用 BaseAwardItem)", goods.Count, cols, rows);
        }

        private void ClearCells()
        {
            for (int i = 0; i < _cells.Count; i++)
                if (_cells[i] != null) Destroy(_cells[i]);
            _cells.Clear();
        }

        // ===================== 按钮 / 红点(降级)=====================

        private void HideReds()
        {
            HideNode(suitRed); HideNode(guard1_red); HideNode(guard2_red);
            HideNode(dragonball_red); HideNode(red_quick); HideNode(useRed); HideNode(smeltRed);
        }

        private void HideTemplates()
        {
            if (_tpl_BagEquipmentIcon != null) _tpl_BagEquipmentIcon.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
        }

        private void BindButtons()
        {
            // 已移植子窗(早期 bag 模块已写 View 并挂载)→ 真切换打开(BagFlow.ToggleSub 叠背包面板上,再点关闭)。
            BindToggle(onekeyBtn, "OneKeyUseView", "一键使用");
            BindToggle(smeltBtn, "BagSmeltView", "熔炼");
            BindToggle(expandBtn, "ExpandBagView", "扩展背包");
            // 未移植/纯逻辑 → 暂日志。
            BindBtn(redequipBtn, "红装");
            BindBtn(useBtn, "使用");
            BindBtn(_btn_guard1, "守护1");
            BindBtn(_btn_guard2, "守护2");
            BindBtn(_btn_dragonball, "龙珠");
        }

        /// <summary>按钮 → 切换背包模块内子窗(BagFlow.ToggleSub 按 View 子类名查找,叠在背包面板上,再点关闭)。</summary>
        private void BindToggle(Component target, string viewType, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                GameLog.Info("Bag", "点击背包按钮[{0}] → 切换 {1}", label, viewType);
                BagFlow.ToggleSub(viewType);
            });
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:子窗/逻辑待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Bag", "点击背包按钮[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
