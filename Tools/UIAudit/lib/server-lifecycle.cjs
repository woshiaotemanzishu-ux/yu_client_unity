'use strict';

const fs = require('fs');
const os = require('os');
const path = require('path');
const crypto = require('crypto');
const childProcess = require('child_process');
const { loadServerProfile, resolvedServerProfile, probeRouteUrl } = require('./server-readiness.cjs');

const TRANSIENT_READINESS_CODES = new Set(['ROUTE_URL_TIMEOUT', 'CONNECTION_RESET']);
const ROUTE_MISMATCH_CODES = new Set([
  'HTTP_STATUS_NOT_READY', 'ROUTE_CONTENT_TYPE_NOT_READY', 'ROUTE_CONTENT_NOT_READY',
  'ROUTE_RESOURCE_NOT_READY', 'TOO_MANY_REDIRECTS',
]);

function runtimeDirectory() {
  return path.join(os.tmpdir(), 'shenxiao-ui-audit');
}

function runtimePaths(profileId, directory = runtimeDirectory()) {
  return {
    directory,
    state: path.join(directory, `${profileId}.owner.json`),
    log: path.join(directory, `${profileId}.log`),
  };
}

function readOwnerState(filePath) {
  try { return JSON.parse(fs.readFileSync(filePath, 'utf8')); }
  catch (_) { return null; }
}

function writeOwnerState(filePath, state) {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  const temporary = `${filePath}.${process.pid}.${crypto.randomUUID()}.tmp`;
  fs.writeFileSync(temporary, `${JSON.stringify(state, null, 2)}\n`, 'utf8');
  fs.renameSync(temporary, filePath);
}

function inspectProcess(pid) {
  if (!Number.isInteger(Number(pid)) || Number(pid) <= 0) return null;
  if (process.platform === 'win32') {
    const command = `$p = Get-CimInstance Win32_Process -Filter \"ProcessId = ${Number(pid)}\" -ErrorAction SilentlyContinue; if ($p) { [pscustomobject]@{ ProcessId=[int]$p.ProcessId; Name=[string]$p.Name; ExecutablePath=[string]$p.ExecutablePath; CommandLine=[string]$p.CommandLine; CreationDate=if ($p.CreationDate) { $p.CreationDate.ToUniversalTime().ToString('o') } else { $null } } | ConvertTo-Json -Compress }`;
    const result = childProcess.spawnSync('powershell.exe', ['-NoProfile', '-NonInteractive', '-Command', command], {
      encoding: 'utf8', windowsHide: true, timeout: 5000,
    });
    if (result.status !== 0 || !String(result.stdout || '').trim()) return null;
    try { return JSON.parse(String(result.stdout).trim()); } catch (_) { return null; }
  }
  try {
    const commandLine = fs.readFileSync(`/proc/${Number(pid)}/cmdline`, 'utf8').split('\0').join(' ');
    return { ProcessId: Number(pid), ExecutablePath: null, CommandLine: commandLine };
  } catch (_) { return null; }
}

function redactCommandLine(commandLine) {
  return String(commandLine || '')
    .replace(/(--(?:owner-token|secret|token|password|passwd|pwd))(?:=|\s+)([^\s]+)/gi, '$1=<redacted>');
}

function publicProcessInfo(processInfo, now = Date.now()) {
  if (!processInfo) return null;
  const creationDate = processInfo.CreationDate || processInfo.creationDate || null;
  const createdMs = creationDate ? Date.parse(creationDate) : NaN;
  return {
    pid: Number(processInfo.ProcessId || processInfo.pid),
    name: processInfo.Name || processInfo.name || null,
    executablePath: processInfo.ExecutablePath || processInfo.executablePath || null,
    commandLine: redactCommandLine(processInfo.CommandLine || processInfo.commandLine),
    creationDate,
    ageMs: Number.isFinite(createdMs) ? Math.max(0, now - createdMs) : null,
  };
}

