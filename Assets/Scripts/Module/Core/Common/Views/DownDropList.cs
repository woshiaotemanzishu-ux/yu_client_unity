using System;
using System.Collections.Generic;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 下拉列表弹层(对标老客户端 common/DownDropList.ts):按数据克隆若干 DownDropItem 进 content,点击背景 _bg 关闭。
    /// 由 DownDropBtn 打开。Init(dataList,callback) 建行;选中某行 → callback(index,data) 后自动关闭。
    /// 自包含 UI(无后端)→ 完整可用。
    /// </summary>
    public sealed class DownDropList : DownDropListBind
    {
        private Action<int, string> _callback;
        private readonly List<DownDropItem> _items = new List<DownDropItem>();

        protected override void OnInit()
        {
            if (_tpl_DownDropItem != null) _tpl_DownDropItem.SetActive(false);
            if (_bg != null) { _bg.raycastTarget = true; UIUtil.AddClick(_bg, Close); }
        }

        /// <summary>用数据建行(对标 Init + UpdateItems)。</summary>
        public void Init(IList<string> dataList, Action<int, string> callback)
        {
            _callback = callback;
            BuildItems(dataList);
        }

        private void BuildItems(IList<string> dataList)
        {
            for (int i = 0; i < _items.Count; i++)
                if (_items[i] != null) Destroy(_items[i].gameObject);
            _items.Clear();

            if (_tpl_DownDropItem == null || content == null || dataList == null) return;
            for (int i = 0; i < dataList.Count; i++)
            {
                GameObject go = Instantiate(_tpl_DownDropItem, content);
                go.SetActive(true);
                DownDropItem item = go.GetComponent<DownDropItem>();
                if (item == null) continue;
                item.Init(i, dataList[i], OnItemClick);
                _items.Add(item);
            }
        }

        private void OnItemClick(int index, string data)
        {
            if (_callback != null) _callback(index, data);
            Close();
        }

        private void Close()
        {
            Hide();
        }
    }
}
