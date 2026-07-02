using System.Collections.Generic;

namespace Shenxiao.Module.Core.Guild
{
    /// <summary>
    /// 结社加入数据层(对标老端 GuildModel.ts 结社列表段,服务端 pt_400)。
    /// 主线 101080(ctype14)=加入结社(guild_id>0 即判定,服务端 join_guild 事件推进,客户端不自算)。
    /// 40001 列表回包落 List;40004 创建 / 40003 一键申请成功(guild_id>0)置 HasGuild。
    /// </summary>
    public sealed class GuildJoinModel
    {
        public static readonly GuildJoinModel Instance = new GuildJoinModel();
        private GuildJoinModel() { }

        /// <summary>40001 结社列表单条(对标回包 guild_list[u16×{...}],工单范围仅取展示所需字段)。</summary>
        public sealed class GuildBrief
        {
            public long GuildId;
            public string Name;
            public int Lv;
            public int MemberNum;
            public int MemberCapacity;
            public bool IsApply;
        }

        private readonly List<GuildBrief> _list = new List<GuildBrief>();
        public IReadOnlyList<GuildBrief> List => _list;

        /// <summary>已有公会(40004 创建成功 或 40003 申请成功回包 guild_id>0 置位);不臆造,缺数据一律 false。</summary>
        public bool HasGuild { get; private set; }

        public bool HasData { get; private set; }

        /// <summary>40001 全量列表落值(清空重建)。</summary>
        public void SetList(List<GuildBrief> list)
        {
            _list.Clear();
            if (list != null) _list.AddRange(list);
            HasData = true;
        }

        /// <summary>40004/40003 回包 guild_id>0 时置位(对标服务端 common_join_guild 判定)。</summary>
        public void MarkHasGuild(bool has)
        {
            if (has) HasGuild = true;
        }

        public void Clear()
        {
            _list.Clear();
            HasGuild = false;
            HasData = false;
        }
    }
}
