using System.Collections.Generic;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.AutoFight;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene;
using UnityEngine;

namespace Shenxiao.Module.Core.Relive
{
    /// <summary>
    /// 复活协议控制器(对标老客户端 commonController/ReliveController.ts,独立于 FightController——一模块一控制器)。
    /// 管:20004(复活请求/结果)、20009(复活时间戳查询)、20017(回城复活疲劳查询);
    /// 订阅 <see cref="GlobalEvent.EVT_ROLE_DEAD"/>(FightController.On20013 发)驱动"停自动战斗+死亡动作+开复活窗"。
    ///
    /// 复活弹窗路由(<see cref="OpenReliveWindow"/>)对标老端 ReliveController.OpenReliveView() 场景分支表——
    /// 完整表拷贝存档在该方法注释里;Unity 现阶段没有 Boss域/九霄/极限本/圣战/跨服圣域等场景类型判定
    /// (对应系统未移植,玩家进不去这些场景,降级安全),只能按 RoleModel.DunId 粗判"是否在副本",
    /// 其余一律兜底 MainUIReliveView。
    /// </summary>
    public sealed class ReliveController : BaseController
    {
        public static readonly ReliveController Instance = new ReliveController();
        private ReliveController() { }

        // 服务端复活方式白名单(对标 pp_battle.erl:82-91:函数头范围 1..25,内层白名单只放行这 19 个值;
        // 2/4/5/7/8/10 虽在范围内但不在白名单,发了服务端只打日志不回包,客户端会挂起等回复,严禁发)。
        private static readonly HashSet<int> AllowedReliveModes = new HashSet<int>
        {
            1, 3, 6, 9, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25,
        };

        private const float RequestThrottleSec = 1f; // 对标 request_relive_lase_time 节流 1 秒
        private float _lastRequestAt = float.NegativeInfinity;

        // 死亡前若挂机开着,记录以便复活成功后恢复(对标老端 isRelive 分支:复活后恢复挂机/清点选目标)。
        private bool _wasAutoFighting;

        protected override void Register()
        {
            RegisterProtocal(Proto.RELIVE_REQUEST, On20004);
            RegisterProtocal(Proto.RELIVE_INFO, On20009);
            RegisterProtocal(Proto.RELIVE_TIRED, On20017);

            EventDispatcher.On(GlobalEvent.EVT_ROLE_DEAD, OnRoleDead);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_DEAD, OnRoleDead);
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            ReliveModel.Instance.Clear();
            _wasAutoFighting = false;
            _lastRequestAt = float.NegativeInfinity;
            base.Dispose();
        }

        /// <summary>进场景/重连"报到"查询复活状态(对标老端 FightController.ts:870 GAME_START → SendFmtToGame(20009))。
        /// 顺带查一次回城复活疲劳(20017 亦可查询,老端无固定 GAME_START 绑定,这里跟着 20009 一起问,无害)。</summary>
        private void OnGameStart()
        {
            SendFmt(Proto.RELIVE_INFO);
            SendFmt(Proto.RELIVE_TIRED);
        }

