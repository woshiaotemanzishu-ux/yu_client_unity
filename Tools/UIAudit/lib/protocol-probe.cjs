'use strict';

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { PROTOCOL_POLICY_SCHEMA_VERSION } = require('./version.cjs');

function sameArgs(actual, expected) {
  return Array.isArray(actual) && Array.isArray(expected)
    && actual.length === expected.length
    && actual.every((value, index) => value === expected[index]);
}

function validateProtocolPolicy(policy) {
  const errors = [];
  if (!policy || Number(policy.schema) !== PROTOCOL_POLICY_SCHEMA_VERSION) errors.push('schema');
  if (!Array.isArray(policy && policy.rules)) errors.push('rules');
  const ids = new Set();
  for (const rule of policy && policy.rules || []) {
    if (!rule.id || ids.has(rule.id)) errors.push(`id:${rule && rule.id}`);
    ids.add(rule && rule.id);
    if (!['system', 'read', 'write'].includes(rule && rule.classification)) errors.push(`classification:${rule && rule.id}`);
    if (!Number.isInteger(Number(rule && rule.cmd))) errors.push(`cmd:${rule && rule.id}`);
    if (rule && rule.classification !== 'write' && !Array.isArray(rule.signatures)) errors.push(`signatures:${rule && rule.id}`);
  }
  if (errors.length) throw new Error(`PROTOCOL_POLICY_INVALID: ${errors.join(',')}`);
  return policy;
}

function loadProtocolPolicy(filePath) {
  const absolute = path.resolve(filePath);
  const policy = JSON.parse(fs.readFileSync(absolute, 'utf8'));
  validateProtocolPolicy(policy);
  Object.defineProperty(policy, '__file', { value: absolute, enumerable: false });
  return policy;
}

function matchSignature(event, signature) {
  if (String(event.fmt == null ? '' : event.fmt) !== String(signature.fmt == null ? '' : signature.fmt)) return false;
  return (signature.args || []).some(expected => sameArgs(event.args || [], expected));
}

function classifyProtocolEvent(event, policy) {
  const cmd = Number(event && event.cmd);
  const candidates = (policy.rules || []).filter(rule => Number(rule.cmd) === cmd);
  if (!candidates.length) return { classification: 'unknown', event, rule: null, malformed: false };
  const write = candidates.find(rule => rule.classification === 'write');
  if (write) return { classification: 'write', event, rule: write, malformed: false };
  const matched = candidates.find(rule => (rule.signatures || []).some(signature => matchSignature(event, signature)));
  if (matched) return { classification: matched.classification, event, rule: matched, malformed: false };
  return { classification: 'unknown', event, rule: candidates[0], malformed: true };
}

function outboundEvents(trace) {
  return Array.isArray(trace && trace.outbound) ? trace.outbound.map(event => ({ direction: 'outbound', ...event })) : [];
}

function inboundEvents(trace) {
  return Array.isArray(trace && trace.inbound) ? trace.inbound.map(event => ({ direction: 'inbound', ...event })) : [];
}

function classifyProtocolTrace(trace, policy) {
  validateProtocolPolicy(policy);
  const classified = outboundEvents(trace).map(event => classifyProtocolEvent(event, policy));
  return {
    outbound: outboundEvents(trace),
    inbound: inboundEvents(trace),
    system: classified.filter(item => item.classification === 'system'),
    read: classified.filter(item => item.classification === 'read'),
    write: classified.filter(item => item.classification === 'write'),
    unknown: classified.filter(item => item.classification === 'unknown' && !item.malformed),
    malformed: classified.filter(item => item.malformed),
  };
}

function eventMatches(event, assertion) {
  if (assertion.direction && event.direction !== assertion.direction) return false;
  if (assertion.cmd != null && Number(event.cmd) !== Number(assertion.cmd)) return false;
  if (assertion.fmt != null && String(event.fmt == null ? '' : event.fmt) !== String(assertion.fmt)) return false;
  if (assertion.args && !sameArgs(event.args || [], assertion.args)) return false;
  if (assertion.payloadFields) {
    if (!event.payload || typeof event.payload !== 'object') return false;
    for (const [key, value] of Object.entries(assertion.payloadFields)) {
      if (event.payload[key] !== value) return false;
    }
  }
  return true;
}

function evaluateProtocolAssertions(trace, assertions = {}, policy) {
  const classified = classifyProtocolTrace(trace, policy);
  const events = [...classified.outbound, ...classified.inbound];
  const required = (assertions.required || []).map(assertion => {
    const matches = events.filter(event => eventMatches(event, assertion));
    const min = assertion.min == null ? 1 : Number(assertion.min);
    const max = assertion.max == null ? Infinity : Number(assertion.max);
    return { assertion, count: matches.length, pass: matches.length >= min && matches.length <= max };
  });
  const forbidden = (assertions.forbidden || []).map(assertion => {
    const matches = events.filter(event => eventMatches(event, assertion));
    return { assertion, count: matches.length, pass: matches.length === 0 };
  });
  const modeChecks = assertions.mode === 'read-only' ? {
    noWrites: classified.write.length === 0,
    noUnknown: classified.unknown.length === 0,
    noMalformed: classified.malformed.length === 0,
  } : {};
  const pass = required.every(result => result.pass)
    && forbidden.every(result => result.pass)
    && Object.values(modeChecks).every(Boolean);
  return { pass, required, forbidden, modeChecks, classified };
}

