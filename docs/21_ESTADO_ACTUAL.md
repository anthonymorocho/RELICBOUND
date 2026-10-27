# Estado actual

_Última actualización: 2026-10-27 (sesión de Claude Code en la nube)._

## Fase autorizada
- **P0 — Auditoría y consolidación:** cerrada a falta del PASS del usuario. Proyecto desde cero (D-001).
- **P1 — Primera apertura y castillo principal:** **en curso**. Método de trabajo D-012: implementar, ejecutar, revisar capturas reales y corregir sin pedir aprobación.

## Cómo se ejecuta y se verifica
- CI `Unity` (GitHub Actions, Environment `UNITY_LICENSE`): configura el proyecto, regenera las escenas `Boot` y `Castle`, ejecuta tests EditMode + PlayMode (recorrido del jugador) y publica capturas reales en el artefacto `evidence`.
- CI `Core tests`: `dotnet test` del núcleo sin Unity.
- Arte: `Tools/Blender/*.py` (headless) y `Tools/UI/make_ui_textures.py`.

## Hecho en P1 (PROPUESTO, a la espera de revisión del usuario)
| Tarea | Estado | Evidencia (artefacto `evidence` de la CI) |
|---|---|---|
| 1.1 Núcleo (perfil, guardado, recursos, nivel, arranque, cámara) | PROPUESTO | 40/40 tests (dotnet y Unity) |
| 1.2 Proyecto Unity 6 + URP + CI | PROPUESTO | CI en verde; escenas y assets versionados |
| 1.3 Escena Boot con splash y carga real | PROPUESTO | `01a_splash_inicio` |
| 1.4 Perfil (estandarte, nombres, validación en vivo, error) | PROPUESTO | `02`–`04` |
| 1.5 Castillo: kit propio con detalle, desgaste (shader StylizedLit), terreno pintado, pradera con flores, vegetación facetada con AO, estandartes con la heráldica del jugador, objetos de patio, SSAO, luz de tarde, gradación | PROPUESTO | `07`, `10` |
| 1.6 Cámara (ratón, táctil, teclado; toque ≠ arrastre) | PROPUESTO, **sin prueba en dispositivo** | Código + tests de Core |
| 1.7 HUD (insignia con medallón de nivel, XP, recursos con iconos 3D) | PROPUESTO | `07`, `10` |
| 1.8 Selección de edificio con panel, brillo y anillo | PROPUESTO | `08` |
| 1.9 Ajustes y audio (ambiente, música, interfaz; volumen por categoría) | PROPUESTO, audio **no verificable en capturas** | `09` + test del recorrido |
| 1.10 Evidencias PC 1920×1080 y Android 2400×1080 | Automatizadas | `11_android_2400x1080` |

## Pendiente / defectos conocidos
- Rendimiento **NO VERIFICADO**: no hay hardware. La hierba instanciada (~1 M triángulos en Alta) y el torreón (28 k triángulos, sobre el presupuesto de 25 k) necesitan medición y LOD en Android de gama media.
- Prueba en dispositivo Android y en PC reales (controles táctiles, zonas seguras, audio).
- El patio queda en sombra a la hora del sol actual; revisar al añadir edificios en P3.
- Las mejoras de edificio (botón Mejorar) son P3: el panel solo informa.

## No iniciar todavía
- P3 Construcción y mejoras (el panel de edificio solo informa).
- P4 Asedios. Backend, PvP, tienda, pagos y anuncios (P11–P13).
