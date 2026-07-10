using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using UnityEngine;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// Goods 协议扩容(自动循环 轮1)实证:纯逻辑用例,不建 Stage/不渲染(仿 CliVerify.ProtoDeltaCase 套路 +
    /// EquipStrenCase 的反射喂包法)。手工按 ClientProtocol.json 字段顺序拼大端合成包,反射喂
    /// BagController 私有 On1500x handler,断言:
    ///   15000 全量详情字段 + 尾哨兵(字节游标对齐)+ EVT_GOODS_DETAIL_UPDATE;
    ///   15002 成功→BagModel.GetMaxCell 更新 + 事件;失败码→不改值不抛异常;
    ///   15019 成功→事件到 + BagModel 未被改(仅展示不落背包);失败码→不抛异常;
    ///   15026 分桶 + 按 id 升序排序;
    ///   15053 三态(成功/进入拾取计时/掉落已消失)→三个不同事件;
    ///   15055 仅 player_id==自己才落缓存(喂别人 id 的包断言不覆盖);
    ///   15090 toast 走到(log 断言)+ EVT_GOODS_DECOMPOSE_SUCCESS(与 15019 共用同一事件)。
    /// 日志前缀统一 "CLIVERIFY goodsproto"。
    /// </summary>
    public static class GoodsProtoCase
    {
        public static async Task<int> Run()
        {
            object ctrl = Shenxiao.Module.Core.Bag.BagController.Instance;
            const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

            MethodInfo GetM(string name)
            {
                MethodInfo m = ctrl.GetType().GetMethod(name, F);
                if (m == null) Debug.LogError("CLIVERIFY goodsproto handler missing(reflection): " + name);
                return m;
            }

            MethodInfo m15000 = GetM("On15000");
            MethodInfo m15002 = GetM("On15002");
            MethodInfo m15019 = GetM("On15019");
            MethodInfo m15026 = GetM("On15026");
            MethodInfo m15053 = GetM("On15053");
            MethodInfo m15055 = GetM("On15055");
            MethodInfo m15090 = GetM("On15090");
            if (m15000 == null || m15002 == null || m15019 == null || m15026 == null
                || m15053 == null || m15055 == null || m15090 == null)
            {
                return 3;
            }

            void Feed(MethodInfo m, Shenxiao.Framework.Net.NetReader reader) =>
                m.Invoke(ctrl, new object[] { reader });
            void FeedBytes(MethodInfo m, byte[] pkt) =>
                Feed(m, new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length));

            var bag = Shenxiao.Module.Core.Bag.BagModel.Instance;
            bag.Clear();
            Shenxiao.Module.Core.Bag.GoodsDynamicModel.Instance.Clear();
            Shenxiao.Module.Core.Bag.GoodsExchangeModel.Instance.Clear();
            Shenxiao.Module.Core.Bag.GoodsBuffModel.Instance.Clear();
            Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId = 999;   // 15055 自己/他人过滤要有确定的"自己" id

            bool detailOk = Test15000(m15000);
            bool expandOk = Test15002(m15002, FeedBytes, bag);
            bool decomposeOk = Test15019(m15019, FeedBytes, bag);
            bool exchangeListOk = Test15026(m15026, FeedBytes);
            bool dropOk = Test15053(m15053, FeedBytes);
            bool buffOk = Test15055(m15055, FeedBytes);
            bool autoDecomposeOk = Test15090(m15090, FeedBytes);

            bag.Clear();
            Shenxiao.Module.Core.Bag.GoodsDynamicModel.Instance.Clear();
            Shenxiao.Module.Core.Bag.GoodsExchangeModel.Instance.Clear();
            Shenxiao.Module.Core.Bag.GoodsBuffModel.Instance.Clear();

            bool pass = detailOk && expandOk && decomposeOk && exchangeListOk && dropOk && buffOk && autoDecomposeOk;
            Debug.Log("CLIVERIFY goodsproto VERDICT detail=" + detailOk + " expand=" + expandOk
                + " decompose=" + decomposeOk + " exchangeList=" + exchangeListOk + " drop=" + dropOk
                + " buff=" + buffOk + " autoDecompose=" + autoDecomposeOk + " pass=" + pass);
            await Task.CompletedTask;
            return pass ? 0 : 3;
        }

        // ---- 15000:全量详情 + 尾哨兵(字节游标对齐)----
        private static bool Test15000(MethodInfo m15000)
        {
            const long goodsId = 90001001L;
            const int typeId = 520100;
            const int color = 5;
            const int stren = 8;
            const long strenExp = 555;
            const long rating = 7777;
            const long washRating = 42;
            const int attrValue = 100;
            const int stoneTypeId = 88001;
            const int magicGoodsId = 77002;
            const int exAttrVal = 321;
            const int washAttrVal = 17;
            const int suitCount = 3;
            const int awakeExp = 30;
            const int refinementLv = 99;
            const int sentinel = 0xBEEF;

            byte[] packet = new CliVerify.Pkt()
                .L(goodsId).I(typeId).C(0).H(7).I(3)                    // goods_id/type_id/sub_pos/cell/num
                .C(0).C(1).C(1).C(color)                                // bind/trade/sell/color
                .I(0).I(123456).C(2).C(0).I(999)                        // expire/combat/equip_type/price_type/sell_price
                .H(stren).I(strenExp).I(rating).I(8888).C(1).I(washRating)   // stren/stren_exp/rating/overall/division/wash_rating
                .H(1).C(1).I(attrValue).C(3).I(50)                      // addition_attrlist[1]
                .H(1).C(1).I(stoneTypeId)                               // stone_list[1]
                .H(1).I(magicGoodsId).I(99999)                          // magic_list[1]
                .H(1).C(4).C(1).H(205).I(exAttrVal).C(5).I(10)          // equip_extra_attr[1]
                .H(1).C(1).C(2).H(99).I(washAttrVal)                    // wash_attr[1]
                .H(1).C(1).C(2).C(suitCount)                            // suit_list[1]
                .H(1).H(2).C(3).I(4001).C(5).I(4002).C(6)               // cspirit_stage/lv/awakening_lv/skill_id/lv×2
                .H(7).H(8).H(88)                                        // pet_equip_stage/star/level
                .H(1).H(10).I(20).I(awakeExp)                           // awake_list[1]
                .H(refinementLv)                                        // refinement_lv
                .H(sentinel)                                            // 尾哨兵(不属于 15000 schema)
                .Bytes();

            var reader = new Shenxiao.Framework.Net.NetReader(packet, 0, packet.Length);
            long emittedGoodsId = -1;
            Action<long> onDetail = id => emittedGoodsId = id;
            EventDispatcher.On(GlobalEvent.EVT_GOODS_DETAIL_UPDATE, onDetail);
            m15000.Invoke(Shenxiao.Module.Core.Bag.BagController.Instance, new object[] { reader });
            EventDispatcher.Off(GlobalEvent.EVT_GOODS_DETAIL_UPDATE, onDetail);

            int tail = reader.ReadU16();   // schema 读完后紧接着就是哨兵;若游标错位这里要么抛异常要么值不对
            bool sentinelOk = tail == sentinel;

            Shenxiao.Module.Core.Bag.GoodsDetailVo vo = Shenxiao.Module.Core.Bag.GoodsDynamicModel.Instance.Peek(goodsId);
            bool fieldsOk = vo != null
                && vo.TypeId == typeId && vo.Color == color && vo.Stren == stren && vo.StrenExp == strenExp
                && vo.Rating == rating && vo.WashRating == washRating
                && vo.AdditionAttrs?.Count == 1 && vo.AdditionAttrs[0].AttrValue == attrValue
                && vo.StoneList?.Count == 1 && vo.StoneList[0].TypeId == stoneTypeId
                && vo.MagicList?.Count == 1 && vo.MagicList[0].GoodsId == magicGoodsId
                && vo.ExtraAttrs?.Count == 1 && vo.ExtraAttrs[0].AttrVal == exAttrVal
                && vo.WashAttrs?.Count == 1 && vo.WashAttrs[0].AttrVal == washAttrVal
                && vo.SuitList?.Count == 1 && vo.SuitList[0].SuitCount == suitCount
                && vo.AwakeList?.Count == 1 && vo.AwakeList[0].AwakeExp == awakeExp
                && vo.RefinementLv == refinementLv;
            bool eventOk = emittedGoodsId == goodsId;

            bool ok = sentinelOk && fieldsOk && eventOk;
            Debug.Log("CLIVERIFY goodsproto 15000 sentinelOk=" + sentinelOk + " fieldsOk=" + fieldsOk
                + " eventOk=" + eventOk + " ok=" + ok);
            return ok;
        }

        // ---- 15002:扩容成功写容量 + 事件;失败码不改值不抛异常 ----
        private static bool Test15002(MethodInfo m15002, Action<MethodInfo, byte[]> feedBytes, Shenxiao.Module.Core.Bag.BagModel bag)
        {
            (int pos, int total) emitted = (-1, -1);
            Action<int, int> onMaxCell = (pos, total) => emitted = (pos, total);
            EventDispatcher.On(GlobalEvent.EVT_BAG_MAX_CELL, onMaxCell);

            byte[] ok1 = new CliVerify.Pkt().I(1).H(Shenxiao.Module.Core.Bag.BagModel.POS_BAG).H(80).Bytes();
            feedBytes(m15002, ok1);
            bool successOk = bag.GetMaxCell(Shenxiao.Module.Core.Bag.BagModel.POS_BAG) == 80
                && emitted.pos == Shenxiao.Module.Core.Bag.BagModel.POS_BAG && emitted.total == 80;

            byte[] fail = new CliVerify.Pkt().I(1500002).H(Shenxiao.Module.Core.Bag.BagModel.POS_BAG).H(999).Bytes();
            bool failNoThrow = true;
            try { feedBytes(m15002, fail); }
            catch (Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY goodsproto 15002 fail threw: " + e); }
            bool failUnchanged = bag.GetMaxCell(Shenxiao.Module.Core.Bag.BagModel.POS_BAG) == 80;   // 未被 999 覆盖

            EventDispatcher.Off(GlobalEvent.EVT_BAG_MAX_CELL, onMaxCell);

            bool ok = successOk && failNoThrow && failUnchanged;
            Debug.Log("CLIVERIFY goodsproto 15002 successOk=" + successOk + " failNoThrow=" + failNoThrow
                + " failUnchanged=" + failUnchanged + " ok=" + ok);
            return ok;
        }

        // ---- 15019:成功→事件到 + BagModel 未被改;失败码→不抛异常 ----
        private static bool Test15019(MethodInfo m15019, Action<MethodInfo, byte[]> feedBytes, Shenxiao.Module.Core.Bag.BagModel bag)
        {
            bag.SetBagFull(1, 40, new List<Shenxiao.Module.Core.Bag.BagGoods>
            {
                new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 1, TypeId = 520100, GoodsNum = 5, Color = 2, Cell = 1 },
            });

            List<(long goodsId, long goodsNum)> emitted = null;
            Action<List<(long, long)>> onDecompose = list => emitted = list;
            EventDispatcher.On(GlobalEvent.EVT_GOODS_DECOMPOSE_SUCCESS, onDecompose);

            byte[] ok1 = new CliVerify.Pkt().I(1).H(1).L(12345).I(3).Bytes();   // code=1, reward_list=1×{goods_id,goods_num}
            feedBytes(m15019, ok1);
            bool eventOk = emitted != null && emitted.Count == 1 && emitted[0].Item1 == 12345 && emitted[0].Item2 == 3;
            bool bagUntouched = bag.BagGoodsList.Count == 1 && bag.BagGoodsList[0].GoodsId == 1 && bag.BagGoodsList[0].GoodsNum == 5;

            byte[] fail = new CliVerify.Pkt().I(1500003).H(0).Bytes();   // 失败码,reward_list 空
            bool failNoThrow = true;
            try { feedBytes(m15019, fail); }
            catch (Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY goodsproto 15019 fail threw: " + e); }

            EventDispatcher.Off(GlobalEvent.EVT_GOODS_DECOMPOSE_SUCCESS, onDecompose);

            bool ok = eventOk && bagUntouched && failNoThrow;
            Debug.Log("CLIVERIFY goodsproto 15019 eventOk=" + eventOk + " bagUntouched=" + bagUntouched
                + " failNoThrow=" + failNoThrow + " ok=" + ok);
            return ok;
        }

        // ---- 15026:按 type 分桶 + 按 id 升序排序 ----
        private static bool Test15026(MethodInfo m15026, Action<MethodInfo, byte[]> feedBytes)
        {
            int emittedType = -1;
            Action<int> onList = t => emittedType = t;
            EventDispatcher.On(GlobalEvent.EVT_GOODS_EXCHANGE_LIST, onList);

            const int type = 7;
            byte[] packet = new CliVerify.Pkt()
                .H(type).H(3)
                    .I(30).H(5).C(1)   // 乱序:30
                    .I(10).H(2).C(0)   // 10
                    .I(20).H(1).C(1)   // 20
                .Bytes();
            feedBytes(m15026, packet);
            EventDispatcher.Off(GlobalEvent.EVT_GOODS_EXCHANGE_LIST, onList);

            List<Shenxiao.Module.Core.Bag.GoodsExchangeEntry> list = Shenxiao.Module.Core.Bag.GoodsExchangeModel.Instance.GetList(type);
            bool ok = emittedType == type && list != null && list.Count == 3
                && list[0].Id == 10 && list[1].Id == 20 && list[2].Id == 30;
            Debug.Log("CLIVERIFY goodsproto 15026 emittedType=" + emittedType + " order="
                + (list != null ? string.Join(",", list.ConvertAll(e => e.Id)) : "<null>") + " ok=" + ok);
            return ok;
        }

        // ---- 15053:三态(成功/进入拾取计时/掉落已消失)→三个不同事件 ----
        private static bool Test15053(MethodInfo m15053, Action<MethodInfo, byte[]> feedBytes)
        {
            Shenxiao.Module.Core.Bag.BagController.DropPickVo? successVo = null;
            Shenxiao.Module.Core.Bag.BagController.DropPickVo? beginVo = null;
            long dismissDropId = -1;
            Action<Shenxiao.Module.Core.Bag.BagController.DropPickVo> onSuccess = vo => successVo = vo;
            Action<Shenxiao.Module.Core.Bag.BagController.DropPickVo> onBegin = vo => beginVo = vo;
            Action<long> onDismiss = id => dismissDropId = id;
            EventDispatcher.On(GlobalEvent.EVT_DROP_PICK_SUCCESS, onSuccess);
            EventDispatcher.On(GlobalEvent.EVT_DROP_PICK_BEGIN, onBegin);
            EventDispatcher.On(GlobalEvent.EVT_DROP_DISMISS, onDismiss);

            // res==1 → 拾取成功
            feedBytes(m15053, new CliVerify.Pkt().I(1).S("").C(0).L(555).Bytes());
            // res!=1 且 status==1 → 进入拾取计时
            feedBytes(m15053, new CliVerify.Pkt().I(0).S("").C(1).L(556).Bytes());
            // res==1500020 → 掉落包已消失
            feedBytes(m15053, new CliVerify.Pkt().I(1500020).S("").C(0).L(557).Bytes());

            EventDispatcher.Off(GlobalEvent.EVT_DROP_PICK_SUCCESS, onSuccess);
            EventDispatcher.Off(GlobalEvent.EVT_DROP_PICK_BEGIN, onBegin);
            EventDispatcher.Off(GlobalEvent.EVT_DROP_DISMISS, onDismiss);

            bool ok = successVo != null && beginVo != null && dismissDropId == 557;
            Debug.Log("CLIVERIFY goodsproto 15053 success=" + (successVo != null) + " begin=" + (beginVo != null)
                + " dismissDropId=" + dismissDropId + " ok=" + ok);
            return ok;
        }

        // ---- 15055:仅 player_id==自己才落缓存(喂别人 id 的包断言不覆盖) ----
        private static bool Test15055(MethodInfo m15055, Action<MethodInfo, byte[]> feedBytes)
        {
            const long selfId = 999;
            const long otherId = 888;

            byte[] selfPkt = new CliVerify.Pkt()
                .L(selfId).H(1)
                    .I(1).C(1).S("[]").I(60).I(0)   // buff_list[0] = {goods_id=1,...}
                .Bytes();
            feedBytes(m15055, selfPkt);
            bool selfOk = Shenxiao.Module.Core.Bag.GoodsBuffModel.Instance.List.Count == 1
                && Shenxiao.Module.Core.Bag.GoodsBuffModel.Instance.List[0].GoodsId == 1;

            byte[] otherPkt = new CliVerify.Pkt()
                .L(otherId).H(1)
                    .I(2).C(1).S("[]").I(60).I(0)   // 别人的 buff_list,不应覆盖
                .Bytes();
            feedBytes(m15055, otherPkt);
            bool notOverwritten = Shenxiao.Module.Core.Bag.GoodsBuffModel.Instance.List.Count == 1
                && Shenxiao.Module.Core.Bag.GoodsBuffModel.Instance.List[0].GoodsId == 1;

            bool ok = selfOk && notOverwritten;
            Debug.Log("CLIVERIFY goodsproto 15055 selfOk=" + selfOk + " notOverwritten=" + notOverwritten + " ok=" + ok);
            return ok;
        }

        // ---- 15090:toast 走到(log 断言)+ 事件(与 15019 共用 EVT_GOODS_DECOMPOSE_SUCCESS) ----
        private static bool Test15090(MethodInfo m15090, Action<MethodInfo, byte[]> feedBytes)
        {
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;

            List<(long, long)> emitted = null;
            Action<List<(long, long)>> onDecompose = list => emitted = list;
            EventDispatcher.On(GlobalEvent.EVT_GOODS_DECOMPOSE_SUCCESS, onDecompose);
            try
            {
                // code=1, reward_list=1×{goods_id,goods_num}, goods_bag_type=11(符文), under_color=2(蓝色)
                byte[] packet = new CliVerify.Pkt().I(1).H(1).L(1).I(1).C(11).C(2).Bytes();
                feedBytes(m15090, packet);
            }
            finally
            {
                Application.logMessageReceived -= cb;
                EventDispatcher.Off(GlobalEvent.EVT_GOODS_DECOMPOSE_SUCCESS, onDecompose);
            }

            bool toastOk = logs.Exists(l => l.Contains("万魄藏容量不足"));
            bool eventOk = emitted != null && emitted.Count == 1;
            bool ok = toastOk && eventOk;
            Debug.Log("CLIVERIFY goodsproto 15090 toastOk=" + toastOk + " eventOk=" + eventOk + " ok=" + ok);
            return ok;
        }
    }
}
