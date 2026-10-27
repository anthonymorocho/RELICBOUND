# Arquitectura Unity 6 URP — contrato técnico

## Estructura
`Assets/_Game/{Scenes,Scripts/{Core,Player,Interaction,Narrative,AI,Save,UI,Audio},Prefabs,Art,Materials,Audio,Data,Tests}`. Escenas: `Bootstrap`, `MainMenu`, `EP01_House`, `EP01_Epilogue` (epílogo puede ser secuencia de UI). ScriptableObjects para eventos/objetivos/checkpoints; escenas desacopladas de datos.

## Servicios
- GameFlow: estados Boot/Menu/Playing/Paused/Failed/Epilogue.
- NarrativeDirector: eventos condicionados por hechos, no únicamente temporizadores.
- InteractionSystem: interactuables con validación de alcance y estado.
- PhoneUI: conversaciones, mensajes persistentes, avisos discretos.
- IntruderAI: FSM + NavMesh, percepción y debug telemetría.
- SaveManager: snapshots completos versionados, restauración determinista.
- AudioDirector: música escasa, capas de ambiente, oclusión simplificada.
- AccessibilitySettings: persistencia y aplicación de opciones.

## Calidad y pruebas
Unity Test Framework EditMode para serialización, flags, transiciones; PlayMode para checkpoints, puertas, menú, muerte y recuperación. Herramienta DEV para saltar CP y visualizar estados IA, excluida de build release. Capturas de cada escena, FPS y frame time. No exigir asset generado con Blender MCP si no está disponible: verificar conexión y exportación, documentar fallback.

## No hacer
No usar nombres de assets de proyectos previos, no depender de paquetes sin licencia, no esconder problemas con black screens, no acoplar diálogos a triggers que se repiten tras carga, no hacer guardado parcial de solo posición, no usar eventos de miedo que ignoran presencia del jugador.
