using Shenxiao.Common.Proto;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.CustomActivity
{
    /// <summary>
    /// P3 抽奖B(自动循环 轮17,spec §4,14号):GASHAPON(103) 33245/33246、LUC_TREA_TWO(102) 33243/33244、
    /// ONLINE_DRAW(81) 33217/33266、LUC_TREA(80) 33213/33214、FORTUNECAT(87) 33224、
    /// BIND_JAGE_WISH(127) 33260/33262/33263。数据落 CustomActivityModel.LotteryB.cs。
    ///
    /// wire 全部逐字段回 yu_server\src\pt\pt_332.erl 原文核对(read/2 与 write/2 两侧,含 item_to_bin_N 辅助
    /// 函数),侦察稿 r17_server_customactivity.md 在本包 4 处与 .erl 原文不符,均按原文订正(订正详情见
    /// CustomActivityModel.LotteryB.cs 头注释 1)-3),以及下方 RequestBindJageDraw 注释 4)):
    ///   1) 33214(LUC_TREA 抽奖) Reward 非扁平三元组数组,是 item_to_bin_6 嵌套结构。
    ///   2) 33243/33244(LUC_TREA_TWO) GradeInfo 与 RewardList/Reward/GradeList 是并列顶层字段,不互相嵌套;
    ///      33244 ErrorCode 是第 5 个顶层字段。
    ///   3) 33224(FORTUNECAT 信息) RewardList 是 4 字段 item_to_bin_17,非侦察表漏记 GradeId/IsHead 的 2 字段版本。
    ///   4) 33262(BIND_JAGE_WISH 开抽) C2S 只发 Type,SubType 两字段(pt_332.erl read(33262):234-237),侦察表
    ///      误记为 5 字段(Grade/Turn/Times 由服务端自算,client 不传);S2C 回包仍是 6 字段、Errcode 末尾。
    ///
    /// 事件粒度收敛(spec §0):一律用 P1 定义的 EVT_CUSTOMACT_DETAIL_UPDATE(纯信息包,无 ErrorCode 或
    /// ErrorCode 仅用于门禁展示)/EVT_CUSTOMACT_RESULT(明确的"操作/查询结果"回执,ErrorCode 驱动 UI 展示成败),
    /// 不为本包开专用事件。失败码统一走 ShowError 显码降级(仿 Marriage/KfBoss 先例);先落 Model 后 Emit。
    /// </summary>
    public sealed partial class CustomActivityController
    {
        /// <summary>P1 预建空壳,由主文件 Register() 调用。</summary>
        private void RegisterLotteryB()
        {
            RegisterProtocal(Proto.CUSTOM_ACT_GASHAPON_INFO, On33245);
            RegisterProtocal(Proto.CUSTOM_ACT_GASHAPON_DRAW, On33246);
            RegisterProtocal(Proto.CUSTOM_ACT_LUCTREA2_PANEL, On33243);
            RegisterProtocal(Proto.CUSTOM_ACT_LUCTREA2_DRAW, On33244);
            RegisterProtocal(Proto.CUSTOM_ACT_ONLINEDRAW_PANEL, On33217);
            RegisterProtocal(Proto.CUSTOM_ACT_ONLINEDRAW_GOODS_POWER, On33266);
            RegisterProtocal(Proto.CUSTOM_ACT_LUCTREA_PANEL, On33213);
            RegisterProtocal(Proto.CUSTOM_ACT_LUCTREA_DRAW, On33214);
            RegisterProtocal(Proto.CUSTOM_ACT_FORTUNECAT_INFO, On33224);
            RegisterProtocal(Proto.CUSTOM_ACT_BINDJAGE_INFO, On33260);
            RegisterProtocal(Proto.CUSTOM_ACT_BINDJAGE_DRAW, On33262);
            RegisterProtocal(Proto.CUSTOM_ACT_BINDJAGE_FREEGIFT, On33263);
        }

        // ---------------------------------------------------------------------------------------
        // GASHAPON(103):33245 通用抽奖信息(无 ErrorCode,纯信息)/ 33246 开抽(ErrorCode 开头)
        // pt_332.erl write 33245:1178-1229 / write 33246:1231-1260(item_to_bin_35/36/37/38)
        // ---------------------------------------------------------------------------------------

        private void On33245(NetReader r)
        {
            var info = new CustomActivityModel.GashaponInfo
            {
                BaseType = r.ReadU16(),
                SubType = r.ReadU16(),
                MaxLuck = r.ReadU32(),
                CurrentLuck = r.ReadU32(),
                PerLuck = r.ReadU16(),
                TotalTimes = r.ReadU32(),
                OneCost = r.ReadString(),
                TenCost = r.ReadString(),
            };
            info.DrawList.AddRange(r.ReadArray(rr => new CustomActivityModel.GashaponDrawGrade
            {
                GradeId = rr.ReadU16(), IsNice = rr.ReadU8(), IsGet1 = rr.ReadU8(), Reward = rr.ReadString(),
            }));
            info.GrandList.AddRange(r.ReadArray(rr => new CustomActivityModel.GashaponGrandGrade
            {
                GradeId = rr.ReadU16(), IsGet2 = rr.ReadU8(), NeedNum = rr.ReadU16(), Reward = rr.ReadString(),
            }));
            info.ExchangeList.AddRange(r.ReadArray(rr => new CustomActivityModel.GashaponExchangeGrade
            {
                GradeId = rr.ReadU16(), NeedPoint = rr.ReadU16(), Reward = rr.ReadString(),
            }));
            CustomActivityModel.Instance.SetGashaponInfo(info);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, info.BaseType, info.SubType);
            GameLog.Info("CustomActivity", "33245 GASHAPON信息 base={0} sub={1} draw={2} grand={3} exchange={4}",
                info.BaseType, info.SubType, info.DrawList.Count, info.GrandList.Count, info.ExchangeList.Count);
        }

        private void On33246(NetReader r)
        {
            var result = new CustomActivityModel.GashaponDrawResult { Code = r.ReadI32() };
            result.BaseType = r.ReadU16();
            result.SubType = r.ReadU16();
            result.AutoBuy = r.ReadU8();
            result.LucencyField = r.ReadU8();
            result.CurrentLuck = r.ReadU32();
            result.CurrentTimes = r.ReadU32();
            result.RewardList.AddRange(r.ReadArray(rr => new CustomActivityModel.GashaponDrawRewardEntry
            {
                GradeId = rr.ReadU16(), Reward = rr.ReadString(), IsNice = rr.ReadU8(),
            }));
            if (result.Code == 1) CustomActivityModel.Instance.SetGashaponDrawResult(result);
            else ShowError(result.Code);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, result.BaseType, result.SubType, result.Code);
            GameLog.Info("CustomActivity", "33246 GASHAPON开抽 code={0} base={1} sub={2} rewardN={3}",
                result.Code, result.BaseType, result.SubType, result.RewardList.Count);
        }

        // ---------------------------------------------------------------------------------------
        // LUC_TREA_TWO(102):33243 幸运鉴宝2界面(无 ErrorCode)/ 33244 抽奖(ErrorCode 第5顶层字段)
        // pt_332.erl write 33243:1104-1134 / write 33244:1136-1176(item_to_bin_31/32/33/34)
        // ---------------------------------------------------------------------------------------

        private void On33243(NetReader r)
        {
            var info = new CustomActivityModel.Luctrea2Info
            {
                BaseType = r.ReadU16(), SubType = r.ReadU16(), DrawTime = r.ReadU16(), Turn = r.ReadU16(),
            };
            info.GradeInfo.AddRange(r.ReadArray(CustomActivityModel.ReadGradeCount));
            info.RewardList.AddRange(r.ReadArray(rr => new CustomActivityModel.Luctrea2RewardConfig
            {
                Grade = rr.ReadU16(), FormType = rr.ReadU8(), Name = rr.ReadString(), Desc = rr.ReadString(),
                Condition = rr.ReadString(), Reward = rr.ReadString(),
            }));
            CustomActivityModel.Instance.SetLuctrea2Info(info);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, info.BaseType, info.SubType);
            GameLog.Info("CustomActivity", "33243 幸运鉴宝2界面 base={0} sub={1} gradeInfoN={2} rewardN={3}",
                info.BaseType, info.SubType, info.GradeInfo.Count, info.RewardList.Count);
        }

        /// <summary>**追发镜像**(ts:2479-2517,自动循环 轮17三镜头验收补):成功后(其余全为 UI 侧弹窗逻辑,
        /// 本轮不镜像)函数末尾无条件追发 33243 重拉幸运鉴宝2界面;失败分支提前 return,不追发。</summary>
        private void On33244(NetReader r)
        {
            var result = new CustomActivityModel.Luctrea2DrawResult
            {
                BaseType = r.ReadU16(), SubType = r.ReadU16(), Times = r.ReadU16(), AutoBuy = r.ReadU8(), Code = r.ReadI32(),
            };
            result.GradeList.AddRange(r.ReadArray(rr => (int)rr.ReadU16())); // item_to_bin_33:单字段 GradeId
            result.Reward.AddRange(r.ReadArray(CustomActivityModel.ReadRewardTriple)); // pt:write_object_list 标准三元组
            result.DrawTime = r.ReadU16();
            result.Turn = r.ReadU16();
            result.GradeInfo.AddRange(r.ReadArray(CustomActivityModel.ReadGradeCount));
            if (result.Code == 1)
            {
                CustomActivityModel.Instance.SetLuctrea2DrawResult(result);
                RequestLuctrea2Info(result.BaseType, result.SubType); // 追发33243重拉,镜像ts:2516
            }
            else ShowError(result.Code);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, result.BaseType, result.SubType, result.Code);
            GameLog.Info("CustomActivity", "33244 幸运鉴宝2抽奖 code={0} base={1} sub={2} gradeListN={3} rewardN={4}",
                result.Code, result.BaseType, result.SubType, result.GradeList.Count, result.Reward.Count);
        }

        // ---------------------------------------------------------------------------------------
        // ONLINE_DRAW(81):33217 等级活跃抽奖界面信息(ErrorCode 开头,DrawTime:32)/ 33266 物品期望战力
        // (ErrorCode 末尾)。pt_332.erl write 33217:532-555(item_to_bin_7 含 write_figure)/ write 33266:1615-1627
        // ---------------------------------------------------------------------------------------

        private void On33217(NetReader r)
        {
            var info = new CustomActivityModel.OnlineDrawInfo { Code = r.ReadI32() };
            info.BaseType = r.ReadU16();
            info.SubType = r.ReadU16();
            info.DrawTime = r.ReadU32();
            info.IsWinner = r.ReadU8();
            info.WinnerList.AddRange(r.ReadArray(rr => new CustomActivityModel.OnlineDrawWinner
            {
                RoleId = rr.ReadU64(), Figure = FigureProto.Read(rr),
            }));
            if (info.Code == 1) CustomActivityModel.Instance.SetOnlineDrawInfo(info);
            else ShowError(info.Code);
            // 本号语义是"界面信息"(等级活跃抽奖当前状态),ErrorCode 仅门禁展示(活动未开等)——按信息类发
            // DETAIL_UPDATE,不发 RESULT(RESULT 语义留给明确的操作/查询类,见头注释)。
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, info.BaseType, info.SubType);
            GameLog.Info("CustomActivity", "33217 等级活跃抽奖界面 code={0} base={1} sub={2} isWinner={3} winnerN={4}",
                info.Code, info.BaseType, info.SubType, info.IsWinner, info.WinnerList.Count);
        }

        private void On33266(NetReader r)
        {
            var result = new CustomActivityModel.GoodsPowerResult
            {
                BaseType = r.ReadU16(), SubType = r.ReadU16(), Power = r.ReadU64(), Code = r.ReadI32(),
            };
            if (result.Code == 1) CustomActivityModel.Instance.SetGoodsPowerResult(result);
            else ShowError(result.Code);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, result.BaseType, result.SubType, result.Code);
            GameLog.Info("CustomActivity", "33266 物品期望战力 code={0} base={1} sub={2} power={3}",
                result.Code, result.BaseType, result.SubType, result.Power);
        }

        // ---------------------------------------------------------------------------------------
        // LUC_TREA(80):33213 幸运抽奖界面(ErrorCode 末尾)/ 33214 抽奖(ErrorCode 第3顶层字段,Reward 嵌套
        // item_to_bin_6)。pt_332.erl write 33213:465-479 / write 33214:481-500(item_to_bin_6:1744-1756)
        // ---------------------------------------------------------------------------------------

        private void On33213(NetReader r)
        {
            var data = new CustomActivityModel.LuctreaPoolData { BaseType = r.ReadU16(), SubType = r.ReadU16() };
            data.Pool.AddRange(r.ReadArray(CustomActivityModel.ReadRewardTriple));
            data.Code = r.ReadI32(); // 末尾
            if (data.Code == 1) CustomActivityModel.Instance.SetLuctreaPool(data);
            else ShowError(data.Code);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, data.BaseType, data.SubType);
            GameLog.Info("CustomActivity", "33213 幸运抽奖界面 code={0} base={1} sub={2} poolN={3}",
                data.Code, data.BaseType, data.SubType, data.Pool.Count);
        }

        private void On33214(NetReader r)
        {
            var result = new CustomActivityModel.LuctreaDrawResult
            {
                BaseType = r.ReadU16(), SubType = r.ReadU16(), Code = r.ReadI32(), // 第3顶层字段,非开头非末尾
            };
            result.Reward.AddRange(r.ReadArray(ReadLuctreaRewardGroup));
            if (result.Code == 1) CustomActivityModel.Instance.SetLuctreaDrawResult(result);
            else ShowError(result.Code);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, result.BaseType, result.SubType, result.Code);
            GameLog.Info("CustomActivity", "33214 幸运抽奖抽奖 code={0} base={1} sub={2} groupN={3}",
                result.Code, result.BaseType, result.SubType, result.Reward.Count);
        }

        /// <summary>item_to_bin_6({Grade,RewardList,Rare}):Grade:16 + 嵌套标准三元组数组 + Rare:8。</summary>
        private static CustomActivityModel.LuctreaRewardGroup ReadLuctreaRewardGroup(NetReader r)
        {
            var g = new CustomActivityModel.LuctreaRewardGroup { Grade = r.ReadU16() };
            g.RewardList.AddRange(r.ReadArray(CustomActivityModel.ReadRewardTriple));
            g.Rare = r.ReadU8();
            return g;
        }

        // ---------------------------------------------------------------------------------------
        // FORTUNECAT(87):33224 信息(无 ErrorCode,RewardId:64)。pt_332.erl write 33224:717-749
        // (item_to_bin_16/17)。
        // **33225/33226 死活口径订正**(自动循环 轮17三镜头验收):老端 On33225/On33226(ts:2267-2282)函数体
        // 整段被注释掉(连 GetSCMD 都没调),且全仓 grep `SCMD_REQUEST, *3322[56]` 零命中——C2S 客户端侧
        // 彻底死(老端不发送、收到也不处理)。但服务端 handle(33225)/handle(33226) 仍活着
        // (pp_custom_act_list.erl:252/261),不是协议本身失效。按"死活以老端客户端为准"的项目铁律,这两号
        // 属死号:R548进一步删除Proto常量、运行时注册、handler与raw模型，避免防御性接收壳伪装成协议覆盖。
        // 33224 仍活(老端 On33224 有完整实现且被 RequestActDetail ACT_ID_FORTUNECAT 分支自动追发),不动。
        // ---------------------------------------------------------------------------------------

        private void On33224(NetReader r)
        {
            var info = new CustomActivityModel.FortunecatInfo
            {
                BaseType = r.ReadU16(), SubType = r.ReadU16(), Turns = r.ReadU32(),
                CgoodsId = r.ReadI32(), CgoodsNum = r.ReadI32(),
            };
            info.RoundsList.AddRange(r.ReadArray(rr => new CustomActivityModel.FortunecatRound
            {
                Rounds = rr.ReadU32(), MaxNum = rr.ReadU32(), MinNum = rr.ReadU32(), RewardId = rr.ReadU64(),
            }));
            info.RewardList.AddRange(r.ReadArray(rr => new CustomActivityModel.FortunecatRewardConfig
            {
                GradeId = rr.ReadU16(), GoodsId = rr.ReadI32(), GoodsNum = rr.ReadI32(), IsHead = rr.ReadU8(),
            }));
            CustomActivityModel.Instance.SetFortunecatInfo(info);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, info.BaseType, info.SubType);
            GameLog.Info("CustomActivity", "33224 招财猫信息 base={0} sub={1} roundsN={2} rewardN={3}",
                info.BaseType, info.SubType, info.RoundsList.Count, info.RewardList.Count);
        }

        // ---------------------------------------------------------------------------------------
        // BIND_JAGE_WISH(127):33260 心愿单信息(无 ErrorCode)/ 33262 开抽(ErrorCode 末尾)/
        // 33263 免费礼(ErrorCode 末尾)。pt_332.erl write 33260:1526-1544 / 33262:1552-1568 / 33263:1570-1580
        // ---------------------------------------------------------------------------------------

        private void On33260(NetReader r)
        {
            var info = new CustomActivityModel.BindJageInfo
            {
                Type = r.ReadU16(), Subtype = r.ReadU16(), FreeTimes = r.ReadU8(), IsFirstRecharge = r.ReadU8(),
                Turn = r.ReadU8(), Times = r.ReadU16(), FreeGiftStatus = r.ReadU8(),
            };
            CustomActivityModel.Instance.SetBindJageInfo(info);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, info.Type, info.Subtype);
            GameLog.Info("CustomActivity", "33260 心愿单信息 type={0} sub={1} freeTimes={2} turn={3} times={4}",
                info.Type, info.Subtype, info.FreeTimes, info.Turn, info.Times);
        }

        private void On33262(NetReader r)
        {
            var result = new CustomActivityModel.BindJageDrawResult
            {
                Type = r.ReadU16(), Subtype = r.ReadU16(), Grade = r.ReadU16(), Turn = r.ReadU8(),
                Times = r.ReadU16(), Code = r.ReadI32(), // 末尾
            };
            if (result.Code == 1) CustomActivityModel.Instance.SetBindJageDrawResult(result);
            else ShowError(result.Code);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, result.Type, result.Subtype, result.Code);
            GameLog.Info("CustomActivity", "33262 心愿单开抽 code={0} type={1} sub={2} grade={3} turn={4} times={5}",
                result.Code, result.Type, result.Subtype, result.Grade, result.Turn, result.Times);
        }

        /// <summary>**三镜头订正,去掉老端没有的弹码**(ts:2718-2730):老端 On33263 只有
        /// `if (errcode==1) {...}` 一支,无 else,失败完全静默不弹错。</summary>
        private void On33263(NetReader r)
        {
            var result = new CustomActivityModel.BindJageFreeGiftResult
            {
                Type = r.ReadU16(), Subtype = r.ReadU16(), Code = r.ReadI32(), // 末尾
            };
            if (result.Code == 1) CustomActivityModel.Instance.SetBindJageFreeGiftResult(result);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, result.Type, result.Subtype, result.Code);
            GameLog.Info("CustomActivity", "33263 心愿单免费礼 code={0} type={1} sub={2}", result.Code, result.Type, result.Subtype);
        }

        // ---------------------------------------------------------------------------------------
        // C2S 请求方法。fmt 以 pt_332.erl read/2 原文实参顺序为准(不是侦察表的简写)。"info/panel" 类号
        // (33245/33243/33217/33213/33224/33260)已由 CustomActivityController.Core.cs 的 RequestActDetail
        // 按 base_type 兜底发出(见该文件 ACT_ID_GASHAPON 等分支),这里仍各暴露一个直发方法供 UI 精确刷新单面板
        // /供本文件 Case 反射调用校验,两条路径最终 SendFmt 同一协议号,不冲突。
        // ---------------------------------------------------------------------------------------

        public void RequestGashaponInfo(int baseType, int subType) => SendFmt(Proto.CUSTOM_ACT_GASHAPON_INFO, "hh", baseType, subType);

        /// <summary>33246 开抽。C2S BaseType,SubType,Times:16,AutoBuy:8,LucencyField:8(pt_332.erl read(33246):172-178)。</summary>
        public void RequestGashaponDraw(int baseType, int subType, int times, int autoBuy, int lucencyField) =>
            SendFmt(Proto.CUSTOM_ACT_GASHAPON_DRAW, "hhhcc", baseType, subType, times, autoBuy, lucencyField);

        public void RequestLuctrea2Info(int baseType, int subType) => SendFmt(Proto.CUSTOM_ACT_LUCTREA2_PANEL, "hh", baseType, subType);

        /// <summary>33244 抽奖。C2S BaseType,SubType,Times:16,AutoBuy:8,Turn:16(pt_332.erl read(33244):161-167)。</summary>
        public void RequestLuctrea2Draw(int baseType, int subType, int times, int autoBuy, int turn) =>
            SendFmt(Proto.CUSTOM_ACT_LUCTREA2_DRAW, "hhhch", baseType, subType, times, autoBuy, turn);

        public void RequestOnlineDrawInfo(int baseType, int subType) => SendFmt(Proto.CUSTOM_ACT_ONLINEDRAW_PANEL, "hh", baseType, subType);

        /// <summary>33266 物品期望战力。C2S Type,Subtype,GoodsId:64(pt_332.erl read(33266):251-255)。</summary>
        public void RequestOnlineDrawGoodsPower(int type, int subtype, long goodsId) =>
            SendFmt(Proto.CUSTOM_ACT_ONLINEDRAW_GOODS_POWER, "hhl", type, subtype, goodsId);

        public void RequestLuctreaPool(int baseType, int subType) => SendFmt(Proto.CUSTOM_ACT_LUCTREA_PANEL, "hh", baseType, subType);

        /// <summary>33214 抽奖。C2S BaseType,SubType,Times:16,AutoBuy:8(pt_332.erl read(33214):55-60)。</summary>
        public void RequestLuctreaDraw(int baseType, int subType, int times, int autoBuy) =>
            SendFmt(Proto.CUSTOM_ACT_LUCTREA_DRAW, "hhhc", baseType, subType, times, autoBuy);

        /// <summary>33224 信息。C2S BaseType,SubType,IsNext:8(pt_332.erl read(33224):98-102)。
        /// RequestActDetail 兜底恒传 IsNext=0(首次打开),此处暴露完整参数供"下一轮预览"等交互调用。</summary>
        public void RequestFortunecatInfo(int baseType, int subType, int isNext) =>
            SendFmt(Proto.CUSTOM_ACT_FORTUNECAT_INFO, "hhc", baseType, subType, isNext);

        // 33225/33226 为死号：严禁恢复发送方法、Proto常量、handler或raw模型。

        public void RequestBindJageInfo(int type, int subtype) => SendFmt(Proto.CUSTOM_ACT_BINDJAGE_INFO, "hh", type, subtype);

        /// <summary>33262 开抽。C2S 只发 Type,SubType(pt_332.erl read(33262):234-237)——**订正**:侦察表
        /// r17_server_customactivity.md 误记为 "Type,Subtype,Grade:16,Turn:8,Times:16" 5 字段,实际服务端
        /// read/2 只解析 2 字段;Grade/Turn/Times 由服务端按当前心愿单状态自算,失败时 Grade 固定填 0
        /// 回显(pp_custom_act_list.erl:576-585)。**三镜头订正**:协议帧带长度前缀,服务端 read/2 用
        /// `_Bin2/binary` 接住尾余字节并直接丢弃(pt_332.erl:234-237),若照侦察表多发 5 字段并不会
        /// "错位断链"(不会污染下一包解析);按 .erl 原文 2 字段发送仍是正确订正,只是理由改为"字段语义
        /// 对齐服务端真实解析结构",而非"避免协议错位崩溃"。</summary>
        public void RequestBindJageDraw(int type, int subtype) => SendFmt(Proto.CUSTOM_ACT_BINDJAGE_DRAW, "hh", type, subtype);

        public void RequestBindJageFreeGift(int type, int subtype) => SendFmt(Proto.CUSTOM_ACT_BINDJAGE_FREEGIFT, "hh", type, subtype);
    }
}
