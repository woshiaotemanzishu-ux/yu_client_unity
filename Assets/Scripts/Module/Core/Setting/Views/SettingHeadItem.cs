using Shenxiao.Generated.UI.Setting;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Setting
{
    /// <summary>
    /// 设置-头像选择项(对标老客户端 setting/SettingHeadItem.ts):
    /// 一个头像选择格——头像图(_img_role_head)+ 名称(_lb_name)+ 选中态(_img_select,选中时可见)+ 使用中标记(use_tag,当前佩戴头像时可见)。
    /// 老端 dataChanged:写 _lb_name=data.desc、按 head/data.resource 设头像图;依据 RoleManager.HaveActiviteThisHead / mainRoleInfo.picture 判断
    /// 是否「使用中」切 use_tag(uixt_013 使用中 / uixt_012 未激活)、未激活按 cost 显示「N转获得」红字;监听 SettingModel.SELECT_HEAD_IMG_ITEM 互斥切选中;
    /// 点击 role_head/_Image1/_img_select → Fire(SELECT_HEAD_IMG_ITEM,id)(拍照项则 REQUEST_ROLE_PROTO 13082)。SetSelect(bool) 切 _img_select.visible。
    ///
    /// 降级:SettingModel(SELECT_HEAD_IMG_ITEM 事件/互斥/select_head_id)、RoleManager(头像激活/使用中判断、mainRoleInfo)、
    /// ResManager 头像图集加载、cost「N转获得」着色、拍照(13082)均未移植 → 本类只做最小可用:SetData 写名称文字 + 记录 id + 绑定点击回调(互斥由父级负责);
    /// SetSelected 仅切 _img_select 高亮;OnInit 先隐藏 _img_select / use_tag 两个 overlay。Null-guard 每处访问。
    /// </summary>
    public sealed class SettingHeadItem : SettingHeadItemBind
    {
        private int _id;
        private System.Action<int> _onClick;

        protected override void OnInit()
        {
            // 老端选中态/使用中标记由数据驱动,数据未移植 → 先隐藏两个 overlay。
            HideNode(_img_select);
            HideNode(use_tag);
        }

        /// <summary>
        /// 设置头像项数据(对标老端 dataChanged + load_callback 的点击绑定):
        /// 写入名称 _lb_name,记录头像 id,并把头像点击区(role_head 下的图 / _Image1 / _img_role_head)绑到 onClick(id)。
        /// 头像图集加载、使用中标记、N转获得着色等数据相关逻辑未移植,降级只写文字 + 绑点击。
        /// </summary>
        public void SetData(int id, string name, System.Action<int> onClick)
        {
            _id = id;
            _onClick = onClick;

            if (_lb_name != null) _lb_name.text = name ?? string.Empty;

            // 老端点击挂在容器 role_head(Box) 与 _Image1/_img_select 上;role_head 为 RectTransform 非 Image,优先用其下的头像图 _img_role_head 承接点击。
            Image btn = _img_role_head;
            if (btn == null) btn = _Image1;
            if (btn == null && role_head != null) btn = role_head.GetComponentInChildren<Image>(true);
            if (btn != null)
            {
                btn.raycastTarget = true;
                UIUtil.AddClick(btn, () =>
                {
                    GameLog.Info("Setting", "点击头像项[{0}] → 待对接(回调 id={1})", name, _id);
                    _onClick?.Invoke(_id);
                });
            }
        }

        /// <summary>设置选中态(对标老端 SetSelect):切换选中高亮 _img_select 的显隐。</summary>
        public void SetSelected(bool on)
        {
            if (_img_select != null) _img_select.gameObject.SetActive(on);
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
