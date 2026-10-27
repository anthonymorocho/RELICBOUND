# Edificios y mejoras

Cada edificio tiene una función concreta. Las definiciones van en ScriptableObjects `BuildingDefinition` con una tabla de niveles (`BuildingLevel`: coste, requisito de nivel del castillo central, estadísticas y prefab visual).

| Edificio | Función | Desbloqueo (nivel de jugador) |
|---|---|---|
| Castillo central | Limita el nivel del resto; su nivel = nivel de fortaleza | 1 |
| Muralla | Bloquea el paso; vida por sección | 1 |
| Puerta | Paso del defensor; objetivo del ariete | 1 (reforzada: 4) |
| Taller de asedio | Construye y mejora máquinas | 2 |
| Almacén | Capacidad de recursos | 2 |
| Aserradero / Cantera | Producción moderada de madera/piedra | 3 |
| Torre de arqueros | Defensa contra unidades | 5 |
| Cuartel | Entrena unidades ofensivas | 5 |
| Fundición | Producción de hierro | 6 |
| Herrería | Investigación de armas y munición | 6 |
| Torre de ballestas | Defensa contra máquinas | 9 |
| Campo de entrenamiento | Mejora de unidades | 11 |
| Torre de artillería | Defensa de área | 14 |
| Trampas | Defensas ocultas | 12 |
| Patio de estandartes | Bonificación defensiva de área | 16 |

## Reglas de mejora
- Un edificio no puede superar el nivel del castillo central.
- Cada mejora indica antes de confirmar: coste, beneficio numérico y **cambio visual**.
- Cambio visual por bandas: murallas (madera → piedra tosca → sillería → sillería con contrafuertes y matacanes), torres (techo de madera → tejado de pizarra → mecanismo visible), castillo central (torreón → torreón con muralla interior → ciudadela). **Nunca** solo cambio de color.
- Sin temporizadores largos: las mejoras de los niveles 1 a 10 son instantáneas o tardan como máximo 5 min (D-009).
