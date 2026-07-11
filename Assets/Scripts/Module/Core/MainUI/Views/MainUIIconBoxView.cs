using System.Collections;
using System.Collections.Generic;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 功能图标集合框(对标老客户端 MainUIIconBoxView.ts):点活动栏上的「收纳盒入口」图标(is_box,如 100 福利大厅 /
    /// 101 寻宝)时弹出的小框,内放该盒收纳的一组成员图标(ActivityIcon),按每行 6 个网格铺,计时自动关闭。
    ///
    /// 数据源已接:ConfigBoxIcon(MainUIConfigs.BoxIconsForHolder)+ ActivityIconManager.NeedInputBox/GetBoxIconInfo
    /// 决定每个成员显不显(对标老端 GetOpenAndDeleteIcons/CheckOpenIcon)。
    /// 降级:CustomActivityModel.IsActIcon/GetOpenState 与 114 的 ClientAddVipService 白名单老端有、Unity 未移植 →
    /// 自管理成员仅凭「是否已被折进盒(GetBoxIconInfo!=null)」判定;箭头/内部结构走预制体默认。
    /// </summary>
    public sealed class MainUIIconBoxView : MainUIIconBoxViewBind
    {
        /// <summary>点开某盒时传入的参数(icon_type + 该盒入口图标的世界坐标,用于把弹窗锚到图标处)。</summary>
        public sealed class OpenArgs
        {
            public string IconType;
            public Vector3 WorldPos;
        }

        /// <summary>每行图标数(对标老端 i%6 / floor(i/6))。</summary>
        [SerializeField] private int _columns = 6;
        /// <summary>计时自动关闭秒数(对标老端 close_time=10)。</summary>
        [SerializeField] private float _autoCloseSeconds = 10f;

        private readonly List<ActivityIcon> _icons = new List<ActivityIcon>();
        private Coroutine _close;
        private string _openedIconType;
        private Vector2 _conBase;          // icon_con 初始局部位(对标老端 con_x_/con_y_),bg 尺寸计算基准
        private bool _conBaseCaptured;

        // ---------- 运行时打开管线(缓存单例弹窗) ----------
        private static GameObject _rootGo;
        private static MainUIIconBoxView _instance;
        private static bool _loading;

        /// <summary>点盒入口图标时调用:按需实例化弹窗(缓存复用),同一个盒再点则关闭(toggle),否则刷新为该盒内容。</summary>
        public static async void OpenFor(string iconType, Vector3 worldPos)
        {
            if (string.IsNullOrEmpty(iconType)) return;

            if (_instance != null && _rootGo != null)
            {
                // 同一个盒且正开着 → 关(对标老端先 CLOSE_VIEW 再 OPEN_VIEW 的 toggle)。
                if (_instance.IsShown && _instance._openedIconType == iconType) { _instance.Hide(); return; }
                _instance.Show(new OpenArgs { IconType = iconType, WorldPos = worldPos });
                return;
            }

            if (_loading) return;
            _loading = true;
            string key = GameResPath.GetUIPrefab("mainUI", "MainUIIconBoxView");
            GameObject go = await ResManager.InstantiateAsync(key, ViewManager.GetLayer(UILayer.Window));
            _loading = false;

            if (go == null)
            {
                GameLog.Error("MainUI", "MainUIIconBoxView 预制加载失败: {0}", key);
                return;
            }
            go.name = "MainUIIconBoxView";
            _rootGo = go;
            _instance = go.GetComponent<MainUIIconBoxView>();
            if (_instance == null)
            {
                GameLog.Error("MainUI", "MainUIIconBoxView 预制缺 MainUIIconBoxView 组件(重跑 mainUI 回填)");
                ResManager.ReleaseInstance(go);
                _rootGo = null;
                return;
            }
            _instance.Show(new OpenArgs { IconType = iconType, WorldPos = worldPos });
        }

        internal static void ResetInstance()
        {
            if (_rootGo != null) ResManager.ReleaseInstance(_rootGo);
            _rootGo = null;
            _instance = null;
            _loading = false;
        }

        protected override void OnInit()
        {
            if (_tpl_ActivityIcon != null) _tpl_ActivityIcon.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            OpenArgs open = args as OpenArgs;
            _openedIconType = open != null ? open.IconType : null;
            CaptureConBase();

            List<string> iconTypes = GetOpenIconTypes(_openedIconType);
            if (iconTypes.Count == 0)
            {
                // 无可显成员:对标老端 RefreshSizeAndPos num<=0 → Close。
                GameLog.Info("MainUI", "收纳盒 [{0}] 无可显成员 → 关闭弹窗", _openedIconType);
                Hide();
                return;
            }

            RefreshIcons(iconTypes);
            RefreshBoxSize(iconTypes.Count);
            SetPos(open != null ? (Vector3?)open.WorldPos : null);
            StartAutoClose();
        }

        protected override void OnHide() => StopAutoClose();
        protected override void OnDispose() => StopAutoClose();

        /// <summary>该盒(attach_icon_type=boxIconType)里当前该显示的成员 icon_type,按 sort_order 升序(对标老端 GetOpenAndDeleteIcons)。</summary>
        private List<string> GetOpenIconTypes(string boxIconType)
        {
            var open = new List<MainUIConfigs.BoxIconCfg>();
            if (!string.IsNullOrEmpty(boxIconType))
            {
                foreach (MainUIConfigs.BoxIconCfg member in MainUIConfigs.BoxIconsForHolder(boxIconType))
                {
                    if (CheckOpenIcon(member)) open.Add(member);
                }
            }
            open.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));

            var list = new List<string>(open.Count);
            foreach (MainUIConfigs.BoxIconCfg m in open) list.Add(m.IconType);
            return list;
        }

        /// <summary>成员是否在盒弹窗里显示(对标老端 CheckOpenIcon)。</summary>
        private bool CheckOpenIcon(MainUIConfigs.BoxIconCfg boxCfg)
        {
            if (boxCfg == null) return false;
            ActivityIconManager mgr = ActivityIconManager.Instance;
            bool canAdd = mgr.NeedInputBox(boxCfg);
            // controll_by_own_fun 成员:由其功能模块决定,凭「是否已被折进盒」判定(对标老端 getBoxIconInfo!=null)。
            if (boxCfg.ControllByOwnFun) canAdd = mgr.GetBoxIconInfo(boxCfg.IconType) != null;
            // 降级:老端此处还查 CustomActivityModel.IsActIcon/GetOpenState + 114 ClientAddVipService,Unity 未移植 → 略。
            return canAdd;
        }

        /// <summary>按 icon_type 列表克隆 ActivityIcon 网格铺(对标 RefreshIcon);多余的隐藏。</summary>
        public void RefreshIcons(IList<string> iconTypes)
        {
            int count = iconTypes != null ? iconTypes.Count : 0;
            for (int i = 0; i < count; i++)
            {
                ActivityIcon icon = GetOrCreateIcon(i);
                if (icon == null) continue;
                icon.Show();
                icon.SetIconType(iconTypes[i]);
                // 网格排布归 prefab:icon_con 挂 GridLayoutGroup(固定 6 列 72×72,见 HudOverlayPopupsCreator),
                // 克隆体按 sibling 顺序自动排,原 i%6/floor(i/6) 代码排版已删。
            }
            for (int i = count; i < _icons.Count; i++)
            {
                if (_icons[i] != null) _icons[i].gameObject.SetActive(false);
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

        // icon_con 初始局部位记为基准(对标老端 LoadSuccess 的 con_x_/con_y_),供 bg 尺寸计算。
        private void CaptureConBase()
        {
            if (_conBaseCaptured || icon_con == null) return;
            _conBase = icon_con.anchoredPosition;
            _conBaseCaptured = true;
        }

        // bg 尺寸随成员数变化(对标老端 RefreshSizeAndPos:>=6 满 6 列、否则 num 列,行数 ceil(num/6))。
        private void RefreshBoxSize(int num)
        {
            if (bg == null || num <= 0) return;
            float conX = _conBase.x;
            float conY = _conBase.y;
            int cols = num >= _columns ? _columns : num;
            int rows = _columns > 0 ? Mathf.CeilToInt((float)num / _columns) : 1;
            float width = cols * ActivityIcon.WIDTH + conX * 2f;
            float height = rows * ActivityIcon.HEIGHT + conY;
            RectTransform bgRt = bg.rectTransform;
            bgRt.sizeDelta = new Vector2(width, Mathf.Abs(height));
        }

        // 把弹窗根锚到被点盒入口图标处(对标老端 SetPos);拿不到坐标/画布时走预制体默认位。内部 bg/arrow 相对根的摆放归预制体。
        private void SetPos(Vector3? worldPos)
        {
            if (worldPos == null) return;
            RectTransform root = transform as RectTransform;
            RectTransform parent = root != null ? root.parent as RectTransform : null;
            if (root == null || parent == null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos.Value);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, cam, out Vector2 local))
            {
                root.anchoredPosition = local;
            }
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
