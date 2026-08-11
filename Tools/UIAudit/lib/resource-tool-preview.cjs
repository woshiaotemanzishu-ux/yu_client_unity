'use strict';

const http = require('http');
const https = require('https');

const PREVIEW_PROVIDER_SCHEMA_VERSION = 1;
const PREVIEW_PROVIDER_OBSERVATION_SCHEMA = 'ui-audit.resource-tool-preview-observation.v1';

function validatePreviewProvider(provider) {
  const errors = [];
  if (!provider || Number(provider.schema) !== PREVIEW_PROVIDER_SCHEMA_VERSION) errors.push('schema');
  if (!provider || typeof provider.id !== 'string' || !provider.id) errors.push('id');
  if (!provider || typeof provider.controlHost !== 'string' || !provider.controlHost) errors.push('controlHost');
  if (!Number.isInteger(Number(provider && provider.controlPort))) errors.push('controlPort');
  if (!provider || typeof provider.statusPath !== 'string' || !provider.statusPath.startsWith('/')) errors.push('statusPath');
  if (!provider || typeof provider.startPath !== 'string' || !provider.startPath.startsWith('/')) errors.push('startPath');
  if (!provider || typeof provider.stopPath !== 'string' || !provider.stopPath.startsWith('/')) errors.push('stopPath');
  if (!provider || typeof provider.recoveryPath !== 'string' || !provider.recoveryPath.startsWith('/')) errors.push('recoveryPath');
  if (provider && provider.recoveryTimeoutMs != null && (!Number.isFinite(Number(provider.recoveryTimeoutMs)) || Number(provider.recoveryTimeoutMs) < 100)) errors.push('recoveryTimeoutMs');
  if (!Array.isArray(provider && provider.expectedProcessCommandIncludes) || !provider.expectedProcessCommandIncludes.length) errors.push('expectedProcessCommandIncludes');
  if (!provider || typeof provider.recoveryContractFromTool !== 'string' || !provider.recoveryContractFromTool) errors.push('recoveryContractFromTool');
  if (errors.length) throw new Error(`PREVIEW_PROVIDER_INVALID: ${errors.join(',')}`);
  return provider;
}

function normalizeCommand(value) {
  return String(value || '').replace(/\\/g, '/').toLowerCase();
}

function processMatchesProvider(processInfo, provider) {
  if (!processInfo) return false;
  const commandLine = normalizeCommand(processInfo.commandLine || processInfo.CommandLine);
  return provider.expectedProcessCommandIncludes.every(marker => commandLine.includes(normalizeCommand(marker)));
}

function listenerPids(observation) {
  return [...new Set(((observation && observation.listener && observation.listener.listeners) || [])
    .map(value => Number(value.pid)).filter(Number.isInteger))].sort((a, b) => a - b);
}

function requestJson(url, timeoutMs = 1500, requestOptions = {}) {
  return new Promise(resolve => {
    const startedAt = Date.now();
    const parsed = new URL(url);
    const transport = parsed.protocol === 'https:' ? https : http;
    let settled = false;
    const finish = value => {
      if (settled) return;
      settled = true;
      clearTimeout(deadline);
      resolve({ ...value, elapsedMs: Date.now() - startedAt });
    };
    const method = String(requestOptions.method || 'GET').toUpperCase();
    const requestBody = requestOptions.body == null ? null : JSON.stringify(requestOptions.body);
    const headers = { Accept: 'application/json', 'Cache-Control': 'no-cache', 'User-Agent': '@shenxiao/ui-audit-preview-provider' };
    if (requestBody != null) {
      headers['Content-Type'] = 'application/json';
      headers['Content-Length'] = Buffer.byteLength(requestBody);
    }
    const request = transport.request(parsed, {
      method,
      headers,
    }, response => {
      const chunks = [];
      let bytes = 0;
      response.on('data', chunk => {
        if (bytes >= 65536) return;
        const remaining = 65536 - bytes;
        chunks.push(chunk.subarray(0, remaining));
        bytes += Math.min(chunk.length, remaining);
      });
      response.on('end', () => finish({ response, body: Buffer.concat(chunks).toString('utf8') }));
      response.on('error', error => finish({ error }));
    });
    request.on('error', error => finish({ error }));
    request.setTimeout(timeoutMs, () => {
      const error = new Error(`preview provider status timed out after ${timeoutMs}ms`);
      error.code = 'UIAUDIT_PROVIDER_STATUS_TIMEOUT';
      request.destroy(error);
    });
    const deadline = setTimeout(() => {
      const error = new Error(`preview provider status timed out after ${timeoutMs}ms`);
      error.code = 'UIAUDIT_PROVIDER_STATUS_TIMEOUT';
      request.destroy(error);
      finish({ error });
    }, timeoutMs + 25);
    if (requestBody != null) request.write(requestBody);
    request.end();
  });
}