function inspectEndpoint(profile) {
  const startedAt = Date.now();
  const port = Number(profile && profile.port);
  if (!Number.isInteger(port) || port < 1 || port > 65535) throw new Error(`SERVER_PORT_INVALID: ${profile && profile.port}`);
  if (process.platform !== 'win32') {
    return {
      schema: 'ui-audit.server-endpoint-observation.v1', observedAt: new Date().toISOString(),
      elapsedMs: Date.now() - startedAt, host: profile.host, port, listener: { up: null, listeners: [] },
      inspectError: 'LISTENER_PROCESS_INSPECTION_UNAVAILABLE',
    };
  }
  const command = [
    `$rows = @(Get-NetTCPConnection -State Listen -LocalPort ${port} -ErrorAction SilentlyContinue | Sort-Object OwningProcess,LocalAddress -Unique)`,
    '$listeners = @()',
    'foreach ($row in $rows) {',
    '  $p = Get-CimInstance Win32_Process -Filter ("ProcessId = " + [int]$row.OwningProcess) -ErrorAction SilentlyContinue',
    '  $proc = $null',
    '  if ($p) { $proc = [pscustomobject]@{ ProcessId=[int]$p.ProcessId; Name=[string]$p.Name; ExecutablePath=[string]$p.ExecutablePath; CommandLine=[string]$p.CommandLine; CreationDate=if ($p.CreationDate) { $p.CreationDate.ToUniversalTime().ToString("o") } else { $null } } }',
    '  $listeners += [pscustomobject]@{ LocalAddress=[string]$row.LocalAddress; LocalPort=[int]$row.LocalPort; OwningProcess=[int]$row.OwningProcess; State=[string]$row.State; Process=$proc }',
    '}',
    '[pscustomobject]@{ Listeners=@($listeners) } | ConvertTo-Json -Depth 5 -Compress',
  ].join('; ');
  const result = childProcess.spawnSync('powershell.exe', ['-NoProfile', '-NonInteractive', '-Command', command], {
    encoding: 'utf8', windowsHide: true, timeout: 5000,
  });
  if (result.status !== 0 || !String(result.stdout || '').trim()) {
    return {
      schema: 'ui-audit.server-endpoint-observation.v1', observedAt: new Date().toISOString(),
      elapsedMs: Date.now() - startedAt, host: profile.host, port, listener: { up: null, listeners: [] },
      inspectError: String(result.stderr || 'LISTENER_PROCESS_INSPECTION_FAILED').trim(),
    };
  }
  try {
    const parsed = JSON.parse(String(result.stdout).trim());
    const rows = Array.isArray(parsed.Listeners) ? parsed.Listeners : parsed.Listeners ? [parsed.Listeners] : [];
    const listeners = rows.map(row => ({
      localAddress: row.LocalAddress,
      localPort: Number(row.LocalPort),
      pid: Number(row.OwningProcess),
      state: row.State,
      process: publicProcessInfo(row.Process),
    }));
    return {
      schema: 'ui-audit.server-endpoint-observation.v1', observedAt: new Date().toISOString(),
      elapsedMs: Date.now() - startedAt, host: profile.host, port,
      listener: { up: listeners.length > 0, identity: listeners.map(value => value.pid).sort((a, b) => a - b).join(','), listeners },
      inspectError: null,
    };
  } catch (error) {
    return {
      schema: 'ui-audit.server-endpoint-observation.v1', observedAt: new Date().toISOString(),
      elapsedMs: Date.now() - startedAt, host: profile.host, port, listener: { up: null, listeners: [] },
      inspectError: `LISTENER_PROCESS_INSPECTION_PARSE_FAILED: ${error.message}`,
    };
  }
}

function normalizeEndpointObservation(observation) {
  if (!observation || typeof observation !== 'object') return observation;
  const listeners = ((observation.listener && observation.listener.listeners) || []).map(listener => ({
    ...listener,
    process: publicProcessInfo(listener.process),
  }));
  return {
    ...observation,
    listener: {
      ...(observation.listener || {}),
      listeners,
      identity: observation.listener && observation.listener.identity != null
        ? String(observation.listener.identity)
        : listeners.map(value => value.pid).sort((a, b) => a - b).join(','),
    },
  };
}

function verifyProcessOwnership(state, processInfo) {
  if (!state || !processInfo) return false;
  const commandLine = String(processInfo.CommandLine || '');
  return Number(processInfo.ProcessId) === Number(state.pid)
    && commandLine.includes(String(state.worker))
    && commandLine.includes(String(state.ownerToken));
}

