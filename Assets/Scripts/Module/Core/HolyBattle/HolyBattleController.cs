using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.HolyBattle
{
    /// <summary>
    /// 圣灵战场的原始协议切片。21800 只记录服务端错误码，21813 只维护怪物原始增量；
    /// 数据层不推断操作成功，也不接入场景、UI、自动战斗或其他表现层联动。
    /// </summary>
    public sealed class HolyBattleController : BaseController
    {
        public static readonly HolyBattleController Instance = new HolyBattleController();

#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private HolyBattleController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.HOLY_BATTLE_ERROR, On21800);
            RegisterProtocal(Proto.HOLY_BATTLE_INFO, On21801);
            RegisterProtocal(Proto.HOLY_BATTLE_EXPERIENCE, On21804);
            RegisterProtocal(Proto.HOLY_BATTLE_SCORE, On21805);
            RegisterProtocal(Proto.HOLY_BATTLE_RECORD_STATS, On21808);
            RegisterProtocal(Proto.HOLY_BATTLE_DEATH_INFO, On21809);
            RegisterProtocal(Proto.HOLY_BATTLE_RESULT_INFO, On21810);
            RegisterProtocal(Proto.HOLY_BATTLE_PHASE_TIME, On21811);
            RegisterProtocal(Proto.HOLY_BATTLE_FIGHT_STATE, On21807);
            RegisterProtocal(Proto.HOLY_BATTLE_MONSTER_INFO, On21813);
        }

        public void RequestInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.HOLY_BATTLE_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame))
            {
                return;
            }
#endif
            SendFmt(Proto.HOLY_BATTLE_INFO);
        }

        public void RequestMonsterInfo()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.HOLY_BATTLE_MONSTER_INFO, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.HOLY_BATTLE_MONSTER_INFO);
        }

        private void On21800(NetReader reader)
        {
            HolyBattleModel.Instance.SetError(reader.ReadU32());
        }

        private void On21801(NetReader reader)
        {
            byte mod = reader.ReadU8();
            byte status = reader.ReadU8();
            uint endTime = reader.ReadU32();
            int count = reader.ReadU16();
            var servers = new List<HolyBattleModel.ServerEntry>(count);

            for (int i = 0; i < count; i++)
            {
                uint serverId = reader.ReadU32();
                uint serverNumber = reader.ReadU32();
                string serverName = reader.ReadString();
                uint level = reader.ReadU32();
                servers.Add(new HolyBattleModel.ServerEntry(serverId, serverNumber, serverName, level));
            }

            HolyBattleModel.Instance.Replace(mod, status, endTime, servers);
        }

        public void RequestExperience()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.HOLY_BATTLE_EXPERIENCE, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame))
            {
                return;
            }
#endif
            SendFmt(Proto.HOLY_BATTLE_EXPERIENCE);
        }

        private void On21804(NetReader reader)
        {
            HolyBattleModel.Instance.ReplaceExperience(unchecked((ulong)reader.ReadU64()));
        }

        public void RequestScore()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.HOLY_BATTLE_SCORE, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame))
            {
                return;
            }
