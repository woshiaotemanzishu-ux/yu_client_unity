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
        }

        public void MarkBaseInfoReady() => HasBaseInfo = true;
    }
}
