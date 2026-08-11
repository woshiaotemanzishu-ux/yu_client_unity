'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { runtimePaths, writeOwnerState, verifyProcessOwnership, stopServer } = require('../lib/server-lifecycle.cjs');

test('owned process identity requires PID, worker path and private owner token', () => {
  const owner = { pid: 123, worker: 'legacy-h5-worker.cjs', ownerToken: 'private-token' };
  assert.equal(verifyProcessOwnership(owner, { ProcessId: 123, CommandLine: 'node legacy-h5-worker.cjs --owner-token private-token' }), true);
  assert.equal(verifyProcessOwnership(owner, { ProcessId: 123, CommandLine: 'node another-server.cjs' }), false);
  assert.equal(verifyProcessOwnership(owner, { ProcessId: 999, CommandLine: 'node legacy-h5-worker.cjs --owner-token private-token' }), false);
});

test('stop refuses an unowned PID and never calls the killer', async () => {
  const directory = fs.mkdtempSync(path.join(os.tmpdir(), 'ui-audit-server-test-'));
  const paths = runtimePaths('legacy-h5-local', directory);
  const profile = {
    schema: 1, id: 'legacy-h5-local', matchOrigins: ['http://127.0.0.1:8091'], host: '127.0.0.1', port: 8091,
    cwdFromRepo: '../yu_client/h5', staticRootFromRepo: '../yu_client/cdn', workerFromTool: 'server/legacy-h5-worker.cjs',
  };
  writeOwnerState(paths.state, { pid: 123, worker: 'legacy-h5-worker.cjs', ownerToken: 'private-token' });
  let killed = false;
  const result = await stopServer({
    profile,
    runtimeDirectory: directory,
    inspectProcess: () => ({ ProcessId: 123, CommandLine: 'node unrelated.cjs' }),
    kill: () => { killed = true; },
  });
  assert.equal(result.code, 'SERVER_OWNER_MISMATCH');
  assert.equal(killed, false);
  assert.equal(fs.existsSync(paths.state), true);
});
