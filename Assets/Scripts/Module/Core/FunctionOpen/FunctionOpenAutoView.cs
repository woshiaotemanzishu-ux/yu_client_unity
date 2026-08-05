using System;
using System.Collections;
using Shenxiao.Common.Audio;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.FunctionOpen;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.FunctionOpen
{
    /// <summary>
    /// FunctionOpenAutoView 的组合演出播放器。静态布局、颜色、时长和动效参数均挂在 Prefab 上；
    /// Flow 只传技能数据并管理队列，避免把可视样式散落到 SkillManager。
    /// </summary>
    public sealed class FunctionOpenAutoView : MonoBehaviour
    {
        [Header("节点")]
        [SerializeField] private FunctionOpenAutoViewBind view;

        [Header("演出")]
        [SerializeField, Min(0f)] private float openDuration = 0.22f;
        [SerializeField, Range(0.1f, 1f)] private float openStartScale = 0.82f;
        [SerializeField, Min(0f)] private float clickEnableDelay = 0.5f;
        [SerializeField, Min(1)] private int autoCloseSeconds = 10;
        [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.72f);

        private CanvasGroup _canvasGroup;
        private Image _backdrop;
        private Coroutine _presentation;
        private Action _onClosed;
        private bool _canClose;
        private bool _showing;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void Bind(FunctionOpenAutoViewBind bind)
        {
            if (view == null) view = bind;
            EnsureInitialized();
        }

        public void ShowSkill(int skillId, string skillName, string iconName, string description, Action onClosed)
        {
            EnsureInitialized();
            if (view == null)
            {
                onClosed?.Invoke();
                return;
            }

            if (_presentation != null) StopCoroutine(_presentation);
            _onClosed = onClosed;
            _showing = true;
            _canClose = false;

            view.Show();
            _ = AudioManager.PlayUi("double_box");
            transform.SetAsLastSibling();
            ConfigureSkillMode(skillName, iconName, description);
            _presentation = StartCoroutine(PlayPresentation());
        }

        public void CancelWithoutCallback()
        {
            if (_presentation != null)
            {
                StopCoroutine(_presentation);
                _presentation = null;
            }
            _showing = false;
            _canClose = false;
            _onClosed = null;
            if (view != null) view.Hide();
        }

        private void EnsureInitialized()
        {
            if (view == null) view = GetComponent<FunctionOpenAutoViewBind>();
            if (view == null) return;

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            if (view.click_bg != null)
            {
                _backdrop = view.click_bg.GetComponent<Image>() ?? view.click_bg.gameObject.AddComponent<Image>();
                _backdrop.color = backdropColor;
                UIUtil.ClearClicks(_backdrop);
                UIUtil.AddClick(_backdrop, RequestClose);
                _backdrop.raycastTarget = false;
            }

            if (view.okBtn != null)
                UIUtil.AddClick(view.okBtn, RequestClose);
        }

        private void ConfigureSkillMode(string skillName, string iconName, string description)
        {
            if (_backdrop != null)
            {
                _backdrop.color = backdropColor;
                _backdrop.raycastTarget = false;
            }

            SetActive(view.star_bg, true);
            SetActive(view.title, true);
            SetActive(view.icon_bg, true);
            SetActive(view.effect, true);
            SetActive(view.con, true);
            SetActive(view.content_bg, false);
            SetActive(view.skill_icon_bg, true);
            SetActive(view.icon, true);
            SetActive(view.skill_icon, false);
            SetActive(view._scroll, false);
            SetActive(view.okBtn, false);
            SetActive(view.skillLab, true);
            SetActive(view.close_tip, true);

            if (view.skillLab != null) view.skillLab.text = skillName ?? string.Empty;
            if (view.tips != null) view.tips.text = description ?? string.Empty;

            _ = ResManager.SetImageAsync(
                view.title, GameResPath.GetIcon("functionOpen", "uignkq_zia"), nativeSize: false);
            _ = ResManager.SetLayaTextureAsync(
                view.star_bg, GameResPath.GetIcon("functionOpen", "uignkq_ditu"), nativeSize: false);
            _ = ResManager.SetImageAsync(
                view.skill_icon_bg, GameResPath.GetIcon("functionOpen", "ui_role_35"), nativeSize: false);
            _ = ResManager.SetImageAsync(
                view.icon, GameResPath.GetSkillIcon(iconName), nativeSize: false);
        }

        private IEnumerator PlayPresentation()
        {
            float startedAt = Time.unscaledTime;
            Vector3 originalScale = view.con != null ? view.con.localScale : Vector3.one;
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            if (view.con != null) view.con.localScale = originalScale * openStartScale;

            while (_showing && openDuration > 0f)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - startedAt) / openDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                if (_canvasGroup != null) _canvasGroup.alpha = eased;
                if (view.con != null)
                    view.con.localScale = Vector3.LerpUnclamped(originalScale * openStartScale, originalScale, eased);
                if (t >= 1f) break;
                yield return null;
            }

            if (!_showing) yield break;
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            if (view.con != null) view.con.localScale = originalScale;

            float closeAt = startedAt + Mathf.Max(1, autoCloseSeconds - 1);
            int lastShown = -1;
            while (_showing)
            {
                float elapsed = Time.unscaledTime - startedAt;
                if (!_canClose && elapsed >= clickEnableDelay)
                {
                    _canClose = true;
                    if (_backdrop != null) _backdrop.raycastTarget = true;
                }

                int remaining = Mathf.Max(0, Mathf.CeilToInt(closeAt - Time.unscaledTime));
                if (remaining != lastShown && view.close_tip != null)
                {
                    view.close_tip.text = remaining > 0
                        ? "点击任意位置关闭（" + remaining + "s）"
                        : string.Empty;
                    lastShown = remaining;
                }
                if (remaining <= 0) break;
                yield return null;
            }

            if (_showing) CloseNow();
        }

        private void RequestClose()
        {
            if (_showing && _canClose) CloseNow();
        }

        private void CloseNow()
        {
            if (!_showing) return;
            _showing = false;
            _canClose = false;
            if (_backdrop != null) _backdrop.raycastTarget = false;
            if (_presentation != null)
            {
                StopCoroutine(_presentation);
                _presentation = null;
            }

            Action callback = _onClosed;
            _onClosed = null;
            if (view != null) view.Hide();
            callback?.Invoke();
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }
    }
}
