using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.HotPoint
{
    /// <summary>
    /// 嗨点只读状态：33300 进游戏活动表，33302/33303 显式键控查询，33305 服务端进度推送。
    /// 33304 是真实领奖事务，刻意不注册、不暴露发送入口。
    /// </summary>
    public sealed class HotPointController : BaseController
    {
        public static readonly HotPointController Instance = new HotPointController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private HotPointController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.HI_POINT_ACTIVITY_LIST, On33300);
            RegisterProtocal(Proto.HI_POINT_DETAIL, On33302);
            RegisterProtocal(Proto.HI_POINT_REWARD_STATUS, On33303);
            RegisterProtocal(Proto.HI_POINT_PROGRESS_PUSH, On33305);
            RegisterProtocal(Proto.HI_POINT_ERROR, On33306);
        }

        /// <summary>33300：严格空帧；老端 GAME_START 唯一自动请求。</summary>
        public void RequestActivityList()
        {
            Send(Proto.HI_POINT_ACTIVITY_LIST, null, null);
        }

        /// <summary>33302：显式查询指定活动的完整明细。</summary>
        public void RequestActivityDetail(ushort baseType, ushort subType)
        {
            Send(Proto.HI_POINT_DETAIL, "hh", new object[] { (int)baseType, (int)subType });
        }

        /// <summary>33303：显式查询指定活动的完整奖励状态。</summary>
        public void RequestRewardStatus(ushort baseType, ushort subType)
        {
            Send(Proto.HI_POINT_REWARD_STATUS, "hh", new object[] { (int)baseType, (int)subType });
        }

        private void Send(int protocol, string format, object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protocol, format, args);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            if (format == null) SendFmt(protocol);
            else SendFmt(protocol, format, args);
        }

        private void On33300(NetReader reader)
        {
            int count = reader.ReadU16();
            var activities = new List<HotPointModel.ActivityInfo>(count);
            for (int i = 0; i < count; i++)
            {
                activities.Add(new HotPointModel.ActivityInfo(
                    reader.ReadU16(), reader.ReadU16(), reader.ReadString(), reader.ReadU32(), reader.ReadU32(), reader.ReadU32()));
            }
            HotPointModel.Instance.ReplaceActivities(activities);
        }

        private void On33302(NetReader reader)
        {
            ushort baseType = reader.ReadU16();
            ushort subType = reader.ReadU16();
            uint sumPoints = reader.ReadU32();
            int count = reader.ReadU16();
            var modules = new List<HotPointModel.DetailItem>(count);
            for (int i = 0; i < count; i++)
            {
                modules.Add(new HotPointModel.DetailItem(
                    reader.ReadU32(), reader.ReadU32(), reader.ReadString(), reader.ReadString(),
                    reader.ReadU16(), reader.ReadU16(), reader.ReadU32(), reader.ReadString(),
                    unchecked((ulong)reader.ReadU64()), reader.ReadU16(), reader.ReadU32(), reader.ReadU32(),
                    reader.ReadString(), reader.ReadU16()));
            }
            HotPointModel.Instance.ReplaceDetail(baseType, subType, sumPoints, modules);
        }

        private void On33303(NetReader reader)
        {
            ushort baseType = reader.ReadU16();
            ushort subType = reader.ReadU16();
            int count = reader.ReadU16();
            var rewards = new List<HotPointModel.RewardItem>(count);
            for (int i = 0; i < count; i++)
            {
                rewards.Add(new HotPointModel.RewardItem(
                    reader.ReadU16(), reader.ReadU8(), reader.ReadU8(), reader.ReadU16(),
                    reader.ReadString(), reader.ReadString(), reader.ReadString(), reader.ReadString()));
            }
            HotPointModel.Instance.ReplaceReward(baseType, subType, rewards);
        }

        private void On33305(NetReader reader)
        {
            ushort baseType = reader.ReadU16();
            ushort subType = reader.ReadU16();
            uint sumPoints = reader.ReadU32();
            int count = reader.ReadU16();
            var modules = new List<HotPointModel.ProgressItem>(count);
            for (int i = 0; i < count; i++)
            {
                modules.Add(new HotPointModel.ProgressItem(
                    reader.ReadU32(), reader.ReadU32(), reader.ReadString(), reader.ReadString(),
                    unchecked((ulong)reader.ReadU64()), reader.ReadU16()));
            }

            HotPointModel.Instance.ApplyProgress(baseType, subType, sumPoints, modules);
            // 对标老端 On33305：进度变化后只重拉同键奖励状态，不触碰领奖事务。
            RequestRewardStatus(baseType, subType);
        }

        private void On33306(NetReader reader)
        {
            HotPointModel.Instance.ReplaceError(reader.ReadU32());
        }

        public override void Dispose()
        {
            HotPointModel.Instance.Reset();
            base.Dispose();
        }
    }
}
