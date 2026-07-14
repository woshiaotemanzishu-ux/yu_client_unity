using System.Collections.Generic;

namespace Shenxiao.Module.Core.Boss
{
    /// <summary>
    /// Boss 家族二期·跨服族(自动循环 轮15b)数据层:pt_470(千幻蜃楼/圣兽岭)+ pt_471(镇煞封魂/幻域Boss)+
    /// pt_619(论剑恩怨簿)+ pt_460 内 kf_great_demon 壳(46037-39/46046,跨服太古遗凶专属四号)。
    /// 15a 的 <see cref="BossModel"/> 只覆盖本服 46000 段;本类与 <see cref="KfBossController"/> 并列新增,
    /// 不改 15a 既有文件结构(仅 BossModel.KillBossNotifyTypes 一处订正,见其自身注释)。
    /// 纯数据 + 事件,不含表现层逻辑(对标老端 BossModel.ts 里跨服族相关字段的数据子集)。
    /// </summary>
    public sealed class KfBossModel
    {
        public static readonly KfBossModel Instance = new KfBossModel();
        private KfBossModel() { }

        /// <summary>老端 BossModel.cross_boss_base_index=1000(跨服 boss_type 客户端侧统一 +1000 偏移)。</summary>
        public const int CROSS_BOSS_BASE_INDEX = 1000;

        public static class BossType
        {
            /// <summary>千幻蜃楼/圣兽岭客户端侧类型(服务端裸值 1=?BOSS_TYPE_EUDEMONS,+1000 偏移,
            /// 对标老端 BossType.holy)。</summary>
            public const int Holy = 1 + CROSS_BOSS_BASE_INDEX;

            /// <summary>镇煞封魂(pt_471)——wire 本身不带 boss_type 字段,老端仅用此数字给掉落日志内部打标记
            /// (BossModel.BossType.domain=471),从不上行,这里保留同值供数据打标一致。</summary>
            public const int DecorationInternalTag = 471;
        }

        // ============================================================================================
        // §1 pt_470 千幻蜃楼/圣兽岭(Eudemons)
        // 存活裁决:47001(击杀日志)/47011(同区服务器列表)为发送侧/整链路死号,不接(spec 明示,详见
        // KfBossController 注释);其余 20 个活号全接,含 47008(r15b 报告称"服务端无发送调用点"经本代理
        // 直接 grep mod_eudemons_land.erl:1158/1188 证伪——两处均 write+send_to_scene 真实可达,按活号实现)。
        // ============================================================================================

        public sealed class CollectEntry
        {
            public int Type;
            public int CollectTimes;
            public int TotalCollectTimes;
        }

        public sealed class EudemonsBossEntry
        {
            public int BossId;
            public int Num;
            public long RebornTime;
            public bool IsRemind;
            public bool IsAlive => Num > 0;
        }

        public sealed class EudemonsState
        {
            public int BossType; // 客户端侧值(已 +1000)
            public int ActStatus;
            public long ResetEtime;
            public int Tired;
            public int MaxTired;
            public readonly List<CollectEntry> CollectList = new List<CollectEntry>();
            public readonly List<EudemonsBossEntry> BossList = new List<EudemonsBossEntry>();
            public bool HasData;

            public EudemonsBossEntry GetEntry(int bossId)
            {
                for (int i = 0; i < BossList.Count; i++)
                    if (BossList[i].BossId == bossId) return BossList[i];
                return null;
            }
        }

        private readonly Dictionary<int, EudemonsState> _eudemons = new Dictionary<int, EudemonsState>();
        public IReadOnlyDictionary<int, EudemonsState> AllEudemonsStates => _eudemons;

        public EudemonsState GetOrCreateEudemons(int clientBossType)
        {
            if (!_eudemons.TryGetValue(clientBossType, out EudemonsState s))
            {
                s = new EudemonsState { BossType = clientBossType };
                _eudemons[clientBossType] = s;
            }
            return s;
        }

        public EudemonsState GetEudemons(int clientBossType) =>
            _eudemons.TryGetValue(clientBossType, out EudemonsState s) ? s : null;

