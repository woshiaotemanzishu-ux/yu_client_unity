using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// Runtime activity icon registry, mirroring yu_client ActivityIconManager.icon_info_dic.
    /// </summary>
    public sealed class ActivityIconManager
    {
        public static readonly ActivityIconManager Instance = new ActivityIconManager();

        public static class LocationType
        {
            public const int ActivityOne = 1;
            public const int ActivityTwo = 2;
            public const int ActivityOther = 3;
            public const int ActivityFourth = 4;
            public const int RightMiddle = 5;
            public const int Left = 6;
            public const int Right = 7;
            public const int Notice = 8;
        }

        public sealed class IconInfo
        {
            public string IconType;
            public int Time;
            public string IconTxt = "";
            public string IconImg;
            public MainUIConfigs.FunctionIconCfg Data;
        }

        private readonly Dictionary<string, IconInfo> _iconInfoByType = new Dictionary<string, IconInfo>();
        private bool _defaultIconsInitialized;

        private ActivityIconManager() { }

        public IReadOnlyDictionary<string, IconInfo> IconInfoByType => _iconInfoByType;

        public IconInfo GetIconInfo(string iconType)
        {
            if (string.IsNullOrEmpty(iconType)) return null;
            return _iconInfoByType.TryGetValue(iconType, out IconInfo info) ? info : null;
        }

        public async Task EnsureDefaultIconsAsync()
        {
            if (_defaultIconsInitialized) return;
            await MainUIConfigs.EnsureLoaded();
            foreach (MainUIConfigs.FunctionIconCfg cfg in MainUIConfigs.AllFunctionIconCfg())
            {
                if (cfg == null || cfg.ControllByOwnFun) continue;
                AddIcon(cfg.IconType);
            }
            _defaultIconsInitialized = true;
        }

        public void AddIcon(string iconType, int time = 0, string iconTxt = "", string iconImg = null)
        {
            if (string.IsNullOrEmpty(iconType)) return;

            MainUIConfigs.FunctionIconCfg cfg = MainUIConfigs.GetFunctionIconCfg(iconType);
            if (cfg == null || !FunIsOpenByIconType(cfg)) return;

            if (_iconInfoByType.TryGetValue(iconType, out IconInfo existing))
            {
                existing.Time = time;
                existing.IconTxt = iconTxt ?? "";
                existing.IconImg = iconImg;
                existing.Data = cfg;
                EventDispatcher.Emit(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_UPDATE, iconType);
                return;
            }

            _iconInfoByType[iconType] = new IconInfo
            {
                IconType = iconType,
                Time = time,
                IconTxt = iconTxt ?? "",
                IconImg = iconImg,
                Data = cfg,
            };
            EventDispatcher.Emit(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_ADD, iconType, cfg.LocationType);
        }

        public void DeleteIcon(string iconType)
        {
            if (string.IsNullOrEmpty(iconType)) return;
            if (!_iconInfoByType.Remove(iconType)) return;
            EventDispatcher.Emit(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_DELETE, iconType);
        }

        private static bool FunIsOpenByIconType(MainUIConfigs.FunctionIconCfg cfg)
        {
            RoleModel role = RoleModel.Instance;
            int level = role.HasBaseInfo ? role.Level : 0;
            int openDay = 1;
            if (level < cfg.OpenLv) return false;
            if (openDay < cfg.OpenDay) return false;
            if (cfg.MaxOpenDay != 0 && openDay > cfg.MaxOpenDay) return false;
            if (cfg.OpenTaskId != 0) return false;
            return true;
        }
    }
}