async function probePreviewProviderStatus(provider, options = {}) {
  const url = `http://${provider.controlHost}:${Number(provider.controlPort)}${provider.statusPath}`;
  const outcome = await (options.requestJson || requestJson)(url, Number(provider.statusTimeoutMs || 1500));
  const request = { method: 'GET', url, elapsedMs: Number(outcome.elapsedMs || 0) };
  if (outcome.error) {
    return {
      pass: false, code: outcome.error.code === 'UIAUDIT_PROVIDER_STATUS_TIMEOUT'
        ? 'RESOURCE_TOOL_STATUS_TIMEOUT' : 'RESOURCE_TOOL_STATUS_NETWORK_ERROR',
      url, networkCode: outcome.error.code || null, message: String(outcome.error.message || outcome.error), request,
    };
  }
  const statusCode = Number(outcome.response && outcome.response.statusCode || 0);
  request.statusCode = statusCode;
  if (statusCode !== 200) return { pass: false, code: 'RESOURCE_TOOL_STATUS_HTTP_ERROR', url, statusCode, request };
  let payload;
  try { payload = JSON.parse(String(outcome.body || '')); }
  catch (error) { return { pass: false, code: 'RESOURCE_TOOL_STATUS_INVALID_JSON', url, statusCode, message: error.message, request }; }
  const data = payload && payload.data;
  if (Number(payload && payload.code) !== 0 || !data || typeof data.running !== 'boolean') {
    return { pass: false, code: 'RESOURCE_TOOL_STATUS_INVALID_PAYLOAD', url, statusCode, payload, request };
  }
  return {
    pass: true, code: 'RESOURCE_TOOL_STATUS_READY', url, statusCode, request,
    data: {
      running: data.running,
      state: typeof data.state === 'string' ? data.state : null,
      port: data.port == null ? null : Number(data.port),
      configuredPort: data.configuredPort == null ? null : Number(data.configuredPort),
      url: data.url || null,
      providerPid: data.providerPid == null ? null : Number(data.providerPid),
      controlPid: data.controlPid == null ? null : Number(data.controlPid),
      previewPid: data.previewPid == null ? null : Number(data.previewPid),
      generation: data.generation == null ? null : Number(data.generation),
      threadAlive: typeof data.threadAlive === 'boolean' ? data.threadAlive : null,
      socketBound: typeof data.socketBound === 'boolean' ? data.socketBound : null,
      httpReady: typeof data.httpReady === 'boolean' ? data.httpReady : null,
    },
  };
}

function providerCapability(statusData, controlPids, previewPids) {
  const data = statusData || {};
  const controlPid = controlPids.length === 1 ? controlPids[0] : null;
  const identityMatches = Number.isInteger(controlPid)
    && Number.isInteger(data.providerPid)
    && Number.isInteger(data.controlPid)
    && data.providerPid === controlPid
    && data.controlPid === controlPid;
  const contractFieldsPresent = Number.isInteger(data.generation)
    && typeof data.threadAlive === 'boolean'
    && typeof data.socketBound === 'boolean'
    && typeof data.httpReady === 'boolean'
    && ['ready', 'stale', 'stopped'].includes(data.state);
  const previewIdentityMatches = Number.isInteger(data.previewPid)
    && data.previewPid === controlPid
    && (previewPids.length === 0 || previewPids.every(pid => pid === data.previewPid));
  const recoverable = identityMatches && contractFieldsPresent && previewIdentityMatches
    && data.state === 'stale' && data.running === false;
  return { identityMatches, contractFieldsPresent, previewIdentityMatches, recoverable };
}

