using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.SeaHegemony
{
    /// <summary>
    /// 四海争霸(海域)数据(对标老客户端 SeaHegemonyModel,大系统只取图标相关部分)。
    /// 只承载驱动主界面图标 18601 显隐所需的两块信息:18600 的阵营(camp,判是否已报名)与
    /// 18625 的报名结束时间(end_time)。老端图标条件:活动结束前 24h 进入报名窗口
    /// (previewStartime = end_time - 86400 < 服务器时间 < end_time)时显示,文字据是否已报名区分。
    /// 砖块/攻防/阵营等玩法协议(18602-18624/18650-18715)一律不移植,本期只做图标。
    /// </summary>
    public sealed class SeaHegemonyModel
    {
        public static readonly SeaHegemonyModel Instance = new SeaHegemonyModel();
        private SeaHegemonyModel() { }

        /// <summary>主界面图标类型(对标老端 addIcon(18601),四海争霸,location_type=6)。</summary>
        public const string ICON_TYPE = "18601";

        // 报名窗口时长:活动结束前 86400 秒(24h)进入报名期(对标老端 previewStartime = end_time - 86400)。
        private const long SIGNUP_WINDOW_SEC = 86400;

        /// <summary>18600 所属阵营(camp!=0 表示已报名参战,对标老端 seaHegemonyData.camp / hasJoinSea)。</summary>
        public int Camp;

        /// <summary>是否已报名(对标老端 hasJoinSea = scmd.camp != 0)。</summary>
        public bool HasJoinSea => Camp != 0;

        /// <summary>18625 报名结束时间戳(服务器秒)。>0 且处于报名窗口内 → 显示图标(对标老端 scmd.end_time)。</summary>
        public long SignupEndTime;

        public void SetSeaInfo(int camp)
        {
            Camp = camp;
        }

        public void SetSignupEndTime(long endTime)
        {
            SignupEndTime = endTime;
        }

        /// <summary>
        /// 入口开启状态(对标老端 18625 handler:end_time 非零且 previewStartime < serverTime < end_time)。
        /// 服务端仅在活动开启且到达报名窗口时才下发有效 end_time,活动等级/开服天门槛由服务端把控;
        /// 客户端图标另受配置 open_lv=400/open_day=30 门(在控制器 AddIconAsync 里过一遍,与老端 addIcon 一致)。
        /// </summary>
        public bool GetEntranceOpenState()
        {
            if (SignupEndTime <= 0) return false;
            long now = TimeUtil.NowSec();
            long signupStart = SignupEndTime - SIGNUP_WINDOW_SEC;
            return signupStart < now && now < SignupEndTime;
        }

        /// <summary>图标文字(对标老端:camp!=0 → "已报名",否则 "报名中")。</summary>
        public string GetIconText()
        {
            return HasJoinSea ? "已报名" : "报名中";
        }

        public void Reset()
        {
            Camp = 0;
            SignupEndTime = 0;
        }
    }
}
