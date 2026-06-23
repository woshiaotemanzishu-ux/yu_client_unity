using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Generated.UI.Role;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Login;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// Role equipment/profile page. Runtime data is sourced from RoleModel, matching the old
    /// Laya EquipmentView flow where the visible model, fighting value and base attrs are built
    /// after role info arrives rather than from the static scene file.
    /// </summary>
    public sealed class EquipmentView : EquipmentViewBind
    {
        private const float ModelScale = 0.5f;
        private const float AttrItemHeight = 38f;
        private const float AttrColumnWidth = 300f;
        private const int AttrColumns = 2;

        private sealed class AttrRow
        {
            public readonly string Label;
            public readonly Func<BattleAttrProto, long> Value;

            public AttrRow(string label, Func<BattleAttrProto, long> value)
            {
                Label = label;
                Value = value;
            }
        }

        private static readonly AttrRow[] BaseAttrs =
        {
            new AttrRow("\u653B\u51FB", a => a != null ? a.Get("att") : 0L),
            new AttrRow("\u6C14\u8840", a => a != null ? a.HpLim : 0L),
            new AttrRow("\u7834\u7532", a => a != null ? a.Get("wreck") : 0L),
            new AttrRow("\u9632\u5FA1", a => a != null ? a.Get("def") : 0L),
            new AttrRow("\u547D\u4E2D", a => a != null ? a.Get("hit") : 0L),
            new AttrRow("\u95EA\u907F", a => a != null ? a.Get("dodge") : 0L),
            new AttrRow("\u66B4\u51FB", a => a != null ? a.Get("crit") : 0L),
            new AttrRow("\u575A\u97E7", a => a != null ? a.Get("ten") : 0L),
        };

        private readonly List<RolePropertyItemRendererBind> _attrItems = new List<RolePropertyItemRendererBind>();
        private FightingShowSmallItem _fightingItem;
        private bool _subscribed;
        private int _modelRequestId;

        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            Subscribe();
        }

        protected override void OnShow(object args)
        {
            BuildRuntimeItems();
            RefreshRole();
            ShowRoleModel();
        }

        protected override void OnHide()
        {
            _modelRequestId++;
            UIModelStage.Clear();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            _modelRequestId++;
            UIModelStage.Clear();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _modelRequestId++;
            UIModelStage.Clear();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            _subscribed = false;
        }

        private void OnRoleInfoUpdate()
        {
            RefreshRole();
            if (gameObject.activeInHierarchy) ShowRoleModel();
        }

        private void BuildRuntimeItems()
        {
            EnsureFightingItem();
            EnsureAttrItems();
        }

        private void EnsureFightingItem()
        {
            if (_fightingItem != null || _tpl_FightingShowSmallItem == null || _gp_fight == null) return;

            GameObject go = Instantiate(_tpl_FightingShowSmallItem, _gp_fight);
            go.name = "FightingShowSmallItem_Runtime";
            var rt = go.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            _fightingItem = go.GetComponent<FightingShowSmallItem>();
            go.SetActive(true);
        }

        private void EnsureAttrItems()
        {
            if (_attrItems.Count > 0 || _tpl_RolePropertyItemRenderer == null
                || _Scroller1 == null || _Scroller1.content == null)
            {
                return;
            }

            RectTransform content = _Scroller1.content;
            content.anchorMin = new Vector2(content.anchorMin.x, 1f);
            content.anchorMax = new Vector2(content.anchorMax.x, 1f);
            content.pivot = new Vector2(content.pivot.x, 1f);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);

            for (int i = 0; i < BaseAttrs.Length; i++)
            {
                GameObject go = Instantiate(_tpl_RolePropertyItemRenderer, content);
                go.name = "RolePropertyItemRenderer_Runtime_" + i;
                RolePropertyItemRendererBind bind = go.GetComponent<RolePropertyItemRendererBind>();
                if (bind != null)
                {
                    bind.Show();
                    _attrItems.Add(bind);
                }
                else
                {
                    go.SetActive(true);
                }

                var rt = go.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    int col = i % AttrColumns;
                    int row = i / AttrColumns;
                    rt.anchoredPosition = new Vector2(col * AttrColumnWidth, -row * AttrItemHeight);
                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, AttrColumnWidth);
                    rt.localScale = Vector3.one;
                }
            }
            int rows = Mathf.CeilToInt(BaseAttrs.Length / (float)AttrColumns);
            content.sizeDelta = new Vector2(
                Mathf.Max(content.sizeDelta.x, AttrColumnWidth * AttrColumns),
                rows * AttrItemHeight + 12f);
        }

        private void RefreshRole()
        {
            RoleModel model = RoleModel.Instance;
            bool hasInfo = model.HasBaseInfo;

            SetText(_lb_name, hasInfo ? model.Name : string.Empty);
            SetText(top_levelLb, hasInfo && model.Level > 0 ? "Lv." + model.Level : string.Empty);
            SetText(levelLb, hasInfo && model.Level > 0 ? model.Level + "\u7EA7" : string.Empty);
            SetText(expLb, hasInfo ? FormatNumber(model.Exp) + "/" + FormatNumber(model.ExpLim) : string.Empty);
            SetExp(model);

            if (_fightingItem != null) _fightingItem.SetFighting(hasInfo ? model.CombatPower : 0L);
            RefreshAttrs(model.BattleAttr, hasInfo);
        }

        private void RefreshAttrs(BattleAttrProto attrs, bool hasInfo)
        {
            for (int i = 0; i < _attrItems.Count && i < BaseAttrs.Length; i++)
            {
                AttrRow row = BaseAttrs[i];
                string value = hasInfo ? FormatNumber(row.Value(attrs)) : string.Empty;
                RolePropertyItemRenderer renderer = _attrItems[i] as RolePropertyItemRenderer;
                if (renderer != null)
                {
                    renderer.SetData(row.Label, value);
                    continue;
                }
                if (_attrItems[i].property_name != null) _attrItems[i].property_name.text = row.Label + ":";
                if (_attrItems[i].property_value != null) _attrItems[i].property_value.text = value;
            }
        }

        private async void ShowRoleModel()
        {
            int requestId = ++_modelRequestId;
            RoleModelSpec spec = await BuildRoleModelSpecAsync(RoleModel.Instance);
            if (spec == null)
            {
                if (requestId == _modelRequestId) UIModelStage.Clear();
                return;
            }

            GameObject model = await RoleModelAssembler.BuildAsync(spec);
            if (model == null) return;
            if (requestId != _modelRequestId || this == null || !gameObject.activeInHierarchy)
            {
                Destroy(model);
                return;
            }

            int sex = RoleModel.Instance.Figure != null ? RoleModel.Instance.Figure.sex : 0;
            UIModelStage.ShowInstance(model_gp, model, ModelScale,
                LoginConfigs.GetModelPos("SelectRole", spec.Career, sex));
        }

        private static async Task<RoleModelSpec> BuildRoleModelSpecAsync(RoleModel model)
        {
            if (model == null || !model.HasBaseInfo || model.Figure == null) return null;

            await LoginConfigs.EnsureLoaded();
            FigureProto figure = model.Figure;
            int career = figure.career;
            int sex = figure.sex;
            LoginConfigs.CareerRes defaults = LoginConfigs.GetCreateRes(career, sex);
            int clothe = figure.ClotheModelId > 0 ? figure.ClotheModelId : (defaults != null ? defaults.RoleRes : 0);
            if (clothe <= 0) return null;

            return new RoleModelSpec
            {
                Career = career,
                ClotheRes = clothe,
                HeadRes = figure.HeadModelId > 0 ? figure.HeadModelId : (defaults != null ? defaults.HeadRes : 0),
                WeaponRes = figure.WeaponModelId > 0 ? figure.WeaponModelId : (defaults != null ? defaults.WeaponRes : 0),
                WingId = figure.WingId,
                BackOrnamentId = figure.BackOrnamentId,
                Actions = LoginConfigs.RoleUIActions("EquipmentView"),
            };
        }

        private void SetExp(RoleModel model)
        {
            if (expImg == null) return;
            float ratio = 0f;
            if (model.HasBaseInfo && model.ExpLim > 0)
                ratio = Mathf.Clamp01(model.Exp / (float)model.ExpLim);
            if (expImg.type == Image.Type.Filled) expImg.fillAmount = ratio;
        }

        private static string FormatNumber(long value)
        {
            if (value >= 100000000L) return (value / 100000000d).ToString("0.##") + "\u4EBF";
            if (value >= 10000L) return (value / 10000d).ToString("0.##") + "\u4E07";
            return value.ToString();
        }

        private static void SetText(TMPro.TextMeshProUGUI label, string value)
        {
            if (label == null) return;
            label.gameObject.SetActive(true);
            label.text = value ?? string.Empty;
        }

        private void HideReds()
        {
            HideNode(skill_red);
            HideNode(suit_red);
            HideNode(fashion_red);
            HideNode(achv_red);
            HideNode(medal_red);
            HideNode(attribute_red);
            HideNode(dsgt_red);
            HideNode(unreal_red);
            HideNode(_red_fame);
        }

        private void HideTemplates()
        {
            HideNode(_tpl_DsgtView);
            HideNode(_tpl_InnateSkillView);
            HideNode(_tpl_InnateListItem);
            HideNode(_tpl_InnateSkillItem);
            HideNode(_tpl_InnateTypeItemRenderer);
            HideNode(_tpl_InnateUpInfoItem);
            HideNode(_tpl_InnateUpCondItem);
            HideNode(_tpl_MedalView);
            HideNode(_tpl_MedalCostItem);
            HideNode(_tpl_RolePropertyItemRenderer);
            HideNode(_tpl_FightingShowSmallItem);
        }

        private static void HideNode(Component node)
        {
            if (node != null) node.gameObject.SetActive(false);
        }

        private static void HideNode(GameObject node)
        {
            if (node != null) node.SetActive(false);
        }
    }
}
