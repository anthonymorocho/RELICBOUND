# BEFORE IT WAS YOU — guía para agentes

- Leer `Docs/Proyecto/ESTADO.md`, `Docs/Proyecto/DECISIONES.md` y el paquete `Docs/Preproduccion` (orden en `00_INICIO/LEEME_PRIMERO.md`) antes de tocar nada.
- Unity 6000.3.27f1 + URP. Las escenas se generan con scripts de editor (`Assets/_Game/Scripts/Editor`); no editar escenas a mano en el repositorio.
- La verificación con Unity real se hace en GitHub Actions (`.github/workflows/unity.yml`). Descargar evidencias con `gh run download <id> -n evidence`.
- Núcleo sin Unity: `dotnet test Tests/BeforeItWasYou.Core.Tests`.
- Textos del juego en español natural, sin comillas angulares ni frases teatrales (GUION §Escritura prohibida).
- Cada asset de terceros se registra en `Docs/Proyecto/REGISTRO_ASSETS.csv` con URL, autor y licencia exacta antes de integrarlo.
- QA en cuatro dimensiones separadas (TECH / VISUAL / GAMEPLAY / NARRATIVE). No marcar aprobación humana por cuenta propia.
- Las builds de Windows solo se generan cuando hay bastante para jugar (`build-windows.yml`, manual).
