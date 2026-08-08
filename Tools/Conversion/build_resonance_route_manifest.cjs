/* Build the exhaustive resonance route manifest from the immutable old-client route summary. */
const fs = require('fs');
const path = require('path');

const summaryPath = process.argv[2] || 'output/ui_route_audit/2026-08-07_resonance/old_full/route_summary.json';
const outputPath = process.argv[3] || 'output/ui_route_audit/2026-08-07_resonance/route-manifest.json';
const summary = JSON.parse(fs.readFileSync(summaryPath, 'utf8'));
const route = 'mainui.role.person.resonance';
const nodes = [];
const control = (id, kind, child) => ({ id, kind, child });
const add = (node) => nodes.push(node);

const rootControls = [
  control('entry-open', 'secondary-navigation', `${route}.entry-open`),
  control('identity-layout', 'page-identity-and-layout', `${route}.identity-layout`),
  control('authoritative-read-model', 'config-and-protocol-snapshot', `${route}.read-model`),
  control('tabs', 'four-shared-content-tabs', `${route}.tabs`),
  control('instruction', 'instruction-popup', `${route}.instruction`),
  control('effects', 'equipment-and-success-effects', `${route}.effects`),
  control('sound', 'open-close-success-sound-lifecycle', `${route}.sound`),
  control('close', 'return-button', `${route}.close`),
  control('reopen', 'cold-warm-reopen', `${route}.reopen`),
  control('adaptation', 'mobile-web-adaptation', `${route}.adaptation`),
  control('performance', 'ready-and-runtime-performance', `${route}.performance`),
  control('transaction-policy', 'destructive-write-boundary', `${route}.transaction-policy`),
];

add({
  id: route,
  type: 'page',
  risk: 'read-only',
  control_inventory: rootControls,
  note: '人物→共鸣完整页面闭环。外窗四页签共享内容页；包含22个装备部位状态、46档属性浏览、说明/预览/回退弹窗、打造与回退事务、效果/声音/重开/性能。',
});
add({ id: `${route}.entry-open`, parent: route, type: 'navigation', risk: 'read-only', note: '主界面角色→人物→_Group5 共鸣按钮，必须真实点击进入 EquipSuitBaseView；不得落到 SuitCollectShellView。' });
add({ id: `${route}.identity-layout`, parent: route, type: 'read', risk: 'read-only', note: '标题共鸣、720×1280 外窗、中央山水背景、左右部位卡、当前/下一阶、材料、2/4/6属性区、底部四页签和返回身份一致。' });
add({ id: `${route}.read-model`, parent: route, type: 'read', risk: 'read-only', note: '配置 config_equip_pos2suittype/config_equip_suit_item/config_equip_suit_make 与 15220/15223/15262 权威切片共同驱动；空包/失败不清其它切片。' });
add({ id: `${route}.instruction`, parent: route, type: 'page', risk: 'read-only', control_inventory: [
  control('open', 'question-mark-button', `${route}.instruction.open`),
  control('content', 'scrollable-instruction-content', `${route}.instruction.content`),
  control('close', 'popup-close', `${route}.instruction.close`),
], note: '问号打开 InstructionType.EquipSuit；正文、滚动、遮罩、关闭和热重开均需验。' });
add({ id: `${route}.instruction.open`, parent: `${route}.instruction`, type: 'navigation', risk: 'read-only', note: '真实点击 infoBox 打开具体 InstructionView。' });
add({ id: `${route}.instruction.content`, parent: `${route}.instruction`, type: 'read', risk: 'read-only', note: '共鸣说明正文按真实 preferred height 排版，末项可达且受视口裁剪。' });
add({ id: `${route}.instruction.close`, parent: `${route}.instruction`, type: 'return', risk: 'read-only', note: '关闭只关说明层，不改变当前页签、部位和属性阶段。' });
add({ id: `${route}.effects`, parent: route, type: 'read', risk: 'read-only', note: '武防三类/饰物的 ui_shenzhuang01/02/03 常驻效果、条件预览和 ui_gongmingchenggong 成功效果都需真实 RT 像素证据；仅 Renderer/Task 不算出帧。' });
add({ id: `${route}.sound`, parent: route, type: 'read', risk: 'read-only', note: '核对入口、切页、说明/预览/回退弹窗、关闭、打造成功音及生命周期；成功音只在权威成功回包后播放。' });
add({ id: `${route}.close`, parent: route, type: 'return', risk: 'read-only', note: '右下返回只关共鸣并回人物页；不得关闭角色主窗或穿透主界面。' });
add({ id: `${route}.reopen`, parent: route, type: 'return', risk: 'read-only', note: '冷开、关闭后热重开、角色页关闭再开均按最新权威状态重建，不累积部位/特效/监听；记录 cold/warm。' });
add({ id: `${route}.adaptation`, parent: route, type: 'read', risk: 'read-only', note: '720×1280 移动端与1920×1080 Web保持比例和页面根坐标；底部页签、返回、左右部位不出安全区。' });
add({ id: `${route}.performance`, parent: route, type: 'read', risk: 'read-only', note: '约350ms/1000ms/ready留证；资源预热限当前页固定闭包，切页/换部位不重复导入，冷暖时延与帧耗记录。' });
add({ id: `${route}.transaction-policy`, parent: route, type: 'read', risk: 'read-only', note: '15221打造和15222回退均为真实资产事务；未获本轮消耗授权只做可控帧/取消链，真实成功叶保持blocked，禁止乐观扣包或孤立Toast。' });

