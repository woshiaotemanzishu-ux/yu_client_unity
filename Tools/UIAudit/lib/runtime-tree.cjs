'use strict';

const { RUNTIME_NODE_SCHEMA_VERSION } = require('./version.cjs');

const NODE_SCHEMA = `ui-audit.runtime-node.v${RUNTIME_NODE_SCHEMA_VERSION}`;

function finite(value, fallback = 0) {
  const number = Number(value);
  return Number.isFinite(number) ? number : fallback;
}

function normalizeRect(rect) {
  if (!rect) return null;
  const value = {
    x: finite(rect.x),
    y: finite(rect.y),
    width: finite(rect.width),
    height: finite(rect.height),
  };
  return value.width >= 0 && value.height >= 0 ? value : null;
}

function identitySubsetMatches(actual, expected) {
  if (expected == null || typeof expected !== 'object' || Array.isArray(expected)) return actual === expected;
  if (actual == null || typeof actual !== 'object' || Array.isArray(actual)) return false;
  return Object.entries(expected).every(([key, value]) => identitySubsetMatches(actual[key], value));
}

function normalizedNode(raw, context = {}) {
  const name = String(raw && raw.name || '');
  const type = String(raw && raw.type || raw && raw.constructorName || '');
  const path = String(context.path || raw && raw.path || name || type || 'node');
  return {
    schema: NODE_SCHEMA,
    source: context.source || raw && raw.source || 'unknown',
    view: context.view || raw && raw.view || null,
    path,
    indexPath: Array.isArray(raw && raw.indexPath) ? raw.indexPath.map(Number) : null,
    parentPath: raw && raw.parentPath || context.parentPath || null,
    depth: finite(raw && raw.depth, context.depth || 0),
    name,
    type,
    visible: raw && raw.visible !== false && raw && raw.effectiveVisible !== false,
    displayed: raw && raw.displayedInStage !== false,
    bounds: normalizeRect(raw && (raw.bounds || raw.globalBounds)),
    local: normalizeRect(raw && (raw.local || raw.localRect)),
    pivot: {
      x: finite(raw && raw.pivot && raw.pivot.x != null ? raw.pivot.x : raw && raw.pivotX),
      y: finite(raw && raw.pivot && raw.pivot.y != null ? raw.pivot.y : raw && raw.pivotY),
    },
    anchor: {
      x: finite(raw && raw.anchor && raw.anchor.x != null ? raw.anchor.x : raw && raw.anchorX),
      y: finite(raw && raw.anchor && raw.anchor.y != null ? raw.anchor.y : raw && raw.anchorY),
    },
    scale: {
      x: finite(raw && raw.scale && raw.scale.x != null ? raw.scale.x : raw && raw.scaleX, 1),
      y: finite(raw && raw.scale && raw.scale.y != null ? raw.scale.y : raw && raw.scaleY, 1),
    },
    alpha: finite(raw && raw.alpha, 1),
    zOrder: finite(raw && raw.zOrder),
    text: typeof (raw && raw.text) === 'string' ? raw.text : '',
    html: typeof (raw && raw.html) === 'string' ? raw.html : '',
    skin: typeof (raw && raw.skin) === 'string' ? raw.skin : '',
    interaction: {
      mouseEnabled: raw && raw.mouseEnabled !== false,
      mouseThrough: !!(raw && raw.mouseThrough),
      disabled: !!(raw && raw.disabled),
      hitTestCenter: raw && raw.hitTestCenter == null ? null : !!raw.hitTestCenter,
    },
    state: {
      selected: raw && raw.selected == null ? null : !!raw.selected,
      gray: !!(raw && raw.gray),
      isAnim: raw && typeof raw.isAnim === 'boolean' ? raw.isAnim : null,
      frameToken: raw && raw.frameToken == null ? null : finite(raw.frameToken),
      dataIdentity: raw && raw.dataIdentity || null,
      scroll: raw && raw.scroll || null,
      scrollRect: normalizeRect(raw && raw.scrollRect),
    },
  };
}

