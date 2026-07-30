using System;
using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 装备熔炼协议控制器(薄增量六件套第20轮工单;服务端 pt_152 段内 15024/15025)。
    /// 解主线 101285(ctype18「熔炼等级」,状态快照自动判定,无需专用任务代码)。
    /// 发包字节序对标老端 GoodsController.ts:1211-1222 OnDevourEquipment WriteBegin(15025):
    /// WriteFMT("h",count) + 循环 WriteFMT("l",goods_id) + WriteFMT("i",goods_num) —— 同 <see cref="BagController.SellGoods"/> 动态 fmt 写法。
    /// </summary>
    public sealed class BagFusionController : BaseController
    {
        public static readonly BagFusionController Instance = new BagFusionController();
#if UNITY_EDITOR
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private BagFusionController() { }

        /// <summary>当前熔炼等级(15024 落库,对标老端 SmeltModel;界面/主线判定读此静态值)。</summary>
        public static int FusionLv { get; private set; }

        /// <summary>当前熔炼经验(15024 落库)。</summary>
        public static long FusionExp { get; private set; }

        private double _pendingUntil;
        public bool IsPending => UnityEngine.Time.realtimeSinceStartupAsDouble < _pendingUntil;

        protected override void Register()
        {
            RegisterProtocal(Proto.BAG_FUSION_INFO, On15024);
            RegisterProtocal(Proto.BAG_FUSION, On15025);
        }

        public override void Dispose()
        {
            _pendingUntil = 0d;
            base.Dispose();
        }

        /// <summary>15024 查询熔炼信息(无参)。</summary>
        public void RequestInfo()
        {
            SendRequest(Proto.BAG_FUSION_INFO);
            GameLog.Info("Bag", "fusion requestInfo 15024");
        }

        /// <summary>15025 熔炼(发 h count + 逐项 l goods_id/i num,对标 OnDevourEquipment WriteBegin(15025)写法,
        /// 同 <see cref="BagController.SellGoods"/> 动态 fmt 构造)。</summary>
        public bool Fuse(IReadOnlyList<(long goodsId, long num)> list)
        {
            if (list == null || list.Count == 0) return false;
            if (IsPending)
            {
                TipsManager.Toast("吞噬请求处理中");
                return false;
            }
            var fmt = new StringBuilder("h");
            var args = new List<object>(1 + list.Count * 2) { list.Count };
            foreach ((long goodsId, long num) it in list)
            {
                fmt.Append("li");
                args.Add(it.goodsId);
                args.Add(it.num);
            }
            _pendingUntil = UnityEngine.Time.realtimeSinceStartupAsDouble + 10d;
            SendRequest(Proto.BAG_FUSION, fmt.ToString(), args.ToArray());
            GameLog.Info("Bag", "fuse 15025 items={0}", list.Count);
            return true;
        }

        private void SendRequest(int protocol, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(protocol, format, args);
            if (s_outboundIntercept != null && s_outboundIntercept(frame)) return;
#endif
            if (string.IsNullOrEmpty(format)) SendFmt(protocol);
            else SendFmt(protocol, format, args);
        }

        /// <summary>15024 回包:level:h, exp:i。落静态 FusionLv/FusionExp + EVT_BAG_UPDATE(复用背包更新事件联动刷新)。</summary>
        private void On15024(NetReader r)
        {
            int level = r.ReadU16();
            long exp = r.ReadU32();
            FusionLv = level;
            FusionExp = exp;
            GameLog.Info("Bag", "15024 fusion level={0} exp={1} remaining={2}B", level, exp, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_BAG_UPDATE);
        }

        /// <summary>15025 回包:code:i + exp_list[u16×{add_exp:h, ratio:c}]。code==1 → toast「熔炼成功」
        /// (随后服务端另推 15024 落最新等级/经验);else 显码降级(错误码表未移植)。</summary>
        private void On15025(NetReader r)
        {
            _pendingUntil = 0d;
            int code = (int)r.ReadU32();
            List<(int addExp, int ratio)> expList = r.ReadArray(ReadExp);
            if (code != 1)
            {
                TipsManager.Toast("熔炼失败(" + code + ")");   // 错误码表未移植,显码降级
                GameLog.Info("Bag", "15025 fail code={0}", code);
                return;
            }
            TipsManager.Toast("熔炼成功");
            EventDispatcher.Emit(GlobalEvent.EVT_BAG_FUSION_SUCCESS);
            GameLog.Info("Bag", "15025 ok exp_list={0} remaining={1}B", expList.Count, r.Remaining);
        }

        private static (int addExp, int ratio) ReadExp(NetReader r)
        {
            return (r.ReadU16(), r.ReadU8());   // {add_exp:h, ratio:c}
        }
    }
}
