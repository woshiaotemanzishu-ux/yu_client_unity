using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.EquipArmor;
using Shenxiao.Module.Core.Armor;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>不朽圣骸已激活部位与套装的总属性弹层。</summary>
    public sealed class ArmorAttrView : ArmorAttrViewBind
    {
        private readonly List<GameObject> _items = new List<GameObject>();
        private int _loadVersion;

        protected override void OnInit()
        {
            if (_tpl_ArmorAttrItem != null) _tpl_ArmorAttrItem.SetActive(false);
            if (_lb_title != null) _lb_title.text = "圣骸总属性";
            if (_lb_none != null) _lb_none.text = "暂未激活圣骸属性";
            if (_img_bg != null)
            {
                _img_bg.raycastTarget = true;
                UIUtil.AddClick(_img_bg, Hide);
            }
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On(GlobalEvent.EVT_ARMOR_UPDATED, Refresh);
            int token = ++_loadVersion;
            _ = LoadAndRefreshAsync(token);
            Refresh();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ARMOR_UPDATED, Refresh);
            ++_loadVersion;
            ClearItems();
        }

        protected override void OnDispose()
        {
            ++_loadVersion;
            ClearItems();
        }

        private async Task LoadAndRefreshAsync(int token)
        {
            await ArmorConfigs.EnsureLoaded();
            if (token == _loadVersion && IsShown) Refresh();
        }

        private void Refresh()
        {
            ClearItems();
            IReadOnlyList<ArmorConfigs.AttrItem> attrs = ArmorConfigs.IsLoaded
                ? ArmorConfigs.GetAllActiveAttributes()
                : new List<ArmorConfigs.AttrItem>();
            if (_gp_none != null) _gp_none.gameObject.SetActive(attrs.Count == 0);
            if (Content == null || _tpl_ArmorAttrItem == null) return;
            for (int i = 0; i < attrs.Count; i++)
            {
                ArmorConfigs.AttrItem data = attrs[i];
                GameObject go = Instantiate(_tpl_ArmorAttrItem, Content, false);
                go.name = "ArmorTotalAttr_" + data.AttrId;
                ArmorAttrItemBind bind = go.GetComponent<ArmorAttrItemBind>();
                go.SetActive(true);
                if (bind != null)
                {
                    string name = GoodsModel.GetAttrName(data.AttrId);
                    if (bind.attr != null) bind.attr.text = (string.IsNullOrEmpty(name) ? ("属性" + data.AttrId) : name) + ":";
                    if (bind.up != null)
                    {
                        bind.up.text = "+" + GoodsModel.FormatAttrValue(data.AttrId, data.Value);
                        bind.up.color = new Color32(10, 149, 62, 255);
                    }
                    foreach (Graphic graphic in go.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                }
                _items.Add(go);
            }
        }

        private void ClearItems()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                GameObject go = _items[i];
                if (go == null) continue;
                if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
            }
            _items.Clear();
        }
    }
}
