using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.HotPoint
{
    public sealed class HotPointController : BaseController
    {
        public static readonly HotPointController Instance = new HotPointController();

        private HotPointController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.HI_POINT_ERROR, On33306);
        }

        private void On33306(NetReader reader)
        {
            HotPointModel.Instance.ReplaceError(reader.ReadU32());
        }

        public override void Dispose()
        {
            HotPointModel.Instance.Reset();
            base.Dispose();
        }
    }
}
