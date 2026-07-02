using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.OutWard
{
    /// <summary>
    /// 幻化外观协议控制器(对标老端 commonController/OutWardController.ts;服务端 pt_160)。
    /// 进游戏(EVT_GAME_START)对 type_id 1(坐骑)、2(剑魄同修)各发一次 16002(系统A阶星)与 16028(系统B等级面板)。
    /// 16023 一键升星(坐骑/同修专线,发 "ccc" type_id,auto_buy=0,gold_type=0):errcode==1 成功后老端另拉一次 16002
    /// 联动刷同修属性(照做);16029 升级(发 "c" type_id)同理成功后拉 16002。
    /// 老端锚点:OutWardController.ts:265-275(On16023)、:302-315(On16029)、:436-443(升星发包)、:475-478(升级发包)。
    /// </summary>
    public sealed class OutWardController : BaseController
    {
        public static readonly OutWardController Instance = new OutWardController();

        private OutWardController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.OUTWARD_INFO, On16002);
            RegisterProtocal(Proto.OUTWARD_STAR_UP, On16023);
            RegisterProtocal(Proto.OUTWARD_LV_PANEL, On16028);
            RegisterProtocal(Proto.OUTWARD_LV_UP, On16029);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            OutWardModel.Instance.Clear();
            base.Dispose();
        }

        /// <summary>对标老端登录拉取:type_id 1(坐骑)、2(剑魄同修)各发 16002+16028,共 4 包。</summary>
        private async void OnGameStart()
        {
            await OutWardConfigs.EnsureLoaded();
            foreach (int typeId in new[] { 1, 2 })
            {
                RequestInfo(typeId);
                RequestLvPanel(typeId);
            }
            GameLog.Info("OutWard", "GameStart request 16002+16028 for type_id 1,2(对标 OutWardController 登录拉取)");
        }

        /// <summary>16002 外观对象信息(系统A阶星)请求。</summary>
        public void RequestInfo(int typeId)
        {
            if (typeId <= 0) return;
            SendFmt(Proto.OUTWARD_INFO, "c", typeId);
        }

        /// <summary>16028 外观等级面板(系统B)请求。</summary>
        public void RequestLvPanel(int typeId)
        {
            if (typeId <= 0) return;
            SendFmt(Proto.OUTWARD_LV_PANEL, "c", typeId);
        }

        /// <summary>一键升星(坐骑/同修专线,对标 :436-443)。</summary>
        public void StarUp(int typeId)
        {
            if (typeId <= 0) return;
            SendFmt(Proto.OUTWARD_STAR_UP, "ccc", typeId, 0, 0);
            GameLog.Info("OutWard", "starUp 16023 type_id={0}", typeId);
        }

        /// <summary>升级(对标 :475-478;老端会连点一键连续升级,TEMP 壳单次即可)。</summary>
        public void LvUp(int typeId)
        {
            if (typeId <= 0) return;
            SendFmt(Proto.OUTWARD_LV_UP, "c", typeId);
            GameLog.Info("OutWard", "lvUp 16029 type_id={0}", typeId);
        }

        /// <summary>16002 回包:type_id:c, stage:c, star:h, blessing:i, figure_stage:c, combat:i, etime:l,
        /// auto_buy:c, attr_list[u16×{attr_id:c,attr_val:i}], skill_list[u16×{skill_id:i}]。</summary>
        private void On16002(NetReader r)
        {
            int typeId = r.ReadU8();
            int stage = r.ReadU8();
            int star = r.ReadU16();
            long blessing = r.ReadU32();
            int figureStage = r.ReadU8();
            long combat = r.ReadU32();
            long etime = r.ReadU64();
            int autoBuy = r.ReadU8();
            List<(int attrId, long val)> attrs = r.ReadArray(ReadAttr);
            List<int> skills = r.ReadArray(rr => (int)rr.ReadU32());
            OutWardModel.Instance.Apply16002(typeId, stage, star, blessing, figureStage, combat, etime, autoBuy, attrs, skills);
            GameLog.Info("OutWard", "16002 type_id={0} {1}阶{2}星 blessing={3} combat={4} remaining={5}B",
                typeId, stage, star, blessing, combat, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_UPDATE);
        }

        /// <summary>16023 升星结果:errcode:i, type_id:c, stage:c, star:h, blessing:i, blessing_plus:i, etime:l,
        /// auto_buy:c, ratio_list[u16×{rate:c,rate_num:h}]。errcode!=1 显码降级;成功后另拉 16002 联动刷新。</summary>
        private void On16023(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int stage = r.ReadU8();
            int star = r.ReadU16();
            long blessing = r.ReadU32();
            long blessingPlus = r.ReadU32();
            long etime = r.ReadU64();
            int autoBuy = r.ReadU8();
            List<(int rate, int rateNum)> ratios = r.ReadArray(ReadRatio);
            if (errcode != 1)
            {
                TipsManager.Toast("提升失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OutWard", "16023 starUp fail errcode={0} type_id={1}", errcode, typeId);
                return;
            }
            OutWardModel.Instance.Apply16023(typeId, stage, star, blessing, etime, autoBuy);
            GameLog.Info("OutWard", "16023 starUp ok type_id={0} → {1}阶{2}星 blessing={3}(+{4}) ratios={5} remaining={6}B",
                typeId, stage, star, blessing, blessingPlus, ratios.Count, r.Remaining);
            RequestInfo(typeId);   // 对标老端成功后 REQUEST_PROTO 16002 联动刷属性
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_UPDATE);
        }

        /// <summary>16028 面板回包:type_id:c, level:h, cur_exp:i, combat:i, attr_list[u16×{attr_id:c,attr_val:i}],
        /// skill_list[u16×{skill_id:i,skill_level:c}]。</summary>
        private void On16028(NetReader r)
        {
            int typeId = r.ReadU8();
            int level = r.ReadU16();
            long curExp = r.ReadU32();
            long combat = r.ReadU32();
            List<(int attrId, long val)> attrs = r.ReadArray(ReadAttr);
            List<(int skillId, int skillLevel)> skills = r.ReadArray(ReadSkillLv);
            OutWardModel.Instance.Apply16028(typeId, level, curExp, combat, attrs, skills);
            GameLog.Info("OutWard", "16028 type_id={0} level={1} curExp={2} combat={3} remaining={4}B",
                typeId, level, curExp, combat, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_UPDATE);
        }

        /// <summary>16029 升级结果:errcode:i, type_id:c, level:h, cur_exp:i, add_exp:i, combat:i,
        /// skill_list[u16×{skill_id:i,skill_level:c}], ratio_list[u16×{rate:c,rate_num:h}]。
        /// errcode!=1 显码降级;成功后另拉 16002 联动刷新(对标老端 On16029:302-315)。</summary>
        private void On16029(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int level = r.ReadU16();
            long curExp = r.ReadU32();
            long addExp = r.ReadU32();
            long combat = r.ReadU32();
            List<(int skillId, int skillLevel)> skills = r.ReadArray(ReadSkillLv);
            List<(int rate, int rateNum)> ratios = r.ReadArray(ReadRatio);
            if (errcode != 1)
            {
                TipsManager.Toast("提升失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OutWard", "16029 lvUp fail errcode={0} type_id={1}", errcode, typeId);
                return;
            }
            OutWardModel.Instance.Apply16029(typeId, level, curExp, combat, skills);
            GameLog.Info("OutWard", "16029 lvUp ok type_id={0} → level={1} curExp={2}(+{3}) combat={4} ratios={5} remaining={6}B",
                typeId, level, curExp, addExp, combat, ratios.Count, r.Remaining);
            RequestInfo(typeId);   // 对标老端成功后 REQUEST_PROTO 16002 联动刷新
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_UPDATE);
        }

        private static (int attrId, long val) ReadAttr(NetReader r)
        {
            return (r.ReadU8(), r.ReadU32());   // {attr_id:c, attr_val:i}
        }

        private static (int skillId, int skillLevel) ReadSkillLv(NetReader r)
        {
            return ((int)r.ReadU32(), r.ReadU8());   // {skill_id:i, skill_level:c}
        }

        private static (int rate, int rateNum) ReadRatio(NetReader r)
        {
            return (r.ReadU8(), r.ReadU16());   // {rate:c, rate_num:h}
        }
    }
}
