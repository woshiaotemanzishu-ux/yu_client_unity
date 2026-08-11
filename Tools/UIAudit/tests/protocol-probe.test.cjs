'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const { loadProtocolPolicy, classifyProtocolTrace, evaluateProtocolAssertions } = require('../lib/protocol-probe.cjs');

const policy = loadProtocolPolicy(path.join(__dirname, '..', 'policies', 'protocols.json'));
const trace = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'protocol-trace.json'), 'utf8'));

test('required and forbidden protocol assertions use transport records', () => {
  const result = evaluateProtocolAssertions(trace, {
    mode: 'read-only',
    required: [
      { direction: 'outbound', cmd: 15010, fmt: 'h', args: [4], min: 1, max: 1 },
      { direction: 'inbound', cmd: 15010, payloadFields: { pos: 4 }, min: 1 },
    ],
    forbidden: [{ direction: 'outbound', cmd: 15201 }],
  }, policy);
  assert.equal(result.pass, true);
  assert.equal(result.classified.read.length, 1);
  assert.equal(result.classified.system.length, 1);
});

test('write, malformed known read and unknown protocol each fail read-only mode', () => {
  const writeTrace = structuredClone(trace);
  writeTrace.outbound.push({ cmd: 15201, fmt: 'i', args: [1] });
  assert.equal(evaluateProtocolAssertions(writeTrace, { mode: 'read-only' }, policy).pass, false);
  const malformed = { outbound: [{ cmd: 15010, fmt: 'i', args: [4] }], inbound: [] };
  assert.equal(classifyProtocolTrace(malformed, policy).malformed.length, 1);
  const unknown = { outbound: [{ cmd: 29999, fmt: '', args: [] }], inbound: [] };
  assert.equal(evaluateProtocolAssertions(unknown, { mode: 'read-only' }, policy).pass, false);
});
