using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.SeaHegemony
{
    /// <summary>
    /// 四海争霸186与沧溟日常187家族安全读侧。审批、申请、任命、切舰、进退场、搬运、升级、
    /// 加入/退出势力、领奖和特权操作等真实写事务不暴露；只保存原始快照，不接场景、技能、
    /// 自动战斗、UI或本地发奖。
    /// </summary>
    public sealed class SeaHegemonyController : BaseController
    {
        public static readonly SeaHegemonyController Instance = new SeaHegemonyController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif
        private SeaHegemonyController() { }

        public const string ICON_TYPE = SeaHegemonyModel.ICON_TYPE;
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.SEAHEGEMONY_INFO, On18600);
            RegisterProtocal(Proto.SEAHEGEMONY_GUARD, On18601);
            RegisterProtocal(Proto.SEAHEGEMONY_APPLICATIONS, On18604);
            RegisterProtocal(Proto.SEAHEGEMONY_ACTIVITY, On18607);
            RegisterProtocal(Proto.SEAHEGEMONY_GUILDS, On18608);
            RegisterProtocal(Proto.SEAHEGEMONY_MONSTERS, On18609);
            RegisterProtocal(Proto.SEAHEGEMONY_SCORE, On18611);
            RegisterProtocal(Proto.SEAHEGEMONY_RESULT, On18612);
            RegisterProtocal(Proto.SEACRAFT_ERROR_18614, On18614);
            RegisterProtocal(Proto.SEAHEGEMONY_KING, On18615);
            RegisterProtocal(Proto.SEACRAFT_ERROR_18616, On18616);
            RegisterProtocal(Proto.SEAHEGEMONY_SIDES, On18617);
            RegisterProtocal(Proto.SEAHEGEMONY_CAMPS, On18618);
            RegisterProtocal(Proto.SEAHEGEMONY_APPLY_LIMIT, On18622);
            RegisterProtocal(Proto.SEAHEGEMONY_ACTIVITY_NOTICE, On18623);
            RegisterProtocal(Proto.SEAHEGEMONY_NEXT_TIMES, On18624);
            RegisterProtocal(Proto.SEAHEGEMONY_SIGNUP, On18625);
            RegisterProtocal(Proto.SEAHEGEMONY_JOB_NOTICE, On18626);
            RegisterProtocal(Proto.SEAHEGEMONY_PRIVILEGES, On18651);
            RegisterProtocal(Proto.SEAHEGEMONY_MERIT, On18653);
            RegisterProtocal(Proto.SEAHEGEMONY_MEMBERS, On18654);
            RegisterProtocal(Proto.SEAHEGEMONY_DISTRIBUTION, On18655);
            RegisterProtocal(Proto.SEAHEGEMONY_OLD_JOB, On18656);
            RegisterProtocal(Proto.SEACRAFT_DAILY_ERROR, On18700);
            RegisterProtocal(Proto.SEACRAFT_DAILY_OVERVIEW, On18701);
            RegisterProtocal(Proto.SEACRAFT_DAILY_SCENE, On18703);
            RegisterProtocal(Proto.SEACRAFT_DAILY_SEA_RANK, On18704);
            RegisterProtocal(Proto.SEACRAFT_DAILY_CARRY_REWARD, On18710);
            RegisterProtocal(Proto.SEACRAFT_DAILY_ALL_RANK, On18711);
            RegisterProtocal(Proto.SEACRAFT_DAILY_TASKS, On18712);
            RegisterProtocal(Proto.SEACRAFT_DAILY_KICK, On18714);
            RegisterProtocal(Proto.SEACRAFT_DAILY_GUILDS, On18715);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.SetIconRedDot(SeaHegemonyModel.RED_ICON_TYPE, false);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            SeaHegemonyModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>
        /// 镜像旧端GAME_START子序列：18600→18607→18615→18617→18624→18712→18654(1,1)。
        /// 旧端没有ResetData，因此请求前不清任何186/187旧快照。
        /// </summary>
        public void RequestStartup()
        {
            RequestInfo();
            RequestActivity();
            RequestKing();
            RequestSides();
            RequestNextTimes();
            RequestDailyTasks();
            RequestMembers(1, 1);
        }

        public void RequestInfo() => SendRequest(Proto.SEAHEGEMONY_INFO);
        public void RequestGuard() => SendRequest(Proto.SEAHEGEMONY_GUARD);
        public void RequestApplications() => SendRequest(Proto.SEAHEGEMONY_APPLICATIONS);
        public void RequestActivity() => SendRequest(Proto.SEAHEGEMONY_ACTIVITY);
        public void RequestGuilds(uint camp) => SendRequest(Proto.SEAHEGEMONY_GUILDS, "i", camp);
        public void RequestMonsters() => SendRequest(Proto.SEAHEGEMONY_MONSTERS);
        public void RequestScore() => SendRequest(Proto.SEAHEGEMONY_SCORE);
        public void RequestKing() => SendRequest(Proto.SEAHEGEMONY_KING);
        public void RequestSides() => SendRequest(Proto.SEAHEGEMONY_SIDES);
        public void RequestCamps() => SendRequest(Proto.SEAHEGEMONY_CAMPS);
        public void RequestApplyLimit() => SendRequest(Proto.SEAHEGEMONY_APPLY_LIMIT);
        public void RequestNextTimes() => SendRequest(Proto.SEAHEGEMONY_NEXT_TIMES);
        public void RequestSignup() => SendRequest(Proto.SEAHEGEMONY_SIGNUP);
        public void RequestPrivileges() => SendRequest(Proto.SEAHEGEMONY_PRIVILEGES);
        public void RequestMerit() => SendRequest(Proto.SEAHEGEMONY_MERIT);
        public void RequestMembers(ushort pageSize, ushort pageNumber) =>
            SendRequest(Proto.SEAHEGEMONY_MEMBERS, "hh", pageSize, pageNumber);
        public void RequestDistribution() => SendRequest(Proto.SEAHEGEMONY_DISTRIBUTION);
        public void RequestOldJob() => SendRequest(Proto.SEAHEGEMONY_OLD_JOB);
        public void RequestDailyOverview() => SendRequest(Proto.SEACRAFT_DAILY_OVERVIEW);
        public void RequestDailyScene() => SendRequest(Proto.SEACRAFT_DAILY_SCENE);
        public void RequestDailySeaRank(byte seaId) =>
            SendRequest(Proto.SEACRAFT_DAILY_SEA_RANK, "c", seaId);
        public void RequestDailyAllRank() => SendRequest(Proto.SEACRAFT_DAILY_ALL_RANK);
        public void RequestDailyTasks() => SendRequest(Proto.SEACRAFT_DAILY_TASKS);
        public void RequestDailyGuilds() => SendRequest(Proto.SEACRAFT_DAILY_GUILDS);

        private void SendRequest(int protoId, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protoId, format, args);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protoId, format, args);
        }

        private void On18600(NetReader r)
        {
            SeaHegemonyModel.InfoSnapshot snapshot = new SeaHegemonyModel.InfoSnapshot(
                r.ReadU32(), r.ReadU32(), r.ReadU16(), unchecked((ulong)r.ReadU64()),
                r.ReadString(), r.ReadString(), unchecked((ulong)r.ReadU64()),
                unchecked((ulong)r.ReadU64()), r.ReadU16(), r.ReadU8());
            SeaHegemonyModel.Instance.ReplaceInfo(snapshot);
            ActivityIconManager.Instance.SetIconRedDot(
                SeaHegemonyModel.RED_ICON_TYPE, SeaHegemonyModel.Instance.DailyRewardRed);

            // 对标旧端18600 handler：先报名截止；海王再查申请；最后查凌晨4点职位。
            RequestSignup();
            if (snapshot.SelfLevel == 1) RequestApplications();
            RequestOldJob();
        }

        private void On18601(NetReader r)
        {
            ushort limitNumber = r.ReadU16();
            ushort number = r.ReadU16();
            byte hasJoin = r.ReadU8();
            List<SeaHegemonyModel.GuardMember> members = r.ReadArray(rr =>
                new SeaHegemonyModel.GuardMember(rr.ReadU16(), rr.ReadU32(), rr.ReadU16(),
                    unchecked((ulong)rr.ReadU64()), rr.ReadString(), rr.ReadU16(), rr.ReadString(),
                    rr.ReadU16(), unchecked((ulong)rr.ReadU64())));
            SeaHegemonyModel.Instance.ReplaceGuard(
                new SeaHegemonyModel.GuardSnapshot(limitNumber, number, hasJoin, members));
        }

        private void On18604(NetReader r)
        {
            List<SeaHegemonyModel.ApplicationEntry> entries = r.ReadArray(rr =>
                new SeaHegemonyModel.ApplicationEntry(rr.ReadString(), rr.ReadU16(), rr.ReadU16(),
                    unchecked((ulong)rr.ReadU64()), rr.ReadString(), unchecked((ulong)rr.ReadU64())));
            SeaHegemonyModel.Instance.ReplaceApplications(
                new SeaHegemonyModel.ApplicationsSnapshot(entries));
        }

        private void On18607(NetReader r) =>
            SeaHegemonyModel.Instance.ReplaceActivity(new SeaHegemonyModel.ActivitySnapshot(
                r.ReadU8(), r.ReadU8(), r.ReadU32(), r.ReadU32(), r.ReadU8()));

        private void On18608(NetReader r)
        {
            uint camp = r.ReadU32();
            List<SeaHegemonyModel.GuildEntry> guilds = r.ReadArray(rr =>
                new SeaHegemonyModel.GuildEntry(rr.ReadU16(), rr.ReadU32(), rr.ReadU16(),
                    unchecked((ulong)rr.ReadU64()), rr.ReadString(), unchecked((ulong)rr.ReadU64()),
                    rr.ReadString(), unchecked((ulong)rr.ReadU64())));
            SeaHegemonyModel.Instance.ReplaceGuilds(new SeaHegemonyModel.GuildsSnapshot(camp, guilds));
        }

        private void On18609(NetReader r)
        {
            List<SeaHegemonyModel.MonsterEntry> entries = r.ReadArray(rr =>
                new SeaHegemonyModel.MonsterEntry(rr.ReadU32(), unchecked((ulong)rr.ReadU64()),
                    unchecked((ulong)rr.ReadU64()), rr.ReadU8(), rr.ReadU32()));
            SeaHegemonyModel.Instance.ApplyMonsterPacket(entries);
        }

        private void On18611(NetReader r)
        {
            List<SeaHegemonyModel.ScoreGroup> groups = r.ReadArray(rr =>
            {
                ulong guildId = unchecked((ulong)rr.ReadU64());
                string guildName = rr.ReadString();
                byte isAttacker = rr.ReadU8();
                byte guildRank = rr.ReadU8();
                ushort guildScore = rr.ReadU16();
                List<SeaHegemonyModel.ScoreMember> members = rr.ReadArray(mr =>
                    new SeaHegemonyModel.ScoreMember(mr.ReadU16(), unchecked((ulong)mr.ReadU64()),
                        mr.ReadString(), mr.ReadU16(), mr.ReadU16()));
                return new SeaHegemonyModel.ScoreGroup(guildId, guildName, isAttacker,
                    guildRank, guildScore, members);
            });
            SeaHegemonyModel.Instance.ReplaceScore(new SeaHegemonyModel.ScoreSnapshot(groups));
        }

        private void On18612(NetReader r)
        {
            byte status = r.ReadU8();
            ushort guildRank = r.ReadU16();
            ushort selfRank = r.ReadU16();
            List<SeaHegemonyModel.ObjectEntry> rankReward = ReadObjectList(r);
            List<SeaHegemonyModel.ObjectEntry> reward = ReadObjectList(r);
            SeaHegemonyModel.Instance.ReplaceResult(new SeaHegemonyModel.ResultSnapshot(
                status, guildRank, selfRank, rankReward, reward));
        }

        private void On18614(NetReader r)
        {
            uint code = r.ReadU32();
            SeaHegemonyModel.Instance.SetExitResult(code);
            if (code != 1)
            {
                TipsManager.Toast("操作失败(" + code + ")");
                GameLog.Warn("SeaHegemony", "18614 code={0}", code);
            }
        }

        private void On18615(NetReader r)
        {
            uint camp = r.ReadU32();
            uint serverId = r.ReadU32();
            ushort serverNumber = r.ReadU16();
            ulong guildId = unchecked((ulong)r.ReadU64());
            string guildName = r.ReadString();
            ushort times = r.ReadU16();
            uint startTime = r.ReadU32();
            uint endTime = r.ReadU32();
            List<SeaHegemonyModel.KingRewardStatus> statuses = r.ReadArray(rr =>
                new SeaHegemonyModel.KingRewardStatus(rr.ReadU8(), rr.ReadU8()));
            SeaHegemonyModel.Instance.ReplaceKing(new SeaHegemonyModel.KingSnapshot(
                camp, serverId, serverNumber, guildId, guildName, times, startTime, endTime, statuses));
        }

        private void On18616(NetReader r)
        {
            uint code = r.ReadU32();
            SeaHegemonyModel.Instance.SetDivideResult(code);
            if (code != 1)
            {
                TipsManager.Toast("操作失败(" + code + ")");
                GameLog.Warn("SeaHegemony", "18616 code={0}", code);
            }
        }

        private void On18617(NetReader r)
        {
            List<SeaHegemonyModel.SideEntry> attackers = ReadSides(r);
            List<SeaHegemonyModel.SideEntry> defenders = ReadSides(r);
            SeaHegemonyModel.Instance.ReplaceSides(
                new SeaHegemonyModel.SidesSnapshot(attackers, defenders));
        }

        private void On18618(NetReader r)
        {
            List<SeaHegemonyModel.CampEntry> camps = r.ReadArray(rr =>
                new SeaHegemonyModel.CampEntry(rr.ReadU32(), rr.ReadU32(), rr.ReadU16(),
                    unchecked((ulong)rr.ReadU64()), rr.ReadString(), unchecked((ulong)rr.ReadU64()),
                    unchecked((ulong)rr.ReadU64()), rr.ReadString()));
            SeaHegemonyModel.Instance.ReplaceCamps(new SeaHegemonyModel.CampsSnapshot(camps));
        }

        private void On18622(NetReader r) =>
            SeaHegemonyModel.Instance.ReplaceApplyLimit(new SeaHegemonyModel.ApplyLimitSnapshot(
                r.ReadU16(), unchecked((ulong)r.ReadU64()), r.ReadU8()));

        private void On18623(NetReader r)
        {
            uint code = r.ReadU32();
            SeaHegemonyModel.Instance.ReplaceActivityNotice(
                new SeaHegemonyModel.ActivityNoticeSnapshot(code));
            if (code == 0) return;
            RequestActivity();
            RequestNextTimes();
            RequestSignup();
        }

        private void On18624(NetReader r)
        {
            List<SeaHegemonyModel.ActivityTimeEntry> times = r.ReadArray(rr =>
                new SeaHegemonyModel.ActivityTimeEntry(rr.ReadU8(), rr.ReadU32(), rr.ReadU32()));
            SeaHegemonyModel.Instance.ReplaceActivityTimes(
                new SeaHegemonyModel.ActivityTimesSnapshot(times));
        }

        private void On18625(NetReader r)
        {
            SeaHegemonyModel.Instance.SetSignupEndTime(r.ReadU32());
            RefreshIcon();
        }

        private void On18626(NetReader r)
        {
            byte code = r.ReadU8();
            SeaHegemonyModel.Instance.ReplaceJobNotice(new SeaHegemonyModel.JobNoticeSnapshot(code));
            if (code != 0) RequestInfo();
        }

        private void On18651(NetReader r)
        {
            List<SeaHegemonyModel.PrivilegeEntry> privileges = r.ReadArray(rr =>
            {
                ushort privilegeId = rr.ReadU16();
                ushort remainingNumber = rr.ReadU16();
                byte status = rr.ReadU8();
                ulong endTime = unchecked((ulong)rr.ReadU64());
                List<ushort> needJobs = rr.ReadArray(jr => jr.ReadU16());
                return new SeaHegemonyModel.PrivilegeEntry(
                    privilegeId, remainingNumber, status, endTime, needJobs);
            });
            SeaHegemonyModel.Instance.ReplacePrivileges(
                new SeaHegemonyModel.PrivilegesSnapshot(privileges));
        }

        private void On18653(NetReader r) =>
            SeaHegemonyModel.Instance.ReplaceMerit(
                new SeaHegemonyModel.MeritSnapshot(r.ReadU16(), r.ReadU32()));

        private void On18654(NetReader r)
        {
            ushort pageTotal = r.ReadU16();
            ushort pageSize = r.ReadU16();
            ushort pageNumber = r.ReadU16();
            List<SeaHegemonyModel.MemberEntry> members = r.ReadArray(rr =>
                new SeaHegemonyModel.MemberEntry(rr.ReadU16(), unchecked((ulong)rr.ReadU64()),
                    rr.ReadString(), rr.ReadU32(), rr.ReadU16(), rr.ReadU32(),
                    unchecked((ulong)rr.ReadU64()), rr.ReadU32(), rr.ReadString()));
            SeaHegemonyModel.Instance.ReplaceMemberPage(new SeaHegemonyModel.MemberPageSnapshot(
                pageTotal, pageSize, pageNumber, members));
        }

        private void On18655(NetReader r)
        {
            List<SeaHegemonyModel.DistributionEntry> guilds = r.ReadArray(rr =>
                new SeaHegemonyModel.DistributionEntry(rr.ReadU32(), rr.ReadU32(), rr.ReadString(),
                    unchecked((ulong)rr.ReadU64()), rr.ReadString(), unchecked((ulong)rr.ReadU64()),
                    rr.ReadU32()));
            SeaHegemonyModel.Instance.ReplaceDistribution(
                new SeaHegemonyModel.DistributionSnapshot(guilds));
        }

        private void On18656(NetReader r) => SeaHegemonyModel.Instance.SetOldJob(r.ReadU16());

        private void On18700(NetReader r)
        {
            uint code = r.ReadU32();
            SeaHegemonyModel.Instance.SetDailyError(code);
            TipsManager.Toast("操作失败(" + code + ")");
            GameLog.Warn("SeaHegemony", "18700 日常错误壳 code={0}", code);
        }

        private void On18701(NetReader r)
        {
            List<SeaHegemonyModel.DailySeaEntry> seas = r.ReadArray(rr =>
                new SeaHegemonyModel.DailySeaEntry(rr.ReadU8(), rr.ReadU32(), rr.ReadU32(),
                    rr.ReadU32(), rr.ReadU8()));
            SeaHegemonyModel.Instance.ReplaceDailyOverview(
                new SeaHegemonyModel.DailyOverviewSnapshot(seas));
        }

        private void On18703(NetReader r)
        {
            uint seaId = r.ReadU32();
            uint brickNumber = r.ReadU32();
            ushort carryCount = r.ReadU16();
            ushort defendCount = r.ReadU16();
            List<SeaHegemonyModel.DailyBossEntry> bosses = r.ReadArray(rr =>
                new SeaHegemonyModel.DailyBossEntry(
                    rr.ReadU32(), rr.ReadU16(), rr.ReadString(), rr.ReadU32()));
            SeaHegemonyModel.Instance.ReplaceDailyScene(
                new SeaHegemonyModel.DailySceneSnapshot(
                    seaId, brickNumber, carryCount, defendCount, bosses));
        }

        private void On18704(NetReader r)
        {
            uint seaId = r.ReadU32();
            uint myBrickNumber = r.ReadU32();
            uint myRank = r.ReadU32();
            ulong myPower = unchecked((ulong)r.ReadU64());
            byte myPosition = r.ReadU8();
            List<SeaHegemonyModel.DailySeaRankEntry> ranks = r.ReadArray(rr =>
                new SeaHegemonyModel.DailySeaRankEntry(rr.ReadU8(), rr.ReadU32(),
                    rr.ReadString(), unchecked((ulong)rr.ReadU64()), rr.ReadU32()));
            SeaHegemonyModel.Instance.ReplaceDailySeaRank(
                new SeaHegemonyModel.DailySeaRankSnapshot(
                    seaId, myBrickNumber, myRank, myPower, myPosition, ranks));
        }

        private void On18710(NetReader r) =>
            SeaHegemonyModel.Instance.ReplaceDailyCarryReward(
                new SeaHegemonyModel.DailyCarryRewardSnapshot(r.ReadU8(), ReadObjectList(r)));

        private void On18711(NetReader r)
        {
            uint myBrickNumber = r.ReadU32();
            byte mySea = r.ReadU8();
            uint myRank = r.ReadU32();
            ulong myPower = unchecked((ulong)r.ReadU64());
            byte myPosition = r.ReadU8();
            List<SeaHegemonyModel.DailyAllRankEntry> ranks = r.ReadArray(rr =>
                new SeaHegemonyModel.DailyAllRankEntry(rr.ReadU8(), rr.ReadU8(), rr.ReadU32(),
                    rr.ReadString(), unchecked((ulong)rr.ReadU64()), rr.ReadU32()));
            SeaHegemonyModel.Instance.ReplaceDailyAllRank(
                new SeaHegemonyModel.DailyAllRankSnapshot(
                    myBrickNumber, mySea, myRank, myPower, myPosition, ranks));
        }

        private void On18712(NetReader r)
        {
            List<SeaHegemonyModel.DailyTaskEntry> tasks = r.ReadArray(rr =>
                new SeaHegemonyModel.DailyTaskEntry(rr.ReadU8(), rr.ReadU16(), rr.ReadU8()));
            SeaHegemonyModel.Instance.ReplaceDailyTasks(
                new SeaHegemonyModel.DailyTasksSnapshot(tasks));
        }

        private void On18714(NetReader r) =>
            SeaHegemonyModel.Instance.ReplaceDailyKick(
                new SeaHegemonyModel.DailyKickSnapshot(r.ReadU8()));

        private void On18715(NetReader r)
        {
            List<SeaHegemonyModel.DailyGuildEntry> seas = r.ReadArray(rr =>
                new SeaHegemonyModel.DailyGuildEntry(
                    rr.ReadU8(), unchecked((ulong)rr.ReadU64()), rr.ReadString()));
            SeaHegemonyModel.Instance.ReplaceDailyGuilds(
                new SeaHegemonyModel.DailyGuildsSnapshot(seas));
        }

        private void RefreshIcon()
        {
            SeaHegemonyModel model = SeaHegemonyModel.Instance;
            bool open = model.GetEntranceOpenState();
            if (open) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE, 0, model.GetIconText());
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            GameLog.Info("SeaHegemony", "18625 四海争霸: camp={0} endTime={1} open={2}",
                model.Camp, model.SignupEndTime, open);
        }

        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo || role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            // 这是Unity现有图标门补拉，只查18600；不得把完整GAME_START六包放大到每次等级变化。
            RequestInfo();
        }

        private static List<SeaHegemonyModel.ObjectEntry> ReadObjectList(NetReader r) =>
            r.ReadArray(rr => new SeaHegemonyModel.ObjectEntry(rr.ReadU8(), rr.ReadU32(), rr.ReadU32()));

        private static List<SeaHegemonyModel.SideEntry> ReadSides(NetReader r) =>
            r.ReadArray(rr => new SeaHegemonyModel.SideEntry(rr.ReadU32(), rr.ReadU16(),
                unchecked((ulong)rr.ReadU64()), rr.ReadString()));
    }
}
