using System;
using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Friend
{
    public sealed partial class FriendController
    {
        private readonly Dictionary<int, LookOverRequestContext> _lookOverRequestByModule
            = new Dictionary<int, LookOverRequestContext>();

        private sealed class LookOverRequestContext
        {
            public long RoleId;
            public int ServerId;
        }

#if UNITY_EDITOR
        // Case hook: return true to consume the exact encoded outbound frame without touching the socket.
        private static Func<byte[], bool> s_lookOverOutboundIntercept;
#endif

        private void RegisterLookOverProtocols()
        {
            RegisterProtocal(Proto.LOOKOVER_DRAGONBALL, On19503);
            RegisterProtocal(Proto.LOOKOVER_SEAL_OR_DRACONIC, On19504);
            RegisterProtocal(Proto.LOOKOVER_REVELATION, On19505);
            RegisterProtocal(Proto.LOOKOVER_ILLUSION, On19506);
            RegisterProtocal(Proto.LOOKOVER_GODBEFALL, On19507);
            RegisterProtocal(Proto.LOOKOVER_UNREAL, On19508);
            RegisterProtocal(Proto.LOOKOVER_LUNG, On19509);
            RegisterProtocal(Proto.LOOKOVER_GODBEAST, On19510);
            RegisterProtocal(Proto.LOOKOVER_PET, On19511);
            RegisterProtocal(Proto.LOOKOVER_RUNE, On19512);
        }

        private void RememberLookOverRequest(long roleId, int moduleId, int serverId)
        {
            if (!IsValidLookOverRequest(roleId, moduleId)) return;
            _lookOverRequestByModule[moduleId] = new LookOverRequestContext { RoleId = roleId, ServerId = serverId };
        }

        private static bool IsValidLookOverRequest(long roleId, int moduleId) => roleId > 0 && moduleId >= 1 && moduleId <= 12;

        private void ClearLookOverRequests() => _lookOverRequestByModule.Clear();

        private void SendLookOverRequest(int serverId, long roleId, int moduleId)
        {
            if (!IsValidLookOverRequest(roleId, moduleId)) return;
#if UNITY_EDITOR
            byte[] frame = UserMsgAdapter.Encode(Proto.LOOKOVER_REQUEST, "hlh", serverId, roleId, moduleId);
            if (s_lookOverOutboundIntercept != null && s_lookOverOutboundIntercept(frame)) return;
#endif
            NetManager.SendFmt(Proto.LOOKOVER_REQUEST, "hlh", serverId, roleId, moduleId);
        }

        private T Stamp<T>(T snapshot, int moduleId) where T : LookOverModuleSnapshot
        {
            snapshot.ModuleId = moduleId;
            // 19503-19512 carry no role/request id. The latest request per module is the only correlation available.
            // Two consecutive requests for the same module may race: the server protocol has no request-id to disambiguate replies.
            if (_lookOverRequestByModule.TryGetValue(moduleId, out LookOverRequestContext context))
            {
                snapshot.RoleId = context.RoleId;
                snapshot.ServerId = context.ServerId;
            }
            return snapshot;
        }

        private void On19503(NetReader r)
        {
            var s = Stamp(new LookOverDragonBallSnapshot
            {
                Title = "龙珠",
                PrimaryPower = r.ReadU64(),
                IsActive = r.ReadU8()
            }, 2);
            ReadCount(r, () => s.BallList.Add(new LookOverDragonBallSnapshot.BallEntry
            {
                DragonBallId = r.ReadU32(), Level = r.ReadU16()
            }));
            ReadCount(r, () => s.FigureList.Add(new LookOverDragonBallSnapshot.FigureEntry
            {
                Type = r.ReadU8(), Lv = r.ReadU8()
            }));
            FriendModel.Instance.SetLookOverModule(s);
        }

        private void On19504(NetReader r)
        {
            int sysType = r.ReadU8();
            int moduleId = sysType == 3 ? 3 : sysType == 4 ? 4 : sysType;
            var s = Stamp(new LookOverSealSnapshot
            {
                Title = sysType == 3 ? "影装" : sysType == 4 ? "神祇" : "影装/神祇",
                SysType = sysType,
                Rating = r.ReadU64(),
                PrimaryPower = r.ReadU64()
            }, moduleId);
            ReadCount(r, () =>
            {
                var e = new LookOverSealSnapshot.PositionEntry
                {
                    Pos = r.ReadU8(), TypeId = r.ReadU32(), GoodsId = r.ReadU32()
                };
                ReadCount(r, () => e.AttrList.Add(ReadSealAttr(r)));
                e.Rating = r.ReadU32(); e.Strong = r.ReadU32(); e.Cell = r.ReadU16();
                s.PosList.Add(e);
            });
            ReadCount(r, () =>
            {
                var e = new LookOverSealSnapshot.PillEntry { GoodsId = r.ReadU32(), TotalNum = r.ReadU16() };
                ReadCount(r, () => e.AttrList.Add(ReadSealAttr(r)));
                s.PillList.Add(e);
            });
            ReadCount(r, () => s.StrenAttr.Add(ReadSealAttr(r)));
            ReadCount(r, () => s.EquipAttr.Add(ReadSealAttr(r)));
            ReadCount(r, () => s.PillAttr.Add(ReadSealAttr(r)));
            ReadCount(r, () => s.SuitAttr.Add(ReadSealAttr(r)));
            ReadCount(r, () => s.SuitList.Add(new LookOverSealSnapshot.SuitEntry { SuitId = r.ReadU16(), Num = r.ReadU32() }));
            FriendModel.Instance.SetLookOverModule(s);
        }

        private static LookOverSealSnapshot.AttrEntry ReadSealAttr(NetReader r) => new LookOverSealSnapshot.AttrEntry
        { Attr = r.ReadU8(), Value = r.ReadU32() };

        private void On19505(NetReader r)
        {
            var s = Stamp(new LookOverRevelationSnapshot
            {
                Title = "天启", MaxFigureId = r.ReadU16(), CurrentFigureId = r.ReadU16(),
                PrimaryPower = r.ReadU64(), AllScore = r.ReadU64()
            }, 6);
            ReadCount(r, () => s.Gathering.Add(new LookOverRevelationSnapshot.GatheringEntry
            {
                Pos = r.ReadU8(), Lv = r.ReadU16(), Exp = r.ReadU32(), Flag = r.ReadU8(),
                EquipId = r.ReadU32(), ItemId = r.ReadU32(), Score = r.ReadU64()
            }));
            ReadCount(r, () => s.Suit.Add(new LookOverRevelationSnapshot.SuitEntry { Star = r.ReadU32(), Num = r.ReadU32() }));
            ReadCount(r, () => s.SkillList.Add(new LookOverRevelationSnapshot.SkillEntry { SkillId = r.ReadU32(), Lv = r.ReadU16() }));
            FriendModel.Instance.SetLookOverModule(s);
        }

        private void On19506(NetReader r)
        {
            var s = Stamp(new LookOverIllusionSnapshot
            {
                Title = "幻化", PrimaryPower = r.ReadU64(), IllusionNum = r.ReadU16()
            }, 5);
            ReadCount(r, () =>
            {
                var e = new LookOverIllusionSnapshot.IllusionEntry { Type = r.ReadU8(), Num = r.ReadU8(), Power = r.ReadU64() };
                ReadCount(r, () =>
                {
                    var f = new LookOverIllusionSnapshot.FigureEntry
                    {
                        FigureType = r.ReadU8(), Id = r.ReadU16(), Stage = r.ReadU16(), Star = r.ReadU16(),
                        Combat = r.ReadU64(), EndTime = r.ReadU32()
                    };
                    ReadCount(r, () => f.AttrList.Add(ReadIllusionAttr(r)));
                    ReadCount(r, () => f.StarAttrList.Add(ReadIllusionAttr(r)));
                    ReadCount(r, () => f.SkillList.Add(r.ReadU32()));
                    e.FigureList.Add(f);
                });
                s.IllusionList.Add(e);
            });
            ReadCount(r, () =>
            {
                var p = new LookOverIllusionSnapshot.FashionPosEntry
                { PosId = r.ReadU8(), PosLv = r.ReadU8(), WearFashionId = r.ReadU32() };
                ReadCount(r, () =>
                {
                    var f = new LookOverIllusionSnapshot.FashionEntry
                    { FashionId = r.ReadU32(), StarLv = r.ReadU16(), Combat = r.ReadU64(), NowColorId = r.ReadU8() };
                    ReadCount(r, () => f.ColorList.Add(new LookOverIllusionSnapshot.ColorEntry
                    { ColorId = r.ReadU32(), StarLv = r.ReadU32() }));
                    p.FashionList.Add(f);
                });
                s.FashionPos.Add(p);
            });
            ReadCount(r, () => s.SelfPower.Add(ReadIllusionPower(r)));
            ReadCount(r, () => s.OthersPower.Add(ReadIllusionPower(r)));
            FriendModel.Instance.SetLookOverModule(s);
        }

        private static LookOverIllusionSnapshot.AttrEntry ReadIllusionAttr(NetReader r) => new LookOverIllusionSnapshot.AttrEntry
        { AttrId = r.ReadU8(), AttrVal = r.ReadU32() };
        private static LookOverIllusionSnapshot.PowerEntry ReadIllusionPower(NetReader r) => new LookOverIllusionSnapshot.PowerEntry
        { Type = r.ReadU8(), Combat = r.ReadU64() };

        private void On19507(NetReader r)
        {
            var s = Stamp(new LookOverGodBefallSnapshot { Title = "降神", PrimaryPower = r.ReadU64() }, 7);
            ReadCount(r, () =>
            {
                var g = new LookOverGodBefallSnapshot.GodEntry
                {
                    BattlePos = r.ReadU8(), GodId = r.ReadU32(), Lv = r.ReadU16(), Grade = r.ReadU16(),
                    Star = r.ReadU32(), Power = r.ReadU64(), GodStren = r.ReadU16()
                };
                ReadCount(r, () => g.EquipList.Add(new LookOverGodBefallSnapshot.EquipEntry
                { Pos = r.ReadU8(), GoodsId = r.ReadU64() }));
                s.GodBattleInfo.Add(g);
            });
            FriendModel.Instance.SetLookOverModule(s);
        }

        private void On19508(NetReader r)
        {
            var s = Stamp(new LookOverUnrealSnapshot { Title = "灵饰", PrimaryPower = r.ReadU64() }, 8);
            ReadCount(r, () =>
            {
                var e = new LookOverUnrealSnapshot.DecorationEntry
                { Pos = r.ReadU8(), GoodsId = r.ReadU64(), Lv = r.ReadU16(), StrenScore = r.ReadU64() };
                ReadCount(r, () => e.EquipExtraAttr.Add(new LookOverUnrealSnapshot.ExtraAttrEntry
                {
                    Color = r.ReadU8(), TypeId = r.ReadU8(), AttrId = r.ReadU16(), AttrVal = r.ReadU32(),
                    PlusInterval = r.ReadU8(), PlusUnit = r.ReadU32()
                }));
                s.DecorationList.Add(e);
            });
            FriendModel.Instance.SetLookOverModule(s);
        }

        private void On19509(NetReader r)
        {
            var s = Stamp(new LookOverLungSnapshot
            { Title = "神纹", PrimaryPower = r.ReadU64(), AllLevel = r.ReadU16() }, 9);
            ReadCount(r, () => s.DragonEquipList.Add(new LookOverLungSnapshot.DragonEquipEntry
            {
                Pos = r.ReadU8(), PosLv = r.ReadU16(), GoodsId = r.ReadU64(), StrenLv = r.ReadU16(),
                AwakeLv = r.ReadU16(), Combat = r.ReadU64()
            }));
            FriendModel.Instance.SetLookOverModule(s);
        }

        private void On19510(NetReader r)
        {
            var s = Stamp(new LookOverGodBeastSnapshot
            { Title = "蜃妖", PrimaryPower = r.ReadU64(), MaxNum = r.ReadU8(), BattleNum = r.ReadU8() }, 10);
            ReadCount(r, () =>
            {
                var e = new LookOverGodBeastSnapshot.EudemonsEntry { Id = r.ReadU8(), State = r.ReadU16(), Score = r.ReadU64() };
                ReadCount(r, () =>
                {
                    var q = new LookOverGodBeastSnapshot.EquipEntry
                    { Pos = r.ReadU8(), GoodsId = r.ReadU64(), Stren = r.ReadU16(), EquipScore = r.ReadU32() };
                    ReadCount(r, () => q.EquipExtraAttr.Add(new LookOverGodBeastSnapshot.ExtraAttrEntry
                    {
                        Color = r.ReadU8(), TypeId = r.ReadU8(), AttrId = r.ReadU16(), AttrVal = r.ReadU32(),
                        PlusInterval = r.ReadU8(), PlusUnit = r.ReadU32()
                    }));
                    e.EquipList.Add(q);
                });
                s.EudemonsList.Add(e);
            });
            FriendModel.Instance.SetLookOverModule(s);
        }

        private void On19511(NetReader r)
        {
            long companionPower = r.ReadU64();
            var s = Stamp(new LookOverPetSnapshot { Title = "神巫/妖灵", CompanionPower = companionPower }, 11);
            ReadCount(r, () =>
            {
                var e = new LookOverPetSnapshot.CompanionEntry
                {
                    Id = r.ReadU8(), Stage = r.ReadU16(), Star = r.ReadU16(), IsFight = r.ReadU8(),
                    TrainNum = r.ReadU16(), Combat = r.ReadU64()
                };
                ReadCount(r, () => e.SkillList.Add(new LookOverPetSnapshot.CompanionSkillEntry
                { SkillId = r.ReadU32(), Level = r.ReadU16() }));
                s.CompanionList.Add(e);
            });
            s.DemonsPower = r.ReadU64();
            s.BattleDemons = r.ReadU32();
            ReadCount(r, () =>
            {
                var e = new LookOverPetSnapshot.DemonEntry
                {
                    Id = r.ReadU32(), Level = r.ReadU16(), Star = r.ReadU8(), SlotNum = r.ReadU8(),
                    Combat = r.ReadU64() // Keep the wire value verbatim, including zero; never infer it from children.
                };
                ReadCount(r, () => e.SkillList.Add(new LookOverPetSnapshot.DemonSkillEntry
                { SkillId = r.ReadU32(), SkillLv = r.ReadU16(), Process = r.ReadU32(), IsActive = r.ReadU8() }));
                ReadCount(r, () => e.SlotSkill.Add(new LookOverPetSnapshot.SlotSkillEntry
                { SkillId = r.ReadU32(), SkillLv = r.ReadU16(), Slot = r.ReadU8(), Quality = r.ReadU8(), Sort = r.ReadU16() }));
                s.DemonsList.Add(e);
            });
            s.PrimaryPower = s.CompanionPower + s.DemonsPower;
            FriendModel.Instance.SetLookOverModule(s);
        }

        private void On19512(NetReader r)
        {
            var s = Stamp(new LookOverRuneSnapshot
            { Title = "御魂", PrimaryPower = r.ReadU64(), SkillLevel = r.ReadU8() }, 12);
            ReadCount(r, () =>
            {
                var e = new LookOverRuneSnapshot.RuneEntry
                {
                    PosId = r.ReadU8(), GoodsId = r.ReadU64(), GoodsTypeId = r.ReadU32(), Color = r.ReadU8(),
                    Lv = r.ReadU16(), SumAwakeLv = r.ReadU16()
                };
                ReadCount(r, () => e.AttrList.Add(new LookOverRuneSnapshot.AttrEntry
                { AttrId = r.ReadU32(), AttrNum = r.ReadU32(), AwakeLv = r.ReadU16() }));
                s.RuneList.Add(e);
            });
            FriendModel.Instance.SetLookOverModule(s);
        }

        private static void ReadCount(NetReader r, Action readOne)
        {
            int count = r.ReadU16();
            for (int i = 0; i < count; i++) readOne();
        }
    }
}
