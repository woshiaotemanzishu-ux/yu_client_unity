using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;

namespace Shenxiao.Framework.Net
{
    /// <summary>
    /// WebSocket connection manager. Single connection assumed (one game world).
    /// Dispatches inbound text payloads as ErlangTerm to registered handlers.
    /// </summary>
    public static class NetManager
    {
        public delegate void Handler(ErlangTerm term);

        private static ClientWebSocket _ws;
        private static CancellationTokenSource _cts;
        private static readonly Dictionary<int, Handler> _handlers = new Dictionary<int, Handler>();
        private static readonly Queue<ErlangTerm> _inbox = new Queue<ErlangTerm>();
        private static readonly object _inboxLock = new object();

        public static bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;

        public static void RegisterProtocal(int protoId, Handler h) => _handlers[protoId] = h;
        public static void UnregisterProtocal(int protoId) => _handlers.Remove(protoId);

        /// <summary>
        /// Connect to a ws:// or wss:// url. Throws on connection failure.
        /// </summary>
        public static async Task ConnectAsync(string url)
        {
            await DisconnectAsync();
            _ws = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            await _ws.ConnectAsync(new Uri(url), _cts.Token);
            EventDispatcher.Emit(GlobalEvent.EVT_NET_CONNECTED);
            _ = ReceiveLoop();
        }

        public static async Task DisconnectAsync()
        {
            if (_cts != null) { _cts.Cancel(); _cts = null; }
            if (_ws != null)
            {
                try { if (_ws.State == WebSocketState.Open) await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None); } catch { /* swallow on shutdown */ }
                _ws.Dispose();
                _ws = null;
                EventDispatcher.Emit(GlobalEvent.EVT_NET_DISCONNECTED);
            }
        }

        /// <summary>
        /// Send a protocol message: SendFmt(Proto.CS_LOGIN, "ss", account, password).
        /// </summary>
        public static void SendFmt(int protoId, string format = null, params object[] args)
        {
            string payload = UserMsgAdapter.Encode(protoId, format, args);
            _ = SendRaw(payload);
        }

        /// <summary>
        /// Pump pending inbound messages on the main thread. Call from a MonoBehaviour.Update.
        /// </summary>
        public static void Pump()
        {
            while (true)
            {
                ErlangTerm term;
                lock (_inboxLock)
                {
                    if (_inbox.Count == 0) return;
                    term = _inbox.Dequeue();
                }
                Dispatch(term);
            }
        }

        private static void Dispatch(ErlangTerm term)
        {
            if (term == null || term.Items == null || term.Items.Count == 0) return;
            int protoId = term.Get<int>(0);
            if (_handlers.TryGetValue(protoId, out var h))
            {
                try { h(term); }
                catch (Exception e) { GameLog.Error("Net", "handler {0} exception: {1}", protoId, e); }
            }
            else
            {
                GameLog.Warn("Net", "no handler for proto={0}", protoId);
            }
        }

        private static async Task SendRaw(string payload)
        {
            if (!IsConnected) { GameLog.Warn("Net", "send while disconnected: {0}", payload); return; }
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
        }

        private static async Task ReceiveLoop()
        {
            var buf = new byte[8192];
            var sb = new StringBuilder();
            try
            {
                while (_ws != null && _ws.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    sb.Clear();
                    WebSocketReceiveResult r;
                    do
                    {
                        r = await _ws.ReceiveAsync(new ArraySegment<byte>(buf), _cts.Token);
                        if (r.MessageType == WebSocketMessageType.Close)
                        {
                            await DisconnectAsync();
                            return;
                        }
                        sb.Append(Encoding.UTF8.GetString(buf, 0, r.Count));
                    } while (!r.EndOfMessage);

                    var term = ErlangParser.Parse(sb.ToString());
                    if (term != null)
                    {
                        lock (_inboxLock) _inbox.Enqueue(term);
                    }
                }
            }
            catch (Exception e)
            {
                GameLog.Error("Net", "recv loop fail: {0}", e.Message);
                EventDispatcher.Emit(GlobalEvent.EVT_NET_ERROR);
                await DisconnectAsync();
            }
        }
    }
}
