using Shenxiao.Generated.UI.MainUI;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 主界面聊天/系统消息条 + 设置/好友/商城入口(对标老客户端 MainUIChatView.ts:LoadSuccess + GetComponents)。
    /// 本轮只还原源码支持的静态/默认态,不造假消息与图标:
    /// - 关掉两个滚动面板的滚动条(对标 GetComponents 末尾的 _panel_chat/_panel_sys.vScrollBar.visible = false)。
    /// - 好友红点 _img_friend_red、商城红点 _img_shop_red、限购商城特效盒 _box_shop_effect 在拿到数据/特效前隐藏
    ///   (老客户端由 MainUIModel.friend_red / ShopModel 红点 / ActivityIcon 特效驱动可见性,这些模型尚未移植)。
    /// 待移植不在本轮:
    /// - 聊天/系统消息项(MainUIChatItem)依赖 ChatModel 真数据,不造假消息。
    /// - 强化活动图标 ActivityIcon("158")依赖 ActivityIconManager(CreateStrengthenIcon),留给活动图标切片。
    /// </summary>
    public sealed class MainUIChatView : MainUIChatViewBind
    {
        private const string WelcomeSystemMessage = "欢迎踏入九州大荒。神霄崩灭后，天殒遗骸化作道痕，秘境引劫而生——愿君融痕证道，历尽九天梯劫！";
        private ActivityIcon _strengthenIcon;
        private MainUIChatItemBind _welcomeSysItem;

        protected override void OnInit()
        {
            HideScrollBars();
            HideUnbackedIndicators();
            HideTemplates();
            CreateWelcomeSystemMessage();
            CreateStrengthenIcon();
            WireHudEntries();
        }

        /// <summary>
        /// 接 HUD 聊天条上的入口按钮,经 MainUIRouter 解耦打开对应面板(各模块 Bootstrap 注册 key,MainUI 不直接依赖它们):
        /// - 聊天底图 _img_bg → "chat"(对标老端点 _panel_chat/_panel_sys 区域 Fire OPEN_CHAT_VIEW);点击热区精度归预制体。
        /// - 设置按钮 _img_setting → "setting"(对标老端 SettingBtn → SettingView)。
        /// 好友/商城(_img_friend/_img_shop)待对应模块移植后在此追加 RouteClick。
        /// </summary>
        private void WireHudEntries()
        {
            RouteClick(_img_bg, "chat");
            RouteClick(_img_setting, "setting");
        }

        private static void RouteClick(Image target, string viewKey)
        {
            if (target == null) return;
            target.raycastTarget = true;
            UIUtil.AddClick(target, () => MainUIRouter.Open(viewKey));
        }

        /// <summary>
        /// 对标 GetComponents: this._panel_chat.vScrollBar.visible = false; this._panel_sys.vScrollBar.visible = false。
        /// Unity 的 ScrollRect 滚动条是可选挂载,存在才隐藏(水平/垂直都关,匹配老客户端不显滚动条)。
        /// </summary>
        private void HideScrollBars()
        {
            HideScrollBars(_panel_chat);
            HideScrollBars(_panel_sys);
        }

        private static void HideScrollBars(ScrollRect scroll)
        {
            if (scroll == null)
            {
                return;
            }
            if (scroll.verticalScrollbar != null)
            {
                scroll.verticalScrollbar.gameObject.SetActive(false);
            }
            if (scroll.horizontalScrollbar != null)
            {
                scroll.horizontalScrollbar.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 红点/特效未移植:好友红点、商城红点、限购商城特效盒先隐藏(老客户端由 MainUIModel.friend_red、
        /// ShopModel.UpdateShopRedState、ActivityIcon 特效驱动)。不造假数据,沿用 gameObject.SetActive(false)。
        /// </summary>
        private void HideUnbackedIndicators()
        {
            _img_friend_red.gameObject.SetActive(false);
            _img_shop_red.gameObject.SetActive(false);
            _box_shop_effect.gameObject.SetActive(false);
        }

        private void HideTemplates()
        {
            if (_tpl_MainUIChatItem != null) _tpl_MainUIChatItem.SetActive(false);
            if (_tpl_ActivityIcon != null) _tpl_ActivityIcon.SetActive(false);
        }

        /// <summary>
        /// Old-client ChatModel.WelcomeChat inserts one client-side SYSTEM message on GAME_START.
        /// This is the first bottom-center/system strip content and does not depend on server chat data.
        /// </summary>
        private void CreateWelcomeSystemMessage()
        {
            if (_welcomeSysItem != null)
            {
                _welcomeSysItem.gameObject.SetActive(true);
                return;
            }

            if (_tpl_MainUIChatItem == null || _box_sys_con == null)
            {
                GameLog.Error("MainUI", "MainUIChatView missing MainUIChatItem template or _box_sys_con");
                return;
            }

            GameObject go = Instantiate(_tpl_MainUIChatItem, _box_sys_con);
            go.SetActive(true);

            MainUIChatItemBind item = go.GetComponent<MainUIChatItemBind>();
            if (item == null)
            {
                GameLog.Error("MainUI", "MainUIChatItem template missing bind component");
                Destroy(go);
                return;
            }

            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new UnityEngine.Vector2(0f, 1f);
            rt.pivot = new UnityEngine.Vector2(0f, 1f);
            rt.anchoredPosition = UnityEngine.Vector2.zero;

            if (item.title != null) item.title.gameObject.SetActive(false);
            if (item._img_trumpet != null) item._img_trumpet.gameObject.SetActive(false);
            if (item.titleBg != null)
            {
                _ = ResManager.SetImageAsync(item.titleBg, GameResPath.GetIcon("mainUI", "mainUI_chat_1"), nativeSize: false);
            }
            if (item.contentLabel != null)
            {
                item.contentLabel.text = WelcomeSystemMessage;
                item.contentLabel.richText = true;
            }

            _welcomeSysItem = item;
        }

        /// <summary>
        /// 对标 yu_client MainUIChatView.ts:CreateStrengthenIcon。
        /// 强化入口是 ActivityIcon("158") 挂在 _box_strengthen,不是 ActivityView 的列表图标。
        /// </summary>
        private void CreateStrengthenIcon()
        {
            if (_strengthenIcon != null)
            {
                return;
            }

            if (_tpl_ActivityIcon == null || _box_strengthen == null)
            {
                GameLog.Error("MainUI", "MainUIChatView missing ActivityIcon template or _box_strengthen");
                return;
            }

            GameObject go = Instantiate(_tpl_ActivityIcon, _box_strengthen);
            go.SetActive(true);

            ActivityIcon icon = go.GetComponent<ActivityIcon>();
            if (icon == null)
            {
                GameLog.Error("MainUI", "MainUIChatView ActivityIcon template is not rebound to business script");
                Destroy(go);
                return;
            }

            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new UnityEngine.Vector2(0.5f, 0.5f);
            rt.pivot = new UnityEngine.Vector2(0.5f, 0.5f);
            rt.anchoredPosition = UnityEngine.Vector2.zero;

            icon.Show();
            icon.SetIconType("158");
            _strengthenIcon = icon;
        }
    }
}
