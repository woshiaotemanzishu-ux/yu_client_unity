using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Bag;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 吞天洗魄(洗炼)协议控制器(自动循环 轮4 队列#4;对标老端 EquipController.ts on15212/on15213/on15214/on15252;
    /// 服务端 pt_152)。UI 挂 EquipView tab3(EquipWashView)。
    ///
    /// 15213 wire 是**手写序列**,严禁按 "cAc" 字面翻译(对标 EquipController.ts:59-71 WriteBegin(15213)):
    /// c(equip_type) + h(锁定槽数量) + c[](锁定槽下标+1,变长,仅数量&gt;0 时写) + c(ratio_plus)。15212 的 index
    /// 同样传 index+1(老端 EquipWashPropItem.ts:88)。
    ///
    /// 15213/15252 成功回包都没给出"变动后的新值"(15213 只给变动下标不给新属性值,15252 不给新段位——见服务端
    /// 侦察报告),故经 <see cref="GoodsDynamicModel.Invalidate"/> 强刷该装备详情缓存,对标老端
    /// GetDynamic(goodsId, cb, <b>true</b>) 强制重拉。照 EquipStrenController 模板。
    /// </summary>
    public sealed class EquipWashController : BaseController
    {
        public static readonly EquipWashController Instance = new EquipWashController();

        private EquipWashController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.EQUIP_WASH_OPEN_SLOT, On15212);
            RegisterProtocal(Proto.EQUIP_WASH_DO, On15213);
            RegisterProtocal(Proto.EQUIP_WASH_FREE_TIMES, On15214);
            RegisterProtocal(Proto.EQUIP_WASH_DIVISION, On15252);
            // 老端 GAME_START 发一次 15214(免费次数;EquipController.ts:215)。
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, RequestFreeTimes);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, RequestFreeTimes);
            EquipWashModel.Instance.Clear();
            base.Dispose();
        }

        // 15212 回包本身不带 equip_type(只有 goods_id/index),记发送时的 pending equip_type 供 On15212 补齐落库。
        private int _pendingOpenEquipType;

        /// <summary>15212 开启洗魄槽(发 "cc" equip_type, index+1;老端如此,对标 EquipWashPropItem.ts:88)。</summary>
        public void OpenSlot(int equipType, int index)
        {
            _pendingOpenEquipType = equipType;
            SendFmt(Proto.EQUIP_WASH_OPEN_SLOT, "cc", equipType, index + 1);
            GameLog.Info("Equip", "openWashSlot 15212 equip_type={0} index={1}(+1={2})", equipType, index, index + 1);
        }

        /// <summary>
        /// 15213 洗魄执行(手写序列,详见类注释)。ratio_plus:0 普通/1 紫色保底/2 红色保底/3 橙色保底
        /// (服务端 guard 只接受 0..3,对标 EquipWashView.ts is_up_/ratio_plus_)。
        /// </summary>
        public void WashExecute(int equipType, IReadOnlyList<int> lockedIndices, int ratioPlus)
        {
            int count = lockedIndices?.Count ?? 0;
            var fmt = new StringBuilder("ch");
            var args = new List<object>(2 + count + 1) { equipType, count };
            if (lockedIndices != null)
            {
                foreach (int idx in lockedIndices)
                {
                    fmt.Append('c');
                    args.Add(idx + 1);   // 老端锁定位 index+1
                }
            }
            fmt.Append('c');
            args.Add(ratioPlus);
            SendFmt(Proto.EQUIP_WASH_DO, fmt.ToString(), args.ToArray());
            GameLog.Info("Equip", "washExecute 15213 equip_type={0} lockCount={1} ratioPlus={2}", equipType, count, ratioPlus);
        }

        /// <summary>15214 免费次数查询(无参;对标 GAME_START + 15213 成功后连锁重发)。</summary>
        public void RequestFreeTimes()
        {
            SendFmt(Proto.EQUIP_WASH_FREE_TIMES);
            GameLog.Info("Equip", "request 15214(洗魄免费次数)");
        }

        /// <summary>15252 升段(发 "cc" equip_type, is_buy 0/1;对标 EquipWashView.ts:505)。</summary>
        public void UpgradeDivision(int equipType, int isBuy)
        {
            SendFmt(Proto.EQUIP_WASH_DIVISION, "cc", equipType, isBuy);
            GameLog.Info("Equip", "upgradeDivision 15252 equip_type={0} isBuy={1}", equipType, isBuy);
        }

        /// <summary>15212 回包:res:i, goods_id:l, index:c。res==1 → toast「开启成功」+ EVT_EQUIP_WASH_UPDATE;
        /// res!=1 显码降级(常见=等级不足/未按序开启/钻石不足,错误码表未移植)。</summary>
        private void On15212(NetReader r)
        {
            int res = (int)r.ReadU32();
            long goodsId = r.ReadU64();
            int index = r.ReadU8();
            if (res != 1)
            {
                TipsManager.Toast("开启洗魄槽失败(" + res + ")");
                GameLog.Info("Equip", "15212 fail res={0} goods_id={1} index={2}", res, goodsId, index);
                return;
            }
            TipsManager.Toast("开启洗魄槽成功");
            GameLog.Info("Equip", "15212 ok goods_id={0} index={1} remaining={2}B", goodsId, index, r.Remaining);
            EquipWashModel.Instance.MarkSlotOpened(_pendingOpenEquipType, index);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_WASH_UPDATE);
        }

        /// <summary>15213 回包:res:i, goods_id:l, attr_list[u16×{index:c}]。res==1 → toast「洗魄成功」+
        /// GoodsDynamicModel.Invalidate(强刷,回包只给变动下标不给新属性值)+清本地锁定选择+连锁 15214;
        /// res!=1 显码降级。</summary>
        private void On15213(NetReader r)
        {
            int res = (int)r.ReadU32();
            long goodsId = r.ReadU64();
            List<int> attrIdx = r.ReadArray(rr => (int)rr.ReadU8());
            if (res != 1)
            {
                TipsManager.Toast("洗魄失败(" + res + ")");
                GameLog.Info("Equip", "15213 fail res={0} goods_id={1}", res, goodsId);
                return;
            }
            TipsManager.Toast("洗魄成功");
            GameLog.Info("Equip", "15213 ok goods_id={0} attrCount={1} remaining={2}B", goodsId, attrIdx.Count, r.Remaining);
            GoodsDynamicModel.Instance.Invalidate(goodsId);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_WASH_UPDATE);
            RequestFreeTimes();   // 对标老端 on15213 成功后连锁 SendFmtToGame(15214)
        }

        /// <summary>15214 回包:free_times:c(无 res 字段,纯查询)。</summary>
        private void On15214(NetReader r)
        {
            int freeTimes = r.ReadU8();
            EquipWashModel.Instance.ApplyFreeTimes(freeTimes);
            GameLog.Info("Equip", "15214 free_times={0} remaining={1}B", freeTimes, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_WASH_UPDATE);
        }

        /// <summary>15252 回包:res:i, goods_id:l(新段位没打包进协议,客户端拿不到,只能靠详情重拉/后续接口间接得知)。
        /// res==1 → toast「升段成功」+ Invalidate 强刷;res!=1 显码降级。</summary>
        private void On15252(NetReader r)
        {
            int res = (int)r.ReadU32();
            long goodsId = r.ReadU64();
            if (res != 1)
            {
                TipsManager.Toast("升段失败(" + res + ")");
                GameLog.Info("Equip", "15252 fail res={0} goods_id={1}", res, goodsId);
                return;
            }
            TipsManager.Toast("升段成功");
            GameLog.Info("Equip", "15252 ok goods_id={0} remaining={1}B", goodsId, r.Remaining);
            GoodsDynamicModel.Instance.Invalidate(goodsId);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_WASH_UPDATE);
        }
    }
}