        /// <summary>请求复活(对标 ReliveController.ts:200 ReLiveRequest)。1 秒节流(immediate 强制跳过节流);
        /// mode 不在服务端白名单 → log 拒发(白名单外服务端静默不回包,见 Proto.RELIVE_REQUEST 注释)。</summary>
        public void RequestRelive(int mode, bool immediate = false)
        {
            if (!AllowedReliveModes.Contains(mode))
            {
                GameLog.Warn("Relive", "RequestRelive mode={0} 不在服务端白名单(pp_battle.erl:84-91),拒绝发送", mode);
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (!immediate && now - _lastRequestAt < RequestThrottleSec)
            {
                GameLog.Info("Relive", "RequestRelive mode={0} 1秒节流内跳过(对标 request_relive_lase_time)", mode);
                return;
            }

            _lastRequestAt = now;
            SendFmt(Proto.RELIVE_REQUEST, "c", mode);
            GameLog.Info("Relive", "send 20004 复活请求 mode={0}", mode);
        }

        /// <summary>20004 复活结果(对标 ReliveController.ts:66-127)。flag 全表文案逐字对标老端。</summary>
        private void On20004(NetReader r)
        {
            int type = r.ReadU8();
            int flag = r.ReadU8();
            GameLog.Info("Relive", "recv 20004 type={0} flag={1}", type, flag);

            switch (flag)
            {
                case 1:
                case 12: // REVIVE_BOSS/REVIVE_ASHES 成功时服务端把 Res 改写成 12(pp_battle.erl:102-107),按成功路径走。
                    OnReliveSuccess(type);
                    break;
                case 0:
                case 2:
                    TipsManager.Toast("绑定勾玉不足");
                    break;
                case 3:
                    TipsManager.Toast("角色未死亡");
                    break;
                case 4:
                    TipsManager.Toast("复活时间未到");
                    break;
                case 5:
                    TipsManager.Toast("复活冷却时间未到");
                    break;
                case 6:
                    TipsManager.Toast("铜币不足");
                    break;
                case 7:
                    TipsManager.Toast("复活次数已满");
                    break;
                case 8:
                    TipsManager.Toast("元宝复活次数已满");
                    break;
                case 9:
                    TipsManager.Toast("本副本不能复活");
                    break;
                case 10:
                    TipsManager.Toast("复活方式出错");
                    break;
                case 11:
                    TipsManager.Toast("物品不足");
                    break;
                case 255:
                    break; // 静默无提示(对标老端)
                default:
                    GameLog.Warn("Relive", "20004 未知 flag={0}", flag);
                    break;
            }
        }

        /// <summary>复活成功落地(flag==1 或 12):清死亡态 + Emit 成功事件(MainUIReliveView 关窗)+
        /// 按 isRelive 语义恢复死前的挂机 + MainRoleAgent 恢复 idle。血量/坐标由服务端后续场景协议下发,
        /// 本处不本地改 HP(对标规格 §2.5)。</summary>
        private void OnReliveSuccess(int type)
        {
            ReliveModel.Instance.ClearDead();
            EventDispatcher.Emit(GlobalEvent.EVT_RELIVE_SUCCESS, type);

            if (_wasAutoFighting)
            {
                _wasAutoFighting = false;
                AutoFightModel.Instance.SetAutoFight(true); // 对标老端 isRelive 分支:复活后恢复挂机
                GameLog.Info("Relive", "复活成功:恢复死前挂机(对标老端 isRelive)");
            }

            MainRoleAgent agent = MainRoleAgent.Current;
            agent?.PlayReviveIdle();
            GameLog.Info("Relive", "复活成功 type={0}", type);
        }

        /// <summary>20009 复活时间戳查询回包(对标 ReliveController.ts:60-64)。</summary>
        private void On20009(NetReader r)
        {
            int canRelive = r.ReadU8();
            long nextReviveTime = r.ReadU32();
            ReliveModel.Instance.SetReviveInfo(canRelive != 0, nextReviveTime);
            GameLog.Info("Relive", "recv 20009 can_relive={0} next_relive_time={1}", canRelive, nextReviveTime);
            EventDispatcher.Emit(GlobalEvent.EVT_RELIVE_INFO, nextReviveTime);
        }

        /// <summary>20017 回城复活疲劳查询回包/主动推送。</summary>
        private void On20017(NetReader r)
        {
            int reviveNum = r.ReadU16();
            long endTime = r.ReadU32();
            ReliveModel.Instance.SetTired(reviveNum, endTime);
            GameLog.Info("Relive", "recv 20017 revive_num={0} end_time={1}", reviveNum, endTime);
            EventDispatcher.Emit(GlobalEvent.EVT_RELIVE_TIRED, reviveNum, endTime);
        }

        /// <summary>主角死亡(对标老端 Fire(SHOWRELIVEWINDOW,0)):①停自动战斗(记住死前状态供复活后恢复);
        /// ②MainRoleAgent 播死亡动作(没有素材就 log TODO,不硬造);③按场景路由开复活窗。</summary>
        private void OnRoleDead()
        {
            _wasAutoFighting = AutoFightModel.Instance.AutoFightState;
            if (_wasAutoFighting)
            {
                AutoFightModel.Instance.SetAutoFight(false);
                GameLog.Info("Relive", "主角死亡:停自动战斗(死前挂机={0},复活成功后恢复)", _wasAutoFighting);
            }

            MainRoleAgent agent = MainRoleAgent.Current;
            if (agent == null || !agent.PlayDeadAnim())
            {
                GameLog.Info("Relive", "TODO 主角死亡动作未播放(MainRoleAgent 不在场景/模型无 death 动作素材)");
            }

            OpenReliveWindow();
        }

        /// <summary>
        /// 复活窗路由(对标老端 ReliveController.OpenReliveView():148-198 场景分支表,完整拷贝存档如下):
        ///   BossScene/SeaHegemony/SeaAsser → IsAbyssBossScene/IsSuitBossScene 为真→"BossSuitReliveView",否则→"BossFieldReliveView"
        ///   IsDungeonPartner → "MainUIReliveView"
        ///   IsKfHolyAreaScene → "BossFieldReliveView"
        ///   IsNineSkyFightScene → "NineSkyReliveView"
        ///   IsGuildFightScene → "BossFieldReliveView"(注释显示曾用 GuildFightReliveView,当前路由未启用)
        ///   FieldScene/DWScene/ConvoyScene → "BossFieldReliveView"
        ///   PolarDungeon/KFPolarDungeon → "DungeonPolarReliveView"
        ///   Eternity/HolyBattleFight/DiamondFight/BrightSea → return(不开任何弹窗)
        ///   DungeonScene(非服务端控制/非极限本)或 TopPk/Kf1vn → return(不开)
        ///   其余(city/普通场景兜底) → "MainUIReliveView"
        ///
        /// Unity 现阶段 Boss域/九霄/极限本/圣战/跨服圣域/公会战等场景类型判定系统均未移植(玩家目前也进不去这些
        /// 场景,降级安全),无法复刻上面的完整分支表。只能用现有 <see cref="RoleModel.DunId"/> 粗判"是否在副本":
        ///   DunId!=0(副本内)→ 照老端"非服控普通副本 return 不开窗"分支,只 log,不臆造场景类型判断;
        ///   DunId==0(野外/主城)→ 兜底 "MainUIReliveView"(对标老端"city/普通场景兜底"分支)。
        /// TODO: Boss域/九霄/公会战/极限本等场景系统移植后,按上表补齐对应 ReliveView 的精确路由。
        /// </summary>
        private void OpenReliveWindow()
        {
            if (RoleModel.Instance.DunId != 0)
            {
                GameLog.Info("Relive", "OpenReliveWindow: 副本内(DunId={0}),对标老端非服控副本分支 return,不开复活窗",
                    RoleModel.Instance.DunId);
                return;
            }

            _ = MainUIFlow.ShowReliveAsync();
            GameLog.Info("Relive", "OpenReliveWindow: 兜底 MainUIReliveView");
        }
    }
}
