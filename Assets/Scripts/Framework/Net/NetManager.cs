using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
using AOT;
#endif

namespace Shenxiao.Framework.Net
{
    /// <summary>
    /// WebSocket 连接管理(单连接,BIG_ENDIAN 二进制,对标 yu_client UserMsgAdapter)。
    ///
    /// 收包帧(见 yu_client ReceiveHandler):
    ///   [u32 总长(含自身)] [u16 协议号] [u8 压缩标记] [载荷...]
    ///   一条 ws message 可能含多帧,按总长依次切。压缩包暂不支持(登录链不压缩),
    ///   遇到非 0 标记打 Error 并跳过。
    ///
    /// 处理器在主线程回调:AppLauncher.Update 每帧调 Pump()。
    /// </summary>
    public static class NetManager
    {
        public delegate void Handler(NetReader reader);

        private const int RECV_HEADER_SIZE = 7; // u32 len + u16 cmd + u8 flag

        private struct InboundFrame
        {
            public int ProtoId;
            public byte[] Payload;
        }

        private static ClientWebSocket _ws;
        private static CancellationTokenSource _cts;
        private static readonly Dictionary<int, Handler> _handlers = new Dictionary<int, Handler>();
        private static readonly Queue<InboundFrame> _inbox = new Queue<InboundFrame>();
        private static readonly object _inboxLock = new object();

        // 心跳:连接期间按间隔发送(无字段协议)。间隔来自 AppConfig,不在这里硬编码默认行为。
        private static int _heartbeatProtoId;
        private static float _heartbeatInterval;
        private static float _nextHeartbeatAt;
        // 链路活性:最近一次收到任何 ws 数据的 UTC ticks(后台收包线程写,Interlocked 读写)。
        // 判死链路必须看"整条连接是否有下行",而不能只看 10006 回包——服务端整体停顿时所有协议
        // 一起延迟,恢复后一起补发;只认 10006 会把可恢复停顿误判成死链。
        private static long _lastInboundUtcTicks;
        private static bool _remoteClosePending;
        private static WebSocketCloseStatus? _remoteCloseStatus;
        private static string _remoteCloseDescription;

#if UNITY_WEBGL && !UNITY_EDITOR
        public static bool IsConnected => _webglActive && WebGlWs.SxWsState() == 1;
#else
        public static bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;
#endif

