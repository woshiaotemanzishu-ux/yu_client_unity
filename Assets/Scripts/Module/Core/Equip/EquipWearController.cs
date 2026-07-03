using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备穿戴协议控制器(薄增量六件套第20轮工单;服务端 pt_152 段内 15201)。
    /// 解主线 101205(ctype93「穿3件3阶橙装」,状态快照自动判定,无需专用任务代码)。
    /// 入口:<see cref="Common.Views.ItemTipsView"/> 装备分支[穿戴]按钮(goods 实例存在且 IsEquip)。
    /// </summary>
    public sealed class EquipWearController : BaseController
    {
        public static readonly EquipWearController Instance = new EquipWearController();

        private EquipWearController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.EQUIP_WEAR, On15201);
            // 自动穿戴(对标老端一键穿戴,自动任务模式代行;见 EquipAutoWear 头注释):
            // 背包变化防抖触发;进游戏后请求装备通道(15010 pos=1)供 rating 比较。
            EventDispatcher.On(GlobalEvent.EVT_BAG_UPDATE, EquipAutoWear.OnBagUpdate);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, RequestWornList);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_BAG_UPDATE, EquipAutoWear.OnBagUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, RequestWornList);
            EquipAutoWear.Clear();
            base.Dispose();
        }

        /// <summary>请求已穿戴装备全量(15010 pos=equip=1;回包经 BagController.On15010 转存 EquipAutoWear)。</summary>
        public void RequestWornList()
        {
            SendFmt(Proto.GOODS_CONTAINER_INFO, "h", EquipAutoWear.POS_EQUIP);
            GameLog.Info("Equip", "request 15010 equip pos={0}(自动穿戴 rating 比较用)", EquipAutoWear.POS_EQUIP);
        }

        /// <summary>15201 穿戴(发 "l" goodsId 实例id)。</summary>
        public void Wear(long goodsId)
        {
            SendFmt(Proto.EQUIP_WEAR, "l", goodsId);
            GameLog.Info("Equip", "wear 15201 goodsId={0}", goodsId);
        }

        /// <summary>15201 回包:res:i, goods_id:l, old_goods_id:l, type_id:i, cell_pos:c。
        /// res==1 → toast「穿戴成功」+ 复用 EVT_BAG_UPDATE(背包/装备格联动刷新);res!=1 显码降级。</summary>
        private void On15201(NetReader r)
        {
            int res = (int)r.ReadU32();
            long goodsId = r.ReadU64();
            long oldGoodsId = r.ReadU64();
            int typeId = (int)r.ReadU32();
            int cellPos = r.ReadU8();
            if (res != 1)
            {
                TipsManager.Toast("穿戴失败(" + res + ")");   // 错误码表未移植,显码降级
                GameLog.Info("Equip", "15201 fail res={0} goods_id={1}", res, goodsId);
                return;
            }
            TipsManager.Toast("穿戴成功");
            GameLog.Info("Equip", "15201 ok goods_id={0} old_goods_id={1} type_id={2} cell_pos={3} remaining={4}B",
                goodsId, oldGoodsId, typeId, cellPos, r.Remaining);
            RequestWornList();   // 穿戴成功后刷新装备通道(对标老端 on15201 连锁刷新)
            EventDispatcher.Emit(GlobalEvent.EVT_BAG_UPDATE);
        }
    }
}
