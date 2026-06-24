using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 神祇(变身)技能按钮(对标老客户端 MainUISkillItemGod.ts):点击发起变身,带 CD/持续时间圆形遮罩与变身特效。
    ///
    /// 降级:GodBefallModel(变身数据 / 协议 44011 / KEEP_TIME·UPDATE_BATTLE_ID 事件)、服务器时间、
    /// 圆形 mask pie 动画、变身特效 UI_2049 均未移植 → 只保留图标 + 点击打日志;CD 遮罩 _img_mask、
    /// 持续遮罩 _img_keep、CD 文字 _lb_cd、特效盒 _gp_eff 先隐藏/清空。数据/协议移植后再补 timer/mask/effect。
    /// </summary>
    public sealed class MainUISkillItemGod : MainUISkillItemGodBind
    {
        private bool _clickBound;

        protected override void OnInit()
        {
            HideDynamic();
            BindClickOnce();
        }

        /// <summary>设变身图标(对标 GetIconOtherPath("godBefallIcon", icon_id));id 空则不动。</summary>
        public void SetGodIcon(string iconId)
        {
            if (icon != null && !string.IsNullOrEmpty(iconId))
                _ = ResManager.SetImageAsync(icon, GameResPath.GetIconOtherPath("godBefallIcon", iconId), nativeSize: false);
        }

        private void HideDynamic()
        {
            if (_img_mask != null) _img_mask.gameObject.SetActive(false);
            if (_img_keep != null) _img_keep.gameObject.SetActive(false);
            if (_lb_cd != null) _lb_cd.text = "";
        }

        private void BindClickOnce()
        {
            if (_clickBound || bg == null) return;
            bg.raycastTarget = true;
            UIUtil.AddClick(bg, OnClickGod);
            _clickBound = true;
        }

        private void OnClickGod()
        {
            // TODO 待接:搬砖中/变身中/冷却中拦截;否则 GodBefallModel.Fire(REQUEST_PROTO, 44011)。
            GameLog.Info("MainUI", "点击神祇变身 → 打开 GodBefall 面板;变身协议 44011 待 GodBefallModel 接入");
            MainUIRouter.Open("232");
        }
    }
}
