using Shenxiao.Generated.UI.Common;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 战力提升项(对标老客户端 common/FightingUpItem.ts):显示战力提升数值(+箭头)。Updatefighting(value)→数字。
    ///
    /// 降级:位图字体(num_new_green/view_fight_up)+箭头款式(SetArrowStype)未移植 → 用 TMP 文本直显;
    /// _tpl_WithBtnHSlider 隐藏。列表项,由 FightingShowSmallItem 克隆。
    /// </summary>
    public sealed class FightingUpItem : FightingUpItemBind
    {
        protected override void OnInit()
        {
            if (_tpl_WithBtnHSlider != null) _tpl_WithBtnHSlider.SetActive(false);
        }

        /// <summary>填战力提升值(对标 Updatefighting)。</summary>
        public void SetFighting(long fighting)
        {
            if (_lb_fighting != null) _lb_fighting.text = fighting.ToString();
        }
    }
}
