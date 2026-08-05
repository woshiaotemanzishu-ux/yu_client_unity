using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.InnateSkill;
using UnityEngine;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 天赋类型 tab 项(对标老客户端 innateSkill/InnateTypeItemRenderer.ts):类型名 + 已投入点数 + 选中态换图。
    /// 由 <see cref="Shenxiao.Editor.UiCreator.Role.InnateSkillCreator"/> 建 4 份固定实例(攻击/防守/通用/绝对,
    /// 类型恒为 5/6/7/8,老端 RefSelType 里 <c>type = selectedIndex + 5</c>),横排入 _Scroller2.content
    /// (HorizontalLayoutGroup spaceX=20)。点击只在 OnInit 绑定一次,读 <see cref="SkillType"/> 字段。
    /// </summary>
    public sealed class InnateTypeItemRenderer : InnateTypeItemRendererBind
    {
        private const string SelectedIconAsset = "innateSkill/texture/ui_role_btn_3.png";
        private const string NormalIconAsset = "innateSkill/texture/ui_role_btn_4.png";
        private static readonly Color SelectedColor = new Color(1f, 1f, 1f, 1f);       // #ffffff
        private static readonly Color NormalColor = new Color(0.310f, 0.325f, 0.561f, 1f); // #4f538f

        public int SkillType { get; private set; }
        public System.Action<int> OnClicked;

        private bool _clickBound;
        private bool _selected;

        protected override void OnInit()
        {
            if (_clickBound || _img_skill_icon == null) return;
            UIUtil.AddClick(_img_skill_icon, () => { if (SkillType > 0) OnClicked?.Invoke(SkillType); });
            _clickBound = true;
        }

        public void SetType(int skillType, string label)
        {
            SkillType = skillType;
            // 老 H5 的 InnateTypeItemRenderer 对 type=8 有明确的运行时兼容覆盖：
            // ConfigSkillUI 仍写“精通”，最终页签显示“绝对”。这里保持同一语义。
            if (typeLb != null) typeLb.text = skillType == 8 ? "绝对" : (label ?? "");
            ApplyVisual();
        }

        public void SetPoint(int point)
        {
            if (_lb_lv != null) _lb_lv.text = "(" + point + ")";
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            Color c = _selected ? SelectedColor : NormalColor;
            if (typeLb != null) typeLb.color = c;
            if (_lb_lv != null) _lb_lv.color = c;
            if (_img_skill_icon != null)
            {
                string path = "resource/game/" + (_selected ? SelectedIconAsset : NormalIconAsset);
                _ = ResManager.SetImageAsync(_img_skill_icon, path, nativeSize: false);
            }
        }
    }
}
