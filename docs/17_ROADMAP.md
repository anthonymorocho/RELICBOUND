# Roadmap

Las fases se siguen en orden. Una fase se cierra solo cuando el usuario da PASS a sus cuatro ejes (`18_QA.md`). Se pueden adelantar dependencias **internas** invisibles (lógica, datos), nunca pantallas sueltas.

| Fase | Objetivo | Criterio de salida (resumen) |
|---|---|---|
| P0 | Auditoría y consolidación | Auditoría hecha (proyecto desde cero, L-001), docs 00–23, arquitectura y licencias definidas, punto de recuperación Git |
| P1 | Primera apertura y castillo principal | Abrir el juego → splash ≤ 3 s → perfil → castillo 3D con cámara y HUD final, en PC y Android |
| P2 | Guardado y personalización inicial | Nombre, estandarte, tutorial contextual, guardado/carga en dispositivo real, recuperación de corrupción |
| P3 | Construcción | Cuadrícula, colocación válida/inválida, murallas, puertas y torres, primera mejora con cambio visual |
| P4 | Primer asedio PvE | Catapulta con apuntado libre, destrucción por estados, brecha navegable, unidad humanoide real, victoria/derrota |
| P5 | Recompensas y progresión | Recompensas → XP/recursos → primera mejora comprada con ellos → vuelta al castillo |
| P6 | Campaña | Región 1 completa (6–8 fortalezas) con objetivos variados y puntuación local |
| P7 | Expansión ofensiva | Trabuquete, ballesta, ariete, municiones, órdenes tácticas |
| P8 | Expansión defensiva | Defensores, torres especializadas, trampas, prioridades de IA |
| P9 | Economía completa | Producción, investigación, inventario, misiones y eventos locales |
| P10 | Calidad PC/Android | Rendimiento medido en hardware real, accesibilidad, audio final |
| P11 | Infraestructura competitiva | Backend aprobado y presupuestado, validación de asedios, PvP asíncrono real |
| P12 | Ligas y temporadas | Cuatro clasificaciones separadas, ligas, temporadas, antitrampas |
| P13 | Tienda y monetización | Solo cosméticos, compras verificadas oficialmente, pase de temporada |
| P14 | Lanzamiento | QA externo, cumplimiento de tiendas, publicación **autorizada** |

## P1 en detalle (fase en curso)
| # | Tarea | Estado |
|---|---|---|
| 1.1 | Núcleo: perfil, guardado, recursos, nivel, flujo de arranque, cámara | PROPUESTO (hecho, sin PASS) |
| 1.2 | Proyecto Unity 6 + URP, Newtonsoft, Input System, CI GameCI | PENDIENTE (requiere licencia Unity en secrets, ver `23`) |
| 1.3 | Escena `Boot`: splash de marca ≤ 3 s, carga asíncrona del guardado | PENDIENTE |
| 1.4 | Pantalla de perfil (nombre del jugador y del castillo) según `04` §Perfil | PENDIENTE |
| 1.5 | Escena `Castle`: castillo inicial 3D, terreno, vegetación, luz (ver `02`) | PENDIENTE |
| 1.6 | `CastleCameraRig` usando `CameraLimits` y `PointerGesture` | PENDIENTE |
| 1.7 | HUD del castillo (componentes UI Toolkit, tema `04`) | PENDIENTE |
| 1.8 | Selección básica de edificio con resaltado y panel (solo información; mejorar llega en P3) | PENDIENTE |
| 1.9 | Ajustes: volúmenes, calidad, idioma | PENDIENTE |
| 1.10 | Evidencias: capturas PC 1920×1080 y Android 2400×1080, vídeo de 30 s, log de tests | PENDIENTE |
