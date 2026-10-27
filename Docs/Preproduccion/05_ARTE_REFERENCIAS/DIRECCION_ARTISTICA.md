# Dirección artística y guía de UI

## Norte visual
Realismo indie ligeramente estilizado, proporciones humanas reales, mobiliario variado, PBR moderado, luz de tungsteno interior y azul de noche exterior, sombras legibles. No filtro VHS fuerte, niebla permanente ni colores saturados. Luz suficiente para entender salidas y leer siluetas. Exterior pequeño pero convincente (fachada, acera, árboles, coches lejanos).

## Moodboard original
`IMAGENES/01_moodboard_original.png` — exterior, sala, lavadero, menú, pausa, HUD. Es **concept art**, no UI implementada ni textura utilizable sin adaptar. Revisar errores de texto generados en imagen: toda tipografía final debe ser UI real de Unity, nunca texto incrustado en captura.

## Casa
- Fachada: casa familiar de una planta, porche, coche estacionado, patio lateral creíble.
- Sala: fotos familiares, sofá usado, luz cálida, objetos heterogéneos y circulación libre.
- Cocina: utensilios, calendario, comida corriente, zona de paso lavadero; no mansión ni ruina.
- Lavadero: lavadora, productos de limpieza, estanterías y acceso de servicio coherente.
- Cobertizo: herramientas, manta, botella y mochila; señales discretas de uso, no guarida de asesino.

## UI
- Menú principal: escena 3D real o imagen de escena aprobada; logo BEFORE IT WAS YOU, subtítulo episodio, Nueva partida, Continuar solo si save válido, Opciones, Salir; navegación por teclado.
- Pausa: fondo de escena atenuado o desenfocado con control de legibilidad, botones claros; no congelar screenshot a baja resolución.
- HUD: casi invisible. Interacción solo al apuntar, objetivo temporal tras actualización, notificación pequeña de mensaje; teléfono abierto por Tab y cerrable. No barra de vida/mini mapa ni misiones enormes.
- Pantalla fallo: sobria, con reintento prioritario; no jumpscare repetido durante carga.
- Accesibilidad: contraste, tamaños, alternativas a iconos exclusivamente cromáticos.

## Personajes
Daniel: primera persona, manos solo si calidad lo permite. Elena: puede aparecer en epílogo mediante encuadre limitado para ahorrar rigging. Lucía: principalmente mensajes. Víctor: humano proporcionado, ropa gastada plausible, sin máscara ni ojos sobrenaturales. **No aceptar maniquí de Blender como personaje final.** Mixamo para animación y MakeHuman/MPFB o asset humano riggeado con licencia adecuada.

## Assets y licencias
Leer `REFERENCIAS_EXTERNAS/FUENTES_Y_LICENCIAS.md`. Mantener `asset_register.csv` con nombre, URL, autor, licencia exacta, captura/evidencia, modificaciones y ubicación. No usar recursos de referencia de juegos como assets. No asumir que gratis significa libre para cualquier uso.

## Puertas de calidad
En cada habitación: captura a altura de ojos con luces diurnas/nocturnas; evaluación de escala, texturas, variedad, lectura de ruta, clipping, repetición y rendimiento. Rehacer elementos pobres antes de expandir el mapa.
