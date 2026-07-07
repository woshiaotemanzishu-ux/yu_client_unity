using System;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 战斗(PK)模式项(对标老客户端 MainUIFightModeItem.ts):背景 + 状态图标 + 描述,
    /// 点击切换该模式,选中显高亮(_img_select)。
    ///
    /// 降级:PkStatusModel.FightModeInfo 配置 + CHANGE_PK_STATUS 协议未移植 → 用最小 DTO
    /// <see cref="FightModeInfoData"/> 填字段;点击回调由父 View 注入(父再降级打日志)。
    /// 图标走 GetIcon("mainUI", source/bg_source)。
    /// </summary>
    public sealed class MainUIFightModeItem : MainUIFightModeItemBind
    {
        private FightModeInfoData _info;
        private Action<int> _onClick;
        private bool _clickBound;

        /// <summary>由父列表 MainUIFightModeView 克隆后调用,填本项数据 + 注入点击回调。</summary>
        public void SetData(FightModeInfoData info, Action<int> onClick)
        {
            _info = info;
            _onClick = onClick;
            if (info == null) return;

            if (_lb_desc != null) _lb_desc.text = info.Desc ?? "";
            if (_img_status != null && !string.IsNullOrEmpty(info.StatusIcon))
                _ = ResManager.SetImageAsync(_img_status, GameResPath.GetIcon("mainUI", info.StatusIcon), nativeSize: false);
            if (_img_bg != null && !string.IsNullOrEmpty(info.BgIcon))
                _ = ResManager.SetImageAsync(_img_bg, GameResPath.GetIcon("mainUI", info.BgIcon), nativeSize: false);

            // 对标 InitEvent:点 _img_bg → call_func(info.pk_status)。只绑一次。
            if (!_clickBound && _img_bg != null)
            {
                _img_bg.raycastTarget = true;
                UIUtil.AddClick(_img_bg, OnClickBg);
                _clickBound = true;
            }
        }

        /// <summary>对标 SetSelect:高亮当前选中模式。</summary>
        public void SetSelect(bool selected)
        {
            if (_img_select != null) _img_select.gameObject.SetActive(selected);
        }

        private void OnClickBg()
        {
            if (_info != null) _onClick?.Invoke(_info.PkStatus);
        }
    }

    /// <summary>PK 模式项数据(对标老端 PkStatusModel.FightModeInfo 单条;静态表见 PkStatusModel.FightModeInfo)。</summary>
    public sealed class FightModeInfoData
    {
        public int PkStatus;
        public string StatusName;   // 老端 info.status_name(和平/全体/…)
        public string Desc;
        public string StatusIcon;   // 老端 info.source(弹窗列表项图标)
        public string MainIcon;     // 老端 info.main_source(HudTop 战斗模式角标)
        public string BgIcon;       // 老端 info.bg_source(列表项底图)
    }
}
