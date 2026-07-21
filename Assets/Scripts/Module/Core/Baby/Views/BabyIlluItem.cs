using System.Collections.Generic;
using Shenxiao.Framework.Res;
using Shenxiao.Generated.UI.Baby;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Baby
{
    public sealed class BabyIlluItem : BabyIlluItemBind
    {
        private readonly List<Image> _shadows = new List<Image>();
        private readonly List<Image> _stars = new List<Image>();

        public void SetStar(int star, bool active)
        {
            if (star_shadow_group != null) star_shadow_group.gameObject.SetActive(active);
            if (star_group != null) star_group.gameObject.SetActive(active);
            if (!active) return;
            EnsureImages(_shadows, star_shadow_group);
            EnsureImages(_stars, star_group);
            string starSkin = star <= 5 ? "flower_0" : star <= 10 ? "flower1" : "flower2";
            int visible = star % 5;
            if (visible == 0) visible = 5;
            for (int i = 0; i < 5; i++)
            {
                if (i < _shadows.Count) _ = ResManager.SetImageAsync(_shadows[i], GameResPath.GetIcon("baby", "flower"), nativeSize: false);
                if (i < _stars.Count)
                {
                    _stars[i].gameObject.SetActive(i < visible);
                    _ = ResManager.SetImageAsync(_stars[i], GameResPath.GetIcon("baby", starSkin), nativeSize: false);
                }
            }
        }

        private static void EnsureImages(List<Image> images, RectTransform parent)
        {
            if (parent == null) return;
            while (images.Count < 5)
            {
                var go = new GameObject("_star_" + images.Count, typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(parent, false);
                rt.sizeDelta = new Vector2(22f, 23f);
                images.Add(go.GetComponent<Image>());
            }
        }
    }
}
