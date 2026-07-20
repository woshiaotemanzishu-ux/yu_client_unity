using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.SuitCollect
{
    /// <summary>
    /// 套装收集数据层(对标老端 commonModel/SuitActivityModel.ts)。主线卡点 #42:task 100391(ctype 84)
    /// 要求套装1(suit_id=1)的 cur_stage 达到 4(server data_task.erl:1652 按 {suit_clt,[{SuitId,CurStage}]} 匹配)。
    /// 15256 全量落此(clt_list 逐套装 + 当前穿戴时装 suit_id);15257 激活单个套装阶段成功后套值。
    /// </summary>
    public sealed class SuitCollectModel
    {
        public static readonly SuitCollectModel Instance = new SuitCollectModel();
        private SuitCollectModel() { }

        /// <summary>单个套装收集进度(字段名对照 ClientProtocol.json "15256" clt_list 单项)。</summary>
        public sealed class SuitVo
        {
            public int SuitId;
            public int CurStage;
            public List<int> PosList;   // cur_pos_list(equip_type 列表,已点亮的部位)
        }

        private readonly Dictionary<int, SuitVo> _suits = new Dictionary<int, SuitVo>();

        /// <summary>当前穿戴时装套装 id(15256 回包末尾 suit_id,对标老端 SetSuitData 尾字段)。</summary>
        public int FashionSuitId;

        public bool HasData { get; private set; }

        public IReadOnlyDictionary<int, SuitVo> Suits => _suits;

        public SuitVo Get(int suitId)
        {
            _suits.TryGetValue(suitId, out SuitVo vo);
            return vo;
        }

        /// <summary>当前阶段(未落数据/未收集过该套装 → 0,对标老端 GetCurStage 缺省 0)。</summary>
        public int GetCurStage(int suitId)
        {
            SuitVo vo = Get(suitId);
            return vo?.CurStage ?? 0;
        }

        /// <summary>15256 全量(对标 SetSuitData:清空重建 + 尾字段当前时装)。</summary>
        public void Apply15256(List<SuitVo> list, int fashionSuitId)
        {
            _suits.Clear();
            if (list != null)
            {
                foreach (SuitVo vo in list)
                {
                    if (vo == null) continue;
                    _suits[vo.SuitId] = vo;
                }
            }
            FashionSuitId = fashionSuitId;
            HasData = true;
        }

        /// <summary>15257 激活成功(对标 AddSuitData):套 cur_stage/cur_pos_list,没有则新增。</summary>
        public void Apply15257(int suitId, int curStage, List<int> posList)
        {
            SuitVo vo = Get(suitId);
            if (vo == null)
            {
                vo = new SuitVo { SuitId = suitId };
                _suits[suitId] = vo;
            }
            vo.CurStage = curStage;
            vo.PosList = posList;
        }

        /// <summary>15258 增量点亮：只合并部位并去重，不覆盖阶段或其它套装。</summary>
        public void MergeLitPositions(List<(int suitId, int equipType)> list)
        {
            if (list == null) return;
            foreach ((int suitId, int equipType) in list)
            {
                SuitVo vo = Get(suitId);
                if (vo == null)
                {
                    vo = new SuitVo { SuitId = suitId, PosList = new List<int>() };
                    _suits[suitId] = vo;
                }
                vo.PosList ??= new List<int>();
                if (!vo.PosList.Contains(equipType)) vo.PosList.Add(equipType);
            }
        }

        public void ApplyFashionWear(int suitId, bool isWear)
        {
            FashionSuitId = suitId == 0 || !isWear ? 0 : suitId;
        }

        public void Clear()
        {
            _suits.Clear();
            FashionSuitId = 0;
            HasData = false;
        }
    }

    /// <summary>
    /// config_suit_clt 读取器(⚠不在 config_table_default 列序表内,主键 "{suit_id}@{career}" 具名键直读:
    /// suit_id/career/name/open_lv/open_turn/show_equip/stage/star/color…)。表经 ClientConfigSync 从 yu_client cdn 同步。
    /// </summary>
    public static class SuitCollectConfigs
    {
        private static JObject _suitClt;

        public static bool IsLoaded => _suitClt != null;

        public static async Task EnsureLoaded()
        {
            if (_suitClt != null) return;
            string key = GameResPath.GetServerConfigPath("config_suit_clt");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("SuitCollect", "missing config_suit_clt: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                _suitClt = new JObject();
                return;
            }
            _suitClt = JObject.Parse(asset.text);
            ResManager.Release(asset);
            GameLog.Info("SuitCollect", "config_suit_clt={0}", _suitClt.Count);
        }

        /// <summary>套装名(具名键 "{suit_id}@{career}" 的 name);缺表/缺项降级 "套装{id}"(标出而非臆造)。</summary>
        public static string GetName(int suitId, int career)
        {
            if (_suitClt?[suitId + "@" + career] is JObject obj)
            {
                string name = obj.Value<string>("name");
                if (!string.IsNullOrEmpty(name)) return name;
            }
            return "套装" + suitId;
        }

        /// <summary>开放等级(具名键 open_lv);缺表/缺项 -1(标出而非臆造)。</summary>
        public static int GetOpenLv(int suitId, int career)
        {
            if (_suitClt?[suitId + "@" + career] is JObject obj)
            {
                return obj.Value<int?>("open_lv") ?? -1;
            }
            return -1;
        }

        /// <summary>满阶数(具名键 stage);缺表/缺项 -1(标出而非臆造)。</summary>
        public static int GetStage(int suitId, int career)
        {
            if (_suitClt?[suitId + "@" + career] is JObject obj)
            {
                return obj.Value<int?>("stage") ?? -1;
            }
            return -1;
        }

        /// <summary>是否为配置表中存在的套装 id（任一职业行存在即有效）。</summary>
        public static bool IsKnownSuit(int suitId)
        {
            if (suitId <= 0 || _suitClt == null) return false;
            string prefix = suitId + "@";
            foreach (JProperty property in _suitClt.Properties())
            {
                if (property.Name.StartsWith(prefix, System.StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }
}
