# BEFORE IT WAS YOU — Episodio 01: Alguien estuvo aquí

Juego narrativo de terror en primera persona (Unity 6.3 LTS · URP · Windows · DirectX 12 · español).

## Cómo está organizado

| Ruta | Contenido |
|---|---|
| `Assets/_Game/Scripts/Core` | Lógica pura en C# sin Unity: flujo de juego, guardado por snapshots, opciones. Testeable con `dotnet test`. |
| `Assets/_Game/Scripts/Runtime` | Sistemas del juego: servicios, entrada, jugador, interacción, audio, interfaz, director del episodio. |
| `Assets/_Game/Scripts/Editor` | Configuración del proyecto y generación de escenas (reproducible en CI). |
| `Assets/_Game/UI` | UXML/USS, fuentes OFL y texturas de interfaz. |
| `Assets/_Game/Tests` | Tests EditMode (núcleo) y PlayMode (recorrido del jugador con capturas). |
| `Tests/BeforeItWasYou.Core.Tests` | Proyecto .NET que compila el núcleo y sus tests fuera de Unity. |
| `Tools/` | Scripts de Blender y Python para producir arte y texturas. |
| `Docs/Preproduccion` | Paquete de preproducción original (GDD, guion, cronología, plano, sistemas, arte, roadmap). |
| `Docs/Proyecto` | Documentación viva: estado, decisiones, plano técnico, pruebas, errores, assets. |

## Abrir en Unity

1. Unity **6000.3.27f1**.
2. Abrir la carpeta del repositorio.
3. Menú **BEFORE IT WAS YOU → Generar todo** (configura URP `PC_RPAsset`, DX12, Input System, capas y genera `L_Startup` y `L_E01_Arrival`).
4. Abrir `Assets/_Game/Scenes/L_Startup.unity` y pulsar Play.

## CI

`.github/workflows/unity.yml` ejecuta en cada push: tests del núcleo (.NET), configuración del proyecto y generación de escenas con Unity real, tests EditMode/PlayMode y capturas (`evidence`). La build de Windows (`build-windows.yml`) es manual y solo se lanza cuando hay bastante para jugar.
