using System.Collections.Generic;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Module.Core.Game;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Tasks;

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
            public const int Left = 4;
            public const int Right = 5;
            public const int Notice = 6;
            public const int NoticeAfter = 7;
            public const int RightMiddle = 9;
            public const int ActivityFourth = 10;
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
        private Task _refreshDefaultIconsTask;

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
            await RefreshDefaultIconsAsync();
        }

        /// <summary>
        /// Re-scan config-driven activity icons after role level or newest finished task changes.
        /// Old client calls addIcon again on those changes; icons whose open condition is no longer
        /// satisfied are deleted, and newly-open icons are added.
        /// </summary>
        public async Task RefreshDefaultIconsAsync()
        {
            if (_refreshDefaultIconsTask != null && !_refreshDefaultIconsTask.IsCompleted)
            {
                await _refreshDefaultIconsTask;
                return;
            }

            _refreshDefaultIconsTask = RefreshDefaultIconsCoreAsync();
            await _refreshDefaultIconsTask;
        }

        private async Task RefreshDefaultIconsCoreAsync()
        {
            await MainUIConfigs.EnsureLoaded();
            foreach (MainUIConfigs.FunctionIconCfg cfg in MainUIConfigs.AllFunctionIconCfg())
            {
                if (cfg == null || cfg.ControllByOwnFun) continue;
                if (FunIsOpenByIconType(cfg)) AddIcon(cfg.IconType);
                else DeleteIcon(cfg.IconType);
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

        /// <summary>
        /// 强加「自管理」图标(controll_by_own_fun=true,如 331@10@0 头号玩家):绕过 FunIsOpenByIconType
        /// (其 open_lv=999/max_open_day 会拦掉),由调用方(各自功能控制器)负责门判。
        /// 仅供 TopPlayerController 等专属流程使用,不要在 RefreshDefaultIconsCoreAsync 通用扫描里调。
        /// </summary>
        public void AddOwnerIcon(string iconType, int time = 0, string iconTxt = "", string iconImg = null)
        {
            if (string.IsNullOrEmpty(iconType)) return;

            MainUIConfigs.FunctionIconCfg cfg = MainUIConfigs.GetFunctionIconCfg(iconType);
            if (cfg == null) return;

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

        public async Task AddIconAsync(string iconType, int time = 0, string iconTxt = "", string iconImg = null)
        {
            await MainUIConfigs.EnsureLoaded();
            AddIcon(iconType, time, iconTxt, iconImg);
        }

        public async Task RefreshFirstRechargeIconAsync(bool show, string iconTxt = "")
        {
            await MainUIConfigs.EnsureLoaded();
            if (show) AddIcon("159", 0, iconTxt);
            else DeleteIcon("159");
        }

        public async Task RefreshRechargeIconAsync(bool haveFirstRecharge)
        {
            await MainUIConfigs.EnsureLoaded();
            const string normalIcon = "158@0";
            const string multipleIcon = "158@3";
            if (haveFirstRecharge)
            {
                DeleteIcon(normalIcon);
                AddIcon(multipleIcon);
            }
            else
            {
                DeleteIcon(multipleIcon);
                AddIcon(normalIcon);
            }
        }

        public void DeleteIcon(string iconType)
        {
            if (string.IsNullOrEmpty(iconType)) return;
            if (!_iconInfoByType.Remove(iconType)) return;
            EventDispatcher.Emit(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_DELETE, iconType);
        }

        private static bool FunIsOpenByIconType(MainUIConfigs.FunctionIconCfg cfg)
        {
            // 审核服下,need_hide_in_alpha 的图标不显示(对标老端 ActivityIconManager.addIcon:
            // is_alpha && cfg.need_hide_in_alpha → return)。默认 PlatformModel.IsAlpha=false,非审核包行为不变。
            if (PlatformModel.IsAlpha && cfg.NeedHideInAlpha) return false;

            RoleModel role = RoleModel.Instance;
            int level = role.HasBaseInfo ? role.Level : 0;
            int openDay = ServerTimeModel.GetOpenServerDay();
            if (openDay <= 0) openDay = 1;
            if (level < cfg.OpenLv) return false;
            if (openDay < cfg.OpenDay) return false;
            if (cfg.MaxOpenDay != 0 && openDay > cfg.MaxOpenDay) return false;
            if (cfg.OpenTaskId != 0 && TaskModel.Instance.NewestFinishTaskId < cfg.OpenTaskId) return false;
            return true;
        }
    }
}
