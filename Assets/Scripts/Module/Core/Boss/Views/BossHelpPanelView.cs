using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Generated.UI.Suitboss;
using Shenxiao.Module.Core.Guild;
using UnityEngine;

namespace Shenxiao.Module.Core.Boss.Views
{
    /// <summary>
    /// 结社协助-伤害面板(自动循环 轮15a"缝合点",对标老端 boss/BossHelpPanel.ts,死嵌在
    /// BossFightSceneView 的 `_tpl_BossHelpPanel` 模板里)。
    ///
    /// **轮15a 侦察订正**:主控裁决表原描述"求助按钮反向调 GuildController 既有API(40401/403/408)"——
    /// 直接读老端源码后订正:`_panel_btn` 实际是纯本地展开/收起动画开关(showpanel),不发任何协议;
    /// 真正消费 40401/403/408 的是另一个类 `BossHelpRolePanel.ts`(前往/退出协助按钮),该类尚未
    /// convert-module(无 Generated Bind),留 TODO/pendingBatch。本类真实能"接真"的是**面板显隐门控**——
    /// 老端 `GuildEvent.REQ_SUCCESS`(type==1)/`UPDATE_HELP_OBJ`(type==1 && role_id==自己)/
    /// `DEL_HELP_DATA` 三事件驱动显隐,数据源即 <see cref="GuildModel.CurrentMyAssist"/>(40408,13b 已验收
    /// 数据链),本类订阅 <see cref="GlobalEvent.EVT_GUILD_ASSIST_UPDATE"/> 复评,是真实数据驱动(非双注册,
    /// 只读 GuildModel/调 GuildController 既有公开方法)。
    ///
    /// **修复轮订正(REQ_SUCCESS 臂)**:老端 BossHelpPanel.ts:69-77 `REQ_SUCCESS`(40401成功,type==1)→
    /// SetVisible(true)+Fire(REQUEST_PROTO,40408),:110 `LoadSuccess` 加载即拉一次 40408——服务端 40408
    /// 是纯应答(AssistId&gt;0 andalso AssistProcess==1 不满足时静默不回),不主动推;之前 OnShow/RefreshVisibility
    /// 从不触发这次拉取,CurrentMyAssist 永远吃不到自己刚发起的求助数据。现在 OnShow 补拉一次(对标
    /// LoadSuccess),RefreshVisibility 里额外识别 <see cref="GuildModel.MyRequest"/>(40401 回显,type==boss)
    /// 与 CurrentMyAssist 不同步时再补拉一次(对标 REQ_SUCCESS),同一 assistId 只拉一次防抖。
    ///
    /// 列表内容(谁在帮我打Boss+伤害占比)老端源自场景内实时战斗追踪(`BossDamageItem.ts`→
    /// `UPDATE_ASSIST_DAMAGE` 本地事件),不是协议字段——Unity 无 BossSceneManager 等价物(战斗场景运行时
    /// 未接),<see cref="SetDamageList"/> 是就绪的落位接口,TODO 场景系统接入后调用。
    /// </summary>
    public sealed class BossHelpPanelView : BossHelpPanelBind
    {
        private const int BOSS_ASSIST_TYPE = 1; // 对标老端 type==1(boss)

        private GameObject _itemTemplate;
        private readonly List<BossHelpItemView> _rows = new List<BossHelpItemView>();
        private bool _expanded = true;
        private bool _subscribed;
        private long _pulledAssistId = -1; // REQ_SUCCESS 等价臂防抖:同一 assistId 只补拉一次 40408

        protected override void OnInit()
        {
            if (_panel_btn != null) UIUtil.AddClick(_panel_btn, TogglePanel); // 对标老端 _panel_btn(BossHelpPanel.ts:46)
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            _pulledAssistId = -1;
            GuildController.Instance.RequestMyAssist(); // 对标老端 LoadSuccess:Fire(REQUEST_PROTO,40408)
            RefreshVisibility();
        }

        protected override void OnHide() => Unsubscribe();
        protected override void OnDispose() => Unsubscribe();

        /// <summary>注入 BossHelpItem 模板(来自宿主 BossFightSceneView 的 `_tpl_BossHelpItem`,本类自身
        /// 无该模板节点——同 BagComponentView.SetItemTemplate 套路)。</summary>
        public void SetItemTemplate(GameObject template) => _itemTemplate = template;

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_GUILD_ASSIST_UPDATE, RefreshVisibility);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_GUILD_ASSIST_UPDATE, RefreshVisibility);
            _subscribed = false;
        }

        /// <summary>对标老端三事件(REQ_SUCCESS/UPDATE_HELP_OBJ/DEL_HELP_DATA)合并语义:
        /// CurrentMyAssist(40408) 存在且 type==boss 且求助发起人是自己 → 显示;否则隐藏。</summary>
        private void RefreshVisibility()
        {
            GuildModel model = GuildModel.Instance;
            GuildModel.MyAssistInfo info = model.CurrentMyAssist;
            GuildModel.MyAssistRequest req = model.MyRequest;

            // REQ_SUCCESS 等价臂(老端 BossHelpPanel.ts:69-77):我方刚发起的求助(40401,type==boss)尚未
            // 拉到 40408 详情 → 补拉一次;按 assistId 防抖,避免其它 EVT_GUILD_ASSIST_UPDATE 触发重复请求。
            if (req != null && req.Type == BOSS_ASSIST_TYPE && (info == null || info.AssistId != req.AssistId)
                && _pulledAssistId != req.AssistId)
            {
                _pulledAssistId = req.AssistId;
                GuildController.Instance.RequestMyAssist();
            }

            bool visible = info != null && info.Type == BOSS_ASSIST_TYPE
                && info.RoleId == Shenxiao.Module.Core.Role.RoleModel.Instance.RoleId;
            gameObject.SetActive(visible);
        }

        /// <summary>面板展开/收起(对标老端 showpanel:纯本地 UI 状态,不发协议;动画留 TODO,本轮瞬切)。</summary>
        private void TogglePanel()
        {
            _expanded = !_expanded;
            if (_panel_gp != null) _panel_gp.gameObject.SetActive(_expanded);
        }

        /// <summary>伤害占比列表落位(老端 UPDATE_ASSIST_DAMAGE;TODO 待场景战斗系统接入调用)。</summary>
        public void SetDamageList(List<(string name, int modulusPercent)> list)
        {
            for (int i = 0; i < _rows.Count; i++) _rows[i].gameObject.SetActive(false);

            if (list == null || _itemTemplate == null || _list == null || _list.content == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                BossHelpItemView row = GetOrCreateRow(i);
                row.gameObject.SetActive(true);
                row.SetData(list[i].name, list[i].modulusPercent);
            }
        }

        private BossHelpItemView GetOrCreateRow(int index)
        {
            if (index < _rows.Count) return _rows[index];
            GameObject go = Object.Instantiate(_itemTemplate, _list.content);
            go.name = "BossHelpItem_" + index;
            go.SetActive(true);
            BossHelpItemView row = go.GetComponent<BossHelpItemView>();
            if (row == null) row = go.AddComponent<BossHelpItemView>();
            _rows.Add(row);
            return row;
        }
    }
}
