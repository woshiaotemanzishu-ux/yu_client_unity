'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { normalizeRuntimeSources } = require('../lib/runtime-tree.cjs');
const { buildSelectorDiagnostic, SelectorIdentityError } = require('../lib/selector-diagnostic.cjs');
const { createReport, finalizeReport, writeReport } = require('../lib/report.cjs');
const { attachFailureDiagnostic } = require('../lib/route-runner.cjs');
const { loadPopupPolicy } = require('../lib/popup-policy.cjs');
const { HeadlessUiSession } = require('../lib/session.cjs');

const fixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'runtime-owner-bindings.json'), 'utf8'));

test('identity mismatch persists a minimal normalized subtree, candidates and hashes into the failure report', () => {
  const outputDir = fs.mkdtempSync(path.join(os.tmpdir(), 'ui-audit-selector-diagnostic-'));
  try {
    const snapshot = normalizeRuntimeSources(fixture);
    const selector = {
      source: 'laya-stage', ownerView: 'CycleimpActlistYesterday', boundField: '_missing_close', expectedCount: 1,
    };
    const diagnostic = buildSelectorDiagnostic(snapshot, selector, { expectedCount: 1, actualCount: 0 });
    const error = new SelectorIdentityError('CANVAS_TARGET_IDENTITY_MISMATCH', 'fixture mismatch', diagnostic);
    const report = createReport({ schema: 1, id: 'fixture', engine: 'legacy-laya', url: 'http://127.0.0.1/' }, { pass: true });

    const failure = attachFailureDiagnostic(report, outputDir, error);
    finalizeReport(report, 'failed', error);
    const reportPath = writeReport(outputDir, report);
    const persistedReport = JSON.parse(fs.readFileSync(reportPath, 'utf8'));
    const diagnosticPath = path.join(outputDir, failure.diagnostic.artifact.path);
    const persistedDiagnostic = JSON.parse(fs.readFileSync(diagnosticPath, 'utf8'));

    assert.equal(persistedReport.status, 'failed');
    assert.equal(persistedReport.failure.code, 'CANVAS_TARGET_IDENTITY_MISMATCH');
    assert.equal(persistedReport.artifacts[0].kind, 'selector-identity-diagnostic');
    assert.equal(persistedDiagnostic.sha256, diagnostic.sha256);
    assert.equal(persistedDiagnostic.subtree.total, 2);
    assert.match(persistedDiagnostic.subtree.sha256, /^[a-f0-9]{64}$/);
    assert.equal(persistedDiagnostic.subtree.nodes.every(node => node.schema === 'ui-audit.runtime-node.v3'), true);
    assert.equal(persistedDiagnostic.candidates.length > 0, true);
    assert.match(persistedReport.failure.diagnostic.artifact.sha256, /^[a-f0-9]{64}$/);
  } finally {
    fs.rmSync(outputDir, { recursive: true, force: true });
  }
});

test('canvas input failures use a dedicated immutable artifact classification', () => {
  const outputDir = fs.mkdtempSync(path.join(os.tmpdir(), 'ui-audit-canvas-input-diagnostic-'));
  try {
    const snapshot = normalizeRuntimeSources(fixture);
    const selector = {
      source: 'laya-stage', ownerView: 'CycleimpActlistYesterday', boundField: '_btn_close', expectedCount: 1,
    };
    const diagnostic = buildSelectorDiagnostic(snapshot, selector, {
      expectedCount: 1,
      actualCount: 1,
      context: {
        kind: 'canvas-input', phase: 'pre-click',
        input: {
          schema: 'ui-audit.canvas-input.v1',
          evaluation: { pass: false, classification: 'stack-order-wrong' },
          targetChain: [{ ownerView: 'CycleimpActlistYesterday' }],
          topmostChain: [{ ownerView: 'DailyActTipView' }],
        },
      },
    });
    const error = new SelectorIdentityError('CANVAS_STACK_ORDER_WRONG', 'fixture stack mismatch', diagnostic);
    const report = createReport({ schema: 1, id: 'canvas-input-fixture', engine: 'legacy-laya', url: 'http://127.0.0.1/' }, { pass: true });
    const failure = attachFailureDiagnostic(report, outputDir, error);
    assert.equal(failure.diagnostic.artifact.kind, 'canvas-input-diagnostic');
    const persisted = JSON.parse(fs.readFileSync(path.join(outputDir, failure.diagnostic.artifact.path), 'utf8'));
    assert.equal(persisted.context.input.evaluation.classification, 'stack-order-wrong');
    assert.equal(persisted.context.input.topmostChain[0].ownerView, 'DailyActTipView');
  } finally {
    fs.rmSync(outputDir, { recursive: true, force: true });
  }
});

