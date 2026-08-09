using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.ListDuobao;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.CustomActivity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.ListDuobao
{
    public sealed class ListDuobaoRecordView : ListDuobaoRecordViewBind
    {
        private readonly List<GameObject> _lines = new List<GameObject>();
        private bool _eventsBound;
        private float _baseHeight;

        protected override void OnInit()
        {
            _baseHeight = _gp_record != null ? _gp_record.rect.height : 0f;
            BindClick(_img_close, ListDuobaoFlow.ClosePopup);
        }

        protected override void OnShow(object args)
        {
            BindEvents();
            int sub = CustomActivityModel.Instance.ListDuobaoSubType;
            CustomActivityController.Instance.RequestWinLog(ListDuobaoFlow.BaseType, sub);
            RefreshData();
        }

        protected override void OnHide() { UnbindEvents(); ClearLines(); }

        public void RefreshData()
        {
            ClearLines();
            if (_gp_record == null || _lb_title == null) return;
            int sub = CustomActivityModel.Instance.ListDuobaoSubType;
            CustomActivityModel.WinLogData data = CustomActivityModel.Instance.GetWinLog(ListDuobaoFlow.BaseType, sub);
            if (data == null) return;
            int y = 0;
            for (int i = 0; i < data.LogList.Count; i++) AddLine("全服记录  " + Describe(data.LogList[i]), ref y);
            for (int i = 0; i < data.SelfList.Count; i++) AddLine("我的记录  " + Describe(data.SelfList[i]), ref y);
            if (data.LogList.Count == 0 && data.SelfList.Count == 0) AddLine("暂无夺宝记录", ref y);
            _gp_record.sizeDelta = new Vector2(_gp_record.sizeDelta.x, Mathf.Max(_baseHeight, y));
        }

        private static string Describe(CustomActivityModel.WinLogEntry entry)
        {
            string text = string.IsNullOrEmpty(entry.Name) ? "玩家" : entry.Name;
            if (entry.RewardList.Count > 0)
            {
                int goodsId = entry.RewardList[0].GoodsId;
                string name = GoodsModel.GetGoodsName(goodsId);
                text += " 获得 " + (string.IsNullOrEmpty(name) ? goodsId.ToString() : name) + "×" + entry.RewardList[0].Num;
            }
            return text;
        }

        private void AddLine(string text, ref int y)
        {
            GameObject go = Instantiate(_lb_title.gameObject, _gp_record);
            go.name = "record_line";
            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            if (label != null) label.text = text;
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = new Vector2(0f, -y);
            y += 34;
            _lines.Add(go);
        }

        private void OnDetail(int type, int subType) { if (type == ListDuobaoFlow.BaseType && subType == CustomActivityModel.Instance.ListDuobaoSubType) RefreshData(); }
        private void BindEvents() { if (_eventsBound) return; EventDispatcher.On<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, OnDetail); _eventsBound = true; }
        private void UnbindEvents() { if (!_eventsBound) return; EventDispatcher.Off<int, int>(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, OnDetail); _eventsBound = false; }
        private void ClearLines() { for (int i = 0; i < _lines.Count; i++) if (_lines[i] != null) Destroy(_lines[i]); _lines.Clear(); }
        private static void BindClick(Component target, System.Action action) { if (target == null) return; Graphic g = target as Graphic ?? target.GetComponent<Graphic>(); if (g != null) UIUtil.ClearClicks(g); UIUtil.AddClick(target, action); }
    }
}
