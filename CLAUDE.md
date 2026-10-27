# RELICBOUND: Siege Kingdoms — instrucciones para agentes

- Antes de trabajar, lee `docs/00_EMPIEZA_AQUI.md` y `docs/21_ESTADO_ACTUAL.md`. Solo se trabaja en la fase autorizada que indica ese archivo.
- Nunca te concedas PASS: usa PENDIENTE / PROPUESTO / FAIL / NO VERIFICADO (`docs/18_QA.md`).
- `Assets/Relicbound/Scripts/Core` no puede usar UnityEngine ni C# > 9. Verifícalo con `dotnet test Tests/Relicbound.Core.Tests`.
- Cifras de balance solo en datos, nunca en scripts.
- Todo asset nuevo se registra con su licencia en `docs/19_LICENCIAS_Y_ASSETS.md` antes de entrar en el repo.
- Al terminar una tarea, actualiza `docs/21_ESTADO_ACTUAL.md`, `docs/18_QA.md` y `docs/23_HANDOFF.md`.
- Blender headless (sesiones en la nube): `blender -b --factory-startup --python script.py`. Renderiza con Cycles en CPU; EEVEE no funciona sin GPU.
- Documentación y textos de juego en español; identificadores de código en inglés.
