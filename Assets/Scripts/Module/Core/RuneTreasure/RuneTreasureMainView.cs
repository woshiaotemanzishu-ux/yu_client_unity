using System;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.RuneTreasure;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Rune;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.RuneTreasure
{
    /// <summary>灵魄寻宝真实 Prefab 消费者。416读侧即时回显；写事务在完整协议闭环前明确保持 blocked。</summary>
    public sealed class RuneTreasureMainView : RuneTreasureMainViewBind
    {
        // 当前老H5 config_treasure_hunt[49].value，配置闭包迁入前保持显式权威常量。
        private const uint LegacyLuckMax = 135;
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_RuneTreasureStageRewardItem != null) _tpl_RuneTreasureStageRewardItem.SetActive(false);
            if (_tpl_BaseAwardItem != null) _tpl_BaseAwardItem.SetActive(false);
            if (_tpl_RuneResultItem != null) _tpl_RuneResultItem.SetActive(false);
            BindClick(_gp_left, () => ItemTipsView.Show(26260005));
            BindClick(_gp_right, () => ItemTipsView.Show(26270005));
            BindClick(_btn_exchange, () => RuneFlow.OpenSub("RuneConvertView"));
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            RuneTreasureController.Instance.Init();
            RuneTreasureController.Instance.RequestRuneInfo();
            RuneTreasureController.Instance.RequestLucky(RuneTreasureController.RuneType);
            Render();
        }

        protected override void OnHide() => Unsubscribe();

        public void Render()
        {
            RuneTreasureModel.RuneSnapshot rune = RuneTreasureModel.Instance.Rune;
            bool free = rune != null && rune.FreeTime <= (ulong)TimeUtil.NowSec();
            if (tiemsLab != null) tiemsLab.text = string.Empty;
            if (_lb_alltimes != null) _lb_alltimes.text = rune == null
                ? "本周累计次数：--"
                : "本周累计次数：" + rune.DrawTimes;
            if (_lb_freetime != null) _lb_freetime.text = free ? "当前免费" : FormatCountdown(rune);
            if (RuneTreasureModel.Instance.TryGetLucky(RuneTreasureController.RuneType,
                    out RuneTreasureModel.LuckySnapshot lucky))
            {
                if (_lb_rate != null) _lb_rate.text = lucky.Value + "/" + LegacyLuckMax;
                if (_lb_percent != null) _lb_percent.text = "+" + lucky.Percent + "%";
            }
            else
            {
                if (_lb_rate != null) _lb_rate.text = "--/" + LegacyLuckMax;
                if (_lb_percent != null) _lb_percent.text = "+--%";
            }
            if (_img_progress != null) _img_progress.fillAmount = rune == null ? 0f : Mathf.Clamp01(rune.DrawTimes / 100f);
            if (red_dot != null) red_dot.gameObject.SetActive(free);
            if (_ten_red != null) _ten_red.gameObject.SetActive(false);
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            RuneTreasureModel.Instance.Changed += Render;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            RuneTreasureModel.Instance.Changed -= Render;
            _subscribed = false;
        }

        private static string FormatCountdown(RuneTreasureModel.RuneSnapshot rune)
        {
            if (rune == null || rune.FreeTime == 0) return string.Empty;
            long seconds = (long)rune.FreeTime - TimeUtil.NowSec();
            if (seconds <= 0) return "当前免费";
            TimeSpan remain = TimeSpan.FromSeconds(seconds);
            return remain.TotalHours >= 24
                ? ((int)remain.TotalHours) + remain.ToString(@"\:mm\:ss")
                : remain.ToString(@"hh\:mm\:ss");
        }

        private void OnDestroy() => Unsubscribe();

        private static void BindClick(Component target, Action action)
        {
            if (target == null) return;
            Image image = target as Image ?? target.GetComponent<Image>() ?? target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, action);
        }
    }
}
