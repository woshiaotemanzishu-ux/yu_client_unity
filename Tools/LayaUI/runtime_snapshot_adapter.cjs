'use strict';

const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');

function sha256(buffer) {
  return crypto.createHash('sha256').update(buffer).digest('hex');
}

function finite(value, fallback = 0) {
  const number = Number(value);
  return Number.isFinite(number) ? number : fallback;
}

function adaptRuntimeSnapshot(snapshot, options = {}) {
  const rootView = options.rootView || 'BaseWindowSkin';
  const rootName = options.rootName;
  const viewName = options.viewName || rootName;
  if (!rootName || !viewName) throw new Error('RUNTIME_SNAPSHOT_ADAPTER_ROOT_REQUIRED');
  const nodes = (snapshot && snapshot.nodes || []).filter(node => node.source === 'managed-view'
    && node.view === rootView && node.bounds && node.displayed !== false);
  const roots = nodes.filter(node => node.name === rootName && node.visible !== false);
  if (roots.length !== 1) throw new Error(`RUNTIME_SNAPSHOT_ROOT_IDENTITY_MISMATCH expected=1 actual=${roots.length}`);
  const root = roots[0];
  const subtree = nodes.filter(node => node.path === root.path || node.path.startsWith(`${root.path}/`));
  const byParent = new Map();
  for (const node of subtree) {
    if (!byParent.has(node.parentPath)) byParent.set(node.parentPath, []);
    byParent.get(node.parentPath).push(node);
  }
  for (const children of byParent.values()) children.sort((a, b) => finite(a.childIndex) - finite(b.childIndex));

  const convert = (node, parent) => {
    const bounds = node.bounds || {};
    const parentBounds = parent && parent.bounds || { x: 0, y: 0 };
    const scaleX = finite(node.scale && node.scale.x, 1) || 1;
    const scaleY = finite(node.scale && node.scale.y, 1) || 1;
    const result = {
      name: node.name || node.type || 'node',
      type: node.type || node.identity && node.identity.runtimeClass || 'Box',
      x: finite(bounds.x) - finite(parentBounds.x),
      y: finite(bounds.y) - finite(parentBounds.y),
      gx: finite(bounds.x), gy: finite(bounds.y),
      width: Math.abs(scaleX) > 0.0001 ? finite(bounds.width) / Math.abs(scaleX) : finite(bounds.width),
      height: Math.abs(scaleY) > 0.0001 ? finite(bounds.height) / Math.abs(scaleY) : finite(bounds.height),
      runtimeWidth: finite(bounds.width), runtimeHeight: finite(bounds.height),
      anchorX: finite(node.anchor && node.anchor.x), anchorY: finite(node.anchor && node.anchor.y),
      pivotX: finite(node.pivot && node.pivot.x), pivotY: finite(node.pivot && node.pivot.y),
      scaleX, scaleY, alpha: finite(node.alpha, 1), visible: node.visible !== false,
      skin: node.skin || '',
      runtime: {
        source: node.source, path: node.path, gx: finite(bounds.x), gy: finite(bounds.y),
        width: finite(bounds.width), height: finite(bounds.height),
        anchor: node.anchor || { x: 0, y: 0 }, pivot: node.pivot || { x: 0, y: 0 },
        scale: node.scale || { x: 1, y: 1 }, visible: node.visible !== false,
        displayed: node.displayed !== false, text: node.text || '', skin: node.skin || '', state: node.state || null,
      },
    };
    if (node.text) result.textProps = { text: node.text };
    result.children = (byParent.get(node.path) || []).map(child => convert(child, node));
    return result;
  };
  const tree = convert(root, null);
  return {
    schema: 1,
    authority: 'ui-audit.runtime-node.v3',
    capturedAt: snapshot.capturedAt || null,
    stage: snapshot.stage || null,
    selector: { source: 'managed-view', view: rootView, name: rootName, expectedCount: 1 },
    views: [{ meta: { name: viewName, rawName: rootName, source: 'runtime-adapter', runtimeRootPath: root.path }, nodeCount: subtree.length, nodeTree: tree }],
    metrics: {
      capturedNodes: subtree.length,
      visibleNodes: subtree.filter(node => node.visible !== false).length,
      textNodes: subtree.filter(node => !!node.text).length,
      imageNodes: subtree.filter(node => !!node.skin).length,
      runtimeGeometryNodes: subtree.filter(node => node.bounds).length,
    },
  };
}

function main(argv) {
  const [input, output, rootView, rootName, viewName] = argv;
  if (!input || !output || !rootName || !viewName) {
    throw new Error('usage: node runtime_snapshot_adapter.cjs <input> <output> <rootView> <rootName> <viewName>');
  }
  const bytes = fs.readFileSync(input);
  const adapted = adaptRuntimeSnapshot(JSON.parse(bytes.toString('utf8')), { rootView, rootName, viewName });
  adapted.source = { path: path.resolve(input), sha256: sha256(bytes) };
  fs.mkdirSync(path.dirname(output), { recursive: true });
  fs.writeFileSync(output, `${JSON.stringify(adapted, null, 2)}\n`, 'utf8');
  process.stdout.write(`${JSON.stringify({ output: path.resolve(output), metrics: adapted.metrics })}\n`);
}

if (require.main === module) main(process.argv.slice(2));

module.exports = { adaptRuntimeSnapshot };
