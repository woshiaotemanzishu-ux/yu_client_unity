using System;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Bossdomain;
using UnityEngine;

namespace Shenxiao.Module.Core.Boss.Views.BossDomain
{
    public sealed class BossDomainItem : BossDomainItemBind
    {
        private KfBossModel.DecorationBossEntry _entry;
        private Action _clicked;
        public int BossId => _entry == null ? 0 : _entry.BossId;

        protected override void OnInit()
        {
            if (_item_ng != null) UIUtil.AddClick(_item_ng, () => _clicked?.Invoke());
        }

        public void SetData(KfBossModel.DecorationBossEntry entry, bool selected, Action clicked)
        {
            _entry = entry;
            _clicked = clicked;
            int capacity = BossConfigs.ReadInt(BossConfigs.GetDecorationBoss(entry.BossId), "role_num");
            if (gp_scene != null) gp_scene.gameObject.SetActive(entry.IsAlive);
            if (text_scene != null) text_scene.text = "场景：" + Math.Min(entry.RoleNum, capacity) + "人";
            if (_lb_time != null)
            {
                long remain = Math.Max(0, entry.RebornTime - TimeUtil.NowSec());
                _lb_time.text = entry.IsAlive ? "已刷新" : "复活时间：" + remain + "秒";
            }
            if (_img_icon != null) _img_icon.gameObject.SetActive(!entry.IsAlive);
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            if (_img_select != null) _img_select.gameObject.SetActive(selected);
        }
    }
}
