using System.Collections.Generic;
using System.Linq;
using Shenxiao.Common.Proto;
using Shenxiao.Common.Tips;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Fashion;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>
    /// 时装主页(对标老端 fashion/FashionMainView.ts,与 fashion/HeadFashionMainView.ts 共用——
    /// 老端是"同一个类 + fashion_pos_id 字段不同"的继承关系;FashionModule.prefab 只烤了一个
    /// FashionMainView 节点实例,故本端同样"一个类 + <see cref="SetPos"/> 参数化",照抄老端继承法
    /// (spec 裁决:FashionMainView + HeadFashionMainView 用同一个 View 类 + posId 参数)。
    ///
    /// 覆盖 8 活号:41300(全量)/41301(Type2解锁颜色)/41302(穿戴)/41303(卸下)/41304(激活)/
    /// 41306(基础色进阶)/41312(按颜色展示当前与下一阶战力)/41316(彩色进阶)。
    /// 41305(部位等级)由 _img_grade 打开 FashionLevelView；41313-15 套装由 FashionFlow 第四个页签承载。
    /// 视觉与运行态必须对齐当前老端：布局由 FashionModule.prefab 保存；模型预览复用
    /// RoleModelAssembler + UIModelStage，选中衣服/发饰及颜色时即时重建，染色走时装贴图。
    /// </summary>
    public sealed class FashionMainView : FashionMainViewBind
    {
        private const float ModelScale = 1.2f;
        private static readonly Vector2 ModelPosition = new Vector2(0f, -0.5f);

        private int _posId = 1;
        private int _selectedFashionId;
        private int _selectedColorId;
        private bool _subscribed;

        private readonly List<FashionItem> _itemPool = new List<FashionItem>();
        private readonly List<FashionColorItem> _colorPool = new List<FashionColorItem>();
        private readonly List<FashionAttrItem> _attrPool = new List<FashionAttrItem>();
        private Common.BaseAwardItem _awardItem;
        private FightingShowSmallItem _fightItem;
        private int _modelRequestId;
        private string _modelKey = "";
        private int _renderedColorId = -1;
        private string _renderedTextureName = "";
        private bool _previewHasWeapon;
        private int _previewEffectCount;

        public int PosId => _posId;
        public int SelectedFashionId => _selectedFashionId;
        public int SelectedColorId => _selectedColorId;
        public int RenderedColorId => _renderedColorId;
        public string RenderedTextureName => _renderedTextureName;
        public bool PreviewHasWeapon => _previewHasWeapon;
        public int PreviewEffectCount => _previewEffectCount;

        /// <summary>切换穿戴位(1=衣服/3=头饰),FashionFlow 页签驱动(对标老端"同一个类不同 fashion_pos_id")。</summary>
        public void SetPos(int posId)
        {
            if (posId != 1 && posId != 3) posId = 1;
            _posId = posId;
            _selectedFashionId = 0;
            _selectedColorId = 0;
            FashionController.Instance.RequestInfoAll();
            Refresh();
        }

        protected override void OnInit()
        {
            _fightItem = transform.Find("_box_fight")?.GetComponentInChildren<FightingShowSmallItem>(true);
            if (_fightItem == null) GameLog.Warn("Fashion", "FashionMainView Prefab 缺 _box_fight/FightingShowSmallItem");
            BindButtons();
            Subscribe();
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            _ = EnsureConfigsThenRefresh();
            Refresh();
        }

        private async System.Threading.Tasks.Task EnsureConfigsThenRefresh()
        {
            await FashionConfigs.EnsureLoaded();
            await GoodsModel.EnsureLoaded();
            if (this == null || !gameObject.activeInHierarchy) return;
            Refresh();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            ClearModelPreview();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            ClearModelPreview();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            ClearModelPreview();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On(GlobalEvent.EVT_FASHION_UPDATE, OnFashionUpdate);
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, OnFashionUpdate);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off(GlobalEvent.EVT_FASHION_UPDATE, OnFashionUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, OnFashionUpdate);
        }

        private void OnFashionUpdate()
        {
            if (!this) { Unsubscribe(); return; }
            if (!gameObject.activeInHierarchy) return;
            Refresh();
        }

        // ---------------------------------------------------------------- 交互绑定

        private void BindButtons()
        {
            if (_box_activate != null) UIUtil.AddClick(_box_activate, OnActivateClick);
            if (_img_box != null) UIUtil.AddClick(_img_box, OnWearToggleClick);
            if (_img_grade != null) UIUtil.AddClick(_img_grade, OnGradeClick);
        }

        private void OnGradeClick()
        {
            if (_posId != 1) return; // 头饰位无部位等级(对标老端 pos==Head 隐藏 _img_grade)
            FashionFlow.OpenLevel(_posId);
        }

        private void OnWearToggleClick()
        {
            if (_selectedFashionId <= 0) return;
            FashionModel.PosInfo pos = FashionModel.Instance.GetPos(_posId);
            FashionModel.FashionEntry entry = FashionModel.Instance.GetActive(_posId, _selectedFashionId);
            if (entry == null) return; // 未激活不该能穿(_img_box 已隐藏,双重防御)

            bool isWorn = pos != null && pos.WearFashionId == _selectedFashionId && entry.NowColorId == _selectedColorId;
            if (isWorn)
            {
                FashionController.Instance.TakeOff(_posId, _selectedFashionId);
            }
            else if (entry.IsColorUnlocked(_selectedColorId))
            {
                FashionController.Instance.Wear(_posId, _selectedFashionId, _selectedColorId);
            }
        }

        private void OnActivateClick()
        {
            if (_selectedFashionId <= 0) return;
            FashionModel.FashionEntry entry = FashionModel.Instance.GetActive(_posId, _selectedFashionId);

            if (entry == null)
            {
                FashionController.Instance.Activate(_posId, _selectedFashionId);
                return;
            }
            if (!entry.IsColorUnlocked(_selectedColorId))
            {
                FashionController.Instance.UnlockColor(_posId, _selectedFashionId, _selectedColorId);
                return;
            }
            int order = entry.GetStarLv(_selectedColorId);
            FashionConfigs.Row next = FashionConfigs.GetRow(_posId, _selectedFashionId, _selectedColorId, order + 1);
            if (!next.Found)
            {
                TipsManager.Toast("已满阶");
                return;
            }
            if (_selectedColorId == 0) FashionController.Instance.UpgradeBase(_posId, _selectedFashionId);
            else FashionController.Instance.UpgradeColor(_posId, _selectedFashionId, _selectedColorId);
        }

        private void OnItemClick(int fashionId)
        {
            if (_selectedFashionId == fashionId) return;
            _selectedFashionId = fashionId;
            _selectedColorId = 0;
            Refresh();
        }

        private void OnColorClick(int colorId)
        {
            if (_selectedColorId == colorId) return;
            _selectedColorId = colorId;
            Refresh();
        }

        // ---------------------------------------------------------------- 渲染

        private void Refresh()
        {
            if (!FashionConfigs.IsLoaded) return;
            IReadOnlyList<int> ids = FashionConfigs.GetFashionIds(_posId);
            FashionModel.PosInfo pos = FashionModel.Instance.GetPos(_posId);

            if (_selectedFashionId <= 0 || IndexOf(ids, _selectedFashionId) < 0)
            {
                // 老端优先选第一个基础培养红点；没有可操作项时才固定选第 0 格。“使用中”只负责角标，
                // 不会把列表自动跳到当前穿戴项。
                _selectedFashionId = 0;
                for (int i = 0; i < ids.Count; i++)
                {
                    if (!ComputeBaseRed(ids[i])) continue;
                    _selectedFashionId = ids[i];
                    break;
                }
                if (_selectedFashionId <= 0 && ids.Count > 0) _selectedFashionId = ids[0];
                _selectedColorId = 0;
            }

            RefreshList(ids, pos);
            RefreshDetail(pos);
        }

        private static int IndexOf(IReadOnlyList<int> list, int v)
        {
            for (int i = 0; i < list.Count; i++) if (list[i] == v) return i;
            return -1;
        }

        private void RefreshList(IReadOnlyList<int> ids, FashionModel.PosInfo pos)
        {
            if (_list_fashion_item == null) return;
            EnsurePoolSize(_itemPool, _tpl_FashionItem, _list_fashion_item, ids.Count, () =>
            {
                GameObject go = Instantiate(_tpl_FashionItem, _list_fashion_item);
                go.SetActive(true);
                return go.GetComponent<FashionItem>();
            });

            for (int i = 0; i < _itemPool.Count; i++)
            {
                FashionItem item = _itemPool[i];
                bool has = i < ids.Count;
                item.gameObject.SetActive(has);
                if (!has) continue;
                int fashionId = ids[i];

                bool activated = FashionModel.Instance.IsActivated(_posId, fashionId);
                bool worn = pos != null && pos.WearFashionId == fashionId;
                bool hasRed = ComputeItemRed(fashionId);
                int captured = fashionId;
                item.SetClick(() => OnItemClick(captured));
                item.SetData(fashionId, fashionId == _selectedFashionId, activated, worn, hasRed);
            }
        }

        /// <summary>该件是否有可操作红点。老端红点必须同时满足“存在下一阶配置 + 背包材料足够”，
        /// 不能把所有未激活/未满阶条目都标红。</summary>
        private bool ComputeItemRed(int fashionId)
        {
            if (ComputeBaseRed(fashionId)) return true;
            FashionModel.FashionEntry entry = FashionModel.Instance.GetActive(_posId, fashionId);
            IReadOnlyList<int> colorIds = FashionConfigs.GetColorIds(_posId, fashionId);
            for (int i = 0; i < colorIds.Count; i++)
                if (ComputeColorRed(fashionId, colorIds[i], entry)) return true;
            return false;
        }

        private bool ComputeBaseRed(int fashionId)
        {
            FashionModel.FashionEntry entry = FashionModel.Instance.GetActive(_posId, fashionId);
            int order = entry?.GetStarLv(0) ?? 0;
            FashionConfigs.Row next = FashionConfigs.GetBaseRow(_posId, fashionId, order + 1);
            return next.Found && HasEnoughCost(order == 0 ? next.ActiveCostJson : next.StarCostJson);
        }

        private bool ComputeColorRed(int fashionId, int colorId, FashionModel.FashionEntry entry)
        {
            int order = entry?.GetStarLv(colorId) ?? 0;
            FashionConfigs.Row next = FashionConfigs.GetColorRow(_posId, fashionId, colorId, order + 1);
            return next.Found && HasEnoughCost(order == 0 ? next.ActiveCostJson : next.StarCostJson);
        }

        private static bool HasEnoughCost(string json)
        {
            List<(int type, int typeId, long num)> costs = FashionConfigs.ParseCostList(json);
            if (costs.Count == 0) return false;
            for (int i = 0; i < costs.Count; i++)
                if (Bag.BagModel.Instance.GetTypeGoodsNum(costs[i].typeId) < costs[i].num) return false;
            return true;
        }

        private static bool HasAnyOwnedCost(string json)
        {
            List<(int type, int typeId, long num)> costs = FashionConfigs.ParseCostList(json);
            for (int i = 0; i < costs.Count; i++)
                if (Bag.BagModel.Instance.GetTypeGoodsNum(costs[i].typeId) > 0) return true;
            return false;
        }

        private bool ComputeLevelRed(FashionModel.PosInfo pos)
        {
            if (_posId != 1 || pos == null || FashionConfigs.GetPositionRow(_posId, pos.PosLv + 1) == null)
                return false;

            IReadOnlyList<int> ids = FashionConfigs.GetFashionIds(_posId);
            for (int i = 0; i < ids.Count; i++)
            {
                FashionModel.FashionEntry entry = FashionModel.Instance.GetActive(_posId, ids[i]);
                if (entry == null) continue;
                var colors = new List<int> { 0 };
                colors.AddRange(FashionConfigs.GetColorIds(_posId, ids[i]));
                for (int j = 0; j < colors.Count; j++)
                {
                    int level = entry.GetStarLv(colors[j]);
                    if (level <= 0 || FashionConfigs.GetRow(_posId, ids[i], colors[j], level + 1).Found) continue;
                    FashionConfigs.Row current = FashionConfigs.GetRow(_posId, ids[i], colors[j], level);
                    if (current.Found && HasAnyOwnedCost(current.StarCostJson)) return true;
                }
            }
            return false;
        }

        private void RefreshDetail(FashionModel.PosInfo pos)
        {
            if (_selectedFashionId <= 0)
            {
                if (_lb_name != null) _lb_name.text = "";
                if (_lb_order != null) _lb_order.text = "";
                if (_box_activate != null) _box_activate.gameObject.SetActive(false);
                if (_img_box != null) _img_box.gameObject.SetActive(false);
                RefreshColors(null);
                RefreshAttrs(FashionConfigs.Row.Empty, FashionConfigs.Row.Empty);
                RefreshCost(FashionConfigs.Row.Empty, 0);
                ClearModelPreview();
                return;
            }

            FashionModel.FashionEntry entry = FashionModel.Instance.GetActive(_posId, _selectedFashionId);
            string name = GoodsModel.GetGoodsName(_selectedFashionId);
            if (_lb_name != null) _lb_name.text = string.IsNullOrEmpty(name) ? ("时装" + _selectedFashionId) : name;

            // 头饰位没有部位等级线(对标老端 pos==Head → _img_grade.visible=false)
            if (_img_grade != null) _img_grade.gameObject.SetActive(_posId == 1);
            if (_img_grade_red != null) _img_grade_red.gameObject.SetActive(ComputeLevelRed(pos));

            bool isWorn = pos != null && pos.WearFashionId == _selectedFashionId
                && entry != null && entry.NowColorId == _selectedColorId;
            if (_img_box != null) _img_box.gameObject.SetActive(entry != null);
            if (_lb_dress_tips != null) _lb_dress_tips.text = isWorn ? "卸下" : "穿戴";

            RefreshColors(entry);

            bool unlocked = entry != null && entry.IsColorUnlocked(_selectedColorId);
            FashionConfigs.Row curRow, nextRow;
            if (entry == null)
            {
                if (_lb_order != null) _lb_order.text = "[未激活]";
                if (_lb_activate_desc != null) _lb_activate_desc.text = "激活";
                curRow = FashionConfigs.Row.Empty;
                nextRow = FashionConfigs.GetRow(_posId, _selectedFashionId, 0, 1);
                RefreshCost(nextRow, 0);
            }
            else if (!unlocked)
            {
                if (_lb_order != null) _lb_order.text = "[未解锁]";
                if (_lb_activate_desc != null) _lb_activate_desc.text = "解锁";
                curRow = FashionConfigs.Row.Empty;
                nextRow = FashionConfigs.GetRow(_posId, _selectedFashionId, _selectedColorId, 1);
                RefreshCost(nextRow, 0);
            }
            else
            {
                int order = entry.GetStarLv(_selectedColorId);
                if (_lb_order != null) _lb_order.text = "[" + order + "阶]";
                curRow = FashionConfigs.GetRow(_posId, _selectedFashionId, _selectedColorId, order);
                nextRow = FashionConfigs.GetRow(_posId, _selectedFashionId, _selectedColorId, order + 1);
                if (nextRow.Found)
                {
                    if (_lb_activate_desc != null) _lb_activate_desc.text = "进阶";
                    RefreshCost(nextRow, 1);
                }
                else
                {
                    if (_lb_activate_desc != null) _lb_activate_desc.text = "已满阶";
                    RefreshCost(FashionConfigs.Row.Empty, 0);
                }
            }
            RefreshAttrs(curRow, nextRow);

            List<FashionModel.PowerEntry> powers = FashionModel.Instance.GetPower(_posId, _selectedFashionId);
            RefreshPower(powers);
            if (powers == null) FashionController.Instance.RequestPower(_posId, _selectedFashionId);
            RefreshModelPreview();
        }

        private void RefreshPower(List<FashionModel.PowerEntry> powers)
        {
            if (_fightItem == null) return;
            FashionModel.PowerEntry selected = powers?.Find(item => item.ColorId == _selectedColorId);
            long power = selected?.Power ?? 0;
            long increase = selected != null && selected.NextPower > selected.Power
                ? selected.NextPower - selected.Power
                : 0;
            _fightItem.SetFighting(power);
            _fightItem.SetFightingUp(increase);
        }

        /// <summary>颜色档位:index0=基础色(0) + 已配置的非0色,共"颜色数+1"格(对标老端排布说明)。</summary>
        private void RefreshColors(FashionModel.FashionEntry entry)
        {
            if (_box_color_item == null) return;
            IReadOnlyList<int> colorIds = _selectedFashionId > 0
                ? FashionConfigs.GetColorIds(_posId, _selectedFashionId)
                : System.Array.Empty<int>();
            int total = 1 + colorIds.Count;

            GameObject template = FindColorTemplate();
            EnsurePoolSize(_colorPool, template, _box_color_item, total, () =>
            {
                GameObject go = Instantiate(template, _box_color_item);
                go.SetActive(true);
                return go.GetComponent<FashionColorItem>();
            });

            for (int i = 0; i < _colorPool.Count; i++)
            {
                FashionColorItem item = _colorPool[i];
                bool has = i < total;
                item.gameObject.SetActive(has);
                if (!has) continue;
                int colorId = i == 0 ? 0 : colorIds[i - 1];

                bool locked = entry == null || !entry.IsColorUnlocked(colorId);
                bool selected = colorId == _selectedColorId;
                bool hasRed = colorId == 0
                    ? ComputeBaseRed(_selectedFashionId)
                    : ComputeColorRed(_selectedFashionId, colorId, entry);
                FigureProto figure = RoleModel.Instance.Figure;
                int career = figure != null && figure.career > 0 ? figure.career : Mathf.Max(1, RoleModel.Instance.Career);
                int sex = figure != null && figure.sex > 0 ? figure.sex : ((career == 2 || career == 4) ? 2 : 1);
                int showColor = FashionConfigs.GetModelRow(
                    _posId, _selectedFashionId, career, sex, colorId)?.ShowColor ?? 0;
                int captured = colorId;
                item.SetClick(() => OnColorClick(captured));
                item.SetData(colorId, showColor, locked, selected, hasRed);
            }
        }

        /// <summary>颜色模板节点是 FashionModule 顶层的独立同级节点(转换器判定 view-prefab,未纳入
        /// FashionMainView 的 _tpl_* 字段——见任务归档对 FashionModule.prefab 层级的实读证据)。
        /// ⚠实测踩过的坑:本节点在 prefab 里默认 inactive,Unity 对 inactive GameObject 延迟 Awake 到
        /// 它第一次被 SetActive(true) 才跑;而 FashionFlow.ReparentFashion 是"先 SetParent 再 SetActive(true)"
        /// (与 PetFlow.ReparentOutWard 同款顺序),等 Awake 真的跑起来时 transform.parent 早已经不是
        /// FashionModule 根了,按同级名字找必定落空。改为 FashionFlow 在 reparent **之前**(_contentRoot
        /// 还没动过)算好模板节点,经 <see cref="SetColorTemplate"/> 直接塞给本类,不依赖 Awake 时序。</summary>
        private GameObject _colorTemplateCache;

        /// <summary>FashionFlow.ReparentFashion 在把本节点从 FashionModule 顶层挪走之前调用,把同级的
        /// FashionColorItem 模板节点交过来(此时 transform.parent 还没变,由调用方直接给引用最稳妥)。</summary>
        public void SetColorTemplate(GameObject template)
        {
            if (template == null || _colorTemplateCache != null) return;
            template.SetActive(false); // 原始模板默认是显示态(烤制残留),藏起来只当克隆源
            _colorTemplateCache = template;
        }

        private GameObject FindColorTemplate()
        {
            if (_colorTemplateCache == null)
            {
                GameLog.Warn("Fashion", "没收到 FashionColorItem 模板节点(FashionFlow.SetColorTemplate 未调用或 prefab 结构变了)");
            }
            return _colorTemplateCache;
        }

        private void RefreshAttrs(FashionConfigs.Row curRow, FashionConfigs.Row nextRow)
        {
            if (_panel_attr_item == null || _panel_attr_item.content == null) return;
            List<(int attrId, long val)> curAttrs = FashionConfigs.ParseAttrList(curRow.AttrListJson);
            List<(int attrId, long val)> nextAttrs = FashionConfigs.ParseAttrList(nextRow.AttrListJson);
            List<(int attrId, long val)> mainList = curAttrs.Count > 0 ? curAttrs : nextAttrs;

            EnsurePoolSize(_attrPool, _tpl_FashionAttrItem, _panel_attr_item.content, mainList.Count, () =>
            {
                GameObject go = Instantiate(_tpl_FashionAttrItem, _panel_attr_item.content);
                go.SetActive(true);
                return go.GetComponent<FashionAttrItem>();
            });

            for (int i = 0; i < _attrPool.Count; i++)
            {
                FashionAttrItem item = _attrPool[i];
                bool has = i < mainList.Count;
                item.gameObject.SetActive(has);
                if (!has) continue;

                int attrId = mainList[i].attrId;
                long curVal = i < curAttrs.Count ? curAttrs[i].val : 0;
                bool hasNext = i < nextAttrs.Count;
                long nextVal = hasNext ? nextAttrs[i].val : 0;
                item.SetData(attrId, curVal, hasNext, nextVal);
            }
        }

        /// <summary>消耗预览(_box_award 克隆 BaseAwardItem;kind=0 激活/解锁消耗=active_cost,1=进阶消耗=star_cost)。</summary>
        private void RefreshCost(FashionConfigs.Row row, int kind)
        {
            List<(int type, int typeId, long num)> cost = FashionConfigs.ParseCostList(kind == 0 ? row.ActiveCostJson : row.StarCostJson);
            bool has = cost.Count > 0;
            if (_box_fashion_num != null) _box_fashion_num.gameObject.SetActive(has);
            if (!has)
            {
                if (_img_red != null) _img_red.gameObject.SetActive(false);
                if (_awardItem != null) _awardItem.gameObject.SetActive(false); // 无消耗时别留上一次选中项的残影
                return;
            }
            (int type, int typeId, long num) c = cost[0];
            long own = Bag.BagModel.Instance.GetTypeGoodsNum(c.typeId);
            bool enough = own >= c.num;
            if (_lb_own != null) _lb_own.text = own.ToString();
            if (_lb_need != null) _lb_need.text = "/" + c.num;
            if (_img_red != null) _img_red.gameObject.SetActive(enough);

            if (_awardItem == null && _tpl_BaseAwardItem != null && _box_award != null)
            {
                GameObject go = Instantiate(_tpl_BaseAwardItem, _box_award);
                go.SetActive(true);
                _awardItem = go.GetComponent<Common.BaseAwardItem>();
            }
            if (_awardItem != null)
            {
                _awardItem.gameObject.SetActive(true);
                _awardItem.SetData(c.typeId, c.num);
            }
        }

        private static void EnsurePoolSize<T>(List<T> pool, GameObject template, Transform parent, int need, System.Func<T> factory)
            where T : Component
        {
            if (template == null || parent == null) return;
            while (pool.Count < need) pool.Add(factory());
        }

        private async void RefreshModelPreview()
        {
            if (_box_model == null || _selectedFashionId <= 0 || !gameObject.activeInHierarchy) return;
            await LoginConfigs.EnsureLoaded();
            if (this == null || !gameObject.activeInHierarchy) return;

            RoleModel role = RoleModel.Instance;
            FigureProto figure = role.Figure;
            int career = figure != null && figure.career > 0 ? figure.career : Mathf.Max(1, role.Career);
            int sex = figure != null && figure.sex > 0 ? figure.sex : ((career == 2 || career == 4) ? 2 : 1);
            LoginConfigs.CareerRes defaults = LoginConfigs.GetCreateRes(career, sex);
            FashionConfigs.ModelRow selected = FashionConfigs.GetModelRow(
                _posId, _selectedFashionId, career, sex, _selectedColorId);
            if (selected == null || selected.ModelId <= 0)
            {
                ClearModelPreview();
                return;
            }

            int currentClothe = figure != null && figure.ClotheModelId > 0
                ? figure.ClotheModelId : (defaults != null ? defaults.RoleRes : 0);
            int currentHead = figure != null && figure.HeadModelId > 0
                ? figure.HeadModelId : (defaults != null ? defaults.HeadRes : 0);
            int weapon = figure != null && figure.WeaponModelId > 0
                ? figure.WeaponModelId : (defaults != null ? defaults.WeaponRes : 0);
            int clothe = _posId == 1 ? selected.ModelId : currentClothe;
            int head = _posId == 3 ? selected.ModelId : currentHead;
            int clotheChartlet = _posId == 1 ? _selectedColorId : (figure?.ClotheChartletId ?? 0);
            int headChartlet = _posId == 3 ? _selectedColorId : (figure?.HeadChartletId ?? 0);
            if (clothe <= 0)
            {
                ClearModelPreview();
                return;
            }

            string key = string.Join("|", _posId, _selectedFashionId, _selectedColorId, career, sex,
                clothe, clotheChartlet, head, headChartlet, weapon);
            if (_modelKey == key) return;
            _modelKey = key;
            int requestId = ++_modelRequestId;
            _renderedColorId = -1;
            // 切页或切色后必须先撤掉上一页/上一色的共享台画面。否则异步加载期间会把旧 RT
            // 误认成本页模型已就绪，出现“测试通过但玩家看到空白/旧模型”的假阳性。
            UIModelStage.Clear();

            // 时装页的第一阶段目标是老端展示一致。这里明确走老模型组合链：既能保留服装贴图、
            // 武器与常驻特效，也避免 ReplaceableRoleModel 的新整模冷加载拖慢切色预览。
            GameObject model = await RoleModelAssembler.BuildOldModelAsync(new RoleModelSpec
            {
                Career = career,
                ClotheRes = clothe,
                ClotheChartletId = clotheChartlet,
                HeadRes = head,
                HeadChartletId = headChartlet,
                WeaponRes = weapon,
                Actions = LoginConfigs.RoleUIActions("FashionMainView"),
            });
            if (model == null)
            {
                if (requestId == _modelRequestId) _modelKey = "";
                return;
            }
            if (requestId != _modelRequestId || this == null || !gameObject.activeInHierarchy)
            {
                Destroy(model);
                return;
            }

            // 头饰会挂进身体骨骼层级，GetComponentInChildren 的“第一个 Renderer”并不保证是衣服；
            // 染衣时若误读到头饰材质，就会出现画面已换色、运行态却仍报告 model_head_xxx 的假失败。
            // 按本次实际请求的贴图名精确找命中的 Renderer，基础色才回退到首个有效材质。
            string requestedTexture = _selectedColorId > 0
                ? (_posId == 1 ? "model_clothe_" + clothe : "model_head_" + head) + "_" + _selectedColorId
                : "";
            string fallbackTexture = "";
            foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Texture texture = renderer != null ? renderer.material.mainTexture : null;
                if (texture == null) continue;
                if (string.IsNullOrEmpty(fallbackTexture)) fallbackTexture = texture.name;
                if (!string.IsNullOrEmpty(requestedTexture)
                    && string.Equals(texture.name, requestedTexture, System.StringComparison.OrdinalIgnoreCase))
                {
                    fallbackTexture = texture.name;
                    break;
                }
            }
            _renderedTextureName = fallbackTexture;
            _previewHasWeapon = model.GetComponentsInChildren<Transform>(true)
                .Any(node => node != null && node.name.StartsWith("model_weapon_r_",
                    System.StringComparison.OrdinalIgnoreCase));
            _previewEffectCount = model.GetComponentsInChildren<Transform>(true)
                .Count(node => node != null && node.name.StartsWith("__fx_always_",
                    System.StringComparison.Ordinal));

            UIModelStage.SetDragRotate(false);
            UIModelStage.ShowInstance(_box_model, model, ModelScale, ModelPosition, UIModelStage.MODEL_YAW);
            // 编辑器验收不跑 PlayerLoop；换色时上一只模型要到下一次更新才真正销毁。
            // 让出一次更新后再渲染，避免 RenderTexture 仍停留在上一色。真机也因此不会把
            // “材质已换、画面未换”的中间态标记为 ready。
            await System.Threading.Tasks.Task.Yield();
            if (requestId != _modelRequestId || this == null || !gameObject.activeInHierarchy) return;
            UIModelStage.RenderNow();
            _renderedColorId = _selectedColorId;
        }

        private void ClearModelPreview()
        {
            ++_modelRequestId;
            _modelKey = "";
            _renderedColorId = -1;
            _renderedTextureName = "";
            _previewHasWeapon = false;
            _previewEffectCount = 0;
            UIModelStage.Clear();
        }

        public bool IsModelPreviewReady
        {
            get
            {
                if (_box_model == null || _renderedColorId < 0) return false;
                RawImage image = _box_model.GetComponentInChildren<RawImage>(true);
                return image != null && image.gameObject.activeInHierarchy && image.texture != null;
            }
        }
    }
}
