# Guion jugable y diálogos — versión de producción v0.1

> **Importante:** escenas y textos son borrador completo de referencia; las frases definitivas requieren lectura en voz alta, pruebas de contexto y revisión lingüística. No insertar comillas angulares en UI.

## P0 — Prólogo, días antes (1–3 min)
Pantalla negra y sonidos cotidianos de cocina. Fragmento de llamada o mensaje guardado; evitar cinemática costosa.
**Versión para implementar, sin comillas angulares:**
- Elena: La puerta del lavadero volvió a quedar abierta.
- Daniel: ¿La de atrás? A veces no engancha bien.
- Elena: Ya sé. Pero ayer la cerré.
- Daniel: El sábado paso y la reviso, ¿sí?
- Elena: Bueno, hijo.
Corte a presente. No insinuar que Elena está delirando.

## A1 — Llegada (min 0–6)
Daniel aparca. Fachada con luz de porche. Lucía escribe:
- Lucía: ¿Ya llegaste?
- Daniel: Sí. Voy entrando.
- Lucía: La carpeta azul está en la cómoda de mamá. Creo.
- Daniel: Voy a ver.
- Lucía: Y el cargador, porfa. Aquí solo hay uno y lo estamos turnando.
Objetivo visible solo unos segundos: Recoge las cosas de mamá.
Jugabilidad: entrar con llave, recorrer sala, encender luz, reconocer cocina y pasillo. Si vuelve al coche sin objetos: Daniel responde por mensaje que aún falta la carpeta; puede decidir salir si siente peligro más adelante.

## A2 — Rutina familiar (min 6–12)
Encontrar bolsa de ropa, carpeta y cargador (orden flexible). Fotos y recibos opcionales, sin textos extensos. Elena guardaba la carpeta en lugar razonable. Una puerta que cuesta encajar se presenta sin convertirla en bloqueo. Lucía:
- Lucía: ¿La encontraste?
- Daniel: La carpeta sí. Me falta el cargador.
- Lucía: Debe estar junto a la cama.
Al cruzar pasillo se oye un ruido leve. Daniel no pronuncia frase de terror; puede ser casa o patio. En lavadero aparece caja desplazada si Víctor pasó por allí según cronología.

## A3 — Primeras pruebas (min 12–21)
El jugador puede notar comida abierta, una taza fuera de lugar y una manta doblada en cobertizo. Ningún indicador brillante de pista. Ruido en servicio, puerta del lavadero que no cierra bien. Mensaje opcional a Lucía:
- Daniel: ¿Mamá dejó a alguien revisando la puerta de atrás?
- Lucía: Que yo sepa, no. ¿Por?
- Daniel: Nada. Está abierta otra vez.
- Lucía: Cierra bien antes de irte.
**Punto de elección:** si jugador considera la situación peligrosa y sale por la puerta principal con objetos, termina por rama de salida prudente; no lo penalizar. Si llama a emergencias, se activa respuesta coherente y salida segura si logra llegar a exterior.

## A4 — Cobertizo y encuentro (min 21–30)
Entra a lavadero/cobertizo porque falta la bolsa con medicinas o porque oye un ruido, pero no crear un objeto arbitrario imprescindible: el jugador puede optar por no entrar. Encuentra refugio improvisado y mochila de Víctor. No manipular mochila obligatoriamente. Víctor entra desde patio y se detiene al verlo; susto de figura inmóvil 0,7–1,5 s sin teletransporte.
- Víctor: No toques eso.
- Daniel: ¿Quién eres?
- Víctor: Es mío. Lo agarro y me voy.
- Daniel: ¿Estabas viviendo aquí?
- Víctor: No quiero problemas.
Daniel puede apartarse y dejarlo salir: **rama de no confrontación**, con Víctor escapando, Daniel llama a policía y se resuelve. Si Daniel intenta bloquearle o queda entre él y mochila, Víctor avanza para apartarlo. Debe haber rutas abiertas; evitar protagonista irracional.

## A5 — Supervivencia y escape (min 30–42)
Rama principal de mayor peligro: Víctor cree que Daniel lo va a detener o denunciar mientras se lleva mochila; ambos quedan separados por la puerta del servicio y el patio. Víctor intenta llegar al portón, Daniel debe encontrar salida sin cruzarse. La búsqueda ocurre tras perder contacto visual, por últimas posiciones/ruidos. Un escondite o desvío permite evitarlo. Segunda tensión breve en puerta/portón solo si rutas convergen, nunca una persecución infinita. Llamar a emergencias o huir a la calle es desenlace válido.

## A6 — Epílogo (min 42–48 o antes según rama)
Días después, hospital. Texto ambiental, sin modelar hospital entero: plano fijo sobrio o imágenes y mensajes. Elena recuperándose.
- Elena: ¿Sacaste la ropa?
- Daniel: Sí. Y los papeles.
- Elena: ¿Revisaron la casa?
- Daniel: Sí, ma. La policía ya fue.
- Elena: Yo te decía que había alguien.
- Daniel: Sí. Tenías razón.
- Elena: Pensé que no me creían.
- Daniel: No te escuchamos. Perdón.
Texto final breve: En el cobertizo encontraron ropa, envases y señales de uso prolongado. Elena se recuperó de la caída. La familia cambió cerraduras y reforzó el acceso; el futuro de la casa queda abierto. No afirmar condena o captura si el juego no la muestra.

## Variantes finales
- **Escape prudente:** Daniel sale al ver indicios, pide ayuda; Víctor puede escapar. Final más corto, legítimo.
- **Retirada sin confrontación:** Víctor recoge mochila, se va; Daniel avisa a autoridades.
- **Supervivencia tensa:** Daniel se cruza con Víctor, escapa por ruta alternativa y llama a emergencias.
- **Muerte:** pantalla de fallo y reintento; no es final canónico salvo diseño futuro.

## Escritura prohibida
No usar comillas angulares, frases como Nunca debí entrar aquí, No estoy solo, Esto no puede estar pasando, monólogos explicativos, objetivos redundantes en mensajes, nombres repetidos en cada línea, diálogos con conocimiento imposible. Evitar la línea entre comillas angulares del borrador P0; solo implementar versión limpia.
