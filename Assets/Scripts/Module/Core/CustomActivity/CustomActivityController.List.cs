using System.Collections.Generic;
using System.Text;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Common;

namespace Shenxiao.Module.Core.CustomActivity
{
    /// <summary>
    /// LIST_DUOBAO=116 夺宝积分墙(自动循环 轮21 PF 补漏批;pt_332.erl 33252/33253/33254)。老端是独立的
    /// commonController/ListDuobaoController.ts + commonModel/ListDuobaoModel.ts,不是主
    /// CustomActivityController.ts 的一部分——Unity 因 base_type/sub_type 语义与本文件其余 P2-P6 族同构,
    /// 并入同一 partial class 复用 ACT_ID 常量/RewardObj/ShowError 等基建,不新建独立单例控制器。
    ///
    /// 触发时机对标老端 commonModel/CustomActivityModel.ts:386-389(33101 全量列表落地时,扫到
    /// base_type==LIST_DUOBAO(116) 的条目就记 sub_type 并主动查 33252)——本端借道已有的
    /// EVT_CUSTOMACT_LIST_UPDATE 通用事件订阅(同 CustomActivityController.Biz.cs OnListUpdateForOverView
    /// 先例:守卫字段避免重复订阅,主文件 Dispose() 不在本代理可编辑范围故不配对 Off,理由同 Biz.cs 头注释——
    /// Dispose() 期间 On33101 已被基类注销、不会再 Emit LIST_UPDATE,长期订阅无副作用)。
    ///
    /// UI 消费方位于 Module/Core/ListDuobao；入口、阶段/排行/记录与抽奖结果已接线。
    /// </summary>
    public sealed partial class CustomActivityController
    {
        private const int ACT_ID_LIST_DUOBAO = 116;
        private bool _listDuobaoHookInstalled;

        /// <summary>LIST_DUOBAO 注册,由主文件 Register() 调用。</summary>
        private void RegisterList()
        {
            RegisterProtocal(Proto.CUSTOM_ACT_LISTDUOBAO_STAGE, On33252);
            RegisterProtocal(Proto.CUSTOM_ACT_LISTDUOBAO_RANK, On33253);
            RegisterProtocal(Proto.CUSTOM_ACT_LISTDUOBAO_CLAIM, On33254);
            RegisterProtocal(Proto.COMPETE_ACT_LIST_DUOBAO_DRAW, On33803);

            if (!_listDuobaoHookInstalled)
            {
                _listDuobaoHookInstalled = true;
                EventDispatcher.On(GlobalEvent.EVT_CUSTOMACT_LIST_UPDATE, OnListUpdateForListDuobao);
                EventDispatcher.On(GlobalEvent.EVT_CUSTOMACT_LIST_ADD, OnListUpdateForListDuobao);
                EventDispatcher.On(GlobalEvent.EVT_CUSTOMACT_LIST_REMOVE, OnListUpdateForListDuobao);
            }
        }

        private void UnregisterList()
        {
            if (!_listDuobaoHookInstalled) return;
            EventDispatcher.Off(GlobalEvent.EVT_CUSTOMACT_LIST_UPDATE, OnListUpdateForListDuobao);
            EventDispatcher.Off(GlobalEvent.EVT_CUSTOMACT_LIST_ADD, OnListUpdateForListDuobao);
            EventDispatcher.Off(GlobalEvent.EVT_CUSTOMACT_LIST_REMOVE, OnListUpdateForListDuobao);
            _listDuobaoHookInstalled = false;
        }

        /// <summary>对标老端 CustomActivityModel.ts:386-389:33101 落地后扫描 base_type==116 的条目,记录
        /// sub_type(ListDuobaoModel.SetActSubType)并主动请求阶段信息(Fire(REQUEST_SCMD,33252,...))。
        /// 同一时刻老端只会有一个夺宝子活动(sub_type 单值存储),取第一条即可。</summary>
        private void OnListUpdateForListDuobao()
        {
            foreach (KeyValuePair<long, CustomActivityModel.ActEntry> kv in CustomActivityModel.Instance.ActList)
            {
                if (kv.Value.BaseType != ACT_ID_LIST_DUOBAO) continue;
                CustomActivityModel.Instance.SetListDuobaoSubType(kv.Value.SubType);
                RequestListDuobaoStage(ACT_ID_LIST_DUOBAO, kv.Value.SubType);
                return;
            }
            CustomActivityModel.Instance.ClearList();
        }

        /// <summary>33252 查询夺宝阶段信息(发 "hh" type,subtype)。</summary>
        public void RequestListDuobaoStage(int type, int subType) => SendFmt(Proto.CUSTOM_ACT_LISTDUOBAO_STAGE, "hh", type, subType);

