using Shenxiao.Generated.UI.Setting;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Setting
{
    /// <summary>
    /// 改名子窗(对标老客户端 setting/SettingChangeNameView.ts):
    /// 输入新名字(InptextDisplay 保留可见)+ 确定(confirmBtn)/取消(cancleBtn)/关闭(_close_btn)+
    /// 消耗显示(free_label 免费 / cost_conta:icon1 改名卡 _tpl_BaseAwardItem + cost1 / icon2 勾玉 + cost2)。
    ///
    /// 降级:SettingModel/config_change_name(消耗配置、改名卡/勾玉数量、敏感词过滤、CAN_USE_THIS_NAME 确认)、
    /// BaseAwardItem(消耗道具图标克隆)均未移植 → _tpl_BaseAwardItem 模板隐藏、消耗显示保持预制体默认(空降级);
    /// 输入框可见但不发协议;confirmBtn 点击打日志(改名协议 42604/42601 待接);
    /// 老端 _close_btn 与 cancleBtn 都走 Close(取消即关闭)→ 这里都挂 Hide。
    /// </summary>
    public sealed class SettingChangeNameView : SettingChangeNameViewBind
    {
        protected override void OnInit()
        {
            HideTemplates();
            // 老端:_close_btn 与 cancleBtn 同走 Close(取消即关闭)。
            BindClose(_close_btn);
            BindClose(cancleBtn);
            // 改名协议(42604 校验 → CAN_USE_THIS_NAME 确认 → 42601 提交)未移植,先打日志。
            BindBtn(confirmBtn, "确定改名(改名协议待接)");
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Setting", "SettingChangeNameView 打开 → 待对接 SettingModel(列表空/默认降级)");
        }

        /// <summary>消耗道具模板(BaseAwardItem 克隆源)未移植先隐藏。GameObject 用 SetActive,不走 HideNode。</summary>
        private void HideTemplates()
        {
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
        }

        /// <summary>关闭/取消按钮(Image 或含 Image 容器)→ Hide(关闭本窗)。</summary>
        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议待对接)。</summary>
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
