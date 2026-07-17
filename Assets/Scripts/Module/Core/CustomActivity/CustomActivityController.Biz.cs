using System.Collections.Generic;
using System.Text.RegularExpressions;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.CustomActivity
{
    /// <summary>
    /// 定制活动商业礼包族(自动循环 轮17 P5):ZERO_MALL=36(33136/37/38)/FTVINVEST=62(33212,同时升级 On33211
    /// 落地 Investments)/VIPGIFT=71(33215)/DAILYSUPPLY=61(33209)/NAMEVERIFY=69(33169,空包)/批量兑换(33179)/
    /// QUESTIONNAIRE=90(33236)/MANY_RECHARGE=107(33247)/冲级(33248)/ADVERTISEMENT=111(33250/33251recv-only)/
    /// RED_ENVELOPE_REBATE=117(33256,同时升级 On33255 落地12字段)/CARNIVAL=118(33258)/
    /// TIRED_CHARGE_POLITE=121(33259)/OVER_VIEW=126(33264+RequireOverViewRew遍历补拉)/
    /// RARE_SURFACE=128(33265/33257recv-only)/33197获奖记录/33140嗨点(防御recv不发送)/33115完美情缘/
    /// 33216封测充值返还/充值统计15955-15960。
    ///
    /// wire 全部逐号回 pt_331.erl/pt_332.erl/pt_159.erl 原文 write/item_to_bin_N 定义核对(而非仅
    /// r17_server_customactivity.md 侦察表摘要)——核对时发现并订正侦察表 2 处字段序误记(33136/33264,
    /// 见 CustomActivityModel.Biz.cs 对应类型注释)。C2S fmt 全部回 yu_client CustomActivityController.ts
    /// 233-412 行 SCMD_REQUEST 分发表核对(与服务端 read() 定义互证一致,未见分歧)。
    ///
    /// 事件粒度收敛(spec §0/15b 先例):只用 P1 已定义的通用事件(EVT_CUSTOMACT_DETAIL_UPDATE/RESULT),
    /// 不新增专用事件(GlobalEvent.cs 是 P1 独占的共享文件)。无 BaseType/SubType 概念的号(33248/33251/
    /// 15959/15960/33140)只落 Model,不发事件——UI 尾包直接读 Model 快照。
    ///
    /// OVER_VIEW(126)遍历补拉:镜像老端 RequireOverViewRew(CustomActivityModel.ts:401-416)——每次 33101 全量
    /// 列表刷新后,扫描 base_type==OVER_VIEW 的条目,解析其 condition 字段取出 {base_type,show_id} 对列表,
    /// 按 show_id 在当前活动列表里找到真正的 (base_type,sub_type),对每个命中发 33264。老端用完整的
    /// ErlangParser 解析 condition(任意嵌套 Erlang term);本端**未见服务端 condition 真实取值样本**(该字段
    /// 是运行时 data_custom_act/data_custom_act_extra 动态数据,不在客户端静态配置里),用正则提取形如
    /// "{数字,数字}" 的二元组作为近似实现——足以覆盖典型"list of 2-tuple"写法,但不是通用 Erlang term 解析器;
    /// 若真实 condition 结构更复杂(嵌套元组/非数字字段),需要补真解析器,TODO 留验收镜头判定。
    /// 依赖 EVT_CUSTOMACT_LIST_UPDATE(P1 通用事件),订阅只在首次 RegisterBiz() 时挂一次(_overViewHookInstalled
    /// 实例字段守卫)——CustomActivityController.Instance 是进程级单例、Register()/Dispose() 可重入(断线重登),
    /// 但本类主文件 Dispose() 不在本代理可编辑范围(spec 铁律:仅 On33211/On33255 方法体),故不能在 Dispose()
    /// 里配对 Off();用守卫确保跨 Init() 重入只订阅一次,且 Dispose() 期间上游 On33101 协议处理器本就被
    /// BaseController 基类注销、不会再 Emit LIST_UPDATE,故长期订阅不会在断线期间误触发,无副作用。
    /// </summary>
    public sealed partial class CustomActivityController
    {
        // ---- 本包独有 ACT_ID(Controller.Core.cs 已定义的同名常量直接复用,不重复定义:ACT_ID_ZERO_MALL/
        // ACT_ID_FTVINVEST/ACT_ID_DAILY_RECHARGE/ACT_ID_ACC_RECHARGE/ACT_ID_actMarriage/ACT_ID_BETA_ACT/
        // ACT_ID_QUESTIONNAIRE_ACT_BASE_TYPE/ACT_ID_MANY_RECHARGE/ACT_ID_ADVERTISEMENT;
        // TIRED_CHARGE_POLITE_BASE_TYPE 复用主文件常量) ----
        private const int ACT_ID_VIPGIFT = 71;
        private const int ACT_ID_DAILYSUPPLY = 61;
        private const int ACT_ID_NAMEVERIFY = 69;
        private const int ACT_ID_CARNIVAL = 118;
        private const int ACT_ID_OVER_VIEW = 126;
        private const int ACT_ID_RARE_SURFACE = 128;
        // CON_RECHARGE=109(ConfigCustomActivity.json ACT_ID.CON_RECHARGE),B3 On15959 追发 RequestActDetail 用。
        private const int ACT_ID_CON_RECHARGE = 109;

        private bool _overViewHookInstalled;

        private static readonly Regex OverViewPairRegex = new Regex(@"\{\s*(\d+)\s*,\s*(\d+)\s*\}", RegexOptions.Compiled);

        /// <summary>P5 商业礼包族注册,由主文件 Register() 调用。</summary>
        private void RegisterBiz()
        {
            RegisterProtocal(Proto.CUSTOM_ACT_ZEROMALL_PANEL, On33136);
            RegisterProtocal(Proto.CUSTOM_ACT_ZEROMALL_BUY, On33137);
            RegisterProtocal(Proto.CUSTOM_ACT_ZEROMALL_REBATE, On33138);
            RegisterProtocal(Proto.CUSTOM_ACT_FTVINVEST_BUY, On33212);
            RegisterProtocal(Proto.CUSTOM_ACT_VIPGIFT_SET_GRADE, On33215);
            RegisterProtocal(Proto.CUSTOM_ACT_DAILYSUPPLY_LIVENESS, On33209);
            RegisterProtocal(Proto.CUSTOM_ACT_NAMEVERIFY_CONFIRM, On33169);
            RegisterProtocal(Proto.CUSTOM_ACT_BATCH_EXCHANGE, On33179);
            RegisterProtocal(Proto.CUSTOM_ACT_QUESTIONNAIRE_SUBMIT, On33236);
            RegisterProtocal(Proto.CUSTOM_ACT_MANYRECHARGE_PANEL, On33247);
            RegisterProtocal(Proto.CUSTOM_ACT_LEVEL_RUSH_GIFT, On33248);
            RegisterProtocal(Proto.CUSTOM_ACT_AD_CD_LIST, On33250);
            RegisterProtocal(Proto.CUSTOM_ACT_RUSH_RANK_TOP_PLAYER_PUSH, On33251); // recv-only 防御
            RegisterProtocal(Proto.CUSTOM_ACT_REDENVELOPE_WITHDRAW, On33256);
            RegisterProtocal(Proto.CUSTOM_ACT_CARNIVAL_TASK, On33258);
            RegisterProtocal(Proto.CUSTOM_ACT_TIRED_CHARGE_POLITE, On33259);
            RegisterProtocal(Proto.CUSTOM_ACT_OVERVIEW_REWARD, On33264);
            RegisterProtocal(Proto.CUSTOM_ACT_RARESURFACE_CLAIM, On33265);
            RegisterProtocal(Proto.CUSTOM_ACT_REWARD_LIST_PUSH, On33257); // recv-only 防御
            RegisterProtocal(Proto.CUSTOM_ACT_WIN_LOG, On33197);
            RegisterProtocal(Proto.CUSTOM_ACT_HI_POINT_INFO, On33140);   // 防御 recv,不发送(见 §B21)
            RegisterProtocal(Proto.CUSTOM_ACT_MARRIAGE_ACT_INFO, On33115);
            RegisterProtocal(Proto.CUSTOM_ACT_BETA_RECHARGE_RETURN, On33216);
            RegisterProtocal(Proto.RECHARGE_STAT_DAILY_ACCUM_INFO, On15955);
            RegisterProtocal(Proto.RECHARGE_STAT_DAILY_ACCUM_REWARD, On15956);
            RegisterProtocal(Proto.RECHARGE_STAT_ACT_RECHARGE, On15957);
            RegisterProtocal(Proto.RECHARGE_STAT_POLITE_RECHARGE, On15958);
            RegisterProtocal(Proto.RECHARGE_STAT_TODAY, On15959);
            RegisterProtocal(Proto.RECHARGE_STAT_HISTORY, On15960);

            if (!_overViewHookInstalled)
            {
                _overViewHookInstalled = true;
                EventDispatcher.On(GlobalEvent.EVT_CUSTOMACT_LIST_UPDATE, OnListUpdateForOverView);
            }
        }

        // ---------------------------------------------------------------------------------------
        // ZERO_MALL=36(33136/33137/33138)
        // ---------------------------------------------------------------------------------------

        public void RequestZeroMallBuy(int subType, int grade) => SendFmt(Proto.CUSTOM_ACT_ZEROMALL_BUY, "hh", subType, grade);
        public void RequestZeroMallRebate(int subType, int grade) => SendFmt(Proto.CUSTOM_ACT_ZEROMALL_REBATE, "hh", subType, grade);

        /// <summary>0元豪礼界面(对标 pt_331.erl write(33136):997-1014,item_to_bin_31 8字段——**订正**见
        /// CustomActivityModel.Biz.cs ZeroMallRewardItem 注释)。**静默阈值镜像**(ts:1218-1222):失败时
        /// code==1012 不弹错,仅 return。**空列表删条目镜像**(ts:1240-1242,自动循环 轮17三镜头验收补):
        /// 成功且 reward_list 为空、且该活动仍在 ActList 里时,从 ActList 删除该条目并 Emit 与 On33103 同款
        /// LIST_REMOVE 事件(镜像老端 delete_func()→DeleteActInfo+return,不再落 ZeroMallPanel/发
        /// DETAIL_UPDATE)。老端另有 show_id==10+buy_day 的第二条删除分支(ts:1243-1264,依赖本端未镜像的
        /// info_cond/show_id 字段),本轮不镜像。</summary>
        private void On33136(NetReader r)
        {
            int code = r.ReadI32();
            int subType = r.ReadU16();
            List<CustomActivityModel.ZeroMallRewardItem> list = r.ReadArray(rr => new CustomActivityModel.ZeroMallRewardItem
            {
                Grade = rr.ReadU16(), FormType = rr.ReadU8(), Status = rr.ReadU8(), ReceiveTime = rr.ReadI32(),
                Name = rr.ReadString(), Desc = rr.ReadString(), Condition = rr.ReadString(), Reward = rr.ReadString(),
            });
            if (code == 1)
            {
                if (list.Count == 0 && CustomActivityModel.Instance.GetActEntry(ACT_ID_ZERO_MALL, subType) != null)
                {
                    CustomActivityModel.Instance.RemoveActEntries(new List<(int, int)> { (ACT_ID_ZERO_MALL, subType) });
                    EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_LIST_REMOVE);
                }
                else
                {
                    var panel = new CustomActivityModel.ZeroMallPanel { SubType = subType };
                    panel.RewardList.AddRange(list);
                    CustomActivityModel.Instance.SetZeroMallPanel(panel);
                    EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, ACT_ID_ZERO_MALL, subType);
                }
            }
            else if (code != 1012) ShowError(code);
            GameLog.Info("CustomActivity", "33136 0元豪礼界面 code={0} subType={1} rewardN={2}", code, subType, list.Count);
        }

        /// <summary>0元豪礼购买(pt_331.erl write(33137):1016-1026,Errcode,Grade,SubType)。</summary>
        private void On33137(NetReader r)
        {
            int code = r.ReadI32();
            int grade = r.ReadU16();
            int subType = r.ReadU16();
            if (code != 1) ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, ACT_ID_ZERO_MALL, subType, code);
            GameLog.Info("CustomActivity", "33137 0元豪礼购买 code={0} grade={1} subType={2}", code, grade, subType);
        }

        /// <summary>0元豪礼返利领取(pt_331.erl write(33138):1028-1038,字段序同33137)。</summary>
        private void On33138(NetReader r)
        {
            int code = r.ReadI32();
            int grade = r.ReadU16();
            int subType = r.ReadU16();
            if (code != 1) ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, ACT_ID_ZERO_MALL, subType, code);
            GameLog.Info("CustomActivity", "33138 0元豪礼返利 code={0} grade={1} subType={2}", code, grade, subType);
        }

        // ---------------------------------------------------------------------------------------
        // FTVINVEST=62(33212;33211 升级见主文件)
        // ---------------------------------------------------------------------------------------

        public void RequestFtvInvestBuy(int baseType, int subType, int lv) =>
            SendFmt(Proto.CUSTOM_ACT_FTVINVEST_BUY, "hhc", baseType, subType, lv);

        /// <summary>节日投资购买(pt_332.erl write(33212):445-463,RewardList=pt:write_object_list)。**追发镜像**
        /// (ts:1844-1854,自动循环 轮17三镜头验收补):成功后重拉 RequireActInfo(base,sub)(对标本端
        /// RequestActDetail),老端另开的 CongratulationObtainView 奖励弹窗属 UI 侧,本轮不镜像。</summary>
        private void On33212(NetReader r)
        {
            int code = r.ReadI32();
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            int lv = r.ReadU8();
            int loginDays = r.ReadU16();
            List<CustomActivityModel.RewardObj> rewardList = CustomActivityModel.ReadRewardObjList(r);
            if (code == 1)
            {
                var result = new CustomActivityModel.FtvInvestBuyResult { BaseType = baseType, SubType = subType, Lv = lv, LoginDays = loginDays };
                result.RewardList.AddRange(rewardList);
                CustomActivityModel.Instance.SetFtvInvestBuyResult(result);
                RequestActDetail(baseType, subType); // 重拉,镜像 ts:1850
            }
            else ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, baseType, subType, code);
            GameLog.Info("CustomActivity", "33212 节日投资购买 code={0} base={1} sub={2} lv={3} loginDays={4} rewardN={5}",
                code, baseType, subType, lv, loginDays, rewardList.Count);
        }

        // ---------------------------------------------------------------------------------------
        // VIPGIFT=71(33215)
        // ---------------------------------------------------------------------------------------

        public void RequestVipGiftSetGrade(int baseType, int subType, int gradeId) =>
            SendFmt(Proto.CUSTOM_ACT_VIPGIFT_SET_GRADE, "hhh", baseType, subType, gradeId);

        /// <summary>vip礼包设置折扣(pt_332.erl write(33215):502-518,NowCost=pt:write_object_list)。</summary>
        private void On33215(NetReader r)
        {
            int code = r.ReadI32();
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            int grade = r.ReadU16();
            List<CustomActivityModel.RewardObj> nowCost = CustomActivityModel.ReadRewardObjList(r);
            if (code == 1)
            {
                var info = new CustomActivityModel.VipGiftInfo { BaseType = baseType, SubType = subType, Grade = grade };
                info.NowCost.AddRange(nowCost);
                CustomActivityModel.Instance.SetVipGiftInfo(info);
            }
            else ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, baseType, subType, code);
            GameLog.Info("CustomActivity", "33215 vip礼包折扣 code={0} base={1} sub={2} grade={3} costN={4}",
                code, baseType, subType, grade, nowCost.Count);
        }

        // ---------------------------------------------------------------------------------------
        // DAILYSUPPLY=61(33209,双向均无 BaseType/SubType)
        // ---------------------------------------------------------------------------------------

        public void RequestDailySupplyLiveness() => SendFmt(Proto.CUSTOM_ACT_DAILYSUPPLY_LIVENESS);

        /// <summary>每日补给活跃度(pt_332.erl write(33209):416-422)。</summary>
        private void On33209(NetReader r)
        {
            int liveness = r.ReadU16();
            CustomActivityModel.Instance.SetDailySupplyLiveness(liveness);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, ACT_ID_DAILYSUPPLY, 0);
            GameLog.Info("CustomActivity", "33209 每日补给活跃度 liveness={0}", liveness);
        }

        // ---------------------------------------------------------------------------------------
        // NAMEVERIFY=69(33169,读写均空包)
        // ---------------------------------------------------------------------------------------

        public void RequestNameVerifyConfirm() => SendFmt(Proto.CUSTOM_ACT_NAMEVERIFY_CONFIRM);

        /// <summary>实名认证成功(pt_331.erl write(33169):1507-1511,空包)。</summary>
        private void On33169(NetReader r)
        {
            CustomActivityModel.Instance.MarkNameVerifyConfirmed(TimeUtil.NowSec());
            GameLog.Info("CustomActivity", "33169 实名认证成功(空包)");
        }

        // ---------------------------------------------------------------------------------------
        // 批量兑换(FTVSHOP/FTVEXCHANGE/ATLISTPURCHASE 共用,33179)
        // ---------------------------------------------------------------------------------------

        public void RequestBatchExchange(int baseType, int subType, int grade, int num) =>
            SendFmt(Proto.CUSTOM_ACT_BATCH_EXCHANGE, "hhhh", baseType, subType, grade, num);

        /// <summary>兑换多份奖励(pt_331.erl write(33179):1734-1748)。**字段序 ErrorCode,Num,BaseType,SubType,
        /// Grade**(非套模板的 ErrorCode,BaseType,SubType,Grade)。**追发镜像**(ts:1710-1718,自动循环
        /// 轮17三镜头验收补):成功后追发 33104(base,sub) 重拉该活动通用详情。</summary>
        private void On33179(NetReader r)
        {
            int code = r.ReadI32();
            int num = r.ReadU16();
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            int grade = r.ReadU16();
            if (code == 1)
            {
                CustomActivityModel.Instance.SetLastBatchExchange(new CustomActivityModel.BatchExchangeResult
                {
                    ErrorCode = code, Num = num, BaseType = baseType, SubType = subType, Grade = grade,
                });
                SendFmt(Proto.CUSTOM_ACT_DETAIL, "hh", baseType, subType); // 追发 33104,镜像 ts:1717
            }
            else ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, baseType, subType, code);
            GameLog.Info("CustomActivity", "33179 批量兑换 code={0} num={1} base={2} sub={3} grade={4}", code, num, baseType, subType, grade);
        }

        // ---------------------------------------------------------------------------------------
        // QUESTIONNAIRE=90(33236)
        // ---------------------------------------------------------------------------------------

        public void RequestQuestionnaireSubmit(int questionType) => SendFmt(Proto.CUSTOM_ACT_QUESTIONNAIRE_SUBMIT, "c", questionType);

        /// <summary>完成问卷调查(pt_332.erl write(33236):961-969)。无 BaseType/SubType,借用
        /// (QUESTIONNAIRE_ACT_BASE_TYPE,questionType)组成通用事件二元键。**三镜头订正**:老端 On33236
        /// (ts:2356-2361)全函数体没有任何 ShowError/Util.ErrorCodeShow 调用——该号老端从不弹错,不套通用
        /// "失败弹错"模板。**追发镜像**:老端判断是 `if (scmd.error_code)` 真值(非0即真,不是 ==1 判等),
        /// 命中即追发 33104(QUESTIONNAIRE_ACT_BASE_TYPE,question_type) 重拉。</summary>
        private void On33236(NetReader r)
        {
            int code = r.ReadI32();
            int questionType = r.ReadU8();
            if (code == 1)
                CustomActivityModel.Instance.SetLastQuestionnaire(new CustomActivityModel.QuestionnaireResult { ErrorCode = code, QuestionType = questionType });
            if (code != 0)
            {
                SendFmt(Proto.CUSTOM_ACT_DETAIL, "hh", ACT_ID_QUESTIONNAIRE_ACT_BASE_TYPE, questionType); // 追发33104,镜像ts:2358-2359
            }
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, ACT_ID_QUESTIONNAIRE_ACT_BASE_TYPE, questionType, code);
            GameLog.Info("CustomActivity", "33236 问卷调查 code={0} questionType={1}", code, questionType);
        }

        // ---------------------------------------------------------------------------------------
        // MANY_RECHARGE=107(33247)
        // ---------------------------------------------------------------------------------------

        /// <summary>多倍充值界面(pt_332.erl write(33247):1260-1270,无 Code;C2S 已由 Controller.Core.cs
        /// RequestActDetail 对 base_type==MANY_RECHARGE 自动追发"hh",此处补一个公开方法供主动刷新用)。</summary>
        public void RequestManyRechargePanel(int baseType, int subType) => SendFmt(Proto.CUSTOM_ACT_MANYRECHARGE_PANEL, "hh", baseType, subType);

        private void On33247(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            int times = r.ReadU8();
            CustomActivityModel.Instance.SetManyRechargeInfo(new CustomActivityModel.ManyRechargeInfo { BaseType = baseType, SubType = subType, Times = times });
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, baseType, subType);
            GameLog.Info("CustomActivity", "33247 多倍充值界面 base={0} sub={1} times={2}", baseType, subType, times);
        }

        // ---------------------------------------------------------------------------------------
        // 冲级礼包(33248,双向均无 BaseType/SubType/Code)
        // ---------------------------------------------------------------------------------------

        public void RequestLevelRushGift() => SendFmt(Proto.CUSTOM_ACT_LEVEL_RUSH_GIFT);

        private void On33248(NetReader r)
        {
            int minTime = r.ReadI32();
            int maxTime = r.ReadI32();
            CustomActivityModel.Instance.SetLevelRushGift(new CustomActivityModel.LevelRushGiftInfo { MinTime = minTime, MaxTime = maxTime });
            GameLog.Info("CustomActivity", "33248 冲级礼包 minTime={0} maxTime={1}", minTime, maxTime);
        }

        // ---------------------------------------------------------------------------------------
        // ADVERTISEMENT=111(33250;33251 recv-only)
        // ---------------------------------------------------------------------------------------

        /// <summary>广告定制活动冷却列表(pt_332.erl write(33250):1290-1307,item_to_bin_39={GradeId:32,
        /// CdTime:32};C2S 已由 Controller.Core.cs RequestActDetail 自动追发)。**追发镜像**(ts:2574-2579,
        /// 自动循环 轮17三镜头验收补):落地后无条件追发 33104(base,sub) 重拉通用详情。</summary>
        private void On33250(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            List<CustomActivityModel.AdCdItem> cdLists = r.ReadArray(rr => new CustomActivityModel.AdCdItem
            {
                GradeId = rr.ReadI32(), CdTime = rr.ReadI32(),
            });
            var v = new CustomActivityModel.AdCdList { BaseType = baseType, SubType = subType };
            v.CdLists.AddRange(cdLists);
            CustomActivityModel.Instance.SetAdCdList(v);
            SendFmt(Proto.CUSTOM_ACT_DETAIL, "hh", baseType, subType); // 追发33104,镜像ts:2577
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, baseType, subType);
            GameLog.Info("CustomActivity", "33250 广告cd列表 base={0} sub={1} cdN={2}", baseType, subType, cdLists.Count);
        }

        /// <summary>头号玩家提示(pt_332.erl write(33251):1309-1323)。**recv-only 防御**:C2S read(33251,_)->
        /// {ok,[]} 但 pp handle 内部 skip 不回写(疑似给别的模块用,r17_server_customactivity.md 已标注),
        /// 老端 fmt 表(ts:233-412)也未见任何 SendFmtToGame(33251,...) 分支——不提供 Request 方法。</summary>
        private void On33251(NetReader r)
        {
            var info = new CustomActivityModel.RushRankTopPlayerInfo
            {
                RushRankId = r.ReadI32(), Type = r.ReadI32(), Rank = r.ReadI32(), Value = r.ReadI32(), SubValue = r.ReadI32(),
            };
            CustomActivityModel.Instance.SetLastRushRankTopPlayer(info);
            GameLog.Info("CustomActivity", "33251 头号玩家提示(recv-only) rushRankId={0} type={1} rank={2}", info.RushRankId, info.Type, info.Rank);
        }

        // ---------------------------------------------------------------------------------------
        // RED_ENVELOPE_REBATE=117(33256;33255 升级见主文件)
        // ---------------------------------------------------------------------------------------

        public void RequestRedEnvelopeWithdraw(int type, int subtype, int withdrawType, string packageCode, string tokenId) =>
            SendFmt(Proto.CUSTOM_ACT_REDENVELOPE_WITHDRAW, "hhcss", type, subtype, withdrawType, packageCode, tokenId);

        /// <summary>红包返利提现(pt_332.erl write(33256):1445-1463)。**Errcode 是第3字段**(非开头/末尾)。</summary>
        private void On33256(NetReader r)
        {
            int type = r.ReadU16();
            int subtype = r.ReadU16();
            int errcode = r.ReadI32();
            int loginMoney = r.ReadU16();
            int rechargeMoney = r.ReadU16();
            int loginStatus = r.ReadU8();
            int rechargeStatus = r.ReadU8();
            if (errcode == 1)
            {
                CustomActivityModel.Instance.SetRedEnvelopeWithdrawResult(new CustomActivityModel.RedEnvelopeWithdrawResult
                {
                    Type = type, Subtype = subtype, Errcode = errcode, LoginMoney = loginMoney,
                    RechargeMoney = rechargeMoney, LoginStatus = loginStatus, RechargeStatus = rechargeStatus,
                });
            }
            else ShowError(errcode);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, type, subtype, errcode);
            GameLog.Info("CustomActivity", "33256 红包返利提现 errcode={0} type={1} subtype={2} loginMoney={3} rechargeMoney={4}",
                errcode, type, subtype, loginMoney, rechargeMoney);
        }

        // ---------------------------------------------------------------------------------------
        // CARNIVAL=118(33258)
        // ---------------------------------------------------------------------------------------

        public void RequestCarnivalTask(int type, int subtype) => SendFmt(Proto.CUSTOM_ACT_CARNIVAL_TASK, "hh", type, subtype);

        /// <summary>全民狂欢任务进度(pt_332.erl write(33258):1484-1501,item_to_bin_45={Grade:16,Process:32),
        /// 无 Code)。**追发镜像**(ts:2667-2671,自动循环 轮17三镜头验收补):落地后无条件追发 33104(type,
        /// subtype) 重拉通用详情。</summary>
        private void On33258(NetReader r)
        {
            int type = r.ReadU16();
            int subtype = r.ReadU16();
            List<CustomActivityModel.CarnivalTaskItem> taskList = r.ReadArray(rr => new CustomActivityModel.CarnivalTaskItem
            {
                Grade = rr.ReadU16(), Process = rr.ReadI32(),
            });
            var info = new CustomActivityModel.CarnivalTaskInfo { Type = type, Subtype = subtype };
            info.TaskList.AddRange(taskList);
            CustomActivityModel.Instance.SetCarnivalTaskInfo(info);
            SendFmt(Proto.CUSTOM_ACT_DETAIL, "hh", type, subtype); // 追发33104,镜像ts:2670
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, type, subtype);
            GameLog.Info("CustomActivity", "33258 全民狂欢任务进度 type={0} subtype={1} taskN={2}", type, subtype, taskList.Count);
        }

        // ---------------------------------------------------------------------------------------
        // TIRED_CHARGE_POLITE=121(33259;C2S 已由主文件 RequestDirectBranchDetails 在 On33101 内追发)
        // ---------------------------------------------------------------------------------------

        /// <summary>手动刷新累充有礼(主文件 RequestDirectBranchDetails 已在见到 121 条目时自动追发,
        /// 本方法供主动刷新用)。</summary>
        public void RequestTiredChargePolite(int baseType, int subType) => SendFmt(Proto.CUSTOM_ACT_TIRED_CHARGE_POLITE, "hh", baseType, subType);

        /// <summary>充值有礼奖励状态(pt_332.erl write(33259):1503-1524,item_to_bin_46/47 嵌套,无 Code)。</summary>
        private void On33259(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            int rechargeNum = r.ReadU16();
            int isRecharge = r.ReadU16();
            List<CustomActivityModel.TiredChargeGradeItem> list = r.ReadArray(rr =>
            {
                var g = new CustomActivityModel.TiredChargeGradeItem
                {
                    Grade = rr.ReadU16(), Condition = rr.ReadString(), Name = rr.ReadString(), Desc = rr.ReadString(),
                };
                g.RewardList.AddRange(rr.ReadArray(rrr => new CustomActivityModel.TiredChargeRewardItem
                {
                    FormType = rrr.ReadU8(), Status = rrr.ReadU8(), Reward = rrr.ReadString(),
                }));
                return g;
            });
            var info = new CustomActivityModel.TiredChargePoliteInfo { BaseType = baseType, SubType = subType, RechargeNum = rechargeNum, IsRecharge = isRecharge };
            info.List.AddRange(list);
            CustomActivityModel.Instance.SetTiredChargePoliteInfo(info);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, baseType, subType);
            GameLog.Info("CustomActivity", "33259 充值有礼奖励状态 base={0} sub={1} rechargeNum={2} isRecharge={3} gradeN={4}",
                baseType, subType, rechargeNum, isRecharge, list.Count);
        }

        // ---------------------------------------------------------------------------------------
        // OVER_VIEW=126(33264 + RequireOverViewRew 遍历补拉)
        // ---------------------------------------------------------------------------------------

        public void RequestOverviewReward(int baseType, int subType) => SendFmt(Proto.CUSTOM_ACT_OVERVIEW_REWARD, "hh", baseType, subType);

        /// <summary>活动奖励配置(pt_332.erl write(33264):1582-1599,item_to_bin_48={Grade:16,FormType:8,
        /// Reward:str}——**订正**见 Model 注释,Reward 是字符串非对象三元组)。</summary>
        private void On33264(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            List<CustomActivityModel.OverViewRewardItem> list = r.ReadArray(rr => new CustomActivityModel.OverViewRewardItem
            {
                Grade = rr.ReadU16(), FormType = rr.ReadU8(), Reward = rr.ReadString(),
            });
            var info = new CustomActivityModel.OverViewRewardInfo { BaseType = baseType, SubType = subType };
            info.RewardList.AddRange(list);
            CustomActivityModel.Instance.SetOverViewRewardInfo(info);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, baseType, subType);
            GameLog.Info("CustomActivity", "33264 活动奖励配置 base={0} sub={1} rewardN={2}", baseType, subType, list.Count);
        }

        /// <summary>镜像老端 RequireOverViewRew(CustomActivityModel.ts:401-416):扫描活动列表里 base_type==
        /// OVER_VIEW 的条目,正则解析其 condition 里的 {base_type,show_id} 对,按 show_id 在当前列表中找到真实
        /// (base_type,sub_type)后逐个补拉 33264。挂在 EVT_CUSTOMACT_LIST_UPDATE 上(P1 On33101 每次收到 33101
        /// 都会 Emit,与老端"每次 SaveActInfo 都跑一遍"语义一致)。</summary>
        private void OnListUpdateForOverView()
        {
            var overViewEntries = new List<CustomActivityModel.ActEntry>();
            foreach (KeyValuePair<long, CustomActivityModel.ActEntry> kv in CustomActivityModel.Instance.ActList)
            {
                if (kv.Value.BaseType == ACT_ID_OVER_VIEW) overViewEntries.Add(kv.Value);
            }
            if (overViewEntries.Count == 0) return;

            int requested = 0;
            foreach (CustomActivityModel.ActEntry ov in overViewEntries)
            {
                foreach (Match m in OverViewPairRegex.Matches(ov.Condition ?? string.Empty))
                {
                    if (!int.TryParse(m.Groups[1].Value, out int pairBaseType)) continue;
                    if (!int.TryParse(m.Groups[2].Value, out int pairShowId)) continue;

                    // 对标老端 GetActInfoByShowID:在 base_type==pairBaseType 的条目里找 ShowId==pairShowId 的那条,
                    // 取其真正的 sub_type(condition 里只给了 show_id,不是 sub_type 本身)。
                    foreach (KeyValuePair<long, CustomActivityModel.ActEntry> kv2 in CustomActivityModel.Instance.ActList)
                    {
                        CustomActivityModel.ActEntry e2 = kv2.Value;
                        if (e2.BaseType == pairBaseType && e2.ShowId == pairShowId)
                        {
                            RequestOverviewReward(e2.BaseType, e2.SubType);
                            requested++;
                            break;
                        }
                    }
                }
            }
            GameLog.Info("CustomActivity", "OVER_VIEW 遍历补拉(镜像 RequireOverViewRew) overViewEntries={0} requested33264={1}",
                overViewEntries.Count, requested);
        }

        // ---------------------------------------------------------------------------------------
        // RARE_SURFACE=128(33265,被 wxOneMoney 复用)
        // ---------------------------------------------------------------------------------------

        public void RequestRareSurfaceClaim(int type, int subtype, int gradeId) => SendFmt(Proto.CUSTOM_ACT_RARESURFACE_CLAIM, "hhh", type, subtype, gradeId);

        /// <summary>绝版外显/一元购通用分档领取(pt_332.erl write(33265):1601-1613)。**Errcode 在末尾**。
        /// **三镜头订正,完全不看 errcode**(ts:2737-2740):老端 On33265 全函数体不检查 errcode,恒定
        /// Fire(UPDATE_VIEW,type,subtype,grade)——不弹错,也不按成败分支决定是否落地。已去掉原先误加的
        /// "仅 errcode==1 才落 Model,否则 ShowError" 分支,改为无条件落 Model + Emit,GameLog 留痕真实
        /// errcode 供排查。</summary>
        private void On33265(NetReader r)
        {
            int type = r.ReadU16();
            int subtype = r.ReadU16();
            int grade = r.ReadU16();
            int errcode = r.ReadI32();
            CustomActivityModel.Instance.SetRareSurfaceClaimResult(new CustomActivityModel.RareSurfaceClaimResult
            {
                Type = type, Subtype = subtype, Grade = grade, Errcode = errcode,
            });
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, type, subtype, errcode);
            GameLog.Info("CustomActivity", "33265 绝版外显/一元购领取 type={0} subtype={1} grade={2} errcode={3}", type, subtype, grade, errcode);
        }

        /// <summary>通用奖励列表推送(pt_332.erl write(33257):1465-1482,item_to_bin_44={Style:16,GoodsId:32,
        /// Num:32})。**recv-only**:C2S read(33257,_)->{ok,[]} 但 pp handle 无 handle(33257) 子句(C2S 死),
        /// S2C 被 ≥3 个不同活动模块复用做通用推送(r17_server_customactivity.md 已列出3个推送点)——不提供
        /// Request 方法。</summary>
        private void On33257(NetReader r)
        {
            int type = r.ReadU16();
            int subtype = r.ReadU16();
            List<CustomActivityModel.RewardListPushItem> list = r.ReadArray(rr => new CustomActivityModel.RewardListPushItem
            {
                Style = rr.ReadU16(), GoodsId = rr.ReadI32(), Num = rr.ReadI32(),
            });
            var push = new CustomActivityModel.RewardListPush { Type = type, Subtype = subtype };
            push.RewardList.AddRange(list);
            CustomActivityModel.Instance.SetRewardListPush(push);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, type, subtype);
            GameLog.Info("CustomActivity", "33257 通用奖励列表推送(recv-only) type={0} subtype={1} rewardN={2}", type, subtype, list.Count);
        }

        // ---------------------------------------------------------------------------------------
        // 活动通用获奖记录(33197)
        // ---------------------------------------------------------------------------------------

        public void RequestWinLog(int baseType, int subType) => SendFmt(Proto.CUSTOM_ACT_WIN_LOG, "hh", baseType, subType);

        /// <summary>pt_331.erl write(33197):2098-2124,item_to_bin_72/73 同构={RoleId:64,Name:str,
        /// RewardList=ObjectList}(三层嵌套:顶层→LogList/SelfList数组→各自的 RewardList ObjectList)。</summary>
        private void On33197(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            CustomActivityModel.WinLogEntry ReadEntry(NetReader rr)
            {
                var e = new CustomActivityModel.WinLogEntry { RoleId = rr.ReadU64(), Name = rr.ReadString() };
                e.RewardList.AddRange(CustomActivityModel.ReadRewardObjList(rr));
                return e;
            }
            List<CustomActivityModel.WinLogEntry> logList = r.ReadArray(ReadEntry);
            List<CustomActivityModel.WinLogEntry> selfList = r.ReadArray(ReadEntry);
            var data = new CustomActivityModel.WinLogData { BaseType = baseType, SubType = subType };
            data.LogList.AddRange(logList);
            data.SelfList.AddRange(selfList);
            CustomActivityModel.Instance.SetWinLog(data);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, baseType, subType);
            GameLog.Info("CustomActivity", "33197 活动通用获奖记录 base={0} sub={1} logN={2} selfN={3}", baseType, subType, logList.Count, selfList.Count);
        }

        // ---------------------------------------------------------------------------------------
        // 嗨点 HOTPOINT(33140)——防御 recv,不发送(见 CustomActivityModel.Biz.cs §B21)
        // ---------------------------------------------------------------------------------------

        /// <summary>pt_331.erl write(33140):1059-1074,item_to_bin_33(14字段,pt_331.erl:2643-2682)。安全解析
        /// 但不落地存储(死路径,pp_custom_act.erl:632-639 handler 空转恒 {ok,Player}、33101 列表层已过滤
        /// HI_POINT,本端在真实环境几乎不可能收到本包)。</summary>
        private void On33140(NetReader r)
        {
            int sumPoints = r.ReadI32();
            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                r.ReadU32(); r.ReadU32();       // ModId, SubId
                r.ReadString(); r.ReadString(); // ConditionType, Name
                r.ReadU16(); r.ReadU16();       // OrderId, JumpId
                r.ReadU32();                    // SecValue
                r.ReadString();                 // IconType
                r.ReadU64();                    // ProValue
                r.ReadU16();                    // IsPro
                r.ReadU32();                    // CondiVal
                r.ReadU32();                    // RewardPoint
                r.ReadString();                 // Dec
                r.ReadU16();                    // IsCom
            }
            GameLog.Info("CustomActivity", "33140 嗨点信息(防御recv,死路径不落地) sumPoints={0} modN={1}", sumPoints, count);
        }

        // ---------------------------------------------------------------------------------------
        // 完美情缘 actMarriage=25(33115)
        // ---------------------------------------------------------------------------------------

        /// <summary>C2S 已由 Controller.Core.cs RequestActDetail 对 base_type==actMarriage 固定以 Opr=1 追发;
        /// 本方法供 Opr=2(领取,IfGetReward 字段暗示存在领取动作)等其它 Opr 值调用。</summary>
        public void RequestMarriageActInfo(int subType, int opr) => SendFmt(Proto.CUSTOM_ACT_MARRIAGE_ACT_INFO, "hc", subType, opr);

        /// <summary>完美恋人(pt_331.erl write(33115):599-620,item_to_bin_13={WeddingTypeId:8,
        /// WeddingTimes:16})。命名 CustomActMarriage* 与 Marriage 模块(172xx)无关,见 Model 注释。</summary>
        private void On33115(NetReader r)
        {
            int code = r.ReadI32();
            int subType = r.ReadU16();
            int opr = r.ReadU8();
            int ifGetReward = r.ReadU8();
            List<CustomActivityModel.CustomActMarriageWeddingType> weddingTypeList = r.ReadArray(rr => new CustomActivityModel.CustomActMarriageWeddingType
            {
                WeddingTypeId = rr.ReadU8(), WeddingTimes = rr.ReadU16(),
            });
            if (code == 1)
            {
                var info = new CustomActivityModel.CustomActMarriageInfo { SubType = subType, Opr = opr, IfGetReward = ifGetReward };
                info.WeddingTypeList.AddRange(weddingTypeList);
                CustomActivityModel.Instance.SetCustomActMarriageInfo(info);
            }
            else ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, ACT_ID_actMarriage, subType, code);
            GameLog.Info("CustomActivity", "33115 完美情缘 code={0} subType={1} opr={2} ifGetReward={3} weddingTypeN={4}",
                code, subType, opr, ifGetReward, weddingTypeList.Count);
        }

        // ---------------------------------------------------------------------------------------
        // 封测充值返还 BETA_ACT=77(33216;C2S 已由主文件 RequestActDetail 追发裸包)
        // ---------------------------------------------------------------------------------------

        /// <summary>pt_332.erl write(33216):520-530,无 Code,无 BaseType/SubType。</summary>
        private void On33216(NetReader r)
        {
            int gold = r.ReadI32();
            int returnGold = r.ReadI32();
            int loginDays = r.ReadI32();
            CustomActivityModel.Instance.SetBetaRechargeReturn(new CustomActivityModel.BetaRechargeReturnInfo
            {
                Gold = gold, ReturnGold = returnGold, LoginDays = loginDays,
            });
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, ACT_ID_BETA_ACT, 0);
            GameLog.Info("CustomActivity", "33216 封测充值返还 gold={0} returnGold={1} loginDays={2}", gold, returnGold, loginDays);
        }

        // ---------------------------------------------------------------------------------------
        // 充值统计 15955-15960(pt_159.erl;15955/15956/15957 的 C2S 已由主文件 RequestActDetail 追发)
        // ---------------------------------------------------------------------------------------

        public void RequestPoliteRecharge(int type, int subType) => SendFmt(Proto.RECHARGE_STAT_POLITE_RECHARGE, "hh", type, subType);
        public void RequestTodayRecharge() => SendFmt(Proto.RECHARGE_STAT_TODAY);
        public void RequestRechargeHistory(int day) => SendFmt(Proto.RECHARGE_STAT_HISTORY, "h", day);

        /// <summary>每日累充信息(pt_159.erl write(15955):192-209,item_to_bin_6(pt_159.erl:371-395)=
        /// {Id:16,State:8,Val:32,Max:32,RewardList=ObjectList,Condition:str,Desc:str})。C2S 已由主文件
        /// RequestActDetail 对 base_type==DAILY_RECHARGE(6) 自动追发"h"。**排序镜像**(ts:1413-1420,自动循环
        /// 轮17三镜头验收补):落 Model 前按 Id 升序排序(老端 `table.sort(reward_infos,(a,b)=>a.id&lt;b.id)`;
        /// 老端另有 `v.grade=v.id` 冗余赋值,本端 Model 无对应 Grade 字段,不镜像这一步)。</summary>
        private void On15955(NetReader r)
        {
            int subType = r.ReadU16();
            int num = r.ReadI32();
            List<CustomActivityModel.DailyAccumInfoItem> list = r.ReadArray(rr =>
            {
                var item = new CustomActivityModel.DailyAccumInfoItem { Id = rr.ReadU16(), State = rr.ReadU8(), Val = rr.ReadI32(), Max = rr.ReadI32() };
                item.RewardList.AddRange(CustomActivityModel.ReadRewardObjList(rr));
                item.Condition = rr.ReadString();
                item.Desc = rr.ReadString();
                return item;
            });
            list.Sort((a, b) => a.Id.CompareTo(b.Id));
            var info = new CustomActivityModel.DailyAccumInfo { SubType = subType, Num = num };
            info.RewardInfos.AddRange(list);
            CustomActivityModel.Instance.SetDailyAccumInfo(info);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, ACT_ID_DAILY_RECHARGE, subType);
            GameLog.Info("CustomActivity", "15955 每日累充信息 subType={0} num={1} rewardInfoN={2}", subType, num, list.Count);
        }

        /// <summary>每日累充奖励列表(pt_159.erl write(15956):211-226,item_to_bin_7(pt_159.erl:396-422)=
        /// item_to_bin_6 + GoldNum:64,位置在 Max 之后、RewardList 之前)。C2S 已由主文件追发"h"。**排序镜像**
        /// (ts:1440-1447,自动循环 轮17三镜头验收补):落 Model 前按 Id 升序排序(同 On15955,老端另有
        /// `v.grade=v.id` 冗余赋值不镜像)。</summary>
        private void On15956(NetReader r)
        {
            int subType = r.ReadU16();
            List<CustomActivityModel.DailyAccumRewardItem> list = r.ReadArray(rr =>
            {
                var item = new CustomActivityModel.DailyAccumRewardItem
                {
                    Id = rr.ReadU16(), State = rr.ReadU8(), Val = rr.ReadI32(), Max = rr.ReadI32(), GoldNum = rr.ReadU64(),
                };
                item.RewardList.AddRange(CustomActivityModel.ReadRewardObjList(rr));
                item.Condition = rr.ReadString();
                item.Desc = rr.ReadString();
                return item;
            });
            list.Sort((a, b) => a.Id.CompareTo(b.Id));
            var reward = new CustomActivityModel.DailyAccumReward { SubType = subType };
            reward.RewardList.AddRange(list);
            CustomActivityModel.Instance.SetDailyAccumReward(reward);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, ACT_ID_DAILY_RECHARGE, subType);
            GameLog.Info("CustomActivity", "15956 每日累充奖励列表 subType={0} rewardN={1}", subType, list.Count);
        }

        /// <summary>某活动类型充值总额(pt_159.erl write(15957):228-238)。C2S 已由主文件对 base_type==
        /// ACC_RECHARGE(7) 自动追发"hhi"。</summary>
        private void On15957(NetReader r)
        {
            int type = r.ReadU16();
            int subType = r.ReadU16();
            int totalGold = r.ReadI32();
            CustomActivityModel.Instance.SetActRecharge(new CustomActivityModel.ActRechargeInfo { Type = type, SubType = subType, TotalGold = totalGold });
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, type, subType);
            GameLog.Info("CustomActivity", "15957 某活动类型充值总额 type={0} subType={1} totalGold={2}", type, subType, totalGold);
        }

        /// <summary>节日活动·充值有礼充值金额(pt_159.erl write(15958):240-250)。</summary>
        private void On15958(NetReader r)
        {
            int type = r.ReadU16();
            int subType = r.ReadU16();
            int totalGold = r.ReadI32();
            CustomActivityModel.Instance.SetPoliteRecharge(new CustomActivityModel.ActRechargeInfo { Type = type, SubType = subType, TotalGold = totalGold });
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, type, subType);
            GameLog.Info("CustomActivity", "15958 充值有礼充值金额 type={0} subType={1} totalGold={2}", type, subType, totalGold);
        }

        /// <summary>当天充值金额(pt_159.erl write(15959):252-258)。无 Type/SubType,老端收到后追发
        /// RequireActInfo(CON_RECHARGE,1)(ts:1319,CustomActivityController.ts:On15959)——**三镜头订正**:
        /// CON_RECHARGE=109(ConfigCustomActivity.json ACT_ID.CON_RECHARGE),该 base_type 在 RequireActInfo
        /// 分发表(CustomActivityModel.ts:965-1122)里没有专属分支,落分发表末尾兜底 else(ts:1119-1121)
        /// Fire(33104,109,1)——就是本轮已实现的 331 家族兜底号,并非"非331协议家族"(原注释判断有误,已订正)。
        /// 镜像:直接调用本类既有 RequestActDetail(109,1),该方法自带"活动列表无此条目则不发送"guard,
        /// 与老端 RequireActInfo 开头的 `if (!act_info) return` 语义一致。</summary>
        private void On15959(NetReader r)
        {
            int totalGold = r.ReadI32();
            CustomActivityModel.Instance.SetTodayRechargeGold(totalGold);
            RequestActDetail(ACT_ID_CON_RECHARGE, 1); // 镜像 ts:1319 RequireActInfo(CON_RECHARGE,1)
            GameLog.Info("CustomActivity", "15959 当天充值金额 totalGold={0}", totalGold);
        }

        /// <summary>几天前的充值金额列表(pt_159.erl write(15960):260-273,item_to_bin_8={Time:32,
        /// TotalGold:32})。</summary>
        private void On15960(NetReader r)
        {
            List<CustomActivityModel.RechargeHistoryItem> list = r.ReadArray(rr => new CustomActivityModel.RechargeHistoryItem
            {
                Time = rr.ReadI32(), TotalGold = rr.ReadI32(),
            });
            CustomActivityModel.Instance.SetRechargeHistory(list);
            GameLog.Info("CustomActivity", "15960 几天前充值金额列表 count={0}", list.Count);
        }
    }
}
