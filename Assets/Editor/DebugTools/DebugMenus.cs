using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.DebugTools
{
    /// <summary>本地调试小工具。</summary>
    public static class DebugMenus
    {
        [MenuItem("神霄/调试/清除本地偏好(PlayerPrefs)", priority = 100)]
        public static void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[Debug] PlayerPrefs 已清空(记住的账号、协议同意状态等会复位)");
        }
    }
}
