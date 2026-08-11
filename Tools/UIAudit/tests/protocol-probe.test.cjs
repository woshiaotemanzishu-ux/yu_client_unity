'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const path = require('path');
const {
  loadProtocolPolicy,
  classifyProtocolEvent,
  classifyProtocolTrace,
  evaluateProtocolAssertions,
  validateRouteProtocolContract,
  installLegacyProtocolTrace,
  readLegacyProtocolTrace,
} = require('../lib/protocol-probe.cjs');

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

test('public source-backed policy classifies Bag, Pet and fashion power reads exactly', () => {
  for (const event of [
    { cmd: 15010, fmt: 'h', args: [5] },
    { cmd: 16002, fmt: 'c', args: [5] },
    { cmd: 16006, fmt: 'c', args: [5] },
    { cmd: 16011, fmt: 'c', args: [5] },
    { cmd: 16028, fmt: 'c', args: [5] },
    { cmd: 41312, fmt: 'ci', args: [1, 12010008] },
  ]) assert.equal(classifyProtocolEvent(event, policy).classification, 'read', JSON.stringify(event));
  assert.equal(classifyProtocolEvent({ cmd: 16002, fmt: 'c', args: [6] }, policy).classification, 'unknown');
});

test('required outbound reads need an exact signature or public rule binding', () => {
  const unsafe = validateRouteProtocolContract({
    protocol: { assertions: { required: [{ direction: 'outbound', cmd: 16002 }] } }, steps: [],
  }, policy);
  assert.equal(unsafe.pass, false);
  assert.match(unsafe.errors[0], /policy-binding/);
  const safe = validateRouteProtocolContract({
    protocol: { assertions: { required: [{ direction: 'outbound', ruleId: 'outward-base-read-16002', cmd: 16002 }] } }, steps: [],
  }, policy);
  assert.equal(safe.pass, true);
});

test('transport trace records custom WriteBegin/WriteFMT/SendToGame writes for forbidden proof', async () => {
  const adapter = {
    register_list: {},
    WriteBegin() {},
    WriteFMT() {},
    SendToGame() {},
  };
  global.window = { UserMsgAdapter: { GetInstance: () => adapter } };
  const page = { evaluate: async (fn, arg) => fn(arg) };
  try {
    await installLegacyProtocolTrace(page);
    adapter.WriteBegin(41305);
    adapter.WriteFMT('c', [1]);
    adapter.WriteFMT('h', [0]);
    adapter.SendToGame();
    const recorded = await readLegacyProtocolTrace(page);
    assert.deepEqual(recorded.outbound[0].cmd, 41305);
    assert.equal(recorded.outbound[0].fmt, 'ch');
    const result = evaluateProtocolAssertions(recorded, {
      mode: 'read-only', forbidden: [{ direction: 'outbound', cmd: 41305 }],
    }, policy);
    assert.equal(result.pass, false);
    assert.equal(result.forbidden[0].count, 1);
  } finally {
    delete global.window;
  }
});
