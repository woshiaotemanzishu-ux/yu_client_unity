using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// Goods 协议扩容(自动循环 轮1):物品"动态"详情 + 若干小状态的独立缓存,对标老端 GoodsModel.ts 的
    /// dynamic_goods_dic_ / goods_exchange_dic_ / _CoolingGoodData_ / goods_buff_list 等散装字段。
    /// 刻意与 <see cref="BagModel"/>(背包槽位权威)和 <see cref="Common.GoodsModel"/>(纯配置查询)分开:
    /// 这里全是"服务端按需下发的实例态/小状态",不是槽位数据也不是配置表。
    /// 各小类均为独立 singleton(同 BagModel/RushGiftModel 等既有风格),BagController 的 On1500x handler
    /// 解出 VO 后调对应类的 Store/SetXxx 落地;GlobalEvent 广播仍统一在 BagController 里发(与既有分工一致)。
    /// </summary>

    // ===================== 15000/15001 物品详情 =====================

    /// <summary>物品详情(15000 全量字段;15001 为他人详情,复用同一结构 —— 老端 15001 缺 stren_exp/wash_rating,
    /// 此处置 0,并额外置 <see cref="OwnerRoleId"/>(15000/自己走 0,15001 记录被查角色 id)以便区分数据来源。</summary>
    public sealed class GoodsDetailVo
    {
        public long OwnerRoleId;      // 15001 独有 player_id:l;15000(自己)恒 0
        public long GoodsId;          // goods_id:l
        public int TypeId;            // type_id:i
        public int SubPos;            // sub_pos:c
        public int Cell;              // cell:h
        public long Num;              // num:i
        public int Bind;              // bind:c
        public int Trade;             // trade:c
        public int Sell;              // sell:c
        public int Color;             // color:c
        public long ExpireTime;       // expire_time:i
        public long CombatPower;      // combat_power:i
        public int EquipType;         // equip_type:c
        public int PriceType;         // price_type:c
        public long SellPrice;        // sell_price:i
        public int Stren;             // stren:h
        public long StrenExp;         // stren_exp:i(15001 无此字段,置0)
        public long Rating;           // rating:i
        public long OverallRating;    // overall_rating:i
        public int Division;          // division:c
        public long WashRating;       // wash_rating:i(15001 无此字段,置0)
        public List<EquipAdditionAttr> AdditionAttrs;   // addition_attrlist[]
        public List<GoodsStoneSlot> StoneList;          // stone_list[]
        public List<GoodsMagicSlot> MagicList;          // magic_list[]
        public List<EquipExtraAttr> ExtraAttrs;         // equip_extra_attr[]
        public List<GoodsWashAttr> WashAttrs;           // wash_attr[]
        public List<GoodsSuitInfo> SuitList;            // suit_list[]
        public int CspiritStage;       // cspirit_stage:h
        public int CspiritLv;          // cspirit_lv:h
        public int AwakeningLv;        // awakening_lv:c
        public int EquipSkillId;       // equip_skill_id:i
        public int EquipSkillLv;       // equip_skill_lv:c
        public int MountEquipSkillId;  // mount_equip_skill_id:i
        public int MountEquipSkillLv;  // mount_equip_skill_lv:c
        public int PetEquipStage;      // pet_equip_stage:h
        public int PetEquipStar;       // pet_equip_star:h
        public int Level;              // level:h
        public List<EquipAwakeAttr> AwakeList;          // awake_list[]
        public int RefinementLv;       // refinement_lv:h
    }

    /// <summary>宝石孔位(stone_list 单项)。</summary>
    public struct GoodsStoneSlot { public int Pos; public int TypeId; }

    /// <summary>附魔(magic_list 单项;注意 goods_id 此处是 u32,指附魔石配置 id,非物品实例 id)。</summary>
    public struct GoodsMagicSlot { public int GoodsId; public long EndTime; }

    /// <summary>洗炼属性(wash_attr 单项)。</summary>
    public struct GoodsWashAttr { public int Index; public int Color; public int AttrId; public long AttrVal; }

    /// <summary>套装态(suit_list 单项)。</summary>
    public struct GoodsSuitInfo { public int SuitLv; public int SuitSlv; public int SuitCount; }

    /// <summary>
    /// 物品详情缓存 + 请求节流 + 一次性回调(对标老端 GoodsModel.GetDynamic/AddDynamic):
    /// 有缓存直接回调;无缓存则按 goodsId 3 秒节流发 15000/15001,回包到达时对该 goodsId 的等待者逐一回调并清空。
    /// </summary>
    public sealed class GoodsDynamicModel
    {
        public static readonly GoodsDynamicModel Instance = new GoodsDynamicModel();
        private GoodsDynamicModel() { }

        private const float ThrottleSec = 3f;   // 对标老端 GetDynamic: Status.NowTime - last_request_time >= 3

        private readonly Dictionary<long, GoodsDetailVo> _cache = new Dictionary<long, GoodsDetailVo>();
        private readonly Dictionary<long, float> _lastRequestAt = new Dictionary<long, float>();
        private readonly Dictionary<long, List<Action<GoodsDetailVo>>> _pending = new Dictionary<long, List<Action<GoodsDetailVo>>>();

        // 15083 礼包等级信息:一次性回调(仿老端 gift_bag_dynamic_call_back),按 goodsId 分槽避免互相覆盖。
        private readonly Dictionary<long, Action<GiftLevelInfo>> _giftLevelCallbacks = new Dictionary<long, Action<GiftLevelInfo>>();

        /// <summary>取当前缓存(不触发请求;详情段渲染完成后如 ItemTipsView 已关闭可用此避免重复弹)。</summary>
        public GoodsDetailVo Peek(long goodsId) => _cache.TryGetValue(goodsId, out GoodsDetailVo v) ? v : null;

        /// <summary>请求自己物品详情(15000 "l" goodsId)。已有缓存 → 直接回调(对标 GetDynamic vo&amp;&amp;!needRequest 分支,
        /// 不管节流窗口);无缓存 → 3 秒节流发送,回调注册等待 On15000 送达。</summary>
        public void RequestDetail(long goodsId, Action<GoodsDetailVo> callback = null)
        {
            if (goodsId <= 0) return;
            if (_cache.TryGetValue(goodsId, out GoodsDetailVo cached))
            {
                callback?.Invoke(cached);
                return;
            }
            RegisterPending(goodsId, callback);
            float now = Time.realtimeSinceStartup;
            if (!_lastRequestAt.TryGetValue(goodsId, out float last) || now - last >= ThrottleSec)
            {
                _lastRequestAt[goodsId] = now;
                NetManager.SendFmt(Proto.GOODS_DETAIL, "l", goodsId);
                GameLog.Info("Bag", "request 15000 goods_id={0}(GoodsDynamicModel.RequestDetail)", goodsId);
            }
            else
            {
                GameLog.Info("Bag", "15000 goods_id={0} 3秒节流内跳过重发(对标 GetDynamic last_request_time)", goodsId);
            }
        }

        /// <summary>请求他人物品详情(15001 "ll" roleId,goodsId;对标 GoodsController OnRequestDynamic role_id 分支)。
        /// 同样走缓存/节流/一次性回调,但缓存与自己物品共用同一字典(goodsId 全局唯一实例键,理论不冲突)。</summary>
        public void RequestOthersDetail(long roleId, long goodsId, Action<GoodsDetailVo> callback = null)
        {
            if (goodsId <= 0 || roleId <= 0) return;
            if (_cache.TryGetValue(goodsId, out GoodsDetailVo cached))
            {
                callback?.Invoke(cached);
                return;
            }
            RegisterPending(goodsId, callback);
            float now = Time.realtimeSinceStartup;
            if (!_lastRequestAt.TryGetValue(goodsId, out float last) || now - last >= ThrottleSec)
            {
                _lastRequestAt[goodsId] = now;
                NetManager.SendFmt(Proto.GOODS_DETAIL_OTHERS, "ll", roleId, goodsId);
                GameLog.Info("Bag", "request 15001 role_id={0} goods_id={1}(GoodsDynamicModel.RequestOthersDetail)", roleId, goodsId);
            }
        }

        private void RegisterPending(long goodsId, Action<GoodsDetailVo> callback)
        {
            if (callback == null) return;
            if (!_pending.TryGetValue(goodsId, out List<Action<GoodsDetailVo>> list))
            {
                list = new List<Action<GoodsDetailVo>>();
                _pending[goodsId] = list;
            }
            list.Add(callback);
        }

        /// <summary>15000/15001 回包落地(BagController 调用):写缓存 + 触发并清空该 goodsId 的等待回调。</summary>
        public void Store(GoodsDetailVo vo)
        {
            if (vo == null) return;
            _cache[vo.GoodsId] = vo;
            if (_pending.TryGetValue(vo.GoodsId, out List<Action<GoodsDetailVo>> list))
            {
                _pending.Remove(vo.GoodsId);
                foreach (Action<GoodsDetailVo> cb in list) cb?.Invoke(vo);
            }
        }

        /// <summary>
        /// 补口子(自动循环 轮4 队列#4;装备成长四件套):清缓存 + 绕过节流窗口,强制下次 RequestDetail 真实重拉。
        /// 用于协议回包"没给出新值,只知道详情变了"的场景(如 15213 洗魄只给变动下标不给新属性值、15252 升段不给
        /// 新段位),对标老端 GoodsModel.GetDynamic(goodsId, cb, <b>true</b>) 的强制重拉参数。
        /// </summary>
        public void Invalidate(long goodsId)
        {
            _cache.Remove(goodsId);
            _lastRequestAt.Remove(goodsId);
        }

        /// <summary>
        /// 补口子(自动循环 轮4 队列#4):就地改写已缓存详情的部分字段,不触发网络请求(对标老端 15255 on 成功后
        /// 直接 <c>vo.refinement_lv = scmd.refine_lv</c>,协议回包本身已带够新值时用这个,比 Invalidate 更快)。
        /// goodsId 尚未被 15000/15001 缓存过时静默跳过——无本地对象可改,等下次 RequestDetail 会带最新值。
        /// </summary>
        public void Patch(long goodsId, Action<GoodsDetailVo> mutate)
        {
            if (mutate == null) return;
            if (_cache.TryGetValue(goodsId, out GoodsDetailVo vo) && vo != null)
            {
                mutate(vo);
            }
        }

        /// <summary>请求礼包等级信息(15083 "li" goodsId,typeId);对标老端 GetGiftBagDynamic 注册单槽回调。</summary>
        public void RequestGiftLevel(long goodsId, int typeId, Action<GiftLevelInfo> callback = null)
        {
            if (goodsId <= 0) return;
            if (callback != null) _giftLevelCallbacks[goodsId] = callback;
            NetManager.SendFmt(Proto.GIFT_LEVEL_INFO, "li", goodsId, typeId);
            GameLog.Info("Bag", "request 15083 goods_id={0} type_id={1}", goodsId, typeId);
        }

        /// <summary>15083 回包落地(BagController 调用):触发并清空对应 goodsId 的一次性回调。</summary>
        public void DeliverGiftLevel(GiftLevelInfo vo)
        {
            if (_giftLevelCallbacks.TryGetValue(vo.GoodsId, out Action<GiftLevelInfo> cb))
            {
                _giftLevelCallbacks.Remove(vo.GoodsId);
                cb?.Invoke(vo);
            }
        }

        /// <summary>断线/登出清空(对标各 dic 在 ReSetDynamic 附近的重置语义)。</summary>
        public void Clear()
        {
            _cache.Clear();
            _lastRequestAt.Clear();
            _pending.Clear();
            _giftLevelCallbacks.Clear();
        }
    }

    // ===================== 15083 礼包等级信息 =====================

    /// <summary>礼包等级信息(15083 recv 全字段)。</summary>
    public struct GiftLevelInfo { public long GoodsId; public int TypeId; public int GiftLevel; }

    // ===================== 15026 物品兑换列表 =====================

    /// <summary>兑换列表单项(exchange_list,15026)。</summary>
    public struct GoodsExchangeEntry { public int Id; public int Count; public int CanExchange; }

    /// <summary>
    /// 兑换列表分桶(对标老端 goods_exchange_dic_:type → exchange_list)。跨系统共享通道(伙伴商店/龙语/
    /// 跨服1v1 等 type 值各异),本类只做通用存取,不绑定具体玩法语义。
    /// </summary>
    public sealed class GoodsExchangeModel
    {
        public static readonly GoodsExchangeModel Instance = new GoodsExchangeModel();
        private GoodsExchangeModel() { }

        private readonly Dictionary<int, List<GoodsExchangeEntry>> _buckets = new Dictionary<int, List<GoodsExchangeEntry>>();

        /// <summary>请求某类型兑换列表(15026 "h" exchangeType)。</summary>
        public void RequestList(int exchangeType)
        {
            NetManager.SendFmt(Proto.GOODS_EXCHANGE_LIST, "h", exchangeType);
            GameLog.Info("Bag", "request 15026 exchange_type={0}", exchangeType);
        }

        /// <summary>写入某类型全量列表(BagController 已按 id 升序排好序,对标老端 On15026 table.sort)。</summary>
        public void SetList(int type, List<GoodsExchangeEntry> sortedList) => _buckets[type] = sortedList;

        public List<GoodsExchangeEntry> GetList(int type) =>
            _buckets.TryGetValue(type, out List<GoodsExchangeEntry> list) ? list : null;

        public void Clear() => _buckets.Clear();
    }

    // ===================== 15084 次数礼包冷却 =====================

    /// <summary>次数礼包冷却态(15084 recv)。</summary>
    public struct GoodsCoolingInfo { public int UseCount; public int TotalCount; public long FreezeEndTime; }

    /// <summary>
    /// 次数礼包冷却缓存(对标老端 _CoolingGoodData_;老端 setGoodCoolingData 已整段注释/链路已断,
    /// 本轮补齐收发与缓存,触发预取暂不做——等红点系统落地后再接 On15050 use_count==35 类物品的预取)。
    /// </summary>
    public sealed class GoodsCoolingModel
    {
        public static readonly GoodsCoolingModel Instance = new GoodsCoolingModel();
        private GoodsCoolingModel() { }

        private readonly Dictionary<long, GoodsCoolingInfo> _cache = new Dictionary<long, GoodsCoolingInfo>();

        /// <summary>请求某物品冷却信息(15084 "l" goodsId)。</summary>
        public void RequestCooling(long goodsId)
        {
            if (goodsId <= 0) return;
            NetManager.SendFmt(Proto.GOODS_COOLING_INFO, "l", goodsId);
            GameLog.Info("Bag", "request 15084 goods_id={0}", goodsId);
        }

        public void Set(long goodsId, GoodsCoolingInfo info) => _cache[goodsId] = info;
        public GoodsCoolingInfo? Get(long goodsId) => _cache.TryGetValue(goodsId, out GoodsCoolingInfo v) ? v : (GoodsCoolingInfo?)null;
        public void Clear() => _cache.Clear();
    }

    // ===================== 15027 过期物品列表 =====================

    /// <summary>过期物品单项(goods_list,15027)。</summary>
    public struct GoodsExpiredEntry { public long GoodsId; public int TypeId; public int GoodsNum; }

    /// <summary>过期物品列表缓存(对标老端 GoodsExpiredView.goodsData;15027 opr=1 查看回包落地于此)。</summary>
    public sealed class GoodsExpiredModel
    {
        public static readonly GoodsExpiredModel Instance = new GoodsExpiredModel();
        private GoodsExpiredModel() { }

        public List<GoodsExpiredEntry> List { get; private set; } = new List<GoodsExpiredEntry>();

        /// <summary>请求查看过期物品(15027 "c" opr=1)。</summary>
        public void RequestExpiredGoods()
        {
            NetManager.SendFmt(Proto.GOODS_EXPIRED, "c", 1);
            GameLog.Info("Bag", "request 15027 opr=1(查看过期物品)");
        }

        public void SetList(List<GoodsExpiredEntry> list) => List = list ?? new List<GoodsExpiredEntry>();
        public void Clear() => List = new List<GoodsExpiredEntry>();
    }

    // ===================== 15055 物品 buff 列表 =====================

    /// <summary>buff 单项(buff_list,15055;goods_id 此处为 u32,buff_type==0 时才是物品配置 type_id)。</summary>
    public struct GoodsBuffEntry { public int GoodsId; public int BuffType; public string EffectList; public long Time; public long SingleTime; }

    /// <summary>本人 buff 列表缓存(对标老端 goodsModel.goods_buff_list;15055 只信 player_id==自己的回包)。</summary>
    public sealed class GoodsBuffModel
    {
        public static readonly GoodsBuffModel Instance = new GoodsBuffModel();
        private GoodsBuffModel() { }

        public event System.Action Changed;
        public List<GoodsBuffEntry> List { get; private set; } = new List<GoodsBuffEntry>();

        /// <summary>请求 buff 列表(15055 无参)。</summary>
        public void RequestBuffList()
        {
            NetManager.SendFmt(Proto.GOODS_BUFF_LIST);
            GameLog.Info("Bag", "request 15055(获取buff列表)");
        }

        public void SetList(List<GoodsBuffEntry> list)
        {
            List = list ?? new List<GoodsBuffEntry>();
            Changed?.Invoke();
        }

        public void Clear()
        {
            List = new List<GoodsBuffEntry>();
            Changed?.Invoke();
        }
    }

    // ===================== 15088 掉落拾取顺序 =====================

    /// <summary>拾取掉落包顺序列表(对标老端 Scene.Instance.SetDropIndexList;场景层消费待补)。</summary>
    public sealed class DropOrderModel
    {
        public static readonly DropOrderModel Instance = new DropOrderModel();
        private DropOrderModel() { }

        public List<int> DropIdList { get; private set; } = new List<int>();

        public void SetList(List<int> list) => DropIdList = list ?? new List<int>();
        public void Clear() => DropIdList = new List<int>();
    }
}
