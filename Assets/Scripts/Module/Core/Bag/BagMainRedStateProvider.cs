using System;
using System.Collections.Generic;

namespace Shenxiao.Module.Core.Bag
{
    /// <summary>
    /// 底栏 Bag 红点的模块边界合同。只聚合权威状态，不直接修改 MainUI，也不把未知来源猜成 false。
    /// 老端 MainUIModel.GetMainFuncRedState(Bag) 的外岛来源由总控在各模块完成后注入。
    /// </summary>
    public sealed class BagMainRedStateProvider
    {
        public static readonly BagMainRedStateProvider Instance = new BagMainRedStateProvider();

        public readonly struct Snapshot
        {
            public readonly bool Use;
            public readonly bool OneKeyUse;
            public readonly bool Smelt;
            public readonly bool OneKeyWear;
            public readonly bool HolySeal;
            public readonly bool Revelation;
            public readonly bool Longlang;
            public readonly bool ExternalGuard;
            public readonly bool ExternalDragonBall;
            public readonly bool ExternalRevelationComposite;
            public readonly bool IsComplete;

            public bool Any => Use || OneKeyUse || Smelt || OneKeyWear || HolySeal ||
                               Revelation || Longlang || ExternalGuard || ExternalDragonBall ||
                               ExternalRevelationComposite;

            public Snapshot(bool use, bool oneKeyUse, bool smelt, bool oneKeyWear,
                bool holySeal, bool revelation, bool longlang, bool externalGuard,
                bool externalDragonBall, bool externalRevelationComposite, bool isComplete)
            {
                Use = use;
                OneKeyUse = oneKeyUse;
                Smelt = smelt;
                OneKeyWear = oneKeyWear;
                HolySeal = holySeal;
                Revelation = revelation;
                Longlang = longlang;
                ExternalGuard = externalGuard;
                ExternalDragonBall = externalDragonBall;
                ExternalRevelationComposite = externalRevelationComposite;
                IsComplete = isComplete;
            }
        }

        private readonly HashSet<string> _unknownSources = new HashSet<string>(StringComparer.Ordinal);
        private Func<bool> _use = False;
        private Func<bool> _oneKeyUse = False;
        private Func<bool> _smelt = False;
        private Func<bool> _oneKeyWear = False;
        private Func<bool> _holySeal = False;
        private Func<bool> _revelation = False;
        private Func<bool> _longlang = False;
        private bool _externalGuard;
        private bool _externalDragonBall;
        private bool _externalRevelationComposite;
        private Snapshot _current;

        private BagMainRedStateProvider()
        {
            _unknownSources.UnionWith(new[]
            {
                "bag.use", "bag.one-key-use", "bag.smelt", "bag.one-key-wear",
                "holy-seal", "revelation", "longlang", "external.guard",
                "external.dragon-ball", "external.revelation-composite"
            });
            _current = BuildSnapshot();
        }

        public Snapshot Current => _current;
        public IReadOnlyCollection<string> UnknownSources => _unknownSources;
        public event Action<Snapshot> Changed;

        public void ConfigureOwnedProviders(Func<bool> use, Func<bool> oneKeyUse,
            Func<bool> smelt, Func<bool> oneKeyWear, Func<bool> holySeal,
            Func<bool> revelation, Func<bool> longlang)
        {
            _use = use ?? False;
            _oneKeyUse = oneKeyUse ?? False;
            _smelt = smelt ?? False;
            _oneKeyWear = oneKeyWear ?? False;
            _holySeal = holySeal ?? False;
            _revelation = revelation ?? False;
            _longlang = longlang ?? False;
            ResolveKnown("bag.use", use);
            ResolveKnown("bag.one-key-use", oneKeyUse);
            ResolveKnown("bag.smelt", smelt);
            ResolveKnown("bag.one-key-wear", oneKeyWear);
            ResolveKnown("holy-seal", holySeal);
            ResolveKnown("revelation", revelation);
            ResolveKnown("longlang", longlang);
            Refresh();
        }

        public void SetExternalGuard(bool value, bool authoritative)
        {
            _externalGuard = value;
            SetAuthority("external.guard", authoritative);
            Refresh();
        }

        public void SetExternalDragonBall(bool value, bool authoritative)
        {
            _externalDragonBall = value;
            SetAuthority("external.dragon-ball", authoritative);
            Refresh();
        }

        public void SetExternalRevelationComposite(bool value, bool authoritative)
        {
            _externalRevelationComposite = value;
            SetAuthority("external.revelation-composite", authoritative);
            Refresh();
        }

        public Snapshot Refresh()
        {
            Snapshot next = BuildSnapshot();
            if (!Equals(_current, next))
            {
                _current = next;
                Changed?.Invoke(next);
            }
            return _current;
        }

        private Snapshot BuildSnapshot()
        {
            return new Snapshot(Safe(_use), Safe(_oneKeyUse), Safe(_smelt),
                Safe(_oneKeyWear), Safe(_holySeal), Safe(_revelation), Safe(_longlang),
                _externalGuard, _externalDragonBall, _externalRevelationComposite,
                _unknownSources.Count == 0);
        }

        private static bool Equals(Snapshot a, Snapshot b)
        {
            return a.Use == b.Use && a.OneKeyUse == b.OneKeyUse && a.Smelt == b.Smelt &&
                   a.OneKeyWear == b.OneKeyWear && a.HolySeal == b.HolySeal &&
                   a.Revelation == b.Revelation && a.Longlang == b.Longlang &&
                   a.ExternalGuard == b.ExternalGuard &&
                   a.ExternalDragonBall == b.ExternalDragonBall &&
                   a.ExternalRevelationComposite == b.ExternalRevelationComposite &&
                   a.IsComplete == b.IsComplete;
        }

        private static bool Safe(Func<bool> provider)
        {
            try { return provider != null && provider(); }
            catch { return false; }
        }

        private static bool False() => false;

        private void ResolveKnown(string source, Delegate provider)
        {
            SetAuthority(source, provider != null);
        }

        private void SetAuthority(string source, bool authoritative)
        {
            if (authoritative) _unknownSources.Remove(source);
            else _unknownSources.Add(source);
        }
    }
}
