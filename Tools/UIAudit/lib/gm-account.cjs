'use strict';

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { HeadlessUiSession, sleep } = require('./session.cjs');
const { loadPopupPolicy } = require('./popup-policy.cjs');
const { loadRuntimeOverlayPolicy } = require('./runtime-overlay.cjs');
const { findServerProfileForUrl } = require('./server-readiness.cjs');
const { ensureServer } = require('./server-lifecycle.cjs');
const { writeJsonAtomic } = require('./safe-json.cjs');
const { createControlledItemUseHandler } = require('./item-use-handler.cjs');

const TOP_UI_LIMITS = Object.freeze({
  level: 840,
  turn: 7,
  directTurnCommandMax: 5,
  vipLevel: 15,
  vipConfigExpFloor: 77000,
  vipExpConversion: 100,
  money: 1000000000,
  appearanceTypes: [1, 2, 3, 4, 5, 12],
});

function finiteInteger(value, name) {
  const number = Number(value);
  if (!Number.isInteger(number)) throw new Error(`GM_ACCOUNT_STATE_INVALID: ${name}=${value}`);
  return number;
}

function getTurnFullLevel(state, turn) {
  const values = state.reincarnation && state.reincarnation.fullLevelByTurn;
  const level = values && Number(values[turn]);
  if (!Number.isInteger(level) || level < 1) {
    throw new Error(`GM_ACCOUNT_REINCARNATION_LEVEL_RECIPE_MISSING: turn=${turn}`);
  }
  return level;
}

function buildTopUiCommands(state, limits = TOP_UI_LIMITS) {
  const level = finiteInteger(state.level, 'level');
  const turn = finiteInteger(state.turn, 'turn');
  const vipLevel = finiteInteger(state.vipLevel, 'vipLevel');
  if (level < 1 || level > limits.level) throw new Error(`GM_ACCOUNT_LEVEL_OUT_OF_RANGE: ${level}`);
  if (turn < 0 || turn > limits.turn) throw new Error(`GM_ACCOUNT_TURN_OUT_OF_RANGE: ${turn}`);
  if (vipLevel < 0 || vipLevel > limits.vipLevel) throw new Error(`GM_ACCOUNT_VIP_OUT_OF_RANGE: ${vipLevel}`);

  const commands = [];
  let simulatedLevel = level;
  let simulatedTurn = turn;
  const directTurnTarget = Math.min(limits.turn, limits.directTurnCommandMax);
  for (let next = turn + 1; next <= directTurnTarget; next++) commands.push({
    command: 'turn',
    kind: 'turn',
    expected: { turn: next },
  });
  simulatedTurn = Math.max(simulatedTurn, directTurnTarget);
  const catalog = state.reincarnation && Array.isArray(state.reincarnation.taskCatalog)
    ? state.reincarnation.taskCatalog : [];
  const taskByTurn = new Map(catalog.map(value => [Number(value.turn), value]));
  for (let next = Math.max(turn + 1, limits.directTurnCommandMax + 1); next <= limits.turn; next++) {
    const gateLevel = getTurnFullLevel(state, simulatedTurn);
    if (simulatedLevel < gateLevel) {
      commands.push({
        command: `lv_${gateLevel - simulatedLevel}`,
        kind: 'level',
        purpose: 'reincarnation-gate',
        expected: { level: gateLevel, unlocksTurnTask: next },
      });
      simulatedLevel = gateLevel;
    }
    const task = taskByTurn.get(next);
    if (!task || !Number.isInteger(Number(task.taskId))) {
      throw new Error(`GM_ACCOUNT_REINCARNATION_RECIPE_MISSING: turn=${next}`);
    }
    commands.push({
      command: `finishtask_${Number(task.taskId)}`,
      kind: 'reincarnation-task',
      taskId: Number(task.taskId),
      recovery: { command: `task_${Number(task.taskId)}`, onlyIfInactive: true },
      expected: { turn: next, turnStage: 0 },
    });
    simulatedTurn = next;
  }
  const configuredTargetLevel = getTurnFullLevel(state, limits.turn);
  if (configuredTargetLevel !== limits.level) {
    throw new Error(`GM_ACCOUNT_TARGET_LEVEL_CONFIG_DRIFT: configured=${configuredTargetLevel} expected=${limits.level}`);
  }
  if (simulatedLevel < limits.level) commands.push({
    command: `lv_${limits.level - simulatedLevel}`,
    kind: 'level',
    expected: { level: limits.level },
  });
  const activeTypes = new Set((state.appearance && state.appearance.baseTypes || []).map(Number));
  for (const type of limits.appearanceTypes) {
    if (activeTypes.has(type)) continue;
    commands.push({
      command: `activemounttype_${type}`,
      kind: 'appearance',
      type,
    });
  }
  if (vipLevel < limits.vipLevel) {
    const vip = state.vip || {};
    const currentConfigExp = finiteInteger(vip.exp, 'vip.exp');
    const observedTargetConfigExp = Number(vip.targetLevelNeedExp || 0);
    if (observedTargetConfigExp && observedTargetConfigExp !== limits.vipConfigExpFloor) {
      throw new Error(`GM_ACCOUNT_VIP_CONFIG_DRIFT: configured=${observedTargetConfigExp} expected=${limits.vipConfigExpFloor}`);
    }
    const targetConfigExp = limits.vipConfigExpFloor;
    const rawDelta = Math.max(0, (targetConfigExp - currentConfigExp) * limits.vipExpConversion);
    if (rawDelta > 0) commands.push({
      command: `vipexp_${rawDelta}`,
      kind: 'vip',
      expected: { vipLevel: limits.vipLevel, configExpAtLeast: targetConfigExp },
    });
  }
  const currencies = state.currencies || {};
  if (Number(currencies.gold) !== limits.money
      || Number(currencies.boundGold) !== limits.money
      || Number(currencies.coin) !== limits.money) {
    commands.push({
      command: `money_${limits.money}`,
      kind: 'money',
      expected: { money: limits.money },
    });
  }
  return commands;
}

