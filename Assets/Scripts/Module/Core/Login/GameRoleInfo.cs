using Shenxiao.Common.Proto;

namespace Shenxiao.Module.Core.Login
{
    /// <summary>10000 回包里的一条角色记录(选角页数据源)。</summary>
    public sealed class GameRoleInfo
    {
        public long roleId;
        public byte state;
        public byte rewardId;
        public FigureProto figure;

        public string DisplayName => figure != null && !string.IsNullOrEmpty(figure.name) ? figure.name : "角色" + roleId;
        public int Level => figure != null ? figure.level : 0;
        public int Career => figure != null ? figure.career : 0;
        public int Turn => figure != null ? figure.turn : 0;
    }
}
