using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 命运系统开启窗(对标老客户端 OpenDestinyView.ts):命运系统解锁时弹出的庆祝窗,点击关闭。
    ///
    /// 老端的 PlayAni 补间动画(circule/bg 旋转位移)与 UI 特效在源码里已整段注释(未启用),故本窗实质=
    /// 背景图 + 静态文案 + 点击关闭。降级:背景图照抄 Laya(uisc_002);标题/图标(title_img/icon_img/
    /// icon_img1)与文案(_Label1~4/bottom_label)为预制体静态件,归用户;特效(effect_gp)未移植留空。
    /// 事件驱动弹层(命运系统开启时开),默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class OpenDestinyView : OpenDestinyViewBind
    {
        protected override void OnInit()
        {
            // 老端 LoadSuccess:SetOutsideImageSprite(bg_img, GetIcon("mainUI","uisc_002"))。
            if (bg_img != null)
                _ = ResManager.SetImageAsync(bg_img, GameResPath.GetIcon("mainUI", "uisc_002"), nativeSize: false);

            // 对标 InitEvent:点 _img_click 关闭。
            if (_img_click != null)
            {
                _img_click.raycastTarget = true;
                UIUtil.AddClick(_img_click, OnClickClose);
            }
        }

        private void OnClickClose()
        {
            Hide();
        }
    }
}