function verifyTopUiState(state, limits = TOP_UI_LIMITS) {
  const activeTypes = new Set((state.appearance && state.appearance.baseTypes || []).map(Number));
  const checks = {
    level: Number(state.level) === limits.level,
    turn: Number(state.turn) === limits.turn,
    turnStage: Number(state.turnStage) === 0,
    vipLevel: Number(state.vipLevel) === limits.vipLevel,
    gold: Number(state.currencies && state.currencies.gold) === limits.money,
    boundGold: Number(state.currencies && state.currencies.boundGold) === limits.money,
    coin: Number(state.currencies && state.currencies.coin) === limits.money,
    appearanceTypes: limits.appearanceTypes.every(type => activeTypes.has(type)),
    roleOutwardWing: !!(state.routeProfiles && state.routeProfiles.roleOutwardWing
      && state.routeProfiles.roleOutwardWing.pass),
  };
  return { pass: Object.values(checks).every(Boolean), checks };
}

function vipObservationSatisfied(state, expected, limits = TOP_UI_LIMITS) {
  const observedLevel = Number(state && state.vipLevel);
  if (observedLevel < Number(expected && expected.vipLevel)) return false;
  if (observedLevel >= Number(limits.vipLevel)) return true;
  return Number(state && state.vip && state.vip.exp) >= Number(expected && expected.configExpAtLeast);
}

function readGmPassword(configPath) {
  const content = fs.readFileSync(configPath, 'utf8');
  const match = content.match(/\{\s*gm_password\s*,\s*"([^"]*)"\s*\}/);
  if (!match) throw new Error(`GM_PASSWORD_NOT_FOUND: ${configPath}`);
  return match[1];
}

function sha256File(filePath) {
  return crypto.createHash('sha256').update(fs.readFileSync(filePath)).digest('hex');
}

function readVipAuthority(repoRoot, limits = TOP_UI_LIMITS) {
  const clientConfigPath = path.resolve(repoRoot, '..', 'yu_client', 'cdn', 'resource', 'config', 'server', 'config_vip_config.json');
  const serverHeaderPath = path.resolve(repoRoot, '..', 'yu_server', 'include', 'vip.hrl');
  const config = JSON.parse(fs.readFileSync(clientConfigPath, 'utf8'));
  const target = config[String(limits.vipLevel)] || config[limits.vipLevel];
  const targetConfigExp = Number(target && target.need_gold);
  const header = fs.readFileSync(serverHeaderPath, 'utf8');
  const match = header.match(/-define\(VIP_CONVERT,\s*(\d+)\)/);
  const conversion = Number(match && match[1]);
  if (targetConfigExp !== limits.vipConfigExpFloor || conversion !== limits.vipExpConversion) {
    throw new Error(`GM_ACCOUNT_VIP_AUTHORITY_DRIFT: level=${limits.vipLevel} configExp=${targetConfigExp} conversion=${conversion}`);
  }
  return {
    targetLevel: limits.vipLevel,
    targetConfigExp,
    conversion,
    clientConfig: { path: clientConfigPath, sha256: sha256File(clientConfigPath) },
    serverHeader: { path: serverHeaderPath, sha256: sha256File(serverHeaderPath) },
  };
}

