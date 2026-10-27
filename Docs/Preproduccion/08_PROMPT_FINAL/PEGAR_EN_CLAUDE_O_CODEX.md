# PROMPT FINAL — PEGAR EN CLAUDE CODE O CODEX

Vas a desarrollar un videojuego de terror narrativo original en **Unity 6 URP**, titulado provisionalmente **BEFORE IT WAS YOU**, Episodio 01 **Alguien estuvo aquí**. Tienes a tu disposición un paquete de preproducción que contiene GDD, guion, cronología del intruso, plano de la casa, mecánicas, checkpoints, dirección visual, imágenes de referencia, fuentes de assets, arquitectura técnica y roadmap.

**NO te limites a resumir documentos, escribir otro plan ni preguntarme qué hacer a continuación. EMPIEZA A TRABAJAR.** Primero localiza la carpeta del paquete, lee `00_INICIO/LEEME_PRIMERO.md` y todos los documentos en el orden indicado. Si no tienes acceso a los archivos, solicítame que los coloque en el workspace; no inventes su contenido. Después inspecciona el proyecto Unity existente o crea uno nuevo únicamente si no existe. No borres, sobrescribas ni alteres otros proyectos. Usa Git y deja un baseline recuperable.

## Principios no negociables
1. Respeta la narrativa humana y el sentido común: Daniel puede salir de una casa peligrosa; Víctor debe tener motivos y rutas reales, nunca teletransportarse ni perseguir sin lógica.
2. La casa debe ser una vivienda creíble, con distribución física, salidas, ventanas y puertas coherentes. No entregar graybox como arte final.
3. Los diálogos, textos de teléfono y menús serán en español natural, sin frases de IA, sin comillas angulares, sin monólogos explicativos.
4. Dirección artística: realismo indie ligeramente estilizado, referencias visuales adjuntas. NO gráficos de juguete ni modelos humanos deformes.
5. Verifica si Blender MCP está realmente conectado. Si sirve, úsalo para geometría, props y adaptación; si no alcanza calidad para humanos, usa MakeHuman/MPFB, Mixamo o modelos gratuitos licenciados. No inventes que tienes acceso a MCP o descargas que no has realizado.
6. Busca assets reales cuando corresponda y documenta fuente/licencia. No uses imágenes de juegos comerciales como assets.
7. Implementa menú, pausa, opciones, teléfono, HUD contextual, IA, ramas y checkpoints según documentos. Un checkpoint restaura el mundo completo, no solo posición.
8. TECH PASS no implica VISUAL PASS, NARRATIVE PASS ni GAMEPLAY PASS. Sé crítico con tus propios resultados; no apruebes automáticamente.
9. Construye en orden de experiencia del jugador y de acuerdo con `07_PRODUCCION/ROADMAP_QA.md`. No saltes a persecuciones o finales con una introducción visualmente deficiente.
10. No uses la marca como si estuviera legalmente registrada o despejada; es provisional.

## Primer trabajo concreto, AHORA
- Audita workspace, versión Unity, URP, dependencias, Git y Blender MCP disponible. Reporta limitaciones sin bloquearte innecesariamente.
- Crea/ajusta estructura de proyecto, escenas Bootstrap/MainMenu/EP01_House, carpetas, scripts base y herramientas de evidencia.
- Implementa **M00** y empieza **M01**: menú funcional y fachada exterior con la dirección visual de `IMAGENES/01_moodboard_original.png`, no una pantalla negra genérica.
- El menú tendrá Nueva partida, Continuar solo con guardado válido, Opciones y Salir. Las opciones básicas deben funcionar.
- Construye una primera escena de llegada visualmente presentable, usando assets con licencias verificadas. Captura pruebas reales desde Unity; no reemplaces evidencias por renders conceptuales.
- Ejecuta pruebas, compara capturas con las referencias, detecta defectos, corrige y repite. Registra TECH/VISUAL/NARRATIVE/GAMEPLAY por separado.
- Si una tarea requiere decisión técnica, elige la solución más simple que cumpla calidad. Pregunta solo por bloqueos reales que impidan continuar.

## Entrega por hito
Deja cambios reales en el proyecto, no únicamente documentación. Incluye archivos cambiados, comandos de prueba, evidencias visuales, defectos encontrados, licencias de assets y estado honesto de QA. Si M01 no alcanza el nivel visual, continúa iterándolo antes de M02. No cierres otras instancias de Unity del usuario sin autorización.

**Empieza ya por inspeccionar el workspace y ejecutar M00/M01.**
