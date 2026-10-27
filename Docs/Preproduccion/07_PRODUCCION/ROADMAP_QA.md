# Roadmap con puertas de aprobación

## M00 — Auditoría y baseline (sin contenido final)
Verificar Unity 6 URP, Git, herramientas disponibles, Blender MCP real, fuentes, licencia, plataforma. Crear README, ADR, registro assets, QA/Evidence. **Salida:** proyecto abre y build vacía estable.

## M01 — Menú y vertical visual de fachada
Menú funcional, opciones persistentes, fachada/calle/coche/porche con calidad de referencia, luces noche. **Salida:** capturas y build, visual review humana.

## M02 — Casa completa y coherencia espacial
Arquitectura real y circulación de sala, dormitorio, baño, cocina, lavadero, patio y cobertizo; puertas, vistas, escalón y dos salidas. **Salida:** recorrido continuo de 3–5 min y plano actualizado. No aprobar solo graybox.

## M03 — Primeros 10–12 min jugables
Llegada, mensajes naturales, objetos, teléfono, objetivo contextual y rutinas domésticas. **Salida:** playtest ciego, sin necesidad de instrucciones externas.

## M04 — Evidencias y cronología dinámica
Movimientos de Víctor verificables, ruido/objetos causales, pistas opcionales, ramas de salida temprana. **Salida:** grabaciones desde posiciones alternativas; ninguna aparición imposible.

## M05 — Víctor y supervivencia
Personaje de calidad, locomoción, percepción, búsqueda, intimidación, escondites, dos rutas de salida y posibles ramas. **Salida:** 10 ejecuciones distintas sin teletransporte, 3 muertes reproducibles.

## M06 — Checkpoints y finales
Guardar/cargar snapshots completos, muerte/reintento, continuar, ramas, epílogo. **Salida:** matriz de regresión de 6 CP y 3 finales.

## M07 — Pulido y lanzamiento
Audio, iluminación, accesibilidad, rendimiento, pruebas externas, controles, créditos, licencias, pantalla de configuración, empaquetado. **Salida:** release candidate; revisión de derechos y marca antes de publicar.

## QA de cada hito
- TECH: compila, no errores graves, escenas/controles/guardado funcionan.
- VISUAL: captura comparada con referencia, proporciones, materiales, luz, composición.
- NARRATIVE: causalidad, cronología, lenguaje humano, motivaciones y salidas.
- GAMEPLAY: comprensión, ritmo, decisiones, justicia de IA, checkpoint, aburrimiento.
**Nunca marcar PASS en otra categoría por tener TECH PASS.**

## Forma de entrega del agente por hito
1. Qué cambió y dónde.
2. Evidencia: captura(s), video corto si aplica, logs/test.
3. Tabla PASS/FAIL/UNVERIFIED por dimensión.
4. Riesgos y siguiente tarea.
5. No inventar validación humana ni avanzar de hito con fallo bloqueante.

## Registro de riesgos
R01 IA persigue sin motivo → validar rutas y objetivos. R02 assets heterogéneos → guía materiales/escala. R03 primera mitad aburrida → playtest ciego. R04 checkpoints corruptos → snapshot atómico. R05 texto artificial → lectura humana. R06 licencia no clara → asset prohibido. R07 duración inflada → recortar. R08 nombre provisional sin clearance → revisar antes de comercializar.