function providerRecovery(provider, code, blocking, capability = {}) {
  const recoverable = blocking && capability.recoverable === true;
  const recoveryUrl = `http://${provider.controlHost}:${Number(provider.controlPort)}${provider.recoveryPath}`;
  return {
    schema: 1,
    code: recoverable
      ? 'RESOURCE_TOOL_PREVIEW_PROVIDER_CAS_RECOVERY_AVAILABLE'
      : (blocking ? 'RESOURCE_TOOL_PREVIEW_PROVIDER_CAS_REQUIRED' : 'RESOURCE_TOOL_PREVIEW_NO_RECOVERY_NEEDED'),
    blocking,
    automaticRecoverySupported: recoverable,
    writeEndpointsAllowed: recoverable,
    status: `GET http://${provider.controlHost}:${Number(provider.controlPort)}${provider.statusPath}`,
    recover: recoverable ? `POST ${recoveryUrl}` : null,
    request: recoverable ? {
      method: 'POST',
      url: recoveryUrl,
      body: {
        expectedControlPid: capability.controlPid,
        expectedPreviewPid: capability.previewPid,
        expectedGeneration: capability.generation,
        port: capability.port,
      },
    } : null,
    forbidden: [
      `POST http://${provider.controlHost}:${Number(provider.controlPort)}${provider.stopPath}`,
      `POST http://${provider.controlHost}:${Number(provider.controlPort)}${provider.startPath}`,
      'taskkill', 'Stop-Process', 'os.kill', 'start on an occupied preview port',
    ],
    reason: recoverable
      ? `${code}: exact provider PID, preview PID and generation are available for one compare-and-swap recovery.`
      : (blocking ? `${code}: the running provider does not expose a verified PID/generation compare-and-swap recovery token.` : null),
    requiredContract: provider.recoveryContract,
  };
}

async function inspectResourceToolPreview(profile, routeProbe, ownership, options = {}) {
  const provider = profile && profile.previewProvider;
  if (!provider) return null;
  validatePreviewProvider(provider);
  if (ownership && ownership.owned) {
    return {
      schema: PREVIEW_PROVIDER_OBSERVATION_SCHEMA, providerId: provider.id,
      code: 'RESOURCE_TOOL_PREVIEW_NOT_APPLICABLE_UIAUDIT_OWNED', pass: true, blocking: false,
      recovery: providerRecovery(provider, 'RESOURCE_TOOL_PREVIEW_NOT_APPLICABLE_UIAUDIT_OWNED', false),
    };
  }
  const inspectEndpoint = options.inspectEndpoint;
  if (typeof inspectEndpoint !== 'function') throw new Error('PREVIEW_PROVIDER_ENDPOINT_INSPECTOR_REQUIRED');
  const controlEndpoint = await Promise.resolve(inspectEndpoint({ host: provider.controlHost, port: Number(provider.controlPort) }));
  const previewEndpoint = routeProbe && routeProbe.endpoint && routeProbe.endpoint.before || null;
  const controlPids = listenerPids(controlEndpoint);
  const previewPids = listenerPids(previewEndpoint);
  const controlListeners = controlEndpoint && controlEndpoint.listener && controlEndpoint.listener.listeners || [];
  const controlIdentityMatches = controlPids.length === 1 && controlListeners.length > 0
    && controlListeners.every(value => processMatchesProvider(value.process, provider));
  const sameSourcePid = controlIdentityMatches && previewPids.length > 0
    && previewPids.every(pid => pid === controlPids[0]);
  let status = null;
  let capability = {};
  let code;
  let blocking = false;
  if (!(controlEndpoint && controlEndpoint.listener && controlEndpoint.listener.up === true)) {
    code = 'RESOURCE_TOOL_CONTROL_NOT_LISTENING';
  } else if (!controlIdentityMatches) {
    code = 'RESOURCE_TOOL_CONTROL_IDENTITY_MISMATCH';
    blocking = true;
  } else if (previewPids.length && !sameSourcePid) {
    code = 'RESOURCE_TOOL_PREVIEW_PORT_CONFLICT';
    blocking = true;
  } else {
    status = await (options.probePreviewProviderStatus || probePreviewProviderStatus)(provider, options);
    capability = providerCapability(status && status.data, controlPids, previewPids);
    if (!status.pass) {
      code = 'RESOURCE_TOOL_PREVIEW_STATUS_UNAVAILABLE';
      blocking = true;
    } else if (status.data.state === 'stale' && capability.recoverable) {
      code = 'RESOURCE_TOOL_PREVIEW_STALE_STATE';
      blocking = true;
    } else if (status.data.running && Number(status.data.port) !== Number(profile.port)) {
      code = 'RESOURCE_TOOL_PREVIEW_STATUS_PORT_MISMATCH';
      blocking = true;
    } else if (status.data.running && (!previewPids.length || (sameSourcePid && !routeProbe.pass))) {
      code = 'RESOURCE_TOOL_PREVIEW_STALE_STATE';
      blocking = true;
    } else if (!status.data.running && previewPids.length) {
      code = 'RESOURCE_TOOL_PREVIEW_STATUS_MISMATCH';
      blocking = true;
    } else if (!status.data.running) {
      code = 'RESOURCE_TOOL_PREVIEW_STOPPED';
      blocking = true;
    } else if (routeProbe.pass && sameSourcePid) {
      code = 'RESOURCE_TOOL_PREVIEW_READY';
    } else {
      code = 'RESOURCE_TOOL_PREVIEW_NOT_READY';
      blocking = true;
    }
  }
  return {
    schema: PREVIEW_PROVIDER_OBSERVATION_SCHEMA,
    providerId: provider.id,
    code,
    pass: code === 'RESOURCE_TOOL_PREVIEW_READY' || code === 'RESOURCE_TOOL_CONTROL_NOT_LISTENING',
    blocking,
    controlEndpoint,
    previewEndpoint,
    status,
    identity: {
      controlPids, previewPids, controlIdentityMatches, sameSourcePid,
      expectedProcessCommandIncludes: provider.expectedProcessCommandIncludes,
    },
    route: { pass: !!(routeProbe && routeProbe.pass), code: routeProbe && routeProbe.code || null, causeCode: routeProbe && routeProbe.causeCode || null },
    recovery: providerRecovery(provider, code, blocking, {
      ...capability,
      controlPid: controlPids.length === 1 ? controlPids[0] : null,
      previewPid: status && status.data && status.data.previewPid,
      generation: status && status.data && status.data.generation,
      port: Number(profile.port),
    }),
  };
}

