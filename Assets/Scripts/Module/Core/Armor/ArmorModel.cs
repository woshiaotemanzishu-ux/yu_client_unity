using System.Collections.Generic;

namespace Shenxiao.Module.Core.Armor
{
    /// <summary>14401 全量圣骸树与 14402 打造结果；14402 成功只替换服务端返回的 stage/type 权威切片。</summary>
    public sealed class ArmorModel
    {
        public sealed class PositionEntry
        {
            public uint GTypeId { get; }
            public byte Position { get; }
            public byte Status { get; }
            public PositionEntry(uint gTypeId, byte position, byte status) { GTypeId = gTypeId; Position = position; Status = status; }
        }

        public sealed class TypeEntry
        {
            public byte Type { get; }
            public byte Status { get; }
            public IReadOnlyList<PositionEntry> Positions { get; }
            public TypeEntry(byte type, byte status, List<PositionEntry> positions)
            {
                Type = type;
                positions = positions ?? new List<PositionEntry>();
                positions.Sort((a, b) => a.Position.CompareTo(b.Position));
                Status = status;
                Positions = positions;
            }
        }

        public sealed class StageEntry
        {
            public byte Stage { get; }
            public IReadOnlyList<TypeEntry> Types { get; }
            public StageEntry(byte stage, List<TypeEntry> types)
            {
                Stage = stage;
                types = types ?? new List<TypeEntry>();
                types.Sort((a, b) => a.Type.CompareTo(b.Type));
                Types = types;
            }
        }

        public static readonly ArmorModel Instance = new ArmorModel();
        private readonly List<StageEntry> _stages = new List<StageEntry>();
        private ArmorModel() { }

        public IReadOnlyList<StageEntry> Stages => _stages;
        public bool HasData { get; private set; }
        public bool HasMakeResult { get; private set; }
        public uint LastMakeCode { get; private set; }
        public int Version { get; private set; }

        /// <summary>14401 的每个包都是全量树，必须替换并允许空包清旧。</summary>
        public void ReplaceData(List<StageEntry> stages)
        {
            _stages.Clear();
            if (stages != null)
            {
                stages.Sort((a, b) => a.Stage.CompareTo(b.Stage));
                _stages.AddRange(stages);
            }
            HasData = true;
            Version++;
        }

        /// <summary>
        /// 记录每个 14402 回执；仅 code==1 时把回包内 stage/type 切片合并进 14401 树。
        /// 失败（含空列表）永远保留旧快照，禁止乐观置位或清空。
        /// </summary>
        public bool ApplyMakeResult(uint code, List<StageEntry> delta)
        {
            HasMakeResult = true;
            LastMakeCode = code;
            if (code != 1) return false;

            if (delta != null)
            {
                for (int i = 0; i < delta.Count; i++) MergeStage(delta[i]);
                _stages.Sort((a, b) => a.Stage.CompareTo(b.Stage));
            }
            Version++;
            return true;
        }

        public StageEntry FindStage(byte stage)
        {
            for (int i = 0; i < _stages.Count; i++) if (_stages[i].Stage == stage) return _stages[i];
            return null;
        }

        public TypeEntry FindType(byte stage, byte type)
        {
            StageEntry stageEntry = FindStage(stage);
            if (stageEntry == null) return null;
            for (int i = 0; i < stageEntry.Types.Count; i++) if (stageEntry.Types[i].Type == type) return stageEntry.Types[i];
            return null;
        }

        public PositionEntry FindPosition(byte stage, byte type, byte position)
        {
            TypeEntry typeEntry = FindType(stage, type);
            if (typeEntry == null) return null;
            for (int i = 0; i < typeEntry.Positions.Count; i++) if (typeEntry.Positions[i].Position == position) return typeEntry.Positions[i];
            return null;
        }

        public bool IsMade(byte stage, byte type, byte position) => FindPosition(stage, type, position)?.Status == 1;
        public bool IsTypeComplete(byte stage, byte type) => FindType(stage, type)?.Status == 1;

        public bool IsStageComplete(byte stage)
        {
            StageEntry entry = FindStage(stage);
            if (entry == null || entry.Types.Count == 0) return false;
            for (int i = 0; i < entry.Types.Count; i++) if (entry.Types[i].Status != 1) return false;
            return true;
        }

        public void Reset()
        {
            _stages.Clear();
            HasData = false;
            HasMakeResult = false;
            LastMakeCode = 0;
            Version++;
        }

        private void MergeStage(StageEntry incoming)
        {
            if (incoming == null) return;
            int stageIndex = -1;
            for (int i = 0; i < _stages.Count; i++)
            {
                if (_stages[i].Stage == incoming.Stage) { stageIndex = i; break; }
            }
            if (stageIndex < 0)
            {
                _stages.Add(incoming);
                return;
            }

            StageEntry current = _stages[stageIndex];
            var types = new List<TypeEntry>(current.Types.Count + incoming.Types.Count);
            for (int i = 0; i < current.Types.Count; i++) types.Add(current.Types[i]);
            for (int i = 0; i < incoming.Types.Count; i++)
            {
                TypeEntry patch = incoming.Types[i];
                int typeIndex = -1;
                for (int j = 0; j < types.Count; j++)
                {
                    if (types[j].Type == patch.Type) { typeIndex = j; break; }
                }
                if (typeIndex >= 0) types[typeIndex] = patch;
                else types.Add(patch);
            }
            _stages[stageIndex] = new StageEntry(incoming.Stage, types);
        }
    }
}
