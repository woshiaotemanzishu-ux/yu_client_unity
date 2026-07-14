using System;
using System.Threading.Tasks;
using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Scene.Vo;
using UnityEngine;

namespace Shenxiao.Module.Core.Scene
{
    /// <summary>
    /// 采集(采集物/任务采集)控制器。把老客户端散在 FightController(20008 收发)+ CollectBarView(进度/请求)+
    /// Scene/Role(蹲下采集动作)+ SceneController(取消)的采集链路,在 Unity 收拢成一个控制器,逐字节对标老端:
    ///
    /// C2S/S2C 同号 20008(对标 FightController.ts:583 handler20008 + :867 onPickMonsterRequest):
    ///   发 "iic"(ins_id, type_id, flag):flag 1=请求开始 / 2=请求完成 / 3=取消;
    ///   回 "c"(flag):1=开始成功(START)→主角蹲下采集 + 进度条;2=完成成功(COMPLETE)→收尾、删采集物;
    ///   ≥3=各类失败/取消(3 太远 / 5 时间不足 / 7 次数满 / 13 正被他人采集 …)。
    ///
    /// 任务/点击入口都走 <see cref="CollectMonster"/>(接近到采集范围内 → 发起采集)。完成后由服务端推 30001
    /// (任务进度)→ TaskController 续驱动下一个采集物/下一任务;本控制器不抢驱动,避免与权威进度竞争。
    /// </summary>
    public sealed class CollectController : BaseController
    {
        public static readonly CollectController Instance = new CollectController();
        private CollectController() { }

        // 老端 MonsterType:TASK_COLLECT=2(任务采集物,采集后删再 0.5s 加回,支撑多次采集同一节点);
        // NOT_DEAD_COLLECT=8(无限/资源型,采集后不删)。其余采集型采集成功即删可见物(服务端 12006 兜底)。
        private const int TASK_COLLECT = 2;
        private const int NOT_DEAD_COLLECT = 8;
        private const int FLAG_START = 1;
        private const int FLAG_COMPLETE = 2;
        private const int FLAG_CANCEL = 3;

        // 采集范围(对标老端 Scene.MainRoleAttackMonster 采集物分支 attack_range=90)。
        private const float CollectRange = 90f;
        private const float ApproachArriveLogicDist = 0.6f; // 接近站位点到达半径(逻辑格,同 SceneCombat 接近 BOSS)
        private const int StartWatchdogMs = 5000;           // 发 FLAG_START 后等 START 回包的超时;超时复位防永久锁
        private const int TaskCollectReAddMs = 500;         // 任务采集物采集后重新加回的延时(对标老端 0.5s addMonster)

        private int _insId;
        private int _typeId;
        private int _pickTimeSec;
        private bool _collecting; // 已发起采集、等 START 或进度中
        private bool _started;    // 服务端已回 START(进度/动作进行中)
        private int _epoch;       // 采集会话代次:取消/换目标/复位即 +1,使在途完成计时器失效

        /// <summary>当前是否在采集中(等 START 或进度中)。任务链据此避免重复驱动。</summary>
        public bool IsCollecting => _collecting;

        protected override void Register()
        {
            RegisterProtocal(Proto.CS_COLLECT, On20008);
            EventDispatcher.On(GlobalEvent.EVT_COLLECT_MOVE_CANCEL, OnMoveCancel);
            EventDispatcher.On(GlobalEvent.EVT_SCENE_OBJECTS_CLEARED, OnSceneCleared);
            // 采集物在采集中被移除(他人采完/服务端 12006/despawn)→ 取消采集复位(对标老端 MONSTER_VO_REMOVE → CANCEL_TO_COLLECT)。
            SceneManager.Instance.MonsterRemoved += OnMonsterRemoved;
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_COLLECT_MOVE_CANCEL, OnMoveCancel);
            EventDispatcher.Off(GlobalEvent.EVT_SCENE_OBJECTS_CLEARED, OnSceneCleared);
            SceneManager.Instance.MonsterRemoved -= OnMonsterRemoved;
            ResetState();
            base.Dispose();
        }

