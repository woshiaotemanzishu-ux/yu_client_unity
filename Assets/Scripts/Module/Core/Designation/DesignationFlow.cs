using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Dsgt;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Designation
{
    /// <summary>人物页称号入口：复用 BaseWindowSkin + DsgtModule。</summary>
    public static class DesignationFlow
    {
        private static readonly List<GameObject> RuntimeItems = new List<GameObject>();
        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static BaseWindowSkinView _window;
        private static DsgtViewBind _view;
        private static DsgtItemRendererBind _itemTemplate;
        private static DsgtDetailsItemBind _detailsTemplate;
        private static DsgtDetailsItemBind _details;
        private static BaseAwardItem _costItem;
        private static bool _loading;
        private static bool _subscribed;
        private static uint _selectedId;
        private static int _detailIconRequestId;

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            if (_window != null) _window.Hide();
        }

        private static async Task OpenAsync()
        {
            await Task.WhenAll(DesignationConfigs.EnsureLoaded(), GoodsModel.EnsureLoaded());
            if (_window != null)
            {
                _window.Show();
                _window.SelectTab(0);
                Render();
                DesignationController.Instance.RequestStartup();
                return;
            }
            if (_loading) return;
            _loading = true;
            try
            {
                string frameKey = GameResPath.GetUIPrefab("common", "BaseWindowSkin");
                string contentKey = GameResPath.GetUIPrefab("dsgt", "DsgtModule");
                _frameRoot = await ResManager.InstantiateAsync(
                    frameKey, ViewManager.GetLayer(UILayer.Window));
                _contentRoot = await ResManager.InstantiateAsync(
                    contentKey, ViewManager.GetLayer(UILayer.Window));
                if (_frameRoot == null || _contentRoot == null)
                {
                    GameLog.Error("Designation", "称号窗口加载失败 frame={0} content={1}",
                        frameKey, contentKey);
                    return;
                }

                _frameRoot.name = "BaseWindowSkin(Designation)";
                _contentRoot.name = "DsgtModule";
                _view = _contentRoot.GetComponentInChildren<DsgtViewBind>(true);
                _itemTemplate = _contentRoot.GetComponentInChildren<DsgtItemRendererBind>(true);
                _detailsTemplate = _contentRoot.GetComponentInChildren<DsgtDetailsItemBind>(true);
                if (_view == null || _itemTemplate == null || _detailsTemplate == null)
                {
                    GameLog.Error("Designation", "DsgtModule 缺称号视图或列表/详情模板");
                    return;
                }
                _itemTemplate.gameObject.SetActive(false);
                _detailsTemplate.gameObject.SetActive(false);
                foreach (Transform child in _contentRoot.transform)
                    child.gameObject.SetActive(false);

                _window = _frameRoot.GetComponent<BaseWindowSkinView>()
                    ?? _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
                if (_window == null)
                {
                    GameLog.Error("Designation", "BaseWindowSkin 缺 BaseWindowSkinView");
                    return;
                }

                Subscribe();
                var specs = new[]
                {
                    new TabSpec
                    {
                        Enabled = true,
                        Label = "称号",
                        TitleImagePath = GameResPath.GetIcon("role", "title_name"),
                        BackgroundImagePath = GameResPath.GetBigBgPath("ui_role_bg5.jpg"),
                        ContentFactory = ReparentView,
                    },
                };
                _window.Show();
                _window.Configure(specs, 0);
                Render();
                DesignationController.Instance.RequestStartup();
            }
            finally
            {
                _loading = false;
            }
        }

        private static BaseView ReparentView(RectTransform parent)
        {
            _view.transform.SetParent(parent, false);
            _view.gameObject.SetActive(true);
            return _view;
        }

        private static void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_DESIGNATION_LIST_UPDATE, OnListUpdated);
            EventDispatcher.On(GlobalEvent.EVT_DESIGNATION_ACTIVATION_RESULT, OnAuxiliaryUpdated);
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, OnAuxiliaryUpdated);
            _subscribed = true;
        }

        private static void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_DESIGNATION_LIST_UPDATE, OnListUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_DESIGNATION_ACTIVATION_RESULT, OnAuxiliaryUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, OnAuxiliaryUpdated);
            _subscribed = false;
        }

        private static void OnListUpdated()
        {
            if (_window != null && _window.IsShown) Render();
        }

        private static void OnAuxiliaryUpdated()
        {
            if (_window != null && _window.IsShown) Render();
        }

        private static void Render()
        {
            if (_view == null || _itemTemplate == null) return;
            foreach (GameObject go in RuntimeItems)
                if (go != null) UnityEngine.Object.Destroy(go);
            RuntimeItems.Clear();

            RectTransform content = _view.Content
                ?? (_view.dsgt_scoller != null ? _view.dsgt_scoller.content : null);
            if (content == null) return;

            var active = new Dictionary<uint, DesignationModel.Entry>();
            foreach (DesignationModel.Entry entry in DesignationModel.Instance.Entries)
                active[entry.Id] = entry;
            if (_selectedId == 0)
                _selectedId = DesignationModel.Instance.CurrentUsedId;
            if (_selectedId == 0 && DesignationConfigs.All.Count > 0)
                _selectedId = DesignationConfigs.All[0].Id;

            const float height = 154f;
            for (int i = 0; i < DesignationConfigs.All.Count; i++)
            {
                DesignationConfigs.Row row = DesignationConfigs.All[i];
                GameObject go = UnityEngine.Object.Instantiate(
                    _itemTemplate.gameObject, content, false);
                RuntimeItems.Add(go);
                go.SetActive(true);
                DsgtItemRendererBind item = go.GetComponent<DsgtItemRendererBind>();
                if (item == null) continue;
                item.Show();

                active.TryGetValue(row.Id, out DesignationModel.Entry entry);
                bool selected = row.Id == _selectedId;
                bool adorned = row.Id == DesignationModel.Instance.CurrentUsedId;
                if (item._lb_title != null) item._lb_title.text = row.Name;
                if (item.dsgt_status_label != null)
                    item.dsgt_status_label.text = adorned ? "佩戴中" : (entry != null ? "已激活" : "未激活");
                if (item.dsgt_adorning_image != null)
                    item.dsgt_adorning_image.gameObject.SetActive(adorned);
                if (item.select != null) item.select.gameObject.SetActive(selected);
                if (item.dsgt_red_image != null) item.dsgt_red_image.gameObject.SetActive(false);
                if (item.resource_image != null)
                {
                    item.resource_image.gameObject.SetActive(false);
                    _ = ApplyIconAsync(item.resource_image, row.ResourceId);
                }
                SetAttrs(row.Attrs, item.attr1, item.attr2, item.attr3, item.attr4);

                RectTransform rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(0f, -i * height);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                BindClick(item.bg_dsgtitem_png, () => Select(row.Id));
            }

            content.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Max(1f, DesignationConfigs.All.Count * height));
            RenderDetails(DesignationConfigs.Get(_selectedId), active);
        }

        private static void Select(uint id)
        {
            _selectedId = id;
            Render();
        }

        private static void RenderDetails(
            DesignationConfigs.Row row,
            Dictionary<uint, DesignationModel.Entry> active)
        {
            if (row == null || _view.dsgt_details_group == null) return;
            if (_details == null)
            {
                GameObject go = UnityEngine.Object.Instantiate(
                    _detailsTemplate.gameObject, _view.dsgt_details_group, false);
                go.name = "DsgtDetailsItem_Runtime";
                go.SetActive(true);
                _details = go.GetComponent<DsgtDetailsItemBind>();
                if (_details != null) _details.Show();
            }
            if (_details == null) return;

            active.TryGetValue(row.Id, out DesignationModel.Entry entry);
            bool adorned = row.Id == DesignationModel.Instance.CurrentUsedId;
            if (_details._lb_title != null) _details._lb_title.text = row.Name;
            if (_details.dsgt_order_label != null)
                _details.dsgt_order_label.text = entry != null ? entry.Order + "阶" : "未激活";
            if (_details.dsgt_description_label != null)
                _details.dsgt_description_label.text = row.Description;
            if (_details.labelDisplay != null)
                _details.labelDisplay.text = adorned ? "佩戴中" : "已激活";
            if (_details.dsgt_unactivate_image != null)
                _details.dsgt_unactivate_image.gameObject.SetActive(entry == null);
            if (_details.dsgt_adorn_button != null)
                _details.dsgt_adorn_button.gameObject.SetActive(false);
            RenderActivation(row, entry);
            if (_details.dsgt_icon_image != null)
            {
                int requestId = ++_detailIconRequestId;
                _details.dsgt_icon_image.sprite = null;
                _details.dsgt_icon_image.enabled = false;
                _details.dsgt_icon_image.gameObject.SetActive(false);
                _ = ApplyDetailIconAsync(_details.dsgt_icon_image, row.ResourceId, requestId);
            }
            if (_details.dsgt_full_order_image != null)
                _details.dsgt_full_order_image.gameObject.SetActive(
                    entry != null && row.OrderLimit > 0 && entry.Order >= row.OrderLimit);
            SetAttrs(row.Attrs, _details.attr1, _details.attr2, _details.attr3, _details.attr4);
        }

        private static void RenderActivation(DesignationConfigs.Row row, DesignationModel.Entry entry)
        {
            bool hasCost = DesignationConfigs.TryGetActivationCost(row.Id, out DesignationConfigs.Cost cost);
            bool show = DesignationModel.Instance.HasData && entry == null && hasCost
                && !DesignationController.Instance.IsAwaitingActivationRefresh(row.Id);
            if (_details.dsgt_Activate_button != null)
            {
                _details.dsgt_Activate_button.gameObject.SetActive(show);
                if (show) BindActivationClick(_details.dsgt_Activate_button, row.Id);
            }
            if (_details.dsgt_expend_label != null)
            {
                _details.dsgt_expend_label.gameObject.SetActive(show);
                if (show) _details.dsgt_expend_label.text = "激活消耗：";
            }
            if (_details.dsgt_awarditem_group != null)
                _details.dsgt_awarditem_group.gameObject.SetActive(show);
            if (_details.dsgt_number_label != null)
                _details.dsgt_number_label.gameObject.SetActive(show);

            if (!show)
            {
                if (_details.dsgt_red_image != null) _details.dsgt_red_image.gameObject.SetActive(false);
                if (_costItem != null) _costItem.gameObject.SetActive(false);
                return;
            }

            long own = BagModel.Instance.GetTypeGoodsNum(cost.TypeId);
            bool enough = BagModel.Instance.HasData && own >= cost.Num;
            if (_details.dsgt_number_label != null)
            {
                _details.dsgt_number_label.text = own + "/" + cost.Num;
                _details.dsgt_number_label.color = enough
                    ? new Color32(70, 145, 58, 255)
                    : new Color32(196, 55, 45, 255);
            }
            if (_details.dsgt_red_image != null)
                _details.dsgt_red_image.gameObject.SetActive(enough);
            if (_details.labelDisplay1 != null) _details.labelDisplay1.text = "激活";

            if (_costItem == null && _details._tpl_BaseAwardItem != null
                && _details.dsgt_awarditem_group != null)
            {
                GameObject go = UnityEngine.Object.Instantiate(
                    _details._tpl_BaseAwardItem, _details.dsgt_awarditem_group, false);
                go.name = "BaseAwardItem_ActivationCost";
                go.SetActive(true);
                _costItem = go.GetComponent<BaseAwardItem>();
            }
            if (_costItem != null)
            {
                _costItem.gameObject.SetActive(true);
                _costItem.SetScale(0.7f);
                _costItem.SetData(cost.TypeId, cost.Num);
            }
        }

        private static void BindActivationClick(RectTransform container, uint designationId)
        {
            if (container == null) return;
            foreach (Graphic graphic in container.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            Image surface = container.GetComponent<Image>();
            if (surface == null) surface = container.gameObject.AddComponent<Image>();
            surface.color = new Color(1f, 1f, 1f, 0f);
            surface.raycastTarget = true;
            UIUtil.ClearClicks(surface);
            UIUtil.AddClick(surface, () => DesignationController.Instance.TryActivateByGoods(designationId));
        }

        private static async Task ApplyIconAsync(Image image, string resourceId)
        {
            if (image == null || string.IsNullOrWhiteSpace(resourceId)) return;
            image.sprite = null;
            image.enabled = false;
            image.gameObject.SetActive(false);
            string path = GameResPath.GetDesignImage(resourceId);
            if (!await ResManager.KeyExistsAsync<Sprite>(path) || image == null) return;
            bool loaded = await ResManager.SetImageAsync(image, path, nativeSize: false);
            if (image == null) return;
            image.raycastTarget = false;
            image.gameObject.SetActive(loaded);
        }

        private static async Task ApplyDetailIconAsync(Image image, string resourceId, int requestId)
        {
            if (image == null || string.IsNullOrWhiteSpace(resourceId)) return;
            string path = GameResPath.GetDesignImage(resourceId);
            if (!await ResManager.KeyExistsAsync<Sprite>(path)) return;
            Sprite sprite = await ResManager.LoadAsync<Sprite>(path);
            if (image == null || requestId != _detailIconRequestId) return;
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.raycastTarget = false;
            image.gameObject.SetActive(sprite != null);
        }

        private static void SetAttrs(
            IReadOnlyList<DesignationConfigs.Attr> attrs,
            TMPro.TextMeshProUGUI a,
            TMPro.TextMeshProUGUI b,
            TMPro.TextMeshProUGUI c,
            TMPro.TextMeshProUGUI d)
        {
            TMPro.TextMeshProUGUI[] labels = { a, b, c, d };
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == null) continue;
                if (attrs == null || i >= attrs.Count)
                {
                    labels[i].gameObject.SetActive(false);
                    continue;
                }
                labels[i].gameObject.SetActive(true);
                DesignationConfigs.Attr attr = attrs[i];
                labels[i].text = GoodsModel.GetAttrName(attr.Id) + "+"
                    + GoodsModel.FormatAttrValue(attr.Id, attr.Value);
            }
        }

        private static void BindClick(Image image, Action action)
        {
            if (image == null || action == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
        }

        internal static void Reset()
        {
            Unsubscribe();
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_contentRoot != null) ResManager.ReleaseInstance(_contentRoot);
            RuntimeItems.Clear();
            _frameRoot = null;
            _contentRoot = null;
            _window = null;
            _view = null;
            _itemTemplate = null;
            _detailsTemplate = null;
            _details = null;
            _costItem = null;
            _loading = false;
            _selectedId = 0;
            _detailIconRequestId++;
        }
    }
}
