using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Framework.UI
{
    /// <summary>UGUI 真灰阶状态；材质放在 Resources，确保玩家包不会被 Shader stripping 移除。</summary>
    public static class UIGrayStyle
    {
        private const string MaterialPath = "UI/UIGrayscale";
        private static Material s_material;

        public static Material Material
        {
            get
            {
                if (s_material == null) s_material = Resources.Load<Material>(MaterialPath);
                return s_material;
            }
        }

        public static void Apply(Graphic graphic, bool gray)
        {
            if (graphic == null) return;
            graphic.material = gray ? Material : null;
            Color color = graphic.color;
            color.r = 1f;
            color.g = 1f;
            color.b = 1f;
            color.a = 1f;
            graphic.color = color;
        }
    }
}
