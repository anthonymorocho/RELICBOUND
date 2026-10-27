# Estado actual

_Última actualización: 2026-10-27 (sesión de Claude Code en la nube)._

## Fase autorizada
- **P0 — Auditoría y consolidación:** PROPUESTO para cierre. No existe prueba S0: el proyecto empieza desde cero (`22` §L-001, D-001).
- **P1 — Primera apertura y castillo principal:** autorizada por el prompt maestro. Solo está hecho el **núcleo lógico sin Unity**. La parte visual (escena, cámara, HUD) necesita el editor de Unity, que requiere la licencia configurada en GitHub (`23`).

## Terminado (estado PROPUESTO, sin PASS)
| Elemento | Dónde | Evidencia |
|---|---|---|
| Documentación base 00–23 | `docs/` | Este repositorio |
| Recursos y almacén (`ResourceWallet`, `ResourceCost`) | `Core/Economy` | `EconomyTests` |
| Nivel y XP con curva configurable de 30 niveles | `Core/Progression` | `ProgressionTests` |
| Perfil y validación de nombres (jugador/castillo) | `Core/Profile` | `ProfileTests` |
| Guardado versionado: checksum, copia de seguridad, cuarentena, migraciones, escritura atómica | `Core/Save` | `SaveTests` |
| Ajustes con volúmenes por grupo del mixer | `Core/Settings` | `SaveTests` (saneado) |
| Flujo de arranque Splash → Carga → Perfil → Intro → Castillo | `Core/Boot/BootFlow` | `BootFlowTests` |
| Sesión de juego ↔ guardado | `Core/Boot/GameSession` | `SaveTests.RoundTrip` |
| Límites de cámara y distinción toque/arrastre/pulsación por mm físicos | `Core/CameraControl` | `CameraTests` |

`dotnet test Tests/Relicbound.Core.Tests` → **40/40 tests superados** (2026-10-27, .NET 8, LangVersion 9).

## Aprobaciones del hito P1 (núcleo)
| Eje | Estado | Motivo |
|---|---|---|
| TECHNICAL | PROPUESTO | Tests en verde fuera de Unity. **No compilado aún dentro de Unity.** |
| VISUAL | PENDIENTE | No existe escena. |
| GAMEPLAY | PENDIENTE | No hay nada jugable. |
| PERFORMANCE | NO VERIFICADO | Sin hardware ni build. |

## Falta para cerrar P0
1. Revisión y PASS del usuario a la documentación y a las decisiones de `20`.

## Falta para cerrar P1
Ver `17_ROADMAP.md` §P1. Resumen: escena Boot, splash, pantalla de perfil, escena Castle con el castillo 3D, rig de cámara, HUD del castillo según `04_UI_UX.md`, panel de ajustes, capturas reales en 16:9 (PC) y 20:9 (Android).

## No iniciar todavía
- P3 Construcción (cuadrícula, colocación): las cotas de `05` son hipótesis.
- Cualquier batalla nueva (P4) antes de cerrar P1 y P2.
- Backend, cuentas, PvP, tienda, pagos y anuncios (P11–P13): requieren autorización expresa y el coste aprobado.
- Modelado de personajes desde cero (ver D-007).
