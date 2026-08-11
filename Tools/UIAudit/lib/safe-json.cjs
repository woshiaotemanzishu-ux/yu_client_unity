'use strict';

const fs = require('fs');
const path = require('path');

const OMIT = Symbol('ui-audit-cycle');

function cloneWithoutCycles(value, ancestors = new Set()) {
  if (value === undefined || typeof value === 'function' || typeof value === 'symbol') return OMIT;
  if (typeof value === 'bigint') return value.toString();
  if (typeof value === 'number' && !Number.isFinite(value)) return null;
  if (value === null || typeof value !== 'object') return value;
  if (ancestors.has(value)) return OMIT;
  if (value instanceof Date) return value.toISOString();
  if (Buffer.isBuffer(value)) return { type: 'Buffer', data: Array.from(value) };
  if (value instanceof Error) {
    return { name: value.name, message: value.message, stack: value.stack || null };
  }

  ancestors.add(value);
  let result;
  if (Array.isArray(value)) {
    result = value.map(item => {
      const cloned = cloneWithoutCycles(item, ancestors);
      return cloned === OMIT ? null : cloned;
    });
  } else if (value instanceof Map) {
    result = {};
    for (const [key, item] of value.entries()) {
      const cloned = cloneWithoutCycles(item, ancestors);
      if (cloned !== OMIT) result[String(key)] = cloned;
    }
  } else if (value instanceof Set) {
    result = [];
    for (const item of value.values()) {
      const cloned = cloneWithoutCycles(item, ancestors);
      if (cloned !== OMIT) result.push(cloned);
    }
  } else {
    result = {};
    for (const key of Object.keys(value)) {
      const cloned = cloneWithoutCycles(value[key], ancestors);
      if (cloned !== OMIT) result[key] = cloned;
    }
  }
  ancestors.delete(value);
  return result;
}

function safeStringify(value, space = 2) {
  const cloned = cloneWithoutCycles(value);
  return JSON.stringify(cloned === OMIT ? null : cloned, null, space);
}

function writeJsonAtomic(targetPath, value, options = {}) {
  const absolute = path.resolve(targetPath);
  const overwrite = options.overwrite === true;
  if (!overwrite && fs.existsSync(absolute)) {
    throw new Error(`IMMUTABLE_EVIDENCE_EXISTS: ${absolute}`);
  }
  fs.mkdirSync(path.dirname(absolute), { recursive: true });
  const temp = `${absolute}.tmp-${process.pid}-${Date.now()}`;
  const body = `${safeStringify(value, options.space == null ? 2 : options.space)}\n`;
  try {
    fs.writeFileSync(temp, body, { encoding: 'utf8', flag: 'wx' });
    fs.renameSync(temp, absolute);
  } catch (error) {
    try { if (fs.existsSync(temp)) fs.unlinkSync(temp); } catch (_) {}
    throw error;
  }
  return absolute;
}

module.exports = {
  cloneWithoutCycles,
  safeStringify,
  writeJsonAtomic,
};
