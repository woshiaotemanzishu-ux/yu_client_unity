using System.Collections.Generic;
using Shenxiao.Common.Proto;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.CustomActivity
{
    /// <summary>
    /// 定制活动跨服+榜(自动循环 轮17 P6):KFGROUPBUY(88,33227/33228/33229/33230/33267)+ 消费/鲜花榜
    /// (224xx,22400/22403/22405)。TopPlayer(225xx)补全(22500/22503-05)不在本文件——归 TopPlayerController.cs
    /// (P6 独占该文件),见其头注释。
    ///
    /// wire 全部逐字段回 pt_332.erl / pt_224.erl 原文核(非仅 write 子句变量名):
    /// - 33227/33228/33229/33230 read/write:pt_332.erl:111-124(read)+797-874(write);item_to_bin_20/21/22/23
    ///   在 pt_332.erl:1922-1987(RecordList 的 FirstBuy/TailBuy 双子数组各自只有一个 GapTime:32 字段)。
    /// - 33267 read/write:pt_332.erl:256-261(read)+1629-1641(write)。
    /// - 22400/22403/22405 read/write:pt_224.erl 全文(24行 read + 26-157行 write);item_to_bin_5/6/7 在
    ///   pt_224.erl:300-346。**22400/22403 实为跨服鲜花榜、22405 才是消费榜**(证据链见 CustomActivityModel.Kf.cs
    ///   §K2 头注释);Proto.cs 三个常量已按本语义定案更名为 KF_FLOWER_RANK_ERROR/KF_FLOWER_RANK_INFO/
    ///   CONSUME_RANK_INFO(原 COST_RANK_* 前缀已废弃)。
    ///
    /// C2S fmt 以老端 fmt 表(CustomActivityController.ts:233-412)+ 调用点实参核对:33227/33228 属"hh"组
    /// (ts:235-284);33229 属"hhhc"组(ts:369-373,与33176/33214同组);33267 属"hhh"组(ts:286-300);22403 与
    /// 22501 同属"ih"组(ts:302-305,专属调用点 FlowerRankView.ts:80-85 `Fire(REQUEST_PROTO,22403,rank_type_id,
    /// sub_type)`确认参数序为 Type,SubType);22405 属"hh"组(ts:235-284,22400 无 read 定义,S2C only)。
    ///
    /// 死号/不发送纪律:33230(pt_332.erl 无 read(33230),S2C only)只注册 recv、不提供发送方法;22400(pt_224.erl
    /// 无 read(22400),S2C only)同理。
    ///
    /// 事件粒度收敛(spec §0):不新开专用事件,复用 P1 已定义的 EVT_CUSTOMACT_DETAIL_UPDATE(信息/记录类落地)
    /// 与 EVT_CUSTOMACT_RESULT(操作回执类)。
    /// </summary>
    public sealed partial class CustomActivityController
    {
        /// <summary>P1 预建空壳,由主文件 Register() 调用。</summary>
        private void RegisterKf()
        {
            RegisterProtocal(Proto.CUSTOM_ACT_KFGROUPBUY_INFO, OnKfGroupBuyInfo);         // 33227
            RegisterProtocal(Proto.CUSTOM_ACT_KFGROUPBUY_RECORD, OnKfGroupBuyRecord);     // 33228
            RegisterProtocal(Proto.CUSTOM_ACT_KFGROUPBUY_BUY, OnKfGroupBuyResult);        // 33229
            RegisterProtocal(Proto.CUSTOM_ACT_KFGROUPBUY_COUNT_PUSH, OnKfGroupBuyCountPush); // 33230(recv-only)
            RegisterProtocal(Proto.CUSTOM_ACT_KFGROUPBUY_SHOUT, OnKfGroupBuyShout);       // 33267

            RegisterProtocal(Proto.KF_FLOWER_RANK_ERROR, OnCostRankError); // 22400(recv-only,跨服鲜花榜错误码;常量名轮17收口按P6语义裁决定名)
            RegisterProtocal(Proto.KF_FLOWER_RANK_INFO, OnFlowerRankInfo); // 22403(跨服鲜花榜)
            RegisterProtocal(Proto.CONSUME_RANK_INFO, OnCostRankExtra);    // 22405(首发充值消费排行)
        }

        // ---------------------------------------------------------------------------------------
        // KFGROUPBUY(88)
        // ---------------------------------------------------------------------------------------

        /// <summary>33227 跨服团购信息(对标老端 On33227,ts:2284-2296)。</summary>
        private void OnKfGroupBuyInfo(NetReader r)
        {
            var info = new CustomActivityModel.KfGroupBuyInfo
            {
                BaseType = r.ReadU16(),
                SubType = r.ReadU16(),
            };
            info.GpGoods.AddRange(r.ReadArray(rr => new CustomActivityModel.KfGroupBuyGrade
            {
                GradeId = rr.ReadU16(),
                FirstBuyCount = rr.ReadU8(),
                TailBuyCount = rr.ReadU8(),
                BuyNum = rr.ReadU16(),
            }));
            info.LastShoutTime = r.ReadU32();

            CustomActivityModel.Instance.SetKfGroupBuyInfo(info);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, info.BaseType, info.SubType);
            GameLog.Info("CustomActivity", "33227 跨服团购信息 base={0} sub={1} goodsN={2} lastShout={3}",
                info.BaseType, info.SubType, info.GpGoods.Count, info.LastShoutTime);
        }

        /// <summary>33228 跨服团购记录(FirstBuy/TailBuy 双子数组三层嵌套;对标老端 On33228,ts:2298-2301)。</summary>
        private void OnKfGroupBuyRecord(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            List<CustomActivityModel.KfGroupBuyRecord> list = r.ReadArray(rr =>
            {
                var rec = new CustomActivityModel.KfGroupBuyRecord
                {
                    RoleId = (long)rr.ReadU64(),
                    RoleName = rr.ReadString(),
                    ServerId = rr.ReadU16(),
                    ServerNum = rr.ReadU16(),
                    GradeId = rr.ReadU16(),
                };
                rec.FirstBuy.AddRange(rr.ReadArray(r2 => (long)r2.ReadU32()));
                rec.FirstBuyTime = rr.ReadU32();
                rec.TailBuy.AddRange(rr.ReadArray(r2 => (long)r2.ReadU32()));
                rec.TailBuyTime = rr.ReadU32();
                return rec;
            });

            CustomActivityModel.Instance.SetKfGroupBuyRecords(baseType, subType, list);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, baseType, subType);
            GameLog.Info("CustomActivity", "33228 跨服团购记录 base={0} sub={1} count={2}", baseType, subType, list.Count);
        }

        /// <summary>33229 购买回执(对标老端 On33229,ts:2303-2317:error_code!=1 走 ShowError 不落地;成功落
        /// Model,RewardList 走标准 object_list)。</summary>
        private void OnKfGroupBuyResult(NetReader r)
        {
            var result = new CustomActivityModel.KfGroupBuyBuyResult
            {
                Code = r.ReadI32(),
                BaseType = r.ReadU16(),
                SubType = r.ReadU16(),
                GradeId = r.ReadU16(),
                PurchaseType = r.ReadU8(),
                BuyCount = r.ReadU8(),
                BuyNum = r.ReadU16(),
            };
            result.RewardList.AddRange(r.ReadArray(rr => new CustomActivityModel.KfGroupBuyReward
            {
                Type = rr.ReadU8(),
                GoodsId = rr.ReadU32(),
                Num = rr.ReadU32(),
            }));

            if (result.Code == 1)
            {
                CustomActivityModel.Instance.SetKfGroupBuyBuyResult(result);
            }
            else
            {
                ShowError(result.Code);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, result.BaseType, result.SubType, result.Code);
            GameLog.Info("CustomActivity", "33229 跨服团购购买回执 code={0} base={1} sub={2} grade={3} rewardN={4}",
                result.Code, result.BaseType, result.SubType, result.GradeId, result.RewardList.Count);
        }

        /// <summary>33230(recv-only 购买数广播,pt_332.erl 无 read(33230)):对标老端 On33230,ts:2319-2332。</summary>
        private void OnKfGroupBuyCountPush(NetReader r)
        {
            int baseType = r.ReadU16();
            int subType = r.ReadU16();
            int gradeId = r.ReadU16();
            int buyNum = r.ReadU16();

            CustomActivityModel.Instance.UpdateKfGroupBuyCount(baseType, subType, gradeId, buyNum);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, baseType, subType);
            GameLog.Info("CustomActivity", "33230 跨服团购购买数广播(recv-only) base={0} sub={1} grade={2} buyNum={3}",
                baseType, subType, gradeId, buyNum);
        }

        /// <summary>33267 喊话回执(对标老端 On33267,ts:2334-2345)。老端此处有一处已知笔误:判断用 scmd.code
        /// 但显示用 scmd.error_code(此号 schema 并无 error_code 字段,实际是 undefined,Util.ErrorCodeShow(undefined)
        /// 空转)——Unity 按正确字段名 code 判断且显示,不镜像这个笔误(数据层轮无 UI 弹窗,用 ShowError 占位)。</summary>
        private void OnKfGroupBuyShout(NetReader r)
        {
            int code = r.ReadI32();
            int type = r.ReadU16();
            int subtype = r.ReadU16();
            long lastShoutTime = r.ReadU32();

            if (code != 1) ShowError(code);
            CustomActivityModel.Instance.UpdateKfGroupBuyShoutTime(type, subtype, lastShoutTime);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, type, subtype, code);
            GameLog.Info("CustomActivity", "33267 跨服团购喊话回执 code={0} type={1} subtype={2} lastShout={3}",
                code, type, subtype, lastShoutTime);
        }

        /// <summary>33227 请求信息(C2S "hh" BaseType,SubType)。</summary>
        public void RequestKfGroupBuyInfo(int baseType, int subType) =>
            SendFmt(Proto.CUSTOM_ACT_KFGROUPBUY_INFO, "hh", baseType, subType);

        /// <summary>33228 请求记录(C2S "hh" BaseType,SubType)。</summary>
        public void RequestKfGroupBuyRecord(int baseType, int subType) =>
            SendFmt(Proto.CUSTOM_ACT_KFGROUPBUY_RECORD, "hh", baseType, subType);

        /// <summary>33229 购买(C2S "hhhc" BaseType,SubType,GradeId,PurchaseType)。</summary>
        public void RequestKfGroupBuyBuy(int baseType, int subType, int gradeId, int purchaseType) =>
            SendFmt(Proto.CUSTOM_ACT_KFGROUPBUY_BUY, "hhhc", baseType, subType, gradeId, purchaseType);

        /// <summary>33267 喊话(C2S "hhh" Type,Subtype,GradeId)。</summary>
        public void RequestKfGroupBuyShout(int type, int subtype, int gradeId) =>
            SendFmt(Proto.CUSTOM_ACT_KFGROUPBUY_SHOUT, "hhh", type, subtype, gradeId);

        // 33230 recv-only(pt_332.erl 无 read(33230)):严禁提供发送方法。

        // ---------------------------------------------------------------------------------------
        // 消费/鲜花榜(224xx)——22400/22403 实为跨服鲜花榜,22405 为消费榜,见本文件与 Model 头注释证据链。
        // ---------------------------------------------------------------------------------------

        /// <summary>22400(recv-only 鲜花榜通用错误码,pt_224.erl 无 read(22400))。对标老端 On22400,ts:1904-1909:
        /// 判定阈值是 !=1(注意与 33100/22500 那套 !=1012 阈值不同,不可套模板)。</summary>
        private void OnCostRankError(NetReader r)
        {
            int code = r.ReadI32();
            if (code != 1) ShowError(code);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_ERROR, code);
            GameLog.Info("CustomActivity", "22400 鲜花榜错误码 code={0}", code);
        }

        /// <summary>22403 跨服鲜花榜数据(对标老端 On22403,ts:1911-1915,联动 FlowerrankModel.SetFlowerRankData;
        /// Unity 无该 Model,数据落 CustomActivityModel,见其 §K2 头注释 TODO)。FigureList 走 write_figure→
        /// 复用 FigureProto.Read(与 Marriage/Chat/Team 等既有用法一致)。</summary>
        private void OnFlowerRankInfo(NetReader r)
        {
            var info = new CustomActivityModel.FlowerRankInfo
            {
                Type = r.ReadU32(),
                SubType = r.ReadU16(),
                SelRank = r.ReadU32(),
                SelVal = r.ReadU32(),
                SelZone = r.ReadU8(),
                Sum = r.ReadU32(),
                MaxLen = r.ReadU16(),
                RankLimit = r.ReadU32(),
            };
            info.RankList.AddRange(r.ReadArray(rr => new CustomActivityModel.FlowerRankRoleEntry
            {
                RoleId = (long)rr.ReadU64(),
                ServerId = rr.ReadU16(),
                Zone = rr.ReadU8(),
                ServerNum = rr.ReadU16(),
                Name = rr.ReadString(),
                FirstValue = rr.ReadU32(),
                Rank = rr.ReadU32(),
            }));
            info.FigureList.AddRange(r.ReadArray(rr => new CustomActivityModel.FlowerRankFigureEntry
            {
                RoleId = (long)rr.ReadU64(),
                Figure = FigureProto.Read(rr),
            }));

            CustomActivityModel.Instance.SetFlowerRankInfo(info);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, (int)info.Type, info.SubType);
            GameLog.Info("CustomActivity", "22403 跨服鲜花榜 type={0} sub={1} rankN={2} figureN={3}",
                info.Type, info.SubType, info.RankList.Count, info.FigureList.Count);
        }

        /// <summary>22405 消费榜/首发充值消费排行(对标老端 On22405,ts:2256-2259)。</summary>
        private void OnCostRankExtra(NetReader r)
        {
            var info = new CustomActivityModel.CostRankInfo
            {
                Code = r.ReadI32(),
                Type = r.ReadU16(),
                SubType = r.ReadU16(),
                RankType = r.ReadU32(),
                SelRank = r.ReadU32(),
                SelVal = r.ReadU32(),
                Sum = r.ReadU32(),
                MaxLen = r.ReadU16(),
                RankLimit = r.ReadU32(),
            };
            info.RankList.AddRange(r.ReadArray(rr => new CustomActivityModel.CostRankRoleEntry
            {
                RoleId = (long)rr.ReadU64(),
                Name = rr.ReadString(),
                FirstValue = rr.ReadU32(),
                Rank = rr.ReadU32(),
            }));

            CustomActivityModel.Instance.SetCostRankInfo(info);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, info.Type, info.SubType);
            GameLog.Info("CustomActivity", "22405 消费榜 code={0} type={1} sub={2} rankN={3}",
                info.Code, info.Type, info.SubType, info.RankList.Count);
        }

        /// <summary>22403 请求跨服鲜花榜(C2S "ih" Type,SubType;对标 FlowerRankView.ts:80-85 调用实参序
        /// rank_type_id,sub_type;与老端 fmt 表 22403 归属"ih"组一致,ts:302-305)。</summary>
        public void RequestFlowerRank(int rankType, int subType) =>
            SendFmt(Proto.KF_FLOWER_RANK_INFO, "ih", rankType, subType);

        /// <summary>22405 请求消费榜(C2S "hh" Type,SubType,对标 fmt 表归属,ts:235-284 组内)。</summary>
        public void RequestCostRank(int type, int subType) =>
            SendFmt(Proto.CONSUME_RANK_INFO, "hh", type, subType);

        // 22400 recv-only(pt_224.erl 无 read(22400)):严禁提供发送方法。
    }
}
