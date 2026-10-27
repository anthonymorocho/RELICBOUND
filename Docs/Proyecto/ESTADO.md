# Estado del proyecto

Fecha: 2026-10-27 · Hito actual: **M00 Auditoría y baseline** → inicio de **M01 Menú y fachada**.

## Hecho
- RELICBOUND archivado en la rama `archivo/relicbound`; repositorio limpio para BEFORE IT WAS YOU.
- Paquete de preproducción copiado a `Docs/Preproduccion` y auditado (`AUDITORIA_M00.md`).
- Proyecto Unity 6000.3.27f1 con URP: `PC_RPAsset` (Alta) + variantes Baja/Media/Ultra, renderer Forward+ con SSAO, DX12, Input System, capas.
- Núcleo C# probado (14 tests): flujo de juego, guardado por snapshots con respaldo y cuarentena, opciones persistentes.
- Sistemas: jugador en primera persona (caminar, correr, agacharse, gravedad, balanceo ajustable, pasos por superficie, ruido para la IA), interacción por mirada (puertas con bisagra, interruptores), buses de audio.
- Interfaz real (UI Toolkit): menú principal estilo moodboard, Continuar solo con guardado válido, confirmación de Nueva partida, Opciones completas, Créditos, Salir, pausa, prólogo con subtítulos, HUD mínimo.
- Escenas generadas por script: `L_Startup` (aviso de contenido + carga) y `L_E01_Arrival` (menú + episodio, `Player_Daniel` con los parámetros pedidos).

## En curso
- Fachada nocturna, calle, porche y coche con materiales PBR CC0 para el fondo del menú (M01).

## Pendiente de decisión del usuario
- Nombre a mostrar en créditos (dirección/desarrollo). Por ahora los créditos solo listan motor, tipografías y assets de terceros.

## QA por dimensión
| Dimensión | Estado | Nota |
|---|---|---|
| TECH | UNVERIFIED | pendiente de la primera ejecución de la CI con Unity |
| VISUAL | FAIL | la fachada aún no existe; no se entregan capturas como finales |
| GAMEPLAY | UNVERIFIED | solo movimiento y menús |
| NARRATIVE | UNVERIFIED | prólogo implementado con el texto limpio del guion |
