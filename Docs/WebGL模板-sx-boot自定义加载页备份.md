# sx-boot 自定义 HTML 加载页备份(2026-07-13 按需求移除,随时可加回)

当前模板(`Assets/WebGLTemplates/Shenxiao/index.html`)已改为官方默认进度条承载全程。
如需恢复"金色进度条+游戏名"的自定义加载页,把下面三段加回模板即可(位置见注释)。

## ⚠ 恢复时的铁律
模板必须保持**官方 Default 底座 + 叠层**结构——从零自写全屏 canvas 页会让 TMP 输入框键盘失灵
(2026-07-12 实证,机理未明)。恢复只是把 sx-boot 层叠回去,别动 Default 底座。

## 一、`<head>` 内的样式块(插在 `<link rel="stylesheet">` 之后)

```html
<style>
  #sx-boot {
    position: fixed; inset: 0; z-index: 10;
    display: flex; flex-direction: column; justify-content: center; align-items: center;
    background: radial-gradient(ellipse at 50% 35%, #1d2033 0%, #0b0b12 70%);
    transition: opacity .4s ease; color: #d8c9a3;
    font-family: "Microsoft YaHei", sans-serif;
  }
  .sx-title { font-size: 34px; letter-spacing: 10px; margin-bottom: 48px; text-shadow: 0 0 18px rgba(216,182,98,.45); }
  .sx-bar { width: 66%; max-width: 420px; height: 10px; border-radius: 5px; background: rgba(255,255,255,.08); overflow: hidden; border: 1px solid rgba(216,182,98,.35); }
  #sx-bar-fill { width: 0%; height: 100%; border-radius: 5px; background: linear-gradient(90deg, #8f6b2d, #e8c877); transition: width .25s ease; }
  #sx-text { margin-top: 16px; font-size: 13px; opacity: .75; }
  #sx-pct { margin-top: 6px; font-size: 12px; opacity: .5; }
</style>
```

## 二、`<body>` 内的层 DOM(插在 `#unity-container` 之后)

```html
<div id="sx-boot">
  <div class="sx-title">九州神霄录</div>
  <div class="sx-bar"><div id="sx-bar-fill"></div></div>
  <div id="sx-text">正在连接资源服务器…</div>
  <div id="sx-pct"></div>
</div>
```

## 三、脚本(替换现在指向 #unity-loading-bar 的 SxBootUpdate/SxBootDone 定义)

```html
<script>
  // ---- sx-boot 加载层:引擎下载 0~85%(loader onProgress),85~100% 由游戏侧 BootOverlay 驱动 ----
  var sxFill = document.getElementById('sx-bar-fill');
  var sxText = document.getElementById('sx-text');
  var sxPct = document.getElementById('sx-pct');
  var sxMax = 0;              // 进度只进不退
  var sxLastUpdate = Date.now();
  window.SxBootUpdate = function (p, t) {
    sxLastUpdate = Date.now();
    if (p > sxMax) sxMax = p;
    if (sxFill) { sxFill.style.width = (sxMax * 100).toFixed(1) + '%'; sxPct.textContent = Math.round(sxMax * 100) + '%'; }
    if (t && sxText) sxText.textContent = t;
  };
  window.SxBootDone = function () {
    var b = document.getElementById('sx-boot');
    if (!b) return;
    window.SxBootUpdate(1, '进入游戏');
    b.style.opacity = '0';
    setTimeout(function () { if (b.parentNode) b.parentNode.removeChild(b); }, 450);
  };
</script>
```

配套改动(恢复时同步):
- `createUnityInstance` 的 onProgress 回调仍是 `window.SxBootUpdate(progress * 0.85, ...)`(现模板未变,不用动);
- 引擎就绪后的 45s 看门狗把判断条件换回 `document.getElementById('sx-boot')` 存在与否;
- `.catch` 分支可改回 `sxText.textContent = '加载失败...'`(现用 unityShowBanner,两者皆可);
- 游戏侧 `BootOverlay`/`ShenxiaoBoot.jslib` 是防御式的(`if (window.SxBootUpdate)`),两种模板通吃,无需改 C#。

改完后跑 `神霄/打包/④b 构建Web壳` 生效。
