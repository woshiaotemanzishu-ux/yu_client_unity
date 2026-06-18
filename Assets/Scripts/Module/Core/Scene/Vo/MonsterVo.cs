using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Scene.Vo
{
    /// <summary>
    /// 怪物/采集物的场景数据(对标老客户端 MonsterVo / 服务端 pt_120 binary_12007,12007 协议体)。
    /// 字段顺序严格对照 yu_server pt_120.erl:609-628,与老客户端 MonsterVo 格式串
    /// "hhiillhshisiicccccicccclllishi" 完全一致。采集物=服务端 Kind(本类 Type)字段为 1/2/8/9/11。
    /// </summary>
    public sealed class MonsterVo
    {
        public int X;
        public int Y;
        public int InstanceId;       // i 怪物唯一实例 id
        public int TypeId;           // i config_mon 配置 id(服务端 Mid)
        public long Hp;              // l
        public long HpLim;           // l
        public int Level;            // h
        public string Name = "";
        public int Speed;            // h
        public int MonsterRes;       // i 模型资源(服务端 Body)
        public string EffectRes = "";// s 特效(服务端 IconEffect)
        public int TextureRes;       // i
        public int WeaponId;         // i
        public int AttackMode;       // c 0近战 1远程
        public int Type;             // c 服务端 Kind:0怪物 1/2/8/9/11采集...
        public int Quality;          // c 服务端 Color 品级
        public int GuajiFlag;        // c 服务端 Out
        public int BossType;         // c 0普通..3世界boss..
        public int PickTime;         // i 采集所需时间(服务端 CollectTime)
        public int CanPick;          // c 服务端 IsBeClicked 可点击
        public int CanAttack;        // c 服务端 IsBeAtted 可攻击
        public int HideFlag;         // c
        public int GhostMode;        // c
        public long WarGroup;        // l 服务端 Group
        public long GuildId;         // l
        public long AttributiveId;   // l 所属玩家 id(服务端 BlRoleId)
        public int KuangbaoLeftTime; // i 服务端 FrenzyEnterTime
        public string GuildName = "";
        public int ServerNum;        // h
        public int NextCollectTime;  // i 采集冷却结束时间戳

        public void ReadFromProtocal(NetReader r)
        {
            X = r.ReadU16();
            Y = r.ReadU16();
            InstanceId = (int)r.ReadU32();
            TypeId = (int)r.ReadU32();
            Hp = r.ReadU64();
            HpLim = r.ReadU64();
            Level = r.ReadU16();
            Name = r.ReadString();
            Speed = r.ReadU16();
            MonsterRes = (int)r.ReadU32();
            EffectRes = r.ReadString();
            TextureRes = (int)r.ReadU32();
            WeaponId = (int)r.ReadU32();
            AttackMode = r.ReadU8();
            Type = r.ReadU8();
            Quality = r.ReadU8();
            GuajiFlag = r.ReadU8();
            BossType = r.ReadU8();
            PickTime = (int)r.ReadU32();
            CanPick = r.ReadU8();
            CanAttack = r.ReadU8();
            HideFlag = r.ReadU8();
            GhostMode = r.ReadU8();
            WarGroup = r.ReadU64();
            GuildId = r.ReadU64();
            AttributiveId = r.ReadU64();
            KuangbaoLeftTime = (int)r.ReadU32();
            GuildName = r.ReadString();
            ServerNum = r.ReadU16();
            NextCollectTime = (int)r.ReadU32();
        }

        /// <summary>采集物(对标老客户端:Type/Kind ∈ {1,2,8,9,11} 不显血条、显示采集冷却)。</summary>
        public bool IsCollect => Type == 1 || Type == 2 || Type == 8 || Type == 9 || Type == 11;

        /// <summary>Boss(对标老客户端 Monster.ts:601-603:type==0 且 boss_type>=3)。</summary>
        public bool IsBoss => Type == 0 && BossType >= 3;
    }
}