async function recoverPreviewProvider(profile, providerObservation, options = {}) {
  const provider = profile && profile.previewProvider;
  const recovery = providerObservation && providerObservation.recovery;
  if (!provider || !recovery || !recovery.automaticRecoverySupported || !recovery.request) {
    return { pass: false, code: 'RESOURCE_TOOL_PREVIEW_RECOVERY_NOT_ALLOWED', attempted: false };
  }
  const outcome = await (options.recoverRequestJson || requestJson)(
    recovery.request.url,
    Number(provider.recoveryTimeoutMs || 8000),
    { method: 'POST', body: recovery.request.body },
  );
  const request = {
    method: 'POST', url: recovery.request.url, body: recovery.request.body,
    elapsedMs: Number(outcome.elapsedMs || 0),
  };
  if (outcome.error) {
    return { pass: false, code: 'RESOURCE_TOOL_PREVIEW_RECOVERY_NETWORK_ERROR', attempted: true, request, networkCode: outcome.error.code || null };
  }
  const statusCode = Number(outcome.response && outcome.response.statusCode || 0);
  request.statusCode = statusCode;
  let payload;
  try { payload = JSON.parse(String(outcome.body || '')); }
  catch (_) { return { pass: false, code: 'RESOURCE_TOOL_PREVIEW_RECOVERY_INVALID_JSON', attempted: true, request }; }
  if (statusCode !== 200 || Number(payload && payload.code) !== 0) {
    return {
      pass: false, code: 'RESOURCE_TOOL_PREVIEW_RECOVERY_REJECTED', attempted: true, request,
      providerCode: payload && payload.data && payload.data.errorCode || null,
    };
  }
  return { pass: true, code: 'RESOURCE_TOOL_PREVIEW_RECOVERED', attempted: true, request };
}

function applyProviderObservationToProbe(routeProbe, providerObservation) {
  if (!providerObservation) return routeProbe;
  if (!providerObservation.blocking) return { ...routeProbe, previewProvider: providerObservation };
  return {
    ...routeProbe,
    pass: false,
    ready: false,
    code: providerObservation.code,
    causeCode: routeProbe.code,
    transportCauseCode: routeProbe.causeCode || null,
    category: 'resource-tool-preview',
    retryable: false,
    previewProvider: providerObservation,
    classification: { code: providerObservation.code, state: 'provider-lifecycle-blocked', causeCode: routeProbe.code },
  };
}

function applyProviderObservationToRecovery(recovery, providerObservation) {
  if (!providerObservation) return recovery;
  if (!providerObservation.blocking) return { ...recovery, previewProvider: providerObservation.recovery };
  return {
    ...recovery,
    start: null,
    stopOwned: null,
    runWithEnsure: null,
    automatic: { ...recovery.automatic, startAllowed: false, stopAllowed: false, retryAllowed: false },
    userActionRequired: true,
    reason: providerObservation.recovery.reason,
    safeActions: [recovery.status, providerObservation.recovery.status, 'Apply the versioned sibling provider contract, then rerun status/preflight.'].filter(Boolean),
    previewProvider: providerObservation.recovery,
  };
}

module.exports = {
  PREVIEW_PROVIDER_SCHEMA_VERSION,
  PREVIEW_PROVIDER_OBSERVATION_SCHEMA,
  validatePreviewProvider,
  normalizeCommand,
  processMatchesProvider,
  listenerPids,
  requestJson,
  probePreviewProviderStatus,
  providerRecovery,
  providerCapability,
  inspectResourceToolPreview,
  recoverPreviewProvider,
  applyProviderObservationToProbe,
  applyProviderObservationToRecovery,
};
