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

function normalizeBinding(binding, fallbackView = null) {
  if (!binding || typeof binding !== 'object') return null;
  const field = String(binding.field || '');
  if (!field) return null;
  return {
    ownerView: String(binding.ownerView || fallbackView || '') || null,
    field,
    runtimeName: String(binding.runtimeName || ''),
    relation: String(binding.relation || 'direct-reference'),
    source: String(binding.source || 'view-instance-field'),
    instanceSource: String(binding.instanceSource || ''),
    instanceKey: String(binding.instanceKey || ''),
  };
}

function stagePathArray(value) {
  if (!Array.isArray(value)) return null;
  const result = value.map(Number);
  return result.every(Number.isInteger) ? result : null;
}

function isPathPrefix(prefix, value) {
  return Array.isArray(prefix) && Array.isArray(value) && prefix.length <= value.length
    && prefix.every((part, index) => part === value[index]);
}

function reconcileStageOwnership(stageRows, loaded = [], managedViews = []) {
  const owners = [];
  const addOwner = (value, source) => {
    if (!value) return;
    const meta = value.meta || value;
    const rootStagePath = stagePathArray(meta.stagePath);
    const view = String(meta.name || value.name || '');
    if (!rootStagePath || !rootStagePath.length || !view) return;
    const existing = owners.find(owner => owner.view === view
      && owner.rootStagePath.length === rootStagePath.length
      && owner.rootStagePath.every((part, index) => part === rootStagePath[index]));
    const evidence = {
      source,
      name: view,
      rawName: String(meta.rawName || ''),
      layoutFile: String(meta.layoutFile || ''),
      registrySource: String(meta.source || ''),
      rootStagePath,
    };
    if (existing) {
      if (!existing.evidence.some(item => JSON.stringify(item) === JSON.stringify(evidence))) existing.evidence.push(evidence);
      return;
    }
    owners.push({ view, rootStagePath, evidence: [evidence] });
  };
  loaded.forEach(value => addOwner(value, 'loaded-view-stage-path'));
  managedViews.forEach(value => addOwner(value, 'managed-view-stage-path'));
  owners.sort((left, right) => right.rootStagePath.length - left.rootStagePath.length);

  return (stageRows || []).map(row => {
    const stagePath = stagePathArray(row && row.indexPath);
    const relativePath = stagePath && stagePath[0] === 0 ? stagePath.slice(1) : stagePath;
    const owner = relativePath && owners.find(candidate => isPathPrefix(candidate.rootStagePath, relativePath));
    const view = row && row.view || owner && owner.view || null;
    const ownerIdentity = row && row.ownerIdentity || (owner ? {
      view: owner.view,
      rootStagePath: owner.rootStagePath,
      isRoot: owner.rootStagePath.length === relativePath.length,
      evidence: owner.evidence,
    } : null);
    return { ...row, view, ownerIdentity };
  });
}

