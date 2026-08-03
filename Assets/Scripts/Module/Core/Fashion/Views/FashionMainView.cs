using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Fashion;
using Shenxiao.Module.Core.Common;
using UnityEngine;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>
    /// 时装主页(对标老端 fashion/FashionMainView.ts,与 fashion/HeadFashionMainView.ts 共用——
    /// 老端是"同一个类 + fashion_pos_id 字段不同"的继承关系;FashionModule.prefab 只烤了一个
    /// FashionMainView 节点实例,故本端同样"一个类 + <see cref="SetPos"/> 参数化",照抄老端继承法
    /// (spec 裁决:FashionMainView + HeadFashionMainView 用同一个 View 类 + posId 参数)。
    ///
    /// 覆盖 8 活号:41300(全量)/41301(Type2解锁颜色)/41302(穿戴)/41303(卸下)/41304(激活)/
    /// 41306(基础色进阶)/41312(战力,请求即可,展示留 TODO)/41316(彩色进阶)。
    /// 41305(部位等级)由 _img_grade 打开 FashionLevelView；41313-15 套装由 FashionFlow 第四个页签承载。
    ///
    /// "能点能用即可,不求像素级"(spec 裁决12):列表按横排铺开,不做虚拟滚动/裁剪遮罩;
    /// 未激活/灰显用透明度代替灰阶滤镜(GuildRBItem.cs 先例);3D 模型预览(_box_model)/染色贴图
    /// (GameResPath.GetFashionPath 3D换装贴图管线)本轮不做——见类尾 TODO。
    /// </summary>
    public sealed class FashionMainView : FashionMainViewBind
    {
        private const float ItemW = 96f, ItemH = 97f, ItemGap = 8f;
        private const float AttrRowH = 30f;
        private const float ColorItemH = 115f;

        private int _posId = 1;
        private int _selectedFashionId;
        private int _selectedColorId;
        private bool _subscribed;

        private readonly List<FashionItem> _itemPool = new List<FashionItem>();
        private readonly List<FashionColorItem> _colorPool = new List<FashionColorItem>();
        private readonly List<FashionAttrItem> _attrPool = new List<FashionAttrItem>();
        private Common.BaseAwardItem _awardItem;

        public int PosId => _posId;

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

        protected override void OnHide() => Unsubscribe();
        protected override void OnDispose() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On(GlobalEvent.EVT_FASHION_UPDATE, OnFashionUpdate);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off(GlobalEvent.EVT_FASHION_UPDATE, OnFashionUpdate);
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
                _selectedFashionId = (pos != null && pos.WearFashionId > 0) ? pos.WearFashionId
                    : (ids.Count > 0 ? ids[0] : 0);
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
                var rt = item.transform as RectTransform;
                if (rt != null) rt.anchoredPosition = new Vector2(i * (ItemW + ItemGap), 0f);

                bool activated = FashionModel.Instance.IsActivated(_posId, fashionId);
                bool worn = pos != null && pos.WearFashionId == fashionId;
                bool hasRed = ComputeItemRed(fashionId, activated);
                int captured = fashionId;
                item.SetClick(() => OnItemClick(captured));
                item.SetData(fashionId, fashionId == _selectedFashionId, activated, worn, hasRed);
            }
        }

        /// <summary>该件是否有可操作红点(可激活,或已激活但基础色可进阶——不算材料够不够,只算"有下一步可做")。</summary>
        private bool ComputeItemRed(int fashionId, bool activated)
        {
            if (!activated) return true;
            FashionModel.FashionEntry e = FashionModel.Instance.GetActive(_posId, fashionId);
            int order = e?.GetStarLv(0) ?? 0;
            return FashionConfigs.GetRow(_posId, fashionId, 0, order + 1).Found;
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
                return;
            }

            FashionModel.FashionEntry entry = FashionModel.Instance.GetActive(_posId, _selectedFashionId);
            string name = GoodsModel.GetGoodsName(_selectedFashionId);
            if (_lb_name != null) _lb_name.text = string.IsNullOrEmpty(name) ? ("时装" + _selectedFashionId) : name;

            // 头饰位没有部位等级线(对标老端 pos==Head → _img_grade.visible=false)
            if (_img_grade != null) _img_grade.gameObject.SetActive(_posId == 1);
            if (_img_grade_red != null) _img_grade_red.gameObject.SetActive(false);

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

            FashionController.Instance.RequestPower(_posId, _selectedFashionId);
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
                var rt = item.transform as RectTransform;
                if (rt != null) rt.anchoredPosition = new Vector2(0f, -i * ColorItemH);

                bool locked = entry == null || !entry.IsColorUnlocked(colorId);
                bool selected = colorId == _selectedColorId;
                bool hasRed = locked ? entry != null /* 已激活基础但该色未解锁,可解锁 */
                    : FashionConfigs.GetRow(_posId, _selectedFashionId, colorId, (entry.GetStarLv(colorId)) + 1).Found;
                int captured = colorId;
                item.SetClick(() => OnColorClick(captured));
                item.SetData(locked, selected, hasRed);
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
                var rt = item.transform as RectTransform;
                if (rt != null) rt.anchoredPosition = new Vector2(0f, -i * AttrRowH);

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

        // ---------------------------------------------------------------- TODO(本轮不做,记录给下一刀/资产组)
        // 1. 3D 角色预览(_box_model):老端 ResManager.SetRoleModel 把选中时装套到主角模型上展示,scale 1.2。
        //    Unity 没有等价"UI 内嵌 3D 预览台"组件,需要仿 SceneCharacterStage 搭一个,工作量超出"第一刀"。
        // 2. 染色贴图(GameResPath.GetFashionPath):3D 模型换色贴图管线,资产 1651 个 jpg 未导入
        //    (Assets/GameRes/resource/object/fashion/ 目录为空),且要给 RoleModelSpec 加 texture/chartlet 字段
        //    (Scene/RoleModelAssembler.cs,不在本包 Module/Core/Fashion/** 所有权内)。染色能拿到数据、
        //    能算战力、UI 能选,但角色模型上暂时看不出换色——如实记录,不假装连上。
        // 3. 41311 到达后本人形象已在 FashionController 落地(RoleModel.Instance.Figure.Raw 就地改),
        //    但主界面已在跑的 3D 模型不会热更(Scene/MainRoleFlow.cs 只在 EVT_SCENE_MAP_READY 时重建整只模型,
        //    没有"figure 变了就地刷新"的订阅通道,且该文件不在本包所有权内)——留给下一次碰 Scene 家族的人接上
        //    EVT_FASHION_UPDATE(或专门加一个更精确的形象刷新事件)。
        // 4. 套装页(FashionSuitView,41313-15)与部位等级(FashionLevelView,41305)已由 FashionFlow 接线。
    }
}
