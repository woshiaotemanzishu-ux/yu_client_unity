using Shenxiao.Framework.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 资源加载页(重构版,自包含独立 prefab)——取代老端碎片化的 LoginLoadingView。
    ///
    /// 结构(对标老 LoginLoadingViewSkin.scene + 截图):全屏龙纹立绘背景 + 左上 version 文本 +
    /// 底部进度条(条底图 + Filled 横向填充前景 + 进度端标) + 进度/状态文案 + 首次加载提示。
    /// prefab 由 LoadingCreator 纯代码建树生成并回填下列 public 引用。
    ///
    /// 本类只做:① 数据绑定(进度/文案)② 功能性状态切换(进度端标显隐+位置)。不写颜色/字号/尺寸样式。
    /// 进度推进 / 关闭时机 / 倒计时 / 网络锁 / runner 每帧 Update / 防沉迷小窗 / 3D 切换 等都是
    /// 【真·运行时】,这里不实现,由外部驱动 SetProgress(对标老端 OnChange 的数据部分)。
    ///
    /// 不调用任何 LoginFlow 方法(加载页只被外部喂进度)。
    /// 暴露给外部:
    ///   public void SetProgress(float progress01, string label = null)  // 设填充 + 文案 + 进度端标
    /// </summary>
    [UIView("prefabs/ui/login/loadingview")]
    public sealed class LoadingView : BaseView
    {
        // 老端字面量(LoginLoadingView.ts):front_img_width=635(满宽),进度端标贴边修正阈值/偏移。
        // 注意:老端进度端标是「左边缘 pivot」,55 = 端标半宽,maskWidth-55 让其【中心】落在填充尖端。
        // Unity 端标已用【中心】pivot(0.5),故偏移应为 0——x=maskWidth 即端标中心贴在 BarFront 最右填充边。
        // 低进度时把 x 夹到 NEAR_X 防止端标飘到条左侧;为与线性段连续,NEAR_X = NEAR_THRESHOLD - OFFSET。
        private const float FULL_WIDTH = 635f;
        private const float PROGRESS_END_OFFSET = 0f;
        private const float PROGRESS_END_NEAR_THRESHOLD = 35f;
        private const float PROGRESS_END_NEAR_X = PROGRESS_END_NEAR_THRESHOLD - PROGRESS_END_OFFSET;

        [Header("背景 / 文本")]
        public Image bg;                       // 全屏背景立绘
        public TextMeshProUGUI versionLabel;   // 左上 version 文本

        [Header("进度条")]
        public Image progressBack;             // 进度条底图
        public Image progressFront;            // 进度条前景(type=Filled, Horizontal)
        public Image progressEnd;              // 进度端标(随进度移动)
        public TextMeshProUGUI progressLabel;  // 进度文案 "loading......N%" / 状态文案
        public TextMeshProUGUI firstLoadLabel; // 首次加载提示(静态文案)

        protected override void OnShow(object args)
        {
            SetProgress(0f);
        }

        // ---------------------------------------------------------------- 数据绑定(外部驱动)

        /// <summary>
        /// 进度驱动(对标老端 OnChange 的数据部分):progress01 = 0~1。
        /// 设前景 fillAmount、移动进度端标、刷新文案。
        /// label 不传时显示 "loading......N%"(老端 _lb_load_progress 文案,N=progress*100 两位小数)。
        /// </summary>
        public void SetProgress(float progress01, string label = null)
        {
            float p = Mathf.Clamp01(progress01);

            // 填充类型/方向由 Creator 建树期设好(Image.Type.Filled/Horizontal/Left),这里只驱动数据。
            if (progressFront != null) progressFront.fillAmount = p;

            // 老端按裁剪宽度(maskWidth = front_img_width * p)定位进度端标。
            float maskWidth = FULL_WIDTH * p;
            if (progressEnd != null)
            {
                progressEnd.gameObject.SetActive(maskWidth > 0f);
                RectTransform end = progressEnd.rectTransform;
                float x = maskWidth < PROGRESS_END_NEAR_THRESHOLD
                    ? PROGRESS_END_NEAR_X
                    : maskWidth - PROGRESS_END_OFFSET;
                end.anchoredPosition = new Vector2(x, end.anchoredPosition.y);
            }

            if (progressLabel != null)
            {
                // 老端 OnChange:loading......{(maskWidth/front_img_width*100).toFixed(2)}%
                float pro = p * 100f;
                progressLabel.text = label ?? $"loading......{pro:0.00}%";
            }
        }
    }
}