function assessOwnerState(owner, owned, processInfo, endpoint) {
  const listenerPids = new Set(((endpoint && endpoint.listener && endpoint.listener.listeners) || []).map(value => Number(value.pid)));
  if (!owner) return { code: 'NO_OWNER_STATE', stale: false, ownerPid: null, listenerPids: [...listenerPids] };
  if (owned && listenerPids.has(Number(owner.pid))) return { code: 'OWNED_CURRENT', stale: false, ownerPid: Number(owner.pid), listenerPids: [...listenerPids] };
  if (!processInfo) return { code: 'STALE_OWNER_STATE_PROCESS_MISSING', stale: true, ownerPid: Number(owner.pid), listenerPids: [...listenerPids] };
  if (!owned) return { code: 'OWNER_IDENTITY_MISMATCH', stale: true, ownerPid: Number(owner.pid), listenerPids: [...listenerPids] };
  if (endpoint && endpoint.listener && endpoint.listener.up === true && !listenerPids.has(Number(owner.pid))) {
    return { code: 'STALE_OWNER_STATE_LISTENER_MISMATCH', stale: true, ownerPid: Number(owner.pid), listenerPids: [...listenerPids] };
  }
  return { code: 'OWNER_STATE_NOT_LISTENING', stale: false, ownerPid: Number(owner.pid), listenerPids: [...listenerPids] };
}

function serverRecovery(profile, context = {}) {
  const listenerState = context.listenerUp;
  const listenerUp = listenerState === true;
  const listenerDown = listenerState === false;
  const owned = context.owned === true;
  const externalOccupied = listenerUp && !owned;
  const inspectionUnknown = !listenerUp && !listenerDown;
  const startPermitted = listenerDown && !owned;
  const needsExternalAction = (externalOccupied && context.ready !== true) || inspectionUnknown;
  return {
    profileId: profile.id,
    start: startPermitted ? `node Tools/UIAudit/cli.cjs server start --profile ${profile.id}` : null,
    status: `node Tools/UIAudit/cli.cjs server status --profile ${profile.id}`,
    stopOwned: owned ? `node Tools/UIAudit/cli.cjs server stop --profile ${profile.id}` : null,
    runWithEnsure: externalOccupied ? null : 'node Tools/UIAudit/cli.cjs run --ensure-server --route <route.json> --output <new-run>',
    automatic: {
      startAllowed: startPermitted,
      stopAllowed: owned,
      retryAllowed: false,
      retryPerformed: context.retryPerformed === true,
    },
    userActionRequired: needsExternalAction,
    reason: externalOccupied
      ? 'External listener occupies the configured port; UIAudit must not stop, replace, or start over it.'
      : inspectionUnknown ? 'Listener ownership could not be inspected; UIAudit must not start or stop a process until status is known.' : null,
    safeActions: needsExternalAction
      ? [`node Tools/UIAudit/cli.cjs server status --profile ${profile.id}`, 'Inspect or restart the reported external process outside UIAudit, then rerun status/preflight.']
      : [],
  };
}

function endpointIdentity(observation) {
  return observation && observation.listener && observation.listener.identity || null;
}

function classifyServerProbe(probe, ownership, endpointBefore, endpointAfter) {
  if (probe.pass) return { code: 'ROUTE_READY', state: 'route-ready', causeCode: null };
  const beforeUp = endpointBefore && endpointBefore.listener && endpointBefore.listener.up;
  const afterUp = endpointAfter && endpointAfter.listener && endpointAfter.listener.up;
  if (typeof beforeUp === 'boolean' && typeof afterUp === 'boolean' && beforeUp !== afterUp) {
    return { code: 'EXTERNAL_SERVER_STATE_CHANGED', state: beforeUp ? 'listener-disappeared' : 'listener-appeared', causeCode: probe.code };
  }
  const identityChanged = beforeUp === true && afterUp === true
    && endpointIdentity(endpointBefore) !== endpointIdentity(endpointAfter);
  if (identityChanged) return { code: 'EXTERNAL_SERVER_STATE_CHANGED', state: 'listener-identity-changed', causeCode: probe.code };
  if (beforeUp === false && afterUp === false) return { code: 'SERVER_NOT_RUNNING', state: 'listener-down', causeCode: probe.code };
  if (ownership.owned) return { code: 'SERVER_OWNED_NOT_READY', state: 'owned-route-not-ready', causeCode: probe.code };
  if (beforeUp === true || afterUp === true) {
    if (ROUTE_MISMATCH_CODES.has(probe.code)) {
      return { code: 'EXTERNAL_SERVER_ROUTE_MISMATCH', state: 'listener-up-route-mismatch', causeCode: probe.code };
    }
    if (TRANSIENT_READINESS_CODES.has(probe.code) || probe.code === 'ROUTE_NETWORK_ERROR') {
      return { code: 'EXTERNAL_SERVER_UNRESPONSIVE', state: 'listener-up-route-unresponsive', causeCode: probe.code };
    }
    if (probe.code === 'SERVER_NOT_RUNNING') {
      return { code: 'EXTERNAL_SERVER_UNRESPONSIVE', state: 'listener-up-connection-refused', causeCode: probe.code };
    }
    return { code: 'EXTERNAL_SERVER_NOT_READY', state: 'listener-up-route-not-ready', causeCode: probe.code };
  }
  return { code: probe.code, state: 'listener-state-unknown', causeCode: probe.code };
}

