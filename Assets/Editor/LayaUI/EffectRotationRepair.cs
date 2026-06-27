using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.UI3D;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// 修复【转换器产出的退化旋转动画】:老端 360°/循环自转被转成「2 帧四元数曲线,起末都≈identity」
    /// (0°≡360° 是同一旋转,legacy Animation 播了不转)。表现:特效该转的部分(如活动图标的旋转圆弧)静止不动。
    /// 退化曲线里仍留着 ε(≈sin(180°) 的浮点残值,8.7e-8)在某个轴上,据此推断【旋转轴】;转速取 360/clip 时长。
    /// 修法:给被动画的节点挂 UIEffectSpin(恒速自转,轴+转速烤进 prefab),并停用那条坏 Animation。
    /// </summary>
    public static class EffectRotationRepair
    {
        // 四元数分量阈值:真旋转会让 x/y/z 在中途冲到接近 1(180°时=1);退化的只有 ε(~1e-7)。
        // 取 0.05(≈半角 2.9° = 整转约 5.7°):小于它=没真转(退化),大于它=有真摆动(保留不动)。
        private const float FLAT_QUAT_THRESHOLD = 0.05f;

        [MenuItem("神霄/特效/修复退化旋转动画(挂代码自转)", priority = 210)]
        public static void RepairAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/GameRes/effect/objs" });
            var sb = new StringBuilder();
            int scanned = 0, repaired = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                scanned++;
                int n = RepairPrefab(path, sb);
                if (n > 0) repaired += n;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RotRepair] 扫描 " + scanned + " 个特效 prefab,修复 " + repaired + " 处退化旋转。\n" + sb);
        }

        [MenuItem("神霄/特效/修复退化旋转动画(仅 ui_zhujiemianzhuanquan)", priority = 211)]
        public static void RepairSuperGiftRing()
        {
            var sb = new StringBuilder();
            int n = RepairPrefab(
                "Assets/GameRes/effect/objs/ui_effect/ui_zhujiemianzhuanquan/ui_zhujiemianzhuanquan.prefab", sb);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RotRepair] ui_zhujiemianzhuanquan 修复 " + n + " 处。\n" + sb);
        }

        private static int RepairPrefab(string prefabPath, StringBuilder log)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            int repaired = 0;
            try
            {
                Animation[] anims = root.GetComponentsInChildren<Animation>(true);
                foreach (Animation anim in anims)
                {
                    AnimationClip clip = anim.clip;
                    if (clip == null || !clip.legacy) continue;
                    if (clip.wrapMode != WrapMode.Loop && clip.wrapMode != WrapMode.PingPong) continue;

                    if (!TryDetectCollapsedSpin(clip, out string bindPath, out Vector3 axis))
                        continue;

                    Transform target = string.IsNullOrEmpty(bindPath) ? anim.transform : anim.transform.Find(bindPath);
                    if (target == null) target = anim.transform;

                    float speed = clip.length > 0.001f ? 360f / clip.length : 240f;
                    var spin = target.GetComponent<UIEffectSpin>();
                    if (spin == null) spin = target.gameObject.AddComponent<UIEffectSpin>();
                    spin.DegreesPerSecond = axis * speed;

                    // 停掉坏动画,免得它每帧把旋转写回 identity 盖掉自转。
                    anim.enabled = false;
                    anim.playAutomatically = false;

                    repaired++;
                    log.AppendLine("FIX  " + prefabPath + "  node=" + target.name +
                                   "  spin=" + (axis * speed) + "  (clip " + clip.name + ", " + clip.length.ToString("0.##") + "s)");
                }

                if (repaired > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return repaired;
        }

        /// <summary>
        /// 退化自转判定:存在 localRotation 曲线,但 x/y/z 分量整段都 ≈0(没真转),且某轴上有非零 ε。
        /// 返回该轴单位向量(带 ε 的符号)与被绑路径。
        /// </summary>
        private static bool TryDetectCollapsedSpin(AnimationClip clip, out string bindPath, out Vector3 axis)
        {
            bindPath = null;
            axis = Vector3.zero;

            float sx = 0f, sy = 0f, sz = 0f;       // 各轴带符号的峰值(取 |值| 最大的那个 key 的原值)
            float ax = 0f, ay = 0f, az = 0f;       // 各轴 |峰值|
            string path = null;
            bool hasRot = false;

            foreach (EditorCurveBinding b in AnimationUtility.GetCurveBindings(clip))
            {
                if (b.type != typeof(Transform)) continue;
                if (!b.propertyName.StartsWith("m_LocalRotation")) continue;
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, b);
                if (curve == null) continue;
                hasRot = true;
                path = b.path;

                float signed = 0f, mag = 0f;
                foreach (Keyframe k in curve.keys)
                {
                    if (Mathf.Abs(k.value) > mag) { mag = Mathf.Abs(k.value); signed = k.value; }
                }
                if (b.propertyName.EndsWith(".x")) { ax = mag; sx = signed; }
                else if (b.propertyName.EndsWith(".y")) { ay = mag; sy = signed; }
                else if (b.propertyName.EndsWith(".z")) { az = mag; sz = signed; }
            }

            if (!hasRot) return false;

            float peak = Mathf.Max(ax, Mathf.Max(ay, az));
            if (peak <= 0f || peak >= FLAT_QUAT_THRESHOLD) return false; // 没 ε(纯静态)或有真旋转 → 不动

            bindPath = path;
            // ε 最大的轴 = 意图旋转轴;符号沿用 ε 的符号(±,只影响转向,转向是观感无关紧要,但尽量忠实)。
            if (ax >= ay && ax >= az) axis = new Vector3(Mathf.Sign(sx), 0f, 0f);
            else if (ay >= ax && ay >= az) axis = new Vector3(0f, Mathf.Sign(sy), 0f);
            else axis = new Vector3(0f, 0f, Mathf.Sign(sz));
            return true;
        }
    }
}
