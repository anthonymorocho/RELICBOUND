# Construcción de castillos (P3 — no iniciar aún)

## Modelo
Cuadrícula de celdas de **1 m**. Cada edificio tiene una huella rectangular en celdas, un pivote en la esquina mínima y una rotación en múltiplos de 90°. El castillo central está fijo en el centro del terreno.

| Nivel de fortaleza | Terreno | Notas |
|---|---|---|
| 1 | 40 × 40 | Hipótesis: validar visualmente con cámara a zoom máximo |
| 4 | 48 × 48 | |
| 7 | 56 × 56 | |
| 10 | 64 × 64 | Validar el rendimiento en Android de gama media |

## Huellas iniciales
| Edificio | Celdas |
|---|---|
| Castillo central | 8 × 8 |
| Torre | 3 × 3 |
| Sección de muralla | 1 × 2 (se encadena) |
| Puerta | 2 × 4 |
| Cuartel / Taller | 5 × 5 |
| Producción (aserradero, cantera, fundición) | 4 × 4 |
| Almacén | 4 × 4 |
| Decoración | 1 × 1 a 2 × 2 |

## Reglas de colocación
1. Dentro de los límites. 2. Sin solaparse con otras huellas. 3. Un pasillo de 1 celda alrededor de los edificios grandes (no en murallas). 4. **Conectividad:** desde la zona de despliegue tiene que existir una ruta hasta el castillo central atravesando como máximo puertas (BFS sobre la cuadrícula tratando las puertas como transitables). Si la colocación la rompe, se marca como inválida. 5. Las murallas se autoconectan con las vecinas (esquina, recta, T, cruz).

## Flujo
Elegir del catálogo → fantasma semitransparente sobre la cuadrícula (verde si es válido y rojo si no, con el motivo) → mover (arrastrar en móvil, seguir el ratón en PC) → rotar (R o botón) → confirmar o cancelar → persistir en `SaveGame.Castle` (llega en P3 con una migración v1→v2).

## Persistencia
Lista de `{buildingId, defId, level, x, y, rotation}`. Nunca se guardan referencias a GameObjects.