async function probeServerRoute(profile, ownership, options = {}) {
  const inspect = options.inspectEndpoint || inspectEndpoint;
  const probe = options.probeRouteUrl || probeRouteUrl;
  const sleep = options.sleep || (milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds)));
  const readiness = { ...(profile.readiness || {}), ...(options.readiness || {}) };
  const retry = readiness.transientRetry || {};
  const maxAttempts = Math.max(1, Math.min(5, Number(retry.maxAttempts || 1)));
  const retryCodes = new Set(Array.isArray(retry.codes) ? retry.codes : [...TRANSIENT_READINESS_CODES]);
  const backoffMs = Array.isArray(retry.backoffMs) ? retry.backoffMs.map(Number) : [250];
  const endpointBefore = normalizeEndpointObservation(options.endpointBefore || await Promise.resolve(inspect(profile)));
  let endpointAfter = endpointBefore;
  let last = null;
  const attempts = [];
  for (let attempt = 1; attempt <= maxAttempts; attempt++) {
    const attemptStartedAt = Date.now();
    last = await probe(options.url || profile.url, readiness);
    attempts.push({
      attempt, startedAt: new Date(attemptStartedAt).toISOString(), elapsedMs: Date.now() - attemptStartedAt,
      code: last.code, pass: !!last.pass, requests: last.requests || [],
    });
    if (last.pass) break;
    endpointAfter = normalizeEndpointObservation(await Promise.resolve(inspect(profile)));
    const stableListener = endpointBefore.listener && endpointBefore.listener.up === true
      && endpointAfter.listener && endpointAfter.listener.up === true
      && endpointIdentity(endpointBefore) === endpointIdentity(endpointAfter);
    const provableTransient = !ownership.owned && stableListener && retryCodes.has(last.code);
    attempts[attempts.length - 1].retryEvidence = {
      listenerUp: endpointAfter.listener && endpointAfter.listener.up,
      listenerIdentityStable: stableListener,
      codeAllowed: retryCodes.has(last.code),
      retryScheduled: provableTransient && attempt < maxAttempts,
    };
    if (!provableTransient || attempt >= maxAttempts) break;
    const delayMs = Math.max(0, Number(backoffMs[Math.min(attempt - 1, backoffMs.length - 1)] || 0));
    attempts[attempts.length - 1].backoffMs = delayMs;
    if (delayMs) await sleep(delayMs);
  }
  if (!last) last = { pass: false, ready: false, code: 'ROUTE_NETWORK_ERROR', url: options.url || profile.url };
  const classification = classifyServerProbe(last, ownership, endpointBefore, endpointAfter);
  return {
    ...last,
    code: classification.code,
    causeCode: classification.causeCode,
    category: classification.code.startsWith('EXTERNAL_SERVER_') ? 'external-server-readiness' : last.category,
    retryable: false,
    attempts,
    endpoint: { before: endpointBefore, after: endpointAfter },
    classification,
  };
}

