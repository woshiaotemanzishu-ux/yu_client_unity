using System.Collections;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 伙伴技能特效容器(对标老客户端 MainUIEffectPartnerSkillView.ts):在 _box_eff 挂伙伴技能特效,计时自动关闭。
    ///
    /// 降级:UI 特效系统(AddUIEffect/ClearUIEffect)未移植 → SetData 只记录 + 打日志;_box_eff 留空;
    /// 保留计时自动关闭(对标 SetCountDown=50s)。事件驱动弹层,默认关闭、不进 FirstPass。复用 EffectMountData。
    /// </summary>
    public sealed class MainUIEffectPartnerSkillView : MainUIEffectPartnerSkillViewBind
    {
        /// <summary>计时自动关闭(对标老端 time_count=50)。</summary>
        [SerializeField] private float _autoCloseSeconds = 50f;

        private Coroutine _close;

        protected override void OnShow(object args)
        {
            if (args is EffectMountData d) SetData(d);
            else StartAutoClose();
        }

        protected override void OnHide() => StopAutoClose();
        protected override void OnDispose() => StopAutoClose();

        /// <summary>对标 SetData:挂伙伴技能特效到 _box_eff(降级:特效系统未移植,先打日志)。</summary>
        public void SetData(EffectMountData data)
        {
            GameLog.Info("MainUI", "伙伴技能特效 name={0} → 待对接 UI 特效系统 AddUIEffect", data != null ? data.Name : "(null)");
            StartAutoClose();
        }

        private void StartAutoClose()
        {
            StopAutoClose();
            _close = StartCoroutine(CloseRoutine());
        }

        private void StopAutoClose()
        {
            if (_close != null) { StopCoroutine(_close); _close = null; }
        }

        private IEnumerator CloseRoutine()
        {
            yield return new WaitForSeconds(_autoCloseSeconds);
            _close = null;
            Hide();
        }
    }
}
