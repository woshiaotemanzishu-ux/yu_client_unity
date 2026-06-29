using Shenxiao.Common.Proto;

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
        }

        public void MarkBaseInfoReady() => HasBaseInfo = true;
    }
}
