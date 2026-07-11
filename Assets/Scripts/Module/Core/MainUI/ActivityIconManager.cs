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
        // 被折进「收纳盒」的成员图标(对标老端 icon_box_info_dic):它们不上活动栏,点开父盒入口才在弹窗里铺。
        private readonly Dictionary<string, IconInfo> _iconBoxInfoByType = new Dictionary<string, IconInfo>();
        private bool _defaultIconsInitialized;
        private Task _refreshDefaultIconsTask;
        // 同步重入保护:默认图标扫描的 foreach 里 AddIcon/DeleteIcon 会同步派发增删事件,
        // 而视图收到事件又会调 RefreshDefaultIconsAsync 重扫。配置已加载时 Core 整段同步执行,
        // _refreshDefaultIconsTask 要等 Core 跑完才赋值 → 那个 task 守卫挡不住同步重入 → 无限递归爆栈。
        // 本标志只在 foreach 同步段为 true,专挡"扫描中事件回调再次触发扫描"。
        private bool _refreshingDefaults;

        private ActivityIconManager() { }

        public IReadOnlyDictionary<string, IconInfo> IconInfoByType => _iconInfoByType;

        public IconInfo GetIconInfo(string iconType)
        {
            if (string.IsNullOrEmpty(iconType)) return null;
            return _iconInfoByType.TryGetValue(iconType, out IconInfo info) ? info : null;
        }

        /// <summary>取被折进收纳盒的成员信息(对标老端 getBoxIconInfo);未收纳返回 null。弹窗判成员显隐用。</summary>
        public IconInfo GetBoxIconInfo(string iconType)
        {
            if (string.IsNullOrEmpty(iconType)) return null;
            return _iconBoxInfoByType.TryGetValue(iconType, out IconInfo info) ? info : null;
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
            // 扫描进行中被增删事件回调再次触发 → 直接跳过(挡同步重入,防爆栈)。
            // 此标志仅在 Core 的 foreach 同步段为 true,不会误挡正常的重扫请求。
            if (_refreshingDefaults) return;

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
            _refreshingDefaults = true;
            try
            {
                foreach (MainUIConfigs.FunctionIconCfg cfg in MainUIConfigs.AllFunctionIconCfg())
                {
                    if (cfg == null || cfg.ControllByOwnFun) continue;
                    if (FunIsOpenByIconType(cfg)) AddIcon(cfg.IconType);
                    else DeleteIcon(cfg.IconType);
                }
                _defaultIconsInitialized = true;
            }
            finally
            {
                _refreshingDefaults = false;
            }
        }

        public void AddIcon(string iconType, int time = 0, string iconTxt = "", string iconImg = null)
        {
            if (string.IsNullOrEmpty(iconType)) return;

            MainUIConfigs.FunctionIconCfg cfg = MainUIConfigs.GetFunctionIconCfg(iconType);
            if (cfg == null || !FunIsOpenByIconType(cfg)) return;

            // 收纳盒分流(对标老端 addIcon 中段):够格的成员图标折进盒、不上活动栏;若被折,直接返回。
            if (TryRouteIntoBox(iconType, time, iconTxt, iconImg, cfg)) return;

            if (_iconInfoByType.TryGetValue(iconType, out IconInfo existing))
            {
                existing.Time = time;
                existing.IconTxt = iconTxt ?? "";
                existing.IconImg = iconImg;
                existing.Data = cfg;
                EventDispatcher.Emit(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_UPDATE, iconType);
            }
            else
            {
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

            // 自身是盒入口(如 100/101):把已在栏上的成员吸进盒(对标老端尾部 if(cfg.is_box) DeleteIconAndIntoBox)。
            if (cfg.IsBox) DeleteIconAndIntoBox(iconType);
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

            // 自管理图标同样走收纳盒分流(老端只有一个 addIcon,box 分支对所有加入路径生效);
            // 331@10@0 等非盒图标 GetBoxIconCfg==null 会直接放行。
            if (TryRouteIntoBox(iconType, time, iconTxt, iconImg, cfg)) return;

            if (_iconInfoByType.TryGetValue(iconType, out IconInfo existing))
            {
                existing.Time = time;
                existing.IconTxt = iconTxt ?? "";
                existing.IconImg = iconImg;
                existing.Data = cfg;
                EventDispatcher.Emit(GlobalEvent.EVT_MAINUI_ACTIVITY_ICON_UPDATE, iconType);
            }
            else
            {
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

            if (cfg.IsBox) DeleteIconAndIntoBox(iconType);
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
            // 对标老端 deleteIcon:栏上 or 盒里任一有它都要删,且两处都清(收纳盒成员也走这里回收)。
            bool had = _iconInfoByType.Remove(iconType);
            bool hadBox = _iconBoxInfoByType.Remove(iconType);
            if (!had && !hadBox) return;
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

            // 2820 特判(对标老端 addIcon:iconType=="2820" && openDay>15 → 只看等级≥160,忽略自身 open_lv/open_day)。
            if (cfg.IconType == "2820" && openDay > 15) return level >= 160;

            if (level < cfg.OpenLv) return false;
            if (openDay < cfg.OpenDay) return false;
            if (cfg.MaxOpenDay != 0 && openDay > cfg.MaxOpenDay) return false;
            if (cfg.OpenTaskId != 0 && TaskModel.Instance.NewestFinishTaskId < cfg.OpenTaskId) return false;
            return true;
        }

        // ---------- 收纳盒(box)聚合状态机(对标老端 ActivityIconManager 的 box 分支) ----------

        /// <summary>
        /// 成员图标够格则折进收纳盒:从栏上撤下、记入 _iconBoxInfoByType、并惰性生成父盒入口图标。
        /// 返回 true=已折进盒(调用方应直接返回,不上栏);false=正常渲染。对标老端 addIcon 中段(:291-314)。
        /// </summary>
        private bool TryRouteIntoBox(string iconType, int time, string iconTxt, string iconImg, MainUIConfigs.FunctionIconCfg cfg)
        {
            MainUIConfigs.BoxIconCfg boxCfg = MainUIConfigs.GetBoxIconCfg(iconType);
            if (boxCfg == null) return false;

            if (!CheckBoxOpenState(boxCfg.AttachIconType))
            {
                RestoreIconFromBox();   // 父盒未开 → 成员回到栏上独立显示(fall through)
                return false;
            }
            if (!NeedInputBox(boxCfg)) return false;   // 够不上放入条件 → 正常渲染

            DeleteIcon(iconType);   // 从栏上/盒里撤下(若在)
            _iconBoxInfoByType[iconType] = new IconInfo
            {
                IconType = iconType,
                Time = time,
                IconTxt = iconTxt ?? "",
                IconImg = iconImg,
                Data = cfg,
            };
            if (GetIconInfo(boxCfg.AttachIconType) == null)
                AddIcon(boxCfg.AttachIconType);   // 惰性生成父盒入口图标(100/101)
            return true;
        }

        /// <summary>成员是否够格「放入盒」(对标老端 NeedInputBox :412-432);全部门槛通过才 true。</summary>
        public bool NeedInputBox(MainUIConfigs.BoxIconCfg cfg)
        {
            if (cfg == null) return false;
            RoleModel role = RoleModel.Instance;
            int level = role.HasBaseInfo ? role.Level : 0;
            int openDay = ServerTimeModel.GetOpenServerDay();
            if (openDay <= 0) openDay = 1;
            if (level < cfg.AttachOpenLv) return false;
            if (openDay < cfg.AttachOpenDay) return false;
            if (cfg.AttachMaxOpenDay != 0 && openDay > cfg.AttachMaxOpenDay) return false;
            if (cfg.AttachOpenTaskId != 0 && TaskModel.Instance.NewestFinishTaskId < cfg.AttachOpenTaskId) return false;
            // 反极性 vip 闸(照抄老端,勿"修正"):VIP≥attach_vip_lv 时该成员反而移出盒(仅 450@1 用)。
            // VipModel.GetVipLevel 未移植 → 降级取 0(v4+ 玩家会把 450@1 留在盒里而非独立显示,极小差异)。
            if (cfg.AttachVipLv != 0 && cfg.AttachVipLv <= GetVipLevelSafe()) return false;
            return true;
        }

        /// <summary>父盒入口本身是否「已开」(对标老端 CheckBoxOpenState :398-410);own_fun 的盒恒视为开。</summary>
        private bool CheckBoxOpenState(string attachIconType)
        {
            MainUIConfigs.FunctionIconCfg cfg = MainUIConfigs.GetFunctionIconCfg(attachIconType);
            if (cfg != null && cfg.IsBox && !cfg.ControllByOwnFun)
            {
                RoleModel role = RoleModel.Instance;
                int level = role.HasBaseInfo ? role.Level : 0;
                int openDay = ServerTimeModel.GetOpenServerDay();
                if (openDay <= 0) openDay = 1;
                if (level < cfg.OpenLv || openDay < cfg.OpenDay
                    || (cfg.MaxOpenDay != 0 && openDay > cfg.MaxOpenDay)
                    || (cfg.OpenTaskId != 0 && TaskModel.Instance.NewestFinishTaskId < cfg.OpenTaskId))
                    return false;
            }
            return true;
        }

        /// <summary>刚加入的图标本身是盒入口 → 把已在栏上的成员吸进盒(对标老端 DeleteIconAndIntoBox :460-473)。</summary>
        private void DeleteIconAndIntoBox(string boxIconType)
        {
            foreach (MainUIConfigs.BoxIconCfg member in MainUIConfigs.BoxIconsForHolder(boxIconType))
            {
                IconInfo info = GetIconInfo(member.IconType);
                if (info != null)
                    AddIcon(member.IconType, info.Time, info.IconTxt, info.IconImg);
            }
        }

        /// <summary>父盒现已关闭的收纳成员放回栏上独立显示(对标老端 RestoreIconFromBox :434-458)。正向进程中几乎不触发。</summary>
        private void RestoreIconFromBox()
        {
            var keys = new List<string>(_iconBoxInfoByType.Keys);
            foreach (string iconType in keys)
            {
                if (!_iconBoxInfoByType.TryGetValue(iconType, out IconInfo iconInfo) || iconInfo == null) continue;
                MainUIConfigs.BoxIconCfg boxCfg = MainUIConfigs.GetBoxIconCfg(iconType);
                if (boxCfg == null) continue;
                if (CheckBoxOpenState(boxCfg.AttachIconType)) continue;   // 盒仍开着,不还原

                string boxIcon = boxCfg.AttachIconType;
                _iconBoxInfoByType.Remove(iconType);

                bool isDelete = true;
                foreach (string other in _iconBoxInfoByType.Keys)
                {
                    MainUIConfigs.BoxIconCfg otherCfg = MainUIConfigs.GetBoxIconCfg(other);
                    if (otherCfg != null && otherCfg.AttachIconType == boxIcon) { isDelete = false; break; }
                }
                if (isDelete && GetBoxIconInfo(boxIcon) != null) DeleteIcon(boxIcon);
                AddIcon(iconType, iconInfo.Time, iconInfo.IconTxt, iconInfo.IconImg);
            }
        }

        // VipModel.GetVipLevel 未移植;仅 450@1(直升v4)读 attach_vip_lv=4。降级取 0(反极性闸不触发)。
        private static int GetVipLevelSafe() => 0;
    }
}
