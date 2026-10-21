#!/usr/bin/env python3
"""
kblock3.py - Test del flujo real: cargar, clic (auto-fullscreen), jugar,
pausa con Escape y comprobar que los atajos del navegador siguen bloqueados.

En Chromium, si el bloqueo de teclado esta activo, Ctrl+D LLEGA a la pagina
(el navegador no lo consume como atajo). Si esta inactivo, se lo come.
"""
import argparse, json, sys, time

DEFAULT_URL = "http://localhost:8000/?v=207"


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
    page.keyboard.press("Control+d")
    page.wait_for_timeout(800)
    hits = page.evaluate("window.__kbHits || [];")
    got = any(h.get('ctrl') and (h.get('key') or '').lower() == 'd' for h in hits)
    return "PAGINA_RECIBIO (lock activo, atajo anulado)" if got else "CHROME_LO_COMIO (lock INACTIVO)"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--url', default=DEFAULT_URL)
    args = ap.parse_args()
    from playwright.sync_api import sync_playwright
    out = {}
    with sync_playwright() as p:
        b = p.chromium.launch(headless=False, args=[
            "--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader",
            "--autoplay-policy=no-user-gesture-required", "--window-size=1280,800"])
        ctx = b.new_context(viewport={"width": 1280, "height": 800})
        page = ctx.new_page()
        errs = []
        page.on('pageerror', lambda e: errs.append(str(e)))
        page.goto(args.url, wait_until='domcontentloaded')
        if not wait_for_canvas(page):
            print('no arranco'); b.close(); return 1
        page.evaluate("window.__kbHits=[];document.addEventListener('keydown',e=>window.__kbHits.push({key:e.key,ctrl:e.ctrlKey}),true);")
        page.wait_for_timeout(1500)

        # 1) primer clic real: el port entra en pantalla completa solo
        page.mouse.click(640, 400)
        page.wait_for_timeout(2500)
        out['1_fullscreen'] = page.evaluate("!!document.fullscreenElement")
        out['1_lock'] = page.evaluate("""async () => { try { await navigator.keyboard.lock(); return 'CONCEDIDO'; } catch(e){ return 'RECHAZADO: '+e.name; } }""")
        out['1_ctrlD'] = ctrl_d(page)
        out['1_estado_pagina'] = page.evaluate("(document.getElementById('uk-fs-status')||{}).textContent")

        # 2) pausa con Escape (Chrome suelta el bloqueo al pulsar Esc)
        page.keyboard.press('Escape')
        page.wait_for_timeout(2000)
        out['2_fullscreen'] = page.evaluate("!!document.fullscreenElement")
        out['2_lock'] = page.evaluate("""async () => { try { await navigator.keyboard.lock(); return 'CONCEDIDO'; } catch(e){ return 'RECHAZADO: '+e.name; } }""")
        out['2_ctrlD'] = ctrl_d(page)

        # 3) reanudar con un clic
        page.mouse.click(640, 494)
        page.wait_for_timeout(2000)
        out['3_ctrlD'] = ctrl_d(page)
        out['3_estado_pagina'] = page.evaluate("(document.getElementById('uk-fs-status')||{}).textContent")

        # 4) el menu de niveles (F3) debe verse en pantalla completa
        page.keyboard.press('F3')
        page.wait_for_timeout(1500)
        out['4_panel_visible'] = page.evaluate("""() => { const e=document.getElementById('uk-dbg-panel');
            if(!e) return 'NO EXISTE'; const r=e.getBoundingClientRect();
            const dentro = document.fullscreenElement ? document.fullscreenElement.contains(e) : false;
            return (e.className||'')+' | dentro del fullscreen='+dentro+' | '+Math.round(r.width)+'x'+Math.round(r.height); }""")
        out['errores'] = errs[:5]
        b.close()
    print(json.dumps(out, indent=1, ensure_ascii=False))
    return 0


if __name__ == '__main__':
    sys.exit(main())
