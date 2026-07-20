using System.Collections.Generic;
using Shenxiao.Framework.Event;

namespace Shenxiao.Module.Core.Friend
{
    public abstract class LookOverModuleSnapshot
    {
        public long RoleId;
        public int ServerId;
        public int ModuleId;
        public string Title = "";
        public long PrimaryPower;
        public abstract IReadOnlyList<string> BuildRows();

        protected List<string> BeginRows()
        {
            return new List<string> { Title, "总战力/评分: " + PrimaryPower };
        }
    }

    public sealed class LookOverDragonBallSnapshot : LookOverModuleSnapshot
    {
        public int IsActive;
        public readonly List<BallEntry> BallList = new List<BallEntry>();
        public readonly List<FigureEntry> FigureList = new List<FigureEntry>();
        public sealed class BallEntry { public uint DragonBallId; public int Level; }
        public sealed class FigureEntry { public int Type; public int Lv; }
        public override IReadOnlyList<string> BuildRows()
        {
            List<string> rows = BeginRows();
            rows.Add("激活: " + IsActive);
            foreach (BallEntry e in BallList) rows.Add($"龙珠 ID={e.DragonBallId} 等级={e.Level}");
            foreach (FigureEntry e in FigureList) rows.Add($"形象 类型={e.Type} 等级={e.Lv}");
            return rows;
        }
    }

    public sealed class LookOverSealSnapshot : LookOverModuleSnapshot
    {
        public int SysType;
        public long Rating;
        public readonly List<PositionEntry> PosList = new List<PositionEntry>();
        public readonly List<PillEntry> PillList = new List<PillEntry>();
        public readonly List<AttrEntry> StrenAttr = new List<AttrEntry>();
        public readonly List<AttrEntry> EquipAttr = new List<AttrEntry>();
        public readonly List<AttrEntry> PillAttr = new List<AttrEntry>();
        public readonly List<AttrEntry> SuitAttr = new List<AttrEntry>();
        public readonly List<SuitEntry> SuitList = new List<SuitEntry>();
        public sealed class AttrEntry { public int Attr; public uint Value; }
        public sealed class PositionEntry
        {
            public int Pos; public uint TypeId; public uint GoodsId; public uint Rating; public uint Strong; public int Cell;
            public readonly List<AttrEntry> AttrList = new List<AttrEntry>();
        }
        public sealed class PillEntry
        {
            public uint GoodsId; public int TotalNum;
            public readonly List<AttrEntry> AttrList = new List<AttrEntry>();
        }
        public sealed class SuitEntry { public int SuitId; public uint Num; }
        public override IReadOnlyList<string> BuildRows()
        {
            List<string> rows = BeginRows();
            rows.Add($"系统={SysType} 总评分={Rating}");
            foreach (PositionEntry e in PosList)
            {
                rows.Add($"部位={e.Pos} 类型ID={e.TypeId} 物品ID={e.GoodsId} 评分={e.Rating} 强化={e.Strong} 格位={e.Cell}");
                AddAttrs(rows, "装备属性", e.AttrList);
            }
            foreach (PillEntry e in PillList)
            {
                rows.Add($"丹药物品ID={e.GoodsId} 总数={e.TotalNum}");
                AddAttrs(rows, "丹药属性", e.AttrList);
            }
            AddAttrs(rows, "强化总属性", StrenAttr); AddAttrs(rows, "装备总属性", EquipAttr);
            AddAttrs(rows, "丹药总属性", PillAttr); AddAttrs(rows, "套装总属性", SuitAttr);
            foreach (SuitEntry e in SuitList) rows.Add($"套装ID={e.SuitId} 数量={e.Num}");
            return rows;
        }
        private static void AddAttrs(List<string> rows, string label, List<AttrEntry> attrs)
        {
            foreach (AttrEntry a in attrs) rows.Add($"{label} ID={a.Attr} 值={a.Value}");
        }
    }

