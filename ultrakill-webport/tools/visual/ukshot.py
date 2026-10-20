#!/usr/bin/env python3
"""
ukshot.py - Herramienta de pruebas visuales del port WebGL de ULTRAKILL.

Abre el juego en un navegador headless (Chromium via Playwright), interactua con
el como lo haria una persona (teclado y raton con eventos de confianza, de modo
que el navegador concede el pointer lock), guarda capturas y calcula el
porcentaje de pixeles que cambian entre dos de ellas. Sirve para comprobar de
forma objetiva que un cambio altera la imagen.

Uso:
  python ukshot.py --url http://localhost:8000/?v=205 --out C:/temp/shots \
      --actions "wait:20;state;key:Escape;wait:3;shot:pausa;state"

  python ukshot.py --diff shots/a.png shots/b.png     # % de pixeles distintos

Acciones (separadas por ';'):
  wait:N        espera N segundos
  key:ESC       pulsa una tecla (Escape, F3, W, ...)
  click:X,Y     clic en coordenadas de la pagina
  scroll:N@X,Y  rueda del raton en un punto
  shot:NOMBRE   guarda una captura
  state         vuelca el estado (pointer lock, cursor, errores de consola)
  send:Obj,M,V  llama a unityInstance.SendMessage(Obj, M, V)
"""
import argparse
import json
import os
import sys
import time

DEFAULT_URL = "http://localhost:8000/?v=205"


def wait_for_canvas(page, timeout=90):
    """Espera a que Unity cree el canvas y tenga contenido."""
    t0 = time.time()
    while time.time() - t0 < timeout:
        try:
            ok = page.evaluate(
                "() => { const c = document.querySelector('#unityContainer canvas');"
                " return !!(c && c.width > 0 && window.unityInstance); }"
            )
            if ok:
                return True
        except Exception:
            pass
        page.wait_for_timeout(500)
    return False


def read_state(page):
    return page.evaluate(
        """() => {
        const c = document.querySelector('#unityContainer canvas');
        return {
          url: location.href,
          canvas: !!c,
          size: c ? [c.width, c.height] : null,
          cursor: c ? getComputedStyle(c).cursor : null,
          cursorInline: c ? c.style.cursor : null,
          pointerLock: document.pointerLockElement ? 'LOCKED' : null,
          fullscreen: document.fullscreenElement ? 'FS' : null,
          unity: !!window.unityInstance,
          sendMessage: !!(window.unityInstance && window.unityInstance.SendMessage),
          dbgPanel: (document.getElementById('uk-dbg-panel')||{}).className || null,
        };
    }"""
    )


