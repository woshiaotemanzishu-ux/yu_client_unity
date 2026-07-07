using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.MainUI
{
    /// <summary>
    /// 主界面功能模型(对标老客户端 yu_client h5/src/commonModel/MainUIModel.ts 的功能图标配置子集)。
    ///
    /// 本类只承载底部功能栏需要的「纯配置 + 开放判定」,把原来散在 MainUIDownView 里的硬编码二维数组/翻面等级/
    /// 开放映射抽出来集中维护(对标 SOP「不要把业务数据存在 View 上」):
    ///   - <see cref="MainFunc"/>        功能枚举       ← MainUIModel.ts 顶层 MainFunc(Role=1..Help=10)
    ///   - <see cref="MainFuncIcons"/>   两行功能图标表 ← Main_Func_Icons
    ///   - <see cref="TurnOpenLv"/>      翻面解锁等级   ← Turn_Open_lv = 65
    ///   - <see cref="GetMainFuncOpenCond"/> 单功能开放判定 ← GetMainFuncOpenCond(委托 FuncOpenConfig)
    ///   - <see cref="GetTurnState"/>    是否可翻面     ← GetTurnState(角色等级 >= Turn_Open_lv)
    ///
    /// 老端 MainUIModel 还混了 buff 多源合并 / 红点中枢 / 事件分发等逻辑,均依赖尚未移植的模块(GoodsModel/
    /// 各业务 Model/RedDotManager…),不在本类范围,留给后续切片。本类是无状态静态配置层,与同目录的
    /// FuncOpenConfig / MainUIRouter 同为 Module.Core.MainUI 下的纯配置/路由工具。业务行为以老端 TS 为准。
    /// </summary>
    public static class MainUIModel
    {
        /// <summary>功能枚举(对标 MainUIModel.ts 的 MainFunc:Role=1 … Help=10,值与老端一致)。</summary>
        public enum MainFunc
        {
            Role = 1,
            Bag = 2,
            Pet = 3,
            Equip = 4,
            Treasure = 5,
            Red = 6,
            Love = 7,
            Guild = 8,
            Composite = 9,
            Help = 10,
        }

        /// <summary>翻面解锁等级(对标 MainUIModel.Turn_Open_lv = 65):角色等级达到才可翻到第二行功能。</summary>
        public const int TurnOpenLv = 65;

        /// <summary>
        /// 功能图标描述(对标 Main_Func_Icons 元素 {func,res,story_arr})。
        /// <para><see cref="Res"/> 既是图标资源名(mainUI atlas),又复用为 <see cref="MainUIRouter"/> 的注册键:
        /// 各功能模块 Bootstrap 用同名短键 Register(如 "role"/"bag"/"232"),所以点击路由直接拿 Res 当 key,
        /// 不再另维护一张 func→viewKey 映射(对标老端 SwitchView 的 func→面板分发,这里收敛成约定键)。</para>
        /// <para>老端的 story_arr(引导手指锚点)依赖 StoryModel,本期未移植,省略。</para>
        /// </summary>
        public readonly struct MainFuncIcon
        {
            public readonly MainFunc Func;
            public readonly string Res;
            public MainFuncIcon(MainFunc func, string res)
            {
                Func = func;
                Res = res;
            }
        }

        /// <summary>
        /// 底部功能图标两行表(对标 Main_Func_Icons;show_type 0/1 两行,等级 ≥65 翻面切换)。
        /// 行/序/res 与老端逐项一致:
        ///   行0: role / bag / pet / equip / treasure
        ///   行1: red / love / guild / composite / 232(Help 图标资源名就是 "232")
        /// </summary>
        public static readonly MainFuncIcon[][] MainFuncIcons =
        {
            new[]
            {
                new MainFuncIcon(MainFunc.Role, "role"),
                new MainFuncIcon(MainFunc.Bag, "bag"),
                new MainFuncIcon(MainFunc.Pet, "pet"),
                new MainFuncIcon(MainFunc.Equip, "equip"),
                new MainFuncIcon(MainFunc.Treasure, "treasure"),
            },
            new[]
            {
                new MainFuncIcon(MainFunc.Red, "red"),
                new MainFuncIcon(MainFunc.Love, "love"),
                new MainFuncIcon(MainFunc.Guild, "guild"),
                new MainFuncIcon(MainFunc.Composite, "composite"),
                new MainFuncIcon(MainFunc.Help, "232"),
            },
        };

        /// <summary>
        /// 任务引导手指的功能图标锚点(对标老端 DoTask 各 case 的 SELECT_STORY_TARGET → StoryTargetName):
        /// task_tips_type → 功能图标路由键(<see cref="MainFuncIcon.Res"/>)。配套 ConfigTaskArrow in_main_ui 的
        /// not_show_in_task_item/front_parent=MainFuncIconItem——箭头挂功能图标而非任务项。
        /// 老端映射:TrainMount(23)/TrainPartner(25)/MountLevel(90) → PetIcon;其余系统页面接入后逐个补
        /// (TrainWing24/TrainArtifact41/ArtifactLevel92 → RoleIcon 等)。
        /// </summary>
        public static string GetGuideIconRes(int taskTipsType)
        {
            switch (taskTipsType)
            {
                case 23:
                case 25:
                case 90:
                    return "pet";
                default:
                    return null;
            }
        }

        /// <summary>
        /// 单功能开放判定(对标 MainUIModel.GetMainFuncOpenCond):
        /// Role/Bag 恒开;其余委托 <see cref="FuncOpenConfig.CheckFuncOpenState"/> 查对应功能视图的
        /// 开服天/等级/前置任务门槛。view 类名逐项对齐老端 GetMainFuncOpenCond 里的 CheckFuncOpenState 参数。
        /// </summary>
        public static bool GetMainFuncOpenCond(MainFunc func)
        {
            switch (func)
            {
                case MainFunc.Role: return true;
                case MainFunc.Bag: return true;
                case MainFunc.Pet: return FuncOpenConfig.CheckFuncOpenState("MountPetView");
                case MainFunc.Equip: return FuncOpenConfig.CheckFuncOpenState("EquipView");
                case MainFunc.Treasure: return FuncOpenConfig.CheckFuncOpenState("SecretTreasureMainView");
                case MainFunc.Red: return FuncOpenConfig.CheckFuncOpenState("RedEnterView");
                case MainFunc.Love: return FuncOpenConfig.CheckFuncOpenState("MarriageBaseView");
                case MainFunc.Guild: return FuncOpenConfig.CheckFuncOpenState("GuildJoinBaseView");
                case MainFunc.Composite: return FuncOpenConfig.CheckFuncOpenState("CompositeView");
                case MainFunc.Help: return FuncOpenConfig.CheckFuncOpenState("GodBefallMainView");
                default: return false;
            }
        }

        /// <summary>是否可翻面(对标 MainUIModel.GetTurnState:角色等级 >= <see cref="TurnOpenLv"/>)。</summary>
        public static bool GetTurnState()
        {
            return RoleModel.Instance.Level >= TurnOpenLv;
        }
    }
}