function normalizedNode(raw, context = {}) {
  const name = String(raw && raw.name || '');
  const type = String(raw && raw.type || raw && raw.constructorName || '');
  const path = String(context.path || raw && raw.path || name || type || 'node');
  const view = context.view || raw && raw.view || null;
  const bindings = (Array.isArray(raw && raw.bindings) ? raw.bindings : [])
    .map(binding => normalizeBinding(binding, view)).filter(Boolean);
  const rawOwner = raw && raw.ownerIdentity || context.ownerIdentity || null;
  const owner = rawOwner ? {
    view: String(rawOwner.view || view || '') || null,
    rootStagePath: stagePathArray(rawOwner.rootStagePath),
    isRoot: !!rawOwner.isRoot,
    evidence: Array.isArray(rawOwner.evidence) ? rawOwner.evidence : [],
    instances: Array.isArray(rawOwner.instances) ? rawOwner.instances : [],
  } : null;
  return {
    schema: NODE_SCHEMA,
    source: context.source || raw && raw.source || 'unknown',
    view,
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
    identity: {
      ownerView: owner && owner.view || view,
      runtimeName: name,
      owner,
      bindings,
    },
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
  const rawStageRows = Array.isArray(raw && raw.stage && raw.stage.nodes) ? raw.stage.nodes : [];
  const stageRows = reconcileStageOwnership(rawStageRows, loaded, managedViews);
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
          rawName: String(view.rawName || ''),
          source: String(view.source || ''),
          baseFile: String(view.baseFile || ''),
          layoutFile: String(view.layoutFile || ''),
          stagePath: Array.isArray(view.stagePath) ? view.stagePath.map(Number) : null,
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

      const stageNodeAt = stagePath => {
        let current = stage;
        if (!Array.isArray(stagePath)) return null;
        for (const index of stagePath) {
          current = childrenOf(current)[Number(index)];
          if (!current) return null;
        }
        return current;
      };
      const ownerByPath = new Map();
      const addOwner = (meta, evidenceSource) => {
        if (!meta || !Array.isArray(meta.stagePath) || !meta.stagePath.length || !meta.name) return;
        const rootStagePath = meta.stagePath.map(Number);
        const key = rootStagePath.join('.');
        const evidence = {
          source: evidenceSource,
          name: String(meta.name || ''),
          rawName: String(meta.rawName || ''),
          layoutFile: String(meta.layoutFile || ''),
          registrySource: String(meta.source || ''),
          rootStagePath,
        };
        const existing = ownerByPath.get(key);
        if (existing) {
          if (!existing.evidence.some(item => JSON.stringify(item) === JSON.stringify(evidence))) existing.evidence.push(evidence);
          return;
        }
        ownerByPath.set(key, {
          view: String(meta.name), rootStagePath, root: stageNodeAt(rootStagePath), evidence: [evidence], instances: [],
        });
      };
      loaded.forEach(meta => addOwner(meta, 'loaded-view-stage-path'));
      for (const exported of managed.views || []) addOwner(exported && exported.meta, 'managed-view-stage-path');

      const viewInstances = [];
      const addViewInstance = (view, instanceSource, instanceKey) => {
        if (!view || !view.display_obj || viewInstances.some(item => item.view === view)) return;
        viewInstances.push({ view, root: view.display_obj, instanceSource, instanceKey: String(instanceKey || '') });
      };
      try {
        const Manager = window.ViewManager || window['ViewManager'];
        const manager = Manager && Manager.GetInstance && Manager.GetInstance();
        const dictionary = manager && (manager.view_dic || manager._view_dic) || {};
        for (const key of Object.keys(dictionary)) addViewInstance(dictionary[key], 'ViewManager', key);
      } catch (error) { warnings.push(`view-instance-manager: ${String(error)}`); }
      try {
        const registry = window.__sxPageSnapshotRegistry__ || {};
        for (const key of Object.keys(registry)) {
          const item = registry[key];
          addViewInstance(item && item.view, 'RuntimeRegistry', key);
        }
      } catch (error) { warnings.push(`view-instance-registry: ${String(error)}`); }

      const bindingsByNode = new Map();
      for (const owner of ownerByPath.values()) {
        if (!owner.root) continue;
        const descendants = new Set();
        const collectDescendants = value => {
          if (!value || descendants.has(value) || descendants.size >= maxNodes) return;
          descendants.add(value);
          childrenOf(value).forEach(collectDescendants);
        };
        collectDescendants(owner.root);
        const instances = viewInstances.filter(item => item.root === owner.root);
        owner.instances = instances.map(item => ({ source: item.instanceSource, key: item.instanceKey }));
        for (const instance of instances) {
          let fields = [];
          try { fields = Object.keys(instance.view); } catch (_) {}
          for (const field of fields) {
            let value = null;
            try { value = instance.view[field]; } catch (_) { continue; }
            if (!descendants.has(value)) continue;
            if (!bindingsByNode.has(value)) bindingsByNode.set(value, []);
            bindingsByNode.get(value).push({
              ownerView: owner.view,
              field: String(field),
              runtimeName: String(value && value.name || ''),
              relation: 'direct-reference',
              source: 'view-instance-field',
              instanceSource: instance.instanceSource,
              instanceKey: instance.instanceKey,
            });
          }
        }
      }

      const publicOwner = (owner, isRoot) => owner ? ({
        view: owner.view,
        rootStagePath: owner.rootStagePath,
        isRoot: !!isRoot,
        evidence: owner.evidence,
        instances: owner.instances,
      }) : null;
      const walk = (node, parentVisible, parentPath, indexPath, depth, activeOwner) => {
        if (!node || depth > maxDepth || stageResult.nodes.length >= maxNodes) return;
        const name = String(node.name || '');
        const type = String(node.constructor && node.constructor.name || '');
        const path = `${parentPath ? `${parentPath}/` : ''}${name || type || 'node'}[${indexPath[indexPath.length - 1] || 0}]`;
        const visible = parentVisible && node.visible !== false && Number(node.alpha == null ? 1 : node.alpha) !== 0;
        const relativePath = indexPath[0] === 0 ? indexPath.slice(1) : indexPath;
        const exactOwner = ownerByPath.get(relativePath.join('.')) || null;
        const heuristicOwner = !exactOwner && /View$/.test(name)
          && (!activeOwner || activeOwner.view !== name) ? {
          view: name, rootStagePath: relativePath, evidence: [{ source: 'stage-name-heuristic', name, rootStagePath: relativePath }], instances: [],
        } : null;
        const nextOwner = exactOwner || activeOwner || heuristicOwner;
        const bounds = boundsOf(node);
        let hitTestCenter = null;
        if (bounds && bounds.width > 0 && bounds.height > 0 && typeof node.hitTestPoint === 'function') {
          try { hitTestCenter = !!node.hitTestPoint(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2); } catch (_) {}
        }
        const vBar = node.vScrollBar || (node._scrollBar && node._scrollBar.isVertical ? node._scrollBar : null);
        const hBar = node.hScrollBar || (node._scrollBar && !node._scrollBar.isVertical ? node._scrollBar : null);
        stageResult.nodes.push({
          name, type, view: nextOwner && nextOwner.view || null, path, parentPath: parentPath || null, indexPath, depth,
          ownerIdentity: publicOwner(nextOwner, !!exactOwner || !!heuristicOwner),
          bindings: bindingsByNode.get(node) || [],
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
          frameToken: exactOwner || heuristicOwner ? stageResult.meta.frameToken : null,
          hitTestCenter, dataIdentity: identityOf(node),
          scroll: {
            v: vBar ? { value: Number(vBar.value || 0), min: Number(vBar.min || 0), max: Number(vBar.max || 0) } : null,
            h: hBar ? { value: Number(hBar.value || 0), min: Number(hBar.min || 0), max: Number(hBar.max || 0) } : null,
          },
          scrollRect: node.scrollRect ? { x: Number(node.scrollRect.x || 0), y: Number(node.scrollRect.y || 0), width: Number(node.scrollRect.width || 0), height: Number(node.scrollRect.height || 0) } : null,
        });
        childrenOf(node).forEach((child, index) => walk(child, visible, path, indexPath.concat(index), depth + 1, nextOwner));
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
  const bindings = node && node.identity && Array.isArray(node.identity.bindings) ? node.identity.bindings : [];
  if (selector.source && node.source !== selector.source) return false;
  if (selector.view && node.view !== selector.view) return false;
  if (selector.ownerView && (!node.identity || node.identity.ownerView !== selector.ownerView)) return false;
  if (selector.name && node.name !== selector.name) return false;
  if (selector.runtimeName && (!node.identity || node.identity.runtimeName !== selector.runtimeName)) return false;
  if (selector.boundField && !bindings.some(binding => binding.field === selector.boundField
    && (!selector.ownerView || binding.ownerView === selector.ownerView))) return false;
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
  normalizeBinding,
  reconcileStageOwnership,
  normalizedNode,
  flattenManagedTree,
  normalizeRuntimeSources,
  collectRuntimeSources,
  collectRuntimeSnapshot,
  matchNode,
  findNodes,
  findExactNode,
};
