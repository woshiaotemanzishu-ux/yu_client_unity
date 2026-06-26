using Shenxiao.Framework.Res;
using Shenxiao.Generated.UI.Login;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 资源加载页(对标老客户端 LoginLoadingView.ts)。
    ///
    /// data-only:结构(背景 / 进度条底图 _img_back / 前景 _img_front / 进度标 _img_progress_end /
    /// 三行文案 _lb_first_load·_lb_load_progress·_lb_version)已烤进 prefab,这里【只绑数据】。
    ///
    /// 进度条:老端用一个运行时 new 出来的 Laya.Sprite 当遮罩裁剪 _img_front(this._progress_mask),
    /// 每帧改它的 width。data-only 这里【不 new 任何节点】——改为把烤进 prefab 的容器 _mask_box 改宽
    /// + RectMask2D 裁剪子节点 _img_front,等价实现「前景随进度横向露出」。这是结构而非数据,
    /// 但遮罩节点本身在 Laya 里也是运行时产物(prefab 里没有对应节点),用容器改宽是最贴近的 data-only 译法。
    ///
    /// 进度推进 / 关闭时机 / 倒计时 / 网络锁标志 / runner 每帧 Update / GlobalEventSystem 事件 /
    /// EyouVn 防沉迷小窗(EyouThAdultItem)/ 3D 场景切换 等都是【真·运行时】,这里不实现,
    /// 由外部驱动 SetProgress;接管路由后再补事件桥(见 risks)。
    /// </summary>
    public sealed class LoginLoadingView : LoginLoadingViewBind
    {
        // 老端字面量:front_img_width = 635(进度条满宽);move_speed/left_time/count_down 等属推进逻辑,真·运行时不译。
        private const float DEFAULT_FULL_WIDTH = 635f;

        // 老端 _img_progress_end 贴边修正:mask_width<35 时 x=-20,否则 x=mask_width-55(见 ts OnChange)。
        private const float PROGRESS_END_NEAR_X = -20f;
        private const float PROGRESS_END_OFFSET = 55f;
        private const float PROGRESS_END_NEAR_THRESHOLD = 35f;

        private RectTransform _maskBox;     // 烤进 prefab 的进度遮罩容器(Bind 里 _mask_box 可能为 null,兜底取)
        private float _fullWidth;

        protected override void OnInit()
        {
            _maskBox = MaskBox();

            // 老端进度遮罩是运行时 Laya.Sprite,这里用容器改宽 + RectMask2D 裁剪 _img_front 等价实现。
            if (_maskBox != null && _maskBox.GetComponent<RectMask2D>() == null)
            {
                _maskBox.gameObject.AddComponent<RectMask2D>();
            }

            _fullWidth = _img_back != null ? _img_back.rectTransform.sizeDelta.x : DEFAULT_FULL_WIDTH;
            if (_fullWidth <= 0f) _fullWidth = DEFAULT_FULL_WIDTH;
        }

        protected override void OnShow(object args)
        {
            SetProgress(0f);
            RefreshBackground();
        }

        protected override void OnHide()
        {
        }

        protected override void OnDispose()
        {
        }

        /// <summary>
        /// 背景图:对标老端 LoadSuccess——有 _preload_bg_id 用 load_bg{id}.jpg,否则 fallback load_bg1.jpg
        /// (老端 LoginLoadingView.ts:220-227,无论如何都运行时加载背景)。
        /// 背景是动态 .jpg,prefab 里 _img_bg 没烤进 sprite(disabled),必须在这里加载,否则一片空白。
        /// </summary>
        private void RefreshBackground()
        {
            if (_img_bg == null) return;
            // 实际资源在 login/other/(老端 TS 字面量写的 texture/ 是其加载器的别名,Unity 资源以 other/ 为准)
            int bgId = ResolvePreloadBgId();
            string path = bgId > 0
                ? $"resource/game/login/other/load_bg{bgId}.jpg"
                : "resource/game/login/other/load_bg1.jpg"; // 对标老端 fallback
            _ = ResManager.SetImageAsync(_img_bg, path, nativeSize: false);
        }

        /// <summary>老端 LoginModel._preload_bg_id;现项目尚无该字段时返回 -1(用默认背景)。见 risks。</summary>
        private static int ResolvePreloadBgId()
        {
            return -1;
        }

        /// <summary>
        /// 进度驱动(对标老端 Update→OnChange):progress01 = 0~1。
        /// 改 _mask_box 宽度裁剪 _img_front、移动 _img_progress_end、刷新 loading 文案。
        /// label 不传时显示 "loading......N%"(老端 _lb_load_progress 文案)。
        /// </summary>
        public void SetProgress(float progress01, string label = null)
        {
            float p = Mathf.Clamp01(progress01);
            float maskWidth = _fullWidth * p;

            if (_maskBox != null)
            {
                _maskBox.sizeDelta = new Vector2(maskWidth, _maskBox.sizeDelta.y);
            }

            if (_img_progress_end != null)
            {
                RectTransform end = _img_progress_end.rectTransform;
                // 对标老端 OnChange:贴左修正,以及无进度时隐藏进度标。
                _img_progress_end.gameObject.SetActive(maskWidth > 0f);
                float x = maskWidth < PROGRESS_END_NEAR_THRESHOLD
                    ? PROGRESS_END_NEAR_X
                    : maskWidth - PROGRESS_END_OFFSET;
                end.anchoredPosition = new Vector2(x, end.anchoredPosition.y);
            }

            if (_lb_load_progress != null)
            {
                // 老端:loading......{(width/front_img_width*100).toFixed(2)}%
                float pro = _fullWidth > 0f ? maskWidth / _fullWidth * 100f : 0f;
                _lb_load_progress.text = label ?? $"loading......{pro:0.00}%";
            }
        }

        // ——— 容器字段没绑上时按名兜底(prefab 里有节点但 Bind 字段为 null,见 LoginLoadingViewBind)———
        // 路径:LoginLoadingView/_box_con/_box_progress/_mask_box
        private RectTransform MaskBox()
        {
            if (_mask_box != null) return _mask_box;
            Transform t = transform.Find("_box_con/_box_progress/_mask_box");
            return t as RectTransform;
        }
    }
}
