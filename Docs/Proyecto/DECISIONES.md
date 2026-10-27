# Registro de decisiones (ADR)

## ADR-001 · Archivo de RELICBOUND y nuevo proyecto (2026-10-27)
El usuario pidió borrar RELICBOUND y empezar BEFORE IT WAS YOU en el mismo repositorio. Antes de borrar, todo RELICBOUND (incluido el trabajo sin terminar) quedó en la rama `archivo/relicbound`. Nada se perdió.

## ADR-002 · Proyecto local del usuario y Unity MCP
El proyecto local del usuario (con MCP for Unity) está en su PC y no es accesible desde la sesión en la nube. Se recrea en el repositorio la misma estructura y configuración pedida (Unity 6000.3.27f1, URP con `PC_RPAsset`, Windows DX12, `Assets/_Game/...`, escenas `L_Startup` y `L_E01_Arrival`, `Player_Daniel` con sus parámetros) mediante scripts de editor reproducibles. La verificación con Unity real se hace en GitHub Actions (GameCI): compilación, tests y capturas.

## ADR-003 · Escenas: L_Startup + L_E01_Arrival (frente a Bootstrap/MainMenu/EP01_House de ARQUITECTURA_UNITY)
El prompt maestro fija `L_Startup` y `L_E01_Arrival` como escenas existentes; se conservan. `L_Startup` muestra el aviso de contenido mientras carga el episodio. El menú principal vive **dentro de** `L_E01_Arrival`, sobre la casa real (cámara cinematográfica): Nueva partida pasa al prólogo y a la llegada sin otra carga, y el fondo del menú es siempre la escena verdadera (DIRECCION_ARTISTICA §UI). El epílogo será una secuencia de UI, como permite ARQUITECTURA_UNITY.

## ADR-004 · Núcleo sin dependencias de Unity
Flujo, guardado y opciones están en `BeforeItWasYou.Core` (`noEngineReferences`), probados con NUnit fuera de Unity. Guardado: sobre con formato, versión y SHA-256; escritura atómica (temporal + replace); copia de respaldo del último checkpoint válido; cuarentena del archivo dañado; migraciones por versión. Nunca se pisa un respaldo válido con un archivo corrupto.

## ADR-005 · DirectX 12 con reserva DirectX 11
Windows usa D3D12 como API principal. D3D11 queda en segunda posición solo como reserva automática para equipos sin soporte DX12; no cambia el comportamiento en equipos compatibles.

## ADR-006 · Entrada y menús
Input System exclusivo (`activeInputHandler = 1`). Acciones creadas en código (`GameInput`) con teclado/ratón y mando. Los menús usan navegación propia (flechas/WASD, Enter, Esc, ratón) para controlar el resaltado rojo del moodboard sin depender del foco de UI Toolkit.

## ADR-007 · Binarios sin Git LFS
Texturas, modelos, audio y fuentes van en Git normal marcados como binarios. Motivo: cada ejecución de la CI hace checkout completo y la cuota de ancho de banda de LFS gratuita se agotaría.

## ADR-008 · Arte: assets CC0 verificados + modelado propio
Tras la experiencia de RELICBOUND (modelado procedimental con cajas = techo de calidad bajo), el arte se basa en materiales PBR escaneados y modelos CC0 verificados uno a uno (Poly Haven, ambientCG), y la arquitectura de la casa se modela en Blender con medidas reales, molduras, marcos y detalles. Cada asset se registra en `REGISTRO_ASSETS.csv` con URL, autor, licencia y fecha.

## ADR-009 · Distribución de la parcela (plano técnico)
La planta conceptual del paquete es topológica. Se fija una distribución a escala (ver `PLANO_TECNICO.md`) con la entrada de coches al oeste para que el encuadre del menú coincida con el moodboard (coche en primer plano a la izquierda, fachada a la derecha). El portón lateral queda al final de la entrada de coches: la ruta B (cocina → lavadero → patio → portón → calle) es continua y no cruza el interior.

## ADR-010 · FOV vertical
La opción Campo de visión es el FOV vertical de la cámara (55–85°, 64° por defecto), el mismo valor que muestra Unity. Un FOV moderado favorece la tensión en interiores sin marear.
