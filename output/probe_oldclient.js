// P1 old-client runtime probe (scratch, gitignored). Captures console/network/page state.
const { chromium } = require('playwright');

(async () => {
  const url = 'http://127.0.0.1:8090/index.html';
  const consoleMsgs = [];
  const pageErrors = [];
  const failedReqs = [];
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 720, height: 1280 } });
  const page = await ctx.newPage();
  page.on('console', m => consoleMsgs.push(`[${m.type()}] ${m.text()}`.slice(0, 300)));
  page.on('pageerror', e => pageErrors.push(String(e).slice(0, 300)));
  page.on('requestfailed', r => failedReqs.push(`${r.failure()?.errorText || '?'} ${r.url()}`.slice(0, 200)));

  let status = 'n/a';
  try {
    const resp = await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30000 });
    status = resp ? resp.status() : 'no-response';
  } catch (e) {
    console.log('GOTO_ERROR ' + String(e).slice(0, 200));
  }

  // Let the Laya game attempt to boot/connect.
  await page.waitForTimeout(20000);

  let bodyText = '';
  try { bodyText = (await page.innerText('body')).replace(/\s+/g, ' ').trim().slice(0, 600); } catch {}
  let title = '';
  try { title = await page.title(); } catch {}
  // Laya often renders to canvas; capture canvas presence + any visible DOM text.
  let canvasInfo = 'none';
  try {
    canvasInfo = await page.evaluate(() => {
      const cs = Array.from(document.querySelectorAll('canvas'));
      return cs.map(c => `${c.width}x${c.height}`).join(',') || 'none';
    });
  } catch {}

  await page.screenshot({ path: 'output/oldclient_probe.png', fullPage: false });

  console.log('=== HTTP_STATUS ' + status);
  console.log('=== TITLE ' + title);
  console.log('=== CANVAS ' + canvasInfo);
  console.log('=== BODY_TEXT ' + (bodyText || '(empty)'));
  console.log('=== CONSOLE (' + consoleMsgs.length + ') ===');
  consoleMsgs.slice(0, 40).forEach(m => console.log(m));
  console.log('=== PAGE_ERRORS (' + pageErrors.length + ') ===');
  pageErrors.slice(0, 20).forEach(m => console.log(m));
  console.log('=== FAILED_REQUESTS (' + failedReqs.length + ') ===');
  failedReqs.slice(0, 30).forEach(m => console.log(m));

  await browser.close();
})();
