using System.Collections.Generic;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Shop
{
    /// <summary>
    /// 商店数据层(自动循环 轮11;对标老端 commonModel/ShopModel.ts)。承载 15301(常规商城 18 类型)/
    /// 15305-15307(神秘·神纹商店)/64000-64003(抢购商城)三条独立数据线,以及跨号复用的
    /// MoneyIsEnough(货币是否足够,老端也是直接在 Model 里查 RoleModel/BagModel,非 Controller)。
    ///
    /// **命名陷阱订正存档**(spec §0):15301 的 "SoldOut" 字段真实语义=已购次数(UsedTime),非售罄布尔;
    /// 15305 的 "BuyType" 字段真实语义=购买状态(1未买/2已买),非货币类型——本类字段按真义命名
    /// (<see cref="GoodsVo.SoldOut"/> 保留老端字段名但注释澄清;<see cref="MysteryGoodVo.BuyType"/> 同理)。
    /// </summary>
    public sealed class ShopModel
    {
        public static readonly ShopModel Instance = new ShopModel();
        private ShopModel() { }

        // ===== ShopType(对标老端 commonModel/ShopModel.ts ShopType 枚举,数值与老端一致,逐条注释存档) =====
        public const int TYPE_LIMIT = 1;          // 限购
        public const int TYPE_DIAMOND = 2;        // 灵玉(老端字段名 Diamond,文案已订正,规格 §0)
        public const int TYPE_BIND_DIAMOND = 3;   // 绑玉
        public const int TYPE_OUTWARD = 4;        // 外观——半死:tabStrList 入口已注释,不建 UI,仅保留枚举值供 GAME_START 复刻查询(规格§0 跳过)
        public const int TYPE_NORMAL_SHOP = 5;    // 常用道具
        public const int TYPE_HONER = 6;          // 荣耀
        public const int TYPE_SACRED_SHOP = 7;    // 领地商城
        public const int TYPE_MEDAL_SHOP = 8;     // 神陨禁区(勋章商城)
        public const int TYPE_EXPLOIT_SHOP = 9;   // 功勋——半死:tabStrList 入口已注释,不建 UI(规格§0 跳过)
        public const int TYPE_TOPVIP_SHOP = 10;   // 至尊VIP商城——老端整包劫持转发独立 TopVipModel,不进本类主表
        public const int TYPE_EUDAEMON_SHOP = 11; // 圣兽领悬赏商城
        public const int TYPE_SINGLE_RANK = 12;   // 天境(跨服单人排位)
        public const int TYPE_LUCKY = 13;         // 幸运——半死:tabStrList 入口已注释,不建 UI(规格§0 跳过)
        public const int TYPE_LONGLANG_EX = 14;   // 九天神祭(老端实际渲染类是 LonglangExchangeView,非本类;Unity 暂共享 ShopCommonView,见 r11_unity #8 TODO)
        public const int TYPE_GOD_COURT = 15;     // 神霄御府
        public const int TYPE_GUILD = 16;         // 善缘(结社,唯一双货币商店)
        public const int TYPE_SOUL_OF_WAR = 17;   // 战魂(冲霄?不,战魂是 BossFieldSoulShopView 复用本类型的"套壳")
        public const int TYPE_GHOST_WALK = 18;    // 冲霄(百煞冲霄)
        public const int TYPE_VIE = 99;           // 抢购——不走 15301,独立 64000/64001 协议簇

        // ===== MysteryShopType(15305/15306/15307 专用,与上面 ShopType 是两套独立枚举,数值/含义均不通用) =====
        public const int MYSTERY_DEMON = 1; // 神秘商店(使魔)
        public const int MYSTERY_LUNG = 2;  // 神纹商店(龙纹,LungShopView 复用)

        // ===== ShopMoneyType(部分,业务实际用到的子集;对标老端同名枚举数值) =====
        public const int MONEY_DIAMOND = 1;
        public const int MONEY_BIND_DIAMOND = 2;
        public const int MONEY_GOLD = 3;
        public const int MONEY_HONER = 41;
        public const int MONEY_HONER2 = 42;
        public const int MONEY_HONER3 = 43;
        public const int MONEY_CSPVP = 36255024;
        public const int MONEY_KF_SINGLE_RANK = 36255094;
        public const int MONEY_LUCKY = 36255096;
        public const int MONEY_GUILD = 36255100;
        public const int MONEY_GUILD2 = 38040091;
        public const int MONEY_TOPVIP = 36255042;
        public const int MONEY_KF_HOLY_AREA = 36255012;
        public const int MONEY_GOD_CLIP = 36255099;
        public const int MONEY_GHOST_WALK = 36255115;

        /// <summary>钻石商城红点判定的硬编码 magic number(老端 special_id=2005)——照抄硬编码,不发明配置
        /// (规格§0:移植前已去 config_shop.json 核实,该 key_id 是灵玉商城里的一件常规商品,无独立含义)。</summary>
        public const int DIAMOND_RED_KEY_ID = 2005;

        /// <summary>服务器配置时区(线上=UTC+8)。64000 的 left_time 客户端自算"下一个游戏日0点"必须用这个,
        /// 不能裸 UTC/裸 DateTime.Now(轮10 血训,同 DailyModel.SERVER_ZONE_HOURS 先例)。
        /// 轮20收敛:转发 Shenxiao.Framework.Util.TimeUtil.SERVER_ZONE_HOURS(唯一事实源),值不变、
        /// 零行为变更,保留常量名/可见性避免改调用点(spec_serverclock_round20.md §2.3)。</summary>
        public const int SERVER_ZONE_HOURS = Shenxiao.Framework.Util.TimeUtil.SERVER_ZONE_HOURS;

        // =====================================================================================
        // 15301:常规商城(按 shop_type 分槽存)
        // =====================================================================================

        /// <summary>15301 单条商品(字段名对照 r11_server §字段序;SoldOut 真实语义=已购次数)。</summary>
        public sealed class GoodsVo
        {
            public int KeyId;
            public string SubtypeList = "";      // 原始 "%[1,2%]" 串(去包裹前)
            public readonly List<int> SeriesList = new List<int>(); // 去包裹+切分后的系列 id 列表(给 ShopSeriesTab 用)
            public int Rank;
            public int GoodsId;
            public int Num;
            public int MoneyType;
            public int Price;
            public int Discount;   // 折扣(100=无折扣,老端 discount/100 相乘算现价)
            public int QuotaType;  // 限购类型:0无限购/1每日/2每周/3终生
            public int QuotaNum;   // 限购上限(真实限购数;老端字段名 quota_num)
            public int SoldOut;    // ⚠字段名沿用老端(具误导性),真实语义=已购买次数(UsedTime),非售罄布尔
            public string Condition = ""; // Erlang term 购买条件串,如 "[{lv,120}]"
            public int TriggerTaskId;
            public int Bind;
            public int ShopType;   // 冗余存一份(对标老端 vo.type),方便跨表按 key_id 反查时知道归属
        }

        private readonly Dictionary<int, List<GoodsVo>> _allGoodsList = new Dictionary<int, List<GoodsVo>>();
        private static readonly List<GoodsVo> EmptyGoodsList = new List<GoodsVo>();

        /// <summary>type==TopVipShop(10) 劫持专槽:不进 <see cref="_allGoodsList"/> 主表。
        /// Unity TopVip 商城UI目前无 SetSupremeVipShopGoodsList 等价接收方，故本端只存槽待用；
        /// 不转发/不双注册 45102，因为45102已明确是TopVip技能任务全量，与15301商品结构无关。
        /// TODO:补至尊VIP商城 UI 后，把这个槽接过去。</summary>
        public List<GoodsVo> TopVipShopGoodsList { get; private set; } = new List<GoodsVo>();

        /// <summary>15301 落地(对标 SetShopData):按 shop_type 分槽存 + 按 Rank 升序排 + 解析 series_list。</summary>
        public void SetShopData(int shopType, List<GoodsVo> list)
        {
            list = list ?? new List<GoodsVo>();
            foreach (GoodsVo vo in list)
            {
                vo.ShopType = shopType;
                ParseSeriesList(vo);
            }
            list.Sort((a, b) => a.Rank.CompareTo(b.Rank));
            if (shopType == TYPE_TOPVIP_SHOP) TopVipShopGoodsList = list;
            else _allGoodsList[shopType] = list;
        }

        private static void ParseSeriesList(GoodsVo vo)
        {
            vo.SeriesList.Clear();
            if (string.IsNullOrEmpty(vo.SubtypeList)) return;
            // 对标老端:先去掉 Erlang 风格的 "%[" / "%]" 包裹再按逗号切分。
            string s = vo.SubtypeList.Replace("%[", "").Replace("%]", "");
            string[] parts = s.Split(',');
            foreach (string p in parts)
            {
                string trimmed = p.Trim();
                if (trimmed.Length > 0 && int.TryParse(trimmed, out int v)) vo.SeriesList.Add(v);
            }
        }

        public List<GoodsVo> GetShopDataByType(int shopType)
            => _allGoodsList.TryGetValue(shopType, out List<GoodsVo> l) ? l : EmptyGoodsList;

        public List<GoodsVo> GetShopDataByTypeAndSeriesId(int shopType, int seriesId)
        {
            var result = new List<GoodsVo>();
            foreach (GoodsVo vo in GetShopDataByType(shopType))
                if (vo.SeriesList.Contains(seriesId)) result.Add(vo);
            return result;
        }

        public bool HaveThisSeriesIdGoods(int shopType, int seriesId)
        {
            foreach (GoodsVo vo in GetShopDataByType(shopType))
                if (vo.SeriesList.Contains(seriesId)) return true;
            return false;
        }

        public GoodsVo GetShopDataByKeyId(int keyId)
        {
            foreach (KeyValuePair<int, List<GoodsVo>> kv in _allGoodsList)
                foreach (GoodsVo vo in kv.Value)
                    if (vo.KeyId == keyId) return vo;
            return null;
        }

        /// <summary>15302 购买成功:sold_out(已购次数)累加 num(对标 UpdateShopData)。
        /// 返回是否命中终生限购(quota_type==3)——命中时调用方(Controller)额外广播整表刷新事件,
        /// 驱动 View 层把已售罄的终生限购条目沉到列表底部重排(对标老端 InitShopContent 的 lifelongList 合并,
        /// 排序本身留在 View 层做,Model 只负责状态 + 事件语义)。</summary>
        public bool UpdateShopData(int keyId, int num)
        {
            GoodsVo vo = GetShopDataByKeyId(keyId);
            if (vo == null) return false;
            vo.SoldOut += num;
            return vo.QuotaType == 3;
        }

        // =====================================================================================
        // 钻石商城红点(硬编码 key_id==2005,对标老端 InitEvent 的 jin BindOne + Handler15301 特判)
        // =====================================================================================

        public bool DiamondRedStatus { get; private set; }
        /// <summary>是否已经进过一次钻石特殊 tab(对标老端 have_enter_special_tab,进过就不再重复点红点)。</summary>
        public bool HaveEnterSpecialTab { get; set; }
        private int _specialPrice;
        private int _specialSoldOut;

        /// <summary>15301(type==Diamond) 落地后调用:扫 key_id==2005 记下价格+已购次数(对标老端特判段)。</summary>
        public void CaptureDiamondSpecial(List<GoodsVo> diamondList)
        {
            _specialPrice = 0;
            _specialSoldOut = 0;
            foreach (GoodsVo vo in diamondList)
            {
                if (vo.KeyId != DIAMOND_RED_KEY_ID) continue;
                _specialPrice = vo.Price;
                _specialSoldOut = vo.SoldOut;
                break;
            }
            RecomputeDiamondRed();
        }

        /// <summary>货币变化(EVT_ROLE_INFO_UPDATE)时复判(对标老端 jin BindOne 回调)。</summary>
        public bool RecomputeDiamondRed()
        {
            bool haveMoney = RoleModel.Instance.Gold >= _specialPrice;
            bool notSoldOut = _specialSoldOut == 0;
            DiamondRedStatus = notSoldOut && haveMoney && !HaveEnterSpecialTab;
            return DiamondRedStatus;
        }

        // =====================================================================================
        // 15305/15306/15307:神秘/神纹商店
        // =====================================================================================

        public sealed class MysteryGoodVo
        {
            public int CfgId;
            public int Discount;
            public int Price;
            public int BuyType; // ⚠字段名沿用老端(具误导性),真实语义=购买状态:1未买/2已买(非货币类型)
            public int BuyNum;
        }

        public sealed class MysteryShopVo
        {
            public int Type;
            public long RefreshTime;
            public int HitNum;
            public List<MysteryGoodVo> GoodList = new List<MysteryGoodVo>();
        }

        private readonly Dictionary<int, MysteryShopVo> _mysteryData = new Dictionary<int, MysteryShopVo>();

        /// <summary>首次登录/刷新到点后是否该点亮活动图标红点(对标老端 all_new && type==MysteryShop 分支;
        /// 消费方未接线,先落状态,TODO)。</summary>
        public bool MysteryFirstAllNewRed { get; private set; }

        /// <summary>15305 落地(对标 SetMysteryShop):按 cfg_id 升序排;返回 hit_num 是否变化(controller 据此
        /// 广播"刷新特效"事件,对标老端 SHOW_REFRESH_EFFECT)。</summary>
        public bool SetMysteryShop(MysteryShopVo vo)
        {
            bool firstTime = !_mysteryData.ContainsKey(vo.Type);
            if (firstTime)
            {
                bool allNew = true;
                foreach (MysteryGoodVo g in vo.GoodList) if (g.BuyType == 2) { allNew = false; break; }
                if (allNew && vo.Type == MYSTERY_DEMON) MysteryFirstAllNewRed = true;
            }
            bool hitChanged = !firstTime && _mysteryData[vo.Type].HitNum != vo.HitNum;

            vo.GoodList.Sort((a, b) => a.CfgId.CompareTo(b.CfgId));
            _mysteryData[vo.Type] = vo;
            return hitChanged;
        }

        public MysteryShopVo GetMysteryDataByType(int type) => _mysteryData.TryGetValue(type, out MysteryShopVo v) ? v : null;

        public MysteryGoodVo GetMysteryDataById(int type, int cfgId)
        {
            if (!_mysteryData.TryGetValue(type, out MysteryShopVo vo)) return null;
            foreach (MysteryGoodVo g in vo.GoodList) if (g.CfgId == cfgId) return g;
            return null;
        }

        /// <summary>15307 购买成功:该 cfg_id 置已买 + 购买次数+1(对标 UpdateMysteryShop)。</summary>
        public void UpdateMysteryShop(int type, int cfgId)
        {
            if (!_mysteryData.TryGetValue(type, out MysteryShopVo vo)) return;
            foreach (MysteryGoodVo g in vo.GoodList)
            {
                if (g.CfgId != cfgId) continue;
                g.BuyType = 2;
                g.BuyNum += 1;
                break;
            }
        }

        // =====================================================================================
        // 64000-64003:抢购(限购)商城
        // =====================================================================================

        public sealed class VieGoodVo
        {
            public int Id;
            public int GoodId;
            public int DefaultNum;
            public int PriceType;
            public int OldPrice;
            public int NewPrice;
            public int TotalLimitNum;
            public int LeftLimitNum;
            public int DailyLimitNum;
            public int BuyNum;
        }

        public sealed class VieInfoVo
        {
            public List<VieGoodVo> IdList = new List<VieGoodVo>();
            /// <summary>客户端自算的"下一个游戏日0点"真实 unix 毫秒(对标老端 left_time;服务器墙钟,非裸 UTC)。</summary>
            public long LeftTimeMs;
        }

        private VieInfoVo _vieInfo;

        /// <summary>抢购红点(对标老端 vie_red_stutus):null=未判定(首次收到才判一次),之后是/否。</summary>
        public bool? VieRedStatus { get; set; }

        public VieInfoVo GetVieInfo() => _vieInfo;

        /// <summary>64000 落地(对标 SetVieInfo):按 id 升序排 + 首次收到时判红点(全部售罄才不亮)。</summary>
        public void SetVieInfo(VieInfoVo vo)
        {
            vo.IdList.Sort((a, b) => a.Id.CompareTo(b.Id));
            if (VieRedStatus == null)
            {
                bool showOut = true;
                foreach (VieGoodVo g in vo.IdList)
                {
                    bool over1 = g.BuyNum >= g.DailyLimitNum;
                    bool over2 = g.LeftLimitNum <= 0;
                    if (!(over1 || over2)) { showOut = false; break; }
                }
                VieRedStatus = vo.IdList.Count > 0 && !showOut;
            }
            _vieInfo = vo;
        }

        /// <summary>清空抢购缓存(对标老端 ShopController.ts:154-155 的 `model.SetVieInfo(null)` +
        /// `model.vie_red_stutus = null` 两行)。老端 SetVieInfo 可以直接吃 null,本端 <see cref="SetVieInfo"/>
        /// 入口就要对 vo.IdList 排序,传 null 会 NPE,故单开此方法承载"置空"语义,二者不可互相替代。
        /// 清 VieRedStatus 是关键:SetVieInfo 只在 VieRedStatus==null 时才重判红点,不清则 4 点后的
        /// 64000 回包会沿用昨天的红点结论。</summary>
        public void ClearVieInfo()
        {
            _vieInfo = null;
            VieRedStatus = null;
        }

        public bool CheckVieOpen() => _vieInfo != null && _vieInfo.IdList.Count > 0;

        public VieGoodVo GetVieGoodById(int id)
        {
            if (_vieInfo == null) return null;
            foreach (VieGoodVo g in _vieInfo.IdList) if (g.Id == id) return g;
            return null;
        }

        /// <summary>64001 购买成功:原地 patch(对标老端,不重拉整张 64000 列表)。</summary>
        public void PatchVieBuy(int id, int buyNum, int leftLimitNum)
        {
            VieGoodVo g = GetVieGoodById(id);
            if (g == null) return;
            g.BuyNum = buyNum;
            g.LeftLimitNum = leftLimitNum;
        }

        /// <summary>64002 库存广播:逐条 patch left_limit_num。</summary>
        public void ApplyVieChangeList(List<(int id, int leftLimitNum)> changes)
        {
            if (_vieInfo == null) return;
            foreach ((int id, int leftLimitNum) c in changes)
            {
                VieGoodVo g = GetVieGoodById(c.id);
                if (g != null) g.LeftLimitNum = c.leftLimitNum;
            }
        }

        /// <summary>64003 下架广播:⚠老端 vinfo.id_list.slice(i,1) 是 Array.slice 误当 splice 的假删除 bug
        /// (slice 不改原数组,老端这条广播从未真删过);本端按显然意图订正为真删(同轮10 rule10 先例)。</summary>
        public void RemoveVieIds(List<int> ids)
        {
            if (_vieInfo == null || ids == null || ids.Count == 0) return;
            _vieInfo.IdList.RemoveAll(g => ids.Contains(g.Id));
        }

        // =====================================================================================
        // 货币是否足够(对标老端 MoneyIsEnough——老端就是直接在 Model 里查 RoleManager/GoodsModel,非 Controller,
        // 本端保持同样的层级:View 层按钮/文案判定直接调本方法)
        // =====================================================================================

        /// <summary>返回(货币是否足够, 差额, 当前持有量)。老端还带 alert_str 文案表,本端未移植文案(TODO,
        /// View 层按需自定简单提示),只提供数值判定——这是本轮 Item 购买按钮态/价格变色的唯一依据。</summary>
        public (bool enough, int gap, long myHave) MoneyIsEnough(int moneyType, int cost)
        {
            long have;
            if (moneyType == MONEY_DIAMOND) have = RoleModel.Instance.Gold;
            else if (moneyType == MONEY_BIND_DIAMOND) have = RoleModel.Instance.BGold;
            else if (moneyType == MONEY_GOLD) have = RoleModel.Instance.Coin;
            else if (moneyType == MONEY_GUILD2) have = BagModel.Instance.GetTypeGoodsNum(MONEY_GUILD2);
            else have = BagModel.Instance.GetSpecialScore(moneyType);
            bool enough = have >= cost;
            int gap = enough ? 0 : cost - (int)have;
            return (enough, gap, have);
        }

        public void Clear()
        {
            _allGoodsList.Clear();
            TopVipShopGoodsList = new List<GoodsVo>();
            DiamondRedStatus = false;
            HaveEnterSpecialTab = false;
            _specialPrice = 0;
            _specialSoldOut = 0;
            _mysteryData.Clear();
            MysteryFirstAllNewRed = false;
            _vieInfo = null;
            VieRedStatus = null;
        }
    }
}
