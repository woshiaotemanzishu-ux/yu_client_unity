using Shenxiao.Generated.UI.Role;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 被动技能项(对标老客户端 role/SkillPassiveItem.ts):图标(_img_icon)+ 名(_lb_name)+ 等级(_lb_lv)+ 选中(_img_select)/红点(_img_red),
    /// 内含子项模板(_tpl_SkillPassiveSubItem)。
    ///
    /// SetData(name, lv) + SetSelected(bool)。降级:技能图标/数据(SkillUIModel)未移植 → 名/等级/选中可用,红点 + 子项模板隐藏。由界面克隆。
    /// </summary>
    public sealed class SkillPassiveItem : SkillPassiveItemBind
    {
        protected override void OnInit()
        {
            if (_img_select != null) _img_select.gameObject.SetActive(false);
            if (_img_red != null) _img_red.gameObject.SetActive(false);
            if (_tpl_SkillPassiveSubItem != null) _tpl_SkillPassiveSubItem.SetActive(false);
        }

        public void SetData(string name, string lv)
        {
            if (_lb_name != null) _lb_name.text = name ?? "";
            if (_lb_lv != null) _lb_lv.text = lv ?? "";
        }

        public void SetSelected(bool sel)
        {
            if (_img_select != null) _img_select.gameObject.SetActive(sel);
        }
    }
}
