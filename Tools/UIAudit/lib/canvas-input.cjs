'use strict';

const crypto = require('crypto');
const { findNodes } = require('./runtime-tree.cjs');
const { buildSelectorDiagnostic, SelectorIdentityError } = require('./selector-diagnostic.cjs');

const CANVAS_INPUT_SCHEMA = 'ui-audit.canvas-input.v1';

function centerOf(rect) {
  if (!rect || ![rect.x, rect.y, rect.width, rect.height].every(value => Number.isFinite(Number(value)))) {
    throw new Error(`INVALID_RUNTIME_BOUNDS: ${JSON.stringify(rect)}`);
  }
  if (Number(rect.width) <= 0 || Number(rect.height) <= 0) {
    throw new Error(`EMPTY_RUNTIME_BOUNDS: ${JSON.stringify(rect)}`);
  }
  return { x: Number(rect.x) + Number(rect.width) / 2, y: Number(rect.y) + Number(rect.height) / 2 };
}

function viewportOf(page) {
  if (typeof page.viewport === 'function') return page.viewport();
  if (typeof page.viewportSize === 'function') return page.viewportSize();
  return null;
}

function assertPointInViewport(point, viewport) {
  if (!viewport || !Number.isFinite(Number(viewport.width)) || !Number.isFinite(Number(viewport.height))) {
    throw new Error('VIEWPORT_UNAVAILABLE');
  }
  const pass = Number(point.x) >= 0 && Number(point.y) >= 0
    && Number(point.x) <= Number(viewport.width) && Number(point.y) <= Number(viewport.height);
  if (!pass) throw new Error(`POINT_OUTSIDE_VIEWPORT point=${JSON.stringify(point)} viewport=${JSON.stringify(viewport)}`);
  return true;
}

function validMatrix(matrix) {
  return matrix && ['a', 'b', 'c', 'd', 'tx', 'ty'].every(key => Number.isFinite(Number(matrix[key])))
    && Math.abs(Number(matrix.a) * Number(matrix.d) - Number(matrix.b) * Number(matrix.c)) > 1e-9;
}

async function readCanvasMetrics(page, snapshot) {
  const stage = snapshot && snapshot.stage || {};
  return page.evaluate(({ logicalWidth, logicalHeight }) => {
    const renderCanvas = window.Laya && Laya.Render && Laya.Render.canvas
      && (Laya.Render.canvas.source || Laya.Render.canvas._source || Laya.Render.canvas);
    const all = [...document.querySelectorAll('canvas')];
    const visible = all.map((canvas, index) => ({ canvas, index, rect: canvas.getBoundingClientRect() }))
      .filter(value => value.rect.width > 0 && value.rect.height > 0);
    const renderEntry = visible.find(value => value.canvas === renderCanvas);
    const selected = renderEntry || visible.sort((left, right) => right.rect.width * right.rect.height - left.rect.width * left.rect.height)[0];
    if (!selected) return null;
    const { canvas, rect, index } = selected;
    const layaStage = window.Laya && Laya.stage;
    const matrix = layaStage && layaStage._canvasTransform;
    return {
      selector: canvas.id ? `#${canvas.id}` : `canvas:nth-of-type(${index + 1})`,
      canvasIndex: index,
      canvasSource: renderEntry ? 'Laya.Render.canvas.source' : 'largest-visible-canvas-fallback',
      x: Number(rect.x), y: Number(rect.y), width: Number(rect.width), height: Number(rect.height),
      logicalWidth: Number(logicalWidth || layaStage && layaStage.width || canvas.width || rect.width),
      logicalHeight: Number(logicalHeight || layaStage && layaStage.height || canvas.height || rect.height),
      backingWidth: Number(canvas.width || 0), backingHeight: Number(canvas.height || 0),
      scrollX: Number(window.scrollX || 0), scrollY: Number(window.scrollY || 0),
      devicePixelRatio: Number(window.devicePixelRatio || 1),
      canvasTransform: matrix ? {
        a: Number(matrix.a), b: Number(matrix.b), c: Number(matrix.c), d: Number(matrix.d),
        tx: Number(matrix.tx), ty: Number(matrix.ty),
      } : null,
    };
  }, { logicalWidth: Number(stage.width || 0), logicalHeight: Number(stage.height || 0) });
}

