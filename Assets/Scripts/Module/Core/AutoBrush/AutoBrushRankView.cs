using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.AutoBrush;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.AutoBrush
{
    /// <summary>
    /// 斩妖排行榜弹窗的数据与交互层。静态结构由 AutoBrushModule.prefab 保存。
    /// </summary>
    public sealed class AutoBrushRankView
    {
        private const float ItemHeight = 46f;

        private readonly AutoBrushRankViewBind _bind;
        private readonly List<GameObject> _items = new List<GameObject>();
        private bool _eventsBound;

        public AutoBrushRankView(AutoBrushRankViewBind bind)
        {
            _bind = bind;
            if (_bind?._tpl_AutoBrushRankItem != null) _bind._tpl_AutoBrushRankItem.SetActive(false);
        }

        public bool IsShown => _bind != null && _bind.gameObject.activeSelf;

        public void Show()
        {
            if (_bind == null) return;
            _bind.Show();
            _bind.transform.SetAsLastSibling();
            BindEvents();
            BindClick(_bind._img_close, Hide);
            Render();
            AutoBrushController.Instance.RequestRankInfo();
        }

        public void Hide()
        {
            UnbindEvents();
            ClearItems();
            if (_bind != null) _bind.Hide();
        }

        private void BindEvents()
        {
            if (_eventsBound) return;
            EventDispatcher.On(GlobalEvent.EVT_AUTOBRUSH_LEVEL_UPDATED, Refresh);
            _eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!_eventsBound) return;
            EventDispatcher.Off(GlobalEvent.EVT_AUTOBRUSH_LEVEL_UPDATED, Refresh);
            _eventsBound = false;
        }

        private void Refresh()
        {
            if (IsShown) Render();
        }

        private void Render()
        {
            ClearItems();
            if (_bind == null) return;

            AutoBrushModel model = AutoBrushModel.Instance;
            IReadOnlyList<AutoBrushModel.RankEntry> entries = model.RankEntries;
            bool hasEntries = entries != null && entries.Count > 0;

            if (_bind._lb_none != null) _bind._lb_none.gameObject.SetActive(!hasEntries);
            if (_bind._list_item != null) _bind._list_item.gameObject.SetActive(hasEntries);
            if (_bind._html_my_rank != null)
                _bind._html_my_rank.text = "我的排名:" + (model.RoleRank > 0 ? "第" + model.RoleRank + "名" : "未上榜");
            if (_bind._html_my_level != null)
                _bind._html_my_level.text = "我的关数:" + (model.Level > 0 ? model.Level + "关" : "暂未通关");

            if (!hasEntries || _bind._list_item == null || _bind._tpl_AutoBrushRankItem == null) return;
            Transform content = _bind._list_item.content != null
                ? _bind._list_item.content
                : _bind._list_item.transform;
            if (content is RectTransform contentRect)
            {
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, entries.Count * ItemHeight);
            }

            for (int i = 0; i < entries.Count; i++)
            {
                GameObject itemObject = Object.Instantiate(_bind._tpl_AutoBrushRankItem, content);
                itemObject.SetActive(true);
                AutoBrushRankItemBind item = itemObject.GetComponent<AutoBrushRankItemBind>();
                if (item == null)
                {
                    Object.Destroy(itemObject);
                    continue;
                }

                item.Show();
                RectTransform rect = (RectTransform)itemObject.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(0f, -i * ItemHeight);
                rect.sizeDelta = new Vector2(552f, ItemHeight);
                RenderItem(item, entries[i], model.RankType);
                _items.Add(itemObject);
            }

            _bind._list_item.StopMovement();
            _bind._list_item.verticalNormalizedPosition = 1f;
        }

        private static void RenderItem(AutoBrushRankItemBind item, AutoBrushModel.RankEntry entry, int rankType)
        {
            bool topThree = entry.Rank >= 1 && entry.Rank <= 3;
            if (item._img_rank != null)
            {
                item._img_rank.gameObject.SetActive(topThree);
                if (topThree)
                    _ = ResManager.SetImageAsync(item._img_rank,
                        GameResPath.GetIcon("autoBrush", "rank_icon_" + entry.Rank), false, false);
            }
            if (item._lb_rank != null)
            {
                item._lb_rank.gameObject.SetActive(!topThree);
                item._lb_rank.text = entry.Rank.ToString();
            }
            if (item._img_bg != null)
                _ = ResManager.SetImageAsync(item._img_bg,
                    GameResPath.GetIcon("autoBrush", topThree
                        ? "ui_activity_" + (7 + entry.Rank).ToString("00")
                        : "ui_activity_11"),
                    false, false);

            if (item._lb_name != null)
                item._lb_name.text = rankType == 1
                    ? "S" + entry.ServerNum + "." + entry.RoleName
                    : entry.RoleName;
            if (item._lb_level != null) item._lb_level.text = entry.Level.ToString();
            if (item._lb_combat != null) item._lb_combat.text = entry.Combat.ToString();
        }

        private static void BindClick(Component target, System.Action action)
        {
            if (target == null) return;
            Graphic graphic = target as Graphic ?? target.GetComponent<Graphic>()
                ?? target.GetComponentInChildren<Graphic>(true);
            if (graphic != null) UIUtil.ClearClicks(graphic);
            UIUtil.AddClick(target, action);
        }

        private void ClearItems()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] == null) continue;
                _items[i].SetActive(false);
                Object.Destroy(_items[i]);
            }
            _items.Clear();
        }
    }
}
