using System;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Title;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Medal
{
    /// <summary>天境横向称号项；视觉参数全部来自 TitleMainView.prefab 内嵌模板。</summary>
    public sealed class TitleItem : TitleItemBind
    {
        private MedalModel.TitleEntry _entry;
        private Action<uint> _onSelected;
        private UIEffectStage.Handle _effect;
        private int _effectEpoch;

        protected override void OnInit()
        {
            BindClick(_Image1, OnClicked);
            BindClick(select_img, OnClicked);
        }

        public void SetData(MedalModel.TitleEntry entry, bool selected, bool red, Action<uint> onSelected)
        {
            _entry = entry;
            _onSelected = onSelected;
            if (entry == null) return;

            TitleConfigs.Row cfg = TitleConfigs.Get(entry.Id, entry.Level)
                ?? TitleConfigs.GetFirst(entry.Id);
            if (name_lb != null) name_lb.text = cfg?.Name ?? entry.Id.ToString();
            SetNode(select_img, selected);
            SetNode(red_img, red);

            bool active = entry.IsEquip != 0;
            SetNode(star_gp, active);
            SetNode(stage_img, !active);
            RenderStars(active ? entry.Level : (ushort)0);
            RestartEffect(cfg?.ShowId ?? 0);
        }

        public void SetSelected(bool selected)
        {
            SetNode(select_img, selected);
        }

        public void SetRed(bool red)
        {
            SetNode(red_img, red);
        }

        protected override void OnHide()
        {
            ReleaseEffect();
        }

        protected override void OnDispose()
        {
            ReleaseEffect();
            base.OnDispose();
        }

        private void OnDestroy() => ReleaseEffect();

        private void OnClicked()
        {
            if (_entry != null) _onSelected?.Invoke(_entry.Id);
        }

        private void RenderStars(ushort count)
        {
            Image[] stars = { star_img_1, star_img_2, star_img_3, star_img_4, star_img_5 };
            for (int i = 0; i < stars.Length; i++)
            {
                Image star = stars[i];
                if (star == null) continue;
                star.gameObject.SetActive(true);
                _ = ResManager.SetImageAsync(star,
                    GameResPath.GetIcon("title", i < count ? "com_star_1a" : "com_star_2"), false);
            }
        }

        private void RestartEffect(int showId)
        {
            ReleaseEffect();
            string effectName = TitleConfigs.EffectName(showId);
            if (title_gp == null || string.IsNullOrEmpty(effectName)) return;
            int epoch = _effectEpoch;
            _ = AttachEffectAsync(effectName, epoch);
        }

        private async Task AttachEffectAsync(string effectName, int epoch)
        {
            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(
                effectName, title_gp, Vector2.zero, Vector3.one * 5f);
            if (this == null || epoch != _effectEpoch || !isActiveAndEnabled)
            {
                handle?.Dispose();
                return;
            }
            _effect = handle;
            if (handle == null) GameLog.Warn("Title", "称号项特效加载失败: {0}", effectName);
        }

        private void ReleaseEffect()
        {
            ++_effectEpoch;
            _effect?.Dispose();
            _effect = null;
        }

        private static void BindClick(Component target, Action callback)
        {
            if (target == null) return;
            Image image = target as Image ?? target.GetComponent<Image>()
                ?? target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.ClearClicks(image);
            UIUtil.AddClick(image, callback);
        }

        private static void SetNode(Component node, bool active)
        {
            if (node != null) node.gameObject.SetActive(active);
        }
    }
}
