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

function valueMatchesSpec(value, spec) {
  if (spec == null || typeof spec !== 'object' || Array.isArray(spec)) return value === spec;
  if (spec.type === 'integer' && !Number.isInteger(Number(value))) return false;
  if (spec.type === 'number' && !Number.isFinite(Number(value))) return false;
  if (spec.type === 'string' && typeof value !== 'string') return false;
  if (Array.isArray(spec.enum) && !spec.enum.some(candidate => candidate === value)) return false;
  if (spec.min != null && Number(value) < Number(spec.min)) return false;
  if (spec.max != null && Number(value) > Number(spec.max)) return false;
  return true;
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
    for (const signature of rule && rule.signatures || []) {
      if (!Array.isArray(signature.args) && !Array.isArray(signature.argsSpec)) errors.push(`signature-args:${rule && rule.id}`);
    }
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
  if (Array.isArray(signature.argsSpec)) {
    const actual = event.args || [];
    return actual.length === signature.argsSpec.length
      && actual.every((value, index) => valueMatchesSpec(value, signature.argsSpec[index]));
  }
  return (signature.args || []).some(expected => sameArgs(event.args || [], expected));
}

function findPolicyRule(policy, id) {
  return (policy && policy.rules || []).find(rule => rule.id === id) || null;
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

function eventMatches(event, assertion, policy) {
  if (assertion.direction && event.direction !== assertion.direction) return false;
  if (assertion.cmd != null && Number(event.cmd) !== Number(assertion.cmd)) return false;
  if (assertion.fmt != null && String(event.fmt == null ? '' : event.fmt) !== String(assertion.fmt)) return false;
  if (assertion.args && !sameArgs(event.args || [], assertion.args)) return false;
  if (assertion.ruleId) {
    const rule = findPolicyRule(policy, assertion.ruleId);
    if (!rule || Number(rule.cmd) !== Number(event.cmd)) return false;
    if (event.direction === 'outbound' && !(rule.signatures || []).some(signature => matchSignature(event, signature))) return false;
  }
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
    const matches = events.filter(event => eventMatches(event, assertion, policy));
    const min = assertion.min == null ? 1 : Number(assertion.min);
    const max = assertion.max == null ? Infinity : Number(assertion.max);
    return { assertion, count: matches.length, pass: matches.length >= min && matches.length <= max };
  });
  const forbidden = (assertions.forbidden || []).map(assertion => {
    const matches = events.filter(event => eventMatches(event, assertion, policy));
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

function validateProtocolAssertionsContract(assertions, policy, location, errors) {
  for (const [index, assertion] of (assertions && assertions.required || []).entries()) {
    if (assertion.direction !== 'outbound') continue;
    let classification = null;
    if (assertion.ruleId) {
      const rule = findPolicyRule(policy, assertion.ruleId);
      if (!rule) errors.push(`${location}.required[${index}].ruleId`);
      else if (assertion.cmd != null && Number(assertion.cmd) !== Number(rule.cmd)) errors.push(`${location}.required[${index}].cmd-rule`);
      else classification = rule.classification;
    } else if (assertion.fmt != null && Array.isArray(assertion.args)) {
      classification = classifyProtocolEvent(assertion, policy).classification;
    } else {
      errors.push(`${location}.required[${index}].policy-binding`);
    }
    if (classification && !['read', 'system'].includes(classification)) errors.push(`${location}.required[${index}].not-read`);
  }
}

function validateRouteProtocolContract(route, policy) {
  validateProtocolPolicy(policy);
  const errors = [];
  for (const [index, request] of (route && route.protocol && route.protocol.reads || []).entries()) {
    const event = { cmd: Number(request.cmd), fmt: String(request.fmt || ''), args: request.args || [] };
    const result = classifyProtocolEvent(event, policy);
    if (request.ruleId && (!result.rule || result.rule.id !== request.ruleId)) errors.push(`protocol.reads[${index}].ruleId`);
    if (result.classification !== 'read') errors.push(`protocol.reads[${index}].not-read`);
  }
  validateProtocolAssertionsContract(route && route.protocol && route.protocol.assertions, policy, 'protocol.assertions', errors);
  const walk = (steps, prefix) => {
    for (const [index, step] of (steps || []).entries()) {
      const location = `${prefix}[${index}]`;
      if (step.action === 'protocol-read') {
        const request = step.request || {};
        const result = classifyProtocolEvent({ cmd: Number(request.cmd), fmt: String(request.fmt || ''), args: request.args || [] }, policy);
        if (request.ruleId && (!result.rule || result.rule.id !== request.ruleId)) errors.push(`${location}.request.ruleId`);
        if (result.classification !== 'read') errors.push(`${location}.request.not-read`);
      }
      if (step.action === 'assert-protocol') validateProtocolAssertionsContract(step.assertions, policy, `${location}.assertions`, errors);
      if (step.action === 'branch') {
        walk(step.then, `${location}.then`);
        walk(step.else, `${location}.else`);
      }
    }
  };
  walk(route && route.steps, 'steps');
  return { pass: errors.length === 0, errors };
}

function verifyProtocolAuthority(policy, legacyRoot, existsSync = fs.existsSync) {
  const checked = new Map();
  for (const rule of policy && policy.rules || []) {
    const evidence = Array.isArray(rule.evidence) ? rule.evidence : rule.evidence ? [rule.evidence] : [];
    for (const item of evidence) {
      if (!item.pathFromLegacyRoot) continue;
      const target = path.resolve(legacyRoot, item.pathFromLegacyRoot);
      const key = `${target}|${item.sha256 || ''}`;
      if (checked.has(key)) continue;
      const exists = existsSync(target);
      const actualSha256 = exists ? crypto.createHash('sha256').update(fs.readFileSync(target)).digest('hex') : null;
      checked.set(key, {
        id: `protocol:${rule.id}:${item.pathFromLegacyRoot}`,
        ruleId: rule.id,
        target,
        exists,
        expectedSha256: item.sha256 || null,
        actualSha256,
        pass: exists && (!item.sha256 || actualSha256 === item.sha256),
      });
    }
  }
  return [...checked.values()];
}

function canonicalHash(value) {
  const canonical = value && typeof value === 'object' && !Array.isArray(value)
    ? Object.fromEntries(Object.keys(value).sort().map(key => [key, value[key]])) : value;
  return crypto.createHash('sha256').update(JSON.stringify(canonical)).digest('hex');
}

async function installLegacyProtocolTrace(page, options = {}) {
  const inboundCommands = [...new Set((options.inboundCommands || []).map(Number))];
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
    const trace = window.__uiAuditProtocolTrace = window.__uiAuditProtocolTrace || {};
    trace.schema = 3;
    trace.installedAt = trace.installedAt || new Date().toISOString();
    trace.outbound = Array.isArray(trace.outbound) ? trace.outbound : [];
    trace.inbound = Array.isArray(trace.inbound) ? trace.inbound : [];
    trace.wrappedInbound = Array.isArray(trace.wrappedInbound) ? trace.wrappedInbound : [];
    trace.watchedInbound = Array.isArray(trace.watchedInbound) ? trace.watchedInbound : [];
    trace.handlerAssociations = trace.handlerAssociations && typeof trace.handlerAssociations === 'object'
      ? trace.handlerAssociations : {};
    trace.pendingOutbound = trace.pendingOutbound || null;
    trace.currentInbound = null;
    trace.inboundSequence = Number(trace.inboundSequence || 0);
    for (const cmd of inboundCommands) if (!trace.watchedInbound.includes(cmd)) trace.watchedInbound.push(cmd);

    if (!trace.outboundTransportWrapped && !trace.transportWrapped) {
      const originalWriteBegin = adapter.WriteBegin;
      const originalWriteFMT = adapter.WriteFMT;
      const originalSendToGame = adapter.SendToGame;
      if (typeof originalWriteBegin !== 'function' || typeof originalWriteFMT !== 'function' || typeof originalSendToGame !== 'function') {
        throw new Error('UserMsgAdapter write transport unavailable');
      }
      adapter.WriteBegin = function uiAuditTracedWriteBegin(cmd) {
        trace.pendingOutbound = { at: new Date().toISOString(), cmd: Number(cmd), fmt: '', args: [], chunks: [], transport: 'write-chain' };
        return originalWriteBegin.apply(this, arguments);
      };
      adapter.WriteFMT = function uiAuditTracedWriteFMT(fmt, args) {
        const values = Array.isArray(args) ? args : [args];
        if (trace.pendingOutbound) {
          trace.pendingOutbound.fmt += String(fmt || '');
          trace.pendingOutbound.args.push(...clone(values));
          trace.pendingOutbound.chunks.push({ fmt: String(fmt || ''), args: clone(values) });
        }
        return originalWriteFMT.apply(this, arguments);
      };
      adapter.SendToGame = function uiAuditTracedSendToGame() {
        if (trace.pendingOutbound) trace.outbound.push(clone(trace.pendingOutbound));
        trace.pendingOutbound = null;
        return originalSendToGame.apply(this, arguments);
      };
      trace.outboundTransportWrapped = true;
      trace.transportWrapped = true;
    } else {
      trace.outboundTransportWrapped = true;
    }

    const association = (cmd, values) => {
      trace.handlerAssociations[String(cmd)] = {
        cmd: Number(cmd),
        ...(trace.handlerAssociations[String(cmd)] || {}),
        ...values,
        updatedAt: new Date().toISOString(),
      };
    };
    const attachInboundHandler = rawCmd => {
      const cmd = Number(rawCmd);
      if (!trace.watchedInbound.includes(cmd)) return false;
      const original = adapter.register_list && adapter.register_list[cmd];
      if (typeof original !== 'function') {
        association(cmd, { registered: false, attached: false, invoked: false });
        return false;
      }
      if (Number(original.__uiAuditProtocolTraceHandlerCmd) === cmd) {
        association(cmd, { registered: true, attached: true });
        if (!trace.wrappedInbound.includes(cmd)) trace.wrappedInbound.push(cmd);
        return true;
      }
      const wrapped = function uiAuditTracedInboundHandler() {
        const event = trace.currentInbound && Number(trace.currentInbound.cmd) === cmd ? trace.currentInbound : null;
        const before = adapter.receive_byteBuff && adapter.receive_byteBuff.pos;
        if (event) {
          event.handler = { registered: true, attached: true, invoked: true };
          try {
            if (typeof adapter.GetSCMD === 'function') event.payload = clone(adapter.GetSCMD(String(cmd)));
            else event.payloadError = 'GetSCMD unavailable';
          } catch (error) {
            event.payloadError = String(error && error.message || error);
          } finally {
            if (adapter.receive_byteBuff && before != null) adapter.receive_byteBuff.pos = before;
          }
        }
        association(cmd, { registered: true, attached: true, invoked: true });
        return original.apply(this, arguments);
      };
      Object.defineProperty(wrapped, '__uiAuditProtocolTraceHandlerCmd', { value: cmd });
      adapter.register_list[cmd] = wrapped;
      association(cmd, { registered: true, attached: true, invoked: false });
      if (!trace.wrappedInbound.includes(cmd)) trace.wrappedInbound.push(cmd);
      return true;
    };

    if (!trace.registerTransportWrapped && typeof adapter.RegisterMsgOperate === 'function') {
      const originalRegister = adapter.RegisterMsgOperate;
      adapter.RegisterMsgOperate = function uiAuditTracedRegisterMsgOperate(id) {
        const result = originalRegister.apply(this, arguments);
        attachInboundHandler(Number(id));
        return result;
      };
      trace.registerTransportWrapped = true;
    }

    if (!trace.inboundTransportWrapped) {
      const originalReceive = adapter.ReceiveHandler;
      if (typeof originalReceive !== 'function') {
        if (trace.watchedInbound.length) throw new Error('UserMsgAdapter receive transport unavailable');
        trace.inboundTransportAvailable = false;
      } else {
        adapter.ReceiveHandler = function uiAuditTracedReceiveHandler() {
          const buffer = adapter.receive_byteBuff;
          let frame = null;
          if (adapter.is_game_connected !== false && buffer && Number(buffer.length || 0) >= 4
            && Number(buffer.pos || 0) <= Number(buffer.length || 0) - 4) {
            const before = Number(buffer.pos || 0);
            try {
              const frameLength = Number(buffer.readUint32());
              const cmd = Number(buffer.readUint16());
              const compression = Number(buffer.readByte());
              frame = {
                at: new Date().toISOString(),
                sequence: ++trace.inboundSequence,
                cmd,
                transport: 'receive-frame',
                frame: { start: before, length: frameLength, compression, bufferLength: Number(buffer.length || 0) },
                payload: null,
              };
            } catch (error) {
              frame = {
                at: new Date().toISOString(),
                sequence: ++trace.inboundSequence,
                cmd: null,
                transport: 'receive-frame',
                frameError: String(error && error.message || error),
              };
            } finally {
              buffer.pos = before;
            }
          }
          if (!frame || frame.cmd == null) return originalReceive.apply(this, arguments);
          attachInboundHandler(frame.cmd);
          frame.handler = {
            registered: typeof (adapter.register_list && adapter.register_list[frame.cmd]) === 'function',
            attached: trace.wrappedInbound.includes(frame.cmd),
            invoked: false,
          };
          const insertAt = trace.inbound.length;
          trace.inbound.push(frame);
          const previous = trace.currentInbound;
          trace.currentInbound = frame;
          try {
            return originalReceive.apply(this, arguments);
          } catch (error) {
            frame.transportError = String(error && error.message || error);
            throw error;
          } finally {
            trace.currentInbound = previous;
            for (let index = trace.inbound.length - 1; index > insertAt; index--) {
              const legacy = trace.inbound[index];
              if (Number(legacy && legacy.cmd) !== Number(frame.cmd) || legacy.transport) continue;
              if (frame.payload == null && legacy.payload != null) frame.payload = clone(legacy.payload);
              frame.handler = { registered: true, attached: true, invoked: true, legacyAssociation: true };
              trace.inbound.splice(index, 1);
            }
          }
        };
        trace.inboundTransportWrapped = true;
        trace.inboundTransportAvailable = true;
      }
    }
    for (const cmd of trace.watchedInbound) attachInboundHandler(cmd);
    return {
      installed: true,
      inboundTransportAvailable: trace.inboundTransportAvailable === true,
      inboundCommands: trace.watchedInbound.slice(),
      attachedHandlers: trace.wrappedInbound.slice(),
      handlerAssociations: clone(trace.handlerAssociations),
    };
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
    trace.pendingOutbound = null;
    trace.currentInbound = null;
    trace.inboundSequence = 0;
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
  valueMatchesSpec,
  validateProtocolPolicy,
  loadProtocolPolicy,
  matchSignature,
  findPolicyRule,
  classifyProtocolEvent,
  classifyProtocolTrace,
  eventMatches,
  evaluateProtocolAssertions,
  validateProtocolAssertionsContract,
  validateRouteProtocolContract,
  verifyProtocolAuthority,
  canonicalHash,
  installLegacyProtocolTrace,
  resetLegacyProtocolTrace,
  readLegacyProtocolTrace,
  sendReadProbe,
};
