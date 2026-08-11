'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { createReport, finalizeReport } = require('../lib/report.cjs');
const { executeStep } = require('../lib/route-runner.cjs');

test('route step timing separates fixed settle candidates from action and evidence time', async () => {
  const outputDir = fs.mkdtempSync(path.join(os.tmpdir(), 'ui-audit-timing-'));
  try {
    const report = createReport({ id: 'timing', schema: 1, engine: 'legacy-laya', url: 'http://fixture' }, {
      timing: { startedAt: '2026-08-11T14:00:00.000Z', endedAt: '2026-08-11T14:00:00.005Z', durationMs: 5 },
    });
    const session = { snapshot: async () => ({ visibleViews: [], nodes: [] }) };
    const context = { session, outputDir, protocolPolicy: {}, report, snapshots: new Map(), lastClickAt: null, lastClickLabel: null };
    const entry = await executeStep(context, { action: 'snapshot', label: 'evidence', screenshot: false, settleMs: 2 }, 0);
    assert.equal(entry.status, 'passed');
    assert.equal(entry.timing.category, 'evidence');
    assert.equal(entry.timing.configuredSettleMs, 2);
    assert.ok(entry.timing.durationMs >= 2);
    finalizeReport(report, 'passed');
    assert.equal(report.timing.summary.configuredSettleMs, 2);
    assert.equal(report.timing.summary.fixedWaitSavingsCeilingMs, 2);
    assert.equal(report.timing.summary.slowestSteps[0].label, 'evidence');
  } finally {
    fs.rmSync(outputDir, { recursive: true, force: true });
  }
});

test('failed steps remain visible with their elapsed timing', async () => {
  const report = createReport({ id: 'timing-fail', schema: 1, engine: 'legacy-laya', url: 'http://fixture' }, null);
  const context = {
    session: { snapshot: async () => ({ visibleViews: [], nodes: [] }) },
    outputDir: os.tmpdir(), protocolPolicy: {}, report, snapshots: new Map(), lastClickAt: null, lastClickLabel: null,
  };
  await assert.rejects(() => executeStep(context, { action: 'snapshot', label: 'bad-sample', samplingTargetMs: 10 }, 0), /WITHOUT_CLICK/);
  assert.equal(report.steps.length, 1);
  assert.equal(report.steps[0].status, 'failed');
  assert.equal(report.steps[0].label, 'bad-sample');
});