        /// <summary>
        /// 采集一个采集物(对标老端 Scene.MainRoleAttackMonster 采集物分支:在采集范围内 → 直接采;
        /// 超范围 → 接近到采集物正前方站位点后再采)。任务驱动与玩家点击共用此入口。
        /// </summary>
        public void CollectMonster(MonsterVo mon)
        {
            if (mon == null) return;
            if (_collecting && _insId == mon.InstanceId) return; // 已在采这个

            MainRoleAgent agent = MainRoleAgent.Current;
            if (agent == null)
            {
                // 无 3D 主角(headless):采集协议链不依赖走位,直接发起。
                RequestCollect(mon.InstanceId, mon.TypeId, mon.PickTime);
                return;
            }

            RoleModel role = RoleModel.Instance;
            float dx = mon.X - role.X;
            float dy = mon.Y - role.Y;
            float dist2 = dx * dx + dy * dy;
            if (dist2 <= CollectRange * CollectRange)
            {
                agent.FaceTowardPixel(mon.X, mon.Y);
                RequestCollect(mon.InstanceId, mon.TypeId, mon.PickTime);
                return;
            }

            // 超范围:接近到采集物正前方站位点(玩家这一侧)后再采(对标老端 StartTargetAction 停在攻击距离处)。
            float dist = Mathf.Sqrt(dist2);
            float stopDist = CollectRange * 0.85f;
            float ax = mon.X, ay = mon.Y;
            if (dist > 0.01f)
            {
                ax = mon.X - dx / dist * stopDist;
                ay = mon.Y - dy / dist * stopDist;
            }

            int insId = mon.InstanceId;
            GameLog.Info("Collect", "采集物 ins={0} type={1} 距离={2:F0}px > 采集范围={3:F0}px → 接近站位点({4:F0},{5:F0})",
                insId, mon.TypeId, dist, CollectRange, ax, ay);
            agent.MoveToNpc(ax, ay, ApproachArriveLogicDist, () =>
            {
                MonsterVo cur = SceneManager.Instance.GetMonster(insId);
                if (cur == null)
                {
                    GameLog.Info("Collect", "接近完成但采集物 ins={0} 已不在 → 放弃(任务链会再找下一个)", insId);
                    return;
                }
                agent.FaceTowardPixel(cur.X, cur.Y);
                RequestCollect(cur.InstanceId, cur.TypeId, cur.PickTime);
            });
        }

        /// <summary>发起采集请求(对标老端 CollectBarView.RequestToCollect → REQUEST_TO_COLLECT flag=1 → 20008)。</summary>
        private bool RequestCollect(int insId, int typeId, int pickTimeSec)
        {
            if (!NetManager.IsConnected) return false;
            if (_collecting)
            {
                if (_insId == insId) return true;
                // 正在采别的:若旧目标已不在场景(卡死自愈),重置后采新的;否则忽略新请求。
                MonsterVo old = SceneManager.Instance.GetMonster(_insId);
                if (old != null)
                {
                    GameLog.Info("Collect", "已在采 ins={0},忽略新采集请求 ins={1}", _insId, insId);
                    return false;
                }
                GameLog.Info("Collect", "旧采集目标 ins={0} 已不在 → 自愈重置,改采 ins={1}", _insId, insId);
                ResetState();
            }

            _insId = insId;
            _typeId = typeId;
            _pickTimeSec = pickTimeSec;
            _collecting = true;
            _started = false;
            SendFmt(Proto.CS_COLLECT, "iic", insId, typeId, FLAG_START);
            GameLog.Info("Collect", "send 20008 采集开始请求 ins={0} type={1} pickTime={2}s", insId, typeId, pickTimeSec);
            _ = StartWatchdogAsync(_epoch); // 防 START 回包丢失致 _collecting 永久锁
            return true;
        }

        // START 看门狗:发 FLAG_START 后若 StartWatchdogMs 内未收到 START(flag=1),复位并触发任务重试,
        // 避免服务端静默丢弃请求时 _collecting 永久为真、堵死后续采集(对标老端"假设服务端必回"的兜底)。
        private async Task StartWatchdogAsync(int epoch)
        {
            await TimeUtil.Delay(StartWatchdogMs);
            if (epoch != _epoch || !_collecting || _started) return; // 已换会话/已复位/已开始
            GameLog.Warn("Collect", "START 回包超时(ins={0})→ 复位采集态并触发重试", _insId);
            FinishVisualAndState();
            FireCollectEnded();
        }

        private void On20008(NetReader reader)
        {
            int flag = reader.ReadU8();
            GameLog.Info("Collect", "recv 20008 flag={0}(collecting={1} ins={2})", flag, _collecting, _insId);
            if (flag == FLAG_START) OnServerStart();
            else if (flag == FLAG_COMPLETE) OnServerComplete();
            else OnServerCancel(flag);
        }

