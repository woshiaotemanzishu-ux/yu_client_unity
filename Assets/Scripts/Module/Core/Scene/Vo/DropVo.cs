using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Scene.Vo
{
    /// <summary>
    /// 场景掉落物数据(对标老客户端 SceneController.On12018,12018 列表项)。
    /// 字段顺序严格对照 yu_client SceneController.ts:421-446。时间戳为毫秒(老客户端 /1000 转秒)。
    /// </summary>
    public sealed class DropVo
    {
        public long DropId;
        public int DropType;
        public int TypeId;
        public int DropNum;
        public long RoleId;
        public int ServerId;
        public long TeamId;
        public int Camp;
        public long GuildId;
        public int X;
        public int Y;
        public string DropEffect = "";
        public string PutIcon = "";
        public int PickUpTimeMs;     // i 拾取耗时(ms)
        public long PickUpRoleId;
        public long PickUpEndTimeMs; // l 拾取结束时间戳(ms)
        public int OverTime;
        public int ShowType;         // c 1直接拾取 2进背包 3模拟掉落
        public int MonId;
        public int MonPosX;
        public int MonPosY;
        public int AssignType;       // c 客户端只判断 ==7

        public void ReadFromProtocal(NetReader r)
        {
            DropId = r.ReadU64();
            DropType = r.ReadU8();
            TypeId = (int)r.ReadU32();
            DropNum = (int)r.ReadU32();
            RoleId = r.ReadU64();
            ServerId = r.ReadU16();
            TeamId = r.ReadU64();
            Camp = r.ReadU16();
            GuildId = r.ReadU64();
            X = r.ReadU16();
            Y = r.ReadU16();
            DropEffect = r.ReadString();
            PutIcon = r.ReadString();
            PickUpTimeMs = (int)r.ReadU32();
            PickUpRoleId = r.ReadU64();
            PickUpEndTimeMs = r.ReadU64();
            OverTime = (int)r.ReadU32();
            ShowType = r.ReadU8();
            MonId = (int)r.ReadU32();
            MonPosX = r.ReadU16();
            MonPosY = r.ReadU16();
            AssignType = r.ReadU8();
        }
    }
}
