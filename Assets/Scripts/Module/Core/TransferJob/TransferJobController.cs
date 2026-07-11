using Shenxiao.Common.Tips;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.Role;

namespace Shenxiao.Module.Core.TransferJob
{
    /// <summary>
    /// 转职协议控制器(对标老端 commonController/RoleController.ts 里的 13045 分支;轮5 拆成独立模块,
    /// 与老端结构不同但更贴合本仓库"一功能一控制器"的既有拆分惯例——13046(转职冷却)仍挂
    /// <see cref="Role.RoleController"/>(归 GAME_START 裸发族,见其 RequestGrowthPackets),
    /// 本控制器只管 13045 转职确认本体)。
    /// </summary>
    public sealed class TransferJobController : BaseController
    {
        public static readonly TransferJobController Instance = new TransferJobController();
        private TransferJobController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.TRANSFER_JOB_CHANGE, On13045);
        }

        /// <summary>请求转职(对标 TransferJobCardItem.ts sureCbk 二次确认后 SendFmtToGame(13045,"cc",career,sex))。</summary>
        public void RequestTransfer(int career, int sex)
        {
            SendFmt(Proto.TRANSFER_JOB_CHANGE, "cc", career, sex);
            GameLog.Info("TransferJob", "send 13045 转职请求 career={0} sex={1}", career, sex);
        }

        /// <summary>13045 回包 "isCC"(error_code:i, args:s, career:c, sex:c)。==1 → 更新
        /// Figure.career/sex + Emit EVT_CAREER_CHANGED + 级联重拉 13080/13046/21002(对标老端
        /// MainRoleVo.changeCareer);OutwardChangedView(外观变更通用展示窗,与换装/结婚共用)未移植,仅 log TODO。
        /// else → 显码降级(服务端 err130_* 系列,未枚举具体文案,退回通用格式)。</summary>
        private void On13045(NetReader r)
        {
            int errorCode = (int)r.ReadU32();
            string args = r.ReadString();
            int career = r.ReadU8();
            int sex = r.ReadU8();
            GameLog.Info("TransferJob", "recv 13045 errorCode={0} args={1} career={2} sex={3}",
                errorCode, args, career, sex);

            if (errorCode != 1)
            {
                TipsManager.Toast("转职失败(" + errorCode + (string.IsNullOrEmpty(args) ? "" : ("," + args)) + ")");
                return;
            }

            RoleModel m = RoleModel.Instance;
            if (m.Figure != null)
            {
                m.Figure.career = (byte)career;
                m.Figure.sex = (byte)sex;
            }
            TipsManager.Toast("转职成功");
            EventDispatcher.Emit(GlobalEvent.EVT_CAREER_CHANGED, career, sex);
            EventDispatcher.Emit(GlobalEvent.EVT_ROLE_INFO_UPDATE);
            GameLog.Info("TransferJob", "转职成功,级联重拉 13080/13046/21002(对标老端 changeCareer);" +
                "OutwardChangedView(外观变更通用展示窗)未移植,TODO 补对应 UI");

            // 级联重拉(对标老端 MainRoleVo.changeCareer:REQUEST_ROLE_PROTO 13080/13046 +
            // SkillManager.Fire(REQUEST_CCMD_EVENT,21002))。ResManager.ResetStaticSprite3DUrlDic 对应的
            // 模型/贴图缓存重建机制本端未有等价实现,角色模型刷新走既有 EVT_ROLE_INFO_UPDATE 订阅链自然重建。
            RoleController.Instance.RequestHeadList();
            RoleController.Instance.RequestTransferCooldown();
            Skill.SkillController.Instance.RequestSkillList();
        }
    }
}
