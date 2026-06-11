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
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                int filled = 0;
                // 单窗口 prefab:Bind 在根上;合并 prefab:Bind 在各窗口子根上
                if (FillWindow(root.transform, root.transform.name, moduleDir)) filled++;
                for (int i = 0; i < root.transform.childCount; i++)
                {
                    Transform child = root.transform.GetChild(i);
                    if (child.name == "__Templates") continue;
                    if (FillWindow(child, child.name, moduleDir)) filled++;
                }
                if (filled == 0)
                {
                    Debug.LogWarning("[LayaUI] " + prefabPath + " 没匹配到任何 Bind 类(还没编译?)");
                    return false;
                }
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 给一个窗口根挂 Bind 组件并按节点名回填字段。没有对应类时返回 false。
        /// 优先挂业务子类(如 LoginEnterView : LoginEnterViewBind),业务逻辑才会随 prefab 实例化。
        /// </summary>
        private static bool FillWindow(Transform windowRoot, string windowName, string moduleDir)
        {
            Type bindType = FindBindType("Shenxiao.Generated.UI." + moduleDir + "." + windowName + "Bind");
            if (bindType == null) return false;
            Type concreteType = FindSingleSubclass(bindType) ?? bindType;

            Component bind = windowRoot.GetComponent(bindType);
            if (bind != null && concreteType != bind.GetType())
            {
                UnityEngine.Object.DestroyImmediate(bind, true); // 升级成业务子类
                bind = null;
            }
            if (bind == null) bind = windowRoot.gameObject.AddComponent(concreteType);

            List<LayaBindGenerator.FieldInfo> fields = new List<LayaBindGenerator.FieldInfo>();
            LayaUIReport dummy = new LayaUIReport("bindfill");
            LayaBindGenerator.Collect(windowRoot, windowRoot, fields, new HashSet<string>(), dummy, windowName);

            SerializedObject so = new SerializedObject(bind);
            foreach (LayaBindGenerator.FieldInfo f in fields)
            {
                SerializedProperty prop = so.FindProperty(f.FieldName);
                if (prop == null) continue;
                Transform t = windowRoot.Find(f.NodePath);
                if (t == null) continue;
                prop.objectReferenceValue = ResolveRef(t, f.TypeName);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
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

        /// <summary>找 Bind 的唯一业务子类;0 个返回 null,多个报错并用基类。</summary>
        private static Type FindSingleSubclass(Type bindType)
        {
            Type found = null;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }
                foreach (Type t in types)
                {
                    if (t == null || t.IsAbstract || !bindType.IsAssignableFrom(t) || t == bindType) continue;
                    if (found != null)
                    {
                        Debug.LogError("[LayaUI] " + bindType.Name + " 有多个业务子类(" + found.Name + ", " + t.Name + "),用基类回填");
                        return null;
                    }
                    found = t;
                }
            }
            return found;
        }
    }
}