    public sealed class LookOverRevelationSnapshot : LookOverModuleSnapshot
    {
        public int MaxFigureId; public int CurrentFigureId; public long AllScore;
        public readonly List<GatheringEntry> Gathering = new List<GatheringEntry>();
        public readonly List<SuitEntry> Suit = new List<SuitEntry>();
        public readonly List<SkillEntry> SkillList = new List<SkillEntry>();
        public sealed class GatheringEntry
        { public int Pos; public int Lv; public uint Exp; public int Flag; public uint EquipId; public uint ItemId; public long Score; }
        public sealed class SuitEntry { public uint Star; public uint Num; }
        public sealed class SkillEntry { public uint SkillId; public int Lv; }
        public override IReadOnlyList<string> BuildRows()
        {
            List<string> rows = BeginRows();
            rows.Add($"最大形象ID={MaxFigureId} 当前形象ID={CurrentFigureId} 总评分={AllScore}");
            foreach (GatheringEntry e in Gathering) rows.Add($"聚灵部位={e.Pos} 等级={e.Lv} 经验={e.Exp} 有效={e.Flag} 装备ID={e.EquipId} 物品ID={e.ItemId} 评分={e.Score}");
            foreach (SuitEntry e in Suit) rows.Add($"套装星级={e.Star} 数量={e.Num}");
            foreach (SkillEntry e in SkillList) rows.Add($"技能ID={e.SkillId} 等级={e.Lv}");
            return rows;
        }
    }

    public sealed class LookOverIllusionSnapshot : LookOverModuleSnapshot
    {
        public int IllusionNum;
        public readonly List<IllusionEntry> IllusionList = new List<IllusionEntry>();
        public readonly List<FashionPosEntry> FashionPos = new List<FashionPosEntry>();
        public readonly List<PowerEntry> SelfPower = new List<PowerEntry>();
        public readonly List<PowerEntry> OthersPower = new List<PowerEntry>();
        public sealed class AttrEntry { public int AttrId; public uint AttrVal; }
        public sealed class FigureEntry
        {
            public int FigureType; public int Id; public int Stage; public int Star; public long Combat; public uint EndTime;
            public readonly List<AttrEntry> AttrList = new List<AttrEntry>();
            public readonly List<AttrEntry> StarAttrList = new List<AttrEntry>();
            public readonly List<uint> SkillList = new List<uint>();
        }
        public sealed class IllusionEntry
        { public int Type; public int Num; public long Power; public readonly List<FigureEntry> FigureList = new List<FigureEntry>(); }
        public sealed class ColorEntry { public uint ColorId; public uint StarLv; }
        public sealed class FashionEntry
        {
            public uint FashionId; public int StarLv; public long Combat; public int NowColorId;
            public readonly List<ColorEntry> ColorList = new List<ColorEntry>();
        }
        public sealed class FashionPosEntry
        { public int PosId; public int PosLv; public uint WearFashionId; public readonly List<FashionEntry> FashionList = new List<FashionEntry>(); }
        public sealed class PowerEntry { public int Type; public long Combat; }
        public override IReadOnlyList<string> BuildRows()
        {
            List<string> rows = BeginRows(); rows.Add("幻化总数=" + IllusionNum);
            foreach (IllusionEntry e in IllusionList)
            {
                rows.Add($"幻化类型={e.Type} 数量={e.Num} 战力={e.Power}");
                foreach (FigureEntry f in e.FigureList)
                {
                    rows.Add($"形象 类型={f.FigureType} ID={f.Id} 阶={f.Stage} 星={f.Star} 战力={f.Combat} 结束时间={f.EndTime}");
                    foreach (AttrEntry a in f.AttrList) rows.Add($"形象属性 ID={a.AttrId} 值={a.AttrVal}");
                    foreach (AttrEntry a in f.StarAttrList) rows.Add($"星级属性 ID={a.AttrId} 值={a.AttrVal}");
                    foreach (uint skill in f.SkillList) rows.Add("形象技能ID=" + skill);
                }
            }
            foreach (FashionPosEntry p in FashionPos)
            {
                rows.Add($"时装位={p.PosId} 等级={p.PosLv} 穿戴时装ID={p.WearFashionId}");
                foreach (FashionEntry f in p.FashionList)
                {
                    rows.Add($"时装ID={f.FashionId} 星级={f.StarLv} 战力={f.Combat} 当前颜色ID={f.NowColorId}");
                    foreach (ColorEntry c in f.ColorList) rows.Add($"颜色ID={c.ColorId} 星级={c.StarLv}");
                }
            }
            foreach (PowerEntry e in SelfPower) rows.Add($"自身类型={e.Type} 战力={e.Combat}");
            foreach (PowerEntry e in OthersPower) rows.Add($"他人类型={e.Type} 战力={e.Combat}");
            return rows;
        }
    }

