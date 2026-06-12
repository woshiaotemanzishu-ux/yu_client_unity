using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Shenxiao.Framework.Event;
using Shenxiao.Framework.Util;
using UnityEngine;

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

        public static bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;

        public static void RegisterProtocal(int protoId, Handler h) => _handlers[protoId] = h;
        public static void UnregisterProtocal(int protoId) => _handlers.Remove(protoId);

        /// <summary>配置心跳协议与间隔(秒);intervalSec &lt;= 0 关闭。</summary>
        public static void ConfigureHeartbeat(int protoId, float intervalSec)
        {
            _heartbeatProtoId = protoId;
            _heartbeatInterval = intervalSec;
            _nextHeartbeatAt = Time.realtimeSinceStartup + intervalSec;
        }

        /// <summary>连接 ws:// 或 wss://,失败抛异常;成功后发 EVT_NET_CONNECTED。</summary>
        public static async Task ConnectAsync(string url)
        {
            await DisconnectAsync();
            _ws = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            GameLog.Info("Net", "connecting {0}", url);
            await _ws.ConnectAsync(new Uri(url), _cts.Token);
            GameLog.Info("Net", "connected {0}", url);
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

        /// <summary>发包,对标 Laya SendFmtToGame(10000, "iiss", ...)。</summary>
        public static void SendFmt(int protoId, string format = null, params object[] args)
        {
            byte[] frame = UserMsgAdapter.Encode(protoId, format, args);
            _ = SendRaw(frame, protoId);
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
                    if (_inbox.Count == 0) return;
                    frame = _inbox.Dequeue();
                }
                Dispatch(frame);
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

        private static async Task SendRaw(byte[] frame, int protoId)
        {
            if (!IsConnected) { GameLog.Warn("Net", "send while disconnected: proto={0}", protoId); return; }
            try
            {
                await _ws.SendAsync(new ArraySegment<byte>(frame), WebSocketMessageType.Binary, true, _cts.Token);
            }
            catch (Exception e)
            {
                GameLog.Error("Net", "send fail proto={0}: {1}", protoId, e.Message);
            }
        }

        private static async Task ReceiveLoop()
        {
            var buf = new byte[16384];
            var message = new MemoryStream();
            try
            {
                while (_ws != null && _ws.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    message.SetLength(0);
                    WebSocketReceiveResult r;
                    do
                    {
                        r = await _ws.ReceiveAsync(new ArraySegment<byte>(buf), _cts.Token);
                        if (r.MessageType == WebSocketMessageType.Close)
                        {
                            await DisconnectAsync();
                            return;
                        }
                        message.Write(buf, 0, r.Count);
                    } while (!r.EndOfMessage);

                    SplitFrames(message.GetBuffer(), (int)message.Length);
                }
            }
            catch (OperationCanceledException)
            {
                // 主动断开
            }
            catch (Exception e)
            {
                GameLog.Error("Net", "recv loop fail: {0}", e.Message);
                EventDispatcher.Emit(GlobalEvent.EVT_NET_ERROR);
                await DisconnectAsync();
            }
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
    }
}