        // flag==1:开始成功(对标 handler20008 flag==1:Fire(START_TO_COLLECT) + MainRoleDoCollect)。
        private void OnServerStart()
        {
            if (!_collecting || _started) return; // 已开始则忽略重复 START(防重复计时器/重复完成请求)
            _started = true;
            MainRoleAgent.Current?.DoCollect();

            // 进度条纯表现:独立 prefab 按需加载;缺/加载未完成也不影响采集闭环——"完成请求"由控制器按 pickTime
            // 计时驱动(下方),而非依赖视图,保证进度条不可用时采集仍能正常完成。
            _ = ShowCollectBarAsync(_pickTimeSec, _epoch);
            _ = WaitThenRequestCompleteAsync(_pickTimeSec, _epoch);
        }

        // 异步加载/显示采集进度条(独立 prefab),加载完成后若本次采集仍在进行才开始进度动画。
        private async Task ShowCollectBarAsync(int pickTimeSec, int epoch)
        {
            CollectBarView view = await MainUIFlow.EnsureCollectBarViewAsync();
            if (view == null) return; // 进度条不可用:仅表现缺失,采集逻辑不受影响
            if (epoch != _epoch || !_collecting || !_started) return; // 加载期间已取消/完成/换目标
            view.BeginCollect(pickTimeSec);
        }

        // 采集计时满 → 请求完成(对标老端 Collecting 满 40 步 / 总时长 = pickTime 秒 → REQUEST_TO_COLLECT flag=2)。
        // 计时在控制器侧(非视图),并以 _epoch 守卫:期间被取消/换目标/复位/断线则不发完成。
        private async Task WaitThenRequestCompleteAsync(int pickTimeSec, int epoch)
        {
            int ms = Mathf.Max(200, pickTimeSec * 1000);
            await TimeUtil.Delay(ms);
            if (epoch != _epoch || !_collecting || !_started) return;
            if (!NetManager.IsConnected) { FinishVisualAndState(); return; }
            SendFmt(Proto.CS_COLLECT, "iic", _insId, _typeId, FLAG_COMPLETE);
            GameLog.Info("Collect", "采集计时满({0}s)→ send 20008 采集完成请求 ins={1} type={2}", pickTimeSec, _insId, _typeId);
        }

        // flag==2:完成成功(对标 handler20008 flag==2 + CollectBarView.onCompleteCollect:删采集物;
        // 任务采集物(TASK_COLLECT=2)还要 0.5s 后把同一 vo 加回 —— 这正是多次采集同一节点(如"采集3个")的关键:
        // 服务端不会重发该采集物,靠客户端加回让任务等待链(WaitCollectMonster/MonsterAdded)接着采下一次)。
        private void OnServerComplete()
        {
            int ins = _insId;
            FinishVisualAndState(); // 先复位(_collecting=false),下面 DeleteSceneObj 触发的 MonsterRemoved 不会误判为 despawn 取消

            MonsterVo vo = SceneManager.Instance.GetMonster(ins);
            if (vo == null)
            {
                GameLog.Info("Collect", "采集完成 ins={0}(采集物已不在场景,无需删除)", ins);
                return;
            }
            if (vo.Type == NOT_DEAD_COLLECT)
            {
                GameLog.Info("Collect", "采集完成 ins={0}(NOT_DEAD_COLLECT,不删除)", ins);
                return;
            }

            int sceneId = RoleModel.Instance.SceneId;
            SceneManager.Instance.DeleteSceneObj(ins);
            if (vo.Type == TASK_COLLECT)
            {
                _ = ReAddTaskCollectAsync(vo, sceneId); // 0.5s 后加回,支撑多次采集(对标老端 addMonster)
                GameLog.Info("Collect", "采集完成 ins={0}(TASK_COLLECT,删后将 {1}ms 加回以支撑多次采集)", ins, TaskCollectReAddMs);
            }
            else
            {
                GameLog.Info("Collect", "采集完成 ins={0} type={1}(普通采集物,删除,服务端 12006 兜底)", ins, vo.Type);
            }
        }

