using System.Collections.Generic;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.AutoBrush;
using Shenxiao.Module.Core.AutoFight;
using Shenxiao.Module.Core.Bag;
using Shenxiao.Module.Core.Chat;
using Shenxiao.Module.Core.CycleimpActlist;
using Shenxiao.Module.Core.CustomActivity;
using Shenxiao.Module.Core.Daily;
using Shenxiao.Module.Core.Dialogue;
using Shenxiao.Module.Core.FirstRecharge;
using Shenxiao.Module.Core.Friend;
using Shenxiao.Module.Core.FunctionOpen;
using Shenxiao.Module.Core.Gm;
using Shenxiao.Module.Core.Mail;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Compose;
using Shenxiao.Module.Core.Dungeon;
using Shenxiao.Module.Core.Equip;
using Shenxiao.Module.Core.Festival;
using Shenxiao.Module.Core.Jjc;
using Shenxiao.Module.Core.OnHook;
using Shenxiao.Module.Core.GuBao;
using Shenxiao.Module.Core.Guild;
using Shenxiao.Module.Core.Notice;
using Shenxiao.Module.Core.OutWard;
using Shenxiao.Module.Core.Partner;
using Shenxiao.Module.Core.Rank;
using Shenxiao.Module.Core.Role;
using Shenxiao.Module.Core.Rune;
using Shenxiao.Module.Core.RushGift;
using Shenxiao.Module.Core.SuitCollect;
using Shenxiao.Module.Core.Scene;
using Shenxiao.Module.Core.Setting;
using Shenxiao.Module.Core.Shop;
using Shenxiao.Module.Core.Skill;
using Shenxiao.Module.Core.SurpriseGift;
using Shenxiao.Module.Core.Svip;
using Shenxiao.Module.Core.TempleAwaken;
using Shenxiao.Module.Core.Tasks;
using Shenxiao.Module.Core.Team;
using Shenxiao.Module.Core.TransferJob;
using Shenxiao.Module.Core.Vip;
using Shenxiao.Module.Core.WeekCard;
using Shenxiao.Module.Core.Compete;
using Shenxiao.Module.Core.Attention;
using Shenxiao.Module.Core.Market;
using Shenxiao.Module.Core.LimitLevelShop;
using Shenxiao.Module.Core.Eyou;
using Shenxiao.Module.Core.Boss;
using Shenxiao.Module.Core.Marriage;
using Shenxiao.Module.Core.ActivityForeshow;
using Shenxiao.Module.Core.Banquet;
using Shenxiao.Module.Core.Kaifu;
using Shenxiao.Module.Core.AddVipService;
using Shenxiao.Module.Core.DiamondFight;
using Shenxiao.Module.Core.Kf1vn;
using Shenxiao.Module.Core.SeaHegemony;
using Shenxiao.Module.Core.KfHolyArea;
using Shenxiao.Module.Core.Lung;
using Shenxiao.Module.Core.BaseDungeon;
using Shenxiao.Module.Core.GrowthBenefits;
using Shenxiao.Module.Core.FriendInvite;
using Shenxiao.Module.Core.TopVip;
using Shenxiao.Module.Core.DragonBall;
using Shenxiao.Module.Core.SevenDay;
using Shenxiao.Module.Core.PushGift;
using Shenxiao.Module.Core.Adventure;
using Shenxiao.Module.Core.Relive;
using Shenxiao.Module.Core.GodBefall;
using Shenxiao.Module.Core.Halo;
using Shenxiao.Module.Core.FairyWish;
using Shenxiao.Module.Core.RedPacket;
using Shenxiao.Module.Core.FirstBlood;
using Shenxiao.Module.Core.Welfare;
using Shenxiao.Module.Core.AdReward;

namespace Shenxiao.Module.Core.Game
{
    /// <summary>
    /// 游戏内控制器注册中心:进游戏统一 Init、断线/登出统一 Dispose。
    /// 新增游戏内模块 = 在 ALL 里加一行(对标老客户端各 commonController 的集中注册)。
    /// 登录控制器(LoginController)不在此列——它在登录链就绪、跨越进游戏边界,单独管理。
    /// </summary>
    public static class ControllerHub
    {
        private static readonly BaseController[] ALL =
        {
            GameStartController.Instance,
            RoleController.Instance,
            TransferJobController.Instance,
            SceneController.Instance,
            TaskController.Instance,
            DialogueController.Instance,
            AutoBrushController.Instance,
            AutoFightController.Instance,
            GmCheatController.Instance,
            NoticeController.Instance,
            MailController.Instance,
            FriendController.Instance,
            TeamController.Instance,
            FunctionOpenController.Instance,
            VipController.Instance,
            FirstRechargeController.Instance,
            WeekCardController.Instance,
            FestivalController.Instance,
            SurpriseGiftController.Instance,
            CompeteController.Instance,
            AttentionController.Instance,
            MarketController.Instance,
            LimitLevelShopController.Instance,
            EyouController.Instance,
            BossController.Instance,
            KfBossController.Instance,
            MarriageController.Instance,
            ActivityForeshowController.Instance,
            BanquetController.Instance,
            KaifuController.Instance,
            AddVipServiceController.Instance,
            DiamondFightController.Instance,
            Kf1vnController.Instance,
            SeaHegemonyController.Instance,
            KfHolyAreaController.Instance,
            LungController.Instance,
            BaseDungeonController.Instance,
            GrowthBenefitsController.Instance,
            FriendInviteController.Instance,
            TopVipController.Instance,
            DragonBallController.Instance,
            SevenDayController.Instance,
            PushGiftController.Instance,
            AdventureController.Instance,
            BagController.Instance,
            SkillController.Instance,
            FightController.Instance,
            ReliveController.Instance,
            CollectController.Instance,
            ChatController.Instance,
            CustomActivityController.Instance,
            TopPlayerController.Instance,
            CycleimpActlistController.Instance,
            SvipController.Instance,
            PartnerController.Instance,
            SuitCollectController.Instance,
            RushGiftController.Instance,
            OutWardController.Instance,
            TempleAwakenController.Instance,
            EquipStrenController.Instance,
            EquipSmeltController.Instance,
            EquipWashController.Instance,
            EquipRefinementController.Instance,
            GuBaoController.Instance,
            GuildJoinController.Instance,
            GuildController.Instance,
            RuneController.Instance,
            DungeonController.Instance,
            EquipStoneController.Instance,
            EquipWearController.Instance,
            EquipJewelController.Instance,
            OnHookController.Instance,
            BagFusionController.Instance,
            ComposeController.Instance,
            JjcController.Instance,
            PkStatusController.Instance,
            SettingController.Instance,
            DailyController.Instance,
            ShopController.Instance,
            RankController.Instance,
            // ----- 便宜活批(自动循环 轮18)已注册:PK1-PK4 各包已补协议,Register() 均已实做(非空壳) -----
            GodBefallController.Instance,
            HaloController.Instance,
            FairyWishController.Instance,
            RedPacketController.Instance,
            FirstBloodController.Instance,
            WelfareController.Instance,
            CombatWelfareController.Instance,
            AdRewardController.Instance,
            // 后续:EquipController ...
        };

        public static bool Initialized { get; private set; }

        public static void InitAll()
        {
            if (Initialized) return;

            foreach (BaseController c in ALL) c.Init();
            Initialized = true;
            GameLog.Info("Game", "游戏内控制器已注册: {0} 个", ALL.Length);
        }

        public static void DisposeAll()
        {
            foreach (BaseController c in ALL) c.Dispose();
            Initialized = false;
            GameLog.Info("Game", "游戏内控制器已注销(断线/登出)");
        }
    }
}
