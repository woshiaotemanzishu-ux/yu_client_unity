#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');

const repoRoot = path.resolve(__dirname, '..', '..');
const ledgerPath = path.join(
  repoRoot,
  'output',
  'ui_route_audit',
  '2026-08-07_resonance',
  'route-ledger.json',
);

const OLD_ROOT = 'output/ui_route_audit/2026-08-07_resonance/old_full';
const UNITY_ROOT = 'output/ui_route_audit/2026-08-07_resonance/unity_editor/2026-08-07_1452_instruction_final';
const ROUTE_RESULT = `${UNITY_ROOT}/route-result.txt`;
const STATIC_RESULT = `${UNITY_ROOT}/static-result.txt`;
const PROTOCOL_REPORT = 'Reports/ProtocolCoverage/coverage_20260807_121650.md';
const USER_REVIEW_ROOT =
  'output/ui_route_audit/2026-08-07_resonance/user_review_2026-08-07_1550';
const USER_REVIEW = `${USER_REVIEW_ROOT}/review.md`;
const FLOW_FRAME_REVIEW_ROOT =
  'output/ui_route_audit/2026-08-07_resonance/user_review_2026-08-07_1632_flow_frame';
const FLOW_FRAME_REVIEW = `${FLOW_FRAME_REVIEW_ROOT}/review.md`;
const SLOT_OWNERSHIP_REVIEW_ROOT =
  'output/ui_route_audit/2026-08-07_resonance/user_review_2026-08-07_1705_slot_ownership';
const SLOT_OWNERSHIP_REVIEW = `${SLOT_OWNERSHIP_REVIEW_ROOT}/review.md`;
const SLOT_CONSUMER_INVENTORY = `${SLOT_OWNERSHIP_REVIEW_ROOT}/consumer-inventory.md`;

function unique(values) {
  return [...new Set(values.filter(Boolean))];
}

function setGateEvidence(node, gates, evidence) {
  node.gates = gates;
  node.applicable_gates = Object.keys(gates);
  node.evidence = unique(evidence);
}

function setDone(node) {
  node.status = 'done';
  delete node.blocked_reason;
  delete node.runtime_gap;

  if (node.type === 'navigation') {
    setGateEvidence(node, { click: true, target_identity: true }, [ROUTE_RESULT, STATIC_RESULT]);
    node.identity_evidence = [ROUTE_RESULT];
  } else if (node.type === 'return') {
    setGateEvidence(node, { click: true, return_chain: true }, [ROUTE_RESULT]);
  } else if (node.type === 'page') {
    setGateEvidence(node, { control_inventory: true, child_routes: true }, [ROUTE_RESULT, STATIC_RESULT]);
  } else {
    setGateEvidence(node, { runtime_state: true }, [ROUTE_RESULT, STATIC_RESULT]);
    node.state_evidence = [ROUTE_RESULT, STATIC_RESULT];
  }

  if (node.id.endsWith('.read-model')) {
    setGateEvidence(node, { runtime_state: true, protocol: true, restore: true },
      [STATIC_RESULT, PROTOCOL_REPORT]);
    node.state_evidence = [STATIC_RESULT, PROTOCOL_REPORT];
  } else if (node.id.endsWith('.effects')) {
    setGateEvidence(node, { runtime_state: true, render_completion: true, effect_match: true }, [
      ROUTE_RESULT,
      `${UNITY_ROOT}/effects/tab_0_ui_shenzhuang01.png`,
      `${UNITY_ROOT}/effects/tab_1_ui_shenzhuang02.png`,
      `${UNITY_ROOT}/effects/tab_2_ui_shenzhuang03.png`,
      `${UNITY_ROOT}/effects/tab_3_ui_shenzhuang03.png`,
      `${UNITY_ROOT}/effects/ui_gongmingchenggong.png`,
    ]);
    node.state_evidence = [ROUTE_RESULT];
    node.render_evidence = [ROUTE_RESULT, `${UNITY_ROOT}/effects/tab_0_ui_shenzhuang01.png`];
    node.effect_evidence = [
      `${UNITY_ROOT}/effects/tab_0_ui_shenzhuang01.png`,
      `${UNITY_ROOT}/effects/tab_1_ui_shenzhuang02.png`,
      `${UNITY_ROOT}/effects/tab_2_ui_shenzhuang03.png`,
      `${UNITY_ROOT}/effects/tab_3_ui_shenzhuang03.png`,
      `${UNITY_ROOT}/effects/ui_gongmingchenggong.png`,
    ];
  } else if (node.id.endsWith('.sound')) {
    setGateEvidence(node, { runtime_state: true }, [
      `${OLD_ROOT}/state_index.json`,
      STATIC_RESULT,
    ]);
    node.state_evidence = [`${OLD_ROOT}/state_index.json`, STATIC_RESULT];
  } else if (node.id.endsWith('.instruction.content')) {
    const instructionEvidence = [ROUTE_RESULT, `${UNITY_ROOT}/instruction_top.png`];
    if (fs.existsSync(path.join(repoRoot, UNITY_ROOT, 'instruction_bottom.png'))) {
      instructionEvidence.push(`${UNITY_ROOT}/instruction_bottom.png`);
    }
    setGateEvidence(node, {
      runtime_state: true,
      layout_structure: true,
    }, instructionEvidence);
    node.state_evidence = [ROUTE_RESULT];
    node.layout_evidence = instructionEvidence;
  } else if (node.id.endsWith('.reopen')) {
    setGateEvidence(node, { click: true, reopen: true, timing: true }, [
      ROUTE_RESULT,
      `${UNITY_ROOT}/reopen_ready.png`,
    ]);
    node.timing = { cold_ms: 1886, warm_ms: 146, environment: 'Unity Editor 6000.3.17f1 (same-process cache warm)' };
  }
}

