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

        /// <summary>幻化形象列表(16006)首次到达后是否已为其批量补拉过 16007 详情(仅 Horse/Partner;
        /// 对标老端 check_illusion_red_[type_id] 的"只做一次"门槛,防止每次 16006 刷新都风暴请求详情)。</summary>
        private readonly HashSet<int> _illusionDetailPrefetched = new HashSet<int>();

#if UNITY_EDITOR
        /// <summary>
        /// CliVerify 的模块内出站观察缝:返回 true 时只记录、不真正下发。正式 Player 构建不包含此字段；
        /// 用于实证 OnGameStart/页面 SetType 都严格发 16002/16006/16011/16028，避免用“未连接不抛异常”冒充发送断言。
        /// </summary>
        private static System.Func<int, int, bool> s_initialRequestIntercept;
#endif

        private OutWardController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.OUTWARD_INFO, On16002);
            RegisterProtocal(Proto.OUTWARD_STAR_UP, On16023);
            RegisterProtocal(Proto.OUTWARD_LV_PANEL, On16028);
            RegisterProtocal(Proto.OUTWARD_LV_UP, On16029);
            RegisterProtocal(Proto.OUTWARD_STAR_UP_GENERIC, On16005);
            RegisterProtocal(Proto.OUTWARD_LV_SKILL_UP, On16030);
            // ---- 轮24 PI:幻化(Illusion)全链补齐 ----
            RegisterProtocal(Proto.OUTWARD_ERROR, On16000);
            RegisterProtocal(Proto.OUTWARD_SCENE_FIGURE_CHANGE, On16001);
            RegisterProtocal(Proto.OUTWARD_ILLUSION_WEAR, On16003);
            RegisterProtocal(Proto.OUTWARD_RIDE_TOGGLE, On16004);
            RegisterProtocal(Proto.OUTWARD_ILLUSION_LIST, On16006);
            RegisterProtocal(Proto.OUTWARD_FIGURE_DETAIL, On16007);
            RegisterProtocal(Proto.OUTWARD_FIGURE_ACTIVATE, On16008);
            RegisterProtocal(Proto.OUTWARD_FIGURE_STAGE_UP, On16009);
            RegisterProtocal(Proto.OUTWARD_CRYSTAL_USE, On16010);
            RegisterProtocal(Proto.OUTWARD_CRYSTAL_COUNTER, On16011);
            RegisterProtocal(Proto.OUTWARD_FIGURE_EXPIRED, On16012);
            RegisterProtocal(Proto.OUTWARD_FIGURE_STAR_UP, On16020);
            RegisterProtocal(Proto.OUTWARD_FIGHT_PREVIEW, On16022);
            RegisterProtocal(Proto.OUTWARD_AUTO_BUY, On16024);
            RegisterProtocal(Proto.OUTWARD_STAR_FIGHT_PREVIEW, On16027);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            OutWardModel.Instance.Clear();
            _illusionDetailPrefetched.Clear();
            base.Dispose();
        }

        /// <summary>对标老端登录拉取(OutWardController.ts:387-405):全部 6 个 type_id∈<see cref="AllTypeIds"/>
        /// 各发 16002(系统A阶星)+16006(幻化列表)+16011(魔晶次数)+16028(系统B等级面板),共 24 包。</summary>
        private async void OnGameStart()
        {
            await OutWardConfigs.EnsureLoaded();
            foreach (int typeId in AllTypeIds)
            {
                RequestPanelData(typeId);
            }
            GameLog.Info("OutWard", "GameStart request 16002+16006+16011+16028 for type_id {0}(全 6 类型,对标 OutWardController 登录拉取)",
                string.Join(",", AllTypeIds));
        }

        /// <summary>打开培养页/登录初始化的四包请求。顺序严格对标老端 OPEN_MOUNTPET_VIEW:
        /// 16002 → 16006 → 16011 → 16028，四包均为 "c" type_id。</summary>
        public void RequestPanelData(int typeId)
        {
            if (typeId <= 0) return;
            SendInitialTypeRequest(Proto.OUTWARD_INFO, typeId);
            SendInitialTypeRequest(Proto.OUTWARD_ILLUSION_LIST, typeId);
            SendInitialTypeRequest(Proto.OUTWARD_CRYSTAL_COUNTER, typeId);
            SendInitialTypeRequest(Proto.OUTWARD_LV_PANEL, typeId);
        }

        private void SendInitialTypeRequest(int protoId, int typeId)
        {
#if UNITY_EDITOR
            if (s_initialRequestIntercept != null && s_initialRequestIntercept(protoId, typeId)) return;
#endif
            SendFmt(protoId, "c", typeId);
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

        /// <summary>生产技能行调用的资格闸；无具名控件时仍可静态验证命令接口，禁止越过等级条件直接发 16030。</summary>
        public bool TryLvSkillUp(int typeId, int skillId, out string reason)
        {
            if (!OutWardModel.Instance.CanUpgradeLevelSkill(typeId, skillId, out _, out reason)) return false;
            LvSkillUp(typeId, skillId);
            return true;
        }

        // =================================================================================
        // 幻化(Illusion,轮24 PI 增量)发送侧:16006 列表/16007 详情/16003 穿戴/16004 骑乘/16008 激活/
        // 16009 升阶/16010 用魔晶/16011 魔晶次数/16020 升星/16022+16027 战力预览/16024 自动购买。
        // 对标老端 OutWardController.ts:418-483 各 Bind 转发函数。
        // =================================================================================

        /// <summary>16006 幻化形象列表请求。</summary>
        public void RequestIllusionList(int typeId)
        {
            if (typeId <= 0) return;
            SendFmt(Proto.OUTWARD_ILLUSION_LIST, "c", typeId);
        }

        /// <summary>16007 幻化形象详情请求(该 id 未激活时服务端直接 skip,不回包也不报错——不是超时/丢包)。</summary>
        public void RequestFigureDetail(int typeId, int figureId)
        {
            if (typeId <= 0 || figureId <= 0) return;
            SendFmt(Proto.OUTWARD_FIGURE_DETAIL, "ci", typeId, figureId);
        }

        /// <summary>16003 幻化穿戴/取消(type 1=基础/2=幻化;args=对应 figure_stage 或 figure_id;
        /// color=染色 id,不染色传 0)。对标 OutWardController.ts:431-434 ILLUSION_OUTWARD。</summary>
        public void WearIllusion(int typeId, int type, int args, int color)
        {
            if (typeId <= 0) return;
            SendFmt(Proto.OUTWARD_ILLUSION_WEAR, "ccii", typeId, type, args, color);
        }

        /// <summary>16004 上/下坐骑(type 0=下/1=上)。对标 OutWardController.ts:726-731 CHANGE_HORSE_STATE。</summary>
        public void ToggleRide(int typeId, int type)
        {
            if (typeId <= 0) return;
            SendFmt(Proto.OUTWARD_RIDE_TOGGLE, "cc", typeId, type);
        }

        /// <summary>16008 激活形象。对标 OutWardController.ts:455-458 ACTIVE_OUTWARD_FIGURE。</summary>
        public void ActivateFigure(int typeId, int figureId)
        {
            if (typeId <= 0 || figureId <= 0) return;
            SendFmt(Proto.OUTWARD_FIGURE_ACTIVATE, "ci", typeId, figureId);
        }

        /// <summary>16009 幻化升阶(goodsId 指定经验道具,老端默认传 0 走通用消耗)。对标
        /// OutWardController.ts:460-463 EVOLUTION_OUTWARD_FIGURE。</summary>
        public void UpgradeFigureStage(int typeId, int figureId, int goodsId)
        {
            if (typeId <= 0 || figureId <= 0) return;
            SendFmt(Proto.OUTWARD_FIGURE_STAGE_UP, "cii", typeId, figureId, goodsId);
        }

        /// <summary>16010 使用魔晶。对标 OutWardController.ts:465-468 USE_OUTWARD_ITEM。</summary>
        public void UseCrystal(int typeId, int goodsId)
        {
            if (typeId <= 0 || goodsId <= 0) return;
            SendFmt(Proto.OUTWARD_CRYSTAL_USE, "ci", typeId, goodsId);
        }

        /// <summary>16011 魔晶使用次数请求。</summary>
        public void RequestCrystalCounter(int typeId)
        {
            if (typeId <= 0) return;
            SendFmt(Proto.OUTWARD_CRYSTAL_COUNTER, "c", typeId);
        }

        /// <summary>16020 幻化升星。对标 OutwardStarView.ts:106-110 upBtn。</summary>
        public void UpgradeFigureStar(int typeId, int figureId)
        {
            if (typeId <= 0 || figureId <= 0) return;
            SendFmt(Proto.OUTWARD_FIGURE_STAR_UP, "ci", typeId, figureId);
        }

        /// <summary>16022 幻化战力预览——"选中未缓存才请求":该 figure 已有 16007 详情缓存时跳过
        /// (对标 IllusionBaseView.SetFightValue:1273-1284,老端用 outward_figure_list[type][id] 是否
        /// 存在做同一判断)。</summary>
        public void RequestFightPreview(int typeId, int figureId)
        {
            if (typeId <= 0 || figureId <= 0) return;
            if (OutWardModel.Instance.GetFigureDetail(typeId, figureId) != null)
            {
                GameLog.Info("OutWard", "16022 skip(figure已有16007详情缓存) type_id={0} id={1}", typeId, figureId);
                return;
            }
            SendFmt(Proto.OUTWARD_FIGHT_PREVIEW, "cc", typeId, figureId);
        }

        /// <summary>16024 自动购买开关(仅 Horse/Partner 服务端 guard 放行,其余 type 发了也无回包)。
        /// 对标 OutWardBaseView.ts:638/1128。</summary>
        public void SetAutoBuy(int typeId, int autoBuy)
        {
            if (typeId <= 0) return;
            SendFmt(Proto.OUTWARD_AUTO_BUY, "cc", typeId, autoBuy);
        }

        /// <summary>16027 幻化升星战力预览——"选中未缓存才请求":该 figure 的 16007 详情缓存存在且
        /// StarCombat!=0 时跳过(对标 OutwardStarView.SelectItem:311-329;老端真实 View 还叠加了一层
        /// "本次打开窗口是否已刷新过"的 View 本地一次性门槛 req_list,那层是纯 UI 状态,留给未来 UI 自行
        /// 叠加,此处只做数据层可判定的缓存语义)。</summary>
        public void RequestStarFightPreview(int typeId, int figureId)
        {
            if (typeId <= 0 || figureId <= 0) return;
            OutWardModel.FigureDetailVo detail = OutWardModel.Instance.GetFigureDetail(typeId, figureId);
            if (detail != null && detail.StarCombat != 0)
            {
                GameLog.Info("OutWard", "16027 skip(figure已有16007详情且star_combat!=0) type_id={0} id={1}", typeId, figureId);
                return;
            }
            SendFmt(Proto.OUTWARD_STAR_FIGHT_PREVIEW, "cc", typeId, figureId);
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
                EmitTransactionResult(Proto.OUTWARD_STAR_UP, typeId, 0, errcode);
                TipsManager.Toast("提升失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OutWard", "16023 starUp fail errcode={0} type_id={1}", errcode, typeId);
                return;
            }
            OutWardModel.Instance.Apply16023(typeId, stage, star, blessing, etime, autoBuy);
            EmitTransactionResult(Proto.OUTWARD_STAR_UP, typeId, 0, errcode);
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
                EmitTransactionResult(Proto.OUTWARD_LV_UP, typeId, 0, errcode);
                TipsManager.Toast("提升失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OutWard", "16029 lvUp fail errcode={0} type_id={1}", errcode, typeId);
                return;
            }
            OutWardModel.Instance.Apply16029(typeId, level, curExp, combat, skills);
            EmitTransactionResult(Proto.OUTWARD_LV_UP, typeId, 0, errcode);
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
                EmitTransactionResult(Proto.OUTWARD_STAR_UP_GENERIC, typeId, 0, errcode);
                TipsManager.Toast("提升失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OutWard", "16005 starUpGeneric fail errcode={0} type_id={1}", errcode, typeId);
                return;
            }
            OutWardModel.Instance.Apply16005(typeId, stage, star, blessing);
            EmitTransactionResult(Proto.OUTWARD_STAR_UP_GENERIC, typeId, 0, errcode);
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
                EmitTransactionResult(Proto.OUTWARD_LV_SKILL_UP, typeId, skillId, errcode);
                TipsManager.Toast("提升失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OutWard", "16030 lvSkillUp fail errcode={0} type_id={1} skill_id={2}", errcode, typeId, skillId);
                return;
            }
            OutWardModel.Instance.Apply16030(typeId, skillId, level);
            EmitTransactionResult(Proto.OUTWARD_LV_SKILL_UP, typeId, skillId, errcode);
            GameLog.Info("OutWard", "16030 lvSkillUp ok type_id={0} skill_id={1} → level={2} remaining={3}B",
                typeId, skillId, level, r.Remaining);
            RequestInfo(typeId);   // 对标老端成功后 REQUEST_PROTO 16002 联动刷新
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_UPDATE);
        }

        // =================================================================================
        // 幻化(Illusion,轮24 PI 增量)接收侧。逐号 wire 复核自 pt_160.erl write/2 + 老端
        // OutWardController.ts:71-355,四层判定见 Proto.cs 各常量注释。
        // =================================================================================

        /// <summary>16000 族错误出口:errcode:i。errcode==1600023 时老端特判"激活数量已达上限"
        /// (Fire PET_ACTIVE_LIMIT),其余显码降级。对标 On16000:71-78。</summary>
        private void On16000(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            if (errcode == 1600023)
            {
                GameLog.Info("OutWard", "16000 激活数量已达上限 errcode={0}", errcode);
                EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_ACTIVE_LIMIT, errcode);
                return;
            }
            TipsManager.Toast("错误(" + errcode + ")");   // 错误码表未移植,显码降级
            GameLog.Info("OutWard", "16000 族错误出口 errcode={0}", errcode);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_ERROR, errcode);
        }

        /// <summary>16001 场景外观变化广播(S2C only):type_id:c, role_id:l, is_ride:c, figure_id:i, speed:h。
        /// Unity 场景暂无角色外观渲染消费方,本轮只落数据 + Emit 事件留 TODO。对标 On16001:80-91。</summary>
        private void On16001(NetReader r)
        {
            int typeId = r.ReadU8();
            long roleId = r.ReadU64();
            int isRide = r.ReadU8();
            long figureId = r.ReadU32();
            int speed = r.ReadU16();
            GameLog.Info("OutWard", "16001 场景外观变化 role_id={0} type_id={1} figure_id={2} is_ride={3} speed={4}(TODO 消费方:场景角色渲染)",
                roleId, typeId, figureId, isRide, speed);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_SCENE_FIGURE_CHANGE, typeId, roleId);
        }

        /// <summary>16003 幻化穿戴/取消结果:errcode:i, type_id:c, type:c, args:i, color:i。errcode!=1 显码降级;
        /// 成功后按 type 套值系统A的 FigureStage / 幻化穿戴 IllusionId(对标老端 On16003:98-106 +
        /// OutWardBaseModel.UpdateOutWardFigure:297-319)。</summary>
        private void On16003(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int type = r.ReadU8();
            long args = r.ReadU32();
            long color = r.ReadU32();
            if (errcode != 1)
            {
                EmitTransactionResult(Proto.OUTWARD_ILLUSION_WEAR, typeId, (int)args, errcode);
                TipsManager.Toast("幻化失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OutWard", "16003 illusion wear fail errcode={0} type_id={1}", errcode, typeId);
                return;
            }
            OutWardModel.Instance.ApplyIllusionWear(typeId, type, args);
            EmitTransactionResult(Proto.OUTWARD_ILLUSION_WEAR, typeId, (int)args, errcode);
            GameLog.Info("OutWard", "16003 illusion wear ok type_id={0} type={1} args={2} color={3}", typeId, type, args, color);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_ILLUSION_WEAR, typeId);
        }

        /// <summary>16004 上/下坐骑结果:errcode:i, type_id:c, type:c。errcode!=1 显码降级;老端仅
        /// errcode==1 且 type_id==Horse 触发骑乘动画,Unity 无场景坐骑动画消费方,只 Emit 事件。
        /// 对标 On16004:108-117。</summary>
        private void On16004(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int type = r.ReadU8();
            if (errcode != 1)
            {
                TipsManager.Toast("操作失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OutWard", "16004 ride toggle fail errcode={0} type_id={1}", errcode, typeId);
                return;
            }
            GameLog.Info("OutWard", "16004 ride toggle ok type_id={0} type={1}", typeId, type);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_RIDE_TOGGLE, typeId, type);
        }

        /// <summary>16006 幻化形象列表:errcode:i(服务端恒发 ?SUCCESS,老端不判,照抄不判), type_id:c,
        /// illusion_id:i(当前穿戴 figure id,0=未穿戴/仅基础形象), color_id:h,
        /// figure_list[u16×{id:i,stage:c,star:h,end_time:i}]。对标 On16006:128-140。
        /// type_id∈{Horse,Partner} 首次收到列表时自动为每个已激活 figure 补拉一次 16007 详情
        /// (对标 OutWardBaseModel.UpdataOutWardIllusionData:358-393,老端还含 Pet(7),协议层不可达已排除)。</summary>
        private void On16006(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int illusionId = (int)r.ReadU32();
            int colorId = r.ReadU16();
            List<OutWardModel.FigureBriefVo> figureList = r.ReadArray(ReadFigureBrief);
            OutWardModel.Instance.Apply16006(typeId, illusionId, colorId, figureList);
            GameLog.Info("OutWard", "16006 illusion list type_id={0} illusion_id={1} color_id={2} count={3} errcode={4} remaining={5}B",
                typeId, illusionId, colorId, figureList.Count, errcode, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_ILLUSION_LIST_UPDATE, typeId);
            if ((typeId == 1 || typeId == 2) && _illusionDetailPrefetched.Add(typeId))
            {
                foreach (OutWardModel.FigureBriefVo f in figureList)
                {
                    RequestFigureDetail(typeId, f.Id);
                }
            }
        }

        /// <summary>16007 幻化形象详情:errcode:i, type_id:c, id:i, stage:c, star:h, blessing:i, combat:i,
        /// star_combat:i, end_time:i, attr_list[u16×{attr_id:c,attr_val:i}], skill_list[u16×skill_id:i]
        /// (⚠仅 id 无 level), color_list[u16×{color_id:h,color_lv:i}], next_star_power:l。errcode!=1 显码降级
        /// (该 id 未激活时服务端直接 skip 不回包,不会走到这个失败分支——纯防御性判断)。对标 On16007:142-149。</summary>
        private void On16007(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int figureId = (int)r.ReadU32();
            int stage = r.ReadU8();
            int star = r.ReadU16();
            long blessing = r.ReadU32();
            long combat = r.ReadU32();
            long starCombat = r.ReadU32();
            long endTime = r.ReadU32();
            List<(int attrId, long val)> attrs = r.ReadArray(ReadAttr);
            List<int> skills = r.ReadArray(rr => (int)rr.ReadU32());
            List<(int colorId, long colorLv)> colors = r.ReadArray(ReadColorPair);
            long nextStarPower = r.ReadU64();
            if (errcode != 1)
            {
                TipsManager.Toast("查询失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OutWard", "16007 detail fail errcode={0} type_id={1} id={2}", errcode, typeId, figureId);
                return;
            }
            var detail = new OutWardModel.FigureDetailVo
            {
                TypeId = typeId, Id = figureId, Stage = stage, Star = star, Blessing = blessing,
                Combat = combat, StarCombat = starCombat, EndTime = endTime,
                Attrs = attrs, Skills = skills, ColorList = colors, NextStarPower = nextStarPower
            };
            OutWardModel.Instance.Apply16007(detail);
            GameLog.Info("OutWard", "16007 detail ok type_id={0} id={1} stage={2} star={3} combat={4} star_combat={5} remaining={6}B",
                typeId, figureId, stage, star, combat, starCombat, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_FIGURE_DETAIL_UPDATE, typeId, figureId);
        }

        /// <summary>16008 激活形象结果:errcode:i, type_id:c, id:i, combat:i。服务端实测 4 条失败分支全部
        /// 改走 16000,16008 本身只在成功时出现,老端仍防御式判 errcode,照抄。成功后无条件补拉 16006
        /// (对标 On16008:151-176)。</summary>
        private void On16008(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int figureId = (int)r.ReadU32();
            long combat = r.ReadU32();
            if (errcode != 1)
            {
                EmitTransactionResult(Proto.OUTWARD_FIGURE_ACTIVATE, typeId, figureId, errcode);
                TipsManager.Toast("激活失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OutWard", "16008 activate fail errcode={0} type_id={1} id={2}", errcode, typeId, figureId);
                return;
            }
            EmitTransactionResult(Proto.OUTWARD_FIGURE_ACTIVATE, typeId, figureId, errcode);
            GameLog.Info("OutWard", "16008 activate ok type_id={0} id={1} combat={2}", typeId, figureId, combat);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_FIGURE_ACTIVATED, typeId, figureId);
            RequestIllusionList(typeId);   // 对标老端成功后无条件 REQUEST_PROTO 16006(:163)
        }

        /// <summary>16009 幻化升阶结果:errcode:i, type_id:c, id:i, stage:c, blessing:i, blessing_plus:i,
        /// ratio_list[u16×{rate:c,rate_num:h}], goods_id:i。服务端失败同样改走 16000,老端仍防御式判
        /// errcode。成功后无条件补拉 16006(对标 On16009:178-194,不区分 type_id 一律补拉)。</summary>
        private void On16009(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int figureId = (int)r.ReadU32();
            int stage = r.ReadU8();
            long blessing = r.ReadU32();
            long blessingPlus = r.ReadU32();
            List<(int rate, int rateNum)> ratios = r.ReadArray(ReadRatio);
            long goodsId = r.ReadU32();
            if (errcode != 1)
            {
                EmitTransactionResult(Proto.OUTWARD_FIGURE_STAGE_UP, typeId, figureId, errcode);
                TipsManager.Toast("升阶失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OutWard", "16009 stage up fail errcode={0} type_id={1} id={2}", errcode, typeId, figureId);
                return;
            }
            EmitTransactionResult(Proto.OUTWARD_FIGURE_STAGE_UP, typeId, figureId, errcode);
            GameLog.Info("OutWard", "16009 stage up ok type_id={0} id={1} stage={2} blessing={3}(+{4}) goods_id={5} ratios={6} remaining={7}B",
                typeId, figureId, stage, blessing, blessingPlus, goodsId, ratios.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_FIGURE_STAGE_UP, typeId, figureId);
            RequestIllusionList(typeId);   // 对标老端成功后无条件 SendFmtToGameImme 16006(:189)
        }

        /// <summary>16010 使用魔晶结果:errcode:i, type_id:c, goods_id:i。errcode!=1 显码降级;成功后补拉
        /// 16011+16002(对标 On16010:196-206)。</summary>
        private void On16010(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int typeId = r.ReadU8();
            long goodsId = r.ReadU32();
            if (errcode != 1)
            {
                EmitTransactionResult(Proto.OUTWARD_CRYSTAL_USE, typeId, (int)goodsId, errcode);
                TipsManager.Toast("使用失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OutWard", "16010 use crystal fail errcode={0} type_id={1} goods_id={2}", errcode, typeId, goodsId);
                return;
            }
            EmitTransactionResult(Proto.OUTWARD_CRYSTAL_USE, typeId, (int)goodsId, errcode);
            GameLog.Info("OutWard", "16010 use crystal ok type_id={0} goods_id={1}", typeId, goodsId);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_CRYSTAL_UPDATE, typeId);
            RequestCrystalCounter(typeId);   // 对标老端成功后 SendFmtToGameImme 16011(:200)
            RequestInfo(typeId);             // 对标老端成功后 SendFmtToGameImme 16002(:201)
        }

        /// <summary>16011 魔晶使用次数:type_id:c, counter_list[u16×{goods_id:i,times:i,times_lim:i}]。
        /// 无 errcode 字段,老端也不判。对标 On16011:208-211。</summary>
        private void On16011(NetReader r)
        {
            int typeId = r.ReadU8();
            List<(int goodsId, int times, int timesLim)> counters = r.ReadArray(ReadCounter);
            OutWardModel.Instance.Apply16011(typeId, counters);
            GameLog.Info("OutWard", "16011 crystal counter type_id={0} count={1} remaining={2}B", typeId, counters.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_CRYSTAL_UPDATE, typeId);
        }

        /// <summary>16012 幻化到期删除推送(S2C only):type_id:c, id:c(⚠均 8 位,与 16007/16008 的 32 位 id 不同)。
        /// 从形象列表+详情缓存里一并摘除,再补拉一次 16006 兜底(对标 On16012:213-223)。</summary>
        private void On16012(NetReader r)
        {
            int typeId = r.ReadU8();
            int figureId = r.ReadU8();
            OutWardModel.Instance.Apply16012(typeId, figureId);
            GameLog.Info("OutWard", "16012 figure expired type_id={0} id={1}", typeId, figureId);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_FIGURE_EXPIRED, typeId, figureId);
            RequestIllusionList(typeId);   // 对标老端 On16012 补拉 16006(:221)
        }

        /// <summary>16020 幻化升星结果:errcode:i, type_id:c, id:i, star:h。服务端失败同样改走 16000,老端
        /// 仍防御式判 errcode。成功后原地 patch 缓存 figure_list 的 Star + 补拉 16006 + 补拉 16007
        /// (对标 On16020:230-250)。</summary>
        private void On16020(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int figureId = (int)r.ReadU32();
            int star = r.ReadU16();
            if (errcode != 1)
            {
                EmitTransactionResult(Proto.OUTWARD_FIGURE_STAR_UP, typeId, figureId, errcode);
                TipsManager.Toast("升星失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OutWard", "16020 star up fail errcode={0} type_id={1} id={2}", errcode, typeId, figureId);
                return;
            }
            OutWardModel.Instance.PatchIllusionStar(typeId, figureId, star);
            EmitTransactionResult(Proto.OUTWARD_FIGURE_STAR_UP, typeId, figureId, errcode);
            GameLog.Info("OutWard", "16020 star up ok type_id={0} id={1} star={2}", typeId, figureId, star);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_FIGURE_STAR_UP, typeId, figureId);
            RequestIllusionList(typeId);        // 对标老端 On16020 补拉 16006(:244)
            RequestFigureDetail(typeId, figureId);   // 对标老端 On16020 补拉 16007(:245)
        }

        /// <summary>16022 幻化战力预览(无 errcode 包装):type_id:c, id:c, power:l, star_combat:l,
        /// next_star_power:l。瞬时值,不做二级索引缓存。对标 On16022:260-263。</summary>
        private void On16022(NetReader r)
        {
            int typeId = r.ReadU8();
            int figureId = r.ReadU8();
            long power = r.ReadU64();
            long starCombat = r.ReadU64();
            long nextStarPower = r.ReadU64();
            OutWardModel.Instance.ApplyFightPreview(typeId, figureId, power, starCombat, nextStarPower);
            GameLog.Info("OutWard", "16022 fight preview type_id={0} id={1} power={2} star_combat={3} next_star_power={4} remaining={5}B",
                typeId, figureId, power, starCombat, nextStarPower, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_FIGHT_PREVIEW, typeId, figureId);
        }

        /// <summary>16024 自动购买开关结果:errcode:i, type_id:c, auto_buy:c。老端不判 errcode 直接套值
        /// (服务端 change_auto_buy 恒发 ?SUCCESS)——照抄不加判断。对标 On16024:277-284。</summary>
        private void On16024(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int typeId = r.ReadU8();
            int autoBuy = r.ReadU8();
            OutWardModel.Instance.Apply16024(typeId, autoBuy);
            GameLog.Info("OutWard", "16024 auto_buy type_id={0} auto_buy={1} errcode={2}(老端不判errcode,照抄)", typeId, autoBuy, errcode);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_UPDATE);   // 无参,与本文件其余 5 处 Emit 同一约定(Rebuild/OnOutWardUpdate 均订阅无参 Action)
        }

        /// <summary>16027 幻化升星战力预览(无 errcode 包装):type_id:c, id:c, power:l, next_star_power:l。
        /// 瞬时值,不做二级索引缓存。对标 On16027:286-289。</summary>
        private void On16027(NetReader r)
        {
            int typeId = r.ReadU8();
            int figureId = r.ReadU8();
            long power = r.ReadU64();
            long nextStarPower = r.ReadU64();
            OutWardModel.Instance.ApplyStarFightPreview(typeId, figureId, power, nextStarPower);
            GameLog.Info("OutWard", "16027 star fight preview type_id={0} id={1} power={2} next_star_power={3} remaining={4}B",
                typeId, figureId, power, nextStarPower, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_STAR_FIGHT_PREVIEW, typeId, figureId);
        }

        private static void EmitTransactionResult(int command, int typeId, int entityId, int code)
        {
            EventDispatcher.Emit(GlobalEvent.EVT_OUTWARD_TRANSACTION_RESULT,
                new OutWardTransactionResult(command, typeId, entityId, code));
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

        private static (int colorId, long colorLv) ReadColorPair(NetReader r)
        {
            return (r.ReadU16(), r.ReadU32());   // {color_id:h, color_lv:i}
        }

        private static (int goodsId, int times, int timesLim) ReadCounter(NetReader r)
        {
            return ((int)r.ReadU32(), (int)r.ReadU32(), (int)r.ReadU32());   // {goods_id:i, times:i, times_lim:i}
        }

        private static OutWardModel.FigureBriefVo ReadFigureBrief(NetReader r)
        {
            return new OutWardModel.FigureBriefVo { Id = (int)r.ReadU32(), Stage = r.ReadU8(), Star = r.ReadU16(), EndTime = r.ReadU32() };
        }
    }
}
