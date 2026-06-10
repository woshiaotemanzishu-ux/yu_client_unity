using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// 第二步:Bind 脚本编译通过后,把组件挂上 prefab 并按节点名回填字段引用。
    /// (生成 cs 和回填必须分两步——新脚本要等 Unity 编译一轮才能 AddComponent。)
    /// </summary>
    public static class LayaBindFiller
    {
        public static void FillModule(string module)
        {
            LayaUIManifest manifest = LayaUIManifest.Load();
            if (manifest == null) return;
            string moduleDir = manifest.ModuleDir(module);
            string folder = LayaUISettings.PREFAB_ROOT + "/" + moduleDir;
            if (!Directory.Exists(folder))
            {
                Debug.LogError("[LayaUI] 目录不存在: " + folder);
                return;
            }
            int ok = 0, miss = 0;
            foreach (string file in Directory.GetFiles(folder, "*.prefab", SearchOption.TopDirectoryOnly))
            {
                string path = file.Replace('\\', '/');
                if (FillOne(path, moduleDir)) ok++; else miss++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[LayaUI] 回填完成: 成功 " + ok + ",缺 Bind 类 " + miss + "(缺的看 Console 前面的警告)");
        }

        private static bool FillOne(string prefabPath, string moduleDir)
        {
            string name = Path.GetFileNameWithoutExtension(prefabPath);
            Type bindType = FindBindType("Shenxiao.Generated.UI." + moduleDir + "." + name + "Bind");
            if (bindType == null)
            {
                Debug.LogWarning("[LayaUI] 找不到 Bind 类(还没编译?): " + name + "Bind");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Component bind = root.GetComponent(bindType);
                if (bind == null) bind = root.AddComponent(bindType);

                List<LayaBindGenerator.FieldInfo> fields = new List<LayaBindGenerator.FieldInfo>();
                LayaUIReport dummy = new LayaUIReport("bindfill");
                LayaBindGenerator.Collect(root.transform, root.transform, fields, new HashSet<string>(), dummy, name);

                SerializedObject so = new SerializedObject(bind);
                foreach (LayaBindGenerator.FieldInfo f in fields)
                {
                    SerializedProperty prop = so.FindProperty(f.FieldName);
                    if (prop == null) continue;
                    Transform t = root.transform.Find(f.NodePath);
                    if (t == null) continue;
                    prop.objectReferenceValue = ResolveRef(t, f.TypeName);
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static UnityEngine.Object ResolveRef(Transform t, string typeName)
        {
            switch (typeName)
            {
                case "TMP_InputField": return t.GetComponent<TMP_InputField>();
                case "TextMeshProUGUI": return t.GetComponent<TextMeshProUGUI>();
                case "ScrollRect": return t.GetComponent<ScrollRect>();
                case "Image": return t.GetComponent<Image>();
                case "GameObject": return t.gameObject;
                default: return t as RectTransform;
            }
        }

        private static Type FindBindType(string fullName)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }
    }
}
