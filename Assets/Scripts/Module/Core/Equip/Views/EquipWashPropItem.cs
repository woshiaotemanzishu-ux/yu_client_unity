using System;
using Shenxiao.Generated.UI.Equip;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 洗魄槽项(对标老客户端 equipWash/EquipWashPropItem.ts):单个装备洗魄槽,两态切换 ——
    /// 已开启(open_group:属性 prop_label + 进度 _img_pregross + 锁定态 _gp_lock/lock_img/unlock_img/_lb_lock)
    /// 与 未开启(close_group + 开启按钮 _gp_btn_close + 钻石消耗 diamond_img/diamond_label + 红点 red_dot)。
    ///
    /// 降级:EquipModel(洗魄属性/锁定字典/解锁配置)、RoleManager(等级/货币)、ResManager(图集)、
    /// config_equip_wash_*(解锁等级/属性区间/消耗)均未移植 → 红点隐藏;开启/锁定两个容器点击打日志「待对接」;
    /// SetData 仅按「是否已开启」切换 open_group/close_group 显隐 + 属性文本占位,属性区间/进度/钻石消耗/锁定刷新待接。
    /// 列表项,由洗魄面板克隆。
    /// </summary>
    public sealed class EquipWashPropItem : EquipWashPropItemBind
    {
        /// <summary>当前槽位下标(对标老端 this.index)。</summary>
        private int _index;

        protected override void OnInit()
        {
            // 红点依赖 EquipModel 解锁/可开启判断(未移植)→ 先隐藏。
            if (red_dot != null) red_dot.gameObject.SetActive(false);

            // 老端 InitEvent:_gp_btn_close 走 15212 开启洗魄槽协议;_gp_lock 切槽位锁定/解锁。降级打日志。
            BindBtn(_gp_btn_close, "开启洗魄槽(协议15212)");
            BindBtn(_gp_lock, "锁定/解锁槽位");
        }

        /// <summary>
        /// 填洗魄槽数据(对标 SetData(cur_equip_info, index)):无数据则不动;有数据按「该 index 是否已洗出属性」
        /// 切换已开启/未开启两态。降级:属性区间/进度/钻石消耗/锁定刷新依赖 EquipModel + 配置(未移植),仅占位。
        /// </summary>
        public void SetData(int index, bool opened, string propText)
        {
            _index = index;

            if (open_group != null) open_group.gameObject.SetActive(opened);
            if (close_group != null) close_group.gameObject.SetActive(!opened);
            // 老端:未开启态才显开启按钮容器。
            if (_gp_btn_close != null) _gp_btn_close.gameObject.SetActive(!opened);

            if (opened)
            {
                // 属性文本(对标 RefreshPropLabel)。颜色/数值百分比/区间进度依赖 WordManager/config,降级直填。
                if (prop_label != null) prop_label.text = propText ?? "";
            }
            else
            {
                // 钻石消耗/红点依赖解锁配置(未移植)→ 钻石隐藏、红点隐藏。
                if (diamond_img != null) diamond_img.gameObject.SetActive(false);
                if (red_dot != null) red_dot.gameObject.SetActive(false);
            }
        }

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击 → 打日志(降级:协议/逻辑待对接)。</summary>
        private void BindBtn(Component target, string label)
        {
            if (target == null) return;
            Image img = target as Image;
            if (img == null) img = target.GetComponentInChildren<Image>(true);
            if (img == null) return;
            img.raycastTarget = true;
            UIUtil.AddClick(img, () => GameLog.Info("Equip", "点击洗魄槽[{0}] → 待对接", label));
        }
    }
}
