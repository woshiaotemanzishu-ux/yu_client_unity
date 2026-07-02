using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Dungeon
{
    /// <summary>
    /// 通用副本(pt_610,老端 BaseDungeonController.ts/BaseDungeonModel.ts)数据层。
    /// 御魂本(config_dungeon.type=12,dun_id 12001~)走这套家族协议:61001 进入/61002 退出(已在
    /// AutoBrushController 注册,复用)/61003 结算推送(对标老端 61003"通用结算界面",非 61013——
    /// 61013 在老端源码里从未注册处理器,desc 写的是"结算界面加好友/邀请公会/积分展示"社交附加功能,
    /// 与 61003 result 字段"跟61003一致"仅注释关联;结算真正入口按 675/767 行 BaseDungeonController 实证为 61003)/
    /// 61020 副本状态(次数/进度)。
    /// </summary>
    public sealed class DungeonModel
    {
        public static readonly DungeonModel Instance = new DungeonModel();
        private DungeonModel() { }

        /// <summary>61020 dun_list 单项(字段名对照 ClientProtocol.json "61020" dun_list)。</summary>
        public sealed class DunState
        {
            public int DunId;
            public int DailyCount;
            public int WeeklyCount;
            public int PermanentCount;
            public int ResetCount;
            public int VipCount;
            public int AddCount;
            public bool IsSweep;
        }

        /// <summary>按 dun_type 分组的副本状态(61020 回包一次带一个 dun_type 的 dun_list)。</summary>
        public readonly Dictionary<int, List<DunState>> DunStatesByType = new Dictionary<int, List<DunState>>();

        /// <summary>61003 结算推送最近一次结果(对标老端 SetDungeonResultInfo);result==1 成功。</summary>
        public int LastSettleResult { get; private set; }

        /// <summary>结算奖励摘要(typeId 经 GoodsModel.GetMappingTypeId 映射前的原始 style/typeId,num 已合并 reward_list)。</summary>
        public readonly List<(int typeId, long num)> LastSettleRewards = new List<(int typeId, long num)>();

        /// <summary>当前所在副本 dun_id(0=不在副本)。61001 成功后置位,61002 退出/61003 结算失败后按需清。</summary>
        public int InDungeonId { get; private set; }

        public bool HasData { get; private set; }

        /// <summary>61001 进入成功回包套值(对标老端 on61001 error_code==1 分支)。</summary>
        public void Apply61001(int dunId)
        {
            InDungeonId = dunId;
            HasData = true;
        }

        /// <summary>61020 副本状态回包套值(对标老端 GetDungeonInfo/SetDungeonInfo,按 dun_type 整表替换)。</summary>
        public void Apply61020(int dunType, List<DunState> list)
        {
            DunStatesByType[dunType] = list ?? new List<DunState>();
            HasData = true;
        }

        /// <summary>61003 结算推送套值(对标老端 SetDungeonResultInfo)。rewards 由控制器按 reward_list 汇总传入。</summary>
        public void ApplySettle(int result, List<(int typeId, long num)> rewards)
        {
            LastSettleResult = result;
            LastSettleRewards.Clear();
            if (rewards != null) LastSettleRewards.AddRange(rewards);
            if (result != 1) InDungeonId = 0;   // 失败:老端随即弹失败面板,副本记录已不在该层,标记不在副本内(实际以 61002/场景切换为准)
            HasData = true;
        }

        public DunState GetState(int dunType, int dunId)
        {
            if (!DunStatesByType.TryGetValue(dunType, out List<DunState> list) || list == null) return null;
            return list.Find(s => s.DunId == dunId);
        }

        public void Clear()
        {
            DunStatesByType.Clear();
            LastSettleRewards.Clear();
            LastSettleResult = 0;
            InDungeonId = 0;
            HasData = false;
        }
    }

    /// <summary>
    /// config_dungeon 读取器(数字索引键,39 列;列序权威来源 yu_client cdn/resource/config/server/
    /// config_table_default.json 的 config_dungeon 字段名列表):
    ///   · "0"  = id(主键)
    ///   · "1"  = name(副本名)
    ///   · "2"  = type(副本大类;御魂本=12)
    ///   · "10" = scene_id(场景 id;12001~12003 实测=2005)
    ///   · "20" = condition(进入条件,erlang term 字符串,原样存不解析——用途留后接 ErlangParser)
    /// 表经 ClientConfigSync 从 yu_client cdn 同步进 Assets/GameRes/resource/config/server/config_dungeon.json。
    /// </summary>
    public static class DungeonConfigs
    {
        private static JObject _dungeon;

        public static bool IsLoaded => _dungeon != null;

        public static async Task EnsureLoaded()
        {
            if (_dungeon != null) return;
            string key = GameResPath.GetServerConfigPath("config_dungeon");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Dungeon", "missing config_dungeon: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                _dungeon = new JObject();
                return;
            }
            _dungeon = JObject.Parse(asset.text);
            ResManager.Release(asset);
            GameLog.Info("Dungeon", "config_dungeon={0}", _dungeon.Count);
        }

        /// <summary>副本名(列 1);缺表/缺项降级 "副本{id}"(标出而非臆造)。</summary>
        public static string GetName(int dunId)
        {
            if (_dungeon?[dunId.ToString()] is JObject obj)
            {
                string name = ReadString(obj, "1");
                if (!string.IsNullOrEmpty(name)) return name;
            }
            return "副本" + dunId;
        }

        /// <summary>副本大类(列 2);缺表/缺项返回 0。</summary>
        public static int GetType(int dunId)
        {
            if (_dungeon?[dunId.ToString()] is JObject obj) return ReadInt(obj, "2");
            return 0;
        }

        /// <summary>场景 id(列 10)。</summary>
        public static int GetSceneId(int dunId)
        {
            if (_dungeon?[dunId.ToString()] is JObject obj) return ReadInt(obj, "10");
            return 0;
        }

        /// <summary>进入条件(列 20,erlang term 字符串原样存;解析留后接 ErlangParser)。</summary>
        public static string GetCondition(int dunId)
        {
            if (_dungeon?[dunId.ToString()] is JObject obj) return ReadString(obj, "20");
            return "";
        }

        // —— 数字索引键读取小工具(字符串/数字混排容错,同 PartnerConfigs/GoodsModel)——
        private static int ReadInt(JObject obj, string key)
        {
            Newtonsoft.Json.Linq.JToken token = obj[key];
            if (token == null || token.Type == Newtonsoft.Json.Linq.JTokenType.Null) return 0;
            return token.Type == Newtonsoft.Json.Linq.JTokenType.Integer ? token.Value<int>()
                : int.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        private static string ReadString(JObject obj, string key)
        {
            Newtonsoft.Json.Linq.JToken token = obj[key];
            return token == null || token.Type == Newtonsoft.Json.Linq.JTokenType.Null ? "" : token.ToString();
        }
    }
}
