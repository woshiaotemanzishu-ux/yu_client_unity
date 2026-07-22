using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Lung
{
    /// <summary>
    /// 神纹熔炉数据(对标老客户端 LungModel,模块 181)。神纹(龙纹)是大系统,本期只做主界面图标 181,
    /// 只承载 18105 下发的熔炉数据(stove_data)中与图标显隐相关的字段。GetStoveOpenState() 对标老端
    /// StoveOpenState(true):有炉数据、crucible_id!=0、且未过期(now<=end_time)时熔炉开启,图标显示。
    /// 神纹穿戴/升级/兑换/商店/红点等玩法协议(18100-18104/18106-18113)与面板均不移植。
    /// </summary>
    public sealed class LungModel
    {
        public static readonly LungModel Instance = new LungModel();
        private LungModel() { }

        /// <summary>主界面图标类型(对标老端 LungModel.LUNG_TOP_ICON="181",loc2 网格)。</summary>
        public const string ICON_TYPE = "181";

        // 18105 神纹熔炉数据(对标老端 stove_data),仅保留图标显隐所需字段
        public bool HasStoveData; // 是否已收到 18105
        public int CrucibleId;    // 当前熔炉id(0=未开炉)
        public long StartTime;    // 熔炉开始时间戳
        public long EndTime;      // 熔炉结束时间戳(now 超过即关炉)
        public int NextCrucibleId;
        public long NextStartTime;
        public bool HasOpenSchedule;

        public void SetStoveData(int crucibleId, long startTime, long endTime)
        {
            HasStoveData = true;
            CrucibleId = crucibleId;
            StartTime = startTime;
            EndTime = endTime;
        }

        public void ApplyOpenSchedule(int crucibleId, long startTime)
        {
            NextCrucibleId = crucibleId;
            NextStartTime = startTime;
            HasOpenSchedule = true;
        }

        /// <summary>
        /// 熔炉开启态(对标老端 LungModel.StoveOpenState(true)):
        /// !stove_data || crucible_id==0 || serverTime - end_time > 0 时关闭,否则开启。
        /// 老端 setStoveIcon 据此增删图标 181,并另叠 open_lv/open_day 门槛;Unity 侧 open_lv/open_day
        /// 由 ActivityIconManager 图标配置门(FunIsOpenByIconType)统一把控,故本方法只判熔炉本身状态。
        /// </summary>
        public bool GetStoveOpenState()
        {
            if (!HasStoveData || CrucibleId == 0) return false;
            if (TimeUtil.NowSec() - EndTime > 0) return false;
            return true;
        }

        public void Reset()
        {
            HasStoveData = false;
            CrucibleId = 0;
            StartTime = 0;
            EndTime = 0;
            NextCrucibleId = 0;
            NextStartTime = 0;
            HasOpenSchedule = false;
        }
    }
}
