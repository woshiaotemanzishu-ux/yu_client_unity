using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Generated.UI.MainStronger;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainStronger
{
    /// <summary>我要变强推荐列表；布局与视觉完全由转换后的 Prefab 持有。</summary>
    public sealed class MainUIStrongerView : MainUIStrongerViewBind
    {
        private readonly List<GameObject> _rows = new List<GameObject>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_MainUIStrongerBtn != null) _tpl_MainUIStrongerBtn.SetActive(false);
            BindBtn(btnClose, MainStrongerFlow.Close);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            _ = RefreshAsync();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            ClearRows();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            ClearRows();
        }

        private async Task RefreshAsync()
        {
            await MainStrongerConfigs.EnsureLoaded();
            if (!IsShown) return;
            MainStrongerModel.Instance.Rebuild();
            Render();
        }

        private void Render()
        {
            ClearRows();
            // 广告依赖 config_banner 与 Activity 状态矩阵；未接齐前整组隐藏。
            if (_panel_ad != null) _panel_ad.gameObject.SetActive(false);
            if (_panel_toggle != null) _panel_toggle.gameObject.SetActive(false);
            if (listStrongBtn == null || listStrongBtn.content == null ||
                _tpl_MainUIStrongerBtn == null) return;

            IReadOnlyList<MainStrongerConfigs.Feature> features =
                MainStrongerModel.Instance.Recommendations;
            for (int i = 0; i < features.Count; i++)
            {
                GameObject row = Instantiate(_tpl_MainUIStrongerBtn,
                    listStrongBtn.content, false);
                row.name = "MainUIStrongerBtn(Runtime:" + features[i].Id + ")";
                MainUIStrongerBtn item = row.GetComponent<MainUIStrongerBtn>();
                if (item == null)
                {
                    Destroy(row);
                    continue;
                }
                _rows.Add(row);
                row.SetActive(true);
                item.Show();
                item.SetData(features[i]);
            }

            listStrongBtn.StopMovement();
            listStrongBtn.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(listStrongBtn.content);
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            MainStrongerModel.Instance.Changed += Render;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            MainStrongerModel.Instance.Changed -= Render;
            _subscribed = false;
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++)
                if (_rows[i] != null) Destroy(_rows[i]);
            _rows.Clear();
        }

        private static void BindBtn(Component target, Action onClick)
        {
            if (target == null) return;
            Image image = target as Image ?? target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.ClearClicks(image);
            UIUtil.AddClick(image, onClick);
        }
    }
}
