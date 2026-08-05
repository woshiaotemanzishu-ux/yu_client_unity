using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Proto;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Role;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Designation;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Marriage;
using Shenxiao.Module.Core.SuitCollect;
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
        private const float ModelScale = 0.73f;
        private static readonly Vector2 ModelPosition = new Vector2(0f, 2f);
        private sealed class AttrRow
        {
            public readonly string Label;
            public readonly Func<BattleAttrProto, long> Value;
            public readonly bool Percentage;
            public readonly bool Spacer;

            public AttrRow(string label, Func<BattleAttrProto, long> value, bool percentage = false)
            {
                Label = label;
                Value = value;
                Percentage = percentage;
            }

            private AttrRow()
            {
                Label = string.Empty;
                Value = _ => 0L;
                Spacer = true;
            }

            public static readonly AttrRow Empty = new AttrRow();
        }

        private static readonly AttrRow[] BaseAttrs =
        {
            new AttrRow("\u653B\u51FB", a => a != null ? a.Get("att") : 0L),
            new AttrRow("\u751F\u547D", a => a != null ? a.HpLim : 0L),
            new AttrRow("\u7834\u7532", a => a != null ? a.Get("wreck") : 0L),
            new AttrRow("\u9632\u5FA1", a => a != null ? a.Get("def") : 0L),
            new AttrRow("\u547D\u4E2D", a => a != null ? a.Get("hit") : 0L),
            new AttrRow("\u95EA\u907F", a => a != null ? a.Get("dodge") : 0L),
            new AttrRow("\u66B4\u51FB", a => a != null ? a.Get("crit") : 0L),
            new AttrRow("\u575A\u97E7", a => a != null ? a.Get("ten") : 0L),
            new AttrRow("\u5143\u7D20\u653B\u51FB", a => a != null ? a.Get("abs_att") : 0L),
            new AttrRow("\u5143\u7D20\u9632\u5FA1", a => a != null ? a.Get("abs_def") : 0L),
            new AttrRow("\u7EDD\u5BF9\u653B\u51FB", a => a != null ? a.Get("real_abs_att") : 0L),
            new AttrRow("\u7EDD\u5BF9\u9632\u5FA1", a => a != null ? a.Get("real_abs_def") : 0L),
            new AttrRow("\u79FB\u52A8\u901F\u5EA6", a => a != null ? a.Speed : 0L),
        };

        // EquipmentView.ts spe_show_index 的原始顺序。空项不是遗漏，而是老端为了两列配对保留的占位。
        private static readonly AttrRow[] SpecialAttrs =
        {
            new AttrRow("\u4F24\u5BB3\u52A0\u6DF1", a => a != null ? a.Get("hurt_add_ratio") : 0L, true),
            new AttrRow("\u4F24\u5BB3\u51CF\u514D", a => a != null ? a.Get("hurt_del_ratio") : 0L, true),
            new AttrRow("\u6280\u80FD\u4F24\u5BB3", a => a != null ? a.Get("skill_hurt_add_ratio") : 0L, true),
            new AttrRow("\u6280\u80FD\u51CF\u514D", a => a != null ? a.Get("skill_hurt_del_ratio") : 0L, true),
            new AttrRow("PVP\u4F24\u5BB3\u52A0\u6DF1", a => a != null ? a.Get("pvp_att_add") : 0L, true),
            new AttrRow("PVP\u4F24\u5BB3\u51CF\u514D", a => a != null ? a.Get("pvp_att_reduece") : 0L, true),
            new AttrRow("\u547D\u4E2D\u51E0\u7387", a => a != null ? a.Get("hit_ratio") : 0L, true),
            new AttrRow("\u8EB2\u95EA\u51E0\u7387", a => a != null ? a.Get("dodge_ratio") : 0L, true),
            new AttrRow("\u66B4\u51FB\u51E0\u7387", a => a != null ? a.Get("crit_ratio") : 0L, true),
            new AttrRow("\u6297\u66B4\u51E0\u7387", a => a != null ? a.Get("uncrit_ratio") : 0L, true),
            new AttrRow("\u4F1A\u5FC3\u51E0\u7387", a => a != null ? a.Get("hurt_float_ratio") : 0L, true),
            new AttrRow("\u6297\u4F1A\u5FC3\u7387", a => a != null ? a.Get("DefHearHurtRatio") : 0L, true),
            AttrRow.Empty,
            new AttrRow("\u5353\u8D8A\u51E0\u7387", a => a != null ? a.Get("ex_ratio") : 0L, true),
            new AttrRow("\u6297\u5353\u51E0\u7387", a => a != null ? a.Get("unex_ratio") : 0L, true),
            new AttrRow("\u653B\u51FB\u52A0\u6210", a => a != null ? a.Get("att_add_ratio") : 0L, true),
            new AttrRow("\u751F\u547D\u52A0\u6210", a => a != null ? a.Get("hp_add_ratio") : 0L, true),
            new AttrRow("\u7834\u7532\u52A0\u6210", a => a != null ? a.Get("wreck_add_ratio") : 0L, true),
            new AttrRow("\u9632\u5FA1\u52A0\u6210", a => a != null ? a.Get("def_add_ratio") : 0L, true),
            new AttrRow("\u547D\u4E2D\u52A0\u6210", a => a != null ? a.Get("hit_add_ratio") : 0L, true),
            new AttrRow("\u95EA\u907F\u52A0\u6210", a => a != null ? a.Get("dodge_add_ratio") : 0L, true),
            new AttrRow("\u66B4\u51FB\u52A0\u6210", a => a != null ? a.Get("crit_add_ratio") : 0L, true),
            new AttrRow("\u575A\u97E7\u52A0\u6210", a => a != null ? a.Get("ten_add_ratio") : 0L, true),
            new AttrRow("\u66B4\u51FB\u52A0\u6DF1", a => a != null ? a.Get("crit_hurt_add_ratio") : 0L, true),
            new AttrRow("\u66B4\u51FB\u51CF\u514D", a => a != null ? a.Get("crit_hurt_del_ratio") : 0L, true),
            new AttrRow("\u4F1A\u5FC3\u52A0\u6DF1", a => a != null ? a.Get("HearHurtAddRatio") : 0L, true),
            new AttrRow("\u4F1A\u5FC3\u51CF\u514D", a => a != null ? a.Get("HearHurtDelRatio") : 0L, true),
            new AttrRow("\u5353\u8D8A\u52A0\u6DF1", a => a != null ? a.Get("ex_hurt_add_ratio") : 0L, true),
            new AttrRow("\u5353\u8D8A\u51CF\u514D", a => a != null ? a.Get("unex_hurt_del_ratio") : 0L, true),
            new AttrRow("\u683C\u6321\u51E0\u7387", a => a != null ? a.Get("parry_ratio") : 0L, true),
            new AttrRow("\u683C\u6321\u5FFD\u89C6", a => a != null ? a.Get("neglect") : 0L, true),
        };

        private readonly List<RolePropertyItemRendererBind> _attrItems = new List<RolePropertyItemRendererBind>();
        private FightingShowSmallItem _fightingItem;
        private bool _subscribed;
        private bool _showingBaseAttrs = true;
        private int _modelRequestId;

        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindActions();
            ShowAttributePage(true);
            if (worldGp != null) worldGp.gameObject.SetActive(false);
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
            int count = Mathf.Max(BaseAttrs.Length, SpecialAttrs.Length);
            for (int i = 0; i < count; i++)
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

            }
        }

        private void RefreshRole()
        {
            RoleModel model = RoleModel.Instance;
            bool hasInfo = model.HasBaseInfo;

            SetText(_lb_name, hasInfo ? model.Name : string.Empty);

            // 老端 top_levelLb 在场景中固定隐藏；境界等级通过 destiny_img + (level - 370) 展示。
            // 原始协议等级仍保留在 RoleModel，只有人物面板的显示值做同语义换算。
            if (top_levelLb != null)
            {
                top_levelLb.text = string.Empty;
                top_levelLb.gameObject.SetActive(false);
            }
            bool isDestinyLevel = hasInfo && model.Level > 370;
            if (destiny_img != null) destiny_img.gameObject.SetActive(isDestinyLevel);
            int displayLevel = isDestinyLevel ? model.Level - 370 : model.Level;
            SetText(levelLb, hasInfo && displayLevel > 0 ? displayLevel + "\u7EA7" : string.Empty);
            SetText(expLb, hasInfo ? FormatNumber(model.Exp) + "/" + FormatNumber(model.ExpLim) : string.Empty);
            SetExp(model);

            if (_fightingItem != null) _fightingItem.SetFighting(hasInfo ? model.CombatPower : 0L);
            RefreshAttrs(model.BattleAttr, hasInfo);
            RefreshWorldInfo(model, hasInfo);
        }

        private void RefreshAttrs(BattleAttrProto attrs, bool hasInfo)
        {
            AttrRow[] rows = _showingBaseAttrs ? BaseAttrs : SpecialAttrs;
            for (int i = 0; i < _attrItems.Count; i++)
            {
                bool visible = i < rows.Length;
                _attrItems[i].gameObject.SetActive(visible);
                if (!visible) continue;
                AttrRow row = rows[i];
                string value = row.Spacer || !hasInfo ? string.Empty : FormatAttribute(row.Value(attrs), row.Percentage);
                RolePropertyItemRenderer renderer = _attrItems[i] as RolePropertyItemRenderer;
                if (renderer != null)
                {
                    renderer.SetData(row.Spacer ? string.Empty : row.Label, value);
                    continue;
                }
                if (_attrItems[i].property_name != null) _attrItems[i].property_name.text = row.Label + ":";
                if (_attrItems[i].property_value != null) _attrItems[i].property_value.text = value;
            }
        }

        private void BindActions()
        {
            BindClick(_img_change_btn, () => ShowAttributePage(!_showingBaseAttrs));
            BindClick(_Group1, RoleFlow.OpenSkill);
            BindClick(_Group5, SuitCollectShellView.Show);
            BindClick(fashion_gp, () => MainUIRouter.Open("fashion"));
            BindClick(_Group2, () => MainUIRouter.Open("AchvEnterView"));
            BindClick(_Group3, () => MainUIRouter.Open("MedalEnterView"));
            BindClick(_Group4, DesignationFlow.Open);
            BindClick(_btn_attribute, () => MainUIRouter.Open("AttributePotionView"));
            BindClick(_Group6, () => MainUIRouter.Open("UnrealEquipView"));
            BindClick(_btn_fame, MarriageHonourFlow.Show);
            BindClick(tipsImg, () => InstructionFlow.Show(453));
            BindClick(worldBtn, () => { if (worldGp != null) worldGp.gameObject.SetActive(true); });
            BindClick(worldBg, () => { if (worldGp != null) worldGp.gameObject.SetActive(false); });
        }

        private void ShowAttributePage(bool showBase)
        {
            _showingBaseAttrs = showBase;
            if (_img_title_base != null) _img_title_base.gameObject.SetActive(showBase);
            if (_img_title_best != null) _img_title_best.gameObject.SetActive(!showBase);
            RefreshAttrs(RoleModel.Instance.BattleAttr, RoleModel.Instance.HasBaseInfo);
        }

        private void RefreshWorldInfo(RoleModel model, bool hasInfo)
        {
            if (worldLb != null)
            {
                worldLb.richText = true;
                worldLb.rectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, 276f);
                string level = model.WorldLv > 370
                    ? "神创" + (model.WorldLv - 370)
                    : model.WorldLv.ToString();
                worldLb.text = hasInfo && model.WorldLv > 0
                    ? "世界等级:<color=#96ff25>" + level
                        + "</color>级\n经验加成:<color=#96ff25>"
                        + model.WorldLvExp + "%</color>"
                    : string.Empty;
            }
            if (worldTips != null)
            {
                worldTips.text = "玩家达到120级后，若低于世界等级一定等级，获得经验加成";
            }
        }

        private static string FormatAttribute(long value, bool percentage)
        {
            if (!percentage) return value.ToString();
            return (value / 100d).ToString("0.##") + "%";
        }

        private static void BindClick(Image image, Action action)
        {
            if (image == null || action == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
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

            UIModelStage.ShowInstance(model_gp, model, ModelScale,
                ModelPosition);
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
                ClotheChartletId = figure.ClotheChartletId,
                HeadRes = figure.HeadModelId > 0 ? figure.HeadModelId : (defaults != null ? defaults.HeadRes : 0),
                HeadChartletId = figure.HeadChartletId,
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
            // _tpl_InnateSkillView/_tpl_InnateListItem/_tpl_InnateSkillItem/_tpl_InnateTypeItemRenderer/
            // _tpl_InnateUpInfoItem/_tpl_InnateUpCondItem 六个引用已不再是本视图的"死重模板"——技能成长线
            // 轮3 3b 单里 InnateSkillCreator 把它们从这里的 __Templates 挪去当 RoleModule 顶层
            // "天赋"tab(InnateSkillView)的常驻/隐藏子节点(见该 Creator 注释),字段引用仍有效(同一 prefab
            // 内 fileID 未变,只是父级变了),但**不能再在这里 HideNode**——本方法只在 EquipmentView 首次
            // Show 时跑一次,若还碰它们会把刚被 InnateSkillView 设成"永久可见"的 InnateListItem/InnateUpInfoItem
            // 误一次性隐藏且此后无人再重新点亮(SetActive(true) 加在父级不会级联子级独立的 active 状态)。
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
