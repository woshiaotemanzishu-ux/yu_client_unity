using System;
using System.Collections;
using System.Text.RegularExpressions;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Tasks;
using UnityEngine;

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

        private Vector2 _aniBasePos;
        private bool _hasBase;
        private Coroutine _bob;
        private Coroutine _auto;
        private Action _autoAction;
        private float _initRectWidth;
        private float _initRectHeight;

        protected override void OnInit()
        {
            if (aniGp != null)
            {
                _aniBasePos = aniGp.anchoredPosition;
                _hasBase = true;
            }
            if (rect_conta != null)
            {
                _initRectWidth = rect_conta.rect.width;
                _initRectHeight = rect_conta.rect.height;
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

            ResizeToOldClientText(data.Direction);

            if (data.Target != null) PlaceNearTarget(data.Target, data.Direction, data.Offset);
            ShowEffect(data.Direction);
            StartBob(data.Direction);

            if (data.AutoCountdown && data.CloseTime > 0 && autoAction != null && TaskModel.Instance.GetAutoTaskSetting())
                StartAutoCountdown(data.CloseTime, autoAction);
            else
                HideAutoCountdown();

            GameLog.Info("MainUI", "guide arrow shown: dir={0} auto={1}", data.Direction, data.AutoCountdown);
        }

        private void ResizeToOldClientText(int direction)
        {
            if (content == null || rect_conta == null || content_bg == null) return;

            content.ForceMeshUpdate();
            float textW = Mathf.Max(1f, content.preferredWidth);
            float textH = Mathf.Max(1f, content.preferredHeight);
            RectTransform contentRt = content.rectTransform;
            contentRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textW);
            contentRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textH);

            float bgW = textW + 53f; // old client: text_w + 155 - 102 when contentImg is hidden.
            float bgH = Mathf.Max(textH + 10f, 97f);
            rect_conta.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bgW);
            rect_conta.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bgH);
            content_bg.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bgW);
            content_bg.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bgH);

            float gapWidth = _initRectWidth - bgW;
            float gapHeight = _initRectHeight - bgH;
            if (arrow_effect != null)
            {
                if (direction == DIR_LEFT)
                    arrow_effect.anchoredPosition = new Vector2(35f, -(120f - gapHeight * 0.5f));
                else if (direction == DIR_RIGHT)
                    arrow_effect.anchoredPosition = new Vector2(405f - gapWidth, -(110f - gapHeight * 0.5f));
                else if (direction == DIR_UP)
                    arrow_effect.anchoredPosition = new Vector2(rect_conta.anchoredPosition.x + bgW * 0.5f, -35f);
                else
                    arrow_effect.anchoredPosition = new Vector2(rect_conta.anchoredPosition.x + bgW * 0.5f, -(205f - gapHeight));
            }

            if (autoImg != null)
            {
                Vector2 pos = autoImg.rectTransform.anchoredPosition;
                pos.y = -(bgH - 9f);
                autoImg.rectTransform.anchoredPosition = pos;
            }
        }

        private void ShowEffect(int direction)
        {
            if (arrow_effect == null) return;
            float rot = 0f;
            if (direction == DIR_DOWN) rot = -90f;
            else if (direction == DIR_RIGHT) rot = 180f;
            else if (direction == DIR_UP) rot = 90f;
            arrow_effect.localRotation = Quaternion.Euler(0f, 0f, rot);
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
            _auto = StartCoroutine(AutoCountdownRoutine(seconds));
        }

        private IEnumerator AutoCountdownRoutine(int seconds)
        {
            int left = Mathf.Max(1, seconds);
            while (left > 0)
            {
                if (autoLb != null) autoLb.text = "<color=#FFFF00>" + left + "</color>秒后自动继续";
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
                desiredContentTopLeft = targetTopLeft + new Vector3(targetW + 55f, 17f, 0f);
            else if (direction == DIR_RIGHT)
                desiredContentTopLeft = targetTopLeft + new Vector3(-selfW - 30f, targetH * 0.5f - 25f, 0f);
            else if (direction == DIR_UP)
                desiredContentTopLeft = targetTopLeft + new Vector3((targetW - selfW) * 0.5f + 55f, -targetH - 40f, 0f);
            else
                desiredContentTopLeft = targetTopLeft + new Vector3((targetW - selfW) * 0.5f + 55f, selfH + 60f, 0f);

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
        public RectTransform Target;
        public Vector2 Offset;
    }
}