async function readAccountState(page) {
  return page.evaluate(() => {
    const number = value => Number.isFinite(Number(value)) ? Number(value) : 0;
    const asList = value => {
      if (Array.isArray(value)) return value;
      if (!value || typeof value !== 'object') return [];
      return Object.keys(value)
        .filter(key => /^\d+$/.test(key))
        .sort((left, right) => Number(left) - Number(right))
        .map(key => value[key]);
    };
    const parseConditions = value => {
      if (Array.isArray(value) || value && typeof value === 'object') return asList(value).map(asList);
      if (typeof value !== 'string' || !value.trim()) return [];
      try {
        const parsed = JSON.parse(value);
        return asList(parsed).map(asList);
      } catch (_) { return [{ kind: 'malformed', raw: value }]; }
    };
    const scalarSummary = (value, fields) => {
      const result = {};
      if (!value || typeof value !== 'object') return result;
      for (const field of fields) {
        if (value[field] === undefined || value[field] === null) continue;
        const current = value[field];
        if (typeof current === 'string' || typeof current === 'boolean' || Number.isFinite(Number(current))) {
          result[field] = typeof current === 'string' && current.trim() && !Number.isFinite(Number(current))
            ? current : number(current);
        }
      }
      return result;
    };
    const manager = window.RoleManager && window.RoleManager.GetInstance && window.RoleManager.GetInstance();
    const role = manager && manager.mainRoleInfo;
    if (!role || !number(role.role_id)) throw new Error('GM_ACCOUNT_ROLE_NOT_READY');
    const vipModel = window.VipModel && window.VipModel.GetInstance && window.VipModel.GetInstance();
    const vipLevel = number(vipModel && vipModel.GetVipLevel && vipModel.GetVipLevel());
    const vipExp = number(vipModel && vipModel.GetExp && vipModel.GetExp());
    const vipConfig = window.Config && window.Config.PRELOAD_SERVER_CONFIG
      && window.Config.PRELOAD_SERVER_CONFIG.config_vip_config || {};
    const vipMaxLevel = number(vipModel && vipModel.max_vip_lv) || 15;
    const vipTargetConfig = vipConfig[vipMaxLevel] || vipConfig[String(vipMaxLevel)] || {};
    const functionModel = window.FunctionOpenModel && window.FunctionOpenModel.GetInstance
      && window.FunctionOpenModel.GetInstance();
    const commonManager = window.CommonManager && window.CommonManager.GetInstance
      && window.CommonManager.GetInstance();
    const serverTime = window.ServerTimeModel && window.ServerTimeModel.GetInstance
      && window.ServerTimeModel.GetInstance();
    const outward = window.OutWardBaseModel && window.OutWardBaseModel.GetInstance
      && window.OutWardBaseModel.GetInstance();
    const reincarnation = window.ReincarnationModel && window.ReincarnationModel.GetInstance
      && window.ReincarnationModel.GetInstance();
    const taskModel = window.TaskModel && window.TaskModel.GetInstance && window.TaskModel.GetInstance();
    const goodsModel = window.GoodsModel && window.GoodsModel.GetInstance && window.GoodsModel.GetInstance();
    const finished = Array.isArray(functionModel && functionModel.finish_fun_data)
      ? functionModel.finish_fun_data : [];
    const finishedIds = new Set(finished.map(value => number(value && value.id)));
    const configMap = window.Config && window.Config.PRELOAD_SERVER_CONFIG
      && window.Config.PRELOAD_SERVER_CONFIG.config_module_open || {};
    const configValues = Object.keys(configMap).map(key => configMap[key]).filter(Boolean);
    const modules = Array.isArray(functionModel && functionModel.module_open_list)
      && functionModel.module_open_list.length ? functionModel.module_open_list : configValues;
    const openDay = number(serverTime && serverTime.GetOpenServerDay && serverTime.GetOpenServerDay());
    const unresolved = modules.filter(value => !finishedIds.has(number(value.id))).map(value => {
      const conditions = parseConditions(value.condition);
      let blocker = 'task-or-server-state';
      for (const condition of conditions) {
        if (!Array.isArray(condition) || condition.length < 2) continue;
        if (condition[0] === 'lv' && number(role.level) < number(condition[1])) blocker = 'level';
        if (condition[0] === 'open_day' && openDay < number(condition[1])) blocker = 'open-day';
      }
      return {
        id: number(value.id),
        name: String(value.name || value.module_name || ''),
        conditions,
        blocker,
      };
    }).sort((left, right) => left.id - right.id);
    const outwardData = outward && outward.outward_data_list || {};
    const baseTypes = Object.keys(outwardData)
      .filter(key => outwardData[key])
      .map(Number)
      .filter(Number.isFinite)
      .sort((left, right) => left - right);
    const baseData = baseTypes.map(typeId => ({
      typeId,
      ...scalarSummary(outwardData[typeId], [
        'type_id', 'stage', 'star', 'level', 'lv', 'blessing', 'figure_stage',
        'upgrade_sys_lv', 'combat', 'etime',
      ]),
    }));
    const illusionSources = outward && outward.active_illu_list || {};
    const illusionState = outward && outward.outward_illu_data_list || {};
    const illusionTypes = Array.from(new Set([
      ...Object.keys(illusionSources),
      ...Object.keys(illusionState),
    ].map(Number).filter(Number.isFinite))).sort((left, right) => left - right);
    const illusions = illusionTypes.map(typeId => {
      const activeMap = illusionSources[typeId] || {};
      const active = Object.keys(activeMap).map(key => activeMap[key]).filter(Boolean).map(value => ({
        id: number(value.id),
        stage: number(value.stage),
        star: number(value.star),
      })).sort((left, right) => left.id - right.id);
      return {
        typeId,
        selectedIllusionId: number(illusionState[typeId] && illusionState[typeId].illusion_id),
        activeCount: active.length,
        active,
      };
    });
    const mountFigureConfig = window.Config && window.Config.PRELOAD_SERVER_CONFIG
      && window.Config.PRELOAD_SERVER_CONFIG.config_mount_figure || {};
    const appearanceCatalog = Object.keys(mountFigureConfig).map(key => mountFigureConfig[key]).filter(Boolean)
      .filter(value => number(value.career) === number(role.career))
      .map(value => {
        const typeId = number(value.type_id);
        const id = number(value.id);
        const active = !!(illusionSources[typeId] && illusionSources[typeId][id]);
        const goodsTypeId = number(value.goods_id);
        return {
          typeId,
          id,
          name: String(value.name || ''),
          active,
          activeStage: active ? number(illusionSources[typeId][id].stage) : 0,
          goodsTypeId,
          goodsRequired: number(value.goods_num),
          goodsOwned: goodsTypeId && goodsModel && goodsModel.GetTypeGoodsNum
            ? number(goodsModel.GetTypeGoodsNum(goodsTypeId)) : 0,
        };
      }).filter(value => value.typeId && value.id)
      .sort((left, right) => left.typeId - right.typeId || left.id - right.id);
    const namedFunctionChecks = {};
    for (const name of ['WingsComponentView', 'WingsIllusionView', 'WingsLvSystem']) {
      namedFunctionChecks[name] = !!(commonManager && commonManager.CheckFuncOpenState
        && commonManager.CheckFuncOpenState(name, true));
    }
    const wingCatalog = appearanceCatalog.filter(value => value.typeId === 3);
    const wingActiveCount = wingCatalog.filter(value => value.active).length;
    const wingLockedCount = wingCatalog.filter(value => !value.active).length;
    const figureList = asList(role.figure_list).map(value => scalarSummary(value, [
      'figure_type', 'figure_id', 'type_id', 'id', 'stage', 'star',
    ])).filter(value => Object.keys(value).length > 0);
    const bagItems = asList(goodsModel && goodsModel.bag_goods_list);
    const bagTypeCounts = {};
    for (const item of bagItems) {
      const typeId = number(item && item.type_id);
      if (!typeId) continue;
      bagTypeCounts[typeId] = number(bagTypeCounts[typeId]) + number(item.goods_num);
    }
    const inventoryTypes = Object.keys(bagTypeCounts).map(Number).sort((left, right) => left - right).map(typeId => ({
      typeId,
      count: bagTypeCounts[typeId],
    }));
    const reincarnationTasks = asList(taskModel && taskModel.GetReincarnationTaskList
      && taskModel.GetReincarnationTaskList()).map(value => scalarSummary(value, [
      'task_id', 'task_type', 'task_tips_type', 'has_finish', 'id', 'need_num', 'now_num',
      'show_num', 'task_name', 'desc', 'tips',
    ]));
    const reincarnationTaskConfig = window.Config && window.Config.PRELOAD_SERVER_CONFIG
      && window.Config.PRELOAD_SERVER_CONFIG.config_reincarnation_task_cfg || {};
    const taskCatalog = Object.keys(reincarnationTaskConfig).map(key => reincarnationTaskConfig[key]).filter(Boolean)
      .map(value => ({
        taskId: number(value.task_id),
        turn: number(value.turn),
        stage: number(value.stage),
        finishLevel: number(value.finish_lv),
      }))
      .filter(value => value.taskId && value.turn)
      .sort((left, right) => left.turn - right.turn || left.stage - right.stage || left.taskId - right.taskId);
    const reincarnationLevelConfig = window.Config && window.Config.PRELOAD_SERVER_CONFIG
      && window.Config.PRELOAD_SERVER_CONFIG.config_reincarnation_cfg || {};
    const career = number(role.career);
    const sex = number(role.sex);
    const levelRows = Object.keys(reincarnationLevelConfig).map(key => {
      const value = reincarnationLevelConfig[key];
      const parts = String(key).split('@').map(number);
      return value ? {
        key,
        career: parts[0],
        sex: parts[1],
        turn: number(value.turn || parts[2]),
        fullLevel: number(value.full_lv),
      } : null;
    }).filter(value => value && value.career === career && value.fullLevel > 0);
    const exactLevelRows = levelRows.filter(value => value.sex === sex);
    const selectedLevelRows = exactLevelRows.length ? exactLevelRows : levelRows;
    const fullLevelByTurn = {};
    for (const value of selectedLevelRows) {
      if (!Object.prototype.hasOwnProperty.call(fullLevelByTurn, value.turn)) {
        fullLevelByTurn[value.turn] = value.fullLevel;
      }
    }
    return {
      capturedAt: new Date().toISOString(),
      roleId: number(role.role_id),
      career,
      sex,
      level: number(role.level),
      turn: number(role.turn),
      turnStage: number(role.turn_stage),
      vipLevel,
      vip: {
        level: vipLevel,
        exp: vipExp,
        maxLevel: vipMaxLevel,
        targetLevelNeedExp: number(vipTargetConfig.need_gold),
      },
      currencies: {
        gold: number(role.jin),
        boundGold: number(role.jinLock),
        coin: number(role.tong),
        boundCoin: number(role.tongLock),
      },
      social: {
        guildId: number(role.guild_id),
        marriageId: number(role.marriage_id),
        isMarriage: !!role.is_marriage,
      },
      server: { openDay, worldLevel: number(role.worldLv) },
      functionOpen: {
        finishedCount: finishedIds.size,
        configuredCount: modules.length,
        unresolvedCount: unresolved.length,
        unresolved,
      },
      reincarnation: {
        roleTurn: number(role.turn),
        roleTurnStage: number(role.turn_stage),
        clientCurrentTurn: number(reincarnation && reincarnation.GetCurTurn && reincarnation.GetCurTurn()),
        clientCurrentStage: number(reincarnation && reincarnation.GetCurStage && reincarnation.GetCurStage()),
        clientMaxTurn: number(window.ReincarnationModel && window.ReincarnationModel.MaxTurn),
        tasks: reincarnationTasks,
        taskCatalog,
        fullLevelByTurn,
        fullLevelSource: exactLevelRows.length ? 'career-and-sex' : 'career-fallback',
      },
      appearance: { baseTypes, baseData, illusions, catalog: appearanceCatalog, roleFigures: figureList },
      routeProfiles: {
        roleOutwardWing: {
          entryChecks: namedFunctionChecks,
          baseActive: baseTypes.includes(3),
          configuredIllusionCount: wingCatalog.length,
          activeIllusionCount: wingActiveCount,
          lockedIllusionCount: wingLockedCount,
          activationReadyItemCount: wingCatalog.filter(value => !value.active
            && value.goodsTypeId && value.goodsOwned >= value.goodsRequired).length,
          pass: baseTypes.includes(3)
            && Object.values(namedFunctionChecks).every(Boolean)
            && wingActiveCount > 0 && wingLockedCount > 0,
        },
      },
      inventory: {
        occupiedBagEntries: bagItems.length,
        bagMaxCells: number(goodsModel && goodsModel.bag_goods_max_cell),
        distinctTypeCount: inventoryTypes.length,
        typeCounts: inventoryTypes,
      },
    };
  });
}

