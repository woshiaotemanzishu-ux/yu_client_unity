'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const http = require('node:http');
const { probeRouteUrl } = require('../lib/server-readiness.cjs');

async function listen(handler) {
  const server = http.createServer(handler);
  await new Promise((resolve, reject) => server.listen(0, '127.0.0.1', error => error ? reject(error) : resolve()));
  return server;
}

async function close(server) {
  await new Promise((resolve, reject) => server.close(error => error ? reject(error) : resolve()));
}

test('bounded route readiness verifies HTML markers and required resources without a browser', async () => {
  const server = await listen((request, response) => {
    if (request.url === '/js/bundle.js') {
      response.writeHead(200, { 'content-type': 'application/javascript' });
      response.end(request.method === 'HEAD' ? undefined : 'window.ready=true');
      return;
    }
    response.writeHead(200, { 'content-type': 'text/html' });
    response.end('<egret-main-player></egret-main-player><script src="js/bundle.js"></script>');
  });
  try {
    const port = server.address().port;
    const result = await probeRouteUrl(`http://127.0.0.1:${port}/index.html`, {
      timeoutMs: 1000,
      contentTypeIncludes: ['text/html'],
      bodyIncludesAll: ['egret-main-player', 'js/bundle.js'],
      requiredHeadPaths: ['/js/bundle.js'],
    });
    assert.equal(result.pass, true);
    assert.equal(result.code, 'ROUTE_READY');
    assert.equal(result.requiredResources[0].statusCode, 200);
  } finally {
    await close(server);
  }
});

test('a closed local route is classified as SERVER_NOT_RUNNING', async () => {
  const server = await listen((_request, response) => response.end('temporary'));
  const port = server.address().port;
  await close(server);
  const result = await probeRouteUrl(`http://127.0.0.1:${port}/index.html`, { timeoutMs: 500 });
  assert.equal(result.pass, false);
  assert.equal(result.code, 'SERVER_NOT_RUNNING');
  assert.equal(result.category, 'server-lifecycle');
});

test('wrong server content is not accepted merely because the port answers', async () => {
  const server = await listen((_request, response) => {
    response.writeHead(200, { 'content-type': 'text/html' });
    response.end('<html>unrelated service</html>');
  });
  try {
    const result = await probeRouteUrl(`http://127.0.0.1:${server.address().port}/index.html`, {
      timeoutMs: 500, bodyIncludesAll: ['egret-main-player'],
    });
    assert.equal(result.code, 'ROUTE_CONTENT_NOT_READY');
  } finally {
    await close(server);
  }
});
