using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.MainStronger
{
    /// <summary>聚合业务模块发布的红点，只保留当前可跳转的推荐项。</summary>
    public sealed class MainStrongerModel
    {
        public static MainStrongerModel Instance { get; } = new MainStrongerModel();

        private readonly Dictionary<string, bool> _redState
            = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly List<MainStrongerConfigs.Feature> _recommendations
            = new List<MainStrongerConfigs.Feature>();
        private bool _skillAwakeReady;

        public event Action Changed;
        public IReadOnlyList<MainStrongerConfigs.Feature> Recommendations => _recommendations;
        public int StrongerCount => _recommendations.Count;

        private MainStrongerModel() { }

        /// <summary>跨模块只发布红点键和值，不把业务模型耦合进来。</summary>
        public void PublishRedState(string redKey, bool active)
        {
            if (string.IsNullOrEmpty(redKey)) return;
            if (_redState.TryGetValue(redKey, out bool old) && old == active) return;
            _redState[redKey] = active;
            Rebuild();
        }

        /// <summary>技能觉醒任务由 Task 模块完成资格判断后发布。</summary>
        public void PublishSkillAwake(bool ready)
        {
            if (_skillAwakeReady == ready) return;
            _skillAwakeReady = ready;
            Rebuild();
        }

        internal void Rebuild()
        {
            _recommendations.Clear();
            foreach (MainStrongerConfigs.Feature feature in MainStrongerConfigs.Features.Values)
            {
                bool active = feature.Id == 10001
                    ? _skillAwakeReady && MainStrongerFlow.CanOpenSkillAwake
                    : _redState.TryGetValue(feature.RedKey, out bool red) && red &&
                      MainStrongerFlow.CanOpenFeature(feature.Func);
                if (active) _recommendations.Add(feature);
            }
            _recommendations.Sort(MainStrongerConfigs.CompareFeature);
            Changed?.Invoke();
        }

        internal void Reset()
        {
            _redState.Clear();
            _recommendations.Clear();
            _skillAwakeReady = false;
            Changed?.Invoke();
        }
    }
}
