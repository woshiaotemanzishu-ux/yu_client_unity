using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Halo;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Halo
{
    /// <summary>
    /// 光环主面板：读取 config_hero_halo 与 51400 快照呈现九项特权、奖励和有效期。
    /// 购买/续费保持 blocked，不调用平台支付或 15804；UI 特效和共享说明弹窗仍需真实运行复验。
    /// </summary>
    public sealed class HaloMainView : HaloMainViewBind
    {
        private const int OriginalPriceKey = 5140002;
        private const int RenewThresholdKey = 5140003;
        private const int CurrentPrice = 88; // 老端 config_recharge_product[154].money；Unity 当前未同步该表。
        private const long DefaultRenewThresholdSec = 432000;

        private readonly List<GameObject> _rows = new List<GameObject>();
        private bool _listening;
        private float _nextTimerRefresh;
        private UIEffectStage.Handle _haloEffect;
        private int _effectEpoch;

        protected override void OnInit()
        {
            if (_tpl_HaloItem != null) _tpl_HaloItem.SetActive(false);
            BindClick(img_btn_close, Hide);
            BindClick(img_btn_buy, OnBuyClicked);
            BindClick(box_effect, OnEffectClicked);
        }

        protected override async void OnShow(object args)
        {
            StartListening();
            RestartHaloEffect();
            await HaloConfigs.EnsureLoaded();
            await KeyValueConfigs.EnsureLoaded();
            if (this == null || !isActiveAndEnabled) return;
            ApplyPriceText();
            RebuildRows();
            RefreshState();
        }

        protected override void OnHide()
        {
            StopListening();
            ReleaseHaloEffect();
            ClearRows();
        }

        protected override void OnDispose()
        {
            StopListening();
            ReleaseHaloEffect();
            ClearRows();
            base.OnDispose();
        }

        private void OnDestroy()
        {
            StopListening();
            ReleaseHaloEffect();
            ClearRows();
        }

        private void Update()
        {
            if (!_listening || Time.unscaledTime < _nextTimerRefresh) return;
            _nextTimerRefresh = Time.unscaledTime + 1f;
            RefreshTimeState();
        }

        private void StartListening()
        {
            if (_listening) return;
            EventDispatcher.On<int>(GlobalEvent.EVT_HALO_UPDATE, RefreshStateFromProtocol);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, RefreshState);
            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, RefreshState);
            EventDispatcher.On<int>(GlobalEvent.EVT_TASK_ONE_UPDATED, RefreshStateFromProtocol);
            _listening = true;
        }

        private void StopListening()
        {
            if (!_listening) return;
            EventDispatcher.Off<int>(GlobalEvent.EVT_HALO_UPDATE, RefreshStateFromProtocol);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, RefreshState);
            EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, RefreshState);
            EventDispatcher.Off<int>(GlobalEvent.EVT_TASK_ONE_UPDATED, RefreshStateFromProtocol);
            _listening = false;
        }

        private void RebuildRows()
        {
            ClearRows();
            if (list_middle == null || list_middle.content == null || _tpl_HaloItem == null)
            {
                GameLog.Error("Halo", "列表绑定不完整: scroll={0} content={1} template={2}",
                    list_middle != null, list_middle != null && list_middle.content != null, _tpl_HaloItem != null);
                return;
            }

            IReadOnlyList<HaloConfigs.Entry> entries = HaloConfigs.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                GameObject rowObject = Instantiate(_tpl_HaloItem, list_middle.content);
                rowObject.name = "HaloItem_" + entries[i].Id;
                HaloItem row = rowObject.GetComponent<HaloItem>();
                if (row == null)
                {
                    DestroyRow(rowObject);
                    continue;
                }
                row.Show();
                row.SetData(entries[i]);
                _rows.Add(rowObject);
            }
            list_middle.StopMovement();
            list_middle.content.anchoredPosition = Vector2.zero;
            GameLog.Info("Halo", "只读特权列表刷新: rows={0} has51400={1}", _rows.Count, HaloModel.Instance.HasData);
        }

        private void RefreshState()
        {
            for (int i = 0; i < _rows.Count; i++)
                _rows[i]?.GetComponent<HaloItem>()?.RefreshState();
            RefreshTimeState();
        }

        private void RefreshStateFromProtocol(int _) => RefreshState();

        private void RefreshTimeState()
        {
            long remaining = (long)HaloModel.Instance.EndTime - TimeUtil.NowSec();
            long renewThreshold = ReadPositiveLong(RenewThresholdKey, DefaultRenewThresholdSec);
            bool active = remaining > 0;
            bool showBuy = !active || remaining <= renewThreshold;

            SetNode(img_bottom_banner, showBuy);
            SetNode(box_buy, showBuy);
            if (list_middle != null)
            {
                RectTransform rect = list_middle.transform as RectTransform;
                if (rect != null) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, showBuy ? 583f : 720f);
            }
            if (lable_buy != null) lable_buy.text = active ? "立即续费" : "立即购买";
            SetNode(html_time, active);
            if (active && html_time != null) html_time.text = FormatRemaining(remaining);
        }

        private void ApplyPriceText()
        {
            long originalPrice = ReadPositiveLong(OriginalPriceKey, 198);
            if (lable_original_price != null) lable_original_price.text = "原价:  " + originalPrice;
            if (lable_now_price != null) lable_now_price.text = "现价:  " + CurrentPrice;
        }

        private static long ReadPositiveLong(int key, long fallback)
        {
            string raw = KeyValueConfigs.GetRaw(key);
            return long.TryParse(raw, out long value) && value > 0 ? value : fallback;
        }

        private static string FormatRemaining(long seconds)
        {
            if (seconds < 0) seconds = 0;
            TimeSpan span = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:00}天 {1:00}:{2:00}:{3:00}", (int)span.TotalDays, span.Hours, span.Minutes, span.Seconds);
        }

        private void OnBuyClicked()
        {
            GameLog.Warn("Halo", "blocked: 未发起平台支付/15804，购买或续费光环属于未授权账号写事务。");
        }

        private void OnEffectClicked()
        {
            IllusionTipsFlow.Show(38065414);
        }

        private void RestartHaloEffect()
        {
            ReleaseHaloEffect();
            if (box_effect == null) return;
            int epoch = _effectEpoch;
            _ = AttachHaloEffectAsync(epoch);
        }

        private async Task AttachHaloEffectAsync(int epoch)
        {
            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(
                "UI_305414", box_effect, Vector2.zero, Vector3.one * 8f, 0f, new Vector2(300f, 200f));
            if (this == null || epoch != _effectEpoch || !_listening)
            {
                handle?.Dispose();
                return;
            }
            _haloEffect = handle;
            if (handle == null)
                GameLog.Warn("Halo", "UI_305414 加载失败；真实足迹/动态仍需 Editor+WebGL 双运行态证据。");
        }

        private void ReleaseHaloEffect()
        {
            ++_effectEpoch;
            _haloEffect?.Dispose();
            _haloEffect = null;
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++) DestroyRow(_rows[i]);
            _rows.Clear();
        }

        private static void DestroyRow(GameObject row)
        {
            if (row == null) return;
            row.SetActive(false);
            if (Application.isPlaying) Destroy(row); else DestroyImmediate(row);
        }

        private static void BindClick(Component target, Action callback)
        {
            if (target == null) return;
            Image image = target as Image ?? target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, callback);
        }

        private static void SetNode(Component node, bool visible)
        {
            if (node != null) node.gameObject.SetActive(visible);
        }
    }
}
