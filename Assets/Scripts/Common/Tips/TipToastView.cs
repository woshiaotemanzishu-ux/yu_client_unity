using System.Collections.Generic;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Common.Tips
{
    /// <summary>
    /// 通用飘字提示视图(对标老端 Message.show → SysInfoMiniMgr → MessageItem 的 Type One 居中飘字)。
    ///
    /// prefab 由 TipToastCreator 纯代码建树生成:全屏透明根 + ToastTemplate 模板(默认隐,底图+文案,
    /// 样式在 prefab 里手调)。运行时按模板克隆逐条播放,模板自身的 anchoredPosition 即出生点。
    /// 注意:模板在运行时会被强制隐藏——编辑器里为调样式把它临时激活存盘也没关系。
    ///
    /// 动画三段状态机(参数可在 prefab 上调):
    ///   Born  缩放 bornScale→1 + 淡入,线性 bornDuration;同一时刻只允许一条在 Born,其余排队
    ///   Alive 停留 lifeSec,期间持续上浮 riseSpeed;每来一条新提示,在场各条再额外上顶 pushDistance
    ///   Dead  继续上浮并淡出 fadeOutDuration 后移除
    /// 入口统一走 TipsManager.Toast(string),不要直接 Open 本视图。
    /// </summary>
    [UIView("prefabs/ui/common/tiptoastview")]
    public sealed class TipToastView : BaseView
    {
        public override UILayer Layer => UILayer.Tip;

        [Header("模板(样式在 prefab 里手调;其 anchoredPosition 即出生点)")]
        public TipToastItem template;

        [Header("动画参数")]
        public float bornDuration = 0.3f;     // 出现:缩放+淡入时长(老端 once_move_time)
        public float bornScale = 0.3f;        // 出现起始缩放(老端 default_scale)
        public float lifeSec = 2f;            // 出现后到开始淡出的停留时长(老端 life_cycle)
        public float riseSpeed = 45f;         // 存活期持续上浮速度(px/s)
        public float pushDistance = 30f;      // 新条入场时旧条额外上顶距离(老端 once_move_dis)
        public float pushSpeed = 100f;        // 上顶消化速度(px/s,老端 30px/0.3s)
        public float fadeOutDuration = 0.35f; // 消失淡出时长(淡出期间仍上浮)
        public int maxVisible = 5;            // 同屏上限,超出顶掉最旧

        [Header("宽度自适应(功能性:底图宽=文本宽+padding,最小 minWidth)")]
        public float minWidth = 222f;         // 老端最小 222
        public float widthPadding = 60f;      // 老端 +60 内边距

        private const int MaxQueue = 200;     // 排队缓存上限(老端 max_cache_count),超出丢最旧

        private enum Phase { Born, Alive, Dead }

        private sealed class Live
        {
            public RectTransform rt;
            public CanvasGroup cg;
            public Phase phase;
            public float t;          // 当前段已历时
            public float pushRemain; // 待消化的上顶距离
        }

        private readonly Queue<string> _pending = new Queue<string>();
        private readonly List<Live> _live = new List<Live>();

        protected override void OnInit()
        {
            // 模板强制隐藏:在编辑器里调样式常把模板临时激活并连带存盘,激活态模板会以
            // 静态节点常驻(不动画、不消失、盖住真克隆条),运行时兜底关掉。
            if (template != null) template.gameObject.SetActive(false);
        }

        /// <summary>入队一条提示(富文本用 TMP 标签,如 &lt;color=#88ff43&gt;)。排队节流对标老端:同刻仅一条在 Born。</summary>
        public void Enqueue(string text)
        {
            if (template == null)
            {
                GameLog.Error("Tip", "TipToastView.template 未绑定(重新生成 prefab):{0}", text);
                return;
            }
            while (_pending.Count >= MaxQueue) _pending.Dequeue();
            _pending.Enqueue(text);
            TryPump();
        }

        /// <summary>队列放行:最新一条不在 Born(或场上没条目)才放下一条(对标 GetMsgItemState/TryShowMiniMsg)。</summary>
        private void TryPump()
        {
            while (_pending.Count > 0 && (_live.Count == 0 || _live[_live.Count - 1].phase != Phase.Born))
            {
                Spawn(_pending.Dequeue());
            }
        }

        private void Spawn(string text)
        {
            // 同屏上限:顶掉最旧(老端靠对象池大小软限制,这里显式移除)
            while (_live.Count >= Mathf.Max(1, maxVisible)) Remove(0);

            // 在场各条额外上顶一档(对标 ShowMiniMsg 里全员 MoveState=true)
            foreach (Live l in _live) l.pushRemain += pushDistance;

            TipToastItem item = Instantiate(template, template.transform.parent);
            item.gameObject.SetActive(true);
            var rt = (RectTransform)item.transform;
            rt.anchoredPosition = ((RectTransform)template.transform).anchoredPosition;

            CanvasGroup cg = item.canvasGroup != null ? item.canvasGroup : item.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            rt.localScale = Vector3.one * bornScale;
            _live.Add(new Live { rt = rt, cg = cg, phase = Phase.Born, t = 0f, pushRemain = 0f });

            if (item.label != null)
            {
                item.label.text = text;
                // 宽度自适应(功能性,对标老端 SetContent):底图宽=文本宽+padding,最小 minWidth
                float w = Mathf.Max(minWidth, item.label.preferredWidth + widthPadding);
                rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
            }
        }

        private void Update()
        {
            if (_live.Count == 0) return;
            float dt = Time.deltaTime;
            bool bornFinished = false;

            for (int i = _live.Count - 1; i >= 0; i--)
            {
                Live l = _live[i];
                if (l.rt == null) { _live.RemoveAt(i); continue; }
                l.t += dt;

                // 上浮 = 持续漂移(Born 段原地弹出不浮) + 被新条顶推的额外距离
                if (l.phase != Phase.Born)
                {
                    float move = riseSpeed * dt;
                    if (l.pushRemain > 0f)
                    {
                        float step = Mathf.Min(l.pushRemain, pushSpeed * dt);
                        move += step;
                        l.pushRemain -= step;
                    }
                    if (move > 0f) l.rt.anchoredPosition += new Vector2(0f, move);
                }

                switch (l.phase)
                {
                    case Phase.Born:
                        float k = bornDuration > 0f ? Mathf.Clamp01(l.t / bornDuration) : 1f;
                        l.cg.alpha = k;
                        l.rt.localScale = Vector3.one * Mathf.Lerp(bornScale, 1f, k);
                        if (k >= 1f)
                        {
                            l.phase = Phase.Alive;
                            l.t = 0f;
                            bornFinished = true;   // Born 完成 → 循环外放行下一条
                        }
                        break;

                    case Phase.Alive:
                        if (l.t >= lifeSec)
                        {
                            if (fadeOutDuration > 0f) { l.phase = Phase.Dead; l.t = 0f; }
                            else Remove(i);
                        }
                        break;

                    case Phase.Dead:
                        l.cg.alpha = Mathf.Clamp01(1f - l.t / fadeOutDuration);
                        if (l.t >= fadeOutDuration) Remove(i);
                        break;
                }
            }

            if (bornFinished) TryPump();
        }

        private void Remove(int index)
        {
            Live l = _live[index];
            _live.RemoveAt(index);
            if (l.rt != null) Destroy(l.rt.gameObject);
        }

        protected override void OnHide()
        {
            // 隐藏时清场(排队的也丢弃):提示是瞬时信息,不跨场景补播
            for (int i = _live.Count - 1; i >= 0; i--) Remove(i);
            _pending.Clear();
        }
    }
}
