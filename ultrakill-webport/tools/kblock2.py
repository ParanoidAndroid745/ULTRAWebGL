#!/usr/bin/env python3
"""
kblock2.py - Test del bloqueo de teclado en un Chromium REAL (headed).

En Chrome el bloqueo de teclado (navigator.keyboard.lock) solo se concede en
pantalla completa iniciada por JavaScript. Por eso hay que comparar:
   A) sin fullscreen  -> la peticion de lock() falla (NotAllowedError)
   B) con fullscreen  -> la peticion se concede y Chrome deja de tratar
                         Ctrl+D como atajo del navegador (la pagina lo recibe)

Uso: python kblock2.py --url http://localhost:8000/?v=205
"""
import argparse, json, sys, time

DEFAULT_URL = "http://localhost:8000/?v=205"

LISTEN_JS = """
window.__kbHits = [];
document.addEventListener('keydown', function (e) {
  window.__kbHits.push({key: e.key, ctrl: e.ctrlKey, code: e.code});
}, true);
"""


def wait_for_canvas(page, timeout=120):
    t0 = time.time()
    while time.time() - t0 < timeout:
        try:
            if page.evaluate("() => { const c = document.querySelector('#unityContainer canvas');"
                             " return !!(c && c.width > 0 && window.unityInstance); }"):
                return True
        except Exception:
            pass
        page.wait_for_timeout(500)
    return False


def ctrl_d(page):
    page.evaluate("window.__kbHits = [];")
    try:
        page.keyboard.press("Control+d")
    except Exception as e:
        return "error:%s" % e
    page.wait_for_timeout(800)
    hits = page.evaluate("window.__kbHits || [];")
    got = any(h.get('ctrl') and (h.get('key') or '').lower() == 'd' for h in hits)
    return "PAGINA_RECIBIO (lock activo)" if got else "CHROME_LO_COMIO (lock INACTIVO)"


def lock_call(page):
    """Llamada directa a navigator.keyboard.lock() y su resultado."""
    return page.evaluate("""async () => {
        if (!navigator.keyboard || !navigator.keyboard.lock) return 'sin API';
        try { await navigator.keyboard.lock(); return 'CONCEDIDO'; }
        catch (e) { return 'RECHAZADO: ' + (e && e.name ? e.name : e); }
    }""")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--url', default=DEFAULT_URL)
    args = ap.parse_args()

    from playwright.sync_api import sync_playwright
    out = {}
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=False, args=[
            "--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader",
            "--autoplay-policy=no-user-gesture-required", "--window-size=1280,800"])
        ctx = browser.new_context(viewport={"width": 1280, "height": 800})
        page = ctx.new_page()
        errors = []
        page.on('pageerror', lambda e: errors.append(str(e)))
        page.goto(args.url, wait_until='domcontentloaded')
        if not wait_for_canvas(page):
            print('no arranco'); browser.close(); return 1
        page.evaluate(LISTEN_JS)
        page.wait_for_timeout(1500)

        out['A_sin_fullscreen_llamada'] = lock_call(page)
        out['A_sin_fullscreen_ctrlD'] = ctrl_d(page)
        out['A_fullscreen'] = page.evaluate("!!document.fullscreenElement")

        # Entrar en fullscreen con un gesto real (el boton de la pagina)
        page.evaluate("window.__kbHits = [];")
        try:
            page.click('#uk-fs-btn', timeout=5000)
        except Exception:
            page.evaluate("window.UKFullscreen && window.UKFullscreen.toggle();")
        page.wait_for_timeout(2500)
        out['B_fullscreen'] = page.evaluate("!!document.fullscreenElement")
        out['B_con_fullscreen_llamada'] = lock_call(page)
        out['B_con_fullscreen_ctrlD'] = ctrl_d(page)

        # Escape (pausa): Chrome suelta el bloqueo
        page.keyboard.press('Escape')
        page.wait_for_timeout(1500)
        out['C_tras_escape_fullscreen'] = page.evaluate("!!document.fullscreenElement")
        out['C_tras_escape_llamada'] = lock_call(page)
        out['C_tras_escape_ctrlD'] = ctrl_d(page)

        out['errores'] = errors[:5]
        browser.close()
    print(json.dumps(out, indent=1, ensure_ascii=False))
    return 0


if __name__ == '__main__':
    sys.exit(main())
