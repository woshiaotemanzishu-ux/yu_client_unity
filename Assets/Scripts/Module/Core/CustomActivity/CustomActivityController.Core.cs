using System.Collections.Generic;
using System.Text.RegularExpressions;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.CustomActivity
{
    /// <summary>
    /// 定制活动框架核心(自动循环 轮17 P1):pt_331 33100-33108(33101 在 CustomActivityController.cs 主文件,
    /// On33101 已升级追加落 Model+Emit+33259 追发)。本文件新增 On33100/02/03/04/05/06/08 + RequestActDetail
    /// (完整镜像老端 RequireActInfo 分发表,CustomActivityModel.ts:965-1122)+ RequestClaim/RequestAllCount。
    ///
    /// wire 全部逐字段回 pt_331.erl 原文 + item_to_bin_N 核对(非仅 write 子句变量名,见各常量 Proto.cs 注释)。
    /// 33104 reward_list 字段序已订正为 8 字段(Grade/FormType/Status/ReceiveTimes/Name/Desc/Condition/Reward),
    /// 与早期侦察表"Type:8,Value:32"不同——那是死号 33107(消费统计,老端未注册)的结构,双源(item_to_bin_3 +
    /// ClientProtocol.json "33104")互证后订正,详见 CUSTOM_ACT_DETAIL 常量注释。
    ///
    /// RequestActDetail 死分支不镜像(§1/spec §2-4):FIGHT_RANK(104,老端仍会误发 22601/22602,Unity 不镜像
    /// 这个死系统误发)整支跳过,不发送任何协议(包括其中夹带的 33104)。TOP_PLAYER(10)**三镜头订正为真镜像**:
    /// 本文件 RequestActDetail 直接发 22502("h",subType)并转调 TopPlayerController.Instance.
    /// RequestOpenRanksAsync()(已改 public)按 config_rush_rank 时间窗遍历发 22501,对标老端 RequireActInfo
    /// TOP_PLAYER 分支(CustomActivityModel.ts:978-988)。原"TopPlayerController 的 OnRoleInfoUpdate→
    /// RequestOpenRanksAsync 已功能覆盖"断言已被验收证伪(那条路径带角色等级/开服天数门禁,与本分支
    /// "见到活动列表条目即应答"的门禁自由语义不同),两条触发路径并存,互不替代。
    /// FEASTBOSS(51)老端本就是纯本地计算(BossModel.FeastBossActivity 不发协议),Unity 已有 EvaluateFeastBoss()
    /// 从 33101 缓存列表扫描驱动,本方法内该分支同样不发送。MORE(12)/DOUBLE(16)分支发 33104,但老端额外调用的
    /// BaseDungeonModel.dungeonActivity(非 331 协议家族)不镜像,注释存档。
    /// </summary>
    public sealed partial class CustomActivityController
    {
        // ---- 老端 cfg_custom_activity.ACT_ID 数值(业务 base_type,非本项目协议号),仅供 RequestActDetail
        // 分发用。数值来源 ConfigCustomActivity.json "ACT_ID" 字典(yu_client h5/bin/assets/resource/config/
        // client/ConfigCustomActivity.json:2-95)。----
        private const int ACT_ID_FLOWERRANK = 2;
        private const int ACT_ID_FTVCOLLECTION = 4;
        private const int ACT_ID_DAILY_RECHARGE = 6;
        private const int ACT_ID_ACC_RECHARGE = 7;
        private const int ACT_ID_TOP_PLAYER = 10;
        private const int ACT_ID_MORE = 12;
        private const int ACT_ID_EGGS = 13;
        private const int ACT_ID_DOUBLE = 16;
        private const int ACT_ID_CLOUD_PURCHASE = 18;
        private const int ACT_ID_actMarriage = 25;
        private const int ACT_ID_TURNTABLE = 28;
        private const int ACT_ID_RECHARGERANK = 33;
        private const int ACT_ID_CONSUMERANK = 39;
        private const int ACT_ID_ZERO_MALL = 36;
        private const int ACT_ID_WEAPON_RENTAL = 44;
        private const int ACT_ID_SEVENDAYCHALLENGE = 46;
        private const int ACT_ID_YOYO_ACT = 47;
        private const int ACT_ID_COST_RANK = 48;
        private const int ACT_ID_MONEYTREE = 50;
        // FEASTBOSS = 51(FEAST_BOSS_BASE_TYPE 已在主文件定义)。
        private const int ACT_ID_MONSTER_INVASION = 52;
        private const int ACT_ID_KF_COST_RANK = 53;
        private const int ACT_ID_MOUNT_TURNTABLE = 54;
        private const int ACT_ID_FTVACTIVENESS = 56;
        private const int ACT_ID_SAIBOTREASURE = 58;
        private const int ACT_ID_SURPRISE_EGG = 60;
        private const int ACT_ID_HOLYCALL = 67;
        private const int ACT_ID_GODPRAYER = 72;
        private const int ACT_ID_RECHARGERETURN = 74;
        private const int ACT_ID_BETA_ACT = 77;
        private const int ACT_ID_OPTIONALLOTTO = 76;
        private const int ACT_ID_LUC_TREA = 80;
        private const int ACT_ID_ONLINE_DRAW = 81;
        private const int ACT_ID_RED_PACKET_RAIN = 82;
        private const int ACT_ID_ACT_TURNTABLE = 83;
        private const int ACT_ID_REAL_LOTTERY = 84;
        private const int ACT_ID_RECHARGE_RESET = 86;
        private const int ACT_ID_FORTUNECAT = 87;
        private const int ACT_ID_ACTCHALLENGE = 91;
        private const int ACT_ID_CONVOY = 92;
        private const int ACT_ID_DESTINY_TURNTABLE = 99;
        private const int ACT_ID_TURNTABLE_100 = 100;
        private const int ACT_ID_LUC_TREA_TWO = 102;
        private const int ACT_ID_GASHAPON = 103;
        private const int ACT_ID_FIGHT_RANK = 104;
        private const int ACT_ID_MANY_RECHARGE = 107;
        private const int ACT_ID_ADVERTISEMENT = 111;
        private const int ACT_ID_QUESTIONNAIRE_ACT_BASE_TYPE = 90;
        private const int ACT_ID_FTVINVEST = 62;
        private const int ACT_ID_BIND_JAGE_WISH = 127;
        // "见到即拉"特判用(自动循环 轮17三镜头验收补,Model.ts:378-385/450-453)。
        private const int ACT_ID_ATTENTION = 70;
        private const int ACT_ID_MONEYTREE_SHOP = 89;
        private const int ACT_ID_LEVEL_WINDOW_REWARD = 108;
        private const int ACT_ID_DAILY_LOGIN = 113;
        private const int ACT_ID_ATLISTPURCHASE = 114;
        // base_type==120(超值礼包)数据变异用(自动循环 轮17三镜头验收补,ts:1010-1029)。老端用完整
        // ErlangParser 解析 condition 找 {is_free,1} 元组;本端沿用 OVER_VIEW 遍历补拉那套"近似正则"先例,
        // 只识别形如 "{is_free,1}"(允许空白)的字面写法,不是通用 Erlang term 解析器。
        private const int ACT_ID_SUPER_GIFT = 120;
        private static readonly Regex IsFreeConditionRegex = new Regex(@"\{\s*is_free\s*,\s*1\s*\}", RegexOptions.Compiled);

        private static void ShowError(int errorCode) => TipsManager.Toast("错误(" + errorCode + ")"); // 错误码表未移植,显码降级(仿 MarriageController)

        /// <summary>P1 框架核心注册(33101 在主文件 Register() 里已注册)。由主文件 Register() 调用。</summary>
        private void RegisterCore()
        {
            RegisterProtocal(Proto.CUSTOM_ACT_ERROR, On33100);
            RegisterProtocal(Proto.CUSTOM_ACT_ADD, On33102);
            RegisterProtocal(Proto.CUSTOM_ACT_REMOVE, On33103);
            RegisterProtocal(Proto.CUSTOM_ACT_DETAIL, On33104);
            RegisterProtocal(Proto.CUSTOM_ACT_CLAIM, On33105);
            RegisterProtocal(Proto.CUSTOM_ACT_ALLCOUNT, On33106);
            RegisterProtocal(Proto.CUSTOM_ACT_REFRESH, On33108);
        }

        // ---------------------------------------------------------------------------------------
        // 33100/33102/33103/33104/33105/33106/33108(33101 在主文件)
        // ---------------------------------------------------------------------------------------

        /// <summary>331 家族通用错误码(对标老端 On33100:926-931,仅 error_code!=1012 显错)。</summary>
        private void On33100(NetReader r)
        {
            int code = r.ReadI32();
            if (code != 1012) ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_ERROR, code);
            GameLog.Info("CustomActivity", "33100 通用错误码 code={0}", code);
        }

        /// <summary>活动增量新开(对标老端 On33102→AddActInfo)。**"见到即拉"特判**(自动循环 轮17三镜头验收补,
        /// 镜像 Model.ts:425-459):MONEYTREE_SHOP(89)/DAILY_LOGIN(113)/ATTENTION(70) 三个(与 On33101 的
        /// 全量版清单不对称,见 RequestSeeOnArrivalDetailsIncremental 注释)。</summary>
        private void On33102(NetReader r)
        {
            List<CustomActivityModel.ActEntry> list = r.ReadArray(CustomActivityModel.ReadActEntry);
            CustomActivityModel.Instance.AddActEntries(list);
            RequestSeeOnArrivalDetailsIncremental(list);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_LIST_ADD);
            GameLog.Info("CustomActivity", "33102 活动增量新开 count={0}", list.Count);
        }

        /// <summary>活动增量关闭(对标老端 On33103→DeleteActInfo)。</summary>
        private void On33103(NetReader r)
        {
            List<(int BaseType, int SubType)> keys = r.ReadArray(rr => ((int)rr.ReadU16(), (int)rr.ReadU16()));
            CustomActivityModel.Instance.RemoveActEntries(keys);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_LIST_REMOVE);
            GameLog.Info("CustomActivity", "33103 活动增量关闭 count={0}", keys.Count);
        }

        /// <summary>单活动通用详情(默认兜底号)。对标老端 On33104:970-1082 的通用落地部分(base_type 专属
        /// 特判如 98/8 充值直购、115 每日直购删图标属 UI 侧,本轮数据层不镜像,P2-P6/UI 尾包按需读
        /// CustomActivityModel.GetDetail 自行处理)。**base_type==120 数据变异镜像**(ts:1010-1029,自动循环
        /// 轮17三镜头验收补):入库前扫描 reward_list,每档解析 Condition 找 {is_free,1};找不到(非免费档)
        /// 就把该档 Status 强改成 2,再落 Model——这是"入库前变异原始数据"而非落地后另存一份,和老端一致。
        /// 老端同段落还有 is_have_receive/product_id/130级弹窗(SuperGiftView)等纯 UI 侧逻辑(ts:1030-1080),
        /// 数据层轮不镜像。</summary>
        private void On33104(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            List<CustomActivityModel.DetailReward> list = r.ReadArray(rr => new CustomActivityModel.DetailReward
            {
                Grade = rr.ReadU16(), FormType = rr.ReadU8(), Status = rr.ReadU8(), ReceiveTimes = rr.ReadU16(),
                Name = rr.ReadString(), Desc = rr.ReadString(), Condition = rr.ReadString(), Reward = rr.ReadString(),
            });
            if (baseType == ACT_ID_SUPER_GIFT)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (!IsFreeConditionRegex.IsMatch(list[i].Condition ?? string.Empty)) list[i].Status = 2;
                }
            }
            CustomActivityModel.Instance.SetDetail(baseType, subType, list);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, baseType, subType);
            GameLog.Info("CustomActivity", "33104 单活动详情 base={0} sub={1} rewardN={2}", baseType, subType, list.Count);
        }

        /// <summary>通用领取/操作结果回执(对标老端 On33105:1084-1106)。失败 ShowError;成功落 Model。
        /// EVT_CUSTOMACT_RESULT 两种情形都发(对标本仓库既有 EVT_MARRIAGE_*_RESULT 惯例,由参数 code 区分),
        /// 老端 VIPGIFT(71) buy_ing 标记复位 / CREAT_ROLE_GIFT(122) 倒计时清理属 UI 状态,本轮不镜像。</summary>
        private void On33105(NetReader r)
        {
            int code = r.ReadI32();
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            int grade = r.ReadU16();
            if (code == 1)
            {
                CustomActivityModel.Instance.SetClaimResult(baseType, subType, grade, code);
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, baseType, subType, code);
            GameLog.Info("CustomActivity", "33105 通用领取回执 code={0} base={1} sub={2} grade={3}", code, baseType, subType, grade);
        }

        /// <summary>全服计数(对标老端 On33106:1108-1112)。</summary>
        private void On33106(NetReader r)
        {
            var entry = new CustomActivityModel.AllCountEntry
            {
                BaseType = r.ReadU16(), SubType = r.ReadU16(), ModId = r.ReadU16(),
                CounterId = r.ReadU16(), Count = r.ReadU16(), Grade = r.ReadU16(),
            };
            CustomActivityModel.Instance.SetAllCount(entry);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_ALLCOUNT_UPDATE, entry.BaseType, entry.SubType);
            GameLog.Info("CustomActivity", "33106 全服计数 base={0} sub={1} mod={2} counter={3} count={4} grade={5}",
                entry.BaseType, entry.SubType, entry.ModId, entry.CounterId, entry.Count, entry.Grade);
        }

        /// <summary>活动刷新批量指令(对标老端 On33108:1114-1120),逐条转 RequestActDetail。</summary>
        private void On33108(NetReader r)
        {
            List<(int BaseType, int SubType)> values = r.ReadArray(rr => ((int)rr.ReadU16(), (int)rr.ReadU16()));
            for (int i = 0; i < values.Count; i++)
            {
                RequestActDetail(values[i].BaseType, values[i].SubType);
            }
            GameLog.Info("CustomActivity", "33108 活动刷新指令 count={0}", values.Count);
        }

        // ---------------------------------------------------------------------------------------
        // C2S 请求方法。RequestActList()=既有 RequestActivityList()(33101 裸包,主文件已有,不重复定义)。
        // ---------------------------------------------------------------------------------------

        /// <summary>通用领取(33105 "hhh" BaseType,SubType,Grade)。</summary>
        public void RequestClaim(int baseType, int subType, int grade) =>
            SendFmt(Proto.CUSTOM_ACT_CLAIM, "hhh", baseType, subType, grade);

        /// <summary>全服计数(33106,老端 args.length>=5 分支镜像:BaseType,SubType,ModId,CounterId,Grade
        /// 全部齐备才发,对标 ts:359-365)。</summary>
        public void RequestAllCount(int baseType, int subType, int modId, int counterId, int grade) =>
            SendFmt(Proto.CUSTOM_ACT_ALLCOUNT, "hhhhh", baseType, subType, modId, counterId, grade);

        /// <summary>
        /// 单活动详情分发(对标老端 RequireActInfo,CustomActivityModel.ts:965-1122)。按 base_type 走专属协议,
        /// 否则兜底发 33104。要求活动已在 CustomActivityModel 的活动列表中(对标老端 `let act_info =
        /// this.GetActInfo(...); if (!act_info) return`),未知活动不发送任何协议。
        /// </summary>
        public void RequestActDetail(int baseType, int subType)
        {
            if (CustomActivityModel.Instance.GetActEntry(baseType, subType) == null) return;

            switch (baseType)
            {
                case ACT_ID_ZERO_MALL:
                    SendFmt(Proto.CUSTOM_ACT_ZEROMALL_PANEL, "h", subType);
                    break;

                case ACT_ID_DAILY_RECHARGE:
                    SendFmt(Proto.RECHARGE_STAT_DAILY_ACCUM_INFO, "h", subType);
                    SendFmt(Proto.RECHARGE_STAT_DAILY_ACCUM_REWARD, "h", subType);
                    break;

                case ACT_ID_TOP_PLAYER:
                    // 真镜像(自动循环 轮17 三镜头验收订正):原"TopPlayerController 的 OnRoleInfoUpdate→
                    // RequestOpenRanksAsync 功能上覆盖老端本分支效果"断言已被证伪——那条路径受角色等级
                    // (>=130)/开服天数(<=8)门禁,只在满足条件时才主动触发一次;而老端 RequireActInfo 的
                    // TOP_PLAYER 分支(CustomActivityModel.ts:978-988)是"见到 33101 活动列表里的 TOP_PLAYER
                    // 条目就应答"语义,无该等级/开服天数门禁。两条触发路径语义不同,不能互相替代,必须都保留。
                    // 镜像老端:先发 22502(sub_type),再转调 TopPlayerController 按 config_rush_rank 时间窗
                    // 遍历发 22501(RequestOpenRanksAsync 已由 P6 改 public 供此处调用)。
                    SendFmt(Proto.TOP_PLAYER_GOAL_INFO, "h", subType);
                    _ = TopPlayerController.Instance.RequestOpenRanksAsync();
                    break;

                case ACT_ID_CLOUD_PURCHASE:      // 云购(死号族 33112-16,老端本分支已注释)
                case ACT_ID_WEAPON_RENTAL:       // 神兵租借(服务端 read+handle 均活,pp_custom_act_list.erl:23/27;死因=老端未注册,故不接,老端本分支已注释)
                case ACT_ID_QUESTIONNAIRE_ACT_BASE_TYPE: // 问卷(33236 走独立入口 OPEN_QUESTIONNAIRE_VIEW,老端本分支已注释)
                case ACT_ID_SEVENDAYCHALLENGE:   // 老端本分支空
                case ACT_ID_YOYO_ACT:            // 摇摇乐(死号族,老端本分支已注释)
                case ACT_ID_COST_RANK:           // 节日排行(死号族 33188/33189,老端本分支已注释)
                case ACT_ID_EGGS:                // 老砸蛋(死号族 33120-23,老端本分支已注释)
                case ACT_ID_MONSTER_INVASION:    // 非331协议(MonsterInvasionModel),老端本分支已注释
                case ACT_ID_CONVOY:              // 非331协议(BossModel 押镖),老端本分支已注释
                case ACT_ID_KF_COST_RANK:        // 跨服消费排行(死号族 33203/33204,老端本分支已注释)
                case ACT_ID_SURPRISE_EGG:        // 惊喜扭蛋(死号族 33205-08,老端本分支已注释)
                case ACT_ID_GODPRAYER:           // 神佑祈愿(死号族 33176/33177,老端本分支已注释)
                case ACT_ID_ACT_TURNTABLE:       // 活跃度转盘(死号族 33218-20,老端本分支已注释)
                case ACT_ID_REAL_LOTTERY:        // 同上死号族,老端本分支已注释
                case ACT_ID_ACTCHALLENGE:        // 非331协议(ActChallengeModel),老端本分支已注释
                case ACT_ID_FIGHT_RANK:          // 升战排行——死系统(22601-03),spec §1/§2-4 明确不镜像,整支不发送
                    // 死分支不镜像:老端该分支要么整段注释,要么(FIGHT_RANK)驱动的是已判定彻底死的系统,
                    // 本端不发送任何协议(包括其中可能夹带的通用 33104),与"死号严禁注册/发送"铁律一致。
                    break;

                case FEAST_BOSS_BASE_TYPE: // = 51,老端本就是纯本地计算不发协议(FEASTBOSS,见主文件常量)
                    // EvaluateFeastBoss() 已从 33101 缓存列表扫描驱动(见主文件),此处无需重复。
                    break;

                case ACT_ID_ACC_RECHARGE:
                    {
                        SendFmt(Proto.CUSTOM_ACT_DETAIL, "hh", baseType, subType);
                        CustomActivityModel.ActEntry entry = CustomActivityModel.Instance.GetActEntry(baseType, subType);
                        SendFmt(Proto.RECHARGE_STAT_ACT_RECHARGE, "hhi", baseType, subType, entry != null ? entry.Stime : 0);
                        break;
                    }

                case ACT_ID_MORE:
                case ACT_ID_DOUBLE:
                    SendFmt(Proto.CUSTOM_ACT_DETAIL, "hh", baseType, subType);
                    // 老端额外调用 BaseDungeonModel.dungeonActivity(base_type,sub_type)——非 331 协议家族,
                    // 超出本轮范围,注释存档不新发(对标 spec §2-4)。
                    break;

                case ACT_ID_MONEYTREE:
                case ACT_ID_MOUNT_TURNTABLE:
                    SendFmt(Proto.CUSTOM_ACT_MONEYTREE_PANEL, "hh", baseType, subType);
                    break;

                case ACT_ID_HOLYCALL:
                    SendFmt(Proto.CUSTOM_ACT_HOLYCALL_PANEL, "hh", baseType, subType);
                    break;

                case ACT_ID_FTVACTIVENESS:
                    SendFmt(Proto.CUSTOM_ACT_DETAIL, "hh", baseType, subType);
                    break;

                case ACT_ID_SAIBOTREASURE:
                    SendFmt(Proto.CUSTOM_ACT_SAIBO_PANEL, "hh", baseType, subType);
                    break;

                case ACT_ID_FTVCOLLECTION:
                    SendFmt(Proto.CUSTOM_ACT_DETAIL, "hh", baseType, subType);
                    break;

                case ACT_ID_TURNTABLE:
                    SendFmt(Proto.CUSTOM_ACT_BINDDIAMOND_PANEL, "hh", baseType, subType);
                    SendFmt(Proto.CUSTOM_ACT_BINDDIAMOND_RECORD, "hh", baseType, subType);
                    break;

                case ACT_ID_FTVINVEST:
                    SendFmt(Proto.CUSTOM_ACTIVITY_FTVINVEST, "hh", baseType, subType);
                    break;

                case ACT_ID_RECHARGERETURN:
                case ACT_ID_RECHARGE_RESET:
                    // 老端本分支不发协议,仅本地 Fire(MAINUI_TOP_LOAD_SUCCEED)(非331协议事件),不镜像。
                    break;

                case ACT_ID_FLOWERRANK:
                case ACT_ID_RECHARGERANK:
                case ACT_ID_CONSUMERANK:
                    SendFmt(Proto.CUSTOM_ACT_DETAIL, "hh", baseType, subType);
                    break;

                case ACT_ID_OPTIONALLOTTO:
                    SendFmt(Proto.CUSTOM_ACT_LOTTO_PANEL, "hh", baseType, subType);
                    break;

                case ACT_ID_actMarriage:
                    // 老端实参是 (sub_type, 1) 不是 (base_type, sub_type)——fmt "hc"。
                    SendFmt(Proto.CUSTOM_ACT_MARRIAGE_ACT_INFO, "hc", subType, 1);
                    break;

                case ACT_ID_BETA_ACT:
                    SendFmt(Proto.CUSTOM_ACT_BETA_RECHARGE_RETURN); // 裸包,老端 fmt 表未命中任何分支落 else 无参
                    break;

                case ACT_ID_LUC_TREA:
                    SendFmt(Proto.CUSTOM_ACT_DETAIL, "hh", baseType, subType);
                    SendFmt(Proto.CUSTOM_ACT_LUCTREA_PANEL, "hh", baseType, subType);
                    break;

                case ACT_ID_ONLINE_DRAW:
                    SendFmt(Proto.CUSTOM_ACT_ONLINEDRAW_PANEL, "hh", baseType, subType);
                    break;

                case ACT_ID_RED_PACKET_RAIN:
                    // 老端只传 sub_type(无 base_type),fmt "h"。
                    SendFmt(Proto.CUSTOM_ACT_REDRAIN_PANEL, "h", subType);
                    break;

                case ACT_ID_FORTUNECAT:
                    SendFmt(Proto.CUSTOM_ACT_FORTUNECAT_INFO, "hhc", baseType, subType, 0);
                    break;

                case ACT_ID_DESTINY_TURNTABLE:
                    SendFmt(Proto.CUSTOM_ACT_DESTINY_PANEL, "hh", baseType, subType);
                    SendFmt(Proto.CUSTOM_ACT_DETAIL, "hh", baseType, subType);
                    break;

                case ACT_ID_LUC_TREA_TWO:
                    SendFmt(Proto.CUSTOM_ACT_LUCTREA2_PANEL, "hh", baseType, subType);
                    break;

                case ACT_ID_TURNTABLE_100:
                    SendFmt(Proto.CUSTOM_ACT_TURN100_PANEL, "hh", baseType, subType);
                    break;

                case ACT_ID_GASHAPON:
                    SendFmt(Proto.CUSTOM_ACT_DETAIL, "hh", baseType, subType);
                    SendFmt(Proto.CUSTOM_ACT_GASHAPON_INFO, "hh", baseType, subType);
                    break;

                case ACT_ID_MANY_RECHARGE:
                    SendFmt(Proto.CUSTOM_ACT_MANYRECHARGE_PANEL, "hh", baseType, subType);
                    break;

                case ACT_ID_BIND_JAGE_WISH:
                    SendFmt(Proto.CUSTOM_ACT_BINDJAGE_INFO, "hh", baseType, subType);
                    SendFmt(Proto.CUSTOM_ACT_DETAIL, "hh", baseType, subType);
                    break;

                case ACT_ID_ADVERTISEMENT:
                    SendFmt(Proto.CUSTOM_ACT_DETAIL, "hh", baseType, subType);
                    SendFmt(Proto.CUSTOM_ACT_AD_CD_LIST, "hh", baseType, subType);
                    break;

                default:
                    // 兜底(对标老端 RequireActInfo 末尾 else,Model.ts:1119-1121)。
                    SendFmt(Proto.CUSTOM_ACT_DETAIL, "hh", baseType, subType);
                    break;
            }
        }

        /// <summary>"见到即拉"特判——全量刷新版(对标老端 SaveActInfo 的 switch-case,Model.ts:356-393),
        /// 由主文件 On33101 落 Model 后调用。命中 base_type 立即 RequestActDetail(等价 RequireActInfo)。
        /// **核对偏差存档**(自动循环 轮17三镜头验收):spec 给的清单是 89/108/113/114/70 五个,且明确排除
        /// 62/117/121/51(已有专属通道);但逐字段回 ts:378-385 原文核对后发现,SaveActInfo(33101)的 switch
        /// 确实是这 5 个(MONEYTREE_SHOP/LEVEL_WINDOW_REWARD/DAILY_LOGIN/ATLISTPURCHASE/ATTENTION,FTVINVEST
        /// 虽同在该 case 但已由主文件 RequestDirectBranchDetails 覆盖,不重复),AddActInfo(33102,見下方
        /// RequestSeeOnArrivalDetailsIncremental)只有其中 3 个——两个函数的清单在老端本就不对称,不是笔误,
        /// 本端如实分别镜像,不能对两个入口套用同一份清单。</summary>
        private void RequestSeeOnArrivalDetailsFull(IReadOnlyList<CustomActivityModel.ActEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                CustomActivityModel.ActEntry e = entries[i];
                switch (e.BaseType)
                {
                    case ACT_ID_MONEYTREE_SHOP:
                    case ACT_ID_LEVEL_WINDOW_REWARD:
                    case ACT_ID_DAILY_LOGIN:
                    case ACT_ID_ATLISTPURCHASE:
                    case ACT_ID_ATTENTION:
                        RequestActDetail(e.BaseType, e.SubType);
                        break;
                }
            }
        }

        /// <summary>"见到即拉"特判——增量新开版(对标老端 AddActInfo 的 switch-case,Model.ts:425-459),
        /// 由本文件 On33102 落 Model 后调用。**注意清单比全量版少 2 个**:老端 AddActInfo 只对
        /// MONEYTREE_SHOP/DAILY_LOGIN/ATTENTION 三个 RequireActInfo,LEVEL_WINDOW_REWARD/ATLISTPURCHASE
        /// 不在这个 switch 里(ts:450-454 逐字段核对确认,并非笔误遗漏——如实镜像这一不对称)。</summary>
        private void RequestSeeOnArrivalDetailsIncremental(IReadOnlyList<CustomActivityModel.ActEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                CustomActivityModel.ActEntry e = entries[i];
                switch (e.BaseType)
                {
                    case ACT_ID_MONEYTREE_SHOP:
                    case ACT_ID_DAILY_LOGIN:
                    case ACT_ID_ATTENTION:
                        RequestActDetail(e.BaseType, e.SubType);
                        break;
                }
            }
        }
    }
}
