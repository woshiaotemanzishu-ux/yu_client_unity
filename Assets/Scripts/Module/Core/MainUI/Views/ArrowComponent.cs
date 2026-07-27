using System;
using System.Collections;
using System.Text.RegularExpressions;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// Runtime behavior for the converted ArrowComponent prefab.
    /// </summary>
    public sealed class ArrowComponent : ArrowComponentBind
    {
        private static readonly Regex FontColorStart = new Regex("<font\\s+color=['\"]?(#[0-9a-fA-F]{3,8})['\"]?>", RegexOptions.IgnoreCase);
        private static readonly Regex BreakTag = new Regex("<br\\s*/?>", RegexOptions.IgnoreCase);

        public const int DIR_DOWN = 2;
        public const int DIR_LEFT = 4;
        public const int DIR_UP = 8;
        public const int DIR_RIGHT = 6;

        [SerializeField] private float _bobDistance = 10f;
        [SerializeField] private float _bobDuration = 1f;

        [Header("Target Placement (edit in ArrowComponent.prefab)")]
        [SerializeField] private Vector2 _downTargetOffset = new Vector2(55f, 60f);
        [SerializeField] private Vector2 _leftTargetOffset = new Vector2(55f, 17f);
        [SerializeField] private Vector2 _upTargetOffset = new Vector2(55f, -40f);
        [SerializeField] private Vector2 _rightTargetOffset = new Vector2(-30f, -25f);

        [Header("Prefab Layout (edit anchors in ArrowComponent.prefab)")]
        public RectTransform arrowDownAnchor;
        public RectTransform arrowLeftAnchor;
        public RectTransform arrowUpAnchor;
        public RectTransform arrowRightAnchor;

        private Vector2 _aniBasePos;
        private bool _hasBase;
        private Coroutine _bob;
        private Coroutine _auto;
        private Action _autoAction;

        protected override void OnInit()
        {
            if (aniGp != null)
            {
                _aniBasePos = aniGp.anchoredPosition;
                _hasBase = true;
            }
            HideAutoCountdown();
        }

        protected override void OnHide()
        {
            StopAutoCountdown();
            StopBob();
        }

        protected override void OnDispose()
        {
            StopAutoCountdown();
            StopBob();
        }

        public void SetData(ArrowData data, Action autoAction = null)
        {
            if (data == null) return;

            if (content != null)
            {
                content.gameObject.SetActive(true);
                content.richText = true;
                content.text = ToTmpRichText(data.Content);
            }
            if (content3 != null)
            {
                bool showPlain = !string.IsNullOrEmpty(data.ContentPlain);
                content3.gameObject.SetActive(showPlain);
                content3.text = showPlain ? data.ContentPlain : "";
            }
            if (contentImg != null) contentImg.gameObject.SetActive(false);

            // 字号、颜色、内边距、气泡尺寸和倒计时位置都由 prefab 上的 TMP/Layout 组件负责。
            // 这里只刷新布局结果，不能把用户在 Inspector 中的视觉调整重新写掉。
            if (rect_conta != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rect_conta);

            if (data.Target != null) PlaceNearTarget(data.Target, data.Direction, data.Offset);
            ShowEffect(data.Direction);
            StartBob(data.Direction);

            if (data.AutoCountdown && data.CloseTime > 0 && autoAction != null && TaskModel.Instance.GetAutoTaskSetting())
                StartAutoCountdown(data.CloseTime, autoAction);
            else
                HideAutoCountdown();

            GameLog.Info("MainUI", "guide arrow shown: dir={0} auto={1}", data.Direction, data.AutoCountdown);
        }

        private void ShowEffect(int direction)
        {
            if (arrow_effect == null) return;
            RectTransform anchor = GetArrowAnchor(direction);
            if (anchor != null)
            {
                arrow_effect.SetParent(anchor, false);
                arrow_effect.anchorMin = arrow_effect.anchorMax = arrow_effect.pivot = new Vector2(0.5f, 0.5f);
                arrow_effect.anchoredPosition = Vector2.zero;
            }

            float rot = 0f;
            if (direction == DIR_DOWN) rot = -90f;
            else if (direction == DIR_RIGHT) rot = 180f;
            else if (direction == DIR_UP) rot = 90f;
            arrow_effect.localRotation = Quaternion.Euler(0f, 0f, rot);
        }

        private RectTransform GetArrowAnchor(int direction)
        {
            if (direction == DIR_LEFT) return arrowLeftAnchor;
            if (direction == DIR_RIGHT) return arrowRightAnchor;
            if (direction == DIR_UP) return arrowUpAnchor;
            return arrowDownAnchor;
        }

        private void StartBob(int direction)
        {
            StopBob();
            if (aniGp == null) return;
            if (!_hasBase)
            {
                _aniBasePos = aniGp.anchoredPosition;
                _hasBase = true;
            }

            bool horizontal = direction == DIR_LEFT || direction == DIR_RIGHT;
            float sign = (direction == DIR_LEFT || direction == DIR_UP) ? -1f : 1f;
            _bob = StartCoroutine(BobRoutine(horizontal, sign * _bobDistance));
        }

        private IEnumerator BobRoutine(bool horizontal, float amount)
        {
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, _bobDuration);
                float ping = Mathf.PingPong(t, 1f);
                Vector2 p = _aniBasePos;
                if (horizontal) p.x += amount * ping;
                else p.y += amount * ping;
                aniGp.anchoredPosition = p;
                yield return null;
            }
        }

        private void StopBob()
        {
            if (_bob != null)
            {
                StopCoroutine(_bob);
                _bob = null;
            }
            if (aniGp != null && _hasBase) aniGp.anchoredPosition = _aniBasePos;
        }

        private void StartAutoCountdown(int seconds, Action action)
        {
            StopAutoCountdown();
            _autoAction = action;
            if (autoImg != null) autoImg.gameObject.SetActive(true);
            if (autoLb2 != null) autoLb2.text = "秒后自动继续";
            _auto = StartCoroutine(AutoCountdownRoutine(seconds));
        }

        private IEnumerator AutoCountdownRoutine(int seconds)
        {
            int left = Mathf.Max(1, seconds);
            while (left > 0)
            {
                // 数字和后缀拆成两个 TMP 节点，颜色、字号和间距全部在 prefab 中调整。
                if (autoLb != null) autoLb.text = left.ToString();
                yield return new WaitForSeconds(1f);
                left--;
            }

            Action action = _autoAction;
            StopAutoCountdown();
            action?.Invoke();
        }

        private void StopAutoCountdown()
        {
            if (_auto != null)
            {
                StopCoroutine(_auto);
                _auto = null;
            }
            _autoAction = null;
            HideAutoCountdown();
        }

        private void HideAutoCountdown()
        {
            if (autoImg != null) autoImg.gameObject.SetActive(false);
            if (autoLb != null) autoLb.text = "";
            if (autoLb2 != null) autoLb2.text = "";
        }

        private void PlaceNearTarget(RectTransform target, int direction, Vector2 offset)
        {
            RectTransform rt = (RectTransform)transform;
            RectTransform parent = target.parent as RectTransform;
            if (parent == null) parent = target;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            float targetW = target.rect.width;
            float targetH = target.rect.height;
            float selfW = rt.rect.width;
            float selfH = rt.rect.height;
            Vector3 targetTopLeft = GetTopLeftInParent(target, parent);
            Vector3 contentTopLeft = GetTopLeftInParent(content_bg != null ? content_bg.rectTransform : rt, rt);
            Vector3 desiredContentTopLeft;
            if (direction == DIR_LEFT)
                desiredContentTopLeft = targetTopLeft + new Vector3(targetW + _leftTargetOffset.x, _leftTargetOffset.y, 0f);
            else if (direction == DIR_RIGHT)
                desiredContentTopLeft = targetTopLeft + new Vector3(-selfW + _rightTargetOffset.x, targetH * 0.5f + _rightTargetOffset.y, 0f);
            else if (direction == DIR_UP)
                desiredContentTopLeft = targetTopLeft + new Vector3((targetW - selfW) * 0.5f + _upTargetOffset.x, -targetH + _upTargetOffset.y, 0f);
            else
                desiredContentTopLeft = targetTopLeft + new Vector3((targetW - selfW) * 0.5f + _downTargetOffset.x, selfH + _downTargetOffset.y, 0f);

            Vector3 rootPos = desiredContentTopLeft - contentTopLeft + new Vector3(offset.x, offset.y, 0f);
            rt.localPosition = new Vector3(rootPos.x, rootPos.y, 0f);
        }

        private static string ToTmpRichText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string result = BreakTag.Replace(text, "\n");
            result = FontColorStart.Replace(result, "<color=$1>");
            return result.Replace("</font>", "</color>");
        }

        private static Vector3 GetTopLeftInParent(RectTransform source, RectTransform parent)
        {
            if (source == null || parent == null) return Vector3.zero;
            Vector3[] corners = new Vector3[4];
            source.GetWorldCorners(corners);
            return parent.InverseTransformPoint(corners[1]);
        }
    }

    public sealed class ArrowData
    {
        public string Content;
        public string ContentPlain;
        public int Direction = ArrowComponent.DIR_DOWN;
        public int CloseTime;
        public bool AutoCountdown;
        public bool NotEffect;
        public Vector3 SelectEffectScale = Vector3.one;
        public Vector2 FingerEffectOffset;
        public RectTransform Target;
        public Vector2 Offset;
    }
}
