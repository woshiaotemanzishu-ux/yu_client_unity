using System.Collections.Generic;
using Shenxiao.Module.Core.Scene.Vo;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 自动循环 轮18 PK5:场景散件(120xx 补全,pt_120.erl)数据层补丁。
    /// 承载既有 <see cref="SceneManager"/>/<see cref="RoleVo"/>/<see cref="MonsterVo"/>/<see cref="DropVo"/>
    /// 容器装不下的广播状态——Boss 伤害榜(12025-28)、玩家求助列表(12043-45)、动态区域标记(12030)、
    /// HP 变化明细(12036 的表现专属字段)、复活明细(12083)、安全区状态(12085)、场景人数(12087)、
    /// 简单用户列表(12088)、公会id变更(12090 的 Role 分支)、怪物 Buff 批量(12092)。
    ///
    /// 边界:凡是能落到既有容器/字段的一律复用(见 SceneController.On12xxx 注释逐条说明:
    /// 12015→SceneManager.AddRole、12017→SceneManager.AddDrop、12022→RoleVo.BossOwner、
    /// 12078→RoleVo.Figure/RoleModel.Figure、12080→MonsterVo.CanAttack、12090 Monster 分支→
    /// MonsterVo.GuildId),本类只收既有 Vo/SceneManager 完全没地方放的那部分——本轮"只许写
    /// SceneController.cs + Scene\ 下新增文件"的边界不允许改动 SceneManager.cs/RoleVo.cs/
    /// MonsterVo.cs/DropVo.cs,故这些字段暂缺的部分(如 RoleVo 没有 SafeAreaState/GuildId、
    /// DropVo 没有 ExpireTime/DropWay/Alloc)先落这里占位,留后续评估是否升级进对应 Vo。
    ///
    /// 协议层解析完统一发 EVT_SCENE_MISC_UPDATE(参数 protoId),消费方按 protoId 读这里对应字段。
    /// </summary>
    public sealed class SceneMiscModel
    {
        public static readonly SceneMiscModel Instance = new SceneMiscModel();
        private SceneMiscModel() { }

        // ===== 12023 怪物喊话气泡 =====
        public struct MonsterTalkInfo
        {
            public int AutoId;
            public string Msg;
        }
        public MonsterTalkInfo LastMonsterTalk;

        // ===== 12025/12026/12027/12028 Boss 伤害榜(AutoId/ConfigId 为场次标识,查询回执与增量推送共用) =====
        public sealed class BossHurtEntry
        {
            public long RoleId;
            public string Name = "";
            public int ServerId;
            public int ServerNum;
            public string ServerName = "";
            public long TeamId;
            public int TeamPos;
            public long Hurt;
            public long AssistId;
        }
        public int BossHurtAutoId;
        public int BossHurtConfigId;
        public readonly List<BossHurtEntry> BossHurtList = new List<BossHurtEntry>();

        // ===== 12030 动态区域标记(独立推送;与 12002 快照内嵌的同结构尾块[SceneController.SkipAreaMark]
        //        是两处不同的调用点,但复用同一 pack_area_mark 编码,字段一致) =====
        public struct AreaMarkEntry
        {
            public int AreaId;
            public int ClientType;
        }
        public readonly List<AreaMarkEntry> AreaMarks = new List<AreaMarkEntry>();

        // ===== 12036 HP 变化广播(核心战斗表现)。Hp/HpLim 已经由 SceneController.On12036 复用既有
        //        SceneManager.ApplyHp 落到对应 RoleVo/MonsterVo(与 12009 同款路径);这里只留
        //        Change/BuffId/SourceSign/SourceId 等"表现"专属字段,供后续飘字/吸血反弹流血
        //        特效层消费(pt_120.erl:288-291;老端 SceneController.ts:553-616 On12036)。 =====
        public struct HpChangeInfo
        {
            public int Sign;        // SceneBaseType:1怪 2人 5假人
            public long Id;
            public long Hp;
            public long HpLim;
            public int IsMinus;     // 0加血 1扣血
            public long Change;
            public int BuffId;
            public int SourceSign;
            public long SourceId;
        }
        public HpChangeInfo LastHpChange;

        // ===== 12043/12044/12045 玩家求助(SOS)列表 =====
        public sealed class AssistEntry
        {
            public long AssistId;
            public long RoleId;
            public string Name = "";
            public int ServerId;
            public int ServerNum;
            public string ServerName = "";
        }
        public int AssistAutoId;
        public int AssistConfigId;
        public readonly List<AssistEntry> AssistList = new List<AssistEntry>();

        // ===== 12083 复活完成(与 Relive 模块[20009/20017 家族]联动 TODO,
        //        见 SceneController.On12083 注释;本轮只落数据不联动) =====
        public struct ReviveInfo
        {
            public int ReviveType;   // 1原地复活 2换场景复活(pt_120.erl:383-385)
            public int SceneId;
            public int X;
            public int Y;
            public string SceneName;
            public long Hp;
            public int Gold;
            public int BGold;
            public int AttProtectedTime;
        }
        public ReviveInfo LastRevive;

        // ===== 12085 安全区状态(九宫格区域广播,PlayerId 不一定是自己) =====
        public readonly Dictionary<long, int> SafeAreaStateByPlayer = new Dictionary<long, int>();
        /// <summary>PlayerId==自己时的便捷镜像,-1=尚未收到。</summary>
        public int MainRoleSafeAreaState = -1;

        // ===== 12087 场景玩家计数(老端消费方=BossModel.UPDATE_PLAYER_NUM,该模块本轮不在
        //        Unity 范围内,留 TODO 待接线) =====
        public int PlayerCountSceneId;
        public int PlayerCountNum;

        // ===== 12088 场景内简单用户列表 =====
        public sealed class SimpleUserEntry
        {
            public string Platform = "";
            public int ServerNum;
            public long Id;
            public int Sex;
            public int Realm;
            public int Career;
            public int Lv;
            public string Name = "";
            public string Picture = "";
            public int PictureVer;
        }
        public readonly List<SimpleUserEntry> SimpleUsers = new List<SimpleUserEntry>();

        // ===== 12090 公会id变更(仅 Role/Fake_Role 分支落这里;Monster 分支复用既有
        //        MonsterVo.GuildId,见 SceneController.On12090/SetMonsterField) =====
        public readonly Dictionary<long, long> GuildIdByRole = new Dictionary<long, long>();

        // ===== 12092 怪物 Buff 批量(Id→BuffList,元素结构 hhiccIIl 与既有 FightVo.BuffInfo 完全
        //        一致,pt_120.erl:442-446 + lib_skill_buff.erl:22-35 pack_buff 证实自带 16位
        //        数量前缀,直接复用 BuffInfo 不新增重复结构体) =====
        public readonly Dictionary<long, List<FightVo.BuffInfo>> MonsterBuffs = new Dictionary<long, List<FightVo.BuffInfo>>();

        /// <summary>切场景/登出清空(对标 SceneManager.Clear,由 SceneController.Dispose/On12005 同步调用)。</summary>
        public void Clear()
        {
            LastMonsterTalk = default;
            BossHurtAutoId = 0;
            BossHurtConfigId = 0;
            BossHurtList.Clear();
            AreaMarks.Clear();
            LastHpChange = default;
            AssistAutoId = 0;
            AssistConfigId = 0;
            AssistList.Clear();
            LastRevive = default;
            SafeAreaStateByPlayer.Clear();
            MainRoleSafeAreaState = -1;
            PlayerCountSceneId = 0;
            PlayerCountNum = 0;
            SimpleUsers.Clear();
            GuildIdByRole.Clear();
            MonsterBuffs.Clear();
        }
    }
}
