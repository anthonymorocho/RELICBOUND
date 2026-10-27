# Handoff — siguiente sesión

1. Lee `docs/21_ESTADO_ACTUAL.md` (qué está hecho y qué falta en P1).
2. `dotnet test Tests/Relicbound.Core.Tests` debe salir en verde.
3. Haz push a la rama de trabajo: la CI `Unity` regenera las escenas, ejecuta el recorrido PlayMode y deja capturas en el artefacto `evidence` (`gh run download <id> -n evidence`). Revisa las capturas antes de dar nada por bueno.
4. Las escenas `Boot` y `Castle` las generan `BootSceneBuilder` y `CastleSceneBuilder`: cambia los builders, no las escenas a mano (hasta P3).
5. P1 está PROPUESTO y a la espera del PASS del usuario. Tras el PASS: P2 (guardado y personalización: tutorial contextual de cámara y selección, más estandartes, recuperación de partida en dispositivo). No empezar P3 antes.
7. Shaders propios en `Assets/Relicbound/Art/Shaders`: StylizedLit (kit), Foliage (vegetación con color de vértice), Grass (instanciada), Banner (estandartes), SelectionRing.
6. Licencia de Unity: secrets `UNITY_LICENSE`, `UNITY_EMAIL` y `UNITY_PASSWORD` en el Environment `UNITY_LICENSE` del repositorio.
