using Newtonsoft.Json.Linq;
using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 限时活动·行项(对标老端 daily/DailyLimitActivityItem.ts):标题(title)+ 开放时间(time)+
    /// 预约(goBtn,15719)/已结束(endBtn)/领取(gotBtn,15720)。由 DailyLimitActivityView 按
    /// 15701(act_type=2)+ 预约状态表(15718/19/20)列表克隆填充。老端夹带的微信订阅检查不移植。
    /// </summary>
    public sealed class DailyLimitActivityItem : DailyLimitActivityItemBind
    {
        private int _module, _moduleSub, _acSub;

        protected override void OnInit()
        {
            BindClick(goBtn, OnClickReserve);
            BindClick(gotBtn, () => DailyController.Instance.ClaimSignUpReward(_module, _moduleSub, _acSub));
        }

        /// <summary>对标老端 DailyLimitActivityItem.ts:74-82:module==621(诸天王者)预约后顺手报名 62102
        /// (Kf1vnEvent.KF1VN_REQUEST_PROTO)。Kf1vnController 目前只移植了 62101,62102 未接入——按规格
        /// 裁决"在场才调,否则 log"降级为日志占位,待 Kf1vn 62102 接入后按老端条件(stage==1 &amp;&amp; 未报名)补真调用。</summary>
        private void OnClickReserve()
        {
            DailyController.Instance.SignUp(_module, _moduleSub, _acSub);
            if (_module == 621)
                GameLog.Info("Daily", "module==621(诸天王者) 预约联动 62102 待 Kf1vn 模块接入后按老端条件补发,当前仅记录");
        }

        /// <summary>填一条限时活动(对标 DailyLimitActivityItem.SetData)。reservationStatus:null=不在预约表内
        /// (该活动不走预约流程,如非 ac_type=2 的项);0=未预约,1=已预约待领,2=已领。</summary>
        public void SetData(DailyModel.ActivityVo vo, int? reservationStatus)
        {
            _module = vo.Module; _moduleSub = vo.ModuleSub; _acSub = vo.AcSub;
            JObject cfg = DailyConfigs.GetAc(vo.Module, vo.ModuleSub, vo.AcSub);
            string name = cfg != null ? DailyConfigs.ReadString(cfg, "ac_name") : "";
            if (title != null) title.text = string.IsNullOrEmpty(name) ? "活动" + vo.Module : name;
            if (huoyue != null) huoyue.text = "+" + vo.Live + "活跃度";
            if (time != null)
            {
                var region = DailyConfigs.ParseTimeRegion(DailyConfigs.ReadString(cfg, "time_region"));
                time.text = region.Count > 0
                    ? string.Format("{0:00}:{1:00}-{2:00}:{3:00}", region[0].startH, region[0].startM, region[0].endH, region[0].endM)
                    : "";
            }

            bool closed = vo.State == 2;
            bool canClaim = reservationStatus.HasValue && reservationStatus.Value == 1 && closed;
            bool canReserve = !closed && (!reservationStatus.HasValue || reservationStatus.Value == 0);
            if (goBtn != null) goBtn.gameObject.SetActive(canReserve);
            if (gotBtn != null) gotBtn.gameObject.SetActive(canClaim);
            if (endBtn != null) endBtn.gameObject.SetActive(closed && !canClaim);
            if (end_tag != null) end_tag.gameObject.SetActive(closed);
            if (resRed != null) resRed.gameObject.SetActive(canReserve || canClaim);
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
