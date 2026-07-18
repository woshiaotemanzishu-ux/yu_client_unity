using System.Collections.Generic;

namespace Shenxiao.Module.Core.Fashion
{
    /// <summary>
    /// 时装数据层(对标老端 commonModel/FashionModel.ts 的 fashion_info_dic/fashion_active_dic,协议段 pt_413)。
    /// 第21轮 PA 第一刀:只落 41300/41301/41302/41303/41304/41306/41312/41316 八个活号需要的字段。
    /// pos 只有 1(衣服)/3(头饰)——data_fashion.erl:19275 get_pos_id_list() -> [1,3]。
    /// </summary>
    public sealed class FashionModel
    {
        public static readonly FashionModel Instance = new FashionModel();
        private FashionModel() { }

        /// <summary>某个颜色档的星级(colorId==0 是基础色,不出现在这个字典——它就是 FashionEntry.StarLv 本身)。</summary>
        public sealed class ColorEntry
        {
            public int ColorId;
            public int StarLv;
        }

        /// <summary>已激活的时装本体(对标 IFashionInfo):基础色星级 + 当前穿的颜色 + 已解锁颜色表。</summary>
        public sealed class FashionEntry
        {
            public int FashionId;
            public int StarLv;      // 基础色(color 0)星级,41306 更新
            public int NowColorId;  // 当前穿的颜色(0=基础色)
            public readonly Dictionary<int, ColorEntry> Colors = new Dictionary<int, ColorEntry>(); // 已解锁颜色(不含0)

            /// <summary>取某颜色档当前星级;0=基础色直接读 StarLv,其余查 Colors(未解锁返回 -1)。</summary>
            public int GetStarLv(int colorId)
            {
                if (colorId == 0) return StarLv;
                return Colors.TryGetValue(colorId, out ColorEntry e) ? e.StarLv : -1;
            }

            public bool IsColorUnlocked(int colorId) => colorId == 0 || Colors.ContainsKey(colorId);
        }

        /// <summary>一个穿戴位(pos 1/3)的整体状态:当前穿的时装 id + 部位等级(41305 第二刀用,先落字段) + 已激活时装表。</summary>
        public sealed class PosInfo
        {
            public int PosId;
            public int WearFashionId;   // 0=未穿
            public int PosLv;           // 部位等级(第二刀 FashionLevelView 用,本轮只落数据)
            public long PosUpgradeNum;
            public readonly Dictionary<int, FashionEntry> Active = new Dictionary<int, FashionEntry>();

            public FashionEntry GetActive(int fashionId) => Active.TryGetValue(fashionId, out FashionEntry e) ? e : null;
        }

        /// <summary>41312 战力(某 pos+fashionId 下每个颜色档的当前/下一档战力)。</summary>
        public sealed class PowerEntry
        {
            public int ColorId;
            public long Power;
            public long NextPower;
        }

        // ---- 41300 快照用的传输结构(Controller 解析 wire 后组装,交给本层落地) ----
        public sealed class ColorWire { public int ColorId; public int StarLv; }
        public sealed class FashionWire { public int FashionId; public int StarLv; public int NowColorId; public List<ColorWire> Colors; }
        public sealed class PosWire { public int PosId; public int WearFashionId; public int PosLv; public long PosUpgradeNum; public List<FashionWire> Fashions; }

        private readonly Dictionary<int, PosInfo> _pos = new Dictionary<int, PosInfo>();
        private readonly Dictionary<long, List<PowerEntry>> _power = new Dictionary<long, List<PowerEntry>>();

        public PosInfo GetPos(int posId) => _pos.TryGetValue(posId, out PosInfo p) ? p : null;

        private PosInfo GetOrCreatePos(int posId)
        {
            if (!_pos.TryGetValue(posId, out PosInfo p))
            {
                p = new PosInfo { PosId = posId };
                _pos[posId] = p;
            }
            return p;
        }

        public FashionEntry GetActive(int posId, int fashionId) => GetPos(posId)?.GetActive(fashionId);

        public bool IsActivated(int posId, int fashionId) => GetActive(posId, fashionId) != null;

        private static long PowerKey(int posId, int fashionId) => ((long)posId << 40) | (uint)fashionId;

        public List<PowerEntry> GetPower(int posId, int fashionId) =>
            _power.TryGetValue(PowerKey(posId, fashionId), out List<PowerEntry> list) ? list : null;

