using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Common.UI3D;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;
using UnityEngine;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 底部经验条/功能图标条(对标老客户端 MainUIDownView.ts:LoadSuccess + RefreshExpWithoutLevelUp)。
    /// 只还原源码支持的首屏静态态与经验刷新:
    /// - _img_bg 底图 uizjmv3_001 已保存在 HudNavBar.prefab,不再运行时换图(原对标 LoadSuccess 的 SetTexture 已删)。
    /// - 经验条按 Exp/ExpLim 比例填充(对标 PlayAnim 的 width = max_len * persent,这里去掉补间直接取终值);
    ///   Prefab 保留固定满宽 Rect,运行时只同步 Image.fillAmount 与特效定位 Slider.value,不改 RectTransform。
    /// - _lb_exp 文案 "exp / exp_lim"(对标 onComleted 的 tb.exp + " / " + tb.exp_lim);ExpLim<=0 时 persent=0、
    ///   文案 "0 / 0"(对标 RefreshExpWithoutLevelUp 的 exp_lim==0 → persent=0 分支)。
    /// 数据只读 RoleModel(唯一真相源),监听 EVT_ROLE_INFO_UPDATE 刷新(对标老客户端 EXP_CHANGE_WITHOUT_ANIMATION)。
    /// 与 MainUITopView 一致用 OnDestroy 兜底注销:模块释放不走 ViewManager,只靠 OnDispose 会漏注销。
    /// 翻面红点 _img_red 与经验特效盒 _box_exp_effect:GetMainFuncRedState/AddUIEffect 依赖未移植模块,先隐藏。
    /// 功能图标条(对标 UpdateIconItem):功能配置(两行图标 / 开放判定 / 翻面等级)集中在 MainUIModel
    /// (对标老端 Main_Func_Icons / GetMainFuncOpenCond / Turn_Open_lv),本 View 只按 show_type 行 + 开放判定
    /// 铺图标并处理翻面;点击经 MainFuncIconItem → MainUIRouter 打开对应功能面板(已注册模块即开,未注册降级)。
    /// </summary>
    public sealed class MainUIDownView : MainUIDownViewBind
    {
        // 功能图标改【槽位式】:105 间距等布局烤在 HudNavBar.prefab 的
        // FuncIconRow 槽位(Slot_*)里,View 只按顺序把图标填进槽,不再算坐标。

        // 功能图标配置(两行图标 / 开放判定 / 翻面等级)集中在 MainUIModel(对标老端 Main_Func_Icons /
        // GetMainFuncOpenCond / Turn_Open_lv),本 View 不再自带硬编码二维数组,只消费模型。

        private readonly List<MainFuncIconItemBind> _funcIconItems = new List<MainFuncIconItemBind>();
        private int _showType;
        // 开着的 BaseWindowSkin 大窗计数(窗开着时功能图标手指让位给页内引导)
        private int _openWindowCount;

        // 经验条闪光特效(对标 MainUIDownView.ts:395 AddUIEffect("ui_expbar", _box_exp_effect, null, 15, null))。
        private UIEffectStage.Handle _expEffect;
        private bool _expEffectAdding;
        private int _expPulseVersion;
        private long _lastExp = long.MinValue;
        private long _lastExpLim = long.MinValue;
        private int _lastLevel = int.MinValue;

        protected override void OnInit()
        {
            // 底图 uizjmv3_001 保存在 HudNavBar.prefab,不再运行时换图。

            HideUnbackedIndicators();
            HideTemplates();
            ClearDesignTimeSampleIcons();

            // 翻面按钮点击(对标 _gp_turn 的 turn_btn_fun);_gp_turn 是 Box 无 Graphic,点在可见的 _img_turn 上。
            if (_img_turn != null)
            {
                _img_turn.raycastTarget = true;
                UIUtil.AddClick(_img_turn, OnClickTurn);
            }

            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On(GlobalEvent.EVT_TASK_LIST_UPDATED, OnTaskListUpdate);
            EventDispatcher.On(GlobalEvent.EVT_TASK_ONE_UPDATED, OnTaskOneUpdate);
            EventDispatcher.On(GlobalEvent.EVT_BASE_WINDOW_OPENED, OnBaseWindowOpened);
            EventDispatcher.On(GlobalEvent.EVT_BASE_WINDOW_CLOSED, OnBaseWindowClosed);

            RefreshExp(true);
            _ = RefreshFuncIconsAsync();
        }

        protected override void OnShow(object args)
        {
            // open_callback → RefreshExpWithoutLevelUp()
            RefreshExp(true);
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
            EventDispatcher.Off(GlobalEvent.EVT_TASK_ONE_UPDATED, OnTaskOneUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_BASE_WINDOW_OPENED, OnBaseWindowOpened);
            EventDispatcher.Off(GlobalEvent.EVT_BASE_WINDOW_CLOSED, OnBaseWindowClosed);
            MainUIGuideManager.Instance.HideMainUiFinger(this);
            _expPulseVersion++;
            if (_box_exp_effect != null) _box_exp_effect.gameObject.SetActive(false);
            if (_expEffect != null)
            {
                _expEffect.Dispose();
                _expEffect = null;
            }
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

        private void OnTaskOneUpdate()
        {
            RefreshIconGuide();
        }

        private void OnBaseWindowOpened()
        {
            _openWindowCount++;
            RefreshIconGuide();
        }

        private void OnBaseWindowClosed()
        {
            _openWindowCount = Mathf.Max(0, _openWindowCount - 1);
            RefreshIconGuide();
        }

        /// <summary>
        /// 主线任务的功能图标手指(对标老端 DoTask 的 SELECT_STORY_TARGET → MainUIDownView 挂 SHOW_FINGER):
        /// 当前主线任务是"开系统面板培养"类(ConfigTaskArrow in_main_ui 配 not_show_in_task_item,箭头归功能图标)
        /// 且未完成、系统窗没开着 → 手指指向对应功能图标;达成后箭头回任务项(领奖指引),此处隐藏。
        /// </summary>
        private void RefreshIconGuide()
        {
            Tasks.TaskModel taskModel = Tasks.TaskModel.Instance;
            Tasks.TaskVo task = taskModel.MainLineTaskVo;
            if (task == null || !taskModel.MainLineTaskNeedShowArrow() || _openWindowCount > 0
                || taskModel.IsAllStepFinish(task.TaskId))
            {
                MainUIGuideManager.Instance.HideMainUiFinger(this);
                return;
            }

            string iconRes = MainUIModel.GetGuideIconRes(task.TaskTipsType);
            Tasks.TaskModel.TaskGuideStep step = taskModel.GetNowGuideCfg(true, task);
            if (iconRes == null || step == null || !step.NotShowInTaskItem)
            {
                MainUIGuideManager.Instance.HideMainUiFinger(this);
                return;
            }

            RectTransform target = FindFuncIconRect(iconRes);
            if (target == null)
            {
                MainUIGuideManager.Instance.HideMainUiFinger(this);
                return;
            }

            var data = new ArrowData
            {
                Content = step.Text,
                Direction = step.Direction,
                CloseTime = step.CloseTime,
                AutoCountdown = step.AutoCountdown,
                NotEffect = step.NotEffect,
                SelectEffectScale = new Vector3(step.EffectScaleX, step.EffectScaleY, step.EffectScaleZ),
                FingerEffectOffset = new Vector2(step.FingerOffsetX, step.FingerOffsetY),
                Offset = new Vector2(step.OffsetX, step.OffsetY),
                Target = target,
            };
            MainUIGuideManager.Instance.ShowMainUiFinger(this, target, data, () => taskModel.DoTask(task));
        }

        /// <summary>按路由键找当前铺出的功能图标(引导手指目标;不在当前行/未开放 → null)。</summary>
        private RectTransform FindFuncIconRect(string res)
        {
            for (int i = 0; i < _funcIconItems.Count; i++)
            {
                MainFuncIconItem item = _funcIconItems[i] as MainFuncIconItem;
                if (item != null && item.gameObject.activeSelf && item.Res == res)
                {
                    return (RectTransform)item.transform;
                }
            }
            return null;
        }

        /// <summary>
        /// 对标 RefreshExpWithoutLevelUp + PlayAnim 的终值:按 Exp/ExpLim 算比例,刷经验条宽度与文案。
        /// 去掉补间动画(无源补间器移植),直接落到目标态;升级/特效闪光等动画态待 MainUIModel 移植后补。
        /// </summary>
        private void RefreshExp(bool forceEffect = false)
        {
            RoleModel m = RoleModel.Instance;
            long exp = m.Exp;
            long expLim = m.ExpLim;
            int level = m.Level;
            bool changed = exp != _lastExp || expLim != _lastExpLim || level != _lastLevel;
            _lastExp = exp;
            _lastExpLim = expLim;
            _lastLevel = level;
            if (changed || forceEffect) _ = PulseExpEffectAsync();

            // 对标 RefreshExpWithoutLevelUp: exp 截顶到 exp_lim;exp_lim==0 → persent=0
            if (expLim <= 0)
            {
                SetExpProgress(0f);
                _lb_exp.text = "0 / 0";
                return;
            }

            if (exp > expLim)
            {
                exp = expLim;
            }

            // 对标旧端: let a = exp / exp_lim * 100; persent = Math.floor(a) / 100
            float persent = Mathf.Floor((float)((double)exp / expLim * 100.0)) / 100f;
            SetExpProgress(persent);
            // 对标 onComleted: this._lb_exp.text = tb.exp + " / " + tb.exp_lim
            _lb_exp.text = exp + " / " + expLim;
        }

        /// <summary>
        /// 同步经验填充与特效进度。布局、满宽和特效相对偏移均保存在 HudNavBar.prefab；
        /// 这里只写 0～1 状态值，避免运行时改短 ExpBarFill 或覆盖 Inspector 调整。
        /// </summary>
        private void SetExpProgress(float progress)
        {
            float value = Mathf.Clamp01(progress);
            _img_exp.fillAmount = value;

            // Slider 不负责绘制，只驱动零尺寸 ExpBarEffectHandle；真实特效挂点是其普通子节点。
            // 因此 Slider 改写的仅是机械 Handle anchors，不会改 ExpBarFill 或 ExpBarSparkleSlot 的布局。
            UnityEngine.UI.Slider slider = _img_exp.GetComponent<UnityEngine.UI.Slider>();
            if (slider != null)
            {
                slider.SetValueWithoutNotify(value);
            }
        }

        /// <summary>
        /// MainUIModel/特效未移植:翻面红点与经验特效盒先隐藏(老客户端由 turn_red_dot 与 AddUIEffect 驱动可见性)。
        /// 不造假数据/特效,沿用既有 View 的 gameObject.SetActive(false) 收法。
        /// </summary>
        private void HideUnbackedIndicators()
        {
            _img_red.gameObject.SetActive(false);
        }

        /// <summary>
        /// 还原经验条闪光特效。对标老端 MainUIDownView.ts:395 AddUIEffect("ui_expbar", _box_exp_effect, null, 15, null)。
        /// 老端在经验条动画时 show + 1s 后 hide;Unity 无补间,先常驻挂一次让经验条不死板(后续可按经验变化脉冲)。
        /// scale 用老端原值 15(UIEffectStage 把 Laya scale 当 effect localScale);如显示偏大/偏小,这是全局校准点。
        /// </summary>
        private async Task AddExpEffectAsync()
        {
            if (_box_exp_effect == null || _expEffect != null || _expEffectAdding) return;
            _expEffectAdding = true;
            _box_exp_effect.gameObject.SetActive(true);
            UIEffectStage.Handle handle = await UIEffectStage.AddAsync(
                "ui_expbar", _box_exp_effect.transform as RectTransform, Vector2.zero, new Vector3(15f, 15f, 15f));
            _expEffectAdding = false;
            if (this == null || _box_exp_effect == null) { handle?.Dispose(); return; }
            _expEffect = handle;
        }

        /// <summary>
        /// 对标老端 PlayAnim：经验刷新时显示 ui_expbar，完成后约 1 秒隐藏；Handle 复用，不重复创建特效源。
        /// </summary>
        private async Task PulseExpEffectAsync()
        {
            int version = ++_expPulseVersion;
            await AddExpEffectAsync();
            if (this == null || _box_exp_effect == null || version != _expPulseVersion) return;
            _box_exp_effect.gameObject.SetActive(true);
            await Task.Delay(1000);
            if (this == null || _box_exp_effect == null || version != _expPulseVersion) return;
            _box_exp_effect.gameObject.SetActive(false);
        }

        private void HideTemplates()
        {
            if (_tpl_MainFuncIconItem != null)
            {
                _tpl_MainFuncIconItem.SetActive(false);
            }
        }

        /// <summary>清掉 prefab 里为“设计期可视化”塞进各槽的样例图标(编辑器可见、便于摆槽位;运行时清掉换真图标)。</summary>
        private void ClearDesignTimeSampleIcons()
        {
            if (_gp_icon_con == null) return;
            for (int s = 0; s < _gp_icon_con.childCount; s++)
            {
                Transform slot = _gp_icon_con.GetChild(s);
                for (int i = slot.childCount - 1; i >= 0; i--)
                {
                    GameObject c = slot.GetChild(i).gameObject;
                    c.SetActive(false);
                    Destroy(c);
                }
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

        /// <summary>对标 UpdateIconItem:遍历当前行,GetMainFuncOpenCond 过的才显示;【槽位式】按顺序填进
        /// FuncIconRow(_gp_icon_con)下的空槽(槽位位置由 prefab 决定,代码不算坐标)。</summary>
        private void BuildFuncIcons()
        {
            MainUIModel.MainFuncIcon[] line = MainUIModel.MainFuncIcons[_showType];
            int slotCount = _gp_icon_con != null ? _gp_icon_con.childCount : 0;
            int open = 0;  // 通过开放判定的图标数(> 槽数时尾部告警)
            int shown = 0; // 实际放进槽的图标数
            for (int i = 0; i < line.Length; i++)
            {
                MainUIModel.MainFuncIcon fi = line[i];
                // 开放判定集中在 MainUIModel.GetMainFuncOpenCond(Role/Bag 恒开,其余查功能开放表)。
                if (!MainUIModel.GetMainFuncOpenCond(fi.Func)) continue;
                open++;

                MainFuncIconItemBind item = GetOrCreateFuncIconItem(shown);
                if (item == null) continue;

                // 槽位放置:图标进槽、继承槽位置(GetChild 越界=槽不够,跳过,尾部统一告警)。
                RectTransform slot = shown < slotCount ? _gp_icon_con.GetChild(shown) as RectTransform : null;
                if (slot == null) continue;

                item.gameObject.SetActive(true);
                PlaceIconInSlot(item, slot);
                slot.gameObject.SetActive(true);

                // 走 MainFuncIconItem.SetData:它填图标 + 隐红点 + 一次性绑定点击(_clickBound 守卫,
                // 防 BuildFuncIcons 多次调用叠加监听),点击经 MainUIRouter 用 MainFuncIcon.Res 打开对应面板。
                MainFuncIconItem view = item as MainFuncIconItem;
                if (view != null)
                {
                    view.SetData(fi);
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

            // 多余空槽隐藏(槽位式:槽比图标多 → 隐藏)。
            for (int i = shown; i < slotCount; i++)
            {
                _gp_icon_con.GetChild(i).gameObject.SetActive(false);
            }

            if (open > slotCount)
            {
                GameLog.Warn("MainUI", "功能图标 {0} 个 > 槽位 {1} 个,超出的 {2} 个未显示(去 HudNavBar.prefab 的 FuncIconRow 下加槽)",
                    open, slotCount, open - slotCount);
            }

            // 图标重铺后手指目标可能重建/换行,重挂引导。
            RefreshIconGuide();
        }

        /// <summary>对标 turn_btn_fun:可翻面时 show_type 在各行间循环,刷新按钮态与图标行。</summary>
        private void OnClickTurn()
        {
            if (!MainUIModel.GetTurnState()) return;

            _showType++;
            if (_showType >= MainUIModel.MainFuncIcons.Length) _showType = 0;
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

            bool open = MainUIModel.GetTurnState();
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

        // 撑满所在槽:槽多大图多大,显示尺寸完全由 prefab 的槽控制(修复:横条图标被压进方形模板)。
        private static void PlaceIconInSlot(MainFuncIconItemBind item, RectTransform slot)
        {
            var rt = (RectTransform)item.transform;
            rt.SetParent(slot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            item.gameObject.SetActive(true);
        }
    }
}
