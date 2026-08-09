/* Deterministic audit for the Role -> Achievement config/resource closure. */
const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '../..');
const OUTPUT = path.resolve(ROOT, process.argv[2]
  || 'output/ui_route_audit/2026-08-08_role_achievement/config_audit.json');
const expectedTypes = [
  [1, '成就总览', []],
  [2, '个人成长', [1, 2, 3, 7]],
  [3, '伙伴培养', [9]],
  [4, '装备打造', [16, 17, 19]],
  [5, '社交活动', [20, 23]],
  [6, '日常活跃', [24, 25]],
  [7, '精彩历程', [27, 28, 29]],
];
const pairs = [
  ['clientachv', 'E:/GitProject/yu_client/cdn/resource/config/client/ClientAchv.json',
    'Assets/GameRes/resource/config/client/clientachv.json'],
  ['achievement', 'E:/GitProject/yu_client/cdn/resource/config/server/config_achievement.json',
    'Assets/GameRes/resource/config/server/config_achievement.json'],
  ['star_reward', 'E:/GitProject/yu_client/cdn/resource/config/server/config_achievement_star_reward.json',
    'Assets/GameRes/resource/config/server/config_achievement_star_reward.json'],
  ['category', 'E:/GitProject/yu_client/cdn/resource/config/server/config_achievement_category.json',
    'Assets/GameRes/resource/config/server/config_achievement_category.json'],
  ['type', 'E:/GitProject/yu_client/cdn/resource/config/server/config_achievement_type_new.json',
    'Assets/GameRes/resource/config/server/config_achievement_type_new.json'],
  ['subtype', 'E:/GitProject/yu_client/cdn/resource/config/server/config_achievement_stage_reward.json',
    'Assets/GameRes/resource/config/server/config_achievement_stage_reward.json'],
  ['background', 'E:/GitProject/yu_client/cdn/resource/game/bigBg/uicj_bg1.jpg',
    'Assets/GameRes/resource/game/bigBg/uicj_bg1.jpg'],
  ['top_selected', 'E:/GitProject/yu_client/cdn/resource/game/achv/texture/uicj_026.png',
    'Assets/GameRes/resource/game/achv/texture/uicj_026.png'],
  ['top_unselected', 'E:/GitProject/yu_client/cdn/resource/game/achv/texture/uicj_027.png',
    'Assets/GameRes/resource/game/achv/texture/uicj_027.png'],
  ['sub_selected', 'E:/GitProject/yu_client/cdn/resource/game/achv/texture/uicj_029.png',
    'Assets/GameRes/resource/game/achv/texture/uicj_029.png'],
  ['sub_unselected', 'E:/GitProject/yu_client/cdn/resource/game/achv/texture/uicj_029b.png',
    'Assets/GameRes/resource/game/achv/texture/uicj_029b.png'],
  ['title', 'E:/GitProject/yu_client/cdn/resource/game/achv/texture/uicj_030.png',
    'Assets/GameRes/resource/game/achv/texture/uicj_030.png'],
  ['window_tab_up', 'E:/GitProject/yu_client/cdn/resource/game/achv/texture/uibqy_001_r3_c2.png',
    'Assets/GameRes/resource/game/achv/texture/uibqy_001_r3_c2.png'],
  ['window_tab_down', 'E:/GitProject/yu_client/cdn/resource/game/achv/texture/uibqy_001_r3_c1.png',
    'Assets/GameRes/resource/game/achv/texture/uibqy_001_r3_c1.png'],
];

const hash = (file) => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const read = (relative) => JSON.parse(fs.readFileSync(path.resolve(ROOT, relative), 'utf8'));
const readText = (relative) => fs.readFileSync(path.resolve(ROOT, relative), 'utf8');
const assertions = [];
const check = (name, pass, detail) => {
  assertions.push({ name, pass: !!pass, detail });
  if (!pass) process.exitCode = 3;
};

