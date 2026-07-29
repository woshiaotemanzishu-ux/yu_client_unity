using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Shenxiao.Framework.UI;
using Shenxiao.Module.Core.Common;
using Shenxiao.Module.Core.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Shenxiao.EditorTools
{
    /// <summary>
    /// 批处理 CLI 验证通道:无需 Unity MCP,用
    ///   Unity.exe -batchmode -projectPath . -executeMethod Shenxiao.EditorTools.CliVerify.XXX -logFile Temp/cliverify.log
    /// 驱动「编辑期真机渲染截图法」(同第 8~10 轮 RunCommand harness:临时 Canvas(ScreenSpaceCamera)+RenderTexture
    /// +LayerManager/ViewManager 初始化+CJK 字体强挂),把断言与截图写进 Temp/ 供外部核对。
    /// 结果行统一以 "CLIVERIFY" 前缀写日志;结束经 EditorApplication.Exit 返回进程码(0 过 / 1 异常 / 2 超时 / 3 断言失败)。
    /// 注意:-batchmode 不能加 -nographics(渲染需要 GPU 设备),不加 -quit(由 Exit 收尾)。
    /// </summary>
    public static class CliVerify
    {
        private const string FontPath = "Assets/_App/Fonts/FZYHJW SDF.asset";

        /// <summary>设计分辨率(= Launch.unity 里 CanvasScaler.referenceResolution)。恒为 720×1280,
        /// 与「本次渲染多大」无关——改渲染档位时不要动它,动了等于改设计基准。
        /// ⚠ 真正的上游事实源是 Assets/Scripts/Framework/Config/AppConfig.cs 的 designResolution
        ///   (默认 720×1280),Launch.unity 的 scaler 由 LaunchSceneCreator.cs 从该配置烤出。
        ///   这里为避免在 batch 域加载配置资产(会给默认跑法引入新的失败点)而写成常量,
        ///   因此改了 AppConfig.designResolution / canvasMatch 必须同步改这里,否则验收舞台又会与线上发散。</summary>
        public const int DesignWidth = 720;
        public const int DesignHeight = 1280;

        /// <summary>默认渲染分辨率(基准档)。不传命令行参数时维持 720×1280,历史用例产物逐像素不变。</summary>
        public const int DefaultCaptureWidth = DesignWidth;
        public const int DefaultCaptureHeight = DesignHeight;

        /// <summary>
        /// 五档标准采样(UI 分辨率自适应验收标尺)。改锚定语义 / 重跑转换器重烤 prefab 后,应逐档跑一遍再比对:
        ///   720×1280   基准档(应与改动前逐像素一致)
        ///   1080×2400  主流长屏手机
        ///   750×1334   9:16 短屏
        ///   1280×720   横屏
        ///   1920×1080  PC 宽屏
        /// 跑法(不传参数即基准档):
        ///   Unity.exe -batchmode -projectPath . -executeMethod Shenxiao.EditorTools.CliVerify.XXX
        ///             -cliVerifyWidth 1080 -cliVerifyHeight 2400 -logFile Temp/x_1080x2400.log
        /// 非基准档的截图文件名会自动追加 _宽x高 后缀(见 AppendResolutionSuffix),各档产物不互相覆盖。
        /// </summary>
        public static readonly Vector2Int[] StandardSampleResolutions =
        {
            new Vector2Int(720, 1280),
            new Vector2Int(1080, 2400),
            new Vector2Int(750, 1334),
            new Vector2Int(1280, 720),
            new Vector2Int(1920, 1080),
        };

        private static int _captureWidth = -1;
        private static int _captureHeight = -1;

        /// <summary>本次验收的渲染宽度(命令行 -cliVerifyWidth,缺省 720)。</summary>
        public static int CaptureWidth
        {
            get
            {
                if (_captureWidth < 0)
                {
                    _captureWidth = GetCommandLineInt("-cliVerifyWidth", DefaultCaptureWidth);
                }

                return _captureWidth;
            }
        }

        /// <summary>本次验收的渲染高度(命令行 -cliVerifyHeight,缺省 1280)。</summary>
        public static int CaptureHeight
        {
            get
            {
                if (_captureHeight < 0)
                {
                    _captureHeight = GetCommandLineInt("-cliVerifyHeight", DefaultCaptureHeight);
                }

                return _captureHeight;
            }
        }

        /// <summary>当前是否跑在基准档(720×1280)。</summary>
        public static bool IsDefaultResolution =>
            CaptureWidth == DefaultCaptureWidth && CaptureHeight == DefaultCaptureHeight;

        /// <summary>命令行整数参数解析(沿用 RuntimeUiCaptureTool.GetCommandLineDelaySeconds 的写法:
        /// 顺扫 argv 找 key,取紧随其后的值;缺失/非法/非正数一律回退默认值)。</summary>
        private static int GetCommandLineInt(string key, int fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) &&
                    value > 0)
                {
                    return value;
                }
            }

            return fallback;
        }

        /// <summary>本次渲染档是否属于 StandardSampleResolutions 五档标准采样。
        /// 用于在日志里点名「非标准档」,兜住只传了 -cliVerifyWidth 却漏传 -cliVerifyHeight
        /// 这类footgun(那会渲染出 1080×1280 之类谁也没打算验收的尺寸,却一路静默跑完)。</summary>
        public static bool IsStandardSampleResolution()
        {
            foreach (Vector2Int r in StandardSampleResolutions)
            {
                if (r.x == CaptureWidth && r.y == CaptureHeight)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>非基准档给截图文件名追加 _宽x高 后缀,避免多档截图互相覆盖;基准档保持原名不变,
        /// 历史文档 / 工单里引用的 Temp/roundXX_*.png 路径不受影响。</summary>
        public static string AppendResolutionSuffix(string projectRelativePng)
        {
            if (IsDefaultResolution || string.IsNullOrEmpty(projectRelativePng))
            {
                return projectRelativePng;
            }

            string dir = Path.GetDirectoryName(projectRelativePng);
            string tagged = Path.GetFileNameWithoutExtension(projectRelativePng)
                            + "_" + CaptureWidth + "x" + CaptureHeight
                            + Path.GetExtension(projectRelativePng);
            return string.IsNullOrEmpty(dir) ? tagged : Path.Combine(dir, tagged);
        }

        /// <summary>编译探针:域加载成功即 0(编译错时 executeMethod 根本不会被调用)。</summary>
        public static void CompileCheck()
        {
            Debug.Log("CLIVERIFY COMPILE OK");
            EditorApplication.Exit(0);
        }

        /// <summary>MainUI 技能点击、接敌距离与圆/直线/扇形范围专项。</summary>
        public static void SkillTargeting()
        {
            Run(SkillTargetingCase.Run, 60.0);
        }

        /// <summary>挂机收益信息协议回归。</summary>
        public static void OnHook()
        {
            Run(() => Task.FromResult(OnHookCase.Run()), 60.0);
        }

        public static void DragonBall()
        {
            Run(DragonBallCase.Run, 60.0);
        }

        public static void Armor()
        {
            Run(ArmorCase.Run, 60.0);
        }

        public static void Medal()
        {
            Run(MedalCase.Run, 60.0);
        }

        public static void KfStage()
        {
            Run(KfStageCase.Run, 60.0);
        }

        public static void Reincarnation()
        {
            Run(ReincarnationCase.Run, 60.0);
        }

        public static void GodBeast()
        {
            Run(GodBeastCase.Run, 60.0);
        }

        public static void Designation()
        {
            Run(DesignationCase.Run, 60.0);
        }

        public static void Mask()
        {
            Run(MaskCase.Run, 60.0);
        }

        public static void Dress()
        {
            Run(DressCase.Run, 60.0);
        }

        public static void Demon()
        {
            Run(DemonCase.Run, 60.0);
        }

        public static void DemonTalentPower()
        {
            Run(DemonTalentPowerCase.Run, 60.0);
        }

        public static void DragonWhisper()
        {
            Run(DragonWhisperCase.Run, 60.0);
        }

        public static void TreasureMap()
        {
            Run(TreasureMapCase.Run, 60.0);
        }
        public static void DungeonPartner() { Run(DungeonPartnerCase.Run, 60.0); }
        public static void DungeonCooldown() { Run(DungeonCooldownCase.Run, 60.0); }
        public static void DungeonExpPanel() { Run(DungeonExpPanelCase.Run, 60.0); }
        public static void DungeonInvite() { Run(DungeonInviteCase.Run, 60.0); }
        public static void DungeonInviteState() { Run(DungeonInviteStateCase.Run, 60.0); }
        public static void DungeonDragonBestRecord() { Run(DungeonDragonBestRecordCase.Run, 60.0); }
        public static void DungeonDragonStageReward() { Run(DungeonDragonStageRewardCase.Run, 60.0); }
        public static void DungeonDragonQuickInfo() { Run(DungeonDragonQuickInfoCase.Run, 60.0); }
        public static void DungeonDragonSkillInfo() { Run(DungeonDragonSkillInfoCase.Run, 60.0); }
        public static void DungeonDragonJumpReward() { Run(DungeonDragonJumpRewardCase.Run, 60.0); }
        public static void DungeonAdvancedExpInfo() { Run(DungeonAdvancedExpInfoCase.Run, 60.0); }
        public static void DungeonAdvancedExpJumpInfo() { Run(DungeonAdvancedExpJumpInfoCase.Run, 60.0); }
        public static void DungeonSettingInfo() { Run(DungeonSettingInfoCase.Run, 60.0); }
        public static void DungeonSettingUpdate() { Run(DungeonSettingUpdateCase.Run, 60.0); }
        public static void DungeonInspiritEntry() { Run(DungeonInspiritEntryCase.Run, 60.0); }
        public static void DungeonPolarSpecialInfo() { Run(DungeonPolarSpecialInfoCase.Run, 60.0); }
        public static void DungeonMarriageQuestionState() { Run(DungeonMarriageQuestionStateCase.Run, 60.0); }
        public static void DungeonRuneRewardInfo() { Run(DungeonRuneRewardInfoCase.Run, 60.0); }
        public static void DungeonRuneDailyStatus() { Run(DungeonRuneDailyStatusCase.Run, 60.0); }
        public static void LimitLevelShopGiftConfig() { Run(LimitLevelShopGiftConfigCase.Run, 60.0); }
        public static void SentientAct() { Run(SentientActCase.Run, 60.0); }
        public static void SentientActMonsterProgress() { Run(SentientActMonsterProgressCase.Run, 60.0); }
        public static void BrightSea() { Run(BrightSeaCase.Run, 60.0); }
        public static void SettingWxSubscription() { Run(SettingWxSubscriptionCase.Run, 60.0); }
        public static void SnatchTreasure() { Run(SnatchTreasureCase.Run, 60.0); }
        public static void FriendInviteLevel() { Run(FriendInviteLevelCase.Run, 60.0); }

        public static void Revelation() { Run(RevelationCase.Run, 60.0); }
        public static void Achievement() { Run(AchievementCase.Run, 60.0); }
        public static void Guard() { Run(GuardCase.Run, 60.0); }
        public static void NineSky() { Run(NineSkyCase.Run, 60.0); }

        public static void AttributePotion()
        {
            Run(AttributePotionCase.Run, 120.0);
        }

        /// <summary>P2a 实证:TaskFinishView 用真实 config_task 货币+物品奖励任务渲染,验货币走图标格(非文本)。</summary>
        public static void RenderTaskFinish()
        {
            Run(RenderTaskFinishAsync, 240.0);
        }

        /// <summary>P2b 实证:ItemTipsView 带背包实例渲染,验「使用」按钮按 config use 字段显隐。</summary>
        public static void RenderItemTips()
        {
            Run(RenderItemTipsAsync, 240.0);
        }

        /// <summary>P1 实证:背包增量协议 15017/15018/15008/15009 —— 按 ClientProtocol.json 手工组大端合成包,
        /// 反射调 BagController 私有 handler,断言 BagModel 增/改/删/积分语义(纯逻辑,不渲染)。</summary>
        public static void ProtoDelta()
        {
            Run(() => Task.FromResult(ProtoDeltaCase()), 60.0);
        }

        /// <summary>P1(13轮)实证:TipsManager.Toast 浮动条渲染(多条顶推)+ 生命周期消亡。</summary>
        public static void RenderToast()
        {
            Run(RenderToastAsync, 240.0);
        }

        /// <summary>P1(14轮)实证:DoTask 主线类型覆盖——真实主线任务 + 服务端权威 tipsType,断言各分支日志走向。</summary>
        public static void DoTaskCoverage()
        {
            Run(DoTaskCoverageAsync, 120.0);
        }

        /// <summary>剑魄同修实证:config_companion 同步 + 14202/14205 合成包驱动 PartnerModel + PartnerShellView 渲染。</summary>
        public static void PartnerCase()
        {
            Run(PartnerCaseAsync, 240.0);
        }

        /// <summary>套装收集 15256-15259 数据、出站与轻量 UI 验证。</summary>
        public static void SuitCollect()
        {
            Run(SuitCollectCase.Run, 300.0);
        }

        /// <summary>设置面板 + PK 模式链路实证(SettingCreator 重建 + 10202/13012 合成包 + 双视图渲染断言)。</summary>
        public static void SettingPk()
        {
            Run(SettingPkCase.Run, 300.0);
        }

        /// <summary>灵宠培养页链路实证(PetCreator 快照重建 + 16002/16023 合成包 + 渲染断言)。</summary>
        public static void PetTrain()
        {
            Run(PetTrainCase.Run, 300.0);
        }

        /// <summary>OutWard 坐骑/幻化协议与初始化请求链定向验证。</summary>
        public static void OutWard()
        {
            Run(OutWardCase.Run, 300.0);
        }

        /// <summary>Goods 协议扩容(自动循环 轮1)实证:15000/15002/15019/15026/15053/15055/15090 合成包驱动
        /// BagController 反射喂包,纯逻辑断言(详见 GoodsProtoCase 注释)。</summary>
        public static void GoodsProto()
        {
            Run(GoodsProtoCase.Run, 120.0);
        }

        /// <summary>复活链(自动循环 队列#2 轮2)实证:20013/20004/20009/20017/20022/20027 合成包驱动
        /// FightController/ReliveController 反射喂包,纯逻辑断言(详见 ReliveCase 注释)。</summary>
        public static void Relive()
        {
            Run(ReliveCase.Run, 120.0);
        }

        /// <summary>场景主角混合驱动接线实证(新模型逐动作替换):BuildAsync 混合容器 + MainRoleAgent
        /// 动作出口委托 ReplaceableRoleModel 断言(run 走新/attack 回落老,详见 SceneMixDriverCase 注释)。</summary>
        public static void SceneMixDriver()
        {
            Run(SceneMixDriverCase.Run, 300.0);
        }

        /// <summary>主角升级/任务跑动拖尾/任务跳跃/采集完成特效与挂载门禁专项。</summary>
        public static void RolePresentationEffects()
        {
            Run(RolePresentationEffectsCase.Run, 120.0);
        }

        /// <summary>NPC 对话全面屏底部锚定与任意位置真实 PointerClick 专项。</summary>
        public static void DialogueInteraction()
        {
            Run(DialogueInteractionCase.Run, 120.0);
        }

        /// <summary>任务完成弹层任意位置真实 PointerClick 进入领取/提交语义专项。</summary>
        public static void TaskFinishInteraction()
        {
            Run(TaskFinishInteractionCase.Run, 120.0);
        }

        /// <summary>技能成长线(自动循环 轮3)实证:21001/21010/21011/21012/13008/13010/12093/18401/20006 合成包驱动
        /// SkillController/FightController 反射喂包,纯逻辑断言(详见 SkillGrowthCase 注释)。</summary>
        public static void SkillGrowth()
        {
            Run(SkillGrowthCase.Run, 120.0);
        }

        /// <summary>天赋技能页(技能成长线轮3 3b 单)实证:InnateSkillCreator 装配 + 21010 合成包 + 渲染断言
        /// (_lb_point 文本/技能树 item 数,详见 InnateViewCase 注释)。</summary>
        public static void InnateView()
        {
            Run(InnateViewCase.Run, 300.0);
        }

        /// <summary>装备成长四件套(自动循环 轮4 队列#4)实证:15250/15251(神兵淬炼)、15212/15213/15214/15252
        /// (吞天洗魄)、15255(神屠九炼)、15260/15261(淬炉宗师全身奖励)合成包驱动 EquipSmeltController/
        /// EquipWashController/EquipRefinementController/EquipStrenController 反射喂包,纯逻辑断言
        /// (详见 EquipGrowthCase 注释)。</summary>
        public static void EquipGrowth()
        {
            Run(EquipGrowthCase.Run, 120.0);
        }

        /// <summary>宝石(骸珀镶嵌,自动循环 轮4 下半/4b)实证:15210/15211/15215/15216 合成包驱动
        /// EquipJewelController 反射喂包(含 15216 一键自循环真的能停的断言)+ JewelModule.prefab 渲染断言
        /// 雕刻等级文本(详见 JewelCase 注释)。</summary>
        public static void Jewel()
        {
            Run(JewelCase.Run, 300.0);
        }

        /// <summary>角色成长补全 + 改名 + 转职(自动循环 轮5)实证:13011/13017/13020/13036/13046/
        /// 13080/81/83/42601/13045 合成包驱动 RoleController/TransferJobController 反射喂包,断言
        /// RoleModel/SkillManager 状态 + GlobalEvent;尾段 TransferJobCreator 生成 + 渲染断言卡片数
        /// (详见 RoleGrowthCase 注释)。</summary>
        public static void RoleGrowth()
        {
            Run(RoleGrowthCase.Run, 300.0);
        }

        /// <summary>聊天补全(自动循环 轮6)实证:11001/11002/11023/11027/11028/11029/11042/11046/11050/11064
        /// 合成包驱动 ChatController 反射喂包,断言 ChatModel 分桶/未读/喇叭队列/公告定时器 + SendChat 发送侧预校验
        /// (详见 ChatCase 注释)。</summary>
        public static void Chat()
        {
            Run(ChatCase.Run, 200.0);
        }

        /// <summary>好友+邮件+私聊(自动循环 轮7)实证:14000-14015/14099 + 19002/19003/19005/19501/19502
        /// 合成包驱动 FriendController/MailController 反射喂包,断言分桶/申请去重/在线离线/增量插入移除/亲密度/
        /// 邮件详情缓存/删除过滤/领取背包预检/资料卡字段 + FriendBindUpgrader 自跑装配渲染断言(详见 FriendMailCase 注释)。</summary>
        public static void FriendMail()
        {
            Run(FriendMailCase.Run, 300.0);
        }

        /// <summary>组队(自动循环 轮8)实证:24010/24012/24013/24014/24015/24007/24003/24005 + 24004/24006/
        /// 24008 手写编码序 + 失败码分支合成包驱动 TeamController 反射喂包,断言 TeamModel 状态/成员排序/
        /// 大厅降序/被邀请队列/连锁重拉 + HudTaskTeamCreator 重建渲染断言 HUD 队伍区成员条目
        /// (详见 TeamCase 注释)。</summary>
        public static void Team()
        {
            Run(TeamCase.Run, 300.0);
        }

        /// <summary>副本家族补全一期(自动循环 轮9)实证:61004 尾哨兵/61007+61019 坐标状态机/61018 type 分支/
        /// 61021 共享 vip_count+6100043 专文案/61022 扫荡 32 位 count/61121 资源次数/50801·50802 周本独立
        /// PolarModel + DungeonBuyTimeView 壳渲染(编辑期不可加载则优雅降级;详见 DungeonFamilyCase 注释)。</summary>
        public static void DungeonFamily()
        {
            Run(DungeonFamilyCase.Run, 300.0);
        }

        /// <summary>日常中心(自动循环 轮10)实证:15701 双表分槽+排序算法/15703 升序/15705 成功联动重拉/
        /// 15706 原地改/15717 联动/15718 预约红点计数/15719 成功 status!=2 事件/41900/41903 额度扣减/
        /// 41904 覆盖式/61801 状态表 合成包驱动 DailyController 反射喂包;失败码各一发;
        /// DailyFlow TabSpec 六标签字段(Label/Title/Background/OpenCheck)静态断言(详见 DailyHubCase 注释)。</summary>
        public static void DailyHub()
        {
            Run(DailyHubCase.Run, 300.0);
        }

        /// <summary>创角「整模→视频」迁移实证:整模资源/条目清净 + 剑士视频在位可加载 +
        /// RoleCreateView.prefab VideoImage 结构(详见 CreateRoleVideoCase 注释)。</summary>
        public static void CreateRoleVideo()
        {
            Run(CreateRoleVideoCase.Run, 120.0);
        }

        /// <summary>新模型部件导入(资产管理[替换新模型]泛化)实证——会真实导入 1213 三件套资产
        /// (幂等,重跑=SkipSame)且依赖本机美术工程,故不入 RenderAll(详见 NewPartImportCase 注释)。</summary>
        public static void NewPartImport()
        {
            Run(NewPartImportCase.Run, 900.0);
        }

        /// <summary>商店(自动循环 轮11)实证:15301 分槽+SoldOut已购次数语义+TopVipShop劫持扇出不炸/
        /// 15305 BuyType状态语义/15306 刷新联动/15307 失败包错位兼容(第二字段=Id)/64000 left_time
        /// 服务器墙钟自算/64001 双编码分流(0-7文案表/≥100000显码)/64003 真删/失败码各一发;
        /// ShopFlow 形态④ 11标签结构断言(labels非空/tab3 override命中 ShopVieView)(详见 ShopCase 注释)。</summary>
        public static void Shop()
        {
            Run(ShopCase.Run, 300.0);
        }

        /// <summary>排行榜(自动循环 轮12 #12,纯数据层轮)实证:22100 防御 recv 不炸+显码/22101 分页续拉
        /// (响应驱动非 Update,config rank_max 驱动——wire Sum 位服务端两分支都填请求 Len 回声,不入控制流)
        /// +SelVal 64位尾哨兵+战力榜5页拉满/等级榜20+20+10短尾不误杀+服务端回空终止+未知 rank_type 兜底
        /// 不炸+Start≤0/Len≤0 本地拦截不发包;config_ranking show==1 过滤排序断言(详见 RankCase 注释)。
        /// 本轮 UI prefab 全套不存在,无渲染段。</summary>
        public static void Rank()
        {
            Run(RankCase.Run, 300.0);
        }

        /// <summary>公会核心一期(自动循环 轮13a)实证:pt_400 第1组33活号(40000/05-21/23/27/28/30/31/39/40/42/43/44/
        /// 60-63)合成包驱动 GuildController 反射喂包,断言 40005 批量链/40006 大列表尾哨兵/40008→40009 订正
        /// 单删/40021 权限 Contains 修正/40012 等级门失败码/40013 双错误通道(40000共享壳+自号)/改名链(40043/44)/
        /// 40018 广播recv 双到达同处理/边界各一发;尾段拉起 GuildMainFlow(信息/成员两页)渲染断言,编辑期不可
        /// 加载则优雅降级(不计入通过判定,详见 GuildCoreCase 注释)。</summary>
        public static void GuildCore()
        {
            Run(GuildCoreCase.Run, 300.0);
        }

        /// <summary>公会二期(自动循环 轮13b)实证:pt_401仓库(40100-110)/pt_403宝箱(40300-305)/pt_404协助
        /// (40401-410)/pt_405神像(40500-509)约37活号合成包驱动 GuildController 反射喂包,断言 40302/40301
        /// AutoId 64位尾哨兵(贯穿写入→读回→按id移除+SendFmt字节级验证)/仓库存取链(嵌套装备属性四件套skip
        /// 不误伤尾字段)/40305无公会防御/协助扇出按条处理/神像40502全量刷新(尾字段god_power)+40509 GodId
        /// 8位独例编码宽度/40500共享壳/边界各一发;尾段拉起宝箱tab+仓库弹层渲染断言,编辑期不可加载则优雅
        /// 降级(不计入通过判定,详见 GuildExtCase 注释)。</summary>
        public static void GuildExt()
        {
            Run(GuildExtCase.Run, 300.0);
        }

        /// <summary>Boss家族一期·本服核心(自动循环 轮15a)实证:46000段主链(列表/进入判定/46009订正后
        /// 类型门/关注/击杀日志100条上限/掉落日志/体力/结算奖励/伤害榜自己+前3防抖广播/连杀通知/死亡debuff
        /// 转发ReliveModel/消耗复活/46045 code字段订正)+ 免战(20201-205)+ 采集(20025-26)+ 死号未注册断言
        /// (代表性子集)+ 边界失败码各一发,合成包驱动 BossController 反射喂包;跨服族(47000-035/47101-117/
        /// 61900-902)全不测(留15b),纯数据层轮无渲染段(BossHelpPanelView/BossHelpItemView 已接真数据链
        /// 但宿主 BossFightSceneView 仍是死分支,详见 BossCase 注释)。</summary>
        public static void Boss()
        {
            Run(BossCase.Run, 300.0);
        }

        /// <summary>Boss家族二期·跨服族(自动循环 轮15b)实证:pt_470千幻蜃楼主链(列表/进出场景/关注/
        /// 47006复活提醒含服务端误发壳量quirk/47008防御recv/宝箱坐标·狩猎等级·榜单/死亡debuff转
        /// ReliveModel.HolyBoss/复活)+ pt_471镇煞封魂全链(主信息/进出购买/关注/复活提醒/掉落/排名全量与
        /// patch/双套奖励结算/场景信息/47117防御recv)+ pt_619论剑恩怨簿(61900-902,含32位ServerId独例)+
        /// pt_460内kf_great_demon壳(46037-39/46046)+ Mystery=20订正后类型门回归+ 死号未注册断言
        /// (47001/47011)+ 边界失败码各一发,合成包驱动 KfBossController 反射喂包;纯数据层轮无渲染段
        /// (CrossServerEnterView 7Tab壳与千幻蜃楼视图均无 Bind 供给,详见 KfBossCase 注释)。</summary>
        public static void KfBoss()
        {
            Run(KfBossCase.Run, 300.0);
        }

        /// <summary>婚姻(征友/戒指/结婚,自动循环 轮16)实证:pt_172 172xx(征友17200-05/戒指17210-13/求婚·
        /// 结婚·离婚·秀恩爱17222-40/副本匹配邀请17245-97)+ 223xx 鲜花(22300-05)共 33 号合成包驱动
        /// MarriageController 反射喂包,断言 MarriageModel 落地字段/事件 + config 四表计数;17212 死号仅
        /// 断言防御 recv 且无公开发送方法;纯数据层轮无渲染段(14 个 View 已烤 Bind 但空壳,同 15a/15b Boss
        /// 先例本轮不接 View,详见 MarriageCase 注释)。</summary>
        public static void Marriage()
        {
            Run(MarriageCase.Run, 300.0);
        }

        /// <summary>轮17 自定义活动框架核心(33100-33108):列表落地/增删/通用详情33104/通用领奖33105/
        /// 全服计数/批量转发+NetManager注册线核实;纯数据层轮无渲染段。</summary>
        public static void CustomActCore()
        {
            Run(CustomActCoreCase.Run, 300.0);
        }

        /// <summary>轮17 P2 抽奖A(自选奖励/许愿池/天命转盘/百抽,15号)。</summary>
        public static void CustomActLotteryA()
        {
            Run(CustomActLotteryACase.Run, 300.0);
        }

        /// <summary>轮17 P3 抽奖B(扭蛋/鉴宝2/在线抽/寻宝/招财猫/绑玉祈愿,14号)。</summary>
        public static void CustomActLotteryB()
        {
            Run(CustomActLotteryBCase.Run, 300.0);
        }

        /// <summary>轮17 P4 节日族(摇钱树/节日活跃/赛博夺宝/绑钻转盘/红包雨/神圣召唤,20号)。</summary>
        public static void CustomActFestival()
        {
            Run(CustomActFestivalCase.Run, 300.0);
        }

        /// <summary>轮17 P5 商业礼包族(0元/投资/vip礼包/问卷/红包返利/总览/获奖记录/159xx充值统计等,~22号)。</summary>
        public static void CustomActBiz()
        {
            Run(CustomActBizCase.Run, 300.0);
        }

        /// <summary>轮17 P6 跨服+榜(跨服团购/TopPlayer补全/跨服鲜花榜/消费榜,~13号)。</summary>
        public static void CustomActKfRank()
        {
            Run(CustomActKfRankCase.Run, 300.0);
        }

        /// <summary>轮18 PK1 谪仙临凡(pt_440,16号,含44000二层嵌套/44016·44018变长C2S)。</summary>
        public static void GodBefall()
        {
            Run(GodBefallCase.Run, 300.0);
        }

        /// <summary>轮18 PK2 三小合包(光环514/仙灵直购513/公会红包339,14号)。</summary>
        public static void CheapTrio()
        {
            Run(CheapTrioCase.Run, 300.0);
        }

        /// <summary>轮18 PK3 首杀首通(pt_188,type收口分发)+祭典宝录(pt_194补全),13号。</summary>
        public static void FbFestival()
        {
            Run(FbFestivalCase.Run, 300.0);
        }

        /// <summary>轮18 PK4 福利余量(签到/下载/在线/心悦/成长41722/战力)+广告独立归属,15号。</summary>
        public static void Welfare()
        {
            Run(WelfareCase.Run, 300.0);
        }

        /// <summary>轮18 PK5 场景散件(pt_120,22号,12024保游标/12089·12091双端死不注册)。</summary>
        public static void SceneMisc()
        {
            Run(SceneMiscCase.Run, 300.0);
        }

        /// <summary>轮19 Market 交易行数据层(pt_151,16号补全;死号15103-05/07/10/13封存;open_lv=90服务端静默门)。</summary>
        public static void Market()
        {
            Run(MarketCase.Run, 300.0);
        }

        /// <summary>轮20 跨天/整点事件源(10201 驱动的 DAY_CHANGE/HOUR_REFRESH;含裁决1 订正老端 truthy bug、
        /// 裁决2 服务器时区、裁决5 10000 只对时不触发、ErlangParser 护栏、config_key_value + 41708 明细)。</summary>
        public static void ServerClock()
        {
            Run(ServerClockCase.Run, 300.0);
        }

        /// <summary>轮21 look_over 角色资料卡(module1:19501 上行 + 19502 落地 + 自查拦截 + 面板)。</summary>
        public static void LookOver()
        {
            Run(LookOverCase.Run, 300.0);
        }

        /// <summary>轮21 Fashion 时装数据层第一刀(41300/41301/41302/41303/41304/41306/41312/41316 + 41311 收)。</summary>
        public static void Fashion()
        {
            Run(FashionCase.Run, 300.0);
        }

        /// <summary>ListDuobao 33252/33253/33803 数据链及七个业务 Bind 组件验证。</summary>
        public static void ListDuobao()
        {
            Run(ListDuobaoCase.Run, 300.0);
        }

        /// <summary>轮21 协议覆盖率核验(防虚假完工):A总量防倒退/B家族防倒退/C完工家族零未申报/
        /// D双注册/E族错误出口。基线 Schemas/ProtocolCoverage/baseline.json,报告落 Reports/(已 gitignore)。</summary>
        public static void ProtocolCoverage()
        {
            Run(ProtocolCoverageCase.Run, 300.0);
        }

        /// <summary>轮22 PK1 公会晚宴数据层(pt_402 主体,26号:公会BOSS 40201/03/04/08/09 + 晚宴主流程
        /// 40211/12/14/17/20/21/22 + 篝火/答题/龙魂/菜肴 40255/56/57/58/59/60/62/64/65/66/67 + 族错误出口
        /// 40200)合成包驱动 GuildActivityController 反射喂包,断言 GuildActivityModel 落地字段/事件 +
        /// config 六项计数 + 40214 尾哨兵字节游标核对 + 40218/40261/40263 死号断言(详见 GuildActivityCase 注释)。</summary>
        public static void GuildActivity()
        {
            Run(GuildActivityCase.Run, 300.0);
        }

        public static void GuildGuardEnter()
        {
            Run(GuildGuardEnterCase.Run, 60.0);
        }

        public static void GuildFightEnter()
        {
            Run(GuildFightEnterCase.Run, 60.0);
        }

        public static void Kf1vnExit()
        {
            Run(Kf1vnExitCase.Run, 60.0);
        }
        /// <summary>诸天王者 62100 原始活动信息与阶段变化查询回归。</summary>
        public static void Kf1vnActivityInfo()
        {
            Run(Kf1vnActivityInfoCase.Run, 60.0);
        }

        /// <summary>轮23 PK1 星宿核心数据层(pt_232 23200-23209/23250-23257,17号)合成包驱动
        /// StarEquipController 反射喂包,断言 StarEquipModel 落地字段/事件 + config 17 表计数 +
        /// 23201/23250 尾哨兵字节游标核对 + 23204 killlist 死号断言(详见 StarEquipCase 注释)。
        /// 星宿锻造(chc/StarForge,PK2,23210-23241)独立 StarForgeCase.cs,由主控收口挂。</summary>
        public static void StarEquip()
        {
            Run(StarEquipCase.Run, 300.0);
        }

        /// <summary>轮23 PK2 星宿锻造(chc,pt_232 兜底转发段 23210-23241,12号:强化/进化/
        /// 觉醒[服务端叫附魔]/启灵四子系统 + 强化·附魔大师)。含 23231 失败分支服务端误发 23211 的
        /// 容忍性断言与嵌套数组尾哨兵核对(详见 StarForgeCase 注释)。</summary>
        public static void StarForge()
        {
            Run(StarForgeCase.Run, 300.0);
        }

        /// <summary>轮24 PB 婚宴数据层(pt_172 172xx 扩壳,22个接收活号:17250/51/52/53/57/58/59/60/61/62/
        /// 65/66/67/70/71/72/75/76/77/78/79/98)合成包驱动 BanquetController 反射喂包,断言 BanquetModel 落地
        /// 字段/事件 + config 13 表计数 + 17252 尾哨兵字节游标核对 + killlist(17254/55/69/73/74/80-94)死号
        /// 断言(详见 BanquetCase 注释)。PI 幻化全链(pt_160,轮24 同批)收工前复核:PI 扩既有 OutWardCase.cs
        /// 而非新建 Case(类注释自述"扩既有 OutWardCase 而非新建 OutWardIllusionCase"),该文件本就已挂在
        /// RenderAll(`int o = await OutWardCase.Run()`)——PI 的新增断言随扩容自动纳入既有挂钩,本次
        /// PB 收工前确认无需为 PI 额外补四点。</summary>
        public static void Banquet()
        {
            Run(BanquetCase.Run, 300.0);
        }

        /// <summary>侍魂装备 16014-16017 数据链、四容器库存与三页可操作 UI 闭环验证。</summary>
        public static void PetEquip()
        {
            Run(async () =>
            {
                int pe = await PetEquipCase.Run();
                int pi = await PetEquipInventoryCase.Run();
                int pu = await PetEquipUiCase.Run();
                return pe != 0 ? pe : pi != 0 ? pi : pu;
            }, 300.0);
        }

        /// <summary>侍魂装备 Base/背包/强化/打造三页 Creator 与真实模型筛选定向验证。</summary>
        public static void PetEquipUI()
        {
            Run(PetEquipUiCase.Run, 300.0);
        }

        /// <summary>宝宝 182xx 的 15 个老端可达数据/操作协议、启动级联与结果合并验证。</summary>
        public static void Baby()
        {
            Run(BabyCase.Run, 300.0);
        }

        /// <summary>神纹熔炉 18105/18112 启动和时间刷新闭环。</summary>
        public static void Lung()
        {
            Run(LungCase.Run, 60.0);
        }

        public static void GhostWalk()
        {
            Run(GhostWalkCase.Run, 60.0);
        }

        public static void TSCrack()
        {
            Run(TSCrackCase.Run, 60.0);
        }

        public static void Eternity()
        {
            Run(EternityCase.Run, 60.0);
        }

        public static void HolyBattle()
        {
            Run(HolyBattleCase.Run, 60.0);
        }

        public static void MondaysAward()
        {
            Run(MondaysAwardCase.Run, 60.0);
        }

        public static void NoonParty()
        {
            Run(NoonPartyCase.Run, 60.0);
        }

        public static void Deposit()
        {
            Run(DepositCase.Run, 60.0);
        }

        public static void MonBook()
        {
            Run(MonBookCase.Run, 60.0);
        }
        public static void KfSingleRank()
        {
            Run(KfSingleRankCase.Run, 60.0);
        }
        public static void JjcTimes()
        {
            Run(JjcTimesCase.Run, 60.0);
        }

        public static void TopPk()
        {
            Run(TopPkCase.Run, 60.0);
        }

        public static void HolyTerritory()
        {
            Run(HolyTerritoryCase.Run, 60.0);
        }
        public static void HotPoint()
        {
            Run(HotPointCase.Run, 60.0);
        }
        public static void MiniGame()
        {
            Run(MiniGameCase.Run, 60.0);
        }
        public static void Pray()
        {
            Run(PrayCase.Run, 60.0);
        }
        public static void HolySeal()
        {
            Run(HolySealCase.Run, 60.0);
        }
        public static void VipWelfareCard()
        {
            Run(VipWelfareCardCase.Run, 60.0);
        }
        public static void JjcRecords()
        {
            Run(JjcRecordsCase.Run, 60.0);
        }

        /// <summary>全部用例(一次 Unity 启动跑完;任一失败进程码非 0)。</summary>
        public static void RenderAll()
        {
            Run(async () =>
            {
                int p = ProtoDeltaCase();
                int d = await DoTaskCoverageAsync();
                int e = await PartnerCaseAsync();
                int s = await SuitCollectCase.Run();
                int g = await RushGiftCase.Run();
                int o = await OutWardCase.Run();
                int t = await TempleAwakenCase.Run();
                int q = await EquipStrenCase.Run();
                int u = await GuBaoCase.Run();
                int j = await GuildJoinCase.Run();
                int n = await RuneCase.Run();
                int v = await DungeonCase.Run();
                int w = await ThinSliceCase.Run();
                int f = await FinalSliceCase.Run();
                int a = await RenderTaskFinishAsync();
                int b = await RenderItemTipsAsync();
                int c = await RenderToastAsync();
                int k = await SettingPkCase.Run();
                int m = await PetTrainCase.Run();
                int gp = await GoodsProtoCase.Run();
                int rl = await ReliveCase.Run();
                int sg = await SkillGrowthCase.Run();
                int iv = await InnateViewCase.Run();
                int eg = await EquipGrowthCase.Run();
                int jw = await JewelCase.Run();
                int rg = await RoleGrowthCase.Run();
                int ch = await ChatCase.Run();
                int fm = await FriendMailCase.Run();
                int tm = await TeamCase.Run();
                int df = await DungeonFamilyCase.Run();
                int dh = await DailyHubCase.Run();
                int cv = await CreateRoleVideoCase.Run();
                int sh = await ShopCase.Run();
                int rk = await RankCase.Run();
                int gc = await GuildCoreCase.Run();
                int ge = await GuildExtCase.Run();
                int bs = await BossCase.Run();
                int kb = await KfBossCase.Run();
                int mr = await MarriageCase.Run();
                int md = await SceneMixDriverCase.Run();
                int c0 = await CustomActCoreCase.Run();
                int c2 = await CustomActLotteryACase.Run();
                int c3 = await CustomActLotteryBCase.Run();
                int c4 = await CustomActFestivalCase.Run();
                int c5 = await CustomActBizCase.Run();
                int c6 = await CustomActKfRankCase.Run();
                int g1 = await GodBefallCase.Run();
                int g2 = await CheapTrioCase.Run();
                int g3 = await FbFestivalCase.Run();
                int g4 = await WelfareCase.Run();
                int g5 = await SceneMiscCase.Run();
                int mk = await MarketCase.Run();
                int sc = await ServerClockCase.Run();
                int lo = await LookOverCase.Run();
                int fa = await FashionCase.Run();
                int ld = await ListDuobaoCase.Run();
                int pc = await ProtocolCoverageCase.Run();
                int ga = await GuildActivityCase.Run();
                int se = await StarEquipCase.Run();
                int sf = await StarForgeCase.Run();
                int bq = await BanquetCase.Run();
                int pe = await PetEquipCase.Run();
                int pi = await PetEquipInventoryCase.Run();
                int pu = await PetEquipUiCase.Run();
                int by = await BabyCase.Run();
                int lu = await LungCase.Run();
                int db = await DragonBallCase.Run();
                int ap = await AttributePotionCase.Run();
                int ar = await ArmorCase.Run();
                int me = await MedalCase.Run();
                int ks = await KfStageCase.Run();
                int re = await ReincarnationCase.Run();
                int gb = await GodBeastCase.Run();
                int ds = await DesignationCase.Run();
                int ma = await MaskCase.Run();
                int dr = await DressCase.Run();
                int dm = await DemonCase.Run();
                int dmp = await DemonTalentPowerCase.Run();
                int rv = await RevelationCase.Run();
                int ac = await AchievementCase.Run();
                int gd = await GuardCase.Run();
                int ns = await NineSkyCase.Run();
                int gw = await GhostWalkCase.Run();
                int tc = await TSCrackCase.Run();
                int et = await EternityCase.Run();
                int hb = await HolyBattleCase.Run();
                int mwa = await MondaysAwardCase.Run();
                int np = await NoonPartyCase.Run();
                int dp = await DepositCase.Run();
                int mb = await MonBookCase.Run();
                int ksr = await KfSingleRankCase.Run();
                int jjc = await JjcTimesCase.Run();
                int jjcr = await JjcRecordsCase.Run();
                int bsea = await BrightSeaCase.Run();
                int wxsub = await SettingWxSubscriptionCase.Run();
                int st = await SnatchTreasureCase.Run();
                int fil = await FriendInviteLevelCase.Run();
                Debug.Log("CLIVERIFY ALL protoDelta=" + p + " dotask=" + d + " partner=" + e
                    + " suitclt=" + s + " rushgift=" + g + " outward=" + o + " snatchtreasure=" + st + " friendinvitelevel=" + fil + " wxsub=" + wxsub
                    + " templeawaken=" + t + " equipstren=" + q + " gubao=" + u
                    + " guildjoin=" + j + " rune=" + n + " dungeon=" + v + " thinslice=" + w + " finalslice=" + f
                    + " taskfinish=" + a + " itemtips=" + b + " toast=" + c + " settingpk=" + k + " pettrain=" + m
                    + " goodsproto=" + gp + " relive=" + rl + " skillgrowth=" + sg + " innateview=" + iv
                    + " equipgrowth=" + eg + " jewel=" + jw + " rolegrowth=" + rg + " chat=" + ch + " friendmail=" + fm
                    + " team=" + tm + " dungeonfam=" + df + " dailyhub=" + dh + " createvideo=" + cv + " shop=" + sh
                    + " rank=" + rk + " guildcore=" + gc + " guildext=" + ge + " boss=" + bs + " kfboss=" + kb + " marriage=" + mr
                    + " mixdriver=" + md
                    + " customactcore=" + c0 + " calotteryA=" + c2 + " calotteryB=" + c3 + " cafestival=" + c4
                    + " cabiz=" + c5 + " cakfrank=" + c6
                    + " godbefall=" + g1 + " cheaptrio=" + g2 + " fbfestival=" + g3 + " welfare=" + g4 + " scenemisc=" + g5
                    + " market=" + mk + " serverclock=" + sc
                    + " lookover=" + lo + " fashion=" + fa + " listduobao=" + ld + " protocolcoverage=" + pc + " guildactivity=" + ga
                    + " starequip=" + se + " starforge=" + sf + " banquet=" + bq
                    + " petequip=" + pe + " petequipinventory=" + pi + " petequipui=" + pu + " baby=" + by + " lung=" + lu + " dragonball=" + db + " attributepotion=" + ap + " armor=" + ar + " medal=" + me + " kfstage=" + ks + " reincarnation=" + re + " godbeast=" + gb + " designation=" + ds + " mask=" + ma + " dress=" + dr + " demon=" + dm + " demontalentpower=" + dmp + " revelation=" + rv + " achievement=" + ac + " guard=" + gd + " ninesky=" + ns + " ghostwalk=" + gw + " tscrack=" + tc + " eternity=" + et + " holybattle=" + hb + " mondaysaward=" + mwa + " noonparty=" + np + " deposit=" + dp + " monbook=" + mb + " kfsinglerank=" + ksr + " brightsea=" + bsea);
                foreach (int r in new[] { p, d, e, s, g, o, t, q, u, j, n, v, w, f, a, b, c, k, m, gp, rl, sg, iv, eg, jw, rg, ch, fm, tm, df, dh, cv, sh, rk, gc, ge, bs, kb, mr, md, c0, c2, c3, c4, c5, c6, g1, g2, g3, g4, g5, mk, sc, lo, fa, ld, pc, ga, se, sf, bq, pe, pi, pu, by, lu, db, ap, ar, me, ks, re, gb, ds, ma, dr, dm, dmp, rv, ac, gd, ns, gw, tc, et, hb, mwa, np, dp, mb, ksr, jjc, jjcr, bsea, st, fil, wxsub })
                    if (r != 0) return r;
                return 0;
            }, 1500.0);
        }

        private static async Task<int> DoTaskCoverageAsync()
        {
            await TaskConfigs.EnsureLoaded();
            var logs = new List<string>();
            Application.LogCallback cb = (msg, stack, type) => logs.Add(msg);
            Application.logMessageReceived += cb;
            try
            {
                // (真实主线任务 id, 服务端权威 tipsType(data_task.erl get_content), 期望分支日志)
                (int taskId, int tips, string expect)[] cases =
                {
                    (100940, 27, "DoTask degrade"),   // LV 到达等级 → 升级提醒未移植降级
                    (100980, 9,  "DungeonRuneShellView"), // FinDunType → 已接真实入口(第19轮;编辑期无层时「无法构建」同含类名)
                    (100330, 23, "PetFlow"), // TrainMount → 真页 MountPet 页签窗(本轮起 23/25/90 → PetFlow,壳仅剩 24/92/41)
                    (100010, 37, "Welcome(37) 无动作"), // 对标老端空 case
                    (999999, 99, "未知类型"),          // 不在主线清单 → 未知 blocker
                };
                bool all = true;
                foreach ((int taskId, int tips, string expect) c in cases)
                {
                    logs.Clear();
                    var vo = new TaskVo(c.taskId, c.tips, "", 0, 0, 1, 0, 1, 0, 0, 0, 0);
                    vo.ApplyConfig(TaskConfigs.Get(c.taskId));
                    TaskModel.Instance.DoTask(vo);
                    bool hit = logs.Exists(l => l.Contains(c.expect));
                    Debug.Log("CLIVERIFY dotask task=" + c.taskId + " tips=" + c.tips + " expect=[" + c.expect + "] hit=" + hit);
                    if (!hit) all = false;
                }
                return all ? 0 : 3;
            }
            finally
            {
                Application.logMessageReceived -= cb;
            }
        }

        /// <summary>剑魄同修实证:config_companion 同步 + 14202(全量列表)/14205(培养成功/失败)合成包
        /// 反射喂 PartnerController 私有 handler,断言 PartnerModel 数据;再拉起 PartnerShellView 渲染断言
        /// 培养按钮显隐 + 行文案含真实"N阶M星"。</summary>
        private static async Task<int> PartnerCaseAsync()
        {
            Stage stage = Stage.Create();
            try
            {
                Shenxiao.EditorTools.ConfigGen.ClientConfigSync.SyncIfStale(true);
                await Shenxiao.Module.Core.Partner.PartnerConfigs.EnsureLoaded();
                if (!Shenxiao.Module.Core.Partner.PartnerConfigs.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY FAIL config_companion not loaded");
                    return 3;
                }

                object ctrl = Shenxiao.Module.Core.Partner.PartnerController.Instance;
                const System.Reflection.BindingFlags F =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                System.Reflection.MethodInfo m14202 = ctrl.GetType().GetMethod("On14202", F);
                System.Reflection.MethodInfo m14205 = ctrl.GetType().GetMethod("On14205", F);
                if (m14202 == null || m14205 == null)
                {
                    Debug.LogError("CLIVERIFY partner handlers missing (reflection)");
                    return 3;
                }
                void Feed(System.Reflection.MethodInfo m, byte[] pkt) =>
                    m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

                Shenxiao.Module.Core.Partner.PartnerModel model = Shenxiao.Module.Core.Partner.PartnerModel.Instance;

                // 14202 全量:fight_id + sum_attr[] + companion_list[单项]
                byte[] p14202 = new Pkt()
                    .I(0)      // fight_id
                    .H(0)      // sum_attr 计数
                    .H(1)      // companion_list 计数
                        .I(1)      // companion_id
                        .H(1)      // stage
                        .H(1)      // star
                        .H(0)      // biog_list 计数
                        .C(1)      // is_active
                        .C(0)      // is_fight
                        .I(1018)   // figure_id
                        .I(0)      // blessing
                        .I(0)      // train_num
                        .H(0)      // attr 计数
                        .L(100)    // combat
                    .Bytes();
                Feed(m14202, p14202);
                bool listOk = model.HasData && model.Companions.Count == 1
                    && model.Get(1) != null && model.Get(1).Star == 1 && model.Get(1).Combat == 100;
                Debug.Log("CLIVERIFY partner 14202 hasData=" + model.HasData + " count=" + model.Companions.Count
                    + " star=" + (model.Get(1)?.Star ?? -1) + " combat=" + (model.Get(1)?.Combat ?? -1) + " ok=" + listOk);

                // 14205 培养成功:errcode=1 + companion_id + stage + star=2 + blessing=10(升星→内部会 SendFmt 14201,未连接只 warn,无害)
                byte[] p14205Ok = new Pkt().I(1).I(1).H(1).H(2).I(10).Bytes();
                Feed(m14205, p14205Ok);
                bool trainOk = model.Get(1) != null && model.Get(1).Star == 2 && model.Get(1).Blessing == 10;
                Debug.Log("CLIVERIFY partner 14205 ok star=" + (model.Get(1)?.Star ?? -1)
                    + " blessing=" + (model.Get(1)?.Blessing ?? -1) + " ok=" + trainOk);

                // 14205 培养失败:errcode=5,只要不抛异常(走 toast log 分支)即过
                byte[] p14205Fail = new Pkt().I(5).I(1).H(1).H(2).I(0).Bytes();
                bool failNoThrow = true;
                try { Feed(m14205, p14205Fail); }
                catch (System.Exception e) { failNoThrow = false; Debug.LogError("CLIVERIFY partner 14205 fail threw: " + e); }
                Debug.Log("CLIVERIFY partner 14205 fail noThrow=" + failNoThrow);

                Shenxiao.Module.Core.Partner.PartnerShellView.Show();
                await Task.Delay(400);
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round15_partner_shell.png");

                Transform trainBtn = FindDeep(stage.CanvasRoot, "Btn培养");
                bool trainBtnOk = trainBtn != null && trainBtn.gameObject.activeInHierarchy;
                Transform row0 = FindDeep(stage.CanvasRoot, "Row0");
                TMP_Text rowLabel = row0 != null ? row0.GetComponentInChildren<TMP_Text>(true) : null;
                bool rowOk = rowLabel != null && !string.IsNullOrEmpty(rowLabel.text) && rowLabel.text.Contains("1阶2星");
                Debug.Log("CLIVERIFY partner shell rowOk=" + rowOk + " trainBtn=" + trainBtnOk + " shot=" + png);

                bool pass = listOk && trainOk && failNoThrow && trainBtnOk && rowOk;
                Debug.Log("CLIVERIFY partner VERDICT listOk=" + listOk + " trainOk=" + trainOk
                    + " failNoThrow=" + failNoThrow + " trainBtnOk=" + trainBtnOk + " rowOk=" + rowOk + " pass=" + pass);

                Shenxiao.Module.Core.Partner.PartnerShellView.Close();
                Shenxiao.Module.Core.Partner.PartnerModel.Instance.Clear();
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        // ---- 渲染用例 ----

        private static async Task<int> RenderTaskFinishAsync()
        {
            Stage stage = Stage.Create();
            try
            {
                await TaskConfigs.EnsureLoaded();
                await GoodsModel.EnsureLoaded();
                if (!TaskConfigs.IsLoaded || !GoodsModel.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY FAIL config not loaded: task=" + TaskConfigs.IsLoaded + " goods=" + GoodsModel.IsLoaded);
                    return 3;
                }

                // 真实任务 100520:award_list=[{5,0,2500000},{3,0,10000},{0,17020001,1},{0,17020004,1}]
                //  → 经验(5→32)/金币(3→31)两个货币 + 两件真实物品,共 4 格。
                const int taskId = 100520;
                TaskConfigs.TaskCfg cfg = TaskConfigs.Get(taskId);
                if (cfg == null)
                {
                    Debug.LogError("CLIVERIFY FAIL config_task missing " + taskId);
                    return 3;
                }

                var vo = new TaskVo(taskId, 0, "", 1, 0, 1, 1, 1, 0, 0, 0, 0);
                vo.ApplyConfig(cfg);
                List<TaskReward.Entry> rewards = TaskReward.Build(vo.SpecialGoodsList, vo.AwardList, 1);
                Debug.Log("CLIVERIFY rewards=" + rewards.Count + " -> " + TaskReward.ToText(rewards, " / "));

                var view = new TaskFinishView();
                view.Open(vo);

                // 等 prefab 实例化 + 奖励格生成 + 图标异步加载(编辑期兜底导入可能较慢)。
                EquipmentItem[] cells = await WaitCells(stage.CanvasRoot, rewards.Count, 90.0);
                await Task.Delay(3000);   // 再等几拍:逐帧实例化的尾部格子/迟到图标(排除截图早于建格的竞态)
                cells = stage.CanvasRoot.GetComponentsInChildren<EquipmentItem>(false);
                // 按格子 GameObject 去重断言:格数 == 奖励数,且每格恰 1 个 EquipmentItem
                // (>1 = 嵌套 prefab 自带 + 回填 added-override 历史重复件,BindClick 双注册回归)。
                var roots = new Dictionary<GameObject, int>();
                foreach (EquipmentItem c in cells)
                    roots[c.gameObject] = roots.TryGetValue(c.gameObject, out int n) ? n + 1 : 1;
                int dupCells = 0, iconOk = 0;
                foreach (KeyValuePair<GameObject, int> kv in roots)
                {
                    if (kv.Value > 1) dupCells++;
                    var bind = (Shenxiao.Generated.UI.Common.EquipmentItemBind)kv.Key.GetComponent<EquipmentItem>();
                    bool ok = bind.icon != null && bind.icon.enabled && bind.icon.sprite != null;
                    if (ok) iconOk++;
                    Debug.Log("CLIVERIFY cell path=" + HierarchyPath(kv.Key.transform) + " comps=" + kv.Value
                        + " icon=" + (ok ? bind.icon.sprite.name : "<none>")
                        + " count=" + (bind.num_text != null && bind.num_text.gameObject.activeSelf ? bind.num_text.text : "-"));
                }

                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round11_taskfinish_currency.png");
                Debug.Log("CLIVERIFY shot=" + png);

                bool pass = roots.Count == rewards.Count && rewards.Count >= 4 && iconOk == roots.Count && dupCells == 0;
                Debug.Log("CLIVERIFY VERDICT cells=" + roots.Count + "/" + rewards.Count + " iconOk=" + iconOk
                    + " dupCompCells=" + dupCells + " pass=" + pass);
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        private static async Task<int> RenderItemTipsAsync()
        {
            Stage stage = Stage.Create();
            try
            {
                await GoodsModel.EnsureLoaded();
                if (!GoodsModel.IsLoaded)
                {
                    Debug.LogError("CLIVERIFY FAIL config_goods not loaded");
                    return 3;
                }

                // 真实 config 物品 520100(type 52 / use=1 → 走默认 15050 分支,使用按钮应显示)。
                const int typeId = 520100;
                GoodsModel.GoodsBasic basic = GoodsModel.GetGoodsBasicByTypeId(typeId);
                if (basic == null || basic.Use == 0)
                {
                    Debug.LogError("CLIVERIFY FAIL 520100 缺失或 use==0: " + (basic == null ? "null" : basic.Use.ToString()));
                    return 3;
                }

                // 合成背包实例仅供渲染断言(GoodsId 非服务端真值,不点使用、不发协议);typeId/数量走真实 config 展示链路。
                var goods = new Shenxiao.Module.Core.Bag.BagGoods { GoodsId = 1, TypeId = typeId, GoodsNum = 3, Color = basic.Color };
                ItemTipsView.Show(goods);
                await Task.Delay(1500);   // 图标/底板异步加载

                Transform useBtn = FindDeep(stage.CanvasRoot, "Use");
                bool useVisible = useBtn != null && useBtn.gameObject.activeInHierarchy;
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round11_itemtips_use.png");
                Debug.Log("CLIVERIFY tips item=" + basic.Name + " useBtnVisible=" + useVisible + " shot=" + png);

                ItemTipsView.Close();
                return useVisible ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        private static async Task<int> RenderToastAsync()
        {
            Stage stage = Stage.Create();
            try
            {
                // prefab 版 TipToastView 由 MonoBehaviour.Update 驱动,编辑期 batchmode 无 player loop 不 tick
                // (实测第一条永卡 Born、队列不放行):本用例强制走代码建树兜底(Task.Yield 驱动,无头可跑);
                // prefab 路径的动画/排队以 2026-07-06 用户实测手调为准,不在无头断言范围。
                typeof(Shenxiao.Common.Tips.TipsManager)
                    .GetField("_prefabMissing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.SetValue(null, true);

                Shenxiao.Common.Tips.TipsManager.Toast("使用成功");
                Shenxiao.Common.Tips.TipsManager.Toast("获得V1体验卡x1");

                // TipToast prefab 化(2026-07-06)后条目=TipToastItem 克隆挂 ViewManager 层(不在 stage.CanvasRoot 下),
                // 且排队"同刻仅一条 Born"→第二条晚 ~bornDuration 入场:改为全场景数活动条目,轮询等两条同时在场。
                // 兼容 prefab 缺失的代码建树兜底(节点名 Toast)。
                static (int live, bool textOk) CountToasts()
                {
                    int n = 0;
                    bool ok = true;
                    foreach (var item in UnityEngine.Object.FindObjectsByType<Shenxiao.Common.Tips.TipToastItem>(FindObjectsSortMode.None))
                    {
                        n++;
                        TMP_Text tx = item.GetComponentInChildren<TMP_Text>(false);
                        if (tx == null || string.IsNullOrEmpty(tx.text)) ok = false;
                    }
                    foreach (var t in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                        if (t.gameObject.name == "Toast") { n++; if (string.IsNullOrEmpty(t.text)) ok = false; }
                    return (n, ok);
                }

                int live = 0;
                bool textOk = false;
                double bothDeadline = EditorApplication.timeSinceStartup + 3.0;
                while (EditorApplication.timeSinceStartup < bothDeadline)
                {
                    (live, textOk) = CountToasts();
                    if (live >= 2) break;
                    await Task.Delay(100);
                }
                stage.ForceCjkFont();
                string png = stage.Capture("Temp/round13_toast.png");
                Debug.Log("CLIVERIFY toast live=" + live + " textOk=" + textOk + " shot=" + png);

                // 生命周期:轮询到全部消亡(编辑期 tick 慢,给足余量)
                double deadline = EditorApplication.timeSinceStartup + 30.0;
                int remain = live;
                while (EditorApplication.timeSinceStartup < deadline)
                {
                    (remain, _) = CountToasts();
                    if (remain == 0) break;
                    await Task.Delay(300);
                }
                Debug.Log("CLIVERIFY toast expiredRemain=" + remain);

                bool pass = live == 2 && textOk && remain == 0;
                return pass ? 0 : 3;
            }
            finally
            {
                stage.Dispose();
            }
        }

        // ---- 协议增量用例(合成包驱动) ----

        private static int ProtoDeltaCase()
        {
            var bag = Shenxiao.Module.Core.Bag.BagModel.Instance;
            bag.Clear();
            object ctrl = Shenxiao.Module.Core.Bag.BagController.Instance;
            const System.Reflection.BindingFlags F =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            System.Reflection.MethodInfo m17 = ctrl.GetType().GetMethod("On15017", F);
            System.Reflection.MethodInfo m18 = ctrl.GetType().GetMethod("On15018", F);
            System.Reflection.MethodInfo m08 = ctrl.GetType().GetMethod("On15008", F);
            System.Reflection.MethodInfo m09 = ctrl.GetType().GetMethod("On15009", F);
            if (m17 == null || m18 == null || m08 == null || m09 == null)
            {
                Debug.LogError("CLIVERIFY proto handlers missing (reflection)");
                return 3;
            }
            void Feed(System.Reflection.MethodInfo m, byte[] pkt) =>
                m.Invoke(ctrl, new object[] { new Shenxiao.Framework.Net.NetReader(pkt, 0, pkt.Length) });

            // 15017 新增(全字段项,嵌套数组空)
            Feed(m17, Goods17(4, 9001, 520100, 5));
            bool add = bag.BagGoodsList.Count == 1 && bag.BagGoodsList[0].GoodsNum == 5
                       && bag.BagGoodsList[0].TypeId == 520100 && bag.BagGoodsList[0].Cell == 7;
            // 15018 数量 5→2
            Feed(m18, Num18(4, 9001, 2, 520100));
            bool chg = bag.BagGoodsList.Count == 1 && bag.BagGoodsList[0].GoodsNum == 2;
            // 15018 num=0 删除
            Feed(m18, Num18(4, 9001, 0, 520100));
            bool del = bag.BagGoodsList.Count == 0;
            // 15008 特殊积分单条
            Feed(m08, new Pkt().I(1001).I(777).Bytes());
            bool score = bag.GetSpecialScore(1001) == 777;
            // 15009 全量重建(旧 777 应被清)
            Feed(m09, new Pkt().H(2).I(1001).I(5).I(2002).I(9).Bytes());
            bool scoreList = bag.SpecialScores.Count == 2 && bag.GetSpecialScore(1001) == 5 && bag.GetSpecialScore(2002) == 9;
            // 15017 非背包 pos → 跳过不落
            Feed(m17, Goods17(1, 9002, 520100, 3));
            bool skip = bag.BagGoodsList.Count == 0;

            // 15021 出售回包读序烟测(res=1 + 1 项所得;UI 层未起 → toast 走 log-only,不炸即过)
            System.Reflection.MethodInfo m21 = ctrl.GetType().GetMethod("On15021", F);
            bool sell = m21 != null;
            if (sell) Feed(m21, new Pkt().I(1).H(1).I(520100).I(3).Bytes());

            Debug.Log("CLIVERIFY proto delta add=" + add + " chg=" + chg + " del=" + del
                + " score=" + score + " scoreList=" + scoreList + " skipPos=" + skip + " sell21=" + sell);
            bag.Clear();
            return (add && chg && del && score && scoreList && skip && sell) ? 0 : 3;
        }

        /// <summary>15017 包:pos:h + 1 项全字段(字段序照 ClientProtocol.json "15010"/"15017",嵌套数组计数 0)。</summary>
        private static byte[] Goods17(int pos, long gid, int typeId, long num)
        {
            return new Pkt().H(pos).H(1)
                .L(gid).I(typeId).C(0).H(7).I(num)      // goods_id/type_id/sub_pos/cell=7/goods_num
                .C(0).C(0).C(0).C(0).C(3)               // bind/trade/sell/is_drop/color
                .I(0).I(0).H(0).H(0).I(0).I(0)          // expire/combat/stren/level/rating/overall
                .H(0)                                   // addition_attrlist[]
                .H(0)                                   // equip_extra_attr[]
                .C(0).C(0).I(0).C(0)                    // equipStage/equipStar/skill_id/skill_lv
                .H(0)                                   // awake_list[]
                .Bytes();
        }

        /// <summary>15018 包:pos:h + 1 项 {goods_id:l, goods_num:i, type_id:i}。</summary>
        private static byte[] Num18(int pos, long gid, long num, int typeId)
        {
            return new Pkt().H(pos).H(1).L(gid).I(num).I(typeId).Bytes();
        }

        /// <summary>大端手工组包(与 NetReader 字节序一致:h=u16, i=u32, l=两个 u32 拼 64, c=u8)。</summary>
        public sealed class Pkt
        {
            private readonly List<byte> _b = new List<byte>();
            public Pkt C(int v) { _b.Add((byte)v); return this; }
            public Pkt H(int v) { _b.Add((byte)(v >> 8)); _b.Add((byte)v); return this; }
            public Pkt I(long v) { _b.Add((byte)(v >> 24)); _b.Add((byte)(v >> 16)); _b.Add((byte)(v >> 8)); _b.Add((byte)v); return this; }
            public Pkt L(long v) { I((v >> 32) & 0xFFFFFFFF); I(v & 0xFFFFFFFF); return this; }
            /// <summary>'s':u16 字节长 + UTF8(对标 NetReader.ReadString / UserMsgAdapter 's' 格式)。</summary>
            public Pkt S(string v)
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(v ?? string.Empty);
                H(bytes.Length);
                _b.AddRange(bytes);
                return this;
            }
            public byte[] Bytes() { return _b.ToArray(); }
        }

        public static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        public static string HierarchyPath(Transform t)
        {
            var sb = new System.Text.StringBuilder(t.name);
            while (t.parent != null) { t = t.parent; sb.Insert(0, t.name + "/"); }
            return sb.ToString();
        }

        /// <summary>轮询场景中激活的奖励格(TaskFinishView 内部逐帧实例化+异步图标)。</summary>
        private static async Task<EquipmentItem[]> WaitCells(Transform root, int expect, double timeoutSec)
        {
            double deadline = EditorApplication.timeSinceStartup + timeoutSec;
            EquipmentItem[] found = Array.Empty<EquipmentItem>();
            while (EditorApplication.timeSinceStartup < deadline)
            {
                found = root.GetComponentsInChildren<EquipmentItem>(false);
                if (found.Length >= expect)
                {
                    bool allIcons = true;
                    foreach (EquipmentItem c in found)
                    {
                        var bind = (Shenxiao.Generated.UI.Common.EquipmentItemBind)c;
                        if (bind.icon == null || !bind.icon.enabled || bind.icon.sprite == null) { allIcons = false; break; }
                    }
                    if (allIcons) return found;
                }
                await Task.Delay(200);
            }
            return found;
        }

        // ---- 驱动与舞台 ----

        /// <summary>batch 模式 async 驱动:EditorApplication.update 泵到任务完成/超时,经 Exit 返回进程码。</summary>
        private static void Run(Func<Task<int>> body, double timeoutSec)
        {
            Task<int> task = null;
            double deadline = EditorApplication.timeSinceStartup + timeoutSec;
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                try
                {
                    if (task == null) task = body();
                    if (task.IsCompleted)
                    {
                        EditorApplication.update -= tick;
                        int code = task.IsFaulted ? 1 : task.Result;
                        if (task.IsFaulted) Debug.LogError("CLIVERIFY EXCEPTION " + task.Exception);
                        Debug.Log("CLIVERIFY EXIT " + code);
                        EditorApplication.Exit(code);
                    }
                    else if (EditorApplication.timeSinceStartup > deadline)
                    {
                        EditorApplication.update -= tick;
                        Debug.LogError("CLIVERIFY TIMEOUT");
                        EditorApplication.Exit(2);
                    }
                }
                catch (Exception e)
                {
                    EditorApplication.update -= tick;
                    Debug.LogError("CLIVERIFY EXCEPTION " + e);
                    EditorApplication.Exit(1);
                }
            };
            EditorApplication.update += tick;
        }

        /// <summary>临时渲染舞台(空场景 + RenderTexture 相机 + ScreenSpaceCamera Canvas + 层/视图管理器)。
        /// 渲染分辨率取 CaptureWidth/CaptureHeight(命令行 -cliVerifyWidth/-cliVerifyHeight,缺省 720×1280);
        /// CanvasScaler 的设计分辨率恒为 720×1280,靠 scaler 把设计尺寸缩放到实际渲染尺寸。</summary>
        public sealed class Stage : IDisposable
        {
            public Transform CanvasRoot => _canvas.transform;

            private Camera _cam;
            private Canvas _canvas;
            private RenderTexture _rt;

            public static Stage Create()
            {
                // batch 域 Addressables 操作不推进(KeyExists 挂死)→ 兜底优先(AssetDatabase 同步命中)。
                Shenxiao.Framework.Res.ResManager.EditorPreferFallback = true;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var s = new Stage();
                int renderW = CaptureWidth;
                int renderH = CaptureHeight;
                if (!IsDefaultResolution)
                {
                    // 基准档不打这行,保证默认跑法的日志与改动前逐行一致(避免误伤按日志断言的用例)。
                    string tierTag = IsStandardSampleResolution()
                        ? "标准采样档"
                        : "非标准档!不在五档标准采样内,确认是否漏传 -cliVerifyWidth / -cliVerifyHeight 之一";
                    Debug.Log("CLIVERIFY RESOLUTION " + renderW + "x" + renderH +
                              " (非基准档,截图名带分辨率后缀;" + tierTag + ")");
                }

                s._rt = new RenderTexture(renderW, renderH, 24);

                var camGo = new GameObject("CliVerifyCam");
                s._cam = camGo.AddComponent<Camera>();
                s._cam.clearFlags = CameraClearFlags.SolidColor;
                s._cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
                s._cam.targetTexture = s._rt;

                var canvasGo = new GameObject("CliVerifyCanvas");
                s._canvas = canvasGo.AddComponent<Canvas>();
                s._canvas.renderMode = RenderMode.ScreenSpaceCamera;
                s._canvas.worldCamera = s._cam;
                s._canvas.planeDistance = 1f;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                // ⚠ 以下五项必须与 Assets/_App/Scenes/Launch.unity 的 CanvasScaler 保持一致,否则一旦跑非基准分辨率,
                //   验收舞台与线上的缩放算法就会发散 —— 截图「过了」而真机是歪的,验收失真。
                //   Launch.unity 实测值:m_UiScaleMode=1(ScaleWithScreenSize)、m_ReferenceResolution={720,1280}、
                //   m_ScreenMatchMode=1、m_MatchWidthOrHeight=0.5、m_ReferencePixelsPerUnit=100;
                //   生成源同为 Assets/Editor/Bootstrap/LaunchSceneCreator.cs(那里显式写了 Expand)。
                //   m_ScreenMatchMode 数值含义:0=MatchWidthOrHeight / 1=Expand / 2=Shrink。
                //   历史上这里漏设 screenMatchMode,取到默认值 0(MatchWidthOrHeight),与线上的 1(Expand)不符。
                //   补设在基准档无影响:720×1280 渲染 720×1280 设计时两算法都得 scaleFactor=1(Expand 取 min(1,1)=1,
                //   MatchWidthOrHeight 取 2^Lerp(log2(1),log2(1),0.5)=2^0=1),故基准档产物不变。
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(DesignWidth, DesignHeight);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                scaler.matchWidthOrHeight = 0.5f;
                scaler.referencePixelsPerUnit = 100f;
                canvasGo.AddComponent<GraphicRaycaster>();

                var lm = new LayerManager();
                lm.Init(s._canvas);
                ViewManager.Init(lm);
                return s;
            }

            /// <summary>batch 域没有场景字体环境,渲染前把 CJK SDF 强挂到全部 TMP 文本(同第 8~10 轮做法)。</summary>
            public void ForceCjkFont()
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                if (font == null)
                {
                    Debug.LogWarning("CLIVERIFY font missing: " + FontPath);
                    return;
                }
                foreach (TMP_Text t in _canvas.GetComponentsInChildren<TMP_Text>(true))
                {
                    t.font = font;
                    t.ForceMeshUpdate();
                }
            }

            public string Capture(string projectRelativePng)
            {
                Canvas.ForceUpdateCanvases();
                _cam.Render();
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = _rt;
                var tex = new Texture2D(_rt.width, _rt.height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;

                // 非基准档自动加 _宽x高 后缀,各档截图不互相覆盖(基准档仍是原文件名)。
                string full = Path.GetFullPath(AppendResolutionSuffix(projectRelativePng));
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllBytes(full, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                return full;
            }

            public void Dispose()
            {
                ViewManager.Init(null);
                if (_cam != null) { _cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(_cam.gameObject); }
                if (_canvas != null) UnityEngine.Object.DestroyImmediate(_canvas.gameObject);
                if (_rt != null) UnityEngine.Object.DestroyImmediate(_rt);
            }
        }
    }
}
