using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包协议控制器(对标老客户端 commonController/GoodsController.ts 的 15010 收/送 + BagController.ts 编排)。
    /// 进游戏(EVT_GAME_START)请求主背包 + 坐骑/伙伴装备四容器(15010 "h" pos=4/22/32/23/33)，
    /// 收 15010/15017/15018 后按 pos 落 <see cref="BagModel"/>；主背包仍独占 EVT_BAG_UPDATE。
    /// 镜像 <see cref="Tasks.TaskController"/>/<see cref="Scene.SceneController"/> 的「一模块一控制器」范式,注册进 ControllerHub。
    ///
    /// 老端 GoodsController GAME_START 明确请求 horse/horse_bag/partner/partner_bag；本端本包只补这四个 PetEquip 前置容器，
    /// 其它 pos 保持既有专线或跳过，不扩大迁移范围。
    /// </summary>
    public sealed class BagController : BaseController
    {
        public static readonly BagController Instance = new BagController();

        private BagController() { }

        // 使用中防重(对标老端 goodsModel.goods_use_dic:发 15050 置位,回包清位;置位期间忽略重复点击)。
        private readonly HashSet<long> _pendingUse = new HashSet<long>();

        // 15027 过期物品简易确认弹窗的竞态令牌(对标 GoodsExpiredView.close_time 倒计时;用户手动确认/取消或
        // 又弹出新一轮时递增,令旧的自动确认延时任务失效,避免重复发 opr=2)。
        private int _expiredConfirmEpoch;

#if UNITY_EDITOR
        /// <summary>CliVerify 启动出站截获缝：返回 true 时记录但不真发；Player 构建不包含。</summary>
        private static Func<int, bool> s_startupContainerIntercept;
#endif

        protected override void Register()
        {
            ItemUseFlow.Initialize();
            RegisterProtocal(Proto.GOODS_CONTAINER_INFO, On15010);
            RegisterProtocal(Proto.GOODS_LIST_UPDATE, On15017);
            RegisterProtocal(Proto.GOODS_NUM_UPDATE, On15018);
            RegisterProtocal(Proto.SPECIAL_SCORE_UPDATE, On15008);
            RegisterProtocal(Proto.SPECIAL_SCORE_LIST, On15009);
            RegisterProtocal(Proto.USE_GOODS, On15050);
            RegisterProtocal(Proto.SELL_GOODS, On15021);

            // ----- Goods 协议扩容(自动循环 轮1;18 个请求-应答/推送号,详见 Proto.cs 对应常量注释) -----
            RegisterProtocal(Proto.GOODS_DETAIL, On15000);
            RegisterProtocal(Proto.GOODS_DETAIL_OTHERS, On15001);
            RegisterProtocal(Proto.BAG_EXPAND, On15002);
            RegisterProtocal(Proto.GOODS_MOVE_POS, On15003);
            RegisterProtocal(Proto.GOODS_DECOMPOSE, On15019);
            RegisterProtocal(Proto.GOODS_EXCHANGE, On15022);
            RegisterProtocal(Proto.GOODS_EXCHANGE_LIST, On15026);
            RegisterProtocal(Proto.GOODS_EXPIRED, On15027);
            RegisterProtocal(Proto.BAG_FULL_MAIL_NOTICE, On15029);
            RegisterProtocal(Proto.GOODS_RELOAD_NOTICE, On15030);
            RegisterProtocal(Proto.DROP_PICK, On15053);
            RegisterProtocal(Proto.GOODS_BUFF_LIST, On15055);
            RegisterProtocal(Proto.GIFT_LEVEL_INFO, On15083);
            RegisterProtocal(Proto.GOODS_COOLING_INFO, On15084);
            RegisterProtocal(Proto.GIFT_OPTIONAL_RECEIVE, On15086);
            RegisterProtocal(Proto.GIFT_CARD_RECEIVE, On15087);
            RegisterProtocal(Proto.DROP_ORDER_LIST, On15088);
            RegisterProtocal(Proto.GOODS_EXPECT_POWER, On15089);
            RegisterProtocal(Proto.GOODS_AUTO_DECOMPOSE_NOTICE, On15090);

            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            BagModel.Instance.Clear();
            GoodsDynamicModel.Instance.Clear();
            GoodsExchangeModel.Instance.Clear();
            GoodsCoolingModel.Instance.Clear();
            GoodsExpiredModel.Instance.Clear();
            GoodsBuffModel.Instance.Clear();
            DropOrderModel.Instance.Clear();
            ItemUseFlow.Reset();
            _expiredConfirmEpoch++;
            base.Dispose();
        }

        private async void OnGameStart()
        {
            // 背包格的真实图标/品质底板走 config_goods(同 TaskController 预载;EnsureLoaded 幂等)。
            await Task.WhenAll(GoodsModel.EnsureLoaded(), ItemUseFlow.EnsureConfigs());
            RequestStartupContainers();
            GameLog.Info("Bag", "request 15010 startup pos=4,22,32,23,33(主背包+坐骑/伙伴装备四容器)");

#if UNITY_EDITOR
            // 截获缝只会由 CliVerify 临时设置；测试启动请求时不再挂一个 2.5 秒后的 15027 真发送任务。
            if (s_startupContainerIntercept != null) return;
#endif

            // 对标老端 GoodsController GAME_START → setTimeout(delay_fun,2.5) 尾部 SendFmtToGame(15027,"c",1):
            // 延时 2.5 秒主动查看一次过期物品(挂在本方法尾部,不阻塞前面的 15010 请求)。
            await Shenxiao.Framework.Util.TimeUtil.Delay(2500);
            GoodsExpiredModel.Instance.RequestExpiredGoods();
        }

        private void RequestStartupContainers()
        {
            RequestContainerInfo(BagModel.POS_BAG);
            RequestContainerInfo(BagModel.POS_HORSE);
            RequestContainerInfo(BagModel.POS_HORSE_BAG);
            RequestContainerInfo(BagModel.POS_PARTNER);
            RequestContainerInfo(BagModel.POS_PARTNER_BAG);
            RequestContainerInfo(BagModel.POS_BABY_BAG);
        }

        private void RequestContainerInfo(int pos)
        {
#if UNITY_EDITOR
            if (s_startupContainerIntercept != null && s_startupContainerIntercept(pos)) return;
#endif
            SendFmt(Proto.GOODS_CONTAINER_INFO, "h", pos);
        }

        /// <summary>
        /// 15017 物品容器增量·全字段(对标 GoodsController.On15017)。pos:h + goods_list[u16 × 同 15010 单项 schema,
        /// 复用 <see cref="ReadGoods"/>]。背包 pos 落 <see cref="BagModel.Upsert"/>；22/32/23/33 落 PetEquip 四容器；
        /// equip 等其它 pos 仍按序读完跳过。
        /// </summary>
        private void On15017(NetReader r)
        {
            int pos = r.ReadU16();
            List<BagGoods> list = r.ReadArray(ReadGoods);
            if (pos == BagModel.POS_BAG)
            {
                var received = new List<BagGoods>();
                foreach (BagGoods g in list)
                {
                    BagGoods before = BagModel.Instance.FindContainerGoods(BagModel.POS_BAG, g.GoodsId);
                    long beforeNum = before?.GoodsNum ?? 0L;
                    BagModel.Instance.Upsert(g);
                    BagGoods current = BagModel.Instance.FindContainerGoods(BagModel.POS_BAG, g.GoodsId);
                    if (current != null && current.GoodsNum > beforeNum) received.Add(current);
                }
                GameLog.Info("Bag", "15017 bag delta: goods={0} bagCount={1} remaining={2}B",
                    list.Count, BagModel.Instance.BagGoodsList.Count, r.Remaining);
                ItemUseFlow.OnReceived(received);
                ItemUseFlow.OnInventoryStateChanged();
                EventDispatcher.Emit(GlobalEvent.EVT_BAG_UPDATE);
                return;
            }
            if (BagModel.IsPetEquipContainer(pos))
            {
                foreach (BagGoods g in list) BagModel.Instance.UpsertPetEquipContainer(pos, g);
                GameLog.Info("Bag", "15017 PetEquip container pos={0} delta={1} count={2} remaining={3}B",
                    pos, list.Count, BagModel.Instance.GetContainer(pos).Count, r.Remaining);
                EventDispatcher.Emit(GlobalEvent.EVT_PET_EQUIP_BAG_UPDATE, pos);
                return;
            }
            if (pos == BagModel.POS_BABY_BAG)
            {
                foreach (BagGoods g in list) BagModel.Instance.UpsertBabyEquipBag(g);
                EventDispatcher.Emit(GlobalEvent.EVT_BABY_EQUIP_BAG_UPDATE);
                return;
            }
            if (pos == BagModel.POS_BABY_EQUIP)
            {
                foreach (BagGoods g in list) BagModel.Instance.UpsertBabyEquip(g);
                EventDispatcher.Emit(GlobalEvent.EVT_BABY_EQUIP_UPDATE);
                return;
            }
            GameLog.Debug("Bag", "15017 pos={0}(未接容器) goods={1} remaining={2}B", pos, list.Count, r.Remaining);
        }

        /// <summary>
        /// 15018 物品容器增量·数量(对标 GoodsController.On15018:使用/出售后数量变化)。
        /// pos:h + goods_list[u16 × {goods_id:l, goods_num:i, type_id:i}] → <see cref="BagModel.UpdateNum"/>。
        /// 老端此包还触发 TRY_SHOW_ITEM_USE_VIEW(获得物品展示 flow)——未移植,先只落数据 + EVT_BAG_UPDATE。
        /// </summary>
        private void On15018(NetReader r)
        {
            int pos = r.ReadU16();
            List<(long goodsId, long num, int typeId)> list = r.ReadArray(rr =>
                (rr.ReadU64(), (long)rr.ReadU32(), (int)rr.ReadU32()));
            if (pos == BagModel.POS_BAG)
            {
                var received = new List<BagGoods>();
                foreach ((long goodsId, long num, int typeId) it in list)
                {
                    BagGoods before = BagModel.Instance.FindContainerGoods(BagModel.POS_BAG, it.goodsId);
                    long beforeNum = before?.GoodsNum ?? 0L;
                    BagModel.Instance.UpdateNum(it.goodsId, it.typeId, it.num);
                    BagGoods current = BagModel.Instance.FindContainerGoods(BagModel.POS_BAG, it.goodsId);
                    if (current != null && current.GoodsNum > beforeNum) received.Add(current);
                }
                GameLog.Info("Bag", "15018 bag num delta: goods={0} bagCount={1} remaining={2}B",
                    list.Count, BagModel.Instance.BagGoodsList.Count, r.Remaining);
                ItemUseFlow.OnReceived(received);
                ItemUseFlow.OnInventoryStateChanged();
                EventDispatcher.Emit(GlobalEvent.EVT_BAG_UPDATE);
                return;
            }
            if (BagModel.IsPetEquipContainer(pos))
            {
                foreach ((long goodsId, long num, int typeId) it in list)
                    BagModel.Instance.UpdatePetEquipContainerNum(pos, it.goodsId, it.typeId, it.num);
                GameLog.Info("Bag", "15018 PetEquip container pos={0} delta={1} count={2} remaining={3}B",
                    pos, list.Count, BagModel.Instance.GetContainer(pos).Count, r.Remaining);
                EventDispatcher.Emit(GlobalEvent.EVT_PET_EQUIP_BAG_UPDATE, pos);
                return;
            }
            if (pos == BagModel.POS_BABY_BAG)
            {
                foreach ((long goodsId, long num, int typeId) it in list) BagModel.Instance.UpdateBabyEquipBagNum(it.goodsId, it.typeId, it.num);
                EventDispatcher.Emit(GlobalEvent.EVT_BABY_EQUIP_BAG_UPDATE);
                return;
            }
            if (pos == BagModel.POS_BABY_EQUIP)
            {
                foreach ((long goodsId, long num, int typeId) it in list) BagModel.Instance.UpdateBabyEquipNum(it.goodsId, it.typeId, it.num);
                EventDispatcher.Emit(GlobalEvent.EVT_BABY_EQUIP_UPDATE);
                return;
            }
            GameLog.Debug("Bag", "15018 pos={0}(未接容器) goods={1} remaining={2}B", pos, list.Count, r.Remaining);
        }

        /// <summary>出售物品(对标 OnSellGoodsHandler:15021 = h count + 逐项 l goods_id/i num)。
        /// 协议备货:SellView 未移植,暂无 UI 入口(老端出售按钮开 SellView 选量,不直发);数量变化由 15018 推送刷新。</summary>
        public void SellGoods(IReadOnlyList<(long goodsId, int num)> list)
        {
            if (list == null || list.Count == 0) return;
            var fmt = new System.Text.StringBuilder("h");
            var args = new List<object>(1 + list.Count * 2) { list.Count };
            foreach ((long goodsId, int num) it in list)
            {
                fmt.Append("li");
                args.Add(it.goodsId);
                args.Add(it.num);
            }
            SendFmt(Proto.SELL_GOODS, fmt.ToString(), args.ToArray());
            GameLog.Info("Bag", "sell 15021 items={0}", list.Count);
        }

        /// <summary>15021 出售结果(对标 On15021:res==1「出售成功」;失败老端 Util.ErrorCodeShow 错误码表未移植 → 显码)。
        /// type_id_list(出售所得预览)老端未消费,按序读完。</summary>
        private void On15021(NetReader r)
        {
            int res = (int)r.ReadU32();
            List<(int typeId, long num)> gains = r.ReadArray(rr => ((int)rr.ReadU32(), (long)rr.ReadU32()));
            GameLog.Info("Bag", "15021 res={0} gains={1} remaining={2}B", res, gains.Count, r.Remaining);
            if (res == 1) TipsManager.Toast("出售成功");
            else TipsManager.Toast("出售失败(" + res + ")");   // 错误码表(Util.ErrorCodeShow)未移植,显码降级
        }

        /// <summary>15008 特殊积分单条(对标 On15008 → UpdateSpecialScore + UPDATE_SPECIAL_SCORE 事件)。</summary>
        private void On15008(NetReader r)
        {
            int currencyId = (int)r.ReadU32();
            long num = r.ReadU32();
            long old = BagModel.Instance.GetSpecialScore(currencyId);
            BagModel.Instance.SpecialScores[currencyId] = num;
            GameLog.Info("Bag", "15008 special score: id={0} {1}→{2}", currencyId, old, num);
            EventDispatcher.Emit(GlobalEvent.EVT_SPECIAL_SCORE_UPDATE, currencyId);
        }

        /// <summary>15009 特殊积分全量(对标 On15009 → CreateSpecialScoreList 清空重建 + CREATE_SPECIAL_SCORE_FINISH)。</summary>
        private void On15009(NetReader r)
        {
            List<(int id, long num)> list = r.ReadArray(rr => ((int)rr.ReadU32(), (long)rr.ReadU32()));
            BagModel.Instance.SpecialScores.Clear();
            foreach ((int id, long num) it in list) BagModel.Instance.SpecialScores[it.id] = it.num;
            GameLog.Info("Bag", "15009 special score list: {0} 条 remaining={1}B", list.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_SPECIAL_SCORE_UPDATE, 0);
        }

        // ===================================================================================
        // Goods 协议扩容(自动循环 轮1):15000/15001/15002/15003/15019/15022/15026/15027/15053/
        // 15055/15083/15084/15086/15087/15089(请求-应答) + 15030/15088/15090(纯服务端推送)。
        // 字段顺序/类型逐条核对 ClientProtocol.json,常量与 house-style 摘要见 Proto.cs 对应块。
        // ===================================================================================

        /// <summary>掉落拾取事件载荷(对标老端 15053 回包 vo;供未来场景层掉落实体消费方绑定)。</summary>
        public struct DropPickVo { public long DropId; public int Res; public int Status; public string Args; }

        /// <summary>读一份物品详情(15000/15001 共用;isOthers=true 时先读 player_id 且跳过 stren_exp/wash_rating,
        /// 字段顺序逐条核对 ClientProtocol.json "15000"/"15001")。</summary>
        private static GoodsDetailVo ReadGoodsDetail(NetReader r, bool isOthers)
        {
            var vo = new GoodsDetailVo();
            if (isOthers) vo.OwnerRoleId = r.ReadU64();   // player_id:l(仅15001)
            vo.GoodsId = r.ReadU64();            // goods_id:l
            vo.TypeId = (int)r.ReadU32();         // type_id:i
            vo.SubPos = r.ReadU8();               // sub_pos:c
            vo.Cell = r.ReadU16();                // cell:h
            vo.Num = r.ReadU32();                 // num:i
            vo.Bind = r.ReadU8();                 // bind:c
            vo.Trade = r.ReadU8();                // trade:c
            vo.Sell = r.ReadU8();                 // sell:c
            vo.Color = r.ReadU8();                // color:c
            vo.ExpireTime = r.ReadU32();          // expire_time:i
            vo.CombatPower = r.ReadU32();         // combat_power:i
            vo.EquipType = r.ReadU8();            // equip_type:c
            vo.PriceType = r.ReadU8();            // price_type:c
            vo.SellPrice = r.ReadU32();           // sell_price:i
            vo.Stren = r.ReadU16();               // stren:h
            if (!isOthers) vo.StrenExp = r.ReadU32();    // stren_exp:i(15001 无此字段)
            vo.Rating = r.ReadU32();              // rating:i
            vo.OverallRating = r.ReadU32();       // overall_rating:i
            vo.Division = r.ReadU8();             // division:c
            if (!isOthers) vo.WashRating = r.ReadU32();  // wash_rating:i(15001 无此字段)

            vo.AdditionAttrs = r.ReadArray(rr => new EquipAdditionAttr
            {
                AttrType = rr.ReadU8(), AttrValue = rr.ReadU32(), Color = rr.ReadU8(), CombatPower = rr.ReadU32(),
            });
            vo.StoneList = r.ReadArray(rr => new GoodsStoneSlot { Pos = rr.ReadU8(), TypeId = (int)rr.ReadU32() });
            vo.MagicList = r.ReadArray(rr => new GoodsMagicSlot { GoodsId = (int)rr.ReadU32(), EndTime = rr.ReadU32() });
            vo.ExtraAttrs = r.ReadArray(rr => new EquipExtraAttr
            {
                Color = rr.ReadU8(), AttrTypeId = rr.ReadU8(), AttrId = rr.ReadU16(), AttrVal = rr.ReadU32(),
                PlusInterval = rr.ReadU8(), PlusUnit = rr.ReadU32(),
            });
            vo.WashAttrs = r.ReadArray(rr => new GoodsWashAttr
            {
                Index = rr.ReadU8(), Color = rr.ReadU8(), AttrId = rr.ReadU16(), AttrVal = rr.ReadU32(),
            });
            vo.SuitList = r.ReadArray(rr => new GoodsSuitInfo
            {
                SuitLv = rr.ReadU8(), SuitSlv = rr.ReadU8(), SuitCount = rr.ReadU8(),
            });

            vo.CspiritStage = r.ReadU16();
            vo.CspiritLv = r.ReadU16();
            vo.AwakeningLv = r.ReadU8();
            vo.EquipSkillId = (int)r.ReadU32();
            vo.EquipSkillLv = r.ReadU8();
            vo.MountEquipSkillId = (int)r.ReadU32();
            vo.MountEquipSkillLv = r.ReadU8();
            vo.PetEquipStage = r.ReadU16();
            vo.PetEquipStar = r.ReadU16();
            vo.Level = r.ReadU16();

            vo.AwakeList = r.ReadArray(rr => new EquipAwakeAttr
            {
                AttrType = rr.ReadU16(), AwakeLv = rr.ReadU32(), AwakeExp = rr.ReadU32(),
            });
            vo.RefinementLv = r.ReadU16();
            return vo;
        }

        /// <summary>15000 自己物品详情(对标 On15000 → goodsModel.AddDynamic)。落 GoodsDynamicModel 缓存 +
        /// Emit EVT_GOODS_DETAIL_UPDATE(goods_id);等待该 goods_id 的一次性回调由 GoodsDynamicModel.Store 内部触发。</summary>
        private void On15000(NetReader r)
        {
            GoodsDetailVo vo = ReadGoodsDetail(r, isOthers: false);
            GoodsDynamicModel.Instance.Store(vo);
            GameLog.Info("Bag", "15000 goods_id={0} type_id={1} remaining={2}B", vo.GoodsId, vo.TypeId, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_GOODS_DETAIL_UPDATE, vo.GoodsId);
        }

        /// <summary>15001 他人物品详情(对标 On15001)。type_id==0 → toast 错误码 1500001(老端「装备信息已过时」的
        /// 好友消息缓存特化分支未移植 → 统一走错误码);player_id 不等于自己才落缓存(对标 vo.player_id != mainRoleId,
        /// 防止串成自己的详情缓存)。</summary>
        private void On15001(NetReader r)
        {
            GoodsDetailVo vo = ReadGoodsDetail(r, isOthers: true);
            GameLog.Info("Bag", "15001 player_id={0} goods_id={1} type_id={2} remaining={3}B",
                vo.OwnerRoleId, vo.GoodsId, vo.TypeId, r.Remaining);
            if (vo.TypeId == 0)
            {
                TipsManager.Toast("查询失败(1500001)");   // 对标 Util.ErrorCodeShow(1500001)
                return;
            }
            if (vo.OwnerRoleId != Role.RoleModel.Instance.RoleId)
            {
                GoodsDynamicModel.Instance.Store(vo);
                EventDispatcher.Emit(GlobalEvent.EVT_GOODS_DETAIL_UPDATE, vo.GoodsId);
            }
        }

        /// <summary>15002 扩容结果(对标 On15002:code==1→toast「扩容成功」+ 按 pos 写容量 + Emit;否则显错误码)。</summary>
        private void On15002(NetReader r)
        {
            int code = (int)r.ReadU32();
            int pos = r.ReadU16();
            int cellNum = r.ReadU16();
            GameLog.Info("Bag", "15002 code={0} pos={1} cell_num={2} remaining={3}B", code, pos, cellNum, r.Remaining);
            if (code == 1)
            {
                BagModel.Instance.SetMaxCell(pos, cellNum);
                TipsManager.Toast("扩容成功");
                EventDispatcher.Emit(GlobalEvent.EVT_BAG_MAX_CELL, pos, cellNum);
            }
            else
            {
                TipsManager.Toast("扩容失败(" + code + ")");
            }
        }

        /// <summary>请求开启背包/仓库格子(对标 OnExpandBagHandler → SendFmtToGame(15002,"hh",pos,cell_num))。
        /// ExpandBagView 现有壳未接线,先留发送封装。</summary>
        public void ExpandBag(int pos, int cellNum)
        {
            SendFmt(Proto.BAG_EXPAND, "hh", pos, cellNum);
            GameLog.Info("Bag", "expand 15002 pos={0} cell_num={1}", pos, cellNum);
        }

        /// <summary>15003 物品转移位置结果(对标 On15003:code!=1 显错误码;成功不本地改状态,等 15017 推送)。</summary>
        private void On15003(NetReader r)
        {
            int code = (int)r.ReadU32();
            GameLog.Info("Bag", "15003 code={0} remaining={1}B", code, r.Remaining);
            if (code != 1) TipsManager.Toast("移动失败(" + code + ")");
        }

        /// <summary>请求转移物品格子位置(对标 MoveGoods → SendFmtToGame(15003,"lhh",goods_id,from_pos,to_pos))。</summary>
        public void MoveGoods(long goodsId, int fromPos, int toPos)
        {
            if (goodsId <= 0) return;
            SendFmt(Proto.GOODS_MOVE_POS, "lhh", goodsId, fromPos, toPos);
            GameLog.Info("Bag", "move 15003 goods_id={0} {1}->{2}", goodsId, fromPos, toPos);
        }

        /// <summary>15019 分解结果(对标 On15019:code==1→toast「分解成功」+ Emit EVT_GOODS_DECOMPOSE_SUCCESS(reward_list);
        /// reward_list 只展示,不写 BagModel,数量变化随 15017/15018 推送)。</summary>
        private void On15019(NetReader r)
        {
            int code = (int)r.ReadU32();
            List<(long goodsId, long goodsNum)> rewards = r.ReadArray(rr => (rr.ReadU64(), (long)rr.ReadU32()));
            GameLog.Info("Bag", "15019 code={0} rewards={1} remaining={2}B", code, rewards.Count, r.Remaining);
            if (code == 1)
            {
                TipsManager.Toast("分解成功");
                EventDispatcher.Emit(GlobalEvent.EVT_GOODS_DECOMPOSE_SUCCESS, rewards);
            }
            else
            {
                TipsManager.Toast("分解失败(" + code + ")");
            }
        }

        /// <summary>发送物品分解(对标 ResolveGoods:WriteBegin(15019)+h 计数+逐项 l goods_id/i num,动态拼 fmt)。</summary>
        public void SendDecompose(IReadOnlyList<(long goodsId, int num)> list)
        {
            if (list == null || list.Count == 0) return;
            var fmt = new System.Text.StringBuilder("h");
            var args = new List<object>(1 + list.Count * 2) { list.Count };
            foreach ((long goodsId, int num) it in list)
            {
                fmt.Append("li");
                args.Add(it.goodsId);
                args.Add(it.num);
            }
            SendFmt(Proto.GOODS_DECOMPOSE, fmt.ToString(), args.ToArray());
            GameLog.Info("Bag", "decompose 15019 items={0}", list.Count);
        }

        /// <summary>15022 兑换/购买/合成结果(对标 On15022;errcode==1 按 type 分文案,2/3/4 类型额外补发 15026 刷新列表)。</summary>
        private void On15022(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            long id = r.ReadU64();
            int type = r.ReadU8();
            GameLog.Info("Bag", "15022 errcode={0} id={1} type={2} remaining={3}B", errcode, id, type, r.Remaining);
            if (errcode != 1)
            {
                TipsManager.Toast("操作失败(" + errcode + ")");
                return;
            }
            if (type == 2 || type == 3 || type == 4)
            {
                TipsManager.Toast("购买成功");
                GoodsExchangeModel.Instance.RequestList(type);   // 对标老端成功后 SendFmtToGame(15026,"h",scmd.type) 刷新列表
            }
            else if (type == 5) TipsManager.Toast("兑换成功");
            else if (type == 6) TipsManager.Toast("合成成功");
            else if (type == 7) TipsManager.Toast("兑换成功");
            EventDispatcher.Emit(GlobalEvent.EVT_GOODS_EXCHANGE_DONE, id);
        }

        /// <summary>发送物品兑换/购买/合成(对标 exchange_fun → SendFmtToGame(15022,"li",id,num);服务端 guard times&gt;0)。</summary>
        public void ExchangeGoods(long ruleId, int times)
        {
            if (times < 1) return;
            SendFmt(Proto.GOODS_EXCHANGE, "li", ruleId, times);
            GameLog.Info("Bag", "exchange 15022 rule_id={0} times={1}", ruleId, times);
        }

        /// <summary>15026 兑换列表(对标 On15026:按 id 升序排序后按 type 分桶存)。</summary>
        private void On15026(NetReader r)
        {
            int type = r.ReadU16();
            List<GoodsExchangeEntry> list = r.ReadArray(rr => new GoodsExchangeEntry
            {
                Id = (int)rr.ReadU32(), Count = rr.ReadU16(), CanExchange = rr.ReadU8(),
            });
            list.Sort((a, b) => a.Id.CompareTo(b.Id));   // 对标老端 table.sort(exchange_list,(a,b)=>a.id<b.id) 升序
            GoodsExchangeModel.Instance.SetList(type, list);
            GameLog.Info("Bag", "15026 type={0} count={1} remaining={2}B", type, list.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_GOODS_EXCHANGE_LIST, type);
        }

        /// <summary>15027 过期物品(对标 On15027):opr==1→存列表 + Emit + 弹简易确认(与老端一致,不判空);
        /// opr==2 回执老端不处理,仅 log。</summary>
        private void On15027(NetReader r)
        {
            int opr = r.ReadU8();
            List<GoodsExpiredEntry> list = r.ReadArray(rr => new GoodsExpiredEntry
            {
                GoodsId = rr.ReadU64(), TypeId = (int)rr.ReadU32(), GoodsNum = rr.ReadU16(),
            });
            GameLog.Info("Bag", "15027 opr={0} goods={1} remaining={2}B", opr, list.Count, r.Remaining);
            if (opr == 1)
            {
                GoodsExpiredModel.Instance.SetList(list);
                EventDispatcher.Emit(GlobalEvent.EVT_GOODS_EXPIRED_LIST);
                ShowExpiredConfirm();
            }
            else
            {
                GameLog.Info("Bag", "15027 opr=2 回执(老端不处理,仅log)");
            }
        }

        /// <summary>弹简易确认(对标 GoodsExpiredView 文案+按钮语义,走现有 TipsManager.Confirm 通道:UI 未就绪时
        /// 内部直接 onYes,与老端「无人可点,阻塞流程没有意义」同语义)。仅当确认框真的显示出来(UI 层就绪)才起 16 秒
        /// 自动确认倒计时(对标 GoodsExpiredView.close_time=15,GlobalTimerQuest 每秒-1,&lt;0 触发,共 16 次 tick)。</summary>
        private void ShowExpiredConfirm()
        {
            bool willShow = ViewManager.GetLayer(UILayer.Tip) != null;
            _expiredConfirmEpoch++;
            int myEpoch = _expiredConfirmEpoch;
            TipsManager.Confirm(
                "修士，您有以下物品过期了，是否回收？",   // 逐字对标 GoodsExpiredView.contentText 默认文案
                () => { _expiredConfirmEpoch++; SendExpiredReclaim(); },
                () => { _expiredConfirmEpoch++; GameLog.Info("Bag", "15027 用户取消回收过期物品"); });

            if (willShow)
            {
                GameLog.Info("Bag", "15027 确认框已弹出,启动 16 秒自动确认倒计时(对标 GoodsExpiredView.close_time)");
                AutoConfirmExpiredAfterDelay(myEpoch);
            }
            else
            {
                GameLog.Info("Bag", "15027 headless(UI 层未就绪),TipsManager.Confirm 已立即 onYes,不起倒计时");
            }
        }

        private async void AutoConfirmExpiredAfterDelay(int epoch)
        {
            await Shenxiao.Framework.Util.TimeUtil.Delay(16000);   // close_time=15,每秒-1,<0 触发,共 16 次 tick ≈16 秒(去老端文件抄准秒数)
            if (epoch != _expiredConfirmEpoch) return;   // 期间用户已手动确认/取消,或又弹了新一轮 → 作废
            _expiredConfirmEpoch++;
            GameLog.Info("Bag", "15027 倒计时到期,自动确认回收(对标 GoodsExpiredView.SetOkText close_time<0)");
            SendExpiredReclaim();
        }

        private void SendExpiredReclaim()
        {
            SendFmt(Proto.GOODS_EXPIRED, "c", 2);
            GameLog.Info("Bag", "15027 opr=2 发送回收请求");
        }

        /// <summary>15029 背包已满改邮件发放通知(S2C 主动,轮21 PF 补漏批;对标老端 BagController.ts:147-167
        /// On15029)。物品已经落进系统邮件(服务端 lib_goods_api.erl:2108-2119 `send_mail_when_no_cell`
        /// 与本包同一次调用先发本号再发系统邮件),此包只是提醒。老端按 location(4=普通背包/45=星装)弹二次
        /// 确认框跳转对应页签;Unity 暂无星装(232星座装备)模块与"打开指定背包位置"事件通道,降级为 toast
        /// 提示,不复刻二次确认框跳转,TODO。</summary>
        private void On15029(NetReader r)
        {
            int state = r.ReadU8();
            int location = r.ReadU16();
            TipsManager.Toast("背包已满,物品已通过邮件发送,请前往整理背包");
            GameLog.Info("Bag", "15029 背包已满改邮件发放 state={0} location={1}(降级为纯提示,未接二次确认跳转,TODO)", state, location);
        }

        /// <summary>15030 服务端要求重拉背包(对标 On15030,老端空桩仅重走 GAME_START 流程)。直接复用 15010 请求路径
        /// (而非整份 OnGameStart,避免重复触发 EnsureLoaded/过期物品 2.5s 定时器)。空包,无字段可读。</summary>
        private void On15030(NetReader r)
        {
            GameLog.Info("Bag", "15030 服务端要求重拉背包(对标老端空桩),重发 15010 bag pos={0}", BagModel.POS_BAG);
            SendFmt(Proto.GOODS_CONTAINER_INFO, "h", BagModel.POS_BAG);
        }

        /// <summary>15053 拾取掉落结果(对标 On15053,三态判断顺序照老端):res==1→拾取成功;否则 status==1→
        /// 进入拾取计时;否则 res==1500020→掉落包已消失;否则→失败(toast 错误码,带 args)。
        /// 场景层掉落实体系统尚未接线,先只发事件+log(TODO 场景层消费方绑定)。</summary>
        private void On15053(NetReader r)
        {
            int res = (int)r.ReadU32();
            string args = r.ReadString();
            int status = r.ReadU8();
            long dropId = r.ReadU64();
            var vo = new DropPickVo { DropId = dropId, Res = res, Status = status, Args = args };
            GameLog.Info("Bag", "15053 res={0} status={1} drop_id={2} args={3} remaining={4}B", res, status, dropId, args, r.Remaining);
            if (res == 1)
            {
                EventDispatcher.Emit(GlobalEvent.EVT_DROP_PICK_SUCCESS, vo);
                GameLog.Info("Bag", "15053 拾取成功(对标老端 PlaySoundEffect(\"openorclosebutton\"),音效未接,仅log)");
            }
            else if (status == 1)
            {
                EventDispatcher.Emit(GlobalEvent.EVT_DROP_PICK_BEGIN, vo);
            }
            else if (res == 1500020)
            {
                EventDispatcher.Emit(GlobalEvent.EVT_DROP_DISMISS, dropId);
            }
            else
            {
                EventDispatcher.Emit(GlobalEvent.EVT_DROP_PICK_FAIL, dropId);
                TipsManager.Toast("拾取失败(" + res + ")");
            }
        }

        /// <summary>拾取场景掉落(对标 SceneEventType.REQUEST_PICK_UP_SCENE_DROP → SendFmtToGame(15053,"l",drop_id))。
        /// 场景层掉落实体系统未接线,先留发送封装。</summary>
        public void PickDrop(long dropId)
        {
            if (dropId <= 0) return;
            SendFmt(Proto.DROP_PICK, "l", dropId);
            GameLog.Info("Bag", "pick 15053 drop_id={0}", dropId);
        }

        /// <summary>15055 buff 列表(对标 On15055:仅 player_id==自己才落缓存,事件无条件发)。</summary>
        private void On15055(NetReader r)
        {
            long playerId = r.ReadU64();
            List<GoodsBuffEntry> list = r.ReadArray(rr => new GoodsBuffEntry
            {
                GoodsId = (int)rr.ReadU32(), BuffType = rr.ReadU8(), EffectList = rr.ReadString(),
                Time = rr.ReadU32(), SingleTime = rr.ReadU32(),
            });
            GameLog.Info("Bag", "15055 player_id={0} buff={1} remaining={2}B", playerId, list.Count, r.Remaining);
            if (playerId == Role.RoleModel.Instance.RoleId) GoodsBuffModel.Instance.SetList(list);
            EventDispatcher.Emit(GlobalEvent.EVT_GOODS_BUFF_UPDATE);
        }

        /// <summary>15083 礼包等级信息(对标 On15083:广播事件 + 一次性回调)。</summary>
        private void On15083(NetReader r)
        {
            var vo = new GiftLevelInfo { GoodsId = r.ReadU64(), TypeId = (int)r.ReadU32(), GiftLevel = r.ReadU16() };
            GameLog.Info("Bag", "15083 goods_id={0} type_id={1} gift_level={2} remaining={3}B", vo.GoodsId, vo.TypeId, vo.GiftLevel, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_GIFT_LEVEL_INFO, vo);
            GoodsDynamicModel.Instance.DeliverGiftLevel(vo);
        }

        /// <summary>15084 次数礼包冷却信息(对标 On15084;老端消费链路已断,本轮补齐缓存)。</summary>
        private void On15084(NetReader r)
        {
            long goodsId = r.ReadU64();
            var info = new GoodsCoolingInfo { UseCount = r.ReadU8(), TotalCount = r.ReadU8(), FreezeEndTime = r.ReadU32() };
            GoodsCoolingModel.Instance.Set(goodsId, info);
            GameLog.Info("Bag", "15084 goods_id={0} use={1}/{2} freeze_endtime={3} remaining={4}B",
                goodsId, info.UseCount, info.TotalCount, info.FreezeEndTime, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_GOODS_COOLING_UPDATE, goodsId);
        }

        /// <summary>15086 自选礼包兑换结果(对标 On15086)。</summary>
        private void On15086(NetReader r)
        {
            int code = (int)r.ReadU32();
            GameLog.Info("Bag", "15086 code={0} remaining={1}B", code, r.Remaining);
            if (code == 1) TipsManager.Toast("兑换成功");
            else TipsManager.Toast("兑换失败(" + code + ")");
        }

        /// <summary>发送自选礼包领取(对标 optional_gift:WriteBegin(15086)+l gift_id+h 计数+逐项 c slot/i num;
        /// slot 序号是 1 字节 c,别写成 h/i)。UI(SelectGiftView)未接线,先留发送封装。</summary>
        public void SendOptionalGift(long giftId, IReadOnlyDictionary<int, int> picks)
        {
            if (giftId <= 0 || picks == null || picks.Count == 0) return;
            var fmt = new System.Text.StringBuilder("lh");
            var args = new List<object>(2 + picks.Count * 2) { giftId, picks.Count };
            foreach (KeyValuePair<int, int> kv in picks)
            {
                fmt.Append("ci");
                args.Add(kv.Key);
                args.Add(kv.Value);
            }
            SendFmt(Proto.GIFT_OPTIONAL_RECEIVE, fmt.ToString(), args.ToArray());
            GameLog.Info("Bag", "optional gift 15086 gift_id={0} picks={1}", giftId, picks.Count);
        }

        /// <summary>15087 礼包卡兑换结果(对标 On15087 + ExchangeGiftView:reward_list 非空→成功,经 GetMappingTypeId
        /// 还原展示「获得X」;为空→失败查错误码。服务端 5 秒中央 CD,结果可能异步再推一次本号,按此逻辑重复处理即可)。</summary>
        private void On15087(NetReader r)
        {
            int res = (int)r.ReadU32();
            List<(int style, int typeId, int count)> rewards = r.ReadArray(rr =>
                ((int)rr.ReadU8(), (int)rr.ReadU32(), (int)rr.ReadU32()));   // reward_list:ObjectList{style:c,typeId:i,count:i}
            GameLog.Info("Bag", "15087 res={0} rewards={1} remaining={2}B", res, rewards.Count, r.Remaining);

            if (rewards.Count > 0)
            {
                foreach ((int style, int typeId, int count) it in rewards)
                {
                    (int mappedId, int _) = GoodsModel.GetMappingTypeId(it.style, it.typeId);
                    GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(mappedId);
                    if (basic == null) continue;   // 表里没有的不臆造名称,跳过
                    TipsManager.Toast("获得" + basic.Name + "x" + it.count);
                }
                EventDispatcher.Emit(GlobalEvent.EVT_GIFT_CARD_RESULT, true, rewards);
            }
            else
            {
                TipsManager.Toast("兑换失败(" + res + ")");
                EventDispatcher.Emit(GlobalEvent.EVT_GIFT_CARD_RESULT, false, (List<(int, int, int)>)null);
            }
        }

        /// <summary>发送礼包卡兑换(对标 ExchangeGiftView._btn_receive → SendFmtToGame(15087,"s",cardNo));空串不发。</summary>
        public void SendGiftCard(string cardNo)
        {
            if (string.IsNullOrEmpty(cardNo)) return;
            SendFmt(Proto.GIFT_CARD_RECEIVE, "s", cardNo);
            GameLog.Info("Bag", "gift card 15087 card_no={0}", cardNo);
        }

        /// <summary>15088 拾取顺序列表(对标 On15088 → Scene.Instance.SetDropIndexList,S2C 推送,禁止发送)。</summary>
        private void On15088(NetReader r)
        {
            List<int> list = r.ReadArray(rr => (int)rr.ReadU32());
            DropOrderModel.Instance.SetList(list);
            GameLog.Info("Bag", "15088 drop_id_list={0} remaining={1}B", list.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_DROP_ORDER_LIST);
        }

        /// <summary>15089 物品预览战力(对标 On15089;goods_type_id 是 4 字节类型 id,非物品实例 id)。</summary>
        private void On15089(NetReader r)
        {
            int goodsTypeId = (int)r.ReadU32();
            long expectPower = r.ReadU32();
            GameLog.Info("Bag", "15089 goods_type_id={0} expect_power={1} remaining={2}B", goodsTypeId, expectPower, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_GOODS_EXPECT_POWER, goodsTypeId, expectPower);
        }

        /// <summary>请求物品预览战力(对标 ccmd_request → SendFmtToGame(15089,"i",goods_type_id))。
        /// 消费方(幻化 tooltip)未接线,先留发送封装。</summary>
        public void RequestExpectPower(int goodsTypeId)
        {
            SendFmt(Proto.GOODS_EXPECT_POWER, "i", goodsTypeId);
            GameLog.Info("Bag", "request 15089 goods_type_id={0}", goodsTypeId);
        }

        /// <summary>15090 物品自动分解提示(对标 On15090:文案逐字对标 GoodsController.ts:1000-1024;
        /// 复用 EVT_GOODS_DECOMPOSE_SUCCESS 同一事件,老端两号共用同一 Fire)。禁止客户端发送。</summary>
        private void On15090(NetReader r)
        {
            int code = (int)r.ReadU32();
            List<(long goodsId, long goodsNum)> rewards = r.ReadArray(rr => (rr.ReadU64(), (long)rr.ReadU32()));
            int bagType = r.ReadU8();
            int underColor = r.ReadU8();
            GameLog.Info("Bag", "15090 code={0} rewards={1} bag_type={2} under_color={3} remaining={4}B",
                code, rewards.Count, bagType, underColor, r.Remaining);

            if (code == 1)
            {
                if (underColor == 2)
                {
                    if (bagType == 11) TipsManager.Toast("万魄藏容量不足，已为你自动分解经验材料和蓝色及以下的九霄劫魄");
                    else if (bagType == 15) TipsManager.Toast("源力背包空间不足，已为你自动分解经验材料和蓝色及以下的源力");
                }
                else if (underColor == 3)
                {
                    if (bagType == 11) TipsManager.Toast("万魄藏容量不足，已为你自动分解经验材料和紫色及以下的九霄劫魄");
                    else if (bagType == 15) TipsManager.Toast("源力背包空间不足，已为你自动分解经验材料和紫色及以下的源力");
                }
                else if (underColor == 0)
                {
                    if (bagType == 43) TipsManager.Toast("九天神祭袋空间不足，已为你自动分解经验材料和所有的天殒神装");
                }
                EventDispatcher.Emit(GlobalEvent.EVT_GOODS_DECOMPOSE_SUCCESS, rewards);
            }
            else
            {
                TipsManager.Toast("分解失败(" + code + ")");
            }
        }

        /// <summary>使用背包物品(对标 GoodsController.ts UseHandler:USE_BAG_GOODS → SendFmtToGame(15050,"li"))。
        /// 使用中防重;结果经 <see cref="On15050"/> 回包处理(成功 toast + EVT_GOODS_USE_SUCCESS + 礼包开出物 toast)。</summary>
        public void UseGoods(long goodsId, int num)
        {
            if (goodsId <= 0 || num <= 0) return;
            if (_pendingUse.Contains(goodsId))
            {
                GameLog.Info("Bag", "use 15050 goods_id={0} 使用中防重跳过(对标 goods_use_dic)", goodsId);
                return;
            }
            _pendingUse.Add(goodsId);
            SendFmt(Proto.USE_GOODS, "li", goodsId, num);
            GameLog.Info("Bag", "use 15050 goods_id={0} num={1}", goodsId, num);
        }

        /// <summary>
        /// 15050 使用物品结果(对标 GoodsController.ts On15050)。res==1 → 「使用成功」toast(type==35 冷却物不弹)+
        /// EVT_GOODS_USE_SUCCESS(goods_type_id);礼包类(type 32/33/35/84)开出物 show_goods 逐项「获得X」toast
        /// (老端 config_gift_box.show==1 走 CongratulationView,该视图/配表未移植 → 统一 toast,数据全为服务端真值)。
        /// 背包数量变化由服务端随后推送的容器/增量包刷新,不在本地臆改。
        /// </summary>
        private void On15050(NetReader r)
        {
            int res = (int)r.ReadU32();          // res:i
            r.ReadString();                      // args:s(老端未消费)
            long goodsId = r.ReadU64();          // goods_id:l
            int goodsTypeId = (int)r.ReadU32();  // goods_type_id:i
            r.ReadU32();                         // goods_num:i(老端未消费)
            r.ReadU32();                         // hp:i(老端未消费)
            r.ReadU32();                         // num:i(老端未消费)
            var shows = r.ReadArray(rr => new ShowGoods
            {
                Gid = rr.ReadU64(),              // gid:l
                Type = rr.ReadU8(),              // type:c
                GoodId = (int)rr.ReadU32(),      // goodid:i
                Num = (int)rr.ReadU32(),         // gnum:i
            });

            _pendingUse.Remove(goodsId);
            GameLog.Info("Bag", "15050 res={0} goods_id={1} type_id={2} show_goods={3} remaining={4}B",
                res, goodsId, goodsTypeId, shows.Count, r.Remaining);
            if (res != 1) return;   // 失败文案走服务端通用错误推送,老端 On15050 亦不处理

            GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(goodsTypeId);
            int type = basic?.Type ?? 0;
            bool cooling = type == 35;
            if (!cooling) TipsManager.Toast("使用成功");
            EventDispatcher.Emit(GlobalEvent.EVT_GOODS_USE_SUCCESS, goodsTypeId);

            // 礼包开出物展示(对标 On15050 type 32/33/84/冷却 35 分支的 show_goods 文案路径;
            // CongratulationView(config_gift_box.show==1)未移植 → 统一走「获得X」toast,不画假数据)。
            if (type == 32 || type == 33 || type == 84 || cooling)
            {
                foreach (ShowGoods s in shows)
                {
                    (int mappedId, int _) = GoodsModel.GetMappingTypeId(s.Type, s.GoodId);
                    GoodsModel.GoodsBasic sb = GoodsModel.GetGoodsBasicByTypeId(mappedId);
                    if (sb == null) continue;   // 表里没有的开出物不臆造名称
                    long shownNum = s.Num != 0 ? s.Num : s.GoodId;
                    TipsManager.Toast("获得" + sb.Name + "x" + shownNum);
                }
            }
        }

        private struct ShowGoods
        {
            public long Gid;
            public int Type;
            public int GoodId;
            public int Num;
        }

        /// <summary>
        /// 15010 物品容器全量。读 pos/cell_num/max_cell/cell_gold + goods_list(u16 计数 + 逐项)。
        /// 每个回包对应一个 pos；主背包及 PetEquip 四容器落 <see cref="BagModel"/>。每项须按 ClientProtocol.json 顺序读完
        /// (含 addition_attrlist / equip_extra_attr / awake_list 3 个嵌套数组)否则错位。
        /// </summary>
        private void On15010(NetReader r)
        {
            int pos = r.ReadU16();
            int cellNum = r.ReadU16();
            int maxCell = r.ReadU16();
            r.ReadU8();                  // cell_gold:c(开格消耗,显示暂不用)

            // goods_list:u16 计数 + 逐项(对标 NetReader.ReadArray;每项 ReadGoods 按 ClientProtocol.json 顺序读完)。
            List<BagGoods> list = r.ReadArray(ReadGoods);

            if (pos == BagModel.POS_BAG)
            {
                BagModel.Instance.SetBagFull(cellNum, maxCell, list);
                int withInstAttr = list.FindAll(x => x.HasInstanceAttr).Count;
                GameLog.Info("Bag", "15010 bag: cellNum={0} maxCell={1} goods={2} equipWithInstAttr={3} remaining={4}B",
                    cellNum, maxCell, list.Count, withInstAttr, r.Remaining);
                ItemUseFlow.OnInitialSnapshot(list);
                ItemUseFlow.OnInventoryStateChanged();
                EventDispatcher.Emit(GlobalEvent.EVT_BAG_UPDATE);
            }
            else if (BagModel.IsPetEquipContainer(pos))
            {
                BagModel.Instance.SetPetEquipContainerFull(pos, maxCell, list);
                GameLog.Info("Bag", "15010 PetEquip container pos={0} cellNum={1} maxCell={2} goods={3} remaining={4}B",
                    pos, cellNum, maxCell, list.Count, r.Remaining);
                EventDispatcher.Emit(GlobalEvent.EVT_PET_EQUIP_BAG_UPDATE, pos);
            }
            else if (pos == BagModel.POS_BABY_BAG)
            {
                BagModel.Instance.SetBabyEquipBagFull(maxCell, list);
                EventDispatcher.Emit(GlobalEvent.EVT_BABY_EQUIP_BAG_UPDATE);
            }
            else if (pos == BagModel.POS_BABY_EQUIP)
            {
                // pos36 是宝宝已穿戴装备实例；pos37 才是待穿候选背包。
                BagModel.Instance.SetBabyEquipFull(maxCell, list);
                EventDispatcher.Emit(GlobalEvent.EVT_BABY_EQUIP_UPDATE);
            }
            else if (pos == Rune.RuneController.RUNE_BAG_POS)
            {
                // 例外(第19轮工单-灵魄镶嵌):15010 单 handler 已被本控制器独占注册,符文背包(rune_bag pos)
                // 只能在此顺路接住、转存到 Rune.RuneModel,不新开协议注册。≤10 行,不改其余分支行为。
                var runeBag = list.ConvertAll(g => new Rune.RuneModel.BagGoodsVo(g.GoodsId, g.TypeId, g.GoodsNum));
                Rune.RuneModel.Instance.SetRuneBag(runeBag);
                GameLog.Info("Bag", "15010 rune_bag: goods={0} remaining={1}B", list.Count, r.Remaining);
            }
            else if (pos == Equip.EquipAutoWear.POS_EQUIP)
            {
                // 例外(活服实证-自动穿戴):已穿戴装备通道(pos=equip=1)转存 EquipAutoWear 供 rating 比较(同 rune_bag 模式)。
                Equip.EquipAutoWear.SetWornList(list);
                ItemUseFlow.OnInventoryStateChanged();
                GameLog.Info("Bag", "15010 equip: goods={0} remaining={1}B", list.Count, r.Remaining);
            }
            else
            {
                GameLog.Debug("Bag", "15010 pos={0}(非背包,本轮暂不接) goods={1} remaining={2}B", pos, list.Count, r.Remaining);
            }
        }

        /// <summary>
        /// 读一项 goods_list(字段名/顺序/嵌套照抄 ClientProtocol.json "15010")。显示字段 + 装备实例态(强化/评分 +
        /// 极品/附加/觉醒 3 数组)均暂存进 <see cref="BagGoods"/>(第 9 轮:从「读过即弃」改为「按序读出并留存」,为装备 tips 实例行做地基)。
        /// </summary>
        private static BagGoods ReadGoods(NetReader r)
        {
            var g = new BagGoods
            {
                GoodsId = r.ReadU64(),       // goods_id:l
                TypeId = (int)r.ReadU32(),   // type_id:i
            };
            r.ReadU8();                      // sub_pos:c
            g.Cell = r.ReadU16();            // cell:h
            g.GoodsNum = r.ReadU32();        // goods_num:i
            g.Bind = r.ReadU8();             // bind:c
            r.ReadU8();                      // trade:c
            r.ReadU8();                      // sell:c
            r.ReadU8();                      // is_drop:c
            g.Color = r.ReadU8();            // color:c
            r.ReadU32();                     // expire_time:i
            g.CombatPower = r.ReadU32();     // combat_power:i
            g.Stren = r.ReadU16();           // stren:h
            g.Level = r.ReadU16();           // level:h
            g.Rating = r.ReadU32();          // rating:i
            g.OverallRating = r.ReadU32();   // overall_rating:i

            int addCount = r.ReadU16();      // addition_attrlist[]
            if (addCount > 0) g.AdditionAttrs = new List<EquipAdditionAttr>(addCount);
            for (int i = 0; i < addCount; i++)
            {
                g.AdditionAttrs.Add(new EquipAdditionAttr
                {
                    AttrType = r.ReadU8(),       // attr_type:c
                    AttrValue = r.ReadU32(),     // attr_value:i
                    Color = r.ReadU8(),          // color:c
                    CombatPower = r.ReadU32(),   // combat_power:i
                });
            }

            int extraCount = r.ReadU16();    // equip_extra_attr[]
            if (extraCount > 0) g.ExtraAttrs = new List<EquipExtraAttr>(extraCount);
            for (int i = 0; i < extraCount; i++)
            {
                g.ExtraAttrs.Add(new EquipExtraAttr
                {
                    Color = r.ReadU8(),          // color:c
                    AttrTypeId = r.ReadU8(),     // type_id:c
                    AttrId = r.ReadU16(),        // attr_id:h
                    AttrVal = r.ReadU32(),       // attr_val:i
                    PlusInterval = r.ReadU8(),   // plus_interval:c
                    PlusUnit = r.ReadU32(),      // plus_unit:i
                });
            }

            g.EquipStage = r.ReadU8();       // equipStage:c
            g.EquipStar = r.ReadU8();        // equipStar:c
            r.ReadU32();                     // skill_id:i
            r.ReadU8();                      // skill_lv:c

            int awakeCount = r.ReadU16();    // awake_list[]
            if (awakeCount > 0) g.AwakeList = new List<EquipAwakeAttr>(awakeCount);
            for (int i = 0; i < awakeCount; i++)
            {
                g.AwakeList.Add(new EquipAwakeAttr
                {
                    AttrType = r.ReadU16(),      // attr_type:h
                    AwakeLv = r.ReadU32(),       // awake_lv:i
                    AwakeExp = r.ReadU32(),      // awake_exp:i
                });
            }
            return g;
        }
    }
}
