# Handoff — siguiente sesión

## Para el usuario (requisito para que el agente trabaje solo con Unity)
Unity exige una licencia (Personal es gratuita) para abrir el editor, incluso en servidores. Hay que añadirla **una sola vez** como *secrets* del repositorio en GitHub → Settings → Secrets and variables → Actions:

| Secret | Contenido |
|---|---|
| `UNITY_LICENSE` | Contenido completo del archivo de licencia `Unity_lic.ulf` del PC donde está activado Unity (Windows: `C:\ProgramData\Unity\Unity_lic.ulf`) |
| `UNITY_EMAIL` | Correo de la cuenta Unity |
| `UNITY_PASSWORD` | Contraseña de la cuenta Unity |

Nunca se pegan en el chat. Con eso, la CI (`.github/workflows/unity.yml`, que se añade en P1.2) compila, ejecuta los tests de Unity, genera builds de Windows y Android y produce capturas.

## Para el agente
1. Leer `21_ESTADO_ACTUAL.md`.
2. `dotnet test Tests/Relicbound.Core.Tests` debe salir en verde antes de tocar nada.
3. Siguiente tarea: **P1.2**, crear el proyecto Unity 6 + URP en la raíz (`Packages/manifest.json`, `ProjectSettings/`) y la CI de GameCI. Después, P1.3 (escena Boot).
4. Modelado del castillo inicial en Blender (headless, instalado por el hook): torreón central, 4 torres, murallas por secciones, puerta con caseta. Antes de modelar, completar las referencias en `03`.
5. Actualizar `21`, `18` y este archivo al cerrar cada tarea.
