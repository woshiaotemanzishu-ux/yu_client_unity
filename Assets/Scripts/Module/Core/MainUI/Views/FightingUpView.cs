using System;
using System.Collections;
using System.Threading.Tasks;
using Shenxiao.Common.UI3D;
using Shenxiao.Generated.UI.MainUI;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 战力提升提示窗(对标老客户端 FightingUpView.ts):战力上升时弹出,显示新战力 + 增量 "+N",
    /// 有数字滚动动画(战力值 5 帧后 10 帧内滚到新值)+ 增量文字上移淡出式位移(5 帧后 5 帧内上移 30px)+
    /// "ui_zhanli" 特效 + 停留 wait_time=1.8s 后自动关闭。连续触发(战力接连上升)时增量数值累加、
    /// 数字滚动动画从当前进度继续顺滑过渡到新目标,照抄老端 SetAnimation/Update 的帧驱动语义。
    ///
    /// 帧驱动说明:老端 Runner 按引擎每帧调用一次 Update(逐帧计数,不是按固定 deltaTime),这里同样用
    /// MonoBehaviour.Update 每帧 +1 计"帧"(gameObject 只在弹层显示期间才 active,故只在显示时跑),
    /// 与老端"1 次引擎帧 = 1 帧"的耦合方式一致,不额外发明"模拟帧率"换算。
    ///
    /// 位图字体 fight_up/fight_up2 未转换成 TMP FontAsset → 视觉近似(渐变色+描边+粗斜体)在
    /// HudOverlayCombatCreator.StyleFightNumberLabel 里做(建树期样式,运行时这里不碰颜色/字号)。
    /// </summary>
    public sealed class FightingUpView : FightingUpViewBind
    {
        // ---------------------------------------------------------------- 老端字段对照(帧参数)
        private const int FightAddFontStartMoveFrame = 5;   // fight_add_font_start_move_frame
        private const int FightAddFontMoveFrame = 5;         // fight_add_font_move_frame
        private const float FightAddFontMoveDistance = 30f;  // fight_add_font_move_distance
        private const int FightFontChangeStartFrame = 5;     // fight_font_change_start_frame
        private const int FightFontChangeFrame = 10;         // fight_font_change_frame
        private const int FightAddFontChangeFrame = 5;       // fight_add_font_change_frame
        private const int PlayEffectFrame = 0;                // play_effect_frame

        /// <summary>老端 update_fight_effect_time_id = setTimeout(1300ms) 节流:同一波连续触发,
        /// 1.3s 内不重挂特效(避免 ui_zhanli 一次性粒子被疯狂重播)。这里用时间戳惰性判断代替 setTimeout。</summary>
        private const float EffectReplayCooldown = 1.3f;

        /// <summary>对标老端 AddUIEffect("ui_zhanli", box, null, 1, ...) 的 scale=1;12.8 是 UIEffectStage
        /// 相机固定取景 12.8 世界单位的换算基准(同 ActivityIcon/MainUIDownView 的既有用法),不是新发明的值。</summary>
        private const float EffectScale = 12.8f;
        private const string EffectName = "ui_zhanli";

        /// <summary>整体停留多少秒后自动关闭(对标老端 wait_time=1.8)。</summary>
        [SerializeField] private float _waitSeconds = 1.8f;

        private long _oldFight;
        private long _newFight;

        // ---- 数值滚动累加状态(对标老端同名字段) ----
        private long _localFightValue;      // local_fight_value
        private long _realFightValue;       // real_fight_value
        private long _fightChangeValue;     // fight_change_value
        private long _localFightAddValue;   // local_fight_add_value
        private long _realFightAddValue;    // real_fight_add_value
        private long _fightAddChangeValue;  // fight_add_change_value

        private bool _isPlaying;            // is_playing
        private int _nowFrame;              // now_frame
        private int _fightFontChangeEndFrame;
        private int _fightAddFontChangeEndFrame;
        private bool _hasSetFightAddFontPos;
        private bool _isFirstOpen = true;

        // ---- 增量文字上移动画的缓存 Y(LoadSuccess 里的 cache_add_y/end_add_y) ----
        private float _cachedAddY;
        private float _endAddY;
        private bool _layoutCached;

        // ---- 增量文字右对齐用的"战力数值文本左边缘"(cache_fight_x 的通用换算,不依赖具体 Place 坐标) ----
        private float _fightLeftEdgeX;
        private float _addFightPivotOffsetX;

        // ---- ui_zhanli 特效(对标 has_set_effect / can_replay_fight_effect) ----
        private bool _hasSetEffect;
        private bool _effectLoading;
        private int _effectVersion;
        private float _effectPlayedAt = -EffectReplayCooldown;
        private UIEffectStage.Handle _effectHandle;

        private Coroutine _autoClose;

        protected override void OnInit()
        {
            CacheLayout();
        }

        /// <summary>缓存战力旧/新值(对标 SetData);若已显示立即刷新。</summary>
        public void SetData(long oldFight, long newFight)
        {
            _oldFight = oldFight;
            _newFight = newFight;
            if (IsShown) BeginOrExtendAnimation(_oldFight, _newFight);
        }

        protected override void OnShow(object args)
        {
            if (args is FightUpData d) { _oldFight = d.OldFight; _newFight = d.NewFight; }
            CacheLayout();

            // 对标老端 open_callback:每次真正弹出都重置增量文字可见性/位置,再据当前值播放一遍动画。
            if (_lb_add_fight != null)
            {
                _lb_add_fight.gameObject.SetActive(false);
                SetAddFightY(_cachedAddY);
            }

            BeginOrExtendAnimation(_oldFight, _newFight);
        }

        protected override void OnHide()
        {
            ResetAnimationState();
        }

        protected override void OnDispose()
        {
            ResetAnimationState();
        }

        /// <summary>LoadSuccess 里读一次的初始布局值,只需要缓存一次(与 Place() 具体数值解耦,通用换算)。</summary>
        private void CacheLayout()
        {
            if (_layoutCached) return;
            _layoutCached = true;

            if (_lb_fight != null)
            {
                RectTransform rt = _lb_fight.rectTransform;
                _fightLeftEdgeX = rt.anchoredPosition.x - rt.rect.width * rt.pivot.x; // cache_fight_x 的通用版
            }
            if (_lb_add_fight != null)
            {
                RectTransform rt = _lb_add_fight.rectTransform;
                _addFightPivotOffsetX = rt.rect.width * rt.pivot.x;
                _cachedAddY = rt.anchoredPosition.y;   // cache_add_y
                _endAddY = _cachedAddY - FightAddFontMoveDistance; // end_add_y
            }
        }

        /// <summary>对标老端 SetAnimation(old_fight, new_fight)。</summary>
        private void BeginOrExtendAnimation(long oldFight, long newFight)
        {
            if (oldFight <= 0 || newFight <= oldFight) return;

            StopAutoClose(); // CancelTime()

            if (_localFightValue == 0) _localFightValue = oldFight;
            _realFightValue = newFight;
            _fightChangeValue = _realFightValue - _localFightValue;

            long add = newFight - oldFight;
            if (_localFightAddValue == 0) _localFightAddValue = add;
            _realFightAddValue += add; // 连续触发时增量累加
            _fightAddChangeValue = _realFightAddValue - _localFightAddValue;

            if (_isFirstOpen)
            {
                _isFirstOpen = false;
                if (_lb_fight != null) _lb_fight.text = _localFightValue.ToString();

                string addStr = "+" + _localFightAddValue;
                if (_lb_add_fight != null) _lb_add_fight.text = addStr;
                PositionAddFightRight(_realFightValue, addStr); // 老端只在首次打开时算一次右对齐位置

                _fightFontChangeEndFrame = FightFontChangeFrame + FightFontChangeStartFrame;
                _fightAddFontChangeEndFrame = 0; // 首次打开:增量数字不做滚动,直接显示终值
                _nowFrame = 0;
            }
            else
            {
                _fightFontChangeEndFrame = Math.Max(_nowFrame + FightFontChangeFrame, FightFontChangeFrame + FightFontChangeStartFrame);
                _fightAddFontChangeEndFrame = _nowFrame + FightAddFontChangeFrame;
            }

            // 对标 can_replay_fight_effect:节流窗口过了才允许下一次重新挂 ui_zhanli(粒子是一次性 burst,
            // 重挂即重播;窗口内保持已有特效播放,不重复加载)。
            if (_hasSetEffect && Time.time - _effectPlayedAt >= EffectReplayCooldown)
            {
                _hasSetEffect = false;
            }

            _isPlaying = true;
        }

        private void Update()
        {
            if (!_isPlaying) return;
            _nowFrame++;

            if (_nowFrame >= PlayEffectFrame && !_hasSetEffect)
            {
                _hasSetEffect = true;
                _ = PlayFightEffectAsync();
            }

            if (!_hasSetFightAddFontPos && _nowFrame >= FightAddFontStartMoveFrame)
            {
                if (_lb_add_fight != null) _lb_add_fight.gameObject.SetActive(true);
                if (_nowFrame < FightAddFontStartMoveFrame + FightAddFontMoveFrame)
                {
                    int count = _nowFrame - FightAddFontStartMoveFrame;
                    float y = (float)count / FightAddFontMoveFrame * (_endAddY - _cachedAddY) + _cachedAddY;
                    SetAddFightY(y);
                }
                else
                {
                    SetAddFightY(_endAddY);
                    _hasSetFightAddFontPos = true;
                }
            }

            if (_nowFrame >= FightFontChangeStartFrame)
            {
                if (_nowFrame < _fightFontChangeEndFrame)
                {
                    long remain = _fightFontChangeEndFrame - _nowFrame;
                    long value = _realFightValue - (long)Math.Floor((double)_fightChangeValue * remain / FightFontChangeFrame);
                    _localFightValue = value;
                    if (_lb_fight != null) _lb_fight.text = value.ToString();
                }
                else if (_lb_fight != null)
                {
                    _lb_fight.text = _realFightValue.ToString();
                }
            }

            if (_fightAddFontChangeEndFrame != 0)
            {
                if (_nowFrame < _fightAddFontChangeEndFrame)
                {
                    long remain = _fightAddFontChangeEndFrame - _nowFrame;
                    long value = _realFightAddValue - (long)Math.Floor((double)_fightAddChangeValue * remain / FightAddFontChangeFrame);
                    _localFightAddValue = value;
                    if (_lb_add_fight != null) _lb_add_fight.text = "+" + value;
                }
                else if (_lb_add_fight != null)
                {
                    _lb_add_fight.text = "+" + _realFightAddValue;
                }
            }

            bool fightAddDone = _fightAddFontChangeEndFrame == 0 || _nowFrame > _fightAddFontChangeEndFrame;
            bool fightDone = _nowFrame > _fightFontChangeEndFrame;
            if (fightAddDone && fightDone)
            {
                _isPlaying = false;
                StartAutoClose(); // StartTime()
            }
        }

        /// <summary>老端:_lb_add_fight.x = cache_fight_x + fight_len - add_len(右边缘贴到"新战力值"文本的右边缘)。
        /// 用 TMP 实测宽度(GetPreferredValues)代替老端硬编码的位图字体逐字符宽表,只在首次打开算一次。</summary>
        private void PositionAddFightRight(long finalFight, string addText)
        {
            if (_lb_fight == null || _lb_add_fight == null) return;
            Vector2 fightSize = _lb_fight.GetPreferredValues(finalFight.ToString());
            Vector2 addSize = _lb_add_fight.GetPreferredValues(addText);
            float addLeftEdge = _fightLeftEdgeX + fightSize.x - addSize.x;

            RectTransform rt = _lb_add_fight.rectTransform;
            rt.anchoredPosition = new Vector2(addLeftEdge + _addFightPivotOffsetX, rt.anchoredPosition.y);
        }

        private void SetAddFightY(float y)
        {
            if (_lb_add_fight == null) return;
            RectTransform rt = _lb_add_fight.rectTransform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
        }

        /// <summary>对标 AddUIEffect("ui_zhanli", _box_effect, null, 1, ...)。写法参照
        /// MainUIDownView.AddExpEffectAsync 的防重入/迟到回调:用版本号让挂起期间被打断的加载自弃,不留悬挂 Handle。</summary>
        private async Task PlayFightEffectAsync()
        {
            if (_box_effect == null || _effectLoading) return;
            _effectLoading = true;
            int version = ++_effectVersion;

            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(
                EffectName, _box_effect, Vector2.zero, new Vector3(EffectScale, EffectScale, EffectScale));

            _effectLoading = false;
            if (this == null || version != _effectVersion || _box_effect == null || !gameObject.activeInHierarchy)
            {
                handle?.Dispose();
                return;
            }

            _effectHandle?.Dispose(); // 换一批新的(理论上上一份此时应已播完/已被 Dispose,这里兜底不留泄漏)
            _effectHandle = handle;
            _effectPlayedAt = Time.time;
        }

        private void StartAutoClose()
        {
            if (_autoClose != null) return; // 对标老端 StartTime 的 if(!this.time_id) 守卫
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

        /// <summary>对标老端 Remove():关闭/销毁时把累加状态清零,下次重开当"首次打开"处理。</summary>
        private void ResetAnimationState()
        {
            _isPlaying = false;
            _nowFrame = 0;
            _localFightValue = 0;
            _realFightValue = 0;
            _localFightAddValue = 0;
            _realFightAddValue = 0;
            _hasSetFightAddFontPos = false;
            _isFirstOpen = true;
            _hasSetEffect = false;

            _effectVersion++; // 让还在飞行中的 PlayFightEffectAsync await 自弃
            _effectHandle?.Dispose();
            _effectHandle = null;

            StopAutoClose();
        }
    }

    /// <summary>战力提升数据(战力变化事件 → FightingUpView.Show 的参数)。</summary>
    public sealed class FightUpData
    {
        public long OldFight;
        public long NewFight;
    }
}