function logicalToDomPoint(point, metrics) {
  if (!metrics || ![metrics.x, metrics.y, metrics.width, metrics.height, metrics.logicalWidth, metrics.logicalHeight]
    .every(value => Number.isFinite(Number(value)))) throw new Error('CANVAS_METRICS_UNAVAILABLE');
  if (Number(metrics.width) <= 0 || Number(metrics.height) <= 0 || Number(metrics.logicalWidth) <= 0 || Number(metrics.logicalHeight) <= 0) {
    throw new Error(`CANVAS_METRICS_INVALID: ${JSON.stringify(metrics)}`);
  }
  if (validMatrix(metrics.canvasTransform)) {
    const matrix = metrics.canvasTransform;
    return {
      x: Number(matrix.a) * Number(point.x) + Number(matrix.c) * Number(point.y) + Number(matrix.tx) - Number(metrics.scrollX || 0),
      y: Number(matrix.b) * Number(point.x) + Number(matrix.d) * Number(point.y) + Number(matrix.ty) - Number(metrics.scrollY || 0),
    };
  }
  return {
    x: Number(metrics.x) + Number(point.x) * Number(metrics.width) / Number(metrics.logicalWidth),
    y: Number(metrics.y) + Number(point.y) * Number(metrics.height) / Number(metrics.logicalHeight),
  };
}

function domToLogicalPoint(point, metrics) {
  if (!metrics) throw new Error('CANVAS_METRICS_UNAVAILABLE');
  if (validMatrix(metrics.canvasTransform)) {
    const matrix = metrics.canvasTransform;
    const determinant = Number(matrix.a) * Number(matrix.d) - Number(matrix.b) * Number(matrix.c);
    const pageX = Number(point.x) + Number(metrics.scrollX || 0) - Number(matrix.tx);
    const pageY = Number(point.y) + Number(metrics.scrollY || 0) - Number(matrix.ty);
    return {
      x: (Number(matrix.d) * pageX - Number(matrix.c) * pageY) / determinant,
      y: (-Number(matrix.b) * pageX + Number(matrix.a) * pageY) / determinant,
    };
  }
  return {
    x: (Number(point.x) - Number(metrics.x)) * Number(metrics.logicalWidth) / Number(metrics.width),
    y: (Number(point.y) - Number(metrics.y)) * Number(metrics.logicalHeight) / Number(metrics.height),
  };
}

function resolveTarget(snapshot, selector = {}) {
  const matches = findNodes(snapshot, selector);
  const expectedCount = selector.expectedCount == null ? 1 : Number(selector.expectedCount);
  if (matches.length !== expectedCount) {
    const diagnostic = buildSelectorDiagnostic(snapshot, selector, { expectedCount, actualCount: matches.length });
    throw new SelectorIdentityError(
      'CANVAS_TARGET_IDENTITY_MISMATCH',
      `CANVAS_TARGET_IDENTITY_MISMATCH expected=${expectedCount} actual=${matches.length} selector=${JSON.stringify(selector)} diagnosticSha256=${diagnostic.sha256}`,
      diagnostic,
    );
  }
  const index = selector.index == null ? 0 : Number(selector.index);
  const node = matches[index];
  if (!node) {
    const diagnostic = buildSelectorDiagnostic(snapshot, selector, { expectedCount, actualCount: matches.length });
    throw new SelectorIdentityError(
      'CANVAS_TARGET_INDEX_MISSING',
      `CANVAS_TARGET_INDEX_MISSING index=${index} diagnosticSha256=${diagnostic.sha256}`,
      diagnostic,
    );
  }
  if (!node.interaction.mouseEnabled || node.interaction.disabled || !node.displayed) {
    throw new Error(`CANVAS_TARGET_NOT_INTERACTIVE: ${node.path}`);
  }
  return node;
}

function pathIsInside(candidate, root) {
  const child = candidate && candidate.indexPath;
  const parent = root && root.indexPath;
  return Array.isArray(child) && Array.isArray(parent) && parent.length <= child.length
    && parent.every((part, index) => Number(part) === Number(child[index]));
}

function classifyPreInput(input) {
  if (!input || input.schema !== CANVAS_INPUT_SCHEMA) return { pass: false, classification: 'canvas-coordinate-mismatch', reason: 'input inspection unavailable' };
  if (!input.mapping || input.mapping.roundTripPass !== true || input.mapping.pointInsideCanvas !== true) {
    return { pass: false, classification: 'canvas-coordinate-mismatch', reason: input.mapping && input.mapping.reason || 'logical/DOM mapping did not round-trip' };
  }
  if (input.mapping.domCanvasTop !== true) {
    return { pass: false, classification: 'overlay-intercepted', reason: 'DOM element above the Laya render canvas' };
  }
  if (Number(input.targetResolution && input.targetResolution.actualCount) !== 1) {
    return { pass: false, classification: 'target-identity-changed', reason: 'live owner/bound-field identity is not unique' };
  }
  if (!input.target || input.target.hitAtPoint !== true) {
    return { pass: false, classification: 'overlay-intercepted', reason: 'target rejects the logical point' };
  }
  if (input.capture && input.capture.exclusive && input.capture.path
    && input.capture.path !== input.target.path && !pathIsInside(input.capture, input.target)) {
    const otherView = input.capture.ownerView && input.capture.ownerView !== input.target.ownerView;
    return { pass: false, classification: otherView ? 'stack-order-wrong' : 'overlay-intercepted', reason: 'exclusive mouse capture redirects the input' };
  }
  if (!input.topmost || !pathIsInside(input.topmost, input.target)) {
    const otherView = input.topmost && input.topmost.ownerView && input.topmost.ownerView !== input.target.ownerView;
    return {
      pass: false,
      classification: otherView ? 'stack-order-wrong' : 'overlay-intercepted',
      reason: otherView ? 'another owner view is topmost at the target point' : 'a sibling or ancestor intercepts the target point',
    };
  }
  return { pass: true, classification: 'topmost-target-ready', reason: null };
}

