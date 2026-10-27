# RELICBOUND: Siege Kingdoms — Empieza aquí

Juego de estrategia, construcción de fortalezas y asedios para **Windows PC y Android**, en Unity (URP) con una sola base de código.

## Lectura obligatoria, en este orden
1. `21_ESTADO_ACTUAL.md`: qué fase está autorizada, qué está hecho y qué **no** debe empezarse.
2. `20_DECISIONES.md`: decisiones tomadas y por qué. No se reabren sin una razón demostrable.
3. `17_ROADMAP.md`: fases P0–P14 y criterios de salida.
4. `23_HANDOFF.md`: qué hacer exactamente en la siguiente sesión de trabajo.
5. El documento del área que vayas a tocar (lista abajo).

## Mapa de documentos
| Doc | Área |
|---|---|
| 01 | GDD maestro: visión, pilares y flujo del jugador |
| 02 | Dirección artística |
| 03 | Referencias visuales y qué se estudia de cada una |
| 04 | UI/UX: HUD del castillo, paneles y proceso de diseño |
| 05 | Construcción de castillos (cuadrícula, colocación) |
| 06 | Edificios y mejoras |
| 07 | Ataque y máquinas de asedio |
| 08 | Destrucción |
| 09 | Defensa y unidades |
| 10 | Campaña PvE |
| 11 | PvP y clasificaciones |
| 12 | Economía y progresión |
| 13 | Temporadas y cosméticos |
| 14 | Audio |
| 15 | Arquitectura Unity |
| 16 | Diferencias PC/Android |
| 17 | Roadmap |
| 18 | QA y protocolo de aprobación |
| 19 | Licencias y assets |
| 20 | Registro de decisiones |
| 21 | Estado actual |
| 22 | Fallos y lecciones |
| 23 | Handoff |

## Estructura del repositorio
```
Assets/Relicbound/Scripts/Core/   Lógica pura C# (sin UnityEngine), asmdef Relicbound.Core
Tests/Relicbound.Core.Tests/      Tests NUnit de Core ejecutables con `dotnet test` (sin Unity)
docs/                             Esta documentación
```
Cuando se cree el proyecto Unity (P1.2), `Assets/`, `Packages/` y `ProjectSettings/` vivirán en la raíz (ver `15_ARQUITECTURA_UNITY.md`).

## Reglas que no se negocian
- El castillo 3D del jugador **es** la pantalla principal. No hay menú principal.
- Nada de pay-to-win: el dinero real solo compra cosméticos.
- Ligas de PC y Android separadas; sin crossplay competitivo.
- El PvP no existe hasta que haya backend real con validación de servidor.
- Ninguna fase recibe PASS si el usuario no lo da (ver `18_QA.md`).
