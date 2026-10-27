# Estado del proyecto

Fecha: 2026-10-27 · Hito actual: **M00 Auditoría y baseline** → inicio de **M01 Menú y fachada**.

## Hecho
- RELICBOUND archivado en la rama `archivo/relicbound`; repositorio limpio para BEFORE IT WAS YOU.
- Paquete de preproducción copiado a `Docs/Preproduccion` y auditado (`AUDITORIA_M00.md`).
- Proyecto Unity 6000.3.27f1 con URP: `PC_RPAsset` (Alta) + variantes Baja/Media/Ultra, renderer Forward+ con SSAO, DX12, Input System, capas.
- Núcleo C# probado (14 tests): flujo de juego, guardado por snapshots con respaldo y cuarentena, opciones persistentes.
- Sistemas: jugador en primera persona (caminar, correr, agacharse, gravedad, balanceo ajustable, pasos por superficie, ruido para la IA), interacción por mirada (puertas con bisagra, interruptores), buses de audio.
- Interfaz real (UI Toolkit): menú principal estilo moodboard, Continuar solo con guardado válido, confirmación de Nueva partida, Opciones completas, Créditos, Salir, pausa, prólogo con subtítulos, HUD mínimo.
- Escenas generadas por script: `L_Startup` (aviso de contenido + carga) y `L_E01_Arrival` (menú + episodio, `Player_Daniel` con los parámetros pedidos).

## Episodio completo implementado (pendiente de verificación con Unity en CI)
- **Fase 1 Identidad y arranque:** aviso de contenido, menú sobre la fachada real, Continuar solo con guardado válido, Nueva partida con confirmación, Opciones, Créditos, Salir.
- **Fase 2 Prólogo:** pantalla negra con tono de cocina y el diálogo limpio de Elena y Daniel.
- **Fase 3 Llegada:** Daniel junto a su coche; Lucía escribe (A1); la puerta principal se abre con la llave; CP00 al entrar.
- **Fase 4 Exploración:** casa completa a oscuras con interruptores por habitación; carpeta azul, cargador y bolsa de ropa; conversación A2 que cambia según lo que Daniel encuentra; CP01.
- **Fase 5 Inquietud:** lata abierta en la cocina, taza ajena en el lavadero, caja que cae cuando Víctor entra (solo si Daniel está lejos), puerta de servicio que no engancha, mensaje opcional A3 a Lucía.
- **Fase 6 Peligro:** refugio en el cobertizo (manta, cartones, latas, mochila con el carnet de Víctor Salas, CP02), encuentro en la puerta (diálogo exacto del guion, CP03), dos ramas (apartarse → Víctor coge la mochila y se va; bloquear o coger la mochila → empujón y huida/búsqueda con ataque avisado), escondite en el armario de Elena, CP04, muerte con pantalla de fallo y reintento.
- **Fase 7 Desenlace:** llamada a emergencias desde el móvil (tecla 3) con respuesta realista; cuatro finales (temprano, prudente, retirada sin confrontación, supervivencia); epílogo en el hospital y créditos.
- **Audio:** pasos por superficie, puertas, interruptores, caja, vibración del móvil, grillos y viento amortiguados dentro de casa.
- **Víctor:** humano MakeHuman (CC0) con animaciones CC0 de Quaternius, NavMesh, visión con oclusión, oído con paredes, memoria de última posición; nunca se teletransporta.

## Pendiente de decisión del usuario
- Nombre a mostrar en créditos (dirección/desarrollo). Por ahora los créditos solo listan motor, tipografías y assets de terceros.

## QA por dimensión
| Dimensión | Estado | Nota |
|---|---|---|
| TECH | UNVERIFIED | pendiente de la primera ejecución de la CI con Unity |
| VISUAL | FAIL | la fachada aún no existe; no se entregan capturas como finales |
| GAMEPLAY | UNVERIFIED | solo movimiento y menús |
| NARRATIVE | UNVERIFIED | prólogo implementado con el texto limpio del guion |