        /// <summary>47000 全量落地(对标老端 SetBossInfo,已是客户端侧 +1000 值)。</summary>
        public void ApplyEudemonsList(int clientBossType, int actStatus, long resetEtime, int tired, int maxTired,
            List<CollectEntry> collectList, List<EudemonsBossEntry> bossList)
        {
            EudemonsState s = GetOrCreateEudemons(clientBossType);
            s.ActStatus = actStatus; s.ResetEtime = resetEtime; s.Tired = tired; s.MaxTired = maxTired;
            s.CollectList.Clear();
            if (collectList != null) s.CollectList.AddRange(collectList);
            s.BossList.Clear();
            if (bossList != null) s.BossList.AddRange(bossList);
            s.HasData = true;
        }

        /// <summary>47007/47008 重生刷新 upsert(两号 S2C 结构相同,共用落地逻辑;不存在则新增)。</summary>
        public void ApplyEudemonsReborn(int clientBossType, int bossId, long rebornTime, int num)
        {
            EudemonsState s = GetOrCreateEudemons(clientBossType);
            EudemonsBossEntry e = s.GetEntry(bossId);
            if (e == null)
            {
                e = new EudemonsBossEntry { BossId = bossId };
                s.BossList.Add(e);
            }
            e.RebornTime = rebornTime;
            e.Num = num;
        }

        public void SetEudemonsRemind(int clientBossType, int bossId, bool remind)
        {
            EudemonsBossEntry e = GetEudemons(clientBossType)?.GetEntry(bossId);
            if (e != null) e.IsRemind = remind;
        }

        /// <summary>47002 掉落日志(跨服变体,含 ServerId/ServerNum/Layers,与 46046 共用形态,见
        /// <see cref="CrossDropLogEntry"/>)。</summary>
        public readonly List<CrossDropLogEntry> EudemonsDropLog = new List<CrossDropLogEntry>();
        public bool HasEudemonsDropLog { get; private set; }

        public void ApplyEudemonsDropLog(List<CrossDropLogEntry> list)
        {
            EudemonsDropLog.Clear();
            if (list != null) EudemonsDropLog.AddRange(list);
            HasEudemonsDropLog = true;
        }

        /// <summary>47017(全量)/47018(单条追加)宝箱坐标,按 boss_id 分桶(对标老端 boxs_pos[boss_type]字典,
        /// 本类固定服务千幻蜃楼一种跨服 boss_type,不再按 type 分桶)。</summary>
        public sealed class XY { public int X; public int Y; }

        private readonly Dictionary<int, List<XY>> _eudemonsBoxPos = new Dictionary<int, List<XY>>();

        public void SetEudemonsBoxPos(Dictionary<int, List<XY>> full)
        {
            _eudemonsBoxPos.Clear();
            if (full == null) return;
            foreach (KeyValuePair<int, List<XY>> kv in full) _eudemonsBoxPos[kv.Key] = kv.Value;
        }

        public void AddEudemonsBoxPos(int bossId, List<XY> xy) => _eudemonsBoxPos[bossId] = xy;

        public IReadOnlyList<XY> GetEudemonsBoxPos(int bossId) =>
            _eudemonsBoxPos.TryGetValue(bossId, out List<XY> v) ? v : null;

        /// <summary>47019 狩猎等级(千幻蜃楼独有子系统)。</summary>
        public int HuntLevel { get; private set; }
        public long HuntExp { get; private set; }
        public long HuntAddExp { get; private set; }
        public bool HasHuntLevel { get; private set; }

        public void SetHuntLevel(int level, long exp, long addExp)
        {
            HuntLevel = level; HuntExp = exp; HuntAddExp = addExp; HasHuntLevel = true;
        }

        /// <summary>47021 圣兽领榜单(伤害/积分排行,一次性全量下发)。</summary>
        public sealed class EudemonsRankEntry
        {
            public long RoleId;
            public string RoleName = "";
            public int ServerId;
            public int ServerNum;
            public long Score;
            public int SortKey1;
            public int KillNum;
            public long SortKey2;
            public long TotalScore;
            public long SortKey3;
        }

        public readonly List<EudemonsRankEntry> EudemonsRank = new List<EudemonsRankEntry>();
        public bool HasEudemonsRank { get; private set; }

        public void ApplyEudemonsRank(List<EudemonsRankEntry> list)
        {
            EudemonsRank.Clear();
            if (list != null) EudemonsRank.AddRange(list);
            HasEudemonsRank = true;
        }

        /// <summary>47015 结算奖励(reward_type==3 走通用弹窗,本轮只落数据,UI 分支留 TODO)。</summary>
        public sealed class RewardEntry { public int Type; public long GoodsTypeId; public long Num; public long Id; }