        // 任务采集物采集后延时加回(对标老端 CollectBarView.onCompleteCollect 的 0.5s addMonster)。
        // 切场景(sceneId 变)或服务端已重发(同 id 已在场景)则不加回。
        private async Task ReAddTaskCollectAsync(MonsterVo vo, int sceneId)
        {
            await TimeUtil.Delay(TaskCollectReAddMs);
            if (vo == null) return;
            if (RoleModel.Instance.SceneId != sceneId) return;                 // 已切场景,不加回
            if (SceneManager.Instance.GetMonster(vo.InstanceId) != null) return; // 服务端/他途已加回
            SceneManager.Instance.AddMonster(vo);
            GameLog.Info("Collect", "任务采集物加回 ins={0} type={1}(支撑下一次采集)", vo.InstanceId, vo.TypeId);
        }

        // flag>=3:各类失败/取消(对标 handler20008 flag>2:Fire(SERVER_CANCEL_TO_COLLECT))。
        private void OnServerCancel(int flag)
        {
            string msg = CollectErrorMessage(flag);
            if (!string.IsNullOrEmpty(msg)) TipsManager.Toast(msg);
            GameLog.Info("Collect", "采集被服务端取消 flag={0} msg={1} ins={2}", flag, msg ?? "", _insId);
            FinishVisualAndState();
            // 无 30001 推进:由任务链延时重试接手(对标老端 13"正被他人采集"等 → 1s 后 FindNextOne)。
            FireCollectEnded();
        }

        // 采集物在采集中被移除(他人采完/despawn/服务端 12006)→ 取消采集复位(对标老端 onMonsterRemove → CANCEL_TO_COLLECT)。
        private void OnMonsterRemoved(int instanceId)
        {
            if (!_collecting || instanceId != _insId) return;
            GameLog.Info("Collect", "采集物 ins={0} 采集中被移除 → 取消采集复位", instanceId);
            if (_started && NetManager.IsConnected) SendFmt(Proto.CS_COLLECT, "iic", _insId, _typeId, FLAG_CANCEL);
            FinishVisualAndState();
            FireCollectEnded(); // 让任务链重新找/等下一个采集物
        }

        // 主角起步打断采集(对标老端 MAINROLE_MOVE_EVENT_IMME → REQUEST_TO_COLLECT flag=3)。
        private void OnMoveCancel()
        {
            if (!_collecting) return;
            GameLog.Info("Collect", "主角起步 → 取消采集 ins={0}", _insId);
            if (NetManager.IsConnected) SendFmt(Proto.CS_COLLECT, "iic", _insId, _typeId, FLAG_CANCEL);
            // 动作已被移动接管,这里只收进度条 + 复位(QuitCollect 在 _collecting 已置 false 时无副作用)。
            // 玩家主动移动 → 不自动重试(不与玩家抢操作);恢复自动后由任务链常规驱动。
            MainUIFlow.CollectBarViewOrNull?.StopCollect();
            ResetState();
        }

        // 切场景/登出:场景对象表清空,采集态复位(进度条随 MainUI 处理,这里只清状态)。新场景任务流自会驱动,不在此重试。
        private void OnSceneCleared()
        {
            if (!_collecting) return;
            GameLog.Info("Collect", "切场景 → 复位采集态 ins={0}", _insId);
            FinishVisualAndState();
        }

        // 采集非成功终止(失败/取消/被移除/START 超时)→ 通知任务链延时重试当前采集任务(对标老端 FindNextOne)。
        // 成功(flag=2)走服务端 30001 驱动,不发此事件,避免双重驱动。
        private static void FireCollectEnded()
        {
            EventDispatcher.Emit(GlobalEvent.EVT_COLLECT_ENDED);
        }

        private void FinishVisualAndState()
        {
            MainRoleAgent.Current?.QuitCollect();
            MainUIFlow.CollectBarViewOrNull?.StopCollect();
            ResetState();
        }

        private void ResetState()
        {
            _epoch++; // 使在途完成计时器(WaitThenRequestCompleteAsync)失效
            _collecting = false;
            _started = false;
            _insId = 0;
            _typeId = 0;
            _pickTimeSec = 0;
        }

        /// <summary>采集失败码文案(对标老端 handler20008 的 Message.show 分支,取任务采集会遇到的子集)。</summary>
        private static string CollectErrorMessage(int flag)
        {
            switch (flag)
            {
                case 3: return "距离采集物太远";
                case 4: return "采集失败";
                case 5: return "采集时间不足";
                case 7: return "采集次数已满";
                case 13: return "正在被他人采集";
                case 15: return "背包空间不足";
                default: return null;
            }
        }
    }
}