async function sendGmCommand(page, command) {
  return page.evaluate(value => {
    const Cheat = window.CheatModel;
    if (!Cheat || typeof Cheat.GetInstance !== 'function') throw new Error('CHEAT_MODEL_MISSING');
    const instance = Cheat.GetInstance();
    if (!instance || typeof instance.Fire !== 'function' || !Cheat.SEND_CHEAT_TO_SERVER) {
      throw new Error('CHEAT_MODEL_SEND_UNAVAILABLE');
    }
    instance.Fire(Cheat.SEND_CHEAT_TO_SERVER, value);
    return true;
  }, command);
}

async function refreshVipState(page) {
  const requested = await page.evaluate(() => {
    const Model = window.VipModel;
    const model = Model && Model.GetInstance && Model.GetInstance();
    if (!model || typeof model.Fire !== 'function' || !Model.REQUEST_PROTO) return false;
    model.Fire(Model.REQUEST_PROTO, 45000);
    return true;
  });
  if (requested) await sleep(400);
  return requested;
}

async function waitForState(page, predicate, timeoutMs, pollMs = 250) {
  const startedAt = Date.now();
  let state = await readAccountState(page);
  while (!predicate(state) && Date.now() - startedAt < timeoutMs) {
    await sleep(pollMs);
    state = await readAccountState(page);
  }
  return { pass: !!predicate(state), elapsedMs: Date.now() - startedAt, state };
}

