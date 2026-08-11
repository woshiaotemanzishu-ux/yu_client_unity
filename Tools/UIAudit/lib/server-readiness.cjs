'use strict';

const fs = require('fs');
const path = require('path');
const http = require('http');
const https = require('https');

const ROUTE_URL_CHECK_ID = 'route-url-readiness';
const SERVER_PROFILE_SCHEMA = 1;

function validateServerProfile(profile) {
  const errors = [];
  if (!profile || Number(profile.schema) !== SERVER_PROFILE_SCHEMA) errors.push('schema');
  if (!profile || typeof profile.id !== 'string' || !profile.id) errors.push('id');
  if (!Array.isArray(profile && profile.matchOrigins) || !profile.matchOrigins.length) errors.push('matchOrigins');
  if (!profile || typeof profile.host !== 'string' || !profile.host) errors.push('host');
  if (!Number.isInteger(Number(profile && profile.port))) errors.push('port');
  if (!profile || typeof profile.cwdFromRepo !== 'string' || !profile.cwdFromRepo) errors.push('cwdFromRepo');
  if (!profile || typeof profile.staticRootFromRepo !== 'string' || !profile.staticRootFromRepo) errors.push('staticRootFromRepo');
  if (!profile || typeof profile.workerFromTool !== 'string' || !profile.workerFromTool) errors.push('workerFromTool');
  if (errors.length) throw new Error(`SERVER_PROFILE_INVALID: ${errors.join(',')}`);
  return profile;
}

function serverProfilesDir() {
  return path.join(__dirname, '..', 'server-profiles');
}

function loadServerProfiles(directory = serverProfilesDir()) {
  if (!fs.existsSync(directory)) return [];
  return fs.readdirSync(directory)
    .filter(name => name.endsWith('.json'))
    .sort()
    .map(name => {
      const filePath = path.join(directory, name);
      const profile = validateServerProfile(JSON.parse(fs.readFileSync(filePath, 'utf8')));
      Object.defineProperty(profile, '__file', { value: filePath, enumerable: false });
      return profile;
    });
}

function loadServerProfile(id, directory = serverProfilesDir()) {
  const profile = loadServerProfiles(directory).find(value => value.id === id);
  if (!profile) throw new Error(`SERVER_PROFILE_NOT_FOUND: ${id}`);
  return profile;
}

function findServerProfileForUrl(url, profiles = loadServerProfiles()) {
  const origin = new URL(url).origin.toLowerCase();
  return profiles.find(profile => profile.matchOrigins.some(value => String(value).toLowerCase() === origin)) || null;
}

function resolvedServerProfile(profile, repoRoot) {
  if (!profile) return null;
  const toolRoot = path.resolve(__dirname, '..');
  return {
    id: profile.id,
    file: profile.__file || null,
    host: profile.host,
    port: Number(profile.port),
    url: `http://${profile.host}:${Number(profile.port)}${profile.routePath || '/index.html'}`,
    cwd: path.resolve(repoRoot, profile.cwdFromRepo),
    staticRoot: path.resolve(repoRoot, profile.staticRootFromRepo),
    worker: path.resolve(toolRoot, profile.workerFromTool),
    readiness: { ...(profile.readiness || {}) },
  };
}

function probeFailure(code, url, startedAt, extra = {}) {
  return {
    pass: false,
    ready: false,
    code,
    category: code === 'SERVER_NOT_RUNNING' ? 'server-lifecycle' : 'route-readiness',
    retryable: ['SERVER_NOT_RUNNING', 'ROUTE_URL_TIMEOUT', 'CONNECTION_RESET'].includes(code),
    url,
    elapsedMs: Date.now() - startedAt,
    ...extra,
  };
}

