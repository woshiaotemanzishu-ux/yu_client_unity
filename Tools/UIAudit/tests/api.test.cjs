'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const uiAudit = require('..');

test('public root entry exposes versioned callable modules', () => {
  assert.equal(uiAudit.version.versionInfo().version, '1.2.1');
  assert.equal(uiAudit.version.versionInfo().schemas.runtimeNode, 3);
  assert.equal(uiAudit.version.versionInfo().schemas.runtimeOverlayPolicy, 1);
  assert.equal(uiAudit.version.versionInfo().schemas.canvasInput, 1);
  assert.equal(typeof uiAudit.popupLifecycle.observePopupLifecycle, 'function');
  assert.equal(uiAudit.version.versionInfo().schemas.protocolTrace, 3);
  assert.equal(uiAudit.version.versionInfo().schemas.serverObservation, 1);
  assert.equal(uiAudit.version.versionInfo().schemas.resourceToolPreview, 1);
  assert.equal(uiAudit.version.versionInfo().schemas.completedScopePolicy, 1);
  for (const name of ['safeJson', 'runtimeTree', 'runtimeOverlay', 'selectorDiagnostic', 'canvasInput', 'popupPolicy', 'itemUse', 'protocolProbe', 'routeAssertions', 'runtimeProbes', 'serverReadiness', 'serverLifecycle', 'resourceToolPreview', 'completedScope', 'preflight', 'session', 'report', 'routeRunner']) {
    assert.equal(typeof uiAudit[name], 'object', name);
  }
  assert.equal(typeof uiAudit.routeRunner.runRoute, 'function');
});
