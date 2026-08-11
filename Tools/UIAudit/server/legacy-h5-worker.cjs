#!/usr/bin/env node
'use strict';

const path = require('path');

function requiredArg(name) {
  const index = process.argv.indexOf(`--${name}`);
  if (index < 0 || !process.argv[index + 1]) throw new Error(`MISSING_WORKER_ARGUMENT: --${name}`);
  return process.argv[index + 1];
}

async function main() {
  const h5Root = path.resolve(requiredArg('cwd'));
  const staticRoot = path.resolve(requiredArg('static-root'));
  const host = requiredArg('host');
  const port = Number(requiredArg('port'));
  const ownerToken = requiredArg('owner-token');
  process.chdir(h5Root);
  process.title = `shenxiao-ui-audit-legacy-h5-${ownerToken}`;

  const webpack = require(path.join(h5Root, 'node_modules', 'webpack'));
  const express = require(path.join(h5Root, 'node_modules', 'express'));
  const middleware = require(path.join(h5Root, 'node_modules', 'webpack-dev-middleware'));
  const compression = require(path.join(h5Root, 'node_modules', 'compression'));
  const config = require(path.join(h5Root, 'egf.config.js'));
  const outputPath = path.join(h5Root, config.binDir || 'bin');
  const webpackConfig = {
    stats: { colors: false, modules: false },
    entry: path.join(h5Root, config.entry || './src/Main.ts'),
    target: 'web',
    mode: 'development',
    context: h5Root,
    devtool: config.sourceMapType === undefined ? 'eval-source-map' : config.sourceMapType,
    output: {
      path: outputPath,
      filename: config.jsOutputPath || 'js/bundle.js',
      publicPath: '/',
    },
    module: {
      rules: [{
        test: /\.tsx?$/,
        loader: require.resolve('esbuild-loader', { paths: [path.join(h5Root, 'node_modules')] }),
        options: { loader: 'tsx', target: 'es2017' },
      }],
    },
    resolve: { extensions: ['.ts', '.js', '.json'] },
    resolveLoader: { modules: [path.join(h5Root, 'node_modules')] },
    watchOptions: {
      poll: config.watchPoll === undefined ? 200 : config.watchPoll,
      ignored: config.watchIgnored || ['**/node_modules/**', '**/*.js', '**/*.d.ts'],
    },
    plugins: [],
  };
  const compiler = webpack(webpackConfig);
  const app = express();
  app.disable('x-powered-by');
  app.use((request, response, next) => {
    response.setHeader('Access-Control-Allow-Origin', '*');
    response.setHeader('Cache-Control', 'no-store, no-cache, must-revalidate');
    next();
  });
  app.use(compression());
  app.use(middleware(compiler, {
    publicPath: '/',
    stats: { colors: false, modules: false },
    writeToDisk: false,
    watchOptions: webpackConfig.watchOptions,
  }));
  app.use(express.static(staticRoot, { etag: false, lastModified: false, maxAge: 0 }));
  const server = app.listen(port, host, () => {
    process.stdout.write(`${JSON.stringify({ event: 'listening', host, port, ownerToken, cwd: h5Root, staticRoot })}\n`);
  });
  const close = signal => {
    process.stdout.write(`${JSON.stringify({ event: 'stopping', signal })}\n`);
    server.close(() => {
      if (typeof compiler.close === 'function') compiler.close(() => process.exit(0));
      else process.exit(0);
    });
    setTimeout(() => process.exit(1), 5000).unref();
  };
  process.on('SIGINT', () => close('SIGINT'));
  process.on('SIGTERM', () => close('SIGTERM'));
}

main().catch(error => {
  process.stderr.write(`${error && error.stack || error}\n`);
  process.exitCode = 1;
});
