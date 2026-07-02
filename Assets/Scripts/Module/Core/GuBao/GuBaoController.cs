using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.GuBao
{
    /// <summary>
    /// 古宝(妖物结界守护 soap)协议控制器(对标老端 commonController/MonsterController.ts;服务端 pt_133)。
    /// 进游戏(EVT_GAME_START)发 13320 求全量状态;13321 激活单个碎片(发 "hh" soap_id,debris_id)。
    /// 主线 100811(ctype89)=soap 10001「幽瞳」全部 2 片碎片(消耗 1105010011/1105010012,刷图掉落)激活。
    /// 老端锚点:MonsterMainView.ts:173-174(发包)、MonsterController.ts(13320/13321)。
    /// </summary>
    public sealed class GuBaoController : BaseController
    {
        public static readonly GuBaoController Instance = new GuBaoController();

        private GuBaoController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.GUBAO_INFO, On13320);
            RegisterProtocal(Proto.GUBAO_ACTIVE, On13321);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            GuBaoModel.Instance.Clear();
            base.Dispose();
        }

        private async void OnGameStart()
        {
            await GuBaoConfigs.EnsureLoaded();
            SendFmt(Proto.GUBAO_INFO);
            GameLog.Info("GuBao", "request 13320 gubao info(对标 MonsterController 进游戏发包)");
        }

        /// <summary>激活古宝碎片(对标老端点击[激活] → SendFmtToGame(13321,"hh",soap_id,debris_id));结果经 <see cref="On13321"/>。</summary>
        public void Activate(int soapId, int debrisId)
        {
            if (soapId <= 0 || debrisId <= 0) return;
            SendFmt(Proto.GUBAO_ACTIVE, "hh", soapId, debrisId);
            GameLog.Info("GuBao", "activate 13321 soap={0} debris={1}", soapId, debrisId);
        }

        /// <summary>13320 全量:combat:l + soap_list[u16×{soap_id:h, debris_list[u16×{debris_id:h}]}]。</summary>
        private void On13320(NetReader r)
        {
            long combat = r.ReadU64();
            List<(int soapId, List<int> debrisIds)> soapList = r.ReadArray(ReadSoap);
            GuBaoModel.Instance.ApplyFull(combat, soapList);
            GameLog.Info("GuBao", "13320 combat={0} soaps={1} remaining={2}B", combat, soapList.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_GUBAO_UPDATE);
        }

        /// <summary>13321 激活结果:errcode:i, soap_id:h, debris_list[u16×{debris_id:h}], combat:l。
        /// errcode!=1 显码降级(常见=缺碎片材料)。</summary>
        private void On13321(NetReader r)
        {
            int errcode = (int)r.ReadU32();
            int soapId = r.ReadU16();
            List<int> debrisIds = r.ReadArray(rr => (int)rr.ReadU16());
            long combat = r.ReadU64();
            if (errcode != 1)
            {
                TipsManager.Toast("激活失败(" + errcode + ")");   // 错误码表未移植,显码降级(常见:缺碎片材料)
                GameLog.Info("GuBao", "13321 activate fail errcode={0} soap={1}", errcode, soapId);
                return;
            }
            GuBaoModel.Instance.ApplyActive(soapId, debrisIds, combat);
            TipsManager.Toast("激活成功");
            GameLog.Info("GuBao", "13321 activate ok soap={0} debris={1} combat={2} remaining={3}B",
                soapId, debrisIds.Count, combat, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_GUBAO_UPDATE);
        }

        /// <summary>读 13320 soap_list 单项:soap_id:h, debris_list[u16×{debris_id:h}]。</summary>
        private static (int soapId, List<int> debrisIds) ReadSoap(NetReader r)
        {
            int soapId = r.ReadU16();
            List<int> debrisIds = r.ReadArray(rr => (int)rr.ReadU16());
            return (soapId, debrisIds);
        }
    }
}
