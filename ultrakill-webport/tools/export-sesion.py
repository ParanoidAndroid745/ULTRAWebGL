# -*- coding: utf-8 -*-
"""
Exporta sesiones de opencode desde la base de datos local al formato que usa
`GET /api/experimental/session/{id}/export` (info + messages), para poder
importarlas en otra maquina con `POST /api/experimental/session/import`.

El formato replica el de un export anterior (mismo tipo de dato en cada campo).
"""
import json, io, os, sqlite3, sys

DB = os.path.expandvars(r'%USERPROFILE%\.local\share\opencode\opencode.db')
OUT = sys.argv[1] if len(sys.argv) > 1 else r'C:\Users\JJ\Documents\ultrakill-refs'
SESSIONS = {
    'ses_ea0be7119ffeRHDND5sC2ouawK': 'sesion-ULTRA-v20.8.json',
    'ses_ea10ea4d1ffeH4zKYYI4119Qsi': 'sesion-STRAWBERRY-v20.8.json',
    'ses_ea0e0eca9ffe9iQbvW2lpMnmFz': 'sesion-TOWER-v20.8.json',
}


def exportar(c, sid):
    cols = [r[1] for r in c.execute('pragma table_info(session_v2)')]
    row = c.execute('select * from session_v2 where id=?', (sid,)).fetchone()
    if not row:
        return None
    s = dict(zip(cols, row))
    info = {
        'id': s['id'],
        'projectID': s['project_id'],
        'agent': s['agent'],
        'model': s['model'],
        'cost': str(s['cost'] or 0),
        'tokens': json.dumps({
            'input': s['tokens_input'] or 0,
            'output': s['tokens_output'] or 0,
            'reasoning': s['tokens_reasoning'] or 0,
            'cacheRead': s['tokens_cache_read'] or 0,
            'cacheWrite': s['tokens_cache_write'] or 0,
        }),
        'outcome': s['idle_outcome'] or 'unknown',
        'time': {'created': s['time_created'], 'updated': s['time_updated']},
        'title': s['title'],
        'permissions': s['permission'] or '[]',
        'location': json.dumps({'directory': s['directory']}),
    }
    messages = []
    for mid, typ, data in c.execute(
            'select id, type, data from session_message where session_id=? order by seq', (sid,)):
        try:
            d = json.loads(data)
        except Exception:
            d = {'text': data}
        m = {'id': mid, 'type': typ}
        m.update(d)
        messages.append(m)
    return {'info': info, 'messages': messages}


def main():
    os.makedirs(OUT, exist_ok=True)
    c = sqlite3.connect('file:%s?mode=ro' % DB.replace('\\', '/'), uri=True)
    for sid, name in SESSIONS.items():
        data = exportar(c, sid)
        if not data:
            print('  (no existe la sesion %s)' % sid)
            continue
        p = os.path.join(OUT, name)
        with io.open(p, 'w', encoding='utf-8-sig', newline='\n') as f:
            json.dump(data, f, ensure_ascii=False, indent=1)
        print('  %-32s %6d mensajes  %7.1f MB' % (name, len(data['messages']), os.path.getsize(p) / 1024 / 1024))


if __name__ == '__main__':
    main()
