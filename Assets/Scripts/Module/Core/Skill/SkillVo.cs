namespace Shenxiao.Module.Core.Skill
{
    /// <summary>
    /// 技能值对象(对标老端 skill/SkillVo.ts 的最小子集):id + level,图标/名字走 config_skill。
    /// 本轮只承载首屏技能槽展示需要的字段;CD/连招/属性/突破等深水区字段下一轮再补。
    /// </summary>
    public sealed class SkillVo
    {
        public int Id { get; }
        public int Level;

        public SkillVo(int id)
        {
            Id = id;
        }

        /// <summary>锁定态(对标 UpdateLockState:level==0 → lock)。</summary>
        public bool Locked => Level <= 0;

        public string GetName() => SkillConfigs.GetName(Id);

        /// <summary>取图标资源名(对标 SkillVo.GetIcon:lv_data[level-1].icon,缺省回落 id)。</summary>
        public string GetIcon(int level = 0)
        {
            int lv = level > 0 ? level : Level;
            return SkillConfigs.GetIconForLevel(Id, lv);
        }

        /// <summary>实际展示图标:level==0 用 1 级图标(对标 MainUISkillItem:show_icon = level==0 && GetIcon(1) || GetIcon())。</summary>
        public string DisplayIcon => Level <= 0 ? GetIcon(1) : GetIcon();
    }
}
