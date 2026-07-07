using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Login;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 顶部状态条(对标老客户端 MainUITopView.ts:InitView + SetMoneyType)。
    /// 进界面克隆 _tpl_MainUIMoneyItem 到 _box_money,默认货币列表 [0,1,2](老客户端 default_money_list)。
    /// 初始化/显示及 EVT_ROLE_INFO_UPDATE 时,从 RoleModel(唯一真相源)刷战力/血量/等级/场景/坐标。
    /// 场景配置读取(config_scene)未移植前,场景名退化为「场景{SceneId}」明示来自真实数据(老客户端
    /// 取 config_scene.name,缺则「未知场景」)。VIP/充值/光环/客服红点与 buff 计数底图在对应数据/模型
    /// 移植前隐藏(无源可依不造假)。
    /// </summary>
    public sealed class MainUITopView : MainUITopViewBind
    {
        // 老客户端 default_money_list = [0, 1, 2](MainUITopView.ts:40)
        private static readonly int[] DEFAULT_MONEY_LIST = { 0, 1, 2 };
        private const float HP_BAR_WIDTH = 182f;
        // _box_icon 下图标排布:对标老端 RefBoxChildsLayout(_box_icon, 12, 72, "left") —— 72 宽 + 12 间距。
        private const float ICON_CELL_WIDTH = 72f;
        private const float ICON_GAP = 12f;

        private readonly List<MainUIMoneyItem> _moneyItems = new List<MainUIMoneyItem>();
        private int _sceneNameRequestId;
        private float _nextClockRefresh;

        protected override void OnInit()
        {
            BuildMoneyItems(DEFAULT_MONEY_LIST);
            HideUnbackedIndicators();
            WireTopButtons();
            // 主角光环默认隐藏(对标老端门禁:未达开放条件不显示),避免新号开局闪现按钮;
            // 隐藏后先把图标行左对齐紧排,RefreshHaloIconAsync 取配置就绪后再按开放条件复检。
            SetNodeVisible(_box_halo, false);
            ReflowIconBox();
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.On(GlobalEvent.EVT_PK_STATUS_CHANGED, RefreshPkStatus);
            RefreshRole();
            RefreshPkStatus();
            RefreshClock();
            _ = RefreshHaloIconAsync();
        }

        /// <summary>顶部状态条按钮 → 经 MainUIRouter 解耦打开对应面板(对标老端 MainUITopView 各 AddClickEvent)。
        /// 已可达目标:地图 _box_map→"map";头像 _img_head/等级 _box_lv→"setting"(老端仅头像可点,本端按需求
        /// 等级同开设置);buff→"buff";战斗模式 _box_fight_mode 带场景门禁(见 OnClickFightMode)。
        /// VIP/充值/光环/客服 等待对应模块移植后补 key。</summary>
        private void WireTopButtons()
        {
            BindNav(_box_map, "map");
            BindNav(_img_head, "setting");
            BindNav(_box_lv, "setting");
            BindNav(_box_buff, "buff");
            UIUtil.AddClick(_box_fight_mode, OnClickFightMode);
            BindNav(_box_vip, "vip");
            BindNav(_box_recharge, "recharge");
            BindNav(_box_halo, "halo");
            BindNav(_box_cs, "customerservice");
        }

        /// <summary>战斗模式角标点击(对标老端 MainUITopView.ts:165):先查当前场景 subtype,
        /// 0/1/2(城镇/安全类)提示「当前场景不允许切换PK状态」,否则打开模式选择弹窗(MainUIFightModeView)。</summary>
        private void OnClickFightMode()
        {
            _ = OpenFightModeGatedAsync();
        }

        private async Task OpenFightModeGatedAsync()
        {
            await MainUIConfigs.EnsureSceneLoaded();
            MainUIConfigs.SceneCfg cfg = MainUIConfigs.GetSceneCfg(RoleModel.Instance.SceneId);
            if (cfg == null) return; // 老端 scene_info 为空时不响应
            if (cfg.Subtype == 0 || cfg.Subtype == 1 || cfg.Subtype == 2)
            {
                TipsManager.Toast("当前场景不允许切换PK状态");
                return;
            }
            MainUIRouter.Open("fightmode");
        }

        private void BindNav(Component target, string routerKey)
        {
            if (target == null) return;
            UIUtil.AddClick(target, () => MainUIRouter.Open(routerKey));
        }

        protected override void OnShow(object args)
        {
            RefreshRole();
            RefreshPkStatus();
            RefreshClock();
        }

        private int _loadedPkIcon = int.MinValue;

        /// <summary>战斗模式角标随主角 pk_status 换图(对标老端 RefreshPkStatus:FightModeInfo[pk_status].main_source)。</summary>
        private void RefreshPkStatus()
        {
            if (_img_fight_mode == null) return;
            int pk = RoleModel.Instance.PkStatus;
            if (pk == _loadedPkIcon) return;
            FightModeInfoData info = PkStatusModel.Get(pk);
            if (info == null || string.IsNullOrEmpty(info.MainIcon)) return;
            _loadedPkIcon = pk;
            _ = ResManager.SetImageAsync(_img_fight_mode, GameResPath.GetIcon("mainUI", info.MainIcon), nativeSize: false);
        }

        public bool HeadVisible => IsNodeVisible(_box_head);
        public bool MapVisible => IsNodeVisible(_box_map);
        public bool IconVisible => IsNodeVisible(_box_icon);

        public void SetHeadVisible(bool visible)
        {
            SetNodeVisible(_box_head, visible);
        }

        public void SetMapVisible(bool visible)
        {
            SetNodeVisible(_box_map, visible);
            SetNodeVisible(_box_icon, visible);
        }

        public void SetBaseWindowTopVisible(bool headVisible, bool mapVisible, bool iconVisible)
        {
            SetNodeVisible(_box_head, headVisible);
            SetNodeVisible(_box_map, mapVisible);
            SetNodeVisible(_box_icon, iconVisible);
        }

        protected override void OnDispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_PK_STATUS_CHANGED, RefreshPkStatus);
        }

        private void OnDestroy()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_PK_STATUS_CHANGED, RefreshPkStatus);
        }

        private void OnRoleInfoUpdate()
        {
            RefreshRole();
            // 等级变化可能跨过光环开放门槛(open_lv=100),复检显隐(对标老端 CHANGE_LEVEL → UpdateHaloIcon)。
            _ = RefreshHaloIconAsync();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextClockRefresh) return;
            RefreshClock();
            _nextClockRefresh = Time.unscaledTime + 1f;
        }

        /// <summary>对标 SetMoneyType:按列表克隆货币项填进 _box_money(HBox 布局归转换产物)。</summary>
        private void BuildMoneyItems(int[] list)
        {
            if (_tpl_MainUIMoneyItem == null)
            {
                GameLog.Error("MainUI", "MainUITopView 缺 _tpl_MainUIMoneyItem 模板(重跑 mainUI 流水线:转换+回填)");
                return;
            }
            foreach (int type in list)
            {
                GameObject go = Instantiate(_tpl_MainUIMoneyItem, _box_money);
                go.SetActive(true);
                var item = go.GetComponent<MainUIMoneyItem>();
                if (item == null)
                {
                    GameLog.Error("MainUI", "货币项缺 MainUIMoneyItem 业务子类,重跑回填");
                    continue;
                }
                item.SetMoneyType(type);
                _moneyItems.Add(item);
            }
        }

        /// <summary>对标 InitView:战力/血量/等级/场景名/坐标 + 货币值,全部读 RoleModel。</summary>
        private int _headLoadedCareer = -1;

        /// <summary>头像:对标老端 RefreshHead(picture→DressModel.getHeadICon)。DressModel 未移植,
        /// 先降级为职业默认头像(同 CustomHeadItem.SetDefaultHead 套路);TODO 接 DressModel 自定义头像+头像框。</summary>
        private void RefreshHead(int career)
        {
            if (_img_head == null || career <= 0 || career == _headLoadedCareer) return;
            _headLoadedCareer = career;
            _ = LoadHeadAsync(career);
        }

        private async Task LoadHeadAsync(int career)
        {
            await LoginConfigs.EnsureLoaded();
            string path = LoginConfigs.HeadIconPath(career, 0);
            if (string.IsNullOrEmpty(path) || _img_head == null) return;
            _img_head.preserveAspect = true;
            await ResManager.SetImageAsync(_img_head, path, nativeSize: false);
        }

        private void RefreshRole()
        {
            RoleModel m = RoleModel.Instance;
            RefreshHead(m.Career);

            _lb_fighting.text = m.CombatPower.ToString();
            _lb_lv.text = m.Level.ToString();

            // 血量来自战斗属性块(13001 携带);未到时显示 0/0(对标老客户端 hp/maxHp 初值)
            if (m.BattleAttr != null)
            {
                _lb_hp.text = m.BattleAttr.Hp + "/" + m.BattleAttr.HpLim;
                SetHpBarWidth(m.BattleAttr.Hp, m.BattleAttr.HpLim);
            }
            else
            {
                _lb_hp.text = "0/0";
                SetHpBarWidth(0, 0);
            }

            // Matches old-client RefreshSceneName: config_scene[sceneId].name, fallback 未知场景.
            RefreshSceneName(m.SceneId);
            _lb_role_pos.gameObject.SetActive(false);

            foreach (MainUIMoneyItem item in _moneyItems)
            {
                item.Refresh();
            }
        }

        private void SetHpBarWidth(long hp, long maxHp)
        {
            float percent = 0f;
            if (maxHp > 0)
            {
                percent = Mathf.Clamp01((float)((double)hp / maxHp));
            }
            _img_hp.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, HP_BAR_WIDTH * percent);
        }

        private void RefreshSceneName(int sceneId)
        {
            int requestId = ++_sceneNameRequestId;
            if (sceneId <= 0)
            {
                _lb_scene_name.text = "未知场景";
                return;
            }

            _ = RefreshSceneNameAsync(sceneId, requestId);
        }

        private async Task RefreshSceneNameAsync(int sceneId, int requestId)
        {
            await MainUIConfigs.EnsureSceneLoaded();
            if (requestId != _sceneNameRequestId || _lb_scene_name == null) return;

            MainUIConfigs.SceneCfg cfg = MainUIConfigs.GetSceneCfg(sceneId);
            _lb_scene_name.text = cfg != null && !string.IsNullOrWhiteSpace(cfg.Name) ? cfg.Name : "未知场景";
        }

        private void RefreshClock()
        {
            DateTime utc = TimeUtil.NowUtc();
            if (utc.Year < 2020) utc = DateTime.UtcNow;
            _lb_time.text = utc.ToLocalTime().ToString("HH:mm");
        }

        /// <summary>
        /// 主角光环按钮(_box_halo)开放门禁:对标老端 MainUITopView.UpdateHaloIcon →
        /// HaloModel.GetOpenState → CheckFuncOpenState("HaloMainView")(开服≥6 天 且 等级≥100)。
        /// 未达条件隐藏(新号创建即满足隐藏),达成后随等级变化(EVT_ROLE_INFO_UPDATE)复检显示;
        /// 显隐后按老端 RefBoxChildsLayout(_box_icon,12,72,"left") 把可见图标左对齐重排,消除空位。
        /// 注:老端 GetOpenState 还含 is_(wx_)alpha 平台分支与 DAY_CHANGE 单独触发,本端未移植平台变体;
        ///     「已满级但尚未到第6天」这一开服天达标转换当前无日切事件驱动,等级门(100)为实际主门。
        /// </summary>
        private async Task RefreshHaloIconAsync()
        {
            await FuncOpenConfig.EnsureLoaded();
            if (this == null || _box_halo == null) return; // view destroyed during await

            SetNodeVisible(_box_halo, FuncOpenConfig.CheckFuncOpenState("HaloMainView"));
            ReflowIconBox();
        }

        /// <summary>对标 RefBoxChildsLayout(_box_icon, 12, 72, true, "left"):_box_icon 下可见子项按
        /// 72 宽 + 12 间距从左到右紧排(隐藏项不占位),保留各自 y。子项 anchorMin/Max=(0,1)、pivot.x=0,
        /// 故 anchoredPosition.x 即左边距,直接对应老端 cell.x。</summary>
        private void ReflowIconBox()
        {
            if (_box_icon == null) return;
            float x = 0f;
            for (int i = 0; i < _box_icon.childCount; i++)
            {
                if (!(_box_icon.GetChild(i) is RectTransform child)) continue;
                if (!child.gameObject.activeSelf) continue;
                Vector2 pos = child.anchoredPosition;
                child.anchoredPosition = new Vector2(x, pos.y);
                x += ICON_CELL_WIDTH + ICON_GAP;
            }
        }

        /// <summary>
        /// 无源可依的红点/计数底图先隐藏(老客户端由 VipModel/HaloModel/buff_list 等驱动可见性,
        /// 这些模型尚未移植)。红点是指示图非可点元素,沿用既有 View 的 gameObject.SetActive(false) 收法。
        /// </summary>
        private void HideUnbackedIndicators()
        {
            _img_vip_red.gameObject.SetActive(false);
            _img_recharge_red.gameObject.SetActive(false);
            _img_halo_red.gameObject.SetActive(false);
            _img_cs_red.gameObject.SetActive(false);
            _img_buff_count_bg.gameObject.SetActive(false);
            _lb_buff_other_count.gameObject.SetActive(false);

            if (_tpl_MainUIMoneyItem != null) _tpl_MainUIMoneyItem.SetActive(false);
            if (_tpl_MainUITopBuffItem != null) _tpl_MainUITopBuffItem.SetActive(false);
            if (_tpl_CustomHeadItem != null) _tpl_CustomHeadItem.SetActive(false);
            if (_tpl_ActivityIcon != null) _tpl_ActivityIcon.SetActive(false);
        }

        private static bool IsNodeVisible(Component node)
        {
            return node != null && node.gameObject.activeSelf;
        }

        private static void SetNodeVisible(Component node, bool visible)
        {
            if (node != null) node.gameObject.SetActive(visible);
        }
    }
}
