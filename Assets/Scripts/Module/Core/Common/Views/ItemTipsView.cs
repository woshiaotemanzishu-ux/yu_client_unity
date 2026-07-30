using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 物品详情 tips —— 复用 CommonModule.prefab 中可编辑的 GoodsTooltips/EquipToolTips，
    /// 数据、点击与协议由本类绑定，不在运行时代码重建视觉树。对标老端 common/UIToolTipMgr.DefaultAppendTips。
    ///
    /// 链路:点任意 <see cref="BaseAwardItem"/> 物品格(完成弹层/背包),未设点击回调 → 默认弹本 tips(对标 UIToolTipMgr 默认分支);
    /// 数量由格子透传(<see cref="BaseAwardItem.OnClick"/> → Show(typeId,num))。
    /// ★数据全为真★,均来自 config 经 <see cref="GoodsModel"/> 解出:
    ///   · 名=key"1" / 图标=key"14" / 品质=key"18" / 描述=key"2"
    ///   · 类型文本=GoodsType[type"9"].type_name(对标 GoodsTooltips.type_text=WordManager.GetGoodsStyle)
    ///   · 数量=透传的堆叠数(对标 GoodsTooltips.quantity_text)
    ///   · 获取途径=key"3" getway(对标 GoodsTooltips.ways=basic.getway)
    ///   · 装备(type==10):基础属性=base_attrlist key"26" 经 ErlangParser + ConfigItemAttr 取真名(对标 EquipToolTips basePro);
    ///     部位=equip_type key"13"、阶/评分=config_equip_attr、等级需求=key"16"、职业=career_id key"15"(对标 EquipToolTips);
    ///     极品属性=config_equip_attr.recommend_attr key5 预览「随机生成 N 条」(对标 SetBestPro is_preview)、专有属性=other_attr key6(对标 SetRedPro)。
    /// 图标 + 品质底板【复用通用 BaseAwardItem.prefab】(同 TaskFinishView:InstantiateAsync + SetData)。
    /// 实例透传:<see cref="Show(Bag.BagGoods)"/> 带 BagGoods → 极品 equip_extra_attr 真值 / 强化 stren(对标 EquipToolTips goods_vo);
    /// 无实例(缺活服)则装备走 config 极品预览 + 基础/专有属性(真实 config,精确 blocker 仅标实例极品/强化加值需活服,不画假属性)。
    /// 视觉参数以 CommonModule.prefab 为唯一事实源；本类只切必要显隐并填充真实数据。
    /// </summary>
    public static class ItemTipsView
    {
        private static GameObject _moduleRoot;
        private static BaseView _activeView;
        private static GoodsTooltipsBind _goodsView;
        private static EquipToolTipsBind _equipView;
        private static TextMeshProUGUI _nameText;
        private static TextMeshProUGUI _bodyText;
        private static RectTransform _iconSlot;
        private static GameObject _iconCell;
        private static GameObject _useBtn;
        private static GameObject _wearBtn;
        private static GameObject _moveBtn;
        private static TextMeshProUGUI _moveLabel;
        private static Bag.BagGoods _goods;   // 当前实例(使用按钮用;无实例=纯 config 展示,无按钮)
        private static ItemContext _context;
        private static int _epoch;

        public enum ItemContext
        {
            Bag,
            Equipped,
            WarehouseBag,
            WarehouseStorage,
        }

        /// <summary>弹物品详情(对标 UIToolTipMgr.DefaultAppendTips):typeId 不在 config_goods 则不弹(对标 if(!basic) return)。
        /// num=堆叠数量(对标 GoodsTooltips quantity_text,由格子透传;默认 1)。无实例 → 装备走 config 极品预览/基础属性。</summary>
        public static void Show(int typeId, long num = 1) => _ = ShowInternal(typeId, num, null, ItemContext.Bag);

        /// <summary>弹装备实例详情(对标 EquipToolTips.SetData(goods_vo)):带 <see cref="Bag.BagGoods"/> 实例 →
        /// 极品 equip_extra_attr / 强化 stren 实例属性行(缺活服实例字段则回落 config 极品预览);typeId/数量取自实例。</summary>
        public static void Show(Bag.BagGoods goods)
        {
            if (goods == null) return;
            _ = ShowInternal(goods.TypeId, goods.GoodsNum, goods, ItemContext.Bag);
        }

        /// <summary>已穿戴装备详情。老端没有可达的普通卸下 sender，因此只展示，不暴露“卸下/穿戴”。</summary>
        public static void ShowEquipped(Bag.BagGoods goods)
        {
            if (goods == null) return;
            _ = ShowInternal(goods.TypeId, goods.GoodsNum, goods, ItemContext.Equipped);
        }

        /// <summary>仓库双栏详情：只提供存入/取出，实际移动走 15003。</summary>
        public static void ShowWarehouse(Bag.BagGoods goods, bool inStorage)
        {
            if (goods == null) return;
            _ = ShowInternal(goods.TypeId, goods.GoodsNum, goods,
                inStorage ? ItemContext.WarehouseStorage : ItemContext.WarehouseBag);
        }

        private static async Task ShowInternal(int typeId, long num, Bag.BagGoods goods, ItemContext context)
        {
            int epoch = ++_epoch;
            await GoodsModel.EnsureLoaded();
            if (epoch != _epoch) return;
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
            if (basic == null)
            {
                GameLog.Warn("Common", "ItemTips: typeId={0} 不在 config_goods(或未加载)→ 不弹详情", typeId);
                return;
            }

            if (!await EnsureBuilt()) return;
            if (epoch != _epoch) return;

            bool isEquip = GoodsModel.IsEquip(typeId);
            if (!ActivatePrefabView(isEquip)) return;
            _activeView.transform.SetAsLastSibling();

            _goods = goods;
            _context = context;
            // 使用按钮:仅背包实例 + config use!=0(对标 GoodsTooltips useBtn 隐藏条件 basic.use==0;
            // isTreasure/takeout/deposite/put/isSoul 等特殊容器态未移植,普通背包物品恒 false)。
            bool useVisible = goods != null && context == ItemContext.Bag && basic.Use != 0;
            // 穿戴按钮(薄增量六件套第20轮):仅背包实例 + 装备类物品(IsEquip),发 15201。
            bool wearVisible = goods != null && context == ItemContext.Bag && isEquip;
            bool moveVisible = goods != null && (context == ItemContext.WarehouseBag || context == ItemContext.WarehouseStorage);
            ConfigurePrefabButtons(isEquip, useVisible, wearVisible, moveVisible, context);
            if (_useBtn != null) _useBtn.SetActive(useVisible);
            if (_wearBtn != null) _wearBtn.SetActive(wearVisible);
            if (_moveBtn != null) _moveBtn.SetActive(moveVisible);
            if (_moveLabel != null && moveVisible)
                _moveLabel.text = context == ItemContext.WarehouseStorage ? "取出" : "存入";

            _nameText.text = string.IsNullOrEmpty(basic.Name) ? ("#" + typeId) : basic.Name;
            _bodyText.text = BuildBody(typeId, num, basic, goods);
            if (context == ItemContext.Bag && goods != null && isEquip)
                _bodyText.text += BuildEquipmentComparison(goods, basic);

            _ = BuildIcon(typeId, epoch);
            if (goods != null && goods.GoodsId > 0)
                RequestDetailSection(goods.GoodsId, epoch);

            GameLog.Info("Common", "ItemTips 打开: typeId={0} '{1}' type={2}({3}) color={4} num={5} equip={6} inst={7}",
                typeId, basic.Name, basic.Type, GoodsModel.GetGoodsTypeName(basic.Type), basic.Color, num, GoodsModel.IsEquip(typeId),
                goods != null && goods.HasInstanceAttr);
        }

        /// <summary>物品详情接入(Goods 协议扩容轮):Show 装备实例(带 GoodsId)时向 GoodsDynamicModel 要 15000 详情
        /// (节流内直接回缓存,否则等 On15000 回包);到达经 epoch 防竞态校验后在正文追加"详情段"(洗炼/宝石/附魔,
        /// 有数据才加,风格照 AppendBestPro 的「&lt;color&gt;【标题】&lt;/color&gt;+逐行」)。首绘不阻塞(BuildBody 已同步显示)。</summary>
        private static void RequestDetailSection(long goodsId, int epoch)
        {
            Bag.GoodsDynamicModel.Instance.RequestDetail(goodsId, detail =>
            {
                if (epoch != _epoch || detail == null) return;   // 已关闭/切换到别的物品 → 丢弃
                string section = BuildDetailSection(detail);
                if (!string.IsNullOrEmpty(section) && _bodyText != null) _bodyText.text += section;
            });
        }

        /// <summary>拼装 15000 详情段(洗炼属性 wash_attr / 宝石 stone_list / 附魔 magic_list;各自有数据才加,不占位)。</summary>
        private static string BuildDetailSection(Bag.GoodsDetailVo detail)
        {
            var sb = new StringBuilder();
            if (detail.WashAttrs != null && detail.WashAttrs.Count > 0)
            {
                sb.Append("\n\n<color=#7fd0ff>【洗炼属性】</color>");
                foreach (Bag.GoodsWashAttr w in detail.WashAttrs)
                {
                    string name = GoodsModel.GetAttrName(w.AttrId);
                    if (string.IsNullOrEmpty(name)) name = "属性" + w.AttrId;
                    sb.Append("\n<color=#8be07a>").Append(name).Append("　+").Append(GoodsModel.FormatAttrValue(w.AttrId, w.AttrVal)).Append("</color>");
                }
            }
            if (detail.StoneList != null && detail.StoneList.Count > 0)
            {
                sb.Append("\n\n<color=#d19bff>【宝石】</color>");
                foreach (Bag.GoodsStoneSlot s in detail.StoneList)
                {
                    GoodsModel.GoodsBasic stoneBasic = GoodsModel.GetGoodsBasicByTypeId(s.TypeId);
                    string stoneName = stoneBasic != null ? stoneBasic.Name : ("#" + s.TypeId);
                    sb.Append("\n孔位").Append(s.Pos).Append("：<color=#d19bff>").Append(stoneName).Append("</color>");
                }
            }
            if (detail.MagicList != null && detail.MagicList.Count > 0)
            {
                sb.Append("\n\n<color=#ffb3d9>【附魔】</color>");
                foreach (Bag.GoodsMagicSlot m in detail.MagicList)
                {
                    GoodsModel.GoodsBasic magicBasic = GoodsModel.GetGoodsBasicByTypeId(m.GoodsId);
                    string magicName = magicBasic != null ? magicBasic.Name : ("#" + m.GoodsId);
                    sb.Append("\n<color=#ffb3d9>").Append(magicName).Append("</color>");
                }
            }
            return sb.ToString();
        }

        public static void Close()
        {
            _epoch++;
            _goods = null;
            _context = ItemContext.Bag;
            if (_iconCell != null) { ResManager.ReleaseInstance(_iconCell); _iconCell = null; }
            if (_activeView != null) _activeView.Hide();
            _activeView = null;
            _nameText = null;
            _bodyText = null;
            _iconSlot = null;
            _useBtn = null;
            _wearBtn = null;
            _moveBtn = null;
            _moveLabel = null;
        }

        /// <summary>
        /// 使用按钮点击(对标 GoodsTooltips useBtn_fun → CheckSecondView() + Close):
        /// 老端按 type/subtype 分流到各专属界面(礼包选择/经验符比较/藏宝图…),这些界面未移植 → 明确提示不移植假发协议;
        /// 普通可用物品走默认分支:数量 1 直接发 15050,堆叠物复用 GoodsFuncView 数量滑杆后按选择数量发送。
        /// </summary>
        private static void OnUseClick()
        {
            Bag.BagGoods goods = _goods;
            if (goods == null) return;
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
            if (basic == null) return;

            // 转职卡(type38/subtype39,轮5 接线):等级预检(对标 GoodsTooltips.ts:1201-1204/ItemInfoTips.ts:696-698
            // 两处重复实现 goods_vo.level>角色等级 → 不开窗直接 toast「等级不足」)→ 打开 TransferJobCardView
            // (对标 Fire(OPEN_VIEW,"TransferJobCardView"))。走专属分支,不落入下面的通用 UseBranchBlocker 降级表。
            if (basic.Type == 38 && basic.Subtype == 39)
            {
                if (basic.Level > 0 && basic.Level > Role.RoleModel.Instance.Level)
                {
                    TipsManager.Toast("等级不足");
                    Close();
                    return;
                }
                TransferJob.TransferJobFlow.Show();
                Close();
                return;
            }

            // 老端 CheckSecondView 的专属界面分流(SelectGiftView/经验符/藏宝图/装扮…未移植):不发 15050(老端也不直发),明确降级。
            string blocked = UseBranchBlocker(basic);
            if (blocked != null)
            {
                TipsManager.Toast("该物品需专属界面(未移植:" + blocked + ")");
                GameLog.Info("Common", "ItemTips 使用分流未移植: typeId={0} type={1}/{2} → {3}(对标 CheckSecondView)",
                    goods.TypeId, basic.Type, basic.Subtype, blocked);
                return;
            }

            if (goods.GoodsNum <= 1)
            {
                Bag.BagController.Instance.UseGoods(goods.GoodsId, 1);
                Close();   // 对标老端 useBtn_fun:CheckSecondView 后 Close
            }
            else
            {
                BatchUseFlow.Show(goods);
                Close();
            }
        }

        /// <summary>穿戴按钮点击(薄增量六件套第20轮):发 15201,回包 res==1 由 EquipWearController 弹 toast「穿戴成功」;
        /// 此处仅发包 + 关闭详情(对标 ItemTips 点击后收起,不等待回包结果)。</summary>
        private static void OnWearClick()
        {
            Bag.BagGoods goods = _goods;
            if (goods == null) return;
            Equip.EquipWearController.Instance.Wear(goods.GoodsId);
            Close();
        }

        private static void OnMoveClick()
        {
            Bag.BagGoods goods = _goods;
            if (goods == null) return;
            int from = _context == ItemContext.WarehouseStorage ? Bag.BagModel.POS_WAREHOUSE : Bag.BagModel.POS_BAG;
            int to = from == Bag.BagModel.POS_WAREHOUSE ? Bag.BagModel.POS_BAG : Bag.BagModel.POS_WAREHOUSE;
            Bag.BagController.Instance.MoveGoods(goods.GoodsId, from, to);
            Close();
        }

        private static string BuildEquipmentComparison(Bag.BagGoods candidate, GoodsModel.GoodsBasic basic)
        {
            if (basic == null || basic.EquipType <= 0) return "";
            Bag.BagGoods worn = Bag.BagModel.Instance.GetEquipmentAt(basic.EquipType);
            if (worn == null || worn.GoodsId == candidate.GoodsId)
                return "\n\n<color=#7fd0ff>【装备对比】</color>\n当前部位未穿戴装备";

            GoodsModel.GoodsBasic wornBasic = GoodsModel.GetGoodsBasicByTypeId(worn.TypeId);
            string wornName = wornBasic != null && !string.IsNullOrEmpty(wornBasic.Name) ? wornBasic.Name : ("#" + worn.TypeId);
            long diff = candidate.Rating - worn.Rating;
            string color = diff > 0 ? "#63df72" : diff < 0 ? "#ff6b6b" : "#ffe222";
            string sign = diff > 0 ? "+" : "";
            var sb = new StringBuilder();
            sb.Append("\n\n<color=#7fd0ff>【装备对比】</color>")
              .Append("\n当前：<color=#ffe222>").Append(wornName).Append("</color>");

            GoodsModel.EquipAttr wornEquip = GoodsModel.GetEquipAttr(worn.TypeId);
            if (wornEquip != null)
            {
                sb.Append("　").Append(wornEquip.Stage).Append("阶");
                if (wornEquip.Star > 0) sb.Append(wornEquip.Star).Append("星");
            }
            if (worn.Stren > 0) sb.Append("　强化+").Append(worn.Stren);
            sb.Append("\n评分：当前 ").Append(worn.Rating)
              .Append(" / 候选 ").Append(candidate.Rating)
              .Append("　<color=").Append(color).Append(">").Append(sign).Append(diff).Append("</color>");

            var currentAttrs = new Dictionary<string, long>();
            foreach ((string name, long val) row in GoodsModel.GetBaseAttrs(worn.TypeId))
                currentAttrs[row.name] = row.val;
            var candidateAttrs = new Dictionary<string, long>();
            foreach ((string name, long val) row in GoodsModel.GetBaseAttrs(candidate.TypeId))
                candidateAttrs[row.name] = row.val;

            var orderedNames = new List<string>();
            foreach (string name in candidateAttrs.Keys) orderedNames.Add(name);
            foreach (string name in currentAttrs.Keys)
                if (!candidateAttrs.ContainsKey(name)) orderedNames.Add(name);
            foreach (string name in orderedNames)
            {
                currentAttrs.TryGetValue(name, out long current);
                candidateAttrs.TryGetValue(name, out long next);
                long attrDiff = next - current;
                string attrColor = attrDiff > 0 ? "#63df72" : attrDiff < 0 ? "#ff6b6b" : "#ffe222";
                sb.Append("\n").Append(name).Append("：当前 ").Append(current)
                  .Append(" / 候选 ").Append(next).Append("　<color=").Append(attrColor).Append(">")
                  .Append(attrDiff > 0 ? "+" : "").Append(attrDiff).Append("</color>");
            }
            return sb.ToString();
        }

        /// <summary>老端 CheckSecondView 专属界面分支表:命中返回目标界面名(未移植),null=可走默认 15050 分支。</summary>
        private static string UseBranchBlocker(GoodsModel.GoodsBasic b)
        {
            if (b.Type == 34) return "SelectGiftView(礼包选择)";
            if (b.Type == 37 && b.Subtype == 2) return "经验符 buff 比较流程";
            if (b.Type == 38 && b.Subtype == 6) return "OpenFun 35(定时宝箱)";
            if (b.Type == 38 && b.Subtype == 10) return "MarriageFlowerView(婚礼鲜花)";
            if (b.Type == 38 && b.Subtype == 36) return "MaskUseView(蒙面人道具)";
            // type38/subtype39(转职卡)轮5 已接真实 TransferJobCardView,提前在 OnUseClick 里专属分流,不落这张表。
            if (b.Type == 38 && b.Subtype == 42) return "OpenFun 18(转生界面)";
            if (b.Type == 75) return "藏宝图(野外场景使用)";
            if (b.Type == 59) return "OpenFun 203(装扮)";
            if (b.Type == 83 && b.Subtype == 1) return "OpenFun 240(古宝)";
            if (b.Type == 14 && b.Subtype == 12) return "OpenFun 11(宝石直升)";
            if (b.Type == 22 && b.Subtype == 1) return "OpenFun 16(伙伴魂珠)";
            if (b.TypeId == 37090001) return "人物直升丹确认流程";
            return null;
        }

        /// <summary>
        /// 组装详情正文(对标 GoodsTooltips/EquipToolTips 的字段拼装):类型 + 数量 → 装备(基础属性 + 部位/阶/等级/职业)或
        /// 普通物品(描述 intro)→ 获取途径。全字段真实 config 驱动,缺则跳过(不占位、不臆造)。
        /// </summary>
        private static string BuildBody(int typeId, long num, GoodsModel.GoodsBasic basic, Bag.BagGoods goods)
        {
            var sb = new StringBuilder();

            // —— 类型 + 数量(对标 GoodsTooltips type_text / quantity_text)——
            string typeName = GoodsModel.GetGoodsTypeName(basic.Type);
            var head = new List<string>();
            if (!string.IsNullOrEmpty(typeName)) head.Add("类型：<color=#ffe222>" + typeName + "</color>");
            head.Add("数量：<color=#ffe222>" + num + "</color>");
            sb.Append(string.Join("    ", head));

            if (GoodsModel.IsEquip(typeId))
                AppendEquip(sb, typeId, basic, goods);
            else
                AppendNormal(sb, basic);

            // —— 获取途径(对标 GoodsTooltips ways=basic.getway,key "3";空 / "[]" 空列表占位则不显)——
            string getway = basic.Getway?.Trim();
            if (!string.IsNullOrEmpty(getway) && getway != "[]")
                sb.Append("\n\n<color=#7fd0ff>获取途径：</color>").Append(ToTmpRich(getway));

            return sb.ToString();
        }

        /// <summary>普通物品:描述 intro(对标 GoodsTooltips else 分支 basic.intro)。</summary>
        private static void AppendNormal(StringBuilder sb, GoodsModel.GoodsBasic basic)
        {
            string intro = ToTmpRich(basic.Intro);
            sb.Append("\n\n").Append(string.IsNullOrEmpty(intro) ? "<color=#8893a6>(暂无描述)</color>" : intro);
        }

        /// <summary>装备(type==10):部位/阶/等级/职业/评分 + 基础属性 + 极品属性(实例或 config 预览)+ 专有属性
        /// (对标 EquipToolTips pos/grade/level/career/basePro/SetBestPro/SetRedPro)。goods!=null 走实例真值,否则 config 预览。</summary>
        private static void AppendEquip(StringBuilder sb, int typeId, GoodsModel.GoodsBasic basic, Bag.BagGoods goods)
        {
            GoodsModel.EquipAttr ea = GoodsModel.GetEquipAttr(typeId);

            // 部位 + 阶 + 等级 + 职业(对标 EquipToolTips pos=GetEquipPos / grade=`${stage}阶` / level / career)
            var meta = new List<string>();
            string pos = GoodsModel.GetEquipPosName(basic.EquipType);
            if (!string.IsNullOrEmpty(pos)) meta.Add("部位：<color=#ffe222>" + pos + "</color>");
            if (ea != null && ea.Stage > 0)
                meta.Add("<color=#ffe222>" + ea.Stage + "阶" + (ea.Star > 0 ? ea.Star + "星" : "") + "</color>");
            if (basic.Level > 0) meta.Add("等级需求：<color=#ffe222>" + basic.Level + "</color>");
            meta.Add("职业：<color=#ffe222>" + GoodsModel.GetCareerName(basic.CareerId) + "</color>");
            if (meta.Count > 0) sb.Append("\n").Append(string.Join("    ", meta));

            // 评分(对标 EquipToolTips score=goods_vo.rating(实例)兜底 config base_rating)
            long score = (goods != null && goods.Rating > 0) ? goods.Rating : (ea != null ? ea.BaseRating : 0);
            if (score > 0) sb.Append("\n评分：<color=#ffef67>").Append(score).Append("</color>");

            // 基础属性行(对标 EquipToolTips basePro)+ 实例强化等级(强化加值需 config_equip_stren_lv,未载 → 仅标强化等级)
            List<(string name, long val)> attrs = GoodsModel.GetBaseAttrs(typeId);
            sb.Append("\n\n<color=#7fd0ff>【基础属性】</color>");
            if (goods != null && goods.Stren > 0) sb.Append("　<color=#0a953e>强化 +").Append(goods.Stren).Append("</color>");
            if (attrs.Count == 0)
            {
                sb.Append("\n<color=#8893a6>(该装备 config 无基础属性)</color>");
            }
            else
            {
                foreach ((string name, long val) in attrs)
                    sb.Append("\n").Append(name).Append("　<color=#d15e00>+").Append(val).Append("</color>");
            }

            AppendBestPro(sb, typeId, goods);   // 极品属性(实例 equip_extra_attr 真值优先,否则 config recommend_attr 预览)
            AppendOtherPro(sb, typeId);          // 专有属性(config other_attr)

            // 仅 config(无实例)时标注:实例极品/强化加值需活服(精确 blocker,不画假属性)
            if (goods == null)
                sb.Append("\n\n<color=#8893a6>(以上极品为 config 预览;强化加值/实例极品属性需登录活服取实装备)</color>");

            // 描述附在属性之后(若有)
            string intro = ToTmpRich(basic.Intro);
            if (!string.IsNullOrEmpty(intro)) sb.Append("\n\n").Append(intro);
        }

        /// <summary>极品属性(对标 EquipToolTips.SetBestPro):实例 equip_extra_attr 真值优先(EquipBestProItem 非预览),
        /// 否则 config recommend_attr 预览「随机生成 N 条」(is_preview)。两者皆空则不显(不占位)。</summary>
        private static void AppendBestPro(StringBuilder sb, int typeId, Bag.BagGoods goods)
        {
            // 有实例极品 → 实例真值(对标 goods_vo.equip_extra_attr → EquipBestProItem 非预览分支)
            if (goods != null && goods.ExtraAttrs != null && goods.ExtraAttrs.Count > 0)
            {
                sb.Append("\n\n<color=#ff8a3c>【极品属性】</color>");
                foreach (Bag.EquipExtraAttr ex in goods.ExtraAttrs)
                {
                    string name = GoodsModel.GetAttrName(ex.AttrId);
                    if (string.IsNullOrEmpty(name)) name = "属性" + ex.AttrId;
                    string val;
                    if (ex.AttrTypeId == 1)   // type_id==1:区间成长(对标 EquipBestProItem data.type_id==1 → plus_unit + 名 {0}→plus_interval)
                    {
                        if (name.Contains("{0}")) name = name.Replace("{0}", ex.PlusInterval.ToString());
                        val = ex.PlusUnit.ToString();
                    }
                    else val = GoodsModel.FormatAttrValue(ex.AttrId, ex.AttrVal);
                    sb.Append("\n<color=#ffa666>").Append(name).Append("　+").Append(val).Append("</color>");
                }
                return;
            }

            // 无实例 → config 极品预览(对标 SetBestPro is_preview → recommend_attr + GetBestProNum 标题)
            List<(string name, string val)> rec = GoodsModel.GetEquipRecommendAttrs(typeId);
            if (rec.Count == 0) return;
            sb.Append("\n\n<color=#ff8a3c>【极品属性】</color>");
            int n = GoodsModel.GetBestProNum(typeId);
            if (n > 0) sb.Append("<color=#8893a6>(随机生成 ").Append(n).Append(" 条)</color>");
            foreach ((string name, string val) in rec)
                sb.Append("\n<color=#ffa666>[推荐] ").Append(name).Append("　+").Append(val).Append("</color>");
        }

        /// <summary>专有属性(对标 EquipToolTips.SetRedPro → Util.GetAttrStr:config other_attr 逐项「名：值」,值经 FormatAttrValue)。空则不显。</summary>
        private static void AppendOtherPro(StringBuilder sb, int typeId)
        {
            List<(string name, string val)> others = GoodsModel.GetEquipOtherAttrs(typeId);
            if (others.Count == 0) return;
            sb.Append("\n\n<color=#d15e00>【专有属性】</color>");
            foreach ((string name, string val) in others)
                sb.Append("\n<color=#ffcaa0>").Append(name).Append("：").Append(val).Append("</color>");
        }

        // 图标 + 品质底板:复用 BaseAwardItem.prefab(真实图标 + com_goods_plate_{color}),epoch 防重开/关闭竞态
        // (epoch 由调用方 ShowInternal 统一签发,与详情段请求共用同一令牌,见 RequestDetailSection)。
        private static async Task BuildIcon(int typeId, int epoch)
        {
            if (_iconCell != null) { ResManager.ReleaseInstance(_iconCell); _iconCell = null; }
            if (_iconSlot == null) return;

            GameObject go = await ResManager.InstantiateAsync(GameResPath.GetUIPrefab("common", "BaseAwardItem"), _iconSlot);
            if (epoch != _epoch) { if (go != null) ResManager.ReleaseInstance(go); return; }
            if (go == null)
            {
                GameLog.Warn("Common", "ItemTips: 复用 BaseAwardItem 失败(prefab 未导入/未分组?) typeId={0}", typeId);
                return;
            }
            go.SetActive(true);
            var cell = go.GetComponent<BaseAwardItem>();
            if (cell == null)
            {
                GameLog.Warn("Common", "ItemTips: BaseAwardItem.prefab 根缺 BaseAwardItem 组件(跑 神霄/UI/回填 Bind 组件)typeId={0}", typeId);
                ResManager.ReleaseInstance(go);
                return;
            }
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            cell.SetClickCallBack(() => { }); // tips 内的图标不再二次弹 tips(避免递归)
            cell.SetData(typeId, 1);
            _iconCell = go;
        }

        /// <summary>Laya HTML → TMP 富文本:&lt;br/&gt;→换行,&lt;font color='#x'&gt;→&lt;color=#x&gt;,&lt;/font&gt;→&lt;/color&gt;。</summary>
        private static string ToTmpRich(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = Regex.Replace(s, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, "<font\\s+color=['\"]?(#?[0-9a-fA-F]+)['\"]?\\s*>", "<color=$1>", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, "</font>", "</color>", RegexOptions.IgnoreCase);
            return s;
        }

        // ===================== 现有 CommonModule Prefab 绑定 =====================

        private static async Task<bool> EnsureBuilt()
        {
            if (_moduleRoot != null && _goodsView != null && _equipView != null) return true;
            Transform parent = ViewManager.GetLayer(UILayer.Popup);
            if (parent == null)
            {
                GameLog.Error("Common", "ItemTipsView 无法打开：UI Popup 层未就绪");
                return false;
            }

            _moduleRoot = await ResManager.InstantiateAsync(
                GameResPath.GetUIPrefab("common", "CommonModule"), parent);
            if (_moduleRoot == null)
            {
                GameLog.Error("Common", "ItemTipsView 无法打开：CommonModule addressable 加载失败");
                return false;
            }
            _moduleRoot.name = "CommonModule(ItemTips)";
            _goodsView = _moduleRoot.GetComponentInChildren<GoodsTooltipsBind>(true);
            _equipView = _moduleRoot.GetComponentInChildren<EquipToolTipsBind>(true);
            if (_goodsView == null || _equipView == null)
            {
                GameLog.Error("Common", "CommonModule 缺 GoodsTooltipsBind/EquipToolTipsBind");
                ResManager.ReleaseInstance(_moduleRoot);
                _moduleRoot = null;
                _goodsView = null;
                _equipView = null;
                return false;
            }
            _goodsView.gameObject.SetActive(false);
            _equipView.gameObject.SetActive(false);
            return true;
        }

        private static bool ActivatePrefabView(bool equip)
        {
            if (_activeView != null) _activeView.Hide();
            if (equip)
            {
                _activeView = _equipView;
                _nameText = _equipView.equip_name;
                _bodyText = _equipView.basePro;
                _iconSlot = _equipView.icon;
                _useBtn = null;
                _wearBtn = _equipView.replaceBtn != null ? _equipView.replaceBtn.gameObject : null;

                SetActive(_equipView.best_conta, false);
                SetActive(_equipView.refine, false);
                SetActive(_equipView.refine_pro_conta, false);
                SetActive(_equipView.spec_conta, false);
                SetActive(_equipView.god_conta, false);
                SetActive(_equipView.stone_conta, false);
                SetActive(_equipView.wash_conta, false);
                SetActive(_equipView.smelt_conta, false);
                SetActive(_equipView.suit_conta, false);
                SetActive(_equipView.suit_conta2, false);
                SetActive(_equipView.base_pro_conta, true);
                ClearText(_equipView.score, _equipView.level, _equipView.career,
                    _equipView.pos, _equipView.grade, _equipView.lb_level);
                _equipView.Show();
            }
            else
            {
                _activeView = _goodsView;
                _nameText = _goodsView.goods_name;
                _bodyText = _goodsView.intro;
                _iconSlot = _goodsView.item_group;
                _useBtn = _goodsView.useBtn != null ? _goodsView.useBtn.gameObject : null;
                _wearBtn = null;

                ClearText(_goodsView.type_text, _goodsView.quantity_text, _goodsView.level_text,
                    _goodsView.level_text_value, _goodsView.tips, _goodsView.ways);
                SetActive(_goodsView.rewardsList, false);
                SetActive(_goodsView.sourceGp, false);
                SetActive(_goodsView._gp_cooling, false);
                _goodsView.Show();
            }
            return _activeView != null && _nameText != null && _bodyText != null && _iconSlot != null;
        }

        private static void ClearText(params TextMeshProUGUI[] texts)
        {
            foreach (TextMeshProUGUI text in texts)
                if (text != null) text.text = "";
        }

        private static void ConfigurePrefabButtons(bool equip, bool useVisible, bool wearVisible,
            bool moveVisible, ItemContext context)
        {
            if (equip)
            {
                RectTransform move = context == ItemContext.WarehouseStorage
                    ? _equipView.takeoutBtn : _equipView.depositBtn;
                _moveBtn = moveVisible && move != null ? move.gameObject : null;
                _moveLabel = _moveBtn != null ? _moveBtn.GetComponentInChildren<TextMeshProUGUI>(true) : null;
                SetActive(_equipView.replaceBtn, wearVisible);
                SetActive(_equipView.closeBtn, true);
                SetActive(_equipView.depositBtn, moveVisible && context == ItemContext.WarehouseBag);
                SetActive(_equipView.takeoutBtn, moveVisible && context == ItemContext.WarehouseStorage);
                SetActive(_equipView.donateBtn, false);
                SetActive(_equipView.destroyBtn, false);
                SetActive(_equipView.exchangeBtn, false);
                SetActive(_equipView.UninstallBtn, false);
                SetActive(_equipView.upShelfBtn, false);
                SetActive(_equipView.outShelfBtn, false);
                SetActive(_equipView.UninstallBtn_spirit, false);
                SetActive(_equipView.sellBtn, false);
                SetActive(_equipView.treasureReceiveBtn, false);
                SetActive(_equipView.guild_conta, false);

                BindUnique(_equipView.replaceBtn, wearVisible ? OnWearClick : null);
                BindUnique(_equipView.closeBtn, Close);
                BindUnique(_equipView.depositBtn,
                    moveVisible && context == ItemContext.WarehouseBag ? OnMoveClick : null);
                BindUnique(_equipView.takeoutBtn,
                    moveVisible && context == ItemContext.WarehouseStorage ? OnMoveClick : null);
                return;
            }

            ConfigureGoodsButtons(useVisible, moveVisible, context);
        }

        private static void ConfigureGoodsButtons(bool useVisible, bool moveVisible, ItemContext context)
        {
            RectTransform move = context == ItemContext.WarehouseStorage
                ? _goodsView.takeoutBtn : _goodsView.depositBtn;
            _moveBtn = moveVisible && move != null ? move.gameObject : null;
            _moveLabel = _moveBtn != null ? _moveBtn.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            SetActive(_goodsView.useBtn, useVisible);
            SetActive(_goodsView.okBtn, true);
            SetActive(_goodsView.depositBtn, moveVisible && context == ItemContext.WarehouseBag);
            SetActive(_goodsView.takeoutBtn, moveVisible && context == ItemContext.WarehouseStorage);
            SetActive(_goodsView.sellBtn, false);
            SetActive(_goodsView.upShelfBtn, false);
            SetActive(_goodsView.outShelfBtn, false);
            SetActive(_goodsView.treasureReceiveBtn, false);
            SetActive(_goodsView.putBtn, false);

            BindUnique(_goodsView.useBtn, useVisible ? OnUseClick : null);
            BindUnique(_goodsView.okBtn, Close);
            BindUnique(_goodsView.depositBtn,
                moveVisible && context == ItemContext.WarehouseBag ? OnMoveClick : null);
            BindUnique(_goodsView.takeoutBtn,
                moveVisible && context == ItemContext.WarehouseStorage ? OnMoveClick : null);
        }

        private static void BindUnique(Component target, System.Action action)
        {
            if (target == null) return;
            GameObject go = target.gameObject;
            foreach (Graphic graphic in go.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            Image image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
            }
            image.raycastTarget = action != null;
            UIUtil.ClearClicks(image);
            if (action != null) UIUtil.AddClick(image, action);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }

    }
}
