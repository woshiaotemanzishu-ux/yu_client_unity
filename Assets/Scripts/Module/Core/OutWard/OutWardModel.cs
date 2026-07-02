using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.OutWard
{
    /// <summary>
    /// 幻化外观数据层(对标老端 commonModel/OutWardModel.ts,协议段 pt_160)。坐骑/同修/翼影/圣器/神兵统一走本框架,
    /// 按 type_id 参数化(1=坐骑 2=剑魄同修 3/4/5=翼影/圣器/神兵)。主线链序:
    ///   100330(ctype23 id=1 need=2):坐骑(type_id=1) Stage&gt;1 或 (Stage==1 且 Star&gt;=2)
    ///   100521(ctype90 id=2 need=2):同修(type_id=2)系统B等级&gt;=2
    ///   100901(ctype90 id=1 need=2):坐骑(type_id=1)系统B等级&gt;=2
    /// 同一 type_id 身上两套并存的养成线(系统A阶星/系统B等级),字段全落一个 OutWardVo。
    /// </summary>
    public sealed class OutWardModel
    {
        public static readonly OutWardModel Instance = new OutWardModel();
        private OutWardModel() { }

        /// <summary>外观对象实例(系统A 阶/星/祝福 + 系统B 等级/经验/战力,字段名对照 Proto.cs OUTWARD_* 注释)。</summary>
        public sealed class OutWardVo
        {
            public int TypeId;

            // ---- 系统A:阶/星(16002/16023) ----
            public int Stage;
            public int Star;
            public long Blessing;
            public int FigureStage;
            public long Combat;
            public long Etime;
            public int AutoBuy;
            public List<(int attrId, long val)> Attrs;   // 16002 attr_list
            public List<int> Skills;                      // 16002 skill_list(skill_id)

            // ---- 系统B:等级/经验(16028/16029) ----
            public bool HasLv;
            public int Level;
            public long CurExp;
            public long LvCombat;
            public List<(int attrId, long val)> LvAttrs;              // 16028 attr_list
            public List<(int skillId, int skillLevel)> LvSkills;      // 16028 skill_list
        }

        private readonly Dictionary<int, OutWardVo> _map = new Dictionary<int, OutWardVo>();

        public OutWardVo Get(int typeId)
        {
            return _map.TryGetValue(typeId, out OutWardVo vo) ? vo : null;
        }

        private OutWardVo GetOrCreate(int typeId)
        {
            if (!_map.TryGetValue(typeId, out OutWardVo vo))
            {
                vo = new OutWardVo { TypeId = typeId };
                _map[typeId] = vo;
            }
            return vo;
        }

        /// <summary>16002 回包套值(系统A阶星全字段 + 附属 attr/skill)。</summary>
        public void Apply16002(int typeId, int stage, int star, long blessing, int figureStage, long combat,
            long etime, int autoBuy, List<(int attrId, long val)> attrs, List<int> skills)
        {
            OutWardVo vo = GetOrCreate(typeId);
            vo.Stage = stage;
            vo.Star = star;
            vo.Blessing = blessing;
            vo.FigureStage = figureStage;
            vo.Combat = combat;
            vo.Etime = etime;
            vo.AutoBuy = autoBuy;
            vo.Attrs = attrs;
            vo.Skills = skills;
        }

        /// <summary>16023 升星成功套值(errcode==1 时调用;blessing_plus/ratio_list 由控制器读完,本层只落 stage/star/blessing)。</summary>
        public void Apply16023(int typeId, int stage, int star, long blessing, long etime, int autoBuy)
        {
            OutWardVo vo = GetOrCreate(typeId);
            vo.Stage = stage;
            vo.Star = star;
            vo.Blessing = blessing;
            vo.Etime = etime;
            vo.AutoBuy = autoBuy;
        }

        /// <summary>16005 通用升星成功套值(≈Apply16023 少 etime/auto_buy 两字段;3翼影/4圣器/5神兵无系统B等级线)。
        /// 薄增量六件套第20轮。</summary>
        public void Apply16005(int typeId, int stage, int star, long blessing)
        {
            OutWardVo vo = GetOrCreate(typeId);
            vo.Stage = stage;
            vo.Star = star;
            vo.Blessing = blessing;
        }

        /// <summary>16028 面板回包套值(系统B全字段)。</summary>
        public void Apply16028(int typeId, int level, long curExp, long combat,
            List<(int attrId, long val)> attrs, List<(int skillId, int skillLevel)> skills)
        {
            OutWardVo vo = GetOrCreate(typeId);
            vo.HasLv = true;
            vo.Level = level;
            vo.CurExp = curExp;
            vo.LvCombat = combat;
            vo.LvAttrs = attrs;
            vo.LvSkills = skills;
        }

        /// <summary>16029 升级成功套值(errcode==1 时调用;add_exp/ratio_list 由控制器读完,本层只落 level/cur_exp/combat)。</summary>
        public void Apply16029(int typeId, int level, long curExp, long combat, List<(int skillId, int skillLevel)> skills)
        {
            OutWardVo vo = GetOrCreate(typeId);
            vo.HasLv = true;
            vo.Level = level;
            vo.CurExp = curExp;
            vo.LvCombat = combat;
            vo.LvSkills = skills;
        }

        public void Clear()
        {
            _map.Clear();
        }
    }

    /// <summary>
    /// config_mount_star 读取器(⚠数字键,列序 config_table_default:0=type_id/1=stage/2=star/3=max_blessing/
    /// 4=attr/5=combat/6=clear_status;主键 "{type_id}@{stage}@{star}")。表经 ClientConfigSync 从 yu_client cdn 同步。
    /// 本轮最小闭环只需 max_blessing(显示"祝福 X/Y")。
    /// </summary>
    public static class OutWardConfigs
    {
        private static JObject _mountStar;

        public static bool IsLoaded => _mountStar != null;

        public static async Task EnsureLoaded()
        {
            if (_mountStar != null) return;
            string key = GameResPath.GetServerConfigPath("config_mount_star");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("OutWard", "missing config_mount_star: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                _mountStar = new JObject();
                return;
            }
            _mountStar = JObject.Parse(asset.text);
            ResManager.Release(asset);
            GameLog.Info("OutWard", "config_mount_star={0}", _mountStar.Count);
        }

        /// <summary>祝福上限(config_mount_star["type@stage@star"]["3"]);缺表/缺项降级 0(标出而非臆造)。</summary>
        public static long GetMaxBlessing(int typeId, int stage, int star)
        {
            if (_mountStar == null) return 0;
            string key = typeId + "@" + stage + "@" + star;
            if (!(_mountStar[key] is JObject obj)) return 0;
            return ReadLong(obj, "3");
        }

        private static long ReadLong(JObject obj, string key)
        {
            JToken token = obj[key];
            if (token == null || token.Type == JTokenType.Null) return 0;
            return token.Type == JTokenType.Integer ? token.Value<long>()
                : long.TryParse(token.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out long v) ? v : 0;
        }
    }
}
