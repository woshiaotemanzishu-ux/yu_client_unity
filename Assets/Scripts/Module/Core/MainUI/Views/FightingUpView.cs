using System.Collections;
using Shenxiao.Generated.UI.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 战力提升提示窗(对标老客户端 FightingUpView.ts):战力上升时弹出,显示新战力 + 增量 "+N",
    /// 老端有数字滚动动画 + "ui_zhanli" 特效 + 停留 wait_time 秒后自动关闭。
    ///
    /// 降级:战力变化触发源、Runner 逐帧数字滚动、ui_zhanli 特效、fight_up 位图字体均未移植 →
    /// 直接显示终值 + 增量(无滚动动画/特效),停留后自动关闭。事件驱动弹层(战力变化时开),
    /// 默认关闭、不进 FirstPass;战力变化事件接上后由它 Show(FightUpData)。
    /// </summary>
    public sealed class FightingUpView : FightingUpViewBind
    {
        /// <summary>整体停留多少秒后自动关闭(对标老端 wait_time=1.8)。</summary>
        [SerializeField] private float _waitSeconds = 1.8f;

        private long _oldFight;
        private long _newFight;
        private Coroutine _autoClose;

        /// <summary>缓存战力旧/新值(对标 SetData);若已显示立即刷新。</summary>
        public void SetData(long oldFight, long newFight)
        {
            _oldFight = oldFight;
            _newFight = newFight;
            if (IsShown) Apply();
        }

        protected override void OnShow(object args)
        {
            if (args is FightUpData d) { _oldFight = d.OldFight; _newFight = d.NewFight; }
            Apply();
        }

        protected override void OnHide()
        {
            StopAutoClose();
        }

        protected override void OnDispose()
        {
            StopAutoClose();
        }

        private void Apply()
        {
            // 降级:无逐帧滚动,直接落终值(对标 SetAnimation 的最终态)。
            if (_lb_fight != null) _lb_fight.text = _newFight.ToString();

            long add = _newFight - _oldFight;
            if (_lb_add_fight != null)
            {
                _lb_add_fight.gameObject.SetActive(add > 0);
                _lb_add_fight.text = "+" + add;
            }
            // ui_zhanli 特效未移植 → _box_effect 留空。

            StartAutoClose();
        }

        private void StartAutoClose()
        {
            StopAutoClose();
            _autoClose = StartCoroutine(AutoCloseRoutine());
        }

        private void StopAutoClose()
        {
            if (_autoClose != null)
            {
                StopCoroutine(_autoClose);
                _autoClose = null;
            }
        }

        private IEnumerator AutoCloseRoutine()
        {
            yield return new WaitForSeconds(_waitSeconds);
            _autoClose = null;
            Hide();
        }
    }

    /// <summary>战力提升数据(待战力变化事件移植后填充)。</summary>
    public sealed class FightUpData
    {
        public long OldFight;
        public long NewFight;
    }
}
