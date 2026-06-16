using Shenxiao.Framework.Res;
using Shenxiao.Generated.UI.MainUI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 技能条(对标老客户端 MainUISkillView.ts)。
    /// SkillManager/技能列表尚未移植:本轮不生成技能按钮、不造假技能数据。只还原自动战斗按钮的
    /// 非自动默认图(UpdateAutoFightState:非自动取 base_file+"_asset" 下的 "uizjmgj_003a")。
    /// </summary>
    public sealed class MainUISkillView : MainUISkillViewBind
    {
        protected override void OnInit()
        {
            // 老客户端 SetImageSprite 使用 mainUI_asset 图集名;Unity 已拆成 mainUI/texture 散图地址。
            _ = ResManager.SetImageAsync(_img_auto_fight, GameResPath.GetIcon("mainUI", "uizjmgj_003a"), nativeSize: false);
        }
    }
}
