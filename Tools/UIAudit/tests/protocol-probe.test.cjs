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
  resetLegacyProtocolTrace,
  readLegacyProtocolTrace,
} = require('../lib/protocol-probe.cjs');

const policy = loadProtocolPolicy(path.join(__dirname, '..', 'policies', 'protocols.json'));
const trace = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'protocol-trace.json'), 'utf8'));
const lifecycleFixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'fixtures', 'protocol-trace-lifecycle.json'), 'utf8'));

class FixtureReceiveByte {
  constructor(cmd, payloadValue) {
    this.cmd = Number(cmd);
    this.payloadValue = Number(payloadValue);
    this.length = lifecycleFixture.frameLength;
    this.pos = 0;
  }

  readUint32() {
    assert.equal(this.pos, 0);
    this.pos = 4;
    return this.length;
  }

  readUint16() {
    if (this.pos === 4) {
      this.pos = 6;
      return this.cmd;
    }
    assert.equal(this.pos, 7);
    this.pos = 9;
    return this.payloadValue;
  }

  readByte() {
    assert.equal(this.pos, 6);
    this.pos = 7;
    return lifecycleFixture.compression;
  }
}

function inboundFixtureAdapter() {
  return {
    is_game_connected: true,
    register_list: {},
    receive_byteBuff: new FixtureReceiveByte(lifecycleFixture.cmd, lifecycleFixture.payloads.afterLateRegistration),
    WriteBegin() {},
    WriteFMT() {},
    SendToGame() {},
    RegisterMsgOperate(id, handler) { this.register_list[Number(id)] = handler; },
    GetSCMD() { return { pos: this.receive_byteBuff.readUint16() }; },
    ReceiveHandler() {
      const buffer = this.receive_byteBuff;
      if (!this.is_game_connected || buffer.length < 4 || buffer.pos > buffer.length - 4) return;
      buffer.readUint32();
      const cmd = buffer.readUint16();
      buffer.readByte();
      const handler = this.register_list[cmd];
      if (handler) handler();
    },
  };
}

function fixturePage(adapter) {
  global.window = { UserMsgAdapter: { GetInstance: () => adapter } };
  return { evaluate: async (fn, arg) => fn(arg) };
}

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
    { cmd: 16022, fmt: 'cc', args: [3, 1] },
    { cmd: 16028, fmt: 'c', args: [5] },
    { cmd: 41312, fmt: 'ci', args: [1, 12010008] },
  ]) assert.equal(classifyProtocolEvent(event, policy).classification, 'read', JSON.stringify(event));
  assert.equal(classifyProtocolEvent({ cmd: 16002, fmt: 'c', args: [6] }, policy).classification, 'unknown');
  for (const event of [
    { cmd: 16022, fmt: 'cc', args: [6, 1] },
    { cmd: 16022, fmt: 'cc', args: [3, 0] },
    { cmd: 16022, fmt: 'cc', args: [3, 256] },
    { cmd: 16022, fmt: 'ci', args: [3, 1] },
  ]) assert.equal(classifyProtocolEvent(event, policy).classification, 'unknown', JSON.stringify(event));
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

test('trace installs before a response handler, lazily associates it, and keeps transport receive authority', async () => {
  const adapter = inboundFixtureAdapter();
  const page = fixturePage(adapter);
  let businessPayload = null;
  try {
    const installed = await installLegacyProtocolTrace(page, { inboundCommands: [lifecycleFixture.cmd] });
    assert.equal(installed.inboundTransportAvailable, true);
    assert.deepEqual(installed.attachedHandlers, []);
    assert.equal(installed.handlerAssociations['15010'].registered, false);

    adapter.receive_byteBuff = new FixtureReceiveByte(lifecycleFixture.cmd, lifecycleFixture.payloads.beforeHandler);
    adapter.ReceiveHandler();
    const withoutHandler = await readLegacyProtocolTrace(page);
    assert.equal(withoutHandler.inbound.length, 1);
    assert.equal(withoutHandler.inbound[0].transport, 'receive-frame');
    assert.deepEqual(withoutHandler.inbound[0].handler, { registered: false, attached: false, invoked: false });
    assert.equal(evaluateProtocolAssertions(withoutHandler, {
      required: [{ direction: 'inbound', cmd: 15010 }],
    }, policy).pass, true);
    await resetLegacyProtocolTrace(page, 'late-handler-registration');

    adapter.RegisterMsgOperate(lifecycleFixture.cmd, () => { businessPayload = adapter.GetSCMD(String(lifecycleFixture.cmd)); });
    adapter.receive_byteBuff = new FixtureReceiveByte(lifecycleFixture.cmd, lifecycleFixture.payloads.afterLateRegistration);
    adapter.ReceiveHandler();

    const recorded = await readLegacyProtocolTrace(page);
    assert.equal(recorded.schema, 3);
    assert.equal(recorded.inbound.length, 1);
    assert.equal(recorded.inbound[0].transport, 'receive-frame');
    assert.equal(recorded.inbound[0].cmd, 15010);
    assert.equal(recorded.inbound[0].frame.length, 9);
    assert.deepEqual(recorded.inbound[0].payload, { pos: 4 });
    assert.deepEqual(recorded.inbound[0].handler, { registered: true, attached: true, invoked: true });
    assert.deepEqual(businessPayload, { pos: 4 });
    assert.equal(evaluateProtocolAssertions(recorded, {
      mode: 'read-only',
      required: [{ direction: 'inbound', cmd: 15010, payloadFields: { pos: 4 } }],
      forbidden: [{ direction: 'outbound', cmd: 15201 }],
    }, policy).pass, true);
  } finally {
    delete global.window;
  }
});

test('an already registered response handler is associated without changing its payload cursor', async () => {
  const adapter = inboundFixtureAdapter();
  let businessPayload = null;
  adapter.RegisterMsgOperate(lifecycleFixture.cmd, () => { businessPayload = adapter.GetSCMD(String(lifecycleFixture.cmd)); });
  const page = fixturePage(adapter);
  try {
    const installed = await installLegacyProtocolTrace(page, { inboundCommands: [lifecycleFixture.cmd] });
    assert.deepEqual(installed.attachedHandlers, [lifecycleFixture.cmd]);
    adapter.receive_byteBuff = new FixtureReceiveByte(lifecycleFixture.cmd, lifecycleFixture.payloads.existingHandler);
    adapter.ReceiveHandler();
    const recorded = await readLegacyProtocolTrace(page);
    assert.equal(recorded.inbound.length, 1);
    assert.deepEqual(recorded.inbound[0].payload, { pos: 5 });
    assert.deepEqual(businessPayload, { pos: 5 });
  } finally {
    delete global.window;
  }
});