        /// <summary>41300 全量套值(对标老端 On41300 遍历 pos_list 逐条 Fire(GETACTIVATEFASHION) → CreateActiveList):
        /// 整体替换,不做增量合并——这是快照,不是补丁。</summary>
        public void Apply41300(List<PosWire> posList)
        {
            if (posList == null) return;
            foreach (PosWire row in posList)
            {
                PosInfo p = GetOrCreatePos(row.PosId);
                p.WearFashionId = row.WearFashionId;
                p.PosLv = row.PosLv;
                p.PosUpgradeNum = row.PosUpgradeNum;
                p.Active.Clear();
                if (row.Fashions == null) continue;
                foreach (FashionWire f in row.Fashions)
                {
                    var entry = new FashionEntry { FashionId = f.FashionId, StarLv = f.StarLv, NowColorId = f.NowColorId };
                    if (f.Colors != null)
                    {
                        foreach (ColorWire c in f.Colors)
                        {
                            entry.Colors[c.ColorId] = new ColorEntry { ColorId = c.ColorId, StarLv = c.StarLv };
                        }
                    }
                    p.Active[f.FashionId] = entry;
                }
            }
        }

        /// <summary>41301 染色解锁成功套值(对标老端 On41301):追加颜色到 color_list,若该时装尚未在 Active 里
        /// (理论不该发生,41301 前提是已激活)先补一条基础壳,避免空引用。</summary>
        public void Apply41301(int posId, int fashionId, int colorId)
        {
            PosInfo p = GetOrCreatePos(posId);
            if (p.WearFashionId <= 0) p.WearFashionId = fashionId;
            FashionEntry e = p.GetActive(fashionId);
            if (e == null)
            {
                e = new FashionEntry { FashionId = fashionId, StarLv = 1, NowColorId = colorId };
                p.Active[fashionId] = e;
            }
            e.NowColorId = colorId;
            e.Colors[colorId] = new ColorEntry { ColorId = colorId, StarLv = 1 };
        }

        /// <summary>41302 穿戴成功套值(对标老端 On41302):切换本位当前穿的 fashion_id + 该时装的当前颜色。</summary>
        public void Apply41302(int posId, int fashionId, int colorId)
        {
            PosInfo p = GetOrCreatePos(posId);
            p.WearFashionId = fashionId;
            FashionEntry e = p.GetActive(fashionId);
            if (e != null) e.NowColorId = colorId;
        }

        /// <summary>41303 卸下成功套值(对标老端 On41303):⚠也用于被动卸下广播(穿神殿/套装收集/天启顶掉时装时,
        /// 服务端会对 pos∈[1,3] 各推一条非本人请求的 41303),故只按 PosId 清 wear,不校验是否是当前选中项。</summary>
        public void Apply41303(int posId)
        {
            PosInfo p = GetOrCreatePos(posId);
            p.WearFashionId = 0;
        }

        /// <summary>41304 激活成功套值(对标老端 On41304):新建/覆盖为基础色 1 星已激活状态。
        /// 穿戴由 Controller 紧接着自动发的 41302 完成,这里不动 wear_fashion_id。</summary>
        public void Apply41304(int posId, int fashionId)
        {
            PosInfo p = GetOrCreatePos(posId);
            FashionEntry e = p.GetActive(fashionId);
            if (e == null)
            {
                e = new FashionEntry { FashionId = fashionId, StarLv = 1, NowColorId = 0 };
                p.Active[fashionId] = e;
            }
            else
            {
                e.StarLv = 1;
                e.NowColorId = 0;
            }
        }

        /// <summary>41306 基础色进阶成功套值(对标老端 On41306):更新 color_list 里 colorId 那一档星级;
        /// 若该档正是当前穿的颜色,顶层 StarLv 同步刷新(对标老端"list.now_color_id==色id 才更新展示星级"分支)。</summary>
        public void Apply41306(int posId, int fashionId, int colorId, int newStarLv)
        {
            FashionEntry e = GetActive(posId, fashionId);
            if (e == null) return;
            if (colorId == 0)
            {
                e.StarLv = newStarLv;
            }
            else if (e.Colors.TryGetValue(colorId, out ColorEntry c))
            {
                c.StarLv = newStarLv;
            }
            if (e.NowColorId == colorId) e.StarLv = newStarLv;
        }

        /// <summary>41316 彩色进阶成功套值,语义同 41306(对标老端 On41316,字段名 Lv 而非 FashionStarLv)。</summary>
        public void Apply41316(int posId, int fashionId, int colorId, int newLv) => Apply41306(posId, fashionId, colorId, newLv);

        /// <summary>41312 战力回包套值(对标老端 On41312 直转发 UPDATE_FIGHT,这里落地供 UI 读)。</summary>
        public void Apply41312(int posId, int fashionId, List<PowerEntry> colorPowers)
        {
            _power[PowerKey(posId, fashionId)] = colorPowers;
        }

        public void Clear()
        {
            _pos.Clear();
            _power.Clear();
        }
    }
}
