using System.Collections.Generic;

namespace Shenxiao.Module.Core.Festival
{
    /// <summary>
    /// 祭典(宝录)数据(对标老客户端 FestivalModel)。19401 承载宝录基础信息,供主界面图标(223)显隐
    /// 判定用(uid>0 表示本次有宝录活动开启,GetEntranceOpenState)。自动循环 轮18 便宜活批 PK3 扩展:
    /// 补 19402 领奖结果/19403 任务列表(二层嵌套)/19404 任务经验领取结果落地容器。面板 UI 待用户验收,
    /// 本轮只做数据层。
    /// </summary>
    public sealed class FestivalModel
    {
        public static readonly FestivalModel Instance = new FestivalModel();
        private FestivalModel() { }

        /// <summary>主界面图标类型(对标老端 FestivalModel.ICON_TYPE=223)。</summary>
        public const string ICON_TYPE = "223";

        // 19401 宝录基础信息(对标老端 IS2CFestivalBasicInfo)。
        // ⚠wire 首字段名 "Uid"(pt_194.erl:37)实为 FiestaId(命名笔误,r18_server_fiesta.md 待深挖①已订正),
        // 功能无影响,此处沿用字段名 Uid 但按真实语义使用(>0 表示本次开启的祭典 id)。
        public int Uid;         // 宝录唯一id(>0 表示有活动开启;实为 FiestaId)
        public int ActId;       // 活动id
        public int Type;        // 宝录类型 0 普通版 1 豪华版 2 至尊版
        public int Lv;          // 当前等级
        public int Exp;         // 当前累计经验
        public int ExpiredTime; // 过期时间戳

        /// <summary>19401 RewardList 单条(pt_194.erl item_to_bin_0,102-112行:Lv:16,Status1:8[普通档领取态],
        /// Status2:8[高阶档领取态])。</summary>
        public struct LevelRewardState
        {
            public int Lv;
            public int Status1;
            public int Status2;
        }

        public readonly List<LevelRewardState> RewardList = new List<LevelRewardState>();

        public void SetBasicInfo(int uid, int actId, int type, int lv, int exp, int expiredTime, List<LevelRewardState> rewardList)
        {
            Uid = uid;
            ActId = actId;
            Type = type;
            Lv = lv;
            Exp = exp;
            ExpiredTime = expiredTime;
            RewardList.Clear();
            if (rewardList != null) RewardList.AddRange(rewardList);
        }

        /// <summary>
        /// 入口开启状态(对标老端 GetEntranceOpenState():_festivalBasicInfo?.uid > 0)。
        /// 服务端仅在满足等级(120级开启宝录)/活动时间窗时才下发 uid>0,门槛由服务端把控,客户端只看 uid。
        /// </summary>
        public bool GetEntranceOpenState()
        {
            return Uid > 0;
        }

        // ---- object_list 三元组(pt:write_object_list 通用形态,u16 计数 + {Type:8,ObjectTypeId:32,Num:32}) ----
        public struct RewardObj
        {
            public int Type;
            public int ObjectTypeId;
            public int Num;
        }

        // 19402 领取等级奖励结果(瞬时,只留最近一条;pt_194.erl 无独立 Code 字段——非空 RewardList 即成功,对齐老端读法)。
        public readonly List<RewardObj> LastLevelAwardReward = new List<RewardObj>();
        public bool LastLevelAwardSuccess;

        public void SetLevelAwardResult(List<RewardObj> rewardList)
        {
            LastLevelAwardReward.Clear();
            if (rewardList != null) LastLevelAwardReward.AddRange(rewardList);
            LastLevelAwardSuccess = LastLevelAwardReward.Count > 0;
        }

        /// <summary>19403 任务单条(pt_194.erl item_to_bin_2,131-143行:TaskId:16,FinishTimes:8,CurNum:32,Status:8)。</summary>
        public sealed class TaskEntry
        {
            public int TaskId;
            public int FinishTimes;
            public int CurNum;
            public int Status;
        }

        /// <summary>19403 分组(pt_194.erl item_to_bin_1,113-130行:Type:8[1日/2周/3赛季],TaskList 二层嵌套,RefreshTime:32)。</summary>
        public sealed class TaskTypeGroup
        {
            public int Type;
            public readonly List<TaskEntry> TaskList = new List<TaskEntry>();
            public int RefreshTime;
        }

        /// <summary>key=Type(1日/2周/3赛季)。</summary>
        public readonly Dictionary<int, TaskTypeGroup> TaskGroupsByType = new Dictionary<int, TaskTypeGroup>();

        public void SetTaskGroup(int type, List<TaskEntry> taskList, int refreshTime)
        {
            var g = new TaskTypeGroup { Type = type, RefreshTime = refreshTime };
            if (taskList != null) g.TaskList.AddRange(taskList);
            TaskGroupsByType[type] = g;
        }

        // 19404 任务经验领取结果(瞬时;pt_194.erl 回包只有 Exp:32,无 Code)。
        public int LastTaskExpClaimed;

        public void Reset()
        {
            Uid = 0;
            ActId = 0;
            Type = 0;
            Lv = 0;
            Exp = 0;
            ExpiredTime = 0;
            RewardList.Clear();

            LastLevelAwardReward.Clear();
            LastLevelAwardSuccess = false;

            TaskGroupsByType.Clear();

            LastTaskExpClaimed = 0;
        }
    }
}
