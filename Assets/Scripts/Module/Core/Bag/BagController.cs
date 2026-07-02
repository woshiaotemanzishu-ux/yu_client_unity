using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 背包协议控制器(对标老客户端 commonController/GoodsController.ts 的 15010 收/送 + BagController.ts 编排)。
    /// 进游戏(EVT_GAME_START)请求满背包(发 15010 "h" pos=bag),收 15010 解析落 <see cref="BagModel"/>,发 EVT_BAG_UPDATE。
    /// 镜像 <see cref="Tasks.TaskController"/>/<see cref="Scene.SceneController"/> 的「一模块一控制器」范式,注册进 ControllerHub。
    ///
    /// 老端 GoodsController 在 GAME_START 批量请求各容器(equip/bag/warehouse/…);本轮只接背包(主线竖切聚焦背包入口),
    /// 其它 pos 暂不请求。15010 回包按 pos 区分,仅 bag(4)落 BagModel;服务端登录主动推送的其它容器(若有)按 pos 跳过。
    /// </summary>
    public sealed class BagController : BaseController
    {
        public static readonly BagController Instance = new BagController();

        private BagController() { }

        // 使用中防重(对标老端 goodsModel.goods_use_dic:发 15050 置位,回包清位;置位期间忽略重复点击)。
        private readonly HashSet<long> _pendingUse = new HashSet<long>();

        protected override void Register()
        {
            RegisterProtocal(Proto.GOODS_CONTAINER_INFO, On15010);
            RegisterProtocal(Proto.GOODS_LIST_UPDATE, On15017);
            RegisterProtocal(Proto.GOODS_NUM_UPDATE, On15018);
            RegisterProtocal(Proto.SPECIAL_SCORE_UPDATE, On15008);
            RegisterProtocal(Proto.SPECIAL_SCORE_LIST, On15009);
            RegisterProtocal(Proto.USE_GOODS, On15050);
            RegisterProtocal(Proto.SELL_GOODS, On15021);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            BagModel.Instance.Clear();
            base.Dispose();
        }

        private async void OnGameStart()
        {
            // 背包格的真实图标/品质底板走 config_goods(同 TaskController 预载;EnsureLoaded 幂等)。
            await GoodsModel.EnsureLoaded();
            SendFmt(Proto.GOODS_CONTAINER_INFO, "h", BagModel.POS_BAG);
            GameLog.Info("Bag", "request 15010 bag pos={0}(对标 GoodsController GAME_START SendFmtToGame(15010,h,bag))", BagModel.POS_BAG);
        }

        /// <summary>
        /// 15017 物品容器增量·全字段(对标 GoodsController.On15017)。pos:h + goods_list[u16 × 同 15010 单项 schema,
        /// 复用 <see cref="ReadGoods"/>]。仅背包 pos 落 <see cref="BagModel.Upsert"/>(num&lt;=0 删/有则替换/新增);
        /// equip 等其它 pos 老端走 UpdateEquipGoods(未移植)→ 按序读完跳过。
        /// </summary>
        private void On15017(NetReader r)
        {
            int pos = r.ReadU16();
            List<BagGoods> list = r.ReadArray(ReadGoods);
            if (pos != BagModel.POS_BAG)
            {
                GameLog.Debug("Bag", "15017 pos={0}(非背包,暂不接) goods={1} remaining={2}B", pos, list.Count, r.Remaining);
                return;
            }
            foreach (BagGoods g in list) BagModel.Instance.Upsert(g);
            GameLog.Info("Bag", "15017 bag delta: goods={0} bagCount={1} remaining={2}B",
                list.Count, BagModel.Instance.BagGoodsList.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_BAG_UPDATE);
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
            if (pos != BagModel.POS_BAG)
            {
                GameLog.Debug("Bag", "15018 pos={0}(非背包,暂不接) goods={1} remaining={2}B", pos, list.Count, r.Remaining);
                return;
            }
            foreach ((long goodsId, long num, int typeId) it in list)
                BagModel.Instance.UpdateNum(it.goodsId, it.typeId, it.num);
            GameLog.Info("Bag", "15018 bag num delta: goods={0} bagCount={1} remaining={2}B",
                list.Count, BagModel.Instance.BagGoodsList.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_BAG_UPDATE);
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
        /// 每个回包对应一个 pos;仅背包(pos==4)落 <see cref="BagModel"/>。每项须按 ClientProtocol.json 顺序读完
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
                EventDispatcher.Emit(GlobalEvent.EVT_BAG_UPDATE);
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
            r.ReadU8();                      // bind:c
            r.ReadU8();                      // trade:c
            r.ReadU8();                      // sell:c
            r.ReadU8();                      // is_drop:c
            g.Color = r.ReadU8();            // color:c
            r.ReadU32();                     // expire_time:i
            g.CombatPower = r.ReadU32();     // combat_power:i
            g.Stren = r.ReadU16();           // stren:h
            g.Level = r.ReadU16();           // level:h
            g.Rating = r.ReadU32();          // rating:i
            r.ReadU32();                     // overall_rating:i

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

            r.ReadU8();                      // equipStage:c
            r.ReadU8();                      // equipStar:c
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
