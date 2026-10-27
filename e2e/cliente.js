// Recorrido automático de la interfaz en un navegador con tamaño de teléfono (Playwright).
// Requiere la API en http://localhost:5276 con los datos de ejemplo y el host web en http://localhost:5190
// (ver e2e/README.md). Las capturas quedan en e2e/capturas (o en CAPTURAS).
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const WEB = process.env.WEB_URL || 'http://localhost:5190';
const SHOTS = process.env.CAPTURAS || path.join(__dirname, 'capturas');
const ESC = __dirname;
const SUFIJO = Date.now().toString(36);   // nombres únicos: el recorrido se puede repetir sobre la misma base
fs.mkdirSync(SHOTS, { recursive: true });

(async () => {
  const browser = await chromium.launch();
  const context = await browser.newContext({ viewport: { width: 390, height: 844 }, deviceScaleFactor: 1, isMobile: true, hasTouch: true, acceptDownloads: true });
  const page = await context.newPage();
  const problemas = [];
  page.on('console', m => { if (['error', 'warning'].includes(m.type())) problemas.push(`[${m.type()}] ${m.text()}`); });
  page.on('pageerror', e => problemas.push('[pageerror] ' + e.message));
  page.on('requestfailed', r => problemas.push('[requestfailed] ' + r.url() + ' ' + r.failure()?.errorText));

  const paso = async (nombre) => { await page.screenshot({ path: `${SHOTS}/${nombre}.png` }); console.log('OK  ' + nombre); };

  await page.goto(WEB);
  await page.waitForSelector('text=Ingrese para ver el catálogo', { timeout: 60000 });
  await paso('01-login');

  // Pantalla "Servidor": guardar y probar la conexión con la API.
  await page.click('a:has-text("Servidor:")');
  await page.waitForSelector('h1:has-text("Servidor")');
  await page.click('button:has-text("Guardar y probar")');
  await page.waitForSelector('.alerta-exito:has-text("Conexión correcta")');
  await paso('01b-servidor');
  await page.fill('#url', 'http://localhost:9');
  await page.click('button:has-text("Guardar y probar")');
  await page.waitForSelector('.alerta-error:has-text("No se pudo conectar")');
  await paso('01c-servidor-error');
  await page.click('button:has-text("Restablecer")');
  await page.waitForSelector('.alerta-exito:has-text("predeterminada")');
  await page.goto(WEB);
  await page.waitForSelector('text=Ingrese para ver el catálogo');

  // Credenciales incorrectas
  await page.fill('#usuario', 'cliente');
  await page.fill('#password', 'mala');
  await page.click('button:has-text("Ingresar")');
  await page.waitForSelector('text=incorrectos');
  await paso('02-login-error');

  // Login correcto
  await page.fill('#password', 'Demo1234');
  await page.click('button:has-text("Ingresar")');
  await page.waitForSelector('.grilla-productos');
  await page.waitForTimeout(500);
  await paso('03-catalogo');

  // Búsqueda
  await page.fill('input[type=search]', 'auricul');
  await page.press('input[type=search]', 'Enter');
  await page.waitForSelector('.tarjeta-titulo:has-text("Auriculares")');
  const resultados = await page.locator('.tarjeta-producto').count();
  console.log('Resultados de "auricul":', resultados);
  await paso('04-busqueda');

  // Limpiar y filtrar por disponibles / orden
  await page.fill('input[type=search]', '');
  await page.press('input[type=search]', 'Enter');
  await page.waitForTimeout(600);
  await page.selectOption('select[aria-label="Ordenar por"]', 'precio_asc');
  await page.waitForTimeout(600);
  const primerPrecio = await page.locator('.tarjeta-producto .precio').first().innerText();
  console.log('Primer precio (asc):', primerPrecio);
  await paso('05-orden-precio');

  // Producto sin stock: botón deshabilitado
  const sinStock = page.locator('.tarjeta-producto', { hasText: 'Teclado inalámbrico' });
  console.log('Teclado deshabilitado:', await sinStock.locator('button').isDisabled());

  // Detalle de producto
  await page.fill('input[type=search]', 'Auriculares');
  await page.press('input[type=search]', 'Enter');
  await page.waitForSelector('.tarjeta-titulo:has-text("Auriculares")');
  await page.click('.tarjeta-enlace:has-text("Auriculares")');
  await page.waitForSelector('text=Agregar al carrito');
  await paso('06-detalle');
  await page.click('button[aria-label="Más"]');
  await page.click('button:has-text("Agregar al carrito")');
  await page.waitForSelector('.notificacion');
  await paso('07-agregado');

  // Carrito
  await page.click('.tab:has-text("Carrito")');
  await page.waitForSelector('h1:has-text("Mi carrito")');
  await page.waitForSelector('text=Auriculares Bluetooth');
  await paso('08-carrito');
  console.log('Total carrito:', await page.locator('.total').innerText());

  // Checkout con tarjeta rechazada
  await page.click('button:has-text("Continuar con el pago")');
  await page.waitForSelector('text=Medio de pago');
  await page.click('label.opcion-pago:has-text("Tarjeta de crédito")');
  await page.fill('#tarjeta', '4000 0000 0000 0000');
  await page.click('button:has-text("Pagar")');
  await page.waitForSelector('text=rechazado');
  await paso('09-pago-rechazado');

  // Checkout aprobado
  await page.fill('#tarjeta', '4242 4242 4242 4242');
  await page.click('button:has-text("Pagar")');
  await page.waitForSelector('text=Compra realizada');
  await page.waitForTimeout(300);
  await paso('10-pedido-confirmado');

  // Comprobante PDF
  const [descarga] = await Promise.all([page.waitForEvent('download'), page.click('button:has-text("Comprobante PDF")')]);
  await descarga.saveAs(path.join(SHOTS, 'comprobante.pdf'));
  console.log('Comprobante descargado:', descarga.suggestedFilename());

  // Enviar por correo (sin SMTP => modo archivo)
  await page.click('button:has-text("Enviar por correo")');
  await page.waitForSelector('text=SMTP');
  await paso('11-correo');

  // Mis pedidos
  await page.click('.tab:has-text("Pedidos")');
  await page.waitForSelector('.fila');
  await paso('12-mis-pedidos');

  // Cuenta
  await page.click('.tab:has-text("Cuenta")');
  await page.waitForSelector('text=Mis datos');
  await paso('13-cuenta');

  // Carrito vacío tras la compra y el panel de personal no aparece
  console.log('Tab Panel visible para cliente:', await page.locator('.tab:has-text("Panel")').count());
  await page.click('.tab:has-text("Carrito")');
  await page.waitForSelector('text=Su carrito está vacío');

  // Acceso a pantalla de personal por URL: debe negarse
  await page.goto(WEB + '/admin/productos');
  await page.waitForSelector('text=Acceso restringido', { timeout: 30000 });
  await paso('14-acceso-restringido');

  // Cerrar sesión
  await page.click('.tab:has-text("Cuenta")');
  await page.click('button:has-text("Cerrar sesión")');
  await page.waitForSelector('text=Ingrese para ver el catálogo');
  await paso('15-logout');

  console.log('\nProblemas de consola/red:', problemas.length);
  problemas.slice(0, 15).forEach(p => console.log('  ' + p));
  await browser.close();
})().catch(e => { console.error('FALLÓ:', e.message); process.exit(1); });
