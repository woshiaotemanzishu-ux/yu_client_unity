using System.Collections;
using System.Collections.Generic;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 功能图标集合框(对标老客户端 MainUIIconBoxView.ts):点某「聚合/更多」图标时弹出的小框,
    /// 内放一组功能/活动图标(ActivityIcon),按每行 6 个网格铺,带箭头指向 + 计时自动关闭。
    ///
    /// 降级:图标数据源(Config.ConfigBoxIcon / CommonManager.GetFunctionIconCfg / ActivityIconManager /
    /// CustomActivityModel 的开放判定)与定位锚点(open 传入的 pos/offset/direction)均未移植 →
    /// 列表暂空 + 模板隐藏 + 日志「待对接 功能图标集数据」;箭头/整体定位走预制体默认,不在代码摆。
    /// 保留计时自动关闭(对标 close_time=10)。事件驱动弹层(点聚合图标开),默认关闭、不进 FirstPass。
    /// 数据接上后 RefreshIcons(真实 icon_type 列表),克隆 ActivityIcon 网格铺。
    /// </summary>
    public sealed class MainUIIconBoxView : MainUIIconBoxViewBind
    {
        /// <summary>每行图标数(对标老端 i%6 / floor(i/6))。</summary>
        [SerializeField] private int _columns = 6;
        /// <summary>图标格宽/高(像素;对标 ActivityIcon.WIDTH/HEIGHT,具体值预制体里调)。</summary>
        [SerializeField] private float _cellWidth = 90f;
        [SerializeField] private float _cellHeight = 90f;
        /// <summary>计时自动关闭秒数(对标老端 close_time=10)。</summary>
        [SerializeField] private float _autoCloseSeconds = 10f;

        private readonly List<ActivityIcon> _icons = new List<ActivityIcon>();
        private Coroutine _close;

        protected override void OnInit()
        {
            if (_tpl_ActivityIcon != null) _tpl_ActivityIcon.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            // 老端 open 传 icon_type/pos/offset/direction → GetOpenAndDeleteIcons 算可显图标。数据源未移植 → 空。
            RefreshIcons(args as IList<string>);
            StartAutoClose();
        }

        protected override void OnHide() => StopAutoClose();
        protected override void OnDispose() => StopAutoClose();

        /// <summary>按 icon_type 列表克隆 ActivityIcon 网格铺(对标 RefreshIcon);null/空=清空+日志。</summary>
        public void RefreshIcons(IList<string> iconTypes)
        {
            int count = iconTypes?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                ActivityIcon icon = GetOrCreateIcon(i);
                if (icon == null) continue;
                icon.Show();
                icon.SetIconType(iconTypes[i]);
                SetIconGridPos(icon, i);
            }
            for (int i = count; i < _icons.Count; i++)
            {
                if (_icons[i] != null) _icons[i].gameObject.SetActive(false);
            }

            if (count == 0)
            {
                GameLog.Info("MainUI", "功能图标集为空 → 待对接 功能图标集数据(Config.ConfigBoxIcon / ActivityIconManager)");
            }
        }

        private ActivityIcon GetOrCreateIcon(int index)
        {
            while (_icons.Count <= index) _icons.Add(null);
            if (_icons[index] != null) return _icons[index];

            if (_tpl_ActivityIcon == null || icon_con == null)
            {
                GameLog.Error("MainUI", "MainUIIconBoxView 缺 _tpl_ActivityIcon 或 icon_con");
                return null;
            }

            GameObject go = Instantiate(_tpl_ActivityIcon, icon_con);
            go.SetActive(true);

            ActivityIcon icon = go.GetComponent<ActivityIcon>();
            if (icon == null)
            {
                GameLog.Error("MainUI", "_tpl_ActivityIcon 缺 ActivityIcon 组件(回填?)");
                Destroy(go);
                return null;
            }

            _icons[index] = icon;
            return icon;
        }

        private void SetIconGridPos(ActivityIcon icon, int index)
        {
            int col = _columns > 0 ? index % _columns : index;
            int row = _columns > 0 ? index / _columns : 0;
            RectTransform rt = (RectTransform)icon.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(col * _cellWidth, -(row * _cellHeight));
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
}
