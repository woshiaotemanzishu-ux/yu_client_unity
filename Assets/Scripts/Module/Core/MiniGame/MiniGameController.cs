using Shenxiao.Framework.Net;

namespace Shenxiao.Module.Core.MiniGame
{
    public sealed class MiniGameController : BaseController
    {
        public static readonly MiniGameController Instance = new MiniGameController();

        private MiniGameController() { }

        protected override void Register()
        {
            RegisterProtocal(Proto.MINI_GAME_ERROR, On39900);
        }

        private void On39900(NetReader reader)
        {
            MiniGameModel.Instance.ReplaceError(reader.ReadU32(), reader.ReadString());
        }

        public override void Dispose()
        {
            MiniGameModel.Instance.Reset();
            base.Dispose();
        }
    }
}