function classifyRequestError(error, url, startedAt) {
  const rawCode = error && error.code || null;
  if (rawCode === 'ECONNREFUSED') return probeFailure('SERVER_NOT_RUNNING', url, startedAt, { networkCode: rawCode });
  if (rawCode === 'ENOTFOUND' || rawCode === 'EAI_AGAIN') return probeFailure('DNS_NOT_FOUND', url, startedAt, { networkCode: rawCode });
  if (rawCode === 'ETIMEDOUT' || rawCode === 'UIAUDIT_ROUTE_TIMEOUT') return probeFailure('ROUTE_URL_TIMEOUT', url, startedAt, { networkCode: rawCode });
  if (rawCode === 'ECONNRESET') return probeFailure('CONNECTION_RESET', url, startedAt, { networkCode: rawCode });
  if (rawCode && /CERT|TLS|SSL/i.test(rawCode)) return probeFailure('TLS_ERROR', url, startedAt, { networkCode: rawCode });
  return probeFailure('ROUTE_NETWORK_ERROR', url, startedAt, { networkCode: rawCode, message: String(error && error.message || error) });
}

function requestOnce(url, options, startedAt) {
  return new Promise(resolve => {
    const requestStartedAt = Date.now();
    const parsed = new URL(url);
    const transport = parsed.protocol === 'https:' ? https : http;
    const remainingMs = Math.max(1, Number(options.timeoutMs) - (Date.now() - startedAt));
    let settled = false;
    const finish = value => {
      if (settled) return;
      settled = true;
      clearTimeout(deadline);
      resolve(value);
    };
    const request = transport.request(parsed, {
      method: options.method || 'GET',
      headers: {
        Accept: 'text/html,application/xhtml+xml;q=0.9,*/*;q=0.1',
        'Cache-Control': 'no-cache',
        'User-Agent': '@shenxiao/ui-audit-preflight',
      },
    }, response => {
      const chunks = [];
      let bytes = 0;
      response.on('data', chunk => {
        if (bytes < Number(options.maxBytes)) {
          const remaining = Number(options.maxBytes) - bytes;
          chunks.push(chunk.subarray(0, remaining));
          bytes += Math.min(chunk.length, remaining);
        }
      });
      response.on('end', () => finish({
        response,
        body: Buffer.concat(chunks).toString('utf8'),
        elapsedMs: Date.now() - requestStartedAt,
      }));
      response.on('error', error => finish({ error, elapsedMs: Date.now() - requestStartedAt }));
    });
    request.on('error', error => finish({ error, elapsedMs: Date.now() - requestStartedAt }));
    request.setTimeout(remainingMs, () => {
      const error = new Error(`route readiness timed out after ${options.timeoutMs}ms`);
      error.code = 'UIAUDIT_ROUTE_TIMEOUT';
      request.destroy(error);
    });
    const deadline = setTimeout(() => {
      const error = new Error(`route readiness timed out after ${options.timeoutMs}ms`);
      error.code = 'UIAUDIT_ROUTE_TIMEOUT';
      request.destroy(error);
      finish({ error, elapsedMs: Date.now() - requestStartedAt });
    }, remainingMs + 25);
    request.end();
  });
}

