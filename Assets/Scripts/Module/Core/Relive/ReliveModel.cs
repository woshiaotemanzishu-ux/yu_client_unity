namespace Shenxiao.Module.Core.Relive
{
    /// <summary>
    /// 复活状态(对标老端 commonModel/ReliveModel.ts + SceneManager.killerType/killerName/killerId 散装字段)。
    /// 单例,持有:死亡态(IsDead)+击杀者信息(20013/20022 落地)+复活时间戳(20009)+回城复活疲劳(20017)。
    /// 不含表现/流程逻辑(那些在 <see cref="ReliveController"/>),纯数据。
    /// </summary>
    public sealed class ReliveModel
    {
        public static readonly ReliveModel Instance = new ReliveModel();
        private ReliveModel() { }

        // 老端 SceneBaseType 枚举(与 Scene.FightController.OBJ_MONSTER/OBJ_ROLE 同一取值,20013/20022 killerType 复用):
        // 1=被怪杀死 2=被玩家杀死。
        public const int KILLER_TYPE_MONSTER = 1;
        public const int KILLER_TYPE_ROLE = 2;

        /// <summary>对标老端 MainUIReliveView.DEFALUT_RELIVE_TYPE=22(倒计时到点默认复活方式,非服控场景用)。</summary>
        public const int DEFAULT_RELIVE_TYPE = 22;

        /// <summary>当前是否处于死亡态(20013/20022 主角死亡分支置真;20004 复活成功清位)。</summary>
        public bool IsDead { get; private set; }

        /// <summary>击杀者类型(KILLER_TYPE_MONSTER/KILLER_TYPE_ROLE),0=未设置。</summary>
        public int KillerType { get; private set; }

        /// <summary>击杀者 id(怪为 instance_id,玩家为 role_id)。</summary>
        public long KillerId { get; private set; }

        /// <summary>击杀者展示名(怪物已按 config_mon 3 级 fallback 覆盖,见 FightController.On20013)。</summary>
        public string KillerName { get; private set; } = "";

        /// <summary>20009 是否已经收到过回包(区分"服务端尚未回过"与"服务端回了但 can_relive=false")。</summary>
        public bool HasReviveInfo { get; private set; }

        /// <summary>对标老端 scene_mgr.can_relive:是否可复活(供服务端强控副本的复活面板用)。</summary>
        public bool CanRelive { get; private set; }

        /// <summary>对标老端 scene_mgr.next_relive_time:下次可复活的服务器时间戳(秒)。</summary>
        public long NextReviveTime { get; private set; }

        /// <summary>5分钟回城复活疲劳次数(20017 ReviveNum)。</summary>
        public int TiredCount { get; private set; }

        /// <summary>疲劳计数窗口结束时间(20017 EndTime,服务器时间戳)。</summary>
        public long TiredEndTime { get; private set; }

        /// <summary>记录击杀者信息并置死亡态(对标老端 20013/20022 handler 写 scene_mgr.killerType/killerName/killerId)。</summary>
        public void SetKiller(int killerType, long killerId, string killerName)
        {
            KillerType = killerType;
            KillerId = killerId;
            KillerName = killerName ?? "";
            IsDead = true;
        }

        /// <summary>复活成功清死亡态(对标老端 flag==1/12 分支)。</summary>
        public void ClearDead() => IsDead = false;

        /// <summary>20009 回包落地。</summary>
        public void SetReviveInfo(bool canRelive, long nextReviveTime)
        {
            HasReviveInfo = true;
            CanRelive = canRelive;
            NextReviveTime = nextReviveTime;
        }

        /// <summary>20017 回包/主动推送落地。</summary>
        public void SetTired(int reviveNum, long endTime)
        {
            TiredCount = reviveNum;
            TiredEndTime = endTime;
        }

        /// <summary>新野外Boss场景死亡次数(自动循环 轮15a,46034 转发落地;对标老端
        /// ReliveModel.SetReliveTimeData(...,BossSpecialReliveType.WorldBoss))。复活窗精确路由
        /// (BossFieldReliveView 等)留 TODO——本条只负责数据转发落地,不驱动 UI。</summary>
        public int BossDieTimes { get; private set; }

        /// <summary>下次可进场景时间戳(秒)。</summary>
        public long BossNextEnterTime { get; private set; }

        /// <summary>死亡debuff结束时间戳(秒)。</summary>
        public long BossDebuffEndTime { get; private set; }

        /// <summary>安全时间结束时间戳(秒,0=无安全时间)。</summary>
        public long BossSafeEndTime { get; private set; }

        public void SetBossDieInfo(int dieTimes, long nextEnterTime, long debuffEndTime, long safeEndTime)
        {
            BossDieTimes = dieTimes;
            BossNextEnterTime = nextEnterTime;
            BossDebuffEndTime = debuffEndTime;
            BossSafeEndTime = safeEndTime;
        }

        /// <summary>千幻蜃楼/圣兽岭场景死亡次数(自动循环 轮15b,pt_470:47034 转发落地;对标老端
        /// ReliveModel.SetReliveTimeData(...,BossSpecialReliveType.HolyBoss),与 <see cref="BossDieTimes"/>
        /// 系(46034→WorldBoss)并列的另一槽位。复活窗精确路由同样留 TODO,本条只负责数据转发。</summary>
        public int HolyBossDieTimes { get; private set; }

        /// <summary>下次可进场景时间戳(秒)。</summary>
        public long HolyBossNextEnterTime { get; private set; }

        /// <summary>死亡debuff结束时间戳(秒)。</summary>
        public long HolyBossDebuffEndTime { get; private set; }

        /// <summary>安全时间结束时间戳(秒,0=无安全时间)。</summary>
        public long HolyBossSafeEndTime { get; private set; }

        public void SetHolyBossDieInfo(int dieTimes, long nextEnterTime, long debuffEndTime, long safeEndTime)
        {
            HolyBossDieTimes = dieTimes;
            HolyBossNextEnterTime = nextEnterTime;
            HolyBossDebuffEndTime = debuffEndTime;
            HolyBossSafeEndTime = safeEndTime;
        }

        /// <summary>断线/登出重置(对标各 Model 既有 Clear 惯例)。</summary>
        public void Clear()
        {
            IsDead = false;
            KillerType = 0;
            KillerId = 0;
            KillerName = "";
            HasReviveInfo = false;
            CanRelive = false;
            NextReviveTime = 0;
            TiredCount = 0;
            TiredEndTime = 0;
            BossDieTimes = 0;
            BossNextEnterTime = 0;
            BossDebuffEndTime = 0;
            BossSafeEndTime = 0;
            HolyBossDieTimes = 0;
            HolyBossNextEnterTime = 0;
            HolyBossDebuffEndTime = 0;
            HolyBossSafeEndTime = 0;
        }
    }
}
