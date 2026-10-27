# Destrucción

## Principio
**La lógica decide y lo visual acompaña.** El daño se calcula en un modelo lógico determinista (`StructureSection.hp`) y los efectos físicos son decorativos y desechables. Así la futura validación del servidor solo reproduce la lógica.

## Unidad destructible
Las murallas se dividen en **secciones de 2 m**. Cada sección tiene 4 estados:

| Estado | Vida | Visual | Colisión / navegación |
|---|---|---|---|
| Intacta | 100–67 % | Modelo limpio | Bloquea |
| Dañada | 66–34 % | Malla con grietas + decal de impacto + polvo al recibir | Bloquea |
| Muy dañada | 33–1 % | Malla con huecos y almena caída, cascotes en la base | Bloquea |
| Colapsada | 0 | Escombro bajo transitable + fragmentos con física 3–5 s | **Libre**: se actualiza el NavMesh |

Cada impacto da feedback **aunque no cambie de estado**: decal, partículas, sonido y una leve sacudida de cámara proporcional al daño.

## Implementación
- Fragmentos pre-cortados en Blender (cell fracture) por cada pieza destructible, guardados como prefab desactivado y activados desde un pool.
- Como máximo 60 cuerpos rígidos activos en Android y 200 en PC. Los más antiguos se duermen y se funden con el escombro estático.
- Navegación: `NavMeshObstacle` con *carve* en cada sección; se desactiva al colapsar.
- Torres: 3 estados; al colapsar dejan de atacar. Puertas: al romperse abren la ruta.

## Rendimiento a medir (P4/P10)
Pico de ms en un colapso, número de fragmentos, partículas vivas y GC alloc por impacto (objetivo: 0).
