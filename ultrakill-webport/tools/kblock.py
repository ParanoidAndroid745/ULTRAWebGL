#!/usr/bin/env python3
"""
kblock.py - Test del bloqueo de teclado del port WebGL de ULTRAKILL.

Chrome/Edge solo suprimen los atajos del navegador (Ctrl+D, Ctrl+T, Ctrl+W)
si el bloqueo de teclado esta ACTIVO (navigator.keyboard.lock). Si esta
inactivo, el navegador se come la pulsacion y la pagina NO recibe el keydown.

Por eso el test mete un listener en la pagina: si tras pulsar Ctrl+D el evento
llega a la pagina, el bloqueo estaba activo.

Escenarios:
  1. tras un clic (gesto real)
  2. tras pulsar Escape (Chrome suelta el bloqueo al pulsar Esc)
  3. tras perder el foco de la ventana y volver

Uso:
  python kblock.py --url http://localhost:8000/?v=205 [--headed]
"""
import argparse, json, os, sys, time

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
            ok = page.evaluate(
                "() => { const c = document.querySelector('#unityContainer canvas');"
                " return !!(c && c.width > 0 && window.unityInstance); }")
            if ok:
                return True
        except Exception:
            pass
        page.wait_for_timeout(500)
    return False


def ctrl_d_reaches_page(page):
    """Pulsa Ctrl+D (evento de confianza) y dice si la pagina lo recibio."""
    page.evaluate("window.__kbHits = [];")
    try:
        page.keyboard.press("Control+d")
    except Exception as e:
        return "error:%s" % e
    page.wait_for_timeout(700)
    hits = page.evaluate("window.__kbHits || [];")
    got = any(h.get('ctrl') and (h.get('key') or '').lower() == 'd' for h in hits)
    return "PAGINA_RECIBIO" if got else "CHROME_LO_COMIO"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--url', default=DEFAULT_URL)
    ap.add_argument('--headed', action='store_true')
    args = ap.parse_args()

    from playwright.sync_api import sync_playwright
    out = {}
    with sync_playwright() as p:
        browser = p.chromium.launch(
            headless=not args.headed,
            args=[
                "--use-gl=angle",
                "--use-angle=swiftshader",
                "--enable-unsafe-swiftshader",
                "--autoplay-policy=no-user-gesture-required",
                "--window-size=1280,800",
            ])
        ctx = browser.new_context(viewport={"width": 1280, "height": 800})
        page = ctx.new_page()
        errors = []
        page.on('console', lambda m: errors.append(m.text) if m.type == 'error' else None)
        page.on('pageerror', lambda e: errors.append(str(e)))
        page.goto(args.url, wait_until='domcontentloaded')
        ok = wait_for_canvas(page)
        out['canvas_ok'] = ok
        print('canvas listo:', ok, flush=True)
        if not ok:
            print('errores:', errors[:5])
            browser.close()
            return 1
        page.evaluate(LISTEN_JS)
        page.wait_for_timeout(2000)

        out['soporta_api'] = page.evaluate(
            "() => !!(navigator.keyboard && navigator.keyboard.lock)")

        # 1) gesto real: clic en el canvas
        page.mouse.click(640, 400)
        page.wait_for_timeout(1200)
        out['1_tras_clic'] = ctrl_d_reaches_page(page)

        # 2) Escape (Chrome suelta el bloqueo de teclado)
        page.keyboard.press('Escape')
        page.wait_for_timeout(1500)
        out['2_tras_escape'] = ctrl_d_reaches_page(page)

        # 3) reanudar con un clic y volver a probar
        page.mouse.click(640, 400)
        page.wait_for_timeout(1200)
        out['3_tras_clic_de_resume'] = ctrl_d_reaches_page(page)

        # 4) perder el foco de la ventana y volver
        page2 = ctx.new_page()
        page2.goto('about:blank')
        page2.bring_to_front()
        page.wait_for_timeout(1200)
        page.bring_to_front()
        page.wait_for_timeout(800)
        out['4_tras_perder_foco'] = ctrl_d_reaches_page(page)

        # 5) tras un gesto nuevo con el foco devuelto
        page.mouse.click(640, 400)
        page.wait_for_timeout(1200)
        out['5_tras_clic_final'] = ctrl_d_reaches_page(page)

        out['estado_pagina'] = page.evaluate("""() => ({
            fullscreen: !!document.fullscreenElement,
            pointerLock: !!document.pointerLockElement,
            cursor: (document.querySelector('#unityContainer canvas')||{}).style ? getComputedStyle(document.querySelector('#unityContainer canvas')).cursor : null,
            status: (document.getElementById('uk-fs-status')||{}).textContent || null,
        })""")
        out['errores_consola'] = errors[:10]
        browser.close()

    print(json.dumps(out, indent=1, ensure_ascii=False))
    return 0


if __name__ == '__main__':
    sys.exit(main())
