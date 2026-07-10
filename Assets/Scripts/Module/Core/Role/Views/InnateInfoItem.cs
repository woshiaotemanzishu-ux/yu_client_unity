using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Shenxiao.Framework.Res;
using Shenxiao.Module.Core.Skill;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 天赋技能详情展示(对标老客户端 innateSkill/InnateInfoItem.ts):图标 + 名称 + 等级 + 当前/下级效果描述。
    ///
    /// 老端该 .scene 未被 LayaUI 流水线捕获过(RoleModule.prefab 里找不到 InnateInfoItem 的 Bind 类/模板节点,
    /// 只有 InnateSkillView/InnateListItem/InnateSkillItem/InnateTypeItemRenderer/InnateUpInfoItem/InnateUpCondItem
    /// 六个),故本类不继承生成的 *Bind(没有),改由 <see cref="Shenxiao.Editor.UiCreator.Role.InnateSkillCreator"/>
    /// 按几何报告手工建树后直接赋值这些 public 字段(仿 HudNotice/MainUINoticeView 手工赋值惯例,非常规 Bind 回填)。
    ///
    /// 简化:老端 label1..4 四条描述用 HTMLDivElement 精确测高做手风琴展开箭头,本端用 VerticalLayoutGroup 自动排布,
    /// 展开箭头(_img_arrow_right)老端逻辑本就恒不触发(InitEvent 里整段展开/收起代码已注释废弃),不复刻。
    /// </summary>
    public sealed class InnateInfoItem : MonoBehaviour
    {
        public Image Icon;
        public Image Mask;
        public Image Frame;
        public TextMeshProUGUI NameLabel;
        public TextMeshProUGUI LevelLabel;
        public RectTransform DecContainer;

        private readonly List<TextMeshProUGUI> _decLines = new List<TextMeshProUGUI>();
        private TMP_FontAsset _font;

        public void SetData(int skillId)
        {
            if (skillId <= 0 || !SkillConfigs.Has(skillId))
            {
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);

            int lv = SkillTalentModel.Instance.GetTalentLevel(skillId);
            int maxLv = SkillConfigs.GetMaxLevel(skillId);

            if (NameLabel != null)
            {
                if (_font == null) _font = NameLabel.font;
                NameLabel.text = SkillConfigs.GetName(skillId);
            }
            if (LevelLabel != null) LevelLabel.text = "等级:" + lv + "/" + maxLv;
            if (Icon != null)
            {
                int iconLv = Mathf.Max(lv, 1);
                _ = ResManager.SetImageAsync(Icon, GameResPath.GetSkillIcon(SkillConfigs.GetIconForLevel(skillId, iconLv)), nativeSize: false);
            }

            ClearDecLines();
            bool maxed = maxLv > 0 && lv >= maxLv;
            string curTitle = lv <= 0 ? "尚未学习" : (maxed ? "[满级效果]" : "[当前效果]");
            if (lv <= 0)
            {
                AddDecLine(SkillConfigs.GetDescForLevel(skillId, 1), "#663915", false);
            }
            else
            {
                AddDecLine(curTitle, "#0a953e", true);
                AddDecLine(SkillConfigs.GetDescForLevel(skillId, lv), "#663915", false);
            }
            if (!maxed && maxLv > 0)
            {
                AddDecLine("[下级效果]", "#ff4f50", true);
                AddDecLine(SkillConfigs.GetDescForLevel(skillId, lv + 1), "#663915", false);
            }
        }

        private void AddDecLine(string text, string colorHex, bool bold)
        {
            if (DecContainer == null) return;
            var go = new GameObject("Line" + _decLines.Count, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(DecContainer, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.text = text;
            t.fontSize = 20f;
            t.color = ColorFromHex(colorHex);
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.alignment = bold ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft;
            t.textWrappingMode = TextWrappingModes.Normal;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 280f;
            _decLines.Add(t);
        }

        private void ClearDecLines()
        {
            foreach (TextMeshProUGUI t in _decLines) if (t != null) Destroy(t.gameObject);
            _decLines.Clear();
        }

        private static Color ColorFromHex(string hex)
            => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;
    }
}
