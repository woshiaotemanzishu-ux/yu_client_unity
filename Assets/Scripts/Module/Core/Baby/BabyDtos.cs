using System.Collections.Generic;

namespace Shenxiao.Module.Core.Baby
{
    public sealed class BabyErrorInfo
    {
        public int Command;
        public int ErrorCode;
        public string Args = string.Empty;
    }

    public sealed class BabyBasicInfo
    {
        public int ActiveTime;
        public int BabyId;
        public string BabyName = string.Empty;
        public bool IsChangeName;
        public bool IsActive => ActiveTime > 0;
    }

    public sealed class BabyTaskInfo
    {
        public int TaskId;
        public int FinishNum;
        public int FinishState;
    }

    public sealed class BabyRaiseInfo
    {
        public int RaiseLevel;
        public int RaiseExp;
        public readonly List<BabyTaskInfo> TaskList = new List<BabyTaskInfo>();
        public int Power;
    }

    public sealed class BabyStageInfo
    {
        public int Stage;
        public int StageLevel;
        public int StageExp;
        public int Power;
    }

    public sealed class BabyEquipEntry
    {
        public int PositionId;
        public long Id;
        public int GoodsTypeId;
        public int Stage;
        public int StageLevel;
        public int StageExp;
        public int SkillId;
    }

    public sealed class BabyEquipInfo
    {
        public readonly List<BabyEquipEntry> EquipList = new List<BabyEquipEntry>();
        public int Power;
    }

    public sealed class BabyEquipWearResult
    {
        public int Code;
        public int PositionId;
        public long Id;
        public int GoodsTypeId;
        public int SkillId;
        public int Power;
        public bool Succeeded => Code == 1;
    }

    public sealed class BabyEquipUpgradeResult
    {
        public int Code;
        public int PositionId;
        public long Id;
        public int GoodsTypeId;
        public int Stage;
        public int StageLevel;
        public int StageExp;
        public int Power;
        public bool Succeeded => Code == 1;
    }

    public sealed class BabyFigureEntry
    {
        public int BabyId;
        public int BabyStar;
        public long Power;
        public long NextPower;
        public bool IsActivated;
    }

    public sealed class BabyFigureInfo
    {
        public readonly List<BabyFigureEntry> ActiveList = new List<BabyFigureEntry>();
    }

    public sealed class BabyAttrEntry
    {
        public int AttrId;
        public int Value;
    }

    public sealed class BabyAttrGroup
    {
        public int Type;
        public readonly List<BabyAttrEntry> AttrList = new List<BabyAttrEntry>();
    }

    public sealed class BabyFamilyEntry
    {
        public long RoleId;
        public int ActiveTime;
        public int BabyId;
        public string BabyName = string.Empty;
        public int RaiseLevel;
        public int Stage;
        public int StageLevel;
        public int BabyPower;
        public readonly List<BabyAttrGroup> AttrInfo = new List<BabyAttrGroup>();
    }

    public sealed class BabyFamilyInfo
    {
        public readonly List<BabyFamilyEntry> InfoList = new List<BabyFamilyEntry>();
    }

    public sealed class BabyActivateResult
    {
        public int Code;
        public bool Succeeded => Code == 1;
    }

    public sealed class BabyStageUpResult
    {
        public int Code;
        public int Stage;
        public int StageLevel;
        public int StageExp;
        public int Power;
        public bool Succeeded => Code == 1;
    }

    public sealed class BabyFigureStarResult
    {
        public int Code;
        public int BabyId;
        public int BabyStar;
        public long Power;
        public long NextPower;
        public bool Succeeded => Code == 1;
        public bool IsActivated => Succeeded && BabyStar > 0;
    }

    public sealed class BabyFigureWearResult
    {
        public int Code;
        public int Type;
        public int BabyId;
        public bool Succeeded => Code == 1;
    }

    public sealed class BabyRenameResult
    {
        public int Code;
        public string Name = string.Empty;
        public bool Succeeded => Code == 1;
    }

    public sealed class BabyTaskRewardResult
    {
        public int Code;
        public int TaskId;
        public int FinishNum;
        public int FinishState;
        public bool Succeeded => Code == 1;
    }

    public sealed class BabyFigurePowerResult
    {
        public int BabyId;
        public int BabyStar;
        public long Power;
        public long NextPower;
        public bool IsActivated;
    }

    public sealed class BabyPraiseRankEntry
    {
        public long RoleId;
        public string Name = string.Empty;
        public int BabyPower;
        public int PraiseNum;
    }

    public sealed class BabyPraiseRankInfo
    {
        public long RoleId;
        public readonly List<BabyPraiseRankEntry> Entries = new List<BabyPraiseRankEntry>();
    }

    public sealed class BabyPraiseRecordEntry
    {
        public long PraiserId;
        public string Name = string.Empty;
        public bool IsPraiseBack;
    }

    public sealed class BabyPraiseRecordsInfo
    {
        public readonly List<BabyPraiseRecordEntry> Entries = new List<BabyPraiseRecordEntry>();
    }

    public sealed class BabyPraiseRewardEntry
    {
        public int Type;
        public int TypeId;
        public int Num;
    }

    public sealed class BabyPraiseActionResult
    {
        public int Code;
        public long RoleId;
        public int Opr;
        public readonly List<BabyPraiseRewardEntry> Rewards = new List<BabyPraiseRewardEntry>();
        public bool Succeeded => Code == 1;
    }

    public sealed class BabyPraisePush
    {
        public long PraiserId;
    }
}