function classifyInputConsumption(evidence) {
  const domTypes = new Set((evidence && evidence.domEvents || []).map(event => event.type));
  const targetClicks = (evidence && evidence.targetEvents || []).filter(event => event.type === 'click');
  if (!domTypes.has('mousedown') || !domTypes.has('mouseup')) {
    return { pass: false, classification: 'event-not-dispatched', reason: 'canvas did not receive both mousedown and mouseup' };
  }
  if (!targetClicks.length) {
    return { pass: false, classification: 'event-not-dispatched', reason: 'the bound target did not receive Laya.Event.CLICK' };
  }
  if (!targetClicks.some(event => event.listenerCountBefore > 0 && event.dispatched !== false)) {
    return { pass: false, classification: 'event-not-dispatched', reason: 'the target click had no registered business listener' };
  }
  return {
    pass: true,
    classification: 'target-click-consumed',
    reason: null,
    closeSemanticObserved: Array.isArray(evidence && evidence.semanticCalls) && evidence.semanticCalls.length > 0,
  };
}

function canvasInputError(code, snapshot, selector, input, phase) {
  const expectedCount = selector.expectedCount == null ? 1 : Number(selector.expectedCount);
  const actualCount = findNodes(snapshot, selector).length;
  const diagnostic = buildSelectorDiagnostic(snapshot, selector, {
    expectedCount,
    actualCount,
    context: { kind: 'canvas-input', phase, input },
  });
  return new SelectorIdentityError(
    code,
    `${code}: ${JSON.stringify(input && input.evaluation || input)} diagnosticSha256=${diagnostic.sha256}`,
    diagnostic,
  );
}