        public int EudemonsRewardType { get; private set; }
        public readonly List<RewardEntry> EudemonsRewardList = new List<RewardEntry>();
        public bool HasEudemonsReward { get; private set; }

        public void SetEudemonsReward(int rewardType, List<RewardEntry> list)
        {
            EudemonsRewardType = rewardType;
            EudemonsRewardList.Clear();
            if (list != null) EudemonsRewardList.AddRange(list);
            HasEudemonsReward = true;
        }

        // ============================================================================================
        // §2 pt_471 镇煞封魂/幻域Boss(Decoration)——17 号全活,wire↔业务连线全套核实完整,无死号。
        // ============================================================================================

        public sealed class DecorationBossEntry
        {
            public int BossId;
            public long RebornTime;
            public int RoleNum;
            public bool IsHadAssist;
            public bool IsAlive => RebornTime <= 0;
        }

        public int DecorationActStatus { get; private set; }
        public int DecorationCount { get; private set; }
        public int DecorationAssistCount { get; private set; }
        public int DecorationBuyCount { get; private set; }
        public int DecorationAddCount { get; private set; }
        public bool DecorationInBuff { get; private set; }
        public int DecorationKillCount { get; private set; }
        public bool DecorationIsAlive { get; private set; }
        public int DecorationSbossRoleNum { get; private set; }
        public readonly List<DecorationBossEntry> DecorationBossList = new List<DecorationBossEntry>();
        public bool HasDecorationInfo { get; private set; }

        /// <summary>47101 主界面数据全量落地(对标老端 do_main_data,本地按 boss_id 排序留调用方,数据层不排)。</summary>
        public void ApplyDecorationInfo(int actStatus, int count, int assistCount, int buyCount, int addCount,
            bool inBuff, int killCount, bool isAlive, int sbossRoleNum, List<DecorationBossEntry> list)
        {
            DecorationActStatus = actStatus; DecorationCount = count; DecorationAssistCount = assistCount;
            DecorationBuyCount = buyCount; DecorationAddCount = addCount; DecorationInBuff = inBuff;
            DecorationKillCount = killCount; DecorationIsAlive = isAlive; DecorationSbossRoleNum = sbossRoleNum;
            DecorationBossList.Clear();
            if (list != null) DecorationBossList.AddRange(list);
            HasDecorationInfo = true;
        }

        /// <summary>47104 购买次数成功:本地自增,不重查 47101(对标老端)。</summary>
        public void IncrementDecorationBuyCount() => DecorationBuyCount++;

        private readonly HashSet<int> _decorationUnfollow = new HashSet<int>();
        public bool IsDecorationUnfollowed(int bossId) => _decorationUnfollow.Contains(bossId);

        /// <summary>47105 取关列表初始化(对标老端 domain_unfollow_dic 批量落 true)。</summary>
        public void SetDecorationUnfollowList(List<int> bossIds)
        {
            _decorationUnfollow.Clear();
            if (bossIds == null) return;
            foreach (int id in bossIds) _decorationUnfollow.Add(id);
        }

        /// <summary>47106 单个关注/取关回执落地。</summary>
        public void SetDecorationFollow(int bossId, bool isFollow)
        {
            if (isFollow) _decorationUnfollow.Remove(bossId);
            else _decorationUnfollow.Add(bossId);
        }

        /// <summary>47108 掉落日志(与 46002/47002/46046 比少 Layers 字段,多 Num 字段,独立形态)。</summary>
        public sealed class DecorationDropLogEntry
        {
            public long RoleId;
            public int ServerId;
            public int ServerNum;
            public string Name = "";
            public int BossId;
            public int GoodsId;
            public long Num;
            public long Rating;
            public List<BossModel.EquipExtraAttrEntry> EquipExtraAttr = new List<BossModel.EquipExtraAttrEntry>();
            public long Time;
        }

        public readonly List<DecorationDropLogEntry> DecorationDropLog = new List<DecorationDropLogEntry>();
        public bool HasDecorationDropLog { get; private set; }

        public void ApplyDecorationDropLog(List<DecorationDropLogEntry> list)
        {
            DecorationDropLog.Clear();
            if (list != null) DecorationDropLog.AddRange(list);
            HasDecorationDropLog = true;
        }

        /// <summary>47109(全量)/47112(单条 patch,按 role_id 命中则更新伤害否则追加,对标老端 ChangeRoleRank)。</summary>
        public sealed class DecorationRankEntry
        {
            public long RoleId;
            public string Name = "";
            public int ServerId;
            public int ServerNum;
            public string ServerName = "";
            public long Hurt;
        }

