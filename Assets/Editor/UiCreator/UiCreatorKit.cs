using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator
{
    /// <summary>
    /// 纯代码建树脚手架(共享给重构后各 UI 模块的 *Creator 用)。
    ///
    /// 定位:用代码 new 出一棵【干净、可手改、看得见】的 Unity UI 树并存成 prefab,取代 Laya
    /// 转换器产出的碎片化镜像结构。这里提供建节点 / 贴图 / 布局 / 存盘的【积木】。
    ///
    /// 关于样式(2026-06-30 用户纠正):
    /// - 建树期(本类 + 各 Creator)【必须】把元素做得看得见、能区分:优先贴老端真实源图
    ///   (TrySetSprite),拿不到再上带颜色的占位框/按钮(fallback color)。绝不留透明白块。
    /// - 运行时 View 逻辑则相反:不许改颜色/字号/尺寸等样式(那是用户在编辑器手调的)。
    ///
    /// 设计分辨率 720×1280(竖屏),节点统一用「中心锚 + anchoredPosition」摆位,
    /// 全屏层(背景/子面板)用 Stretch 铺满父层。所有视觉值均为可手改的起步默认值。
    /// </summary>
    public static class UiCreatorKit
    {
        public const float DesignWidth = 720f;
        public const float DesignHeight = 1280f;
        public const string ProjectFontPath = "Assets/_App/Fonts/FZYHJW SDF.asset";
        public const string GameResRoot = "Assets/GameRes/";

        /// <summary>建树期占位配色(贴不到真图时用,保证看得见、点得到)。</summary>
        public static class Palette
        {
            public static readonly Color Bg = new Color(0.13f, 0.15f, 0.20f, 1f);     // 深石板:背景
            public static readonly Color Panel = new Color(0.20f, 0.22f, 0.28f, 1f);  // 面板/容器
            public static readonly Color Input = new Color(0.92f, 0.92f, 0.92f, 1f);  // 输入框(浅)
            public static readonly Color BtnPrimary = new Color(0.90f, 0.55f, 0.20f, 1f); // 主按钮(橙)
            public static readonly Color BtnSecond = new Color(0.30f, 0.55f, 0.85f, 1f);  // 次按钮(蓝)
            public static readonly Color BtnNeutral = new Color(0.45f, 0.48f, 0.55f, 1f); // 中性按钮(灰)
            public static readonly Color Mark = new Color(0.30f, 0.80f, 0.45f, 1f);   // 勾选/标记(绿)
            public static readonly Color InputText = new Color(0.10f, 0.10f, 0.10f, 1f);  // 输入文字(深)
            public static readonly Color InputHint = new Color(0.45f, 0.45f, 0.45f, 1f);  // 占位提示(灰)
        }

        // ---------------------------------------------------------------- 节点创建

        /// <summary>建一个游离根节点(全屏 Stretch);实例化到层下时会铺满该层。</summary>
        public static RectTransform NewRoot(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            Stretch(rt);
            return rt;
        }

        /// <summary>建一个空 RectTransform 容器(纯布局盒,无图形)。</summary>
        public static RectTransform NewNode(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>建一张 Image(默认占位色,随后用 TrySetSprite 贴真图)。</summary>
        public static Image NewImage(string name, Transform parent)
        {
            RectTransform rt = NewNode(name, parent);
            return rt.gameObject.AddComponent<Image>();
        }

        /// <summary>建一段 TMP 文本(挂项目字体;字号/对齐/颜色为起步默认值,自行调)。</summary>
        public static TextMeshProUGUI NewText(string name, Transform parent, string text)
        {
            RectTransform rt = NewNode(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null) t.font = Font;
            t.text = text;
            t.enableAutoSizing = false;
            t.fontSize = 32f;                              // 起步默认值
            t.alignment = TextAlignmentOptions.Center;     // 起步默认值
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>
        /// 建一个 TMP_InputField(含 Text Area / Placeholder / Text 标准子结构并接好引用)。
        /// 关键:整棵子树在【父节点未激活】时构建,避免 TMP_InputField.OnEnable 早于接引用而刷错。
        /// 输入框底图默认占位浅色(随后可 TrySetSprite 贴 bg_05);文字/占位上深色保证可见。
        /// </summary>
        public static TMP_InputField NewInput(string name, Transform parent, string placeholder, bool password)
        {
            RectTransform rt = NewNode(name, parent);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = Palette.Input;                       // 起步占位浅色(贴图后变白)

            RectTransform area = NewNode("Text Area", rt);
            StretchPadding(area, 14f, 6f);
            area.gameObject.AddComponent<RectMask2D>();

            TextMeshProUGUI ph = NewText("Placeholder", area, placeholder);
            Stretch(ph.rectTransform);
            ph.alignment = TextAlignmentOptions.Left;
            ph.fontStyle = FontStyles.Italic;
            ph.color = Palette.InputHint;

            TextMeshProUGUI txt = NewText("Text", area, string.Empty);
            Stretch(txt.rectTransform);
            txt.alignment = TextAlignmentOptions.Left;
            txt.color = Palette.InputText;

            var input = rt.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = txt;
            input.placeholder = ph;
            input.targetGraphic = bg;
            input.transition = Selectable.Transition.None;
            input.lineType = TMP_InputField.LineType.SingleLine;
            if (password) input.contentType = TMP_InputField.ContentType.Password;
            input.text = string.Empty;
            return input;
        }

        /// <summary>
        /// 建一个按钮盒:一张可点击底图 Image + 居中 Label 子节点。
        /// 返回 (容器 rt, 底图 bg=命中体, 文字 label);业务侧把点击绑到 bg 上,Creator 用 TrySetSprite 贴图。
        /// </summary>
        public static ButtonParts NewButton(string name, Transform parent, string text)
        {
            RectTransform rt = NewNode(name, parent);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = Palette.BtnNeutral;                  // 起步占位色(贴图后变白)
            bg.raycastTarget = true;
            TextMeshProUGUI label = NewText("Label", rt, text);
            Stretch(label.rectTransform);
            return new ButtonParts { root = rt, bg = bg, label = label };
        }

        public struct ButtonParts
        {
            public RectTransform root;
            public Image bg;
            public TextMeshProUGUI label;
        }

        // ---------------------------------------------------------------- 贴真图(建树期)

        /// <summary>
        /// 给 Image 贴老端真实源图(从 Assets/GameRes/ 下按相对路径找);找不到则回退到 fallback 占位色,
        /// 绝不留白。返回是否贴到了真图。
        /// gameResRelPath 例:"resource/game/login/texture/bg_05.png"。
        /// 资产存在但被导成非 Sprite(常见于 jpg 背景)时,自动改成 Sprite 类型再读(对标 ResManager 兜底)。
        /// </summary>
        public static bool TrySetSprite(Image img, string gameResRelPath, Color fallback)
        {
            if (img == null) return false;
            string assetPath = GameResRoot + gameResRelPath;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null && File.Exists(assetPath))
            {
                if (AssetImporter.GetAtPath(assetPath) is TextureImporter ti
                    && ti.textureType != TextureImporterType.Sprite)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.SaveAndReimport();
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                }
            }

            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
                return true;
            }

            // 拿不到图也别留白:上彩色占位,看得见、点得到。
            img.sprite = null;
            img.color = fallback;
            Debug.LogWarning("[UiCreator] 未找到源图,改用占位色: " + assetPath);
            return false;
        }

        // ---------------------------------------------------------------- 布局(仅建树期,起步值)

        /// <summary>中心锚摆位:x/y 相对父中心,w/h 为尺寸。</summary>
        public static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>铺满父节点(四边贴齐)。</summary>
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>铺满父节点但留内边距(给 Text Area 用)。</summary>
        public static void StretchPadding(RectTransform rt, float padX, float padY)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }

        // ---------------------------------------------------------------- 字体

        private static TMP_FontAsset _font;
        private static bool _fontLoaded;

        public static TMP_FontAsset Font
        {
            get
            {
                if (!_fontLoaded)
                {
                    _fontLoaded = true;
                    _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ProjectFontPath);
                    if (_font == null)
                    {
                        Debug.LogWarning("[UiCreator] 未找到项目字体 " + ProjectFontPath + ",TMP 文本将用默认字体(可能不显中文)");
                    }
                }
                return _font;
            }
        }

        // ---------------------------------------------------------------- 存盘

        /// <summary>
        /// 把内存里建好的根节点存成 prefab,然后销毁内存对象。
        /// View 上指向子节点的 public 引用会随 SaveAsPrefabAsset 一并序列化进 prefab。
        /// </summary>
        public static GameObject SavePrefab(GameObject root, string assetPath)
        {
            string dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return saved;
        }
    }
}
