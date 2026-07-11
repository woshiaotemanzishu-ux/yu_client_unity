using System.Collections.Generic;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.GrowthBenefits
{
    /// <summary>
    /// 成长福利数据(对标老客户端 GrowthBenefitsModel)。承载 41720/41721 下发的成长任务领取态,
    /// 供主界面图标(41720)显隐判定用。入口开启条件(对标老端 GetEntranceOpenState):
    /// 主角等级 ≥ 成长福利开启等级(config_welfare_cfg["8"].val=grow_welfare_open_lv=135)且任务未全部领取。
    /// 面板/任务列表/领取 UI 待用户验收,本期只做图标。
    /// TODO: 战力福利(GrowthForce)那半个 or 分支(41723/41724)本期未移植,见 GetEntranceOpenState。
    /// </summary>
    public sealed class GrowthBenefitsModel
    {
        public static readonly GrowthBenefitsModel Instance = new GrowthBenefitsModel();
        private GrowthBenefitsModel() { }

        /// <summary>主界面图标类型(对标老端 EnumGrowthBenefitsProtocol.eProtocol41720=41720)。</summary>
        public const string ICON_TYPE = "41720";

        // 成长任务领取态(对标老端 EnumGrowWelefareTaskState)
        public const int STATUS_CANT_RECEIVE = 0; // 不可领取
        public const int STATUS_CAN_RECEIVE = 1;  // 可领取
        public const int STATUS_RECEIVED = 2;     // 已领取

        /// <summary>
        /// 成长福利开启等级(对标老端 config_welfare_cfg["8"].val,key=grow_welfare_open_lv,线上=135)。
        /// 该配置为服务端预载配置(PRELOAD_SERVER_CONFIG.config_welfare_cfg),Unity 侧尚未导入该 json,
        /// 故此处以常量落定线上值;若策划改动开启等级,需同步此常量或后续接 config_welfare_cfg 读取。
        /// </summary>
        public const int OPEN_LEVEL = 135;

        /// <summary>
        /// 成长福利全量配置任务 id(对标老端 config_grow_welfare_info,线上共 36 条,task_id 连续 1..36,
        /// 分布 open_day 1~7)。服务端 41720 只下发"已启动(status!=0 或 process>0)"的任务,未启动的不发。
        /// 判"是否全领"必须以完整配置任务表为基准(缺下发态=未启动=未领),而非只看下发的那几条——
        /// 否则玩家把已下发任务全领、但配置里还有未启动任务时会被误判为全领 → 图标误隐(本次修复的根因)。
        /// Unity 侧尚未导入该 server config json,此处以常量落定线上任务 id 集合(同 <see cref="OPEN_LEVEL"/> 落定做法);
        /// 若策划增删成长任务,需同步此集合或后续接 config_grow_welfare_info 读取。
        /// </summary>
        private static readonly int[] ALL_TASK_IDS =
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18,
            19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36,
        };

        // 41720/41721 下发的任务领取态:task_id -> status(process 面板进度用,本期图标不需,解析时读掉即可)。
        private readonly Dictionary<int, int> _taskStatus = new Dictionary<int, int>();

        /// <summary>整表覆盖(41720 登录/换天全量下发):先清空再填。</summary>
        public void SetTaskList(IReadOnlyList<KeyValuePair<int, int>> tasks)
        {
            _taskStatus.Clear();
            if (tasks == null) return;
            for (int i = 0; i < tasks.Count; i++) _taskStatus[tasks[i].Key] = tasks[i].Value;
        }

        /// <summary>增量合并(41721 服务端推送任务进度变更):按 task_id 覆盖单条。</summary>
        public void UpsertTasks(IReadOnlyList<KeyValuePair<int, int>> tasks)
        {
            if (tasks == null) return;
            for (int i = 0; i < tasks.Count; i++) _taskStatus[tasks[i].Key] = tasks[i].Value;
        }

        /// <summary>
        /// 任务是否已全部领取(对标老端 GetTaskIsAllReceive:遍历全量配置任务表 config_grow_welfare_info,
        /// 逐条取服务端下发态——任一"无下发态(未启动)"或"下发态非已领取"即返回 false)。
        /// 服务端只下发已启动任务,故配置里缺下发态的任务=未启动=未领取 → 倾向显图标,与老端完全一致。
        /// (旧实现只遍历下发列表,会把"已下发全领但仍有未启动配置任务"误判为全领 → 图标误隐,已修。)
        /// </summary>
        public bool GetTaskIsAllReceive()
        {
            for (int i = 0; i < ALL_TASK_IDS.Length; i++)
            {
                if (!_taskStatus.TryGetValue(ALL_TASK_IDS[i], out int status)) return false; // 配置有、服务端没发 = 未启动 → 未全领
                if (status != STATUS_RECEIVED) return false;
            }
            return true;
        }

        /// <summary>
        /// 入口开启状态(对标老端 GetEntranceOpenState)。
        /// 成长福利分支:主角等级 ≥ OPEN_LEVEL(config_welfare_cfg["8"]=135)且任务未全部领取。
        /// TODO: 老端还有一支战力福利分支(GrowthForceModel.CheckFightWelfareOpen() 且未全领,协议 41723/41724),
        /// 本期未移植该系统,故此处仅实现成长福利这一支;补齐战力福利后在此 OR 上其开启条件。
        /// </summary>
        public bool GetEntranceOpenState()
        {
            RoleModel role = RoleModel.Instance;
            int level = role.HasBaseInfo ? role.Level : 0;
            if (level >= OPEN_LEVEL && !GetTaskIsAllReceive()) return true;

            // TODO(战力福利): if (GrowthForceModel.CheckFightWelfareOpen() && !GrowthForceModel.IsFightWelfareGotAll()) return true;
            return false;
        }

        public void Reset()
        {
            _taskStatus.Clear();
        }
    }
}
