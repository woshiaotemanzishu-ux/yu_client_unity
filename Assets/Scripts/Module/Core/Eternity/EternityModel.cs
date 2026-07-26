using System.Collections.Generic;

namespace Shenxiao.Module.Core.Eternity
{
    public sealed class EternityModel
    {
        public static readonly EternityModel Instance = new EternityModel();

        public uint OpenTime { get; private set; }
        public uint EnterTime { get; private set; }
        public uint EndTime { get; private set; }
        public bool HasData { get; private set; }
        private readonly List<JoinEntry> _joinList = new List<JoinEntry>();
        private readonly IReadOnlyList<JoinEntry> _readOnlyJoinList;
        public bool HasJoinInfo { get; private set; }
        public byte CanEnterScene { get; private set; }
        public IReadOnlyList<JoinEntry> JoinList => _readOnlyJoinList;
        public ushort DieTimes { get; private set; }
        public uint Time { get; private set; }
        public uint DieTime { get; private set; }
        public uint SafeTime { get; private set; }
        public bool HasReliveInfo { get; private set; }

        public sealed class JoinEntry
        {
            public uint Scene { get; }
            public ushort SelfServerNum { get; }
            public ushort SceneNum { get; }
            public JoinEntry(uint scene, ushort selfServerNum, ushort sceneNum) { Scene = scene; SelfServerNum = selfServerNum; SceneNum = sceneNum; }
        }

        private EternityModel()
        {
            _readOnlyJoinList = _joinList.AsReadOnly();
        }

        public void Replace(uint openTime, uint enterTime, uint endTime)
        {
            OpenTime = openTime;
            EnterTime = enterTime;
            EndTime = endTime;
            HasData = true;
        }

        public void ReplaceJoinInfo(byte canEnterScene, List<JoinEntry> joinList)
        {
            CanEnterScene = canEnterScene;
            _joinList.Clear();
            if (joinList != null) _joinList.AddRange(joinList);
            HasJoinInfo = true;
        }

        public void ReplaceReliveInfo(ushort dieTimes, uint time, uint dieTime, uint safeTime)
        {
            DieTimes = dieTimes;
            Time = time;
            DieTime = dieTime;
            SafeTime = safeTime;
            HasReliveInfo = true;
        }

        public void Reset()
        {
            OpenTime = 0;
            EnterTime = 0;
            EndTime = 0;
            HasData = false;
            CanEnterScene = 0;
            _joinList.Clear();
            HasJoinInfo = false;
            DieTimes = 0;
            Time = 0;
            DieTime = 0;
            SafeTime = 0;
            HasReliveInfo = false;
        }
    }
}
