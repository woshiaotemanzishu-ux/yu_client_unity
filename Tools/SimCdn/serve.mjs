// SimCdn — 模拟 CDN 的本地静态服务器(打包冒烟用,后续上真 CDN 时客户端零改动)。
// 用法: node tools/SimCdn/serve.mjs   (可选环境变量 PORT,默认 8090)
// 路由: /res/*  -> ServerData/*      (Addressables bundle + catalog_live.*)
//       /*      -> Builds/WebGL/*    (WebGL 壳,含 StreamingAssets 本地组)
// 头策略(与将来真 CDN 对齐,本身就是被测对象):
//   catalog_*            -> Cache-Control: no-store   (更新入口,永不缓存)
//   *.bundle             -> immutable 一年            (文件名带内容 hash,天然免失效)
//   *.gz / *.br          -> Content-Encoding 对应压缩 (WebGL 壳文件)
//   全部                 -> Access-Control-Allow-Origin: * (跨域取资源)
import http from 'node:http';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const PORT = Number(process.env.PORT || 8090);

const TYPES = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'application/javascript',
  '.wasm': 'application/wasm',
  '.json': 'application/json',
  '.css': 'text/css',
  '.png': 'image/png',
  '.ico': 'image/x-icon',
  '.hash': 'text/plain',
  '.bin': 'application/octet-stream',
  '.bundle': 'application/octet-stream',
  '.data': 'application/octet-stream',
};

http.createServer((req, res) => {
  const urlPath = decodeURIComponent(new URL(req.url, 'http://x').pathname);
  const filePath = urlPath.startsWith('/res/')
    ? path.join(root, 'ServerData', urlPath.slice(5))
    : path.join(root, 'Builds', 'WebGL', urlPath === '/' ? 'index.html' : urlPath.slice(1));

  if (!filePath.startsWith(root)) { res.writeHead(403); return res.end(); }

  fs.stat(filePath, (err, st) => {
    if (err || !st.isFile()) {
      console.log('404', urlPath);
      res.writeHead(404, { 'Access-Control-Allow-Origin': '*' });
      return res.end('not found');
    }

    const name = path.basename(filePath);
    // 校验头:没有它们,UnityCache 的 must-revalidate 拿不到 304,只能每次整包重下
    // (实测:三连载 stored=108/cacheHit=0,加载耗时不降——就是这个原因)。
    const etag = `"${st.size}-${st.mtimeMs}"`;
    const lastMod = st.mtime.toUTCString();
    const inm = req.headers['if-none-match'];
    const ims = req.headers['if-modified-since'];
    if (inm === etag || (!inm && ims && new Date(ims).getTime() >= Math.floor(st.mtimeMs / 1000) * 1000)) {
      res.writeHead(304, { 'Access-Control-Allow-Origin': '*', ETag: etag, 'Last-Modified': lastMod });
      console.log('304', urlPath);
      return res.end();
    }

    const headers = {
      'Access-Control-Allow-Origin': '*',
      'Content-Length': st.size,
      ETag: etag,
      'Last-Modified': lastMod,
    };

    let ext = path.extname(name);
    if (ext === '.gz') { headers['Content-Encoding'] = 'gzip'; ext = path.extname(name.slice(0, -3)); }
    else if (ext === '.br') { headers['Content-Encoding'] = 'br'; ext = path.extname(name.slice(0, -3)); }
    headers['Content-Type'] = TYPES[ext] || 'application/octet-stream';

    headers['Cache-Control'] = name.startsWith('catalog_') ? 'no-store'
      : name.endsWith('.bundle') ? 'public, max-age=31536000, immutable'
      : 'no-cache';

    res.writeHead(200, headers);
    fs.createReadStream(filePath).pipe(res);
    console.log('200', urlPath, `${(st.size / 1024).toFixed(0)}KB`);
  });
}).listen(PORT, '0.0.0.0', () => {
  const lan = Object.values(os.networkInterfaces()).flat()
    .find(i => i && i.family === 'IPv4' && !i.internal)?.address || '<本机IP>';
  console.log(`SimCdn 已启动:
  本机:   http://127.0.0.1:${PORT}/            (打开即 WebGL 壳)
  局域网: http://${lan}:${PORT}/               (真机/其他设备)
  资源:   http://127.0.0.1:${PORT}/res/WebGL/  -> ServerData/WebGL/
  AppConfig.addressablesCdnBaseUrl 填: http://127.0.0.1:${PORT}/res  (真机用局域网地址)`);
});
