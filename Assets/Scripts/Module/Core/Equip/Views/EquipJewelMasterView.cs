using Shenxiao.Common.Tips;
using Shenxiao.Generated.UI.Jewel;
using Shenxiao.Framework.Util;
using Shenxiao.Framework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.Equip
{
    /// <summary>
    /// 骸珀镶嵌大师/宝石全身总览(对标老客户端 jewel/EquipJewelMasterView.ts):标题 + 当前效果组(group_cur/Content1)+
    /// 下一阶效果组(group_next/Content)+ 等级进度(lb_stren1/2/3)+ 激活按钮(btn_active/lb_active)+ 激活红点(img_redAc)+ 关闭。
    ///
    /// **Bind 家族核查**:<see cref="Shenxiao.Generated.UI.Jewel.EquipJewelMasterViewBind"/> 与 4a 的
    /// <see cref="Shenxiao.Generated.UI.Equip.EquipStrenMasterViewBind"/> 结构高度同构(group_cur/group_next/
    /// btn_active/btn_close/gp_stren/lb_stren1-3/_tpl_EquipMasterItem 字段一一对应,仅少数字段改名:
    /// bg→img_bg、_lb_title→lb_title(且多了 img_title)、cur_tip→lb_cur、_Label1→lb_next),**但两者是不同命名
    /// 空间下各自独立生成的 partial 类,C# 不支持从两个不同基类继承同一份逻辑**——故本类是独立文件,不是
    /// EquipStrenMasterView 的子类/复用;取舍:逻辑体走同一套(照抄 <see cref="EquipStrenMasterView"/> 结构),
    /// 仅把 type 参数从 1(强化)换成 3(宝石,对标 EquipDefine.JEWEL_WHOLE_TYPE=3),两处 Model/Controller
    /// 底层复用同一个 <see cref="EquipStrenController"/>(15260/15261)与 <see cref="EquipWholeAwardModel"/>
    /// (type→whole_lv 分桶天然不冲突)。
    ///
    /// 由 <see cref="EquipJewelView"/>.btnMaster 打开(EquipFlow.OpenSub("EquipJewelMasterView"));
    /// 降级同 EquipStrenMasterView:EquipModel 两段属性列表/WordManager 均未移植 → 激活红点隐藏、属性模板隐藏、
    /// 列表空、等级进度文本默认降级。协议查询(15261)已接真。
    /// </summary>
    public sealed class EquipJewelMasterView : EquipJewelMasterViewBind
    {
        /// <summary>对标老端 EquipDefine.JEWEL_WHOLE_TYPE=3(宝石全身奖励)。</summary>
        private const int JewelWholeType = 3;

        protected override void OnInit()
        {
            HideReds();
            HideTemplates();
            BindButtons();
        }

        protected override void OnShow(object args)
        {
            GameLog.Info("Equip", "EquipJewelMasterView 打开 → 请求 15261(列表渲染/等级进度仍默认降级)");
            EquipStrenController.Instance.QueryWholeAward();
        }

        private void HideReds()
        {
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
                TipsManager.Toast("镶嵌大师条件配置未就绪");
                GameLog.Warn("Equip", "点击[激活](type={0})被阻止：当前/下一阶条件与属性列表尚未形成可验证展示",
                    JewelWholeType);
            });
            BindClick(btn_close, () =>
            {
                GameLog.Info("Equip", "点击[关闭] → Hide()");
                Hide();
            });
        }

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