function flattenManagedTree(root, viewName, options = {}) {
  const maxDepth = options.maxDepth || 80;
  const maxNodes = options.maxNodes || 50000;
  const nodes = [];
  const walk = (raw, parentPath, depth, siblingIndex) => {
    if (!raw || depth > maxDepth || nodes.length >= maxNodes) return;
    const segment = `${String(raw.name || raw.type || 'node')}[${siblingIndex}]`;
    const currentPath = parentPath ? `${parentPath}/${segment}` : segment;
    nodes.push(normalizedNode(raw, {
      source: 'managed-view', view: viewName, path: currentPath, parentPath, depth,
    }));
    const children = Array.isArray(raw.children) ? raw.children : [];
    children.forEach((child, index) => walk(child, currentPath, depth + 1, index));
  };
  walk(root, null, 0, 0);
  return nodes;
}

function normalizeRuntimeSources(raw, options = {}) {
  const loaded = Array.isArray(raw && raw.loaded) ? raw.loaded : [];
  const managedViews = Array.isArray(raw && raw.managed && raw.managed.views)
    ? raw.managed.views : [];
  const stageRows = Array.isArray(raw && raw.stage && raw.stage.nodes) ? raw.stage.nodes : [];
  const nodes = [];

  for (const view of loaded) {
    nodes.push(normalizedNode({
      name: view.name,
      type: 'LoadedView',
      visible: view.visible,
      displayedInStage: view.visible,
      dataIdentity: { loaded: view.loaded !== false, open: view.open !== false },
    }, { source: 'loaded-view', view: String(view.name || ''), path: `loaded/${String(view.name || '')}` }));
  }

  for (const view of managedViews) {
    const viewName = String(view && view.meta && view.meta.name || view && view.name || '');
    nodes.push(...flattenManagedTree(view && view.nodeTree, viewName, options));
  }

  for (const row of stageRows) {
    nodes.push(normalizedNode(row, {
      source: 'laya-stage',
      view: row.view || null,
      path: row.path,
      parentPath: row.parentPath,
      depth: row.depth,
    }));
  }

  const visibleViews = [];
  const seen = new Set();
  for (const node of nodes) {
    if (!node.view || !node.visible || seen.has(node.view)) continue;
    if (node.source === 'loaded-view' || node.name === node.view) {
      seen.add(node.view);
      visibleViews.push(node.view);
    }
  }

  return {
    schema: RUNTIME_NODE_SCHEMA_VERSION,
    nodeSchema: NODE_SCHEMA,
    capturedAt: raw && raw.capturedAt || new Date().toISOString(),
    stage: raw && raw.stage && raw.stage.meta || null,
    sources: {
      loadedViews: loaded.length,
      managedViews: managedViews.length,
      stageNodes: stageRows.length,
    },
    visibleViews,
    nodes,
    warnings: Array.isArray(raw && raw.warnings) ? raw.warnings : [],
  };
}