async function waitForFunctionOpenStability(page, options = {}) {
  const timeoutMs = Number(options.timeoutMs || 120000);
  const stableMs = Number(options.stableMs || 5000);
  const startedAt = Date.now();
  let lastCount = -1;
  let stableSince = Date.now();
  let state = await readAccountState(page);
  while (Date.now() - startedAt < timeoutMs) {
    const count = Number(state.functionOpen && state.functionOpen.finishedCount || 0);
    if (count !== lastCount) {
      lastCount = count;
      stableSince = Date.now();
    }
    if (Date.now() - stableSince >= stableMs) {
      return { pass: true, elapsedMs: Date.now() - startedAt, stableMs, state };
    }
    await sleep(500);
    state = await readAccountState(page);
  }
  return { pass: false, elapsedMs: Date.now() - startedAt, stableMs, state };
}

async function openLegacySession(options) {
  const repoRoot = path.resolve(options.repoRoot);
  const session = new HeadlessUiSession({
    repoRoot,
    url: options.url,
    viewport: { width: 720, height: 1280 },
    snapshotSource: path.resolve(repoRoot, '..', 'yu_client', 'tools', 'yu-resource-tool', 'frontend', 'src', 'utils', 'pageSnapshot.js'),
  });
  const popupPolicy = loadPopupPolicy(path.join(repoRoot, 'Tools', 'UIAudit', 'policies', 'startup-popups.json'));
  const runtimeOverlayPolicy = loadRuntimeOverlayPolicy(path.join(repoRoot, 'Tools', 'UIAudit', 'policies', 'runtime-overlays.json'));
  try {
    await session.start();
    const itemUseHandler = options.controlledItemUse === true
      ? createControlledItemUseHandler(session) : undefined;
    await session.loginAndReachMainUi({
      account: options.account,
      password: options.password,
      popupPolicy,
      runtimeOverlayPolicy,
      allowBlockedReadOnly: options.allowBlockedReadOnly === true,
      itemUseHandler,
    });
    await refreshVipState(session.page);
    return session;
  } catch (error) {
    error.sessionEvents = session.events;
    await session.close().catch(() => {});
    throw error;
  }
}

