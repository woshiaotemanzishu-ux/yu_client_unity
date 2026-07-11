using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Generated.Config;
using Shenxiao.Module.Core.MainUI;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.Attention
{
    /// <summary>
    /// 关注控制器(对标老客户端 commonController/AttentionController)。管理两个平台/渠道驱动的主界面图标:
    /// 113 = 收藏小程序(通用关注),113113 = 关注(SDK 关注)。**两者均无服务端协议**——老端
    /// RegisterAllProtocals() 为空,全靠平台条件 + native subscribe 回调 + CustomActivity(base_type=70)
    /// 奖励态决定增删,不发/不收任何 pt 协议,故本控制器不 RegisterProtocal 任何协议(仅挂等级变化事件复评)。
    ///
    /// 进游戏(对标老端 GAME_START→ResetData + CheckSdkAttentionOpenState + CheckIconOpen)时评估两图标:
    ///  - 113(通用关注):走 ClientAttention 渠道配置 + attention_open 开关 + 开服天/等级门(见 AttentionModel)。
    ///    有等级门 → 等级变化(EVT_ROLE_INFO_UPDATE)时复评。
    ///  - 113113(SDK 关注):爱疯平台 + native subscribe 开启态 + 奖励未领态。原生无 SDK 桥,恒不显;
    ///    预留 <see cref="OnSdkOpenState"/>(native subscribe 回调)与 <see cref="OnActivityRewardState"/>
    ///    (CustomActivity 奖励态回填)两入口,接桥/接活动奖励态后由平台层/活动层调用即可复评。
    ///
    /// 忠实移植:平台门/SDK 运行态默认原生态值(见 <see cref="AttentionModel"/>),两图标默认都不显示。
    /// 本期只做图标;关注面板/二维码/复制公众号/领奖等 UI 待用户验收。
    /// </summary>
    public sealed class AttentionController : BaseController
    {
        public static readonly AttentionController Instance = new AttentionController();
        private AttentionController() { }

        public const string ICON_TYPE = AttentionModel.ICON_TYPE;
        public const string SDK_ICON_TYPE = AttentionModel.SDK_ICON_TYPE;

        // 复评通用关注(113)的等级去抖:EVT_ROLE_INFO_UPDATE 亦随经验/货币变化触发,只在等级真变时复评。
        private int _lastLevel = -1;

        protected override void Register()
        {
            // 无服务端协议:关注图标全由平台/渠道门 + SDK/活动奖励态驱动(见类注释),故此处不 RegisterProtocal。
            // 对标老端 GAME_START 后随等级变化复评通用关注(113 带 open_lv 等级门)。
            EventDispatcher.On(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
        }

        public override void Dispose()
        {
            EventDispatcher.Off(GlobalEvent.EVT_ROLE_INFO_UPDATE, OnRoleInfoUpdate);
            ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);
            ActivityIconManager.Instance.DeleteIcon(SDK_ICON_TYPE);
            AttentionModel.Instance.Reset();
            _lastLevel = -1;
            base.Dispose();
        }

        /// <summary>
        /// 进游戏入口(GameStartController.RequestStartupPackets 调用,对标老端 GAME_START→
        /// ResetData + CheckSdkAttentionOpenState + CheckIconOpen)。无服务端请求,仅按平台门评估两图标。
        /// </summary>
        public void RequestStartup()
        {
            AttentionModel.Instance.Reset(); // 对标老端 ResetData
            RefreshAttentionIcon();          // 113 通用关注(CheckIconOpen)
            RefreshSdkIcon();                // 113113 SDK 关注(原生无桥,恒不显)
        }

        /// <summary>
        /// native subscribe(apiType=enabled)回调入口(对标老端 CheckSdkAttentionOpenState 的 subscribe 回调
        /// 写 sdk_open_state)。本端预留:接入爱疯 SDK 桥后由平台层调用(open=cpStatus==1),再复评 113113。
        /// </summary>
        public void OnSdkOpenState(bool open)
        {
            AttentionModel.Instance.SetSdkOpenState(open);
            RefreshSdkIcon();
        }

        /// <summary>
        /// CustomActivity(base_type=70,sub_type=1)奖励态回填入口(对标老端
        /// UpdateSdkAttentionActivityRewardState:第 1 个奖励 status==2 → activity_reward_state=true)。
        /// 本端预留:CustomActivity 奖励态移植后由活动层调用(claimed=已领),再复评 113113。
        /// </summary>
        public void OnActivityRewardState(bool claimed)
        {
            AttentionModel.Instance.SetActivityRewardState(claimed);
            RefreshSdkIcon();
        }

        // 通用关注(113):走 AddIconAsync 过配置门(与老端 addIcon 内部 open_lv=90/开服天/审核校验一致)。
        private void RefreshAttentionIcon()
        {
            _ = RefreshAttentionIconAsync();
        }

        private async Task RefreshAttentionIconAsync()
        {
            // attention_open 打开后才需读 ClientAttention 渠道配置(默认 false → 跳过加载,恒不显)。
            if (AttentionModel.AttentionOpen && !ConfigClientAttention.IsLoaded)
                await ConfigClientAttention.LoadAsync();

            bool open = AttentionModel.Instance.GetAttentionIconOpenState();
            if (open) await ActivityIconManager.Instance.AddIconAsync(ICON_TYPE);
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);

            GameLog.Info("Attention", "通用关注(113): open={0} attentionOpen={1}",
                open, AttentionModel.AttentionOpen);
        }

        // SDK 关注(113113):同样走 AddIconAsync 过配置门(对标老端 addIcon);爱疯/SDK/奖励门在 GetSdkIconOpenState。
        private void RefreshSdkIcon()
        {
            bool open = AttentionModel.Instance.GetSdkIconOpenState();
            if (open) _ = ActivityIconManager.Instance.AddIconAsync(SDK_ICON_TYPE);
            else ActivityIconManager.Instance.DeleteIcon(SDK_ICON_TYPE);

            GameLog.Info("Attention", "SDK关注(113113): open={0} aifeng={1} sdkOpen={2} reward={3}",
                open, AttentionModel.IsAiFengGamePlatform,
                AttentionModel.Instance.SdkOpenState, AttentionModel.Instance.ActivityRewardState);
        }

        // 对标老端:主角等级变化复评通用关注(EVT_ROLE_INFO_UPDATE 亦随经验/货币触发,故只在等级真变时复评)。
        private void OnRoleInfoUpdate()
        {
            RoleModel role = RoleModel.Instance;
            if (!role.HasBaseInfo) return;
            if (role.Level == _lastLevel) return;
            _lastLevel = role.Level;
            RefreshAttentionIcon();
        }
    }
}