const tabsId = `${route}.tabs`;
add({
  id: tabsId,
  parent: route,
  type: 'page',
  risk: 'read-only',
  control_inventory: summary.tabs.map((tab, index) => control(`tab-${index + 1}`, 'tab-button', `${tabsId}.${tab.label}`)),
  note: '四个底部页签真实点击、选中底图、红点、共享内容复用、回顶和切页状态隔离。',
});

for (const tab of summary.tabs) {
  const tabId = `${tabsId}.${tab.label}`;
  const positionsId = `${tabId}.positions`;
  const stageText = tab.attributeStages.filter(Boolean).join('、');
  add({
    id: tabId,
    parent: tabsId,
    type: 'page',
    risk: 'read-only',
    control_inventory: [
      control('identity', 'tab-selected-state', `${tabId}.identity`),
      control('positions', 'equipment-position-list', positionsId),
      control('attributes', 'left-right-attribute-browser', `${tabId}.attributes`),
      control('tab-red-dot', 'conditional-red-dot', `${tabId}.red-dot`),
      control('gift', 'conditional-push-gift-entry', `${tabId}.gift`),
    ],
    note: `${tab.label}：${tab.positions.length}个真实部位，属性阶段${tab.attributeStages.length}档。`,
  });
  add({ id: `${tabId}.identity`, parent: tabId, type: 'read', risk: 'read-only', note: '页签标签、选中皮肤、标题色、背景与参数(suitType/subType)一致；共享内容切页不得残留前页数据。' });
  add({ id: `${tabId}.attributes`, parent: tabId, type: 'read', risk: 'read-only', note: `左右箭头逐档可达并验证最低/最高边界；该页完整阶段：${stageText}。每档2/4/6件属性、战力和当前件数来自配置与15262。` });
  add({ id: `${tabId}.red-dot`, parent: tabId, type: 'read', risk: 'read-only', note: '页签红点由穿戴、品质/星级/阶数、材料、同阶件数和当前共鸣快照共同决定，并在权威刷新后即时变化。' });
  add({ id: `${tabId}.gift`, parent: tabId, type: 'navigation', risk: 'read-only', note: 'EnumPushGiftType.eGongMing 有数据才显示；当前111111隐藏。条件出现时需核对具体礼包目标、关闭和生命周期。' });
  add({
    id: positionsId,
    parent: tabId,
    type: 'page',
    risk: 'read-only',
    control_inventory: tab.positions.map((position, index) => control(`position-${index + 1}`, 'equipment-position-card', `${positionsId}.position-${index + 1}`)),
    note: `逐格真实点击${tab.positions.length}个部位；空槽和已穿戴、选中/未选中、红点、特效、材料、属性、弹窗均不得由其它格代验。`,
  });

  for (const position of tab.positions) {
    const positionId = `${positionsId}.position-${position.index + 1}`;
    const displayName = position.currentName || `position-${position.index + 1}`;
    add({
      id: positionId,
      parent: positionsId,
      type: 'page',
      risk: 'read-only',
      control_inventory: [
        control('select-display', 'select-current-next-and-condition', `${positionId}.display`),
        control('materials', 'material-item-list-and-details', `${positionId}.materials`),
        control('attributes', 'position-attribute-state', `${positionId}.attributes`),
        control('effect-preview', 'conditional-effect-preview', `${positionId}.preview`),
        control('return-preview', 'conditional-return-preview', `${positionId}.return-preview`),
        control('return-confirm', 'return-confirmation', `${positionId}.return-confirm`),
        control('build', 'build-button', `${positionId}.build`),
        control('refresh', 'immediate-authoritative-refresh', `${positionId}.refresh`),
      ],
      note: `部位${position.index + 1} 当前账号显示“${displayName}”；按该具体格独立核查。`,
    });
    add({ id: `${positionId}.display`, parent: positionId, type: 'read', risk: 'read-only', note: '真实点击后选中框、部位名、装备图标/阶数、当前与下一阶名称、条件提示、可打造/无法打造/满阶、红点和常驻特效同步。' });
    add({ id: `${positionId}.materials`, parent: positionId, type: 'page', risk: 'read-only', control_inventory: [
      control('display', 'owned-required-color', `${positionId}.materials.display`),
      control('detail', 'specific-goods-tooltip', `${positionId}.materials.detail`),
    ], note: '按性别选择配置中的精确材料列表；最多三格横排，逐格数量与具体物品详情一致。' });
    add({ id: `${positionId}.materials.display`, parent: `${positionId}.materials`, type: 'read', risk: 'read-only', note: '材料typeId、图标、品质底板、own/need和足够/不足颜色按当前背包权威快照显示。' });
    add({ id: `${positionId}.materials.detail`, parent: `${positionId}.materials`, type: 'page', risk: 'read-only', control_inventory: [
      control('open', 'material-icon-click', `${positionId}.materials.detail.open`),
      control('close', 'tooltip-close', `${positionId}.materials.detail.close`),
    ], note: '点击每个可见材料格打开该具体typeId的物品详情，核对标题/描述/数量/底图/遮罩。' });
    add({ id: `${positionId}.materials.detail.open`, parent: `${positionId}.materials.detail`, type: 'navigation', risk: 'read-only', note: '真实Graphic点击打开具体材料详情，不得打开任意通用弹窗代验。' });
    add({ id: `${positionId}.materials.detail.close`, parent: `${positionId}.materials.detail`, type: 'return', risk: 'read-only', note: '关闭详情回到同一页签/部位，状态不丢且不穿透。' });
    add({ id: `${positionId}.attributes`, parent: positionId, type: 'read', risk: 'read-only', note: '该部位在每个属性阶段的当前件数、2/4/6件属性和战力与配置/15262精确一致。' });
    add({ id: `${positionId}.preview`, parent: positionId, type: 'page', risk: 'read-only', control_inventory: [
      control('open', 'preview-button', `${positionId}.preview.open`),
      control('pixels', 'rendered-effect-pixels', `${positionId}.preview.pixels`),
      control('close', 'preview-close-or-mask', `${positionId}.preview.close`),
    ], note: '只有装备条件允许时显示预览眼睛；打开具体 EquipSuitPreviewTips 并验证RT真实像素、关闭和热开。' });
    add({ id: `${positionId}.preview.open`, parent: `${positionId}.preview`, type: 'navigation', risk: 'read-only', note: '真实点击 previewBox 打开所选装备和共鸣类型的预览。' });
    add({ id: `${positionId}.preview.pixels`, parent: `${positionId}.preview`, type: 'read', risk: 'read-only', note: '装备图标、最高可达共鸣文案、ui_shenzhuang01/02/03按参数缩放且有非透明像素；仅Renderer存在不算通过。' });
    add({ id: `${positionId}.preview.close`, parent: `${positionId}.preview`, type: 'return', risk: 'read-only', note: '关闭按钮和遮罩只关预览，不改变主页面选中状态。' });
    add({ id: `${positionId}.return-preview`, parent: positionId, type: 'page', risk: 'read-only', control_inventory: [
      control('open-query', 'return-button-and-15223', `${positionId}.return-preview.open-query`),
      control('display', 'return-rewards-and-text', `${positionId}.return-preview.display`),
      control('cancel', 'cancel-close-mask', `${positionId}.return-preview.cancel`),
    ], note: '已有共鸣才显示回退；打开时只读查询15223，展示精确返还奖励，取消/关闭不写入。' });
    add({ id: `${positionId}.return-preview.open-query`, parent: `${positionId}.return-preview`, type: 'navigation', risk: 'read-only', note: '点击回退后发15223(make_type,equip_type)，回包按键隔离，过期请求不得串位。' });
    add({ id: `${positionId}.return-preview.display`, parent: `${positionId}.return-preview`, type: 'read', risk: 'read-only', note: '装备、当前/回退后等级、100%返还说明和奖励横列精确呈现；奖励可横向滚动且逐格详情正确。' });
    add({ id: `${positionId}.return-preview.cancel`, parent: `${positionId}.return-preview`, type: 'return', risk: 'read-only', note: '取消、关闭、遮罩均只关闭当前弹窗，不发15222、不改变材料/共鸣/背包。' });
    add({ id: `${positionId}.return-confirm`, parent: positionId, type: 'transaction', risk: 'destructive-write', note: '确认15222前冻结页签/部位/等级/15223预览/背包指纹并二次校验；单飞；成功只按权威回包更新并展示返还奖励，失败/超时保留旧状态。真实账号成功需单独授权。' });
    add({ id: `${positionId}.build`, parent: positionId, type: 'transaction', risk: 'destructive-write', note: '15221前校验穿戴、品质/星级/阶数、性别材料、背包、同阶降战风险并冻结指纹；确认/单飞/失败/超时/成功特效与权威刷新闭环。真实账号成功需单独授权。' });
    add({ id: `${positionId}.refresh`, parent: positionId, type: 'read', risk: 'read-only', note: '可控成功/失败回包后父页的等级、材料数量、红点、2/4/6件属性、战力和回退可见性即时刷新；关闭重开仅作二次一致性，不替代即时验证。' });
  }
}

fs.mkdirSync(path.dirname(outputPath), { recursive: true });
fs.writeFileSync(outputPath, JSON.stringify({ route, nodes }, null, 2), 'utf8');
console.log(`WROTE ${outputPath} nodes=${nodes.length}`);
