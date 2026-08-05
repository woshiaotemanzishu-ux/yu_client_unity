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

        /// <summary>被动技能解锁任务，仅用于角色技能页展示文案，不参与技能协议。</summary>
        public int TaskId;

        public SkillVo(int id)
        {
            Id = id;
        }

        /// <summary>锁定态(对标 UpdateLockState:level==0 → lock)。</summary>
        public bool Locked => Level <= 0;

        public string GetName() => SkillConfigs.GetName(Id);

        /// <summary>职业(对标 SkillVo.getCarrer);52=伙伴技能。</summary>
        public int Career => SkillConfigs.GetCareer(Id);

        /// <summary>选取模式(对标 SkillVo.GetSelectType:1自己 2最近敌方 3最近队友)。</summary>
        public int SelectType => SkillConfigs.GetSelectType(Id);

        /// <summary>取图标资源名(对标 SkillVo.GetIcon:lv_data[level-1].icon,缺省回落 id)。</summary>
        public string GetIcon(int level = 0)
        {
            int lv = level > 0 ? level : Level;
            return SkillConfigs.GetIconForLevel(Id, lv);
        }

        /// <summary>实际展示图标:level==0 用 1 级图标(对标 MainUISkillItem:show_icon = level==0 && GetIcon(1) || GetIcon())。</summary>
        public string DisplayIcon => Level <= 0 ? GetIcon(1) : GetIcon();

        // ── 技能成长线(自动循环 轮3)追加字段:升级所需(condition:goods/MaxLv/desc),对标老端 SkillVo.getDesc/GetTotalLevel ──

        /// <summary>最大等级(对标 SkillVo.GetTotalLevel)。</summary>
        public int MaxLevel => SkillConfigs.GetMaxLevel(Id);

        /// <summary>是否已满级(level>=MaxLevel 且 MaxLevel 有效)。</summary>
        public bool IsMaxLevel => MaxLevel > 0 && Level >= MaxLevel;

        /// <summary>取当前(或指定)等级描述文本(对标 SkillVo.getDesc;level&lt;=0 用当前 Level)。</summary>
        public string GetDesc(int level = 0) => SkillConfigs.GetDescForLevel(Id, level > 0 ? level : Level);

        /// <summary>下一级升级所需材料(condition 里的 {goods,TypeId,Count} 项;已满级/无材料条件 → false)。</summary>
        public bool TryGetNextLevelGoodsCost(out int typeId, out int count)
        {
            typeId = 0;
            count = 0;
            if (IsMaxLevel) return false;
            return SkillConfigs.TryGetGoodsCost(Id, Level + 1, out typeId, out count);
        }
    }
}
