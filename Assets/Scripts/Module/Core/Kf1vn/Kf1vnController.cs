using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Kf1vn
{
    /// <summary>
    /// 诸天王者621族安全读侧。保留启动、阶段重查、原始全量与推送增量；报名、竞猜扣费、
    /// 观战切场景和领奖交易不开放，也不恢复赛事UI、自动战斗、奖励入包或场景生命周期。
    /// </summary>
    public sealed class Kf1vnController : BaseController
    {
        public static readonly Kf1vnController Instance = new Kf1vnController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_exitOutboundIntercept = null;
        private static Func<byte[], bool> s_activityInfoOutboundIntercept = null;
        private static Func<byte[], bool> s_readOutboundIntercept = null;
#endif
        private Kf1vnController() { }

        public const string ICON_TYPE = Kf1vnModel.ICON_TYPE;
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.KF1VN_ACTIVITY_INFO, On62100);
            RegisterProtocal(Proto.KF1VN_STAGE_INFO, On62101);
            RegisterProtocal(Proto.KF1VN_ERROR, On62103);
            RegisterProtocal(Proto.KF1VN_WAIT_INFO, On62104);
            RegisterProtocal(Proto.KF1VN_QUALIFICATION_BATTLE, On62105);
            RegisterProtocal(Proto.KF1VN_QUALIFICATION_RESULT, On62108);
            RegisterProtocal(Proto.KF1VN_QUALIFICATION_SETTLEMENT, On62109);
            RegisterProtocal(Proto.KF1VN_QUALIFICATION_RANK, On62110);
            RegisterProtocal(Proto.KF1VN_LEADER_BATTLE, On62112);
            RegisterProtocal(Proto.KF1VN_LEADER_RESULT, On62113);
            RegisterProtocal(Proto.KF1VN_LEADER_RANK, On62116);
            RegisterProtocal(Proto.KF1VN_QUIZ_INFO, On62117);
            RegisterProtocal(Proto.KF1VN_WAITING_RANK, On62119);
            RegisterProtocal(Proto.KF1VN_LEADER_SETTLEMENT, On62120);
            RegisterProtocal(Proto.KF1VN_QUIZ_RESULT, On62123);
            RegisterProtocal(Proto.KF1VN_QUIZ_ERROR, On62132);
            RegisterProtocal(Proto.KF1VN_QUIZ_HISTORY, On62133);
            RegisterProtocal(Proto.KF1VN_BATTLE_RESULT, On62135);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            Kf1vnModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>老端GAME_START：清模块态后严格请求62101→62133；兑换商店仍归Goods模块。</summary>
        public void RequestStartup()
        {
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            Kf1vnModel.Instance.Reset();
            RequestBaseInfo();
        }

        public void RequestStage() => SendRead(Proto.KF1VN_STAGE_INFO);

        public void RequestActivityInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.KF1VN_ACTIVITY_INFO, null, null);
            if (s_activityInfoOutboundIntercept != null && s_activityInfoOutboundIntercept(frame)) return;
            if (s_readOutboundIntercept != null && s_readOutboundIntercept(frame)) return;
#endif
            SendFmt(Proto.KF1VN_ACTIVITY_INFO);
        }

        public void RequestWaitInfo() => SendRead(Proto.KF1VN_WAIT_INFO);
        public void RequestQualificationBattle() => SendRead(Proto.KF1VN_QUALIFICATION_BATTLE);
        public void RequestQualificationRank(byte area) => SendRead(Proto.KF1VN_QUALIFICATION_RANK, "c", area);
        public void RequestLeaderBattle() => SendRead(Proto.KF1VN_LEADER_BATTLE);
        public void RequestLeaderRank(byte area) => SendRead(Proto.KF1VN_LEADER_RANK, "c", area);
        public void RequestQuizInfo() => SendRead(Proto.KF1VN_QUIZ_INFO);
        public void RequestWaitingRank() => SendRead(Proto.KF1VN_WAITING_RANK);
        public void RequestQuizHistory() => SendRead(Proto.KF1VN_QUIZ_HISTORY);

        /// <summary>62107仅C2S退出请求；服务端从不写同号回执。</summary>
        public void RequestExit()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.KF1VN_EXIT, null, null);
            if (s_exitOutboundIntercept != null && s_exitOutboundIntercept(frame)) return;
