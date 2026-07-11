using System.Collections.Generic;
using Shenxiao.Generated.UI.Setting;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Setting
{
    /// <summary>
    /// 改头像子窗(对标老客户端 setting/SettingChangeHeadView.ts):
    /// 头像滚动列表(scroll,克隆模板 _tpl_SettingHeadItem)+ 确定按钮(okBtn)+ 关闭(_close_btn)。
    ///
    /// 协议链(轮5 接线):开窗即发 13080(RoleController.RequestHeadList,对标老端 LoadSuccess Fire(...,13080));
    /// 回包/13081 推送到达 → EVT_ROLE_HEAD_LIST_UPDATE → 重铺列表(读 RoleModel.HeadIdList,13080 只回"已解锁"
    /// 列表,不含未解锁候选——老端还需 config_picture 配全量候选表,该表在本端未同步/未加载,故列表只显示
    /// 真实已解锁项,不臆造"锁灰态"候选;差异见类注释)。点 item 选中(互斥高亮)写 _selectedHeadId;
    /// okBtn 用 _selectedHeadId 发 13083 换头像,成功后 EVT_ROLE_HEAD_SET_SUCCESS 关窗。
    ///
    /// 降级:config_picture(全量头像候选表,含未解锁项/职业过滤/自定义头像上传占位格)未同步 → 列表仅显示
    /// RoleModel.HeadIdList 里真实已解锁的项;13081(激活头像)的客户端"发送"侧对标老端仅绑在废弃的
    /// "自定义头像上传"(13082)成功回调,该半成品不移植,故本窗无"激活"按钮,只被动刷新推送到达的新解锁项。
    /// </summary>
    public sealed class SettingChangeHeadView : SettingChangeHeadViewBind
    {
        private readonly List<SettingHeadItem> _items = new List<SettingHeadItem>();
        private int _selectedHeadId;
        private bool _subscribed;

        protected override void OnInit()
        {
            HideTemplates();
            BindClose(_close_btn);
            BindOk();
            Subscribe();
        }

        protected override void OnDispose() => Unsubscribe();

        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_ROLE_HEAD_LIST_UPDATE, OnHeadListUpdate);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_HEAD_SET_SUCCESS, OnSetSuccess);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_HEAD_LIST_UPDATE, OnHeadListUpdate);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_HEAD_SET_SUCCESS, OnSetSuccess);
            _subscribed = false;
        }

        protected override void OnShow(object args)
        {
            _selectedHeadId = 0;
            RoleController.Instance.RequestHeadList(); // 13080(对标老端 LoadSuccess 开窗时拉)
            RebuildList();
        }

        /// <summary>头像列表模板(老端 SettingHeadItem)先隐藏,克隆时点亮。</summary>
        private void HideTemplates()
        {
            if (_tpl_SettingHeadItem != null) _tpl_SettingHeadItem.SetActive(false);
        }

        private void OnHeadListUpdate()
        {
            if (IsShown) RebuildList();
        }

        private void OnSetSuccess()
        {
            if (IsShown) Hide();
        }

        private void RebuildList()
        {
            if (_tpl_SettingHeadItem == null || scroll == null || scroll.content == null) return;
            RectTransform content = scroll.content;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);

            IReadOnlyList<int> ids = RoleModel.Instance.HeadIdList;
            const float ItemHeight = 100f;

            while (_items.Count < ids.Count)
            {
                GameObject go = Instantiate(_tpl_SettingHeadItem, content);
                go.name = "SettingHeadItem_" + _items.Count;
                SettingHeadItem item = go.GetComponent<SettingHeadItem>();
                if (item == null) item = go.GetComponentInChildren<SettingHeadItem>(true);
                if (item == null) { Destroy(go); break; }
                item.Show();
                _items.Add(item);
            }

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] == null) continue;
                bool active = i < ids.Count;
                _items[i].gameObject.SetActive(active);
                if (!active) continue;

                int id = ids[i];
                _items[i].SetData(id, "头像" + id, OnItemClick);
                _items[i].SetSelected(id == _selectedHeadId);

                var rt = (RectTransform)_items[i].transform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(0f, -i * ItemHeight);
            }

            content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(ids.Count * ItemHeight, 0f));
        }

        private void OnItemClick(int id)
        {
            _selectedHeadId = id;
            RebuildList(); // 数量小(实测≤数十),整表重刷选中态足够,不额外维护增量高亮状态机
        }

        private void BindOk()
        {
            Image img = okBtn != null ? okBtn.GetComponentInChildren<Image>(true) : null;
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () =>
            {
                if (_selectedHeadId <= 0)
                {
                    TipsManager.Toast("请先选择头像");
                    return;
                }
                RoleController.Instance.SetHead(_selectedHeadId);
            });
        }

        private void BindClose(Component target)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, Hide);
        }
    }
}
