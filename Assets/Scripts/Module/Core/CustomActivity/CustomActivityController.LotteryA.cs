using System.Collections.Generic;
using System.Text;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.CustomActivity
{
    /// <summary>
    /// P2 抽奖A(自动循环 轮17,spec §3)控制器段:OPTIONALLOTTO=76(33128/29/33/34/35/39)/
    /// WISH_POOL=79(33141/42/44)/DESTINY_TURNTABLE=99(33238/39/40)/TURNTABLE_100=100(33241/42)。
    ///
    /// 纪律执行摘要(spec §8):①wire 全部逐字段回 pt_331.erl/pt_332.erl 原文核对(非仅套用侦察表,33128/
    /// 33242 两处订正见 CustomActivityModel.LotteryA.cs 对应类注释);②Errcode 位置三态逐号照抄——33129/
    /// 33135 末尾、33133 第3字段、33240 开头、33128/33139/33141/33238/33239/33241/33242 全款无 Errcode;
    /// ③33239/33242 是**recv-only**(pt_332.erl:141-148 read(33239,_)/pp_custom_act_list.erl 无
    /// handle(33242)),本文件不提供对应 Request 方法;④先落 Model 后 Emit,失败走 ShowError(Core.cs 定义,
    /// partial 内私有静态方法跨文件可见)降级,不套模板;⑤33129 是变长发送特例(老端 WriteBegin/WriteFMT
    /// 手写,ts:378-392),用动态 fmt 字符串镜像。
    ///
    /// 【wire 争议留档,待主控裁决】33142(WISH_POOL 取奖池奖励)C2S 发送参数个数:服务端 read(33142)
    /// (pt_331.erl:140-146)要求 5 参 BaseType,SubType,Grade:16,Times:16,AutoBuy:8(fmt "hhhhc"),
    /// proto331.d.ts:1276-1294 声明的 sendFmt 同为 "hhhhc"——但老端运行时 SCMD_REQUEST 分发器
    /// (CustomActivityController.ts)对 33142 命中的是 298 行"hhh"分支(仅 base_type,sub_type,grade 3参),
    /// 397-399 行真正的"hhhhc"分支因排在 if-else 链更后而**永远不可达**(纯死代码)。本代理在
    /// h5/src 全仓库搜索 `Fire(custom_even.SCMD_REQUEST, 33142` 及等价写法,**零命中**——唯一相关引用是
    /// CustomActivityModel.ts:4191 一行被注释掉的 `// this.Fire(custom_even.SCMD_REQUEST, 33141, ...)`
    /// (且是 33141 奖池查询,不是 33142 取奖),证明 WISH_POOL 取奖功能在老端从未被任何 UI 实际触发过,
    /// 无法拿到"真实调用点实参"佐证。若真有人触发,老端"hhh"截断分支只会发 6 字节,服务端 read 会因
    /// 数据不足在 <<AutoBuy:8,_/binary>> 匹配处抛 badmatch——这本身就是老端的死代码/协议不匹配 bug,不是
    /// 一个可镜像的"正确行为"。本端按服务端权威 wire(5 参 "hhhhc")实现 RequestWishPoolClaim,不镜像老端
    /// 会导致协议崩溃的截断行为。此结论供主控三镜头验收复核。
    /// </summary>
    public sealed partial class CustomActivityController
    {
        /// <summary>P1 预建空壳,由主文件 Register() 调用。</summary>
        private void RegisterLotteryA()
        {
            RegisterProtocal(Proto.CUSTOM_ACT_LOTTO_PANEL, On33128);
            RegisterProtocal(Proto.CUSTOM_ACT_LOTTO_LOCK, On33129);
            RegisterProtocal(Proto.CUSTOM_ACT_LOTTO_RESET, On33133);
            RegisterProtocal(Proto.CUSTOM_ACT_LOTTO_DRAW, On33134);
            RegisterProtocal(Proto.CUSTOM_ACT_LOTTO_STAGE, On33135);
            RegisterProtocal(Proto.CUSTOM_ACT_LOTTO_POOL, On33139);
            RegisterProtocal(Proto.CUSTOM_ACT_WISHPOOL_POOL, On33141);
            RegisterProtocal(Proto.CUSTOM_ACT_WISHPOOL_CLAIM, On33142);
            RegisterProtocal(Proto.CUSTOM_ACT_WISHPOOL_RESET, On33144);
            RegisterProtocal(Proto.CUSTOM_ACT_DESTINY_PANEL, On33238);
            RegisterProtocal(Proto.CUSTOM_ACT_DESTINY_PUSH, On33239);
            RegisterProtocal(Proto.CUSTOM_ACT_DESTINY_DRAW, On33240);
            RegisterProtocal(Proto.CUSTOM_ACT_TURN100_PANEL, On33241);
            RegisterProtocal(Proto.CUSTOM_ACT_TURN100_PUSH, On33242);
        }

        // ---------------------------------------------------------------------------------------
        // OPTIONALLOTTO(76):33128/33129/33133/33134/33135/33139
        // ---------------------------------------------------------------------------------------

        /// <summary>33128 界面(pt_331.erl:77-80 read "hh")。RequestActDetail(Core.cs)对
        /// ACT_ID_OPTIONALLOTTO 已经会自动发本号,此处另开公开方法供直接调用/测试。</summary>
        public void RequestLottoPanel(int baseType, int subType) => SendFmt(Proto.CUSTOM_ACT_LOTTO_PANEL, "hh", baseType, subType);

        /// <summary>33129 锁定奖池(变长发送特例,对标老端 WriteBegin/WriteFMT 手写数组,ts:378-392;
        /// 服务端 read 带前导数组 pt_331.erl:81-91,元素 Rare:8,Grade:16)。</summary>
        public void RequestLottoLock(int baseType, int subType, IReadOnlyList<(int rare, int grade)> pool)
        {
            var fmt = new StringBuilder("hhh");
            var args = new List<object> { baseType, subType, pool?.Count ?? 0 };
            if (pool != null)
            {
                for (int i = 0; i < pool.Count; i++)
                {
                    fmt.Append("ch");
                    args.Add(pool[i].rare);
                    args.Add(pool[i].grade);
                }
            }
            SendFmt(Proto.CUSTOM_ACT_LOTTO_LOCK, fmt.ToString(), args.ToArray());
        }

        public void RequestLottoReset(int baseType, int subType) => SendFmt(Proto.CUSTOM_ACT_LOTTO_RESET, "hh", baseType, subType);

        /// <summary>33134 抽奖(pt_331.erl:108-112 read "hhc" BaseType,SubType,AutoBuy)。</summary>
        public void RequestLottoDraw(int baseType, int subType, int autoBuy) =>
            SendFmt(Proto.CUSTOM_ACT_LOTTO_DRAW, "hhc", baseType, subType, autoBuy);

        public void RequestLottoStage(int baseType, int subType, int grade) =>
            SendFmt(Proto.CUSTOM_ACT_LOTTO_STAGE, "hhh", baseType, subType, grade);

        public void RequestLottoPool(int baseType, int subType) => SendFmt(Proto.CUSTOM_ACT_LOTTO_POOL, "hh", baseType, subType);

        /// <summary>33128 界面全量(pt_331.erl:788-827)。无 ErrorCode 前导。</summary>
        private void On33128(NetReader r)
        {
            var d = new CustomActivityModel.LottoPanelData
            {
                BaseType = r.ReadU16(), SubType = r.ReadU16(), DrawTimes = r.ReadU16(), Reset = r.ReadU16(),
            };
            d.Pool.AddRange(r.ReadArray(CustomActivityModel.ReadLottoPoolEntry));
            d.Stage.AddRange(r.ReadArray(CustomActivityModel.ReadLottoStageEntry));
            d.RewardList.AddRange(r.ReadArray(CustomActivityModel.ReadLottoRewardEntry));
            CustomActivityModel.Instance.SetLottoPanel(d);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, d.BaseType, d.SubType);
            GameLog.Info("CustomActivity", "33128 自选奖励抽奖界面 base={0} sub={1} drawTimes={2} poolN={3} stageN={4} rewardN={5}",
                d.BaseType, d.SubType, d.DrawTimes, d.Pool.Count, d.Stage.Count, d.RewardList.Count);
        }

        /// <summary>33129 锁定奖池回执(pt_331.erl:829-848)。ErrorCode **在末尾**,Pool 元素回包 3 字段
        /// (Rare,Grade,Status,item_to_bin_25),与 C2S 请求侧 2 字段(Rare,Grade)不同结构,严禁混用同一读法。
        /// 成功时对标老端 On33129(ts:1930-1934)回写缓存面板的 Pool 字段。</summary>
        private void On33129(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            List<CustomActivityModel.LottoPoolEntry> pool = r.ReadArray(CustomActivityModel.ReadLottoPoolEntry);
            int errorCode = r.ReadI32();
            var result = new CustomActivityModel.LottoLockResult { BaseType = baseType, SubType = subType, ErrorCode = errorCode };
            result.Pool.AddRange(pool);
            if (errorCode == 1)
            {
                CustomActivityModel.Instance.SetLottoLockResult(result);
                CustomActivityModel.LottoPanelData panel = CustomActivityModel.Instance.GetLottoPanel(baseType, subType);
                if (panel != null)
                {
                    panel.Pool.Clear();
                    panel.Pool.AddRange(pool);
                }
            }
            else
            {
                ShowError(errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, baseType, subType, errorCode);
            GameLog.Info("CustomActivity", "33129 锁定奖池 base={0} sub={1} errorCode={2} poolN={3}", baseType, subType, errorCode, pool.Count);
        }

        /// <summary>33133 重置回执(pt_331.erl:927-959)。ErrorCode 是**第3字段**。成功时对标老端 On33133
        /// (ts:1944-1951)回写缓存面板的 DrawTimes/Reset/Pool/Stage。</summary>
        private void On33133(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            int errorCode = r.ReadI32();
            int drawTimes = r.ReadU16();
            int reset = r.ReadU16();
            List<CustomActivityModel.LottoPoolEntry> pool = r.ReadArray(CustomActivityModel.ReadLottoPoolEntry);
            List<CustomActivityModel.LottoStageEntry> stage = r.ReadArray(CustomActivityModel.ReadLottoStageEntry);
            var result = new CustomActivityModel.LottoResetResult
            {
                BaseType = baseType, SubType = subType, ErrorCode = errorCode, DrawTimes = drawTimes, Reset = reset,
            };
            result.Pool.AddRange(pool);
            result.Stage.AddRange(stage);
            if (errorCode == 1)
            {
                CustomActivityModel.Instance.SetLottoResetResult(result);
                CustomActivityModel.LottoPanelData panel = CustomActivityModel.Instance.GetLottoPanel(baseType, subType);
                if (panel != null)
                {
                    panel.DrawTimes = drawTimes;
                    panel.Reset = reset;
                    panel.Pool.Clear();
                    panel.Pool.AddRange(pool);
                    panel.Stage.Clear();
                    panel.Stage.AddRange(stage);
                }
            }
            else
            {
                ShowError(errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, baseType, subType, errorCode);
            GameLog.Info("CustomActivity", "33133 重置 base={0} sub={1} errorCode={2} drawTimes={3} reset={4}",
                baseType, subType, errorCode, drawTimes, reset);
        }

        /// <summary>33134 抽奖回执(pt_331.erl:961-981)。ErrorCode 是第4字段;Reward 是
        /// pt:write_object_list 直接展开(非嵌套结构体,与 33142 的 {Reward,IsRare} 包装不同)。</summary>
        private void On33134(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            int drawTimes = r.ReadU16();
            int errorCode = r.ReadI32();
            int grade = r.ReadU16();
            int rare = r.ReadU8();
            List<CustomActivityModel.LottoObjReward> reward = r.ReadArray(CustomActivityModel.ReadLottoObjReward);
            if (errorCode == 1)
            {
                var result = new CustomActivityModel.LottoDrawResult
                {
                    BaseType = baseType, SubType = subType, DrawTimes = drawTimes, ErrorCode = errorCode, Grade = grade, Rare = rare,
                };
                result.Reward.AddRange(reward);
                CustomActivityModel.Instance.SetLottoDrawResult(result);
            }
            else
            {
                ShowError(errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, baseType, subType, errorCode);
            GameLog.Info("CustomActivity", "33134 抽奖 base={0} sub={1} errorCode={2} grade={3} rare={4} rewardN={5}",
                baseType, subType, errorCode, grade, rare, reward.Count);
        }

        /// <summary>33135 阶段奖励回执(pt_331.erl:983-995)。ErrorCode **在末尾**。</summary>
        private void On33135(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            int grade = r.ReadU16();
            int errorCode = r.ReadI32();
            if (errorCode == 1)
            {
                CustomActivityModel.Instance.SetLottoStageResult(new CustomActivityModel.LottoStageResult
                {
                    BaseType = baseType, SubType = subType, Grade = grade, ErrorCode = errorCode,
                });
            }
            else
            {
                ShowError(errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, baseType, subType, errorCode);
            GameLog.Info("CustomActivity", "33135 阶段奖励 base={0} sub={1} grade={2} errorCode={3}", baseType, subType, grade, errorCode);
        }

        /// <summary>33139 奖池(pt_331.erl:1040-1057)。无 ErrorCode。Pool 元素**仅2字段**(Rare,Grade,
        /// item_to_bin_32),与 33128/33129回执/33133 的3字段 Pool 不同结构。</summary>
        private void On33139(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            List<CustomActivityModel.LottoRareGradeEntry> pool = r.ReadArray(CustomActivityModel.ReadLottoRareGradeEntry);
            CustomActivityModel.Instance.SetLottoRandomPool(baseType, subType, pool);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, baseType, subType);
            GameLog.Info("CustomActivity", "33139 奖池 base={0} sub={1} poolN={2}", baseType, subType, pool.Count);
        }

        // ---------------------------------------------------------------------------------------
        // WISH_POOL(79):33141/33142/33144
        // ---------------------------------------------------------------------------------------

        public void RequestWishPoolPanel(int baseType, int subType) => SendFmt(Proto.CUSTOM_ACT_WISHPOOL_POOL, "hh", baseType, subType);

        /// <summary>33142 取奖池奖励(**wire 争议已按服务端权威裁定**,见本文件头注释)。fmt "hhhhc":
        /// BaseType,SubType,Grade,Times,AutoBuy(pt_331.erl:140-146 / proto331.d.ts:1276-1294)。</summary>
        public void RequestWishPoolClaim(int baseType, int subType, int grade, int times, int autoBuy) =>
            SendFmt(Proto.CUSTOM_ACT_WISHPOOL_CLAIM, "hhhhc", baseType, subType, grade, times, autoBuy);

        public void RequestWishPoolReset(int baseType, int subType, int grade) =>
            SendFmt(Proto.CUSTOM_ACT_WISHPOOL_RESET, "hhh", baseType, subType, grade);

        /// <summary>33141 奖池(pt_331.erl:1076-1093)。无 ErrorCode。**排序镜像**(ts:2016-2018,自动循环
        /// 轮17三镜头验收补):落 Model 前按 Grade 升序排序(老端 `table.sort(list,(a,b)=>a.grade&lt;b.grade)`)。</summary>
        private void On33141(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            List<CustomActivityModel.WishRarePoolEntry> pool = r.ReadArray(CustomActivityModel.ReadWishRarePoolEntry);
            pool.Sort((a, b) => a.Grade.CompareTo(b.Grade));
            CustomActivityModel.Instance.SetWishPool(baseType, subType, pool);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, baseType, subType);
            GameLog.Info("CustomActivity", "33141 许愿池奖池 base={0} sub={1} poolN={2}", baseType, subType, pool.Count);
        }

        /// <summary>33142 取奖池奖励回执(pt_331.erl:1095-1122)。ErrorCode 是第4字段;RewardList 元素
        /// (item_to_bin_35)是 {Reward:嵌套ObjectList, IsRare:8} 包装结构,与 33134 的直接展开不同。</summary>
        private void On33142(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            int grade = r.ReadU16();
            int errorCode = r.ReadI32();
            List<CustomActivityModel.WishClaimRewardEntry> rewardList = r.ReadArray(CustomActivityModel.ReadWishClaimRewardEntry);
            int luckyValue = r.ReadU16();
            int freeTimes = r.ReadU16();
            int state = r.ReadU8();
            if (errorCode == 1)
            {
                var result = new CustomActivityModel.WishClaimResult
                {
                    BaseType = baseType, SubType = subType, Grade = grade, ErrorCode = errorCode,
                    LuckyValue = luckyValue, FreeTimes = freeTimes, State = state,
                };
                result.RewardList.AddRange(rewardList);
                CustomActivityModel.Instance.SetWishClaimResult(result);
            }
            else
            {
                ShowError(errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, baseType, subType, errorCode);
            GameLog.Info("CustomActivity", "33142 取奖池奖励 base={0} sub={1} grade={2} errorCode={3} rewardN={4} luckyValue={5}",
                baseType, subType, grade, errorCode, rewardList.Count, luckyValue);
        }

        /// <summary>33144 重置回执(pt_331.erl:1134-1154)。字段名原文即"Code"(非"ErrorCode"),老端
        /// On33144 读 scmd.code(ts:2038)。</summary>
        private void On33144(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            int grade = r.ReadU16();
            int code = r.ReadI32();
            int luckyValue = r.ReadU16();
            int freeTimes = r.ReadU16();
            int state = r.ReadU8();
            int maxLuckyValue = r.ReadU16();
            if (code == 1)
            {
                CustomActivityModel.Instance.SetWishResetResult(new CustomActivityModel.WishResetResult
                {
                    BaseType = baseType, SubType = subType, Grade = grade, Code = code,
                    LuckyValue = luckyValue, FreeTimes = freeTimes, State = state, MaxLuckyValue = maxLuckyValue,
                });
            }
            else
            {
                ShowError(code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, baseType, subType, code);
            GameLog.Info("CustomActivity", "33144 许愿池重置 base={0} sub={1} grade={2} code={3}", baseType, subType, grade, code);
        }

        // ---------------------------------------------------------------------------------------
        // DESTINY_TURNTABLE(99):33238/33239(recv-only)/33240
        // ---------------------------------------------------------------------------------------

        public void RequestDestinyPanel(int baseType, int subType) => SendFmt(Proto.CUSTOM_ACT_DESTINY_PANEL, "hh", baseType, subType);

        /// <summary>33240 开抽(pt_332.erl:149-152 read "hh" BaseType,SubType——服务端按累计 Point 自动结算,
        /// 客户端无需额外传 Grade/Times)。</summary>
        public void RequestDestinyDraw(int baseType, int subType) => SendFmt(Proto.CUSTOM_ACT_DESTINY_DRAW, "hh", baseType, subType);

        // 33239 是 recv-only(pt_332.erl:147-148 read(33239,_)->{ok,[]},pp_custom_act_list.erl 无
        // handle(33239)——C2S 死,S2C 抽奖后主动推送积分),不提供 Request 方法。

        /// <summary>33238 面板全量(pt_332.erl:988-1024)。无 ErrorCode。</summary>
        private void On33238(NetReader r)
        {
            var d = new CustomActivityModel.DestinyPanelData
            {
                BaseType = r.ReadU16(), SubType = r.ReadU16(), Turn = r.ReadU16(), Point = r.ReadU32(), NeedPoint = r.ReadU32(),
                MaxTurn = r.ReadU16(),
            };
            d.RewardList.AddRange(r.ReadArray(CustomActivityModel.ReadDestinyRewardEntry));
            d.DoublePoint.AddRange(r.ReadArray(CustomActivityModel.ReadDestinyDoublePointEntry));
            d.Label = r.ReadU8();
            CustomActivityModel.Instance.SetDestinyPanel(d);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, d.BaseType, d.SubType);
            GameLog.Info("CustomActivity", "33238 天命转盘界面 base={0} sub={1} turn={2} point={3} needPoint={4} rewardN={5} doublePointN={6}",
                d.BaseType, d.SubType, d.Turn, d.Point, d.NeedPoint, d.RewardList.Count, d.DoublePoint.Count);
        }

        /// <summary>33239 **recv-only** 积分推送(pt_332.erl:1026-1040)。无 ErrorCode 前导。对标老端
        /// On33239(ts:2372-2383)`if (!actData) return`——面板未缓存时仍落地推送记录,只有回写面板这一步
        /// 照老端做 guard。</summary>
        private void On33239(NetReader r)
        {
            var p = new CustomActivityModel.DestinyPushInfo
            {
                BaseType = r.ReadU16(), SubType = r.ReadU16(), Turn = r.ReadU16(), Point = r.ReadU32(), NeedPoint = r.ReadU32(),
            };
            CustomActivityModel.Instance.SetDestinyPushInfo(p);
            CustomActivityModel.DestinyPanelData panel = CustomActivityModel.Instance.GetDestinyPanel(p.BaseType, p.SubType);
            if (panel != null)
            {
                panel.Turn = p.Turn;
                panel.Point = p.Point;
                panel.NeedPoint = p.NeedPoint;
            }
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, p.BaseType, p.SubType);
            GameLog.Info("CustomActivity", "33239 天命转盘积分推送(recv-only) base={0} sub={1} turn={2} point={3} needPoint={4}",
                p.BaseType, p.SubType, p.Turn, p.Point, p.NeedPoint);
        }

        /// <summary>33240 开抽回执(pt_332.erl:1042-1064)。ErrorCode **在最前**;**Reward 走 write_string,
        /// 不是 write_object_list**(pt_332.erl:1052 `pt:write_string(Reward)`)。成功时对标老端 On33240
        /// (ts:2385-2407)回写缓存面板 Turn/Point/NeedPoint,并把 RewardList 内 Grade==GradeId 的条目
        /// Status 置 1。</summary>
        private void On33240(NetReader r)
        {
            int errorCode = r.ReadI32();
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            int gradeId = r.ReadU16();
            string reward = r.ReadString();
            int turn = r.ReadU16();
            long point = r.ReadU32();
            long needPoint = r.ReadU32();
            if (errorCode == 1)
            {
                CustomActivityModel.Instance.SetDestinyDrawResult(new CustomActivityModel.DestinyDrawResult
                {
                    ErrorCode = errorCode, BaseType = baseType, SubType = subType, GradeId = gradeId, Reward = reward,
                    Turn = turn, Point = point, NeedPoint = needPoint,
                });
                CustomActivityModel.DestinyPanelData panel = CustomActivityModel.Instance.GetDestinyPanel(baseType, subType);
                if (panel != null)
                {
                    panel.Turn = turn;
                    panel.Point = point;
                    panel.NeedPoint = needPoint;
                    for (int i = 0; i < panel.RewardList.Count; i++)
                    {
                        if (panel.RewardList[i].Grade == gradeId) { panel.RewardList[i].Status = 1; break; }
                    }
                }
            }
            else
            {
                ShowError(errorCode);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, baseType, subType, errorCode);
            GameLog.Info("CustomActivity", "33240 天命转盘开抽 base={0} sub={1} errorCode={2} gradeId={3} reward={4} turn={5}",
                baseType, subType, errorCode, gradeId, reward, turn);
        }

        // ---------------------------------------------------------------------------------------
        // TURNTABLE_100(100):33241/33242(recv-only)
        // ---------------------------------------------------------------------------------------

        public void RequestTurn100Panel(int baseType, int subType) => SendFmt(Proto.CUSTOM_ACT_TURN100_PANEL, "hh", baseType, subType);

        // 33242 是 recv-only(pp_custom_act_list.erl 无 handle(33242),推送点 mod_custom_act_task.erl:129),
        // 不提供 Request 方法。

        /// <summary>33241 面板(pt_332.erl:1066-1083)。无 ErrorCode。</summary>
        private void On33241(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            List<CustomActivityModel.Turn100RewardEntry> list = r.ReadArray(CustomActivityModel.ReadTurn100RewardEntry);
            CustomActivityModel.Instance.SetTurn100Panel(baseType, subType, list);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, baseType, subType);
            GameLog.Info("CustomActivity", "33241 幸运寻宝界面 base={0} sub={1} rewardN={2}", baseType, subType, list.Count);
        }

        /// <summary>33242 **recv-only** 推送(pt_332.erl:1085-1100)。无 ErrorCode。RewardList 元素
        /// **Grade:16,Process:16**(item_to_bin_30,订正见 CustomActivityModel.Turn100PushEntry 注释,
        /// r17_server_customactivity.md 表格对此号有误记)。对标老端 On33242(ts:2416-2434)按 Grade 匹配
        /// 合并进缓存面板更新 Process;config 依赖的"达标自动翻 Status=1"分支超出数据层范围,不镜像。</summary>
        private void On33242(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            List<CustomActivityModel.Turn100PushEntry> pushList = r.ReadArray(CustomActivityModel.ReadTurn100PushEntry);
            CustomActivityModel.Instance.SetTurn100Push(baseType, subType, pushList);
            List<CustomActivityModel.Turn100RewardEntry> panel = CustomActivityModel.Instance.GetTurn100Panel(baseType, subType);
            if (panel != null)
            {
                for (int i = 0; i < panel.Count; i++)
                {
                    for (int j = 0; j < pushList.Count; j++)
                    {
                        if (panel[i].Grade == pushList[j].Grade) { panel[i].Process = pushList[j].Process; break; }
                    }
                }
            }
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, baseType, subType);
            GameLog.Info("CustomActivity", "33242 幸运寻宝推送(recv-only) base={0} sub={1} pushN={2}", baseType, subType, pushList.Count);
        }
    }
}
