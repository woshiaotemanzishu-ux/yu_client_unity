using System.Collections.Generic;

namespace Shenxiao.Module.Core.Dialogue
{
    /// <summary>
    /// 运行期对话载荷(对标老端 dialogue/NpcDialogVo.ts)。由 12101/12102 回包装配,驱动 DialogueView。
    /// - Set12101Scmd → InitVo:用 NPC 默认对话(config_npc[npc_id].talk → config_talk),找不到回退 DEFAULT_TALK。
    /// - Set12102Scmd → InitVo2:用回包给的 talk_id 直接查 config_talk(无默认回退)。
    /// </summary>
    public sealed class NpcDialogVo
    {
        public int TalkId;
        public int NpcId;
        public int TaskId;
        public List<DialogueTaskEntry> TaskList = new List<DialogueTaskEntry>();
        public TalkConfigs.TalkCfg TalkCfg;
        public bool UseDefaultTalk;

        /// <summary>12101:npc_id + task_list,对话 id 取 NPC 默认对话(InitVo)。</summary>
        public void Set12101Scmd(int npcId, List<DialogueTaskEntry> taskList)
        {
            NpcId = npcId;
            TaskList = taskList ?? new List<DialogueTaskEntry>();
            InitVo();
        }

        /// <summary>12102:npc_id + task_id + talk_id,直接按 talk_id 查对话(InitVo2)。</summary>
        public void Set12102Scmd(int npcId, int taskId, int talkId)
        {
            NpcId = npcId;
            TaskId = taskId;
            TalkId = talkId;
            InitVo2();
        }

        // 对标 NpcDialogVo.ts:44-63
        private void InitVo()
        {
            TalkId = DialogueModel.Instance.GetDialogueId(NpcId);
            TalkCfg = DialogueModel.Instance.GetTalkCfg(TalkId);
            if (TalkCfg == null)
            {
                TalkCfg = DialogueModel.DefaultTalk;
                UseDefaultTalk = true;
            }
        }

        // 对标 NpcDialogVo.ts:65-77(无默认回退)
        private void InitVo2()
        {
            TalkCfg = DialogueModel.Instance.GetTalkCfg(TalkId);
        }

        public int GetTalkContentCount() => TalkCfg?.ContentCount ?? 0;

        public int GetTalkContentListCount(int contentIndex1Based)
            => TalkCfg?.GetContent(contentIndex1Based)?.TotalList ?? 0;

        public TalkConfigs.TalkContentBlock GetContent(int contentIndex1Based)
            => TalkCfg?.GetContent(contentIndex1Based);

        public bool IsDefaultTalk() => UseDefaultTalk;
    }
}