    public sealed class LookOverGodBefallSnapshot : LookOverModuleSnapshot
    {
        public readonly List<GodEntry> GodBattleInfo = new List<GodEntry>();
        public sealed class EquipEntry { public int Pos; public long GoodsId; }
        public sealed class GodEntry
        {
            public int BattlePos; public uint GodId; public int Lv; public int Grade; public uint Star; public long Power; public int GodStren;
            public readonly List<EquipEntry> EquipList = new List<EquipEntry>();
        }
        public override IReadOnlyList<string> BuildRows()
        {
            List<string> rows = BeginRows();
            foreach (GodEntry g in GodBattleInfo)
            {
                rows.Add($"降神ID={g.GodId} 孔位={g.BattlePos} 等级={g.Lv} 品阶={g.Grade} 星={g.Star} 战力={g.Power} 神格强化={g.GodStren}");
                foreach (EquipEntry e in g.EquipList) rows.Add($"降神装备 部位={e.Pos} 物品ID={e.GoodsId}");
            }
            return rows;
        }
    }

    public sealed class LookOverUnrealSnapshot : LookOverModuleSnapshot
    {
        public readonly List<DecorationEntry> DecorationList = new List<DecorationEntry>();
        public sealed class ExtraAttrEntry
        { public int Color; public int TypeId; public int AttrId; public uint AttrVal; public int PlusInterval; public uint PlusUnit; }
        public sealed class DecorationEntry
        { public int Pos; public long GoodsId; public int Lv; public long StrenScore; public readonly List<ExtraAttrEntry> EquipExtraAttr = new List<ExtraAttrEntry>(); }
        public override IReadOnlyList<string> BuildRows()
        {
            List<string> rows = BeginRows();
            foreach (DecorationEntry e in DecorationList)
            {
                rows.Add($"灵饰 部位={e.Pos} 物品ID={e.GoodsId} 等级={e.Lv} 强化评分={e.StrenScore}");
                foreach (ExtraAttrEntry a in e.EquipExtraAttr) rows.Add($"极品属性 颜色={a.Color} 类型={a.TypeId} 属性ID={a.AttrId} 值={a.AttrVal} 成长间隔={a.PlusInterval} 成长值={a.PlusUnit}");
            }
            return rows;
        }
    }

    public sealed class LookOverLungSnapshot : LookOverModuleSnapshot
    {
        public int AllLevel;
        public readonly List<DragonEquipEntry> DragonEquipList = new List<DragonEquipEntry>();
        public sealed class DragonEquipEntry
        { public int Pos; public int PosLv; public long GoodsId; public int StrenLv; public int AwakeLv; public long Combat; }
        public override IReadOnlyList<string> BuildRows()
        {
            List<string> rows = BeginRows(); rows.Add("总等级=" + AllLevel);
            foreach (DragonEquipEntry e in DragonEquipList) rows.Add($"神纹 部位={e.Pos} 部位等级={e.PosLv} 物品ID={e.GoodsId} 强化={e.StrenLv} 觉醒={e.AwakeLv} 战力={e.Combat}");
            return rows;
        }
    }

    public sealed class LookOverGodBeastSnapshot : LookOverModuleSnapshot
    {
        public int MaxNum; public int BattleNum;
        public readonly List<EudemonsEntry> EudemonsList = new List<EudemonsEntry>();
        public sealed class ExtraAttrEntry
        { public int Color; public int TypeId; public int AttrId; public uint AttrVal; public int PlusInterval; public uint PlusUnit; }
        public sealed class EquipEntry
        { public int Pos; public long GoodsId; public int Stren; public uint EquipScore; public readonly List<ExtraAttrEntry> EquipExtraAttr = new List<ExtraAttrEntry>(); }
        public sealed class EudemonsEntry
        { public int Id; public int State; public long Score; public readonly List<EquipEntry> EquipList = new List<EquipEntry>(); }
        public override IReadOnlyList<string> BuildRows()
        {
            List<string> rows = BeginRows(); rows.Add($"最大助战={MaxNum} 当前助战={BattleNum}");
            foreach (EudemonsEntry e in EudemonsList)
            {
                rows.Add($"蜃妖ID={e.Id} 状态={e.State} 评分={e.Score}");
                foreach (EquipEntry q in e.EquipList)
                {
                    rows.Add($"蜃妖装备 部位={q.Pos} 物品ID={q.GoodsId} 强化={q.Stren} 评分={q.EquipScore}");
                    foreach (ExtraAttrEntry a in q.EquipExtraAttr) rows.Add($"装备属性 颜色={a.Color} 类型={a.TypeId} 属性ID={a.AttrId} 值={a.AttrVal} 成长间隔={a.PlusInterval} 成长值={a.PlusUnit}");
                }
            }
            return rows;
        }
    }

