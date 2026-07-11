using Shenxiao.Common.Proto;

namespace Shenxiao.Module.Core.Role
{
    /// <summary>
    /// 13013 查看他人 Figure 回包 VO(对标老端 GET_ROLE_FIGURE_INFO_RETURN scmd13013)。
    /// 通用"拉人物模型"通道,被排行榜/记录列表等多处功能复用,与自身角色面板无关。
    /// </summary>
    public sealed class RoleFigureInfo
    {
        public int ServerId;
        public int PlayerNum;
        public long PlayerId;
        /// <summary>调用方自定义的"来源标签"(非协议本身枚举),对标老端 module_id。</summary>
        public int ModuleId;
        public long Fighting;
        public FigureProto Figure;
        /// <summary>服务端字段名 ServerName,老端变量名 platform——同一尾串字段,不是两个字段。</summary>
        public string Platform;
    }
}
