# Casa — propuesta arquitectónica verificable

## Escala y criterio
Vivienda modesta de una planta, con pequeño escalón en unión cocina/lavadero (caída de Elena). Fachada y calle al sur; patio/cobertizo al norte. Puerta frontal y portón lateral conectan con calle. Lavadero comunica cocina y patio; cobertizo se abre al patio. **No es un laberinto.**

## Espacios
1. Entrada/sala (5 x 4 m aprox.): sofá, TV, fotos, puerta frontal, visión al pasillo.
2. Pasillo central (1.2 m ancho mínimo): une sala, dormitorio, baño y cocina.
3. Dormitorio Elena (3.5 x 3.5 m): cómoda con carpeta azul, mesita con cargador, armario ropa.
4. Baño (2.2 x 2.4 m): uso doméstico, no zona de combate.
5. Cocina/comedor (4.2 x 3.8 m): nevera, fregadero, mesa, escalón hacia lavadero.
6. Lavadero (2.5 x 3.2 m): lavadora, estanterías, puerta a patio con cierre defectuoso; evitar dos lavadoras si no tiene sentido.
7. Patio (6 x 7 m): iluminación exterior, portón lateral hacia calle, acceso a cobertizo.
8. Cobertizo (2.8 x 3 m): refugio improvisado y mochila; una única puerta al patio.

## Rutas válidas
A: dormitorio → pasillo → sala → puerta frontal → calle.
B: cocina → lavadero → patio → portón lateral → calle.
C: cobertizo → patio → lavadero → cocina → sala, solo si hay puertas abiertas.
El portón puede tardar 2–3 segundos en abrir si fue mostrado previamente; **nunca bloquearlo sin razón**. Si el jugador huye por A mientras Víctor está en patio, debería escapar.

## Comprobaciones de plano
- Vista desde ventanas: coherente con exterior.
- Cada puerta tiene marco, bisagra, recorrido de apertura y destino visible.
- Distancias permiten IA y jugador sin colisiones absurdas.
- Escondites: armario de ropa o detrás de muebles solo si cabe cuerpo humano y hay animaciones/cámara apropiadas.
- Ningún recorrido obliga a pasar por Víctor cuando existe otra salida físicamente disponible.
- Audio oclusión razonable entre patio/lavadero/cocina.

Ver `IMAGENES/02_planta_propuesta.png` (esquema topológico, no plano ejecutivo a escala). Antes de modelar: bloquear layout con medidas reales, testear navegación y luego realizar **visual pass**; graybox interno permitido solo como prototipo descartable, nunca como entrega visual.