async function collectRuntimeSources(page, options = {}) {
  const maxDepth = options.maxDepth || 80;
  const maxNodes = options.maxNodes || 50000;
  return page.evaluate(({ maxDepth, maxNodes }) => {
    const warnings = [];
    const cloneWithoutCycles = value => {
      const ancestors = new Set();
      const visit = input => {
        if (input === undefined || typeof input === 'function' || typeof input === 'symbol') return undefined;
        if (input === null || typeof input !== 'object') return input;
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
      return visit(value);
    };
    let loaded = [];
    let managed = { views: [] };
    try {
      if (typeof window.__sxListLoadedPages__ === 'function') {
        const listed = window.__sxListLoadedPages__();
        loaded = (listed.views || []).map(view => ({
          name: String(view.name || ''),
          visible: view.visible !== false,
          loaded: view.loaded !== false,
          open: view.open !== false,
        }));
      } else warnings.push('loaded-view runtime missing');
    } catch (error) { warnings.push(`loaded-view: ${String(error)}`); }
    try {
      if (typeof window.__sxExportPageSnapshots__ === 'function') {
        managed = cloneWithoutCycles(window.__sxExportPageSnapshots__(loaded.map(view => view.name)));
      } else warnings.push('managed-view runtime missing');
    } catch (error) { warnings.push(`managed-view: ${String(error)}`); }

    const stage = window.Laya && window.Laya.stage;
    const stageResult = { meta: null, nodes: [] };
    if (!stage) {
      warnings.push('Laya.stage missing');
    } else {
      const childrenOf = node => node && (node._children || (node.numChildren
        ? Array.from({ length: node.numChildren }, (_, index) => node.getChildAt(index)) : [])) || [];
      const boundsOf = node => {
        try {
          const width = Number(node.width || 0), height = Number(node.height || 0);
          const points = [
            node.localToGlobal(new Laya.Point(0, 0), true),
            node.localToGlobal(new Laya.Point(width, 0), true),
            node.localToGlobal(new Laya.Point(0, height), true),
            node.localToGlobal(new Laya.Point(width, height), true),
          ];
          const xs = points.map(point => Number(point.x));
          const ys = points.map(point => Number(point.y));
          return { x: Math.min(...xs), y: Math.min(...ys), width: Math.max(...xs) - Math.min(...xs), height: Math.max(...ys) - Math.min(...ys) };
        } catch (_) { return null; }
      };
      const identityOf = node => {
        for (const key of ['_goodsVo', 'goodsVo', '_dataSource', 'dataSource', '_itemData', 'itemData']) {
          const value = node && node[key];
          if (!value || typeof value !== 'object') continue;
          const identity = {};
          for (const [field, fieldValue] of Object.entries(value)) {
            if (!/(^id$|^name$|_id$|Id$)/.test(field)) continue;
            if (fieldValue != null && ['string', 'number', 'boolean'].includes(typeof fieldValue)) identity[field] = fieldValue;
            if (Object.keys(identity).length >= 32) break;
          }
          if (Object.keys(identity).length) return identity;
        }
        return null;
      };
      const timer = window.Laya && Laya.timer;
      const frameToken = timer && (timer.currFrame != null ? timer.currFrame : timer._currFrame);
      stageResult.meta = {
        width: Number(stage.width || 0), height: Number(stage.height || 0),
        scaleX: Number(stage.scaleX == null ? 1 : stage.scaleX),
        scaleY: Number(stage.scaleY == null ? 1 : stage.scaleY),
        frameToken: Number.isFinite(Number(frameToken)) ? Number(frameToken) : null,
      };
      const walk = (node, parentVisible, parentPath, indexPath, depth, activeView) => {
        if (!node || depth > maxDepth || stageResult.nodes.length >= maxNodes) return;
        const name = String(node.name || '');
        const type = String(node.constructor && node.constructor.name || '');
        const path = `${parentPath ? `${parentPath}/` : ''}${name || type || 'node'}[${indexPath[indexPath.length - 1] || 0}]`;
        const visible = parentVisible && node.visible !== false && Number(node.alpha == null ? 1 : node.alpha) !== 0;
        const nextView = /View$/.test(name) ? name : activeView;
        const bounds = boundsOf(node);
        let hitTestCenter = null;
        if (bounds && bounds.width > 0 && bounds.height > 0 && typeof node.hitTestPoint === 'function') {
          try { hitTestCenter = !!node.hitTestPoint(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2); } catch (_) {}
        }
        const vBar = node.vScrollBar || (node._scrollBar && node._scrollBar.isVertical ? node._scrollBar : null);
        const hBar = node.hScrollBar || (node._scrollBar && !node._scrollBar.isVertical ? node._scrollBar : null);
        stageResult.nodes.push({
          name, type, view: nextView || null, path, parentPath: parentPath || null, indexPath, depth,
          visible, displayedInStage: node.displayedInStage !== false, bounds,
          local: { x: Number(node.x || 0), y: Number(node.y || 0), width: Number(node.width || 0), height: Number(node.height || 0) },
          pivot: { x: Number(node.pivotX || 0), y: Number(node.pivotY || 0) },
          anchor: { x: Number(node.anchorX || 0), y: Number(node.anchorY || 0) },
          scale: { x: Number(node.scaleX == null ? 1 : node.scaleX), y: Number(node.scaleY == null ? 1 : node.scaleY) },
          alpha: Number(node.alpha == null ? 1 : node.alpha), zOrder: Number(node.zOrder || 0),
          text: typeof node.text === 'string' ? node.text : '',
          html: typeof node.innerHTML === 'string' ? node.innerHTML : '',
          skin: typeof node.skin === 'string' ? node.skin : '',
          gray: !!node.gray, disabled: !!node.disabled,
          mouseEnabled: node.mouseEnabled !== false, mouseThrough: !!node.mouseThrough,
          selected: node.selected === undefined ? null : !!node.selected,
          isAnim: typeof node.is_anim === 'boolean' ? node.is_anim : null,
          frameToken: name === nextView ? stageResult.meta.frameToken : null,
          hitTestCenter, dataIdentity: identityOf(node),
          scroll: {
            v: vBar ? { value: Number(vBar.value || 0), min: Number(vBar.min || 0), max: Number(vBar.max || 0) } : null,
            h: hBar ? { value: Number(hBar.value || 0), min: Number(hBar.min || 0), max: Number(hBar.max || 0) } : null,
          },
          scrollRect: node.scrollRect ? { x: Number(node.scrollRect.x || 0), y: Number(node.scrollRect.y || 0), width: Number(node.scrollRect.width || 0), height: Number(node.scrollRect.height || 0) } : null,
        });
        childrenOf(node).forEach((child, index) => walk(child, visible, path, indexPath.concat(index), depth + 1, nextView));
      };
      walk(stage, true, '', [0], 0, null);
    }
    return { capturedAt: new Date().toISOString(), loaded, managed, stage: stageResult, warnings };
  }, { maxDepth, maxNodes });
}

async function collectRuntimeSnapshot(page, options = {}) {
  return normalizeRuntimeSources(await collectRuntimeSources(page, options), options);
}

function matchNode(node, selector = {}) {
  if (selector.source && node.source !== selector.source) return false;
  if (selector.view && node.view !== selector.view) return false;
  if (selector.name && node.name !== selector.name) return false;
  if (selector.text && node.text.trim() !== String(selector.text).trim()) return false;
  if (selector.skinIncludes && !node.skin.includes(selector.skinIncludes)) return false;
  if (selector.path && node.path !== selector.path) return false;
  if (selector.dataIdentity && !identitySubsetMatches(node.state && node.state.dataIdentity, selector.dataIdentity)) return false;
  if (selector.visible !== false && !node.visible) return false;
  return true;
}

function findNodes(snapshot, selector = {}) {
  return (snapshot && snapshot.nodes || []).filter(node => matchNode(node, selector));
}

function findExactNode(snapshot, selector = {}) {
  const matches = findNodes(snapshot, selector);
  const expectedCount = selector.expectedCount == null ? 1 : Number(selector.expectedCount);
  if (matches.length !== expectedCount) {
    throw new Error(`RUNTIME_NODE_IDENTITY_MISMATCH expected=${expectedCount} actual=${matches.length} selector=${JSON.stringify(selector)}`);
  }
  const index = selector.index == null ? 0 : Number(selector.index);
  if (!matches[index]) throw new Error(`RUNTIME_NODE_INDEX_MISSING index=${index}`);
  return matches[index];
}

module.exports = {
  NODE_SCHEMA,
  normalizeRect,
  identitySubsetMatches,
  normalizedNode,
  flattenManagedTree,
  normalizeRuntimeSources,
  collectRuntimeSources,
  collectRuntimeSnapshot,
  matchNode,
  findNodes,
  findExactNode,
};
