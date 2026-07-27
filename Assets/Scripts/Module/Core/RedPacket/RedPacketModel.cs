using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.Util;

namespace Shenxiao.Module.Core.RedPacket
{
    /// <summary>
    /// 公会红包(RedPacket)数据层(自动循环 轮18 PK2;对标老端 commonModel/RedPacketModel.ts,服务端 pt_339,
    /// 7 号活:33900/33901/33902/33904/33906/33907/33908;33903/33905 死号封存不接,见
    /// <see cref="RedPacketController"/> 类头注释)。
    /// </summary>
    public sealed class RedPacketModel
    {
        public static readonly RedPacketModel Instance = new RedPacketModel();
        private RedPacketModel() { }

        /// <summary>33901/33907 列表项(item_to_bin_0/item_to_bin_3 同构 16 字段,pt_339.erl:182-224/267-309)。</summary>
        public sealed class RedEnvelopeEntry
        {
            public long Id;
            public long RoleId;
            public string RoleName;
            public int Career;
            public int Sex;
            public int Turn;
            public string Picture;
            public int PictureVer;
            public int Type;
            public int Extra;
            public int Status;
            public int ReceiveStatus;
            public int TotalNum;
            public int RecipientsNum;
            public string Msg;
            public int Stime;
        }

        /// <summary>33901 领取记录(item_to_bin_1 4字段,pt_339.erl:225-239)。</summary>
        public sealed class RecordEntry
        {
            public int Id;
            public string RoleName;
            public int CfgId;
            public int Time;
        }

        /// <summary>33902 RecipientList 单项(item_to_bin_2 9字段,pt_339.erl:240-266)。</summary>
        public sealed class RecipientEntry
        {
            public long RoleId;
            public string RoleName;
            public int Career;
            public int Sex;
            public int Turn;
            public string Picture;
            public int PictureVer;
            public int ReceiveMoney;
            public int Time;
        }

        /// <summary>33902 打开详情(15 标量字段 + RecipientList,pt_339.erl:65-112)。</summary>
        public sealed class OpenDetail
        {
            public long RedEnvelopesId;
            public long RoleId;
            public string RoleName;
            public int Career;
            public int Sex;
            public int Turn;
            public string Picture;
            public int PictureVer;
            public int Status;
            public int ReceiveMoney;
            public int TotalNum;
            public int RecipientsNum;
            public int Money;
            public int Type;
            public int Extra;
            public readonly List<RecipientEntry> RecipientList = new List<RecipientEntry>();
        }

        private readonly List<RedEnvelopeEntry> _list = new List<RedEnvelopeEntry>();
        private readonly List<RecordEntry> _records = new List<RecordEntry>();

        public bool HasData { get; private set; }
        public IReadOnlyList<RedEnvelopeEntry> List => _list;
        public IReadOnlyList<RecordEntry> Records => _records;
        public OpenDetail LastOpenDetail { get; private set; }

