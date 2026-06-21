namespace Shenxiao.Module.Core.Dialogue
{
    /// <summary>
    /// 单条对话/选项(对标老端 DialogueNodeVo.ts + config_talk 的 content.list[*])。
    /// 来源:config_talk[talk_id].content(JSON 串)→ 解析后每个 list 项 = {type, text, other}。
    /// </summary>
    public sealed class DialogueNodeVo
    {
        public int Type;       // DialogueTypeConst
        public string Text;    // 对话内容(动作节点时是按钮文案,如"完成任务")
        public string Other;   // 附加信息(老端 Split("|") 解析,本期最小化不拆)

        public DialogueNodeVo(int type, string text, string other)
        {
            Type = type;
            Text = text ?? "";
            Other = other ?? "";
        }
    }

    /// <summary>
    /// 12101 回包里的一条 NPC 关联任务(对标 ClientProtocol.json 12101.task_list 项)。
    /// task_state:0无 / 1可接 / 2接了未完成 / 3完成可提交 / 4有任务对话(对标 DialogueController.ts:261)。
    /// </summary>
    public sealed class DialogueTaskEntry
    {
        public int TaskId;
        public int TaskState;
        public string TaskName;
        public int TaskType;

        public DialogueTaskEntry(int taskId, int taskState, string taskName, int taskType)
        {
            TaskId = taskId;
            TaskState = taskState;
            TaskName = taskName ?? "";
            TaskType = taskType;
        }
    }
}
