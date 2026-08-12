using System;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>QuickBuy 获取途径的序列化条目；视觉节点固定来自 CommonModule Prefab。</summary>
    public sealed class QuickBuySourceItem : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI label;
        private Action _onClick;

        public int OpenFunId { get; private set; }

        public void SetData(int openFunId, string displayName, Action onClick)
        {
            OpenFunId = openFunId;
            _onClick = onClick;
            if (label != null) label.text = displayName ?? string.Empty;
            if (icon == null) return;
            icon.raycastTarget = true;
            UIUtil.ClearClicks(icon);
            UIUtil.AddClick(icon, () => _onClick?.Invoke());
            // 老端 OpenFun.OpenIcon: 140..145 共用 icon/22。
            _ = ResManager.SetImageAsync(icon, GameResPath.GetIcon("icon", "22"), false, false);
        }

        private void OnDestroy()
        {
            if (icon != null) UIUtil.ClearClicks(icon);
            _onClick = null;
        }
    }
}
