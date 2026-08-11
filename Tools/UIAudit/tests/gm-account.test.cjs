'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const {
  TOP_UI_LIMITS,
  buildTopUiCommands,
  verifyTopUiState,
  vipObservationSatisfied,
} = require('../lib/gm-account.cjs');

const highTurnCatalog = [
  { taskId: 301600, turn: 6, stage: 1 },
  { taskId: 301700, turn: 7, stage: 1 },
];
const fullLevelByTurn = {
  0: 370, 1: 370, 2: 370, 3: 370, 4: 520, 5: 630, 6: 720, 7: 840,
};

test('top UI recipe computes additive level and preserves safe command scope', () => {
  const commands = buildTopUiCommands({
    level: 630, turn: 2, vipLevel: 4, appearance: { baseTypes: [] },
    vip: { exp: 0, targetLevelNeedExp: TOP_UI_LIMITS.vipConfigExpFloor },
    currencies: { gold: 0, boundGold: 0, coin: 0 },
    reincarnation: { taskCatalog: highTurnCatalog, fullLevelByTurn },
  });
  assert.equal(commands.filter(value => value.command === 'turn').length, 3);
  assert.deepEqual(commands.filter(value => ['level', 'reincarnation-task'].includes(value.kind))
    .map(value => value.command), [
    'finishtask_301600', 'lv_90', 'finishtask_301700', 'lv_120',
  ]);
  assert.ok(!commands.some(value => value.command === 'lv_210'));
  assert.deepEqual(commands.filter(value => value.kind === 'appearance').map(value => value.type), [1, 2, 3, 4, 5, 12]);
  assert.ok(commands.some(value => value.command === 'vipexp_7700000'));
  assert.ok(commands.some(value => value.command === 'money_1000000000'));
  assert.ok(!commands.some(value => /setlv|opday|worldlv|completeachv/.test(value.command)));
});

test('top UI recipe is fully idempotent when the verified baseline is already complete', () => {
  const commands = buildTopUiCommands({
    level: TOP_UI_LIMITS.level,
    turn: TOP_UI_LIMITS.turn,
    vipLevel: 15,
    currencies: {
      gold: TOP_UI_LIMITS.money,
      boundGold: TOP_UI_LIMITS.money,
      coin: TOP_UI_LIMITS.money,
    },
    appearance: { baseTypes: [...TOP_UI_LIMITS.appearanceTypes] },
    reincarnation: { taskCatalog: highTurnCatalog, fullLevelByTurn },
  });
  assert.ok(!commands.some(value => value.kind === 'level' || value.kind === 'turn' || value.kind === 'vip'));
  assert.ok(!commands.some(value => value.kind === 'appearance'));
  assert.ok(!commands.some(value => ['main-task', 'awaken-task', 'figure-task'].includes(value.kind)));
  assert.ok(!commands.some(value => value.kind === 'money'));
});

test('top UI recipe refuses to guess missing high-turn task commands', () => {
  assert.throws(() => buildTopUiCommands({
    level: 630, turn: 5, vipLevel: 0, appearance: { baseTypes: [] },
    vip: { exp: 0, targetLevelNeedExp: TOP_UI_LIMITS.vipConfigExpFloor },
    currencies: { gold: 0, boundGold: 0, coin: 0 },
    reincarnation: { taskCatalog: [], fullLevelByTurn },
  }), /GM_ACCOUNT_REINCARNATION_RECIPE_MISSING: turn=6/);
});

test('top UI recipe refuses to guess reincarnation level gates', () => {
  assert.throws(() => buildTopUiCommands({
    level: 630, turn: 6, vipLevel: 0, appearance: { baseTypes: [] },
    vip: { exp: 0, targetLevelNeedExp: TOP_UI_LIMITS.vipConfigExpFloor },
    currencies: { gold: 0, boundGold: 0, coin: 0 },
    reincarnation: { taskCatalog: highTurnCatalog, fullLevelByTurn: {} },
  }), /GM_ACCOUNT_REINCARNATION_LEVEL_RECIPE_MISSING: turn=6/);
});

test('top UI recipe computes VIP raw-unit delta from the runtime value', () => {
  const commands = buildTopUiCommands({
    level: TOP_UI_LIMITS.level,
    turn: TOP_UI_LIMITS.turn,
    vipLevel: 5,
    vip: { exp: 770, targetLevelNeedExp: TOP_UI_LIMITS.vipConfigExpFloor },
    currencies: {
      gold: TOP_UI_LIMITS.money,
      boundGold: TOP_UI_LIMITS.money,
      coin: TOP_UI_LIMITS.money,
    },
    appearance: { baseTypes: [...TOP_UI_LIMITS.appearanceTypes] },
    reincarnation: { taskCatalog: highTurnCatalog, fullLevelByTurn },
  });
  assert.ok(commands.some(value => value.command === 'vipexp_7623000'));
});

test('VIP command observation accepts max-level runtime exp reset but not a lower level', () => {
  const expected = { vipLevel: TOP_UI_LIMITS.vipLevel, configExpAtLeast: TOP_UI_LIMITS.vipConfigExpFloor };
  assert.equal(vipObservationSatisfied({
    vipLevel: TOP_UI_LIMITS.vipLevel,
    vip: { exp: 0 },
  }, expected), true);
  assert.equal(vipObservationSatisfied({
    vipLevel: TOP_UI_LIMITS.vipLevel - 1,
    vip: { exp: TOP_UI_LIMITS.vipConfigExpFloor },
  }, expected), false);
});

test('fresh-session verification requires every top UI prerequisite', () => {
  const state = {
    level: TOP_UI_LIMITS.level,
    turn: TOP_UI_LIMITS.turn,
    turnStage: 0,
    vipLevel: TOP_UI_LIMITS.vipLevel,
    currencies: {
      gold: TOP_UI_LIMITS.money,
      boundGold: TOP_UI_LIMITS.money,
      coin: TOP_UI_LIMITS.money,
    },
    appearance: { baseTypes: [...TOP_UI_LIMITS.appearanceTypes] },
    routeProfiles: { roleOutwardWing: { pass: true } },
  };
  assert.equal(verifyTopUiState(state).pass, true);
  state.appearance.baseTypes = state.appearance.baseTypes.filter(value => value !== 3);
  const failed = verifyTopUiState(state);
  assert.equal(failed.pass, false);
  assert.equal(failed.checks.appearanceTypes, false);
  state.appearance.baseTypes = [...TOP_UI_LIMITS.appearanceTypes];
  state.turnStage = 1;
  assert.equal(verifyTopUiState(state).checks.turnStage, false);
  state.turnStage = 0;
  state.routeProfiles.roleOutwardWing.pass = false;
  assert.equal(verifyTopUiState(state).checks.roleOutwardWing, false);
});
