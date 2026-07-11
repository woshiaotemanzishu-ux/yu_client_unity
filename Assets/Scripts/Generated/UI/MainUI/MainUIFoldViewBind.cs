// 折叠协调器(太极 TurnDisk)的绑定。太极从 MainUIActivityView 拆出、提到 MainUIModule 根层,
// 因为它收放的不止活动区(活动/竞榜/次要图标/任务组队都跟着折,对标老端 CHANGE_ACTIVITY_STATE)。
// 挂在根层、不属任何会折叠的区域 → 折叠时太极自身始终可见、可再展开。
using UnityEngine.UI;
using Shenxiao.Framework.UI;

namespace Shenxiao.Generated.UI.MainUI
{
    public partial class MainUIFoldViewBind : BaseView
    {
        public Image _img_turn; // 太极折叠钮(TurnDisk)

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_turn), _img_turn);
        }
    }
}
