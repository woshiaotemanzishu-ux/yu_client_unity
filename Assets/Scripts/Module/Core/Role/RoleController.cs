using Shenxiao.Common.Proto;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Scene;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 主角信息控制器(对标老客户端 RoleController 的 130xx 部分):
    /// 接服务端进游戏后主动推送的 13001(全量)/13002(经验)/13003(升级)/13006(货币),
    /// 写进 RoleModel,发 EVT_ROLE_INFO_UPDATE 供 UI 绑定。格式严格对标 yu_server pt_130。
    /// 这是"进游戏看到主角"的首个游戏内模块,后续 130xx 其余协议在此扩展。
    /// </summary>
    public sealed class RoleController : BaseController
    {
        public static readonly RoleController Instance = new RoleController();
        private RoleController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.ROLE_INFO, On13001);
            RegisterProtocal(Proto.ROLE_EXP, On13002);
            RegisterProtocal(Proto.ROLE_LEVEL, On13003);
            RegisterProtocal(Proto.ROLE_CURRENCY, On13006);
            RegisterProtocal(Proto.ROLE_BATTLE_UPDATE, On13033);
        }

        /// <summary>13001 主角全量(字段顺序严格对标 pt_130 write(13001))。</summary>
        private void On13001(NetReader r)
        {
            RoleModel m = RoleModel.Instance;
            m.RoleId = r.ReadU64();
            r.ReadString();                 // platform(平台名,暂不用)
            r.ReadU16();                    // server_num(服数)
            r.ReadString();                 // cserver_msg(跨服消息)
            m.ServerId = r.ReadU16();
            m.ServerName = r.ReadString();
            m.Figure = FigureProto.Read(r);
            m.BattleAttr = BattleAttrProto.Read(r);
            m.SceneId = (int)r.ReadU32();
            m.X = r.ReadU16();
            m.Y = r.ReadU16();
            m.DunId = (int)r.ReadU32();
            m.Exp = r.ReadU64();
            m.ExpLim = r.ReadU64();
            m.Gold = (int)r.ReadU32();       // 元宝
            m.BGold = (int)r.ReadU32();      // 绑元
            m.Coin = r.ReadU64();            // 铜币
            m.GCoin = (int)r.ReadU32();      // 帮贡
            m.CombatPower = r.ReadU64();
            m.GuildId = r.ReadU64();
            m.GuildName = r.ReadString();
            m.SetPeaceCd(r.ReadU16());       // peace_cd_time(和平切换冷却剩余秒,对标老端 MainRoleVo.ReadFrom13001:276)
            r.ReadU16();                     // hatred(仇恨值,对标老端 :277;暂不用)
            r.ReadU64();                     // team_id
            r.ReadU64();                     // mate_role_id
            r.ReadString();                  // ip
            r.ReadU16();                     // camp(阵营)
            r.ReadU32();                     // reg_time
            // level 不在 13001 里(由 13003 给);若从未收到 13003,用 figure.level 兜底
            if (m.Level == 0 && m.Figure != null) m.Level = m.Figure.level;
            m.MarkBaseInfoReady();
            GameLog.Info("Role", "★ 13001 主角: {0} 服[{1}]{2} 战力={3} 场景={4}({5},{6}) 铜币={7} 元宝={8}",
                m.Name, m.ServerId, m.ServerName, m.CombatPower, m.SceneId, m.X, m.Y, m.Coin, m.Gold);
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
        }

        /// <summary>13002 经验 "l"。</summary>
        private void On13002(NetReader r)
        {
            RoleModel.Instance.Exp = r.ReadU64();
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
        }

        /// <summary>13003 升级 "hll"。</summary>
        private void On13003(NetReader r)
        {
            RoleModel m = RoleModel.Instance;
            int oldLevel = m.Level;
            m.Level = r.ReadU16();
            m.Exp = r.ReadU64();
            m.ExpLim = r.ReadU64();
            GameLog.Info("Role", "13003 等级={0} 经验={1}/{2}", m.Level, m.Exp, m.ExpLim);
            if (oldLevel > 0 && m.Level > oldLevel)
            {
                MainRoleAgent.Current?.PlayLevelUpEffect();
            }
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
        }

        /// <summary>13033 战斗属性/战力更新(对标老端 ReadFrom13033:首 "l"=战力,后接战斗属性块)。
        /// 本端只取战力:更新 RoleModel.CombatPower;战力上升(且已有过非零旧值,排除进场首推)时发
        /// EVT_COMBAT_POWER_UP,由 MainUIFlow 弹「战力提升」窗(对标老端 MainUIController 绑 "fighting" 变化)。
        /// 包内战力之后的战斗属性块本端暂不解析——每条协议各拿独立 NetReader,读完即弃,部分读取安全。</summary>
        private void On13033(NetReader r)
        {
            RoleModel m = RoleModel.Instance;
            long oldPower = m.CombatPower;
            long newPower = r.ReadU64();        // 战力 "l"
            m.CombatPower = newPower;
            GameLog.Info("Role", "13033 战力 {0} -> {1}", oldPower, newPower);
            if (oldPower > 0 && newPower > oldPower)
            {
                EventDispatcher.Emit(GlobalEvent.EVT_COMBAT_POWER_UP, oldPower, newPower);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
        }

        /// <summary>13006 货币 "liii"(铜币/元宝/绑元/帮贡)。</summary>
        private void On13006(NetReader r)
        {
            RoleModel m = RoleModel.Instance;
            m.Coin = r.ReadU64();
            m.Gold = (int)r.ReadU32();
            m.BGold = (int)r.ReadU32();
            m.GCoin = (int)r.ReadU32();
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
        }
    }
}
