using System.Collections.Generic;

namespace Shenxiao.Module.Core.FirstRecharge
{
    /// <summary>
    /// 首充数据（对标老客户端 FirstRechargeController / yu_server pt_159 的 15905/15906/15908 子集）。
    /// 只存数据；首充面板/红点 UI 待用户验收（订阅事件读这里）。
    /// </summary>
    public sealed class FirstRechargeModel
    {
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
    }
}
