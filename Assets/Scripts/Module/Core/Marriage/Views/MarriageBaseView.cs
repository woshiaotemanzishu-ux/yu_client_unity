using System.Collections.Generic;
using Shenxiao.Generated.UI.Marriage;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;

namespace Shenxiao.Module.Core.Marriage
{
    /// <summary>
    /// 婚恋主界面(对标老客户端 marriage/MarriageBaseView.ts):分页容器。顶部页签条(_group_btn,HorizontalLayoutGroup)
    /// 由 _tpl_MarriageBaseBtn 克隆 5 个页签按钮(大厅/姻缘/指环/锦囊/副本),点击切换下方页内容;右上 _btn_close 关闭。
    /// 5 页内容直接用 Bind 的 _tpl_* 页节点(大厅 MarriageFriendView / 姻缘 MarriageMainView / 指环 MarriageRingView /
    /// 锦囊 MarriageGiftView / 副本 MarriageDunView),ShowTab 切换其显隐并 Show 对应 BaseView。
    ///
    /// 我管结构:页签构建 + 切换逻辑(本类);用户管效果:页签按钮/页内容的位置大小由预制体调
    /// (_group_btn 已有 HLG 自动横排;页节点位置归预制体)。降级:各页后端(MarriageModel/协议)未移植 → 页内空,
    /// 但「点页签切页」的可见效果完整可用。事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class MarriageBaseView : MarriageBaseViewBind
    {
        // 页签顺序对标老端 _tabDataList_:大厅/姻缘/指环/锦囊/副本。
        private static readonly string[] TabLabels = { "大厅", "姻缘", "指环", "锦囊", "副本" };

        private GameObject[] _pages;
        private readonly List<MarriageBaseBtn> _tabButtons = new List<MarriageBaseBtn>();
        private int _curTab = -1;
        private bool _tabsBuilt;

        protected override void OnInit()
        {
            // 页内容节点(与 TabLabels 一一对应)。
            _pages = new[]
            {
                _tpl_MarriageFriendView,
                _tpl_MarriageMainView,
                _tpl_MarriageRingView,
                _tpl_MarriageGiftView,
                _tpl_MarriageDunView,
            };

            // 页签按钮模板隐藏(作克隆源);先隐藏全部页内容,待 ShowTab 选中。
            if (_tpl_MarriageBaseBtn != null) _tpl_MarriageBaseBtn.SetActive(false);
            HideAllPages();

            if (_btn_close != null)
            {
                _btn_close.raycastTarget = true;
                UIUtil.AddClick(_btn_close, Hide);
            }

            BuildTabs();
        }

        protected override void OnShow(object args)
        {
            // 打开默认落到大厅页(对标老端 _default_index_ = 0)。
            ShowTab(_curTab < 0 ? 0 : _curTab);
            GameLog.Info("Marriage", "婚恋主界面打开 → 页签可切换;各页数据待对接 MarriageModel(降级空)");
        }

        /// <summary>对标老端 LoadSuccess 构建页签:克隆 _tpl_MarriageBaseBtn 到 _group_btn(HLG 自动横排),填文字 + 绑点击。</summary>
        private void BuildTabs()
        {
            if (_tabsBuilt) return;
            if (_tpl_MarriageBaseBtn == null || _group_btn == null)
            {
                GameLog.Warn("Marriage", "MarriageBaseView 缺 _tpl_MarriageBaseBtn / _group_btn,页签无法构建(重跑 marriage 流水线)");
                return;
            }

            for (int i = 0; i < TabLabels.Length; i++)
            {
                GameObject go = Instantiate(_tpl_MarriageBaseBtn, _group_btn);
                go.SetActive(true);
                MarriageBaseBtn btn = go.GetComponent<MarriageBaseBtn>();
                if (btn == null)
                {
                    GameLog.Warn("Marriage", "页签模板缺 MarriageBaseBtn 组件(重跑回填)");
                    Destroy(go);
                    continue;
                }
                btn.SetData(TabLabels[i], i, OnTabClick);
                _tabButtons.Add(btn);
            }
            _tabsBuilt = true;
        }

        private void OnTabClick(int index)
        {
            ShowTab(index);
        }

        /// <summary>切到第 index 页:仅该页显示并 Show 其 BaseView,页签高亮同步。</summary>
        private void ShowTab(int index)
        {
            if (_pages == null) return;
            if (index < 0 || index >= _pages.Length) index = 0;

            for (int i = 0; i < _pages.Length; i++)
            {
                GameObject page = _pages[i];
                if (page == null) continue;
                bool selected = i == index;
                page.SetActive(selected);
                if (selected)
                {
                    BaseView view = page.GetComponent<BaseView>();
                    if (view != null) view.Show();
                }
            }

            for (int i = 0; i < _tabButtons.Count; i++)
            {
                if (_tabButtons[i] != null) _tabButtons[i].SetSelected(i == index);
            }

            _curTab = index;
        }

        private void HideAllPages()
        {
            if (_pages == null) return;
            foreach (GameObject page in _pages)
            {
                if (page != null) page.SetActive(false);
            }
        }
    }
}
