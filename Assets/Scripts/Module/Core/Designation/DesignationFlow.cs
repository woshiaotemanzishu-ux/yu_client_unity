using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
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
    /// <summary>人物页称号入口：复用 BaseWindowSkin + RoleModule 内容。</summary>
    public static class DesignationFlow
    {
        private static readonly List<GameObject> RuntimeItems = new List<GameObject>();
        private static readonly List<DsgtItemRendererBind> RuntimeItemViews = new List<DsgtItemRendererBind>();
        private static readonly List<DesignationConfigs.Row> RuntimeRows = new List<DesignationConfigs.Row>();
        private static readonly HashSet<int> RequestedListIcons = new HashSet<int>();
        private static readonly Dictionary<int, UIEffectStage.Handle> ListEffects =
            new Dictionary<int, UIEffectStage.Handle>();
        private static GameObject _frameRoot;
        private static GameObject _contentRoot;
        private static GameObject _templateRoot;
        private static BaseWindowSkinView _window;
        private static DsgtViewBind _view;
        private static DsgtItemRendererBind _itemTemplate;
        private static DsgtDetailsItemBind _detailsTemplate;
        private static DsgtDetailsItemBind _details;
        private static Sprite _itemBackgroundSprite;
        private static BaseAwardItem _costItem;
        private static FightingShowSmallItem _fight;
        private static UIEffectStage.Handle _detailEffect;
        private static bool _loading;
        private static bool _subscribed;
        private static uint _selectedId;
        private static uint _powerRequestedId;
        private static int _detailIconRequestId;

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            if (_window != null) _window.Hide();
            _powerRequestedId = 0;
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
                string contentKey = GameResPath.GetUIPrefab("role", "RoleModule");
                string templateKey = GameResPath.GetUIPrefab("dsgt", "DsgtModule");
                Transform windowLayer = ViewManager.GetLayer(UILayer.Window);
                Task<GameObject> frameTask = ResManager.InstantiateAsync(frameKey, windowLayer);
                Task<GameObject> contentTask = ResManager.InstantiateAsync(contentKey, windowLayer);
                Task<GameObject> templateTask = ResManager.InstantiateAsync(templateKey, windowLayer);
                Task<Sprite> itemBackgroundTask = ResManager.LoadAsync<Sprite>(
                    GameResPath.GetIcon("dsgt", "ui_role_51"));
                await Task.WhenAll(frameTask, contentTask, templateTask, itemBackgroundTask);
                _frameRoot = frameTask.Result;
                _contentRoot = contentTask.Result;
                _templateRoot = templateTask.Result;
                _itemBackgroundSprite = itemBackgroundTask.Result;
                if (_frameRoot == null || _contentRoot == null || _templateRoot == null)
                {
                    GameLog.Error("Designation", "称号窗口加载失败 frame={0} content={1}",
                        frameKey, contentKey);
                    return;
                }

                _frameRoot.name = "BaseWindowSkin(Designation)";
                _contentRoot.name = "RoleModule(Designation)";
                _templateRoot.name = "DsgtModule(DesignationTemplates)";
                _view = _contentRoot.GetComponentInChildren<DsgtViewBind>(true);
                _itemTemplate = _templateRoot.GetComponentInChildren<DsgtItemRendererBind>(true);
                _detailsTemplate = _templateRoot.GetComponentInChildren<DsgtDetailsItemBind>(true);
                if (_view == null || _itemTemplate == null || _detailsTemplate == null)
                {
                    GameLog.Error("Designation", "RoleModule 缺称号视图或列表/详情模板");
                    return;
                }
                _itemTemplate.gameObject.SetActive(false);
                _detailsTemplate.gameObject.SetActive(false);
                _templateRoot.SetActive(false);
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
                if (_view.dsgt_scoller != null)
                {
                    _view.dsgt_scoller.onValueChanged.RemoveListener(OnScrolled);
                    _view.dsgt_scoller.onValueChanged.AddListener(OnScrolled);
                }
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
            EventDispatcher.On(GlobalEvent.EVT_DESIGNATION_UPGRADE_RESULT, OnAuxiliaryUpdated);
            EventDispatcher.On(GlobalEvent.EVT_DESIGNATION_WEAR_RESULT, OnAuxiliaryUpdated);
            EventDispatcher.On(GlobalEvent.EVT_DESIGNATION_POWER_RESULT, OnPowerUpdated);
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, OnAuxiliaryUpdated);
            _subscribed = true;
        }

        private static void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_DESIGNATION_LIST_UPDATE, OnListUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_DESIGNATION_ACTIVATION_RESULT, OnAuxiliaryUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_DESIGNATION_UPGRADE_RESULT, OnAuxiliaryUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_DESIGNATION_WEAR_RESULT, OnAuxiliaryUpdated);
            EventDispatcher.Off(GlobalEvent.EVT_DESIGNATION_POWER_RESULT, OnPowerUpdated);
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

        private static void OnPowerUpdated()
        {
            if (_window == null || !_window.IsShown || _fight == null) return;
            DesignationModel.PowerQuerySnapshot power = DesignationModel.Instance.PowerQuery;
            _fight.SetFighting(power != null && power.Code == 1 ? power.Power : 0);
        }

        private static void OnScrolled(Vector2 unused)
        {
            if (_window != null && _window.IsShown) _ = RefreshVisibleIconsAsync();
        }

        private static void Render()
        {
            if (_view == null || _itemTemplate == null) return;
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

            List<DesignationConfigs.Row> orderedRows = BuildOrderedRows(active);
            EnsureListBuilt(content, orderedRows);
            RefreshListStates(active);
            RenderDetails(DesignationConfigs.Get(_selectedId), active);
            PositionSelectedInList();
            RequestSelectedPower();
            _ = RefreshVisibleIconsAsync();
        }

        private static void PositionSelectedInList()
        {
            ScrollRect scroll = _view?.dsgt_scoller;
            RectTransform content = scroll?.content;
            RectTransform viewport = scroll?.viewport;
            if (scroll == null || content == null || viewport == null) return;
            int selectedIndex = RuntimeRows.FindIndex(row => row != null && row.Id == _selectedId);
            if (selectedIndex < 0) return;
            Canvas.ForceUpdateCanvases();
            const float rowHeight = 120f;
            float max = Mathf.Max(0f, content.rect.height - viewport.rect.height);
            float targetY = Mathf.Clamp(Mathf.Max(0, selectedIndex - 2) * rowHeight, 0f, max);
            scroll.StopMovement();
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetY);
            Canvas.ForceUpdateCanvases();
        }

        private static List<DesignationConfigs.Row> BuildOrderedRows(
            Dictionary<uint, DesignationModel.Entry> active)
        {
            var rows = new List<DesignationConfigs.Row>(DesignationConfigs.All);
            rows.Sort((a, b) =>
            {
                bool aa = a != null && active.ContainsKey(a.Id);
                bool ba = b != null && active.ContainsKey(b.Id);
                if (aa != ba) return aa ? -1 : 1;
                int byLocation = (a?.Location ?? int.MaxValue).CompareTo(b?.Location ?? int.MaxValue);
                return byLocation != 0 ? byLocation : (a?.Id ?? 0u).CompareTo(b?.Id ?? 0u);
            });
            return rows;
        }

        private static void EnsureListBuilt(RectTransform content,
            IReadOnlyList<DesignationConfigs.Row> orderedRows)
        {
            bool sameOrder = RuntimeItems.Count == orderedRows.Count
                && RuntimeItemViews.Count == orderedRows.Count
                && RuntimeRows.Count == orderedRows.Count;
            if (sameOrder)
                for (int i = 0; i < orderedRows.Count; i++)
                    if (RuntimeRows[i].Id != orderedRows[i].Id) { sameOrder = false; break; }
            if (sameOrder) return;

            DisposeListEffects();
            foreach (GameObject go in RuntimeItems)
                if (go != null) UnityEngine.Object.Destroy(go);
            RuntimeItems.Clear();
            RuntimeItemViews.Clear();
            RuntimeRows.Clear();
            RequestedListIcons.Clear();

            const float height = 120f;
            for (int i = 0; i < orderedRows.Count; i++)
            {
                DesignationConfigs.Row row = orderedRows[i];
                GameObject go = UnityEngine.Object.Instantiate(
                    _itemTemplate.gameObject, content, false);
                RuntimeItems.Add(go);
                go.SetActive(true);
                DsgtItemRendererBind item = go.GetComponent<DsgtItemRendererBind>();
                if (item == null) continue;
                item.Show();
                foreach (Graphic graphic in item.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = false;
                RuntimeItemViews.Add(item);
                RuntimeRows.Add(row);
                if (item._lb_title != null)
                    item._lb_title.text = row.MainType == 7 ? row.Name : string.Empty;
                if (item.bg_dsgtitem_png != null)
                {
                    item.bg_dsgtitem_png.sprite = _itemBackgroundSprite;
                    item.bg_dsgtitem_png.enabled = _itemBackgroundSprite != null;
                    item.bg_dsgtitem_png.type = Image.Type.Sliced;
                }
                if (item.resource_image != null)
                {
                    item.resource_image.gameObject.SetActive(false);
                    item.resource_image.sprite = null;
                    item.resource_image.enabled = false;
                }
                if (item._gp_dsgt_effect != null)
                    item._gp_dsgt_effect.gameObject.SetActive(false);
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
                Mathf.Max(1f, orderedRows.Count * height));
            Canvas.ForceUpdateCanvases();
        }

        private static void RefreshListStates(Dictionary<uint, DesignationModel.Entry> active)
        {
            for (int i = 0; i < RuntimeItemViews.Count && i < RuntimeRows.Count; i++)
            {
                DsgtItemRendererBind item = RuntimeItemViews[i];
                DesignationConfigs.Row row = RuntimeRows[i];
                if (item == null || row == null) continue;
                active.TryGetValue(row.Id, out DesignationModel.Entry entry);
                bool selected = row.Id == _selectedId;
                bool adorned = row.Id == DesignationModel.Instance.CurrentUsedId;
                if (item.dsgt_status_label != null)
                {
                    item.dsgt_status_label.text = FormatStatus(entry);
                    item.dsgt_status_label.color = entry == null
                        ? new Color32(0x9b, 0x9b, 0x9b, 0xff)
                        : new Color32(0x0a, 0x95, 0x3e, 0xff);
                }
                if (item.dsgt_att_label != null)
                    item.dsgt_att_label.text = entry == null ? "激活加成：" : "属性加成：";
                if (item.dsgt_adorning_image != null)
                    item.dsgt_adorning_image.gameObject.SetActive(adorned);
                if (item.select != null) item.select.gameObject.SetActive(selected);
                if (item.dsgt_red_image != null)
                    item.dsgt_red_image.gameObject.SetActive(CanAffordAction(row, entry));
            }
        }

        private static bool CanAffordAction(DesignationConfigs.Row row, DesignationModel.Entry entry)
        {
            if (!BagModel.Instance.HasData || row == null) return false;
            DesignationConfigs.Cost cost;
            bool actionable = entry == null
                ? DesignationConfigs.TryGetActivationCost(row.Id, out cost)
                : DesignationConfigs.TryGetUpgradeCost(row.Id, entry.Order, out cost);
            return actionable && cost != null && BagModel.Instance.GetTypeGoodsNum(cost.TypeId) >= cost.Num;
        }

        private static void Select(uint id)
        {
            if (_selectedId == id) return;
            _selectedId = id;
            var active = new Dictionary<uint, DesignationModel.Entry>();
            foreach (DesignationModel.Entry entry in DesignationModel.Instance.Entries)
                active[entry.Id] = entry;
            RefreshListStates(active);
            RenderDetails(DesignationConfigs.Get(_selectedId), active);
            RequestSelectedPower();
            _ = RefreshVisibleIconsAsync();
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
            if (_details._lb_title != null)
                _details._lb_title.text = row.MainType == 7 ? row.Name : string.Empty;
            if (_details.dsgt_order_label != null)
                _details.dsgt_order_label.text = entry != null ? entry.Order + "阶" : "未激活";
            if (_details.dsgt_description_label != null)
                _details.dsgt_description_label.text = row.Description;
            if (_details.labelDisplay != null)
                _details.labelDisplay.text = adorned ? "卸下" : "佩戴";
            if (_details.dsgt_unactivate_image != null)
                _details.dsgt_unactivate_image.gameObject.SetActive(entry == null);
            if (_details.dsgt_adorn_button != null)
            {
                bool waiting = DesignationController.Instance.HasPendingWear
                    || DesignationController.Instance.IsAwaitingWearRefresh(row.Id);
                bool showAdorn = entry != null && DesignationModel.Instance.HasData && !waiting;
                _details.dsgt_adorn_button.gameObject.SetActive(showAdorn);
                if (showAdorn)
                    BindActionClick(_details.dsgt_adorn_button,
                        () => DesignationController.Instance.TryToggleWear(row.Id));
            }
            RenderActivation(row, entry);
            EnsureFightItem();
            if (_details.dsgt_icon_image != null)
            {
                int requestId = ++_detailIconRequestId;
                _detailEffect?.Dispose();
                _detailEffect = null;
                _details.dsgt_icon_image.sprite = null;
                _details.dsgt_icon_image.enabled = false;
                _details.dsgt_icon_image.gameObject.SetActive(false);
                if (_details._gp_effect_icon != null)
                    _details._gp_effect_icon.gameObject.SetActive(false);
                _ = ApplyDetailVisualAsync(row, requestId);
            }
            if (_details.dsgt_full_order_image != null)
                _details.dsgt_full_order_image.gameObject.SetActive(
                    entry != null && row.OrderLimit > 0 && entry.Order >= row.OrderLimit);
            SetAttrs(entry != null ? DesignationConfigs.GetDisplayAttrs(row.Id, entry.Order) : row.Attrs,
                _details.attr1, _details.attr2, _details.attr3, _details.attr4);
        }

        private static void RenderActivation(DesignationConfigs.Row row, DesignationModel.Entry entry)
        {
            DesignationConfigs.Cost cost = null;
            bool upgrading = entry != null
                && DesignationConfigs.TryGetUpgradeCost(row.Id, entry.Order, out cost);
            bool activating = false;
            if (!upgrading && entry == null)
                activating = DesignationConfigs.TryGetActivationCost(row.Id, out cost);
            bool waiting = entry == null
                ? DesignationController.Instance.IsAwaitingActivationRefresh(row.Id)
                : DesignationController.Instance.IsAwaitingUpgradeRefresh(row.Id);
            bool show = DesignationModel.Instance.HasData && (activating || upgrading) && !waiting;
            if (_details.dsgt_Activate_button != null)
            {
                _details.dsgt_Activate_button.gameObject.SetActive(show);
                if (show)
                {
                    if (upgrading) BindUpgradeClick(_details.dsgt_Activate_button, row.Id);
                    else BindActivationClick(_details.dsgt_Activate_button, row.Id);
                }
            }
            if (_details.dsgt_expend_label != null)
            {
                _details.dsgt_expend_label.gameObject.SetActive(show);
                if (show) _details.dsgt_expend_label.text = upgrading ? "升阶消耗：" : "激活消耗：";
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
            if (_details.labelDisplay1 != null) _details.labelDisplay1.text = upgrading ? "升阶" : "激活";

            if (_costItem == null && _details._tpl_BaseAwardItem != null
                && _details.dsgt_awarditem_group != null)
            {
                GameObject go = UnityEngine.Object.Instantiate(
                    _details._tpl_BaseAwardItem, _details.dsgt_awarditem_group, false);
                go.name = "BaseAwardItem_DesignationCost";
                go.SetActive(true);
                _costItem = go.GetComponent<BaseAwardItem>();
            }
            if (_costItem != null)
            {
                _costItem.gameObject.SetActive(true);
                _costItem.SetScale(0.7f);
                _costItem.SetData(cost.TypeId, cost.Num);
                int costTypeId = cost.TypeId;
                _costItem.SetClickCallBack(() => IllusionTipsFlow.Show(costTypeId));
            }
        }

        private static void EnsureFightItem()
        {
            if (_fight != null || _details?._tpl_FightingShowSmallItem == null || _details._gp_fight == null)
                return;
            GameObject go = UnityEngine.Object.Instantiate(
                _details._tpl_FightingShowSmallItem, _details._gp_fight, false);
            go.name = "FightingShowSmallItem_Designation";
            go.SetActive(true);
            _fight = go.GetComponent<FightingShowSmallItem>();
            _fight?.Show();
            _fight?.SetFighting(0);
            _fight?.SetFightingUp(0);
        }

        private static void RequestSelectedPower()
        {
            if (_selectedId == 0 || _selectedId == _powerRequestedId) return;
            _powerRequestedId = _selectedId;
            _fight?.SetFighting(0);
            DesignationController.Instance.RequestPower(_selectedId);
        }

        private static async Task RefreshVisibleIconsAsync()
        {
            await Task.Yield();
            if (_view?.dsgt_scoller == null || _view.dsgt_scoller.viewport == null) return;
            Canvas.ForceUpdateCanvases();
            RectTransform viewport = _view.dsgt_scoller.viewport;
            Rect visible = viewport.rect;
            const float buffer = 180f;
            for (int i = 0; i < RuntimeItemViews.Count && i < RuntimeRows.Count; i++)
            {
                DsgtItemRendererBind item = RuntimeItemViews[i];
                Image image = item?.resource_image;
                DesignationConfigs.Row row = RuntimeRows[i];
                if (item == null || image == null || row == null) continue;
                bool visualReady = row.Type == 1
                    ? item._gp_dsgt_effect != null && item._gp_dsgt_effect.gameObject.activeSelf
                    : image.sprite != null;
                if (visualReady) continue;
                Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                    viewport, item.transform as RectTransform);
                bool near = bounds.max.y >= visible.yMin - buffer
                    && bounds.min.y <= visible.yMax + buffer;
                int key = image.GetInstanceID();
                if (!near || !RequestedListIcons.Add(key)) continue;
                _ = ApplyListVisualAsync(item, row, key);
            }
        }

        private static async Task ApplyListVisualAsync(DsgtItemRendererBind item,
            DesignationConfigs.Row row, int key)
        {
            if (item == null || row == null) return;
            if (row.Type != 1)
            {
                await ApplyIconAsync(item.resource_image, row.ResourceId);
                return;
            }

            if (item.resource_image != null) item.resource_image.gameObject.SetActive(false);
            RectTransform host = item._gp_dsgt_effect;
            if (host == null) return;
            host.gameObject.SetActive(false);
            DesignationEffectDisplayConfigs.Display display = DesignationEffectDisplayConfigs.Get(
                row.Id, DesignationEffectDisplayConfigs.Surface.ListItem);
            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(
                row.ResourceId?.Trim(), host, DesignationEffectDisplayConfigs.ToUnityPosition(display),
                Vector3.one * display.Scale,
                0f, new Vector2(237f, display.Height));
            if (item == null || !RuntimeItemViews.Contains(item))
            {
                handle?.Dispose();
                return;
            }
            if (handle == null) return;
            if (ListEffects.TryGetValue(key, out UIEffectStage.Handle old)) old?.Dispose();
            ListEffects[key] = handle;
            host.gameObject.SetActive(true);
        }

        private static void DisposeListEffects()
        {
            foreach (UIEffectStage.Handle handle in ListEffects.Values) handle?.Dispose();
            ListEffects.Clear();
        }

        private static void BindActivationClick(RectTransform container, uint designationId)
            => BindActionClick(container, () => DesignationController.Instance.TryActivateByGoods(designationId));

        private static void BindUpgradeClick(RectTransform container, uint designationId)
            => BindActionClick(container, () => DesignationController.Instance.TryUpgrade(designationId));

        private static void BindActionClick(RectTransform container, Action action)
        {
            if (container == null || action == null) return;
            foreach (Graphic graphic in container.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            Image surface = container.GetComponent<Image>();
            if (surface == null) surface = container.gameObject.AddComponent<Image>();
            surface.color = new Color(1f, 1f, 1f, 0f);
            surface.raycastTarget = true;
            UIUtil.ClearClicks(surface);
            UIUtil.AddClick(surface, action);
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

        private static async Task ApplyDetailVisualAsync(DesignationConfigs.Row row, int requestId)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.ResourceId) || _details == null) return;
            if (row.Type == 1 && _details._gp_effect_icon != null)
            {
                RectTransform host = _details._gp_effect_icon;
                DesignationEffectDisplayConfigs.Display display = DesignationEffectDisplayConfigs.Get(
                    row.Id, DesignationEffectDisplayConfigs.Surface.Details);
                UIEffectStage.Handle handle = await UIEffectStage.AddAsync(
                    row.ResourceId.Trim(), host, DesignationEffectDisplayConfigs.ToUnityPosition(display),
                    Vector3.one * display.Scale,
                    0f, new Vector2(237f, display.Height));
                if (requestId != _detailIconRequestId || _details == null)
                {
                    handle?.Dispose();
                    return;
                }
                _detailEffect = handle;
                host.gameObject.SetActive(handle != null);
                return;
            }

            Image image = _details.dsgt_icon_image;
            string path = GameResPath.GetDesignImage(row.ResourceId);
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
                labels[i].richText = true;
                labels[i].text = "<color=#763320>" + GoodsModel.GetAttrName(attr.Id)
                    + "</color><color=#C85A37>+"
                    + GoodsModel.FormatAttrValue(attr.Id, attr.Value) + "</color>";
            }
        }

        private static string FormatStatus(DesignationModel.Entry entry)
        {
            if (entry == null) return "未激活";
            if (entry.EndTime == 0) return "永久有效";
            long remaining = (long)entry.EndTime - TimeUtil.NowSec();
            if (remaining <= 0) return string.Empty;
            if (remaining > 24L * 3600L)
                return ((remaining + 24L * 3600L - 1L) / (24L * 3600L)) + "天";
            if (remaining > 3600L)
                return ((remaining + 3599L) / 3600L) + "小时";
            return ((remaining + 59L) / 60L) + "分";
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
            DisposeListEffects();
            _detailEffect?.Dispose();
            _detailEffect = null;
            if (_view?.dsgt_scoller != null)
                _view.dsgt_scoller.onValueChanged.RemoveListener(OnScrolled);
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_contentRoot != null) ResManager.ReleaseInstance(_contentRoot);
            if (_templateRoot != null) ResManager.ReleaseInstance(_templateRoot);
            RuntimeItems.Clear();
            RuntimeItemViews.Clear();
            RuntimeRows.Clear();
            RequestedListIcons.Clear();
            _frameRoot = null;
            _contentRoot = null;
            _templateRoot = null;
            _window = null;
            _view = null;
            _itemTemplate = null;
            _detailsTemplate = null;
            _details = null;
            _itemBackgroundSprite = null;
            _costItem = null;
            _fight = null;
            _loading = false;
            _selectedId = 0;
            _powerRequestedId = 0;
            _detailIconRequestId++;
        }
    }
}
