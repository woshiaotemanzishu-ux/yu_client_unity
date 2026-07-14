// 启动进度桥:游戏侧驱动页面 HTML 加载层(Assets/WebGLTemplates/Shenxiao/index.html)。
// 页面未定义对应函数时静默跳过(兼容默认模板)。
mergeInto(LibraryManager.library, {
  SxBootUpdateJs: function (p, textPtr) {
    if (window.SxBootUpdate) window.SxBootUpdate(p, UTF8ToString(textPtr));
  },
  SxBootDoneJs: function () {
    if (window.SxBootDone) window.SxBootDone();
  }
});