#endif
            SendFmt(Proto.KF1VN_EXIT);
        }

        private void RequestBaseInfo()
        {
            RequestStage();
            RequestQuizHistory();
        }

        private void SendRead(int protocolId, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protocolId, format, args);
            if (s_readOutboundIntercept != null && s_readOutboundIntercept(frame)) return;
#endif
            SendFmt(protocolId, format, args);
        }

        private void On62100(NetReader r)
        {
            Kf1vnModel m = Kf1vnModel.Instance;
            m.SetActivityInfo(r.ReadU8(), r.ReadU32(), r.ReadU16(), r.ReadU8());
            RefreshIcon(m);
        }

        private void On62101(NetReader r)
        {
            byte stage = r.ReadU8();
            ushort turn = r.ReadU16();
            uint edtime = r.ReadU32();
            byte subStage = r.ReadU8();
            uint subEdtime = r.ReadU32();

            Kf1vnModel m = Kf1vnModel.Instance;
            bool had = m.HasStageInfo;
            int oldStage = m.Stage;
            m.SetStageInfo(stage, turn, edtime, subStage, subEdtime);

            if (!had) RequestActivityInfo();
            else if (oldStage != stage)
            {
                // 对标旧端Handler62101：阶段变化先更新报名快照，再查等待场景战绩。
                RequestActivityInfo();
                RequestWaitInfo();
            }

            RefreshIcon(m);
            GameLog.Info("Kf1vn", "62101 stage={0} turn={1} sub={2} open={3}",
                stage, turn, subStage, m.GetEntranceOpenState());
        }

        private void On62103(NetReader r)
        {
            uint code = r.ReadU32();
            TipsManager.Toast("操作失败(" + code + ")");
            GameLog.Warn("Kf1vn", "62103 code={0}", code);
        }

        private void On62104(NetReader r)
        {
            Kf1vnModel.Instance.ReplaceWaitInfo(new Kf1vnModel.WaitInfoSnapshot
            {
                LeftTimes = r.ReadU8(), Score = r.ReadU32(), Time = r.ReadU32(), Win = r.ReadU16(),
                Lose = r.ReadU8(), ExpSum = U64(r), DefNum = r.ReadU16()
            });
        }

        private void On62105(NetReader r)
        {
            List<Kf1vnModel.QualificationRole> roles = r.ReadArray(ReadQualificationRole);
            Kf1vnModel.Instance.ReplaceQualificationBattle(new Kf1vnModel.QualificationBattleSnapshot
            {
                Roles = roles, LoadingTime = r.ReadU32(), BattleTime = r.ReadU32()
            });
        }

        private void On62108(NetReader r)
        {
            Kf1vnModel.Instance.ReplaceQualificationResult(new Kf1vnModel.QualificationResultSnapshot
            {
                Result = r.ReadU8(), OldScore = r.ReadU32(), AddScore = r.ReadU16(),
                LeftTimes = r.ReadU8(), IsTimeout = r.ReadU8(), Roles = r.ReadArray(ReadResultRole)
            });
        }

        private void On62109(NetReader r)
        {
            Kf1vnModel.Instance.ReplaceQualificationSettlement(
                new Kf1vnModel.QualificationSettlementSnapshot
                {
                    IsDef = r.ReadU8(), Rank = r.ReadU16(), Score = r.ReadU32(), Award = ReadObjectList(r)
                });
        }

        private void On62110(NetReader r)
        {
            byte area = r.ReadU8();
            Kf1vnModel.Instance.ReplaceQualificationRank(new Kf1vnModel.QualificationRankSnapshot
            {
                Area = area, Entries = r.ReadArray(ReadQualificationRankEntry)
            });
        }

        private void On62112(NetReader r)
        {
            var value = new Kf1vnModel.LeaderBattleSnapshot
            {
                PlayerId = U64(r), Platform = r.ReadString(), ServerNum = r.ReadU16(),
                ServerName = r.ReadString(), Name = r.ReadString(), Career = r.ReadU8(),
                CombatPower = U64(r), Win = r.ReadU16(), Lose = r.ReadU8(), Sex = r.ReadU8(),
                Picture = r.ReadString(), PictureVer = r.ReadU32(), Level = r.ReadU16(),
                Challengers = r.ReadArray(ReadChallenger), LoadingTime = r.ReadU32(), BattleTime = r.ReadU32()
            };
            Kf1vnModel.Instance.ReplaceLeaderBattle(value);
        }

        private void On62113(NetReader r)
        {
            Kf1vnModel.Instance.ReplaceLeaderResult(new Kf1vnModel.LeaderResultSnapshot
            {
                Result = r.ReadU8(), ChallengerNum = r.ReadU8(), TotalChallengerNum = r.ReadU8(),
                RoleId = U64(r), Platform = r.ReadString(), ServerNum = r.ReadU16(),
                Name = r.ReadString(), Career = r.ReadU8(), Sex = r.ReadU8(), Picture = r.ReadString(),
                PictureVer = r.ReadU32(), Level = r.ReadU16(), Hp = U64(r), HpLimit = U64(r),
                Award = ReadObjectList(r)
            });
        }

        private void On62116(NetReader r)
        {
            byte area = r.ReadU8();
            Kf1vnModel.Instance.ReplaceLeaderRank(new Kf1vnModel.LeaderRankSnapshot
            {
                Area = area, Entries = r.ReadArray(ReadLeaderRankEntry), DailyAward = ReadObjectList(r)
            });
        }

        private void On62117(NetReader r)
        {
            Kf1vnModel.Instance.ReplaceQuiz(new Kf1vnModel.QuizSnapshot
            {
                Battles = r.ReadArray(ReadQuizBattle), DefNum = r.ReadU16(), BetNum = r.ReadU8()
            });
        }

        private void On62119(NetReader r)
        {
            Kf1vnModel.Instance.ReplaceWaitingRank(new Kf1vnModel.WaitingRankSnapshot
            {
                Rank = r.ReadU8(), TopName = r.ReadString()
            });
        }

        private void On62120(NetReader r)
        {
            Kf1vnModel.Instance.ReplaceLeaderSettlement(new Kf1vnModel.LeaderSettlementSnapshot
            {
                Rank = r.ReadU8(), Score = r.ReadU16(), Award = ReadObjectList(r), Turn = r.ReadU8()
            });
        }

        private void On62123(NetReader r)
        {
            Kf1vnModel.Instance.ApplyQuizResult(new Kf1vnModel.QuizResultDelta
            {
                BattleId = r.ReadU16(), BattleResult = r.ReadU8(), BetResult = r.ReadU8()
            });
        }

        private void On62132(NetReader r)
        {
            uint code = r.ReadU32();
            string args = r.ReadString();
            TipsManager.Toast("操作失败(" + code + ")");
            GameLog.Warn("Kf1vn", "62132 code={0} args={1}", code, args);
        }

        private void On62133(NetReader r)
        {
            Kf1vnModel.Instance.ReplaceQuizHistory(new Kf1vnModel.QuizHistorySnapshot
            {
                Entries = r.ReadArray(rr => new Kf1vnModel.QuizHistoryEntry
                {
                    Key = U64(rr), Platform = rr.ReadString(), ServerNum = rr.ReadU16(),
                    Name = rr.ReadString(), Race2Turn = rr.ReadU8(), BetCostType = rr.ReadU8(),
                    BetResult = rr.ReadU8(), Status = rr.ReadU8()
                })
            });
        }

        private void On62135(NetReader r)
        {
            Kf1vnModel.Instance.ApplyBattleResult(new Kf1vnModel.BattleResultDelta
            {
                BattleId = r.ReadU16(), BattleResult = r.ReadU8()
            });
        }

        private static Kf1vnModel.QualificationRole ReadQualificationRole(NetReader r)
        {
            return new Kf1vnModel.QualificationRole
            {
                PlayerId = U64(r), Platform = r.ReadString(), ServerNum = r.ReadU16(),
                ServerName = r.ReadString(), Name = r.ReadString(), Career = r.ReadU8(),
                CombatPower = U64(r), Win = r.ReadU16(), Lose = r.ReadU8(), Sex = r.ReadU8(),
                Picture = r.ReadString(), PictureVer = r.ReadU32(), Level = r.ReadU16()
            };
        }

        private static Kf1vnModel.ResultRole ReadResultRole(NetReader r)
        {
            return new Kf1vnModel.ResultRole
            {
                PlayerId = U64(r), Platform = r.ReadString(), ServerNum = r.ReadU16(),
                Name = r.ReadString(), Career = r.ReadU8(), Sex = r.ReadU8(), Picture = r.ReadString(),
                PictureVer = r.ReadU32(), Level = r.ReadU16(), Hp = U64(r), HpLimit = U64(r)
            };
        }

        private static Kf1vnModel.QualificationRankEntry ReadQualificationRankEntry(NetReader r)
        {
            return new Kf1vnModel.QualificationRankEntry
            {
                Rank = r.ReadU8(), PlayerId = U64(r), Platform = r.ReadString(), ServerNum = r.ReadU16(),
                ServerName = r.ReadString(), Name = r.ReadString(), GuildName = r.ReadString(),
                Vip = r.ReadU8(), Score = r.ReadU32(), Win = r.ReadU16(), Lose = r.ReadU8(),
                CombatPower = U64(r), Career = r.ReadU8(), Level = r.ReadU16()
            };
        }

        private static Kf1vnModel.ChallengerEntry ReadChallenger(NetReader r)
        {
            return new Kf1vnModel.ChallengerEntry
            {
                PlayerId = U64(r), Platform = r.ReadString(), ServerNum = r.ReadU16(),
                ServerName = r.ReadString(), Name = r.ReadString(), Career = r.ReadU8(), Turn = r.ReadU8(),
                Sex = r.ReadU8(), Picture = r.ReadString(), PictureVer = r.ReadU32(), Level = r.ReadU16(),
                CombatPower = U64(r)
            };
        }

        private static Kf1vnModel.LeaderRankEntry ReadLeaderRankEntry(NetReader r)
        {
            return new Kf1vnModel.LeaderRankEntry
            {
                Rank = r.ReadU8(), ServerId = r.ReadU16(), PlayerId = U64(r), Platform = r.ReadString(),
                ServerNum = r.ReadU16(), ServerName = r.ReadString(), Name = r.ReadString(),
                GuildName = r.ReadString(), Vip = r.ReadU8(), Score = r.ReadU32(), Turn = r.ReadU8(),
                CombatPower = U64(r), Career = r.ReadU8(), SurvivalTime = r.ReadU16(), Lose = r.ReadU8(),
                Level = r.ReadU16(), Hp = U64(r), HpLimit = U64(r)
            };
        }

        private static Kf1vnModel.QuizBattleEntry ReadQuizBattle(NetReader r)
        {
            return new Kf1vnModel.QuizBattleEntry
            {
                BattleId = r.ReadU16(), Status = r.ReadU8(), PlayerId = U64(r), Platform = r.ReadString(),
                ServerNum = r.ReadU16(), ServerName = r.ReadString(), Name = r.ReadString(),
                Career = r.ReadU8(), Turn = r.ReadU8(), Sex = r.ReadU8(), Level = r.ReadU16(),
                Picture = r.ReadString(), PictureVer = r.ReadU32(), CombatPower = U64(r),
                Challengers = r.ReadArray(ReadQuizChallenger), BattleResult = r.ReadU8(),
                IsBet = r.ReadU8(), BetResult = r.ReadU8()
            };
        }

        private static Kf1vnModel.QuizChallengerEntry ReadQuizChallenger(NetReader r)
        {
            return new Kf1vnModel.QuizChallengerEntry
            {
                PlayerId = U64(r), Platform = r.ReadString(), ServerNum = r.ReadU16(),
                ServerName = r.ReadString(), Name = r.ReadString(), Career = r.ReadU8(), Turn = r.ReadU8(),
                Sex = r.ReadU8(), Level = r.ReadU16(), Picture = r.ReadString(), PictureVer = r.ReadU32(),
                CombatPower = U64(r)
            };
        }

        private static List<Kf1vnModel.ObjectEntry> ReadObjectList(NetReader r) =>
            r.ReadArray(rr => new Kf1vnModel.ObjectEntry
            {
                Type = rr.ReadU8(), TypeId = rr.ReadU32(), Num = rr.ReadU32()
            });

        private static ulong U64(NetReader r) => unchecked((ulong)r.ReadU64());

        private static void RefreshIcon(Kf1vnModel model)
        {
            if (model.GetEntranceOpenState())
                _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE, 0, model.GetIconText());
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
        }

        private void OnServerDayChange()
        {
            MainUIConfigs.FunctionIconCfg cfg = MainUIConfigs.GetFunctionIconCfg(ICON_TYPE);
            if (cfg != null && ServerTimeModel.GetOpenServerDay() < cfg.OpenDay)
                ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
        }

        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo || role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            // 老端等级变化仅重发基础查询，不执行GAME_START的ClearData。
            RequestBaseInfo();
        }
    }
}