def run(args):
    from playwright.sync_api import sync_playwright

    outdir = args.out
    os.makedirs(outdir, exist_ok=True)
    console_msgs = []
    state_log = []

    with sync_playwright() as p:
        browser = p.chromium.launch(
            headless=not args.headed,
            args=[
                "--use-gl=angle",
                "--use-angle=swiftshader",
                "--enable-unsafe-swiftshader",
                "--disable-features=Translate,MediaRouter",
                "--autoplay-policy=no-user-gesture-required",
                "--window-size=1280,800",
            ],
        )
        ctx = browser.new_context(
            viewport={"width": 1280, "height": 800},
            permissions=[],
        )
        page = ctx.new_page()
        page.on("console", lambda m: console_msgs.append(f"[{m.type}] {m.text}"))
        page.on("pageerror", lambda e: console_msgs.append(f"[pageerror] {e}"))

        print(f"-> {args.url}", flush=True)
        page.goto(args.url, wait_until="domcontentloaded", timeout=120000)
        try:
            page.bring_to_front()
        except Exception:
            pass

        ok = wait_for_canvas(page)
        print(f"   canvas listo: {ok}", flush=True)
        if not ok:
            print("   AVISO: el juego no arranco", flush=True)

        # Las acciones se separan con ';' porque 'click:X,Y' usa la coma.
        for act in [a.strip() for a in args.actions.split(";") if a.strip()]:
            kind, _, val = act.partition(":")
            kind = kind.strip().lower()
            if kind == "wait":
                time.sleep(float(val))
                print(f"   wait {val}s", flush=True)
            elif kind == "key":
                key = val.strip()
                # los nombres de tecla de Playwright: Escape, F3, w, ...
                page.keyboard.press(key if len(key) > 1 else key.lower())
                print(f"   key {key}", flush=True)
            elif kind == "click":
                x, y = val.split(",")
                page.mouse.click(float(x), float(y))
                print(f"   click {x},{y}", flush=True)
            elif kind == "send":
                # send:GameObject,Metodo,Valor  -> unityInstance.SendMessage(...)
                parts = val.split(",")
                obj, meth = parts[0], parts[1]
                value = parts[2] if len(parts) > 2 else ""
                page.evaluate(
                    "([o,m,v]) => { if (window.unityInstance) window.unityInstance.SendMessage(o,m,v); }",
                    [obj, meth, value],
                )
                print(f"   send {obj}.{meth}({value})", flush=True)
            elif kind == "scroll":
                # scroll:N  (N>0 = hacia abajo, N<0 = hacia arriba) sobre el punto dado
                # scroll:N@X,Y
                target = None
                if "@" in val:
                    dy, _, coords = val.partition("@")
                    x, y = coords.split(",")
                    target = (float(x), float(y))
                else:
                    dy = val
                if target:
                    page.mouse.move(target[0], target[1])
                page.mouse.wheel(0, float(dy))
                print(f"   scroll {val}", flush=True)
            elif kind == "shot":
                path = os.path.join(outdir, f"{val.strip()}.png")
                page.screenshot(path=path)
                print(f"   shot -> {path}", flush=True)
            elif kind == "state":
                st = read_state(page)
                state_log.append(st)
                print("   state: " + json.dumps(st, ensure_ascii=False), flush=True)
            else:
                print(f"   accion desconocida: {act}", flush=True)

        # estado final
        final = read_state(page)
        browser.close()

    print("\n=== ESTADO FINAL ===")
    print(json.dumps(final, ensure_ascii=False, indent=2))
    errores = [m for m in console_msgs if "[error]" in m or "[pageerror]" in m]
    print(f"\n=== CONSOLA: {len(console_msgs)} mensajes, {len(errores)} errores ===")
    for m in errores[-10:]:
        print("   " + m[:220])
    return final


def diff(a, b):
    """Porcentaje de pixeles que difieren entre dos capturas."""
    try:
        from PIL import Image, ImageChops
    except ImportError:
        print("FALTA Pillow: pip install pillow")
        return
    ia = Image.open(a).convert("RGB")
    ib = Image.open(b).convert("RGB")
    if ia.size != ib.size:
        print(f"tamanos distintos: {ia.size} vs {ib.size}")
        return
    diff = ImageChops.difference(ia, ib)
    bbox = diff.getbbox()
    hist = diff.convert("L").histogram()
    total = ia.size[0] * ia.size[1]
    changed = total - hist[0]
    print(f"pixels distintos: {changed} / {total} = {100.0*changed/total:.3f}%")
    if bbox:
        print(f"bbox del cambio: {bbox}")
        # media de la diferencia
        stat = diff.convert("L").getdata()
        mean = sum(stat) / len(list(stat))
        print(f"diferencia media: {mean:.3f}/255")
    else:
        print("IMAGENES IDENTICAS")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--url", default=DEFAULT_URL)
    ap.add_argument("--out", default=os.path.join(os.path.expanduser("~"), "ukshots"))
    ap.add_argument("--actions", default="wait:20,state,shot:juego")
    ap.add_argument("--headed", action="store_true")
    ap.add_argument("--diff", nargs=2, metavar=("A", "B"))
    args = ap.parse_args()

    if args.diff:
        diff(args.diff[0], args.diff[1])
    else:
        run(args)
