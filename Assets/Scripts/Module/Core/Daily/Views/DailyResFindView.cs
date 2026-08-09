using System.Collections.Generic;
using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using Shenxiao.Common.Tips;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 每日·资源找回页(对标老客户端 daily/DailyResFindView.ts,DailyView 标签3 内容):
    /// 资源找回列表(scroll/Content 克隆 DailyResFindItem)+ 空提示(none_conta/tips)+ 一键找回(receive_gp,41904)。
    ///
    /// checkBtn0/checkBtn1 对应 type=1 绑玉与 type=2 免费，默认免费；切换同步排序、行次数和确认类型。
    /// 单条与一键写入均先走确认框，付费分支明确提示绑玉不足时可能消耗勾玉。config_res_act 尚未导入，
    /// 因而精确价格与奖励预览仍是运行收口 blocker。
    /// </summary>
    public sealed class DailyResFindView : DailyResFindViewBind
    {
        private readonly List<GameObject> _cells = new List<GameObject>();
        private bool _subscribed;
        private int _moneyType = 2;

        protected override void OnInit()
        {
            if (_tpl_DailyResFindItem != null) _tpl_DailyResFindItem.SetActive(false);
            BindClick(checkBtn0, () => SelectMoneyType(1));
            BindClick(checkBtn1, () => SelectMoneyType(2));
            BindClick(receive_gp, ConfirmOneKey);
            UpdateCheckState();
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
            List<DailyModel.ResFindVo> list = DailyModel.Instance.ResFindList == null
                ? new List<DailyModel.ResFindVo>()
                : new List<DailyModel.ResFindVo>(DailyModel.Instance.ResFindList);
            list.Sort((a, b) =>
            {
                int aTimes = _moneyType == 1 ? a.Lefttimes + a.LefttimesVip : a.Lefttimes;
                int bTimes = _moneyType == 1 ? b.Lefttimes + b.LefttimesVip : b.Lefttimes;
                int times = bTimes.CompareTo(aTimes);
                return times != 0 ? times : a.ActId.CompareTo(b.ActId);
            });
            bool empty = list == null || list.Count == 0;
            if (none_conta != null) none_conta.gameObject.SetActive(empty);
            if (checkBtn0 != null) checkBtn0.gameObject.SetActive(!empty);
            if (checkBtn1 != null) checkBtn1.gameObject.SetActive(!empty);
            if (receive_gp != null) receive_gp.gameObject.SetActive(!empty);
            if (!empty && _tpl_DailyResFindItem != null && Content != null)
            {
                foreach (DailyModel.ResFindVo vo in list)
                {
                    GameObject cellGo = Object.Instantiate(_tpl_DailyResFindItem, Content);
                    cellGo.SetActive(true);
                    DailyResFindItem item = cellGo.GetComponent<DailyResFindItem>();
                    if (item != null)
                    {
                        item.Show();
                        item.SetData(vo, _moneyType);
                    }
                    _cells.Add(cellGo);
                }
            }
            GameLog.Info("Daily", "资源找回列表刷新 count={0}", list?.Count ?? 0);
        }

        private void SelectMoneyType(int type)
        {
            if (_moneyType == type) return;
            _moneyType = type;
            UpdateCheckState();
            Refresh();
        }

        private void UpdateCheckState()
        {
            if (check_img0 != null) check_img0.color = _moneyType == 1 ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            if (check_img1 != null) check_img1.color = _moneyType == 2 ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }

        private void ConfirmOneKey()
        {
            string text = _moneyType == 1
                ? "是否使用绑玉一键找回所有奖励？\n（绑玉不足时可能消耗勾玉代替）"
                : "是否一键免费找回所有奖励？";
            TipsManager.Confirm(text, () => DailyController.Instance.ResFindOneKey(_moneyType));
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
