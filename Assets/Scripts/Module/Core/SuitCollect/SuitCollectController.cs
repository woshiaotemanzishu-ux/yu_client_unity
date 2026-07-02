using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.SuitCollect
{
    /// <summary>
    /// 套装收集协议控制器(对标老端 commonController/SuitActivityController.ts;服务端 pt_152 段内 15256-15259,
    /// yu_server goods/suit_collect)。进游戏(EVT_GAME_START)发 15256 求全量;Activate(suitId,stage) 发 15257
    /// 激活套装阶段——**主线 100391 任务闭环唯一触发点**(服务端按 {suit_clt,[{SuitId,CurStage}]} 匹配完成)。
    /// 15258(穿装自动点亮广播)/15259(时装穿脱)本轮未接,留后。
    /// </summary>
    public sealed class SuitCollectController : BaseController
    {
        public static readonly SuitCollectController Instance = new SuitCollectController();

        private SuitCollectController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.SUIT_CLT_INFO, On15256);
            RegisterProtocal(Proto.SUIT_CLT_ACTIVE, On15257);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            SuitCollectModel.Instance.Clear();
            base.Dispose();
        }

        private async void OnGameStart()
        {
            await SuitCollectConfigs.EnsureLoaded();
            SendFmt(Proto.SUIT_CLT_INFO);
            GameLog.Info("SuitCollect", "request 15256 suit clt info(对标 SuitActivityController GAME_START 发)");
        }

        /// <summary>激活套装阶段(对标老端按钮 → SendFmtToGame(15257,"cc",suit_id,stage));结果经 <see cref="On15257"/>。</summary>
        public void Activate(int suitId, int stage)
        {
            if (suitId <= 0 || stage <= 0) return;
            SendFmt(Proto.SUIT_CLT_ACTIVE, "cc", suitId, stage);
            GameLog.Info("SuitCollect", "activate 15257 suit={0} stage={1}", suitId, stage);
        }

        /// <summary>15256 套装收集全量:clt_list[u16×{suit_id:c, cur_stage:c, cur_pos_list[u16×{equip_type:c}]}]
        /// + suit_id:c(末尾,当前穿戴时装,对标 SetSuitData)。</summary>
        private void On15256(NetReader r)
        {
            List<SuitCollectModel.SuitVo> list = r.ReadArray(ReadSuit);
            int fashionSuitId = r.ReadU8();
            SuitCollectModel.Instance.Apply15256(list, fashionSuitId);
            GameLog.Info("SuitCollect", "15256 suits={0} fashionSuitId={1} remaining={2}B",
                list.Count, fashionSuitId, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_SUIT_CLT_UPDATE);
        }

        /// <summary>15257 激活结果(对标 AddSuitData):code:i, suit_id:c, cur_stage:c,
        /// cur_pos_list[u16×{equip_type:c}];code==1 成功套值,否则显码降级(错误码表未移植)。</summary>
        private void On15257(NetReader r)
        {
            int code = (int)r.ReadU32();
            int suitId = r.ReadU8();
            int curStage = r.ReadU8();
            List<int> posList = r.ReadArray(rr => (int)rr.ReadU8());
            if (code != 1)
            {
                TipsManager.Toast("激活失败(" + code + ")");   // 错误码表(Util.ErrorCodeShow)未移植,显码降级
                GameLog.Info("SuitCollect", "15257 activate fail code={0} suit={1}", code, suitId);
                return;
            }
            SuitCollectModel.Instance.Apply15257(suitId, curStage, posList);
            GameLog.Info("SuitCollect", "15257 activate ok suit={0} curStage={1} pos={2} remaining={3}B",
                suitId, curStage, posList.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_SUIT_CLT_UPDATE);
        }

        /// <summary>读 15256 clt_list 单项(字段序照 ClientProtocol.json,逐字段按序读完)。</summary>
        private static SuitCollectModel.SuitVo ReadSuit(NetReader r)
        {
            var vo = new SuitCollectModel.SuitVo
            {
                SuitId = r.ReadU8(),      // suit_id:c
                CurStage = r.ReadU8(),    // cur_stage:c
            };
            vo.PosList = r.ReadArray(rr => (int)rr.ReadU8());   // cur_pos_list[u16×{equip_type:c}]
            return vo;
        }
    }
}
