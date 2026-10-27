# Campaña PvE (P6)

| Región | Tema visual | Mecánica nueva |
|---|---|---|
| 1. Frontera Verde | Prados, empalizadas que pasan a piedra | Fundamentos: brecha y entrada |
| 2. Colinas de Piedra | Desniveles, murallas en terrazas | Altura: alcance y ángulo importan |
| 3. Tierras de Hierro | Forjas, torres con armadura | Munición perforante, torres blindadas |
| 4. Paso Helado | Nieve, hielo, puentes | Objetivos móviles, tiempo limitado |
| 5. Reino de Ceniza | Volcánico, fuego | Defensas incendiarias, múltiples recintos |

Cada región tiene de 6 a 8 fortalezas y un jefe (fortaleza de varios recintos).

## Variedad obligatoria por fortaleza
Distribución de murallas, posición de torres, tipo de puerta, terreno, defensas, recursos de despliegue, **condición de victoria** (destruir el castillo central, capturar el estandarte, destruir N torres, sobrevivir X s con la máquina intacta) y un objetivo secundario opcional.

## Dificultad
Se sube con **mecánicas y disposición**, no multiplicando la vida. Regla: un multiplicador de vida máximo de ×1,5 entre la primera y la última fortaleza de una misma región.

## Puntuación (local, offline)
`objetivo (base) + tiempo + precisión (impactos útiles / disparos) + recursos ahorrados + máquinas conservadas + secundarios`, de 0 a 3 estrellas. Los rankings mundiales PvE solo existirán con desafíos idénticos y validación de servidor (P11).

## Formato de datos
`FortressDefinition` (ScriptableObject): layout de edificios (el mismo formato que el castillo del jugador), defensas, presupuesto de despliegue, condiciones y recompensas de primera victoria y de repetición.