async function inspectStageInput(page, node, point, domPoint, canvas, selector = {}) {
  if (node.source !== 'laya-stage' || !Array.isArray(node.indexPath)) {
    return {
      schema: CANVAS_INPUT_SCHEMA,
      applicable: false,
      pass: null,
      classification: 'not-laya-stage',
      reason: 'target is not a Laya.stage node',
    };
  }
  const input = await page.evaluate(payload => {
    const stage = window.Laya && Laya.stage;
    const manager = window.Laya && Laya.MouseManager && Laya.MouseManager.instance;
    const childrenOf = value => value && (value._children || (value.numChildren
      ? Array.from({ length: value.numChildren }, (_, index) => value.getChildAt(index)) : [])) || [];
    const indexPathOf = value => {
      const indices = [];
      let current = value;
      while (current && current !== stage) {
        const parent = current.parent;
        if (!parent) return null;
        const index = childrenOf(parent).indexOf(current);
        if (index < 0) return null;
        indices.unshift(index);
        current = parent;
      }
      return current === stage ? [0, ...indices] : null;
    };
    const nodeAt = indexPath => {
      let current = stage;
      for (const index of (indexPath || []).slice(1)) {
        current = childrenOf(current)[Number(index)];
        if (!current) return null;
      }
      return current;
    };
    const contains = (root, candidate) => root === candidate
      || !!(root && candidate && typeof root.contains === 'function' && root.contains(candidate));
    const qualifiedName = value => {
      try { return window.GetQualifiedClassName ? String(window.GetQualifiedClassName(value) || '') : ''; }
      catch (_) { return ''; }
    };
    const entries = [];
    const seenViews = new Set();
    const addView = (view, source, key) => {
      if (!view || seenViews.has(view)) return;
      seenViews.add(view);
      entries.push({
        view, source, key: String(key || ''), root: view.display_obj || null,
        names: [key, qualifiedName(view), view.layout_file, view.layoutFile, view.constructor && view.constructor.name]
          .filter(Boolean).map(String),
      });
    };
    try {
      const ViewManagerClass = window.ViewManager;
      const viewManager = ViewManagerClass && ViewManagerClass.GetInstance && ViewManagerClass.GetInstance();
      const dictionary = viewManager && (viewManager.view_dic || viewManager._view_dic) || {};
      for (const key of Object.keys(dictionary)) addView(dictionary[key], 'ViewManager', key);
    } catch (_) {}
    try {
      const registry = window.__sxPageSnapshotRegistry__ || {};
      for (const key of Object.keys(registry)) addView(registry[key] && registry[key].view, 'RuntimeRegistry', key);
    } catch (_) {}
    const matchesView = (entry, name) => !name || entry.names.includes(String(name));
    let targetCandidates = [];
    if (payload.selector.ownerView && payload.selector.boundField) {
      targetCandidates = entries.filter(entry => matchesView(entry, payload.selector.ownerView))
        .map(entry => ({ entry, node: entry.view && entry.view[payload.selector.boundField] }))
        .filter(candidate => candidate.node && contains(candidate.entry.root, candidate.node));
    } else {
      const current = nodeAt(payload.indexPath);
      if (current) targetCandidates = [{ entry: null, node: current }];
    }
    const uniqueTargets = [];
    for (const candidate of targetCandidates) {
      if (!uniqueTargets.some(item => item.node === candidate.node)) uniqueTargets.push(candidate);
    }
    const targetCandidate = uniqueTargets[Number(payload.selector.index || 0)] || null;
    const target = targetCandidate && targetCandidate.node;
    const ownerOf = value => {
      const owners = entries.filter(entry => entry.root && contains(entry.root, value))
        .sort((left, right) => (indexPathOf(right.root) || []).length - (indexPathOf(left.root) || []).length);
      const owner = owners[0] || null;
      if (!owner) return null;
      const requested = payload.selector.ownerView && owner.names.includes(String(payload.selector.ownerView))
        ? String(payload.selector.ownerView) : owner.names[0] || null;
      return { view: requested, source: owner.source, key: owner.key, rootPath: indexPathOf(owner.root) };
    };
    const boundsOf = value => {
      if (!value || typeof value.localToGlobal !== 'function') return null;
      try {
        const width = Number(value.width || 0), height = Number(value.height || 0);
        const points = [
          value.localToGlobal(new Laya.Point(0, 0), true),
          value.localToGlobal(new Laya.Point(width, 0), true),
          value.localToGlobal(new Laya.Point(0, height), true),
          value.localToGlobal(new Laya.Point(width, height), true),
        ];
        const xs = points.map(item => Number(item.x)), ys = points.map(item => Number(item.y));
        return { x: Math.min(...xs), y: Math.min(...ys), width: Math.max(...xs) - Math.min(...xs), height: Math.max(...ys) - Math.min(...ys) };
      } catch (_) { return null; }
    };
    const hitAt = value => {
      try { return value && typeof value.hitTestPoint === 'function' ? !!value.hitTestPoint(Number(payload.logicalPoint.x), Number(payload.logicalPoint.y)) : null; }
      catch (_) { return false; }
    };
    const describe = value => {
      if (!value) return null;
      const indexPath = indexPathOf(value);
      const owner = ownerOf(value);
      let effectiveAlpha = 1;
      let effectiveVisible = true;
      for (let current = value; current; current = current.parent) {
        effectiveAlpha *= Number(current.alpha == null ? 1 : current.alpha);
        effectiveVisible = effectiveVisible && current.visible !== false;
        if (current === stage) break;
      }
      const mask = value.mask || value._cacheStyle && value._cacheStyle.mask || null;
      return {
        path: indexPath ? indexPath.map((index, depth) => {
          const current = nodeAt(indexPath.slice(0, depth + 1));
          return `${String(current && (current.name || current.constructor && current.constructor.name) || 'node')}[${index}]`;
        }).join('/') : null,
        indexPath,
        name: String(value.name || ''),
        type: String(value.constructor && value.constructor.name || ''),
        ownerView: owner && owner.view || null,
        ownerInstance: owner ? { source: owner.source, key: owner.key, rootPath: owner.rootPath } : null,
        childIndex: indexPath && indexPath.length ? indexPath[indexPath.length - 1] : null,
        zOrder: Number(value.zOrder || 0),
        visible: value.visible !== false,
        displayed: value.displayedInStage !== false,
        alpha: Number(value.alpha == null ? 1 : value.alpha),
        effectiveAlpha,
        effectiveVisible,
        mouseEnabled: value.mouseEnabled !== false,
        mouseThrough: !!value.mouseThrough,
        hitTestPrior: !!value.hitTestPrior,
        mouseState: Number(value._mouseState == null ? 0 : value._mouseState),
        disabled: !!value.disabled,
        bounds: boundsOf(value),
        hitAtPoint: hitAt(value),
        scrollRect: value.scrollRect ? {
          x: Number(value.scrollRect.x || 0), y: Number(value.scrollRect.y || 0),
          width: Number(value.scrollRect.width || 0), height: Number(value.scrollRect.height || 0),
        } : null,
        mask: mask ? {
          path: indexPathOf(mask), name: String(mask.name || ''), visible: mask.visible !== false,
          alpha: Number(mask.alpha == null ? 1 : mask.alpha), bounds: boundsOf(mask), hitAtPoint: hitAt(mask),
        } : null,
      };
    };
    const chainOf = value => {
      const chain = [];
      for (let current = value; current; current = current.parent) {
        chain.unshift(describe(current));
        if (current === stage) break;
      }
      return chain;
    };
    const allCanvas = [...document.querySelectorAll('canvas')];
    const renderCanvas = window.Laya && Laya.Render && Laya.Render.canvas
      && (Laya.Render.canvas.source || Laya.Render.canvas._source || Laya.Render.canvas);
    const canvas = renderCanvas && allCanvas.includes(renderCanvas) ? renderCanvas : allCanvas[Number(payload.canvas.canvasIndex)] || null;
    const elementAtPoint = document.elementFromPoint(Number(payload.domPoint.x), Number(payload.domPoint.y));
    const rect = canvas && canvas.getBoundingClientRect();
    let roundTrip = null;
    try {
      const pagePoint = new Laya.Point(Number(payload.domPoint.x) + Number(window.scrollX || 0), Number(payload.domPoint.y) + Number(window.scrollY || 0));
      stage._canvasTransform.invertTransformPoint(pagePoint);
      roundTrip = { x: Number(pagePoint.x), y: Number(pagePoint.y) };
    } catch (_) {
      roundTrip = {
        x: rect ? (Number(payload.domPoint.x) - rect.x) * Number(stage.width || rect.width) / rect.width : NaN,
        y: rect ? (Number(payload.domPoint.y) - rect.y) * Number(stage.height || rect.height) / rect.height : NaN,
      };
    }
    const roundTripError = roundTrip ? Math.hypot(roundTrip.x - Number(payload.logicalPoint.x), roundTrip.y - Number(payload.logicalPoint.y)) : Infinity;
    const mapping = {
      logicalPoint: payload.logicalPoint,
      domPoint: payload.domPoint,
      runtimeRoundTrip: roundTrip,
      roundTripError,
      roundTripPass: Number.isFinite(roundTripError) && roundTripError <= 1,
      pointInsideCanvas: !!(rect && payload.domPoint.x >= rect.left && payload.domPoint.x <= rect.right
        && payload.domPoint.y >= rect.top && payload.domPoint.y <= rect.bottom),
      domCanvasTop: !!(canvas && (elementAtPoint === canvas || canvas.contains(elementAtPoint))),
      domTopElement: elementAtPoint ? {
        tag: String(elementAtPoint.tagName || ''), id: String(elementAtPoint.id || ''), className: String(elementAtPoint.className || ''),
      } : null,
      canvas: payload.canvas,
      reason: null,
    };
    let topmost = null;
    let topmostError = null;
    if (manager && stage) {
      const saved = {
        target: manager._target, hitCapture: manager._hitCaputreSp,
        point: manager._point && { x: manager._point.x, y: manager._point.y },
        rect: manager._rect && { x: manager._rect.x, y: manager._rect.y, width: manager._rect.width, height: manager._rect.height },
      };
      try {
        if (manager._captureExlusiveMode && manager._captureSp) topmost = manager._captureSp;
        else manager.check(stage, Number(payload.logicalPoint.x), Number(payload.logicalPoint.y), value => { topmost = value; });
      } catch (error) { topmostError = String(error); }
      finally {
        manager._target = saved.target;
        manager._hitCaputreSp = saved.hitCapture;
        if (manager._point && saved.point) manager._point.setTo(saved.point.x, saved.point.y);
        if (manager._rect && saved.rect) manager._rect.setTo(saved.rect.x, saved.rect.y, saved.rect.width, saved.rect.height);
      }
    }
    const targetDescription = describe(target);
    const topDescription = describe(topmost);
    const targetChain = chainOf(target);
    const topmostChain = chainOf(topmost);
    let commonDepth = 0;
    while (commonDepth < targetChain.length && commonDepth < topmostChain.length
      && JSON.stringify(targetChain[commonDepth] && targetChain[commonDepth].indexPath)
        === JSON.stringify(topmostChain[commonDepth] && topmostChain[commonDepth].indexPath)) commonDepth++;
    const occlusion = {
      intercepted: !(target && topmost && contains(target, topmost)),
      commonAncestor: commonDepth ? targetChain[commonDepth - 1] : null,
      targetBranch: targetChain.slice(commonDepth),
      topmostBranch: topmostChain.slice(commonDepth),
    };
    const capture = manager && manager._captureSp ? {
      ...describe(manager._captureSp), exclusive: !!manager._captureExlusiveMode,
    } : null;
    return {
      schema: payload.schema,
      applicable: true,
      targetResolution: {
        requestedIndexPath: payload.indexPath,
        currentIndexPath: targetDescription && targetDescription.indexPath || null,
        actualCount: uniqueTargets.length,
        via: payload.selector.ownerView && payload.selector.boundField ? 'owner-view-bound-field' : 'stage-index-path',
      },
      target: targetDescription,
      targetChain,
      topmost: topDescription,
      topmostChain,
      occlusion,
      capture,
      mapping,
      mouseManager: manager ? {
        available: true, enabled: Laya.MouseManager.enabled !== false,
        disableMouseEvent: !!manager.disableMouseEvent, topmostError,
      } : { available: false, topmostError: 'Laya.MouseManager.instance missing' },
    };
  }, {
    operation: 'inspect-canvas-input', schema: CANVAS_INPUT_SCHEMA,
    indexPath: node.indexPath, selector, logicalPoint: point, domPoint, canvas,
  });
  const evaluation = classifyPreInput(input);
  return { ...input, ...evaluation, evaluation };
}

