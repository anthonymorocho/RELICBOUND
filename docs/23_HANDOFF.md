# Handoff — siguiente sesión

1. Lee `docs/21_ESTADO_ACTUAL.md` (qué está hecho y qué falta en P1).
2. `dotnet test Tests/Relicbound.Core.Tests` debe salir en verde.
3. Haz push a la rama de trabajo: la CI `Unity` regenera las escenas, ejecuta el recorrido PlayMode y deja capturas en el artefacto `evidence` (`gh run download <id> -n evidence`). Revisa las capturas antes de dar nada por bueno.
4. Las escenas `Boot` y `Castle` las generan `BootSceneBuilder` y `CastleSceneBuilder`: cambia los builders, no las escenas a mano (hasta P3).
5. Pendiente de P1: vida en el patio (estandartes, antorchas), suciedad y AO en la piedra, audio de castillo e interfaz, y revisión en dispositivo Android real.
6. Licencia de Unity: secrets `UNITY_LICENSE`, `UNITY_EMAIL` y `UNITY_PASSWORD` en el Environment `UNITY_LICENSE` del repositorio.
