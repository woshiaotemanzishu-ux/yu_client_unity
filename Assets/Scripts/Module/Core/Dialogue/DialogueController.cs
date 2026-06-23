using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Tasks;

namespace Shenxiao.Module.Core.Dialogue
{
    /// <summary>
    /// NPC 对话子系统协议层(对标老端 commonController/DialogueController.ts)。
    ///
    /// 链路:任务点击(TaskModel.DoTask 找 NPC 分支)/ 点 NPC → ShowTask(npc_id) → 发 12101 →
    /// On12101 装配 NpcDialogVo(NPC 默认对话)→ ShowNpcTalk:
    ///   · 单任务单行短路 → SelectTask → 发 12102 → On12102 用 talk_id 查 config_talk → 展示任务对话;
    ///   · 否则直接展示 NPC 默认对话 + 任务选项。
    /// 对话里点动作节点(接/交/对话事件)→ 发 30003/30004/30007;服务端回推 30001/30000 刷新任务。
    ///
    /// 对话文字 100% 来自 config_talk,NPC 身份来自 config_npc,任务来自 12101 真实回包——无假数据。
    /// 视图为最小原生 uGUI 临时壳(<see cref="DialogueView"/>,已标注),但数据与协议入口都是真的。
    /// </summary>
    public sealed class DialogueController : BaseController
    {
        public static readonly DialogueController Instance = new DialogueController();
        private DialogueController() { }

        private DialogueView _view;
        private DialogueModel Model => DialogueModel.Instance;

        protected override void Register()
        {
            RegisterProtocal(Proto.CC_NPC_TASK_LIST, On12101);
            RegisterProtocal(Proto.CC_NPC_TASK_TALK, On12102);
            EventDispatcher.On(GlobalEvent.EVT_GAME_START, OnGameStart);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_GAME_START, OnGameStart);
            CloseDialogueView();
            base.Dispose();
        }

        private async void OnGameStart()
        {
            // 对话依赖 config_npc(身份/默认对话 id)+ config_talk(对话文字),进游戏后预载(对标 TaskController.OnGameStart)。
            await NpcConfigs.EnsureLoaded();
            await TalkConfigs.EnsureLoaded();
            Model.Reset();
            GameLog.Info("Dialogue", "对话子系统就绪: config_npc={0} config_talk={1}", NpcConfigs.IsLoaded, TalkConfigs.IsLoaded);
        }

        // ===================== 入口:打开 NPC 对话 =====================

        /// <summary>
        /// 打开 NPC 对话(对标 DialogueController.ShowTask + SHOW_TASK 监听)。发 12101 取该 NPC 的关联任务。
        /// 已对同一 NPC 打开则不重复发(对标老端 dialog_is_open 去重,MainUITaskTeamView.ts:563-573)。
        /// </summary>
        public void ShowTask(int npcId)
        {
            _ = ShowTaskAsync(npcId);
        }

        private async Task ShowTaskAsync(int npcId)
        {
            Model.ChangeSceneClose = false;
            Model.IsAuto = false;          // 对标老端 SHOW_TASK 事件:发起对话前重置 auto 状态。
            if (!NpcConfigs.IsLoaded || !TalkConfigs.IsLoaded)
            {
                GameLog.Info("Dialogue", "ShowTask({0}) wait configs: config_npc={1} config_talk={2}",
                    npcId, NpcConfigs.IsLoaded, TalkConfigs.IsLoaded);
                await NpcConfigs.EnsureLoaded();
                await TalkConfigs.EnsureLoaded();
            }
            SendShowTaskNow(npcId);
        }

        private void SendShowTaskNow(int npcId)
        {
            if (!NpcConfigs.IsLoaded || !TalkConfigs.IsLoaded)
            {
                GameLog.Warn("Dialogue", "ShowTask({0}) 但对话配置未就绪(config_npc={1} config_talk={2}),先跳过",
                    npcId, NpcConfigs.IsLoaded, TalkConfigs.IsLoaded);
                return;
            }
            if (Model.DialogIsOpen && Model.CurrentNpcId == npcId)
            {
                GameLog.Info("Dialogue", "ShowTask({0}) 该 NPC 对话已打开,不重复请求", npcId);
                return;
            }
            Model.ChangeSceneClose = false;
            Model.IsAuto = false;          // 发事件过来的要重置 auto(对标 ts:140)
            Model.Scmd12101Send = true;
            SendFmt(Proto.CC_NPC_TASK_LIST, "i", npcId);
            GameLog.Info("Dialogue", "send 12101: npcId={0}", npcId);
        }