        /// <summary>
        /// 主界面红包通知数量。完全沿用老端 CheckRedStatus：
        /// receive_status==2，或未开启(status==0)且由自己发出的红包，才属于待处理消息。
        /// </summary>
        public int GetMainNotificationCount(long selfRoleId)
        {
            int count = 0;
            for (int i = 0; i < _list.Count; i++)
            {
                RedEnvelopeEntry entry = _list[i];
                if (entry == null) continue;
                if (entry.ReceiveStatus == 2
                    || (entry.Status == 0 && entry.RoleId == selfRoleId))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>33901 全量落地(对标 SetRedPacketInfo,本端不做 SortRedPacketList 排序——排序是 UI 展示序,
        /// 数据层保留原始到达序,留消费方按需排)。</summary>
        public void ApplyList(List<RedEnvelopeEntry> list, List<RecordEntry> records)
        {
            _list.Clear();
            if (list != null) _list.AddRange(list);
            _records.Clear();
            if (records != null) _records.AddRange(records);
            HasData = true;
        }

        /// <summary>33902 打开详情落地(对标 UpdateRedPacketInfo):列表中同 id 的条目 receive_status 置 1、
        /// recipients_num 同步刷新;33902 全端仅成功路径会发(失败改走 33900,见 Controller 类头注释),
        /// 故本方法恒当"成功"处理。</summary>
        public void ApplyOpenDetail(OpenDetail detail)
        {
            if (detail == null) return;
            LastOpenDetail = detail;
            RedEnvelopeEntry e = _list.Find(x => x.Id == detail.RedEnvelopesId);
            if (e != null)
            {
                e.ReceiveStatus = 1;
                e.RecipientsNum = detail.RecipientsNum;
            }
        }

        /// <summary>33907 新增推送单条落地。m3存档老端事实 bug(RedPacketController.ts:116-126):
        /// on33907 把 info.red_envelopes_list.concat(scmd.red_envelopes_list) 拼成一个裸数组 arr,
        /// 直接传给 model.SetRedPacketInfo(arr)——该方法期望入参是 {red_envelopes_list:[...], ...}
        /// 整包对象(RedPacketModel.ts:107-111 读 scmd.red_envelopes_list 再整体赋给 _rp_info),裸数组没有
        /// 这个字段,结果是 _rp_info 被整体替换成一个裸数组,原有的非 red_envelopes_list 字段全部丢失,
        /// 结构损坏。本端不复刻该 bug,按语义实现:仅原地 append 新条目,不动其余字段。</summary>
        public void ApplyNewPush(RedEnvelopeEntry entry)
        {
            if (entry == null) return;
            _list.Add(entry);
        }

        /// <summary>33908 领完推送落地(对标 on33908):同 id 的 status 置 2(已领完),receive_status==2 时清 0,
        /// recipients_num 补满 total_num(镜像老端逐字段赋值,含 "receuve_status" 拼写一致的语义)。</summary>
        public void ApplyTakenPush(long id)
        {
            RedEnvelopeEntry e = _list.Find(x => x.Id == id);
            if (e == null) return;
            e.Status = 2;
            if (e.ReceiveStatus == 2) e.ReceiveStatus = 0;
            e.RecipientsNum = e.TotalNum;
        }

        public void Reset()
        {
            _list.Clear();
            _records.Clear();
            LastOpenDetail = null;
            HasData = false;
        }
    }

    /// <summary>
    /// config_red_envelopes(主键=id 字符串,16 条)+ config_red_envelopes_goods(主键=goods_type_id 字符串,
    /// 3 条)读取器。表经 ClientConfigSync 从 yu_client cdn 同步(P0 已搬运)。
    /// </summary>
    public static class RedPacketConfigs
    {
        private static JObject _cfg;
        private static JObject _goods;

        public static bool IsLoaded => _cfg != null && _goods != null;
        public static int Count => _cfg?.Count ?? 0;
        public static int GoodsCount => _goods?.Count ?? 0;

        public static async Task EnsureLoaded()
        {
            if (_cfg != null && _goods != null) return;
            await LoadCfg();
            await LoadGoods();
        }

        private static async Task LoadCfg()
        {
            if (_cfg != null) return;
            string key = GameResPath.GetServerConfigPath("config_red_envelopes");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("RedPacket", "missing config_red_envelopes: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                _cfg = new JObject();
                return;
            }
            _cfg = JObject.Parse(asset.text);
            ResManager.Release(asset);
            GameLog.Info("RedPacket", "config_red_envelopes={0}", _cfg.Count);
        }

        private static async Task LoadGoods()
        {
            if (_goods != null) return;
            string key = GameResPath.GetServerConfigPath("config_red_envelopes_goods");
            UnityEngine.TextAsset asset = await ResManager.LoadAsync<UnityEngine.TextAsset>(key);
            if (asset == null)
            {
                GameLog.Error("RedPacket", "missing config_red_envelopes_goods: {0}(未同步?跑 神霄/配表/同步客户端配置)", key);
                _goods = new JObject();
                return;
            }
            _goods = JObject.Parse(asset.text);
            ResManager.Release(asset);
            GameLog.Info("RedPacket", "config_red_envelopes_goods={0}", _goods.Count);
        }
    }
}