#endif
            SendFmt(Proto.HOLY_BATTLE_SCORE);
        }

        private void On21805(NetReader reader)
        {
            uint point = reader.ReadU32();
            int count = reader.ReadU16();
            var rewards = new List<HolyBattleModel.RewardEntry>(count);
            for (int i = 0; i < count; i++)
            {
                rewards.Add(new HolyBattleModel.RewardEntry(reader.ReadU16(), reader.ReadU8()));
            }

            HolyBattleModel.Instance.ReplaceScore(point, rewards);
        }

        public void RequestRecordStats()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.HOLY_BATTLE_RECORD_STATS, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.HOLY_BATTLE_RECORD_STATS);
        }

        private void On21808(NetReader reader)
        {
            int groupCount = reader.ReadU16();
            var groups = new List<HolyBattleModel.RecordGroupEntry>(groupCount);
            for (int i = 0; i < groupCount; i++)
            {
                byte groupId = reader.ReadU8(); byte towerNum = reader.ReadU8(); uint point = reader.ReadU32(); byte rank = reader.ReadU8();
                int roleCount = reader.ReadU16(); var roles = new List<HolyBattleModel.RecordRoleEntry>(roleCount);
                for (int j = 0; j < roleCount; j++)
                    roles.Add(new HolyBattleModel.RecordRoleEntry(unchecked((ulong)reader.ReadU64()), reader.ReadU8(), reader.ReadU32(), reader.ReadU32(), reader.ReadString(), reader.ReadU32(), reader.ReadU16(), reader.ReadU16()));
                for (int roleIndex = 1; roleIndex < roles.Count; roleIndex++)
                {
                    HolyBattleModel.RecordRoleEntry current = roles[roleIndex];
                    int insertIndex = roleIndex;
                    while (insertIndex > 0 && roles[insertIndex - 1].Point < current.Point)
                    {
                        roles[insertIndex] = roles[insertIndex - 1];
                        insertIndex--;
                    }

                    roles[insertIndex] = current;
                }
                groups.Add(new HolyBattleModel.RecordGroupEntry(groupId, towerNum, point, rank, roles));
            }
            HolyBattleModel.Instance.ReplaceRecordStats(groups);
        }

        public void RequestPhaseTime()
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.HOLY_BATTLE_PHASE_TIME, null, null);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            SendFmt(Proto.HOLY_BATTLE_PHASE_TIME);
        }

        private void On21811(NetReader reader)
        {
            HolyBattleModel.Instance.ReplacePhaseTime(reader.ReadU8(), reader.ReadU32());
        }

        private void On21807(NetReader reader)
        {
            ushort point = reader.ReadU16(); ushort singleRank = reader.ReadU16(); byte groupRank = reader.ReadU8(); byte anger = reader.ReadU8(); uint angerEnd = reader.ReadU32();
            int count = reader.ReadU16(); var buffs = new List<HolyBattleModel.BuffEntry>(count);
            for (int i = 0; i < count; i++) buffs.Add(new HolyBattleModel.BuffEntry(reader.ReadU16(), reader.ReadU32()));
            HolyBattleModel.Instance.ReplaceFightState(point, singleRank, groupRank, anger, angerEnd, buffs);
        }

        private void On21813(NetReader reader)
        {
            int count = reader.ReadU16();
            var entries = new List<HolyBattleModel.MonsterEntry>(count);
            for (int i = 0; i < count; i++)
            {
                entries.Add(new HolyBattleModel.MonsterEntry(
                    reader.ReadU32(),
                    reader.ReadU32(),
                    reader.ReadU32(),
                    reader.ReadU32(),
                    reader.ReadU8()));
            }

            HolyBattleModel.Instance.ApplyMonsterInfo(entries);
        }

        private void On21809(NetReader reader)
        {
            HolyBattleModel.Instance.ReplaceDeathInfo(
                reader.ReadString(),
                unchecked((ulong)reader.ReadU64()),
                reader.ReadU16(),
                unchecked((ulong)reader.ReadU64()),
                reader.ReadU32(),
                reader.ReadString(),
                reader.ReadU32(),
                reader.ReadU32(),
                reader.ReadU8(),
                reader.ReadU8());
        }

        private void On21810(NetReader reader)
        {
            byte resultCode = reader.ReadU8();
            int groupCount = reader.ReadU16();
            var groups = new List<HolyBattleModel.ResultGroupEntry>(groupCount);
            for (int i = 0; i < groupCount; i++)
            {
                groups.Add(new HolyBattleModel.ResultGroupEntry(reader.ReadU8(), reader.ReadU8(), reader.ReadU32()));
            }

            HolyBattleModel.Instance.ReplaceResultInfo(resultCode, groups, reader.ReadU8(), reader.ReadU8());
        }

        public override void Dispose()
        {
            HolyBattleModel.Instance.Reset();
            base.Dispose();
        }
    }
}
