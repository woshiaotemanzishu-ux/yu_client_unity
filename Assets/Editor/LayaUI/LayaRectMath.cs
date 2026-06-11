using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Shenxiao.Editor.LayaUI
{
    /// <summary>
    /// Laya 坐标系 -> UGUI RectTransform 的全部换算,集中在这一个文件,别处不要散落坐标公式。
    ///
    /// Laya 约定:父节点左上角为原点,y 向下;node.x/y 是节点「锚点」(anchorX/anchorY,
    /// 占自身宽高的比例,默认 0,0=左上;或 pivotX/pivotY 像素值)所在的位置。
    /// centerX/centerY 是节点中心相对父中心的偏移(优先于 x/y);
    /// left/right/top/bottom 是到父对应边的距离,左右(上下)同时给即拉伸。
    ///
    /// 统一映射:Unity pivot=(px, 1-py)(py 为 Laya 自顶向下的锚比例),
    /// 锚在父左上时 anchoredPosition=(x, -y) 恰好把同一个物理点对齐,绝不混用其它原点。
    /// </summary>
    public static class LayaRectMath
    {
        public static float? F(JObject p, string key)
        {
            JToken t = p[key];
            if (t == null || t.Type == JTokenType.Null) return null;
            if (t.Type == JTokenType.String)
            {
                float v;
                return float.TryParse((string)t, out v) ? (float?)v : null;
            }
            try { return (float)t; }
            catch { return null; }
        }

        /// <summary>node 自身锚点(Unity pivot 语义,y 自底向上)。</summary>
        public static Vector2 ResolvePivot(JObject p, Vector2 size)
        {
            float px = F(p, "anchorX") ?? (size.x > 0 ? (F(p, "pivotX") ?? 0f) / size.x : 0f);
            float pyTop = F(p, "anchorY") ?? (size.y > 0 ? (F(p, "pivotY") ?? 0f) / size.y : 0f);
            return new Vector2(Mathf.Clamp01(px), Mathf.Clamp01(1f - pyTop));
        }

        /// <summary>把一个 Laya 节点的布局属性套到 RectTransform 上。size 是已解析好的节点尺寸。</summary>
        public static void Apply(RectTransform rt, JObject p, Vector2 size)
        {
            Vector2 pivot = ResolvePivot(p, size);
            rt.pivot = pivot;

            float? left = F(p, "left"), right = F(p, "right");
            float? top = F(p, "top"), bottom = F(p, "bottom");
            float? centerX = F(p, "centerX"), centerY = F(p, "centerY");
            float x = F(p, "x") ?? 0f, y = F(p, "y") ?? 0f;

            Vector2 aMin = rt.anchorMin, aMax = rt.anchorMax;
            Vector2 pos = rt.anchoredPosition;
            float w = size.x, h = size.y;
            // Laya 的 right/centerX/bottom 等相对布局用的是显示尺寸(width×scaleX),
            // 定位公式必须用 dw/dh,否则带缩放的节点(如 0.35 倍的 16+ 图标)整体偏移
            float sclX = Mathf.Abs(F(p, "scaleX") ?? 1f);
            float sclY = Mathf.Abs(F(p, "scaleY") ?? 1f);
            float dw = w * sclX, dh = h * sclY;

            // 水平轴
            if (left.HasValue && right.HasValue)
            {
                aMin.x = 0f; aMax.x = 1f;
            }
            else if (centerX.HasValue)
            {
                aMin.x = aMax.x = 0.5f;
                pos.x = centerX.Value + (pivot.x - 0.5f) * dw;
            }
            else if (right.HasValue)
            {
                aMin.x = aMax.x = 1f;
                pos.x = -(right.Value + (1f - pivot.x) * dw);
            }
            else if (left.HasValue)
            {
                aMin.x = aMax.x = 0f;
                pos.x = left.Value + pivot.x * dw;
            }
            else
            {
                aMin.x = aMax.x = 0f;
                pos.x = x; // Laya x 即锚点位置,pivot 已对齐同一物理点
            }

            // 垂直轴(Laya y 向下,Unity y 向上)
            if (top.HasValue && bottom.HasValue)
            {
                aMin.y = 0f; aMax.y = 1f;
            }
            else if (centerY.HasValue)
            {
                aMin.y = aMax.y = 0.5f;
                pos.y = -(centerY.Value + (0.5f - pivot.y) * dh);
            }
            else if (bottom.HasValue)
            {
                aMin.y = aMax.y = 0f;
                pos.y = bottom.Value + pivot.y * dh;
            }
            else if (top.HasValue)
            {
                aMin.y = aMax.y = 1f;
                pos.y = -(top.Value + (1f - pivot.y) * dh);
            }
            else
            {
                aMin.y = aMax.y = 1f;
                pos.y = -y;
            }

            rt.anchorMin = aMin;
            rt.anchorMax = aMax;

            // 尺寸:拉伸轴用 offset,其余用 sizeDelta
            if (left.HasValue && right.HasValue)
            {
                rt.offsetMin = new Vector2(left.Value, rt.offsetMin.y);
                rt.offsetMax = new Vector2(-right.Value, rt.offsetMax.y);
            }
            else
            {
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                rt.anchoredPosition = new Vector2(pos.x, rt.anchoredPosition.y);
            }
            if (top.HasValue && bottom.HasValue)
            {
                rt.offsetMin = new Vector2(rt.offsetMin.x, bottom.Value);
                rt.offsetMax = new Vector2(rt.offsetMax.x, -top.Value);
            }
            else
            {
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, pos.y);
            }

            // 缩放与旋转(Laya 顺时针为正,Unity z 轴逆时针为正)
            float sx = F(p, "scaleX") ?? 1f, sy = F(p, "scaleY") ?? 1f;
            if (sx != 1f || sy != 1f) rt.localScale = new Vector3(sx, sy, 1f);
            float rot = F(p, "rotation") ?? 0f;
            if (rot != 0f) rt.localEulerAngles = new Vector3(0f, 0f, -rot);
        }

        /// <summary>Laya sizeGrid "上,右,下,左" -> Unity Sprite border (左,下,右,上)。</summary>
        public static Vector4 SizeGridToBorder(string sizeGrid)
        {
            string[] parts = sizeGrid.Split(',');
            if (parts.Length < 4) return Vector4.zero;
            float t, r, b, l;
            float.TryParse(parts[0].Trim(), out t);
            float.TryParse(parts[1].Trim(), out r);
            float.TryParse(parts[2].Trim(), out b);
            float.TryParse(parts[3].Trim(), out l);
            return new Vector4(l, b, r, t);
        }

        public static Color ParseColor(string hex, Color fallback)
        {
            Color c;
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out c)) return c;
            return fallback;
        }
    }
}
