using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// Laya 文本描边 -> TMP outline 材质预设。同一 (字体, 颜色, 粗细) 复用一份材质,
    /// 存 Assets/GameRes/Fonts/Materials/。描边宽度从像素到 SDF 是近似换算,报告里标记。
    /// </summary>
    public static class LayaTextStyles
    {
        private const string MAT_DIR = "Assets/GameRes/Fonts/Materials";

        public static void ApplyOutline(TextMeshProUGUI tmp, Color color, float strokePx, LayaUIReport report, string nodeName)
        {
            TMP_FontAsset font = tmp.font;
            if (font == null) return;
            // 近似:Laya stroke 像素 -> TMP outline 0..1。按字号 24 左右的常见描边 2px≈0.2 标定。
            float width = Mathf.Clamp(strokePx * 0.1f, 0.05f, 0.5f);
            string hex = ColorUtility.ToHtmlStringRGB(color);
            string matPath = MAT_DIR + "/" + font.name + "_Outline_" + hex + "_" + strokePx.ToString("0.#") + ".mat";

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Directory.CreateDirectory(MAT_DIR);
                mat = new Material(font.material);
                mat.EnableKeyword("OUTLINE_ON");
                mat.SetColor(ShaderUtilities.ID_OutlineColor, color);
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            tmp.fontSharedMaterial = mat;
            report.Approx(nodeName + " 描边 " + strokePx + "px#" + hex + " 用 SDF outline 近似(" + width.ToString("0.##") + "),样式需核对");
        }
    }
}
