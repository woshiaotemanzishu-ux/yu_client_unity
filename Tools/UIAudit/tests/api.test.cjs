'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const uiAudit = require('..');

test('public root entry exposes versioned callable modules', () => {
  assert.equal(uiAudit.version.versionInfo().version, '1.1.5');
  assert.equal(uiAudit.version.versionInfo().schemas.runtimeNode, 2);
  assert.equal(uiAudit.version.versionInfo().schemas.canvasInput, 1);
  assert.equal(typeof uiAudit.popupLifecycle.observePopupLifecycle, 'function');
  assert.equal(uiAudit.version.versionInfo().schemas.protocolTrace, 3);
  for (const name of ['safeJson', 'runtimeTree', 'selectorDiagnostic', 'canvasInput', 'popupPolicy', 'itemUse', 'protocolProbe', 'routeAssertions', 'runtimeProbes', 'serverReadiness', 'serverLifecycle', 'preflight', 'session', 'report', 'routeRunner']) {
    assert.equal(typeof uiAudit[name], 'object', name);
  }
  assert.equal(typeof uiAudit.routeRunner.runRoute, 'function');
});
