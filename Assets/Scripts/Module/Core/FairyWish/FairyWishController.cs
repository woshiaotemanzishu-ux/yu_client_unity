using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.FairyWish
{
    /// <summary>
    /// 仙灵祝福(FairyWish)协议控制器(自动循环 轮18 PK2;对标老端 commonController/FairyWishController.ts,
    /// 服务端 pt_513,4 号全活:51300/51301/51302/51303)。⚠51302 为 send-only(pt_513.erl:53-57 write 子句
    /// 体为空,发后不等回包,回执改走后续 51300 主动推送,严禁按通用请求模式阻塞等待本号 ack);
    /// 51303 为 recv-only(全仓无发送点,严禁发送)。
    /// 服务端在上线/重连/充值时主动推送 51300(对 5 个 FairyId 各推一次,r18 A组侦察),故本端不做 GAME_START
    /// 批量请求,仅在 config 就绪后记录日志;真正的请求入口留 Pet/OutWard 系统尾包对接(OutWardBaseView.ts:411)。
    /// </summary>
    public sealed class FairyWishController : BaseController
    {
        public static readonly FairyWishController Instance = new FairyWishController();
        private FairyWishController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.FAIRYWISH_INFO, On51300);
            RegisterProtocal(Proto.FAIRYWISH_NODE_ACTIVATE, On51301);
            RegisterProtocal(Proto.FAIRYWISH_CLICK_PUSH, On51303);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            FairyWishModel.Instance.Reset();
            base.Dispose();
        }

        private async void OnGameStart()
        {
            await FairyWishConfigs.EnsureLoaded();
            // 51300 由服务端主动推送(上线/重连/充值,对 5 个 FairyId 各推一次),本端不主动逐个请求,
            // 仅记录 config 就绪(对标老端:REQUEST_PROTO 只在 UI 入口点触发,GAME_START 无对应发送)。
            GameLog.Info("FairyWish", "config ready fairy={0} node={1}(51300 由服务端主动推送)",
                FairyWishConfigs.FairyCount, FairyWishConfigs.NodeCount);
        }

        /// <summary>请求某仙灵全部信息(入口留 Pet/OutWard 系统尾包对接)。发 "i"(FairyId)。</summary>
        public void RequestInfo(int fairyId)
        {
            if (fairyId <= 0) return;
            SendFmt(Proto.FAIRYWISH_INFO, "i", fairyId);
            GameLog.Info("FairyWish", "request 51300 fairyId={0}", fairyId);
        }

        /// <summary>强化节点(对标 FairyWishView.ts:228)。发 "ii"(FairyId, NodeId)。</summary>
        public void RequestNodeActivate(int fairyId, int nodeId)
        {
            if (fairyId <= 0 || nodeId <= 0) return;
            SendFmt(Proto.FAIRYWISH_NODE_ACTIVATE, "ii", fairyId, nodeId);
            GameLog.Info("FairyWish", "request 51301 fairyId={0} nodeId={1}", fairyId, nodeId);
        }

        /// <summary>购买仙灵(对标 pet/OutWardBaseView.ts:411)。发 "i"(FairyId);**send-only,发后不等回包**,
        /// 回执改走后续 <see cref="Proto.FAIRYWISH_INFO"/> 主动推送,严禁阻塞等待本号 ack。</summary>
        public void RequestBuy(int fairyId)
        {
            if (fairyId <= 0) return;
            SendFmt(Proto.FAIRYWISH_BUY, "i", fairyId);
            GameLog.Info("FairyWish", "request 51302 buy fairyId={0}(fire-and-forget,无回包)", fairyId);
        }

        /// <summary>51300:FairyId:32, IsBuy:8, NodeList[u16×{NodeId:32,IsActivate:8,Combat:32}]。</summary>
        private void On51300(NetReader r)
        {
            int fairyId = (int)r.ReadU32();
            int isBuy = r.ReadU8();
            List<(int NodeId, int IsActivate, int Combat)> nodeList = r.ReadArray(rr =>
                ((int)rr.ReadU32(), (int)rr.ReadU8(), (int)rr.ReadU32()));
            FairyWishModel.Instance.ApplyInfo(fairyId, isBuy, nodeList);
            GameLog.Info("FairyWish", "51300 info fairyId={0} isBuy={1} nodes={2}", fairyId, isBuy, nodeList.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_FAIRYWISH_UPDATE, fairyId);
        }

        /// <summary>51301:FairyId:32, NodeId:32, **Code:32 在末尾**(pt_513.erl:41-51 已核)。仅 Code==1 才落地
        /// 节点激活(镜像老端 updateNodeInfo:node_data.code==1 判定),失败显码降级不落地。成功后老端联动
        /// OutWardBaseModel.UpdateOutWardStrongerRed(fairy_id-1000)刷新红点——耦合 Pet/OutWard 系统,
        /// TODO(PK2 遗留,可检索 "FairyWish 红点耦合"):UI 落地时对接。</summary>
        private void On51301(NetReader r)
        {
            int fairyId = (int)r.ReadU32();
            int nodeId = (int)r.ReadU32();
            int code = (int)r.ReadU32();
            if (code != 1)
            {
                TipsManager.Toast("强化失败(" + code + ")"); // 错误码表未移植,显码降级
                GameLog.Info("FairyWish", "51301 activate fail fairyId={0} nodeId={1} code={2}", fairyId, nodeId, code);
                return;
            }
            FairyWishModel.Instance.ApplyNodeActivate(fairyId, nodeId);
            GameLog.Info("FairyWish", "51301 activate ok fairyId={0} nodeId={1}", fairyId, nodeId);
            EventDispatcher.Emit(GlobalEvent.EVT_FAIRYWISH_UPDATE, fairyId);
        }

        /// <summary>51303 recv-only(全仓无发送点,严禁发送):ClickList[u16×{FairyId:32,Times:8}]。</summary>
        private void On51303(NetReader r)
        {
            List<(int FairyId, int Times)> clickList = r.ReadArray(rr => ((int)rr.ReadU32(), (int)rr.ReadU8()));
            FairyWishModel.Instance.ApplyClickPush(clickList);
            GameLog.Info("FairyWish", "51303 click push count={0}", clickList.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_FAIRYWISH_UPDATE, 0);
        }
    }
}
