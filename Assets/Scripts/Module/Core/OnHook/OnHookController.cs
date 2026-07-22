using Shenxiao.Common.Tips;
using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.OnHook
{
    /// <summary>
    /// 挂机收益协议控制器(薄增量六件套第20轮工单;服务端 pt_132 段内 13216)。
    /// 解主线 101211(ctype91「领取1次挂机收益」,唯一事件计数型:领一次即完成,无需专用任务代码)。
    /// 回包 schema 摘自 ClientProtocol.json "13216":errcode:i, old_lv:h, old_lv_ratio:h,
    /// goods_list[u16×{style:c, typeId:i, count:l}]；13211/13212/13214快照同由本控制器接收。
    /// </summary>
    public sealed class OnHookController : BaseController
    {
        public static readonly OnHookController Instance = new OnHookController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private OnHookController() { }

        /// <summary>挂机时长上限(秒;对标老端 OutLineModel.max_outline_time = 20*3600+onhook_time)。
        /// 数据来源 18401(模块加成列表 key==2),由 <see cref="Skill.SkillTalentModel"/> 解析后写入,
        /// 与本模块 13216(领取挂机收益)配套——领取上限/挂机时长展示可读它,当前 13216 本身不消费,先留字段。</summary>
        public static long MaxOnlineTimeSec { get; private set; }

        public static void SetMaxOnlineTimeSec(long sec) => MaxOnlineTimeSec = sec;

        protected override void Register()
        {
            RegisterProtocal(Proto.ONHOOK_TICK, On13211);
            RegisterProtocal(Proto.ONHOOK_INFO, On13212);
            RegisterProtocal(Proto.ONHOOK_TIME_UPDATE, On13214);
            RegisterProtocal(Proto.ONHOOK_EXP_EFFECT, On13215);
            RegisterProtocal(Proto.ONHOOK_RECEIVE, On13216);
        }

        /// <summary>13212 挂机收益信息(C2S 空包)。</summary>
        public void RequestInfo() => SendEmpty(Proto.ONHOOK_INFO);

        private static void On13211(NetReader r)
        {
            int errorCode = unchecked((int)r.ReadU32());
            int nextTime = unchecked((int)r.ReadU32());
            int hadAfkTime = unchecked((int)r.ReadU32());
            OnHookModel.Instance.ApplyTick(errorCode, nextTime, hadAfkTime);
            GameLog.Info("OnHook", "13211 code={0} remaining={1}B", errorCode, r.Remaining);
        }

        private void SendEmpty(int protoId)
        {
#if UNITY_EDITOR
            if (s_outboundIntercept != null && s_outboundIntercept(UserMsgAdapter.Encode(protoId, null, null))) return;
#endif
            SendFmt(protoId);
        }

        private static void On13212(NetReader r)
        {
            byte loginType = r.ReadU8();
            ushort offLevel = r.ReadU16();
            int costAfkTime = unchecked((int)r.ReadU32());
            var rewards = r.ReadArray(ReadReward);
            int backCount = unchecked((int)r.ReadU32());
            long backExp = r.ReadU64();
            int afkTime = unchecked((int)r.ReadU32());
            int nextTime = unchecked((int)r.ReadU32());
            long expEffect = r.ReadU64();
            int hadAfkTime = unchecked((int)r.ReadU32());
            OnHookModel.Instance.ApplyInfo(loginType, offLevel, costAfkTime, rewards, backCount, backExp, afkTime,
                nextTime, expEffect, hadAfkTime);
            GameLog.Info("OnHook", "13212 rewards={0} remaining={1}B", rewards.Count, r.Remaining);
        }

        private static void On13214(NetReader r)
        {
            int afkTime = unchecked((int)r.ReadU32());
            int nextTime = unchecked((int)r.ReadU32());
            OnHookModel.Instance.ApplyTime(afkTime, nextTime);
            GameLog.Info("OnHook", "13214 remaining={0}B", r.Remaining);
        }

        /// <summary>13215 仅服务端推送的挂机经验效率，完整读取 u64；不提供主动请求或轮询入口。</summary>
        private static void On13215(NetReader r)
        {
            long expEffect = r.ReadU64();
            OnHookModel.Instance.ApplyExpEffect(expEffect);
            GameLog.Info("OnHook", "13215 exp_effect={0} remaining={1}B", expEffect, r.Remaining);
        }

        /// <summary>13216 领取挂机收益(C2S 无参)。</summary>
        public void Receive()
        {
            SendEmpty(Proto.ONHOOK_RECEIVE);
            GameLog.Info("OnHook", "receive 13216");
        }

        public override void Dispose()
        {
            OnHookModel.Instance.Reset();
            base.Dispose();
        }

        /// <summary>13216 回包(对标 ClientProtocol.json "13216" 原文按序读完):
        /// errcode:i, old_lv:h, old_lv_ratio:h, goods_list[u16×{style:c, typeId:i, count:l}]。
        /// errcode==1 → toast「挂机收益已领取」;else 显码降级(错误码表未移植)。</summary>
        private void On13216(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int oldLv = r.ReadU16();
            int oldLvRatio = r.ReadU16();
            var goodsList = r.ReadArray(ReadGoods);
            if (errcode != 1)
            {
                TipsManager.Toast("领取失败(" + errcode + ")");   // 错误码表未移植,显码降级
                GameLog.Info("OnHook", "13216 fail errcode={0}", errcode);
                return;
            }
            // 服务端 receive_afk_reward 成功后主动紧跟 13212；客户端不得重复请求。
            TipsManager.Toast("挂机收益已领取");
            GameLog.Info("OnHook", "13216 ok old_lv={0} old_lv_ratio={1} goods={2} remaining={3}B",
                oldLv, oldLvRatio, goodsList.Count, r.Remaining);
        }

        private static (int style, int typeId, long count) ReadGoods(NetReader r)
        {
            return (r.ReadU8(), (int)r.ReadU32(), r.ReadU64());   // {style:c, typeId:i, count:l}
        }

        private static OnHookModel.Reward ReadReward(NetReader r)
        {
            return new OnHookModel.Reward(r.ReadU8(), unchecked((int)r.ReadU32()), r.ReadU64());
        }
    }

    /// <summary>挂机收益 13211/13212/13214 的服务端快照；View 只订阅本模型，不持有协议数据。</summary>
    public sealed class OnHookModel
    {
        public readonly struct Reward
        {
            public Reward(byte type, int goodsId, long num) { Type = type; GoodsId = goodsId; Num = num; }
            public byte Type { get; }
            public int GoodsId { get; }
            public long Num { get; }
        }

        public static readonly OnHookModel Instance = new OnHookModel();
        private readonly List<Reward> _rewards = new List<Reward>();
        public event Action Changed;
        public int TotalAfkTime { get; private set; }
        public int CostAfkTime { get; private set; }
        public int RemainingAfkTime { get; private set; }
        public int NextTime { get; private set; }
        public long ExpEffect { get; private set; }
        public byte LoginType { get; private set; }
        public ushort OffLevel { get; private set; }
        public int BackCount { get; private set; }
        public long BackExp { get; private set; }
        public IReadOnlyList<Reward> Rewards => _rewards;

        public void ApplyInfo(byte loginType, ushort offLevel, int costAfkTime, List<Reward> rewards, int backCount,
            long backExp, int afkTime, int nextTime, long expEffect, int hadAfkTime)
        {
            LoginType = loginType; OffLevel = offLevel; CostAfkTime = costAfkTime; RemainingAfkTime = afkTime;
            NextTime = nextTime; ExpEffect = expEffect; TotalAfkTime = hadAfkTime; BackCount = backCount; BackExp = backExp;
            _rewards.Clear();
            if (rewards != null) _rewards.AddRange(rewards);
            Changed?.Invoke();
        }

        public void ApplyTick(int errorCode, int nextTime, int hadAfkTime)
        {
            NextTime = nextTime;
            if (errorCode == 1) TotalAfkTime = hadAfkTime;
            Changed?.Invoke();
        }

        public void ApplyTime(int afkTime, int nextTime)
        {
            RemainingAfkTime = afkTime; NextTime = nextTime;
            Changed?.Invoke();
        }

        /// <summary>13215 是独立增量推送，绝不能覆盖 13212 的奖励和其他快照字段。</summary>
        public void ApplyExpEffect(long expEffect)
        {
            ExpEffect = expEffect;
            Changed?.Invoke();
        }

        public void Reset()
        {
            TotalAfkTime = CostAfkTime = RemainingAfkTime = NextTime = BackCount = 0;
            BackExp = ExpEffect = 0; LoginType = 0; OffLevel = 0; _rewards.Clear();
            Changed?.Invoke();
        }
    }
}
