using Newtonsoft.Json.Linq;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.Guild;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Guild.Views
{
    /// <summary>
    /// 宝箱送达条目(对标老客户端 guild/GuildRBItem.ts):任务名+类型+发放者+倒计时/已过期 + 领取按钮(40302)。
    /// **降级**:奖励图标(_gp_reward,老端克隆 BaseAwardItem)——`BaseAwardItemBind` 组件在
    /// `Assets/Prefabs/UI/Common/BaseAwardItem.prefab` 上尚未回填(第6轮已知阻断,见
    /// <see cref="Shenxiao.Module.Core.Common.BaseAwardItem"/> 类注释),本轮不重复踩坑,仅隐藏 _gp_reward,
    /// 奖励信息暂不做图标展示(TODO,待该 prefab 回填后接线)。红点(_red)本轮无统一红点系统,不接。</summary>
    public sealed class GuildRBItem : GuildRBItemBind
    {
        private GuildModel.BoxSendEntry _data;

        protected override void OnInit()
        {
            if (_gp_reward != null) _gp_reward.gameObject.SetActive(false); // TODO:奖励图标,见类注释
            if (_red != null) _red.gameObject.SetActive(false);
            BindClick(_btn, OnClickReceive);
        }

        /// <summary>status:对标老端 GuildRBItem.SetData——status==1 = **已领取**,其余 = 可领取。</summary>
        public void SetData(GuildModel.BoxSendEntry data)
        {
            _data = data;
            if (data == null) return;

            Newtonsoft.Json.Linq.JObject cfg = GuildConfigs.GetDailyTask(data.TaskId);
            string taskText = cfg?["task"]?.ToString() ?? ("任务" + data.TaskId);
            string typeName = cfg?["name"]?.ToString() ?? "";
            if (_lb_title != null) _lb_title.text = taskText;
            if (_lb_type != null) _lb_type.text = typeName + "任务";

            bool claimed = data.Status == 1;
            if (_lb_role != null) _lb_role.text = "发放者：" + data.RoleName;
            if (labelDisplay != null) labelDisplay.text = claimed ? "已领取" : "领取";
            // 对标老端 Util.SetImageGray(this._Image1, status==1):按钮保留可见,仅置灰(不隐藏整个 _btn——
            // 隐藏会连"已领取"文案一起吞掉,老端从未这么做)。SetImageGray 灰阶滤镜未移植,降级为颜色 tint。
            if (_Image1 != null) _Image1.color = claimed ? new Color(0.6f, 0.6f, 0.6f, 1f) : Color.white;

            long persistSec = (cfg?["persist"]?.Value<long>() ?? 4) * 3600L;
            long endTime = data.Time + persistSec;
            long remain = endTime - TimeUtil.NowSec();
            if (_lb_time != null)
            {
                _lb_time.gameObject.SetActive(!claimed);
                _lb_time.text = !claimed ? (remain > 0 ? FormatRemain(remain) : "已过期") : "";
            }
        }

        private static string FormatRemain(long sec)
        {
            long h = sec / 3600; long m = (sec % 3600) / 60; long s = sec % 60;
            return string.Format("{0:00}:{1:00}:{2:00}", h, m, s);
        }

        private void OnClickReceive()
        {
            if (_data == null) return;
            GuildController.Instance.ReceiveBox(_data.AutoId);
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
