using Shenxiao.Framework.Net;
using Shenxiao.Framework.Util;
using Shenxiao.Module.Core.MainUI;

namespace Shenxiao.Module.Core.Svip
{
    /// <summary>
    /// 至尊VIP(SVIP)控制器(对标老客户端 SvipMainController)。进游戏请求 45120;回包据 open_act_id
    /// 增删主界面图标 45120(location_type=5,在 MainUISecondaryView 右竖栏渲染)。点击图标经 MainUIRouter
    /// 路由 "45120" 打开 SvipMainView(面板 prefab 烤好并在 SvipBootstrap 注册 opener 后生效;未烤前落占位)。
    /// </summary>
    public sealed class SvipController : BaseController
    {
        public static readonly SvipController Instance = new SvipController();
        private SvipController() { }

        public const string ICON_TYPE = "45120";

        protected override void Register()
        {
            RegisterProtocal(Proto.SVIP_INFO, On45120);
        }

        public override void Dispose()
        {
            SvipMainModel.Instance.Reset();
            base.Dispose();
        }

        /// <summary>进游戏请求(GameStartController.RequestStartupPackets 调用)。</summary>
        public void RequestSvipInfo()
        {
            SendFmt(Proto.SVIP_INFO);
        }

        // 45120: open_act_id:c, list[u16×{type:h, content_list[u16×{content_id:c}]}]
        private void On45120(NetReader r)
        {
            SvipMainModel m = SvipMainModel.Instance;
            m.Reset();
            m.OpenActId = r.ReadU8();
            int n = r.ReadU16();
            for (int i = 0; i < n; i++)
            {
                var vo = new SvipMainModel.ContentVo { Type = r.ReadU16() };
                int cn = r.ReadU16();
                for (int j = 0; j < cn; j++) vo.ContentIds.Add(r.ReadU8());
                m.ContentList.Add(vo);
            }

            if (m.OpenActId > 0) _ = ActivityIconManager.Instance.AddIconAsync(ICON_TYPE);
            else ActivityIconManager.Instance.DeleteIcon(ICON_TYPE);

            GameLog.Info("Svip", "45120 open_act_id={0} contents={1}", m.OpenActId, m.ContentList.Count);
        }
    }
}
