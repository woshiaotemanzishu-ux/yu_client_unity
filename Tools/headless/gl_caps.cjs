// 查无头环境 WebGL2 压缩纹理扩展支持(判断内存探针数据是否失真)
const puppeteer = require('puppeteer');
(async () => {
  const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox', '--enable-webgl', '--use-gl=angle'] });
  const page = await browser.newPage();
  const caps = await page.evaluate(() => {
    const c = document.createElement('canvas');
    const gl = c.getContext('webgl2');
    if (!gl) return { webgl2: false };
    return {
      webgl2: true,
      renderer: (() => { const d = gl.getExtension('WEBGL_debug_renderer_info'); return d ? gl.getParameter(d.UNMASKED_RENDERER_WEBGL) : gl.getParameter(gl.RENDERER); })(),
      s3tc: !!gl.getExtension('WEBGL_compressed_texture_s3tc'),
      etc2: (() => { try { return gl.getSupportedExtensions().some(e => e.includes('etc')); } catch (e) { return false; } })(),
      astc: !!gl.getExtension('WEBGL_compressed_texture_astc'),
      maxTex: gl.getParameter(gl.MAX_TEXTURE_SIZE),
    };
  });
  console.log(JSON.stringify(caps));
  await browser.close();
})();