        /// <summary>距最近一次收到任何下行数据的秒数;未连接/无记录时为正无穷。</summary>
        public static float SecondsSinceLastInbound
        {
            get
            {
                long ticks = Interlocked.Read(ref _lastInboundUtcTicks);
                if (ticks == 0) return float.PositiveInfinity;
                return (float)(DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds;
            }
        }

        public static void RegisterProtocal(int protoId, Handler h) => _handlers[protoId] = h;
        public static void UnregisterProtocal(int protoId) => _handlers.Remove(protoId);

        /// <summary>配置心跳协议与间隔(秒);intervalSec &lt;= 0 关闭。</summary>
        public static void ConfigureHeartbeat(int protoId, float intervalSec)
        {
            _heartbeatProtoId = protoId;
            _heartbeatInterval = intervalSec;
            _nextHeartbeatAt = intervalSec > 0f
                ? Time.realtimeSinceStartup + intervalSec
                : float.PositiveInfinity;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>连接 ws:// 或 wss://(WebGL:浏览器原生 WebSocket,IL2CPP 不支持 ClientWebSocket/线程池)。</summary>
        public static async Task ConnectAsync(string url)
        {
            await DisconnectAsync();
            ClearRemoteCloseState();
            _webglConnectTcs = new TaskCompletionSource<bool>();
            _webglActive = true;
            GameLog.Info("Net", "connecting {0} (webgl)", url);
            WebGlWs.SxWsConnect(url, _onWsOpen, _onWsMessage, _onWsClose, _onWsError);
            await _webglConnectTcs.Task;
            Interlocked.Exchange(ref _lastInboundUtcTicks, DateTime.UtcNow.Ticks);
            GameLog.Info("Net", "connected {0}", url);
            EventDispatcher.Emit(GlobalEvent.EVT_NET_CONNECTED);
        }

        public static Task DisconnectAsync()
        {
            ConfigureHeartbeat(0, 0f);
            ClearRemoteCloseState();
            Interlocked.Exchange(ref _lastInboundUtcTicks, 0L);
            _webglConnectTcs?.TrySetCanceled();
            _webglConnectTcs = null;
            if (_webglActive)
            {
                _webglActive = false;
                WebGlWs.SxWsClose(1000);
                EventDispatcher.Emit(GlobalEvent.EVT_NET_DISCONNECTED);
            }
            return Task.CompletedTask;
        }
#else
        /// <summary>连接 ws:// 或 wss://,失败抛异常;成功后发 EVT_NET_CONNECTED。</summary>
        public static async Task ConnectAsync(string url)
        {
            await DisconnectAsync();
            ClearRemoteCloseState();
            _ws = new ClientWebSocket();
            // ★禁用 .NET ClientWebSocket 的自动 keepalive PING 帧(默认 KeepAliveInterval=30s,运行时会定时发
            // 一个 opcode=9 的 WebSocket Ping 控制帧)。本服务端 WS 解帧器(gsrv_ws:parse_payload)只认 binary/close
            // 帧,收到 ping 帧返回 error → reader_ws 立刻 websocket_close(发完1000关闭帧裸关TCP、不等握手)→ 客户端
            // 报 'without completing the close handshake',表现为进游戏 ~30-36s(首个 keepalive 周期)莫名 force-close。
            // 老 Laya 跑浏览器、浏览器 WS 永不主动发 ping 故不触发;保活由应用层 10006 心跳负责,无需 WS 层 ping。
            // 复现根因见 yu_server reader_ws.beam/gsrv_ws.beam(parse_payload 只处理 binary/close)。
            // 用 Timeout.InfiniteTimeSpan(而非 TimeSpan.Zero):它在各 .NET/Mono 实现里都是公认的"禁用"哨兵
            // (无论内部按 >TimeSpan.Zero 还是 !=InfiniteTimeSpan 判断都禁用),最稳。
            _ws.Options.KeepAliveInterval = System.Threading.Timeout.InfiniteTimeSpan;
            _cts = new CancellationTokenSource();
            GameLog.Info("Net", "connecting {0}", url);
            await _ws.ConnectAsync(new Uri(url), _cts.Token);
            Interlocked.Exchange(ref _lastInboundUtcTicks, DateTime.UtcNow.Ticks);
            GameLog.Info("Net", "connected {0}", url);
            EventDispatcher.Emit(GlobalEvent.EVT_NET_CONNECTED);
            _ = ReceiveLoop();
        }

        public static async Task DisconnectAsync()
        {
            ConfigureHeartbeat(0, 0f);
            ClearRemoteCloseState();
            Interlocked.Exchange(ref _lastInboundUtcTicks, 0L);
            if (_cts != null) { _cts.Cancel(); _cts = null; }
            if (_ws != null)
            {
                try { if (_ws.State == WebSocketState.Open) await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None); } catch { /* swallow on shutdown */ }
                _ws.Dispose();
                _ws = null;
                EventDispatcher.Emit(GlobalEvent.EVT_NET_DISCONNECTED);
            }
        }
#endif

        /// <summary>发包,对标 Laya SendFmtToGame(10000, "iiss", ...)。</summary>
        public static void SendFmt(int protoId, string format = null, params object[] args)
        {
            byte[] frame = UserMsgAdapter.Encode(protoId, format, args);
            _ = SendRaw(frame, protoId);
        }

        /// <summary>Send and wait for the WebSocket write to complete. Use for login gate packets.</summary>
        public static Task SendFmtAsync(int protoId, string format = null, params object[] args)
        {
            byte[] frame = UserMsgAdapter.Encode(protoId, format, args);
            return SendRaw(frame, protoId);
        }

        /// <summary>主线程消息泵 + 心跳。AppLauncher.Update 每帧调用。</summary>
        public static void Pump()
        {
            if (_heartbeatInterval > 0f && IsConnected && Time.realtimeSinceStartup >= _nextHeartbeatAt)
            {
                _nextHeartbeatAt = Time.realtimeSinceStartup + _heartbeatInterval;
                SendFmt(_heartbeatProtoId);
            }

            while (true)
            {
                InboundFrame frame;
                lock (_inboxLock)
                {
                    if (_inbox.Count == 0) break;
                    frame = _inbox.Dequeue();
                }
                Dispatch(frame);
            }

            if (TryConsumeRemoteClose(out WebSocketCloseStatus? closeStatus, out string closeDescription))
            {
                _ = CompleteRemoteCloseAsync(closeStatus, closeDescription);
            }
        }

        private static void Dispatch(InboundFrame frame)
        {
            if (_handlers.TryGetValue(frame.ProtoId, out Handler h))
            {
                try { h(new NetReader(frame.Payload, 0, frame.Payload.Length)); }
                catch (Exception e) { GameLog.Error("Net", "handler {0} exception: {1}", frame.ProtoId, e); }
            }
            else
            {
                // 进游戏初期服务端会推送大量尚未实现模块的协议(130xx/150xx/16xxx…),
                // 这是按模块推进的预期内噪音,只记 Info 不刷 Warn(见 Docs/Shenxiao协议架构.md §4)
                GameLog.Info("Net", "未注册协议 proto={0} payload={1}B(对应模块未接,预期内)", frame.ProtoId, frame.Payload.Length);
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private static Task SendRaw(byte[] frame, int protoId)
        {
            if (!IsConnected) { GameLog.Warn("Net", "send while disconnected: proto={0}", protoId); return Task.CompletedTask; }
            if (WebGlWs.SxWsSend(frame, frame.Length) == 0)
            {
                GameLog.Error("Net", "send fail proto={0}: ws not open", protoId);
            }
            else if (ShouldLogHandshakeTraffic(protoId))
            {
                GameLog.Info("Net", "sent proto={0} bytes={1}", protoId, frame.Length);
            }
            return Task.CompletedTask;
        }
#else
        private static async Task SendRaw(byte[] frame, int protoId)
        {
            if (!IsConnected) { GameLog.Warn("Net", "send while disconnected: proto={0}", protoId); return; }
            try
            {
                await _ws.SendAsync(new ArraySegment<byte>(frame), WebSocketMessageType.Binary, true, _cts.Token);
                if (ShouldLogHandshakeTraffic(protoId))
                {
                    GameLog.Info("Net", "sent proto={0} bytes={1}", protoId, frame.Length);
                }
            }
            catch (Exception e)
            {
                GameLog.Error("Net", "send fail proto={0}: {1}", protoId, e.Message);
            }
        }
#endif

        private static async Task ReceiveLoop()
        {
            // 捕获本次连接的 ws/token 局部引用:整个收包循环跑在后台线程(下面 ReceiveAsync 用 ConfigureAwait(false)),
            // 期间主线程可能 DisconnectAsync 把静态 _ws/_cts 置空;用局部引用避免读到 null 触发 NRE/竞态。
            ClientWebSocket ws = _ws;
            CancellationTokenSource cts = _cts;
            if (ws == null || cts == null) return;
            CancellationToken token = cts.Token;

            var buf = new byte[16384];
            var message = new MemoryStream();
            try
            {
                // ★ ConfigureAwait(false):把 socket 读取 + WS 协议层 PING/PONG 自动应答 + 拆帧入队全部移出 Unity 主线程。
                // 这样主线程被重活(进副本重载地图、刷一屏 NPC 图标等)钉死时,网络层仍持续收发、自动回 PONG,
                // 不会因 keepalive 被饿死而被服务端 force-close(对标老端浏览器 WS 走独立事件循环、不与渲染抢线程)。
                // 帧只入 _inbox 线程安全队列;真正派发(handler 碰 Unity 对象)仍在主线程 Pump() 里做,语义不变。
                while (ws.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    message.SetLength(0);
                    WebSocketReceiveResult r;
                    do
                    {
                        r = await ws.ReceiveAsync(new ArraySegment<byte>(buf), token).ConfigureAwait(false);
                        Interlocked.Exchange(ref _lastInboundUtcTicks, DateTime.UtcNow.Ticks);
                        if (r.MessageType == WebSocketMessageType.Close)
                        {
                            MarkRemoteClose(r.CloseStatus, r.CloseStatusDescription);
                            return;
                        }
                        message.Write(buf, 0, r.Count);
                    } while (!r.EndOfMessage);

                    if (message.Length >= RECV_HEADER_SIZE)
                    {
                        int protoId = (buf[4] << 8) | buf[5];
                        if (ShouldLogHandshakeTraffic(protoId))
                        {
                            GameLog.Info("Net", "recv ws message bytes={0} proto={1}", message.Length, protoId);
                        }
                    }
                    SplitFrames(message.GetBuffer(), (int)message.Length);
                }
            }
            catch (OperationCanceledException)
            {
                // 主动断开(DisconnectAsync 取消 token),正常退出。
            }
            catch (Exception e)
            {
                // 收包异常(服务端 force-close / 网络断)。★不在此后台线程发事件或调 DisconnectAsync★——
                // 那会让 EVT_NET_DISCONNECTED 等在非主线程触发,handler 碰 Unity 对象会崩。改为置远端关闭标志,
                // 由主线程 Pump() 走与 Close 帧同一路径统一收尾(TryConsumeRemoteClose → DisconnectAsync)。
                if (token.IsCancellationRequested) return; // 主动断开过程中的异常,不当远端关闭处理
                GameLog.Error("Net", "recv loop fail: {0}", e.Message);
                MarkRemoteClose(null, "recv loop fail: " + e.Message);
            }
        }

        private static void MarkRemoteClose(WebSocketCloseStatus? status, string description)
        {
            int pendingFrames;
            lock (_inboxLock)
            {
                _remoteClosePending = true;
                _remoteCloseStatus = status;
                _remoteCloseDescription = description;
                pendingFrames = _inbox.Count;
            }

            GameLog.Warn("Net", "websocket close received status={0} desc={1} pendingFrames={2}",
                status, description, pendingFrames);
        }

        private static bool TryConsumeRemoteClose(out WebSocketCloseStatus? status, out string description)
        {
            lock (_inboxLock)
            {
                if (!_remoteClosePending)
                {
                    status = null;
                    description = null;
                    return false;
                }

                _remoteClosePending = false;
                status = _remoteCloseStatus;
                description = _remoteCloseDescription;
                _remoteCloseStatus = null;
                _remoteCloseDescription = null;
                return true;
            }
        }

        private static void ClearRemoteCloseState()
        {
            lock (_inboxLock)
            {
                _remoteClosePending = false;
                _remoteCloseStatus = null;
                _remoteCloseDescription = null;
            }
        }

        private static async Task CompleteRemoteCloseAsync(WebSocketCloseStatus? status, string description)
        {
            GameLog.Warn("Net", "websocket remote close dispatch complete status={0} desc={1}",
                status, description);
            await DisconnectAsync();
        }

        /// <summary>按 [u32 总长] 把一条 ws message 切成若干协议帧入队。</summary>
        private static void SplitFrames(byte[] data, int length)
        {
            int offset = 0;
            while (offset + RECV_HEADER_SIZE <= length)
            {
                int frameLen = (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
                if (frameLen < RECV_HEADER_SIZE || offset + frameLen > length)
                {
                    GameLog.Error("Net", "异常帧长 {0} (offset={1}, total={2}),丢弃剩余数据", frameLen, offset, length);
                    return;
                }
                int protoId = (data[offset + 4] << 8) | data[offset + 5];
                byte compressFlag = data[offset + 6];
                if (compressFlag != 0)
                {
                    GameLog.Error("Net", "proto={0} 带压缩标记 {1},暂不支持,丢弃该帧", protoId, compressFlag);
                }
                else
                {
                    var payload = new byte[frameLen - RECV_HEADER_SIZE];
                    Buffer.BlockCopy(data, offset + RECV_HEADER_SIZE, payload, 0, payload.Length);
                    lock (_inboxLock)
                    {
                        _inbox.Enqueue(new InboundFrame { ProtoId = protoId, Payload = payload });
                    }
                }
                offset += frameLen;
            }
        }

        private static bool ShouldLogHandshakeTraffic(int protoId)
        {
            return protoId == 10000 || protoId == 10003 || protoId == 10004;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // ---- WebGL 浏览器原生 WebSocket 桥(Assets/Plugins/WebGL/ShenxiaoWebSocket.jslib) ----
        // 回调都发生在浏览器 JS 事件循环 = Unity 主线程,入队走既有 _inboxLock 路径,派发仍在 Pump()。
        private static bool _webglActive;
        private static TaskCompletionSource<bool> _webglConnectTcs;
        // 委托必须存静态字段:传给 native 后若被 GC,回调即野指针。
        private static readonly WebGlWs.VoidCb _onWsOpen = OnWsOpen;
        private static readonly WebGlWs.MsgCb _onWsMessage = OnWsMessage;
        private static readonly WebGlWs.CloseCb _onWsClose = OnWsClose;
        private static readonly WebGlWs.VoidCb _onWsError = OnWsError;

        [MonoPInvokeCallback(typeof(WebGlWs.VoidCb))]
        private static void OnWsOpen()
        {
            _webglConnectTcs?.TrySetResult(true);
        }

        [MonoPInvokeCallback(typeof(WebGlWs.MsgCb))]
        private static void OnWsMessage(IntPtr ptr, int len)
        {
            if (len <= 0) return;
            var data = new byte[len];
            Marshal.Copy(ptr, data, 0, len);
            Interlocked.Exchange(ref _lastInboundUtcTicks, DateTime.UtcNow.Ticks);
            if (len >= RECV_HEADER_SIZE)
            {
                int protoId = (data[4] << 8) | data[5];
                if (ShouldLogHandshakeTraffic(protoId))
                {
                    GameLog.Info("Net", "recv ws message bytes={0} proto={1}", len, protoId);
                }
            }
            SplitFrames(data, len);
        }

        [MonoPInvokeCallback(typeof(WebGlWs.CloseCb))]
        private static void OnWsClose(int code, int wasClean)
        {
            if (_webglConnectTcs != null && !_webglConnectTcs.Task.IsCompleted)
            {
                _webglConnectTcs.TrySetException(new IOException("websocket closed during connect: code=" + code));
                return;
            }
            MarkRemoteClose((WebSocketCloseStatus)code, "wasClean=" + wasClean);
        }

        [MonoPInvokeCallback(typeof(WebGlWs.VoidCb))]
        private static void OnWsError()
        {
            if (_webglConnectTcs != null && !_webglConnectTcs.Task.IsCompleted)
            {
                _webglConnectTcs.TrySetException(new IOException("websocket connect error"));
                return;
            }
            GameLog.Error("Net", "websocket error (webgl)");
        }

        private static class WebGlWs
        {
            public delegate void VoidCb();
            public delegate void MsgCb(IntPtr ptr, int len);
            public delegate void CloseCb(int code, int wasClean);

            [DllImport("__Internal")] public static extern void SxWsConnect(string url, VoidCb onOpen, MsgCb onMessage, CloseCb onClose, VoidCb onError);
            [DllImport("__Internal")] public static extern int SxWsSend(byte[] data, int len);
            [DllImport("__Internal")] public static extern int SxWsState();
            [DllImport("__Internal")] public static extern void SxWsClose(int code);
        }
#endif
    }
}
