using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Compose
{
    /// <summary>
    /// 神装合成协议控制器(对标老端 CompositeController.ts:107 OnCompositeEquipBtn WriteBegin(15020);
    /// 服务端 pt_150 段内 15020,规则 type==2 成功 → red_equip_combine +1)。解主线 101725(ctype73)。
    /// </summary>
    public sealed class ComposeController : BaseController
    {
        public static readonly ComposeController Instance = new ComposeController();

        private ComposeController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.GOODS_COMPOSE, On15020);
        }

        public override void Dispose()
        {
            ComposeModel.Instance.Clear();
            base.Dispose();
        }

        /// <summary>
        /// 发起合成(对标 CompositeController.ts:107 OnCompositeEquipBtn):
        /// WriteFMT("i", rule_id) + WriteFMT("h", regulars.length) + 逐 WriteFMT("l", goods_id) +
        /// WriteFMT("h", specifies.length) + 逐 WriteFMT("l", goods_id)。fmt 串按材料数动态拼接
        /// ("i" + "h" + n×"l" + "h" + m×"l"),regulars/specifies 允许为空表(对应 h count=0)。
        /// </summary>
        public void Compose(int ruleId, List<long> regulars, List<long> specifies)
        {
            if (ruleId <= 0) return;
            regulars ??= new List<long>();
            specifies ??= new List<long>();

            var fmt = new System.Text.StringBuilder();
            fmt.Append('i').Append('h');
            for (int i = 0; i < regulars.Count; i++) fmt.Append('l');
            fmt.Append('h');
            for (int i = 0; i < specifies.Count; i++) fmt.Append('l');

            var args = new List<object> { ruleId, regulars.Count };
            foreach (long v in regulars) args.Add(v);
            args.Add(specifies.Count);
            foreach (long v in specifies) args.Add(v);

            SendFmt(Proto.GOODS_COMPOSE, fmt.ToString(), args.ToArray());
            GameLog.Info("Compose", "compose 15020 rule_id={0} regulars={1} specifies={2} fmt={3}",
                ruleId, regulars.Count, specifies.Count, fmt);
        }

        /// <summary>15020 合成结果:code:i, compose_type:c, rule_id:i, goods_id:l。code==1 成功。</summary>
        private void On15020(NetReader r)
        {
            int code = (int)r.ReadU32();        // code:i
            int composeType = r.ReadU8();        // compose_type:c
            int ruleId = (int)r.ReadU32();       // rule_id:i
            long goodsId = r.ReadU64();          // goods_id:l

            ComposeModel.Instance.Apply15020(code, composeType, ruleId, goodsId);

            if (code != 1)
            {
                TipsManager.Toast("合成失败(" + code + ")");   // 错误码表(Util.ErrorCodeShow)未移植,显码降级
                GameLog.Info("Compose", "15020 compose fail code={0} rule_id={1}", code, ruleId);
                return;
            }

            TipsManager.Toast("合成成功");
            GameLog.Info("Compose", "15020 compose ok compose_type={0} rule_id={1} goods_id={2} remaining={3}B",
                composeType, ruleId, goodsId, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_COMPOSE_UPDATE);
        }
    }
}
