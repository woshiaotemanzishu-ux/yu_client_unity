using System;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Baby
{
    /// <summary>宝宝 182xx 首包控制器，仅负责只读状态与老端一致的查询级联。</summary>
    public sealed class BabyController : BaseController
    {
        public static readonly BabyController Instance = new BabyController();

#if UNITY_EDITOR
        private static Func<int, bool> s_requestIntercept;
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
            RegisterProtocal(Proto.BABY_TASK_UPDATE, On18221);
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

        private void SendEmpty(int protoId)
        {
#if UNITY_EDITOR
            if (s_requestIntercept != null && s_requestIntercept(protoId)) return;
#endif
            SendFmt(protoId);
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
    }
}
