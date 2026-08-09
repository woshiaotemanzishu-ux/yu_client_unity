using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Medal;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Dungeon;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Medal
{
    /// <summary>
    /// 角色人物页“勋章”外窗：复用 BaseWindowSkin 与 RoleModule 内既有 MedalView/MedalCostItem。
    /// 本类只装配数据、事件和协议语义，不重建或改写人工 Prefab 视觉树。
    /// </summary>
    public static class MedalFlow
    {
        private static readonly List<GameObject> CostRows = new List<GameObject>();
        private static readonly List<GameObject> CostAwards = new List<GameObject>();

        private static GameObject _frameRoot;
        private static GameObject _moduleRoot;
        private static GameObject _titleRoot;
        private static GameObject _awardPrefab;
        private static BaseWindowSkinView _window;
        private static MedalViewBind _view;
        private static TitleMainView _titleView;
        private static MedalCostItemBind _costTemplate;
        private static MedalConfigs.UpgradePreview _preview;
        private static bool _loading;
        private static bool _subscribed;
        private static bool _clicksBound;
        private static float _lastUpgradeClickAt = -10f;

        public static void Toggle()
        {
            if (_window != null && _window.IsShown) Close();
            else Open();
        }

        public static void Open() => _ = OpenAsync();

        public static void Close()
        {
            if (_window != null) _window.Hide();
        }

        private static async Task OpenAsync()
        {
            if (_loading) return;
            _loading = true;
            try
            {
                await Task.WhenAll(MedalConfigs.EnsureLoaded(), TitleConfigs.EnsureLoaded(), GoodsModel.EnsureLoaded(),
                    FuncOpenConfig.EnsureLoaded());
                if (!MedalConfigs.IsLoaded || !TitleConfigs.IsLoaded)
                {
                    TipsManager.Toast("境界配置加载失败");
                    return;
                }
                if (!FuncOpenConfig.CheckFuncOpenState("MedalView"))
                {
                    TipsManager.Toast("勋章功能尚未开启");
                    return;
                }

                MedalController.Instance.Init();
                if (!await EnsureViewAsync()) return;
                Subscribe();

                _window.SetReturnAction(ReturnToRole);
                _window.Show();
                _window.Configure(BuildTabSpecs(), 0);

                MedalController.Instance.RequestStartup();
                Render();
            }
            finally
            {
                _loading = false;
            }
        }

        private static IList<TabSpec> BuildTabSpecs()
        {
            return new[]
            {
                new TabSpec
                {
                    Enabled = true,
                    Label = "地境",
                    TitleImagePath = GameResPath.GetIcon("medal", "uixz_001"),
                    BackgroundImagePath = GameResPath.GetBigBgPath("ui_role_bg2.jpg"),
                    ContentFactory = ReparentView,
                },
                new TabSpec
                {
                    Enabled = true,
                    Label = "天境",
                    TitleImagePath = GameResPath.GetIcon("title", "uixz_001"),
                    BackgroundImagePath = GameResPath.GetBigBgPath("ui_role_bg1.jpg"),
                    ContentFactory = ReparentTitleView,
                },
            };
        }

        private static async Task<bool> EnsureViewAsync()
        {
            if (_frameRoot != null && _moduleRoot != null && _titleRoot != null && _window != null
                && _view != null && _titleView != null && _costTemplate != null) return true;

            Transform layer = ViewManager.GetLayer(UILayer.Window);
            Task<GameObject> frameTask = ResManager.InstantiateAsync(
                GameResPath.GetUIPrefab("common", "BaseWindowSkin"), layer);
            Task<GameObject> moduleTask = ResManager.InstantiateAsync(
                GameResPath.GetUIPrefab("role", "RoleModule"), layer);
            Task<GameObject> titleTask = ResManager.InstantiateAsync(
                GameResPath.GetUIPrefab("title", "TitleMainView"), layer);
            Task<GameObject> awardTask = ResManager.LoadAsync<GameObject>(
                GameResPath.GetUIPrefab("common", "BaseAwardItem"));
            await Task.WhenAll(frameTask, moduleTask, titleTask, awardTask);

            _frameRoot = frameTask.Result;
            _moduleRoot = moduleTask.Result;
            _titleRoot = titleTask.Result;
            _awardPrefab = awardTask.Result;
            if (_frameRoot == null || _moduleRoot == null || _titleRoot == null)
            {
                GameLog.Error("Medal", "境界外窗加载失败 frame={0} module={1} title={2}",
                    _frameRoot != null, _moduleRoot != null, _titleRoot != null);
                ReleaseView();
                return false;
            }

            _frameRoot.name = "BaseWindowSkin(Medal)";
            _moduleRoot.name = "RoleModule(Medal)";
            _titleRoot.name = "TitleMainView(Medal)";
            _window = _frameRoot.GetComponent<BaseWindowSkinView>()
                ?? _frameRoot.GetComponentInChildren<BaseWindowSkinView>(true);
            _view = _moduleRoot.GetComponentInChildren<MedalViewBind>(true);
            _titleView = _titleRoot.GetComponent<TitleMainView>()
                ?? _titleRoot.GetComponentInChildren<TitleMainView>(true);
            _costTemplate = _moduleRoot.GetComponentsInChildren<MedalCostItemBind>(true)
                .FirstOrDefault();
            if (_window == null || _view == null || _titleView == null || _costTemplate == null
                || _view.costList == null || _view.costList.content == null)
            {
                GameLog.Error("Medal", "境界缺 BaseWindow/MedalView/TitleMainView/MedalCostItem 或 costList.content 绑定");
                ReleaseView();
                return false;
            }

            foreach (BaseView child in _moduleRoot.GetComponentsInChildren<BaseView>(true))
            {
                if (child != _view && child != _costTemplate) child.gameObject.SetActive(false);
            }
            _view.gameObject.SetActive(false);
            _costTemplate.gameObject.SetActive(false);
            _titleView.gameObject.SetActive(false);
            _moduleRoot.SetActive(true);
            return true;
        }

        private static BaseView ReparentView(RectTransform parent)
        {
            _view.transform.SetParent(parent, false);
            _view.gameObject.SetActive(true);
            _view.Show();
            BindClicks();
            ApplyStaticVisibility();
            Render();
            return _view;
        }

        private static BaseView ReparentTitleView(RectTransform parent)
        {
            _titleView.transform.SetParent(parent, false);
            _titleView.gameObject.SetActive(true);
            _titleView.Show();
            return _titleView;
        }

        private static void ApplyStaticVisibility()
        {
            // 当前 config_medal_stren_cost 的所有门槛均为 9999，地境等级最大 131；强化入口不可达。
            SetActive(_view._gp_icon, false);
            SetActive(_view._str_red, false);
            // 礼包属于 PushGift 跨模块，不在境界页内伪造。
            SetActive(_view.giftIcon, false);
            // 新版消耗统一由现有 MedalCostItem 列表呈现，隐藏旧版三块重复条件。
            SetActive(_view._gp, false);
            SetActive(_view._gp_fight, false);
            SetActive(_view._gp_dungeon, false);
            SetActive(_view._gp_needGoods, false);
        }

        private static void BindClicks()
        {
            if (_clicksBound || _view == null) return;
            BindClick(_view.btnUp, OnUpgradeClicked);
            BindClick(_view._btn_dungeon, OpenRuneDungeon);
            _clicksBound = true;
        }

        private static void Subscribe()
        {
            if (_subscribed) return;
            MedalModel.Instance.Changed += OnAuthoritativeChanged;
            MedalModel.Instance.ErrorReceived += OnErrorReceived;
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, OnVisualStateChanged);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnVisualStateChanged);
            _subscribed = true;
        }

        private static void Unsubscribe()
        {
            if (!_subscribed) return;
            MedalModel.Instance.Changed -= OnAuthoritativeChanged;
            MedalModel.Instance.ErrorReceived -= OnErrorReceived;
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, OnVisualStateChanged);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnVisualStateChanged);
            _subscribed = false;
        }

        private static void OnAuthoritativeChanged()
        {
            if (_window != null && _window.IsShown) Render();
        }

        private static void OnVisualStateChanged()
        {
            if (_window != null && _window.IsShown) Render();
        }

        private static void OnErrorReceived(uint code)
        {
            if (_window != null && _window.IsShown)
                TipsManager.Toast("勋章操作失败（错误码 " + code + "）");
        }

        private static void Render()
        {
            if (_view == null || !MedalConfigs.IsLoaded) return;
            MedalModel model = MedalModel.Instance;
            _preview = MedalConfigs.Evaluate(model.Id, model.HasData,
                RoleModel.Instance.CombatPower, model.PassLayers,
                typeId => BagModel.Instance.GetTypeGoodsNum(typeId));
            MedalConfigs.Row current = _preview.Current;
            MedalConfigs.Row next = _preview.Next;
            if (current == null)
            {
                if (_view.nullLabel != null)
                    _view.nullLabel.text = model.HasData ? "勋章配置缺失" : "勋章数据加载中";
                SetActive(_view.nullLabel, true);
                SetActive(_view.btnUp, false);
                ClearCostRows();
                _window.SetTabRed(0, false);
                return;
            }

            bool unactivated = model.Id == 0 || current.Id == 1;
            SetActive(_view.nullLabel, unactivated);
            if (_view.nullLabel != null) _view.nullLabel.text = "未获得";
            SetActive(_view.medalNullGroup, current.Id == 1);
            SetActive(_view.medalImage, current.Id != 1);
            if (current.Id != 1 && _view.medalImage != null)
                _ = ResManager.SetImageAsync(_view.medalImage,
                    GameResPath.GetIcon("medal", current.LargeImageId.ToString()), nativeSize: false);
            if (_view._img_medal_icon != null)
                _ = ResManager.SetImageAsync(_view._img_medal_icon,
                    GameResPath.GetIcon("medal", "uixunzhang_010"), nativeSize: false);
            if (_view._img_medal_question != null)
                _ = ResManager.SetImageAsync(_view._img_medal_question,
                    GameResPath.GetIcon("medal", "uixunzhang_008"), nativeSize: false);

            SetActive(_view._img_txt, current.LargeImageId != 0);
            if (current.LargeImageId != 0 && _view._img_txt != null)
                _ = ResManager.SetImageAsync(_view._img_txt,
                    GameResPath.GetIcon("mainUI", "tx_" + current.Title), nativeSize: true);
            if (_view.medalName != null)
                _view.medalName.text = current.Id == 1 ? current.MedalName : RoleModel.Instance.Name;

            RenderStars(current.Id == 131 ? 9 : current.Star);
            RenderAttributePanel(current, next);
            RenderCosts(_preview.Conditions);

            bool max = _preview.IsMax;
            SetActive(_view._gp_max, max);
            SetActive(_view.btnUp, !max);
            if (_view.labelDisplay != null && !max)
            {
                _view.labelDisplay.text = _preview.ShouldJump
                    ? "前往挑战"
                    : current.Star >= 9 ? "晋升" : current.Id == 1 ? "激活" : "升级";
            }
            bool ready = _preview.CanUpgrade;
            SetActive(_view._red_dot, ready);
            _window.SetTabRed(0, ready);
            _window.SetTabRed(1, TitleMainView.HasAnyRed());
        }

        private static void RenderStars(int count)
        {
            Image[] stars =
            {
                _view._satr0, _view._satr1, _view._satr2, _view._satr3, _view._satr4,
                _view._satr5, _view._satr6, _view._satr7, _view._satr8,
            };
            int filled = Mathf.Clamp(count, 0, stars.Length);
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                stars[i].gameObject.SetActive(true);
                _ = ResManager.SetImageAsync(stars[i], GameResPath.GetIcon("medal",
                    i < filled ? "uixz_010" : "uixz_009b"), nativeSize: false);
            }
        }

        private static void RenderAttributePanel(MedalConfigs.Row current, MedalConfigs.Row next)
        {
            if (_view._lb_cur_Name != null) _view._lb_cur_Name.text = current.MedalName;
            SetActive(_view._img_txt_cur, current.LargeImageId != 0);
            if (current.LargeImageId != 0 && _view._img_txt_cur != null)
                _ = ResManager.SetImageAsync(_view._img_txt_cur,
                    GameResPath.GetIcon("mainUI", "tx_" + current.Title), nativeSize: true);
            SplitAttributes(current.Attributes, out string currentNames, out string currentValues);
            if (_view._lb_cur_attr != null)
            {
                SetTopLeftX(_view._lb_cur_attr.rectTransform,
                    current.Attributes.Count > 2 ? 60f : 65f);
                ConfigureAttributeText(_view._lb_cur_attr, currentNames);
            }
            if (_view._lb_cur_attr1 != null)
                ConfigureAttributeText(_view._lb_cur_attr1, currentValues);

            SetActive(_view._gp_next, next != null);
            if (next == null)
            {
                if (_view._lb_next_Name != null) _view._lb_next_Name.text = "已满阶";
                if (_view._lb_cur_next != null) _view._lb_cur_next.text = string.Empty;
                if (_view._lb_cur_next1 != null) _view._lb_cur_next1.text = string.Empty;
                return;
            }
            if (_view._lb_next_Name != null) _view._lb_next_Name.text = next.MedalName;
            SetActive(_view._img_txt_next, next.LargeImageId != 0);
            if (next.LargeImageId != 0 && _view._img_txt_next != null)
                _ = ResManager.SetImageAsync(_view._img_txt_next,
                    GameResPath.GetIcon("mainUI", "tx_" + next.Title), nativeSize: true);
            SplitAttributes(next.Attributes, out string nextNames, out string nextValues);
            if (_view._lb_cur_next != null)
            {
                SetTopLeftX(_view._lb_cur_next.rectTransform,
                    next.Attributes.Count > 2 ? 70f : 90f);
                ConfigureAttributeText(_view._lb_cur_next, nextNames);
            }
            if (_view._lb_cur_next1 != null)
                ConfigureAttributeText(_view._lb_cur_next1, nextValues);
        }

        private static void ConfigureAttributeText(TMPro.TextMeshProUGUI label, string text)
        {
            if (label == null) return;
            label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            label.overflowMode = TMPro.TextOverflowModes.Overflow;
            label.text = text;
        }

        private static void SetTopLeftX(RectTransform rect, float x)
        {
            if (rect == null) return;
            Vector2 position = rect.anchoredPosition;
            position.x = x;
            rect.anchoredPosition = position;
        }

        private static void SplitAttributes(IReadOnlyList<MedalConfigs.AttributeValue> attrs,
            out string names, out string values)
        {
            var left = new List<string>();
            var right = new List<string>();
            for (int i = 0; i < attrs.Count; i++)
            {
                MedalConfigs.AttributeValue attr = attrs[i];
                string name = GoodsModel.GetAttrName(attr.Id);
                left.Add((string.IsNullOrEmpty(name) ? ("属性" + attr.Id) : name) + ":");
                right.Add(GoodsModel.FormatAttrValue(attr.Id, attr.Value));
            }
            names = string.Join("\n", left);
            values = string.Join("\n", right);
        }

        private static void RenderCosts(IReadOnlyList<MedalConfigs.ConditionState> conditions)
        {
            ClearCostRows();
            RectTransform parent = _view.costList != null ? _view.costList.content : null;
            if (parent == null || _costTemplate == null) return;
            int count = Mathf.Min(conditions.Count, 3);
            const float itemWidth = 420f;
            const float itemHeight = 34f;
            const float itemStride = 36f;
            float listHeight = count > 0 ? count * 38f : 1f;
            RectTransform listRect = (RectTransform)_view.costList.transform;
            listRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, listHeight);
            parent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, itemWidth);
            parent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, listHeight);

            for (int i = 0; i < count; i++)
            {
                MedalConfigs.ConditionState condition = conditions[i];
                GameObject go = UnityEngine.Object.Instantiate(
                    _costTemplate.gameObject, parent, false);
                go.name = "MedalCostItem(Runtime:" + condition.Type + ")";
                MedalCostItemBind bind = go.GetComponent<MedalCostItemBind>();
                if (bind == null)
                {
                    UnityEngine.Object.Destroy(go);
                    continue;
                }
                CostRows.Add(go);
                go.SetActive(true);
                bind.Show();
                RectTransform row = (RectTransform)go.transform;
                row.anchorMin = row.anchorMax = new Vector2(0f, 1f);
                row.pivot = new Vector2(0f, 1f);
                row.anchoredPosition = new Vector2(0f, -i * itemStride);
                row.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, itemWidth);
                row.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemHeight);
                BindCost(bind, condition);
            }
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }

        private static void BindCost(MedalCostItemBind bind, MedalConfigs.ConditionState state)
        {
            string label;
            switch (state.Type)
            {
                case MedalConfigs.ConditionType.Power: label = "所需战力:"; break;
                case MedalConfigs.ConditionType.Layer: label = "九劫塔层数:"; break;
                default: label = "所需道具:"; break;
            }
            if (bind.descLab != null) bind.descLab.text = label;
            if (bind.curNum != null) bind.curNum.text = state.Current.ToString();
            if (bind.nextLab != null) bind.nextLab.text = "/" + state.Required;
            SetActive(bind.lImg, state.Enough);
            SetActive(bind.hImg, !state.Enough);
            SetActive(bind.gouImg, state.Enough);
            SetActive(bind.chaImg, !state.Enough);
            Color color = state.Enough
                ? new Color32(10, 149, 62, 255)
                : new Color32(254, 26, 26, 255);
            if (bind.curNum != null) bind.curNum.color = color;
            if (bind.nextLab != null) bind.nextLab.color = color;

            bool isItem = state.Type == MedalConfigs.ConditionType.Item;
            SetActive(bind.iconBox, isItem);
            if (!isItem || _awardPrefab == null || bind.iconBox == null) return;
            GameObject awardGo = UnityEngine.Object.Instantiate(_awardPrefab, bind.iconBox, false);
            awardGo.name = "BaseAwardItem(MedalCost)";
            BaseAwardItem award = awardGo.GetComponent<BaseAwardItem>();
            if (award != null)
            {
                award.SetScale(30f / 127f);
                award.SetData(state.ItemTypeId, state.Required);
            }
            CostAwards.Add(awardGo);
        }

        private static void OnUpgradeClicked()
        {
            MedalConfigs.UpgradePreview preview = MedalConfigs.Evaluate(
                MedalModel.Instance.Id, MedalModel.Instance.HasData,
                RoleModel.Instance.CombatPower, MedalModel.Instance.PassLayers,
                typeId => BagModel.Instance.GetTypeGoodsNum(typeId));
            if (preview.ShouldJump)
            {
                OpenRuneDungeon();
                return;
            }
            switch (preview.Block)
            {
                case MedalConfigs.UpgradeBlock.MaterialNotEnough:
                    TipsManager.Toast("勋章晋升材料不足");
                    return;
                case MedalConfigs.UpgradeBlock.PowerNotEnough:
                    TipsManager.Toast("战力不足");
                    return;
                case MedalConfigs.UpgradeBlock.MaxLevel:
                    TipsManager.Toast("勋章已满阶");
                    return;
                case MedalConfigs.UpgradeBlock.None:
                    break;
                default:
                    TipsManager.Toast("勋章数据尚未就绪");
                    return;
            }
            if (Time.unscaledTime - _lastUpgradeClickAt < 1f) return;
            _lastUpgradeClickAt = Time.unscaledTime;
            // 唯一写事务出口：仅真实按钮点击且本地前置条件全部满足时发送严格空包。
            MedalController.Instance.RequestUpgrade();
        }

        private static void OpenRuneDungeon()
        {
            Close();
            DungeonRuneShellView.Show();
        }

        private static void ReturnToRole()
        {
            Close();
            RoleFlow.Open();
        }

        private static void BindClick(RectTransform target, Action action)
        {
            if (target == null) return;
            Image image = target.GetComponent<Image>() ?? target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.ClearClicks(image);
            UIUtil.AddClick(image, action);
        }

        private static void ClearCostRows()
        {
            for (int i = 0; i < CostAwards.Count; i++)
                if (CostAwards[i] != null) UnityEngine.Object.Destroy(CostAwards[i]);
            CostAwards.Clear();
            for (int i = 0; i < CostRows.Count; i++)
                if (CostRows[i] != null) UnityEngine.Object.Destroy(CostRows[i]);
            CostRows.Clear();
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }

        private static void ReleaseView()
        {
            Unsubscribe();
            ClearCostRows();
            _window?.SetReturnAction(null);
            if (_frameRoot != null) ResManager.ReleaseInstance(_frameRoot);
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            if (_titleRoot != null) ResManager.ReleaseInstance(_titleRoot);
            if (_awardPrefab != null) ResManager.Release(_awardPrefab);
            _frameRoot = null;
            _moduleRoot = null;
            _titleRoot = null;
            _awardPrefab = null;
            _window = null;
            _view = null;
            _titleView = null;
            _costTemplate = null;
            _preview = null;
            _clicksBound = false;
            _lastUpgradeClickAt = -10f;
        }

        internal static void Reset()
        {
            Close();
            ReleaseView();
            MedalController.Instance.Dispose();
            _loading = false;
        }
    }
}