async function serverStatus(options = {}) {
  const repoRoot = path.resolve(options.repoRoot || path.join(__dirname, '..', '..', '..'));
  const rawProfile = options.profile || loadServerProfile(options.profileId || 'legacy-h5-local');
  const profile = resolvedServerProfile(rawProfile, repoRoot);
  const paths = runtimePaths(profile.id, options.runtimeDirectory);
  const owner = readOwnerState(paths.state);
  const inspect = options.inspectProcess || inspectProcess;
  const processInfo = owner ? inspect(owner.pid) : null;
  const owned = verifyProcessOwnership(owner, processInfo);
  const ownership = { owned, owner: owner || null, process: publicProcessInfo(processInfo) };
  const probe = await probeServerRoute(profile, ownership, {
    ...options,
    url: options.url || profile.url,
    readiness: options.readiness || profile.readiness,
  });
  let code = probe.code;
  if (probe.pass && owner && !owned) code = 'SERVER_READY_UNOWNED';
  const ownerState = assessOwnerState(owner, owned, processInfo, probe.endpoint.before);
  const listener = probe.endpoint.before && probe.endpoint.before.listener;
  const listenerState = listener && listener.up;
  const listenerUp = listenerState === true;
  const externalState = listenerUp && !owned ? {
    code: probe.classification.state,
    listenerIdentity: listener.identity,
    identityStable: endpointIdentity(probe.endpoint.before) === endpointIdentity(probe.endpoint.after),
    attemptCodes: probe.attempts.map(value => value.code),
    repeatedFailure: probe.attempts.length > 1 && probe.attempts.every(value => !value.pass),
    processes: listener.listeners.map(value => value.process).filter(Boolean),
    stale: null,
    staleAssessment: 'Process age and stable listener identity are evidence only; UIAudit does not infer or terminate a stale external service.',
  } : null;
  const observation = {
    schema: 'ui-audit.server-observation.v1',
    endpoint: probe.endpoint,
    ownerState,
    externalState,
  };
  return {
    pass: probe.pass,
    code,
    profile,
    probe,
    ownership,
    observation,
    runtime: paths,
    recovery: serverRecovery(profile, {
      owned,
      listenerUp: listenerState,
      ready: probe.pass,
      retryPerformed: probe.attempts.some(value => value.retryEvidence && value.retryEvidence.retryScheduled),
    }),
  };
}

async function waitForServer(profile, options = {}) {
  const timeoutMs = Number(options.timeoutMs || profile.readiness.readyTimeoutMs || 120000);
  const processVisibilityGraceMs = Number(options.processVisibilityGraceMs || 1500);
  const startedAt = Date.now();
  const deadline = Date.now() + timeoutMs;
  let last;
  while (Date.now() < deadline) {
    last = await (options.probeRouteUrl || probeRouteUrl)(profile.url, {
      ...profile.readiness,
      timeoutMs: Math.min(Number(profile.readiness.timeoutMs || 2500), Math.max(50, deadline - Date.now())),
    });
    if (last.pass) return last;
    if (options.isProcessAlive && !options.isProcessAlive() && Date.now() - startedAt >= processVisibilityGraceMs) break;
    await new Promise(resolve => setTimeout(resolve, Math.min(250, Math.max(1, deadline - Date.now()))));
  }
  return last || { pass: false, ready: false, code: 'SERVER_READY_TIMEOUT', url: profile.url };
}

async function startServer(options = {}) {
  const repoRoot = path.resolve(options.repoRoot || path.join(__dirname, '..', '..', '..'));
  const rawProfile = options.profile || loadServerProfile(options.profileId || 'legacy-h5-local');
  const profile = resolvedServerProfile(rawProfile, repoRoot);
  const initial = await serverStatus({ ...options, repoRoot, profile: rawProfile });
  if (initial.pass) return { ...initial, started: false, code: initial.ownership.owned ? 'SERVER_ALREADY_OWNED_READY' : 'SERVER_ALREADY_READY' };
  if (initial.ownership.owned) {
    const probe = await waitForServer(profile, {
      ...options,
      isProcessAlive: () => verifyProcessOwnership(initial.ownership.owner, (options.inspectProcess || inspectProcess)(initial.ownership.owner.pid)),
    });
    return { ...(await serverStatus({ ...options, repoRoot, profile: rawProfile })), probe, started: false };
  }
  if (initial.probe.code !== 'SERVER_NOT_RUNNING') {
    return { ...initial, started: false, code: 'SERVER_PORT_OR_CONTENT_CONFLICT' };
  }
  for (const required of [profile.cwd, profile.staticRoot, profile.worker]) {
    if (!fs.existsSync(required)) throw new Error(`SERVER_PROFILE_PATH_MISSING: ${required}`);
  }
  const paths = runtimePaths(profile.id, options.runtimeDirectory);
  fs.mkdirSync(paths.directory, { recursive: true });
  const ownerToken = crypto.randomUUID();
  const logFd = fs.openSync(paths.log, 'a');
  const spawn = options.spawn || childProcess.spawn;
  const child = spawn(process.execPath, [
    profile.worker,
    '--cwd', profile.cwd,
    '--static-root', profile.staticRoot,
    '--host', profile.host,
    '--port', String(profile.port),
    '--owner-token', ownerToken,
  ], {
    cwd: profile.cwd,
    detached: true,
    windowsHide: true,
    stdio: ['ignore', logFd, logFd],
  });
  fs.closeSync(logFd);
  if (!child || !Number.isInteger(Number(child.pid))) throw new Error('SERVER_PROCESS_SPAWN_FAILED');
  const owner = {
    schema: 1,
    profileId: profile.id,
    pid: Number(child.pid),
    ownerToken,
    worker: profile.worker,
    cwd: profile.cwd,
    url: profile.url,
    startedAt: new Date().toISOString(),
  };
  writeOwnerState(paths.state, owner);
  if (typeof child.unref === 'function') child.unref();
  const probe = await waitForServer(profile, {
    ...options,
    isProcessAlive: () => {
      const info = (options.inspectProcess || inspectProcess)(owner.pid);
      return options.spawn ? true : verifyProcessOwnership(owner, info);
    },
  });
  const status = await serverStatus({ ...options, repoRoot, profile: rawProfile });
  if (!status.pass) {
    let cleanup = null;
    if (status.ownership.owned) cleanup = await stopServer({ ...options, repoRoot, profile: rawProfile });
    else {
      try { fs.unlinkSync(paths.state); } catch (_) {}
    }
    return { ...status, code: 'SERVER_START_NOT_READY', probe, started: true, owner, cleanup };
  }
  return { ...status, probe, started: true, owner };
}

