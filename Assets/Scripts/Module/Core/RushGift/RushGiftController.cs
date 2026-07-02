using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Tasks;

namespace Shenxiao.Module.Core.RushGift
{
    /// <summary>
    /// 冲级豪礼协议控制器(对标老端 commonController/WelfareController.ts;服务端 pt_417/pp_welfare)。
    /// 进游戏发 41700 求全量列表(老端 GAME_START 发,无参);Receive(lv) 发 41701 "h" 领取。
    /// 41701 领取成功(code==1)后老端再发一次 41700 刷新(WelfareController.ts:80-88);
    /// 若当前主线任务==100420(领取 35 级礼包)领取成功后自动关窗(对标 LevelRewardItem.ts:106-112)。
    /// </summary>
    public sealed class RushGiftController : BaseController
    {
        public static readonly RushGiftController Instance = new RushGiftController();

        private RushGiftController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.RUSH_GIFT_STATE, On41700);
            RegisterProtocal(Proto.RUSH_GIFT_RECEIVE, On41701);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            RushGiftModel.Instance.Clear();
            base.Dispose();
        }

        private async void OnGameStart()
        {
            await RushGiftConfigs.EnsureLoaded();
            SendFmt(Proto.RUSH_GIFT_STATE);
            GameLog.Info("RushGift", "request 41700 giftbag state(对标 WelfareController GAME_START 发,无参)");
        }

        /// <summary>领取礼包(对标老端 LevelRewardItem.ts:99 Fire(...,41701,"h",lv));结果经 <see cref="On41701"/>。</summary>
        public void Receive(int lv)
        {
            if (lv <= 0) return;
            SendFmt(Proto.RUSH_GIFT_RECEIVE, "h", lv);
            GameLog.Info("RushGift", "receive 41701 lv={0}", lv);
        }

        /// <summary>41700 礼包状态列表:giftbag_state[u16×{lv:h, received:c, end_time:l, remain_num:i}]。</summary>
        private void On41700(NetReader r)
        {
            List<RushGiftModel.GiftVo> list = r.ReadArray(rr => new RushGiftModel.GiftVo
            {
                Lv = rr.ReadU16(),          // lv:h
                Received = rr.ReadU8(),     // received:c(0未达/1可领/2已领/4已被领完)
                EndTime = rr.ReadU64(),     // end_time:l
                RemainNum = (int)rr.ReadU32(), // remain_num:i
            });
            RushGiftModel.Instance.SetList(list);
            GameLog.Info("RushGift", "41700 giftbag_state={0} remaining={1}B", list.Count, r.Remaining);
            EventDispatcher.Emit(GlobalEvent.EVT_RUSH_GIFT_UPDATE);
        }

        /// <summary>41701 领取结果:lv:h, code:i, rewards:ObjectList[u16×{type:c, type_id:i, num:i}]。
        /// code==1 → 逐项经 GoodsModel.GetMappingTypeId 还原展示「获得X」toast(表里没有的不臆造,跳过;
        /// 对标 BagController.On15050 show_goods 处理)+ ApplyReceived + 再发 41700(对标老端刷新)+「领取成功」toast;
        /// code!=1 → 「领取失败(code)」显码降级(错误码表 Util.ErrorCodeShow 未移植)。
        /// 领取成功且当前主线任务为 100420(领取 35 级礼包)时关闭壳(对标 LevelRewardItem.ts:106-112 自动关窗)。</summary>
        private void On41701(NetReader r)
        {
            int lv = r.ReadU16();            // lv:h
            int code = (int)r.ReadU32();     // code:i
            List<(int type, int typeId, int num)> rewards = r.ReadArray(rr =>
                ((int)rr.ReadU8(), (int)rr.ReadU32(), (int)rr.ReadU32()));   // rewards[u16×{type:c,type_id:i,num:i}]

            GameLog.Info("RushGift", "41701 lv={0} code={1} rewards={2} remaining={3}B", lv, code, rewards.Count, r.Remaining);

            if (code != 1)
            {
                TipsManager.Toast("领取失败(" + code + ")");   // 错误码表(Util.ErrorCodeShow)未移植,显码降级
                return;
            }

            foreach ((int type, int typeId, int num) it in rewards)
            {
                (int mappedId, int _) = GoodsModel.GetMappingTypeId(it.type, it.typeId);
                GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(mappedId);
                if (basic == null) continue;   // 表里没有的不臆造名称,直接跳过
                TipsManager.Toast("获得" + basic.Name + "x" + it.num);
            }

            RushGiftModel.Instance.ApplyReceived(lv);
            SendFmt(Proto.RUSH_GIFT_STATE);   // 对标老端领取成功后再发一次 41700 刷新
            TipsManager.Toast("领取成功");
            EventDispatcher.Emit(GlobalEvent.EVT_RUSH_GIFT_UPDATE);

            if (TaskModel.Instance.MainLineTaskVo != null && TaskModel.Instance.MainLineTaskVo.TaskId == 100420)
            {
                RushGiftShellView.Close();   // 对标老端 LevelRewardItem.ts:106-112 领取后自动关窗
                GameLog.Info("RushGift", "41701 主线100420 领取完成 → 自动关窗(对标老端)");
            }
        }
    }
}
