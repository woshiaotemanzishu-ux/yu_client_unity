using Shenxiao.Generated.UI.Setting;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Setting
{
    /// <summary>
    /// 设置-微信订阅项(对标老客户端 setting/SettingSubscriptionItem.ts):
    /// 老端是双形态项——type==1 为分组标题(_box_title + 标题文字 _Label3);type==2 为订阅条目(_box_desc + 描述文字 _Label6 + 订阅按钮 _btn,内含按钮文字 _Label41 与"已订阅"提示 _Label4)。
    /// 老端 SetData(type,data):按 type 切 _box_title/_box_desc 显隐并写文字;UpdateBtn 据 SettingModel.CheckWxSubsState 切 _btn(未订阅可点)/_Label4(已订阅提示)显隐;点击 _btn 走平台微信订阅 PlatformManager.WeChatSubscriptionSH。
    ///
    /// 降级:SettingModel(订阅状态/UPDATE_SUBSCRIPTION 事件)、PlatformManager 微信订阅、SysInfo 提示均未移植 →
    /// 本类只做最小可用:SetData(title,desc) 同时写标题 _Label3 与描述 _Label6;_btn 仅绑日志占位(BindBtn);OnInit 无特殊处理。Null-guard 每处访问。
    /// </summary>
    public sealed class SettingSubscriptionItem : SettingSubscriptionItemBind
    {
        protected override void OnInit()
        {
            // _btn:老端点击走微信订阅,数据未移植 → 先绑日志占位。
            BindBtn(_btn, "订阅");
        }

        /// <summary>
        /// 填充订阅项文字(对标老端 SetData 的写文字部分):
        /// title → 分组标题 _Label3;desc → 订阅描述 _Label6。两路文字独立 null-guard,空值兜底空串。
        /// </summary>
        public void SetData(string title, string desc)
        {
            if (_Label3 != null) _Label3.text = title ?? string.Empty;
            if (_Label6 != null) _Label6.text = desc ?? string.Empty;
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
