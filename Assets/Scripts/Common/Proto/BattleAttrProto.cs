using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Common.Proto
{
    /// <summary>
    /// 战斗属性块,1:1 对标服务端 pt:write_battle_attr:
    ///   Hp:64, HpLim:64, Speed:16, attr_list[u16 count × (AttrId:16, Value:32)]
    /// 出现在 13001(主角)、13004(他人在线)等回包里。
    /// </summary>
    public sealed class BattleAttrProto
    {
        public long Hp;
        public long HpLim;
        public int Speed;
        /// <summary>属性 id → 值(攻击/防御/暴击等,id 含义见服务端属性表)。</summary>
        public readonly Dictionary<int, int> Attrs = new Dictionary<int, int>();

        public static BattleAttrProto Read(NetReader r)
        {
            var a = new BattleAttrProto
            {
                Hp = r.ReadU64(),
                HpLim = r.ReadU64(),
                Speed = r.ReadU16(),
            };
            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                int id = r.ReadU16();
                int val = r.ReadI32();
                a.Attrs[id] = val;
            }
            return a;
        }
    }
}
