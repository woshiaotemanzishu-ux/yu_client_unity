using System.Collections.Generic;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Login;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 选服窗(对标老客户端 login/LoginSelectServerView.ts)。
    ///
    /// data-only:结构(背景 / 关闭按钮 / 左侧大区 tab 列表 / 右侧服务器行列表 / 地区列表)全部由
    /// 快照烤进 prefab,这里【只绑数据】——不再运行时 Instantiate 模板、不再摆位置
    /// (老的 LoopScrowViewMgr.SetData/克隆 _tpl_* 已删)。
    ///
    /// 三段列表沿用样板 LoginCreateRoleView.BindBakedCareers 的范式:
    /// 遍历容器(_list_tab / _list_item / _list_location 的 Content)下已烤好的同名项节点,
    /// FindImage/FindText 按路径取内部图/文字逐个绑数据,点击前先 ClearClicks 再 AddClick 防叠加。
    ///
    /// 数据源:LoginModel(Servers/Areas/Roles)。tab 序对标老端:虚拟「最近登录」(玩过的服)
    /// + 各大区;选中态规则照抄 LoginSelectServerTabItem.ts(选中亮底 + 字色 #FFEEE4,未选 #81452B)。
    /// 服务器行状态/职业图标对标 LoginSelectServerItem.ts。
    ///
    /// 真·运行时(海外平台成年提示 EyouThAdultItem、网络切区)按老端语义保留入口,资源未接处降级 + 日志。
    /// </summary>
    public sealed class LoginSelectServerView : LoginSelectServerViewBind
    {
        private const int TAB_RECENT = -1; // 老端 tab_list[0] = { name:"最近登录", id:0 } 的虚拟分区

        private static readonly Color TAB_SELECTED = ParseColor("#FFEEE4");
        private static readonly Color TAB_NORMAL = ParseColor("#81452B");

        /// <summary>烤进 _list_tab 的页签项:绑数据时缓存它内部的底图/文字 + 该项的大区 id。</summary>
        private struct TabSlot
        {
            public GameObject root;
            public Image bg;
            public TMP_Text label;
            public int area;
        }

        /// <summary>烤进 _list_item 的服务器行:缓存内部各图/文字 + 对应 server。</summary>
        private struct ServerSlot
        {
            public GameObject root;
            public Image bg;
            public Image mode;
            public Image tips;
            public Image career;
            public TMP_Text name;
            public TMP_Text level;
            public LoginServerInfo server;
        }

        private readonly List<TabSlot> _tabSlots = new List<TabSlot>();
        private readonly List<ServerSlot> _serverSlots = new List<ServerSlot>();
        private readonly List<(int area, string label)> _tabs = new List<(int area, string label)>();
        private int _selectedArea = TAB_RECENT;

        protected override void OnInit()
        {
            // 关闭按钮:对标老端 InitEvent 里 _img_close 的 AddClickEvent → Close()
            UIUtil.AddClick(_img_close, OnClickClose);
        }

        protected override void OnShow(object args)
        {
            BuildTabData();
            BindBakedTabs();
            BindBakedServers();
        }

        // ------------------------------------------------ tab 数据(对标 UpdateTab / InitTab)

        private void BuildTabData()
        {
            _tabs.Clear();
            // 老端 InitTab:tab_list[0] = 最近登录(虚拟分区,聚合玩过的服)
            _tabs.Add((TAB_RECENT, "最近登录"));
            // 老端 UpdateTab:SortListByType(GetAreaData(),"id",1) 追加各大区
            foreach (LoginAreaInfo area in LoginModel.Instance.Areas)
            {
                _tabs.Add((area.id, area.name));
            }
            // areas 缺失时按 server.area 兜底分组(保证至少能选服)
            if (_tabs.Count == 1)
            {
                var fallback = new SortedSet<int>();
                foreach (LoginServerInfo s in LoginModel.Instance.Servers) fallback.Add(Mathf.Max(1, s.area));
                foreach (int a in fallback) _tabs.Add((a, "第" + a + "区"));
            }
            // 老端 RefreshTab:有最近服默认停「最近登录」(index=0),否则落到首个大区
            if (!HasRecentServers() && _selectedArea == TAB_RECENT && _tabs.Count > 1)
            {
                _selectedArea = _tabs[1].area;
            }
        }

        private static bool HasRecentServers()
        {
            foreach (LoginServerInfo s in LoginModel.Instance.Servers)
            {
                if (s.roleId > 0 || s.level > 0) return true;
            }
            return false;
        }

        // ------------------------------------------------ 绑左侧大区 tab(只绑烤好的项)

        /// <summary>把 tab 数据绑到烤进 _list_tab 的页签项上(不再克隆 _tpl_LoginSelectServerTabItem)。</summary>
        private void BindBakedTabs()
        {
            _tabSlots.Clear();
            RectTransform content = TabContent();
            if (content == null)
            {
                GameLog.Error("Login", "选服缺 _list_tab/Content(烤制 prefab 结构异常或未烤页签项)");
                return;
            }

            var items = CollectItems(content, "LoginSelectServerTabItem");
            if (items.Count == 0)
            {
                GameLog.Warn("Login", "_list_tab 下未发现烤好的页签项(LoginSelectServerTabItem*),tab 无法绑定——需重烤含项节点的 prefab");
            }
            if (items.Count < _tabs.Count)
            {
                GameLog.Warn("Login", "烤制页签项 {0} 少于大区数 {1}(数据变了需重烤)", items.Count, _tabs.Count);
            }

            for (int i = 0; i < items.Count; i++)
            {
                Transform item = items[i];
                bool used = i < _tabs.Count;
                item.gameObject.SetActive(used);
                if (!used) continue;

                (int area, string label) = _tabs[i];
                var slot = new TabSlot
                {
                    root = item.gameObject,
                    bg = FindImage(item, "_img_bg"),
                    label = FindText(item, "_lb_name"),
                    area = area,
                };
                if (slot.label != null) slot.label.text = label;
                int captured = area;
                Graphic hit = (Graphic)slot.bg ?? slot.label;
                if (hit != null)
                {
                    UIUtil.ClearClicks(hit); // 每次 OnShow 重绑前先清再加,防点击叠加(样板规则)
                    UIUtil.AddClick(hit, () => OnClickTab(captured));
                }
                _tabSlots.Add(slot);
            }
            RefreshTabStates();
        }

        private void RefreshTabStates()
        {
            for (int i = 0; i < _tabSlots.Count; i++)
            {
                bool selected = _tabSlots[i].area == _selectedArea;
                // 对标 LoginSelectServerTabItem.ts:选中显底图 ui_Login_21 + 字色 #FFEEE4;未选隐底 + #81452B
                if (_tabSlots[i].bg != null)
                {
                    if (selected)
                    {
                        _tabSlots[i].bg.enabled = true;
                        _ = ResManager.SetImageAsync(_tabSlots[i].bg,
                            GameResPath.GetIcon("login", "ui_Login_21"), nativeSize: false);
                    }
                    else
                    {
                        _tabSlots[i].bg.enabled = false;
                    }
                }
                if (_tabSlots[i].label != null)
                {
                    _tabSlots[i].label.color = selected ? TAB_SELECTED : TAB_NORMAL;
                }
            }
        }

        private void OnClickTab(int area)
        {
            // 对标老端 SwitchTab:同分区直接返回;切换后重建右侧服务器行
            if (_selectedArea == area) return;
            _selectedArea = area;
            RefreshTabStates();
            BindBakedServers();
        }

        // ------------------------------------------------ 绑右侧服务器行(只绑烤好的项)

        /// <summary>把当前分区的服务器绑到烤进 _list_item 的行上(对标 UpdateServerItem + LoginSelectServerItem.ts)。</summary>
        private void BindBakedServers()
        {
            _serverSlots.Clear();
            RectTransform content = ServerContent();
            if (content == null)
            {
                GameLog.Error("Login", "选服缺 _list_item/Content(烤制 prefab 结构异常或未烤服务器行)");
                return;
            }

            // 选出当前分区的服务器:最近登录 = 玩过的服;否则 = 该大区的服(对标 SwitchTab/UpdateServerItem)
            var servers = new List<LoginServerInfo>();
            foreach (LoginServerInfo s in LoginModel.Instance.Servers)
            {
                bool match = _selectedArea == TAB_RECENT
                    ? (s.roleId > 0 || s.level > 0)
                    : s.area == _selectedArea;
                if (match) servers.Add(s);
            }

            var items = CollectItems(content, "LoginSelectServerItem");
            if (items.Count == 0)
            {
                GameLog.Warn("Login", "_list_item 下未发现烤好的服务器行(LoginSelectServerItem*),列表无法绑定——需重烤含项节点的 prefab");
            }
            if (items.Count < servers.Count)
            {
                GameLog.Warn("Login", "烤制服务器行 {0} 少于待显示服务器 {1}(超出部分无法显示,需重烤足够行)", items.Count, servers.Count);
            }

            for (int i = 0; i < items.Count; i++)
            {
                Transform item = items[i];
                bool used = i < servers.Count;
                item.gameObject.SetActive(used);
                if (!used) continue;

                LoginServerInfo server = servers[i];
                var slot = new ServerSlot
                {
                    root = item.gameObject,
                    bg = FindImage(item, "_img_bg"),
                    mode = FindImage(item, "_img_mode"),
                    tips = FindImage(item, "_img_tips"),
                    career = FindImage(item, "_img_career"),
                    name = FindText(item, "_lb_server_name"),
                    level = FindText(item, "_lb_level"),
                    server = server,
                };
                BindServerSlot(slot);
                _serverSlots.Add(slot);
            }
        }

        private void BindServerSlot(ServerSlot slot)
        {
            LoginServerInfo server = slot.server;

            // 服名:对标 LoginSelectServerItem.UpdateName(优先 server.name,空则用 DisplayName 兜底)
            if (slot.name != null)
            {
                slot.name.text = string.IsNullOrEmpty(server.name)
                    ? server.DisplayName
                    : server.name;
            }

            // 状态:对标 UpdateState —— closed=1 关服图标且隐推荐标;否则按 state 切运营图标 + 推荐/火爆标
            if (slot.mode != null)
            {
                string modeIcon;
                bool showTips;
                string tipsIcon = null;
                if (server.closed == 1)
                {
                    modeIcon = "ui_Login_29";
                    showTips = false;
                }
                else if (server.state == 0)
                {
                    modeIcon = "ui_Login_27";
                    showTips = true;
                    tipsIcon = "uidl_026";
                }
                else if (server.state == 1)
                {
                    modeIcon = "ui_Login_28";
                    showTips = true;
                    tipsIcon = "uidl_027";
                }
                else
                {
                    modeIcon = "ui_Login_27";
                    showTips = false;
                }
                _ = ResManager.SetImageAsync(slot.mode, GameResPath.GetIcon("login", modeIcon), nativeSize: false);
                if (slot.tips != null)
                {
                    slot.tips.enabled = showTips;
                    if (showTips && tipsIcon != null)
                        _ = ResManager.SetImageAsync(slot.tips, GameResPath.GetIcon("login", tipsIcon), nativeSize: false);
                }
            }

            // 职业/等级:对标 UpdateCareer —— 有角色显职业图标 + Lv.xx;无角色隐藏
            bool hasRole = server.level > 0 || server.career > 0;
            if (slot.career != null)
            {
                slot.career.enabled = hasRole && server.career > 0;
                if (hasRole && server.career > 0)
                {
                    string careerIcon = CareerIcon(server.career);
                    if (careerIcon != null)
                        _ = ResManager.SetImageAsync(slot.career, GameResPath.GetIcon("login", careerIcon), nativeSize: false);
                }
            }
            if (slot.level != null)
            {
                slot.level.enabled = hasRole;
                slot.level.text = hasRole ? "Lv." + server.level : string.Empty;
            }

            // 点击进服:对标 ClickServer → CHANGE_CUR_SERVER_ID。命中体取 _img_bg(对标老端 item._img_bg 点击)
            Graphic hit = (Graphic)slot.bg ?? slot.name;
            if (hit != null)
            {
                LoginServerInfo captured = server;
                UIUtil.ClearClicks(hit); // 每次重绑前先清防叠加(样板规则)
                UIUtil.AddClick(hit, () => OnClickServer(captured));
            }
        }

        /// <summary>对标 UpdateCareer:1/2/3/4 → ui_Login_23/24/25/26。</summary>
        private static string CareerIcon(int career)
        {
            switch (career)
            {
                case 1: return "ui_Login_23";
                case 2: return "ui_Login_24";
                case 3: return "ui_Login_25";
                case 4: return "ui_Login_26";
                default: return null;
            }
        }

        private void OnClickServer(LoginServerInfo server)
        {
            // 对标老端 ClickServer:Fire CHANGE_CUR_SERVER_ID + 提示 + 关闭。沿用现有 LoginFlow 选服链路。
            _ = LoginFlow.SelectServerAsync(server);
        }

        private void OnClickClose()
        {
            // 对标老端 _img_close → Close()
            Hide();
        }

        // ------------------------------------------------ 容器兜底 + 项收集 + 节点查找

        // 容器字段没绑上时按名兜底(对标样板 HeadCon/ModelCon):
        // _list_tab/_list_item 都是 ScrollRect,数据项烤在它的 content(Viewport/Content)下。
        private RectTransform TabContent() => ScrollContent(_list_tab, "_list_tab");
        private RectTransform ServerContent() => ScrollContent(_list_item, "_list_item");

        private RectTransform ScrollContent(ScrollRect scroll, string nodeName)
        {
            if (scroll != null && scroll.content != null) return scroll.content;
            // 兜底:Bind 字段没绑上时,从根按名找 ScrollRect,再取它的 content
            Transform node = transform.Find(nodeName);
            ScrollRect sr = scroll != null ? scroll : (node != null ? node.GetComponent<ScrollRect>() : null);
            if (sr != null && sr.content != null) return sr.content;
            // 再兜底:直接找 Viewport/Content 子节点
            if (node != null)
            {
                Transform viewport = node.Find("Viewport");
                Transform contentT = viewport != null ? viewport.Find("Content") : node.Find("Content");
                if (contentT != null) return contentT as RectTransform;
            }
            return null;
        }

        /// <summary>收集容器下已烤好的项节点(节点名以 namePrefix 开头),按层级序。</summary>
        private static List<Transform> CollectItems(Transform content, string namePrefix)
        {
            var items = new List<Transform>();
            for (int i = 0; i < content.childCount; i++)
            {
                Transform c = content.GetChild(i);
                if (c.name.StartsWith(namePrefix)) items.Add(c);
            }
            return items;
        }

        private static Image FindImage(Transform root, string path)
        {
            Transform t = root.Find(path);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private static TMP_Text FindText(Transform root, string path)
        {
            Transform t = root.Find(path);
            if (t == null) return null;
            TMP_Text self = t.GetComponent<TMP_Text>();
            return self != null ? self : t.GetComponentInChildren<TMP_Text>(true);
        }

        private static Color ParseColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;
        }
    }
}