const entries = read('Assets/GameRes/resource/config/server/config_achievement.json');
const categories = read('Assets/GameRes/resource/config/server/config_achievement_category.json');
const types = read('Assets/GameRes/resource/config/server/config_achievement_type_new.json');
const subtypes = read('Assets/GameRes/resource/config/server/config_achievement_stage_reward.json');
const stages = read('Assets/GameRes/resource/config/server/config_achievement_star_reward.json');
const titles = read('Assets/GameRes/resource/config/client/clientachv.json');
const goods = read('Assets/GameRes/resource/config/server/config_goods.json');
const notNormalGoods = read('Assets/GameRes/resource/config/client/confignotnormalgoods.json');
const prefabPath = path.resolve(ROOT, 'Assets/Prefabs/UI/Achv/AchvModule.prefab');
const prefabText = fs.readFileSync(prefabPath, 'utf8');
const prefabYaml = prefabText.replaceAll('\r\n', '\n');
const addressableGroupPath = path.resolve(ROOT,
  'Assets/AddressableAssetsData/AssetGroups/Remote_resource.asset');
const addressableGroupText = fs.readFileSync(addressableGroupPath, 'utf8')
  .replaceAll('\r\n', '\n');
const yamlBlock = (classId, fileId) => {
  const marker = `--- !u!${classId} &${fileId}\n`;
  const start = prefabYaml.indexOf(marker);
  if (start < 0) return '';
  const end = prefabYaml.indexOf('\n--- !u!', start + marker.length);
  return prefabYaml.slice(start, end < 0 ? prefabYaml.length : end);
};
const oldRoute = read('output/ui_route_audit/2026-08-08_role_achievement/'
  + 'old_full_achievement_v5/route_summary.json');
const equipmentText = readText('Assets/Scripts/Module/Core/Role/Views/EquipmentView.cs');
const bootstrapText = readText('Assets/Scripts/Module/Core/Achievement/AchievementBootstrap.cs');
const flowText = readText('Assets/Scripts/Module/Core/Achievement/AchievementFlow.cs');
const controllerText = readText('Assets/Scripts/Module/Core/Achievement/AchievementController.cs');
const tipsManagerText = readText('Assets/Scripts/Common/Tips/TipsManager.cs');
const confirmDialogText = readText('Assets/Scripts/Common/Tips/ConfirmDialog.cs');
const oldAchievementViewText = fs.readFileSync(
  'E:/GitProject/yu_client/h5/src/achv/achvView.ts', 'utf8');
const oldTotalItemText = fs.readFileSync(
  'E:/GitProject/yu_client/h5/src/achv/AchvTotalItem.ts', 'utf8');
const oldSubItemText = fs.readFileSync(
  'E:/GitProject/yu_client/h5/src/achv/achvSubItem.ts', 'utf8');
const oldBigEffectText = fs.readFileSync(
  'E:/GitProject/yu_client/h5/src/mainUI/MainUIEffectView.ts', 'utf8');
const protoText = readText('Assets/Scripts/Framework/Net/Proto.cs');
const caseText = readText('Assets/Editor/CliVerify/Cases/AchievementCase.cs');
const syncText = readText('Assets/Editor/ConfigGenerator/ClientConfigSync.cs');
const awardMeta = fs.readFileSync(path.resolve(ROOT,
  'Assets/Prefabs/UI/Common/BaseAwardItem.prefab.meta'), 'utf8');
const awardGuid = (/^guid:\s*([0-9a-f]+)$/m.exec(awardMeta) || [])[1] || '';
const stageEffectPath = path.resolve(ROOT,
  'Assets/GameRes/effect/objs/ui_effect/ui_shengjitexiao/ui_shengjitexiao.prefab');

const hashes = pairs.map(([name, source, destination]) => {
  const sourceHash = hash(source);
  const destinationPath = path.resolve(ROOT, destination);
  const destinationHash = hash(destinationPath);
  const meta = destinationPath + '.meta';
  return { name, source, destination, sourceHash, destinationHash,
    match: sourceHash === destinationHash, meta: fs.existsSync(meta) };
});
check('source hashes and metas', hashes.every((item) => item.match && item.meta), hashes);
check('table cardinality', Object.keys(entries).length === 237
  && Object.keys(categories).length === 129
  && Object.keys(types).length === 7
  && Object.keys(subtypes).length === 30
  && Object.keys(stages).length === 200
  && Object.keys(titles).length === 6,
{ entries: Object.keys(entries).length, categories: Object.keys(categories).length,
  types: Object.keys(types).length, subtypes: Object.keys(subtypes).length,
  stages: Object.keys(stages).length, titles: Object.keys(titles).length });

