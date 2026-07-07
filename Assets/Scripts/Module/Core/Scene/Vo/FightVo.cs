using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Scene.Vo
{
    /// <summary>
    /// 20001/20003 攻击结果广播体(S2C),逐字段、逐字节对标老端 <c>scene/fight/FightVo.ts</c>
    /// <c>FightVo.ReadFromProtocal</c>(yu_client),BIG_ENDIAN(与 <see cref="NetReader"/> 同字符)。
    ///
    /// 老端读序(<c>u_mgr.ReadFmt</c>):
    ///   攻击者头  <c>cllicihhhhhh</c> = attacker_type(c) role_id(l) hp(l) anger(i) move_anim(c)
    ///             skill_id(i) skill_level(h) pos_x(h) pos_y(h) attack_pos_x(h) attack_pos_y(h) attack_angle(h)
    ///   攻击方 buff: h(数量) + 每个 <c>hhiccIIl</c>
    ///   触发技能:   h(数量) + 每个 <c>i</c>
    ///   防御者列表: h(数量) + 每个 <c>clliicchhci</c> = type_flag(c) role_id(l) hp(l) anger(i) damage(i)
    ///             damage_flag(c) second_damage_flag(c) pos_x(h) pos_y(h) move_anim(c) breaked_skill_id(i)
    ///             —— 每个防御者后再跟 h(buff 数量) + <c>hhiccIIl</c>×N。
    ///
    /// 字段语义(对标老端 FightController.ts <c>RefreshObjVo</c>:1527 + <c>ClientFightServer</c>:1000-1002):
    ///   · defender.hp = 服务端给出的**新绝对 hp**(非增量);<c>hp==0 → 死亡</c>(老端 ForceDoDead,本端移除可见对象);
    ///   · damage / damage_flag = 仅飘字用,**不参与 hp 计算**。flag 全表(老端 FightDamageFlag,
    ///     FightDamageManager.ts:13):0正常 1闪避 2暴击 3免疫 4会心 5护盾免伤 6格挡 7无伤害(实测 engage 帧后
    ///     怪反击常带 7) 8卓越 9暴击会心 10卓越会心;客户端另有 1001吸血/1002反弹/1003流血(非协议值);
    ///   · type_flag = 老端 <c>SceneBaseType</c>(SceneConfig.ts:31:Monster=1 Role=2 Fake_Role=5 ...)。
    ///
    /// 本类只做解析 + 留数据;buff 字段读出仅为保持多防御者字节对齐(buff 表现/飘字属后续,本轮不消费)。
    /// 第9轮真连 108B 样本逐字节解析:见 Docs/RuntimeCompare/MainQuest-FightVoDamage-第10轮.md。
    /// </summary>
    public sealed class FightVo
    {
        /// <summary>buff:<c>hhiccIIl</c>(到期 unix 时间戳 period,本轮仅解析不展示)。</summary>
        public struct BuffInfo
        {
            public int IconType;        // h
            public int BuffEffectId;    // h
            public int Id;              // i
            public int Level;           // c
            public int Diejia;          // c
            public int Integer;         // I(有符号)
            public int Decimals;        // I(有符号;老端再 UnsignToSigned,C# ReadI32 已是有符号)
            public long Period;         // l
        }

        public sealed class AttackInfo
        {
            public int AttackerType;    // c  老端 SceneBaseType(2=人/玩家)
            public long RoleId;         // l  攻击者 id(玩家=roleId)
            public long Hp;             // l  攻击者当前 hp
            public long Anger;          // i
            public int MoveAnim;        // c
            public int SkillId;         // i
            public int SkillLevel;      // h
            public int PosX;            // h  攻击者坐标
            public int PosY;            // h
            public int AttackPosX;      // h  攻击点(center_pos)
            public int AttackPosY;      // h
            public int AttackAngle;     // h
            public readonly List<BuffInfo> Buffs = new List<BuffInfo>();
        }

        public sealed class DefenseInfo
        {
            public int TypeFlag;        // c  老端 SceneBaseType(1怪 2人 5假人)
            public long RoleId;         // l  怪=实例id;人=roleId
            public long Hp;             // l  服务端新绝对 hp(0=死亡)
            public long Anger;          // i
            public long Damage;         // i  本次伤害(飘字用)
            public int DamageFlag;      // c  0正常 1闪避 2暴击 3免疫 4会心 5护盾免伤 6格挡 7无伤害 8卓越 9暴击会心 10卓越会心
            public int SecondDamageFlag;// c
            public int PosX;            // h  防御者坐标(随击退更新)
            public int PosY;            // h
            public int MoveAnim;        // c
            public int BreakedSkillId;  // i
            public readonly List<BuffInfo> Buffs = new List<BuffInfo>();
        }

        public readonly AttackInfo Attack = new AttackInfo();
        public readonly List<int> AttackTriggerSkills = new List<int>();
        public readonly List<DefenseInfo> DefenseList = new List<DefenseInfo>();

        /// <summary>原始 payload 字节长度(取证用;格式正确时应被完全消费,解析后 reader.Remaining==0)。</summary>
        public int PayloadLen { get; private set; }

        public void ReadFromProtocal(NetReader r)
        {
            PayloadLen = r.Remaining;

            // —— 攻击者头 cllicihhhhhh ——(C# 对象/语句按源码顺序求值,逐字节对齐老端 ReadFmt)
            Attack.AttackerType = r.ReadU8();
            Attack.RoleId = r.ReadU64();
            Attack.Hp = r.ReadU64();
            Attack.Anger = r.ReadU32();
            Attack.MoveAnim = r.ReadU8();
            Attack.SkillId = (int)r.ReadU32();
            Attack.SkillLevel = r.ReadU16();
            Attack.PosX = r.ReadU16();
            Attack.PosY = r.ReadU16();
            Attack.AttackPosX = r.ReadU16();
            Attack.AttackPosY = r.ReadU16();
            Attack.AttackAngle = r.ReadU16();

            // —— 攻击方 buff 列表 h + hhiccIIl×N ——
            int attackBuffNum = r.ReadU16();
            for (int i = 0; i < attackBuffNum; i++) Attack.Buffs.Add(ReadBuff(r));

            // —— 攻击触发技能列表 h + i×N ——
            int triggerNum = r.ReadU16();
            for (int i = 0; i < triggerNum; i++) AttackTriggerSkills.Add((int)r.ReadU32());

            // —— 防御者列表 h + clliicchhci×N(每个防御者后跟自身 buff 列表) ——
            int defenseNum = r.ReadU16();
            for (int i = 0; i < defenseNum; i++)
            {
                var d = new DefenseInfo();
                d.TypeFlag = r.ReadU8();
                d.RoleId = r.ReadU64();
                d.Hp = r.ReadU64();
                d.Anger = r.ReadU32();
                d.Damage = r.ReadU32();
                d.DamageFlag = r.ReadU8();
                d.SecondDamageFlag = r.ReadU8();
                d.PosX = r.ReadU16();
                d.PosY = r.ReadU16();
                d.MoveAnim = r.ReadU8();
                d.BreakedSkillId = (int)r.ReadU32();
                DefenseList.Add(d);

                int buffNum = r.ReadU16();
                for (int b = 0; b < buffNum; b++) d.Buffs.Add(ReadBuff(r));
            }
        }

        // buff:hhiccIIl(对标老端 ReadFmt("hhiccIIl"))
        private static BuffInfo ReadBuff(NetReader r)
        {
            BuffInfo buff;
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
