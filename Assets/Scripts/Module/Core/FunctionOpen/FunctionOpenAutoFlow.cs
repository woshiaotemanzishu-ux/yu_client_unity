using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Res;
using Shenxiao.Framework.UI;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.UI.FunctionOpen;
using Shenxiao.Module.Core.Skill;
using UnityEngine;

namespace Shenxiao.Module.Core.FunctionOpen
{
    /// <summary>
    /// 获得技能组合弹层队列。只负责真实技能变化后的排队、Prefab 生命周期与数据快照；
    /// 具体布局和演出参数由 FunctionOpenAutoView/FunctionOpenModule.prefab 持有。
    /// </summary>
    public static class FunctionOpenAutoFlow
    {
        private readonly struct SkillEntry
        {
            public readonly int SkillId;
            public readonly int Level;

            public SkillEntry(int skillId, int level)
            {
                SkillId = skillId;
                Level = level;
            }
        }

        private static readonly Queue<SkillEntry> Pending = new Queue<SkillEntry>();
        private static GameObject _moduleRoot;
        private static FunctionOpenAutoView _presenter;
        private static bool _initialized;
        private static bool _loading;
        private static bool _showing;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            EventDispatcher.On(GlobalEvent.EVT_SCENE_MAP_READY, OnEnvironmentReady);
        }

        public static void EnqueueSkillUpgrade(int skillId, int level)
        {
            if (skillId <= 0 || level <= 0) return;
            Pending.Enqueue(new SkillEntry(skillId, level));
            _ = TryShowNext();
        }

        /// <summary>编辑器/运行时验收入口，走与真实技能获得完全相同的弹层队列。</summary>
        public static void PreviewSkill(int skillId, int level)
        {
            EnqueueSkillUpgrade(skillId, Mathf.Max(1, level));
        }

        public static void Reset()
        {
            if (_initialized)
            {
                EventDispatcher.Off(GlobalEvent.EVT_SCENE_MAP_READY, OnEnvironmentReady);
                _initialized = false;
            }

            Pending.Clear();
            _showing = false;
            _loading = false;
            if (_presenter != null) _presenter.CancelWithoutCallback();
            _presenter = null;
            if (_moduleRoot != null)
            {
                ResManager.ReleaseInstance(_moduleRoot);
                _moduleRoot = null;
            }
        }

        private static async Task TryShowNext()
        {
            if (_loading || _showing || Pending.Count == 0) return;
            _loading = true;
            try
            {
                if (!await EnsureViewLoaded()) return;
                await SkillConfigs.EnsureLoaded();

                while (!_showing && Pending.Count > 0)
                {
                    SkillEntry entry = Pending.Dequeue();
                    if (!SkillConfigs.Has(entry.SkillId))
                    {
                        GameLog.Warn("FunctionOpen", "skip skill popup: config missing skill={0}", entry.SkillId);
                        continue;
                    }

                    string name = SkillConfigs.GetName(entry.SkillId);
                    string icon = SkillConfigs.GetIconForLevel(entry.SkillId, entry.Level);
                    string desc = SkillConfigs.GetDescRichForLevel(entry.SkillId, entry.Level);
                    _moduleRoot.SetActive(true);
                    _moduleRoot.transform.SetAsLastSibling();
                    _showing = true;
                    _presenter.ShowSkill(entry.SkillId, name, icon, desc, OnClosed);
                    GameLog.Info("FunctionOpen", "show skill popup skill={0} level={1}", entry.SkillId, entry.Level);
                }
            }
            finally
            {
                _loading = false;
            }
        }

        private static async Task<bool> EnsureViewLoaded()
        {
            if (_moduleRoot != null && _presenter != null) return true;

            Transform parent = ViewManager.GetLayer(UILayer.Popup);
            if (parent == null)
            {
                GameLog.Warn("FunctionOpen", "Popup layer not ready; keep skill popup queued");
                return false;
            }

            string key = GameResPath.GetUIPrefab("functionOpen", "FunctionOpenModule");
            _moduleRoot = await ResManager.InstantiateAsync(key, parent);
            if (_moduleRoot == null)
            {
                GameLog.Error("FunctionOpen", "FunctionOpenModule load failed: {0}", key);
                return false;
            }

            _moduleRoot.name = "FunctionOpenModule(SkillPopup)";
            BaseView[] views = _moduleRoot.GetComponentsInChildren<BaseView>(true);
            for (int i = 0; i < views.Length; i++) views[i].gameObject.SetActive(false);

            FunctionOpenAutoViewBind bind = _moduleRoot.GetComponentInChildren<FunctionOpenAutoViewBind>(true);
            if (bind == null)
            {
                GameLog.Error("FunctionOpen", "FunctionOpenModule missing FunctionOpenAutoViewBind");
                ResManager.ReleaseInstance(_moduleRoot);
                _moduleRoot = null;
                return false;
            }

            _presenter = bind.GetComponent<FunctionOpenAutoView>();
            if (_presenter == null) _presenter = bind.gameObject.AddComponent<FunctionOpenAutoView>();
            _presenter.Bind(bind);
            _moduleRoot.SetActive(false);
            return true;
        }

        private static void OnClosed()
        {
            _showing = false;
            if (_moduleRoot != null) _moduleRoot.SetActive(false);
            _ = TryShowNext();
        }

        private static void OnEnvironmentReady()
        {
            _ = TryShowNext();
        }
    }
}
