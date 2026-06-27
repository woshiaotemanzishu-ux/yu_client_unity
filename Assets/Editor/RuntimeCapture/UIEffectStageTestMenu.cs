using Shenxiao.Common.UI3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Editor.RuntimeCapture
{
    /// <summary>
    /// 决定性对照实验:同一个特效(ui_zhujiemianzhuanquan,scale 12.29,120px 盒),一份挂【独立置顶 Overlay 画布】(对照),
    /// 一份挂【真实 UIRoot/Main 画布】(=业务特效所在的真画布)。一次截图就能判:
    ///   · 两个都显示 → 真画布没问题,业务特效不显示是「盒太小 / 宿主图标 inactive」之类的摆位/激活问题;
    ///   · 只有 Overlay 显示、Main 不显示 → 真 Main 画布上 additive RawImage 被某种东西(Mask/材质/排序)挡了,得查真画布;
    ///   · 两个都不显示 → additive 整体回归了。
    /// 配合 神霄/调试/UI运行态/截图+节点Dump:看 screenshot.png + ui_effects.txt(每特效 parentActive/imageActive)。
    /// </summary>
    public static class UIEffectStageTestMenu
    {
        private const string OwnCanvasName = "__UIEffectStageTestRoot";
        private const string RealProbeName = "__UIEffectStageTestReal";
        private const string TestEffect = "ui_zhujiemianzhuanquan";
        private const float TestScale = 12.29f; // 业务里 box_effect2=100 时的 1280/box*0.96
        private const float BoxSize = 120f;

        [MenuItem("神霄/调试/UI运行态/挂测试UI特效(真画布对照)", priority = 112)]
        public static void Spawn()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[UIEffectTest] 需在 Play 模式下挂测试特效。");
                return;
            }

            Clear();

            // A) 对照组:独立置顶 Overlay 画布(已知能显示)。
            var ownGo = new GameObject(OwnCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            UnityEngine.Object.DontDestroyOnLoad(ownGo);
            var canvas = ownGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = ownGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);
            scaler.matchWidthOrHeight = 1f;
            SpawnOne(ownGo.transform, OwnCanvasName + "_box", new Vector2(-150f, 0f));

            // B) 关键:真实 Main 画布(业务特效就在这画布上)。
            Transform real = FindRealMainParent();
            if (real != null)
            {
                SpawnOne(real, RealProbeName, new Vector2(150f, 0f));
                Debug.Log("[UIEffectTest] 已在【真 Main 画布】(" + GetPath(real) + ")挂一份 + 独立画布挂一份对照。等1秒后截图+Dump。");
            }
            else
            {
                Debug.LogWarning("[UIEffectTest] 未找到 UIRoot/Main,只挂了独立画布对照。");
            }
        }

        private static void SpawnOne(Transform parent, string name, Vector2 anchoredPos)
        {
            var boxGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)boxGo.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(BoxSize, BoxSize);
            rt.anchoredPosition = anchoredPos;
            rt.localScale = Vector3.one;

            var bg = boxGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.5f); // 半透黑底,衬出粒子、标出盒边界
            bg.raycastTarget = false;

            _ = UIEffectStage.AddAsync(TestEffect, rt, Vector2.zero, new Vector3(TestScale, TestScale, TestScale));
        }

        // 真实主 UI 层:业务特效挂在 UIRoot/Main 下(ScreenSpaceOverlay 根画布 UIRoot,sortingOrder0)。
        private static Transform FindRealMainParent()
        {
            var main = GameObject.Find("UIRoot/Main");
            if (main != null) return main.transform;
            var uiroot = GameObject.Find("UIRoot");
            return uiroot != null ? uiroot.transform : null;
        }

        private static string GetPath(Transform t)
        {
            string p = t.name;
            while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
            return p;
        }

        [MenuItem("神霄/调试/UI运行态/清除测试UI特效", priority = 113)]
        public static void Clear()
        {
            var own = GameObject.Find(OwnCanvasName);
            if (own != null) UnityEngine.Object.DestroyImmediate(own);
            var real = GameObject.Find(RealProbeName);
            if (real != null) UnityEngine.Object.DestroyImmediate(real);
        }
    }
}
