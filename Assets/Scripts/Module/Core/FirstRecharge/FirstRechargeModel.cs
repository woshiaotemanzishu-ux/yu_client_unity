using System.Collections.Generic;

namespace Shenxiao.Module.Core.FirstRecharge
{
    /// <summary>
    /// 首充数据（对标老客户端 FirstRechargeController / yu_server pt_159 的 15905/15906/15908 子集）。
    /// 只存数据；首充面板/红点 UI 待用户验收（订阅事件读这里）。
    /// </summary>
    public sealed class FirstRechargeModel
    {
        public enum MainIconPresentation
        {
            Hidden,
            NewRoleBanner,
            StandardIcon,
        }

        // 老端 FirstRechargeController.CheckShowBubble 的权威展示窗：create_role_time + 30 分钟。
        // 服务端 15905 只下发 IsNotify，不下发持续时长；这里保留协议配套语义，不把它伪装成活动配置。
        private const long NewRoleBannerDurationSeconds = 30L * 60L;

        public static readonly FirstRechargeModel Instance = new FirstRechargeModel();
        private FirstRechargeModel() { }

        public struct Slot
        {
            public int Open;   // 该档位是否开放(0/1)
            public int Index;  // 档位序号(领取用)
            public Slot(int open, int index) { Open = open; Index = index; }
        }

        /// <summary>首充各档位（15905）。</summary>
        public readonly List<Slot> Slots = new List<Slot>();
        public int ProductId;
        public bool IsNotify;
        public bool IsBuy;     // 是否已购首充(15908)

        public void SetInfo(List<Slot> slots, int productId, bool isNotify)
        {
            Slots.Clear();
            if (slots != null) Slots.AddRange(slots);
            ProductId = productId;
            IsNotify = isNotify;
        }

        public void Clear()
        {
            Slots.Clear();
            ProductId = 0;
            IsNotify = false;
            IsBuy = false;
        }

        public bool IsDoneFirstRecharge()
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                int open = Slots[i].Open;
                if (open != 0 && open != 5) return true;
            }
            return IsBuy;
        }

        public bool IsDoneAllReward()
        {
            if (Slots.Count == 0) return false;
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].Open != 4) return false;
            }
            return true;
        }

        public bool IsNoFirstRecharge()
        {
            if (Slots.Count == 0) return !IsBuy;
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].Open == 0) return true;
            }
            return false;
        }

        public bool HasClaimableReward()
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].Open == 1) return true;
            }
            return false;
        }

        public bool HasTomorrowReward()
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].Open == 2) return true;
            }
            return false;
        }

        public bool ShouldShowMainIcon()
        {
            return Slots.Count > 0 && !IsDoneAllReward();
        }

        public long GetNewRoleBannerEndTime(long registerTime)
        {
            return registerTime > 0 ? registerTime + NewRoleBannerDurationSeconds : 0;
        }

        /// <summary>
        /// 对标老端 FirstRechargeController.CheckShowBubble：
        /// 已通知/已充值显示普通 159 图标；未充值的新角色在创角后 30 分钟内显示横幅，超时切普通图标。
        /// </summary>
        public MainIconPresentation ResolveMainIconPresentation(long nowSec, bool roleInfoReady, long registerTime)
        {
            if (!ShouldShowMainIcon()) return MainIconPresentation.Hidden;
            if (IsNotify || IsDoneFirstRecharge()) return MainIconPresentation.StandardIcon;
            if (!roleInfoReady || registerTime <= 0) return MainIconPresentation.Hidden;
            return nowSec < GetNewRoleBannerEndTime(registerTime)
                ? MainIconPresentation.NewRoleBanner
                : MainIconPresentation.StandardIcon;
        }
    }
}
