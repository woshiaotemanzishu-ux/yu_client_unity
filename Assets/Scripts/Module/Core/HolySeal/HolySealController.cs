using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.HolySeal
{
    public sealed class HolySealController : BaseController
    {
        public static readonly HolySealController Instance = new HolySealController();

        private HolySealController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.HOLY_SEAL_ERROR, On65400);
        }

        private void On65400(NetReader reader)
        {
            HolySealModel.Instance.ReplaceError(reader.ReadU32(), reader.ReadString());
        }

        public override void Dispose()
        {
            HolySealModel.Instance.Reset();
            base.Dispose();
        }
    }
}