async function installInputProbe(page, node, selector, canvas) {
  const probeId = `ui-audit-input-${crypto.randomUUID()}`;
  return page.evaluate(payload => {
    const stage = window.Laya && Laya.stage;
    const childrenOf = value => value && (value._children || (value.numChildren
      ? Array.from({ length: value.numChildren }, (_, index) => value.getChildAt(index)) : [])) || [];
    const indexPathOf = value => {
      const result = [];
      let current = value;
      while (current && current !== stage) {
        const parent = current.parent;
        if (!parent) return null;
        const index = childrenOf(parent).indexOf(current);
        if (index < 0) return null;
        result.unshift(index);
        current = parent;
      }
      return current === stage ? [0, ...result] : null;
    };
    const nodeAt = indexPath => {
      let current = stage;
      for (const index of (indexPath || []).slice(1)) {
        current = childrenOf(current)[Number(index)];
        if (!current) return null;
      }
      return current;
    };
    const contains = (root, candidate) => root === candidate
      || !!(root && candidate && typeof root.contains === 'function' && root.contains(candidate));
    const qualifiedName = value => {
      try { return window.GetQualifiedClassName ? String(window.GetQualifiedClassName(value) || '') : ''; }
      catch (_) { return ''; }
    };
    const entries = [];
    const seen = new Set();
    const add = (view, source, key) => {
      if (!view || seen.has(view)) return;
      seen.add(view);
      entries.push({ view, source, key: String(key || ''), root: view.display_obj || null,
        names: [key, qualifiedName(view), view.layout_file, view.layoutFile, view.constructor && view.constructor.name].filter(Boolean).map(String) });
    };
    try {
      const Manager = window.ViewManager;
      const manager = Manager && Manager.GetInstance && Manager.GetInstance();
      const dictionary = manager && (manager.view_dic || manager._view_dic) || {};
      for (const key of Object.keys(dictionary)) add(dictionary[key], 'ViewManager', key);
    } catch (_) {}
    try {
      const registry = window.__sxPageSnapshotRegistry__ || {};
      for (const key of Object.keys(registry)) add(registry[key] && registry[key].view, 'RuntimeRegistry', key);
    } catch (_) {}
    let candidates = [];
    if (payload.selector.ownerView && payload.selector.boundField) {
      candidates = entries.filter(entry => entry.names.includes(String(payload.selector.ownerView)))
        .map(entry => ({ entry, node: entry.view && entry.view[payload.selector.boundField] }))
        .filter(candidate => candidate.node && contains(candidate.entry.root, candidate.node));
    } else {
      const target = nodeAt(payload.indexPath);
      if (target) candidates = [{ entry: null, node: target }];
    }
    candidates = candidates.filter((candidate, index) => candidates.findIndex(other => other.node === candidate.node) === index);
    const candidate = candidates[Number(payload.selector.index || 0)] || null;
    if (!candidate || candidates.length !== 1) return { pass: false, actualCount: candidates.length, reason: 'live target identity changed' };
    const target = candidate.node;
    const view = candidate.entry && candidate.entry.view || null;
    const allCanvas = [...document.querySelectorAll('canvas')];
    const renderCanvas = window.Laya && Laya.Render && Laya.Render.canvas
      && (Laya.Render.canvas.source || Laya.Render.canvas._source || Laya.Render.canvas);
    const canvas = renderCanvas && allCanvas.includes(renderCanvas) ? renderCanvas : allCanvas[Number(payload.canvas.canvasIndex)] || null;
    if (!canvas) return { pass: false, actualCount: 1, reason: 'render canvas missing' };
    const listenerCount = type => {
      const listeners = target._events && target._events[type];
      if (!listeners) return 0;
      if (listeners.run) return 1;
      return listeners.filter(Boolean).length;
    };
    const viewState = () => ({
      displayVisible: view && view.display_obj ? view.display_obj.visible !== false : null,
      displayedInStage: view && view.display_obj ? view.display_obj.displayedInStage !== false : null,
      isPop: view && typeof view.isPop === 'boolean' ? view.isPop : view && typeof view._isPop === 'boolean' ? view._isPop : null,
      openFlag: view && typeof view.open === 'boolean' ? view.open : view && typeof view._open === 'boolean' ? view._open : null,
    });
    const trace = {
      schema: payload.schema, probeId: payload.probeId, createdAt: new Date().toISOString(),
      target: { path: indexPathOf(target), name: String(target.name || ''), ownerView: payload.selector.ownerView || null },
      listenersBefore: { mousedown: listenerCount('mousedown'), mouseup: listenerCount('mouseup'), click: listenerCount('click') },
      targetEvents: [], domEvents: [], semanticCalls: [], viewBefore: viewState(),
    };
    const domListener = event => trace.domEvents.push({
      type: event.type, clientX: Number(event.clientX), clientY: Number(event.clientY), button: Number(event.button), timeStamp: Number(event.timeStamp),
    });
    for (const type of ['mousedown', 'mouseup', 'click']) canvas.addEventListener(type, domListener, true);
    const hadOwnEvent = Object.prototype.hasOwnProperty.call(target, 'event');
    const originalEvent = target.event;
    const eventWrapper = function eventWrapper(type, data) {
      const before = listenerCount(type);
      const targetPath = data && data.target ? indexPathOf(data.target) : null;
      const currentPath = data && data.currentTarget ? indexPathOf(data.currentTarget) : indexPathOf(this);
      const result = originalEvent.apply(this, arguments);
      trace.targetEvents.push({ type: String(type), listenerCountBefore: before, dispatched: !!result, eventTargetPath: targetPath, currentTargetPath: currentPath });
      return result;
    };
    target.event = eventWrapper;
    const semanticWrappers = [];
    if (view) {
      for (const name of ['Close', 'CloseView', 'Hide', 'DeleteMe', 'Remove']) {
        const original = view[name];
        if (typeof original !== 'function') continue;
        const hadOwn = Object.prototype.hasOwnProperty.call(view, name);
        const wrapper = function semanticWrapper() {
          trace.semanticCalls.push({ name, at: new Date().toISOString() });
          return original.apply(this, arguments);
        };
        try {
          view[name] = wrapper;
          if (view[name] === wrapper) semanticWrappers.push({ name, original, wrapper, hadOwn });
        } catch (_) {}
      }
    }
    window.__uiAuditInputProbes__ = window.__uiAuditInputProbes__ || {};
    window.__uiAuditInputProbes__[payload.probeId] = {
      trace, target, view, canvas, domListener, originalEvent, eventWrapper, hadOwnEvent, semanticWrappers, viewState, indexPathOf,
    };
    return { pass: true, probeId: payload.probeId, target: trace.target, listenersBefore: trace.listenersBefore, viewBefore: trace.viewBefore };
  }, {
    operation: 'install-canvas-input-probe', schema: CANVAS_INPUT_SCHEMA,
    probeId, indexPath: node.indexPath, selector, canvas,
  });
}

