#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');
const { versionInfo } = require('./lib/version.cjs');
const { runPreflight, assertPreflight } = require('./lib/preflight.cjs');
const { loadPopupPolicy } = require('./lib/popup-policy.cjs');
const { loadProtocolPolicy } = require('./lib/protocol-probe.cjs');
const { runRoute } = require('./lib/route-runner.cjs');
const { serverStatus, startServer, stopServer } = require('./lib/server-lifecycle.cjs');
const { safeStringify, writeJsonAtomic } = require('./lib/safe-json.cjs');

function parseArgs(argv) {
  const result = { _: [] };
  for (let index = 0; index < argv.length; index++) {
    const value = argv[index];
    if (!value.startsWith('--')) result._.push(value);
    else {
      const key = value.slice(2);
      const next = argv[index + 1];
      result[key] = next && !next.startsWith('--') ? argv[++index] : true;
    }
  }
  return result;
}

function required(args, name) {
  if (!args[name] || args[name] === true) throw new Error(`MISSING_ARGUMENT: --${name}`);
  return args[name];
}

function resolvePolicy(routePath, route, key, fallback) {
  const value = route[key] || fallback;
  return path.isAbsolute(value) ? value : path.resolve(path.dirname(routePath), value);
}

async function main(argv = process.argv.slice(2)) {
  const args = parseArgs(argv);
  const command = args._[0] || 'help';
  if (command === 'version') {
    process.stdout.write(`${safeStringify(versionInfo())}\n`);
    return 0;
  }
  if (command === 'preflight') {
    const routePath = path.resolve(required(args, 'route'));
    const outputDir = path.resolve(required(args, 'output'));
    const route = JSON.parse(fs.readFileSync(routePath, 'utf8'));
    const popupPolicy = loadPopupPolicy(resolvePolicy(routePath, route, 'popupPolicy', '../policies/startup-popups.json'));
    const protocolPolicy = loadProtocolPolicy(resolvePolicy(routePath, route, 'protocolPolicy', '../policies/protocols.json'));
    const result = await runPreflight({ routePath, route, outputDir, popupPolicy, protocolPolicy });
    if (args.report && args.report !== true) writeJsonAtomic(path.resolve(args.report), result);
    process.stdout.write(`${safeStringify(result)}\n`);
    assertPreflight(result);
    return 0;
  }
  if (command === 'run') {
    const report = await runRoute({
      routePath: required(args, 'route'),
      outputDir: required(args, 'output'),
      account: args.account === true ? null : args.account,
      password: args.password === true ? null : args.password,
      ensureServer: args['ensure-server'] === true,
    });
    process.stdout.write(`${safeStringify({ status: report.status, route: report.route, report: path.join(path.resolve(args.output), 'ui-audit-report.json') })}\n`);
    return 0;
  }
  if (command === 'server') {
    const action = args._[1] || 'status';
    const options = { profileId: args.profile === true ? undefined : args.profile };
    let result;
    if (action === 'status') result = await serverStatus(options);
    else if (action === 'start') result = await startServer(options);
    else if (action === 'stop') result = await stopServer(options);
    else throw new Error(`UNSUPPORTED_SERVER_ACTION: ${action}`);
    process.stdout.write(`${safeStringify(result)}\n`);
    if ((action === 'start' || action === 'stop') && !result.pass) return 1;
    return 0;
  }
  process.stdout.write('Usage:\n  node Tools/UIAudit/cli.cjs version\n  node Tools/UIAudit/cli.cjs server <status|start|stop> [--profile legacy-h5-local]\n  node Tools/UIAudit/cli.cjs preflight --route <route.json> --output <output/new-run> [--report <file>]\n  node Tools/UIAudit/cli.cjs run [--ensure-server] --route <route.json> --output <output/new-run>\n');
  return command === 'help' ? 0 : 1;
}

if (require.main === module) {
  main().then(code => { process.exitCode = code; }).catch(error => {
    process.stderr.write(`${error && error.stack || error}\n`);
    process.exitCode = 1;
  });
}

module.exports = { parseArgs, main };
