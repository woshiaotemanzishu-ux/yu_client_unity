'use strict';

const { findExactNode } = require('./runtime-tree.cjs');

async function installSoundTrace(page) {
  return page.evaluate(() => {
    const SoundManager = window.SoundManager;
    const manager = SoundManager && SoundManager.GetInstance && SoundManager.GetInstance();
    if (!manager) throw new Error('SoundManager instance unavailable');
    const trace = window.__uiAuditSoundTrace = window.__uiAuditSoundTrace || { schema: 1, installedAt: new Date().toISOString(), events: [] };
    for (const method of ['PlaySoundEffect', 'PlaySceneSound']) {
      if (trace[`wrapped:${method}`]) continue;
      const original = manager[method];
      if (typeof original !== 'function') continue;
      manager[method] = function uiAuditTracedSound() {
        const args = Array.from(arguments).map(value => value == null || ['string', 'number', 'boolean'].includes(typeof value) ? value : String(value));
        trace.events.push({ at: new Date().toISOString(), method, key: args[0] == null ? null : String(args[0]), args });
        return original.apply(this, arguments);
      };
      trace[`wrapped:${method}`] = true;
    }
    return { installed: true, methods: ['PlaySoundEffect', 'PlaySceneSound'].filter(method => trace[`wrapped:${method}`]) };
  });
}

async function resetSoundTrace(page, label) {
  return page.evaluate(label => {
    const trace = window.__uiAuditSoundTrace;
    if (!trace) throw new Error('sound trace not installed');
    trace.label = label;
    trace.resetAt = new Date().toISOString();
    trace.events.length = 0;
    return true;
  }, label);
}

async function readSoundTrace(page) {
  return page.evaluate(() => {
    const trace = window.__uiAuditSoundTrace;
    if (!trace) throw new Error('sound trace not installed');
    return JSON.parse(JSON.stringify(trace));
  });
}

function soundEventMatches(event, assertion) {
  if (assertion.method && event.method !== assertion.method) return false;
  if (assertion.key != null && String(event.key) !== String(assertion.key)) return false;
  return true;
}

function evaluateSoundAssertions(trace, assertions = {}) {
  const events = trace && trace.events || [];
  const required = (assertions.required || []).map(assertion => {
    const count = events.filter(event => soundEventMatches(event, assertion)).length;
    const min = Number(assertion.min == null ? 1 : assertion.min);
    const max = Number(assertion.max == null ? Infinity : assertion.max);
    return { assertion, count, pass: count >= min && count <= max };
  });
  const forbidden = (assertions.forbidden || []).map(assertion => {
    const count = events.filter(event => soundEventMatches(event, assertion)).length;
    return { assertion, count, pass: count === 0 };
  });
  return { pass: required.every(value => value.pass) && forbidden.every(value => value.pass), required, forbidden, events };
}

async function probeRenderTexture(page, snapshot, spec) {
  const target = findExactNode(snapshot, spec.selector || {});
  const propertyPath = Array.isArray(spec.propertyPath) ? spec.propertyPath : [];
  if (!propertyPath.length || propertyPath.some(value => !/^[A-Za-z_$][A-Za-z0-9_$]*$/.test(String(value)))) {
    throw new Error('RENDER_TEXTURE_PROPERTY_PATH_INVALID');
  }
  return page.evaluate(({ indexPath, propertyPath, alphaThreshold, sampleStride }) => {
    const stage = window.Laya && Laya.stage;
    if (!stage) return { pass: false, code: 'LAYA_STAGE_MISSING' };
    const childrenOf = value => value && (value._children || (value.numChildren
      ? Array.from({ length: value.numChildren }, (_, index) => value.getChildAt(index)) : [])) || [];
    let current = stage;
    for (const index of indexPath.slice(1)) {
      current = childrenOf(current)[Number(index)];
      if (!current) return { pass: false, code: 'RUNTIME_TARGET_MISSING' };
    }
    for (const property of propertyPath) {
      current = current && current[property];
      if (!current) return { pass: false, code: 'RENDER_TEXTURE_PROPERTY_MISSING', property };
    }
    const texture = current.renderTexture || current.render_texture || current;
    const width = Number(texture && (texture.width || texture._width) || 0);
    const height = Number(texture && (texture.height || texture._height) || 0);
    if (!texture || typeof texture.getData !== 'function' || width <= 0 || height <= 0) {
      return { pass: false, code: 'RENDER_TEXTURE_UNAVAILABLE', width, height };
    }
    const pixels = texture.getData(0, 0, width, height, new Uint8Array(width * height * 4));
    if (!pixels || pixels.length !== width * height * 4) return { pass: false, code: 'RENDER_TEXTURE_READ_FAILED', length: pixels && pixels.length || 0 };
    let nonTransparentPixels = 0;
    let sampledPixels = 0;
    const stride = Math.max(1, Number(sampleStride || 1));
    for (let pixel = 0; pixel < width * height; pixel += stride) {
      sampledPixels += 1;
      if (pixels[pixel * 4 + 3] > Number(alphaThreshold || 0)) nonTransparentPixels += 1;
    }
    return { pass: true, code: 'RENDER_TEXTURE_READ', width, height, sampledPixels, nonTransparentPixels, alphaThreshold: Number(alphaThreshold || 0), sampleStride: stride };
  }, {
    indexPath: target.indexPath,
    propertyPath,
    alphaThreshold: Number(spec.alphaThreshold || 0),
    sampleStride: Number(spec.sampleStride || 1),
  });
}

async function waitRenderTextureReady(page, snapshot, spec, sleep) {
  const timeoutMs = Number(spec.timeoutMs || 12000);
  const stableFrames = Number(spec.stableFrames || 2);
  const minimum = Number(spec.minNonTransparentPixels == null ? 8 : spec.minNonTransparentPixels);
  const deadline = Date.now() + timeoutMs;
  const samples = [];
  let consecutive = 0;
  while (Date.now() < deadline) {
    const sample = await probeRenderTexture(page, snapshot, spec);
    sample.at = new Date().toISOString();
    sample.ready = sample.pass && sample.nonTransparentPixels >= minimum;
    samples.push(sample);
    consecutive = sample.ready ? consecutive + 1 : 0;
    if (consecutive >= stableFrames) return { pass: true, minimum, stableFrames, samples };
    await sleep(Math.min(Number(spec.pollMs || 100), Math.max(1, deadline - Date.now())));
  }
  return { pass: false, minimum, stableFrames, samples, code: 'RENDER_TEXTURE_READY_TIMEOUT' };
}

module.exports = {
  installSoundTrace,
  resetSoundTrace,
  readSoundTrace,
  soundEventMatches,
  evaluateSoundAssertions,
  probeRenderTexture,
  waitRenderTextureReady,
};
