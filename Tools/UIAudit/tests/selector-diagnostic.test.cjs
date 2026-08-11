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
    assert.equal(persistedDiagnostic.subtree.nodes.every(node => node.schema === 'ui-audit.runtime-node.v1'), true);
    assert.equal(persistedDiagnostic.candidates.length > 0, true);
    assert.match(persistedReport.failure.diagnostic.artifact.sha256, /^[a-f0-9]{64}$/);
  } finally {
    fs.rmSync(outputDir, { recursive: true, force: true });
  }
});
