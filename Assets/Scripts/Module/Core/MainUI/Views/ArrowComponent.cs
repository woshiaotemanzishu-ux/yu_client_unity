using System;
using System.Collections;
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

            if (content != null) content.text = data.Content ?? "";
            if (content3 != null) content3.text = data.ContentPlain ?? "";

            if (data.Target != null) PlaceNearTarget(data.Target, data.Direction, data.Offset);
            ShowEffect(data.Direction);
            StartBob(data.Direction);

            if (data.AutoCountdown && autoAction != null && TaskModel.Instance.GetAutoTaskSetting())
                StartAutoCountdown(data.CloseTime <= 0 ? 10 : data.CloseTime, autoAction);
            else
                HideAutoCountdown();

            GameLog.Info("MainUI", "guide arrow shown: dir={0} auto={1}", data.Direction, data.AutoCountdown);
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
            rt.SetParent(target, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            float targetW = target.rect.width;
            float targetH = target.rect.height;
            float selfW = rt.rect.width;
            float selfH = rt.rect.height;
            Vector2 pos;
            if (direction == DIR_LEFT)
                pos = new Vector2(targetW, -targetH * 0.5f + 10f);
            else if (direction == DIR_RIGHT)
                pos = new Vector2(-selfW - 30f, -targetH * 0.5f + 25f);
            else if (direction == DIR_UP)
                pos = new Vector2((targetW - selfW) * 0.5f, -targetH - 40f);
            else
                pos = new Vector2((targetW - selfW) * 0.5f, selfH + 60f);

            rt.anchoredPosition = pos + offset;
        }
    }

    public sealed class ArrowData
    {
        public string Content;
        public string ContentPlain;
        public int Direction = ArrowComponent.DIR_DOWN;
        public int CloseTime = 10;
        public bool AutoCountdown = true;
        public RectTransform Target;
        public Vector2 Offset;
    }
}
