using System;
using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.PushGift
{
    /// <summary>
    /// 礼包推送控制器(对标老客户端 PushGiftController,模块 191)。进游戏(GAME_START)先发 19104
    /// (领取离线过期礼包,一次性,服务端回 19101 type3),再发 19101(拉当前激活礼包列表)。回包 19101
    /// 据礼包列表增删主界面图标 191(有未过期礼包→AddIconAsync(191,倒计时=最近结束时间),否则 DeleteIcon)。
    /// 图标经配置门(open_lv/open_day)过滤,故用 AddIconAsync(对标老端走 config-gated addIcon(191,...))。
    /// 等级变化(EVT_ROLE_INFO_UPDATE)时复请求 19101,让升级开启配置门后图标及时出现。
    /// 19102 仅提供显式详情快照；购买/激活弹窗(19103/PushGiftTips 等)与红点仍不接。
    /// </summary>
    public sealed class PushGiftController : BaseController
    {
        public static readonly PushGiftController Instance = new PushGiftController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private PushGiftController() { }

        public const string ICON_TYPE = PushGiftModel.ICON_TYPE;

        // 复请求 19101 的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发。
        private int _lastLevel = -1;

        protected override void Register()
        {
            // 只有 19101(激活礼包列表)携带图标数据;19104 的回包也复用 19101 下发(type3 离线过期)。
            RegisterProtocal(Proto.PUSHGIFT_LIST, On19101);
            RegisterProtocal(Proto.PUSHGIFT_DETAIL, On19102);
            // 对标老端 GAME_START 后主角升级:复请求 19101(升级开启配置门后图标及时出现)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            PushGiftModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START 发 19104+19101)。</summary>
        public void RequestStartup()
        {
            SendEmpty(Proto.PUSHGIFT_OFFLINE); // 19104 领取离线过期礼包(一次性,服务端回 19101 type3)
            RequestGiftList();               // 19101 拉当前激活礼包列表
        }

        // 拉当前激活礼包列表(bare,pt_191 read(19101,_)->{ok,[]})。
        private void RequestGiftList()
        {
            SendEmpty(Proto.PUSHGIFT_LIST);
        }

        /// <summary>请求指定礼包详情（19102，显式调用；不绑定 GAME_START）。</summary>
        public void RequestGiftDetail(ushort giftId, ushort subId)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.PUSHGIFT_DETAIL, "hh", new object[] { (int)giftId, (int)subId });
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.PUSHGIFT_DETAIL, "hh", (int)giftId, (int)subId);
        }

        private void SendEmpty(int protocol)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protocol, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protocol);
        }

        // 19101: type:c, gift_list[u16×{gift_id:h, sub_id:h, title_name:str, gift_name:str, end_time:i, infos:str}]
        private void On19101(NetReader r)
        {
            int type = r.ReadU8();
            int count = r.ReadU16();
            var incoming = new List<PushGiftModel.GiftEntry>(count);
            for (int i = 0; i < count; i++)
            {
                int giftId = r.ReadU16();
                int subId = r.ReadU16();
                r.ReadString();            // title_name(标题,面板用,本期不存)
                r.ReadString();            // gift_name(礼包名,面板用,本期不存)
                long endTime = r.ReadU32();
                r.ReadString();            // infos(激活条件,面板/弹窗用,本期不存)
                incoming.Add(new PushGiftModel.GiftEntry(giftId, subId, endTime));
            }

            PushGiftModel m = PushGiftModel.Instance;

            // 对标老端 On19101:空列表下发时,本地已无礼包则啥也不做,否则摘掉图标。
            if (count == 0)
            {
                if (!m.IsGiftListEmpty()) ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
                EventDispatcher.Emit(GlobalEvent.EVT_PUSH_GIFT_UPDATE);
                return;
            }

            m.SetGiftList(incoming);              // 累加未过期礼包(对标 SetGiftList)
            if (type == 4) m.RemoveGifts(incoming); // type4 立即清理(对标 DeleteGiftList)

            // 还有可购礼包 → 挂图标(倒计时=最近结束时间);否则摘掉。
            if (m.GetEntranceOpenState())
                _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE, m.NextEndTime());
            else
                ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);

            EventDispatcher.Emit(GlobalEvent.EVT_PUSH_GIFT_UPDATE);
            GameLog.Info("PushGift", "19101 礼包推送: type={0} count={1} open={2} nextEnd={3}",
                type, count, m.GetEntranceOpenState(), m.NextEndTime());
        }

        // 19102: gift_id:h, sub_id:h, gift_name:s, end_time:i, conditions:s,
        // reward_list[h×{grade_id:h, grade_name:s, buy_cnt:c, buy_time:i, rewards_conditions:s, rewards:s}]
        private void On19102(NetReader r)
        {
            int giftId = r.ReadU16();
            int subId = r.ReadU16();
            string giftName = r.ReadString();
            uint endTime = r.ReadU32();
            string conditions = r.ReadString();
            int count = r.ReadU16();
            var rewards = new List<PushGiftModel.RewardEntry>(count);
            for (int i = 0; i < count; i++)
            {
                rewards.Add(new PushGiftModel.RewardEntry(
                    r.ReadU16(), r.ReadString(), r.ReadU8(), r.ReadU32(), r.ReadString(), r.ReadString()));
            }
            PushGiftModel.Instance.ReplaceGiftDetail(giftId, subId, giftName, endTime, conditions, rewards);
            GameLog.Info("PushGift", "19102 礼包详情: gift={0}@{1} rewards={2} remaining={3}B",
                giftId, subId, count, r.Remaining);
        }

        // 对标老端:主角等级变化复请求 19101(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时发)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestGiftList();
        }
    }
}
