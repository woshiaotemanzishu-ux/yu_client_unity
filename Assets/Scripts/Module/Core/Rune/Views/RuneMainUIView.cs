using Shenxiao.Generated.UI.Rune;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Rune
{
    /// <summary>
    /// 九霄劫魄主界面(对标老客户端 rune/RuneMainUIView.ts):符文盘(conta1..10 镶嵌槽,克隆 RuneIcon)+ 当前符文卡(_gp_card)+
    /// 属性进度(pro_conta/left_pro/right_pro/top_level)+ 战力(_fight_con)+ 一排功能按钮:转化(convertBtn→RuneConvertView)/
    /// 查看(lookBtn→RunePropertyView)/技能(skillBtn→RuneSkillView)/觉醒(awakeBtn→RuneAwakenView)/合成(composeBtn)/分解(resolveBtn→RuneDecMainView)/
    /// 替换(replaceBtn)/升级(upgradeBtn)/镶嵌(insertBtn)/前往(goBtn)/选标签(_btn_xb/_btn_fb)+ 各红点(*Dot/*Red)。
    ///
    /// 降级:RuneModel/GoodsModel/config_rune/协议、各子窗(转化/技能/觉醒/属性/分解)与列表项(RuneIcon/RuneSpIcon/BaseAwardItem)均未移植 →
    /// 红点/技能锁隐藏、_tpl_* 模板隐藏、符文盘空、属性默认;子窗按钮经 RuneFlow.OpenSub 打开(子窗未写时日志降级),其余按钮打日志。
    /// 无独立关闭按钮 → 由 HUD 秘宝图标再点关闭(RuneFlow.Toggle)。事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class RuneMainUIView : RuneMainUIViewBind
    {
        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            RuneController.Instance.RequestInfo();
            // 老端 LoadSuccess → 读 RuneModel 铺符文盘 + 当前符文卡 + 属性/战力。数据未移植 → 盘空、属性默认降级。
            GameLog.Info("Rune", "九霄劫魄打开 → 待对接 RuneModel(符文盘空/属性默认降级)");
        }

        private void HideReds()
        {
            HideNode(convertDot); HideNode(composeDot); HideNode(resolveDot);
            HideNode(skillRed); HideNode(awakeRed); HideNode(replaceDot);
            HideNode(upgradeDot); HideNode(insertDot); HideNode(_img_skill_lock);
        }

        private void HideTemplates()
        {
            if (_tpl_RuneSpIcon != null) _tpl_RuneSpIcon.SetActive(false);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_FightingShowSmallItem != null) _tpl_FightingShowSmallItem.SetActive(false);
            if (_tpl_RuneIcon != null) _tpl_RuneIcon.SetActive(false);
        }

        private void BindButtons()
        {
            // 子窗按钮 → RuneFlow.OpenSub(子窗 View 未写时日志降级,写后即生效)。
            BindOpen(convertBtn, "RuneConvertView", "转化");
            BindOpen(lookBtn, "RunePropertyView", "查看属性");
            BindOpen(skillBtn, "RuneSkillView", "符文技能");
            BindOpen(awakeBtn, "RuneAwakenView", "觉醒");
            BindOpen(resolveBtn, "RuneDecMainView", "分解");
            // 操作类按钮(协议/容器内逻辑)→ 暂日志。
            BindBtn(composeBtn, "合成");
            BindBtn(replaceBtn, "替换符文");
            BindBtn(upgradeBtn, "升级符文");
            BindBtn(insertBtn, "镶嵌符文");
            BindBtn(goBtn, "前往获取");
            BindBtn(_btn_xb, "标签-稀有");
            BindBtn(_btn_fb, "标签-普通");
        }

        /// <summary>按钮 → 切换符文模块内子窗(RuneFlow.ToggleSub 按 View 子类名查找,叠在主面板上,再点关闭;含无关闭按钮的子窗)。</summary>
        private void BindOpen(Component target, string viewType, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                GameLog.Info("Rune", "点击[{0}] → 切换 {1}", label, viewType);
                RuneFlow.ToggleSub(viewType);
            });
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/逻辑待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Rune", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
