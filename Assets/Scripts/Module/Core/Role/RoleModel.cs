using System.Collections.Generic;
using Shenxiao.Common.Proto;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 主角数据(进游戏后唯一真相源,对标老客户端 RoleManager.mainRoleInfo)。
    /// 由 RoleController 按 13001/13002/13003/13006 填充;UI 监听 EVT_ROLE_INFO_UPDATE 读这里。
    /// 字段命名对标服务端 pt_130 write 字段(铜币/元宝/绑元/帮贡)。
    /// </summary>
    public sealed class RoleModel
    {
        public static readonly RoleModel Instance = new RoleModel();
        private RoleModel() { }

        public bool HasBaseInfo { get; private set; } // 13001 是否已到

        public long RoleId;
        public int ServerId;
        public string ServerName = "";

        public long Exp;
        public long ExpLim;
        public int Level;

        public long Coin;    // 铜币(tong)
        public int Gold;     // 元宝(jin)
        public int BGold;    // 绑元(jinLock)
        public int GCoin;    // 帮贡(guild_coin)

        public long CombatPower;
        public int SceneId;
        public int X;
        public int Y;
        public int DunId;
        public long GuildId;
        public string GuildName = "";

        public FigureProto Figure;        // 外观块
        public BattleAttrProto BattleAttr; // 战斗属性块

        /// <summary>主角 PK(战斗)模式(PK_STATUS:0和平/1全体/2强制/3跨服/4结社/5阵营/6海域)。
        /// 对标老端 mainRoleVo.pk_status:进场自块(12002/12003 里 roleId==自己)同步、12074 主角广播、13012 切换成功更新。</summary>
        public int PkStatus;

        /// <summary>和平模式切换冷却截止(服务器秒;0=无冷却)。对标老端 mainRoleVo.peace_cd_time
        /// (13001 携带剩余秒 / 13012 回包 remain_time>0 时设置)。</summary>
        public long PeaceCdEndSec;

        /// <summary>是否处于 PK 切换冷却中(对标老端 peace_cd_is_playing)。</summary>
        public bool PeaceCdActive => PeaceCdEndSec > TimeUtil.NowSec();

        // ----- 角色成长补全(自动循环 轮5;13011/13017/13046/13080/13081/13086/13089) -----

        /// <summary>世界等级(13011 worldLv,16位无符号)。</summary>
        public int WorldLv;
        /// <summary>世界等级经验加成%(13011 worldLvExp,16位有符号)。</summary>
        public int WorldLvExp;
        /// <summary>是否处于托管(自动战斗)状态(13017)。对标老端 RoleManager.GetMainRoleDepositState,
        /// 战斗表现门控用(Scene/FightMovie 多处消费点未接,TODO)。</summary>
        public bool DepositState;
        /// <summary>转职冷却截止(13046,**绝对服务器时间戳,不是剩余秒**——与 PeaceCdEndSec 的
        /// "剩余秒转绝对时间"存法相反,勿复用同一 helper)。0=从未拉取过。</summary>
        public long ChangeCareerTime;
        /// <summary>已激活头像 id 列表(13080 全量 / 13081 推送增量)。</summary>
        public readonly List<int> HeadIdList = new List<int>();

        /// <summary>13086 查看玩家指定数据(Type→Value,老端亦仅埋点无消费方,见 Proto.ROLE_MISC_COUNTERS)。</summary>
        private readonly Dictionary<int, long> _miscCounters = new Dictionary<int, long>();
        /// <summary>13088(全量)/13089(增量)通用终身计数存储,key=(module,sub,type)。对标老端
        /// RoleManager.lifelong_counts_dic/SetLivelongCount/GetLivelongCount。</summary>
        private readonly Dictionary<(int module, int sub, int type), int> _lifelongCounts =
            new Dictionary<(int, int, int), int>();

        /// <summary>该头像是否已激活(对标老端 RoleManager.HaveActiviteThisHead:id 1/3 恒激活的硬编码照抄)。</summary>
        public bool IsHeadActivated(int headId) => headId == 1 || headId == 3 || HeadIdList.Contains(headId);

        /// <summary>13080 全量落地(整表覆盖)。</summary>
        public void SetHeadIdList(List<int> ids)
        {
            HeadIdList.Clear();
            if (ids != null) HeadIdList.AddRange(ids);
        }

        /// <summary>13081 推送单条激活(去重追加,对标老端 UpdateHeadImgList)。</summary>
        public void AddActivatedHead(int headId)
        {
            if (!HeadIdList.Contains(headId)) HeadIdList.Add(headId);
        }

        public void SetMiscCounter(int type, long value) => _miscCounters[type] = value;
        public long GetMiscCounter(int type) => _miscCounters.TryGetValue(type, out long v) ? v : 0L;

        public void SetLifelongCount(int module, int sub, int type, int count)
            => _lifelongCounts[(module, sub, type)] = count;
        public int GetLifelongCount(int module, int sub, int type)
            => _lifelongCounts.TryGetValue((module, sub, type), out int v) ? v : 0;

        /// <summary>按"剩余秒"记录冷却截止(remainSec<=0 清除)。</summary>
        public void SetPeaceCd(int remainSec)
        {
            PeaceCdEndSec = remainSec > 0 ? TimeUtil.NowSec() + remainSec : 0;
        }

        /// <summary>展示名:figure.name(13001 携带)。</summary>
        public string Name => Figure != null && !string.IsNullOrEmpty(Figure.name) ? Figure.name : "角色" + RoleId;
        public int Career => Figure?.career ?? 0;
        public int Sex => Figure?.sex ?? 0;

        public void Reset()
        {
            HasBaseInfo = false;
            Figure = null;
            BattleAttr = null;
            GuildName = "";
            ServerName = "";
            PkStatus = 0;
            PeaceCdEndSec = 0;
            WorldLv = 0;
            WorldLvExp = 0;
            DepositState = false;
            ChangeCareerTime = 0;
            HeadIdList.Clear();
            _miscCounters.Clear();
            _lifelongCounts.Clear();
        }

        public void MarkBaseInfoReady() => HasBaseInfo = true;
    }
}
