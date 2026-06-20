using Shenxiao.Generated.UI.Setting;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Setting
{
    /// <summary>
    /// 设置主界面(大)(对标老客户端 setting/SettingView.ts):顶部头像(_role_head/change_head_btn 改头像)+ 角色信息
    /// (_lb_name 名/_btn_changename 改名/id_number 玩家ID/_btn_copy 复制/id_ser_name 区服)+ 底部操作
    /// (change_role 换角色/return_login 返回登录/simple_mode_btn 简洁模式/confirm_flee 确认逃跑(脱离卡死)/confirm_res 复活(修复))+
    /// 双页签(_box_tab_base_setting 基础设置 / _box_tab_shield_setting 屏蔽列表)+
    /// 基础设置页(_box_base_setting):音/效/屏 滑条(slider_audio/slider_music/slider_conta2/slider_conta1,克隆 _tpl_WithBtnHSlider)、
    /// 自动拾取(_list_pick)、御风云骑(_box_horse/_img_horse_check)、神祭(_box_god/_img_god_check)、自动任务(_box_task/_img_task_check1-2)+
    /// 屏蔽列表页(_box_shield_setting/_list_shield 克隆 _tpl_SettingShieldItem)+ 关闭(_img_close/_img_empty)。
    ///
    /// 降级:SettingModel/RoleManager/SoundManager/各协议(10203/10210/42602)、WithBtnHSlider 滑条组件、
    /// CustomHeadItem 头像、LoopScrowViewMgr 列表(SettingShieldItem/SettingSubscriptionItem)、OpenFun(203 改头像)、
    /// Alert 二次确认 均未移植 → 所有 _tpl_* 模板隐藏、屏蔽/拾取列表空、滑条无数值;头像/改名/换角色/返回登录/简洁模式/
    /// 确认逃跑/复活/页签 等按钮点击仅打日志「待对接」;change_head_btn / _btn_changename 暂打日志(父级后续接 OpenSub);
    /// _img_close / _img_empty 关闭可用。事件驱动窗口,默认关闭、不进 FirstPass。数据/列表后续 tick 补。
    /// </summary>
    public sealed class SettingView : SettingViewBind
    {
        protected override void OnInit()
        {
            HideTemplates();
            BindClose(_img_close);
            BindClose(_img_empty);
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            // 老端 OpenCallback → SettingModel/RoleManager 铺角色信息 + 滑条/列表/页签。均未移植 → 列表空/默认降级。
            GameLog.Info("Setting", "SettingView 打开 → 待对接 SettingModel(列表空/默认降级)");
        }

        /// <summary>克隆模板节点(滑条/屏蔽项/拾取项/自定义头像/神祭)在运行时由列表/容器复制,先全部隐藏。</summary>
        private void HideTemplates()
        {
            if (_tpl_SettingShieldItem != null) _tpl_SettingShieldItem.SetActive(false);
            if (_tpl_SettingSubscriptionItem != null) _tpl_SettingSubscriptionItem.SetActive(false);
            if (_tpl_CustomHeadItem != null) _tpl_CustomHeadItem.SetActive(false);
            if (_tpl_WithBtnHSlider != null) _tpl_WithBtnHSlider.SetActive(false);
            if (_tpl_GodBefallMainView != null) _tpl_GodBefallMainView.SetActive(false);
        }

        private void BindButtons()
        {
            // 顶部头像/角色信息:改头像/改名 → 真打开子窗(经 SettingFlow.OpenSub 叠在主面板上,各子窗 _close_btn 返回)。
            BindOpen(change_head_btn, "SettingChangeHeadView", "更换头像");
            BindOpen(_btn_changename, "SettingChangeNameView", "修改名字");
            BindBtn(_btn_copy, "复制玩家ID");
            // 页签切换(基础设置 / 屏蔽列表)。
            BindBtn(_box_tab_base_setting, "页签-基础设置");
            BindBtn(_box_tab_shield_setting, "页签-屏蔽列表");
            // 屏蔽/勾选项(神祭 / 御风云骑 / 自动任务开关)。
            BindBtn(_img_god_check, "神祭开关");
            BindBtn(_img_horse_check, "御风云骑开关");
            BindBtn(_img_task_check1, "自动任务-开");
            BindBtn(_img_task_check2, "自动任务-关");
            // 底部操作(换角色 / 返回登录 / 简洁模式 / 确认逃跑 / 复活)。
            BindBtn(change_role, "更换角色");
            BindBtn(return_login, "返回登录");
            BindBtn(simple_mode_btn, "简洁模式");
            BindBtn(confirm_flee, "确认逃跑(脱离卡死)");
            BindBtn(confirm_res, "复活(修复异常)");
        }

        /// <summary>按钮 → 打开设置模块内子窗(SettingFlow.OpenSub 按 View 子类名查找并叠在主面板上)。</summary>
        private void BindOpen(Component target, string viewType, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                GameLog.Info("Setting", "点击[{0}] → 打开 {1}", label, viewType);
                SettingFlow.OpenSub(viewType);
            });
        }

        /// <summary>关闭按钮(Image 或含 Image 容器)→ Hide(关闭本窗)。</summary>
        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/子面板待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Setting", "点击[{0}] → 待对接", label));
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
