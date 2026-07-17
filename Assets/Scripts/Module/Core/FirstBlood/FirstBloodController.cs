using System.Collections.Generic;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.FirstBlood
{
    /// <summary>
    /// 首杀/首通(FirstBlood)控制器(自动循环 轮18 便宜活批 PK3 实做)。协议号见 Proto.cs
    /// FIRSTBLOOD_*(188xx,pt_188.erl 全 8 号:18800-18807)。type 收口分发:96=Boss 首杀(UI 归本模块)/
    /// 97=副本首通(UI 归 DungeonPartner)/105=神符本首通(UI 归 DungeonRune),handler 统一在本类落
    /// <see cref="FirstBloodModel"/> 分桶+发 <see cref="GlobalEvent.EVT_FIRSTBLOOD_UPDATE"/>,消费方各自按需读取
    /// 对应桶(留档,本轮不接 UI)。⚠18802 的 Type==96&&Subtype==2 组合服务端 handle 已注释
    /// (pp_boss_first_blood_plus.erl:47-56),<see cref="ClaimReward"/> 内置防御门拒绝该组合。
    /// 老端触发链镜像(r18_oldclient_cheapwins.md §4):GAME_START 发 18801(96,1)(97,1)+18805(105,1);
    /// CHANGE_LEVEL(本端用 EVT_ROLE_INFO_UPDATE + 等级去抖镜像)B2修复:老端 CHANGE_LEVEL 绑在主角 VO 上
    /// (FirstBloodController.ts:57-63),仅在 level==LIMIT_LV(130,精确相等非≥,FirstBloodModel.ts:28)时
    /// 补发 18801(96,1) 一条,无 97·1(该行系注释掉的死代码,ts:61)、18805 无此触发。
    /// 收到 18801 后 type==96 分支按列表逐条发 18806 详情查询(老端 Controller:106-139 循环在 if(type==96)
    /// 块内,B3修复;97 只走红点不镜像)。既有
    /// <see cref="FirstBloodFlow"/>/<see cref="FirstBloodBootstrap"/>(窗口编排壳)与本文件相互独立,勿动。
    /// </summary>
    public sealed class FirstBloodController : BaseController
    {
        public static readonly FirstBloodController Instance = new FirstBloodController();
        private FirstBloodController() { }

        // CHANGE_LEVEL 去抖(EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时重发)。
        private int _lastLevel = -1;

        /// <summary>FirstBloodModel.ts:28 limitLv 硬编码常量,CHANGE_LEVEL 精确等于该值才补发 18801(96,1)。</summary>
        private const int LIMIT_LV = 130;

        protected override void Register()
        {
            RegisterProtocal(Proto.FIRSTBLOOD_ERROR, On18800);
            RegisterProtocal(Proto.FIRSTBLOOD_LIST, On18801);
            RegisterProtocal(Proto.FIRSTBLOOD_REWARD_CLAIM, On18802);
            RegisterProtocal(Proto.FIRSTBLOOD_NOTICE_PUSH, On18803);
            RegisterProtocal(Proto.FIRSTBLOOD_RUNE_REWARD_CLAIM, On18804);
            RegisterProtocal(Proto.FIRSTBLOOD_REDPOINT_LIST, On18805);
            RegisterProtocal(Proto.FIRSTBLOOD_DETAIL_QUERY, On18806);
            RegisterProtocal(Proto.FIRSTBLOOD_GUILD_REWARD_CLAIM, On18807);

            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            FirstBloodModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        // ---- 老端触发链镜像:GAME_START 发 18801(96,1)(97,1)+18805(105,1)(r18_oldclient_cheapwins.md §4) ----
        private void OnGameStart()
        {
            RequestList(FirstBloodModel.TYPE_BOSS, 1);
            RequestList(FirstBloodModel.TYPE_DUNGEON, 1);
            RequestRedPointList(FirstBloodModel.TYPE_RUNE, 1);
        }

        // B2修复:CHANGE_LEVEL 仅在 level 精确等于 LIMIT_LV(130)时补发 18801(96,1) 一条,无 97·1
        // (老端 FirstBloodController.ts:57-63:role_vo.Bind(CHANGE_LEVEL) 里 `if (level == model.limitLv)`
        // 只发一次 SendFmtToGame(18801,"cc",96,1),96·2 与 97·1 均无——96·2 行在老端就是被注释掉的死代码)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            if (role.Level == LIMIT_LV)
            {
                RequestList(FirstBloodModel.TYPE_BOSS, 1);
            }
        }

        // ---- 发送封装(消费方 UI 待接,先提供 1:1 wire 包装) ----

        public void RequestList(int type, int subtype) => SendFmt(Proto.FIRSTBLOOD_LIST, "cc", type, subtype);

        /// <summary>18802 领取首杀/首通奖励。⚠Type==96(Boss)&&Subtype==2 服务端 handle 已注释
        /// (pp_boss_first_blood_plus.erl:47-56),严禁发送——本方法内置防御门,命中直接拒绝不发包。</summary>
        public void ClaimReward(int type, int subtype, int bossId)
        {
            if (type == FirstBloodModel.TYPE_BOSS && subtype == 2)
            {
                GameLog.Error("FirstBlood", "18802 拒绝发送 Type=96&&Subtype=2 组合(服务端 handle 已注释,pp_boss_first_blood_plus.erl:47-56)");
                return;
            }
            SendFmt(Proto.FIRSTBLOOD_REWARD_CLAIM, "cci", type, subtype, bossId);
        }

        /// <summary>18804 神符本(type=105)专属领奖(老端发送点 dungeonRune/DungeonRuneFirstView.ts:133)。</summary>
        public void ClaimRuneReward(int type, int subtype, int dunId) => SendFmt(Proto.FIRSTBLOOD_RUNE_REWARD_CLAIM, "cci", type, subtype, dunId);

        public void RequestRedPointList(int type, int subtype) => SendFmt(Proto.FIRSTBLOOD_REDPOINT_LIST, "cc", type, subtype);

        public void RequestDetail(int type, int subtype, int bossId) => SendFmt(Proto.FIRSTBLOOD_DETAIL_QUERY, "cci", type, subtype, bossId);

        /// <summary>18807 领全服归属奖(MainView.ts:152)。</summary>
        public void ClaimGuildReward(int type, int subtype, int bossId) => SendFmt(Proto.FIRSTBLOOD_GUILD_REWARD_CLAIM, "cci", type, subtype, bossId);

        // ---- 18800: 纯推送错误码(Code:32,无 read 子句) ----
        private void On18800(NetReader r)
        {
            int code = (int)r.ReadU32();
            FirstBloodModel.Instance.LastErrorCode = code;
            GameLog.Info("FirstBlood", "18800 错误推送: code={0}", code);
            EventDispatcher.Emit(GlobalEvent.EVT_FIRSTBLOOD_UPDATE, 0, 0);
        }

        // ---- 18801: Type:8, Subtype:8, FirstBloodList[u16×item_to_bin_0(11字段,DressList二层嵌套)] ----
        private void On18801(NetReader r)
        {
            int type = r.ReadU8();
            int subtype = r.ReadU8();
            int count = r.ReadU16();
            var list = new List<FirstBloodModel.ListEntry>(count);
            for (int i = 0; i < count; i++)
            {
                var e = new FirstBloodModel.ListEntry
                {
                    ShowFirstBlood = r.ReadU8(),
                    BossId = (int)r.ReadU32(),
                    FirstBloodRoleId = r.ReadU64(),
                    RoleName = r.ReadString(),
                    RoleLv = r.ReadU16(),
                    RoleSex = r.ReadU8(),
                    RoleCarrer = r.ReadU8(),
                    Picture = r.ReadString(),
                    PictureVer = (int)r.ReadU32(),
                };
                int dressCount = r.ReadU16();
                for (int d = 0; d < dressCount; d++)
                {
                    e.DressList.Add(new FirstBloodModel.DressEntry { DressType = r.ReadU8(), DressId = (int)r.ReadU32() });
                }
                e.RewardState = r.ReadU8();
                list.Add(e);
            }

            FirstBloodModel m = FirstBloodModel.Instance;
            m.ListByType[type] = list;
            m.ListSubtypeByType[type] = subtype;
            GameLog.Info("FirstBlood", "18801 列表: type={0} subtype={1} count={2}", type, subtype, count);
            EventDispatcher.Emit(GlobalEvent.EVT_FIRSTBLOOD_UPDATE, type, subtype);

            // B3修复:老端 Controller:106-139 循环发 18806 的代码在 `if (scmd.type == 96)` 块内(ts:136-139),
            // 97(副本首通)只走红点(ADD_FIRST_BLOOD_ICON),不做逐条详情查询——本端此前对所有 type 都镜像发,已收窄。
            if (type == FirstBloodModel.TYPE_BOSS)
            {
                foreach (FirstBloodModel.ListEntry e in list)
                {
                    RequestDetail(type, subtype, e.BossId);
                }
            }
        }

        // ---- 18802: Type:8, Subtype:8, Code:32, BossId:32, RewardList(object_list) ----
        private void On18802(NetReader r)
        {
            int type = r.ReadU8();
            int subtype = r.ReadU8();
            int code = (int)r.ReadU32();
            int bossId = (int)r.ReadU32();
            List<FirstBloodModel.RewardObj> rewards = ReadRewardObjList(r);

            FirstBloodModel m = FirstBloodModel.Instance;
            m.LastClaimType = type;
            m.LastClaimSubtype = subtype;
            m.LastClaimCode = code;
            m.LastClaimBossId = bossId;
            m.LastClaimRewardList.Clear();
            m.LastClaimRewardList.AddRange(rewards);

            // B4修复:成功分支补三路镜像(老端 On18802 code==1 内按 type 分流,ts:148-181)。
            if (code == 1)
            {
                if (type == FirstBloodModel.TYPE_BOSS)
                {
                    RequestList(FirstBloodModel.TYPE_BOSS, subtype); // ts:149
                }
                else if (type == FirstBloodModel.TYPE_DUNGEON && subtype == 1)
                {
                    RequestList(FirstBloodModel.TYPE_DUNGEON, 1); // ts:162
                }
                else if (type == FirstBloodModel.TYPE_RUNE && subtype == 1)
                {
                    // ts:164-181:本地置位——红点桶里 DunId 命中 bossId(type==105语境下 bossId 即 dun_id,
                    // 与 18804/18805 复用同一整数槽位)的条目 ShowPoint 置 2;RuneClaim 结果 reward_state 置 1
                    // (按 Model 现有容器实现,复合键 type@subtype@dun_id 本端简化为仅 DunId,见 On18804 注释)。
                    if (m.RedPointByType.TryGetValue(FirstBloodModel.TYPE_RUNE, out List<FirstBloodModel.RedPointEntry> redList))
                    {
                        for (int i = 0; i < redList.Count; i++)
                        {
                            if (redList[i].DunId != bossId) continue;
                            FirstBloodModel.RedPointEntry e = redList[i];
                            e.ShowPoint = 2;
                            redList[i] = e;
                            break;
                        }
                    }
                    if (m.RuneClaimByDunId.TryGetValue(bossId, out FirstBloodModel.RuneClaimState state))
                    {
                        state.RewardState = 1;
                    }
                }
            }

            GameLog.Info("FirstBlood", "18802 领奖结果: type={0} subtype={1} code={2} bossId={3} rewardN={4}",
                type, subtype, code, bossId, rewards.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_FIRSTBLOOD_UPDATE, type, subtype);
        }

        // ---- 18803: Type:8, Subtype:8, FirstBloodRoleName:string, BossName:string(无Code,纯提醒推送) ----
        private void On18803(NetReader r)
        {
            int type = r.ReadU8();
            int subtype = r.ReadU8();
            string roleName = r.ReadString();
            string bossName = r.ReadString();

            FirstBloodModel m = FirstBloodModel.Instance;
            m.NoticeType = type;
            m.NoticeSubtype = subtype;
            m.NoticeRoleName = roleName;
            m.NoticeBossName = bossName;
            GameLog.Info("FirstBlood", "18803 首杀提醒: type={0} subtype={1} role={2} boss={3}", type, subtype, roleName, bossName);
            EventDispatcher.Emit(GlobalEvent.EVT_FIRSTBLOOD_UPDATE, type, subtype);
        }

        // ---- 18804: Type:8, Subtype:8, DunId:32, RewardState:8, PassRoleList[u16×item_to_bin_2(10字段,末尾Time:64,二层嵌套)] ----
        private void On18804(NetReader r)
        {
            int type = r.ReadU8();
            int subtype = r.ReadU8();
            int dunId = (int)r.ReadU32();
            int rewardState = r.ReadU8();
            int passCount = r.ReadU16();
            var state = new FirstBloodModel.RuneClaimState { RewardState = rewardState };
            for (int i = 0; i < passCount; i++)
            {
                var p = new FirstBloodModel.PassRoleEntry
                {
                    RoleId = r.ReadU64(),
                    RoleName = r.ReadString(),
                    Rank = r.ReadU8(),
                    RoleLv = r.ReadU16(),
                    RoleSex = r.ReadU8(),
                    RoleCarrer = r.ReadU8(),
                    Picture = r.ReadString(),
                    PictureVer = (int)r.ReadU32(),
                };
                int dressCount = r.ReadU16();
                for (int d = 0; d < dressCount; d++)
                {
                    p.DressList.Add(new FirstBloodModel.DressEntry { DressType = r.ReadU8(), DressId = (int)r.ReadU32() });
                }
                p.Time = r.ReadU64();
                state.PassRoleList.Add(p);
            }

            FirstBloodModel.Instance.RuneClaimByDunId[dunId] = state;
            GameLog.Info("FirstBlood", "18804 神符本领奖: type={0} subtype={1} dunId={2} rewardState={3} passN={4}",
                type, subtype, dunId, rewardState, passCount);
            EventDispatcher.Emit(GlobalEvent.EVT_FIRSTBLOOD_UPDATE, type, subtype);
        }

        // ---- 18805: Type:8, Subtype:8, RedPointList[u16×{DunId:32, ShowPoint:8}] ----
        private void On18805(NetReader r)
        {
            int type = r.ReadU8();
            int subtype = r.ReadU8();
            int count = r.ReadU16();
            var list = new List<FirstBloodModel.RedPointEntry>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(new FirstBloodModel.RedPointEntry { DunId = (int)r.ReadU32(), ShowPoint = r.ReadU8() });
            }

            FirstBloodModel.Instance.RedPointByType[type] = list;
            GameLog.Info("FirstBlood", "18805 红点列表: type={0} subtype={1} count={2}", type, subtype, count);
            EventDispatcher.Emit(GlobalEvent.EVT_FIRSTBLOOD_UPDATE, type, subtype);
        }

        // ---- 18806: Type:8, Subtype:8, BossId:32, SharedStatus:8(收到18801后逐条查,老端 Controller:138 镜像) ----
        private void On18806(NetReader r)
        {
            int type = r.ReadU8();
            int subtype = r.ReadU8();
            int bossId = (int)r.ReadU32();
            int sharedStatus = r.ReadU8();

            FirstBloodModel.Instance.SharedStatusByBossId[bossId] = sharedStatus;
            GameLog.Info("FirstBlood", "18806 详情: type={0} subtype={1} bossId={2} sharedStatus={3}", type, subtype, bossId, sharedStatus);
            EventDispatcher.Emit(GlobalEvent.EVT_FIRSTBLOOD_UPDATE, type, subtype);
        }

        // ---- 18807: 结构同 18802(Type:8, Subtype:8, Code:32, BossId:32, RewardList(object_list)) ----
        private void On18807(NetReader r)
        {
            int type = r.ReadU8();
            int subtype = r.ReadU8();
            int code = (int)r.ReadU32();
            int bossId = (int)r.ReadU32();
            List<FirstBloodModel.RewardObj> rewards = ReadRewardObjList(r);

            FirstBloodModel m = FirstBloodModel.Instance;
            m.LastGuildClaimType = type;
            m.LastGuildClaimSubtype = subtype;
            m.LastGuildClaimCode = code;
            m.LastGuildClaimBossId = bossId;
            m.LastGuildClaimRewardList.Clear();
            m.LastGuildClaimRewardList.AddRange(rewards);
            GameLog.Info("FirstBlood", "18807 全服归属奖: type={0} subtype={1} code={2} bossId={3} rewardN={4}",
                type, subtype, code, bossId, rewards.Count);
            EventDispatcher.Emit(GlobalEvent.EVT_FIRSTBLOOD_UPDATE, type, subtype);
        }

        // ---- object_list 通用读取(pt.erl write_object_list,u16 计数 + {Type:8,GoodsId:32,Num:32}) ----
        private static List<FirstBloodModel.RewardObj> ReadRewardObjList(NetReader r)
        {
            int count = r.ReadU16();
            var list = new List<FirstBloodModel.RewardObj>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(new FirstBloodModel.RewardObj { Type = r.ReadU8(), GoodsId = (int)r.ReadU32(), Num = (int)r.ReadU32() });
            }
            return list;
        }
    }
}
