using Shenxiao.Generated.UI.InnateSkill;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 升级条件单行(对标老客户端 innateSkill/InnateUpCondItem.ts):"XX系投入:have/need" 或 "前置天赋:name lv/lv",
    /// 达标绿字(#0a953e)/不足红字(#ff4f50)。由 <see cref="InnateUpInfoItem"/> 从隐藏模板克隆按需摆放。
    /// TMP 富文本 &lt;color=#XXXXXX&gt; 直接对标老端 Laya HTMLDivElement 的 &lt;font color="..."&gt;。
    /// </summary>
    public sealed class InnateUpCondItem : InnateUpCondItemBind
    {
        public void SetPointCond(string branchName, int have, int need)
        {
            string color = have >= need ? "#0a953e" : "#ff4f50";
            SetText(branchName + "系投入:<color=" + color + ">" + have + "/" + need + "</color>");
        }

        public void SetPreSkillCond(string skillName, int haveLv, int needLv)
        {
            string color = haveLv >= needLv ? "#0a953e" : "#ff4f50";
            SetText("前置天赋:<color=" + color + ">" + skillName + " " + haveLv + "/" + needLv + "</color>");
        }

        private void SetText(string rich)
        {
            if (_lb_cond == null) return;
            _lb_cond.richText = true;
            _lb_cond.text = rich;
        }
    }
}
