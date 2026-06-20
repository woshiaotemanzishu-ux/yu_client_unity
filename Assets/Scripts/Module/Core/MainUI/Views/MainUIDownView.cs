using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 底部经验条/功能图标条(对标老客户端 MainUIDownView.ts:LoadSuccess + RefreshExpWithoutLevelUp)。
    /// 只还原源码支持的首屏静态态与经验刷新:
    /// - _img_bg 换底图 uizjmv3_001(对标 LoadSuccess 的 SetTexture("mainUI","uizjmv3_001"))。
    /// - 经验条宽度按 Exp/ExpLim 比例填充(对标 PlayAnim 的 width = max_len * persent,这里去掉补间直接取终值);
    ///   老客户端 max_len 明确写死为 722,转换产物初始宽度不是运行时满宽,这里按旧端常量走。
    /// - _lb_exp 文案 "exp / exp_lim"(对标 onComleted 的 tb.exp + " / " + tb.exp_lim);ExpLim<=0 时 persent=0、
    ///   文案 "0 / 0"(对标 RefreshExpWithoutLevelUp 的 exp_lim==0 → persent=0 分支)。
    /// 数据只读 RoleModel(唯一真相源),监听 EVT_ROLE_INFO_UPDATE 刷新(对标老客户端 EXP_CHANGE_WITHOUT_ANIMATION)。
    /// 与 MainUITopView 一致用 OnDestroy 兜底注销:模块释放不走 ViewManager,只靠 OnDispose 会漏注销。
    /// 翻面红点 _img_red 与经验特效盒 _box_exp_effect:GetMainFuncRedState/AddUIEffect 依赖未移植模块,先隐藏。
    /// 功能图标条(对标 UpdateIconItem):按 MainUIModel.Main_Func_Icons 两行 + ConfigFuncOpenCondition 开放判定
    /// (FuncOpenConfig)铺设,翻面按钮在等级 ≥65 时循环 show_type;图标的点击跳转(SwitchView)依赖各功能
    /// 模块,本期不接,只还原"显示哪些图标 + 翻面切换"。
    /// </summary>
    public sealed class MainUIDownView : MainUIDownViewBind
    {
        // 老客户端 MainUIDownView.ts 明确写死 max_len = 722,经验条目标宽度按它计算。
        private const float EXP_BAR_MAX_WIDTH = 722f;
        private const float FUNC_ICON_GAP = 105f;
        // 对标 MainUIModel.Turn_Open_lv:到此等级才能翻面切换第二行功能。
        private const int TURN_OPEN_LV = 65;

        /// <summary>功能图标条目:res=图标名(mainUI atlas);openView=功能开放判定的 view 类名(null=恒开)。</summary>
        private readonly struct FuncIcon
        {
            public readonly string Res;
            public readonly string OpenView;
            public FuncIcon(string res, string openView)
            {
                Res = res;
                OpenView = openView;
            }
        }

        // 对标 MainUIModel.Main_Func_Icons:两行(show_type 0/1)。func→view 映射对标 GetMainFuncOpenCond。
        private static readonly FuncIcon[][] FuncIconLines =
        {
            new[]
            {
                new FuncIcon("role", null),
                new FuncIcon("bag", null),
                new FuncIcon("pet", "MountPetView"),
                new FuncIcon("equip", "EquipView"),
                new FuncIcon("treasure", "SecretTreasureMainView"),
            },
            new[]
            {
                new FuncIcon("red", "RedEnterView"),
                new FuncIcon("love", "MarriageBaseView"),
                new FuncIcon("guild", "GuildJoinBaseView"),
                new FuncIcon("composite", "CompositeView"),
                new FuncIcon("232", "GodBefallMainView"),
            },
        };

        private readonly List<MainFuncIconItemBind> _funcIconItems = new List<MainFuncIconItemBind>();
        private int _showType;

        protected override void OnInit()
        {
            // LoadSuccess uses ResManager.SetTexture, whose old-client path rule maps /texture/ to /other/.
            _ = ResManager.SetLayaTextureAsync(_img_bg, GameResPath.GetIcon("mainUI", "uizjmv3_001"), nativeSize: false);

            HideUnbackedIndicators();
            HideTemplates();

            // 翻面按钮点击(对标 _gp_turn 的 turn_btn_fun);_gp_turn 是 Box 无 Graphic,点在可见的 _img_turn 上。
            if (_img_turn != null)
            {
                _img_turn.raycastTarget = true;
                UIUtil.AddClick(_img_turn, OnClickTurn);
            }

            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, OnTaskListUpdate);

            RefreshExp();
            _ = RefreshFuncIconsAsync();
        }

        protected override void OnShow(object args)
        {
            // open_callback → RefreshExpWithoutLevelUp()
            RefreshExp();
        }

        protected override void OnDispose()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_TASK_LIST_UPDATED, OnTaskListUpdate);
        }

        private void OnRoleInfoUpdate()
        {
            RefreshExp();
            // 等级变化可能解锁图标/翻面(对标 CHANGE_LEVEL → UpdateTurnState + 图标开放条件)。
            _ = RefreshFuncIconsAsync();
        }

        private void OnTaskListUpdate()
        {
            // 最新完成任务变化可能解锁图标(对标 UPDATE_NEWEST_TASK_ID_NOT_DELAY → TryRefreshItem)。
            _ = RefreshFuncIconsAsync();
        }

        /// <summary>
        /// 对标 RefreshExpWithoutLevelUp + PlayAnim 的终值:按 Exp/ExpLim 算比例,刷经验条宽度与文案。
        /// 去掉补间动画(无源补间器移植),直接落到目标态;升级/特效闪光等动画态待 MainUIModel 移植后补。
        /// </summary>
        private void RefreshExp()
        {
            RoleModel m = RoleModel.Instance;
            long exp = m.Exp;
            long expLim = m.ExpLim;

            // 对标 RefreshExpWithoutLevelUp: exp 截顶到 exp_lim;exp_lim==0 → persent=0
            if (expLim <= 0)
            {
                SetExpBarWidth(0f);
                _lb_exp.text = "0 / 0";
                return;
            }

            if (exp > expLim)
            {
                exp = expLim;
            }

            // 对标旧端: let a = exp / exp_lim * 100; persent = Math.floor(a) / 100
            float persent = Mathf.Floor((float)((double)exp / expLim * 100.0)) / 100f;
            SetExpBarWidth(EXP_BAR_MAX_WIDTH * persent);
            // 对标 onComleted: this._lb_exp.text = tb.exp + " / " + tb.exp_lim
            _lb_exp.text = exp + " / " + expLim;
        }

        /// <summary>设经验条宽度(对标老客户端 this._img_exp.width = ...),只改宽不动高。</summary>
        private void SetExpBarWidth(float width)
        {
            if (width < 0f)
            {
                width = 0f;
            }
            else if (width > EXP_BAR_MAX_WIDTH)
            {
                width = EXP_BAR_MAX_WIDTH;
            }
            Vector2 size = _img_exp.rectTransform.sizeDelta;
            size.x = width;
            _img_exp.rectTransform.sizeDelta = size;
        }

        /// <summary>
        /// MainUIModel/特效未移植:翻面红点与经验特效盒先隐藏(老客户端由 turn_red_dot 与 AddUIEffect 驱动可见性)。
        /// 不造假数据/特效,沿用既有 View 的 gameObject.SetActive(false) 收法。
        /// </summary>
        private void HideUnbackedIndicators()
        {
            _img_red.gameObject.SetActive(false);
            _box_exp_effect.gameObject.SetActive(false);
        }

        private void HideTemplates()
        {
            if (_tpl_MainFuncIconItem != null)
            {
                _tpl_MainFuncIconItem.SetActive(false);
            }
        }

        /// <summary>
        /// 先确保功能开放表就绪,再按当前 show_type 行 + 开放条件铺图标(对标 UpdateView → UpdateIconItem)。
        /// 表是异步加载;await 后做存活检查(模块释放/对象销毁则不再操作)。
        /// </summary>
        private async Task RefreshFuncIconsAsync()
        {
            await FuncOpenConfig.EnsureLoaded();
            if (this == null) return; // view destroyed during await

            UpdateTurnState();
            BuildFuncIcons();
        }

        /// <summary>对标 UpdateIconItem:遍历当前行,GetMainFuncOpenCond 过的才显示并按 105 间距排布。</summary>
        private void BuildFuncIcons()
        {
            FuncIcon[] line = FuncIconLines[_showType];
            int shown = 0;
            for (int i = 0; i < line.Length; i++)
            {
                FuncIcon fi = line[i];
                // 恒开图标 openView==null;其余查功能开放表(对标 GetMainFuncOpenCond)。
                if (fi.OpenView != null && !FuncOpenConfig.CheckFuncOpenState(fi.OpenView)) continue;

                MainFuncIconItemBind item = GetOrCreateFuncIconItem(shown);
                if (item == null) continue;

                item.gameObject.SetActive(true);
                SetFuncIconPosition(item, shown * FUNC_ICON_GAP, 0f);

                // 走 MainFuncIconItem.SetData:它填图标 + 隐红点 + 一次性绑定点击(_clickBound 守卫,
                // 防 BuildFuncIcons 多次调用叠加监听),点击经 MainUIRouter.Open(func) 打开对应面板。
                // Func 用图标 res 作路由 key(如 "bag"),已注册模块即可打开,未注册的路由打日志降级。
                MainFuncIconItem view = item as MainFuncIconItem;
                if (view != null)
                {
                    view.SetData(new FuncIconData { Res = fi.Res, Func = fi.Res });
                }
                else
                {
                    // 兜底:模板未挂 View 子类(理论上回填后不会发生)→ 只显示图标,不接点击。
                    if (item._img_red != null) item._img_red.gameObject.SetActive(false);
                    _ = ResManager.SetImageAsync(item._img_icon, GameResPath.GetIcon("mainUI", fi.Res), nativeSize: false);
                }
                shown++;
            }

            // 多余复用项隐藏(对标 UpdateIconItem 尾部 SetVisible(false))。
            for (int i = shown; i < _funcIconItems.Count; i++)
            {
                if (_funcIconItems[i] != null)
                {
                    _funcIconItems[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>对标 MainUIModel.GetTurnState:角色等级达到翻面等级才能翻面。</summary>
        private static bool GetTurnState()
        {
            return RoleModel.Instance.Level >= TURN_OPEN_LV;
        }

        /// <summary>对标 turn_btn_fun:可翻面时 show_type 在 0/1 间循环,刷新按钮态与图标行。</summary>
        private void OnClickTurn()
        {
            if (!GetTurnState()) return;

            _showType++;
            if (_showType > 1) _showType = 0;
            UpdateTurnState();
            BuildFuncIcons();
        }

        /// <summary>
        /// 对标 UpdateTurnState:可翻面非灰,图标按 show_type 取 uizjmv3_015/016;不可翻面置灰 + 015。
        /// 老端用 _img_turn.gray;Unity 无灰度材质,用颜色压暗近似(不伪造,仅视觉降级)。
        /// </summary>
        private void UpdateTurnState()
        {
            if (_img_turn == null) return;

            bool open = GetTurnState();
            string res = open && _showType != 0 ? "uizjmv3_016" : "uizjmv3_015";
            _ = ResManager.SetImageAsync(_img_turn, GameResPath.GetIcon("mainUI", res), nativeSize: false);
            _img_turn.color = open ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        private MainFuncIconItemBind GetOrCreateFuncIconItem(int index)
        {
            while (_funcIconItems.Count <= index)
            {
                _funcIconItems.Add(null);
            }

            MainFuncIconItemBind item = _funcIconItems[index];
            if (item != null) return item;

            if (_tpl_MainFuncIconItem == null || _gp_icon_con == null)
            {
                GameLog.Error("MainUI", "MainUIDownView missing MainFuncIconItem template or _gp_icon_con");
                return null;
            }

            GameObject go = Instantiate(_tpl_MainFuncIconItem, _gp_icon_con);
            go.SetActive(true);

            item = go.GetComponent<MainFuncIconItemBind>();
            if (item == null)
            {
                GameLog.Error("MainUI", "MainFuncIconItem template missing bind component");
                Destroy(go);
                return null;
            }

            _funcIconItems[index] = item;
            return item;
        }

        private static void SetFuncIconPosition(MainFuncIconItemBind item, float x, float y)
        {
            RectTransform rt = (RectTransform)item.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
        }
    }
}
