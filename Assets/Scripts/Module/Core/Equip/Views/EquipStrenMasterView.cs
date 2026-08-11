using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Generated.UI.Equip;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 天殒淬炉大师/全身强化总览(对标老客户端 equip/EquipStrenMasterView.ts):标题 + 当前效果组(group_cur/Content1)+
    /// 下一阶效果组(group_next/Content)+ 等级进度(lb_stren1/2/3)+ 激活按钮(btn_active/lb_active)+ 激活红点(img_redAc)+ 关闭。
    /// 老端 open 后请求协议 15261、监听 UPDATE_MASTER_VIEW 刷新两段属性列表(EquipMasterItem/EquipNextMasterItem),
    /// 激活点 15260;等级取 EquipModel.GetAllStrenLv/GetMasterNextLv,满阶时 cur_tip="已满阶"、按钮置灰。
    ///
    /// 全身奖励协议(15260/15261,经 EquipStrenController,自动循环 轮4 队列#4)已接线:OnShow → QueryWholeAward()
    /// (对标老端 LoadSuccess 发 15261);btn_active 必须在当前/下一阶条件与属性列表形成可验证展示后才可发送，
    /// 当前明确阻断而不依赖服务端兜底；btn_close → 真关闭(Hide())。
    /// 降级:EquipModel 两段属性列表(EquipMasterItem/EquipNextMasterItem)、WordManager 均未移植 →
    /// 激活红点(img_redAc)隐藏、属性模板(_tpl_EquipMasterItem)隐藏、列表空、等级进度文本默认降级。
    /// 事件驱动窗口,默认关闭、不进 FirstPass。
    /// </summary>
    public sealed class EquipStrenMasterView : EquipStrenMasterViewBind
    {
        [SerializeField] private ScrollRect _nextScroll;

        private readonly List<EquipMasterItem> _currentItems = new List<EquipMasterItem>();
        private readonly List<EquipMasterItem> _nextItems = new List<EquipMasterItem>();
        private bool _subscribed;
        private bool _canActivate;
        private bool _maxLevel;
        private int _epoch;

        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            ++_epoch;
            Subscribe();
            EquipStrenController.Instance.QueryWholeAward();
            _ = EnsureConfigAndRefreshAsync(_epoch);
        }

        protected override void OnHide()
        {
            ++_epoch;
            Unsubscribe();
            HideItems(_currentItems);
            HideItems(_nextItems);
            if (Content1 != null) { Content1.StopMovement(); Content1.verticalNormalizedPosition = 1f; }
            if (_nextScroll != null) { _nextScroll.StopMovement(); _nextScroll.verticalNormalizedPosition = 1f; }
        }

        protected override void OnDispose()
        {
            Unsubscribe();
            _currentItems.Clear();
            _nextItems.Clear();
            base.OnDispose();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_EQUIP_STREN_UPDATE, RefreshView);
            EventDispatcher.On(GlobalEvent.EVT_EQUIP_WHOLE_UPDATE, RefreshView);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_EQUIP_STREN_UPDATE, RefreshView);
            EventDispatcher.Off(GlobalEvent.EVT_EQUIP_WHOLE_UPDATE, RefreshView);
            _subscribed = false;
        }

        private async Task EnsureConfigAndRefreshAsync(int epoch)
        {
            await EquipConfigs.EnsureLoaded();
            if (this == null || !IsShown || epoch != _epoch) return;
            RefreshView();
        }

        private void RefreshView()
        {
            int progress = EquipStrenView.TotalStrengthLevel();
            int activated = EquipWholeAwardModel.Instance.GetWholeLv(1);
            EquipConfigs.GetWholeRewardPair(1, activated, out bool hasCurrent, out EquipConfigs.WholeReward current,
                out bool hasNext, out EquipConfigs.WholeReward next);

            _maxLevel = !hasNext;
            _canActivate = hasNext && progress >= next.NeedLevel;
            if (lb_stren1 != null) lb_stren1.text = "全身强化";
            if (lb_stren2 != null)
            {
                lb_stren2.text = hasNext ? progress.ToString() : string.Empty;
                lb_stren2.color = _canActivate ? new Color32(10, 149, 62, 255) : new Color32(255, 79, 80, 255);
            }
            if (lb_stren3 != null) lb_stren3.text = hasNext ? ("/" + next.NeedLevel) : progress.ToString();
            if (cur_tip != null) cur_tip.text = hasNext ? "当前效果" : "已满阶";
            if (lb_active != null) lb_active.text = _maxLevel ? "已满阶" : "激活";
            if (btn_active != null) btn_active.color = _canActivate ? Color.white : new Color32(150, 150, 150, 255);
            if (img_redAc != null) img_redAc.gameObject.SetActive(_canActivate);
            if (group_cur != null) group_cur.gameObject.SetActive(hasCurrent);
            if (group_next != null) group_next.gameObject.SetActive(hasNext);

            RenderReward(current.Attributes, hasCurrent, Content1 != null ? Content1.content : null, _currentItems);
            RenderReward(next.Attributes, hasNext, _nextScroll != null ? _nextScroll.content : null, _nextItems);

            if (transform is RectTransform rt)
            {
                Vector2 size = rt.sizeDelta;
                size.y = hasCurrent && hasNext ? 607f : 377f;
                rt.sizeDelta = size;
            }
        }

        private void RenderReward(IReadOnlyList<EquipConfigs.StrengthAttribute> attrs, bool visible,
            RectTransform parent, List<EquipMasterItem> items)
        {
            int count = visible ? (attrs?.Count ?? 0) : 0;
            if (_tpl_EquipMasterItem != null && parent != null)
            {
                while (items.Count < count)
                {
                    GameObject go = Instantiate(_tpl_EquipMasterItem, parent, false);
                    go.name = "EquipMasterItem_Runtime_" + (items.Count + 1);
                    go.SetActive(false);
                    EquipMasterItem item = go.GetComponent<EquipMasterItem>();
                    if (item == null) { Destroy(go); break; }
                    item.Show();
                    items.Add(item);
                }
            }
            for (int i = 0; i < items.Count; i++)
            {
                EquipMasterItem item = items[i];
                if (i >= count)
                {
                    if (item.IsShown) item.Hide();
                    continue;
                }
                if (!item.IsShown) item.Show();
                EquipConfigs.StrengthAttribute attr = attrs[i];
                string name = attr.AttrId == 0 ? string.Empty : GoodsModel.GetAttrName(attr.AttrId);
                string value = attr.AttrId == 0
                    ? ("+" + (attr.PerLevelValue / 100d).ToString("0.##") + "%")
                    : ("+" + GoodsModel.FormatAttrValue(attr.AttrId, attr.PerLevelValue));
                item.SetData(name, value);
            }
        }

        private static void HideItems(List<EquipMasterItem> items)
        {
            foreach (EquipMasterItem item in items)
                if (item != null && item.IsShown) item.Hide();
        }

        private void HideReds()
        {
            // img_redAc:激活红点(cur_lv >= next_lv 时亮),降级先隐藏。
            HideNode(img_redAc);
        }

        private void HideTemplates()
        {
            if (_tpl_EquipMasterItem != null) _tpl_EquipMasterItem.SetActive(false);
        }

        private void BindButtons()
        {
            BindClick(btn_active, () =>
            {
                if (_maxLevel) return;
                if (!_canActivate)
                {
                    TipsManager.Toast("当前天殒淬炉等级不足，无法激活");
                    return;
                }
                EquipStrenController.Instance.ActivateWhole(1);
            });
            BindClick(btn_close, () =>
            {
                GameLog.Info("Equip", "点击[关闭] → Hide()");
                Hide();
            });
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击回调。</summary>
        private void BindClick(Component target, System.Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }

        private static void HideNode(Component c)
        {
            if (c != null) c.gameObject.SetActive(false);
        }
    }
}
