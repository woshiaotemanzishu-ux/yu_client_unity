using System.Collections.Generic;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// Buff 详情弹窗(对标老客户端 MainUIBuffView.ts):竖向列表展示当前角色的 Buff。
    ///
    /// 降级:Buff 数据源(GoodsModel.GET_GOODS_BUFF_LIST 请求 / MainUIModel.buff_list 真相源 /
    /// UPDATE_BUFF_DATA 刷新事件)尚未移植 → 列表暂空、模板隐藏、打日志「待对接 Buff 数据」;
    /// 数据/协议移植后:OnInit 订阅 UPDATE_BUFF_DATA → RefreshBuffList(MainUIModel.buff_list)。
    /// 事件驱动弹层(点 Buff 图标打开),默认关闭、不进 MainUIFlow FirstPass。
    /// 列表项克隆走 MainUIDownView 同款模板模式(_tpl 隐藏 + Instantiate 到容器)。
    /// </summary>
    public sealed class MainUIBuffView : MainUIBuffViewBind
    {
        private readonly List<MainUIBuffItem> _items = new List<MainUIBuffItem>();

        protected override void OnInit()
        {
            if (_tpl_MainUIBuffItem != null) _tpl_MainUIBuffItem.SetActive(false);
            // TODO 待接:EventDispatcher.On(EVT_BUFF_DATA_UPDATED, () => RefreshBuffList(MainUIModel.buff_list))。
        }

        protected override void OnShow(object args)
        {
            // 老端 LoadSuccess 会 Fire(GET_GOODS_BUFF_LIST) 请求数据;数据源未移植 → 先空列表。
            RefreshBuffList(null);
        }

        /// <summary>按数据铺设 Buff 项(对标 UpdateItem 的 LoopScrowViewMgr.replaceAll);null/空=清空。</summary>
        public void RefreshBuffList(IList<BuffItemData> list)
        {
            int count = list?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                MainUIBuffItem item = GetOrCreateItem(i);
                if (item == null) continue;
                item.gameObject.SetActive(true);
                item.SetData(list[i]);
            }
            for (int i = count; i < _items.Count; i++)
            {
                if (_items[i] != null) _items[i].gameObject.SetActive(false);
            }

            if (count == 0)
            {
                GameLog.Info("MainUI", "Buff 列表为空 → 待对接 Buff 数据(GoodsModel / MainUIModel.buff_list)");
            }
        }

        private MainUIBuffItem GetOrCreateItem(int index)
        {
            while (_items.Count <= index) _items.Add(null);
            if (_items[index] != null) return _items[index];

            if (_tpl_MainUIBuffItem == null || _list_buff_con == null)
            {
                GameLog.Error("MainUI", "MainUIBuffView 缺 _tpl_MainUIBuffItem 或 _list_buff_con");
                return null;
            }

            // 项挂到滚动容器的 content 下(对标 _list_buff_con 列表容器)。
            Transform parent = _list_buff_con.content != null ? _list_buff_con.content : _list_buff_con.transform;
            GameObject go = Instantiate(_tpl_MainUIBuffItem, parent);
            go.SetActive(true);

            MainUIBuffItem item = go.GetComponent<MainUIBuffItem>();
            if (item == null)
            {
                GameLog.Error("MainUI", "_tpl_MainUIBuffItem 缺 MainUIBuffItem 组件(回填?)");
                Destroy(go);
                return null;
            }

            _items[index] = item;
            return item;
        }
    }
}