test('popup close timeout persists instance lifecycle sources, frame tokens and selector candidates', async () => {
  const outputDir = fs.mkdtempSync(path.join(os.tmpdir(), 'ui-audit-popup-lifecycle-diagnostic-'));
  try {
    const before = normalizeRuntimeSources(fixture);
    const popupPolicy = structuredClone(loadPopupPolicy(path.join(__dirname, '..', 'policies', 'startup-popups.json')));
    const entry = popupPolicy.entries.find(item => item.view === 'CycleimpActlistYesterday');
    entry.safeClose.stability = {
      ...entry.safeClose.stability,
      timeoutMs: 8,
      pollMs: 1,
    };
    let snapshots = 0;
    const session = new HeadlessUiSession({});
    session.page = {
      viewport: () => ({ width: 720, height: 1280 }),
      evaluate: async (_function, payload) => {
        if (payload && Object.hasOwn(payload, 'logicalWidth')) {
          return { x: 0, y: 0, width: 720, height: 1280, logicalWidth: 720, logicalHeight: 1280, canvasIndex: 0 };
        }
        if (payload && payload.operation === 'inspect-canvas-input') {
          const target = {
            path: payload.indexPath.join('/'), indexPath: payload.indexPath,
            ownerView: payload.selector.ownerView, hitAtPoint: true,
          };
          return {
            schema: 'ui-audit.canvas-input.v1', applicable: true,
            targetResolution: { actualCount: 1 }, target, targetChain: [target],
            topmost: target, topmostChain: [target], capture: null,
            mapping: { roundTripPass: true, pointInsideCanvas: true, domCanvasTop: true },
          };
        }
        if (payload && payload.operation === 'install-canvas-input-probe') return { pass: true, probeId: payload.probeId };
        if (payload && payload.operation === 'finish-canvas-input-probe') {
          return {
            schema: 'ui-audit.canvas-input.v1', probeId: payload.probeId,
            domEvents: [{ type: 'mousedown' }, { type: 'mouseup' }],
            targetEvents: [{ type: 'click', listenerCountBefore: 3, dispatched: true }],
            semanticCalls: [{ name: 'Close' }],
          };
        }
        throw new Error(`unexpected page.evaluate payload: ${JSON.stringify(payload)}`);
      },
      mouse: { click: async () => {} },
    };
    session.snapshot = async () => {
      snapshots++;
      const value = structuredClone(before);
      value.stage.frameToken = 800 + snapshots;
      return value;
    };

    let caught = null;
    try {
      await session.closeAllowlistedPopup('CycleimpActlistYesterday', popupPolicy);
    } catch (error) {
      caught = error;
    }
    assert.equal(caught && caught.code, 'POPUP_CLOSE_NOT_STABLE');
    assert.equal(caught.diagnostic.context.kind, 'popup-close-lifecycle');
    assert.equal(caught.diagnostic.context.evaluation.classification, 'business-handled-but-not-closed');
    assert.equal(caught.diagnostic.context.input.consumption.classification, 'target-click-consumed');
    assert.equal(caught.diagnostic.context.samples.length > 0, true);
    assert.equal(caught.diagnostic.context.samples.every(sample => Number.isFinite(sample.lifecycle.frameToken)), true);
    assert.equal(caught.diagnostic.context.samples.every(sample => sample.lifecycle.sources.length >= 3), true);
    assert.equal(caught.diagnostic.context.initialSelector.subtree.total, 2);
    assert.match(caught.diagnostic.context.initialSelector.subtree.sha256, /^[a-f0-9]{64}$/);
    assert.equal(caught.diagnostic.subtree.total, 2);
    assert.equal(caught.diagnostic.candidates.length > 0, true);

    const report = createReport({ schema: 1, id: 'popup-fixture', engine: 'legacy-laya', url: 'http://127.0.0.1/' }, { pass: true });
    const failure = attachFailureDiagnostic(report, outputDir, caught);
    finalizeReport(report, 'failed', caught);
    const reportPath = writeReport(outputDir, report);
    const persistedReport = JSON.parse(fs.readFileSync(reportPath, 'utf8'));
    const diagnosticPath = path.join(outputDir, failure.diagnostic.artifact.path);
    const persistedDiagnostic = JSON.parse(fs.readFileSync(diagnosticPath, 'utf8'));
    assert.equal(persistedReport.failure.code, 'POPUP_CLOSE_NOT_STABLE');
    assert.equal(persistedReport.artifacts[0].kind, 'popup-close-lifecycle-diagnostic');
    assert.equal(persistedDiagnostic.context.evaluation.classification, 'business-handled-but-not-closed');
    assert.equal(persistedDiagnostic.sha256, caught.diagnostic.sha256);
  } finally {
    fs.rmSync(outputDir, { recursive: true, force: true });
  }
});
