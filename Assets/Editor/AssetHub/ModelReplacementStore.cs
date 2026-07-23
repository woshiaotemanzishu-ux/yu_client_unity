using System;
using System.Collections.Generic;
using System.IO;
using Shenxiao.Common.UI3D;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.AssetHub
{
    /// <summary>
    /// 模型替换清单(编辑器写入侧;数据类型/格式与运行时 ModelReplacement 共用,唯一事实源):
    /// **按动作逐条配置**——某 (module/id, action) 配了新 prefab 就用新,没配用原始,没有全局开关。
    /// 文件:Assets/GameRes/resource/config/client/model_replacement.json(随包可热更)。
    /// 打包规则:清单没引用的新资源剔除;原始资源只有在"该模型全部原始动作都被新覆盖"时才可剔。
    /// 运行时消费:RoleModelAssembler.BuildAsync(有配置走新模型整装,回落原始管线)。
    /// </summary>
    public static class ModelReplacementStore
    {
        public const string FilePath = "Assets/GameRes/resource/config/client/model_replacement.json";

        private static ModelReplacement.Data _pendingData;
        private static bool _saveHooked;
        private static int _saveAttempts;
        private static double _nextSaveTime;

        private static ModelReplacement.Data Load()
        {
            // OnGUI/导入回调连续改多个动作时，磁盘写入会延后一帧；后续修改必须继续基于
            // 尚未落盘的数据，不能重新读旧文件把前一个动作覆盖掉。
            if (_pendingData != null) return _pendingData;
            if (!File.Exists(FilePath)) return new ModelReplacement.Data();
            try
            {
                return JsonUtility.FromJson<ModelReplacement.Data>(File.ReadAllText(FilePath))
                       ?? new ModelReplacement.Data();
            }
            catch
            {
                return new ModelReplacement.Data();
            }
        }

        private static void Save(ModelReplacement.Data data)
        {
            // Unity 资源刷新/OnGUI 期间直接 File.WriteAllText 会触发 Win32 IO 1224，且随后破坏
            // GUILayout 状态。合并本帧的配置修改，等 AssetDatabase 空闲后一次写入并自动重试。
            _pendingData = data;
            _saveAttempts = 0;
            _nextSaveTime = EditorApplication.timeSinceStartup;
            if (_saveHooked) return;
            _saveHooked = true;
            EditorApplication.update += FlushPendingSave;
        }

        private static void FlushPendingSave()
        {
            if (_pendingData == null)
            {
                StopSaveLoop();
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.timeSinceStartup < _nextSaveTime)
                return;

            try
            {
                ModelReplacement.Data saving = _pendingData;
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath) ?? ".");
                File.WriteAllText(FilePath, JsonUtility.ToJson(saving, true));
                if (ReferenceEquals(_pendingData, saving)) _pendingData = null;
                StopSaveLoop();
                ModelReplacement.InvalidateCache(); // 编辑器播放模式下一次 EnsureLoaded 重读
                AssetDatabase.ImportAsset(FilePath, ImportAssetOptions.ForceSynchronousImport);
            }
            catch (IOException exception)
            {
                _saveAttempts++;
                if (_saveAttempts < 40)
                {
                    _nextSaveTime = EditorApplication.timeSinceStartup + 0.25d;
                    return;
                }
                StopSaveLoop();
                Debug.LogException(new IOException(
                    $"模型替换清单在 {_saveAttempts} 次重试后仍无法写入:{FilePath}", exception));
            }
            catch (Exception exception)
            {
                StopSaveLoop();
                Debug.LogException(exception);
            }
        }

        private static void StopSaveLoop()
        {
            if (!_saveHooked) return;
            EditorApplication.update -= FlushPendingSave;
            _saveHooked = false;
        }

        /// <summary>该模型已配置的 动作→prefabKey 表(无配置返回空表)。</summary>
        public static Dictionary<string, string> GetActions(string module, string id)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ModelReplacement.Entry e = Load().entries.Find(x => x != null && x.key == module + "/" + id);
            if (e == null) return result;
            if (e.actions == null) return result;
            foreach (ModelReplacement.ActionOverride o in e.actions)
            {
                if (o != null && !string.IsNullOrEmpty(o.action) && !string.IsNullOrEmpty(o.prefabKey))
                    result[o.action] = o.prefabKey;
            }
            return result;
        }

        /// <summary>配置/更新一个动作的新模型;prefabKey 为空等价 RemoveAction。</summary>
        public static void SetAction(string module, string id, string action, string prefabKey)
        {
            if (string.IsNullOrEmpty(prefabKey))
            {
                RemoveAction(module, id, action);
                return;
            }
            ModelReplacement.Data data = Load();
            string key = module + "/" + id;
            ModelReplacement.Entry e = data.entries.Find(x => x != null && x.key == key);
            if (e == null)
            {
                e = new ModelReplacement.Entry { key = key };
                data.entries.Add(e);
            }
            ModelReplacement.ActionOverride o = e.actions.Find(x =>
                x != null && string.Equals(x.action, action, StringComparison.OrdinalIgnoreCase));
            if (o == null)
            {
                o = new ModelReplacement.ActionOverride { action = action.ToLowerInvariant() };
                e.actions.Add(o);
            }
            o.prefabKey = prefabKey;
            Save(data);
            Debug.Log($"[ModelReplacement] {key} {action} → {prefabKey}");
        }

        /// <summary>
        /// 一次写入一组动作，可同时覆盖老资源别名 ID。导入结束自动填槽必须走批量入口，
        /// 避免每个动作/别名各写一次文件，也避免 Unity 正在刷新时撞文件锁。
        /// </summary>
        public static void SetActions(string module, IEnumerable<string> ids,
            IDictionary<string, string> actions)
        {
            if (string.IsNullOrEmpty(module) || ids == null || actions == null || actions.Count == 0) return;
            ModelReplacement.Data data = Load();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string rawId in ids)
            {
                string id = (rawId ?? "").Trim();
                if (id.Length == 0 || !seen.Add(id)) continue;
                string key = module + "/" + id;
                ModelReplacement.Entry entry = data.entries.Find(x => x != null && x.key == key);
                if (entry == null)
                {
                    entry = new ModelReplacement.Entry { key = key };
                    data.entries.Add(entry);
                }
                if (entry.actions == null) entry.actions = new List<ModelReplacement.ActionOverride>();
                foreach (KeyValuePair<string, string> pair in actions)
                {
                    if (string.IsNullOrEmpty(pair.Key) || string.IsNullOrEmpty(pair.Value)) continue;
                    ModelReplacement.ActionOverride action = entry.actions.Find(x =>
                        x != null && string.Equals(x.action, pair.Key, StringComparison.OrdinalIgnoreCase));
                    if (action == null)
                    {
                        action = new ModelReplacement.ActionOverride { action = pair.Key.ToLowerInvariant() };
                        entry.actions.Add(action);
                    }
                    action.prefabKey = pair.Value;
                    Debug.Log($"[ModelReplacement] {key} {pair.Key} → {pair.Value}");
                }
            }
            Save(data);
        }

        /// <summary>还原一个动作(删配置=该动作回原始)。</summary>
        public static void RemoveAction(string module, string id, string action)
        {
            ModelReplacement.Data data = Load();
            ModelReplacement.Entry e = data.entries.Find(x => x != null && x.key == module + "/" + id);
            if (e == null) return;
            int n = e.actions.RemoveAll(x =>
                x != null && string.Equals(x.action, action, StringComparison.OrdinalIgnoreCase));
            if (e.actions.Count == 0) data.entries.Remove(e);
            if (n > 0)
            {
                Save(data);
                Debug.Log($"[ModelReplacement] {module}/{id} {action} → 还原原始");
            }
        }

        /// <summary>全部还原(清掉该模型全部动作配置;新资源保留在工程,打包按清单剔除)。</summary>
        public static void ClearEntry(string module, string id)
        {
            ModelReplacement.Data data = Load();
            int n = data.entries.RemoveAll(x => x != null && x.key == module + "/" + id);
            if (n > 0)
            {
                Save(data);
                Debug.Log($"[ModelReplacement] {module}/{id} → 全部还原原始");
            }
        }

        /// <summary>资产路径 → Addressables key(GameRes 相对小写去扩展);不在 GameRes 下返回 null。</summary>
        public static string PathToKey(string assetPath)
        {
            const string prefix = "Assets/GameRes/";
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith(prefix, StringComparison.Ordinal))
                return null;
            string rel = assetPath.Substring(prefix.Length);
            string ext = Path.GetExtension(rel);
            if (!string.IsNullOrEmpty(ext)) rel = rel.Substring(0, rel.Length - ext.Length);
            return rel.Replace('\\', '/').ToLowerInvariant();
        }

        /// <summary>key → 资产路径(编辑器展示/定位用;假定 .prefab)。</summary>
        public static string KeyToPrefabPath(string prefabKey)
        {
            return string.IsNullOrEmpty(prefabKey) ? null : $"Assets/GameRes/{prefabKey}.prefab";
        }
    }
}
