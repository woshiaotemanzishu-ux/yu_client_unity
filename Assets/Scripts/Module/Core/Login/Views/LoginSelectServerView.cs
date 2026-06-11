using System.Collections.Generic;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Login;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 选服窗(对齐老客户端):左侧 _list_tab 是区服分组 tab(底板 _select_left 自带米白条),
    /// 右侧 _list_item 是当前区的服务器行。tab 选中态规则照抄 LoginSelectServerTabItem.ts:
    /// 选中 = 亮底(ui_Login_21,scene 已烘)+ 字色 #FFEEE4;未选中 = 隐藏底 + #81452B。
    /// TODO(转换器):为 _tpl 模板生成 ItemBind,替换 transform.Find 取节点。
    /// </summary>
    public sealed class LoginSelectServerView : LoginSelectServerViewBind
    {
        private const float ITEM_SPACING = 26f; // scene _list_item.spaceY
        private const float TAB_SPACING = 4f;   // scene _list_tab.spaceY

        private static readonly Color TAB_SELECTED = ParseColor("#FFEEE4");
        private static readonly Color TAB_NORMAL = ParseColor("#81452B");

        private const int TAB_RECENT = -1; // 「最近登录」虚拟分区

        private readonly List<GameObject> _items = new List<GameObject>();
        private readonly List<GameObject> _tabs = new List<GameObject>();
        private readonly List<int> _tabAreas = new List<int>();
        private int _selectedArea = TAB_RECENT;

        protected override void OnInit()
        {
            UIUtil.AddClick(_img_close, OnClickClose);
        }

        protected override void OnShow(object args)
        {
            BuildTabs();
            BuildList();
        }

        // ------------------------------------------------ 区服 tab

        private void BuildTabs()
        {
            foreach (GameObject go in _tabs) Destroy(go);
            _tabs.Clear();
            _tabAreas.Clear();

            if (_tpl_LoginSelectServerTabItem == null || _list_tab == null) return;

            // 老客户端 tab 序:最近登录(玩过的服)+ 各大区(名字来自登录返回的 areas)
            var tabs = new List<(int area, string label)> { (TAB_RECENT, "最近登录") };
            foreach (LoginAreaInfo area in LoginModel.Instance.Areas)
            {
                tabs.Add((area.id, area.name));
            }
            if (tabs.Count == 1)
            {
                // 没有大区数据时按 server.area 兜底分组
                var fallback = new SortedSet<int>();
                foreach (LoginServerInfo s in LoginModel.Instance.Servers) fallback.Add(Mathf.Max(1, s.area));
                foreach (int a in fallback) tabs.Add((a, $"第{a}区"));
            }
            if (!HasRecentServers() && _selectedArea == TAB_RECENT && tabs.Count > 1)
            {
                _selectedArea = tabs[1].area;
            }

            RectTransform content = _list_tab.content;
            RectTransform tplRect = (RectTransform)_tpl_LoginSelectServerTabItem.transform;
            float tabHeight = tplRect.sizeDelta.y;

            for (int i = 0; i < tabs.Count; i++)
            {
                (int area, string label) = tabs[i];
                GameObject tab = Instantiate(_tpl_LoginSelectServerTabItem, content);
                tab.SetActive(true);
                ((RectTransform)tab.transform).anchoredPosition = new Vector2(0f, -i * (tabHeight + TAB_SPACING));

                TextMeshProUGUI nameLabel = FindLabel(tab, "_lb_name");
                if (nameLabel != null) nameLabel.text = label;
                Image bg = FindImage(tab, "_img_bg");
                if (bg != null)
                {
                    int captured = area;
                    UIUtil.AddClick(bg, () => OnClickTab(captured));
                }

                _tabs.Add(tab);
                _tabAreas.Add(area);
            }
            content.sizeDelta = new Vector2(content.sizeDelta.x, tabs.Count * (tabHeight + TAB_SPACING));
            RefreshTabStates();
        }

        private static bool HasRecentServers()
        {
            foreach (LoginServerInfo s in LoginModel.Instance.Servers)
            {
                if (s.roleId > 0 || s.level > 0) return true;
            }
            return false;
        }

        private void RefreshTabStates()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                bool selected = _tabAreas[i] == _selectedArea;
                Image bg = FindImage(_tabs[i], "_img_bg");
                if (bg != null) bg.enabled = selected;
                TextMeshProUGUI nameLabel = FindLabel(_tabs[i], "_lb_name");
                if (nameLabel != null) nameLabel.color = selected ? TAB_SELECTED : TAB_NORMAL;
            }
        }

        private void OnClickTab(int area)
        {
            if (_selectedArea == area) return;
            _selectedArea = area;
            RefreshTabStates();
            BuildList();
        }

        // ------------------------------------------------ 服务器行

        private void BuildList()
        {
            foreach (GameObject go in _items) Destroy(go);
            _items.Clear();

            if (_tpl_LoginSelectServerItem == null || _list_item == null)
            {
                GameLog.Error("Login", "选服列表模板缺失(_tpl_LoginSelectServerItem),检查 Bind 回填");
                return;
            }

            RectTransform content = _list_item.content;
            RectTransform tplRect = (RectTransform)_tpl_LoginSelectServerItem.transform;
            float itemHeight = tplRect.sizeDelta.y;

            int index = 0;
            foreach (LoginServerInfo server in LoginModel.Instance.Servers)
            {
                bool match = _selectedArea == TAB_RECENT
                    ? (server.roleId > 0 || server.level > 0)
                    : server.area == _selectedArea;
                if (!match) continue;
                GameObject item = Instantiate(_tpl_LoginSelectServerItem, content);
                item.SetActive(true);
                ((RectTransform)item.transform).anchoredPosition = new Vector2(0f, -index * (itemHeight + ITEM_SPACING));
                BindItem(item, server);
                _items.Add(item);
                index++;
            }
            content.sizeDelta = new Vector2(content.sizeDelta.x, index * (itemHeight + ITEM_SPACING));
        }

        private void BindItem(GameObject item, LoginServerInfo server)
        {
            GameLog.Debug("Login", "服务器行 id={0} name='{1}' area={2} lv={3} career={4} closed={5}",
                server.id, server.name, server.area, server.level, server.career, server.closed);

            TextMeshProUGUI nameLabel = FindLabel(item, "_lb_server_name");
            if (nameLabel != null)
            {
                nameLabel.text = string.IsNullOrEmpty(server.name)
                    ? $"服务器 {server.id:0000}"
                    : server.DisplayName;
            }
            TextMeshProUGUI levelLabel = FindLabel(item, "_lb_level");
            if (levelLabel != null)
            {
                levelLabel.text = server.level > 0 ? "LV." + server.level : string.Empty;
            }
            Image career = FindImage(item, "_img_career");
            if (career != null) career.enabled = server.career > 0;
            Image tips = FindImage(item, "_img_tips");
            if (tips != null) tips.enabled = server.isNew;

            Image bg = FindImage(item, "_img_bg");
            if (bg != null)
            {
                UIUtil.AddClick(bg, () => OnClickItem(server));
            }
        }

        private void OnClickItem(LoginServerInfo server)
        {
            _ = LoginFlow.SelectServerAsync(server);
        }

        private void OnClickClose()
        {
            Hide();
        }

        // ------------------------------------------------ 工具

        private static TextMeshProUGUI FindLabel(GameObject root, string name)
        {
            Transform t = root.transform.Find(name);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        private static Image FindImage(GameObject root, string name)
        {
            Transform t = root.transform.Find(name);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private static Color ParseColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;
        }
    }
}
