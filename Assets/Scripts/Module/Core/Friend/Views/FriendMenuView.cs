using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Generated.UI.Friend;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 好友/黑名单右键交互菜单(对标老客户端 friend/FriendMenuView.ts):按目标关系(rela)动态生成按钮列表——
    /// rela==1(好友):查看信息/开始聊天/赠礼给TA/删除好友/拉入黑名单;rela==3或4(黑名单):查看信息/解除黑名单;
    /// 其余(陌生人):查看信息/加为好友/赠礼给TA/拉入黑名单。打开即发 14010(菜单数据,自身节流)。
    ///
    /// "赠礼给TA"(MarriageFlowerView)/"举报头像"(CustomRoleHead)两按钮功能未移植,
    /// 点击仅日志降级。
    /// </summary>
    public sealed class FriendMenuView : FriendMenuViewBind
    {
        public sealed class OpenArgs
        {
            public FriendModel.FriendVo Vo;
            public Vector2 ScreenPos;
        }

        private FriendModel.FriendVo _vo;
        private Vector2 _screenPos;
        private readonly List<GameObject> _buttons = new List<GameObject>();
        private bool _subscribed;

        protected override void OnInit() { }

        protected override void OnShow(object args)
        {
            if (args is OpenArgs oa)
            {
                _vo = oa.Vo;
                _screenPos = oa.ScreenPos;
            }
            if (_vo == null) { Hide(); return; }

            Subscribe();
            FriendController.Instance.RequestMenuData(_vo.RoleId);
            RefreshView();
        }

        protected override void OnHide()
        {
            Unsubscribe();
            ClearButtons();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            EventDispatcher.On<long>(GlobalEvent.EVT_FRIEND_MENU_UPDATE, OnMenuUpdate);
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            EventDispatcher.Off<long>(GlobalEvent.EVT_FRIEND_MENU_UPDATE, OnMenuUpdate);
        }

        private void OnMenuUpdate(long roleId)
        {
            if (_vo != null && roleId == _vo.RoleId) RefreshView();
        }

        private void RefreshView()
        {
            if (_vo == null) return;
            FriendModel.MenuInfo info = FriendModel.Instance.GetMenuData(_vo.RoleId);
            if (info == null) return;

            List<string> labels = new List<string>();
            if (info.Rela == 1) labels.AddRange(new[] { "查看信息", "开始聊天", "赠礼给TA", "删除好友", "拉入黑名单" });
            else if (info.Rela == 3 || info.Rela == 4) labels.AddRange(new[] { "查看信息", "解除黑名单" });
            else labels.AddRange(new[] { "查看信息", "加为好友", "赠礼给TA", "拉入黑名单" });

            // 老端仅当 picture 是该角色上传头像(role_id)时追加“举报头像”。普通系统头像不出现。
            if (long.TryParse(_vo.Picture, out long pictureRoleId) && pictureRoleId == _vo.RoleId)
                labels.Add("举报头像");

            BuildButtons(labels);
        }

        private void BuildButtons(List<string> labels)
        {
            ClearButtons();
            if (btnGroup == null) return;
            foreach (string label in labels)
            {
                var go = new GameObject("Btn_" + label, typeof(RectTransform));
                go.transform.SetParent(btnGroup, false);
                var rt = go.transform as RectTransform;
                if (rt != null) rt.sizeDelta = new Vector2(149, 60);

                Image img = go.AddComponent<Image>();
                bool isDanger = label == "拉入黑名单" || label == "解除黑名单" || label == "删除好友";
                img.color = Color.white;
                _ = ResManager.SetImageAsync(
                    img,
                    GameResPath.GetIcon("common", isDanger ? "com_rect_btn3" : "com_rect_btn1"),
                    nativeSize: false);

                var textGo = new GameObject("Label", typeof(RectTransform));
                textGo.transform.SetParent(go.transform, false);
                var textRt = textGo.transform as RectTransform;
                if (textRt != null) { textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one; textRt.offsetMin = Vector2.zero; textRt.offsetMax = Vector2.zero; }
                TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
                text.text = label;
                text.fontSize = 24;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;

                UIUtil.AddClick(img, () => OnClickButton(label));
                _buttons.Add(go);
            }

            UpdateMenuGeometry(labels.Count);
        }

        /// <summary>
        /// 对标老端 FriendMenuView.initView/updateView：菜单落在触发条目的屏幕位置，背景高度
        /// 按“按钮数 * 60 + 16”伸缩。先换算到 Window 父容器局部坐标，避免 CanvasScaler
        /// 下直接把屏幕像素写入 anchoredPosition。
        /// </summary>
        private void UpdateMenuGeometry(int buttonCount)
        {
            float height = buttonCount * 60f + 16f;
            if (img_bg != null)
                img_bg.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            RectTransform rect = transform as RectTransform;
            RectTransform parent = rect != null ? rect.parent as RectTransform : null;
            if (rect == null || parent == null) return;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, _screenPos, camera, out Vector2 localPoint))
                rect.anchoredPosition = localPoint;
        }

        private void ClearButtons()
        {
            foreach (GameObject go in _buttons) Object.Destroy(go);
            _buttons.Clear();
        }

        private void OnClickButton(string label)
        {
            if (_vo == null) { Hide(); return; }
            switch (label)
            {
                case "查看信息":
                    Shenxiao.Module.Core.LookOver.LookOverFlow.Show(_vo.RoleId);
                    break;
                case "加为好友":
                    FriendController.Instance.AddFriendApply(_vo.RoleId);
                    break;
                case "拉入黑名单":
                    FriendController.Instance.FriendsOperate(2, _vo.RoleId);
                    break;
                case "解除黑名单":
                    FriendController.Instance.FriendsOperate(3, _vo.RoleId);
                    FriendController.Instance.RequestMenuData(_vo.RoleId);
                    break;
                case "开始聊天":
                    FriendFlow.OpenChat(_vo.RoleId, _vo.Name, _vo.Career, _vo.Turn, _vo.Lv);
                    break;
                case "删除好友":
                    long roleId = _vo.RoleId;
                    string name = _vo.Name;
                    ConfirmDialog.Show("您确定要删除好友" + name + "吗？", () => FriendController.Instance.FriendsOperate(1, roleId), null);
                    break;
                case "赠礼给TA":
                    GameLog.Info("Friend", "菜单[赠礼给TA] → MarriageFlowerView 未移植,TODO");
                    break;
                case "举报头像":
                    GameLog.Info("Friend", "菜单[举报头像] → CustomRoleHead 举报未移植,TODO");
                    break;
            }
            Hide();
        }
    }
}
