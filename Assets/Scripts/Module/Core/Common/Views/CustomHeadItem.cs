using System;
using Shenxiao.Generated.UI.Common;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Login;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Common
{
    /// <summary>
    /// 玩家头像项(对标老客户端 common/CustomHeadItem.ts):头像(系统/自定义)+ 头像框 + 等级牌 + 队伍底,点击进玩家信息。
    ///
    /// 公开 API 对标:SetLevel / SetActiveFrame / SetActiveLevel / SetActiveBg / SetClickFunc + 便捷 SetData(level)。
    /// 降级:DressModel/RoleManager/CustomRoleHead(头像图标、头像框、跨服头像)未移植 → 头像/框图标待接(打TODO);
    /// 等级数字、各覆盖层开关、点击回调 即时可用。覆盖层(框/等级/队伍底/自定义头像)OnInit 默认隐藏。
    /// </summary>
    public sealed class CustomHeadItem : CustomHeadItemBind
    {
        private Action _clickCb;
        private int _headRequestVersion;

        protected override void OnInit()
        {
            if (frame != null) frame.gameObject.SetActive(false);
            if (team_bg != null) team_bg.gameObject.SetActive(false);
            if (_level_gp != null) _level_gp.gameObject.SetActive(false);
            if (icon != null) icon.gameObject.SetActive(false); // 自定义头像待接,默认走 icon_sys_head 占位
            BindClick();
        }

        /// <summary>便捷:填等级(头像/框待 DressModel)。</summary>
        public void SetData(int level)
        {
            SetLevel(level);
            GameLog.Info("Common", "CustomHeadItem 头像/头像框 待对接 DressModel/RoleManager(等级已显示)");
        }

        /// <summary>等级牌(对标 UpdateLevel:>370 为转生段,显示 level-370)。图标皮肤待接,数字即时。</summary>
        public void SetRoleData(int career, int turn, int level, bool showLevel)
        {
            if (showLevel) SetLevel(level);
            else SetActiveLevel(false);
            SetDefaultHead(career, turn);
        }

        public async void SetDefaultHead(int career, int turn)
        {
            int version = ++_headRequestVersion;
            await LoginConfigs.EnsureLoaded();
            if (this == null || version != _headRequestVersion) return;
            string path = LoginConfigs.HeadIconPath(career, turn);
            if (string.IsNullOrEmpty(path))
            {
                GameLog.Warn("Common", "CustomHeadItem default head path missing career={0} turn={1}", career, turn);
                return;
            }

            if (icon != null) icon.gameObject.SetActive(false);
            if (icon_sys_head != null)
            {
                icon_sys_head.gameObject.SetActive(true);
                icon_sys_head.enabled = true;
                icon_sys_head.color = Color.white;
                icon_sys_head.preserveAspect = true;
                HideSystemHeadOverlay();
                _ = ResManager.SetImageAsync(icon_sys_head, path, nativeSize: false);
            }
        }

        /// <summary>显示装扮系统当前穿戴头像；加载失败时保留此前默认头像。</summary>
        public async void SetCustomHead(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            int version = ++_headRequestVersion;
            if (icon != null) icon.gameObject.SetActive(false);
            bool loaded = icon != null && await ResManager.SetImageAsync(icon, path, nativeSize: false);
            if (this == null || version != _headRequestVersion || !loaded) return;
            if (icon_sys_head != null) icon_sys_head.gameObject.SetActive(false);
            icon.gameObject.SetActive(true);
            icon.enabled = true;
            icon.color = Color.white;
            icon.preserveAspect = true;
        }

        private void HideSystemHeadOverlay()
        {
            if (icon_sys_head == null) return;
            Transform t = icon_sys_head.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform child = t.GetChild(i);
                if (child != null) child.gameObject.SetActive(false);
            }
        }

        public void SetLevel(int level)
        {
            if (_level_gp != null) _level_gp.gameObject.SetActive(true);
            if (_lev_ != null)
            {
                bool sc = level > 370;
                _lev_.text = (sc ? level - 370 : level).ToString();
            }
        }

        /// <summary>头像框开关。</summary>
        public void SetActiveFrame(bool active)
        {
            if (frame != null) frame.gameObject.SetActive(active);
        }

        /// <summary>等级牌开关。</summary>
        public void SetActiveLevel(bool active)
        {
            if (_level_gp != null) _level_gp.gameObject.SetActive(active);
        }

        /// <summary>底图开关(bg + bg2)。</summary>
        public void SetActiveBg(bool active)
        {
            if (bg != null) bg.gameObject.SetActive(active);
            if (bg2 != null) bg2.gameObject.SetActive(active);
        }

        /// <summary>点击回调(对标 SetClickFunc);未设则打日志。</summary>
        public void SetClickFunc(Action callback)
        {
            _clickCb = callback;
        }

        private void BindClick()
        {
            if (click_group == null) return;
            Image img = click_group.GetComponent<Image>();
            if (img == null) img = click_group.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, OnClick);
        }

        private void OnClick()
        {
            if (_clickCb != null) { _clickCb(); return; }
            GameLog.Info("Common", "头像点击 → 待对接 玩家信息面板");
        }
    }
}