const actualTypes = Object.values(types).sort((a, b) => a.id - b.id)
  .map((row) => [row.id, row.desc, JSON.parse(row.subtypes)]);
check('seven menu topology', JSON.stringify(actualTypes) === JSON.stringify(expectedTypes), actualTypes);
const selectedSubtypes = new Set(expectedTypes.flatMap((row) => row[2]));
check('selected subtypes resolve', [...selectedSubtypes].every((id) => subtypes[id]),
  [...selectedSubtypes].filter((id) => !subtypes[id]));
check('selected subtypes have categories', [...selectedSubtypes].every((id) =>
  Object.values(categories).some((row) => row.subtype === id)),
  [...selectedSubtypes].filter((id) => !Object.values(categories).some((row) => row.subtype === id)));
const missingCategories = [...new Set(Object.values(entries)
  .map((row) => String(row['1'])).filter((id) => !categories[id]))];
check('all entry categories resolve', missingCategories.length === 0, missingCategories);
check('overview title chain', [1, 2, 3, 4, 5, 6].every((id) => titles[id]), titles);
check('stage chain contiguous', Array.from({ length: 200 }, (_, i) => i + 1)
  .every((id) => stages[id] && stages[id].stage === id), '1..200');
const malformed = Object.entries(entries).filter(([, row]) =>
  typeof row['7'] !== 'string' || !/^\[.*\]$/.test(row['7'].trim())
  || typeof row['8'] !== 'string' || !/^\[.*\]$/.test(row['8'].trim()))
  .map(([id]) => Number(id));
check('entry condition/reward tuple shape', malformed.length === 0, malformed);

const rewardParseFailures = [];
const rewardKeys = new Map();
for (const [entryId, row] of Object.entries(entries)) {
  try {
    const tuples = JSON.parse(String(row['8'] || '[]').replaceAll('{', '[').replaceAll('}', ']'));
    for (const tuple of tuples) {
      if (!Array.isArray(tuple) || tuple.length < 3) throw new Error('tuple shape');
      const type = Number(tuple[0]);
      const typeId = Number(tuple[1]);
      const key = (type === -1 || type === 255) ? typeId : type;
      const mappedId = type === 0 || type === 100
        ? typeId
        : Number(notNormalGoods[String(key)]?.goods_id || typeId);
      rewardKeys.set(`${type}:${typeId}`, { type, typeId, mappedId });
    }
  } catch (error) {
    rewardParseFailures.push({ entryId: Number(entryId), reward: row['8'], error: error.message });
  }
}
// 每条详情固定追加成就点奖励；它不在 config_achievement.reward 字段中。
rewardKeys.set('0:40', { type: 0, typeId: 40, mappedId: 40 });
const rewardIcons = [...rewardKeys.values()].map((reward) => {
  const goodsRow = goods[String(reward.mappedId)];
  const icon = goodsRow ? String(goodsRow['14'] || '') : '';
  const source = icon
    ? path.resolve('E:/GitProject/yu_client/cdn/resource/game/goodsicon', `${icon}.png`)
    : '';
  const destination = icon
    ? path.resolve(ROOT, 'Assets/GameRes/resource/game/goodsicon', `${icon}.png`)
    : '';
  const meta = destination ? `${destination}.meta` : '';
  const sourceExists = !!source && fs.existsSync(source);
  const destinationExists = !!destination && fs.existsSync(destination);
  const metaExists = !!meta && fs.existsSync(meta);
  const sourceHash = sourceExists ? hash(source) : '';
  const destinationHash = destinationExists ? hash(destination) : '';
  const guid = metaExists
    ? ((/^guid:\s*([0-9a-f]+)$/m.exec(fs.readFileSync(meta, 'utf8')) || [])[1] || '')
    : '';
  return {
    ...reward,
    icon,
    goods: !!goodsRow,
    source: source.replaceAll('\\', '/'),
    destination: path.relative(ROOT, destination).replaceAll('\\', '/'),
    sourceHash,
    destinationHash,
    match: sourceExists && destinationExists && sourceHash === destinationHash,
    meta: metaExists,
    guid,
  };
});
const rewardGuids = rewardIcons.map((item) => item.guid).filter(Boolean);
check('reward resource closure', rewardParseFailures.length === 0
  && rewardIcons.length === 31
  && rewardIcons.every((item) => item.goods && item.icon && item.match && item.meta && item.guid)
  && new Set(rewardGuids).size === rewardGuids.length,
{ rewardKeys: rewardIcons.length, parseFailures: rewardParseFailures,
  unresolved: rewardIcons.filter((item) => !item.goods || !item.icon || !item.match || !item.meta || !item.guid),
  uniqueGuids: new Set(rewardGuids).size });
