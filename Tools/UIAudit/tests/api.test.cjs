'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const uiAudit = require('..');

test('public root entry exposes versioned callable modules', () => {
  assert.equal(uiAudit.version.versionInfo().version, '1.0.0');
  for (const name of ['safeJson', 'runtimeTree', 'canvasInput', 'popupPolicy', 'itemUse', 'protocolProbe', 'preflight', 'session', 'report', 'routeRunner']) {
    assert.equal(typeof uiAudit[name], 'object', name);
  }
  assert.equal(typeof uiAudit.routeRunner.runRoute, 'function');
});
