using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.Pray
{
    public sealed class PrayController : BaseController
    {
        public static readonly PrayController Instance = new PrayController();

        private PrayController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.PRAY_ERROR, On41500);
        }

        private void On41500(NetReader reader)
        {
            PrayModel.Instance.ReplaceError(reader.ReadU32());
        }

        public override void Dispose()
        {
            PrayModel.Instance.Reset();
            base.Dispose();
        }
    }
}
