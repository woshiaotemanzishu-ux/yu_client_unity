#if UNITY_EDITOR
using Shenxiao.Common.UI3D;
using UnityEditor;

namespace Shenxiao.Editor.Debugging
{
    /// <summary>
    /// 头饰调参浮层(ArtModelTuner)的开关菜单。默认隐藏;需要调头饰相对身体的位置/旋转/缩放时,
    /// 点「神霄/调试/头饰调参浮层」打勾 → 进选角页就出「头饰调参」按钮。状态存 EditorPrefs,跨域重载/进 Play 保持。
    /// 工具代码保留不删,平时不显示、不进正式包(ArtModelTuner 本身 #if UNITY_EDITOR||DEVELOPMENT_BUILD)。
    /// </summary>
    [InitializeOnLoad]
    internal static class ArtModelTunerMenu
    {
        private const string Key = "shenxiao.artModelTuner.enabled";
        private const string MenuPath = "神霄/调试/头饰调参浮层";

        static ArtModelTunerMenu()
        {
            ArtModelTuner.Enabled = EditorPrefs.GetBool(Key, false);
        }

        [MenuItem(MenuPath, priority = 200)]
        private static void Toggle()
        {
            bool on = !EditorPrefs.GetBool(Key, false);
            EditorPrefs.SetBool(Key, on);
            ArtModelTuner.Enabled = on;
            Menu.SetChecked(MenuPath, on);
        }

        [MenuItem(MenuPath, validate = true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(Key, false));
            return true;
        }
    }
}
#endif
