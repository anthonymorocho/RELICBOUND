# Auditoría M00 — contradicciones y riesgos del paquete

| # | Tema | Documentos | Resolución |
|---|---|---|---|
| 1 | Nombres de escenas | ARQUITECTURA_UNITY (Bootstrap/MainMenu/EP01_House) vs prompt maestro (L_Startup, L_E01_Arrival) | Se mantienen las del prompt maestro (ADR-003). |
| 2 | Planta | 02_planta_propuesta es topológica, sin escala | Plano a escala en `PLANO_TECNICO.md` (ADR-009). |
| 3 | Orden del menú | Moodboard: Nueva partida, Continuar, Opciones, Salir · Prompt: añade Créditos | Continuar aparece primero cuando hay partida (es la acción más probable); Créditos antes de Salir. |
| 4 | Línea del prólogo con comillas angulares | GUION §P0 | Se usa solo la versión limpia. |
| 5 | Blender MCP | LEEME_PRIMERO pide verificarlo | No disponible en la nube; se usa Blender 4.2 headless con scripts versionados en `Tools/`. |
| 6 | Mixamo | DIRECCION_ARTISTICA | Requiere cuenta de Adobe; no accesible desde la nube. Alternativas para Víctor: MakeHuman/MPFB (CC0) y animación propia o CC0; se decidirá en M05. |
| 7 | Marca provisional | LEEME_PRIMERO, R08 | El título es provisional; revisar clearance antes de comercializar. |
| 8 | Escalón cocina–lavadero | GDD, CRONOLOGIA, CASA_Y_FLUJOS | Coherente en los tres; se modela con 0.18 m. |
| 9 | Cobertura del móvil | LEEME_PRIMERO (sin móviles sin cobertura porque sí) | El teléfono funciona siempre; las llamadas a emergencias tendrán respuesta realista. |
