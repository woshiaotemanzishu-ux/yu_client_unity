using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Banquet
{
    /// <summary>
    /// 婚宴(自动循环 轮24 PB)配置读取器——13 张 config_wedding_* 表,均从 yu_client
    /// cdn/resource/config/server/ 原样拷入(与 MarriageConfigs/GuildActivityConfigs 同规格,ClientConfigSync
    /// 白名单见该文件注释)。本代理直接 python 解析源 json 实测条数/键形态(未抄侦察稿):
    ///   · config_wedding_info(3条,数字键=wedding_type)/config_wedding_time(12条,数字键=time_id,
    ///     begin_time·end_time 为 JSON 字符串 {"0":H,"1":M})/config_wedding_time_stage(3条,数字键=stage_id)——
    ///     预约(17250/17251)阶段流程,本轮 wire 不消费(服务端 17251 校验用 data_wedding:get_wedding_time_con
    ///     等服务端权威值,回包已含终值),数据层先落地供尾包 UI(选日期/时段面板)用。
    ///   · config_wedding_candies(2条,数字键=candies_id 8002003/8002004)/config_wedding_fires(2条,数字键=
    ///     fires_id 1/2)/config_wedding_table(3条,数字键=table_id 8002001/02/05)/config_wedding_aura
    ///     (1条,数字键=aura_id)——场景内采集/烟花/气氛值奖励表,17266/67/72/78 的 aura/reward 服务端已算好
    ///     随包下发,本表供尾包 UI 展示用(名称/图标/消耗预览)。
    ///   · config_wedding_guest_position(41条,数字键=id)/config_wedding_position(696条,数字键=pos_id)——
    ///     婚礼场景坐标点位,Unity 无婚礼场景,本轮只加载不消费。
    ///   · config_wedding_scene_exp_coef(27条,复合键"wedding_type@num1@num2")——经验系数表;
    ///     **config_wedding_scene_exp 实测 0 条**(空表,与 candies_id/fires_id 等有数据的表不同,老端
    ///     BanquetModel 也未引用该表名,scene_exp_coef 才是真正在用的经验系数源)。
    ///   · **config_wedding_card 实测 0 条**(空表——对应 pp_marriage.erl:1662 `%WeddingCardCon =
    ///     data_wedding:get_wedding_card_con(GoodsTypeId)` 整行注释,17251 从未读取该表,与死链证据一致)。
    ///   · **config_wedding_trouble_maker 实测 0 条**(空表,对应 killlist 17269/17274 捣蛋鬼死链——本代理
    ///     python 实测复核,与主控裁决2一致)。
    /// 以上三张空表仍照裁决"13 张全进白名单"加载(占位,同 config_constellation_evolution_rate 轮23 先例),
    /// 只登记 Count 供 CliVerify 配置计数断言,不建行访问器。
    /// </summary>
    public static class BanquetConfigs
    {
        private static JObject _info;
        private static JObject _time;
        private static JObject _timeStage;
        private static JObject _candies;
        private static JObject _fires;
        private static JObject _table;
        private static JObject _aura;
        private static JObject _guestPosition;
        private static JObject _position;
        private static JObject _sceneExpCoef;
        private static JObject _card;       // 空表(占位)
        private static JObject _sceneExp;   // 空表(占位)
        private static JObject _troubleMaker; // 空表(占位,killlist 17269/17274 死链佐证)

        public static bool IsLoaded => _info != null;

        public static async Task EnsureLoaded()
        {
            if (_info != null) return;
            _info = await LoadServer("config_wedding_info");
            _time = await LoadServer("config_wedding_time");
            _timeStage = await LoadServer("config_wedding_time_stage");
            _candies = await LoadServer("config_wedding_candies");
            _fires = await LoadServer("config_wedding_fires");
            _table = await LoadServer("config_wedding_table");
            _aura = await LoadServer("config_wedding_aura");
            _guestPosition = await LoadServer("config_wedding_guest_position");
            _position = await LoadServer("config_wedding_position");
            _sceneExpCoef = await LoadServer("config_wedding_scene_exp_coef");
            _card = await LoadServer("config_wedding_card");
            _sceneExp = await LoadServer("config_wedding_scene_exp");
            _troubleMaker = await LoadServer("config_wedding_trouble_maker");
            GameLog.Info("Banquet", "BanquetConfigs 加载: info={0} time={1} timeStage={2} candies={3} fires={4} table={5} aura={6} guestPos={7} pos={8} sceneExpCoef={9} card={10}(空) sceneExp={11}(空) troubleMaker={12}(空,死链)",
                _info.Count, _time.Count, _timeStage.Count, _candies.Count, _fires.Count, _table.Count, _aura.Count,
                _guestPosition.Count, _position.Count, _sceneExpCoef.Count, _card.Count, _sceneExp.Count, _troubleMaker.Count);
        }

        private static async Task<JObject> LoadServer(string cfg)
        {
            string key = GameResPath.GetServerConfigPath(cfg);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Banquet", "缺配表: {0}(跑「神霄/配表/同步客户端配置」或手动拷贝)", key);
                return new JObject();
            }
            JObject obj = JObject.Parse(asset.text);
            ResManager.Release(asset);
            return obj;
        }

        // ---------- config_wedding_info(婚礼类型主表,数字键=wedding_type) ----------

        public sealed class InfoRow
        {
            public int Type;
            public string Name = "";
            public string WeddingName = "";
            public string CostRaw = "[]";
            public string RewardRaw = "[]";
            public string FailReturnRaw = "[]";
            public int DesignationId;
            public int GuestNum;
            public int GuestNumMax;
            public int Time;
            public int ExpTime;
            public int ExpCoefficient;
            public string Explain = "";
        }

        public static InfoRow GetInfo(int weddingType)
        {
            if (!(_info?[weddingType.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new InfoRow
            {
                Type = weddingType, Name = ReadString(row, "name"), WeddingName = ReadString(row, "wedding_name"),
                CostRaw = ReadString(row, "cost"), RewardRaw = ReadString(row, "reward"),
                FailReturnRaw = ReadString(row, "wedding_fail_return"), DesignationId = ReadInt(row, "designation_id"),
                GuestNum = ReadInt(row, "guest_num"), GuestNumMax = ReadInt(row, "guest_num_max"),
                Time = ReadInt(row, "time"), ExpTime = ReadInt(row, "exp_time"),
                ExpCoefficient = ReadInt(row, "exp_coefficient"), Explain = ReadString(row, "explain"),
            };
        }

        // ---------- config_wedding_time(预约时段,数字键=time_id;begin/end_time 为内嵌 JSON 字符串) ----------

        public sealed class TimeRow { public int Id; public int BeginHour; public int BeginMinute; public int EndHour; public int EndMinute; }

        public static TimeRow GetTime(int timeId)
        {
            if (!(_time?[timeId.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            (int bh, int bm) = ParseHm(ReadString(row, "begin_time"));
            (int eh, int em) = ParseHm(ReadString(row, "end_time"));
            return new TimeRow { Id = timeId, BeginHour = bh, BeginMinute = bm, EndHour = eh, EndMinute = em };
        }

        /// <summary>解析 {"0":H,"1":M} 形态的内嵌 JSON 字符串字段(config_wedding_time 特有形态)。</summary>
        private static (int, int) ParseHm(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return (0, 0);
            try
            {
                JObject o = JObject.Parse(raw);
                return (ReadInt(o, "0"), ReadInt(o, "1"));
            }
            catch
            {
                return (0, 0);
            }
        }

        // ---------- config_wedding_time_stage(预约阶段流程,数字键=stage_id) ----------

        public sealed class TimeStageRow { public int Id; public string Name = ""; public int ContinueTime; public string Test = ""; }

        public static TimeStageRow GetTimeStage(int stageId)
        {
            if (!(_timeStage?[stageId.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new TimeStageRow
            {
                Id = stageId, Name = ReadString(row, "stage_name"), ContinueTime = ReadInt(row, "continue_time"), Test = ReadString(row, "test"),
            };
        }

        // ---------- config_wedding_candies(喜糖,数字键=candies_id) ----------

        public sealed class CandyRow
        {
            public int Id;
            public string Name = "";
            public string CostRaw = "[]";
            public int FreeTimes;
            public int Num;
            public string RewardRaw = "[]";
            public int Aura;
            public int LimitNum;
        }

        public static CandyRow GetCandy(int candiesId)
        {
            if (!(_candies?[candiesId.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new CandyRow
            {
                Id = candiesId, Name = ReadString(row, "candies_name"), CostRaw = ReadString(row, "cost_list"),
                FreeTimes = ReadInt(row, "free_times"), Num = ReadInt(row, "candies_num"),
                RewardRaw = ReadString(row, "reward_list"), Aura = ReadInt(row, "aura"), LimitNum = ReadInt(row, "limit_num"),
            };
        }

        // ---------- config_wedding_fires(烟花,数字键=fires_id) ----------

        public sealed class FiresRow
        {
            public int Id;
            public string Name = "";
            public string CostRaw = "[]";
            public string Charact = "";
            public int FreeTimes;
            public string RewardRaw = "[]";
            public int Aura;
        }

        public static FiresRow GetFires(int firesId)
        {
            if (!(_fires?[firesId.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new FiresRow
            {
                Id = firesId, Name = ReadString(row, "fires_name"), CostRaw = ReadString(row, "cost_list"),
                Charact = ReadString(row, "charact"), FreeTimes = ReadInt(row, "free_times"),
                RewardRaw = ReadString(row, "reward_list"), Aura = ReadInt(row, "aura"),
            };
        }

        // ---------- config_wedding_table(餐桌菜品,数字键=table_id) ----------

        public sealed class TableRow
        {
            public int Id;
            public string Name = "";
            public int WeddingType;
            public int Num;
            public string RewardRaw = "[]";
            public int Aura;
        }

        public static TableRow GetTable(int tableId)
        {
            if (!(_table?[tableId.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new TableRow
            {
                Id = tableId, Name = ReadString(row, "table_name"), WeddingType = ReadInt(row, "wedding_type"),
                Num = ReadInt(row, "table_num"), RewardRaw = ReadString(row, "reward_list"), Aura = ReadInt(row, "aura"),
            };
        }

        // ---------- config_wedding_aura(气氛值奖励档位,数字键=aura_id) ----------

        public sealed class AuraRow { public int Id; public int AuraNum; public string RewardRaw = "[]"; }

        public static AuraRow GetAura(int auraId)
        {
            if (!(_aura?[auraId.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new AuraRow { Id = auraId, AuraNum = ReadInt(row, "aura_num"), RewardRaw = ReadString(row, "reward_list") };
        }

        // ---------- config_wedding_guest_position(宾客坐标点位,数字键=id) ----------

        public sealed class GuestPositionRow { public int Id; public int X; public int Y; public int Angle; }

        public static GuestPositionRow GetGuestPosition(int id)
        {
            if (!(_guestPosition?[id.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new GuestPositionRow { Id = id, X = ReadInt(row, "x"), Y = ReadInt(row, "y"), Angle = ReadInt(row, "angle") };
        }

        // ---------- config_wedding_position(场景坐标点位,数字键=pos_id) ----------

        public sealed class PositionRow { public int PosId; public int Type; public int X; public int Y; }

        public static PositionRow GetPosition(int posId)
        {
            if (!(_position?[posId.ToString(CultureInfo.InvariantCulture)] is JObject row)) return null;
            return new PositionRow { PosId = posId, Type = ReadInt(row, "type"), X = ReadInt(row, "x"), Y = ReadInt(row, "y") };
        }

        // ---------- config_wedding_scene_exp_coef(经验系数,复合键"wedding_type@num1@num2") ----------

        public sealed class SceneExpCoefRow { public int WeddingType; public int Num1; public int Num2; public int AuraNum; public int ExpCoef; }

        public static SceneExpCoefRow GetSceneExpCoef(int weddingType, int num1, int num2)
        {
            string key = weddingType.ToString(CultureInfo.InvariantCulture) + "@" + num1.ToString(CultureInfo.InvariantCulture)
                + "@" + num2.ToString(CultureInfo.InvariantCulture);
            if (!(_sceneExpCoef?[key] is JObject row)) return null;
            return new SceneExpCoefRow
            {
                WeddingType = weddingType, Num1 = num1, Num2 = num2,
                AuraNum = ReadInt(row, "aura_num"), ExpCoef = ReadInt(row, "exp_coef"),
            };
        }

        public static int InfoCount => _info?.Count ?? 0;
        public static int TimeCount => _time?.Count ?? 0;
        public static int TimeStageCount => _timeStage?.Count ?? 0;
        public static int CandiesCount => _candies?.Count ?? 0;
        public static int FiresCount => _fires?.Count ?? 0;
        public static int TableCount => _table?.Count ?? 0;
        public static int AuraCount => _aura?.Count ?? 0;
        public static int GuestPositionCount => _guestPosition?.Count ?? 0;
        public static int PositionCount => _position?.Count ?? 0;
        public static int SceneExpCoefCount => _sceneExpCoef?.Count ?? 0;
        public static int CardCount => _card?.Count ?? 0;               // 恒 0(空表)
        public static int SceneExpCount => _sceneExp?.Count ?? 0;       // 恒 0(空表)
        public static int TroubleMakerCount => _troubleMaker?.Count ?? 0; // 恒 0(空表,死链)

        // ---------- JSON 读取小工具(同 MarriageConfigs/GuildActivityConfigs 套路,自成一份不跨模块耦合) ----------

        private static int ReadInt(JObject obj, string key)
        {
            if (obj == null) return 0;
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            if (token.Type == JTokenType.Integer) return token.Value<int>();
            if (token.Type == JTokenType.Float) return (int)token.Value<double>();
            return int.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        private static string ReadString(JObject obj, string key)
        {
            if (obj == null) return "";
            JToken token = obj[key];
            return token == null || token.Type == JTokenType.Null ? "" : token.ToString();
        }
    }
}
