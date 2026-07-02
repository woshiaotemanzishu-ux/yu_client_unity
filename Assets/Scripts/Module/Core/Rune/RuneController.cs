using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>
    /// 灵魄镶嵌协议控制器(对标老端 RuneBagItem.ts「cl」发包 + pp_rune.erl 镶嵌成功 rune_num 事件;pt_167)。
    /// 主线 100990(ctype33)=镶嵌一次(孔位1无条件开放)。
    ///
    /// 15010(物品容器全量)已被 <see cref="Bag.BagController"/> 独占注册(单 handler,NetManager 不支持同协议多路注册)——
    /// 本控制器**不再注册** 15010。符文背包(loc=rune_bag,pos=<see cref="RUNE_BAG_POS"/>=11,老端 GOODS_POS_TYPE.rune_bag,
    /// 出处 h5/src/commonModel/GoodsModel.ts:312)数据改经 BagController.On15010 的既有「非背包 pos 跳过」分支例外接住
    /// (该文件已按第19轮工单注释放行 pos==rune_bag 转存到 <see cref="RuneModel.SetRuneBag"/>,≤10 行改动)。
    /// </summary>
    public sealed class RuneController : BaseController
    {
        public static readonly RuneController Instance = new RuneController();

        /// <summary>符文背包 pos 值(老端 GOODS_POS_TYPE.rune_bag=11,h5/src/commonModel/GoodsModel.ts:312)。</summary>
        public const int RUNE_BAG_POS = 11;

        private RuneController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.RUNE_INFO, On16700);
            RegisterProtocal(Proto.RUNE_WEAR, On16701);
            RegisterProtocal(Proto.RUNE_UPGRADE, On16702);
        }

        public override void Dispose()
        {
            RuneModel.Instance.Clear();
            base.Dispose();
        }

        /// <summary>请求符文全量(对标打开面板时发 16700,无参)。</summary>
        public void RequestInfo()
        {
            SendFmt(Proto.RUNE_INFO);
            GameLog.Info("Rune", "request 16700 rune info(灵魄镶嵌)");
        }

        /// <summary>请求符文背包(发 15010 "h" pos=rune_bag;回包经 BagController.On15010 例外分支转存)。</summary>
        public void RequestRuneBag()
        {
            SendFmt(Proto.GOODS_CONTAINER_INFO, "h", RUNE_BAG_POS);
            GameLog.Info("Rune", "request 15010 rune_bag pos={0}(对标老端 GoodsController SendFmtToGame(15010,h,rune_bag))", RUNE_BAG_POS);
        }

        /// <summary>镶嵌符文(对标 RuneBagItem.ts:31 发 "cl" pos_id, goods_id)。</summary>
        public void Wear(int posId, long goodsId)
        {
            if (posId <= 0 || goodsId <= 0) return;
            SendFmt(Proto.RUNE_WEAR, "cl", posId, goodsId);
            GameLog.Info("Rune", "wear 16701 pos_id={0} goods_id={1}", posId, goodsId);
        }

        /// <summary>灵魄强化(发 "l" goods_id 已穿戴符文实例;解主线 101525 ctype50「御魂总等级达3」)。</summary>
        public void Upgrade(long goodsId)
        {
            if (goodsId <= 0) return;
            SendFmt(Proto.RUNE_UPGRADE, "l", goodsId);
            GameLog.Info("Rune", "upgrade 16702 goods_id={0}", goodsId);
        }

        /// <summary>16700 全量:rune_point:i, rune_chip:i, skill_lv:h, rune_list[u16×单项], rune_sum_power:l。</summary>
        private void On16700(NetReader r)
        {
            int runePoint = (int)r.ReadU32();   // rune_point:i
            int runeChip = (int)r.ReadU32();    // rune_chip:i
            r.ReadU16();                        // skill_lv:h(未移植技能线,按序读过)
            List<RuneModel.SlotVo> slots = r.ReadArray(ReadSlot);
            long sumPower = r.ReadI64();         // rune_sum_power:l

            RuneModel.Instance.Apply16700(runePoint, runeChip, slots, sumPower);
            GameLog.Info("Rune", "16700 point={0} chip={1} slots={2} sumPower={3} remaining={4}B",
                runePoint, runeChip, slots.Count, sumPower, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_RUNE_UPDATE);
        }

        /// <summary>16701 镶嵌结果:code:i, pos_id:c, new_goods_id:l, old_goods_id:l, new_goods_type_id:i。</summary>
        private void On16701(NetReader r)
        {
            int code = (int)r.ReadU32();          // code:i
            int posId = r.ReadU8();                // pos_id:c
            long newGoodsId = r.ReadU64();         // new_goods_id:l
            r.ReadU64();                           // old_goods_id:l(未消费)
            int newGoodsTypeId = (int)r.ReadU32(); // new_goods_type_id:i

            if (code != 1)
            {
                TipsManager.Toast("镶嵌失败(" + code + ")");   // 错误码表(Util.ErrorCodeShow)未移植,显码降级
                GameLog.Info("Rune", "16701 wear fail code={0} pos={1}", code, posId);
                return;
            }

            RuneModel.Instance.Apply16701(posId, newGoodsId, newGoodsTypeId);
            TipsManager.Toast("镶嵌成功");
            GameLog.Info("Rune", "16701 wear ok pos={0} newGoodsId={1} newGoodsTypeId={2} remaining={3}B",
                posId, newGoodsId, newGoodsTypeId, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_RUNE_UPDATE);

            RequestInfo();   // 镶嵌后再拉一次全量刷新(对标老端镶嵌成功后刷面板)
        }

        /// <summary>16702 强化结果:code:i, rune_point:i, goods_id:l。code==1 成功 → 更新 RunePoint(符文经验,
        /// 镶嵌溢出自然产生)+ 再拉一次 16700 刷新(服务端随后自动触发 rune_lv 事件驱动 ctype49/50 判定)。
        /// code!=1(常见=经验不足)显码降级(err167,Util.ErrorCodeShow 未移植)。</summary>
        private void On16702(NetReader r)
        {
            int code = (int)r.ReadU32();       // code:i
            int runePoint = (int)r.ReadU32();  // rune_point:i
            long goodsId = r.ReadU64();        // goods_id:l

            if (code != 1)
            {
                TipsManager.Toast("强化失败(" + code + ")");   // 错误码表(Util.ErrorCodeShow)未移植,显码降级(常见:经验不足 err167)
                GameLog.Info("Rune", "16702 upgrade fail code={0} goods_id={1}", code, goodsId);
                return;
            }

            RuneModel.Instance.ApplyRunePoint(runePoint);
            TipsManager.Toast("强化成功");
            GameLog.Info("Rune", "16702 upgrade ok rune_point={0} goods_id={1} remaining={2}B", runePoint, goodsId, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_RUNE_UPDATE);

            RequestInfo();   // 强化后再拉一次全量刷新(对标既有镶嵌成功后刷面板约定)
        }

        /// <summary>读 16700 rune_list 单项:pos_id:c, if_open:c, goods_id:l, goods_type_id:i, color:c, lv:h, attr_list[u16×6项]。</summary>
        private static RuneModel.SlotVo ReadSlot(NetReader r)
        {
            var vo = new RuneModel.SlotVo
            {
                PosId = r.ReadU8(),              // pos_id:c
                IfOpen = r.ReadU8() != 0,        // if_open:c
                GoodsId = r.ReadU64(),           // goods_id:l
                GoodsTypeId = (int)r.ReadU32(),  // goods_type_id:i
                Color = r.ReadU8(),              // color:c
                Lv = r.ReadU16(),                // lv:h
            };
            r.ReadArray(ReadAttr);               // attr_list[u16×{attr_id:i,attr_num:i,awake_lv:i,awake_exp:i,next_power:l,cur_power:l}](未移植属性面板,按序读过)
            return vo;
        }

        /// <summary>attr_list 单项:attr_id:i, attr_num:i, awake_lv:i, awake_exp:i, next_power:l, cur_power:l(6 个字段按序读完)。</summary>
        private static int ReadAttr(NetReader r)
        {
            r.ReadU32();   // attr_id:i
            r.ReadU32();   // attr_num:i
            r.ReadU32();   // awake_lv:i
            r.ReadU32();   // awake_exp:i
            r.ReadU64();   // next_power:l
            r.ReadU64();   // cur_power:l
            return 0;
        }
    }
}
