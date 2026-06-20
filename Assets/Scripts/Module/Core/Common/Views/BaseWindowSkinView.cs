using System;
using System.Collections.Generic;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>一个标签页的配置:是否开放 + 内容工厂(把内容视图挂到内容区并返回其 BaseView)+ 可选 选中/未选中 sprite。</summary>
    public sealed class TabSpec
    {
        public bool Enabled = true;
        /// <summary>首次选中该页时调用:把内容视图 reparent / 实例化到给定内容区(_gp_item_con)下,返回其 BaseView(可空=空页)。结果会被缓存。</summary>
        public Func<RectTransform, BaseView> ContentFactory;
        public Sprite UpSprite;
        public Sprite DownSprite;
    }

    /// <summary>
    /// 六标签窗框架视图(对标老端 BaseWindowComponent + 共享皮肤 BaseWindowSkin)。**地基**:各分页大窗
    /// (DailyView/装备/合成/九霄劫魄/宠物/角色…)= 实例化 BaseWindowSkin 预制体 + <see cref="Configure"/> 各自标签,**不再各写窗框**。
    ///
    /// 数据驱动:Configure(specs, defaultIndex) → 克隆 _tpl_TabButtonTwoSkin 进 _list_tab_con(标签条)→ 点击切换
    /// _gp_item_con(内容区)里的内容视图(**懒加载 + 缓存**,照抄老端 ChangeView)。关闭走 _img_return;说明 _img_instruction 降级日志。
    ///
    /// 我管「结构」(克隆标签/切页/懒加载/关闭);标签 sprite、标签条 layout 等「效果」由美术/用户在预制体上配。
    /// </summary>
    public sealed class BaseWindowSkinView : BaseWindowSkinBind
    {
        private readonly List<TabButtonTwoSkin> _tabs = new List<TabButtonTwoSkin>();
        private readonly Dictionary<int, BaseView> _contentCache = new Dictionary<int, BaseView>();
        private IList<TabSpec> _specs;
        private int _current = -1;

        protected override void OnInit()
        {
            if (_tpl_TabButtonTwoSkin != null) _tpl_TabButtonTwoSkin.SetActive(false);
            if (_img_return0 != null)
            {
                _img_return0.raycastTarget = true;
                UIUtil.AddClick(_img_return0, Hide);
            }
            if (_img_instruction != null)
            {
                _img_instruction.raycastTarget = true;
                UIUtil.AddClick(_img_instruction, () => GameLog.Info("Window", "点击[说明] → 待对接 InstructionType"));
            }
            if (_img_red != null) _img_red.gameObject.SetActive(false);
        }

        /// <summary>配置标签页 + 默认页索引。在窗口 Show() 之后调用(此时 OnInit 已跑)。</summary>
        public void Configure(IList<TabSpec> specs, int defaultIndex = 0)
        {
            _specs = specs;
            BuildTabs();
            SelectTab(ClampDefault(defaultIndex));
        }

        /// <summary>设置某标签红点(由各窗的红点逻辑调用)。</summary>
        public void SetTabRed(int index, bool on)
        {
            if (index >= 0 && index < _tabs.Count && _tabs[index] != null) _tabs[index].SetRed(on);
        }

        public int CurrentIndex => _current;

        private int ClampDefault(int idx)
        {
            if (_specs == null || _specs.Count == 0) return -1;
            if (idx >= 0 && idx < _specs.Count && _specs[idx].Enabled) return idx;
            for (int i = 0; i < _specs.Count; i++) if (_specs[i].Enabled) return i;
            return -1;
        }

        private RectTransform TabParent()
        {
            if (_list_tab_con == null) return null;
            return _list_tab_con.content != null ? _list_tab_con.content : (RectTransform)_list_tab_con.transform;
        }

        private void BuildTabs()
        {
            foreach (TabButtonTwoSkin t in _tabs) if (t != null) Destroy(t.gameObject);
            _tabs.Clear();

            if (_specs == null || _tpl_TabButtonTwoSkin == null) return;
            RectTransform parent = TabParent();
            if (parent == null) return;

            for (int i = 0; i < _specs.Count; i++)
            {
                GameObject go = Instantiate(_tpl_TabButtonTwoSkin, parent);
                go.SetActive(true);
                TabButtonTwoSkin tab = go.GetComponent<TabButtonTwoSkin>();
                if (tab == null) { Destroy(go); continue; }
                tab.SetEnabledVisual(_specs[i].Enabled);
                tab.SetData(i, SelectTab, _specs[i].UpSprite, _specs[i].DownSprite);
                _tabs.Add(tab);
            }
        }

        public void SelectTab(int index)
        {
            if (_specs == null || index < 0 || index >= _specs.Count) return;
            if (!_specs[index].Enabled)
            {
                GameLog.Info("Window", "标签[{0}]未开放(内容未移植/未达条件)", index);
                return;
            }

            if (!_contentCache.TryGetValue(index, out BaseView content))
            {
                content = _specs[index].ContentFactory != null ? _specs[index].ContentFactory.Invoke(_gp_item_con) : null;
                _contentCache[index] = content;
            }

            foreach (KeyValuePair<int, BaseView> kv in _contentCache)
            {
                if (kv.Value == null) continue;
                if (kv.Key == index) kv.Value.Show(); else kv.Value.Hide();
            }

            for (int i = 0; i < _tabs.Count; i++) if (_tabs[i] != null) _tabs[i].SetSelected(i == index);
            _current = index;
        }
    }
}