        // ===================== 12101:NPC 关联任务 =====================

        private void On12101(NetReader r)
        {
            // 只处理客户端主动发起的回包(对标 ts:35-36 scmd_12101_send 闸)。
            if (!Model.Scmd12101Send) return;
            Model.Scmd12101Send = false;

            int npcId = (int)r.ReadU32();
            int count = r.ReadU16();
            var taskList = new List<DialogueTaskEntry>(count);
            for (int i = 0; i < count; i++)
            {
                int taskId = (int)r.ReadU32();
                int taskState = r.ReadU8();
                string taskName = r.ReadString();
                int taskType = r.ReadU8();
                taskList.Add(new DialogueTaskEntry(taskId, taskState, taskName, taskType));
            }
            GameLog.Info("Dialogue", "recv 12101: npcId={0} tasks={1}", npcId, count);
            ShowNpcTalk(npcId, taskList);
        }

        // 对标 DialogueController.ShowNpcTalk(ts:215-287)
        private void ShowNpcTalk(int npcId, List<DialogueTaskEntry> taskList)
        {
            var vo = new NpcDialogVo();
            vo.Set12101Scmd(npcId, taskList);
            Model.CurrentVo = vo;

            int talkCount = taskList.Count;

            // 同 NPC 多任务(主线+支线)时,客户端先记的 NowSelectTaskId 决定取哪条,删掉非当前任务(对标 ts:228-252)。
            int nowSelect = TaskModel.Instance.NowSelectTaskId;
            if (nowSelect != 0 && taskList.Count > 1)
            {
                bool found = taskList.Exists(t => t.TaskId == nowSelect);
                if (found)
                {
                    taskList.RemoveAll(t => t.TaskId != nowSelect);
                    talkCount = taskList.Count;
                }
            }

            // 自动流程下空对话直接退出(对标 ts:256-259)。
            if (Model.IsAuto && talkCount == 0)
            {
                Model.IsAuto = false;
                return;
            }

            // 单内容块 + 单行 + 单任务 + 该任务非"接了未完成(2)" → 直接拉取任务对话(12102),不显示默认对话(对标 ts:262-266)。
            if (vo.GetTalkContentCount() == 1
                && vo.GetTalkContentListCount(1) == 1
                && talkCount == 1
                && taskList[0].TaskState != 2)
            {
                SelectTask(vo.NpcId, taskList[0].TaskId, taskList[0].TaskState);
            }
            else
            {
                // npc_id==0 且默认对话 → 不弹(对标 ts:268-270)。
                if (vo.NpcId == 0 && vo.IsDefaultTalk()) { Model.IsAuto = false; return; }

                if (!Model.IsAuto)
                {
                    ShowDialog(vo);
                }
                else if (talkCount > 0)
                {
                    bool anyShow = taskList.Exists(t => t.TaskState != 2);
                    if (anyShow && vo.TalkId != 0) ShowDialog(vo);
                }
            }
            Model.IsAuto = false;
        }

        // ===================== 12102:某任务的对话 =====================

        /// <summary>选择某任务 → 拉取它的对话(对标 SelectTask:发 12102)。</summary>
        public void SelectTask(int npcId, int taskId, int state)
        {
            SendFmt(Proto.CC_NPC_TASK_TALK, "ii", npcId, taskId);
            Model.IsAuto = false;
            GameLog.Info("Dialogue", "send 12102: npcId={0} taskId={1} state={2}", npcId, taskId, state);
        }

