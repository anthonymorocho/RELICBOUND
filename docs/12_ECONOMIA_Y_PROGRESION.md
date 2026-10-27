# Economía y progresión

Todos los números viven en datos (ScriptableObjects de balance). Los valores de esta tabla son **hipótesis** y se ajustarán con pruebas.

## Recursos
| Recurso | Uso | Capacidad inicial |
|---|---|---|
| Oro | Mejoras generales, entrenamiento | 5 000 |
| Madera | Construcciones, máquinas | 5 000 |
| Piedra | Murallas, estructuras | 5 000 |
| Hierro | Armamento, defensas avanzadas | 5 000 |
| Gemas | Solo cosméticos | Sin límite |

Recursos iniciales: 1 000 de oro, 800 de madera, 600 de piedra, 0 de hierro y 50 gemas (`StartingRules.CreateDefault`). Bajar la capacidad nunca borra recursos.

## Curva de nivel (implementada: `LevelCurve.CreateDefault`)
XP para pasar del nivel *n* al *n+1* = `round(100 · n^1.6, decenas)`. Nivel 1→2: 100; 2→3: 300; 5→6: 1 310; 10→11: 3 980; 29→30: 21 870. Total hasta el 30 ≈ 255 k XP.

Fuentes de XP orientativas: victoria PvE 60–200 · primera victoria ×2 · objetivo secundario 30 · mejora de edificio = 5 % de su coste en oro / 10 · desafío semanal 500.

## Desbloqueos 1–30
| Nv | Desbloqueo | Nv | Desbloqueo |
|---|---|---|---|
| 1 | Castillo, murallas básicas, catapulta | 16 | Patio de estandartes |
| 2 | Taller de asedio, almacén | 17 | Munición de dispersión |
| 3 | Murallas mejoradas, aserradero, cantera | 18 | Región 4 |
| 4 | Puerta reforzada | 19 | Investigación defensiva nivel III |
| 5 | Torre de arqueros, cuartel | 20 | Terreno 64 × 64, apariencia de ciudadela |
| 6 | Piedra pesada, herrería, fundición | 21 | Especialista de asedio |
| 7 | Terreno 48 × 48, región 2 | 22 | Torre de artillería nivel II |
| 8 | Trabuquete | 23 | Trampas avanzadas |
| 9 | Prioridades de defensa, torre de ballestas | 24 | Región 5 |
| 10 | Nueva apariencia del castillo central | 25 | Ballesta pesada nivel II |
| 11 | Campo de entrenamiento, zapadores | 26 | Muralla de sillería reforzada |
| 12 | Trampas, munición incendiaria | 27 | Máquinas: segunda ranura de munición |
| 13 | Ballesta pesada, región 3 | 28 | Investigación ofensiva nivel IV |
| 14 | Torre de artillería, terreno 56 × 56 | 29 | Defensores de élite |
| 15 | Ariete, munición perforante | 30 | Apariencia de fortaleza legendaria (cosmética) |

## Investigación
Dos árboles (ofensivo y defensivo) de 4 niveles por rama. Cada nivel desbloquea **una opción táctica**, no solo un porcentaje. Por ejemplo: «Recarga II: los disparos consecutivos a la misma sección hacen +15 %» o «Puertas reforzadas II: la puerta se cierra de nuevo una vez tras romperse».

## Sin esperas abusivas
Producción pasiva con un tope de 8 h de acumulación. Las mejoras de los niveles 1 a 10 son inmediatas o tardan como máximo 5 min, y las de los niveles 11 a 30 como máximo 2 h. Nunca se puede pagar con gemas para saltarse una espera (D-009).
