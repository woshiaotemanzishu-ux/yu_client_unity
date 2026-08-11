'use strict';

const { RUNTIME_NODE_SCHEMA_VERSION } = require('./version.cjs');
const { normalizeRuntimeOverlay } = require('./runtime-overlay.cjs');

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

function normalizeMask(mask) {
  if (!mask) return null;
  return {
    name: String(mask.name || ''),
    type: String(mask.type || mask.constructorName || ''),
    visible: mask.visible !== false,
    alpha: finite(mask.alpha, 1),
    bounds: normalizeRect(mask.bounds),
    hitTestCenter: mask.hitTestCenter == null ? null : !!mask.hitTestCenter,
  };
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

function normalizedInstances(value) {
  if (!Array.isArray(value)) return [];
  const seen = new Set();
  const result = [];
  for (const item of value) {
    const source = String(item && item.source || '');
    const key = String(item && item.key || '');
    if (!source || !key || seen.has(`${source}\u0000${key}`)) continue;
    seen.add(`${source}\u0000${key}`);
    result.push({ source, key });
  }
  return result;
}

function isPathPrefix(prefix, value) {
  return Array.isArray(prefix) && Array.isArray(value) && prefix.length <= value.length
    && prefix.every((part, index) => part === value[index]);
}

function samePath(left, right) {
  return Array.isArray(left) && Array.isArray(right) && left.length === right.length
    && left.every((part, index) => Number(part) === Number(right[index]));
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
    const instances = normalizedInstances(meta.instances || value.ownerIdentity && value.ownerIdentity.instances);
    if (existing) {
      if (!existing.evidence.some(item => JSON.stringify(item) === JSON.stringify(evidence))) existing.evidence.push(evidence);
      existing.instances = normalizedInstances([...(existing.instances || []), ...instances]);
      return;
    }
    owners.push({ view, rootStagePath, evidence: [evidence], instances });
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
      instances: owner.instances,
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
    instances: normalizedInstances(rawOwner.instances),
  } : null;
  return {
    schema: NODE_SCHEMA,
    source: context.source || raw && raw.source || 'unknown',
    view,
    path,
    indexPath: Array.isArray(raw && raw.indexPath) ? raw.indexPath.map(Number) : null,
    parentPath: raw && raw.parentPath || context.parentPath || null,
    depth: finite(raw && raw.depth, context.depth || 0),
    childIndex: raw && raw.childIndex != null ? finite(raw.childIndex)
      : Array.isArray(raw && raw.indexPath) && raw.indexPath.length ? finite(raw.indexPath[raw.indexPath.length - 1]) : null,
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
    effectiveAlpha: finite(raw && raw.effectiveAlpha, finite(raw && raw.alpha, 1)),
    zOrder: finite(raw && raw.zOrder),
    text: typeof (raw && raw.text) === 'string' ? raw.text : '',
    html: typeof (raw && raw.html) === 'string' ? raw.html : '',
    skin: typeof (raw && raw.skin) === 'string' ? raw.skin : '',
    identity: {
      ownerView: owner && owner.view || view,
      runtimeName: name,
      runtimeClass: String(raw && raw.runtimeClass || type),
      owner,
      bindings,
      systemOverlay: raw && raw.systemOverlay || null,
    },
    interaction: {
      mouseEnabled: raw && raw.mouseEnabled !== false,
      mouseThrough: !!(raw && raw.mouseThrough),
      hitTestPrior: !!(raw && raw.hitTestPrior),
      mouseState: raw && raw.mouseState == null ? null : finite(raw.mouseState),
      disabled: !!(raw && raw.disabled),
      hitTestCenter: raw && raw.hitTestCenter == null ? null : !!raw.hitTestCenter,
      hitArea: raw && raw.hitArea || null,
      eventListeners: Array.isArray(raw && raw.eventListeners) ? raw.eventListeners : [],
    },
    state: {
      selected: raw && raw.selected == null ? null : !!raw.selected,
      gray: !!(raw && raw.gray),
      isAnim: raw && typeof raw.isAnim === 'boolean' ? raw.isAnim : null,
      frameToken: raw && raw.frameToken == null ? null : finite(raw.frameToken),
      dataIdentity: raw && raw.dataIdentity || null,
      scroll: raw && raw.scroll || null,
      scrollRect: normalizeRect(raw && raw.scrollRect),
      mask: normalizeMask(raw && raw.mask),
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
    const meta = options.ownerMeta || {};
    const rootStagePath = stagePathArray(meta.stagePath);
    const enriched = depth === 0 ? {
      ...raw,
      ownerIdentity: {
        view: viewName,
        rootStagePath,
        isRoot: true,
        evidence: [{
          source: 'managed-view-stage-path',
          name: viewName,
          rawName: String(meta.rawName || ''),
          layoutFile: String(meta.layoutFile || ''),
          registrySource: String(meta.source || ''),
          rootStagePath,
        }],
        instances: normalizedInstances(meta.instances),
      },
      dataIdentity: {
        ...(raw && raw.dataIdentity || {}),
        lifecycle: {
          source: String(meta.source || ''),
          loaded: meta.loaded !== false,
          open: meta.open !== false,
          visible: meta.visible !== false,
          stagePath: rootStagePath,
        },
      },
    } : raw;
    nodes.push(normalizedNode(enriched, {
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
  const runtimeOverlays = (Array.isArray(raw && raw.stage && raw.stage.overlays) ? raw.stage.overlays : [])
    .map(normalizeRuntimeOverlay).filter(Boolean);
  const nodes = [];

  const inferredInstances = view => {
    const rootStagePath = stagePathArray(view && view.stagePath);
    const owner = stageRows.map(row => row && row.ownerIdentity).find(identity => identity
      && identity.isRoot && identity.view === String(view && view.name || '')
      && samePath(identity.rootStagePath, rootStagePath));
    return normalizedInstances(view && view.instances || owner && owner.instances);
  };

  for (const view of loaded) {
    const viewName = String(view.name || '');
    const rootStagePath = stagePathArray(view.stagePath);
    nodes.push(normalizedNode({
      name: viewName,
      type: 'LoadedView',
      visible: view.visible,
      displayedInStage: !!(rootStagePath && rootStagePath.length && view.visible !== false),
      ownerIdentity: {
        view: viewName,
        rootStagePath,
        isRoot: true,
        evidence: [{
          source: 'loaded-view-stage-path',
          name: viewName,
          rawName: String(view.rawName || ''),
          layoutFile: String(view.layoutFile || ''),
          registrySource: String(view.source || ''),
          rootStagePath,
        }],
        instances: inferredInstances(view),
      },
      dataIdentity: {
        lifecycle: {
          source: String(view.source || ''),
          loaded: view.loaded !== false,
          open: view.open !== false,
          visible: view.visible !== false,
          stagePath: rootStagePath,
        },
      },
    }, { source: 'loaded-view', view: viewName, path: `loaded/${viewName}` }));
  }

  for (const view of managedViews) {
    const viewName = String(view && view.meta && view.meta.name || view && view.name || '');
    const loadedMeta = loaded.find(item => String(item && item.name || '') === viewName) || {};
    const ownerMeta = {
      ...loadedMeta,
      ...(view && view.meta || {}),
      instances: normalizedInstances(view && view.meta && view.meta.instances).length
        ? view.meta.instances : inferredInstances(loadedMeta),
    };
    nodes.push(...flattenManagedTree(view && view.nodeTree, viewName, { ...options, ownerMeta }));
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
    if ((node.source === 'loaded-view' || node.source === 'managed-view')
      && node.state && node.state.dataIdentity && node.state.dataIdentity.lifecycle
      && node.state.dataIdentity.lifecycle.open === false) continue;
    if (node.source === 'loaded-view' || node.name === node.view) {
      seen.add(node.view);
      visibleViews.push(node.view);
    }
  }
  for (const overlay of runtimeOverlays) {
    const current = overlay.kind === 'managed-view-background' && overlay.currentView;
    if (!current || !current.name || !current.visible || !current.displayed || !current.open || seen.has(current.name)) continue;
    seen.add(current.name);
    visibleViews.push(current.name);
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
      runtimeOverlays: runtimeOverlays.length,
    },
    visibleViews,
    runtimeOverlays,
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
    const stageResult = { meta: null, nodes: [], overlays: [] };
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
      const stagePathOf = node => {
        const result = [];
        let current = node;
        while (current && current !== stage) {
          const parent = current.parent;
          if (!parent) return null;
          const index = childrenOf(parent).indexOf(current);
          if (index < 0) return null;
          result.unshift(index);
          current = parent;
        }
        return current === stage ? result : null;
      };
      const fullStagePathOf = node => {
        const relative = stagePathOf(node);
        return relative ? [0, ...relative] : null;
      };
      const stageDisplayPathOf = node => {
        const indices = fullStagePathOf(node);
        if (!indices) return '';
        let current = stage;
        return indices.map((index, depth) => {
          if (depth > 0) current = childrenOf(current)[Number(index)];
          return `${String(current && (current.name || current.constructor && current.constructor.name) || 'node')}[${index}]`;
        }).join('/');
      };
      const qualifiedName = value => {
        try { return window.GetQualifiedClassName ? String(window.GetQualifiedClassName(value) || '') : ''; }
        catch (_) { return ''; }
      };
      const eventListenersOf = node => {
        const result = [];
        const events = node && node._events;
        if (!events || typeof events !== 'object') return result;
        const handlersOf = value => Array.isArray(value) ? value.filter(Boolean) : value ? [value] : [];
        for (const type of Object.keys(events)) {
          const handlers = handlersOf(events[type]);
          result.push({
            type: String(type),
            count: handlers.length,
            handlers: handlers.slice(0, 8).map(handler => ({
              callerClass: qualifiedName(handler && handler.caller),
              method: String(handler && handler.method && handler.method.name || ''),
              once: !!(handler && handler.once),
            })),
          });
        }
        return result;
      };
      const hitAreaOf = node => {
        const area = node && node._style && node._style.hitArea;
        if (!area) return null;
        const source = area._hit || area;
        return {
          type: String(source && source.constructor && source.constructor.name || area.constructor && area.constructor.name || ''),
          x: Number(source && source.x || 0), y: Number(source && source.y || 0),
          width: Number(source && source.width || 0), height: Number(source && source.height || 0),
        };
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
        const runtimeQualifiedName = qualifiedName(view);
        viewInstances.push({
          view, root: view.display_obj, instanceSource, instanceKey: String(instanceKey || ''),
          names: [instanceKey, runtimeQualifiedName, view.layout_file, view.layoutFile, view.constructor && view.constructor.name]
            .filter(Boolean).map(String),
        });
      };
      let viewManager = null;
      try {
        const Manager = window.ViewManager || window['ViewManager'];
        viewManager = Manager && Manager.GetInstance && Manager.GetInstance();
        const dictionary = viewManager && (viewManager.view_dic || viewManager._view_dic) || {};
        for (const key of Object.keys(dictionary)) addViewInstance(dictionary[key], 'ViewManager', key);
      } catch (error) { warnings.push(`view-instance-manager: ${String(error)}`); }
      try {
        const registry = window.__sxPageSnapshotRegistry__ || {};
        for (const key of Object.keys(registry)) {
          const item = registry[key];
          addViewInstance(item && item.view, 'RuntimeRegistry', key);
        }
      } catch (error) { warnings.push(`view-instance-registry: ${String(error)}`); }

      const viewDescription = (view, instanceSource = '', instanceKey = '') => {
        if (!view) return null;
        const root = view.display_obj || null;
        const name = qualifiedName(view) || String(view.layout_file || view.layoutFile || view.constructor && view.constructor.name || '');
        if (!name) return null;
        let open = true;
        try { if (typeof view.HasOpen === 'function') open = !!view.HasOpen(); } catch (_) {}
        return {
          name,
          rawName: qualifiedName(view),
          layoutFile: String(view.layout_file || view.layoutFile || ''),
          constructorName: String(view.constructor && view.constructor.name || ''),
          hashCode: view.hashCode == null ? null : String(view.hashCode),
          stagePath: fullStagePathOf(root),
          visible: !!(root && root.visible !== false),
          displayed: !!(root && root.displayedInStage !== false && fullStagePathOf(root)),
          open,
          useBackground: view.use_background == null ? null : !!view.use_background,
          clickBackgroundToClose: view.click_bg_toClose == null ? null : !!view.click_bg_toClose,
          backgroundTouchEnabled: view.backgroup_touchEnable == null ? null : !!view.backgroup_touchEnable,
          instanceSource: String(instanceSource || ''),
          instanceKey: String(instanceKey || ''),
        };
      };
      const instanceIdentity = view => {
        const instance = viewInstances.find(item => item.view === view);
        return instance ? { source: instance.instanceSource, key: instance.instanceKey } : { source: 'ViewManagerRuntime', key: String(view && view.hashCode || '') };
      };
      const layerDescription = node => {
        const Layer = window.LayerManager || window['LayerManager'];
        const layerManager = Layer && Layer.GetInstance && Layer.GetInstance();
        const layers = layerManager && layerManager.ui_layer_list || [];
        let current = node;
        while (current) {
          const index = layers.indexOf(current);
          if (index >= 0) return { name: String(current.name || ''), index, stagePath: fullStagePathOf(current) };
          current = current.parent;
        }
        return null;
      };
      const overlayByNode = new Map();
      const registerOverlay = (node, value) => {
        if (!node || !fullStagePathOf(node)) return;
        const overlay = {
          ...value,
          nodeStagePath: fullStagePathOf(node),
          nodePath: stageDisplayPathOf(node),
          active: node.visible !== false && node.displayedInStage !== false,
          visible: node.visible !== false,
          displayed: node.displayedInStage !== false,
          interactive: node.mouseEnabled !== false && !node.mouseThrough,
          layer: layerDescription(node),
          node: {
            runtimeName: String(node.name || ''),
            runtimeClass: qualifiedName(node),
            constructorName: String(node.constructor && node.constructor.name || ''),
            childIndex: node.parent ? childrenOf(node.parent).indexOf(node) : null,
            zOrder: Number(node.zOrder || 0),
            visible: node.visible !== false,
            alpha: Number(node.alpha == null ? 1 : node.alpha),
            mouseEnabled: node.mouseEnabled !== false,
            mouseThrough: !!node.mouseThrough,
            hitTestPrior: !!node.hitTestPrior,
            mouseState: Number(node._mouseState == null ? 0 : node._mouseState),
            bounds: boundsOf(node),
            hitArea: hitAreaOf(node),
            eventListeners: eventListenersOf(node),
          },
        };
        stageResult.overlays.push(overlay);
        overlayByNode.set(node, overlay);
      };
      if (viewManager) {
        try {
          const background = viewManager.GetBackGround ? viewManager.GetBackGround() : viewManager._background;
          if (background && fullStagePathOf(background)) {
            const currentView = background.curr_view || background['curr_view'] || null;
            if (currentView) {
              const identity = instanceIdentity(currentView);
              addViewInstance(currentView, identity.source, identity.key);
              const description = viewDescription(currentView, identity.source, identity.key);
              if (description && Array.isArray(description.stagePath)) {
                addOwner({
                  name: description.name,
                  rawName: description.rawName,
                  layoutFile: description.layoutFile,
                  source: 'ViewManager.GetBackGround.curr_view',
                  stagePath: description.stagePath.slice(1),
                }, 'runtime-overlay-current-view');
              }
            }
            const candidates = [];
            const dictionary = viewManager.has_backgroup_view_dic || viewManager._has_backgroup_view_dic || {};
            for (const key of Object.keys(dictionary)) {
              const candidate = dictionary[key];
              const identity = instanceIdentity(candidate);
              const description = viewDescription(candidate, identity.source, identity.key || key);
              if (description) candidates.push(description);
            }
            const currentIdentity = instanceIdentity(currentView);
            registerOverlay(background, {
              id: 'view-manager-background',
              kind: 'managed-view-background',
              authority: 'ViewManager.GetBackGround',
              manager: 'ViewManager',
              managerField: '_background',
              currentView: viewDescription(currentView, currentIdentity.source, currentIdentity.key),
              candidates,
              evidence: [{ source: 'ViewManager._background.curr_view', relation: 'shared-background-current-view' }],
            });
          }
        } catch (error) { warnings.push(`runtime-overlay-background: ${String(error)}`); }
        try {
          const loading = viewManager.waitfor_openView_loading || viewManager._waitfor_openView_loading;
          const gateNode = loading && (loading.display_obj || loading);
          if (gateNode && fullStagePathOf(gateNode) && gateNode.visible !== false && gateNode.displayedInStage !== false) {
            const pending = loading.curr_loading_view_dic || {};
            registerOverlay(gateNode, {
              id: 'waitfor-open-view-loading',
              kind: 'global-input-gate',
              authority: 'ViewManager.waitfor_openView_loading.display_obj',
              manager: 'ViewManager',
              managerField: 'waitfor_openView_loading',
              gate: {
                pendingKeys: Object.keys(pending),
                ready: Object.keys(pending).length === 0 || gateNode.visible === false || gateNode.displayedInStage === false,
                visible: gateNode.visible !== false,
                releaseCondition: 'curr_loading_view_dic empty and display_obj hidden',
              },
              evidence: [{ source: 'WaitforOpenViewLoading.curr_loading_view_dic', relation: 'pending-resource-view-loads' }],
            });
          }
        } catch (error) { warnings.push(`runtime-overlay-loading-gate: ${String(error)}`); }
      }

      try {
        const Layer = window.LayerManager || window['LayerManager'];
        const layerManager = Layer && Layer.GetInstance && Layer.GetInstance();
        const layers = layerManager && layerManager.ui_layer_list || [];
        for (const layer of layers) {
          for (const child of childrenOf(layer)) {
            if (overlayByNode.has(child) || child.visible === false || child.displayedInStage === false
              || child.mouseEnabled === false || Number(child._mouseState == null ? 0 : child._mouseState) <= 1) continue;
            const bounds = boundsOf(child);
            if (!bounds || bounds.width < Number(stage.width || 0) * 0.8 || bounds.height < Number(stage.height || 0) * 0.8) continue;
            const relativePath = stagePathOf(child);
            const knownOwner = relativePath && ownerByPath.has(relativePath.join('.'))
              || viewInstances.some(instance => instance.root === child);
            if (knownOwner) continue;
            const candidates = viewInstances.filter(instance => instance.root && instance.root.parent === layer)
              .map(instance => viewDescription(instance.view, instance.instanceSource, instance.instanceKey)).filter(Boolean);
            registerOverlay(child, {
              id: `unknown-interactive-overlay:${(fullStagePathOf(child) || []).join('.')}`,
              kind: 'unknown-interactive-overlay',
              authority: 'Laya.stage.hit-policy',
              manager: '',
              managerField: '',
              currentView: null,
              candidates,
              evidence: [{ source: 'LayerManager.ui_layer_list', relation: 'unowned-fullscreen-interactive-child' }],
            });
          }
        }
      } catch (error) { warnings.push(`runtime-overlay-unknown-scan: ${String(error)}`); }

      for (const meta of loaded) {
        const root = stageNodeAt(meta.stagePath);
        let instances = root ? viewInstances.filter(item => item.root === root) : [];
        if (!instances.length) {
          const names = new Set([meta.name, meta.rawName, meta.layoutFile].filter(Boolean).map(String));
          instances = viewInstances.filter(item => item.names.some(name => names.has(name)));
        }
        meta.instances = instances.map(item => ({ source: item.instanceSource, key: item.instanceKey }));
      }
      for (const exported of managed.views || []) {
        const meta = exported && exported.meta;
        if (!meta) continue;
        const loadedMeta = loaded.find(item => item.name === meta.name
          && JSON.stringify(item.stagePath || []) === JSON.stringify(meta.stagePath || []))
          || loaded.find(item => item.name === meta.name);
        meta.instances = loadedMeta && loadedMeta.instances || [];
      }

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
      const walk = (node, parentVisible, parentAlpha, parentPath, indexPath, depth, activeOwner) => {
        if (!node || depth > maxDepth || stageResult.nodes.length >= maxNodes) return;
        const name = String(node.name || '');
        const type = String(node.constructor && node.constructor.name || '');
        const path = `${parentPath ? `${parentPath}/` : ''}${name || type || 'node'}[${indexPath[indexPath.length - 1] || 0}]`;
        const localAlpha = Number(node.alpha == null ? 1 : node.alpha);
        const effectiveAlpha = parentAlpha * localAlpha;
        const visible = parentVisible && node.visible !== false && effectiveAlpha !== 0;
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
        const mask = node.mask || node._cacheStyle && node._cacheStyle.mask || null;
        let maskInfo = null;
        if (mask) {
          const maskBounds = boundsOf(mask);
          let maskHitTestCenter = null;
          if (maskBounds && maskBounds.width > 0 && maskBounds.height > 0 && typeof mask.hitTestPoint === 'function') {
            try { maskHitTestCenter = !!mask.hitTestPoint(maskBounds.x + maskBounds.width / 2, maskBounds.y + maskBounds.height / 2); } catch (_) {}
          }
          maskInfo = {
            name: String(mask.name || ''), type: String(mask.constructor && mask.constructor.name || ''),
            visible: mask.visible !== false, alpha: Number(mask.alpha == null ? 1 : mask.alpha),
            bounds: maskBounds, hitTestCenter: maskHitTestCenter,
          };
        }
        stageResult.nodes.push({
          name, type, view: nextOwner && nextOwner.view || null, path, parentPath: parentPath || null, indexPath, depth,
          childIndex: indexPath[indexPath.length - 1],
          ownerIdentity: publicOwner(nextOwner, !!exactOwner || !!heuristicOwner),
          bindings: bindingsByNode.get(node) || [],
          systemOverlay: overlayByNode.get(node) || null,
          visible, displayedInStage: node.displayedInStage !== false, bounds,
          local: { x: Number(node.x || 0), y: Number(node.y || 0), width: Number(node.width || 0), height: Number(node.height || 0) },
          pivot: { x: Number(node.pivotX || 0), y: Number(node.pivotY || 0) },
          anchor: { x: Number(node.anchorX || 0), y: Number(node.anchorY || 0) },
          scale: { x: Number(node.scaleX == null ? 1 : node.scaleX), y: Number(node.scaleY == null ? 1 : node.scaleY) },
          alpha: localAlpha, effectiveAlpha, zOrder: Number(node.zOrder || 0),
          text: typeof node.text === 'string' ? node.text : '',
          html: typeof node.innerHTML === 'string' ? node.innerHTML : '',
          skin: typeof node.skin === 'string' ? node.skin : '',
          gray: !!node.gray, disabled: !!node.disabled,
          mouseEnabled: node.mouseEnabled !== false, mouseThrough: !!node.mouseThrough,
          hitTestPrior: !!node.hitTestPrior, mouseState: Number(node._mouseState == null ? 0 : node._mouseState),
          hitArea: overlayByNode.has(node) ? hitAreaOf(node) : null,
          eventListeners: overlayByNode.has(node) ? eventListenersOf(node) : [],
          selected: node.selected === undefined ? null : !!node.selected,
          isAnim: typeof node.is_anim === 'boolean' ? node.is_anim : null,
          frameToken: exactOwner || heuristicOwner ? stageResult.meta.frameToken : null,
          hitTestCenter, dataIdentity: identityOf(node),
          scroll: {
            v: vBar ? { value: Number(vBar.value || 0), min: Number(vBar.min || 0), max: Number(vBar.max || 0) } : null,
            h: hBar ? { value: Number(hBar.value || 0), min: Number(hBar.min || 0), max: Number(hBar.max || 0) } : null,
          },
          scrollRect: node.scrollRect ? { x: Number(node.scrollRect.x || 0), y: Number(node.scrollRect.y || 0), width: Number(node.scrollRect.width || 0), height: Number(node.scrollRect.height || 0) } : null,
          mask: maskInfo,
        });
        childrenOf(node).forEach((child, index) => walk(child, visible, effectiveAlpha, path, indexPath.concat(index), depth + 1, nextOwner));
      };
      walk(stage, true, 1, '', [0], 0, null);
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
  normalizeMask,
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
