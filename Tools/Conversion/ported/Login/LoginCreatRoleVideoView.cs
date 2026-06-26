using Shenxiao.Generated.UI.Login;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 建角色开场视频界面(对标老客户端 login/LoginCreatRoleVideoView.ts)。
    ///
    /// Phase 2:结构(全屏背景 _img_bg / 视频容器 _box_video / 跳过按钮 _box_skip)已烤进 prefab,
    /// 这里【只绑数据/接交互】——不再运行时 new 节点、不再摆位置:
    ///   - 老端的 ResizeBg(把 _img_bg 拉满 stage) 是运行时布局 → 已删,背景尺寸交给 prefab/锚点。
    ///   - 老端的 CreatVideo(new GameVideo + InitVideo + Play) 是真·运行时视频系统 → Unity 尚未移植
    ///     GameVideo/VideoPlayer 基础设施,这里降级为 TODO(对标既有 live 版),不在此处臆造播放逻辑。
    /// 交互:点 _box_skip(对标老端 _box_skip 点击 → PlayEnded)→ 关闭本页。
    /// 事件驱动,默认关闭。
    /// </summary>
    public sealed class LoginCreatRoleVideoView : LoginCreatRoleVideoViewBind
    {
        protected override void OnInit()
        {
            // 交互在 OnShow 重绑,这里不预绑,避免与 OnShow 的 ClearClicks/AddClick 重复。
        }

        protected override void OnShow(object args)
        {
            // 对标老端 CreatVideo:真·运行时视频播放。Unity 暂无 GameVideo/VideoPlayer 基建,降级。
            GameLog.Info("Login", "建角色开场视频 → 待对接视频播放(GameVideo 未移植);现可点跳过关闭");

            // 跳过按钮:容器字段没绑上(prefab 里 _box_skip 为 null)→ transform.Find 兜底。
            Image skip = SkipHit();
            if (skip != null)
            {
                skip.raycastTarget = true;
                UIUtil.ClearClicks(skip); // OnShow 每次重绑前先清,防点击监听叠加(对标样板)。
                UIUtil.AddClick(skip, OnClickSkip);
            }
            else
            {
                GameLog.Warn("Login", "建角色开场视频缺 _box_skip 命中体(烤制 prefab 结构异常?)");
            }
        }

        protected override void OnHide()
        {
        }

        protected override void OnDispose()
        {
        }

        /// <summary>对标老端 PlayEnded:结束开场视频 → 关闭本页(进 LoginLoadingView 由上层流程接管)。</summary>
        private void OnClickSkip()
        {
            Hide();
        }

        // ——— 跳过按钮命中体:_box_skip 容器字段没绑上时按名兜底(节点名与老端一致)———
        // 容器自身无 Graphic,优先取容器上的图(_box_skip/_img_skip),没有再退到容器自身。
        private Image SkipHit()
        {
            Transform box = _box_skip != null
                ? _box_skip
                : transform.Find("_box_skip");
            if (box == null) return _img_skip; // 退到已绑的跳过图标作命中体

            Image self = box.GetComponent<Image>();
            if (self != null) return self;
            Image inner = box.GetComponentInChildren<Image>(true);
            return inner != null ? inner : _img_skip;
        }
    }
}