        public readonly List<DecorationRankEntry> DecorationRank = new List<DecorationRankEntry>();
        public bool HasDecorationRank { get; private set; }

        public void ApplyDecorationRank(List<DecorationRankEntry> list)
        {
            DecorationRank.Clear();
            if (list != null) DecorationRank.AddRange(list);
            HasDecorationRank = true;
        }

        public void PatchDecorationRank(DecorationRankEntry patch)
        {
            for (int i = 0; i < DecorationRank.Count; i++)
            {
                if (DecorationRank[i].RoleId == patch.RoleId) { DecorationRank[i].Hurt = patch.Hurt; return; }
            }
            DecorationRank.Add(patch);
        }

        /// <summary>47113 boss 结算(两套奖励表:普通/翻倍,is_belong 归属判定)。</summary>
        public sealed class DecorationRewardItem { public int Style; public long TypeId; public long Count; public long GoodsId; }
        public sealed class DecorationRewardGroup { public int RewardType; public readonly List<DecorationRewardItem> Items = new List<DecorationRewardItem>(); }

        public sealed class DecorationSettleResult
        {
            public bool IsBelong;
            public bool IsDouble;
            public readonly List<DecorationRewardGroup> RewardTypeList = new List<DecorationRewardGroup>();
            public readonly List<DecorationRewardGroup> RewardTypeList2 = new List<DecorationRewardGroup>();
        }

        public DecorationSettleResult LastDecorationSettle { get; private set; }
        public void SetDecorationSettle(DecorationSettleResult r) => LastDecorationSettle = r;

        /// <summary>47114 进场景全量 / 47115 退出时间 / 47116 复活时间。</summary>
        public int DecorationEnterType { get; private set; }
        public long DecorationQuitTime { get; private set; }
        public long DecorationReviveTime { get; private set; }
        public bool HasDecorationSceneInfo { get; private set; }

        public void SetDecorationSceneInfo(int enterType, long quitTime, long reviveTime)
        {
            DecorationEnterType = enterType; DecorationQuitTime = quitTime; DecorationReviveTime = reviveTime;
            HasDecorationSceneInfo = true;
        }

        public void SetDecorationQuitTime(long quitTime) => DecorationQuitTime = quitTime;
        public void SetDecorationReviveTime(long reviveTime) => DecorationReviveTime = reviveTime;

        // ============================================================================================
        // §3 pt_619 论剑恩怨簿(PkLog)——纯会话内记录,凌晨清理≈全量清空(pt_619 quirk,注释存档,不做本地
        // 跨会话持久化假设);scope quirk:服务端 is_in_kf_pk_scene 只覆盖 EUDEMONS_BOSS/KF_SANCTUARY/SANCTUM
        // 三类场景,镇煞封魂/跨服大妖场景死亡不产恩怨记录——照实接收,不额外过滤/校验。
        // ============================================================================================

        public sealed class KillRecordEntry
        {
            public int Sign;
            public long Time;
            public string SceneName = "";
            public string AttrName = "";
            public long AttrId;
        }

        public sealed class KfKillRecordEntry
        {
            public int Sign;
            public long Time;
            public string SceneName = "";
            public int ServerId;   // 注:pt_619 家族 ServerId/ServerNum 用 32 位,与 47xxx/46xxx 系普遍 16 位不同
            public int ServerNum;
            public string AttrName = "";
            public long AttrId;
        }

        public readonly List<KillRecordEntry> KillRecordList = new List<KillRecordEntry>();
        public readonly List<KfKillRecordEntry> KfKillRecordList = new List<KfKillRecordEntry>();
        public bool HasKillRecord { get; private set; }

        /// <summary>61900 全量落地(本服 send_list + 跨服 kf_send_list 分开两个数组,对标老端 SetkillRecord
        /// 合并成一个列表——本端保留分列存储,调用方需要合并展示时自行拼接,信息不丢失)。</summary>
        public void ApplyKillRecord(List<KillRecordEntry> local, List<KfKillRecordEntry> kf)
        {
            KillRecordList.Clear();
            if (local != null) KillRecordList.AddRange(local);
            KfKillRecordList.Clear();
            if (kf != null) KfKillRecordList.AddRange(kf);
            HasKillRecord = true;
        }

