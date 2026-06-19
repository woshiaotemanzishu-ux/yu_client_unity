using System.Collections;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 主 UI 特效容器(对标老客户端 MainUIEffectView.ts):在 _box_eff 挂一个 UI 特效,计时自动关闭防残留。
    ///
    /// 降级:UI 特效系统(AddUIEffect/ClearUIEffect)未移植 → SetData 只记录 + 打日志「待对接 UI 特效」,
    /// _box_eff 留空;仍保留计时自动关闭(对标 SetCountDown=15s)。事件驱动弹层(放特效时开),
    /// 默认关闭、不进 FirstPass;UI 特效系统移植后 SetData 里 AddUIEffect(name, _box_eff, pos, scale)。
    /// </summary>
    public sealed class MainUIEffectView : MainUIEffectViewBind
    {
        /// <summary>计时自动关闭(对标老端 time_count=15)。</summary>
        [SerializeField] private float _autoCloseSeconds = 15f;

        private Coroutine _close;

        protected override void OnShow(object args)
        {
            if (args is EffectMountData d) SetData(d);
            else StartAutoClose();
        }

        protected override void OnHide() => StopAutoClose();
        protected override void OnDispose() => StopAutoClose();

        /// <summary>对标 SetData:挂特效到 _box_eff(降级:特效系统未移植,先打日志)。</summary>
        public void SetData(EffectMountData data)
        {
            // 老端:ClearUIEffect(_box_eff) + AddUIEffect(obj.name, _box_eff, obj.pos, obj.scale)。特效系统未移植。
            GameLog.Info("MainUI", "主UI特效 name={0} → 待对接 UI 特效系统 AddUIEffect", data != null ? data.Name : "(null)");
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

    /// <summary>UI 特效挂载数据(待 UI 特效系统移植后填:特效名/位置/缩放)。</summary>
    public sealed class EffectMountData
    {
        public string Name;
        public Vector2 Pos;
        public float Scale = 1f;
    }
}
