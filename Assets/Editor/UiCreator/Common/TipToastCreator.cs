using System.IO;
using Shenxiao.Common.Tips;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Common
{
    /// <summary>
    /// 通用飘字提示(Toast)纯代码建树生成器 —— 对标老端 sysInfo/MessageItem.scene(「恭喜登录成功」这类)。
    ///
    /// 结构:全屏透明根(挂 TipToastView) + ToastTemplate 模板(默认隐):
    ///   九宫格底图 _img_tips(mainui_ui_45,222×26,切 10,10,10,10) + 居中白色粗体文案(老端视觉 16px)。
    /// 运行时 TipsManager.Toast 按模板克隆逐条播放(缩放淡入 0.3s → 停 2s,新条来时旧条上顶 30px → 消失),
    /// 动画参数在 TipToastView 组件上、样式在 ToastTemplate 节点里,均可手调后直接生效(不用重新生成)。
    /// 模板 anchoredPosition 即出生点(老端 y=600(顶部起) ≈ 中心坐标 +55)。
    /// 尺寸/位置为 720×1280 起步值。入口在「神霄/重构UI 生成器」面板。
    /// </summary>
    public static class TipToastCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Common/TipToastView.prefab";

        // 老端源图:九宫格底图(222×26,sizeGrid 10,10,10,10;已从 yu_client 拷入 GameRes)
        private const string IMG_BG = "resource/game/sysInfo/texture/mainui_ui_45.png";

        // 模板布局(720×1280,中心锚;老端 MessageItem 底图 222×26,出生点 y=600(顶部起)≈中心 +55)
        private const float TplW = 222f, TplH = 26f, TplY = 55f;
        private const float FontSize = 16f;   // 老端 32 * scale0.5 = 视觉 16px 白色粗体

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Common",
                Name = "TipToast(通用飘字提示)",
                Note = "居中飘字「恭喜登录成功」:模板样式手调,TipsManager.Toast 全局入口",
                Order = 10,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            // 整棵树在 root 未激活时构建,建完再激活(对齐其它 Creator)。
            RectTransform root = UiCreatorKit.NewRoot("TipToastView");
            root.gameObject.SetActive(false);
            var view = root.gameObject.AddComponent<TipToastView>();

            // ---------- 飘字模板(默认隐;克隆源,样式在此手调) ----------
            RectTransform tpl = UiCreatorKit.NewNode("ToastTemplate", root);
            UiCreatorKit.Place(tpl, 0f, TplY, TplW, TplH);
            var item = tpl.gameObject.AddComponent<TipToastItem>();

            var cg = tpl.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;   // 飘字不挡点击
            cg.interactable = false;
            item.canvasGroup = cg;

            // 九宫格底图(铺满模板,宽度随文本自适应由运行时改模板宽实现)
            Image bg = UiCreatorKit.NewImage("Bg", tpl);
            UiCreatorKit.Stretch(bg.rectTransform);
            bg.raycastTarget = false;
            TrySetSpriteSliced(bg, IMG_BG, new Vector4(10f, 10f, 10f, 10f));
            item.bg = bg;

            // 文案(白色粗体居中,支持富文本;字号/颜色自行手调)
            TextMeshProUGUI label = UiCreatorKit.NewText("Label", tpl, "恭喜登录成功");
            UiCreatorKit.Stretch(label.rectTransform);
            label.fontSize = FontSize;
            label.fontStyle = FontStyles.Bold;
            label.richText = true;
            item.label = label;

            tpl.gameObject.SetActive(false);
            view.template = item;

            root.gameObject.SetActive(true);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log("[UiCreator] TipToastView.prefab 已生成: " + PrefabPath +
                      "(全局入口 TipsManager.Toast;真机包前记得跑 Addressable 自动分组)");
        }

        /// <summary>
        /// 贴九宫格图:确保导入为 Sprite 且写好 spriteBorder/FullRect,再经 TrySetSprite 贴上并切 Sliced。
        /// 贴不到(缺图)回退占位色,Image.Type 保持 Simple。
        /// </summary>
        private static void TrySetSpriteSliced(Image img, string gameResRelPath, Vector4 border)
        {
            string assetPath = UiCreatorKit.GameResRoot + gameResRelPath;
            if (AssetImporter.GetAtPath(assetPath) == null && File.Exists(assetPath))
            {
                AssetDatabase.ImportAsset(assetPath);   // 刚拷入还没进库:先导一次
            }
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter ti)
            {
                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                bool dirty = settings.textureType != TextureImporterType.Sprite
                             || settings.spriteMeshType != SpriteMeshType.FullRect
                             || ti.spriteBorder != border;
                if (dirty)
                {
                    settings.textureType = TextureImporterType.Sprite;
                    settings.spriteMeshType = SpriteMeshType.FullRect;   // 九宫格拉伸需 FullRect
                    ti.SetTextureSettings(settings);
                    ti.spriteBorder = border;
                    ti.SaveAndReimport();
                }
            }
            if (UiCreatorKit.TrySetSprite(img, gameResRelPath, UiCreatorKit.Palette.Panel))
            {
                img.type = Image.Type.Sliced;
            }
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览 TipToast",
                    "请先进入 Play 模式(登录场景已起、UI 层已初始化)再点预览。\n\n" +
                    "预览会释放旧实例、从最新 prefab 重建,并连发 3 条示例飘字(可看排队+上顶)。",
                    "好");
                return;
            }
            // 释放上次预览缓存的实例,确保每次都从【最新】prefab 重新实例化。
            TipsManager.ReloadView();
            TipsManager.Toast("恭喜登录成功");
            TipsManager.Toast("恭喜注册成功");
            TipsManager.Toast("<color=#88ff43>富文本示例:获得 崩玉×10</color>");
        }
    }
}
