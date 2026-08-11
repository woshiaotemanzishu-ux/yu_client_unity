'use strict';

const crypto = require('crypto');
const path = require('path');
const { safeStringify, writeJsonAtomic } = require('./safe-json.cjs');

const SELECTOR_DIAGNOSTIC_SCHEMA = 'ui-audit.selector-diagnostic.v1';

function projectNode(node) {
  return {
    schema: node.schema,
    source: node.source,
    view: node.view,
    path: node.path,
    indexPath: node.indexPath,
    parentPath: node.parentPath,
    depth: node.depth,
    name: node.name,
    type: node.type,
    visible: node.visible,
    displayed: node.displayed,
    bounds: node.bounds,
    identity: node.identity,
    interaction: node.interaction,
    state: node.state,
  };
}

function bindingFields(node) {
  return node && node.identity && Array.isArray(node.identity.bindings)
    ? node.identity.bindings.map(binding => binding.field) : [];
}

function scoreCandidate(node, selector, targetView) {
  let score = 0;
  const reasons = [];
  const runtimeName = selector.runtimeName || selector.name || null;
  const fields = bindingFields(node);
  if (selector.source && node.source === selector.source) { score += 4; reasons.push('source'); }
  if (targetView && (node.view === targetView || node.identity && node.identity.ownerView === targetView)) {
    score += 8; reasons.push('owner-view');
  }
  if (runtimeName && node.name === runtimeName) { score += 16; reasons.push('runtime-name'); }
  if (selector.boundField && fields.includes(selector.boundField)) { score += 32; reasons.push('bound-field'); }
  if (selector.text && String(node.text || '').trim() === String(selector.text).trim()) { score += 2; reasons.push('text'); }
  if (node.visible && node.displayed) { score += 1; reasons.push('visible'); }
  if (node.interaction && node.interaction.mouseEnabled && !node.interaction.disabled) {
    score += 1; reasons.push('interactive');
  }
  return { score, reasons };
}

function buildSelectorDiagnostic(snapshot, selector, options = {}) {
  const nodes = Array.isArray(snapshot && snapshot.nodes) ? snapshot.nodes : [];
  const targetView = String(selector && (selector.ownerView || selector.view) || '') || null;
  const maxSubtreeNodes = Number(options.maxSubtreeNodes || 160);
  const maxCandidates = Number(options.maxCandidates || 32);
  const targetNodes = nodes.filter(node => node.source === 'laya-stage' && targetView
    && (node.view === targetView || node.identity && node.identity.ownerView === targetView));
  let candidates = nodes.map(node => ({ node, ...scoreCandidate(node, selector || {}, targetView) }))
    .filter(candidate => candidate.score > 0)
    .sort((left, right) => right.score - left.score || String(left.node.path).localeCompare(String(right.node.path)))
    .slice(0, maxCandidates)
    .map(candidate => ({ score: candidate.score, reasons: candidate.reasons, node: projectNode(candidate.node) }));
  if (!candidates.length && targetNodes.length) {
    candidates = targetNodes.filter(node => node.interaction && node.interaction.mouseEnabled)
      .slice(0, maxCandidates).map(node => ({ score: 0, reasons: ['target-view-interactive-fallback'], node: projectNode(node) }));
  }
  const selectorExpectedCount = selector && selector.expectedCount == null ? 1 : Number(selector && selector.expectedCount);
  const expectedCount = options.expectedCount == null ? selectorExpectedCount : Number(options.expectedCount);
  const subtreeNodes = targetNodes.slice(0, maxSubtreeNodes).map(projectNode);
  const subtreeSha256 = crypto.createHash('sha256').update(safeStringify(subtreeNodes, 0)).digest('hex');
  const core = {
    schema: SELECTOR_DIAGNOSTIC_SCHEMA,
    capturedAt: snapshot && snapshot.capturedAt || null,
    selector: selector || {},
    expectedCount,
    actualCount: Number(options.actualCount == null ? 0 : options.actualCount),
    targetView,
    runtimeSources: snapshot && snapshot.sources || null,
    stage: snapshot && snapshot.stage || null,
    subtree: {
      total: targetNodes.length,
      truncated: targetNodes.length > maxSubtreeNodes,
      sha256: subtreeSha256,
      nodes: subtreeNodes,
    },
    candidates,
    context: options.context || null,
  };
  const sha256 = crypto.createHash('sha256').update(safeStringify(core, 0)).digest('hex');
  return { ...core, sha256 };
}

function safeSegment(value) {
  const text = String(value || 'unknown').replace(/[^a-zA-Z0-9._-]+/g, '-').replace(/^-+|-+$/g, '');
  return (text || 'unknown').slice(0, 80);
}

function writeSelectorDiagnostic(outputDir, diagnostic) {
  if (!diagnostic || diagnostic.schema !== SELECTOR_DIAGNOSTIC_SCHEMA || !diagnostic.sha256) {
    throw new Error('SELECTOR_DIAGNOSTIC_INVALID');
  }
  const identity = diagnostic.selector.boundField || diagnostic.selector.runtimeName
    || diagnostic.selector.name || 'identity';
  const filename = `selector-diagnostic-${safeSegment(diagnostic.targetView)}-${safeSegment(identity)}-${diagnostic.sha256.slice(0, 12)}.json`;
  return writeJsonAtomic(path.join(outputDir, filename), diagnostic);
}

class SelectorIdentityError extends Error {
  constructor(code, message, diagnostic) {
    super(message);
    this.name = 'SelectorIdentityError';
    this.code = code;
    this.diagnostic = diagnostic;
  }
}

module.exports = {
  SELECTOR_DIAGNOSTIC_SCHEMA,
  projectNode,
  buildSelectorDiagnostic,
  writeSelectorDiagnostic,
  SelectorIdentityError,
};
