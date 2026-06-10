namespace Shenxiao.Framework.Net
{
    /// <summary>
    /// Base class for module controllers. Subclasses register protocol handlers in Init().
    /// </summary>
    public abstract class BaseController
    {
        public bool IsInitialized { get; private set; }

        public void Init()
        {
            if (IsInitialized) return;
            Register();
            IsInitialized = true;
        }

        /// <summary>Register protocol handlers via RegisterProtocal.</summary>
        protected abstract void Register();

        protected void RegisterProtocal(int protoId, NetManager.Handler h)
            => NetManager.RegisterProtocal(protoId, h);

        protected void UnregisterProtocal(int protoId)
            => NetManager.UnregisterProtocal(protoId);

        /// <summary>SendFmt(Proto.CS_LOGIN, "ss", acc, pwd).</summary>
        protected void SendFmt(int protoId, string format = null, params object[] args)
            => NetManager.SendFmt(protoId, format, args);
    }
}
