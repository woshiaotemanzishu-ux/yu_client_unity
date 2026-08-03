using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Generated.UI.Dress;
using UnityEngine;

namespace Shenxiao.Module.Core.Dress
{
    /// <summary>装扮页：气泡 / 相框 / 头像三个子页，对标老端 DressView。</summary>
    public sealed class DressView : DressViewBind
    {
        public const byte BubbleType = 1;
        public const byte PhotoType = 2;
        public const byte HeadType = 5;

        private static readonly byte[] Types = { BubbleType, PhotoType, HeadType };
        private static readonly string[] Labels = { "气泡", "相框", "头像" };
        private static readonly string[] UpIcons = { "uirwv4_zb_010_02", "uirwv4_zb_010_05", "uirwv4_zb_010_05" };
        private static readonly string[] DownIcons = { "uirwv4_zb_010_01", "uirwv4_zb_010_04", "uirwv4_zb_010_04" };

        private readonly List<DressTab> _tabs = new List<DressTab>();
        private DressSubView _subView;
        private byte _initialType = BubbleType;
        private byte _selectedType = BubbleType;
        private bool _subscribed;

        public byte SelectedType => _selectedType;

        public void SetInitialType(byte type)
        {
            if (Array.IndexOf(Types, type) < 0) type = BubbleType;
            _initialType = type;
            if (IsInitialized && IsShown) SelectType(type, true);
        }

        protected override void OnInit()
        {
            BuildTabs();
            BuildSubView();
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            _ = EnsureReady();
            SelectType(_initialType, true);
        }

        protected override void OnHide()
        {
            Unsubscribe();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
        }

        private async Task EnsureReady()
        {
            await DressConfigs.EnsureLoaded();
            await Common.GoodsModel.EnsureLoaded();
            if (this == null || !gameObject.activeInHierarchy) return;
            SelectType(_selectedType, false);
        }

        private void BuildTabs()
        {
            if (_tpl_DressTab == null || _Scroller1 == null) return;
            Transform parent = _Scroller1.content != null ? _Scroller1.content : _Scroller1.transform;
            for (int i = 0; i < Types.Length; i++)
            {
                GameObject go = Instantiate(_tpl_DressTab, parent);
                go.name = "DressTab_" + Labels[i];
                go.SetActive(false);
                DressTab tab = go.GetComponent<DressTab>();
                if (tab == null) { Destroy(go); continue; }
                byte captured = Types[i];
                tab.Show();
                tab.SetData(Labels[i], UpIcons[i], DownIcons[i], false, () => SelectType(captured, true));
                _tabs.Add(tab);
            }
        }

        private void BuildSubView()
        {
            if (_tpl_DressSubView == null || sub_con == null) return;
            GameObject go = Instantiate(_tpl_DressSubView, sub_con);
            go.name = "DressSubView";
            go.SetActive(false);
            _subView = go.GetComponent<DressSubView>();
            if (_subView == null) { Destroy(go); return; }
            _subView.Configure(_tpl_DressItem, _tpl_DressProItem, _tpl_DressSkillItem);
            _subView.Show();
        }

        private void SelectType(byte type, bool request)
        {
            int index = Array.IndexOf(Types, type);
            if (index < 0) { index = 0; type = Types[0]; }
            _selectedType = type;
            for (int i = 0; i < _tabs.Count; i++) _tabs[i].SetSelected(i == index);
            _subView?.SetType(type);
            if (request) DressController.Instance.RequestInfo(type);
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            DressModel.Instance.Changed += OnDressChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            DressModel.Instance.Changed -= OnDressChanged;
            _subscribed = false;
        }

        private void OnDressChanged(byte type)
        {
            if (type == _selectedType) _subView?.Refresh();
        }
    }
}
