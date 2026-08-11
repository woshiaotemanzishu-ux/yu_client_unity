'use strict';

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { REPORT_SCHEMA_VERSION, versionInfo } = require('./version.cjs');
const { writeJsonAtomic } = require('./safe-json.cjs');

function sha256File(filePath) {
  return crypto.createHash('sha256').update(fs.readFileSync(filePath)).digest('hex');
}

function createReport(route, preflight) {
  const report = {
    schema: REPORT_SCHEMA_VERSION,
    tool: versionInfo(),
    route: { id: route.id, schema: route.schema, engine: route.engine, url: route.url },
    startedAt: new Date().toISOString(),
    endedAt: null,
    status: 'running',
    preflight,
    session: null,
    events: [],
    steps: [],
    protocol: null,
    artifacts: [],
    error: null,
    failure: null,
    timing: {
      phases: [],
      summary: null,
    },
  };
  if (preflight && preflight.timing) {
    report.timing.phases.push({
      id: 'preflight',
      category: 'environment-check',
      startedAt: preflight.timing.startedAt,
      endedAt: preflight.timing.endedAt,
      durationMs: Number(preflight.timing.durationMs || 0),
    });
  }
  return report;
}

function startPhase(report, id, category) {
  const startedAtMs = Date.now();
  const phase = { id, category, startedAt: new Date(startedAtMs).toISOString(), endedAt: null, durationMs: null };
  Object.defineProperty(phase, '_startedAtMs', { value: startedAtMs, writable: true, enumerable: false });
  report.timing.phases.push(phase);
  return phase;
}

function endPhase(phase) {
  const endedAtMs = Date.now();
  phase.endedAt = new Date(endedAtMs).toISOString();
  const startedAtMs = phase._startedAtMs == null ? Date.parse(phase.startedAt) : phase._startedAtMs;
  phase.durationMs = Math.max(0, endedAtMs - startedAtMs);
  return phase;
}

async function measurePhase(report, id, category, action) {
  const phase = startPhase(report, id, category);
  try {
    return await action();
  } finally {
    endPhase(phase);
  }
}

function buildTimingSummary(report) {
  const steps = report.steps || [];
  const phases = report.timing && report.timing.phases || [];
  const totalDurationMs = report.startedAt && report.endedAt
    ? Math.max(0, Date.parse(report.endedAt) - Date.parse(report.startedAt)) : 0;
  const stepDurationMs = steps.reduce((sum, step) => sum + Number(step.timing && step.timing.durationMs || 0), 0);
  const configuredSettleMs = steps.reduce((sum, step) => sum + Number(step.timing && step.timing.configuredSettleMs || 0), 0);
  const samplingWaitMs = steps.reduce((sum, step) => sum + Number(step.timing && step.timing.samplingWaitMs || 0), 0);
  const runtimeReadyWaitMs = steps
    .filter(step => ['wait-view', 'wait-render-ready'].includes(step.action))
    .reduce((sum, step) => sum + Number(step.timing && step.timing.durationMs || 0), 0);
  const byCategoryMs = {};
  for (const phase of phases) byCategoryMs[phase.category] = (byCategoryMs[phase.category] || 0) + Number(phase.durationMs || 0);
  return {
    totalDurationMs,
    phaseDurationMs: phases.reduce((sum, phase) => sum + Number(phase.durationMs || 0), 0),
    stepDurationMs,
    configuredSettleMs,
    samplingWaitMs,
    runtimeReadyWaitMs,
    fixedWaitSavingsCeilingMs: configuredSettleMs,
    byPhaseCategoryMs: byCategoryMs,
    slowestSteps: steps
      .map(step => ({ index: step.index, label: step.label, action: step.action, durationMs: Number(step.timing && step.timing.durationMs || 0), status: step.status || 'passed' }))
      .sort((left, right) => right.durationMs - left.durationMs)
      .slice(0, 10),
  };
}

function addArtifact(report, outputDir, filePath, kind) {
  const absolute = path.resolve(filePath);
  if (!fs.existsSync(absolute)) throw new Error(`REPORT_ARTIFACT_MISSING: ${absolute}`);
  const artifact = {
    kind,
    path: path.relative(outputDir, absolute).replace(/\\/g, '/'),
    bytes: fs.statSync(absolute).size,
    sha256: sha256File(absolute),
  };
  report.artifacts.push(artifact);
  return artifact;
}

function finalizeReport(report, status, error = null) {
  report.endedAt = new Date().toISOString();
  report.status = status;
  report.error = error ? String(error && error.stack || error) : null;
  report.timing.summary = buildTimingSummary(report);
  return report;
}

function writeReport(outputDir, report, name = 'ui-audit-report.json') {
  return writeJsonAtomic(path.join(outputDir, name), report);
}

module.exports = {
  sha256File,
  createReport,
  startPhase,
  endPhase,
  measurePhase,
  buildTimingSummary,
  addArtifact,
  finalizeReport,
  writeReport,
};
