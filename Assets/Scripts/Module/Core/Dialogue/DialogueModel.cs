namespace Shenxiao.Module.Core.Dialogue
{
    /// <summary>
    /// 对话数据层(对标老端 commonModel/DialogueModel.ts:40-133)。只存状态 + 提供配置访问 + DEFAULT_TALK,
    /// 不碰协议/UI(协议在 DialogueController,展示在 DialogueView)。
    /// </summary>
    public sealed class DialogueModel
    {
        public static readonly DialogueModel Instance = new DialogueModel();
        private DialogueModel() { }

        /// <summary>当前对话载荷(最近一次 12101/12102 装配)。</summary>
        public NpcDialogVo CurrentVo { get; set; }

        /// <summary>是否自动流程(接/交任务后自动推进下一段),对标 is_auto。</summary>
        public bool IsAuto { get; set; }

        /// <summary>切场景期间收到的对话回包要丢弃,对标 change_scene_close。</summary>
        public bool ChangeSceneClose { get; set; }

        /// <summary>本次 12101 是否客户端主动发起(只处理自己发起的回包),对标 scmd_12101_send。</summary>
        public bool Scmd12101Send { get; set; }

        /// <summary>对话框是否打开,对标 dialog_is_open(任务栏据此去重,不重复 DoTask)。</summary>
        public bool DialogIsOpen { get; set; }

        /// <summary>当前已打开对话的 NPC 实例 id(0 = 未打开)。</summary>
        public int CurrentNpcId { get; set; }

        /// <summary>NPC 默认对话 id(对标 GetDialogueId:config_npc[npc_id].talk)。</summary>
        public int GetDialogueId(int npcId) => NpcConfigs.GetDialogueId(npcId);

        /// <summary>按 talk_id 取对话配置(对标 GetTalkCfg:config_talk[talk_id])。</summary>
        public TalkConfigs.TalkCfg GetTalkCfg(int talkId) => TalkConfigs.Get(talkId);

        /// <summary>NPC 显示名(名牌/对话标题),来自 config_npc.name;缺则用 "NPC {id}" 占位。</summary>
        public string GetNpcName(int npcId)
        {
            NpcConfigs.NpcCfg cfg = NpcConfigs.Get(npcId);
            return cfg != null && !string.IsNullOrEmpty(cfg.Name) ? cfg.Name : "NPC " + npcId;
        }

        public void Reset()
        {
            CurrentVo = null;
            IsAuto = false;
            ChangeSceneClose = false;
            Scmd12101Send = false;
            DialogIsOpen = false;
            CurrentNpcId = 0;
        }

        // 默认对话(对标 DialogueModel.ts:47-62 DEFAULT_TALK):一段 NPC 旁白,talk_id 缺失时兜底。
        private static TalkConfigs.TalkCfg _defaultTalk;
        public static TalkConfigs.TalkCfg DefaultTalk
        {
            get
            {
                if (_defaultTalk == null)
                {
                    _defaultTalk = new TalkConfigs.TalkCfg { Id = 1, TotalContent = 1 };
                    var block = new TalkConfigs.TalkContentBlock { TotalList = 1 };
                    block.Nodes.Add(new DialogueNodeVo(DialogueTypeConst.NPC, "听说这里有好多好多的神话", ""));
                    _defaultTalk.Contents.Add(block);
                }
                return _defaultTalk;
            }
        }
    }
}
