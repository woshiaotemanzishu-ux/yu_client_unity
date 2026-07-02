using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 宝石镶嵌/拆除协议控制器(薄增量六件套第20轮工单;服务端 pt_152 段内 15208/15209)。
    /// 解主线 101175(ctype48「宝石强化总等级达1」,状态快照自动判定,无需专用任务代码)。
    /// 无专属壳:动作由后续 UI(装备位+背包宝石选择)接,本轮只打通协议收发 + toast/事件联动。
    /// </summary>
    public sealed class EquipStoneController : BaseController
    {
        public static readonly EquipStoneController Instance = new EquipStoneController();

        private EquipStoneController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.EQUIP_STONE_SET, On15208);
            RegisterProtocal(Proto.EQUIP_STONE_UNSET, On15209);
        }

        /// <summary>15208 镶嵌(发 "ccl" equipPos,stonePos,goodsId)。</summary>
        public void SetStone(int equipPos, int stonePos, long goodsId)
        {
            SendFmt(Proto.EQUIP_STONE_SET, "ccl", equipPos, stonePos, goodsId);
            GameLog.Info("Equip", "setStone 15208 equipPos={0} stonePos={1} goodsId={2}", equipPos, stonePos, goodsId);
        }

        /// <summary>15209 拆除(发 "cc" equipPos,stonePos)。</summary>
        public void UnsetStone(int equipPos, int stonePos)
        {
            SendFmt(Proto.EQUIP_STONE_UNSET, "cc", equipPos, stonePos);
            GameLog.Info("Equip", "unsetStone 15209 equipPos={0} stonePos={1}", equipPos, stonePos);
        }

        /// <summary>15208 回包:res:i, equip_type:c, pos:c, type_id:i。res==1 → toast「镶嵌成功」+ 复用
        /// EVT_EQUIP_STREN_UPDATE(装备属性联动刷新,同强化事件);res!=1 显码降级(错误码表未移植)。</summary>
        private void On15208(NetReader r)
        {
            int res = (int)r.ReadU32();
            int equipType = r.ReadU8();
            int pos = r.ReadU8();
            int typeId = (int)r.ReadU32();
            if (res != 1)
            {
                TipsManager.Toast("镶嵌失败(" + res + ")");   // 错误码表未移植,显码降级
                GameLog.Info("Equip", "15208 fail res={0} equip_type={1} pos={2}", res, equipType, pos);
                return;
            }
            TipsManager.Toast("镶嵌成功");
            GameLog.Info("Equip", "15208 ok equip_type={0} pos={1} type_id={2} remaining={3}B", equipType, pos, typeId, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_STREN_UPDATE);
        }

        /// <summary>15209 回包:res:i, equip_type:c, pos:c。res==1 → toast「拆除成功」+ EVT_EQUIP_STREN_UPDATE;
        /// res!=1 显码降级。</summary>
        private void On15209(NetReader r)
        {
            int res = (int)r.ReadU32();
            int equipType = r.ReadU8();
            int pos = r.ReadU8();
            if (res != 1)
            {
                TipsManager.Toast("拆除失败(" + res + ")");   // 错误码表未移植,显码降级
                GameLog.Info("Equip", "15209 fail res={0} equip_type={1} pos={2}", res, equipType, pos);
                return;
            }
            TipsManager.Toast("拆除成功");
            GameLog.Info("Equip", "15209 ok equip_type={0} pos={1} remaining={2}B", equipType, pos, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_STREN_UPDATE);
        }
    }
}
