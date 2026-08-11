using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 神兵淬炼(精炼)协议控制器(自动循环 轮4 队列#4;对标老端 EquipController.ts on15250/on15251;服务端 pt_152,
    /// 15250/15251 是唯二"连查询都没等级门"的号)。UI 挂 EquipView tab1(EquipSmeltView,文案"神兵淬炼"),但老端
    /// 底层变量/事件全叫 Smelt——与 15255"神炼"(EquipRefinementController)是两套完全独立系统,命名相似,别混。
    /// 照 EquipStrenController 模板:单例 + Register 收发 + SendFmt + NetReader 手解 + TipsManager + EventDispatcher。
    /// </summary>
    public sealed class EquipSmeltController : BaseController
    {
        public static readonly EquipSmeltController Instance = new EquipSmeltController();

        private EquipSmeltController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.EQUIP_SMELT_INFO, On15250);
            RegisterProtocal(Proto.EQUIP_SMELT_DO, On15251);
        }

        public override void Dispose()
        {
            EquipSmeltModel.Instance.Clear();
            base.Dispose();
        }

        /// <summary>15250 查询指定槽位精炼信息(发 "c" equip_type)。</summary>
        public void QuerySmelt(int equipType)
        {
            SendFmt(Proto.EQUIP_SMELT_INFO, "c", equipType);
        }

        /// <summary>15251 单件精炼(发 "cc" equip_type,1;对标 EquipSmeltView.ts:172 btnStrOne)。</summary>
        public void SmeltOne(int equipType)
        {
            SendFmt(Proto.EQUIP_SMELT_DO, "cc", equipType, 1);
            GameLog.Info("Equip", "smeltOne 15251 equip_type={0} type=1", equipType);
        }

        /// <summary>15251 一键精炼(发 "cc" 当前选中 equip_type,2；老端 EquipSmeltView.ts:175-187
        /// 明确沿用当前部位，不是 15205 一键强化的 equip_type=0 语义)。</summary>
        public void SmeltAll(int equipType)
        {
            SendFmt(Proto.EQUIP_SMELT_DO, "cc", equipType, 2);
            GameLog.Info("Equip", "smeltAll 15251 equip_type={0} type=2", equipType);
        }

        /// <summary>15250 回包:res:i, equip_type:c, refine:h, refine_high:h。res==1 才落库;查询失败只日志不弹 toast。</summary>
        private void On15250(NetReader r)
        {
            int res = (int)r.ReadU32();
            int equipType = r.ReadU8();
            int refine = r.ReadU16();
            int refineHigh = r.ReadU16();
            if (res != 1)
            {
                GameLog.Info("Equip", "15250 query fail res={0} equip_type={1}", res, equipType);
                return;
            }
            EquipSmeltModel.Instance.Apply15250(equipType, refine, refineHigh);
            GameLog.Info("Equip", "15250 equip_type={0} refine={1} refineHigh={2} remaining={3}B",
                equipType, refine, refineHigh, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_SMELT_UPDATE);
        }

        /// <summary>15251 回包:res:i, res1:c, type:c, refine_info[u16×{equip_type:c, refine_high:h}]。
        /// res==1 → 落库;refine_info 为空(已满级/无可精炼项,对标老端 res==1 分支的空列表提示)→ toast「精炼失败」;
        /// res!=1 且 res1!=0 → toast「精炼失败」(对标老端 ALL_SMELT_FAIL);其余 res!=1 显码降级。</summary>
        private void On15251(NetReader r)
        {
            int res = (int)r.ReadU32();
            int res1 = r.ReadU8();
            int type = r.ReadU8();
            List<(int equipType, int refineHigh)> refineInfo = r.ReadArray(ReadRefineInfo);
            if (res != 1)
            {
                if (res1 != 0)
                {
                    TipsManager.Toast("精炼失败");   // 对标老端 ALL_SMELT_FAIL(res!=1 且 res1!=0)
                }
                else
                {
                    TipsManager.Toast("精炼失败(" + res + ")");   // 错误码表未移植,显码降级
                }
                GameLog.Info("Equip", "15251 fail res={0} res1={1} type={2}", res, res1, type);
                return;
            }
            EquipSmeltModel.Instance.Apply15251(refineInfo);
            if (refineInfo.Count < 1)
            {
                TipsManager.Toast("精炼失败");   // 对标老端:res==1 但 refine_info 为空(已满级/无可精炼项)
            }
            else
            {
                // 老端成功分支直接播放页面演出并刷新，不额外弹成功 toast。
            }
            GameLog.Info("Equip", "15251 ok res1={0} type={1} count={2} remaining={3}B",
                res1, type, refineInfo.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_SMELT_UPDATE);
        }

        private static (int equipType, int refineHigh) ReadRefineInfo(NetReader r)
        {
            return (r.ReadU8(), r.ReadU16());   // {equip_type:c, refine_high:h}
        }
    }
}
