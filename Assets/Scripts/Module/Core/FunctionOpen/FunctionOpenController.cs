using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.FunctionOpen
{
    /// <summary>
    /// 功能开放达成奖励协议（对标老客户端 FunctionOpenController / yu_server pt_138）：
    ///   13800 已完成功能列表（h + {Id:h, State:c}×N）；
    ///   13801 领奖结果（Code:i, Id:h, State:c）。
    /// 解析落 <see cref="FunctionOpenModel"/> 并发 EVT_FUNC_OPEN_UPDATE；红点/领取 UI 待用户验收。
    /// </summary>
    public sealed class FunctionOpenController : BaseController
    {
        public static readonly FunctionOpenController Instance = new FunctionOpenController();
        private FunctionOpenController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.FUNC_OPEN_LIST, On13800);
            RegisterProtocal(Proto.FUNC_OPEN_CLAIM, On13801);
        }

        /// <summary>请求已完成功能列表（无参，回 13800）。</summary>
        public void RequestList() => SendFmt(Proto.FUNC_OPEN_LIST);

        /// <summary>领取某功能开放奖励（C2S "hc"：id, state）。</summary>
        public void Claim(int id, int state) => SendFmt(Proto.FUNC_OPEN_CLAIM, "hc", id, state);

        private void On13800(NetReader r)
        {
            int count = r.ReadU16();
            var map = new Dictionary<int, int>(count);
            for (int i = 0; i < count; i++)
            {
                int id = r.ReadU16();
                int state = r.ReadU8();
                map[id] = state;
            }
            FunctionOpenModel.Instance.SetAll(map);
            GameLog.Info("FuncOpen", "13800 已完成功能列表: {0} 项", count);
            EventDispatcher.Emit(GlobalEvent.EVT_FUNC_OPEN_UPDATE);
        }

        private void On13801(NetReader r)
        {
            int code = (int)r.ReadU32();
            int id = r.ReadU16();
            int state = r.ReadU8();
            if (code == 1)
            {
                FunctionOpenModel.Instance.SetState(id, state);
                GameLog.Info("FuncOpen", "13801 领奖成功 id={0} state={1}", id, state);
                EventDispatcher.Emit(GlobalEvent.EVT_FUNC_OPEN_UPDATE);
            }
            else
            {
                GameLog.Warn("FuncOpen", "13801 领奖失败 code={0} id={1}", code, id);
            }
        }

    }
}
