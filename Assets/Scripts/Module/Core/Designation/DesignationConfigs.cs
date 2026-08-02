using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Designation
{
    /// <summary>称号基础配置 config_dsgt。</summary>
    public static class DesignationConfigs
    {
        public sealed class Attr
        {
            public int Id;
            public long Value;
        }

        public sealed class Cost
        {
            /// <summary>服务端 ObjectList 类型；称号道具激活只接受 0=背包物品。</summary>
            public int Type;
            public int TypeId;
            public long Num;
        }

        public sealed class Row
        {
            public uint Id;
            public string Name = "";
            public string Description = "";
            public string ResourceId = "";
            public int MainType;
            public int Location;
            public int OrderLimit;
            public readonly List<Attr> Attrs = new List<Attr>();
            public readonly List<Cost> GoodsConsume = new List<Cost>();
        }

        public sealed class OrderRow
        {
            public uint Id;
            public int Order;
            public readonly List<Attr> Attrs = new List<Attr>();
            public readonly List<Cost> Consume = new List<Cost>();
        }

        private static readonly List<Row> Rows = new List<Row>();
        private static readonly Dictionary<string, OrderRow> OrderRows = new Dictionary<string, OrderRow>();
        private static JObject _config;
        private static JObject _orderConfig;
        private static Task _loading;

        public static IReadOnlyList<Row> All => Rows;

        public static Task EnsureLoaded()
        {
            if (_config != null && _orderConfig != null) return Task.CompletedTask;
            return _loading ?? (_loading = LoadAsync());
        }

        public static Row Get(uint id)
        {
            for (int i = 0; i < Rows.Count; i++)
                if (Rows[i].Id == id) return Rows[i];
            return null;
        }

        /// <summary>
        /// 对齐服务端 lib_designation:active_designation/2：41109 只接受一条物品消耗，配置不完整时不得发包。
        /// </summary>
        public static bool TryGetActivationCost(uint id, out Cost cost)
        {
            Row row = Get(id);
            if (row != null && row.GoodsConsume.Count == 1)
            {
                Cost value = row.GoodsConsume[0];
                if (value.Type == 0 && value.TypeId > 0 && value.Num > 0)
                {
                    cost = value;
                    return true;
                }
            }
            cost = null;
            return false;
        }

        /// <summary>
        /// 对齐服务端 lib_designation:check_up_designation_order/2：当前阶与下一阶都必须有配置，
        /// 实际扣除当前阶配置的 consume；这里只接受一条 type=0 的真实背包物品。
        /// </summary>
        public static bool TryGetUpgradeCost(uint id, int currentOrder, out Cost cost)
        {
            Row row = Get(id);
            if (row == null || row.MainType != 3 || currentOrder <= 0
                || row.OrderLimit <= currentOrder
                || !OrderRows.TryGetValue(OrderKey(id, currentOrder), out OrderRow current)
                || !OrderRows.ContainsKey(OrderKey(id, currentOrder + 1))
                || current.Consume.Count != 1)
            {
                cost = null;
                return false;
            }

            Cost value = current.Consume[0];
            if (value.Type != 0 || value.TypeId <= 0 || value.Num <= 0)
            {
                cost = null;
                return false;
            }
            cost = value;
            return true;
        }

        public static IReadOnlyList<Attr> GetDisplayAttrs(uint id, int currentOrder)
        {
            Row row = Get(id);
            if (row != null && row.MainType == 3 && currentOrder > 0
                && OrderRows.TryGetValue(OrderKey(id, currentOrder), out OrderRow order)
                && order.Attrs.Count > 0)
                return order.Attrs;
            return row?.Attrs;
        }

        private static async Task LoadAsync()
        {
            string baseKey = GameResPath.GetServerConfigPath("config_dsgt");
            string orderKey = GameResPath.GetServerConfigPath("config_dsgt_order");
            UnityEngine.TextAsset baseAsset = null;
            UnityEngine.TextAsset orderAsset = null;
            try
            {
                Task<UnityEngine.TextAsset> baseTask = ResManager.LoadAsync<UnityEngine.TextAsset>(baseKey);
                Task<UnityEngine.TextAsset> orderTask = ResManager.LoadAsync<UnityEngine.TextAsset>(orderKey);
                await Task.WhenAll(baseTask, orderTask);
                baseAsset = baseTask.Result;
                orderAsset = orderTask.Result;
                if (baseAsset == null || orderAsset == null)
                {
                    GameLog.Error("Designation", "称号配置缺失: base={0} order={1}",
                        baseAsset != null, orderAsset != null);
                    return;
                }

                JObject parsedBase = JObject.Parse(baseAsset.text);
                JObject parsedOrder = JObject.Parse(orderAsset.text);
                var rows = new List<Row>();
                var orderRows = new Dictionary<string, OrderRow>();
                foreach (JProperty property in parsedBase.Properties())
                {
                    if (!(property.Value is JObject row)) continue;
                    var parsed = new Row
                    {
                        Id = (uint)ReadLong(row, "id"),
                        Name = ReadString(row, "name"),
                        Description = ReadString(row, "description"),
                        ResourceId = ReadString(row, "resource_id"),
                        MainType = (int)ReadLong(row, "main_type"),
                        Location = (int)ReadLong(row, "location"),
                        OrderLimit = (int)ReadLong(row, "order_limit"),
                    };
                    ParseAttrs(ReadString(row, "attr_list"), parsed.Attrs);
                    ParseCosts(ReadString(row, "goods_consume"), parsed.GoodsConsume);
                    rows.Add(parsed);
                }
                foreach (JProperty property in parsedOrder.Properties())
                {
                    if (!(property.Value is JObject row)) continue;
                    var parsed = new OrderRow
                    {
                        Id = (uint)ReadLong(row, "0"),
                        Order = (int)ReadLong(row, "2"),
                    };
                    ParseCosts(ReadString(row, "1"), parsed.Consume);
                    ParseAttrs(ReadString(row, "3"), parsed.Attrs);
                    if (parsed.Id != 0 && parsed.Order > 0)
                        orderRows[OrderKey(parsed.Id, parsed.Order)] = parsed;
                }

                rows.Sort((a, b) => a.Location.CompareTo(b.Location));
                Rows.Clear();
                Rows.AddRange(rows);
                OrderRows.Clear();
                foreach (KeyValuePair<string, OrderRow> pair in orderRows)
                    OrderRows[pair.Key] = pair.Value;
                _config = parsedBase;
                _orderConfig = parsedOrder;
            }
            catch (System.Exception e)
            {
                GameLog.Error("Designation", "称号配置解析失败: {0}", e.Message);
            }
            finally
            {
                if (baseAsset != null) ResManager.Release(baseAsset);
                if (orderAsset != null) ResManager.Release(orderAsset);
                _loading = null;
            }
        }

        private static string OrderKey(uint id, int order) => id + "@" + order;

        private static void ParseAttrs(string raw, List<Attr> target)
        {
            if (string.IsNullOrEmpty(raw) || raw == "0" || raw == "[]") return;
            try
            {
                JArray list = JArray.Parse(raw);
                foreach (JToken token in list)
                {
                    if (!(token is JObject row)) continue;
                    target.Add(new Attr
                    {
                        Id = (int)(row["0"]?.Value<long>() ?? 0L),
                        Value = row["1"]?.Value<long>() ?? 0L,
                    });
                }
            }
            catch
            {
                // 单条坏配置不阻断整个称号页。
            }
        }

        private static void ParseCosts(string raw, List<Cost> target)
        {
            if (string.IsNullOrEmpty(raw) || raw == "0" || raw == "[]") return;
            try
            {
                JArray list = JArray.Parse(raw);
                foreach (JToken token in list)
                {
                    if (!(token is JObject row)) continue;
                    target.Add(new Cost
                    {
                        Type = (int)(row["0"]?.Value<long>() ?? 0L),
                        TypeId = (int)(row["1"]?.Value<long>() ?? 0L),
                        Num = row["2"]?.Value<long>() ?? 0L,
                    });
                }
            }
            catch
            {
                // 单条坏配置不能阻断整页加载；TryGetActivationCost 会让该称号保持不可操作。
            }
        }

        private static long ReadLong(JObject row, string key)
        {
            JToken token = row?[key];
            if (token == null || token.Type == JTokenType.Null) return 0L;
            if (token.Type == JTokenType.Integer) return token.Value<long>();
            return long.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out long value)
                ? value
                : 0L;
        }

        private static string ReadString(JObject row, string key)
            => row?[key]?.ToString() ?? string.Empty;
    }
}
