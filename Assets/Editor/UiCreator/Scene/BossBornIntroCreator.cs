using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Scene;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.UiCreator.Scene
{
    /// <summary>可重复生成的大妖入场演出 prefab；生成后可直接在 Prefab Mode 手调，不会由运行时代码改布局。</summary>
    public static class BossBornIntroCreator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/Scene/BossBornIntro.prefab";
        private const string MaskTopPath = "resource/game/dungeonCommon/texture/mask.png";
        private const string MaskBottomPath = "resource/game/dungeonCommon/texture/mask1.png";

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UiRebuildRegistry.Register(new UiCreatorEntry
            {
                Module = "Scene",
                Name = "BossBornIntro(大妖来袭)",
                Note = "上下遮罩+effect_ui_dayaolaixi；位置/缩放/时序全部在 prefab 可调",
                Order = 10,
                Generate = Generate,
                Preview = Preview,
                PrefabPath = PrefabPath,
            });
        }

        public static void Generate()
        {
            RectTransform root = UiCreatorKit.NewRoot("BossBornIntro");
            BossBornEffectPlayer player = root.gameObject.AddComponent<BossBornEffectPlayer>();

            Image top = UiCreatorKit.NewImage("MaskTop", root);
            PlaceMask(top.rectTransform, top: true);
            top.raycastTarget = false;
            UiCreatorKit.TrySetSprite(top, MaskTopPath, Color.clear);
            top.color = new Color(1f, 1f, 1f, 0.8f);

            Image bottom = UiCreatorKit.NewImage("MaskBottom", root);
            PlaceMask(bottom.rectTransform, top: false);
            bottom.raycastTarget = false;
            UiCreatorKit.TrySetSprite(bottom, MaskBottomPath, Color.clear);
            bottom.color = new Color(1f, 1f, 1f, 0.8f);

            RectTransform host = UiCreatorKit.NewNode("BannerHost", root);
            UiCreatorKit.Place(host, 0f, 300f, 2000f, 1280f);
            UIEffectSlot slot = host.gameObject.AddComponent<UIEffectSlot>();
            const string effect = "effect_ui_dayaolaixi";
            slot.ConfigureEffect("dungeon_boss_intro", effect, GameResPath.GetUIEffectPrefabPath(effect),
                "yu_client DungeonFightSceneMaskView.scene + effect_ui_dayaolaixi.lh",
                "old host=2000x1280 centerY=-300; old AddUIEffect scale=1.5",
                Vector2.zero, Vector3.one * 1.5f, 0f, false, "dungeon_boss_intro");

            player.ConfigurePrefab(top.rectTransform, bottom.rectTransform, host, slot, 0.15f, 3f, 0.15f);
            GameObject saved = UiCreatorKit.SavePrefab(root.gameObject, PrefabPath);
            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
        }

        private static void PlaceMask(RectTransform rt, bool top)
        {
            rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rt.sizeDelta = new Vector2(150f, 670f);
            rt.anchoredPosition = Vector2.zero;
        }

        public static void Preview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("预览大妖来袭", "请先进入 Play Mode，再点预览。", "好");
                return;
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Transform parent = ViewManager.GetLayer(UILayer.Top);
            if (prefab == null || parent == null) return;
            GameObject go = Object.Instantiate(prefab, parent);
            go.GetComponent<BossBornEffectPlayer>()?.Begin(() => Object.Destroy(go));
        }
    }
}
