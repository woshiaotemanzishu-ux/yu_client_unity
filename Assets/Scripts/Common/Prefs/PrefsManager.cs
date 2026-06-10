using UnityEngine;

namespace Shenxiao.Common.Prefs
{
    /// <summary>
    /// Local persistent storage. Wraps UnityEngine.PlayerPrefs with consistent typed access.
    /// </summary>
    public static class PrefsManager
    {
        public static int GetInt(string key, int fallback = 0) => PlayerPrefs.GetInt(key, fallback);
        public static void SetInt(string key, int value) { PlayerPrefs.SetInt(key, value); PlayerPrefs.Save(); }

        public static float GetFloat(string key, float fallback = 0f) => PlayerPrefs.GetFloat(key, fallback);
        public static void SetFloat(string key, float value) { PlayerPrefs.SetFloat(key, value); PlayerPrefs.Save(); }

        public static string GetString(string key, string fallback = "") => PlayerPrefs.GetString(key, fallback);
        public static void SetString(string key, string value) { PlayerPrefs.SetString(key, value); PlayerPrefs.Save(); }

        public static bool GetBool(string key, bool fallback = false) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) != 0;
        public static void SetBool(string key, bool value) { PlayerPrefs.SetInt(key, value ? 1 : 0); PlayerPrefs.Save(); }

        public static bool Has(string key) => PlayerPrefs.HasKey(key);
        public static void Remove(string key) { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
    }
}
