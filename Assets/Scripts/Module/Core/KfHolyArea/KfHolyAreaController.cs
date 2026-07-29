using System;
using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.KfHolyArea
{
    /// <summary>
    /// 神陨禁区284族安全读侧。保留活动窗、原始全量和有证据的推送重查；进入、解锁、
    /// 领奖等场景/资产写操作不开放，也不恢复玩法UI、配置派生、红点或自动战斗。
    /// </summary>
    public sealed class KfHolyAreaController : BaseController
    {
        public static readonly KfHolyAreaController Instance = new KfHolyAreaController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept = null;
#endif
        private KfHolyAreaController() { }

        public const string ICON_TYPE = KfHolyAreaModel.ICON_TYPE;
        private int _lastLevel = -1;

        protected override void Register()
        {
            RegisterProtocal(Proto.KFHOLYAREA_OVERVIEW, On28400);
            RegisterProtocal(Proto.KFHOLYAREA_BUILDING, On28401);
            RegisterProtocal(Proto.KFHOLYAREA_BOSS_DAMAGE, On28403);
            RegisterProtocal(Proto.KFHOLYAREA_SCORE, On28405);
            RegisterProtocal(Proto.KFHOLYAREA_EXIT_ERROR, On28407);
            RegisterProtocal(Proto.KFHOLYAREA_ACT_STATE, On28410);
            RegisterProtocal(Proto.KFHOLYAREA_OCCUPY, On28411);
            RegisterProtocal(Proto.KFHOLYAREA_KILL_LOG, On28412);
            RegisterProtocal(Proto.KFHOLYAREA_BOSS_REFRESH, On28413);
            RegisterProtocal(Proto.KFHOLYAREA_ERROR, On28414);
            RegisterProtocal(Proto.KFHOLYAREA_DEATH_FATIGUE, On28415);
            RegisterProtocal(Proto.KFHOLYAREA_BOSS_LIFE, On28416);
            RegisterProtocal(Proto.KFHOLYAREA_EXIT_COUNTDOWN, On28417);
            RegisterProtocal(Proto.KFHOLYAREA_SCENE_RANK, On28421);
            RegisterProtocal(Proto.KFHOLYAREA_ROLE_RANK, On28422);
            RegisterProtocal(Proto.KFHOLYAREA_BELONG_REFRESH, On28423);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_SERVER_DAY_CHANGE, OnServerDayChange);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            KfHolyAreaModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>老端GAME_START只查询28410；28410回包再严格补查28400→28405。</summary>
        public void RequestStartup() => SendRead(Proto.KFHOLYAREA_ACT_STATE);
        public void RequestOverview() => SendRead(Proto.KFHOLYAREA_OVERVIEW);
        public void RequestBuildingInfo(uint sceneId) => SendRead(Proto.KFHOLYAREA_BUILDING, "i", sceneId);
        public void RequestBossDamage(uint sceneId, uint bossId) =>
            SendRead(Proto.KFHOLYAREA_BOSS_DAMAGE, "ii", sceneId, bossId);
        public void RequestScore() => SendRead(Proto.KFHOLYAREA_SCORE);
        public void RequestKillLog(uint sceneId, uint monsterId) =>
            SendRead(Proto.KFHOLYAREA_KILL_LOG, "ii", sceneId, monsterId);
        public void RequestDeathFatigue() => SendRead(Proto.KFHOLYAREA_DEATH_FATIGUE);
        public void RequestRoleRank(ushort sceneId) => SendRead(Proto.KFHOLYAREA_ROLE_RANK, "h", sceneId);

        private void SendRead(int protocolId, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protocolId, format, args);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(protocolId, format, args);
        }

        private void On28400(NetReader r)
        {
            KfHolyAreaModel.Instance.ReplaceOverview(new KfHolyAreaModel.OverviewSnapshot
            {
                SanctuaryType = r.ReadU8(),
                Servers = r.ReadArray(rr => new KfHolyAreaModel.ServerEntry
                {
                    ServerId = rr.ReadU32(), ServerNum = rr.ReadU16(), ServerName = rr.ReadString(),
                    OpenDay = rr.ReadU16(), Camp = rr.ReadU8()
                })
            });
        }

        private void On28401(NetReader r)
        {
            KfHolyAreaModel.Instance.ReplaceBuilding(new KfHolyAreaModel.BuildingSnapshot
            {
                SceneId = r.ReadU32(), ConstructionType = r.ReadU8(), BelongCamp = r.ReadU32(),
                PreviousBelongCamp = r.ReadU32(), CampScores = r.ReadArray(ReadCampScore),
                BelongRewardState = r.ReadU8(), PersonCount = r.ReadU16(),
                Bosses = r.ReadArray(ReadBoss), RankEntries = r.ReadArray(ReadSceneRank)
            });
        }

        private void On28403(NetReader r)
        {
            KfHolyAreaModel.Instance.ReplaceBossDamage(new KfHolyAreaModel.BossDamageSnapshot
            {
                BossId = r.ReadU32(),
                Entries = r.ReadArray(rr => new KfHolyAreaModel.BossDamageEntry
                {
                    ServerId = rr.ReadU32(), ServerNum = rr.ReadU16(), ServerName = rr.ReadString(),
                    RoleId = rr.ReadU32(), Name = rr.ReadString(), Hurt = rr.ReadU16()
                })
            });
        }

        private void On28405(NetReader r)
        {
            KfHolyAreaModel.Instance.ReplaceScore(new KfHolyAreaModel.ScoreSnapshot
            {
                Score = r.ReadU32(), Cost = r.ReadU8(), Anger = r.ReadU16(),
                Rewards = r.ReadArray(rr => new KfHolyAreaModel.ScoreRewardEntry
                {
                    ScoreConfig = rr.ReadU16(), State = rr.ReadU8()
                })
            });
        }

        private void On28407(NetReader r)
        {
            uint code = r.ReadU32();
            TipsManager.Toast("操作失败(" + code + ")");
            GameLog.Warn("KfHolyArea", "28407 legacy exit code={0}", code);
        }

        private void On28410(NetReader r)
        {
            long actStart = r.ReadU32();
            long actEnd = r.ReadU32();
            KfHolyAreaModel model = KfHolyAreaModel.Instance;
            model.SetActTime(actStart, actEnd);
            RefreshIcon();
            RequestOverview();
            RequestScore();
            GameLog.Info("KfHolyArea", "28410 act_start={0} act_end={1} open={2}",
                actStart, actEnd, model.GetEntranceOpenState());
        }

        private void On28411(NetReader r)
        {
            KfHolyAreaModel.Instance.ReplaceOccupy(new KfHolyAreaModel.OccupyEvent
            {
                SceneId = r.ReadU32(), ConstructionType = r.ReadU8()
            });
            RequestOverview();
        }

        private void On28412(NetReader r)
        {
            KfHolyAreaModel.Instance.ReplaceKillLog(new KfHolyAreaModel.KillLogSnapshot
            {
                SceneId = r.ReadU32(), MonsterId = r.ReadU32(),
                Entries = r.ReadArray(rr => new KfHolyAreaModel.KillLogEntry
                {
                    ServerId = rr.ReadU32(), ServerNum = rr.ReadU32(), RoleId = rr.ReadU32(),
                    RoleName = rr.ReadString(), Time = rr.ReadU32()
                })
            });
        }

        private void On28413(NetReader r) =>
            KfHolyAreaModel.Instance.ReplaceBossRefresh(
                new KfHolyAreaModel.BossRefreshEvent { Code = r.ReadU8() });

        private void On28414(NetReader r)
        {
            uint code = r.ReadU32();
            TipsManager.Toast("操作失败(" + code + ")");
            GameLog.Warn("KfHolyArea", "28414 code={0}", code);
        }

        private void On28415(NetReader r)
        {
            KfHolyAreaModel.Instance.ReplaceDeathFatigue(new KfHolyAreaModel.DeathFatigueSnapshot
            {
                DieTimes = r.ReadU16(), FreeReviveTime = r.ReadU32(), DebuffEndTime = r.ReadU32(),
                SafeTime = r.ReadU32()
            });
        }

        private void On28416(NetReader r) =>
            KfHolyAreaModel.Instance.ReplaceBossLife(new KfHolyAreaModel.BossLifeEvent
            {
                BossId = r.ReadU32(), RebornTime = r.ReadU32()
            });

        private void On28417(NetReader r) =>
            KfHolyAreaModel.Instance.ReplaceExitCountdown(new KfHolyAreaModel.ExitCountdownEvent
            {
                OutTime = r.ReadU32()
            });

        private void On28421(NetReader r)
        {
            KfHolyAreaModel.Instance.ApplySceneRank(new KfHolyAreaModel.SceneRankEvent
            {
                SceneId = r.ReadU32(), Camp = r.ReadU8(), Entries = r.ReadArray(ReadSceneRank)
            });
        }

        private void On28422(NetReader r)
        {
            KfHolyAreaModel.Instance.ReplaceRoleRank(new KfHolyAreaModel.RoleRankSnapshot
            {
                SceneId = r.ReadU16(), Rank = r.ReadU8(), Score = r.ReadU16(), KillScore = r.ReadU16()
            });
        }

        private void On28423(NetReader r)
        {
            ushort sceneId = r.ReadU16();
            KfHolyAreaModel.Instance.ReplaceBelongRefresh(
                new KfHolyAreaModel.BelongRefreshEvent { SceneId = sceneId });
            RequestBuildingInfo(sceneId);
        }

        private static KfHolyAreaModel.CampScoreEntry ReadCampScore(NetReader r) =>
            new KfHolyAreaModel.CampScoreEntry { Camp = r.ReadU8(), Score = r.ReadU16() };

        private static KfHolyAreaModel.BossEntry ReadBoss(NetReader r) =>
            new KfHolyAreaModel.BossEntry
            {
                BossId = r.ReadU32(), MonsterType = r.ReadU8(), BossLevel = r.ReadU16(),
                RebornTime = r.ReadU32()
            };

        private static KfHolyAreaModel.SceneRankEntry ReadSceneRank(NetReader r) =>
            new KfHolyAreaModel.SceneRankEntry
            {
                PlayerId = unchecked((ulong)r.ReadU64()), RoleName = r.ReadString(),
                ServerId = r.ReadU32(), ServerNum = r.ReadU16(), Score = r.ReadU32(),
                KillNum = unchecked((ulong)r.ReadU64()), Rank = r.ReadU8()
            };

        private void RefreshIcon()
        {
            KfHolyAreaModel model = KfHolyAreaModel.Instance;
            if (model.GetEntranceOpenState())
                _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE, 0, model.GetIconStatusText());
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
        }

        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo || role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RequestStartup();
        }

        // 老端GetOpenState返回数组，在JS条件中恒truthy；跨天真实行为是无条件重查28410。
        private void OnServerDayChange()
        {
            RequestStartup();
            RefreshIcon();
        }
    }
}
