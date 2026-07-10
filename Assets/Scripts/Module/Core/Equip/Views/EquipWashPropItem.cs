using System;
using Shenxiao.Common.Tips;
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
    /// 协议(自动循环 轮4 队列#4)已接线:_gp_btn_close → EquipWashController.OpenSlot(15212,equip_type 由
    /// <see cref="SetEquipType"/> 设置);等级门槛先查 EquipConfigs.TryGetWashUnlockLv,缺表(本轮实际状态)则不拦截、
    /// 直接发送、log 记录(对标规格 §0 末条)。_gp_lock → EquipWashModel.ToggleLock 本地切换锁定态(老端同样纯本地
    /// 状态、不发协议,15213 发送时才读取)。
    /// 降级:EquipModel(洗魄属性/解锁配置完整表)、RoleManager 货币、ResManager 图集均未移植 → 红点隐藏;
    /// SetData 仅按「是否已开启」切换 open_group/close_group 显隐 + 属性文本占位,属性区间/进度/钻石消耗刷新待接。
    /// 列表项,由洗魄面板克隆(本轮父面板暂未铺格,故本项当前无实例被创建;协议/状态wiring已就绪待接)。
    /// </summary>
    public sealed class EquipWashPropItem : EquipWashPropItemBind
    {
        /// <summary>当前槽位下标(对标老端 this.index)。</summary>
        private int _index;
        /// <summary>该槽所属装备部位(对标老端外层 cur_equip_info_.equip_type);0 = 未指定。</summary>
        private int _equipType;

        protected override void OnInit()
        {
            // 红点依赖 EquipModel 解锁/可开启判断(未移植)→ 先隐藏。
            if (red_dot != null) red_dot.gameObject.SetActive(false);

            // _gp_btn_close → 15212 开启洗魄槽:先查等级门槛(缺表则不拦截,直接发)。
            BindBtn(_gp_btn_close, () =>
            {
                if (_equipType == 0)
                {
                    GameLog.Info("Equip", "洗魄槽[{0}] 未设置部位,跳过", _index);
                    return;
                }
                if (EquipConfigs.TryGetWashUnlockLv(_index, out int needLv))
                {
                    int lv = Shenxiao.Module.Core.Role.RoleModel.Instance.Level;
                    if (lv < needLv)
                    {
                        TipsManager.Toast("等级不足," + needLv + "级解锁");
                        return;
                    }
                }
                else
                {
                    GameLog.Info("Equip", "config_equip_wash_unlock_lv 缺表,不拦截,直接发送(服务端兜底)");
                }
                EquipWashController.Instance.OpenSlot(_equipType, _index);
            });
            // _gp_lock → 切换该槽锁定态(纯本地状态,15213 发送时读取,不发协议)。
            BindBtn(_gp_lock, () =>
            {
                if (_equipType == 0) return;
                EquipWashModel.Instance.ToggleLock(_equipType, _index);
                GameLog.Info("Equip", "切换洗魄槽锁定 equip_type={0} index={1}", _equipType, _index);
            });
        }

        /// <summary>设置该槽所属装备部位(由父面板铺格时挂;协议/锁定操作均需要它)。</summary>
        public void SetEquipType(int equipType) => _equipType = equipType;

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

        /// <summary>给按钮(Image 或含 Image 子节点的容器)挂点击回调。</summary>
        private void BindBtn(Component target, Action onClick)
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
