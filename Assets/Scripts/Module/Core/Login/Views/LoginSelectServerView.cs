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
    /// 选服窗:用 LoginModel.Servers 真实数据填充 _list_item,
    /// 行模板来自转换器内联的 __Templates/_tpl_LoginSelectServerItem。
    /// 区域 tab / location 列表本轮不接(隐藏),后续随选角一起补。
    /// TODO(转换器):为 _tpl 模板生成 ItemBind,替换下面的 transform.Find 取节点。
    /// </summary>
    public sealed class LoginSelectServerView : LoginSelectServerViewBind
    {
        private const float ITEM_SPACING = 8f;

        private readonly List<GameObject> _items = new List<GameObject>();

        protected override void OnInit()
        {
            UIUtil.AddClick(_img_close, OnClickClose);
            if (_list_location != null) _list_location.gameObject.SetActive(false);
            if (_list_tab != null) _list_tab.gameObject.SetActive(false);
        }

        protected override void OnShow(object args)
        {
            BuildList();
        }

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
            IReadOnlyList<LoginServerInfo> servers = LoginModel.Instance.Servers;

            for (int i = 0; i < servers.Count; i++)
            {
                LoginServerInfo server = servers[i];
                GameObject item = Instantiate(_tpl_LoginSelectServerItem, content);
                item.SetActive(true);
                RectTransform rect = (RectTransform)item.transform;
                rect.anchoredPosition = new Vector2(0f, -i * (itemHeight + ITEM_SPACING));

                BindItem(item, server);
                _items.Add(item);
            }

            content.sizeDelta = new Vector2(content.sizeDelta.x,
                servers.Count * (itemHeight + ITEM_SPACING));
        }

        private void BindItem(GameObject item, LoginServerInfo server)
        {
            Transform nameNode = item.transform.Find("_lb_server_name");
            TextMeshProUGUI nameLabel = nameNode != null ? nameNode.GetComponent<TextMeshProUGUI>() : null;
            if (nameLabel != null)
            {
                nameLabel.text = server.DisplayName;
            }
            Transform levelNode = item.transform.Find("_lb_level");
            TextMeshProUGUI levelLabel = levelNode != null ? levelNode.GetComponent<TextMeshProUGUI>() : null;
            if (levelLabel != null)
            {
                levelLabel.text = server.level > 0 ? "LV." + server.level : string.Empty;
            }

            Transform bgNode = item.transform.Find("_img_bg");
            Image bg = bgNode != null ? bgNode.GetComponent<Image>() : null;
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
    }
}
