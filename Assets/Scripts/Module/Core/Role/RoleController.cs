using System.Collections.Generic;
using Shenxiao.Common.Proto;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Scene;
using Shenxiao.Module.Core.Setting;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 主角信息控制器(对标老客户端 RoleController 的 130xx 部分):
    /// 接服务端进游戏后主动推送的 13001(全量)/13002(经验)/13003(升级)/13006(货币),
    /// 写进 RoleModel,发 EVT_ROLE_INFO_UPDATE 供 UI 绑定。格式严格对标 yu_server pt_130。
    /// 这是"进游戏看到主角"的首个游戏内模块,后续 130xx 其余协议在此扩展。
    ///
    /// 轮5 扩容:13011(世界等级)/13013(查看他人Figure)/13017(托管状态)/13020(被动技能解锁推送)/
    /// 13036(经验飘字推送)/13046(转职冷却,13045 转职本体归 <see cref="TransferJob.TransferJobController"/>)/
    /// 13080-13083(头像三件套)/13086(玩家指定数据)/13089(终身次数+1)+ 42601/42602/42604(改名全链路)。
    /// </summary>
    public sealed class RoleController : BaseController
    {
        public static readonly RoleController Instance = new RoleController();
        private RoleController() { }

        /// <summary>改名类型(对标服务端 pp_rename:judge_goods Type 分支:1免费/2钻石/3改名卡)。</summary>
        public const int RENAME_TYPE_FREE = 1;
        public const int RENAME_TYPE_GOLD = 2;
        public const int RENAME_TYPE_CARD = 3;

        /// <summary>上一次 42604 校验发送时携带的 type,42604 通过后原样透传给二次确认→42601(单流程内无并发,足够)。</summary>
        private int _pendingRenameType;

        protected override void Register()
        {
            RegisterProtocal(Proto.ROLE_INFO, On13001);
            RegisterProtocal(Proto.ROLE_EXP, On13002);
            RegisterProtocal(Proto.ROLE_LEVEL, On13003);
            RegisterProtocal(Proto.ROLE_CURRENCY, On13006);
            RegisterProtocal(Proto.ROLE_BATTLE_UPDATE, On13033);

            // ----- 角色成长补全(轮5) -----
            RegisterProtocal(Proto.ROLE_WORLD_LEVEL, On13011);
            RegisterProtocal(Proto.ROLE_FIGURE_QUERY, On13013);
            RegisterProtocal(Proto.ROLE_DEPOSIT_STATE, On13017);
            RegisterProtocal(Proto.ROLE_SKILL_PASSIVE_UNLOCK, On13020);
            RegisterProtocal(Proto.ROLE_EXP_FLOAT, On13036);
            RegisterProtocal(Proto.TRANSFER_JOB_COOLDOWN, On13046);
            RegisterProtocal(Proto.ROLE_HEAD_LIST, On13080);
            RegisterProtocal(Proto.ROLE_HEAD_ACTIVATE_PUSH, On13081);
            RegisterProtocal(Proto.ROLE_HEAD_SET, On13083);
            RegisterProtocal(Proto.ROLE_MISC_COUNTERS, On13086);
            RegisterProtocal(Proto.ROLE_LIFELONG_INCREMENT, On13089);

            // ----- 改名(轮5) -----
            RegisterProtocal(Proto.RENAME_SUBMIT, On42601);
            RegisterProtocal(Proto.RENAME_FREE_CHECK, On42602);
            RegisterProtocal(Proto.RENAME_CHECK, On42604);
        }

        /// <summary>GAME_START 裸发族:13011/13017/13046/13080/13086(均无请求体,回包见对应 On 方法)。
        /// 由 GameStartController.RequestStartupPackets 调用。</summary>
        public void RequestGrowthPackets()
        {
            SendFmt(Proto.ROLE_WORLD_LEVEL);
            SendFmt(Proto.ROLE_DEPOSIT_STATE);
            SendFmt(Proto.TRANSFER_JOB_COOLDOWN);
            SendFmt(Proto.ROLE_HEAD_LIST);
            SendFmt(Proto.ROLE_MISC_COUNTERS);
            GameLog.Info("Role", "request 13011/13017/13046/13080/13086(GAME_START 裸发族)");
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
            // position 不在 13001 本体里,但 Figure 块自带 position/position_name(FigureProto.cs:51-52)——
            // 登录即种子落值,避免"首个 40015 到达前 IsGuildMaster()/职位门控恒为 false"的短暂竞态
            // (老端 mainRoleVo 随登录角色数据即带 position;40015 到达后照常覆盖为准)。
            if (m.Figure != null)
            {
                if (m.Figure.Raw.TryGetValue("position", out object posObj)) m.GuildPosition = System.Convert.ToInt32(posObj);
                if (m.Figure.Raw.TryGetValue("position_name", out object posNameObj)) m.GuildPositionName = posNameObj as string ?? "";
            }
            m.SetPeaceCd(r.ReadU16());       // peace_cd_time(和平切换冷却剩余秒,对标老端 MainRoleVo.ReadFrom13001:276)
            r.ReadU16();                     // hatred(仇恨值,对标老端 :277;暂不用)
            r.ReadU64();                     // team_id
            r.ReadU64();                     // mate_role_id
            r.ReadString();                  // ip
            r.ReadU16();                     // camp(阵营)
            m.RegisterTime = r.ReadU32();    // reg_time(Unix 秒)
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

        // ===================== 角色成长补全(轮5)=====================

        /// <summary>13011 世界等级 "Hh"(⚠反直觉:第一个字段 16位有符号,第二个 16位无符号,对标服务端
        /// pt_130.erl write(13011,[ExpAdd,ServerLv]))。消费方(EquipmentView worldLb 世界等级面板)接。</summary>
        private void On13011(NetReader r)
        {
            RoleModel m = RoleModel.Instance;
            m.WorldLvExp = r.ReadI16();
            m.WorldLv = r.ReadU16();
            GameLog.Info("Role", "13011 世界等级={0} 经验加成={1}%", m.WorldLv, m.WorldLvExp);
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
        }

        /// <summary>请求查看他人 Figure(对标老端 module_id 是调用方自定义来源标签)。</summary>
        public void RequestFigureInfo(int serverId, long roleId, int moduleId)
        {
            SendFmt(Proto.ROLE_FIGURE_QUERY, "hlh", serverId, roleId, moduleId);
            GameLog.Info("Role", "send 13013 查看Figure server={0} roleId={1} moduleId={2}", serverId, roleId, moduleId);
        }

        /// <summary>13013 他人 Figure 回包:server_id:h, player_num:h, player_id:l, module_id:h, fighting:l,
        /// +FigureProto 块, platform:s。消费方(排行榜/记录列表"点开看模型")未接线,Emit 供后补。</summary>
        private void On13013(NetReader r)
        {
            var vo = new RoleFigureInfo
            {
                ServerId = r.ReadU16(),
                PlayerNum = r.ReadU16(),
                PlayerId = r.ReadU64(),
                ModuleId = r.ReadU16(),
                Fighting = r.ReadU64(),
            };
            vo.Figure = FigureProto.Read(r);
            vo.Platform = r.ReadString();
            GameLog.Info("Role", "13013 他人Figure: server={0} playerId={1} moduleId={2} fighting={3} name={4}",
                vo.ServerId, vo.PlayerId, vo.ModuleId, vo.Fighting, vo.Figure?.name);
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_FIGURE_RETURN, vo);
        }

        /// <summary>13017 托管(自动战斗)状态 "c"(1=托管中)。战斗表现门控消费方(Scene/FightMovie 多处)
        /// 未接线,TODO;本处只落 RoleModel。</summary>
        private void On13017(NetReader r)
        {
            RoleModel.Instance.DepositState = r.ReadU8() == 1;
            GameLog.Info("Role", "13017 托管状态={0}(战斗表现门控消费未接,TODO)", RoleModel.Instance.DepositState);
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
        }

        /// <summary>13020 被动技能解锁通知(S2C 专用,禁发):len:h + skill_id:i×len,逐个按
        /// config_skill[id].type==2 静默并入 SkillManager 技能列表(level=1),无事件Fire、无toast(对标老端)。</summary>
        private void On13020(NetReader r)
        {
            int len = r.ReadU16();
            for (int i = 0; i < len; i++)
            {
                int skillId = (int)r.ReadU32();
                Skill.SkillManager.Instance.AddPassiveSkillFromPush(skillId);
            }
            GameLog.Info("Role", "13020 被动技能解锁推送 count={0}(静默并入,无toast,对标老端)", len);
        }

        /// <summary>13036 经验获得飘字(S2C 专用,禁发) "clh"(expType:c, exp:l, percent:h)。
        /// 分支逐条对标老端 RoleController.ts:305-367(见 Proto.ROLE_EXP_FLOAT 注释);
        /// Message.showExp 专用图标飘字通道未移植,降级走 TipsManager.Float/Toast(现有飘字/toast 通道)。</summary>
        private void On13036(NetReader r)
        {
            int expType = r.ReadU8();
            long exp = r.ReadU64();
            int percent = r.ReadU16();
            GameLog.Info("Role", "13036 经验飘字 expType={0} exp={1} percent={2}", expType, exp, percent);

            if (expType == 3)
            {
                // 经验副本刷经验条:BaseDungeonModel.UPDATE_EXP 对应通道未移植,仅记录;不 return,
                // 继续走底部通用飘字(对标老端双重生效:进度条 + 飘字)。
                GameLog.Info("Role", "13036 expType=3 经验副本刷条(BaseDungeonModel.UPDATE_EXP 未移植,TODO)");
            }
            else if (expType == 6 || expType == 0)
            {
                TipsManager.Float(FormatExpPlain(exp));
                return;
            }
            else if (expType == 14 || expType == 8)
            {
                TipsManager.Float(FormatExpPercent(exp, percent));
                return;
            }
            else if (expType == 2) // 老端 "expType==6||8||2" 分支,6/8 均已提前 return,实际只剩 2 可达
            {
                TipsManager.Toast("获得经验 x" + exp);
                return;
            }

            // 通用兜底(expType==3 或未显式匹配的 1/4/5/7/9…):对标老端仅副本/宴会场景才飘字,野外挂机不飘。
            // Unity 无 IsNoonPartyScene/IsFieldScene 场景分类基建(帮派宴会系统/场景类型表未移植),
            // 用 RoleModel.DunId>0 近似"在副本中"作为放行条件——TODO:补场景分类表后按老端条件精确对齐。
            if (RoleModel.Instance.DunId > 0)
            {
                TipsManager.Float(FormatExpPercent(exp, percent));
            }
        }

        private static string FormatExpPlain(long exp) => "经验 +" + exp;

        private static string FormatExpPercent(long exp, int percent)
        {
            int pct = percent - 100;
            return pct > 0 ? ("经验 +" + exp + " (+" + pct + "%)") : FormatExpPlain(exp);
        }

        /// <summary>请求转职冷却(GAME_START 裸发,转职成功后 <see cref="TransferJob.TransferJobController"/> 重拉)。</summary>
        public void RequestTransferCooldown() => SendFmt(Proto.TRANSFER_JOB_COOLDOWN);

        /// <summary>13046 转职冷却 "i"=change_career_time,**绝对服务器时间戳,不是剩余秒**
        /// (与 PeaceCdEndSec 的"剩余秒转绝对时间"存法相反)。消费方(道具tooltip冷却展示)未接,TODO;
        /// 老端亦无事件Fire,仅惰性读取,本端同样不 Emit。</summary>
        private void On13046(NetReader r)
        {
            RoleModel.Instance.ChangeCareerTime = r.ReadU32();
            GameLog.Info("Role", "13046 转职冷却截止时间戳={0}", RoleModel.Instance.ChangeCareerTime);
        }

        /// <summary>请求头像激活列表(GAME_START 裸发 + 改头像开窗时拉)。</summary>
        public void RequestHeadList() => SendFmt(Proto.ROLE_HEAD_LIST);

        /// <summary>13080 头像激活列表 "h"+i×len。</summary>
        private void On13080(NetReader r)
        {
            int len = r.ReadU16();
            var ids = new List<int>(len);
            for (int i = 0; i < len; i++) ids.Add((int)r.ReadU32());
            RoleModel.Instance.SetHeadIdList(ids);
            GameLog.Info("Role", "13080 头像激活列表 count={0}", len);
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_HEAD_LIST_UPDATE);
        }

        /// <summary>13081 激活头像推送(S2C)。⚠字段序按服务端权威改正,见 Proto.ROLE_HEAD_ACTIVATE_PUSH 注释:
        /// Res:32, Id:64。code==1 成功→并入 HeadIdList;2无此头像/3已激活/4物品不足/5性别不符(仅记日志)。</summary>
        private void On13081(NetReader r)
        {
            int code = (int)r.ReadU32();
            long headId = r.ReadU64();
            GameLog.Info("Role", "13081 头像激活推送 code={0} headId={1}", code, headId);
            if (code == 1)
            {
                RoleModel.Instance.AddActivatedHead((int)headId);
                EventDispatcher.Emit(GlobalEvent.EVT_ROLE_HEAD_LIST_UPDATE);
            }
            else
            {
                GameLog.Info("Role", "13081 非成功码={0}(2无此头像/3已激活/4物品不足/5性别不符)", code);
            }
        }

        /// <summary>设置玩家头像(对标老端 SettingChangeHeadView.ts:107 okBtn_fun)。</summary>
        public void SetHead(long headId) => SendFmt(Proto.ROLE_HEAD_SET, "l", headId);

        /// <summary>13083 设置头像回包:code:i, head_ver:i, head_id_str:s。1成功→改
        /// Figure.Raw["picture"](FigureProto 无强类型 picture 字段)+ Emit EVT_ROLE_HEAD_SET_SUCCESS;
        /// 2管理员禁止;4无该头像;else 显码降级。</summary>
        private void On13083(NetReader r)
        {
            int code = (int)r.ReadU32();
            int headVer = (int)r.ReadU32();
            string headIdStr = r.ReadString();
            GameLog.Info("Role", "13083 设置头像 code={0} ver={1} idStr={2}", code, headVer, headIdStr);
            if (code == 1)
            {
                if (RoleModel.Instance.Figure != null) RoleModel.Instance.Figure.Raw["picture"] = headIdStr;
                EventDispatcher.Emit(GlobalEvent.EVT_ROLE_HEAD_SET_SUCCESS);
                EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
            }
            else if (code == 2) TipsManager.Toast("管理员禁止修改头像");
            else if (code == 4) TipsManager.Toast("没有该头像");
            else TipsManager.Toast("设置头像失败(" + code + ")");
        }

        /// <summary>13086 查看玩家指定数据 "h"+{type:c,value:i}×len。双端语义标签不一致但字节序一致
        /// (见 Proto.ROLE_MISC_COUNTERS 注释),落 RoleModel 泛用字典;老端亦仅 console.warn 埋点、
        /// 无任何消费方,本端同样不 Emit 事件。</summary>
        private void On13086(NetReader r)
        {
            int len = r.ReadU16();
            for (int i = 0; i < len; i++)
            {
                int type = r.ReadU8();
                long value = r.ReadU32();
                RoleModel.Instance.SetMiscCounter(type, value);
            }
            GameLog.Info("Role", "13086 玩家指定数据 count={0}(老端仅埋点无消费方,不发事件)", len);
        }

        /// <summary>13089 角色终身次数+1 "hhhh"(ModuleId,SubModule,Type,Count)。与 13088 共用
        /// RoleModel 通用终身计数存储;无 UI 消费方(TODO)。</summary>
        private void On13089(NetReader r)
        {
            int moduleId = r.ReadU16();
            int subModuleId = r.ReadU16();
            int type = r.ReadU16();
            int count = r.ReadU16();
            RoleModel.Instance.SetLifelongCount(moduleId, subModuleId, type, count);
            GameLog.Info("Role", "13089 终身次数+1 module={0} sub={1} type={2} count={3}(无 UI 消费方 TODO)",
                moduleId, subModuleId, type, count);
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_LIFELONG_COUNT_UPDATE, moduleId, subModuleId);
        }

        // ===================== 改名(轮5;42601/42602/42604)=====================

        /// <summary>查询是否免费改名(改名入口按钮点击发,裸请求)。</summary>
        public void RequestRenameFreeCheck() => SendFmt(Proto.RENAME_FREE_CHECK);

        /// <summary>改名合法性预检(对标老端确认发送前的 42604)。type 见 RENAME_TYPE_*。</summary>
        public void CheckRename(string name, int type)
        {
            _pendingRenameType = type;
            SendFmt(Proto.RENAME_CHECK, "si", name, type);
            GameLog.Info("Role", "send 42604 改名校验 name={0} type={1}", name, type);
        }

        /// <summary>提交改名(42604 通过 + 二次确认后发)。</summary>
        public void SubmitRename(string name, int type)
        {
            SendFmt(Proto.RENAME_SUBMIT, "si", name, type);
            GameLog.Info("Role", "send 42601 改名提交 name={0} type={1}", name, type);
        }

        /// <summary>42602 是否免费改名 "i"。收到后打开改名窗(result 作为 is_free 参数透传),
        /// 对标老端 SettingModel.Fire(SETTING_OPEN_VIEW,"SettingChangeNameView",scmd.result)。</summary>
        private void On42602(NetReader r)
        {
            int result = (int)r.ReadU32();
            GameLog.Info("Role", "42602 是否免费改名 result={0}", result);
            SettingFlow.OpenSub("SettingChangeNameView", result);
        }

        /// <summary>42604 改名校验回包 "is"(result:i, name:s)。==1 → Emit
        /// EVT_ROLE_RENAME_CHECK_PASSED(name,type) 供二次确认;否则显码降级(FormatRenameMsg)。</summary>
        private void On42604(NetReader r)
        {
            int result = (int)r.ReadU32();
            string name = r.ReadString();
            GameLog.Info("Role", "42604 改名校验 result={0} name={1}", result, name);
            if (result == 1)
            {
                EventDispatcher.Emit(GlobalEvent.EVT_ROLE_RENAME_CHECK_PASSED, name, _pendingRenameType);
            }
            else
            {
                TipsManager.Toast(FormatRenameMsg(result));
            }
        }

        /// <summary>42601 改名提交回包 "is"(result:i, name:s)。==1 → toast「改名成功」+
        /// Emit EVT_ROLE_RENAME_SUCCESS(仅关窗);Figure.Name 的更新走既有 12086 广播路径
        /// (SceneController.On12086 自身分流),此处**勿双改**。</summary>
        private void On42601(NetReader r)
        {
            int result = (int)r.ReadU32();
            string name = r.ReadString();
            GameLog.Info("Role", "42601 改名提交结果 result={0} name={1}", result, name);
            if (result == 1)
            {
                TipsManager.Toast("改名成功");
                EventDispatcher.Emit(GlobalEvent.EVT_ROLE_RENAME_SUCCESS);
            }
            else
            {
                TipsManager.Toast(FormatRenameMsg(result));
            }
        }

        /// <summary>改名错误码文案。⚠取值以服务端为准:老端 TS On42601 硬编码假设 result 2/3/4/5/6
        /// 小整数枚举,但服务端 pt_426.erl + data_error_code 运行期表实际下发的是数值型错误码
        /// (1001/1008/1009/1010/1450002/4260001/4260002),两套编码冲突,本端按服务端实测为准
        /// (见 Proto.RENAME_SUBMIT 注释)。</summary>
        private static string FormatRenameMsg(int code)
        {
            switch (code)
            {
                case 1001: return "勾玉不足";
                case 1011: return "改名卡不足";
                case 1008: return "名字长度不合法(需 4-12 个字符)";
                case 1009: return "该名字已被使用";
                case 1010: return "包含非法字符";
                case 1450002: return "包含敏感词";
                case 4260001: return "今天已经改过名";
                case 4260002: return "改名系统升级中";
                default: return "改名失败(" + code + ")";
            }
        }
    }
}