async function finishInputProbe(page, probeId) {
  return page.evaluate(payload => {
    const store = window.__uiAuditInputProbes__ || {};
    const probe = store[payload.probeId];
    if (!probe) return { schema: payload.schema, probeId: payload.probeId, missing: true, targetEvents: [], domEvents: [], semanticCalls: [] };
    const { trace, target, view, canvas, domListener, originalEvent, eventWrapper, hadOwnEvent, semanticWrappers, viewState, indexPathOf } = probe;
    try {
      if (target.event === eventWrapper) {
        if (hadOwnEvent) target.event = originalEvent;
        else delete target.event;
      }
      for (const item of semanticWrappers) {
        if (view[item.name] !== item.wrapper) continue;
        if (item.hadOwn) view[item.name] = item.original;
        else delete view[item.name];
      }
      for (const type of ['mousedown', 'mouseup', 'click']) canvas.removeEventListener(type, domListener, true);
      const manager = window.Laya && Laya.MouseManager && Laya.MouseManager.instance;
      trace.managerTargetAfter = manager && manager._target ? indexPathOf(manager._target) : null;
      trace.viewAfter = viewState();
      trace.finishedAt = new Date().toISOString();
      return trace;
    } finally {
      delete store[payload.probeId];
    }
  }, { operation: 'finish-canvas-input-probe', schema: CANVAS_INPUT_SCHEMA, probeId });
}