    public sealed class LookOverPetSnapshot : LookOverModuleSnapshot
    {
        public long CompanionPower; public long DemonsPower; public uint BattleDemons;
        public readonly List<CompanionEntry> CompanionList = new List<CompanionEntry>();
        public readonly List<DemonEntry> DemonsList = new List<DemonEntry>();
        public sealed class CompanionSkillEntry { public uint SkillId; public int Level; }
        public sealed class CompanionEntry
        { public int Id; public int Stage; public int Star; public int IsFight; public int TrainNum; public long Combat; public readonly List<CompanionSkillEntry> SkillList = new List<CompanionSkillEntry>(); }
        public sealed class DemonSkillEntry { public uint SkillId; public int SkillLv; public uint Process; public int IsActive; }
        public sealed class SlotSkillEntry { public uint SkillId; public int SkillLv; public int Slot; public int Quality; public int Sort; }
        public sealed class DemonEntry
        { public uint Id; public int Level; public int Star; public int SlotNum; public long Combat; public readonly List<DemonSkillEntry> SkillList = new List<DemonSkillEntry>(); public readonly List<SlotSkillEntry> SlotSkill = new List<SlotSkillEntry>(); }
        public override IReadOnlyList<string> BuildRows()
        {
            List<string> rows = BeginRows(); rows.Add($"神巫战力={CompanionPower} 妖灵战力={DemonsPower} 出战妖灵ID={BattleDemons}");
            foreach (CompanionEntry e in CompanionList)
            {
                rows.Add($"神巫ID={e.Id} 阶={e.Stage} 星={e.Star} 出战={e.IsFight} 训练数={e.TrainNum} 战力={e.Combat}");
                foreach (CompanionSkillEntry s in e.SkillList) rows.Add($"神巫技能ID={s.SkillId} 等级={s.Level}");
            }
            foreach (DemonEntry e in DemonsList)
            {
                rows.Add($"妖灵ID={e.Id} 等级={e.Level} 星={e.Star} 槽位数={e.SlotNum} 战力={e.Combat}");
                foreach (DemonSkillEntry s in e.SkillList) rows.Add($"妖灵技能ID={s.SkillId} 等级={s.SkillLv} 进度={s.Process} 激活={s.IsActive}");
                foreach (SlotSkillEntry s in e.SlotSkill) rows.Add($"槽位技能ID={s.SkillId} 等级={s.SkillLv} 槽位={s.Slot} 品质={s.Quality} 种类={s.Sort}");
            }
            return rows;
        }
    }

    public sealed class LookOverRuneSnapshot : LookOverModuleSnapshot
    {
        public int SkillLevel;
        public readonly List<RuneEntry> RuneList = new List<RuneEntry>();
        public sealed class AttrEntry { public uint AttrId; public uint AttrNum; public int AwakeLv; }
        public sealed class RuneEntry
        { public int PosId; public long GoodsId; public uint GoodsTypeId; public int Color; public int Lv; public int SumAwakeLv; public readonly List<AttrEntry> AttrList = new List<AttrEntry>(); }
        public override IReadOnlyList<string> BuildRows()
        {
            List<string> rows = BeginRows(); rows.Add("御魂技能等级=" + SkillLevel);
            foreach (RuneEntry e in RuneList)
            {
                rows.Add($"御魂 槽位={e.PosId} 唯一ID={e.GoodsId} 类型ID={e.GoodsTypeId} 品质={e.Color} 等级={e.Lv} 总觉醒={e.SumAwakeLv}");
                foreach (AttrEntry a in e.AttrList) rows.Add($"御魂属性ID={a.AttrId} 值={a.AttrNum} 觉醒={a.AwakeLv}");
            }
            return rows;
        }
    }

    public sealed partial class FriendModel
    {
        private readonly Dictionary<(long RoleId, int ModuleId), LookOverModuleSnapshot> _lookOverModules
            = new Dictionary<(long, int), LookOverModuleSnapshot>();

        public LookOverModuleSnapshot LastLookOverModule { get; private set; }

        public LookOverModuleSnapshot GetLookOverModule(long roleId, int moduleId)
        {
            return _lookOverModules.TryGetValue((roleId, moduleId), out LookOverModuleSnapshot value) ? value : null;
        }

        public void SetLookOverModule(LookOverModuleSnapshot snapshot)
        {
            if (snapshot == null || snapshot.RoleId <= 0 || snapshot.ModuleId < 2 || snapshot.ModuleId > 12) return;
            _lookOverModules[(snapshot.RoleId, snapshot.ModuleId)] = snapshot;
            LastLookOverModule = snapshot;
            EventDispatcher.Emit(GlobalEvent.EVT_LOOKOVER_MODULE, snapshot);
        }

        public void ClearLookOverModules()
        {
            _lookOverModules.Clear();
            LastLookOverModule = null;
        }
    }
}
