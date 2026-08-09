using Shenxiao.Common.Tips;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.LevelReward;
using Shenxiao.Module.Core.RushGift;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.Module.Core.LevelReward
{
    /// <summary>
    /// 等级奖励行项。received 状态来自 41700；本轮禁止发送 41701，点击只记录 blocked。
    /// 奖励格和限量分母依赖尚未公开的配置契约，保持隐藏而不猜测。
    /// </summary>
    public sealed class LevelRewardItem : LevelRewardItemBind
    {
        private RushGiftModel.GiftVo _data;

        protected override void OnInit()
        {
            if (_tpl_ArrowComponent != null) _tpl_ArrowComponent.SetActive(false);
            if (_lb_left_count != null) _lb_left_count.gameObject.SetActive(false);
            if (_gp_item_con != null) _gp_item_con.gameObject.SetActive(false);
            BindClaimSurface(_img_get);
        }

        public void SetData(RushGiftModel.GiftVo data)
        {
            _data = data;
            if (data == null) return;

            if (_lb_lv != null) _lb_lv.text = data.Lv.ToString();
            SetNode(_img_red, data.Received == 1);
            SetNode(_img_received, data.Received == 2);
            SetNode(_gp_get, data.Received != 2);
            if (data.Received != 2)
                SetClaimGray(data.Received == 0 || data.Received == 3 || data.Received == 4);

            if (_lb_get != null)
            {
                switch (data.Received)
                {
                    case 0: _lb_get.text = "未达到"; break;
                    case 1: _lb_get.text = "领取"; break;
                    case 2: _lb_get.text = "已领取"; break;
                    case 3:
                    case 4: _lb_get.text = "领取"; break;
                    default: _lb_get.text = "不可领取"; break;
                }
            }
        }

        private void OnClaimClicked()
        {
            if (_data == null) return;
            switch (_data.Received)
            {
                case 0:
                    TipsManager.Toast("条件不足");
                    return;
                case 1:
                    GameLog.Warn("LevelReward",
                        "blocked: 41701 未发送，领取属于未授权写事务。lv={0} received=1", _data.Lv);
                    return;
                case 2:
                    TipsManager.Toast("已领取～");
                    return;
                case 3:
                    return;
                case 4:
                    TipsManager.Toast("已被领完~");
                    return;
                default:
                    GameLog.Warn("LevelReward", "未知 received 状态，未发送协议。lv={0} received={1}",
                        _data.Lv, _data.Received);
                    return;
            }
        }

        private void BindClaimSurface(Component target)
        {
            if (target == null) return;
            Image image = target as Image;
            if (image == null) image = target.GetComponentInChildren<Image>(true);
            if (image == null) return;
            image.raycastTarget = true;
            UIUtil.AddClick(image, OnClaimClicked);
        }

        private void SetClaimGray(bool gray)
        {
            if (_gp_get == null) return;
            foreach (Graphic graphic in _gp_get.GetComponentsInChildren<Graphic>(true))
                UIGrayStyle.Apply(graphic, gray);
        }

        private static void SetNode(Component node, bool visible)
        {
            if (node != null) node.gameObject.SetActive(visible);
        }
    }
}