        private void On12102(NetReader r)
        {
            int npcId = (int)r.ReadU32();
            int taskId = (int)r.ReadU32();
            int talkId = (int)r.ReadU32();

            // 切场景期间的回包丢弃(对标 ts:47-49)。
            if (Model.ChangeSceneClose) return;
            GameLog.Info("Dialogue", "recv 12102: npcId={0} taskId={1} talkId={2}", npcId, taskId, talkId);

            var vo = new NpcDialogVo();
            vo.Set12102Scmd(npcId, taskId, talkId);
            Model.CurrentVo = vo;

            // 奖励(special_goods_list/award_list,对标 ts:56-80):config_task 真实数据经 TaskReward 按职业过滤解析,
            // 与完成弹层 TaskFinishView 共用同一解析。装配到 vo.RewardSummary,DialogueView 在对话里展示。
            TaskConfigs.TaskCfg cfg = TaskConfigs.Get(taskId);
            if (cfg != null)
            {
                List<TaskReward.Entry> rewards = TaskReward.Build(cfg.SpecialGoodsList, cfg.AwardList, RoleModel.Instance.Career);
                vo.Rewards = rewards;
                vo.RewardSummary = TaskReward.ToText(rewards);
                if (rewards.Count > 0)
                    GameLog.Info("Dialogue", "12102 任务 {0} 奖励 {1} 项: {2}", taskId, rewards.Count, TaskReward.ToText(rewards, " / "));
            }

            // 对话文字为空 → 不弹 UI,直接按可接/已接自动接/交(对标 ts:82-108 空文本自动接受/完成)。
            if (HasEmptyNpcText(vo))
            {
                if (TaskModel.Instance.IsCanTask(taskId)) AcceptTask(taskId);
                else if (TaskModel.Instance.IsReceivedTask(taskId)) FinishTask(taskId);
                return;
            }

            ShowDialog(vo);
        }

        // 任一 NPC(type==0)节点文字为空 → 触发空文本自动接/交分支。
        private static bool HasEmptyNpcText(NpcDialogVo vo)
        {
            int contentCount = vo.GetTalkContentCount();
            for (int c = 1; c <= contentCount; c++)
            {
                TalkConfigs.TalkContentBlock block = vo.GetContent(c);
                if (block == null) continue;
                for (int i = 0; i < block.Nodes.Count; i++)
                {
                    DialogueNodeVo n = block.Nodes[i];
                    if (n.Type == DialogueTypeConst.NPC && string.IsNullOrEmpty(n.Text.Trim())) return true;
                }
            }
            return false;
        }

        // ===================== 动作节点 → 任务协议 =====================

        /// <summary>接受任务(对标 AcceptTask:30003)。</summary>
        public void AcceptTask(int taskId)
        {
            SendFmt(Proto.CC_TASK_ACCEPT, "i", taskId);
            Model.IsAuto = true;
            GameLog.Info("Dialogue", "send 30003 accept task={0}", taskId);
        }

        /// <summary>完成/提交任务(对标 FinishTask:30004)。</summary>
        public void FinishTask(int taskId)
        {
            Model.IsAuto = true;
            SendFmt(Proto.CC_TASK_FINISH, "i", taskId);
            GameLog.Info("Dialogue", "send 30004 finish task={0}", taskId);
        }

        /// <summary>对话事件(对标 TalkToNPC:30007,注意发的是 npc_id)。</summary>
        public void TalkToNPC(int npcId, int taskId)
        {
            SendFmt(Proto.CC_TASK_TALK_EVENT, "i", npcId);
            Model.IsAuto = true;
            GameLog.Info("Dialogue", "send 30007 talk event npc={0} task={1}", npcId, taskId);
        }

        /// <summary>点击对话动作节点的派发(对标 ClickAnswerHandler:ts:296-308)。</summary>
        public void ClickAnswerHandler(NpcDialogVo vo, DialogueNodeVo node)
        {
            if (vo == null || node == null) return;
            switch (node.Type)
            {
                case DialogueTypeConst.FINISH_AND_TRIGGER: FinishTask(vo.TaskId); break;
                case DialogueTypeConst.TRIGGER: AcceptTask(vo.TaskId); break;
                case DialogueTypeConst.FINISH: FinishTask(vo.TaskId); break;
                case DialogueTypeConst.TRIGGER_AND_FINISH: break; // 老端点击无动作
                case DialogueTypeConst.TALK_EVENT: TalkToNPC(vo.NpcId, vo.TaskId); break;
            }
        }

        // ===================== 视图 =====================

        private void ShowDialog(NpcDialogVo vo)
        {
            if (vo == null) return;
            if (_view == null) _view = new DialogueView();
            _view.Open(vo);
        }

        public void CloseDialogueView()
        {
            _view?.Close();
        }
    }
}
