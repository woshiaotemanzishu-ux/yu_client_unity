using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Module.Core.Skill;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Dress
{
    /// <summary>装扮技能图标的只读详情叶子，复用 CommonModule.prefab/SkillTipsView。</summary>
    public static class DressSkillTipFlow
    {
        private static GameObject _moduleRoot;
        private static SkillTipsViewBind _view;
        private static bool _loading;
        private static int _pendingSkillId;

        public static void Show(int skillId)
        {
            if (skillId <= 0) return;
            _pendingSkillId = skillId;
            _ = ShowAsync();
        }

        public static void Close()
        {
            if (_view != null && _view.IsShown) _view.Hide();
            if (_moduleRoot != null) _moduleRoot.SetActive(false);
        }

        public static void Reset()
        {
            if (_moduleRoot != null) ResManager.ReleaseInstance(_moduleRoot);
            _moduleRoot = null;
            _view = null;
            _loading = false;
            _pendingSkillId = 0;
        }

        private static async Task ShowAsync()
        {
            await SkillConfigs.EnsureLoaded();
            if (!await EnsureViewAsync()) return;
            int skillId = _pendingSkillId;
            string name = SkillConfigs.GetName(skillId);
            if (string.IsNullOrWhiteSpace(name))
            {
                GameLog.Warn("Dress", "装扮技能配置不存在 skill={0}", skillId);
                return;
            }

            if (_view.name_text != null) _view.name_text.text = name;
            if (_view.lv_text != null) _view.lv_text.text = "Lv.1";
            if (_view.des_text != null)
            {
                _view.des_text.richText = true;
                _view.des_text.text = SkillConfigs.GetDescRichForLevel(skillId, 1);
                // 该公共弹层的说明可视区只有 65px；装扮三条说明均应在首屏完整可读，
                // 超长兜底仍由原 ScrollRect 处理。
                _view.des_text.fontSize = 16f;
                _view.des_text.textWrappingMode = TMPro.TextWrappingModes.Normal;
                _view.des_text.overflowMode = TMPro.TextOverflowModes.Overflow;
                _view.des_text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 270f);
                _view.des_text.ForceMeshUpdate();
                float height = Mathf.Max(58f, _view.des_text.preferredHeight + 8f);
                _view.des_text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                if (_view.Content != null)
                {
                    _view.Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 270f);
                    _view.Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                }
            }
            if (_view.labelDisplay != null) _view.labelDisplay.text = "确定";
            if (_view.icon != null)
            {
                _view.icon.gameObject.SetActive(false);
                string icon = SkillConfigs.GetIconForLevel(skillId, 1);
                bool loaded = await ResManager.SetImageAsync(
                    _view.icon, GameResPath.GetSkillIconPath(icon), nativeSize: false);
                if (_view == null || skillId != _pendingSkillId) return;
                _view.icon.gameObject.SetActive(loaded);
            }

            _moduleRoot.SetActive(true);
            _view.Show(skillId);
            _view.transform.SetAsLastSibling();
        }

        private static async Task<bool> EnsureViewAsync()
        {
            if (_moduleRoot != null && _view != null) return true;
            if (_loading) return false;
            _loading = true;
            try
            {
                Transform parent = ViewManager.GetLayer(UILayer.Popup);
                if (parent == null)
                {
                    GameLog.Error("Dress", "装扮技能详情无法打开：Popup 层未就绪");
                    return false;
                }

                _moduleRoot = await ResManager.InstantiateAsync(
                    GameResPath.GetUIPrefab("common", "CommonModule"), parent);
                if (_moduleRoot == null) return false;
                _moduleRoot.name = "CommonModule(DressSkillTip)";
                foreach (BaseView view in _moduleRoot.GetComponentsInChildren<BaseView>(true))
                    view.gameObject.SetActive(false);

                _view = _moduleRoot.GetComponentInChildren<SkillTipsViewBind>(true);
                if (_view == null)
                {
                    GameLog.Error("Dress", "CommonModule 缺 SkillTipsViewBind");
                    Reset();
                    return false;
                }

                foreach (Graphic graphic in _view.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = false;
                if (_view._Image1 != null) UIUtil.AddClick(_view._Image1, Close);
                else if (_view.enter_btn != null) UIUtil.AddClick(_view.enter_btn, Close);
                _moduleRoot.SetActive(false);
                return true;
            }
            finally
            {
                _loading = false;
            }
        }
    }
}
