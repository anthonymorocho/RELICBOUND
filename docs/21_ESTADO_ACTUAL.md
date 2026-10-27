# Estado actual

_Última actualización: 2026-10-27 (sesión de Claude Code en la nube)._

## Fase autorizada
- **P0 — Auditoría y consolidación:** cerrada a falta del PASS del usuario. Proyecto desde cero (D-001).
- **P1 — Primera apertura y castillo principal:** **en curso**. Método de trabajo D-012: implementar, ejecutar, revisar capturas reales y corregir sin pedir aprobación.

## Cómo se ejecuta y se verifica
- CI `Unity` (GitHub Actions, Environment `UNITY_LICENSE`): configura el proyecto, regenera las escenas `Boot` y `Castle`, ejecuta tests EditMode + PlayMode (recorrido del jugador) y publica capturas reales en el artefacto `evidence`.
- CI `Core tests`: `dotnet test` del núcleo sin Unity.
- Arte: `Tools/Blender/*.py` (headless) y `Tools/UI/make_ui_textures.py`.

## Hecho en P1 (PROPUESTO, sin PASS)
| Tarea | Estado | Evidencia |
|---|---|---|
| 1.1 Núcleo (perfil, guardado, recursos, nivel, arranque, cámara) | PROPUESTO | 40/40 tests en dotnet y en Unity |
| 1.2 Proyecto Unity 6 + URP + CI | PROPUESTO | CI en verde |
| 1.3 Escena Boot con splash y carga real | IMPLEMENTADO, verificándose | Captura `01_splash` |
| 1.4 Pantalla de perfil (estandarte, nombres, validación en vivo) | IMPLEMENTADO, verificándose | Capturas `02`–`04` |
| 1.5 Escena Castle: kit propio, terreno pintado, vegetación, luz, postproceso | PROPUESTO | `docs/evidencias/P1` |
| 1.6 Cámara (ratón, táctil, teclado; toque ≠ arrastre) | IMPLEMENTADO, sin prueba en dispositivo | Código + tests de Core |
| 1.7 HUD del castillo (insignia, nivel, XP, recursos) | IMPLEMENTADO, verificándose | Captura `07` |
| 1.8 Selección de edificio con panel de información | IMPLEMENTADO, verificándose | Captura `08` |
| 1.9 Ajustes (volúmenes, calidad, vibración) | IMPLEMENTADO, verificándose | Captura `09` |
| 1.10 Evidencias PC 1920×1080 y Android 2400×1080 | Automatizadas en CI | Artefacto `evidence` |

## Defectos conocidos (a corregir dentro de P1)
- El patio del castillo está vacío: faltan estandartes, antorchas y objetos de vida.
- La piedra se ve demasiado limpia; falta suciedad en la base y oclusión ambiental.
- Audio: no hay sonidos ni música todavía (lista de P1 en `docs/14`).
- Sin prueba en hardware real: rendimiento **NO VERIFICADO**.

## No iniciar todavía
- P3 Construcción y mejoras (el panel de edificio solo informa).
- P4 Asedios. Backend, PvP, tienda, pagos y anuncios (P11–P13).
