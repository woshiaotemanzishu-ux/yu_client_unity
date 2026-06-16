namespace Shenxiao.Module.Core.Tasks
{
    public sealed class TaskVo
    {
        public int TaskId;
        public int TaskTipsType;
        public string TaskTipsMsg;
        public int HasFinish;
        public int Id;
        public int NeedNum;
        public int NowNum;
        public int ShowNum;
        public int SceneId;
        public int SceneX;
        public int SceneY;
        public int NeedGuide;

        public int TaskType;
        public string TaskName = "";
        public string Desc = "";
        public string Tips = "";
        public int MainLineOrder;
        public int ShowFinish;
        public bool NewFinishFlag;

        public TaskVo(int taskId, int taskTipsType, string taskTipsMsg, int hasFinish, int id,
            int needNum, int nowNum, int showNum, int sceneId, int sceneX, int sceneY, int needGuide)
        {
            TaskId = taskId;
            TaskTipsType = taskTipsType;
            TaskTipsMsg = taskTipsMsg ?? "";
            HasFinish = hasFinish;
            Id = id;
            NeedNum = needNum;
            NowNum = nowNum;
            ShowNum = showNum;
            SceneId = sceneId;
            SceneX = sceneX;
            SceneY = sceneY;
            NeedGuide = needGuide;
        }

        public void ApplyConfig(TaskConfigs.TaskCfg cfg)
        {
            if (cfg == null) return;
            TaskType = cfg.Type;
            TaskName = cfg.Name ?? "";
            Desc = cfg.Desc ?? "";
            Tips = cfg.Tips ?? "";
            MainLineOrder = cfg.MainLineOrder;
            ShowFinish = cfg.ShowFinish;
        }
    }
}
