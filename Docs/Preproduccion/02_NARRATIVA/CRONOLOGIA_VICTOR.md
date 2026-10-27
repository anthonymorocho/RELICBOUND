# Cronología física de Víctor y reglas de continuidad

## Antes de la noche
- Semanas anteriores: descubre cierre deteriorado de acceso servicio; entra cuando Elena no está o duerme; guarda ropa y mochila en cobertizo. No habita paredes ni sótanos secretos.
- Día de la caída: Elena lo sorprende en cocina; Víctor retrocede hacia lavadero; Elena pierde pie en escalón junto a cocina/servicio. Él huye sin socorrerla. La caída no se graba en vídeo.
- Noche del episodio: vuelve por mochila con documentos y objetos personales. No sabe que Daniel llegará.

## Línea de tiempo de referencia, dependiente de jugador
- T0 Daniel aparca. Víctor llega al patio trasero o espera fuera del cobertizo, fuera de visión directa.
- T+3 a +8: Daniel en sala/dormitorio; Víctor accede brevemente a cobertizo desde patio. Ruidos proceden de puerta, nunca de una pared.
- T+8 a +15: Víctor intenta recoger pertenencias, derriba caja de lavadero; si Daniel se aproxima, se oculta en cobertizo o sale al patio mediante puerta funcional.
- T+15 a +25: si Daniel se acerca al cobertizo, Víctor permanece físicamente en uno de tres nodos (patio, cobertizo, lavadero), no aparece de la nada.
- Confrontación: Víctor prioriza mochila → salida. Si Daniel se aparta, Víctor huye. Si siente bloqueo físico, empuja/intimida, no asesina por defecto.
- Búsqueda: investiga última ubicación conocida, sonido y líneas de visión; abandona búsqueda si tiene ruta libre y mochila. No persigue por paredes.

## Sistema requerido
`IntruderState`: Outside, Shed, Laundry, RetrievingBag, Aware, Warning, Searching, Fleeing, AggressiveIfCornered, Gone. Guardar ubicación física, objetivo, mochila, visión, oído, última posición conocida. No cambiar estado sin transición y ruta de navegación válida. Telemetría DEV con estado, destino y causa de transición.

## Invariante narrativo
Cualquier objeto desplazado debe tener agente y momento plausible. Si la ruta de Víctor cruza una zona visible para Daniel, simular riesgo de ser visto, no forzar evento invisible. Si el jugador vigila la puerta, la escena debe responder: Víctor espera, rodea o no mueve el objeto. **No usar eventos guionizados incompatibles con observación.**
