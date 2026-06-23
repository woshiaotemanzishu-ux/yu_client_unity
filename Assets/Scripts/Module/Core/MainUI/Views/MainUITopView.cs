using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.MainUI;
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

        private readonly List<MainUIMoneyItem> _moneyItems = new List<MainUIMoneyItem>();
        private int _sceneNameRequestId;
        private float _nextClockRefresh;

        protected override void OnInit()
        {
            BuildMoneyItems(DEFAULT_MONEY_LIST);
            HideUnbackedIndicators();
            WireTopButtons();
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            RefreshRole();
            RefreshClock();
        }

        /// <summary>顶部状态条按钮 → 经 MainUIRouter 解耦打开对应面板(对标老端 MainUITopView 各 AddClickEvent)。
        /// 已可达目标:地图 _box_map→"map"(MapFlow,世界地图);头像 _img_head→"setting"(SettingView,老端头像开设置)。
        /// VIP/充值/光环/客服/buff/战斗模式 等待对应模块移植后补 key。</summary>
        private void WireTopButtons()
        {
            BindNav(_box_map, "map");
            BindNav(_img_head, "setting");
            BindNav(_box_buff, "buff");
            BindNav(_box_fight_mode, "fightmode");
            BindNav(_box_vip, "vip");
            BindNav(_box_recharge, "recharge");
            BindNav(_box_halo, "halo");
        }

        private void BindNav(Component target, string routerKey)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => MainUIRouter.Open(routerKey));
        }

        protected override void OnShow(object args)
        {
            RefreshRole();
            RefreshClock();
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
        }

        private void OnDestroy()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        private void OnRoleInfoUpdate()
        {
            RefreshRole();
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
        private void RefreshRole()
        {
            RoleModel m = RoleModel.Instance;

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
