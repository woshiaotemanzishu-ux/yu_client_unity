using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.GameNotice;
using Shenxiao.Module.Core.Login;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GameNotice
{
    public enum GameNoticeMode
    {
        Login,
        Inside,
    }

    /// <summary>
    /// 公告真实消费者：登录阶段显示 open_login，游戏内福利入口显示 open_inside；
    /// 左侧标题保留 belong 线序，右侧按“\n\n##”拆段，选择游戏内公告即持久化已读并刷新 417 红点。
    /// </summary>
    public sealed class GameNoticeView : GameNoticeViewBind
    {
        private readonly List<GameObject> _titleItems = new List<GameObject>();
        private readonly List<GameObject> _contentItems = new List<GameObject>();
        private List<LoginNoticeDisplayInfo> _notices = new List<LoginNoticeDisplayInfo>();
        private GameNoticeMode _mode;
        private int _selectedIndex = -1;

        protected override void OnInit()
        {
            if (_tpl_GameNoticeListItem != null) _tpl_GameNoticeListItem.SetActive(false);
            if (_tpl_GameNoticeContentItem != null) _tpl_GameNoticeContentItem.SetActive(false);
            CreateCloseButton();
        }

        protected override void OnShow(object args)
        {
            _mode = args is GameNoticeMode mode ? mode : GameNoticeMode.Inside;
            EventDispatcher.Off(GlobalEvent.EVT_LOGIN_NOTICE_UPDATED, Refresh);
            EventDispatcher.On(GlobalEvent.EVT_LOGIN_NOTICE_UPDATED, Refresh);
            Refresh();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_LOGIN_NOTICE_UPDATED, Refresh);
        }

        protected override void OnDispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_LOGIN_NOTICE_UPDATED, Refresh);
            ClearItems(_titleItems);
            ClearItems(_contentItems);
        }

        private void OnDestroy()
        {
            EventDispatcher.Off(GlobalEvent.EVT_LOGIN_NOTICE_UPDATED, Refresh);
        }

        private void Refresh()
        {
            _notices = _mode == GameNoticeMode.Login
                ? LoginNoticeModel.Instance.GetLoginNotices()
                : LoginNoticeModel.Instance.GetInsideNotices();

            ClearItems(_titleItems);
            ClearItems(_contentItems);
            _selectedIndex = -1;

            if (_notices.Count == 0)
            {
                if (_lab_title != null) _lab_title.text = "公告";
                AddContent("当前没有可展示的公告");
                if (_img_next != null) _img_next.gameObject.SetActive(false);
                return;
            }

            for (int i = 0; i < _notices.Count; i++) AddTitle(i);
            if (_img_next != null) _img_next.gameObject.SetActive(_notices.Count > 6);
            Select(0);
        }

        private void AddTitle(int index)
        {
            if (_tpl_GameNoticeListItem == null || _list_title?.content == null) return;
            GameObject go = Instantiate(_tpl_GameNoticeListItem, _list_title.content);
            go.name = "NoticeTitle_" + index;
            go.SetActive(true);
            _titleItems.Add(go);
            GameNoticeListItem item = go.GetComponent<GameNoticeListItem>();
            if (item == null) return;
            item.Show();
            int captured = index;
            LoginNoticeDisplayInfo data = _notices[index];
            item.SetData(data.Notice.Title, data.IsUnread, () => Select(captured));
            item.SetSelected(index == _selectedIndex);
        }

        private void Select(int index)
        {
            if (index < 0 || index >= _notices.Count) return;
            _selectedIndex = index;
            for (int i = 0; i < _titleItems.Count; i++)
            {
                GameNoticeListItem item = _titleItems[i] != null ? _titleItems[i].GetComponent<GameNoticeListItem>() : null;
                item?.SetSelected(i == index);
            }

            LoginNoticeDisplayInfo selected = _notices[index];
            if (_lab_title != null) _lab_title.text = selected.Notice.Title;
            ClearItems(_contentItems);
            string[] sections = selected.Content.Content.Split(new[] { "\n\n##" }, StringSplitOptions.None);
            for (int i = 0; i < sections.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(sections[i])) AddContent(sections[i]);
            }
            if (_gp_content != null) _gp_content.verticalNormalizedPosition = 1f;

            if (_mode == GameNoticeMode.Inside && selected.IsUnread)
            {
                LoginNoticeModel.Instance.MarkInsideRead(selected.ReadKey);
                selected.IsUnread = false;
                GameNoticeListItem item = index < _titleItems.Count && _titleItems[index] != null
                    ? _titleItems[index].GetComponent<GameNoticeListItem>()
                    : null;
                if (item != null) item.SetData(selected.Notice.Title, false, () => Select(index));
            }
        }

        private void AddContent(string content)
        {
            if (_tpl_GameNoticeContentItem == null || _gp_item == null) return;
            GameObject go = Instantiate(_tpl_GameNoticeContentItem, _gp_item);
            go.name = "NoticeContent_" + _contentItems.Count;
            go.SetActive(true);
            _contentItems.Add(go);
            GameNoticeContentItem item = go.GetComponent<GameNoticeContentItem>();
            if (item == null) return;
            item.Show();
            item.SetData(content);
        }

        private void CreateCloseButton()
        {
            var go = new GameObject("CloseButton", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-18f, -18f);
            rt.sizeDelta = new Vector2(72f, 72f);
            var image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.55f);
            image.raycastTarget = true;
            UIUtil.AddClick(image, GameNoticeFlow.Close);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.SetParent(rt, false);
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            label.text = "×";
            label.fontSize = 48f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }

        private static void ClearItems(List<GameObject> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null) continue;
                if (Application.isPlaying) Destroy(items[i]);
                else DestroyImmediate(items[i]);
            }
            items.Clear();
        }
    }
}
