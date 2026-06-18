using Shenxiao.Common.Proto;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Scene.Vo
{
    /// <summary>
    /// 其他玩家的场景数据(对标老客户端 RoleVo / 服务端 pt_120 binary_to_12003,12003 协议体)。
    /// 字段顺序严格对照 yu_server pt_120.erl:632-664。figure 块复用 <see cref="FigureProto"/>(13001 已在用)。
    /// 只是数据;渲染(建模/名牌/移动插值)由 RoleSpawner+Role 负责。
    /// </summary>
    public sealed class RoleVo
    {
        public long RoleId;
        public string Platform = "";
        public int ServerId;
        public int ServerNum;
        public string ServerName = "";
        public FigureProto Figure;
        public int X;
        public int Y;
        public long Hp;
        public long HpLim;
        public int Speed;
        public int Hide;
        public int Ghost;
        public long Group;
        public int BossOwner;     // bl_who:boss 归属标志
        public long TeamId;
        public int TeamPos;
        public int PkStatus;
        public long CombatPower;
        public int PkValue;
        public int PkProtectTime;
        public long MateRoleId;
        public int ShipModelId;
        public int ProtectTime;
        public int CampId;

        /// <summary>最近一次移动广播(12001)的移动类型,渲染层据此选普通/轻功/瞬移表现。</summary>
        public int MoveFlag;

        /// <summary>展示状态(12075 广播设置,非 12003 字段,默认 0)。</summary>
        public int Show;

        public void ReadFromProtocal(NetReader r)
        {
            RoleId = r.ReadU64();
            Platform = r.ReadString();
            ServerId = r.ReadU16();
            ServerNum = r.ReadU16();
            ServerName = r.ReadString();
            Figure = FigureProto.Read(r);
            X = r.ReadU16();
            Y = r.ReadU16();
            Hp = r.ReadU64();
            HpLim = r.ReadU64();
            Speed = r.ReadU16();
            Hide = r.ReadU8();
            Ghost = r.ReadU8();
            Group = r.ReadU64();
            BossOwner = r.ReadU8();
            TeamId = r.ReadU64();
            TeamPos = r.ReadU8();
            PkStatus = r.ReadU8();
            CombatPower = r.ReadU64();
            PkValue = r.ReadU16();
            PkProtectTime = (int)r.ReadU32();
            MateRoleId = r.ReadU64();
            ShipModelId = (int)r.ReadU32();
            ProtectTime = (int)r.ReadU32();
            CampId = r.ReadU16();
        }

        /// <summary>展示名:跨服(server_num 与本服不同)时老客户端前缀 "S{服}." 由渲染层决定,这里只给原名。</summary>
        public string Name => Figure != null ? Figure.name : "玩家" + RoleId;
    }
}
