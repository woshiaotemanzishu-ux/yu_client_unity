using System;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 下拉列表项(对标老客户端 common/DownDropItem.ts):下拉框里的一行,显示文本,点击回选。
    /// 由 DownDropList 克隆。Init(index,data,callback) 填文本+回调;点击 _group → callback(index,data)。
    /// 自包含 UI(无后端)→ 完整可用。
    /// </summary>
    public sealed class DownDropItem : DownDropItemBind
    {
        private Action<int, string> _callback;
        private int _index;
        private string _data;

        protected override void OnInit()
        {
            BindClick(OnClick);
        }

        /// <summary>填行数据(对标 Init)。</summary>
        public void Init(int index, string data, Action<int, string> callback)
        {
            _index = index;
            _data = data;
            _callback = callback;
            if (_lb_content != null) _lb_content.text = data ?? "";
        }

        private void OnClick()
        {
            if (_callback != null) _callback(_index, _data);
        }

        private void BindClick(Action onClick)
        {
            Image img = _group != null ? _group.GetComponentInChildren<Image>(true) : null;
            if (img == null) img = _img_bg;
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
