using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.TopPk
{
    public sealed class TopPkController : BaseController
    {
        public static readonly TopPkController Instance = new TopPkController();

        private TopPkController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.TOP_PK_ERROR, On28100);
        }

        private void On28100(NetReader r)
        {
            TopPkModel.Instance.SetError(r.ReadU32(), r.ReadString());
        }

        public override void Dispose()
        {
            TopPkModel.Instance.Reset();
            base.Dispose();
        }
    }
}
