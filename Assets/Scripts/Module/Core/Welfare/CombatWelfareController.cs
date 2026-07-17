using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Welfare
{
    /// <summary>
    /// 战力福利(CombatWelfare)控制器(自动循环 轮18 PK4)。协议 COMBAT_WELFARE_*(41723=面板/41724=摇奖,
    /// pt_417.erl:60-63,384-421)。老端此二号独立挂在 GrowthForceModel(commonModel/GrowthForceModel.ts,
    /// 与 GrowthBenefitsModel 同文件不同类,由老端 GrowthBenefitsController 统一注册),Unity 侧按同名拆分
    /// 独立 Controller,不塞进 <see cref="WelfareController"/> 或
    /// <see cref="Shenxiao.Module.Core.GrowthBenefits.GrowthBenefitsController"/>。
    /// 开界面门槛(老端 CheckFightWelfareOpen:等级≥config_welfare_cfg["6"]=combat_welfare_open_lv 且创角天数≥
    /// config_welfare_cfg["9"]=combat_welfare_open_day)与红点(CheckFightWelfareRed)、最大轮数计算(CheckMaxRound
    /// 遍历 config_combat_welfare_reward["{round}@12"])均属面板/图标 UI 层,且 Unity RoleModel 尚无"创角天数"
    /// 数据源(全仓核对无果),故本类只落协议收发与摇奖态,不做开启判定与图标联动,注释存档待该数据源补齐后接线。
    /// 摇奖具体奖励物品名需查 config_combat_welfare_reward["{round}@{reward_id}"].reward(Erlang term),
    /// 数据层暂不展开解析,仅落 RewardId 供未来面板层查表展示。
    /// </summary>
    public sealed class CombatWelfareController : BaseController
    {
        public static readonly CombatWelfareController Instance = new CombatWelfareController();
        private CombatWelfareController() { }

        public bool HasInfo { get; private set; }
        public int Round { get; private set; }
        public int Times { get; private set; }
        public long Combat { get; private set; }
        public long NextCombat { get; private set; }

        // 本轮已解锁/已摇中的 reward_id 集合(对标老端 GrowthForceModel.fight_welfare_list 字典存在性判定)。
        private readonly HashSet<int> _claimedRewardIds = new HashSet<int>();
        public IReadOnlyCollection<int> ClaimedRewardIds => _claimedRewardIds;
        public bool IsRewardClaimed(int rewardId) => _claimedRewardIds.Contains(rewardId);

        protected override void Register()
        {
            RegisterProtocal(Proto.COMBAT_WELFARE_INFO, On41723);
            RegisterProtocal(Proto.COMBAT_WELFARE_DRAW, On41724);
        }

        public override void Dispose()
        {
            HasInfo = false;
            Round = 0;
            Times = 0;
            Combat = 0;
            NextCombat = 0;
            _claimedRewardIds.Clear();
            base.Dispose();
        }

        /// <summary>请求战力福利面板数据(对标老端 GrowthForceModel.FightWelfareSend,开界面/GAME_START条件
        /// 触发,发空)。</summary>
        public void RequestInfo() => SendFmt(Proto.COMBAT_WELFARE_INFO);

        /// <summary>摇奖(对标老端 GrowthForceModel.FightWelfareRoll,发空)。</summary>
        public void Draw() => SendFmt(Proto.COMBAT_WELFARE_DRAW);

        /// <summary>41723 战力福利面板(裸;List 为裸 u16 RewardId 数组,item_to_bin_10 单字段)。
        /// pt_417.erl:60-61,384-405。</summary>
        private void On41723(NetReader r)
        {
            int round = r.ReadU8();
            int times = r.ReadU8();
            long combat = (long)r.ReadU64();
            long nextCombat = (long)r.ReadU64();
            List<int> rewardIds = r.ReadArray(rr => (int)rr.ReadU16());

            Round = round;
            Times = times;
            Combat = combat;
            NextCombat = nextCombat;
            _claimedRewardIds.Clear();
            foreach (int id in rewardIds) _claimedRewardIds.Add(id);
            HasInfo = true;

            EventDispatcher.Emit(GlobalEvent.EVT_WELFARE_UPDATE, Proto.COMBAT_WELFARE_INFO);
            GameLog.Info("Welfare", "41723 战力福利面板 round={0} times={1} combat={2} nextCombat={3} claimedN={4}",
                round, times, combat, nextCombat, rewardIds.Count);
        }

        /// <summary>41724 摇奖(裸;成功后对标老端:round 变化则清空已领集合并切轮,否则原地追加本次 reward_id)。
        /// pt_417.erl:62-63,407-421。m4修复:老端 Handler41724(GrowthBenefitsController.ts:205-217)
        /// `if(is_new){ list={}; round=... } else { list[reward_id]=1 }`——reward_id 只在"同轮"分支追加,
        /// 换轮分支只清空+切轮,本次抽中的 reward_id **不**记入新一轮已领集合(本端此前无条件 Add 是错误镜像)。</summary>
        private void On41724(NetReader r)
        {
            int code = (int)r.ReadU32();
            int round = r.ReadU8();
            int times = r.ReadU8();
            int rewardId = r.ReadU16();
            long nextCombat = (long)r.ReadU64();

            if (code == 1)
            {
                bool isNewRound = round != Round;
                Times = times;
                NextCombat = nextCombat;
                if (isNewRound)
                {
                    Round = round;
                    _claimedRewardIds.Clear();
                }
                else
                {
                    _claimedRewardIds.Add(rewardId);
                }
                TipsManager.Toast("摇奖成功"); // 具体奖励物品名需查 config_combat_welfare_reward,面板层落地时再展开
            }
            else
            {
                TipsManager.Toast("摇奖失败(" + code + ")");
            }

            EventDispatcher.Emit(GlobalEvent.EVT_WELFARE_RESULT, Proto.COMBAT_WELFARE_DRAW, code);
            GameLog.Info("Welfare", "41724 战力福利摇奖 code={0} round={1} times={2} rewardId={3} nextCombat={4}",
                code, round, times, rewardId, nextCombat);
        }
    }
}
