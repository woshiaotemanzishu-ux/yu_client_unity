namespace Shenxiao.Module.Core.Dialogue
{
    /// <summary>
    /// 对话节点类型(对标老端 DialogueModel.ts:6-37 DialogueTypeConst)。
    /// 节点 type 决定按钮形态与点击动作:NPC/ROLE 是纯文本(继续),TRIGGER/FINISH/... 是动作按钮(发协议)。
    /// 值不可改——来自服务端 config_talk 的 content.list[*].type 字段。
    /// </summary>
    public static class DialogueTypeConst
    {
        public const int NPC = 0;                 // NPC 对话内容
        public const int ROLE = 1;                // 角色(玩家)对话内容
        public const int YES = 2;                 // 确定
        public const int NO = 3;                  // 取消
        public const int FIGHT = 4;               // 发生战斗
        public const int TRIGGER = 5;             // 触发(接取)任务
        public const int FINISH = 6;              // 完成(提交)任务
        public const int TRIGGER_AND_FINISH = 7;  // 触发并完成任务(老端点击无动作)
        public const int TALK_EVENT = 8;          // 触发对话事件(发 30007)
        public const int TASK_AWARD = 38;         // 任务奖励
        public const int ENTER_DUNGEON = 41;      // 进入副本
        public const int LEAVE_DUNGEON = 42;      // 离开副本
        public const int ANSWER_NO = 43;          // 回答错误
        public const int FINISH_AND_TRIGGER = 82; // 完成并触发新任务(发 30004)
        public const int BANQUET = 172;           // 婚宴仪式

        /// <summary>该节点点击后是否会触发任务协议(接/交/对话事件),即"动作节点"。</summary>
        public static bool IsActionNode(int type)
            => type == TRIGGER || type == FINISH || type == FINISH_AND_TRIGGER || type == TALK_EVENT;
    }
}
