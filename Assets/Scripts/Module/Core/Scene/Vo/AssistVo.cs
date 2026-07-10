using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Scene.Vo
{
    /// <summary>
    /// 20006 辅助技能广播体(S2C),逐字段对标老端 <c>scene/fight/AssistVo.ts</c> <c>AssistVo.ReadFromProtocal</c>
    /// (yu_client),字节序=服务端 <c>pt_200.erl:113-117 write(20006,...)</c> + <c>226-233 assist_list/1</c>
    /// (两端一致,无冲突)。**比 <see cref="FightVo"/> 精简得多**:无 anger/damage/pos/move_anim/触发技能列表/
    /// 攻击方 buff,不要混用 FightVo 的字段表去读它。
    ///
    /// 老端读序(<c>u_mgr.ReadFmt</c>):
    ///   攻击者头  <c>lcic</c> = role_id(l) attacker_type(c) skill_id(i) skill_level(c)
    ///   防御者列表: h(数量) + 每个 <c>cll</c> = type_flag(c) role_id(l) hp(l)
    ///             —— 每个防御者后再跟 h(buff 数量) + <c>hhiccIIl</c>×N(buff 结构同 FightVo.BuffInfo)。
    /// </summary>
    public sealed class AssistVo
    {
        /// <summary>攻击者角色 id(role_id,发起辅助技能的角色/伙伴)。</summary>
        public long RoleId;          // l

        /// <summary>攻击者类型(老端 SceneBaseType:1怪 2人 5假人 等,与 FightVo.AttackInfo.AttackerType 同枚举)。</summary>
        public int AttackerType;     // c

        public int SkillId;          // i
        public int SkillLevel;       // c

        public sealed class DefenseInfo
        {
            public int TypeFlag;     // c  1怪 2人 5假人
            public long RoleId;      // l  怪=实例id;人=roleId
            public long Hp;          // l  服务端新绝对 hp(0=死亡)
            public readonly List<FightVo.BuffInfo> Buffs = new List<FightVo.BuffInfo>();
        }

        public readonly List<DefenseInfo> DefenseList = new List<DefenseInfo>();

        /// <summary>原始 payload 字节长度(取证用)。</summary>
        public int PayloadLen { get; private set; }

        public void ReadFromProtocal(NetReader r)
        {
            PayloadLen = r.Remaining;

            // —— 攻击者头 lcic ——
            RoleId = r.ReadU64();
            AttackerType = r.ReadU8();
            SkillId = (int)r.ReadU32();
            SkillLevel = r.ReadU8();

            // —— 防御者列表 h + cll×N(每个防御者后跟自身 buff 列表) ——
            int defenseNum = r.ReadU16();
            for (int i = 0; i < defenseNum; i++)
            {
                var d = new DefenseInfo
                {
                    TypeFlag = r.ReadU8(),
                    RoleId = r.ReadU64(),
                    Hp = r.ReadU64(),
                };
                DefenseList.Add(d);

                int buffNum = r.ReadU16();
                for (int b = 0; b < buffNum; b++) d.Buffs.Add(ReadBuff(r));
            }
        }

        // buff:hhiccIIl(与 FightVo.ReadBuff 同结构;此处独立复制一份避免跨类依赖私有方法)。
        private static FightVo.BuffInfo ReadBuff(NetReader r)
        {
            FightVo.BuffInfo buff;
            buff.IconType = r.ReadU16();
            buff.BuffEffectId = r.ReadU16();
            buff.Id = (int)r.ReadU32();
            buff.Level = r.ReadU8();
            buff.Diejia = r.ReadU8();
            buff.Integer = r.ReadI32();
            buff.Decimals = r.ReadI32();
            buff.Period = r.ReadU64();
            return buff;
        }
    }
}