function canonicalHash(value) {
  const canonical = value && typeof value === 'object' && !Array.isArray(value)
    ? Object.fromEntries(Object.keys(value).sort().map(key => [key, value[key]])) : value;
  return crypto.createHash('sha256').update(JSON.stringify(canonical)).digest('hex');
}

async function installLegacyProtocolTrace(page, options = {}) {
  const inboundCommands = (options.inboundCommands || []).map(Number);
  return page.evaluate(({ inboundCommands }) => {
    const Adapter = window.UserMsgAdapter;
    if (!Adapter || typeof Adapter.GetInstance !== 'function') throw new Error('UserMsgAdapter unavailable');
    const adapter = Adapter.GetInstance();
    if (!adapter) throw new Error('UserMsgAdapter instance unavailable');
    const clone = value => {
      const ancestors = new Set();
      const visit = input => {
        if (input == null || typeof input !== 'object') return input;
        if (ancestors.has(input)) return undefined;
        ancestors.add(input);
        const output = Array.isArray(input) ? [] : {};
        for (const key of Object.keys(input)) {
          const child = visit(input[key]);
          if (child !== undefined) output[key] = child;
        }
        ancestors.delete(input);
        return output;
      };
      try { return visit(value); } catch (_) { return String(value); }
    };
    const trace = window.__uiAuditProtocolTrace = window.__uiAuditProtocolTrace || {
      schema: 1, installedAt: new Date().toISOString(), outbound: [], inbound: [], wrappedInbound: [],
    };
    if (!trace.outboundWrapped) {
      const originalSend = adapter.SendAllFmtToGame;
      if (typeof originalSend !== 'function') throw new Error('SendAllFmtToGame unavailable');
      adapter.SendAllFmtToGame = function uiAuditTracedSend(cmd, fmt, args) {
        trace.outbound.push({ at: new Date().toISOString(), cmd: Number(cmd), fmt: String(fmt || ''), args: clone(args || []) });
        return originalSend.apply(this, arguments);
      };
      trace.outboundWrapped = true;
    }
    for (const cmd of inboundCommands) {
      if (trace.wrappedInbound.includes(cmd)) continue;
      const original = adapter.register_list && adapter.register_list[cmd];
      if (typeof original !== 'function') throw new Error(`response handler unavailable: ${cmd}`);
      adapter.register_list[cmd] = function uiAuditTracedInbound() {
        const before = adapter.receive_byteBuff && adapter.receive_byteBuff.pos;
        let payload = null;
        try { payload = clone(adapter.GetSCMD(String(cmd))); }
        finally { if (adapter.receive_byteBuff && before != null) adapter.receive_byteBuff.pos = before; }
        trace.inbound.push({ at: new Date().toISOString(), cmd, payload });
        return original.apply(this, arguments);
      };
      trace.wrappedInbound.push(cmd);
    }
    return { installed: true, inboundCommands: trace.wrappedInbound.slice() };
  }, { inboundCommands });
}

async function resetLegacyProtocolTrace(page, label) {
  return page.evaluate(label => {
    const trace = window.__uiAuditProtocolTrace;
    if (!trace) throw new Error('protocol trace not installed');
    trace.label = label;
    trace.resetAt = new Date().toISOString();
    trace.outbound.length = 0;
    trace.inbound.length = 0;
    return true;
  }, label);
}

async function readLegacyProtocolTrace(page) {
  return page.evaluate(() => {
    const trace = window.__uiAuditProtocolTrace;
    if (!trace) throw new Error('protocol trace not installed');
    return JSON.parse(JSON.stringify(trace));
  });
}

async function sendReadProbe(page, request, policy) {
  const event = { cmd: Number(request.cmd), fmt: String(request.fmt || ''), args: request.args || [] };
  const classification = classifyProtocolEvent(event, policy);
  if (classification.classification !== 'read') {
    throw new Error(`PROTOCOL_PROBE_NOT_EXACT_READ: ${JSON.stringify({ request: event, classification })}`);
  }
  await page.evaluate(event => {
    const Adapter = window.UserMsgAdapter;
    const adapter = Adapter && Adapter.GetInstance && Adapter.GetInstance();
    if (!adapter || typeof adapter.SendAllFmtToGame !== 'function') throw new Error('UserMsgAdapter transport unavailable');
    adapter.SendAllFmtToGame(event.cmd, event.fmt, event.args);
  }, event);
  return event;
}

module.exports = {
  sameArgs,
  validateProtocolPolicy,
  loadProtocolPolicy,
  matchSignature,
  classifyProtocolEvent,
  classifyProtocolTrace,
  eventMatches,
  evaluateProtocolAssertions,
  canonicalHash,
  installLegacyProtocolTrace,
  resetLegacyProtocolTrace,
  readLegacyProtocolTrace,
  sendReadProbe,
};
