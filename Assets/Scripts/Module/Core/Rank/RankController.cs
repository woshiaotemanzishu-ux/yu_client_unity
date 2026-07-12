using System.Collections.Generic;
using Shenxiao.Common.Proto;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Rank
{
    /// <summary>
    /// 排行榜网络层(自动循环 轮12 #12;纯数据层轮,对标老端 commonController/RankController.ts:88-124
    /// 实注册清单)。只注册 22100(防御壳)+22101(个人榜查询)。
    ///
    /// **存活裁决(r12_server §存活判定)**:22102(公会榜)/22103(点赞信息)/22104(膜拜)服务端 handle 整段
    /// 被注释(pp_common_rank.erl:95"公会排行榜和膜拜被剥离出排行榜！！！"),彻底不可达且无替代迁移——
    /// 严禁实现发送与业务,注释存档,不臆造。22105("我要变强")服务端活但老端 RankController.ts 自己从未
    /// RegisterProtocal,老端行为优先,跳过不移植。
    ///
    /// **22101 位宽陷阱**:SelVal 是 64位(l)——与 22102(已死)同名字段的 32位(i)不通用,别混用宽度表。
    ///
    /// **分页续拉改为 config 驱动(轮12 blocker 修复)**:服务端 lib_common_rank_mod.erl 正常分支(:1220)
    /// 与越界分支(:1190)在 Sum 字段位置都传的是客户端请求的 Len——wire sum 恒为请求 len 的回声,从不是
    /// 真实总数,不能拿来判断"是否还有下一页"(旧实现 received&lt;wire_sum 对真实服务端恒为 false,续拉
    /// 死代码,战力榜等只能拉到前 20 名)。现改为对标老端 RankModel.ts:128-160:续拉条件为
    /// received&lt;RankConfigs.GetByType(type).RankMax(BeginQuery 时锁定为 <see cref="RankModel.RankTypeData.ConfiguredMax"/>),
    /// 响应驱动、非 Update 驱动(轮1 教训)——On22101 内直接判断是否需要下一页并立即续发,不依赖
    /// MonoBehaviour.Update/协程轮询,天然可被 CliVerify 反射喂包同步断言。
    ///
    /// **红点(spec §0 裁决:不移植,注释存档)**:老端 RankModel.rankRedDot()/RankTabButton.redDisplay 全仓
    /// 零调用点(r12_oldrank 死代码#5)——从不生效,本端不移植。
    /// </summary>
    public sealed class RankController : BaseController
    {
        public static readonly RankController Instance = new RankController();
        private RankController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.RANK_ERROR, On22100);
            RegisterProtocal(Proto.RANK_QUERY, On22101);
            // 22102/22103/22104:服务端 handle 整段注释(彻底不可达,r12_server §存活判定)+ 22105:老端自己
            // 从未注册(r12_oldrank §RegisterProtocal 实注册清单)——规格§0/纪律5,严禁实现发送与业务,跳过。
        }

        public override void Dispose()
        {
            RankModel.Instance.Clear();
            base.Dispose();
        }

        // =====================================================================================
        // 22101:查询个人排行榜(公开发送 API,RankFlow.Toggle/未来 UI 层调)
        // =====================================================================================

        /// <summary>请求某 rank_type 某一页(对标老端 22101 send "iii")。
        /// Guard(r12_server §Guard lib_common_rank_mod.erl:1187):Start≤0 或 Len≤0 服务端**静默 skip**
        /// (不回任何包)——本端在发送侧本地拦截,不发废包(规格§0/纪律5)。
        /// start==1 视为新一轮查询起点:据 RankConfigs 锁定本轮 config 驱动的分页总量上界(见类注释),
        /// 查不到配置行时兜底单页(=<see cref="RankModel.ONE_MAX"/>)终止,不臆造总数。</summary>
        public void RequestRank(int rankType, int start, int len)
        {
            if (start <= 0 || len <= 0)
            {
                GameLog.Warn("Rank", "RequestRank 本地拦截非法分页 type={0} start={1} len={2}" +
                    "(服务端对此静默 skip,不发废包)", rankType, start, len);
                return;
            }
            if (start == 1)
            {
                RankConfigs.RankTypeCfg cfg = RankConfigs.GetByType(rankType);
                int configuredMax = (cfg != null && cfg.RankMax > 0) ? cfg.RankMax : RankModel.ONE_MAX;
                RankModel.Instance.BeginQuery(rankType, configuredMax);
            }
            SendFmt(Proto.RANK_QUERY, "iii", rankType, start, len);
        }

        /// <summary>入口 API(对标老端 requestRankData(selectTab)):从第 1 页开始拉,单页最多
        /// <see cref="RankModel.ONE_MAX"/> 条,总量由 RankConfigs.GetByType(type).RankMax 驱动(老端
        /// RankModel.ts:137-139 ceil(rank_max/20) 预排页数的等价语义,本端用"响应驱动续拉"替代
        /// "逐帧节流",见类注释)。
        /// GAME_START 不主动拉榜——⚠老端事实上在 GAME_START 会预拉一次默认榜(RankController.ts:66-69
        /// `GlobalEventSystem.Bind(EventName.GAME_START, ()=>_model.requestRankData(0))`),本端裁决刻意
        /// 不复刻这个自动预取(无窗口时白白发包),改为只在开窗(RankFlow.Toggle)时才拉——这是裁决偏离,
        /// 不是漏移植,记录以免下轮误判。</summary>
        public void RequestRankFirstPage(int rankType)
        {
            RankConfigs.RankTypeCfg cfg = RankConfigs.GetByType(rankType);
            int configuredMax = (cfg != null && cfg.RankMax > 0) ? cfg.RankMax : RankModel.ONE_MAX;
            int firstLen = System.Math.Min(RankModel.ONE_MAX, configuredMax);
            RequestRank(rankType, 1, firstLen);
        }

        private void On22100(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            // 孤儿协议:r12_server 证实全仓库 pt_221:write(22100 零调用点(从建库起就没接入)。
            // 照老端仍注册防御 recv(避免真出现时无 handler 报"unhandled proto"噪音),显码 toast 兜底。
            TipsManager.Toast("排行榜错误(" + errcode + ")");
            GameLog.Warn("Rank", "22100 错误码壳 errcode={0}(服务端从未发过此号,防御性注册)", errcode);
        }

        private void On22101(NetReader r)
        {
            int rankType = (int)r.ReadU32();
            int start = (int)r.ReadU32();
            int len = (int)r.ReadU32();
            int roleRank = (int)r.ReadU32();
            long selVal = r.ReadU64();      // ⚠64位(位宽陷阱,别按22102的32位假设——22102本身已死,仅作提醒)
            int selSecVal = (int)r.ReadU32();
            int sum = (int)r.ReadU32();     // ⚠恒为请求 Len 的回声(服务端两分支皆如此),非真实总数,仅存档展示
            List<RankModel.RankItemVo> items = r.ReadArray(ReadRankItem);

            RankModel.Instance.ApplySelf(rankType, roleRank, selVal, selSecVal);
            RankModel.Instance.ApplySum(rankType, sum);
            // 占位项(真实数据不足 Len 条时服务端用 PlayerId=0 补的全 0 行)照样入库,对标老端(渲染"虚位以待",
            // UI 尾包再处理展示)——不做任何"疑似占位就丢弃"的过滤。
            RankModel.Instance.AppendItems(rankType, items);

            int received = RankModel.Instance.GetItemCount(rankType);
            RankModel.RankTypeData data = RankModel.Instance.GetData(rankType);
            int configuredMax = data != null ? data.ConfiguredMax : 0;
            // config 驱动续拉(轮12 blocker 修复,见类注释):wire sum 不参与判断,续拉直到收满 configuredMax
            // (RankConfigs.RankMax)或服务端回空提前终止。
            bool needMore = items.Count > 0 && received < configuredMax;

            GameLog.Info("Rank", "22101 type={0} start={1} len={2} roleRank={3} selVal={4} sumEcho={5}" +
                " items={6} received={7} configuredMax={8} needMore={9}", rankType, start, len, roleRank, selVal,
                sum, items.Count, received, configuredMax, needMore);

            if (needMore)
            {
                // 响应驱动续拉(非 Update):下一页 Start=已收到条数+1,Len=min(ONE_MAX,剩余量)。
                int nextLen = System.Math.Min(RankModel.ONE_MAX, configuredMax - received);
                RequestRank(rankType, received + 1, nextLen);
            }
            else
            {
                RankModel.Instance.MarkComplete(rankType);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_RANK_DATA_UPDATE, rankType);
        }

        private static RankModel.RankItemVo ReadRankItem(NetReader r)
        {
            return new RankModel.RankItemVo
            {
                PlayerId = r.ReadU64(),
                PraiseNum = (int)r.ReadU32(),
                Figure = FigureProto.Read(r),
                SelCombat = r.ReadU64(),
                FirstValue = r.ReadU64(),
                SecondValue = (int)r.ReadU32(),
                ThirdValue = (int)r.ReadU32(),
                Rank = (int)r.ReadU32(),
            };
        }
    }
}