async function executeTopUiRecipe(session, commands, gmPassword, options = {}) {
  const results = [];
  const progress = typeof options.onProgress === 'function' ? options.onProgress : () => {};
  await sendGmCommand(session.page, `setgmpassword_${gmPassword}`);
  await sleep(300);
  for (const entry of commands) {
    const startedAt = Date.now();
    const commandBefore = await readAccountState(session.page);
    const noRegression = state => Number(state.level) >= Number(commandBefore.level)
      && Number(state.turn) >= Number(commandBefore.turn);
    progress('gm-command-start', { command: entry.command, kind: entry.kind });
    let recoveredTask = false;
    let completedDuringRecovery = false;
    if (entry.kind === 'reincarnation-task') {
      const readiness = await waitForState(session.page, state => (state.reincarnation.tasks || [])
        .some(value => Number(value.task_id || value.id) === entry.taskId)
        || Number(state.turn) >= Number(entry.expected.turn), 2000);
      if (!readiness.pass) {
        const recoveryCommand = entry.recovery && entry.recovery.command;
        if (!recoveryCommand) {
          const failure = new Error(`GM_ACCOUNT_COMMAND_PRECONDITION_FAILED: ${entry.command} task-not-active`);
          failure.partialApplied = { results, state: readiness.state };
          throw failure;
        }
        progress('gm-task-recovery-start', { command: recoveryCommand, taskId: entry.taskId });
        await sendGmCommand(session.page, recoveryCommand);
        const recovered = await waitForState(session.page, state => (state.reincarnation.tasks || [])
          .some(value => Number(value.task_id || value.id) === entry.taskId)
          || Number(state.turn) >= Number(entry.expected.turn), 10000);
        recoveredTask = recovered.pass;
        completedDuringRecovery = Number(recovered.state.turn) >= Number(entry.expected.turn);
        progress('gm-task-recovery-finished', {
          command: recoveryCommand, taskId: entry.taskId, pass: recovered.pass,
          completedDuringRecovery,
        });
        if (!recovered.pass || !noRegression(recovered.state)) {
          const failure = new Error(`GM_ACCOUNT_TASK_RECOVERY_FAILED: ${entry.command}`);
          failure.partialApplied = { results, state: recovered.state };
          throw failure;
        }
      }
    }
    if (!completedDuringRecovery) await sendGmCommand(session.page, entry.command);
    if (entry.kind === 'vip') {
      await sleep(250);
      await refreshVipState(session.page);
    }
    let observation = null;
    if (entry.kind === 'level') {
      observation = await waitForState(session.page, state => state.level === entry.expected.level
        && noRegression(state), 30000);
    } else if (entry.kind === 'turn') {
      observation = await waitForState(session.page, state => state.turn >= entry.expected.turn
        && noRegression(state), 10000);
    } else if (entry.kind === 'reincarnation-task') {
      observation = await waitForState(session.page, state => state.turn >= entry.expected.turn
        && (state.turn > entry.expected.turn || state.turnStage === entry.expected.turnStage)
        && noRegression(state), 15000);
    } else if (entry.kind === 'vip') {
      observation = await waitForState(session.page, state => vipObservationSatisfied(state, entry.expected)
        && noRegression(state), 15000);
    } else if (entry.kind === 'money') {
      observation = await waitForState(session.page, state => state.currencies.gold === entry.expected.money
        && state.currencies.boundGold === entry.expected.money
        && state.currencies.coin === entry.expected.money && noRegression(state), 15000);
    } else if (entry.kind === 'appearance') {
      observation = await waitForState(session.page, state => (state.appearance.baseTypes || []).includes(entry.type)
        && noRegression(state), 10000);
    } else {
      await sleep(entry.asynchronous ? 750 : 350);
      const state = await readAccountState(session.page);
      observation = { pass: noRegression(state), elapsedMs: Date.now() - startedAt, state };
    }
    const result = {
      command: entry.command,
      kind: entry.kind,
      type: entry.type || null,
      purpose: entry.purpose || null,
      recoveredTask,
      completedDuringRecovery,
      asynchronous: !!entry.asynchronous,
      elapsedMs: Date.now() - startedAt,
      observed: observation ? {
        pass: observation.pass,
        elapsedMs: observation.elapsedMs,
        state: {
          level: observation.state.level,
          turn: observation.state.turn,
          turnStage: observation.state.turnStage,
          vipLevel: observation.state.vipLevel,
          vipExp: observation.state.vip && observation.state.vip.exp,
        },
      } : null,
    };
    results.push(result);
    progress('gm-command-finished', result);
    if (observation && !observation.pass) {
      const failure = new Error(`GM_ACCOUNT_COMMAND_OBSERVATION_FAILED: ${entry.command}`);
      failure.partialApplied = { results, state: observation.state };
      throw failure;
    }
  }
  const stability = await waitForFunctionOpenStability(session.page, { timeoutMs: 20000, stableMs: 3000 });
  return { results, stability, state: stability.state };
}

