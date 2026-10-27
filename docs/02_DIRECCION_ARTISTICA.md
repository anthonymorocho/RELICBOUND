# Dirección artística

## Estilo
Fantasía medieval **estilizada** en 3D, legible desde cámara cenital a ~50°. Proporciones ligeramente exageradas (almenas y tejados un 15–25 % más grandes que lo realista) para que las siluetas se lean en un móvil de 6".

## Reglas de forma
- Silueta primero: cada tipo de edificio debe reconocerse en negro sobre blanco a 64 px de alto.
- Bisel visible en aristas de piedra y madera (sin cubos de aristas vivas).
- Piedra con **variación por bloque** (tamaño, tono ±8 % de luminosidad, desgaste en esquinas).
- Madera con volumen: vigas con sección real, tablones con separación, clavos/abrazaderas de metal.
- Tejados con aleros, cumbreras y tejas por filas; nunca un prisma plano.
- Todo apoya en el suelo: zócalo o base que se hunde 5–10 cm en el terreno. Nada flota.

## Paleta del mundo (Frontera Verde, región inicial)
| Uso | Color | Nota |
|---|---|---|
| Piedra clara | `#B9B2A3` | Murallas del jugador |
| Piedra sombra | `#6F6A62` | |
| Madera | `#7A5432` | |
| Tejado jugador | `#3E5F8A` | Azul: identidad del jugador |
| Tejado enemigo | `#8A3E3A` | Rojo: identifica al defensor en asedios |
| Hierba | `#6E8F3E` → `#9DB35A` | Degradado por ruido, no plano |
| Tierra/caminos | `#9A7B55` | |

El color de equipo (azul/rojo) va en tejados, estandartes y escudos, **nunca en la piedra**, para que el castillo siga siendo legible con la skin que sea.

## Iluminación (URP)
- Luz direccional cálida (5200 K) a 40–50° de elevación, sombras suaves con cascadas: 2 en móvil y 4 en PC.
- Luz ambiental con gradiente de cielo azulado.
- Oclusión ambiental: SSAO en PC; en Android, AO horneada en vértices o texturas.
- Post: tonemapping ACES, bloom ligero (solo fuego y magia), viñeta muy suave. Sin aberración cromática.

## Técnica de materiales
- Shader Lit de URP con texturas atlas de 2048² por familia (piedra, madera, metal, tela).
- Texel density objetivo: 256 px/m en edificios y 512 px/m en máquinas de asedio, que se ven de cerca.
- Edificios en 1 o 2 materiales como máximo para limitar draw calls.

## Presupuestos (orientativos, a medir en P10)
| Elemento | Tris LOD0 | LODs |
|---|---|---|
| Castillo central | 15–25 k | 3 |
| Torre | 4–8 k | 3 |
| Sección de muralla | 1–2 k | 2 |
| Máquina de asedio | 6–10 k | 2 |
| Humanoide | 3–6 k | 2 |

## Prohibido
Primitivas sin trabajar como arte final, materiales planos idénticos, cápsulas o esferas como unidades, elementos flotando, interpenetraciones visibles, escalas mezcladas y mezclar estilos incompatibles (low-poly facetado con realista PBR).

## Escala de referencia
1 unidad = 1 m. Humano = 1,8 m. Puerta de muralla = 3,5 m de alto. Muralla básica = 5 m de alto y 2 m de grosor. Celda de construcción = 1 m (ver `05`).
