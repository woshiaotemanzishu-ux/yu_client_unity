using System.Collections.Generic;

namespace Shenxiao.Module.Core.AdReward
{
    /// <summary>
    /// 广告奖励(AdReward)数据(对标老客户端 commonModel/WelfareModel.ts 广告分支,自动循环 轮18 PK4)。
    /// 承载 19302 下发的广告冷却/开放列表(ad_list)。
    /// </summary>
    public sealed class AdRewardModel
    {
        public static readonly AdRewardModel Instance = new AdRewardModel();
        private AdRewardModel() { }

        /// <summary>ad_list[i](对标老端 scmd.ad_list 单项):{ModId,SubId,Count}。Count&gt;0 表示当日仍有可看次数
        /// (对标老端 GetAdLookState:mod_id==v.mod_id&amp;&amp;sub_id==v.sub_id&amp;&amp;count&gt;0)。</summary>
        public readonly struct AdEntry
        {
            public readonly int ModId;
            public readonly int SubId;
            public readonly int Count;
            public AdEntry(int modId, int subId, int count) { ModId = modId; SubId = subId; Count = count; }
        }

        public bool HasList { get; private set; }
        public IReadOnlyList<AdEntry> AdList => _adList;
        private readonly List<AdEntry> _adList = new List<AdEntry>();

        public void SetList(List<AdEntry> list)
        {
            _adList.Clear();
            if (list != null) _adList.AddRange(list);
            HasList = true;
        }

        /// <summary>对标老端 WelfareModel.GetAdLookState(mod_id,mod_sub_id):某广告位当日是否仍有可看次数。</summary>
        public bool GetLookState(int modId, int subId)
        {
            for (int i = 0; i < _adList.Count; i++)
            {
                AdEntry e = _adList[i];
                if (e.ModId == modId && e.SubId == subId && e.Count > 0) return true;
            }
            return false;
        }

        /// <summary>广告开放状态(对标老端 WelfareModel.GetAdOpenState:
        /// `Util.IsConch() &amp;&amp; PlatformManager.IsEyouPlatform() &amp;&amp; ClientConfig.ShowEyouAd`,
        /// 三者皆 Laya-native 壳/Eyou 发行渠道专属平台信号,Unity 客户端无对应实现——保守恒 false,不臆造平台判定。
        /// 若后续接入 Eyou 发行渠道,在此接真实平台探测再放开。</summary>
        public bool GetAdOpenState() => false;

        public void Reset()
        {
            HasList = false;
            _adList.Clear();
        }
    }
}
