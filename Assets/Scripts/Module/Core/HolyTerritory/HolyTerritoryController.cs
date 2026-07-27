using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.HolyTerritory
{
    public sealed class HolyTerritoryController : BaseController
    {
        public static readonly HolyTerritoryController Instance = new HolyTerritoryController();

        private HolyTerritoryController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.HOLY_TERRITORY_ERROR, On28300);
        }

        private void On28300(NetReader r)
        {
            HolyTerritoryModel.Instance.SetError(r.ReadU32());
        }

        public override void Dispose()
        {
            HolyTerritoryModel.Instance.Reset();
            base.Dispose();
        }
    }
}
