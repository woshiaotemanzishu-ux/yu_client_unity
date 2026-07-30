using System.Collections.Generic;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>
    /// 选服页(重构版,自包含独立 prefab)——取代老端 LoginSelectServerView + 碎片化 tab/item scene。
    ///
    /// 结构(对标截图4):背景 + 面板框 + 标题图「选择服务器」+ 右上关闭按钮;
    ///   左侧 tabContainer = 区服分组 tab 列(最近登录 / 各大区),右侧 itemContainer = 当前区服务器行。
    ///   tabTemplate / itemTemplate 是 prefab 内默认隐藏的克隆源(运行时 Instantiate 填数据)。
    /// prefab 由 ServerSelectCreator 纯代码建树生成并回填下列 public 引用。
    ///
    /// 选中态规则照搬老 LoginSelectServerView/LoginSelectServerTabItem:
    ///   选中 tab = 亮底(白)+ 字色 #FFEEE4;未选中 = 透明底(保持可点)+ 字色 #81452B。
    ///
    /// 本类只做:① 数据绑定(从 LoginController.Instance.Model.Areas/Servers 建 tab/列表)
    ///   ② 功能性状态切换(选中态换底/换字色 / 模板项 SetActive)。不写颜色字号尺寸样式(那是 Creator + 手调的事;
    ///   选中态色值是功能性指示,照抄老端常量,允许)。
    ///
    /// ---- 对接说明(给主控 LoginFlow)----
    /// 暴露的 public 方法:
    ///   void Refresh()                      —— 重建 tab + 列表(LoginFlow 在 OpenServerSelect 后调用,或 OnShow 自动调)
    /// 调用的 LoginFlow 方法:
    ///   LoginFlow.SelectServerAsync(LoginServerInfo server)   —— 已存在
    /// 关闭:点 X → 自身 Hide()(老端即 Hide;若主控想统一收口可改调 LoginFlow.CloseServerSelect,本类不依赖它)。
    /// </summary>
    [UIView("prefabs/ui/login/serverselectview")]
    public sealed class ServerSelectView : BaseView
    {
        // 模板项内部子节点名(由 ServerSelectCreator 按这些名字建树;运行时按名取,保持 data-only)
        private const string TAB_BG = "_img_bg";
        private const string TAB_NAME = "_lb_name";
        private const string ITEM_NAME = "_lb_server_name";
        private const string ITEM_LEVEL = "_lb_level";
        private const string ITEM_CAREER = "_img_career";
        private const string ITEM_TIPS = "_img_tips";

        // 排版(间距/对齐/位置)全由 prefab 上 TabContainer/ItemContainer 的 VerticalLayoutGroup 负责,
        // 本类不写 anchoredPosition / sizeDelta,避免覆盖编辑器里手调的布局。

        private static readonly Color TAB_SELECTED = ParseColor("#FFEEE4");
        private static readonly Color TAB_NORMAL = ParseColor("#81452B");

        private const int TAB_RECENT = -1; // 「最近登录」虚拟分区

        [Header("框架")]
        public Image closeBtn;

        [Header("左侧区服 tab 列")]
        public RectTransform tabContainer;   // tab 克隆父节点(对应老 _list_tab.content)
        public GameObject tabTemplate;       // 默认隐藏,克隆源

        [Header("右侧服务器列表")]
        public RectTransform itemContainer;  // 服务器行克隆父节点(对应老 _list_item.content)
        public GameObject itemTemplate;      // 默认隐藏,克隆源

        private readonly List<GameObject> _items = new List<GameObject>();
        private readonly List<GameObject> _tabs = new List<GameObject>();
        private readonly List<int> _tabAreas = new List<int>();
        private int _selectedArea = TAB_RECENT;

        protected override void OnInit()
        {
            // 模板默认隐藏,克隆出来的副本才显示
            if (tabTemplate != null) tabTemplate.SetActive(false);
            if (itemTemplate != null) itemTemplate.SetActive(false);
            UIUtil.AddClick(closeBtn, OnClickClose);
        }

        protected override void OnShow(object args)
        {
            Refresh();
        }

        /// <summary>重建 tab + 服务器列表(对外入口;LoginFlow 可在打开后主动调,OnShow 也会自动调)。</summary>
        public void Refresh()
        {
            BuildTabs();
            BuildList();
        }

        // ------------------------------------------------ 区服 tab(搬自老 BuildTabs)

        private void BuildTabs()
        {
            foreach (GameObject go in _tabs) Destroy(go);
            _tabs.Clear();
            _tabAreas.Clear();

            if (tabTemplate == null || tabContainer == null) return;

            // tab 序:最近登录(玩过的服)+ 各大区(名字来自登录返回的 areas)
            var tabs = new List<(int area, string label)> { (TAB_RECENT, "最近登录") };
            foreach (LoginAreaInfo area in LoginController.Instance.Model.Areas)
            {
                tabs.Add((area.id, area.name));
            }
            if (tabs.Count == 1)
            {
                // 没有大区数据时按 server.area 兜底分组
                var fallback = new SortedSet<int>();
                foreach (LoginServerInfo s in LoginController.Instance.Model.Servers) fallback.Add(Mathf.Max(1, s.area));
                foreach (int a in fallback) tabs.Add((a, $"第{a}区"));
            }
            if (!HasRecentServers() && _selectedArea == TAB_RECENT && tabs.Count > 1)
            {
                _selectedArea = tabs[1].area;
            }

            for (int i = 0; i < tabs.Count; i++)
            {
                (int area, string label) = tabs[i];
                GameObject tab = Instantiate(tabTemplate, tabContainer);
                tab.SetActive(true);
                // 位置交给 tabContainer 的 VerticalLayoutGroup,这里不手动排

                TextMeshProUGUI nameLabel = FindText(tab, TAB_NAME);
                if (nameLabel != null) nameLabel.text = label;

                Image bg = FindImage(tab, TAB_BG);
                int captured = area;
                if (bg != null) UIUtil.AddClick(bg, () => OnClickTab(captured));

                _tabs.Add(tab);
                _tabAreas.Add(area);
            }
            RefreshTabStates();
        }

        private static bool HasRecentServers()
        {
            foreach (LoginServerInfo s in LoginController.Instance.Model.Servers)
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
                Image bg = FindImage(_tabs[i], TAB_BG);
                TextMeshProUGUI nameLabel = FindText(_tabs[i], TAB_NAME);
                // 不能用 enabled 隐藏:Graphic 禁用后不接收点击;用 alpha=0(clear),点击区保持有效。
                if (bg != null)
                {
                    bg.enabled = true;
                    bg.color = selected ? Color.white : Color.clear;
                }
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

        // ------------------------------------------------ 服务器行(搬自老 BuildList/BindItem)

        private void BuildList()
        {
            foreach (GameObject go in _items) Destroy(go);
            _items.Clear();

            if (itemTemplate == null || itemContainer == null)
            {
                GameLog.Error("Login", "选服列表模板缺失(itemTemplate/itemContainer),检查 Creator 回填");
                return;
            }

            foreach (LoginServerInfo server in LoginController.Instance.Model.Servers)
            {
                bool match = _selectedArea == TAB_RECENT
                    ? (server.roleId > 0 || server.level > 0)
                    : server.area == _selectedArea;
                if (!match) continue;
                GameObject item = Instantiate(itemTemplate, itemContainer);
                item.SetActive(true);
                // 位置交给 itemContainer 的 VerticalLayoutGroup,这里不手动排
                BindItem(item, server);
                _items.Add(item);
            }
        }

        private void BindItem(GameObject item, LoginServerInfo server)
        {
            GameLog.Debug("Login", "服务器行 id={0} name='{1}' area={2} lv={3} career={4} closed={5}",
                server.id, server.name, server.area, server.level, server.career, server.closed);

            TextMeshProUGUI nameLabel = FindText(item, ITEM_NAME);
            if (nameLabel != null)
            {
                nameLabel.text = string.IsNullOrEmpty(server.name)
                    ? $"服务器 {server.id:0000}"
                    : server.DisplayName;
            }

            TextMeshProUGUI levelLabel = FindText(item, ITEM_LEVEL);
            if (levelLabel != null) levelLabel.text = server.level > 0 ? "LV." + server.level : string.Empty;

            Image career = FindImage(item, ITEM_CAREER);
            if (career != null) career.enabled = server.career > 0;

            Image tips = FindImage(item, ITEM_TIPS);
            if (tips != null) tips.enabled = server.isNew;

            // 命中体 = 整行根节点(对标老端:整张行底 _img_bg 都可点)。
            // 不绑 _img_bg 子节点:当前 prefab 里它已被改成行内小图,绑上去只有中间一小块能点。
            // 子节点(名字/等级等)无自己的点击处理器 → 点击会冒泡到行根这颗 Button 上,整行都能触发。
            UIUtil.AddClick(item, () => OnClickItem(server));
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

        private static Image FindImage(GameObject root, string childName)
        {
            Transform t = FindChild(root.transform, childName);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private static TextMeshProUGUI FindText(GameObject root, string childName)
        {
            Transform t = FindChild(root.transform, childName);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        /// <summary>按名在子树里找节点(模板项扁平,直接子级即可命中)。</summary>
        private static Transform FindChild(Transform root, string childName)
        {
            if (root.name == childName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), childName);
                if (found != null) return found;
            }
            return null;
        }

        private static Color ParseColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;
        }
    }
}
