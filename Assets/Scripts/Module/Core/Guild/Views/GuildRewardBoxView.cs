using System.Collections.Generic;
using System.Linq;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Guild;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Guild.Views
{
    /// <summary>
    /// 结社宝箱页(对标老客户端 guild/GuildRewardBoxView.ts;40301 界面+40302 领取+40303/304/305 推送):
    /// `_list_item`→<see cref="GuildRBItem"/>(send_list,主列表)、`_list_content`→<see cref="GuildRBRecordItem"/>
    /// (log,日志流)——字段配对已用老端 .ts 源码 GetComponents 交叉核实,勿按名称直觉颠倒。
    /// **降级**:`_btn_send`(GuildRBSendView 规则说明弹窗)/`_instruction`(通用说明弹窗)未移植,TODO。
    /// </summary>
    public sealed class GuildRewardBoxView : GuildRewardBoxViewBind
    {
        private readonly List<GuildRBItem> _itemRows = new List<GuildRBItem>();
        private readonly List<GuildRBRecordItem> _logRows = new List<GuildRBRecordItem>();

        protected override void OnInit()
        {
            if (_tpl_GuildRBItem != null) _tpl_GuildRBItem.SetActive(false);
            if (_tpl_GuildRBRecordItem != null) _tpl_GuildRBRecordItem.SetActive(false);
            if (_red_dot != null) _red_dot.gameObject.SetActive(false);
            BindClick(_btn_get, () => GuildController.Instance.ReceiveBox(0)); // auto_id=0=一键领取(对标老端)
            BindClick(_btn_send, () => GameLog.Info("Guild", "点击说明 → GuildRBSendView 未移植(40301数据链已通),TODO"));
            BindClick(_instruction, () => GameLog.Info("Guild", "点击通用说明弹窗未移植,TODO"));
        }

        protected override void OnShow(object args)
        {
            EventDispatcher.On(GlobalEvent.EVT_GUILD_BOX_UPDATE, Refresh);
            GuildController.Instance.RequestBoxInfo(); // 对标老端 LoadSuccess:每次打开发 40301
            if (_Label1 != null) _Label1.text = "暂时还没有仙宗宝箱，冒险者快去完成宝箱任务吧~";
            Refresh();
        }

        protected override void OnHide()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GUILD_BOX_UPDATE, Refresh);
        }

        private void Refresh()
        {
            UpdateBox();
            UpdateRecord();
            UpdateDesc();
        }

        /// <summary>对标老端 updateBox 排序:已领取(status==1)优先,同类按 time 降序。</summary>
        private void UpdateBox()
        {
            if (_tpl_GuildRBItem == null || _list_item == null || _list_item.content == null) return;
            var list = new List<GuildModel.BoxSendEntry>(GuildModel.Instance.BoxSendList);
            list.Sort((a, b) =>
            {
                int ai = a.Status == 1 ? 8 : 10;
                int bi = b.Status == 1 ? 8 : 10;
                if (a.Time != b.Time) { if (a.Time > b.Time) ai--; else bi--; }
                return ai - bi;
            });

            bool empty = list.Count == 0;
            if (_gp_null != null) _gp_null.gameObject.SetActive(empty);
            _list_item.gameObject.SetActive(!empty);

            EnsureItemRows(list.Count);
            for (int i = 0; i < _itemRows.Count; i++)
            {
                bool active = i < list.Count;
                _itemRows[i].gameObject.SetActive(active);
                if (active) _itemRows[i].SetData(list[i]);
            }
        }

        private void UpdateRecord()
        {
            if (_tpl_GuildRBRecordItem == null || _list_content == null || _list_content.content == null) return;
            IReadOnlyList<GuildModel.BoxLogEntry> list = GuildModel.Instance.BoxLog;
            EnsureLogRows(list.Count);
            for (int i = 0; i < _logRows.Count; i++)
            {
                bool active = i < list.Count;
                _logRows[i].gameObject.SetActive(active);
                if (active) _logRows[i].SetData(list[i]);
            }
        }

        private void UpdateDesc()
        {
            if (_Label2 != null) _Label2.text = "今日可领取宝箱：";
            if (_lb_next_attr != null) _lb_next_attr.text = GuildModel.Instance.BoxNum + "/" + GuildModel.Instance.BoxMaxNum;
            if (_red_dot != null)
                _red_dot.gameObject.SetActive(GuildModel.Instance.BoxSendList.Any(e => e.Status != 1) && GuildModel.Instance.BoxNum < GuildModel.Instance.BoxMaxNum);
        }

        private void EnsureItemRows(int count)
        {
            while (_itemRows.Count < count)
            {
                GameObject go = Object.Instantiate(_tpl_GuildRBItem, _list_item.content);
                go.SetActive(true);
                GuildRBItem item = go.GetComponent<GuildRBItem>();
                if (item != null) _itemRows.Add(item);
                else break;
            }
        }

        private void EnsureLogRows(int count)
        {
            while (_logRows.Count < count)
            {
                GameObject go = Object.Instantiate(_tpl_GuildRBRecordItem, _list_content.content);
                go.SetActive(true);
                GuildRBRecordItem item = go.GetComponent<GuildRBRecordItem>();
                if (item != null) _logRows.Add(item);
                else break;
            }
        }

        private static void BindClick(UnityEngine.Component target, System.Action onClick)
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
