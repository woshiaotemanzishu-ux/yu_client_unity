using System;
using System.Collections;
using Shenxiao.Generated.UI.UiComponent;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.UiComponent
{
    /// <summary>
    /// 圆形冷却(对标老客户端 uiComponent/CirCleCdView.ts):_img_mask 圆形遮罩(老端用 Laya drawPie 扇形,
    /// Unity 用 Filled-Radial360 的 fillAmount)随 cd 收缩 + _lb_cd 剩余秒。老端由 SkillManager 技能 cd 驱动。
    ///
    /// 降级:SkillManager(技能 cd 事件)未移植 → 提供**自包含本地计时** StartCd(total)/StopCd 完整可用;
    /// 技能绑定(skill_id)待接。视觉:_img_mask 设 Filled-Radial360 最佳(走 fillAmount);文字即时。
    /// </summary>
    public sealed class CirCleCdView : CirCleCdViewBind
    {
        private Coroutine _co;
        private Action _onComplete;

        protected override void OnInit()
        {
            SetMask(0f);
            if (_lb_cd != null) _lb_cd.text = "";
        }

        /// <summary>启动本地 cd(对标 Show:total 秒 + 可选完成回调)。自包含,不依赖 SkillManager。</summary>
        public void StartCd(float totalSeconds, Action onComplete = null)
        {
            StopCd();
            _onComplete = onComplete;
            if (totalSeconds > 0f)
                _co = StartCoroutine(CdRoutine(totalSeconds));
        }

        public void StopCd()
        {
            if (_co != null) { StopCoroutine(_co); _co = null; }
            SetMask(0f);
            if (_lb_cd != null) _lb_cd.text = "";
        }

        private IEnumerator CdRoutine(float total)
        {
            float t = total;
            while (t > 0f)
            {
                SetMask(t / total); // 剩余比例
                if (_lb_cd != null) _lb_cd.text = t > 1f ? Mathf.Ceil(t).ToString() : t.ToString("F1");
                t -= Time.deltaTime;
                yield return null;
            }
            SetMask(0f);
            if (_lb_cd != null) _lb_cd.text = "";
            _co = null;
            if (_onComplete != null) _onComplete();
        }

        private void SetMask(float rate)
        {
            if (_img_mask == null) return;
            _img_mask.gameObject.SetActive(rate > 0f);
            if (_img_mask.type == Image.Type.Filled) _img_mask.fillAmount = rate;
        }
    }
}
