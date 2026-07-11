namespace Shenxiao.Module.Core.Festival
{
    /// <summary>
    /// 祭典(宝录)数据(对标老客户端 FestivalModel)。只承载 19401 下发的宝录基础信息,
    /// 供主界面图标(223)显隐判定用。uid>0 表示本次有宝录活动开启(GetEntranceOpenState)。
    /// 面板/任务/奖励等 UI 待用户验收,本期只做图标。
    /// </summary>
    public sealed class FestivalModel
    {
        public static readonly FestivalModel Instance = new FestivalModel();
        private FestivalModel() { }

        /// <summary>主界面图标类型(对标老端 FestivalModel.ICON_TYPE=223)。</summary>
        public const string ICON_TYPE = "223";

        // 19401 宝录基础信息(对标老端 IS2CFestivalBasicInfo)
        public int Uid;         // 宝录唯一id(>0 表示有活动开启)
        public int ActId;       // 活动id
        public int Type;        // 宝录类型 0 普通版 1 豪华版 2 至尊版
        public int Lv;          // 当前等级
        public int Exp;         // 当前累计经验
        public int ExpiredTime; // 过期时间戳

        public void SetBasicInfo(int uid, int actId, int type, int lv, int exp, int expiredTime)
        {
            Uid = uid;
            ActId = actId;
            Type = type;
            Lv = lv;
            Exp = exp;
            ExpiredTime = expiredTime;
        }

        /// <summary>
        /// 入口开启状态(对标老端 GetEntranceOpenState():_festivalBasicInfo?.uid > 0)。
        /// 服务端仅在满足等级(120级开启宝录)/活动时间窗时才下发 uid>0,门槛由服务端把控,客户端只看 uid。
        /// </summary>
        public bool GetEntranceOpenState()
        {
            return Uid > 0;
        }

        public void Reset()
        {
            Uid = 0;
            ActId = 0;
            Type = 0;
            Lv = 0;
            Exp = 0;
            ExpiredTime = 0;
        }
    }
}