function setBlocked(node, reason, evidence, gates) {
  node.status = 'blocked';
  node.blocked_reason = reason;
  delete node.runtime_gap;
  setGateEvidence(node, gates, evidence);
}

function setRuntimeVerify(node, gap, evidence, gates) {
  node.status = 'needs-runtime-verify';
  node.runtime_gap = gap;
  delete node.blocked_reason;
  setGateEvidence(node, gates, evidence);
}

const ledger = JSON.parse(fs.readFileSync(ledgerPath, 'utf8'));
if (ledger.schema !== 4 || ledger.route !== 'mainui.role.person.resonance') {
  throw new Error(`unexpected resonance ledger identity: schema=${ledger.schema} route=${ledger.route}`);
}
if (!Array.isArray(ledger.nodes) || ledger.nodes.length !== 458) {
  throw new Error(`unexpected resonance node count: ${ledger.nodes && ledger.nodes.length}`);
}

ledger.baseline = {
  old_h5: OLD_ROOT,
  unity_editor: UNITY_ROOT,
  static_result: STATIC_RESULT,
  protocol_report: PROTOCOL_REPORT,
  user_review_reopen: USER_REVIEW,
  user_review_flow_frame_reopen: FLOW_FRAME_REVIEW,
  user_review_slot_ownership_reopen: SLOT_OWNERSHIP_REVIEW,
  viewport: '720x1280',
  current_unity_build_target: 'Android',
  real_web_same_account: 'not-run',
};
ledger.updated_at = '2026-08-07T17:05:20+08:00';

