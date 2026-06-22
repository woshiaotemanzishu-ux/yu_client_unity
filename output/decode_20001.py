import struct

hex_str = """
02 00 00 00 01 00 00 00 3B 00 00 00 00 00 00 02 E4 00 00 00 00 00 03 85 CB 61 00 01 14 DC 0A 40
14 FE 0A 70 00 00 00 01 00 C8 00 00 03 85 CB 61 01 00 00 00 00 00 00 00 00 00 00 00 01 9E EB E4
3E B8 00 00 00 01 01 00 00 00 00 00 00 04 69 00 00 00 00 00 00 00 8C 00 00 00 00 00 00 00 00 00
00 14 FE 0A 70 00 00 00 00 00 00 00
"""
data = bytes(int(b, 16) for b in hex_str.split())
print("total len =", len(data))

pos = 0
def rd(n):
    global pos
    v = data[pos:pos+n]; pos += n; return v
def u8():  return rd(1)[0]
def u16(): return struct.unpack(">H", rd(2))[0]
def u32(): return struct.unpack(">I", rd(4))[0]
def i32(): return struct.unpack(">i", rd(4))[0]
def u64():
    hi = u32(); lo = u32(); return (hi << 32) | lo

def read_buff():
    return dict(iconType=u16(), buff_effect_id=u16(), id=u32(), level=u8(),
                diejia=u8(), integer=i32(), decimals=i32(), period=u64())

atk = dict(attacker_type=u8(), role_id=u64(), hp=u64(), anger=u32(), move_anim=u8(),
           skill_id=u32(), skill_level=u16(), pos_x=u16(), pos_y=u16(),
           attack_pos_x=u16(), attack_pos_y=u16(), attack_angle=u16())
print("attacker header end offset =", pos)
for k, v in atk.items(): print(f"  attacker.{k} = {v}")

n_atk_buff = u16(); print("attack_buff_num =", n_atk_buff)
for _ in range(n_atk_buff): print("  attacker buff:", read_buff())

n_trig = u16(); print("attack_trigger_skill_num =", n_trig)
print("  trigger skills:", [u32() for _ in range(n_trig)])

n_def = u16(); print("defense_num =", n_def)
for di in range(n_def):
    d = dict(type_flag=u8(), role_id=u64(), hp=u64(), anger=u32(), damage=u32(),
             damage_flag=u8(), second_damage_flag=u8(), pos_x=u16(), pos_y=u16(),
             move_anim=u8(), breaked_skill_id=u32())
    print(f"  defender[{di}]:")
    for k, v in d.items(): print(f"    {k} = {v}")
    n_db = u16(); print("    defender_buff_num =", n_db)
    for _ in range(n_db): print("    defender buff:", read_buff())

print("FINAL pos =", pos, " remaining =", len(data) - pos)
