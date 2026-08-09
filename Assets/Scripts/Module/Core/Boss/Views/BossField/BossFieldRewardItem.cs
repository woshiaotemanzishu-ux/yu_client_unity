using Shenxiao.Generated.UI.BossField;

namespace Shenxiao.Module.Core.Boss.Views.BossField
{
    /// <summary>奖励格 Prefab 接管点；具体 EquipmentItem 数据由宿主在配置闭包就绪后注入。</summary>
    public sealed class BossFieldRewardItem : BossFieldRewardItemBind
    {
        protected override void OnInit()
        {
            if (_tpl_EquipmentItem != null) _tpl_EquipmentItem.SetActive(false);
            if (_img_tag != null) _img_tag.gameObject.SetActive(false);
        }

        public void SetUpTag(bool visible)
        { if (_img_tag != null) _img_tag.gameObject.SetActive(visible); }
    }
}
