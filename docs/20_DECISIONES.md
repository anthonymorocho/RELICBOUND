# Registro de decisiones

| ID | Fecha | Decisión | Motivo | Alternativas descartadas |
|---|---|---|---|---|
| D-001 | 2026-10-27 | Proyecto **desde cero** en **Unity 6 LTS + URP** | No existe prueba S0 (confirmado por el usuario); Unity 6 LTS es la versión estable con soporte largo, buen soporte de Android y URP maduro | Integrar S0 (no existe); Unity 2022 LTS (soporte más corto) |
| D-002 | 2026-10-27 | Lógica de reglas en un ensamblado **sin UnityEngine** (`Relicbound.Core`), probado también con `dotnet test` | Permite validación determinista en servidor (PvP), tests rápidos y trabajar sin editor | Lógica en MonoBehaviours |
| D-003 | 2026-10-27 | Guardado JSON (Newtonsoft) con sobre versionado + SHA-256 + copia de respaldo + migraciones | Requisito de recuperación ante corrupción y migraciones | `PlayerPrefs` (frágil, sin migraciones); binario propio (difícil de depurar) |
| D-004 | 2026-10-27 | El checksum **no** es antitrampas | Cualquier dato del cliente es manipulable; la competición se valida en el servidor | Cifrar el guardado (falsa seguridad) |
| D-005 | 2026-10-27 | **UI Toolkit** para la UI en pantalla; uGUI solo para marcadores en el mundo | UI Toolkit permite temas USS consistentes y escalado limpio en PC/Android | Todo en uGUI |
| D-006 | 2026-10-27 | Simulación de combate determinista a paso fijo; física solo decorativa | Necesario para re-simular asedios en el servidor | Física de Unity como fuente de verdad |
| D-007 | 2026-10-27 | Personajes desde packs con licencia + personalización; no esculpir humanoides | Resultados previos pobres al modelar personajes; prompt §15 | Modelado propio de personajes |
| D-008 | 2026-10-27 | Umbral de arrastre en **milímetros físicos** (2,5 mm) | Mismo comportamiento en móvil de alta densidad y en monitor | Umbral fijo en píxeles |
| D-009 | 2026-10-27 | Sin pagar por saltarse esperas; esperas cortas | Prompt §13.3 y §14: nada de ventajas compradas | Aceleradores con gemas |
| D-010 | 2026-10-27 | Bajar la capacidad del almacén nunca borra recursos | Principio de no destruir progreso | Recortar al límite |
| D-011 | 2026-10-27 | Builds y tests de Unity en **GitHub Actions (GameCI)** | Permite compilar, probar y generar builds sin el PC del usuario | Compilar solo en local |
| D-012 | 2026-10-27 | **Método de trabajo:** implementar directamente en Unity, ejecutar, revisar capturas reales y corregir sin pedir aprobación dentro de la fase autorizada; documentación solo como registro interno | Instrucción definitiva del usuario | Mockups y propuestas previas a implementar |
| D-013 | 2026-10-27 | Arte del kit y vegetación generados por script en Blender (`Tools/Blender`), versionados y reproducibles | Calidad controlable e iterable sin depender de packs; permite regenerar con cambios de dirección artística | Modelado manual no reproducible |