async function probeRouteUrl(url, readiness = {}) {
  const startedAt = Date.now();
  const options = {
    timeoutMs: Math.max(50, Number(readiness.timeoutMs || 2500)),
    maxRedirects: Math.max(0, Number(readiness.maxRedirects == null ? 2 : readiness.maxRedirects)),
    maxBytes: Math.max(1024, Number(readiness.maxBytes || 262144)),
    acceptedStatus: Array.isArray(readiness.acceptedStatus) ? readiness.acceptedStatus.map(Number) : [200],
    contentTypeIncludes: readiness.contentTypeIncludes || [],
    bodyIncludesAll: readiness.bodyIncludesAll || [],
    bodyIncludesAny: readiness.bodyIncludesAny || [],
    requiredHeadPaths: readiness.requiredHeadPaths || [],
  };
  let currentUrl = String(url);
  let redirects = 0;
  const requests = [];
  while (true) {
    if (Date.now() - startedAt >= options.timeoutMs) return probeFailure('ROUTE_URL_TIMEOUT', currentUrl, startedAt, { requests });
    const outcome = await requestOnce(currentUrl, options, startedAt);
    if (outcome.error) {
      requests.push({ method: 'GET', url: currentUrl, elapsedMs: outcome.elapsedMs, networkCode: outcome.error.code || null });
      return { ...classifyRequestError(outcome.error, currentUrl, startedAt), requests };
    }
    const { response, body } = outcome;
    const statusCode = Number(response.statusCode || 0);
    const location = response.headers.location;
    const responseContentType = String(response.headers['content-type'] || '').toLowerCase();
    requests.push({ method: 'GET', url: currentUrl, elapsedMs: outcome.elapsedMs, statusCode, contentType: responseContentType });
    if (statusCode >= 300 && statusCode < 400 && location) {
      if (redirects >= options.maxRedirects) return probeFailure('TOO_MANY_REDIRECTS', currentUrl, startedAt, { statusCode, requests });
      currentUrl = new URL(location, currentUrl).toString();
      redirects += 1;
      continue;
    }
    const contentType = responseContentType;
    if (!options.acceptedStatus.includes(statusCode)) {
      return probeFailure('HTTP_STATUS_NOT_READY', currentUrl, startedAt, { statusCode, contentType, redirects, requests });
    }
    if (options.contentTypeIncludes.length && !options.contentTypeIncludes.some(value => contentType.includes(String(value).toLowerCase()))) {
      return probeFailure('ROUTE_CONTENT_TYPE_NOT_READY', currentUrl, startedAt, { statusCode, contentType, redirects, requests });
    }
    const lowerBody = body.toLowerCase();
    const missingAll = options.bodyIncludesAll.filter(value => !lowerBody.includes(String(value).toLowerCase()));
    const anyMatched = !options.bodyIncludesAny.length || options.bodyIncludesAny.some(value => lowerBody.includes(String(value).toLowerCase()));
    if (missingAll.length || !anyMatched) {
      return probeFailure('ROUTE_CONTENT_NOT_READY', currentUrl, startedAt, {
        statusCode, contentType, redirects, missingMarkers: missingAll, requests,
      });
    }
    const requiredResources = [];
    for (const requiredPath of options.requiredHeadPaths) {
      const resourceUrl = new URL(String(requiredPath), currentUrl).toString();
      const resourceOutcome = await requestOnce(resourceUrl, { ...options, method: 'HEAD' }, startedAt);
      if (resourceOutcome.error) {
        requests.push({ method: 'HEAD', url: resourceUrl, elapsedMs: resourceOutcome.elapsedMs, networkCode: resourceOutcome.error.code || null });
        return { ...classifyRequestError(resourceOutcome.error, resourceUrl, startedAt), requiredResource: resourceUrl, requests };
      }
      const resourceStatus = Number(resourceOutcome.response.statusCode || 0);
      const resource = { url: resourceUrl, method: 'HEAD', elapsedMs: resourceOutcome.elapsedMs, statusCode: resourceStatus };
      requiredResources.push(resource);
      requests.push(resource);
      if (!options.acceptedStatus.includes(resourceStatus)) {
        return probeFailure('ROUTE_RESOURCE_NOT_READY', resourceUrl, startedAt, { statusCode: resourceStatus, requiredResource: resourceUrl, requests });
      }
    }
    return {
      pass: true,
      ready: true,
      code: 'ROUTE_READY',
      category: 'route-readiness',
      retryable: false,
      url: currentUrl,
      statusCode,
      contentType,
      redirects,
      bytesInspected: Buffer.byteLength(body),
      requiredResources,
      requests,
      elapsedMs: Date.now() - startedAt,
    };
  }
}

module.exports = {
  ROUTE_URL_CHECK_ID,
  SERVER_PROFILE_SCHEMA,
  validateServerProfile,
  serverProfilesDir,
  loadServerProfiles,
  loadServerProfile,
  findServerProfileForUrl,
  resolvedServerProfile,
  classifyRequestError,
  probeRouteUrl,
};
