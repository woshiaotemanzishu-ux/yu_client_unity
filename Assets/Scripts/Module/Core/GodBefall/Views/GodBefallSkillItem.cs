using Shenxiao.Generated.UI.GodBefall;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.GodBefall
{
    /// <summary>
    /// 神祇降临 技能/天赋格(对标老客户端 godBefall/GodBefallSkillItem.ts):一个技能或天赋图标。
    /// 容器 _box_con 可点 → 打开 SkillTipsView 查看技能详情;技能图标 _img_icon(灰显表示未解锁)、
    /// 渐现副图标 _img_icon2(仅 tips 升级动画用,默认隐)、锁罩 _box_lock + 解锁文案 _lb_lock(「N星解锁」)、
    /// 技能标记 _img_skill_tag(默认隐)、新解锁标 _img_new(默认隐)。两套数据:技能(is_skill)/天赋(数组 [skillId, grade, lv])。
    ///
    /// 降级:GodBefallModel(GetGodVoInDic 解锁判定)、SkillManager(getSkill/getLevelVo 图标 key)、
    /// ResManager(SetOutsideImageSprite 技能图标)、Util.SetImageGray(灰显)、SkillTipsView 子窗、
    /// 各 ShowTween1/2/3 入场/升级动画 均未移植 → OnInit 隐藏副图标/技能标/新标/锁罩;
    /// SetData 仅按老端结构落锁状态文案(无 Model 则按 grade 简单判定/留空),图标走 Model 待对接;点击仅打日志。
    /// 列表项,由 神祇降临 面板克隆/铺设。
    /// </summary>
    public sealed class GodBefallSkillItem : GodBefallSkillItemBind
    {
        protected override void OnInit()
        {
            // 副图标:仅升级渐现动画(ShowTween3)用,动画未移植 → 隐藏。
            HideNode(_img_icon2);
            // 技能标记:对标 _img_skill_tag,默认不显(仅 is_tag_skill 时显)→ 隐藏。
            HideNode(_img_skill_tag);
            // 新解锁标:对标 _img_new(SetNewFlag),默认不显 → 隐藏。
            HideNode(_img_new);
            // 锁罩:默认未锁,有数据时由 SetData 决定 → 先隐藏。
            HideNode(_box_lock);

            // 容器点击:对标 InitEvent → 打开 SkillTipsView(降级:仅打日志)。
            BindBtn(_box_con, "查看技能详情 SkillTipsView");
        }

        /// <summary>
        /// 填技能/天赋数据(对标 SetData + UpdateItem)。
        /// data 在老端有两态:技能对象(is_skill)或天赋数组([skillId, grade, lv]);
        /// grade 为当前神祇阶数(天赋解锁判定用,可选)。
        /// 降级:GodBefallModel/SkillManager/ResManager 未移植 → 图标待对接、锁状态按 grade 简单判定。
        /// </summary>
        public void SetData(int requireGrade, int curGrade)
        {
            // 天赋解锁:老端 is_lock = require > 当前阶。无 Model 时按入参 grade 判。
            bool isLock = requireGrade > 0 && curGrade < requireGrade;
            if (_box_lock != null) _box_lock.gameObject.SetActive(isLock);
            if (_lb_lock != null) _lb_lock.text = isLock ? (requireGrade + "星解锁") : "";

            // 图标 + 灰显依赖 SkillManager/ResManager/Util.SetImageGray(未移植)→ 待对接。
            HideNode(_img_icon2);
            HideNode(_img_skill_tag);
            HideNode(_img_new);

            GameLog.Info("GodBefall", "GodBefallSkillItem.SetData(require={0}, cur={1}) → 待对接 GodBefallModel/SkillManager/ResManager(技能图标/灰显)", requireGrade, curGrade);
        }

        /// <summary>设置「新解锁」标(对标 SetNewFlag)。</summary>
        public void SetNewFlag(bool show)
        {
            if (_img_new != null) _img_new.gameObject.SetActive(show);
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:逻辑/子窗待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("GodBefall", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
