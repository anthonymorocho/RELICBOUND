# UI/UX

## Tecnología
**UI Toolkit** (UXML + USS) para la interfaz en pantalla, con un tema USS único (`Assets/Relicbound/UI/Theme/Relicbound.uss`). En Unity 6.0, los marcadores sobre edificios en el mundo (indicador de mejora, nombre) usan uGUI en *world space* o *sprites* (ver D-005).

## Proceso obligatorio por pantalla
1. Reunir referencias (`03`). 2. Boceto de composición con jerarquía (aquí). 3. Tokens de tamaño/tipografía. 4. Comprobar en 16:9 y 20:9 con zonas seguras. 5. Implementar con componentes reutilizables. 6. Capturar en el motor. 7. Comparar y corregir. 8. Registrar en `18_QA.md`.

## Tokens de diseño
| Token | Valor | Uso |
|---|---|---|
| `--night` | `#14213D` al 92 % | Fondo de paneles |
| `--stone` | `#2B2A2E` | Superficies secundarias, bordes |
| `--gold` | `#C9A54C` | Títulos, elementos importantes, borde del botón principal |
| `--positive` | `#4E9A51` | Confirmar, mejorar |
| `--danger` | `#B5403A` | Daño, cancelar destructivo |
| `--text` | `#F3EAD8` | Texto principal |
| `--text-dim` | `#B9AF9C` | Texto secundario |
| Radio | 10 px | Paneles; 999 px solo para píldoras de recursos |
| Espaciado | Base 8 px (8/16/24/32) | |

Tipografía: una serif de títulos con carácter medieval legible y una sans humanista para cuerpo y números (fuentes con licencia OFL, a elegir y registrar en `19`). Tamaños a 1080 p de alto: título 40, subtítulo 28, cuerpo 24, números de recursos 26 con dígitos tabulares. En Android se escala por la densidad (Panel Settings → *Scale With Screen Size*, 1080 de referencia, *match* 0,5).

Iconos: set propio vectorial (SVG → Vector Graphics) con trazo de 2 px y relleno dorado o claro. **Sin emojis.**

## HUD del castillo — composición
```
┌──────────────────────────────────────────────────────────────────────┐
│ [Escudo] Nombre  Nv 4      [Oro▮▮▮][Madera▮▮][Piedra▮▮][Hierro▮][Gemas]│
│          ▬▬▬▬▬▬▭ XP  Liga                                    [⚙] [👤]│
│                                                                      │
│ [Misiones]                                                           │
│ [Eventos ]                  ( castillo 3D )                          │
│ [Informes]                                                           │
│ [Temporada]                                                          │
│                                                                      │
│ [ATACAR]   [Construir] [Taller] [Ejército] [Tienda] [Clasificación]  │
└──────────────────────────────────────────────────────────────────────┘
```
(En el boceto, ⚙ y 👤 solo representan los botones de Ajustes y Perfil. Los iconos finales son vectoriales).
- **Atacar** es el botón más grande (1,6× los demás) y va con borde dorado, en la esquina inferior izquierda, cerca del pulgar en horizontal.
- La barra de recursos muestra el valor y, al tocarla, la capacidad. Si un recurso está al 100 %, su borde pasa a dorado.
- El lateral izquierdo se puede plegar, admite 5 entradas como máximo y muestra un punto rojo cuando hay novedades.
- Ajustes y Perfil van arriba a la derecha. Investigación e Inventario se abren desde **Taller** y **Perfil**: nada de 20 botones.
- Los sistemas bloqueados **no se muestran** hasta su nivel de desbloqueo. Si tienen un adelanto, aparecen con un candado y el nivel necesario.

## Selección de edificio
Al tocar un edificio: contorno dorado (render feature de *outline*) más un panel inferior compacto de unos 180 px de alto, centrado y sin tapar el edificio. La cámara encuadra el edificio en el tercio superior.
Contenido: nombre + nivel · estado · 2–3 estadísticas clave · botones `Información` `Mejorar (coste)` `Mover` · cerrar con un toque fuera del panel o con Esc.
Estados: **mejorable** (botón verde), **sin recursos** (coste en rojo con el déficit), **bloqueado** (requisito visible), **en mejora** (temporizador si aplica).

## Pantalla de perfil (primera apertura)
Fondo: el propio castillo inicial desenfocado, con una órbita lenta de la cámara (no una imagen estática). Tarjeta central con: estandarte (3 opciones iniciales), *Nombre del señor* y *Nombre de la fortaleza*. Validación en vivo con `NameValidator` y mensajes como «Mínimo 3 letras» o «Solo letras, números, espacios, - y '». Botón **Fundar fortaleza** desactivado hasta que ambos nombres sean válidos.

## Estados comunes de toda pantalla
Carga (esqueleto con brillo animado, nunca un panel vacío) · error (mensaje claro + Reintentar) · vacío (ilustración + acción sugerida) · bloqueado (requisito) · confirmación (cuando se gastan más de 25 % de un recurso o gemas).

## Adaptación
| | PC | Android |
|---|---|---|
| Objetivo táctil | — | ≥ 48 dp |
| Hover | Tooltip a los 400 ms | Pulsación larga = tooltip |
| Atajos | A Atacar, B Construir, Esc cerrar, F encuadrar castillo | — |
| Zonas seguras | — | `Screen.safeArea` aplicado al contenedor raíz |
