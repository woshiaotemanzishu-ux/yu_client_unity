'use strict';

const fs = require('fs');
const os = require('os');
const path = require('path');
const crypto = require('crypto');
const childProcess = require('child_process');
const { loadServerProfile, resolvedServerProfile, probeRouteUrl } = require('./server-readiness.cjs');

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
    const command = `$p = Get-CimInstance Win32_Process -Filter \"ProcessId = ${Number(pid)}\" -ErrorAction SilentlyContinue; if ($p) { $p | Select-Object ProcessId,ExecutablePath,CommandLine | ConvertTo-Json -Compress }`;
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

function verifyProcessOwnership(state, processInfo) {
  if (!state || !processInfo) return false;
  const commandLine = String(processInfo.CommandLine || '');
  return Number(processInfo.ProcessId) === Number(state.pid)
    && commandLine.includes(String(state.worker))
    && commandLine.includes(String(state.ownerToken));
}

function serverRecovery(profile) {
  return {
    profileId: profile.id,
    start: `node Tools/UIAudit/cli.cjs server start --profile ${profile.id}`,
    status: `node Tools/UIAudit/cli.cjs server status --profile ${profile.id}`,
    stopOwned: `node Tools/UIAudit/cli.cjs server stop --profile ${profile.id}`,
    runWithEnsure: 'node Tools/UIAudit/cli.cjs run --ensure-server --route <route.json> --output <new-run>',
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
  const probe = await (options.probeRouteUrl || probeRouteUrl)(profile.url, profile.readiness);
  let code = probe.code;
  if (!probe.pass && owned) code = 'SERVER_OWNED_NOT_READY';
  if (probe.pass && owner && !owned) code = 'SERVER_READY_UNOWNED';
  return {
    pass: probe.pass,
    code,
    profile,
    probe,
    ownership: { owned, owner: owner || null, process: processInfo || null },
    runtime: paths,
    recovery: serverRecovery(profile),
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
  verifyProcessOwnership,
  serverRecovery,
  serverStatus,
  waitForServer,
  startServer,
  stopServer,
  ensureServer,
};
