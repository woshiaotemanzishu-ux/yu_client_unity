using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 装备强化协议控制器(对标老端 EquipController.ts:312-339 on15204/on15205;服务端 pt_152)。
    /// 老端进面板才查(EquipStrenView.ts LoadSuccess),GameStart 不主动查询;主线 100720(ctype31 StrenSum)
    /// 由服务端 equip_sum 事件驱动,不依赖本地查询结果。
    /// 老端锚点:EquipStrenView.ts:199(单件发 "cc" equip_type,1)/:219(一键发 "cc" 0,2)。
    /// </summary>
    public sealed class EquipStrenController : BaseController
    {
        public static readonly EquipStrenController Instance = new EquipStrenController();

        private EquipStrenController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.EQUIP_STREN_INFO, On15204);
            RegisterProtocal(Proto.EQUIP_STREN_DO, On15205);
        }

        public override void Dispose()
        {
            EquipStrenModel.Instance.Clear();
            base.Dispose();
        }

        /// <summary>15204 查询指定槽位强化信息(发 "c" equip_type)。</summary>
        public void QueryStren(int equipType)
        {
            SendFmt(Proto.EQUIP_STREN_INFO, "c", equipType);
        }

        /// <summary>15205 单件强化(发 "cc" equip_type,1;对标 EquipStrenView.ts:199)。</summary>
        public void StrenOne(int equipType)
        {
            SendFmt(Proto.EQUIP_STREN_DO, "cc", equipType, 1);
            GameLog.Info("Equip", "strenOne 15205 equip_type={0} type=1", equipType);
        }

        /// <summary>15205 一键强化(发 "cc" 0,2;对标 EquipStrenView.ts:219)。</summary>
        public void StrenAll()
        {
            SendFmt(Proto.EQUIP_STREN_DO, "cc", 0, 2);
            GameLog.Info("Equip", "strenAll 15205 equip_type=0 type=2");
        }

        /// <summary>15204 回包:res:i, equip_type:c, stren:h。res==1 才落库;查询失败只日志不弹 toast。</summary>
        private void On15204(NetReader r)
        {
            int res = (int)r.ReadU32();
            int equipType = r.ReadU8();
            int stren = r.ReadU16();
            if (res != 1)
            {
                GameLog.Info("Equip", "15204 query fail res={0} equip_type={1}", res, equipType);
                return;
            }
            EquipStrenModel.Instance.Apply15204(equipType, stren);
            GameLog.Info("Equip", "15204 equip_type={0} stren={1} remaining={2}B", equipType, stren, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_STREN_UPDATE);
        }

        /// <summary>15205 回包:res:i, res1:c, type:c, stren_info[u16×{equip_type:c, stren:h}]。
        /// res==1 → 逐项落库 + toast「强化成功」;res!=1 → toast「强化失败(res)」(常见=铜币不足/已到上限)。</summary>
        private void On15205(NetReader r)
        {
            int res = (int)r.ReadU32();
            int res1 = r.ReadU8();
            int type = r.ReadU8();
            List<(int equipType, int stren)> strenInfo = r.ReadArray(ReadStrenInfo);
            if (res != 1)
            {
                TipsManager.Toast("强化失败(" + res + ")");   // 常见=铜币不足/已到上限,错误码表未移植,显码降级
                GameLog.Info("Equip", "15205 fail res={0} res1={1} type={2}", res, res1, type);
                return;
            }
            EquipStrenModel.Instance.Apply15205(strenInfo);
            TipsManager.Toast("强化成功");
            GameLog.Info("Equip", "15205 ok res1={0} type={1} count={2} total={3} remaining={4}B",
                res1, type, strenInfo.Count, EquipStrenModel.Instance.TotalStren(), r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_STREN_UPDATE);
        }

        private static (int equipType, int stren) ReadStrenInfo(NetReader r)
        {
            return (r.ReadU8(), r.ReadU16());   // {equip_type:c, stren:h}
        }
    }
}
