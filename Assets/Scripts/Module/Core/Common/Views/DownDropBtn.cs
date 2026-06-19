using System;
using System.Collections.Generic;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 下拉框按钮(对标老客户端 common/DownDropBtn.ts):显示当前选中文本 + 箭头,点击展开 DownDropList 选择。
    ///
    /// Init(dataList,callback,defaultContent) 配置选项+回调;点击 _group → 从 _tpl_DownDropList 克隆下拉弹层;
    /// 选中 → 刷新 _lb_content + 回调 + 关闭。SetSelectIndex(i) 程序选中。自包含 UI(无后端)→ 完整可用。
    /// </summary>
    public sealed class DownDropBtn : DownDropBtnBind
    {
        private IList<string> _dataList;
        private Action<int, string> _callback;
        private string _defaultContent;
        private DownDropList _list;

        protected override void OnInit()
        {
            if (_tpl_DownDropItem != null) _tpl_DownDropItem.SetActive(false);
            if (_tpl_DownDropList != null) _tpl_DownDropList.SetActive(false);
            BindClick(_group, Toggle);
        }

        /// <summary>配置下拉(对标 Init):选项 + 选中回调 + 默认标题(缺省取首项)。</summary>
        public void Init(IList<string> dataList, Action<int, string> callback, string defaultContent = null)
        {
            _dataList = dataList;
            _callback = callback;
            _defaultContent = defaultContent;
            UpdateContent(defaultContent ?? (dataList != null && dataList.Count > 0 ? dataList[0] : ""));
        }

        /// <summary>程序选中第 index 项(对标 SetSelectIndex)。</summary>
        public void SetSelectIndex(int index)
        {
            if (_dataList == null || index < 0 || index >= _dataList.Count) return;
            UpdateContent(_dataList[index]);
            if (_callback != null) _callback(index, _dataList[index]);
        }

        private void Toggle()
        {
            if (_list != null) { CloseList(); return; }
            OpenList();
        }

        private void OpenList()
        {
            if (_tpl_DownDropList == null) return;
            GameObject go = Instantiate(_tpl_DownDropList, transform);
            go.SetActive(true);
            _list = go.GetComponent<DownDropList>();
            if (_list != null) _list.Init(_dataList, OnSelect);
        }

        private void OnSelect(int index, string data)
        {
            UpdateContent(data);
            CloseList();
            if (_callback != null) _callback(index, data);
        }

        private void CloseList()
        {
            if (_list != null) { Destroy(_list.gameObject); _list = null; }
        }

        private void UpdateContent(string text)
        {
            if (_lb_content != null) _lb_content.text = text ?? "";
        }

        private void BindClick(Component area, Action onClick)
        {
            if (area == null) return;
            Image img = area.GetComponent<Image>();
            if (img == null) img = area.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
