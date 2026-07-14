// 浏览器原生 WebSocket 桥(WebGL 专用):IL2CPP WebGL 不支持 System.Net.WebSockets.ClientWebSocket,
// 也没有线程池,必须走 JS 事件回调。单连接语义与 NetManager 一致。
mergeInto(LibraryManager.library, {
  SxWsConnect: function (urlPtr, onOpen, onMessage, onClose, onError) {
    var url = UTF8ToString(urlPtr);
    if (Module.SxWs) {
      try { Module.SxWs.onclose = null; Module.SxWs.close(1000); } catch (e) {}
      Module.SxWs = null;
    }
    var ws;
    try {
      ws = new WebSocket(url);
    } catch (e) {
      console.error('[SxWs] ctor fail', e);
      {{{ makeDynCall('v', 'onError') }}}();
      return;
    }
    ws.binaryType = 'arraybuffer';
    Module.SxWs = ws;
    ws.onopen = function () { {{{ makeDynCall('v', 'onOpen') }}}(); };
    ws.onmessage = function (ev) {
      if (!(ev.data instanceof ArrayBuffer)) return; // 服务端只发 binary,文本帧忽略
      var bytes = new Uint8Array(ev.data);
      var ptr = _malloc(bytes.length);
      HEAPU8.set(bytes, ptr);
      {{{ makeDynCall('vii', 'onMessage') }}}(ptr, bytes.length);
      _free(ptr);
    };
    ws.onclose = function (ev) {
      if (Module.SxWs === ws) Module.SxWs = null;
      {{{ makeDynCall('vii', 'onClose') }}}(ev.code, ev.wasClean ? 1 : 0);
    };
    ws.onerror = function () { {{{ makeDynCall('v', 'onError') }}}(); };
  },

  SxWsSend: function (ptr, len) {
    var ws = Module.SxWs;
    if (!ws || ws.readyState !== 1) return 0;
    ws.send(HEAPU8.subarray(ptr, ptr + len));
    return 1;
  },

  // 0=CONNECTING 1=OPEN 2=CLOSING 3=CLOSED(含无连接)
  SxWsState: function () {
    return Module.SxWs ? Module.SxWs.readyState : 3;
  },

  SxWsClose: function (code) {
    var ws = Module.SxWs;
    if (!ws) return;
    Module.SxWs = null;
    ws.onclose = null; // 主动关闭不再回调,由 C# 侧自行收尾
    try { ws.close(code); } catch (e) {}
  }
});
