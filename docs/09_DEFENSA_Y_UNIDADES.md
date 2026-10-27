# Defensa y unidades (P8; la primera unidad ofensiva llega en P4)

## Origen de los personajes
Humanoides con rig y animaciones de packs con licencia verificada (candidatos en `19`), con identidad propia por armadura, casco, escudo, colores y estandarte. **No se esculpen personajes desde cero** (D-007). Un único esqueleto humanoide (Mecanim Humanoid) para poder reutilizar animaciones.

## Ofensivas
| Unidad | Rol | Fase |
|---|---|---|
| Infantería de asalto | Entra por las brechas y ataca edificios | P4 |
| Zapadores | Abren pasos y desactivan trampas | P7 |
| Portaestandarte | Aura de moral (+velocidad) | P7 |
| Apoyo (curandero de campo) | Cura unidades cercanas | P7 |
| Especialista de asedio | Repara y acelera las máquinas | P7 |

## Defensivas
| Unidad | Posición | Rol |
|---|---|---|
| Arquero de muralla | Sobre muralla o torre | Contra unidades |
| Guardia de puerta | Detrás de la puerta | Contiene brechas |
| Ballestero | Torre de ballestas | Contra máquinas |
| Ingeniero defensivo | Patio | Repara secciones sin colapsar |
| Guardia del estandarte | Patio de estandartes | Último reducto |

## Configuración defensiva
Asignar unidades a posiciones fijas (*slots* en murallas y torres), prioridad de objetivo por torre (más cercano, máquinas, unidades o menor vida) y colocación de trampas. Todo se guarda como datos y se valida con presupuestos (`11`).

## IA defensiva
Reglas deterministas y públicas, sin sorpresas: cada torre evalúa objetivos en alcance cada 0,25 s según su prioridad configurada y las unidades defensivas van a la brecha más cercana.

## Derrota del defensor
Nunca se pierden edificios, mejoras, recursos almacenados ni unidades. El castillo se restaura. Solo cambian los puntos de liga (con límite) y se genera un informe.
