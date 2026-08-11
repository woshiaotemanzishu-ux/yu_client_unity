using System;
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

        public readonly struct StrengthResult
        {
            public readonly int Result;
            public readonly int FailedEquipType;
            public readonly int Type;
            public readonly IReadOnlyList<(int equipType, int stren)> Items;

            public StrengthResult(int result, int failedEquipType, int type,
                IReadOnlyList<(int equipType, int stren)> items)
            {
                Result = result;
                FailedEquipType = failedEquipType;
                Type = type;
                Items = items;
            }
        }

        public event Action<StrengthResult> StrengthCompleted;

        protected override void Register()
        {
            RegisterProtocal(Proto.EQUIP_STREN_INFO, On15204);
            RegisterProtocal(Proto.EQUIP_STREN_DO, On15205);
            // 全身奖励(自动循环 轮4 队列#4;规格 §0 指定挂本控制器):15260 激活/15261 列表。
            RegisterProtocal(Proto.EQUIP_WHOLE_ACTIVE, On15260);
            RegisterProtocal(Proto.EQUIP_WHOLE_LIST, On15261);
        }

        public override void Dispose()
        {
            StrengthCompleted = null;
            EquipStrenModel.Instance.Clear();
            EquipWholeAwardModel.Instance.Clear();
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
                if (res != 1520090) TipsManager.Toast("强化失败(" + res + ")");
                GameLog.Info("Equip", "15205 fail res={0} res1={1} type={2}", res, res1, type);
                StrengthCompleted?.Invoke(new StrengthResult(res, res1, type, strenInfo));
                return;
            }
            if (strenInfo.Count == 0)
            {
                TipsManager.Toast("强化失败");
                GameLog.Info("Equip", "15205 res=1 但 stren_info 为空 type={0}", type);
                StrengthCompleted?.Invoke(new StrengthResult(0, res1, type, strenInfo));
                return;
            }
            EquipStrenModel.Instance.Apply15205(strenInfo);
            foreach ((int equipType, int stren) in strenInfo)
            {
                Shenxiao.Module.Core.Bag.BagGoods worn = Shenxiao.Module.Core.Equip.EquipAutoWear.GetWorn(equipType);
                if (worn != null) worn.Stren = stren;
            }
            GameLog.Info("Equip", "15205 ok res1={0} type={1} count={2} total={3} remaining={4}B",
                res1, type, strenInfo.Count, EquipStrenModel.Instance.TotalStren(), r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_STREN_UPDATE);
            StrengthCompleted?.Invoke(new StrengthResult(1, res1, type, strenInfo));
        }

        private static (int equipType, int stren) ReadStrenInfo(NetReader r)
        {
            return (r.ReadU8(), r.ReadU16());   // {equip_type:c, stren:h}
        }

        // ----- 全身奖励(自动循环 轮4 队列#4;老端 EquipStrenMasterView.ts/EquipJewelMasterView.ts 共用) -----

        /// <summary>15260 激活全身奖励(发 "c" type;本轮只用 type=1 强化,type=3 宝石留 4b)。</summary>
        public void ActivateWhole(int type)
        {
            SendFmt(Proto.EQUIP_WHOLE_ACTIVE, "c", type);
            GameLog.Info("Equip", "activateWhole 15260 type={0}", type);
        }

        /// <summary>15261 查询全身奖励列表(无参)。</summary>
        public void QueryWholeAward()
        {
            SendFmt(Proto.EQUIP_WHOLE_LIST);
            GameLog.Info("Equip", "request 15261(全身奖励列表)");
        }

        /// <summary>15260 回包:errcode:i, type:c, whole_lv:h。errcode==1 → 落库 + toast「激活成功」;
        /// 否则显码降级(常见=等级/阶段未达标,错误码表未移植)。</summary>
        private void On15260(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int type = r.ReadU8();
            int wholeLv = r.ReadU16();
            if (errcode != 1)
            {
                TipsManager.Toast("激活失败(" + errcode + ")");
                GameLog.Info("Equip", "15260 fail errcode={0} type={1}", errcode, type);
                return;
            }
            EquipWholeAwardModel.Instance.Update(type, wholeLv);
            GameLog.Info("Equip", "15260 ok type={0} whole_lv={1} remaining={2}B", type, wholeLv, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_WHOLE_UPDATE);
        }

        /// <summary>15261 回包:list[u16×{type:c, whole_lv:h}]。纯查询,无 errcode,直接整表覆盖落库。</summary>
        private void On15261(NetReader r)
        {
            List<(int type, int wholeLv)> list = r.ReadArray(ReadWholeAward);
            EquipWholeAwardModel.Instance.SetList(list);
            GameLog.Info("Equip", "15261 count={0} remaining={1}B", list.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_EQUIP_WHOLE_UPDATE);
        }

        private static (int type, int wholeLv) ReadWholeAward(NetReader r)
        {
            return (r.ReadU8(), r.ReadU16());   // {type:c, whole_lv:h}
        }
    }
}
