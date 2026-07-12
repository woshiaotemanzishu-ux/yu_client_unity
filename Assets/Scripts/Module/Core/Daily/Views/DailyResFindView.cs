using System.Collections.Generic;
using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 每日·资源找回页(对标老客户端 daily/DailyResFindView.ts,DailyView 标签3 内容):
    /// 资源找回列表(scroll/Content 克隆 DailyResFindItem)+ 空提示(none_conta/tips)+ 一键找回(receive_gp,41904)。
    ///
    /// 降级:checkBtn0/checkBtn1(老端按 money_type/资源类型筛选)仍打日志(筛选逻辑未接线,TODO);
    /// receive_gp 按 type=2(金币/免费,老端默认 money_type)一键找回——⚠轮10交叉验收 blocker 订正:此前误
    /// 固定 type=1(绑钻/付费),会在无任何确认的情况下直接扣绑钻,与老端默认行为相反。type=1 付费路径待
    /// config_res_act 导入后再开且需二次确认(TODO)。
    /// </summary>
    public sealed class DailyResFindView : DailyResFindViewBind
    {
        private readonly List<GameObject> _cells = new List<GameObject>();
        private bool _subscribed;

        protected override void OnInit()
        {
            if (_tpl_DailyResFindItem != null) _tpl_DailyResFindItem.SetActive(false);
            BindBtn(checkBtn0, "资源找回·筛选0");
            BindBtn(checkBtn1, "资源找回·筛选1");
            BindClick(receive_gp, () => DailyController.Instance.ResFindOneKey(2));
        }

        protected override void OnShow(object args)
        {
            Subscribe();
            DailyController.Instance.RequestResFindInfo(); // 对标 DailyResFindView.LoadSuccess 每次开页再拉一次
            Refresh();
        }

        protected override void OnHide() => Unsubscribe();
        protected override void OnDispose() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed) return;
            EventDispatcher.On(GlobalEvent.EVT_DAILY_RES_FIND_UPDATE, Refresh);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            EventDispatcher.Off(GlobalEvent.EVT_DAILY_RES_FIND_UPDATE, Refresh);
            _subscribed = false;
        }

        private void Refresh()
        {
            foreach (GameObject go in _cells) if (go != null) Object.Destroy(go);
            _cells.Clear();
            List<DailyModel.ResFindVo> list = DailyModel.Instance.ResFindList;
            bool empty = list == null || list.Count == 0;
            if (none_conta != null) none_conta.gameObject.SetActive(empty);
            if (!empty && _tpl_DailyResFindItem != null && Content != null)
            {
                foreach (DailyModel.ResFindVo vo in list)
                {
                    GameObject cellGo = Object.Instantiate(_tpl_DailyResFindItem, Content);
                    cellGo.SetActive(true);
                    DailyResFindItem item = cellGo.GetComponent<DailyResFindItem>();
                    if (item != null) item.SetData(vo);
                    _cells.Add(cellGo);
                }
            }
            GameLog.Info("Daily", "资源找回列表刷新 count={0}", list?.Count ?? 0);
        }

        private static void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Daily", "点击[{0}] → 待对接", label));
        }

        private static void BindClick(Component target, System.Action onClick)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, onClick);
        }
    }
}