        public void AddKillRecord(KillRecordEntry e) => KillRecordList.Add(e);       // 61901 本服增量
        public void AddKfKillRecord(KfKillRecordEntry e) => KfKillRecordList.Add(e); // 61902 跨服增量

        // ============================================================================================
        // §4 pt_460 内 kf_great_demon 壳(46037-39/46046,太古遗凶专属四号)——pp_boss.erl 对应 handle 子句
        // **无条件**转发 mod_great_demon_local:*,与本轮 15a 已接的 46000-46046 主链并列,15b 补齐这四号。
        // ============================================================================================

        public int GreatDemonKillNum { get; private set; }
        public readonly List<int> GreatDemonHadRewardStages = new List<int>();
        public bool HasGreatDemonReward { get; private set; }

        public void SetGreatDemonReward(int killNum, List<int> stages)
        {
            GreatDemonKillNum = killNum;
            GreatDemonHadRewardStages.Clear();
            if (stages != null) GreatDemonHadRewardStages.AddRange(stages);
            HasGreatDemonReward = true;
        }

        public sealed class GreatDemonBoxEntry
        {
            public int BossId;
            public readonly List<XY> XyList = new List<XY>();
        }

        public readonly List<GreatDemonBoxEntry> GreatDemonBoxList = new List<GreatDemonBoxEntry>();
        public bool HasGreatDemonBox { get; private set; }

        public void SetGreatDemonBoxList(List<GreatDemonBoxEntry> list)
        {
            GreatDemonBoxList.Clear();
            if (list != null) GreatDemonBoxList.AddRange(list);
            HasGreatDemonBox = true;
        }

        /// <summary>46046 跨服掉落日志(与 47002 相同形态,共用 <see cref="CrossDropLogEntry"/>)。</summary>
        public readonly List<CrossDropLogEntry> GreatDemonDropLog = new List<CrossDropLogEntry>();
        public bool HasGreatDemonDropLog { get; private set; }

        public void ApplyGreatDemonDropLog(List<CrossDropLogEntry> list)
        {
            GreatDemonDropLog.Clear();
            if (list != null) GreatDemonDropLog.AddRange(list);
            HasGreatDemonDropLog = true;
        }

        // ============================================================================================
        // 共用形态:47002/46046 掉落日志(RoleId:64,ServerId:16,ServerNum:16,Name,BossId:32,Layers:8,
        // GoodsId:32,Rating:32,EquipExtraAttr[...],Time:32)——两号字段序逐位相同,合并一个类避免重复定义。
        // ============================================================================================
        public sealed class CrossDropLogEntry
        {
            public long RoleId;
            public int ServerId;
            public int ServerNum;
            public string Name = "";
            public int BossId;
            public int Layers;
            public int GoodsId;
            public long Rating;
            public List<BossModel.EquipExtraAttrEntry> EquipExtraAttr = new List<BossModel.EquipExtraAttrEntry>();
            public long Time;
        }

        public void Clear()
        {
            _eudemons.Clear();
            EudemonsDropLog.Clear(); HasEudemonsDropLog = false;
            _eudemonsBoxPos.Clear();
            HuntLevel = 0; HuntExp = 0; HuntAddExp = 0; HasHuntLevel = false;
            EudemonsRank.Clear(); HasEudemonsRank = false;
            EudemonsRewardType = 0; EudemonsRewardList.Clear(); HasEudemonsReward = false;

            DecorationActStatus = 0; DecorationCount = 0; DecorationAssistCount = 0; DecorationBuyCount = 0;
            DecorationAddCount = 0; DecorationInBuff = false; DecorationKillCount = 0; DecorationIsAlive = false;
            DecorationSbossRoleNum = 0; DecorationBossList.Clear(); HasDecorationInfo = false;
            _decorationUnfollow.Clear();
            DecorationDropLog.Clear(); HasDecorationDropLog = false;
            DecorationRank.Clear(); HasDecorationRank = false;
            LastDecorationSettle = null;
            DecorationEnterType = 0; DecorationQuitTime = 0; DecorationReviveTime = 0; HasDecorationSceneInfo = false;

            KillRecordList.Clear(); KfKillRecordList.Clear(); HasKillRecord = false;

            GreatDemonKillNum = 0; GreatDemonHadRewardStages.Clear(); HasGreatDemonReward = false;
            GreatDemonBoxList.Clear(); HasGreatDemonBox = false;
            GreatDemonDropLog.Clear(); HasGreatDemonDropLog = false;
        }
    }
}