for (const node of ledger.nodes) {
  setDone(node);

  if (node.risk === 'destructive-write') {
    setBlocked(
      node,
      '15221打造或15222回退会真实消耗账号资产；本轮仅完成确认前点击、取消链、精确wire、单飞、失败释放、主动推送和权威重拉，未获最终确认消耗授权。',
      [ROUTE_RESULT, STATIC_RESULT, PROTOCOL_REPORT],
      {
        click_to_confirmation: true,
        cancel_path: true,
        protocol: true,
        single_flight: true,
        failure_release: true,
        authoritative_refresh: true,
        live_account_success: false,
      },
    );
  } else if (node.id.endsWith('.gift')) {
    setBlocked(
      node,
      '当前 Unity PushGift 无 eGongMing 类型切片和对应购买页；同账号老端当前也不显示入口，禁止把任意礼包伪装成共鸣礼包。',
      [`${OLD_ROOT}/state_index.json`, ROUTE_RESULT, STATIC_RESULT],
      { condition_hidden: true, typed_slice: false, purchase_target: false },
    );
  } else if (node.id.endsWith('.identity-layout')) {
    setRuntimeVerify(
      node,
      '720x1280 Unity Editor 真渲染与老 H5 已分别留证，但尚无当前源码/catalog/Player 同批的真实 Unity WebGL 同账号 overlay/diff。',
      [
        `${OLD_ROOT}/shots/32_resonance_ready.png`,
        `${UNITY_ROOT}/03_resonance_ready.png`,
        `${OLD_ROOT}/shots/40_tab_3_all-things.png`,
        `${UNITY_ROOT}/tab_2_ready.png`,
      ],
      { old_h5_pixels: true, unity_editor_pixels: true, same_build_web_diff: false },
    );
  } else if (node.id.endsWith('.adaptation')) {
    setRuntimeVerify(
      node,
      '当前 Editor 构建目标为 Android；未获切换 WebGL、内容构建和发布授权，1920x1080 真实 WebGL 安全区/比例证据未执行。',
      [`${UNITY_ROOT}/03_resonance_ready.png`, ROUTE_RESULT],
      { mobile_720x1280: true, web_1920x1080: false },
    );
  } else if (node.id.endsWith('.performance')) {
    setRuntimeVerify(
      node,
      'Editor 最终同进程复跑冷开1886ms、热开146ms且重开无克隆/特效残留；首次完整证据轮为6488ms/1031ms。Player、catalog、源码同批指纹与真实 Web 冷暖耗时未执行。',
      [ROUTE_RESULT, `${UNITY_ROOT}/01_resonance_0350ms.png`, `${UNITY_ROOT}/02_resonance_1000ms.png`, `${UNITY_ROOT}/03_resonance_ready.png`],
      { editor_cold_warm: true, no_clone_leak: true, player_catalog_fingerprint: false, real_web_timing: false },
    );
    node.timing = { cold_ms: 1886, warm_ms: 146, environment: 'Unity Editor 6000.3.17f1 (same-process cache warm)' };
  } else if (node.id.endsWith('.effects')) {
    setRuntimeVerify(
      node,
      '最新人工复查纠正了特效归属：流光属于明确 opt-in 的已穿戴装备槽，不属于共鸣中央当前/下一阶图标。共享 Prefab 根组件已恢复，槽位与页面特效倍率已拆开；待新运行态抽查共鸣边缘槽、背包装备槽、普通背包格及中央展示，并验证二维足迹、动画与清理。',
      [
        SLOT_OWNERSHIP_REVIEW,
        `${SLOT_OWNERSHIP_REVIEW_ROOT}/reference_equipped_slot_flow.png`,
        `${SLOT_OWNERSHIP_REVIEW_ROOT}/regression_bag_blank_slots.png`,
        `${SLOT_OWNERSHIP_REVIEW_ROOT}/regression_resonance_central_slot_effect.png`,
      ],
      { runtime_state: false, render_completion: false, effect_match: false },
    );
    node.component_evidence = [
      SLOT_OWNERSHIP_REVIEW,
      SLOT_CONSUMER_INVENTORY,
      'Assets/Prefabs/UI/Common/EquipmentItem.prefab#EquipmentItem-root',
      'Assets/Prefabs/UI/Common/BaseAwardItem.prefab#BaseAwardItem-root',
      'Assets/Scripts/Module/Core/Common/Views/EquipmentItem.cs#SetSuitEffect',
      'Assets/Scripts/Module/Core/Bag/Views/BagEquipmentIcon.cs#SetData',
      'Assets/Scripts/Module/Core/Resonance/ResonancePresenter.cs#GetEffectScale',
    ];
    node.component_state_evidence = [
      'roots=restored; resonance-position=conditional-on; bag-equipped=conditional-on; bag-grid=off; central-current-next=page-effect-only; representative runtime pending',
    ];
  }
}

