using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.OutWard
{
    /// <summary>
    /// 幻化外观协议控制器(对标老端 commonController/OutWardController.ts;服务端 pt_160)。
    /// 进游戏(EVT_GAME_START)对全部 6 个培养对象 type_id∈{1坐骑,2剑魄同修,3翼影,4古法符相,5殒锋天刃,12玄穹云披}
    /// 各发一次 16002(系统A阶星)与 16028(系统B等级面板)——服务端总闸 pp_mount.erl:26-45 的
    /// ?APPERENCE=data_mount:get_constant_cfg(20)=[1,2,3,4,5,12],type 6(精灵)/7(宠物)/8(法阵)一律 skip 不回包,严禁发。
    /// 16023 一键升星(坐骑/同修专线,发 "ccc" type_id,auto_buy=0,gold_type=0):errcode==1 成功后老端另拉一次 16002
    /// 联动刷同修属性(照做);16029 升级(发 "c" type_id)同理成功后拉 16002。
    /// 老端锚点:OutWardController.ts:265-275(On16023)、:302-315(On16029)、:317-330(On16030)、:389-402(GameStart 拉取)、
    /// :436-443(升星发包)、:475-478(升级发包)。
    /// 薄增量六件套(第20轮工单):16005 通用一键升星(type_id∉{1,2}:3翼影/4圣器/5神兵),回包=16023 少 etime/auto_buy 两字段。
    /// ⚠第21轮侦察订正:第20轮误判"3/4/5 只有系统A阶星线,无系统B等级线"——实际系统B(16028/16029/16030)对全部
    /// 6 个 type_id 都活(config_mount_level 每 type_id 各 750 条;lib_mount_upgrade_sys.erl:33-43 send_panel_info
    /// 不含 type_id guard),本轮已把 GameStart 拉取集与 16030 补齐。
    /// 老端枚举注释警示:Artifact=4=古法符相线,HolyDevice=5=殒锋天刃线(曾错位,以 mount.hrl ARTIFACT_ID=4/HOLYORGAN_ID=5 为准)。
    /// </summary>
    public sealed class OutWardController : BaseController
    {
        public static readonly OutWardController Instance = new OutWardController();

        /// <summary>全部 6 个培养对象 type_id(服务端 ?APPERENCE 总闸;对标老端 enum_OutWardType 减去 Sprite/Pet/MagicArr)。</summary>
        public static readonly int[] AllTypeIds = { 1, 2, 3, 4, 5, 12 };

        private OutWardController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.OUTWARD_INFO, On16002);
            RegisterProtocal(Proto.OUTWARD_STAR_UP, On16023);
            RegisterProtocal(Proto.OUTWARD_LV_PANEL, On16028);
            RegisterProtocal(Proto.OUTWARD_LV_UP, On16029);
            RegisterProtocal(Proto.OUTWARD_STAR_UP_GENERIC, On16005);
            RegisterProtocal(Proto.OUTWARD_LV_SKILL_UP, On16030);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            OutWardModel.Instance.Clear();
            base.Dispose();
        }

        /// <summary>对标老端登录拉取(OutWardController.ts:389-402):全部 6 个 type_id∈<see cref="AllTypeIds"/>
        /// 各发 16002(系统A阶星)+16028(系统B等级面板),共 12 包。第21轮订正:系统B对全部 6 个 type_id 都活,
        /// 不再像第20轮那样把 3/4/5/12 排除在 16028 拉取外。</summary>
        private async void OnGameStart()
        {
            await OutWardConfigs.EnsureLoaded();
            foreach (int typeId in AllTypeIds)
            {
                RequestInfo(typeId);
                RequestLvPanel(typeId);
            }
            GameLog.Info("OutWard", "GameStart request 16002+16028 for type_id {0}(全 6 类型,对标 OutWardController 登录拉取)",
                string.Join(",", AllTypeIds));
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

        /// <summary>16005 通用一键升星(type_id∉{1,2}:3翼影/4圣器/5神兵;发 "c" type_id,无 autoBuy/goldType,
        /// 对标服务端 guard type_id∈{1,2}→16023,其它→16005)。薄增量六件套第20轮。</summary>
        public void StarUpGeneric(int typeId)
        {
            if (typeId <= 0) return;
            SendFmt(Proto.OUTWARD_STAR_UP_GENERIC, "c", typeId);
            GameLog.Info("OutWard", "starUpGeneric 16005 type_id={0}", typeId);
        }

        /// <summary>16030 系统B技能升级(发 "ci" type_id,skill_id;对全部 6 个 type_id 都活,第21轮补齐。
        /// 对标老端 OutWardController.ts:317-330,发包点见其 SkillUp 惯例——本轮只补数据层,壳/真页未接技能升级按钮,
        /// 留待技能 UI 落地时调用)。</summary>
        public void LvSkillUp(int typeId, int skillId)
        {
            if (typeId <= 0 || skillId <= 0) return;
            SendFmt(Proto.OUTWARD_LV_SKILL_UP, "ci", typeId, skillId);
            GameLog.Info("OutWard", "lvSkillUp 16030 type_id={0} skill_id={1}", typeId, skillId);
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

        /// <summary>16005 通用升星结果:errcode:i, type_id:c, stage:c, star:h, blessing:i, blessing_plus:i,
        /// ratio_list[u16×{rate:c,rate_num:h}]。=16023 少 etime/auto_buy 两字段。errcode!=1 显码降级;
        /// 成功后另拉 16002 联动刷新(对标 On16023 同构)。薄增量六件套第20轮。</summary>
        private void On16005(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int stage = r.ReadU8();
            int star = r.ReadU16();
            long blessing = r.ReadU32();
            long blessingPlus = r.ReadU32();
            List<(int rate, int rateNum)> ratios = r.ReadArray(ReadRatio);
            if (errcode != 1)
            {
                TipsManager.Toast("提升失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OutWard", "16005 starUpGeneric fail errcode={0} type_id={1}", errcode, typeId);
                return;
            }
            OutWardModel.Instance.Apply16005(typeId, stage, star, blessing);
            GameLog.Info("OutWard", "16005 starUpGeneric ok type_id={0} → {1}阶{2}星 blessing={3}(+{4}) ratios={5} remaining={6}B",
                typeId, stage, star, blessing, blessingPlus, ratios.Count, r.Remaining);
            RequestInfo(typeId);   // 对标老端成功后 REQUEST_PROTO 16002 联动刷属性
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_UPDATE);
        }

        /// <summary>16030 系统B技能升级结果:errcode:i, type_id:c, skill_id:i, level:c。第21轮补齐(对全部6类型都活)。
        /// errcode!=1 显码降级;成功后套值 LvSkills 对应技能等级 + 另拉 16002 联动刷新(对标老端 On16030:317-330)。</summary>
        private void On16030(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int skillId = (int)r.ReadU32();
            int level = r.ReadU8();
            if (errcode != 1)
            {
                TipsManager.Toast("提升失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OutWard", "16030 lvSkillUp fail errcode={0} type_id={1} skill_id={2}", errcode, typeId, skillId);
                return;
            }
            OutWardModel.Instance.Apply16030(typeId, skillId, level);
            GameLog.Info("OutWard", "16030 lvSkillUp ok type_id={0} skill_id={1} → level={2} remaining={3}B",
                typeId, skillId, level, r.Remaining);
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