async function runTopUiAccount(options) {
  const repoRoot = path.resolve(options.repoRoot || path.join(__dirname, '..', '..', '..'));
  const outputDir = path.resolve(options.outputDir);
  const account = String(options.account || '');
  const password = String(options.password || '');
  const url = options.url || 'http://127.0.0.1:8091/index.html';
  if (!account) throw new Error('MISSING_ARGUMENT: account');
  if (!password) throw new Error('MISSING_ARGUMENT: password');
  if (fs.existsSync(outputDir)) throw new Error(`IMMUTABLE_EVIDENCE_EXISTS: ${outputDir}`);
  fs.mkdirSync(outputDir, { recursive: true });
  const startedAt = new Date().toISOString();
  const progressPath = path.join(outputDir, 'gm-account-progress.json');
  const progressEvents = [];
  let ensuredServer = null;
  if (options.ensureServer) {
    const profile = findServerProfileForUrl(url);
    if (!profile) throw new Error(`SERVER_PROFILE_NOT_FOUND: ${url}`);
    ensuredServer = await ensureServer({ repoRoot, profile });
  }

  let firstSession = null;
  let verificationSession = null;
  let before = null;
  let applied = null;
  let fresh = null;
  let commands = [];
  let error = null;
  let vipAuthority = null;
  let firstSessionEvents = [];
  let verificationSessionEvents = [];
  const progress = (phase, detail = {}) => {
    const event = { at: new Date().toISOString(), phase, ...detail };
    progressEvents.push(event);
    writeJsonAtomic(progressPath, {
      schema: 1,
      id: 'ui-audit.gm-account.progress.v1',
      account,
      passwordRecorded: false,
      gmPasswordRecorded: false,
      startedAt,
      latest: event,
      events: progressEvents,
    }, { overwrite: true });
    if (typeof options.onProgress === 'function') options.onProgress(event);
  };
  try {
    vipAuthority = readVipAuthority(repoRoot);
    progress('vip-authority-verified', {
      targetLevel: vipAuthority.targetLevel,
      targetConfigExp: vipAuthority.targetConfigExp,
      conversion: vipAuthority.conversion,
    });
    progress('login-before-start', { account });
    firstSession = await openLegacySession({
      repoRoot, url, account, password,
      allowBlockedReadOnly: options.allowBlockedReadOnly === true && !options.apply,
      controlledItemUse: true,
    });
    progress('login-before-ready', { account });
    before = await readAccountState(firstSession.page);
    progress('snapshot-before', { level: before.level, turn: before.turn, vipLevel: before.vipLevel });
    commands = buildTopUiCommands(before);
    if (options.apply) {
      progress('gm-apply-start', { commandCount: commands.length });
      const gmPassword = readGmPassword(path.resolve(repoRoot, '..', 'yu_server', 'config', 'gsrv.config'));
      applied = await executeTopUiRecipe(firstSession, commands, gmPassword, { onProgress: progress });
      progress('gm-apply-finished', { commandCount: commands.length });
      await firstSession.close();
      firstSessionEvents = [...firstSession.events];
      firstSession = null;
      progress('fresh-login-start', { account });
      verificationSession = await openLegacySession({
        repoRoot, url, account, password, controlledItemUse: true,
      });
      fresh = await readAccountState(verificationSession.page);
      progress('fresh-snapshot', { level: fresh.level, turn: fresh.turn, vipLevel: fresh.vipLevel });
    }
  } catch (caught) {
    error = caught;
    if (caught && caught.partialApplied) applied = caught.partialApplied;
    progress('failed', { error: String(caught && caught.message || caught) });
  } finally {
    if (firstSession) {
      await firstSession.close().catch(() => {});
      firstSessionEvents = [...firstSession.events];
    }
    if (verificationSession) {
      await verificationSession.close().catch(() => {});
      verificationSessionEvents = [...verificationSession.events];
    }
  }

  const verification = fresh ? verifyTopUiState(fresh) : null;
  progress('finished', { pass: verification ? verification.pass : null, error: !!error });
  const report = {
    schema: 2,
    id: 'ui-audit.gm-account.top-ui.v2',
    account,
    apply: !!options.apply,
    passwordRecorded: false,
    gmPasswordRecorded: false,
    authCommand: options.apply ? 'setgmpassword_<redacted>' : null,
    startedAt,
    finishedAt: new Date().toISOString(),
    source: 'legacy-h5-real-runtime',
    readOnlyBlockedSnapshot: !options.apply
      ? firstSessionEvents.find(event => event.kind === 'login-read-only-blocked-snapshot') || null
      : null,
    server: ensuredServer ? { profileId: ensuredServer.profile.id, code: ensuredServer.code } : null,
    recipe: {
      target: TOP_UI_LIMITS,
      vipAuthority,
      commands,
      excluded: [
        'setlv (breaks normal level-up combat chain)',
        'opday/worldlv/mergeday (server-wide state)',
        'completeachv/claim protocols (destroys useful claimable UI states)',
        'guild/marriage/activity schedule (separate scenario recipes)',
      ],
      reset: 'no automatic rollback; this is the persistent top UI capture account',
    },
    before,
    applied,
    freshSessionAfter: fresh,
    verification,
    residualLimits: [
      '开服天数、活动时间和跨服状态仍由服务器环境决定',
      '公会、婚姻、组队等社交页面仍需独立状态配方',
      '未批量完成成就、领取奖励或灌入所有道具，以保留可测试状态',
    ],
    failureDiagnostic: error && error.diagnostic || null,
    failureSessionEvents: error && error.sessionEvents || null,
    itemUseDismissals: {
      beforeSession: firstSessionEvents.filter(event => event.kind === 'item-use-controlled-dismiss'),
      freshSession: verificationSessionEvents.filter(event => event.kind === 'item-use-controlled-dismiss'),
    },
    progressFile: progressPath,
    error: error ? String(error && error.stack || error) : null,
  };
  const reportPath = writeJsonAtomic(path.join(outputDir, 'gm-account-report.json'), report);
  if (error) {
    error.reportPath = reportPath;
    throw error;
  }
  if (options.apply && (!verification || !verification.pass)) {
    const failure = new Error(`GM_ACCOUNT_FRESH_VERIFICATION_FAILED: ${JSON.stringify(verification)}`);
    failure.reportPath = reportPath;
    throw failure;
  }
  return { ...report, reportPath };
}

module.exports = {
  TOP_UI_LIMITS,
  buildTopUiCommands,
  verifyTopUiState,
  vipObservationSatisfied,
  readGmPassword,
  readVipAuthority,
  readAccountState,
  sendGmCommand,
  refreshVipState,
  waitForFunctionOpenStability,
  runTopUiAccount,
};
