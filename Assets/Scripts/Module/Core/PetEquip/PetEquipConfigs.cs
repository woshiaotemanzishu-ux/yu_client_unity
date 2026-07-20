using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.PetEquip
{
    /// <summary>
    /// 侍魂装备五张服务端配置表。数字列序严格沿用 config_table_default，是否满级只由下一档配置是否存在决定。
    /// </summary>
    public static class PetEquipConfigs
    {
        private static JObject _position;
        private static JObject _positionLevel;
        private static JObject _stage;
        private static JObject _star;
        private static JObject _goods;
        private static Task _loading;

        public static bool IsLoaded => _position != null && _positionLevel != null && _stage != null && _star != null && _goods != null;
        public static int PositionCount => _position?.Count ?? 0;
        public static int PositionLevelCount => _positionLevel?.Count ?? 0;
        public static int StageCount => _stage?.Count ?? 0;
        public static int StarCount => _star?.Count ?? 0;
        public static int GoodsCount => _goods?.Count ?? 0;

        public static Task EnsureLoaded()
        {
            if (IsLoaded) return Task.CompletedTask;
            return _loading ?? (_loading = LoadCoreAsync());
        }

        private static async Task LoadCoreAsync()
        {
            _position = await LoadServerConfig("config_pet_equip_pos");
            _positionLevel = await LoadServerConfig("config_pet_equip_pos_lv");
            _stage = await LoadServerConfig("config_pet_equip_stage");
            _star = await LoadServerConfig("config_pet_equip_star");
            _goods = await LoadServerConfig("config_pet_equip_goods");
        }

        private static async Task<JObject> LoadServerConfig(string name)
        {
            string key = GameResPath.GetServerConfigPath(name);
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("PetEquip", "missing {0}: {1}(未同步?跑 神霄/配表/同步客户端配置)", name, key);
                return new JObject();
            }

            JObject table = JObject.Parse(asset.text);
            ResManager.Release(asset);
            GameLog.Info("PetEquip", "{0}={1}", name, table.Count);
            return table;
        }

        /// <summary>装备部位行，主键 type_id@pos，字段为具名列。</summary>
        public static JObject GetPosition(int typeId, int pos)
            => _position?[typeId + "@" + pos] as JObject;

        /// <summary>强化等级行，主键 type_id@pos@pos_lv；数字列 0/1/2/3/4。</summary>
        public static JObject GetPositionLevel(int typeId, int pos, int level)
            => _positionLevel?[typeId + "@" + pos + "@" + level] as JObject;

        /// <summary>进阶行，主键 type_id@pos@stage；数字列 0..7。</summary>
        public static JObject GetStage(int typeId, int pos, int stage)
            => _stage?[typeId + "@" + pos + "@" + stage] as JObject;

        /// <summary>升星行，主键 type_id@pos@star；数字列 0..5。</summary>
        public static JObject GetStar(int typeId, int pos, int star)
            => _star?[typeId + "@" + pos + "@" + star] as JObject;

        /// <summary>装备物品行，主键 goods_type_id，字段为具名列。</summary>
        public static JObject GetGoods(int goodsTypeId)
            => _goods?[goodsTypeId.ToString()] as JObject;

        public static bool HasNextPositionLevel(int typeId, int pos, int currentLevel)
            => GetPositionLevel(typeId, pos, currentLevel + 1) != null;

        public static bool HasNextStage(int typeId, int pos, int currentStage)
            => GetStage(typeId, pos, currentStage + 1) != null;

        public static bool HasNextStar(int typeId, int pos, int currentStar)
            => GetStar(typeId, pos, currentStar + 1) != null;
    }
}
