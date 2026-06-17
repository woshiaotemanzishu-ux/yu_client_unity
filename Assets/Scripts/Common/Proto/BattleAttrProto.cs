using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Common.Proto
{
    /// <summary>
    /// 战斗属性块,1:1 对标老客户端 BattleProtoVo + BaseAttrProtoVo(h5/src/common/),
    /// 也对应服务端 pt:write_battle_attr 的 #attr{} 记录分支(pt.erl write_attr_list/1):
    ///   Hp:64, HpLim:64, Speed:16, 然后是 51 个 int32 固定属性(无计数前缀、无属性 id)。
    ///
    /// 易错点:服务端 write_attr_list 还有"列表(count+id16+val32)"和"map(count+key8+val32)"两个分支,
    /// 但 13001 主角(及 13004 等)走的是 #attr{} 固定记录分支。早期版本误按"count+列表"解析,
    /// 会少读约 202 字节,导致同包后续 场景id/副本id/坐标 全部错位,12005 进场被服务端拒(errorCode 1200005)。
    /// 字段顺序与 BaseAttrProtoVo.pro_list 完全一致,改动必须与老客户端同步。
    /// </summary>
    public sealed class BattleAttrProto
    {
        /// <summary>BaseAttrProtoVo.pro_list 的 51 个属性名(顺序即字节顺序,全部 int32)。</summary>
        public static readonly string[] AttrNames =
        {
            "att", "wreck", "def", "hit", "dodge", "crit", "ten",
            "hurt_add_ratio", "hurt_del_ratio", "hit_ratio", "dodge_ratio", "crit_ratio", "uncrit_ratio",
            "abs_att", "abs_def", "hurt_float_ratio", "skill_hurt_add_ratio", "skill_hurt_del_ratio",
            "crit_hurt_add_ratio", "crit_hurt_del_ratio", "parry_ratio", "overload_ratio", "parry", "neglect",
            "HearHurtAddRatio", "HearHurtDelRatio", "DefHearHurtRatio",
            "att_add_ratio", "hp_add_ratio", "wreck_add_ratio", "def_add_ratio", "hit_add_ratio",
            "dodge_add_ratio", "crit_add_ratio", "ten_add_ratio",
            "draconic_sacred", "draconic_tspace", "draconic_rule",
            "ex_ratio", "unex_ratio", "ex_hurt_add_ratio", "unex_hurt_del_ratio",
            "armor", "undizzy_ratio", "unedef_ratio", "fixed_att", "fixed_def",
            "pvp_att_add", "pvp_att_reduece", "real_abs_att", "real_abs_def",
        };

        public long Hp;
        public long HpLim;
        public int Speed; // move_speed(攻击距离/格子数,int16)

        /// <summary>属性名 → 值(全部按 int32 读,百分比类约定 ×0.0001,展示时再换算)。</summary>
        public readonly Dictionary<string, long> Attrs = new Dictionary<string, long>(AttrNames.Length);

        public static BattleAttrProto Read(NetReader r)
        {
            var a = new BattleAttrProto
            {
                Hp = (long)r.ReadU64(),
                HpLim = (long)r.ReadU64(),
                Speed = r.ReadU16(),
            };
            foreach (string name in AttrNames)
            {
                a.Attrs[name] = r.ReadU32();
            }
            return a;
        }

        public long Get(string name) => Attrs.TryGetValue(name, out long v) ? v : 0L;
    }
}
