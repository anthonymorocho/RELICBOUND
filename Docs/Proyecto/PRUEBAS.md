# Pruebas

## Automáticas
| Suite | Dónde | Qué cubre |
|---|---|---|
| Núcleo (.NET y EditMode) | `Assets/_Game/Tests/Core` | Guardado: ida y vuelta completa, alteración → respaldo, corrupto sin respaldo, respaldo nunca pisado por corrupto, versión más nueva, migración, borrado, disco atómico. Flujo: recorrido completo y transiciones prohibidas. Opciones: saneado, persistencia, archivo dañado. |
| Recorrido (PlayMode) | `Assets/_Game/Tests/PlayMode` | L_Startup → menú (sin Continuar si no hay partida) → Opciones con teclado (cambio persistido) → Nueva partida con prólogo → llegada → caminar con W → pausa con Esc y vuelta. Capturas en `Evidence/journey`. |

## Manuales pendientes (requieren persona)
- Lectura en voz alta de los diálogos.
- Playtest ciego de los primeros 10–12 minutos (M03).
