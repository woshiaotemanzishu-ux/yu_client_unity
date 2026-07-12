using Newtonsoft.Json.Linq;
using Shenxiao.Generated.UI.Daily;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Daily
{
    /// <summary>
    /// 每日任务·行项(对标老端 daily/DailyTaskItem.ts):标题(title,config_ac.ac_name)+ 描述(desc_txt,进度)+
    /// 活跃度值(huoyue)+ 领取(getBtn,活跃度可领时显)/完成(doneBtn)/前往(goBtn)。由 DailyTaskView 按
    /// 15701(act_type=1)列表克隆填充。降级:奖励图标预览(reward_list)/条件文案(condition/open_time)/
    /// 破晓标记(destiny_img/corner_type)未接线,先留空(TODO)。
    /// </summary>
    public sealed class DailyTaskItem : DailyTaskItemBind
    {
        private int _module, _moduleSub;

        protected override void OnInit()
        {
            BindClick(getBtn, () => DailyController.Instance.ClaimTaskLiveness(_module, _moduleSub));
            BindClick(goBtn, () => GameLog.Info("Daily", "点击[每日任务·前往] module={0}@{1} → 待对接(jump_id 跳转映射表未移植)", _module, _moduleSub));
        }

        /// <summary>填一条每日任务(对标 DailyTaskItem.SetData)。</summary>
        public void SetData(DailyModel.ActivityVo vo)
        {
            _module = vo.Module;
            _moduleSub = vo.ModuleSub;
            JObject cfg = DailyConfigs.GetAc(vo.Module, vo.ModuleSub, vo.AcSub);
            string name = cfg != null ? DailyConfigs.ReadString(cfg, "ac_name") : "";
            if (title != null) title.text = string.IsNullOrEmpty(name) ? "活动" + vo.Module : name;
            if (desc_txt != null) desc_txt.text = "已完成 " + vo.Num + "/" + vo.MaxNum;
            if (huoyue != null) huoyue.text = "+" + vo.Live + "活跃度";

            bool canGet = vo.CanGetLive > 0;
            bool done = !canGet && vo.Num >= vo.MaxNum;
            if (getBtn != null) getBtn.gameObject.SetActive(canGet);
            if (doneBtn != null) doneBtn.gameObject.SetActive(done);
            if (goBtn != null) goBtn.gameObject.SetActive(!canGet && !done);
            if (get_btn_dot != null) get_btn_dot.gameObject.SetActive(canGet);
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
