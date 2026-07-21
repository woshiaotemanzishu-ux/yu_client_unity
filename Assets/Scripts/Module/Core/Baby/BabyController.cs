using System;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>宝宝 182xx 数据层控制器：状态查询、操作结果落地及与老端一致的后续查询级联。</summary>
    public sealed class BabyController : BaseController
    {
        public static readonly BabyController Instance = new BabyController();

#if UNITY_EDITOR
        // CliVerify 出站截获缝：返回 true 时仅记录真实编码帧，不向活连接发送。
        private static Func<byte[], bool> s_outboundIntercept;
#endif

        private bool _startupRequested;

        private BabyController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.BABY_ERROR, On18200);
            RegisterProtocal(Proto.BABY_BASIC_INFO, On18201);
            RegisterProtocal(Proto.BABY_RAISE_INFO, On18203);
            RegisterProtocal(Proto.BABY_STAGE_INFO, On18204);
            RegisterProtocal(Proto.BABY_EQUIP_INFO, On18205);
            RegisterProtocal(Proto.BABY_FIGURE_INFO, On18206);
            RegisterProtocal(Proto.BABY_FAMILY_INFO, On18207);
            RegisterProtocal(Proto.BABY_LIKE_RANK, On18208);
            RegisterProtocal(Proto.BABY_LIKE_RECORDS, On18209);
            RegisterProtocal(Proto.BABY_ACTIVATE, On18210);
            RegisterProtocal(Proto.BABY_STAGE_UP, On18211);
            RegisterProtocal(Proto.BABY_FIGURE_STAR_UP, On18213);
            RegisterProtocal(Proto.BABY_FIGURE_WEAR, On18214);
            RegisterProtocal(Proto.BABY_RENAME, On18215);
            RegisterProtocal(Proto.BABY_EQUIP_WEAR, On18218);
            RegisterProtocal(Proto.BABY_EQUIP_UPGRADE, On18219);
            RegisterProtocal(Proto.BABY_PRAISE, On18217);
            RegisterProtocal(Proto.BABY_TASK_UPDATE, On18221);
            RegisterProtocal(Proto.BABY_TASK_REWARD, On18222);
            RegisterProtocal(Proto.BABY_FIGURE_POWER, On18223);
            RegisterProtocal(Proto.BABY_PRAISE_PUSH, On18224);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            _startupRequested = false;
            BabyModel.Instance.Reset();
            base.Dispose();
        }

        private void OnGameStart()
        {
            if (_startupRequested) return;
            _startupRequested = true;
            RequestStartup();
        }

        public void RequestStartup()
        {
            SendEmpty(Proto.BABY_BASIC_INFO);
        }

        public void RequestFamily() => SendEmpty(Proto.BABY_FAMILY_INFO);

        public void RequestActivate() => SendEmpty(Proto.BABY_ACTIVATE);

        public void RequestStageUp() => SendEmpty(Proto.BABY_STAGE_UP);

        public void RequestFigureStarUp(int babyId)
        {
            if (babyId <= 0) return;
            SendRequest(Proto.BABY_FIGURE_STAR_UP, "i", babyId);
        }

        public void RequestSetFigure(int type, int babyId)
        {
            if ((type != 1 && type != 2) || babyId <= 0) return;
            SendRequest(Proto.BABY_FIGURE_WEAR, "ci", type, babyId);
        }

        public void RequestRename(string name)
        {
            SendRequest(Proto.BABY_RENAME, "s", name ?? string.Empty);
        }

        public void RequestTaskReward(int taskId)
        {
            if (taskId <= 0 || taskId > ushort.MaxValue) return;
            SendRequest(Proto.BABY_TASK_REWARD, "h", taskId);
        }

        public void RequestFigurePower(int babyId)
        {
            if (babyId <= 0) return;
            SendRequest(Proto.BABY_FIGURE_POWER, "i", babyId);
        }

        public void RequestEquipInfo() => SendEmpty(Proto.BABY_EQUIP_INFO);
        public void RequestEquipWear(int posId, long goodsId)
        {
            if (posId < 1 || posId > 6 || goodsId <= 0) return;
            SendRequest(Proto.BABY_EQUIP_WEAR, "cl", posId, goodsId);
        }

        /// <summary>仅协议前置：服务端会自动选材并扣料，未提供消耗预览/确认前不得直接绑定 UI 按钮。</summary>
        public void RequestEquipUpgrade(int posId)
        {
            if (posId < 1 || posId > 6) return;
            SendRequest(Proto.BABY_EQUIP_UPGRADE, "c", posId);
        }

        public void RequestLikeRank() => SendEmpty(Proto.BABY_LIKE_RANK);

        public void RequestLikeRecords() => SendEmpty(Proto.BABY_LIKE_RECORDS);

        public void RequestPraise(long roleId, int opr)
        {
            if (roleId <= 0 || (opr != 1 && opr != 2)) return;
            SendRequest(Proto.BABY_PRAISE, "lc", roleId, opr);
        }

        private void SendEmpty(int protoId)
        {
            SendRequest(protoId);
        }

        private void SendRequest(int protoId, string format = null, params object[] args)
        {
#if UNITY_EDITOR
            if (s_outboundIntercept != null)
            {
                byte[] frame = UserMsgAdapter.Encode(protoId, format, args);
                if (s_outboundIntercept(frame)) return;
            }
#endif
            SendFmt(protoId, format, args);
        }

        private void On18200(NetReader r)
        {
            BabyErrorInfo info = new BabyErrorInfo
            {
                Command = r.ReadU16(),
                ErrorCode = r.ReadI32(),
                Args = r.ReadString()
            };
            BabyModel.Instance.ApplyError(info);
            GameLog.Warn("Baby", "18200 cmd={0} error={1} args={2}", info.Command, info.ErrorCode, info.Args);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_ERROR);
        }

        private void On18201(NetReader r)
        {
            BabyBasicInfo info = new BabyBasicInfo
            {
                ActiveTime = r.ReadI32(),
                BabyId = r.ReadI32(),
                BabyName = r.ReadString(),
                IsChangeName = r.ReadU8() != 0
            };
            BabyModel.Instance.ApplyBasic(info);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_BASIC_INFO);
            if (!info.IsActive) return;

            SendEmpty(Proto.BABY_RAISE_INFO);
            SendEmpty(Proto.BABY_STAGE_INFO);
            SendEmpty(Proto.BABY_EQUIP_INFO);
            SendEmpty(Proto.BABY_FIGURE_INFO);
            SendEmpty(Proto.BABY_FAMILY_INFO);
        }

        private void On18203(NetReader r)
        {
            BabyRaiseInfo info = new BabyRaiseInfo
            {
                RaiseLevel = r.ReadU16(),
                RaiseExp = r.ReadI32()
            };
            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                info.TaskList.Add(new BabyTaskInfo
                {
                    TaskId = r.ReadU16(),
                    FinishNum = r.ReadU16(),
                    FinishState = r.ReadU8()
                });
            }
            info.Power = r.ReadI32();
            BabyModel.Instance.ApplyRaise(info);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_RAISE_INFO);
        }

        private void On18204(NetReader r)
        {
            BabyStageInfo info = new BabyStageInfo
            {
                Stage = r.ReadU16(),
                StageLevel = r.ReadU8(),
                StageExp = r.ReadI32(),
                Power = r.ReadI32()
            };
            BabyModel.Instance.ApplyStage(info);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_STAGE_INFO);
        }

        private void On18205(NetReader r)
        {
            BabyEquipInfo info = new BabyEquipInfo();
            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                info.EquipList.Add(new BabyEquipEntry
                {
                    PositionId = r.ReadU8(),
                    Id = r.ReadU64(),
                    GoodsTypeId = r.ReadI32(),
                    Stage = r.ReadU16(),
                    StageLevel = r.ReadU16(),
                    StageExp = r.ReadI32(),
                    SkillId = r.ReadI32()
                });
            }
            info.Power = r.ReadI32();
            BabyModel.Instance.ApplyEquip(info);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_EQUIP_INFO);
        }

        private void On18206(NetReader r)
        {
            BabyFigureInfo info = new BabyFigureInfo();
            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                info.ActiveList.Add(new BabyFigureEntry
                {
                    BabyId = r.ReadI32(),
                    BabyStar = r.ReadU16()
                });
            }
            BabyModel.Instance.ApplyFigures(info);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_FIGURE_INFO);
        }

        private void On18207(NetReader r)
        {
            BabyFamilyInfo info = new BabyFamilyInfo();
            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                BabyFamilyEntry entry = new BabyFamilyEntry
                {
                    RoleId = r.ReadU64(),
                    ActiveTime = r.ReadI32(),
                    BabyId = r.ReadI32(),
                    BabyName = r.ReadString(),
                    RaiseLevel = r.ReadU16(),
                    Stage = r.ReadU16(),
                    StageLevel = r.ReadU8(),
                    BabyPower = r.ReadI32()
                };
                int attrGroupCount = r.ReadU16();
                for (int j = 0; j < attrGroupCount; j++)
                {
                    BabyAttrGroup group = new BabyAttrGroup { Type = r.ReadU8() };
                    int attrCount = r.ReadU16();
                    for (int k = 0; k < attrCount; k++)
                    {
                        group.AttrList.Add(new BabyAttrEntry
                        {
                            AttrId = r.ReadU16(),
                            Value = r.ReadI32()
                        });
                    }
                    entry.AttrInfo.Add(group);
                }
                info.InfoList.Add(entry);
            }
            info.InfoList.Reverse();
            BabyModel.Instance.ApplyFamily(info);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_FAMILY_INFO);
        }

        private void On18210(NetReader r)
        {
            BabyActivateResult result = new BabyActivateResult { Code = r.ReadI32() };
            BabyModel.Instance.ApplyActivateResult(result);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_ACTIVATE);
            if (result.Succeeded) SendEmpty(Proto.BABY_BASIC_INFO);
        }

        private void On18211(NetReader r)
        {
            BabyStageUpResult result = new BabyStageUpResult
            {
                Code = r.ReadI32(),
                Stage = r.ReadU16(),
                StageLevel = r.ReadU8(),
                StageExp = r.ReadI32(),
                Power = r.ReadI32()
            };
            BabyModel.Instance.ApplyStageUpResult(result);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_STAGE_UP);
        }

        private void On18213(NetReader r)
        {
            BabyFigureStarResult result = new BabyFigureStarResult
            {
                Code = r.ReadI32(),
                BabyId = r.ReadI32(),
                BabyStar = r.ReadU16(),
                Power = r.ReadU64(),
                NextPower = r.ReadU64()
            };
            BabyModel model = BabyModel.Instance;
            model.ApplyFigureStarResult(result);
            if (result.Succeeded)
            {
                bool hadActivatedFigure = model.HasAnyActivatedFigure();
                bool added = model.MergeFigure(result.BabyId, result.BabyStar, result.Power, result.NextPower);
                if (added) SendEmpty(Proto.BABY_FIGURE_INFO);
                if (result.BabyStar == 1 && !hadActivatedFigure) RequestSetFigure(1, result.BabyId);
            }
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_FIGURE_STAR_UP);
        }

        private void On18214(NetReader r)
        {
            BabyFigureWearResult result = new BabyFigureWearResult
            {
                Code = r.ReadI32(),
                Type = r.ReadU8(),
                BabyId = r.ReadI32()
            };
            BabyModel.Instance.ApplyFigureWearResult(result);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_FIGURE_WEAR);
        }

        private void On18215(NetReader r)
        {
            BabyRenameResult result = new BabyRenameResult
            {
                Code = r.ReadI32(),
                Name = r.ReadString()
            };
            BabyModel.Instance.ApplyRenameResult(result);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_RENAME);
        }

        private void On18221(NetReader r)
        {
            int taskId = r.ReadU16();
            int finishNum = r.ReadU16();
            int finishState = r.ReadU8();
            if (finishState == 1)
            {
                SendEmpty(Proto.BABY_RAISE_INFO);
                return;
            }
            if (BabyModel.Instance.TryApplyTaskProgress(taskId, finishNum, finishState))
            {
                EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_TASK_UPDATE);
            }
        }

        private void On18208(NetReader r)
        {
            BabyPraiseRankInfo info = new BabyPraiseRankInfo { RoleId = r.ReadU64() };
            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                info.Entries.Add(new BabyPraiseRankEntry
                {
                    RoleId = r.ReadU64(),
                    Name = r.ReadString(),
                    BabyPower = r.ReadI32(),
                    PraiseNum = r.ReadI32()
                });
            }
            BabyModel.Instance.ApplyPraiseRank(info);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_LIKE_RANK);
        }

        private void On18209(NetReader r)
        {
            BabyPraiseRecordsInfo info = new BabyPraiseRecordsInfo();
            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                info.Entries.Add(new BabyPraiseRecordEntry
                {
                    PraiserId = r.ReadU64(),
                    Name = r.ReadString(),
                    IsPraiseBack = r.ReadU8() != 0
                });
            }
            BabyModel.Instance.ApplyPraiseRecords(info);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_LIKE_RECORDS);
        }

        private void On18217(NetReader r)
        {
            BabyPraiseActionResult result = new BabyPraiseActionResult
            {
                Code = r.ReadI32(),
                RoleId = r.ReadU64(),
                Opr = r.ReadU8()
            };
            int count = r.ReadU16();
            for (int i = 0; i < count; i++)
            {
                result.Rewards.Add(new BabyPraiseRewardEntry
                {
                    Type = r.ReadU8(),
                    TypeId = r.ReadI32(),
                    Num = r.ReadI32()
                });
            }
            BabyModel.Instance.ApplyPraiseAction(result);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_PRAISE);
            if (result.Succeeded && result.Opr != 1) SendEmpty(Proto.BABY_LIKE_RECORDS);
        }

        private void On18218(NetReader r)
        {
            var result = new BabyEquipWearResult
            {
                Code = r.ReadI32(),
                PositionId = r.ReadU8(),
                Id = r.ReadU64(),
                GoodsTypeId = r.ReadI32(),
                SkillId = r.ReadI32(),
                Power = r.ReadI32()
            };
            BabyModel.Instance.ApplyEquipWearResult(result);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_EQUIP_WEAR);
        }

        private void On18219(NetReader r)
        {
            var result = new BabyEquipUpgradeResult
            {
                Code = r.ReadI32(),
                PositionId = r.ReadU8(),
                Id = r.ReadU64(),
                GoodsTypeId = r.ReadI32(),
                Stage = r.ReadU16(),
                StageLevel = r.ReadU16(),
                StageExp = r.ReadI32(),
                Power = r.ReadI32()
            };
            bool updated = BabyModel.Instance.ApplyEquipUpgradeResult(result);
            if (result.Succeeded && !updated) RequestEquipInfo();
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_EQUIP_UPGRADE);
        }

        private void On18224(NetReader r)
        {
            BabyPraisePush push = new BabyPraisePush { PraiserId = r.ReadU64() };
            if (push.PraiserId == RoleModel.Instance.RoleId) return;
            BabyModel.Instance.ApplyPraisePush(push);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_PRAISE_PUSH);
        }

        private void On18222(NetReader r)
        {
            BabyTaskRewardResult result = new BabyTaskRewardResult
            {
                Code = r.ReadI32(),
                TaskId = r.ReadU16(),
                FinishNum = r.ReadU16(),
                FinishState = r.ReadU8()
            };
            BabyModel.Instance.ApplyTaskRewardResult(result);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_TASK_REWARD);
            SendEmpty(Proto.BABY_RAISE_INFO);
        }

        private void On18223(NetReader r)
        {
            BabyFigurePowerResult result = new BabyFigurePowerResult
            {
                BabyId = r.ReadI32(),
                BabyStar = r.ReadU16(),
                Power = r.ReadU64(),
                NextPower = r.ReadU64()
            };
            BabyModel.Instance.ApplyFigurePowerResult(result);
            EventDispatcher.Emit(GlobalEvent.EVT_BABY_UPDATE, Proto.BABY_FIGURE_POWER);
        }
    }
}
