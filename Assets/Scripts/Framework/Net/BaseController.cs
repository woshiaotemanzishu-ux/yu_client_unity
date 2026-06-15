using System.Collections.Generic;

namespace Shenxiao.Framework.Net
{
    /// <summary>
    /// 业务模块控制器基类:子类在 Register() 里 RegisterProtocal 注册本段协议回调。
    /// 进游戏由 ControllerHub 统一 Init();断线/登出统一 Dispose()(注销回调、可重入)。
    /// 对标老客户端 commonController/*.ts —— 一模块一控制器。
    /// </summary>
    public abstract class BaseController
    {
        public bool IsInitialized { get; private set; }
        private readonly List<int> _registered = new List<int>();

        public void Init()
        {
            if (IsInitialized) return;
            Register();
            IsInitialized = true;
        }

        /// <summary>注销本控制器注册过的所有协议回调,重置为未初始化(可再次 Init)。</summary>
        public virtual void Dispose()
        {
            if (!IsInitialized) return;
            foreach (int id in _registered) NetManager.UnregisterProtocal(id);
            _registered.Clear();
            IsInitialized = false;
        }

        /// <summary>Register protocol handlers via RegisterProtocal.</summary>
        protected abstract void Register();

        protected void RegisterProtocal(int protoId, NetManager.Handler h)
        {
            NetManager.RegisterProtocal(protoId, h);
            _registered.Add(protoId);
        }

        protected void UnregisterProtocal(int protoId)
        {
            NetManager.UnregisterProtocal(protoId);
            _registered.Remove(protoId);
        }

        /// <summary>SendFmt(Proto.ACCOUNT_LOGIN, "iiss", pid, time, accountId, platName)。</summary>
        protected void SendFmt(int protoId, string format = null, params object[] args)
            => NetManager.SendFmt(protoId, format, args);
    }
}
