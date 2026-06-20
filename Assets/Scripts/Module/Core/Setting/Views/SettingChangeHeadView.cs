using Shenxiao.Generated.UI.Setting;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Setting
{
    /// <summary>
    /// 改头像子窗(对标老客户端 setting/SettingChangeHeadView.ts):
    /// 头像滚动列表(scroll,克隆模板 _tpl_SettingHeadItem,对应老端 SettingHeadItem)+ 确定按钮(okBtn)+ 关闭(_close_btn)。
    /// 老端逻辑:LoadSuccess 拉 config_picture(13080) → FormatAllHeadList 按职业过滤/排序 → LoopScrowViewMgr 铺列表;
    /// 点 item 选中(SELECT_HEAD_IMG_ITEM)写 model.select_head_id;okBtn 用 select_head_id 发 13083 换头像;_close_btn 关闭。
    ///
    /// 降级:SettingModel(config_picture/select_head_id)、RoleManager(职业/已激活头像)、头像协议(13080/13083)、
    /// LoopScrowViewMgr 列表系统均未移植 → 列表空、模板 _tpl_SettingHeadItem 隐藏;okBtn 点击打日志「待对接」;
    /// _close_btn → Hide 可用。null-guard 每处访问。
    /// </summary>
    public sealed class SettingChangeHeadView : SettingChangeHeadViewBind
    {
        protected override void OnInit()
        {
            HideTemplates();
            BindClose(_close_btn);
            BindBtn(okBtn, "确定换头像(协议 13083 待接)");
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Setting", "SettingChangeHeadView 打开 → 待对接 SettingModel(列表空/默认降级)");
        }

        /// <summary>头像列表模板(老端 SettingHeadItem),列表数据未移植先隐藏。</summary>
        private void HideTemplates()
        {
            if (_tpl_SettingHeadItem != null) _tpl_SettingHeadItem.SetActive(false);
        }

        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }

        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Setting", "点击[{0}] → 待对接", label));
        }
    }
}
