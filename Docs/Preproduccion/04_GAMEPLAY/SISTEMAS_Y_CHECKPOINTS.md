# Gameplay, terror y checkpoints

## Controles
WASD mover; ratón mirar; E interactuar; Shift correr con ruido; Ctrl agacharse; Tab teléfono; Esc pausa. Mostrar indicaciones contextuales solo cuando aporten información. Evitar barra permanente de resistencia salvo que pruebas la justifiquen.

## Sistema de interacciones
Raycast con distancia coherente, etiqueta contextual breve, animación/sonido; puertas con estados Open/Closed/LockedByRealReason y tiempo de apertura; cajones con contenidos persistentes. El jugador puede guardar objetos sin inspeccionar veinte cajones irrelevantes. Teléfono con mensajes diegéticos, no chat infinito.

## Terror
- Susto 1: ruido en lavadero con causa física (Víctor/caja), sin sonido orquestal exagerado.
- Susto 2: silueta de Víctor al cruzar umbral, pausa breve, sonido abrupto moderado y cámara configurable; debe existir físicamente antes del encuadre.
- Susto 3 opcional: Víctor visible a distancia tras vidrio, solo si su ruta lo permite.
- No spawnear detrás del jugador ni cortar controles durante huida. Alternar silencio, ruidos cotidianos y tensión real.

## IA Víctor
Visión por FOV, distancia y oclusión; oído por fuente/volumen/superficie; memoria de última posición; búsqueda por nodos alcanzables; ruta preferente mochila/salida; abandono si objetivo alcanzado. Estados en `CRONOLOGIA_VICTOR.md`. Animaciones de locomoción, mirar, detenerse, recoger mochila, apartar/amenazar. Fallo solo por contacto confirmado o ataque telegráfico con ventana de reacción; evitar hitbox injusta.

## Checkpoints propuestos (ajustables tras playtest)
- CP00: entrada a la casa (permite Continuar desde comienzo).
- CP01: después de reunir pertenencias principales; sin repetir intro.
- CP02: primer indicio fuerte y estado de investigación.
- CP03: antes del encuentro en lavadero/cobertizo; guardar posición real de Víctor.
- CP04: tras sobrevivir primer cruce, antes de búsqueda activa.
- CP05: antes de convergencia final en patio/portón, **solo si existe tal secuencia en la rama**.
No todos deben dispararse en todas las ramas. **No guardar durante persecución, en campo visual directo del enemigo, ni en un estado imposible de recuperar.**

## Datos persistentes por checkpoint
ID capítulo, versión de esquema, escena, spawn y rotación, objetivo, objetos, estados puertas/interruptores/cajones, mensajes leídos y enviados, flags narrativos, rama, ubicación/estado/meta de Víctor, mochila, tiempo de escena si relevante, accesibilidad/configuración, RNG semilla si necesaria. Escritura atómica (temporal + rename) y respaldo de slot; no sobrescribir un save válido con uno corrupto.

## Muerte
Captura breve, fade, menú `Reintentar desde el último checkpoint` / `Menú principal`; reinicio rápido; sin repetir textos ni escenas ya vistos; restauración íntegra. Testear 3 muertes consecutivas por CP, muerte durante transición, salir a escritorio y Continuar, y carga tras cambio de versión.

## Pausa y opciones
Esc pausa congela lógica y audio según diseño, permite continuar, opciones y menú con confirmación si pierde progreso posterior al CP. Ajustes de gráficos, resolución, pantalla completa, audio, sensibilidad, FOV, sacudida de cámara, tamaño de subtítulos y accesibilidad.

## Rama salida temprana
Si Daniel llega a la calle o coche con ruta válida, no teletransportar a Víctor para impedirlo. Termina con escena breve de llamada/aviso y epílogo reducido. Si faltan objetos, puede decidir irse y pedir ayuda. El objetivo es sobrevivir y actuar racionalmente, no forzar 45 min.