        /// <summary>33253 查询夺宝排行榜(发 "hh" type,subtype)。</summary>
        public void RequestListDuobaoRank(int type, int subType) => SendFmt(Proto.CUSTOM_ACT_LISTDUOBAO_RANK, "hh", type, subType);

        /// <summary>33254 领取夺宝阶段奖励(发 "hhh" type,subtype,reward_id)。</summary>
        public void ClaimListDuobaoReward(int type, int subType, int rewardId) =>
            SendFmt(Proto.CUSTOM_ACT_LISTDUOBAO_CLAIM, "hhh", type, subType, rewardId);

        /// <summary>33252 回包(pt_332.erl write(33252):1325-1361)。</summary>
        private void On33252(NetReader r)
        {
            int type = r.ReadU16();
            int subType = r.ReadU16();
            int score = r.ReadI32();
            int todayScore = r.ReadI32();
            string condition = r.ReadString();
            List<CustomActivityModel.ListDuobaoStageReward> rewardList = r.ReadArray(rr => new CustomActivityModel.ListDuobaoStageReward
            {
                GradeId = rr.ReadU16(),
                IsRare = rr.ReadU8(),
                Reward = CustomActivityModel.ReadRewardObjList(rr),
            });
            List<CustomActivityModel.ListDuobaoStageState> stageList = r.ReadArray(rr => new CustomActivityModel.ListDuobaoStageState
            {
                Id = rr.ReadU16(),
                GotType = rr.ReadU8(),
            });
            int worldLv = r.ReadI32();

            if (type != ACT_ID_LIST_DUOBAO || subType != CustomActivityModel.Instance.ListDuobaoSubType)
            {
                GameLog.Warn("CustomActivity", "ignore 33252 stale list-duobao type={0} sub={1} current={2}",
                    type, subType, CustomActivityModel.Instance.ListDuobaoSubType);
                return;
            }

            var info = new CustomActivityModel.ListDuobaoStageInfo
            {
                Type = type,
                SubType = subType,
                Score = score,
                TodayScore = todayScore,
                Condition = condition,
                WorldLv = worldLv,
            };
            info.RewardList.AddRange(rewardList);
            info.StageList.AddRange(stageList);
            CustomActivityModel.Instance.SetListDuobaoStage(info);
            _ = RefreshCustomActivityRedDotsAsync();
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, type, subType);
            GameLog.Info("CustomActivity", "33252 夺宝阶段信息 type={0} sub={1} score={2}/{3} rewardN={4} stageN={5} worldLv={6}",
                type, subType, score, todayScore, rewardList.Count, stageList.Count, worldLv);
        }

