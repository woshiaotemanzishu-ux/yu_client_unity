#if UNITY_EDITOR
using Shenxiao.Common.UI3D;
using UnityEditor;

namespace Shenxiao.Editor.Debugging
{
    /// <summary>
    /// 头饰调参浮层(ArtModelTuner)的开关菜单。默认隐藏;需要调头饰相对身体的位置/旋转/缩放时,
    /// 点「神霄/调试/头饰调参浮层」打勾 → 进选角页就出「头饰调参」按钮。开关只在当前脚本域有效,
    /// 编译或重启 Unity 后自动关闭,避免调试面板遗留到正常验收画面。
    /// 工具代码保留不删,平时不显示、不进正式包(ArtModelTuner 本身 #if UNITY_EDITOR||DEVELOPMENT_BUILD)。
    /// </summary>
    [InitializeOnLoad]
    internal static class ArtModelTunerMenu
    {
        private const string MenuPath = "神霄/调试/头饰调参浮层";

        static ArtModelTunerMenu()
        {
            ArtModelTuner.Enabled = false;
            Menu.SetChecked(MenuPath, false);
        }

        [MenuItem(MenuPath, priority = 200)]
        private static void Toggle()
        {
            bool on = !ArtModelTuner.Enabled;
            ArtModelTuner.Enabled = on;
            if (!on) ArtModelTuner.Detach();
            Menu.SetChecked(MenuPath, on);
        }

        [MenuItem(MenuPath, validate = true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, ArtModelTuner.Enabled);
            return true;
        }
    }
}
#endif
