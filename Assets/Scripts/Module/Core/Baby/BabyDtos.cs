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

    public sealed class BabyFigureEntry
    {
        public int BabyId;
        public int BabyStar;
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
}
