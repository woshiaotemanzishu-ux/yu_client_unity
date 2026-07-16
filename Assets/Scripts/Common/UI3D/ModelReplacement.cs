using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using UnityEngine;

namespace Shenxiao.Common.UI3D
{
    /// <summary>
    /// 模型替换清单(运行时侧;资产管理窗口按动作逐条配置写入):
    /// `resource/config/client/model_replacement.json`,按 (module, id, action) 查新模型 prefab 的
    /// Addressables key。契约:**某动作配了新 prefab → 该动作用新模型;没配 → 原始管线**,
    /// 没有全局开关,生效方完全由配置逐条决定(打包线也按此剔除未用侧)。
    /// 编辑器下直接读工程文件(资产管理改完立刻生效,不依赖 Addressables 登记);真机走 ResManager。
    /// </summary>
    public static class ModelReplacement
    {
        private const string CONFIG_KEY = "resource/config/client/model_replacement";
        private const string EDITOR_FILE = "Assets/GameRes/resource/config/client/model_replacement.json";

        [Serializable]
        public class ActionOverride
        {
            public string action;     // 动作名(小写):idle/run/attack/…
            public string prefabKey;  // 新模型 prefab 的 Addressables key:object/role/role_1213/1213@idle
        }

        [Serializable]
        public class Entry
        {
            public string key;        // "{module}/{id}":role/1213、head/1213、weapon/1200
            public Vector3 attachmentPositionOffset; // 动态部件定位骨的挂点局部位置校准(默认 0)
            public Vector3 attachmentRotationOffset; // 动态部件 prefab 根的挂点局部旋转校准(默认 0)
            public float attachmentScale = 1f;        // 动态部件相对挂点缩放(默认 1)
            public List<ActionOverride> actions = new List<ActionOverride>();
        }

        [Serializable]
        public class Data
        {
            public int version = 2;
            public List<Entry> entries = new List<Entry>();
        }

        private static Data _data;
        private static Task _loading;

        public static async Task EnsureLoaded()
        {
            if (_data != null) return;
            if (_loading == null) _loading = LoadAsync();
            await _loading;
        }

        private static async Task LoadAsync()
        {
#if UNITY_EDITOR
            // 编辑器直读文件:资产管理刚写完立刻可见,免登记/免重启
            try
            {
                if (System.IO.File.Exists(EDITOR_FILE))
                    _data = JsonUtility.FromJson<Data>(System.IO.File.ReadAllText(EDITOR_FILE));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ModelReplacement] 清单解析失败:" + e.Message);
            }
            _data = _data ?? new Data();
            await Task.CompletedTask;
#else
            TextAsset txt = await ResManager.LoadOptionalAsync<TextAsset>(CONFIG_KEY);
            try { _data = txt != null ? JsonUtility.FromJson<Data>(txt.text) : null; }
            catch { _data = null; }
            _data = _data ?? new Data();
#endif
        }

        /// <summary>该模型是否有任何动作配了新 prefab(决定要不要上混合驱动器)。须先 EnsureLoaded。</summary>
        public static bool HasEntry(string module, int id)
        {
            if (_data == null) return false;
            string entryKey = module + "/" + id;
            for (int i = 0; i < _data.entries.Count; i++)
            {
                Entry e = _data.entries[i];
                if (e != null && e.key == entryKey && e.actions != null && e.actions.Count > 0) return true;
            }
            return false;
        }

        /// <summary>该 (module,id,action) 配置的新模型 prefab key;没配返回 null(=用原始)。须先 EnsureLoaded。</summary>
        public static string GetPrefabKey(string module, int id, string action)
        {
            if (_data == null || string.IsNullOrEmpty(action)) return null;
            string entryKey = module + "/" + id;
            for (int i = 0; i < _data.entries.Count; i++)
            {
                Entry e = _data.entries[i];
                if (e == null || e.key != entryKey) continue;
                for (int j = 0; j < e.actions.Count; j++)
                {
                    ActionOverride o = e.actions[j];
                    if (o != null && string.Equals(o.action, action, StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrEmpty(o.prefabKey))
                    {
                        return o.prefabKey;
                    }
                }
                return null;
            }
            return null;
        }

        /// <summary>动态部件定位骨的挂点局部位置校准;未配置返回零。</summary>
        public static Vector3 GetAttachmentPositionOffset(string module, int id)
        {
            if (_data == null) return Vector3.zero;
            string entryKey = module + "/" + id;
            for (int i = 0; i < _data.entries.Count; i++)
            {
                Entry e = _data.entries[i];
                if (e != null && e.key == entryKey) return e.attachmentPositionOffset;
            }
            return Vector3.zero;
        }

        /// <summary>动态部件 prefab 根的挂点局部旋转校准;未配置返回零。</summary>
        public static Vector3 GetAttachmentRotationOffset(string module, int id)
        {
            if (_data == null) return Vector3.zero;
            string entryKey = module + "/" + id;
            for (int i = 0; i < _data.entries.Count; i++)
            {
                Entry e = _data.entries[i];
                if (e != null && e.key == entryKey) return e.attachmentRotationOffset;
            }
            return Vector3.zero;
        }

        /// <summary>动态部件相对挂点缩放;未配置或非法值返回 1。</summary>
        public static float GetAttachmentScale(string module, int id)
        {
            if (_data == null) return 1f;
            string entryKey = module + "/" + id;
            for (int i = 0; i < _data.entries.Count; i++)
            {
                Entry e = _data.entries[i];
                if (e != null && e.key == entryKey)
                    return e.attachmentScale > 0.01f ? e.attachmentScale : 1f;
            }
            return 1f;
        }

#if UNITY_EDITOR
        /// <summary>资产管理写清单后调:下次 EnsureLoaded 重读(编辑器专用)。</summary>
        public static void InvalidateCache()
        {
            _data = null;
            _loading = null;
        }
#endif
    }
}
