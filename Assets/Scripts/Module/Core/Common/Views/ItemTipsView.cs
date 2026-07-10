using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 物品详情 tips —— 临时原生 uGUI 壳(TEMP SHELL),对标老端 common/UIToolTipMgr.DefaultAppendTips →
    /// GoodsTooltips(普通物品)/ EquipToolTips(装备 type==10)。
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
    /// 老端 GoodsTooltips.lh/EquipToolTips.lh 无 Unity 转换产物,故按任务包许可做最小原生壳(同 TaskFinishView TEMP 壳约定);
    /// 字体复用场景中已打开文本的 TMP 字体(含中文字形)。
    /// </summary>
    public static class ItemTipsView
    {
        private static GameObject _root;
        private static TextMeshProUGUI _nameText;
        private static TextMeshProUGUI _bodyText;
        private static RectTransform _iconSlot;
        private static GameObject _iconCell;
        private static GameObject _useBtn;
        private static GameObject _wearBtn;
        private static RectTransform _closeRt;
        private static Bag.BagGoods _goods;   // 当前实例(使用按钮用;无实例=纯 config 展示,无按钮)
        private static int _epoch;

        private static TMP_FontAsset _font;
        private static Material _fontMat;

        /// <summary>弹物品详情(对标 UIToolTipMgr.DefaultAppendTips):typeId 不在 config_goods 则不弹(对标 if(!basic) return)。
        /// num=堆叠数量(对标 GoodsTooltips quantity_text,由格子透传;默认 1)。无实例 → 装备走 config 极品预览/基础属性。</summary>
        public static void Show(int typeId, long num = 1) => ShowInternal(typeId, num, null);

        /// <summary>弹装备实例详情(对标 EquipToolTips.SetData(goods_vo)):带 <see cref="Bag.BagGoods"/> 实例 →
        /// 极品 equip_extra_attr / 强化 stren 实例属性行(缺活服实例字段则回落 config 极品预览);typeId/数量取自实例。</summary>
        public static void Show(Bag.BagGoods goods)
        {
            if (goods == null) return;
            ShowInternal(goods.TypeId, goods.GoodsNum, goods);
        }

        private static void ShowInternal(int typeId, long num, Bag.BagGoods goods)
        {
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
            if (basic == null)
            {
                GameLog.Warn("Common", "ItemTips: typeId={0} 不在 config_goods(或未加载)→ 不弹详情", typeId);
                return;
            }

            EnsureBuilt();
            if (_root == null) return;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();

            _goods = goods;
            // 使用按钮:仅背包实例 + config use!=0(对标 GoodsTooltips useBtn 隐藏条件 basic.use==0;
            // isTreasure/takeout/deposite/put/isSoul 等特殊容器态未移植,普通背包物品恒 false)。
            bool useVisible = goods != null && basic.Use != 0;
            // 穿戴按钮(薄增量六件套第20轮):仅背包实例 + 装备类物品(IsEquip),发 15201。
            bool wearVisible = goods != null && GoodsModel.IsEquip(typeId);
            if (_useBtn != null) _useBtn.SetActive(useVisible);
            if (_wearBtn != null) _wearBtn.SetActive(wearVisible);
            LayoutButtons(useVisible, wearVisible);

            _nameText.text = string.IsNullOrEmpty(basic.Name) ? ("#" + typeId) : basic.Name;
            _bodyText.text = BuildBody(typeId, num, basic, goods);

            int epoch = ++_epoch;   // 本次打开的竞态令牌:图标异步加载 + 详情回包共用,任何后续 Show/Close 都会使其失效
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
            if (_iconCell != null) { ResManager.ReleaseInstance(_iconCell); _iconCell = null; }
            if (_root != null) _root.SetActive(false);
        }

        /// <summary>
        /// 使用按钮点击(对标 GoodsTooltips useBtn_fun → CheckSecondView() + Close):
        /// 老端按 type/subtype 分流到各专属界面(礼包选择/经验符比较/藏宝图…),这些界面未移植 → 明确提示不移植假发协议;
        /// 普通可用物品走默认分支:数量 1 直接发 15050,多个则确认后先用 1 个(BatchUseView 未移植)。
        /// </summary>
        private static void OnUseClick()
        {
            Bag.BagGoods goods = _goods;
            if (goods == null) return;
            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goods.TypeId);
            if (basic == null) return;

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
                // 老端多个走 BatchUseView(批量选数界面,未移植)→ 确认后先用 1 个,不臆造批量行为。
                TipsManager.Confirm("批量使用界面未移植,先使用 1 个?", () =>
                {
                    Bag.BagController.Instance.UseGoods(goods.GoodsId, 1);
                    Close();
                });
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

        /// <summary>按钮布局(使用/穿戴/关闭 最多三按钮一排;仅关闭时居中,二按钮双档位(200 宽),
        /// 三按钮收窄间距均分(140 宽,避免 480 宽面板溢出)。</summary>
        private static void LayoutButtons(bool useVisible, bool wearVisible)
        {
            int visibleCount = (useVisible ? 1 : 0) + (wearVisible ? 1 : 0) + 1;   // +1 = 关闭恒显示
            if (visibleCount >= 3)
            {
                // 三按钮:收窄宽度 + 收窄间距均分(使用 / 穿戴 / 关闭,从左到右)。
                SetBtnRect(_useBtn, -150f, 140f);
                SetBtnRect(_wearBtn, 0f, 140f);
                SetBtnRect(_closeRt, 150f, 140f);
            }
            else if (visibleCount == 2)
            {
                // 双按钮:使用/穿戴其一 + 关闭,原有使用/关闭双档位布局(200 宽)。
                float otherX = -110f;
                SetBtnRect(_useBtn, otherX, 200f);
                SetBtnRect(_wearBtn, otherX, 200f);
                SetBtnRect(_closeRt, 110f, 200f);
            }
            else
            {
                // 仅关闭:居中(200 宽)。
                SetBtnRect(_closeRt, 0f, 200f);
            }
        }

        private static void SetBtnRect(GameObject go, float x, float width)
        {
            if (go == null) return;
            SetBtnRect((RectTransform)go.transform, x, width);
        }

        private static void SetBtnRect(RectTransform rt, float x, float width)
        {
            if (rt == null) return;
            rt.anchoredPosition = new Vector2(x, 18f);
            rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
        }

        /// <summary>老端 CheckSecondView 专属界面分支表:命中返回目标界面名(未移植),null=可走默认 15050 分支。</summary>
        private static string UseBranchBlocker(GoodsModel.GoodsBasic b)
        {
            if (b.Type == 34) return "SelectGiftView(礼包选择)";
            if (b.Type == 37 && b.Subtype == 2) return "经验符 buff 比较流程";
            if (b.Type == 38 && b.Subtype == 6) return "OpenFun 35(定时宝箱)";
            if (b.Type == 38 && b.Subtype == 10) return "MarriageFlowerView(婚礼鲜花)";
            if (b.Type == 38 && b.Subtype == 36) return "MaskUseView(蒙面人道具)";
            if (b.Type == 38 && b.Subtype == 39) return "TransferJobCardView(转职卡)";
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

        // ===================== 构建(代码建 uGUI,居中弹层;同 TaskFinishView TEMP 壳)=====================

        private static void EnsureBuilt()
        {
            if (_root != null) return;
            Transform parent = ViewManager.GetLayer(UILayer.Popup);
            if (parent == null)
            {
                GameLog.Error("Common", "ItemTipsView 无法构建:UI Popup 层未就绪");
                return;
            }

            _root = NewRect("ItemTipsView(TempShell)", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject bg = NewRect("Backdrop", _root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.45f);
            bgImg.raycastTarget = true;
            UIUtil.AddClick(bgImg, Close);

            GameObject panel = NewRect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var panelRt = (RectTransform)panel.transform;
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            // 装备 tips 内容较长(基础+极品预览+专有+blocker),面板加高避免正文压到关闭按钮(TEMP 壳无滚动)。
            panelRt.sizeDelta = new Vector2(480f, 820f);
            panelRt.anchoredPosition = Vector2.zero;
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.07f, 0.09f, 0.14f, 0.97f);

            // 物品名(顶部居中)
            _nameText = NewText("Name", panel.transform, 30, TextAlignmentOptions.Top);
            var nameRt = _nameText.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 1f); nameRt.anchorMax = new Vector2(1f, 1f); nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0f, -20f); nameRt.sizeDelta = new Vector2(-40f, 44f);
            _nameText.color = new Color(1f, 0.86f, 0.45f);
            _nameText.fontStyle = FontStyles.Bold;

            // 图标位(品质底板 + 真实图标,复用 BaseAwardItem,127px)
            GameObject slot = NewRect("IconSlot", panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            _iconSlot = (RectTransform)slot.transform;
            _iconSlot.pivot = new Vector2(0.5f, 1f);
            _iconSlot.sizeDelta = new Vector2(127f, 127f);
            _iconSlot.anchoredPosition = new Vector2(0f, -76f);

            // 正文(类型/数量 + 装备属性 或 描述 + 获取途径)
            _bodyText = NewText("Body", panel.transform, 22, TextAlignmentOptions.TopLeft);
            var bodyRt = _bodyText.rectTransform;
            bodyRt.anchorMin = new Vector2(0f, 0f); bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(28f, 88f); bodyRt.offsetMax = new Vector2(-28f, -218f);
            _bodyText.textWrappingMode = TextWrappingModes.Normal;
            _bodyText.color = new Color(0.86f, 0.91f, 1f);

            // 使用按钮(底部左;仅背包实例且 config use!=0 时显示,对标 GoodsTooltips useBtn)
            _useBtn = NewRect("Use", panel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            var useRt = (RectTransform)_useBtn.transform;
            useRt.pivot = new Vector2(0.5f, 0f);
            useRt.sizeDelta = new Vector2(200f, 56f);
            useRt.anchoredPosition = new Vector2(-110f, 18f);
            Image useImg = _useBtn.AddComponent<Image>();
            useImg.color = new Color(0.22f, 0.42f, 0.24f, 1f);
            TextMeshProUGUI useLbl = NewText("Label", _useBtn.transform, 26, TextAlignmentOptions.Center);
            var ulRt = useLbl.rectTransform;
            ulRt.anchorMin = Vector2.zero; ulRt.anchorMax = Vector2.one; ulRt.offsetMin = Vector2.zero; ulRt.offsetMax = Vector2.zero;
            useLbl.text = "使用";
            useLbl.color = Color.white;
            UIUtil.AddClick(useImg, OnUseClick);
            _useBtn.SetActive(false);

            // 穿戴按钮(薄增量六件套第20轮;仅背包实例且装备类物品时显示,对标使用按钮同款布局)
            _wearBtn = NewRect("Wear", panel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            var wearRt = (RectTransform)_wearBtn.transform;
            wearRt.pivot = new Vector2(0.5f, 0f);
            wearRt.sizeDelta = new Vector2(200f, 56f);
            wearRt.anchoredPosition = new Vector2(-110f, 18f);
            Image wearImg = _wearBtn.AddComponent<Image>();
            wearImg.color = new Color(0.42f, 0.33f, 0.18f, 1f);
            TextMeshProUGUI wearLbl = NewText("Label", _wearBtn.transform, 26, TextAlignmentOptions.Center);
            var wlRt = wearLbl.rectTransform;
            wlRt.anchorMin = Vector2.zero; wlRt.anchorMax = Vector2.one; wlRt.offsetMin = Vector2.zero; wlRt.offsetMax = Vector2.zero;
            wearLbl.text = "穿戴";
            wearLbl.color = Color.white;
            UIUtil.AddClick(wearImg, OnWearClick);
            _wearBtn.SetActive(false);

            // 关闭按钮(底部;使用/穿戴按钮可见时让位,三按钮同显时收窄间距,见 LayoutButtons)
            GameObject closeBtn = NewRect("Close", panel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            _closeRt = (RectTransform)closeBtn.transform;
            _closeRt.pivot = new Vector2(0.5f, 0f);
            _closeRt.sizeDelta = new Vector2(200f, 56f);
            _closeRt.anchoredPosition = new Vector2(0f, 18f);
            Image closeImg = closeBtn.AddComponent<Image>();
            closeImg.color = new Color(0.20f, 0.30f, 0.48f, 1f);
            TextMeshProUGUI closeLbl = NewText("Label", closeBtn.transform, 26, TextAlignmentOptions.Center);
            var clRt = closeLbl.rectTransform;
            clRt.anchorMin = Vector2.zero; clRt.anchorMax = Vector2.one; clRt.offsetMin = Vector2.zero; clRt.offsetMax = Vector2.zero;
            closeLbl.text = "关闭";
            closeLbl.color = Color.white;
            UIUtil.AddClick(closeImg, Close);
        }

        // ---- uGUI 构建小工具(同 TaskFinishView 的 TEMP 壳约定)----

        private static GameObject NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = offMin; rt.offsetMax = offMax;
            return go;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.alignment = align;
            t.richText = true;
            ApplyFont(t);
            return t;
        }

        private static void ApplyFont(TextMeshProUGUI t)
        {
            if (_font == null)
            {
                TextMeshProUGUI src = Object.FindAnyObjectByType<TextMeshProUGUI>();
                if (src != null) { _font = src.font; _fontMat = src.fontSharedMaterial; }
            }
            if (_font != null) t.font = _font;
            if (_fontMat != null) t.fontSharedMaterial = _fontMat;
        }
    }
}