const addressableSources = [
  ...pairs.slice(0, 7).map(([name, , destination]) => ({
    name,
    destination,
    label: destination.includes('/config/')
      ? 'pack_resource_config'
      : 'pack_resource_game_bigbg',
  })),
  ...rewardIcons.map((reward) => ({
    name: `reward_${reward.icon}`,
    destination: reward.destination,
    label: 'pack_resource_game_goodsicon',
  })),
];
const addressableClosure = addressableSources.map((asset) => {
  const metaPath = path.resolve(ROOT, `${asset.destination}.meta`);
  const guid = fs.existsSync(metaPath)
    ? ((/^guid:\s*([0-9a-f]+)$/m.exec(fs.readFileSync(metaPath, 'utf8')) || [])[1] || '')
    : '';
  const address = asset.destination
    .replaceAll('\\', '/')
    .replace(/^Assets\/GameRes\//, '')
    .replace(/\.[^.]+$/, '')
    .toLowerCase();
  const marker = `  - m_GUID: ${guid}\n`;
  const occurrences = guid ? addressableGroupText.split(marker).length - 1 : 0;
  const start = guid ? addressableGroupText.indexOf(marker) : -1;
  const end = start >= 0
    ? addressableGroupText.indexOf('\n  - m_GUID:', start + marker.length)
    : -1;
  const block = start >= 0
    ? addressableGroupText.slice(start, end < 0 ? addressableGroupText.length : end)
    : '';
  return {
    ...asset,
    guid,
    address,
    occurrences,
    pass: occurrences === 1
      && block.includes(`    m_Address: ${address}\n`)
      && block.includes(`    - ${asset.label}\n`),
  };
});
check('addressable config/background/reward closure',
  addressableClosure.length === 38
  && addressableClosure.every((item) => item.pass),
  { resources: addressableClosure.length,
    unresolved: addressableClosure.filter((item) => !item.pass) });
const prefabNodes = ['AchvMainView', 'AchvTabBar', 'AchvTabBtn', 'AchvTabSubBtn',
  'AchvTotalItem', 'achvSubItem'];
check('prefab editable templates', prefabNodes.every((name) =>
  prefabText.includes(`m_Name: ${name}`)), prefabNodes);
check('shared BaseAwardItem identity', !!awardGuid
  && prefabText.includes(`m_SourcePrefab: {fileID: 100100000, guid: ${awardGuid}, type: 3}`),
  { awardGuid });
const verticalScrolls = [
  ['overview', '1119539655247599684', '2758920106149663567'],
  ['detail', '5600439743294609583', '740783770766237454'],
  ['attributes', '7203213776145658767', '4275879885349684943'],
];
const horizontalScrolls = [
  ['top tabs', '6239617868906171049', '817420020342951068'],
  ['type cards', '3398376172097113483', '3880143878740540783'],
  ['detail rewards', '1630637696270534530', '7706317938732744909'],
];
const scrollStructure = [
  ...verticalScrolls.map(([name, component, content]) => {
    const block = yamlBlock(114, component);
    return { name, pass: block.includes(`m_Content: {fileID: ${content}}`)
      && block.includes('m_Horizontal: 0') && block.includes('m_Vertical: 1')
      && block.includes('m_MovementType: 2') };
  }),
  ...horizontalScrolls.map(([name, component, content]) => {
    const block = yamlBlock(114, component);
    return { name, pass: block.includes(`m_Content: {fileID: ${content}}`)
      && block.includes('m_Horizontal: 1') && block.includes('m_Vertical: 0')
      && block.includes('m_MovementType: 2') };
  }),
];
const layoutComponentIds = Array.from({ length: 9 }, (_, index) =>
  String(910000000000000001n + BigInt(index)));
const slotSpecs = [
  ['910000000000000101', '910000000000000102', '__SubSlot0', 61, -141],
  ['910000000000000103', '910000000000000104', '__SubSlot1', 70, -70],
  ['910000000000000105', '910000000000000106', '__SubSlot2', 30, -6],
  ['910000000000000107', '910000000000000108', '__SubSlot3', -41, 11],
];
const slotStructure = slotSpecs.map(([go, rect, name, x, y]) => ({
  name,
  pass: yamlBlock(1, go).includes(`m_Name: ${name}`)
    && yamlBlock(224, rect).includes('m_Father: {fileID: 3951037817870947736}')
    && yamlBlock(224, rect).includes(`m_AnchoredPosition: {x: ${x}, y: ${y}}`),
}));
check('prefab-owned scroll and layout structure',
  scrollStructure.every((item) => item.pass)
  && yamlBlock(114, '8540357230025611318').includes('m_Enabled: 0')
  && yamlBlock(114, '8798995644871613748').includes('m_Enabled: 0')
  && layoutComponentIds.every((id) => yamlBlock(114, id))
  && slotStructure.every((item) => item.pass)
  && yamlBlock(224, '3951037817870947736').includes('{fileID: 910000000000000102}')
  && yamlBlock(224, '3951037817870947736').includes('{fileID: 910000000000000108}'),
  { scrollStructure, slotStructure, layoutComponentIds });

const oldLabels = oldRoute.topTabs.map((top) => [top.label, top.subTabs.map((sub) => sub.label)]);
const expectedLabels = [
  ['成就总览', []],
  ['个人成长', ['人物', '时装', '灵魄', '合成']],
  ['伙伴培养', ['培养']],
  ['装备打造', ['打造', '穿戴', '共鸣']],
  ['社交活动', ['交友', '结社']],
  ['日常活跃', ['日常', '财富']],
  ['精彩历程', ['活动', '副本', '大妖']],
];
check('old H5 full read route', JSON.stringify(oldLabels) === JSON.stringify(expectedLabels)
  && oldRoute.topTabs.reduce((sum, top) => sum + top.subTabs.length, 0) === 15
  && oldRoute.warmOpenMs > 0,
{ labels: oldLabels, subTabs: oldRoute.topTabs.reduce((sum, top) => sum + top.subTabs.length, 0),
  warmOpenMs: oldRoute.warmOpenMs });
const writeProtocols = new Set(oldRoute.destructiveClicks.map((item) => item.protocol));
check('old H5 write controls enumerated without click', writeProtocols.has(40902)
  && writeProtocols.has(40905)
  && oldRoute.destructiveClicks.length > 0
  && oldRoute.destructiveClicks.every((item) => item.clicked === false),
{ observations: oldRoute.destructiveClicks.length, protocols: [...writeProtocols].sort(), clicked: 0 });

check('Unity person entry route', equipmentText.includes('MainUIRouter.Open("AchvEnterView")')
  && bootstrapText.includes('MainUIRouter.Register("AchvEnterView", AchievementFlow.Toggle)'),
{ entry: 'EquipmentView._Group2', route: 'AchvEnterView' });
const protocolSymbols = [
  ['ACHIEVEMENT_STAGE', 40901], ['ACHIEVEMENT_STAGE_CLAIM', 40902],
  ['ACHIEVEMENT_ENTRIES', 40903], ['ACHIEVEMENT_ENTRY_UPDATES', 40904],
  ['ACHIEVEMENT_ENTRY_CLAIM', 40905], ['ACHIEVEMENT_STAR', 40906],
  ['ACHIEVEMENT_STAGE_REWARD_UPDATE', 40907], ['ACHIEVEMENT_TYPES', 40908],
  ['ACHIEVEMENT_CATEGORY_ENTRIES', 40909],
];
check('409 protocol family complete', protocolSymbols.every(([name, id]) =>
  protoText.includes(`const int ${name} = ${id};`)
  && controllerText.includes(`RegisterProtocal(Proto.${name},`)), protocolSymbols);
check('read and transaction flows wired', [
  'AchievementConfigs.GetTypes()', 'RequestStartup()', 'RequestCategory(OverviewCategory)',
  'RequestStageClaim(stage)', 'RequestEntryClaim(entry.Id, entry.Category)',
  'ValidateVerticalScroll(_overviewScroll', 'ValidateVerticalScroll(_detailScroll',
  'ValidateHorizontalScroll(_tabBar.scroll, _tabBar.Content',
  '_tpl_BaseAwardItem', '_awardPrefab',
].every((needle) => flowText.includes(needle)), 'overview/categories/details/scroll/claims/shared-award');
check('runtime does not author achievement layout',
  !flowText.includes('AddComponent<VerticalLayoutGroup>')
  && !flowText.includes('AddComponent<HorizontalLayoutGroup>')
  && !flowText.includes('AddComponent<ContentSizeFitter>')
  && !flowText.includes('AddComponent<LayoutElement>')
  && !flowText.includes('ConfigureVerticalLayout')
  && !flowText.includes('ConfigureHorizontalLayout')
  && !flowText.includes('SetLayoutSize(')
  && !flowText.includes('new Vector2(61f, -141f)')
  && flowText.includes('__SubSlot'),
  'LayoutGroup/ContentSizeFitter/LayoutElement/scroll direction/sub-tab positions live in AchvModule.prefab');
check('initial selection preserves overview claim priority',
  flowText.includes('!AchievementModel.Instance.HasAllStartupData')
  && flowText.includes('TryGetCategory(OverviewCategory, out _)')
  && flowText.includes('overview != null && HasClaimableType(overview.Type)')
  && flowText.indexOf('overview != null && HasClaimableType(overview.Type)')
    < flowText.lastIndexOf('state.Type.Subtypes.FirstOrDefault(HasClaimableSubtype)'),
  'overview claimable -> overview; otherwise first claimable subtype -> overview fallback');
check('claim single-flight waits for authority', controllerText.includes('_stageClaimAwaitingRefresh')
  && controllerText.includes('_entryClaimAwaitingRefresh')
  && controllerText.includes('StageSnapshotConfirmsClaim(stage)')
  && controllerText.includes('ClaimTimeoutMs = 12000')
  && controllerText.includes('ReleaseStageClaimTimeoutAsync')
  && controllerText.includes('ReleaseEntryClaimTimeoutAsync')
  && caseText.includes('40902 unlocks on authoritative snapshot')
  && caseText.includes('40902 stale snapshot keeps single flight')
  && caseText.includes('40909 category snapshot'),
{ stage: '40902 -> 40901/40907', entry: '40905 -> 40903/40909' });
check('entry claim bag capacity and recovery route',
  oldTotalItemText.includes('CheckEquipNum(5, 5)')
  && oldSubItemText.includes('CheckEquipNum(5, 5)')
  && flowText.includes('bag.MaxCell - bag.BagGoodsList.Count < 5')
  && flowText.includes('背包空间不足，是否前往整理？')
  && flowText.includes('null, "前往整理", "取消"')
  && flowText.includes('BagFlow.Open()'),
  'five free main-bag cells; 前往整理/取消; confirm can navigate to bag');
check('confirm custom labels preserve existing consumers',
  tipsManagerText.includes('string yesLabel = "确认", string noLabel = "取消"')
  && tipsManagerText.includes('ConfirmDialog.Show(text, onYes, onNo, yesLabel, noLabel)')
  && confirmDialogText.includes('_pendingYesLabel = "确认"')
  && confirmDialogText.includes('_pendingNoLabel = "取消"')
  && confirmDialogText.includes('_view.ok_label.text = _pendingYesLabel')
  && confirmDialogText.includes('_view.cancel_label.text = _pendingNoLabel'),
  { directConsumers: 15, defaults: ['确认', '取消'], target: ['前往整理', '取消'] });
check('stage success effect ownership and lifecycle',
  oldAchievementViewText.includes('PlayBigEffect("ui_shengjitexiao")')
  && oldBigEffectText.includes('this._obj.pos || { x: 0, y: 2 }')
  && oldBigEffectText.includes('this._obj.scale || 1')
  && oldBigEffectText.includes('this.time_count = 15')
  && flowText.includes('UIEffectStage.AddAsync("ui_shengjitexiao", parent')
  && flowText.includes('new Vector2(0f, 2f), Vector3.one')
  && flowText.includes('StageSuccessEffectDurationMs = 15000')
  && fs.existsSync(stageEffectPath) && fs.existsSync(`${stageEffectPath}.meta`),
  { layer: 'Top/FightingUp equivalent', position: [0, 2], scale: 1, durationMs: 15000,
    asset: path.relative(ROOT, stageEffectPath).replaceAll('\\', '/') });
check('entry reward fly uses shared award and bag target',
  oldTotalItemText.includes('EventName.REWARD_FLY')
  && oldSubItemText.includes('EventName.REWARD_FLY')
  && flowText.includes('BaseAwardItem(AchievementRewardFly)')
  && flowText.includes('item.Res == "bag"')
  && flowText.includes('RewardFlyDurationMs = 750')
  && flowText.includes('RewardFlyStaggerMs = 120'),
  { component: 'BaseAwardItem', target: 'MainUI bag', durationMs: 750, staggerMs: 120 });
const syncNames = ['ClientAchv', 'config_achievement', 'config_achievement_star_reward',
  'config_achievement_category', 'config_achievement_type_new', 'config_achievement_stage_reward'];
check('config sync closure registered', syncNames.every((name) => syncText.includes(`"${name}"`)), syncNames);

const result = {
  schema: 1,
  generatedAt: new Date().toISOString(),
  route: '人物/成就',
  counts: { entries: 237, categories: 129, types: 7, subtypes: 30, stages: 200, titles: 6 },
  menuTopology: actualTypes,
  oldH5: {
    run: 'output/ui_route_audit/2026-08-08_role_achievement/old_full_achievement_v5',
    topTabs: oldRoute.topTabs.length,
    subTabs: oldRoute.topTabs.reduce((sum, top) => sum + top.subTabs.length, 0),
    writeObservations: oldRoute.destructiveClicks.length,
    writesClicked: oldRoute.destructiveClicks.filter((item) => item.clicked).length,
    warmOpenMs: oldRoute.warmOpenMs,
  },
  hashes,
  rewardIcons,
  prefab: {
    path: 'Assets/Prefabs/UI/Achv/AchvModule.prefab',
    sha256: hash(prefabPath),
    nodes: prefabNodes,
    sharedBaseAwardItemGuid: awardGuid,
  },
  dynamicResources: {
    stageSuccessEffect: path.relative(ROOT, stageEffectPath).replaceAll('\\', '/'),
    stageSuccessEffectSha256: hash(stageEffectPath),
  },
  addressables: {
    group: path.relative(ROOT, addressableGroupPath).replaceAll('\\', '/'),
    groupSha256: hash(addressableGroupPath),
    resources: addressableClosure,
  },
  assertions,
  pass: assertions.every((item) => item.pass),
};
fs.mkdirSync(path.dirname(OUTPUT), { recursive: true });
fs.writeFileSync(OUTPUT, JSON.stringify(result, null, 2) + '\n', 'utf8');
console.log(JSON.stringify({ output: OUTPUT, pass: result.pass,
  assertions: assertions.length, counts: result.counts }));
if (!result.pass) process.exit(3);
