using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Vip
{
    /// <summary>
    /// VIP、充值商品与福利卡只读状态控制器。45000/45004 是启动与跨天快照，45005/45006 是独立原始通知；
    /// 15800/15801 只更新充值数据，15901 仅提供显式福利卡列表查询，不接入口、红点或领奖行为。
    /// 老客户端虽把 158@0/158@3 写入 ActivityIconManager，但 location_type=7 没有任何 View 消费；
    /// Unity 不再把这两个历史死入口注册进 HudActivity，避免与顶部固定入口重复显示。
    /// </summary>
    public sealed class VipController : BaseController
    {
        public static readonly VipController Instance = new VipController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_vipOutboundIntercept = null;
        private static Func<byte[], bool> s_welfareCardOutboundIntercept = null;
#endif

        private VipController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.VIP_INFO, On45000);
            RegisterProtocal(Proto.VIP_PRIVILEGE_CARD_LIST, On45004);
            RegisterProtocal(Proto.VIP_CARD_ACTIVATED, On45005);
            RegisterProtocal(Proto.VIP_CARD_EXPIRED, On45006);
            RegisterProtocal(Proto.RECHARGE_PRODUCT_LIST, On15800);
            RegisterProtocal(Proto.RECHARGE_PRODUCT_UPDATE, On15801);
            RegisterProtocal(Proto.WELFARE_CARD_LIST, On15901);
            // 对标老端 VipController.ResetData 的读侧安全子集：跨天固定重拉 45000→45004→15800。
            // 15901 继续显式查询，15803 尚未迁移；禁止借日切接入领取、购买、UI 或红点链。
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            VipModel.Instance.Reset();
            base.Dispose();
        }

        public void RequestStartup()
        {
            VipModel.Instance.Reset();
            RequestVipInfo();
            RequestPrivilegeCards();
            RequestRechargeProducts();
        }

        public void RequestVipInfo() => SendVipEmpty(Proto.VIP_INFO);

        public void RequestPrivilegeCards() => SendVipEmpty(Proto.VIP_PRIVILEGE_CARD_LIST);

        public void RequestRechargeProducts()
        {
            SendVipEmpty(Proto.RECHARGE_PRODUCT_LIST);
        }

        public void RequestWelfareCards()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.WELFARE_CARD_LIST, null, null);
            if (s_welfareCardOutboundIntercept != null && s_welfareCardOutboundIntercept(frame)) return;
#endif
            SendFmt(Proto.WELFARE_CARD_LIST);
        }

        private void OnServerDayChange()
        {
            RequestVipInfo();
            RequestPrivilegeCards();
            RequestRechargeProducts();
        }

        private void SendVipEmpty(int proto)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(proto, null, null);
            if (s_vipOutboundIntercept != null && s_vipOutboundIntercept(frame)) return;
#endif
            SendFmt(proto);
        }

        private void On45000(NetReader r)
        {
            ushort vipLevel = r.ReadU16();
            uint vipExp = r.ReadU32();
            uint needExp = r.ReadU32();
            byte vipHide = r.ReadU8();

            int gotCount = r.ReadU16();
            var gotRewards = new List<ushort>(gotCount);
            for (int i = 0; i < gotCount; i++) gotRewards.Add(r.ReadU16());

            int canCount = r.ReadU16();
            var canRewards = new List<ushort>(canCount);
            for (int i = 0; i < canCount; i++) canRewards.Add(r.ReadU16());

            int useCardCount = r.ReadU16();
            var useCards = new List<VipModel.UseCard>(useCardCount);
            for (int i = 0; i < useCardCount; i++) useCards.Add(new VipModel.UseCard(r.ReadU8(), r.ReadU32()));

            VipModel.Instance.ReplaceVipInfo(new VipModel.VipInfoSnapshot(vipLevel, vipExp, needExp, vipHide,
                gotRewards, canRewards, useCards));
            GameLog.Info("Vip", "45000 vip info: lv={0} got={1} can={2} useCards={3}",
                vipLevel, gotCount, canCount, useCardCount);
        }

        private void On45004(NetReader r)
        {
            int count = r.ReadU16();
            var cards = new List<VipModel.PrivilegeCard>(count);
            for (int i = 0; i < count; i++)
            {
                cards.Add(new VipModel.PrivilegeCard(r.ReadU8(), r.ReadU8(), r.ReadU8(), r.ReadU8(), r.ReadU32()));
            }

            VipModel.Instance.ReplacePrivilegeCards(cards);
            GameLog.Info("Vip", "45004 privilege cards: {0}", count);
        }

        private void On45005(NetReader r)
        {
            byte cardType = r.ReadU8();
            byte isTempCard = r.ReadU8();
            VipModel.Instance.ReplaceActivationNotice(new VipModel.CardNotice(cardType, isTempCard));
            GameLog.Info("Vip", "45005 card activated: type={0} temp={1}", cardType, isTempCard);
        }

        private void On45006(NetReader r)
        {
            byte cardType = r.ReadU8();
            byte isTempCard = r.ReadU8();
            VipModel.Instance.ReplaceTimeoutNotice(new VipModel.CardNotice(cardType, isTempCard));
            RequestPrivilegeCards();
            GameLog.Info("Vip", "45006 card expired: type={0} temp={1}; refresh 45004", cardType, isTempCard);
        }

        private void On15800(NetReader r)
        {
            int count = r.ReadU16();
            var products = new List<VipModel.RechargeProduct>(count);
            for (int i = 0; i < count; i++)
            {
                int productId = (int)r.ReadU32();
                int returnType = r.ReadU8();
                products.Add(new VipModel.RechargeProduct(productId, returnType));
            }

            VipModel.Instance.SetRechargeProductList(products);
            GameLog.Info("Vip", "15800 recharge products: {0}", count);
        }

        private void On15801(NetReader r)
        {
            int productId = (int)r.ReadU32();
            int returnType = r.ReadU8();
            VipModel.Instance.SetRechargeOneProduct(productId, returnType);
            GameLog.Info("Vip", "15801 recharge product changed: productId={0} returnType={1}", productId, returnType);
        }

        private void On15901(NetReader r)
        {
            int count = r.ReadU16();
            var cards = new List<VipModel.WelfareCard>(count);
            for (int i = 0; i < count; i++)
            {
                cards.Add(new VipModel.WelfareCard(r.ReadU32(), r.ReadU32(), r.ReadU32(), r.ReadU8(), r.ReadU16()));
            }

            VipModel.Instance.ReplaceWelfareCards(cards);
            GameLog.Info("Vip", "15901 welfare cards: {0}", count);
        }
    }
}
