using Shenxiao.Framework.Util;

namespace Shenxiao.Common.Tooltip
{
    /// <summary>Generic tooltip (item / equip / skill). Phase 0 skeleton.</summary>
    public static class TooltipManager
    {
        public static void ShowItem(int itemId) { GameLog.Debug("Tooltip", "item={0}", itemId); }
        public static void ShowEquip(int equipUid) { GameLog.Debug("Tooltip", "equip={0}", equipUid); }
        public static void ShowSkill(int skillId) { GameLog.Debug("Tooltip", "skill={0}", skillId); }
        public static void Hide() { }
    }
}