async function probeStageHit(page, node, point = centerOf(node.bounds), options = {}) {
  const canvas = options.canvasMetrics || await readCanvasMetrics(page, options.snapshot || { stage: {} });
  const domPoint = logicalToDomPoint(point, canvas);
  return inspectStageInput(page, node, point, domPoint, canvas, options.selector || {});
}

async function clickRuntimeTarget(page, snapshot, selector, options = {}) {
  const target = resolveTarget(snapshot, selector);
  const logicalPoint = options.point || centerOf(target.bounds);
  const canvas = options.canvasMetrics || await readCanvasMetrics(page, snapshot);
  const point = logicalToDomPoint(logicalPoint, canvas);
  assertPointInViewport(point, viewportOf(page));
  const hit = await inspectStageInput(page, target, logicalPoint, point, canvas, selector);
  if (hit.applicable && !hit.pass) {
    const code = hit.classification === 'canvas-coordinate-mismatch' ? 'CANVAS_COORDINATE_MISMATCH'
      : hit.classification === 'stack-order-wrong' ? 'CANVAS_STACK_ORDER_WRONG'
        : hit.classification === 'target-identity-changed' ? 'CANVAS_TARGET_IDENTITY_CHANGED'
          : 'CANVAS_OVERLAY_INTERCEPTED';
    throw canvasInputError(code, snapshot, selector, hit, 'pre-click');
  }
  const installed = await installInputProbe(page, target, selector, canvas);
  if (!installed || !installed.pass) {
    const input = { ...hit, probeInstall: installed, evaluation: { pass: false, classification: 'target-identity-changed', reason: installed && installed.reason } };
    throw canvasInputError('CANVAS_TARGET_IDENTITY_CHANGED', snapshot, selector, input, 'probe-install');
  }
  let evidence;
  try {
    await page.mouse.click(point.x, point.y, {
      button: options.button || 'left',
      clickCount: options.clickCount || 1,
      delay: options.delay || 0,
    });
  } finally {
    evidence = await finishInputProbe(page, installed.probeId);
  }
  const consumption = classifyInputConsumption(evidence);
  const input = { preflight: hit, evidence, consumption };
  if (!consumption.pass) throw canvasInputError('CANVAS_EVENT_NOT_DISPATCHED', snapshot, selector, input, 'post-click');
  return { action: 'click', target, logicalPoint, point, canvas, hit, input };
}

