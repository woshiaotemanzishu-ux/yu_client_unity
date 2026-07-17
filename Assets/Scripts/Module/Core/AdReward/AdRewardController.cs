using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.AdReward
{
    /// <summary>
    /// 广告奖励(AdReward)控制器(对标老客户端 commonController/WelfareController.ts 广告分支,自动循环
    /// 轮18 PK4)。协议 ADREWARD_*(193xx,pt_193.erl;ClientProtocol.json L2102-2109)。独立成模块,不塞进
    /// <see cref="Shenxiao.Module.Core.Welfare.WelfareController"/>。GAME_START 时老端按
    /// GetAdOpenState()(Conch壳+Eyou发行渠道专属平台信号,Unity 恒 false,见 <see cref="AdRewardModel.GetAdOpenState"/>)
    /// 条件发 19302,本端镜像同一条件(当前恒不发,保留结构供未来平台接入后自然生效)。19304 老端 Handler19304
    /// 逻辑全注释(仅保留给第三方 SDK 埋点的占位),本端同样仅注册防御 recv,不提供发送方法、不做业务消费。
    /// </summary>
    public sealed class AdRewardController : BaseController
    {
        public static readonly AdRewardController Instance = new AdRewardController();
        private AdRewardController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.ADREWARD_REWARD_PUSH, On19301);
            RegisterProtocal(Proto.ADREWARD_LIST, On19302);
            RegisterProtocal(Proto.ADREWARD_WATCH_CLAIM, On19303);
            RegisterProtocal(Proto.ADREWARD_GRADE_PUSH, On19304);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            AdRewardModel.Instance.Reset();
            base.Dispose();
        }

        /// <summary>对标老端 GAME_START 分支:`if(welfareModel.GetAdOpenState()) Fire 19302`。</summary>
        private void OnGameStart()
        {
            AdRewardModel.Instance.Reset();
            if (AdRewardModel.Instance.GetAdOpenState()) SendFmt(Proto.ADREWARD_LIST);
        }

        /// <summary>请求广告冷却/开放列表(发空)。</summary>
        public void RequestList() => SendFmt(Proto.ADREWARD_LIST);

        /// <summary>上报广告观看完成/领取(发 "iii" ModId,SubId,GradeId)。</summary>
        public void WatchClaim(int modId, int subId, int gradeId) =>
            SendFmt(Proto.ADREWARD_WATCH_CLAIM, "iii", modId, subId, gradeId);

        /// <summary>19301 广告奖励推送(S2C only,标准 write_object_list)。对标老端 Handler19301:非空即弹奖励。</summary>
        private void On19301(NetReader r)
        {
            List<(int type, int typeId, int num)> reward = ReadObjectList(r);
            if (reward.Count > 0)
            {
                TipsManager.Toast("获得 " + FormatRewardSummary(reward));
            }
            EventDispatcher.Emit(GlobalEvent.EVT_ADREWARD_UPDATE, Proto.ADREWARD_REWARD_PUSH);
            GameLog.Info("AdReward", "19301 广告奖励推送 rewardN={0}", reward.Count);
        }

        /// <summary>19302 广告冷却/开放列表:AdList[u16×{ModId:32,SubId:32,Count:8}]。</summary>
        private void On19302(NetReader r)
        {
            List<AdRewardModel.AdEntry> list = r.ReadArray(rr =>
                new AdRewardModel.AdEntry((int)rr.ReadU32(), (int)rr.ReadU32(), rr.ReadU8()));
            AdRewardModel.Instance.SetList(list);
            EventDispatcher.Emit(GlobalEvent.EVT_ADREWARD_UPDATE, Proto.ADREWARD_LIST);
            GameLog.Info("AdReward", "19302 广告列表 count={0}", list.Count);
        }

        /// <summary>19303 上报广告观看完成/领取回执:ModId:32,SubId:32,GradeId:32,Code:32(Code在最后)。
        /// 对标老端 Handler19303:失败才显码,真实奖励经 <see cref="On19301"/> 推送另到。</summary>
        private void On19303(NetReader r)
        {
            int modId = (int)r.ReadU32();
            int subId = (int)r.ReadU32();
            int gradeId = (int)r.ReadU32();
            int code = (int)r.ReadU32();
            if (code != 1)
            {
                TipsManager.Toast("领取失败(" + code + ")");
            }
            EventDispatcher.Emit(GlobalEvent.EVT_ADREWARD_UPDATE, Proto.ADREWARD_WATCH_CLAIM);
            GameLog.Info("AdReward", "19303 观看领取回执 modId={0} subId={1} gradeId={2} code={3}", modId, subId, gradeId, code);
        }

        /// <summary>19304 广告档位变更推送(老端 Handler19304 逻辑全注释=第三方 SDK 埋点占位,仅注册防御 recv,
        /// 不做业务消费、不提供发送方法)。</summary>
        private void On19304(NetReader r)
        {
            int modId = (int)r.ReadU32();
            int subId = (int)r.ReadU32();
            int gradeId = (int)r.ReadU32();
            EventDispatcher.Emit(GlobalEvent.EVT_ADREWARD_UPDATE, Proto.ADREWARD_GRADE_PUSH);
            GameLog.Info("AdReward", "19304 档位变更推送(占位防御recv) modId={0} subId={1} gradeId={2}", modId, subId, gradeId);
        }

        // ---- 小工具 ----

        private static List<(int type, int typeId, int num)> ReadObjectList(NetReader r) =>
            r.ReadArray(rr => ((int)rr.ReadU8(), (int)rr.ReadU32(), (int)rr.ReadU32()));

        private static string FormatRewardSummary(List<(int type, int typeId, int num)> rewards)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < rewards.Count; i++)
            {
                if (i > 0) sb.Append('、');
                (int goodsId, int _) = GoodsModel.GetMappingTypeId(rewards[i].type, rewards[i].typeId);
                string name = GoodsModel.GetGoodsName(goodsId);
                if (string.IsNullOrEmpty(name)) name = "物品" + goodsId;
                sb.Append(name).Append('x').Append(rewards[i].num);
            }
            return sb.ToString();
        }
    }
}
