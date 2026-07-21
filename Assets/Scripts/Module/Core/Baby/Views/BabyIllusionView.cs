using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Baby;
using UnityEngine;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>宝宝幻化的已激活列表与穿戴切换；配置、属性、升星和模型预览由后续轮次接入。</summary>
    public sealed class BabyIllusionView : BabyIllusionViewBind
    {
        private readonly List<GameObject> _items = new List<GameObject>();
        private bool _listening;
        private int _selectedBabyId;

        protected override void OnInit()
        {
            UIUtil.AddClick(useGp, OnUseClick);
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            Refresh();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            ClearItems();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            ClearItems();
        }

        private void Subscribe()
        {
            if (_listening) return;
            _listening = true;
            EventDispatcher.On<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
        }

        private void Unsubscribe()
        {
            if (!_listening) return;
            _listening = false;
            EventDispatcher.Off<int>(GlobalEvent.EVT_BABY_UPDATE, OnBabyUpdate);
        }

        private void OnBabyUpdate(int command)
        {
            if (!gameObject.activeInHierarchy) return;
            if (command == Proto.BABY_FIGURE_INFO || command == Proto.BABY_FIGURE_WEAR
                || command == Proto.BABY_FIGURE_STAR_UP)
                Refresh();
        }

        private void Refresh()
        {
            BabyModel model = BabyModel.Instance;
            int wornBabyId = model.Basic != null ? model.Basic.BabyId : 0;
            List<BabyFigureEntry> active = model.Figures != null ? model.Figures.ActiveList : null;
            if (!ContainsActive(active, _selectedBabyId))
                _selectedBabyId = ContainsActive(active, wornBabyId) ? wornBabyId : FirstActiveId(active);

            ClearItems();
            if (_tpl_BabyIlluItem != null && illuGp != null && active != null)
            {
                for (int i = 0; i < active.Count; i++)
                {
                    BabyFigureEntry entry = active[i];
                    if (entry == null || !entry.IsActivated) continue;
                    CreateItem(entry);
                }
            }

            if (babyName != null) babyName.text = _selectedBabyId > 0 ? _selectedBabyId.ToString() : string.Empty;
            if (selectedImg != null) selectedImg.gameObject.SetActive(_selectedBabyId > 0 && wornBabyId == _selectedBabyId);
            if (useGp != null) useGp.gameObject.SetActive(_selectedBabyId > 0);
        }

        private void CreateItem(BabyFigureEntry entry)
        {
            GameObject go = Instantiate(_tpl_BabyIlluItem, illuGp, false);
            go.SetActive(true);
            _items.Add(go);

            BabyIlluItemBind item = go.GetComponent<BabyIlluItemBind>();
            if (item == null) return;
            if (item.stageLb != null)
            {
                item.stageLb.gameObject.SetActive(true);
                item.stageLb.text = entry.BabyStar.ToString();
            }
            if (item.unActive != null) item.unActive.gameObject.SetActive(false);
            if (item.select_img != null) item.select_img.gameObject.SetActive(entry.BabyId == _selectedBabyId);
            int babyId = entry.BabyId;
            UIUtil.AddClick(item.clickGp, () => Select(babyId));
        }

        private void Select(int babyId)
        {
            if (babyId <= 0 || babyId == _selectedBabyId) return;
            _selectedBabyId = babyId;
            Refresh();
        }

        private void OnUseClick()
        {
            if (_selectedBabyId <= 0) return;
            BabyBasicInfo basic = BabyModel.Instance.Basic;
            int type = basic != null && basic.BabyId == _selectedBabyId ? 2 : 1;
            BabyController.Instance.RequestSetFigure(type, _selectedBabyId);
        }

        private void ClearItems()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                GameObject item = _items[i];
                if (item == null) continue;
                if (Application.isPlaying) Destroy(item);
                else DestroyImmediate(item);
            }
            _items.Clear();
        }

        private static bool ContainsActive(List<BabyFigureEntry> entries, int babyId)
        {
            if (entries == null || babyId <= 0) return false;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && entries[i].IsActivated && entries[i].BabyId == babyId) return true;
            return false;
        }

        private static int FirstActiveId(List<BabyFigureEntry> entries)
        {
            if (entries == null) return 0;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && entries[i].IsActivated) return entries[i].BabyId;
            return 0;
        }
    }
}