        /// <summary>33253 回包(pt_332.erl write(33253):1363-1397)。</summary>
        private void On33253(NetReader r)
        {
            int type = r.ReadU16();
            int subType = r.ReadU16();
            int score = r.ReadI32();
            int rank = r.ReadU16();
            List<CustomActivityModel.ListDuobaoRankEntry> rankList = r.ReadArray(rr => new CustomActivityModel.ListDuobaoRankEntry
            {
                Rank = rr.ReadU16(),
                ServerId = rr.ReadI32(),
                RoleId = (long)rr.ReadU64(),
                RoleName = rr.ReadString(),
                RoleScore = rr.ReadI32(),
            });
            int serverScore = r.ReadI32();
            int serverRank = r.ReadU16();
            List<CustomActivityModel.ListDuobaoServerRankEntry> serverRankList = r.ReadArray(rr => new CustomActivityModel.ListDuobaoServerRankEntry
            {
                Rank = rr.ReadU16(),
                ServerId = rr.ReadI32(),
                ServerName = rr.ReadString(),
                ServerScore = rr.ReadI32(),
            });

            if (type != ACT_ID_LIST_DUOBAO || subType != CustomActivityModel.Instance.ListDuobaoSubType)
            {
                GameLog.Warn("CustomActivity", "ignore 33253 stale list-duobao type={0} sub={1} current={2}",
                    type, subType, CustomActivityModel.Instance.ListDuobaoSubType);
                return;
            }

            var info = new CustomActivityModel.ListDuobaoRankInfo
            {
                Type = type,
                SubType = subType,
                Score = score,
                Rank = rank,
                ServerScore = serverScore,
                ServerRank = serverRank,
            };
            info.RankList.AddRange(rankList);
            info.ServerRankList.AddRange(serverRankList);
            CustomActivityModel.Instance.SetListDuobaoRank(info);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_DETAIL_UPDATE, type, subType);
            GameLog.Info("CustomActivity", "33253 夺宝排行榜 type={0} sub={1} score={2} rank={3} rankN={4} serverScore={5} serverRank={6} serverRankN={7}",
                type, subType, score, rank, rankList.Count, serverScore, serverRank, serverRankList.Count);
        }

        /// <summary>33254 回包(pt_332.erl write(33254):1399-1411)。对标老端 On33254:领取后**无条件**(不看
        /// error_code)追发 33252 刷新阶段信息(ts:78 `model.Fire(REQUEST_SCMD,33252,base_type,sub_type)`)。
        /// 老端另有"命中当前打开面板时用 CongratulationObtainView 展示 config_rush_treasure_stage_reward
        /// 配置奖励"的分支,Unity 该配置/展示通道未接线,降级为 toast(同 MailController 先例),TODO。</summary>
        private void On33254(NetReader r)
        {
            int type = r.ReadU16();
            int subType = r.ReadU16();
            int rewardId = r.ReadU16();
            int errorCode = r.ReadI32();
            if (type != ACT_ID_LIST_DUOBAO || subType != CustomActivityModel.Instance.ListDuobaoSubType) return;
            if (errorCode == 1) TipsManager.Toast("领取成功");
            else ShowError(errorCode);
            EventDispatcher.Emit(GlobalEvent.EVT_CUSTOMACT_RESULT, type, subType, errorCode);
            RequestListDuobaoStage(type, subType); // 对标老端无条件追发33252
            GameLog.Info("CustomActivity", "33254 夺宝领取 type={0} sub={1} rewardId={2} code={3}", type, subType, rewardId, errorCode);
        }

        /// <summary>33803 连服夺宝抽奖结果；33191 仍由 Festival partial 唯一注册。</summary>
        private void On33803(NetReader r)
        {
            var result = new CustomActivityModel.ListDuobaoDrawResult
            {
                Type = r.ReadU16(),
                SubType = r.ReadU16(),
                Times = r.ReadU8(),
                TodayScore = r.ReadU32(),
                Error = r.ReadU32(),
            };
            List<CustomActivityModel.ListDuobaoDrawReward> rewards = r.ReadArray(rr =>
            {
                var reward = new CustomActivityModel.ListDuobaoDrawReward { RewardId = rr.ReadU16() };
                reward.Reward.AddRange(CustomActivityModel.ReadRewardObjList(rr));
                return reward;
            });
            result.RewardList.AddRange(rewards);

            if (result.Type != ACT_ID_LIST_DUOBAO || result.SubType != CustomActivityModel.Instance.ListDuobaoSubType)
            {
                GameLog.Warn("CustomActivity", "ignore 33803 stale list-duobao type={0} sub={1} current={2}",
                    result.Type, result.SubType, CustomActivityModel.Instance.ListDuobaoSubType);
                return;
            }

            CustomActivityModel.Instance.SetListDuobaoDraw(result);
            if (result.Error == 1)
            {
                string rewardSummary = FormatListDuobaoRewards(result.RewardList);
                TipsManager.Toast(string.IsNullOrEmpty(rewardSummary)
                    ? (result.Times > 1 ? "十连夺宝成功" : "夺宝成功")
                    : "获得 " + rewardSummary);
            }
            else ShowError((int)result.Error);
            EventDispatcher.Emit(GlobalEvent.EVT_LIST_DUOBAO_DRAW_RESULT, result);
            GameLog.Info("CustomActivity", "33803 list-duobao draw type={0} sub={1} times={2} score={3} code={4} rewardN={5}",
                result.Type, result.SubType, result.Times, result.TodayScore, result.Error, result.RewardList.Count);
        }

        // Unity 尚无通用 CongratulationObtainView，按 Mail/Daily 既有降级约定展示完整奖励摘要。
        private static string FormatListDuobaoRewards(IReadOnlyList<CustomActivityModel.ListDuobaoDrawReward> groups)
        {
            var totals = new Dictionary<int, long>();
            var order = new List<int>();
            for (int i = 0; i < groups.Count; i++)
            {
                IReadOnlyList<CustomActivityModel.RewardObj> rewards = groups[i].Reward;
                for (int j = 0; j < rewards.Count; j++)
                {
                    CustomActivityModel.RewardObj reward = rewards[j];
                    (int goodsId, int _) = GoodsModel.GetMappingTypeId(reward.Type, reward.GoodsId);
                    if (!totals.ContainsKey(goodsId))
                    {
                        totals[goodsId] = 0;
                        order.Add(goodsId);
                    }
                    totals[goodsId] += reward.Num;
                }
            }

            var text = new StringBuilder();
            for (int i = 0; i < order.Count; i++)
            {
                int goodsId = order[i];
                if (i > 0) text.Append('、');
                string name = GoodsModel.GetGoodsName(goodsId);
                text.Append(string.IsNullOrEmpty(name) ? "物品" + goodsId : name)
                    .Append('x').Append(totals[goodsId]);
            }
            return text.ToString();
        }
    }
}
