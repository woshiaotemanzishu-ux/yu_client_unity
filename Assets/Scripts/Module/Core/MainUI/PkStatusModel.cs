namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// PK(战斗)模式静态表(对标老客户端 commonModel/PkStatusModel.ts:18 FightModeInfo,逐字段一致)。
    /// 下标即 pk_status(PK_STATUS:0和平/1全体/2强制/3跨服/4结社/5阵营/6海域)。
    /// 注意贴图映射不是顺号:全体(1)用 flag_2、强制(2)用 flag_1、阵营(5)用 flag_6、海域(6)用 flag_5,照抄老端。
    /// 主角当前模式在 RoleModel.PkStatus;切换协议见 <see cref="PkStatusController"/>。
    /// </summary>
    public static class PkStatusModel
    {
        // 对标老端 PK_STATUS 枚举(PkStatusModel.ts:3)。
        public const int Peace = 0;
        public const int All = 1;
        public const int Force = 2;
        public const int Server = 3;
        public const int Guild = 4;
        public const int Camp = 5;
        public const int Sea = 6;

        public static readonly FightModeInfoData[] FightModeInfo =
        {
            new FightModeInfoData { PkStatus = 0, StatusName = "和平", Desc = "不能攻击任何人", StatusIcon = "pkstatus_flag_0", MainIcon = "pkstatus_flag_0", BgIcon = "pkstatus_bg_0" },
            new FightModeInfoData { PkStatus = 1, StatusName = "全体", Desc = "可以攻击任何人", StatusIcon = "pkstatus_flag_2", MainIcon = "pkstatus_flag_2", BgIcon = "pkstatus_bg_2" },
            new FightModeInfoData { PkStatus = 2, StatusName = "强制", Desc = "不能攻击同结社和同队伍成员", StatusIcon = "pkstatus_flag_1", MainIcon = "pkstatus_flag_1", BgIcon = "pkstatus_bg_1" },
            new FightModeInfoData { PkStatus = 3, StatusName = "跨服", Desc = "不能攻击同服务器成员", StatusIcon = "pkstatus_flag_3", MainIcon = "pkstatus_flag_3", BgIcon = "pkstatus_bg_3" },
            new FightModeInfoData { PkStatus = 4, StatusName = "结社", Desc = "不能攻击同结社成员", StatusIcon = "pkstatus_flag_4", MainIcon = "pkstatus_flag_4", BgIcon = "pkstatus_bg_1" },
            new FightModeInfoData { PkStatus = 5, StatusName = "阵营", Desc = "不能攻击同阵营成员", StatusIcon = "pkstatus_flag_6", MainIcon = "pkstatus_flag_6", BgIcon = "pkstatus_bg_3" },
            new FightModeInfoData { PkStatus = 6, StatusName = "海域", Desc = "不能同海域成员", StatusIcon = "pkstatus_flag_5", MainIcon = "pkstatus_flag_5", BgIcon = "pkstatus_bg_1" },
        };

        public static FightModeInfoData Get(int pkStatus)
        {
            return pkStatus >= 0 && pkStatus < FightModeInfo.Length ? FightModeInfo[pkStatus] : null;
        }
    }
}
