using System.Collections.Generic;
using Shenxiao.Framework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 等待开服 Loading(对标老客户端 login/WaitforOpenViewLoading.ts)。
    ///
    /// data-only:结构(旋转圈 _img_circle + loading 文字 _lb_loading)由重构 UI 生成器建进 prefab,
    /// 这里【只绑数据/驱动表现】——不运行时 new 节点、不摆位置。老端没有 Item 列表,
    /// 故无「数据驱动列表」一节。
    ///
    /// 旋转:老端用 Laya.TimeLine 让 _img_circle 2000ms 转一圈(rotation -360),等价为每帧 Rotate(-180°/s)。
    /// 这是表现动画、非静态布局,保留(对标 LoginCreateRoleView 的「3D 模型/特效真·运行时保留」)。
    ///
    /// 引用计数 + 15s 超时:老端 curr_loading_view_dic[hash_code]=NowTime,事件 SHOW/HIDE 增删,空了就 Hide;
    /// OnTimer(200ms loop)里清 15s 过期项 + 兜底 Hide。这里译为按 hash_code 的引用计数表 + Time.unscaledTime
    /// 时间戳,Show(hash)/Hide(hash) 增删,Update 里淘汰过期项,空了自动隐藏。事件桥(GlobalEventSystem 的
    /// SHOW_WAITFOR_OPENVIEW_LOADING / HIDE_WAITFOR_OPENVIEW_LOADING)项目暂无对应 GlobalEvent 常量,故由外部直接
    /// 调 Show(hash)/Hide(hash) 驱动,事件桥待路由接管后补(见 risks)。
    ///
    /// 0.15s 延迟显形:老端 start_show_time 后 0.15s 才真正 SetVisible(true)(避免一闪而过的 loading)。这里保留。
    ///
    /// 真·运行时(平台 WX/Eyou 防沉迷的点点动画 UpdateTimer/_dot_accout、PlatformManager 判定、th_text 泰语文案)
    /// 属平台特化分支,本轮不译(项目无 PlatformManager 对等;见 risks),只保留主线旋转圈 + 文字。
    /// </summary>
    public sealed class WaitforOpenViewLoading : BaseView
    {
        [Header("Prefab 引用（由 WaitforOpenViewLoadingCreator 回填）")]
        public Image _img_circle;
        public TextMeshProUGUI _lb_loading;

        /// <summary>旋转速度(度/秒;对标老端 TimeLine 2000ms 一圈 = 180°/s,负向)。</summary>
        [SerializeField] private float _rotateSpeed = 180f;

        /// <summary>老端 OnTimer 里每条 loading 源 15s 未刷新即淘汰。</summary>
        private const float LOADING_TIMEOUT = 15f;

        /// <summary>老端 start_show_time 后 0.15s 才真正显形。</summary>
        private const float DELAY_VISIBLE = 0.15f;

        /// <summary>对标 curr_loading_view_dic:key=请求方 hash_code,value=最近一次刷新的(unscaled)时间戳。</summary>
        private readonly Dictionary<int, float> _loadingSources = new Dictionary<int, float>();
        private readonly List<int> _expiredKeys = new List<int>();

        /// <summary>对标 start_show_time:首次有 loading 源时记录;为负表示当前未在显示流程中。</summary>
        private float _startShowTime = -1f;

        /// <summary>圈是否在转(对应老端 circle_time_line 存活与否)。</summary>
        private bool _spinning;

        protected override void BindNodes()
        {
            EnsureBound(nameof(_img_circle), _img_circle);
            EnsureBound(nameof(_lb_loading), _lb_loading);
        }

        protected override void OnInit()
        {
            // 旋转圈起始角清零(对标老端 Hide 时 _img_circle.rotation = 0)。
            if (_img_circle != null) _img_circle.transform.localRotation = Quaternion.identity;
        }

        protected override void OnShow(object args)
        {
            // BaseView.Show 已 SetActive(true)。本视图真正「可见」由 0.15s 延迟门控(_img_circle/_lb_loading 的显隐)。
            // OnShow 不预置可见,等到有 loading 源且过了延迟阈值再露出(对标老端 Show/OnTimer)。
            ApplyVisible(_loadingSources.Count > 0 && PastDelay());
            _spinning = _loadingSources.Count > 0;
        }

        protected override void OnHide()
        {
            StopSpin();
            _startShowTime = -1f;
        }

        protected override void OnDispose()
        {
            _loadingSources.Clear();
        }

        private void Update()
        {
            // 旋转圈(对标老端 TimeLine,每帧 -180°/s)。
            if (_spinning && _img_circle != null)
            {
                _img_circle.transform.Rotate(0f, 0f, -_rotateSpeed * Time.unscaledDeltaTime);
            }

            if (_loadingSources.Count == 0) return;

            float now = Time.unscaledTime;

            // 对标 OnTimer:延迟 0.15s 后才真正显形。
            if (_startShowTime >= 0f && now - _startShowTime >= DELAY_VISIBLE)
            {
                ApplyVisible(true);
            }

            // 淘汰 15s 未刷新的 loading 源(对标 OnTimer 的 15s 过期清理)。
            _expiredKeys.Clear();
            foreach (var kv in _loadingSources)
            {
                if (now - kv.Value >= LOADING_TIMEOUT) _expiredKeys.Add(kv.Key);
            }
            for (int i = 0; i < _expiredKeys.Count; i++) _loadingSources.Remove(_expiredKeys[i]);

            // 空了自动隐藏(对标 OnTimer 末尾 IsTableEmpty → Hide)。
            if (_loadingSources.Count == 0) Hide();
        }

        /// <summary>
        /// 注册一个 loading 源并显示(对标 onShowWaitforOpenViewLoading):curr_loading_view_dic[hash]=NowTime → Show。
        /// hashCode 标识请求方,多源去重靠它;同一源重复调即刷新时间戳(续期)。
        /// </summary>
        public void Show(int hashCode)
        {
            bool first = _loadingSources.Count == 0;
            _loadingSources[hashCode] = Time.unscaledTime;
            if (first || _startShowTime < 0f) _startShowTime = Time.unscaledTime;

            if (!IsShown) Show(); // BaseView.Show():SetActive(true) + OnShow
            StartSpin();
        }

        /// <summary>
        /// 注销一个 loading 源(对标 onHideWaitforOpenViewLoading):delete dic[hash];空了 Hide。
        /// </summary>
        public void Hide(int hashCode)
        {
            _loadingSources.Remove(hashCode);
            if (_loadingSources.Count == 0) Hide(); // BaseView.Hide()
        }

        /// <summary>设 loading 文字(对标老端 _lb_loading.text)。</summary>
        public void SetText(string str)
        {
            if (_lb_loading != null) _lb_loading.text = str ?? string.Empty;
        }

        private bool PastDelay()
        {
            return _startShowTime >= 0f && Time.unscaledTime - _startShowTime >= DELAY_VISIBLE;
        }

        /// <summary>表现层显隐(只控烤进 prefab 的 _img_circle/_lb_loading,不动节点结构)。</summary>
        private void ApplyVisible(bool visible)
        {
            if (_img_circle != null) _img_circle.gameObject.SetActive(visible);
            if (_lb_loading != null) _lb_loading.gameObject.SetActive(visible);
        }

        private void StartSpin()
        {
            _spinning = true;
        }

        private void StopSpin()
        {
            _spinning = false;
            // 对标老端 Hide:circle_time_line 销毁后 _img_circle.rotation = 0。
            if (_img_circle != null) _img_circle.transform.localRotation = Quaternion.identity;
        }
    }
}