async function stopServer(options = {}) {
  const repoRoot = path.resolve(options.repoRoot || path.join(__dirname, '..', '..', '..'));
  const rawProfile = options.profile || loadServerProfile(options.profileId || 'legacy-h5-local');
  const profile = resolvedServerProfile(rawProfile, repoRoot);
  const paths = runtimePaths(profile.id, options.runtimeDirectory);
  const owner = readOwnerState(paths.state);
  if (!owner) return { pass: true, code: 'SERVER_NOT_OWNED', stopped: false, profile, runtime: paths };
  const inspect = options.inspectProcess || inspectProcess;
  const info = inspect(owner.pid);
  if (!verifyProcessOwnership(owner, info)) {
    return { pass: false, code: 'SERVER_OWNER_MISMATCH', stopped: false, profile, owner, process: info, runtime: paths };
  }
  const kill = options.kill || (pid => process.kill(pid, 'SIGTERM'));
  kill(Number(owner.pid));
  const deadline = Date.now() + Number(options.stopTimeoutMs || 5000);
  while (Date.now() < deadline && inspect(owner.pid)) await new Promise(resolve => setTimeout(resolve, 100));
  if (inspect(owner.pid) && process.platform === 'win32' && !options.kill) {
    childProcess.spawnSync('taskkill.exe', ['/PID', String(Number(owner.pid)), '/T', '/F'], { windowsHide: true, timeout: 5000 });
  }
  const remaining = inspect(owner.pid);
  if (remaining) return { pass: false, code: 'SERVER_STOP_FAILED', stopped: false, profile, owner, process: remaining, runtime: paths };
  try { fs.unlinkSync(paths.state); } catch (_) {}
  const probe = await (options.probeRouteUrl || probeRouteUrl)(profile.url, { ...profile.readiness, timeoutMs: 500 });
  return { pass: probe.code === 'SERVER_NOT_RUNNING', code: 'SERVER_OWNED_STOPPED', stopped: true, profile, owner, probe, runtime: paths };
}

async function ensureServer(options = {}) {
  const status = await startServer(options);
  if (!status.pass) throw new Error(`UI_AUDIT_SERVER_ENSURE_FAILED: ${JSON.stringify({ code: status.code, probe: status.probe, recovery: status.recovery })}`);
  return status;
}

module.exports = {
  runtimeDirectory,
  runtimePaths,
  readOwnerState,
  writeOwnerState,
  inspectProcess,
  redactCommandLine,
  publicProcessInfo,
  inspectEndpoint,
  normalizeEndpointObservation,
  verifyProcessOwnership,
  assessOwnerState,
  serverRecovery,
  classifyServerProbe,
  probeServerRoute,
  serverStatus,
  waitForServer,
  startServer,
  stopServer,
  ensureServer,
};