const positionDisplay = /\.positions\.position-\d+\.display$/;
const materialDisplay = /\.positions\.position-\d+\.materials\.display$/;
const materialDetailOpen = /\.positions\.position-\d+\.materials\.detail\.open$/;
for (const node of ledger.nodes) {
  let gap = '';
  let component = '';
  let states = '';
  let reviewEvidence = [];
  if (positionDisplay.test(node.id)) {
    gap = '最新人工复查确认流光属于已穿戴装备槽；此前把槽位倍率同时写进页面中央 Presenter 是错误的，共享 Prefab 根组件丢失还导致背包格回归。现已恢复共享根、限制槽位显式 opt-in，并拆开页面特效倍率；待按共鸣边缘槽、背包装备槽、普通背包格和中央展示四种形态做代表性运行抽查。';
    component = 'Assets/Scripts/Module/Core/Common/Views/EquipmentItem.cs#SetSuitEffect';
    states = 'shared-roots-restored,exact-worn-instance,position-slot-on,bag-equipped-on,ordinary-grid-off,central-page-effect-only,isolated-alpha-footprint,refresh-dispose';
    reviewEvidence = [
      SLOT_OWNERSHIP_REVIEW,
      SLOT_CONSUMER_INVENTORY,
      `${SLOT_OWNERSHIP_REVIEW_ROOT}/reference_equipped_slot_flow.png`,
      `${SLOT_OWNERSHIP_REVIEW_ROOT}/regression_bag_blank_slots.png`,
      `${SLOT_OWNERSHIP_REVIEW_ROOT}/regression_resonance_central_slot_effect.png`,
    ];
  } else if (materialDisplay.test(node.id)) {
    gap = '人工运行态复查发现材料列表按左侧起排；共享 costList.content 已改为 preferred width 中心锚点布局并通过编译，待新运行态验证 1/2/3 项整体居中。';
    component = 'Assets/Prefabs/UI/Suit/SuitModule.prefab#costList.content';
    states = 'one-item-centered,two-items-centered,three-items-centered,viewport-clipped';
    reviewEvidence = [
      USER_REVIEW,
      `${USER_REVIEW_ROOT}/user_feedback_full.png`,
      `${USER_REVIEW_ROOT}/user_feedback_detail.png`,
    ];
  } else if (materialDetailOpen.test(node.id)) {
    gap = '人工运行态复查发现共享物品详情类型文字换行且单按钮偏左；CommonModule 已扩宽类型/数量栏并给 btn_group 增加居中布局，待新运行态验证短长文案和单/双按钮。';
    component = 'Assets/Prefabs/UI/Common/CommonModule.prefab#GoodsTooltips';
    states = 'short-type,long-type,single-button-centered,double-buttons-centered';
    reviewEvidence = [
      USER_REVIEW,
      `${USER_REVIEW_ROOT}/user_feedback_full.png`,
      `${USER_REVIEW_ROOT}/user_feedback_detail.png`,
    ];
  } else {
    continue;
  }

  setRuntimeVerify(node, gap, reviewEvidence, {
    shared_component_identity: true,
    component_state_matrix: false,
    render_completion: false,
  });
  node.component_evidence = [component];
  node.component_state_evidence = [states + ': post-fix runtime pending'];
}

const children = new Map();
for (const node of ledger.nodes) {
  if (!node.parent) continue;
  if (!children.has(node.parent)) children.set(node.parent, []);
  children.get(node.parent).push(node);
}

const depth = (node) => node.id.split('.').length;
for (const node of [...ledger.nodes].sort((a, b) => depth(b) - depth(a))) {
  const directChildren = children.get(node.id) || [];
  if (directChildren.length === 0) continue;
  const blockedChildren = directChildren.filter((child) => child.status === 'blocked');
  const runtimeChildren = directChildren.filter((child) => child.status === 'needs-runtime-verify');
  if (blockedChildren.length > 0) {
    node.status = 'blocked';
    node.blocked_reason = `未收口直接子节点: ${blockedChildren.map((child) => child.id).join(', ')}`;
  } else if (runtimeChildren.length > 0 && node.status === 'done') {
    node.status = 'needs-runtime-verify';
    node.runtime_gap = `待真实运行复验直接子节点: ${runtimeChildren.map((child) => child.id).join(', ')}`;
  }
  node.evidence = unique([
    ...(node.evidence || []),
    ...directChildren.flatMap((child) => child.evidence || []).slice(0, 8),
  ]);
}

const statusCounts = ledger.nodes.reduce((counts, node) => {
  counts[node.status] = (counts[node.status] || 0) + 1;
  return counts;
}, {});
ledger.summary = {
  total: ledger.nodes.length,
  status_counts: statusCounts,
  editor_route: {
    positions: '22/22',
    stages: '46/46',
    material_details: '40/40',
    build_cancels: 22,
    effect_previews: 18,
    return_previews: 22,
    destructive_frames: { 15221: 0, 15222: 0 },
    user_review_reopened_leaf_consumers: 66,
    flow_frame_clarification_reopened_position_consumers: 22,
    invalidated_old_effect_gate: 'nonTransparentPixels>=8',
    pending_effect_gate: 'isolatedHandle pixels>=150,width>=24,height>=24',
    post_fix_runtime: 'not-run',
  },
};

fs.writeFileSync(ledgerPath, `${JSON.stringify(ledger, null, 2)}\n`, 'utf8');
console.log(JSON.stringify({ ledger: path.relative(repoRoot, ledgerPath), statusCounts }, null, 2));
