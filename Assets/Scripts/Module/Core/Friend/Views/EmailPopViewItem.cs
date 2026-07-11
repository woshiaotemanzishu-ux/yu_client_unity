using System.Threading.Tasks;
using Shenxiao.Framework.Res;
using Shenxiao.Generated.UI.Friend;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Mail;
using UnityEngine;

namespace Shenxiao.Module.Core.Friend
{
    /// <summary>
    /// 邮件详情附件格(对标老客户端 friend/EmailPopViewItem.ts):运行时实例化一个 common/EquipmentItem 展示真实
    /// 图标+数量(经 GoodsModel.GetMappingTypeId 把 object_type/type_id 换算成真实 goods_id,对标
    /// SettingView.RefreshHeadIcon 同款 Addressable 单件实例化路径)。由 <see cref="EmailPopView"/> 克隆
    /// <see cref="EmailPopViewBind._tpl_EmailPopViewItem"/> 铺横向奖励列表。
    ///
    /// 简化:老端对"堆叠货币(max_overlap==0 且 type==10)"会把一条附件拆成多个 num=1 图标,本轮不拆分——
    /// 单图标 + 数量角标即可完整表达获得量,总获得数不受影响(TODO 如需逐字节对齐可再拆)。
    /// 已领取(is_receive)变灰遮罩未接(本工程 EquipmentItem 本身未移植该覆盖层)。
    /// </summary>
    public sealed class EmailPopViewItem : EmailPopViewItemBind
    {
        private EquipmentItem _item;
        private bool _loading;

        protected override void OnInit() { }

        public void SetData(MailAttachment attachment)
        {
            if (attachment == null || _gp == null) return;
            (int goodsId, int _) = GoodsModel.GetMappingTypeId(attachment.ObjectType, attachment.TypeId);
            _ = ApplyAsync(goodsId, attachment.Num);
        }

        private async Task ApplyAsync(int goodsId, long num)
        {
            if (_item == null)
            {
                if (_loading) return;
                _loading = true;
                GameObject go = await ResManager.InstantiateAsync(GameResPath.GetUIPrefab("common", "EquipmentItem"), _gp);
                _loading = false;
                if (go == null || _gp == null) return;
                go.name = "EquipmentItem";
                _item = go.GetComponent<EquipmentItem>();
            }
            if (_item == null) return;
            _item.Show();
            _item.SetScale(0.75f);
            _item.SetData(goodsId, num);
        }
    }
}
