using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.Partner
{
    /// <summary>
    /// 剑魄同修数据层(对标老端 commonModel/PartnerModel.ts)。主线链序第一闸:task 100190(ctype 25)要求
    /// 同修培养到 1阶2星(server data_task.erl get_content(100190,0,0):id=1 阶,need_num=2 星)。
    /// 列表 14202 全量落此;14201 单个推送/请求回包 upsert(部分字段,保留既有 figure/biog);
    /// 14205 培养结果套 stage/star/blessing(对标 On14205 的 data.stage/star/blessing 赋值)。
    /// </summary>
    public sealed class PartnerModel
    {
        public static readonly PartnerModel Instance = new PartnerModel();
        private PartnerModel() { }

        /// <summary>同修实例(字段名对照 ClientProtocol.json "14202" companion_list 单项)。</summary>
        public sealed class CompanionVo
        {
            public int CompanionId;
            public int Stage;       // 阶
            public int Star;        // 星
            public bool IsActive;   // 已激活
            public bool IsFight;    // 出战中
            public int FigureId;
            public long Blessing;   // 祝福值(培养进度)
            public long TrainNum;   // 培养次数
            public long Combat;     // 战力
            public List<int> BiogLvs;                 // biog_list(传记等级,本轮只按序读出留存)
            public List<(int attrId, long val)> Attrs; // attr(属性,本轮只按序读出留存)
        }

        /// <summary>14200 最近一包原始场景外观通知；不可变，避免后包覆盖时污染历史事件参数。</summary>
        public sealed class SceneFigureNotice
        {
            public byte TypeId { get; }
            public long RoleId { get; }
            public uint FigureId { get; }

            public SceneFigureNotice(byte typeId, long roleId, uint figureId)
            {
                TypeId = typeId;
                RoleId = roleId;
                FigureId = figureId;
            }
        }

        public readonly List<CompanionVo> Companions = new List<CompanionVo>();
        public int FightId;
        public bool HasData { get; private set; }
        public SceneFigureNotice LastSceneFigureNotice { get; private set; }
        public bool HasSceneFigureNotice => LastSceneFigureNotice != null;

        public CompanionVo Get(int companionId)
        {
            return Companions.Find(c => c.CompanionId == companionId);
        }

        /// <summary>14202 全量(对标 SetScmd14202:清空重建)。</summary>
        public void SetList(int fightId, List<CompanionVo> list)
        {
            Companions.Clear();
            if (list != null) Companions.AddRange(list);
            FightId = fightId;
            HasData = true;
        }

        /// <summary>14201 单个更新(部分字段包):已有则套字段(保留 IsFight/FigureId/BiogLvs 等 14201 不带的),没有则新增。</summary>
        public void Upsert(CompanionVo vo)
        {
            if (vo == null) return;
            CompanionVo old = Get(vo.CompanionId);
            if (old == null)
            {
                Companions.Add(vo);
                return;
            }
            old.Stage = vo.Stage;
            old.Star = vo.Star;
            old.IsActive = vo.IsActive;
            old.Blessing = vo.Blessing;
            old.TrainNum = vo.TrainNum;
            old.Combat = vo.Combat;
            if (vo.Attrs != null) old.Attrs = vo.Attrs;
        }

        /// <summary>14205 培养成功回包套值(对标 On14205 else 分支);返回 阶/星是否变化(老端变了才再拉 14201)。</summary>
        public bool ApplyTrain(int companionId, int stage, int star, long blessing)
        {
            CompanionVo vo = Get(companionId);
            if (vo == null) return false;
            bool changed = vo.Stage != stage || vo.Star != star;
            vo.Stage = stage;
            vo.Star = star;
            vo.Blessing = blessing;
            return changed;
        }

        /// <summary>14200 每包完整替换独立通知切片，不改 14201/14202 的同修列表与 FightId。</summary>
        public SceneFigureNotice ReplaceSceneFigureNotice(byte typeId, long roleId, uint figureId)
        {
            LastSceneFigureNotice = new SceneFigureNotice(typeId, roleId, figureId);
            return LastSceneFigureNotice;
        }

        public void Clear()
        {
            Companions.Clear();
            FightId = 0;
            HasData = false;
            LastSceneFigureNotice = null;
        }
    }

    /// <summary>
    /// config_companion 读取器(具名键:id/name/figure_id/goods_id/train_goods_id/train_limit…;
    /// 对标老端 PartnerModel 的 pre_ser_cfg.config_companion)。表经 ClientConfigSync 从 yu_client cdn 同步。
    /// </summary>
    public static class PartnerConfigs
    {
        private static JObject _companion;

        public static bool IsLoaded => _companion != null;

        public static async Task EnsureLoaded()
        {
            if (_companion != null) return;
            string key = GameResPath.GetServerConfigPath("config_companion");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("Partner", "missing config_companion: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                _companion = new JObject();
                return;
            }
            _companion = JObject.Parse(asset.text);
            ResManager.Release(asset);
            GameLog.Info("Partner", "config_companion={0}", _companion.Count);
        }

        /// <summary>同修名(具名键 name);缺表/缺项降级 "同修{id}"(标出而非臆造)。</summary>
        public static string GetName(int companionId)
        {
            if (_companion?[companionId.ToString()] is JObject obj)
            {
                string name = obj.Value<string>("name");
                if (!string.IsNullOrEmpty(name)) return name;
            }
            return "同修" + companionId;
        }
    }
}