async function dragRuntimeTarget(page, snapshot, selector, options = {}) {
  const target = resolveTarget(snapshot, selector);
  const logicalStart = options.start || centerOf(target.bounds);
  const logicalEnd = options.end || {
    x: logicalStart.x + Number(options.deltaX || 0),
    y: logicalStart.y + Number(options.deltaY || 0),
  };
  const canvas = options.canvasMetrics || await readCanvasMetrics(page, snapshot);
  const start = logicalToDomPoint(logicalStart, canvas);
  const end = logicalToDomPoint(logicalEnd, canvas);
  const viewport = viewportOf(page);
  assertPointInViewport(start, viewport);
  assertPointInViewport(end, viewport);
  const hit = await inspectStageInput(page, target, logicalStart, start, canvas, selector);
  if (hit.applicable && !hit.pass) {
    const code = hit.classification === 'canvas-coordinate-mismatch' ? 'CANVAS_COORDINATE_MISMATCH'
      : hit.classification === 'stack-order-wrong' ? 'CANVAS_STACK_ORDER_WRONG' : 'CANVAS_OVERLAY_INTERCEPTED';
    throw canvasInputError(code, snapshot, selector, hit, 'pre-drag');
  }
  await page.mouse.move(start.x, start.y);
  await page.mouse.down({ button: options.button || 'left' });
  try {
    await page.mouse.move(end.x, end.y, { steps: options.steps || 12 });
  } finally {
    await page.mouse.up({ button: options.button || 'left' });
  }
  return { action: 'drag', target, logicalStart, logicalEnd, start, end, canvas, hit };
}

module.exports = {
  CANVAS_INPUT_SCHEMA,
  centerOf,
  viewportOf,
  assertPointInViewport,
  readCanvasMetrics,
  logicalToDomPoint,
  domToLogicalPoint,
  resolveTarget,
  classifyPreInput,
  classifyInputConsumption,
  inspectStageInput,
  installInputProbe,
  finishInputProbe,
  probeStageHit,
  clickRuntimeTarget,
  dragRuntimeTarget,
};
