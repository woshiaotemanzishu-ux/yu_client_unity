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
  return {
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
  return report;
}

function writeReport(outputDir, report, name = 'ui-audit-report.json') {
  return writeJsonAtomic(path.join(outputDir, name), report);
}

module.exports = {
  sha256File,
  createReport,
  addArtifact,
  finalizeReport,
  writeReport,
};
