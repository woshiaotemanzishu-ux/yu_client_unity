using Shenxiao.Generated.UI.Login;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 资源加载页(对标 Laya LoginLoadingView):进度条 = _mask_box 改宽裁剪 _img_front,
    /// 文案 = _lb_load_progress。启动加载与进游戏加载复用。
    /// </summary>
    public sealed class LoginLoadingView : LoginLoadingViewBind
    {
        // 背景图已由转换器「动态图默认填充」烘进 prefab(load_bg*.jpg 取第一张);
        // Laya 按 id 随机换图的逻辑接平台/活动配置时再用 ResManager.SetImageAsync 覆盖
        private float _fullWidth;

        protected override void OnInit()
        {
            // Laya 用运行时改宽的遮罩当进度条,转换产物是静态初始态,这里补上裁剪行为
            if (_mask_box != null && _mask_box.GetComponent<RectMask2D>() == null)
            {
                _mask_box.gameObject.AddComponent<RectMask2D>();
            }
            _fullWidth = _img_back != null ? _img_back.rectTransform.sizeDelta.x : 635f;
            SetProgress(0f);
        }

        /// <summary>progress01: 0~1;label 不传则显示 loading......N%。</summary>
        public void SetProgress(float progress01, string label = null)
        {
            float p = Mathf.Clamp01(progress01);
            if (_mask_box != null)
            {
                _mask_box.sizeDelta = new Vector2(_fullWidth * p, _mask_box.sizeDelta.y);
            }
            if (_img_progress_end != null)
            {
                RectTransform end = _img_progress_end.rectTransform;
                end.anchoredPosition = new Vector2(_fullWidth * p - 20f, end.anchoredPosition.y);
            }
            if (_lb_load_progress != null)
            {
                _lb_load_progress.text = label ?? $"loading......{Mathf.RoundToInt(p * 100f)}%";
            }
        }
    }
}
