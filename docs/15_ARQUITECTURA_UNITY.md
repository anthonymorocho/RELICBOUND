# Arquitectura Unity

## Versión
**Unity 6 LTS (6000.0.x)** con **URP**, ya que el proyecto empieza desde cero (D-001). Paquetes: `com.unity.render-pipelines.universal`, `com.unity.inputsystem`, `com.unity.nuget.newtonsoft-json`, `com.unity.ai.navigation`, `com.unity.addressables` (P2+), `com.unity.test-framework` y `com.unity.localization` (P10).

## Capas y ensamblados
| asmdef | Contenido | Puede referenciar |
|---|---|---|
| `Relicbound.Core` | Reglas puras: economía, progresión, perfil, guardado, arranque, cámara (matemáticas), más adelante simulación de combate | Solo Newtonsoft. **Sin UnityEngine** (`noEngineReferences`) |
| `Relicbound.Data` | ScriptableObjects de definiciones (edificios, máquinas, balance) que convierten a tipos de Core | Core, UnityEngine |
| `Relicbound.Game` | MonoBehaviours: directores de escena, rig de cámara, presentación de edificios, combate visual | Core, Data |
| `Relicbound.UI` | Vistas UI Toolkit y controladores | Core, Data, Game (solo interfaces) |
| `Relicbound.Audio` | Servicio de audio, mixer | Core |
| `Relicbound.Online` | Futuro: cuentas, castillos compartidos y validación (deshabilitado) | Core |
| `Relicbound.*.Tests` | EditMode / PlayMode | |

Core se prueba también **fuera de Unity**: `dotnet test Tests/Relicbound.Core.Tests` compila los mismos `.cs` con C# 9. Esto permite trabajar y validar la lógica en entornos sin editor.

## Escenas
`Boot` (splash, carga de guardado, perfil) → `Castle` (pantalla principal, aditiva con UI) → `Siege` (P4). La transición se hace con carga asíncrona y un velo de fundido, nunca con un menú intermedio.

## Guardado (implementado)
`SaveService` + `FileSaveStorage(Application.persistentDataPath)`: sobre JSON con versión y SHA-256, copia de la última partida válida, cuarentena del archivo dañado, migraciones v→v+1 sobre JSON crudo y escritura atómica (temporal + reemplazo). Guardar al: crear perfil, gastar recursos, cerrar panel de ajustes, `OnApplicationPause(true)` (Android) y `OnApplicationQuit`.

## Convenciones
- Namespaces `Relicbound.<Capa>.<Área>`. Comentarios y textos de diseño en español; identificadores en inglés.
- Cero `FindObjectOfType` en tiempo de juego; las dependencias se pasan desde el director de escena.
- Ninguna cifra de balance dentro de los scripts: todo sale de datos.
- C# 9 como máximo en Core (compatibilidad con Unity).
