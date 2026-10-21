# MAPEO DEFINITIVO de las escenas de act-1 (GROUND TRUTH = bundle original)

Fuente de verdad: `original-bundles/StreamingAssets/acts/act-1` (bundle UnityFS
del juego original, 2019.4.16f1). Se parseo con `%TEMP%\opencode\unityfs.py`
(UnityFS v7, LZ4HC; cabecera: uint64 offset | uint64 size | uint32 flags | path
terminada en NUL).

Los 13 nodos de escena del bundle original (nombre REAL que espera el juego):
  Level 2-S, Level 1-2, Level 1-3, Level 2-3, Level 2-1, Level 3-1, Level 2-2,
  Level 2-4, Level 1-1, Intermission1, Level 3-2, Level 1-S, Level 1-4
(+ .sharedAssets y .resource por cada uno = 39 nodos).

Match por contenido (tokens compartidos + (levelName, nextAct, targetLevelName)):
  canonico        -> fichero exportado por AssetRipper
  ------------       --------------------------
  Level 1-1       -> Level 1-1            (HEART OF THE SUNRISE)   OK
  Level 1-2       -> Level 1-2            (THE BURNING WORLD)      OK
  Level 1-3       -> Level 1-3            (HALLS OF SACRED REMAINS) OK
  Level 1-4       -> Level 3-S            (CLAIR DE LUNE)          RENOMBRAR
  Level 1-S       -> Level 1-S            (TESTAMENT I)            OK
  Level 2-1       -> Level 2-1            (BRIDGEBURNER)           OK
  Level 2-2       -> Level 2-2            (DEATH AT 20,000 VOLTS)  OK
  Level 2-3       -> Level 2-3            (SHEER HEART ATTACK)     OK
  Level 2-4       -> Level 2-4            (COURT OF THE CORPSE KING) OK
  Level 2-S       -> Intermission1         (LEVEL NAME, act-1)      RENOMBRAR
  Level 3-1       -> Level 3-1            (BELLY OF THE BEAST)     OK
  Level 3-2       -> Level 2-S            (IN THE FLESH)           RENOMBRAR
  Intermission1   -> Level 3-2            (LEVEL NAME, act-2)      RENOMBRAR

Es un ciclo de 4 renombrados:
  Level 3-S.unity     -> Level 1-4.unity
  Intermission1.unity -> Level 2-S.unity
  Level 2-S.unity     -> Level 3-2.unity
  Level 3-2.unity     -> Intermission1.unity
(+ los .meta correspondientes)

Evidencia (verificada, no supuesto):
- act-0 contiene 0-1..0-5 y 0-S. prime contiene P-1. common no tiene escenas.
  Los 3 bundles suman 20 escenas = las 20 del export (5 mas son del player:
  Tutorial, Main Menu, Intro, Endless, DebugRoom).
- El original "BuildPlayer-Level 1-S" contiene "TESTAMENT I" + target "Level 1-2".
  El original "BuildPlayer-Level 3-2" contiene "IN THE FLESH" + target
  "Intermission1". El original "BuildPlayer-Intermission1" contiene "act-2".
  El original "BuildPlayer-Level 2-S" contiene LEVEL NAME + act-1 + sin target.
- El export coincide con esas 4 tuplas (levelName, nextAct, targetLevelName).
- Los targetLevelName DENTRO del export ya son canonicos ("Level 1-4",
  "Level 3-2", "Intermission1"...): solo los NOMBRES DE FICHERO estan permutados.
  => al renombrar, las referencias internas ya encajan y no hay que editarlas.

Cuidado: "Level 3-S" NO existe en ningun bundle original, y el Main Menu del
juego referencia "Level 1-4" pero no "Level 3-S" => "Level 3-S" es un nombre
inventado por AssetRipper.
