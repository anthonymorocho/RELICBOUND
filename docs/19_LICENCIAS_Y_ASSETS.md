# Licencias y assets

**Regla:** se verifica la licencia de **cada pack o archivo** antes de descargarlo, y se apunta aquí con su URL y la fecha de comprobación. Ningún asset entra en el repositorio sin una fila en esta tabla.

## Candidatos (pendientes de verificar uno a uno)
| Fuente | Qué buscar | Licencia esperada | Verificado |
|---|---|---|---|
| KayKit (kaylousberg.itch.io) | Personajes rigged medievales, edificios | CC0 en la mayoría de packs: **comprobar cada pack** | No |
| Quaternius (quaternius.com) | Personajes animados, naturaleza | CC0 habitual: comprobar cada pack | No |
| Mixamo (mixamo.com) | Animaciones humanoides | Términos de Adobe: uso en juegos permitido, no redistribuir los archivos sueltos | No |
| Kenney (kenney.nl) | Iconos de UI, partículas, sonidos de UI | CC0 | No |
| Poly Haven (polyhaven.com) | Texturas de piedra, madera, terreno, HDRI | CC0 | No |
| Google Fonts | Tipografías de UI | OFL: comprobar cada fuente | No |

## Registro de assets integrados
| Fecha | Asset | Origen (URL) | Licencia | Atribución requerida | Ruta en el proyecto |
|---|---|---|---|---|---|
| 2026-10-27 | Texturas tileables StoneWall, Wood, RoofBlue, RoofRed, Iron (albedo + normal, 1024²) | **Propias**, generadas con `Tools/Blender/rb_textures.py` | Propiedad del proyecto | No | `Assets/Relicbound/Art/Textures/` |
| 2026-10-27 | Kit del castillo inicial: SM_Wall_Straight, SM_Tower_Round, SM_Gatehouse, SM_Keep_L1 | **Propios**, generados con `Tools/Blender/rb_castle_kit.py` | Propiedad del proyecto | No | `Assets/Relicbound/Art/Castle/` |
| 2026-10-27 | Texturas Grass y Dirt (terreno) | **Propias**, `Tools/Blender/rb_textures.py` | Propiedad del proyecto | No | `Assets/Relicbound/Art/Textures/` |
| 2026-10-27 | Vegetación y rocas: 3 frondosos, 2 pinos, 2 arbustos, 3 rocas | **Propios**, `Tools/Blender/rb_nature.py` | Propiedad del proyecto | No | `Assets/Relicbound/Art/Nature/` |
| 2026-10-27 | Fuente Cinzel Bold (instancia estática de la variable) | https://github.com/google/fonts/tree/main/ofl/cinzel | SIL OFL 1.1 (texto incluido) | Mención en créditos recomendada | `Assets/Relicbound/UI/Fonts/` |
| 2026-10-27 | Fuente Nunito Sans SemiBold y ExtraBold (instancias estáticas) | https://github.com/google/fonts/tree/main/ofl/nunitosans | SIL OFL 1.1 (texto incluido) | Mención en créditos recomendada | `Assets/Relicbound/UI/Fonts/` |
| 2026-10-27 | Iconos de recursos (oro, madera, piedra, hierro, gemas) | **Propios**, `Tools/Blender/rb_icons.py` | Propiedad del proyecto | No | `Assets/Relicbound/UI/Icons/` |
| 2026-10-27 | Texturas 9-slice de interfaz (paneles, botones, campos, barras) | **Propias**, `Tools/UI/make_ui_textures.py` | Propiedad del proyecto | No | `Assets/Relicbound/UI/Textures/` |
| 2026-10-27 | Objetos de patio (barril, caja, bala de paja, carro, pozo) y textura Hay | **Propios**, `Tools/Blender/rb_props.py` | Propiedad del proyecto | No | `Assets/Relicbound/Art/Props/` |
| 2026-10-27 | Máscaras heráldicas de estandartes | **Propias**, `Tools/UI/make_banner_masks.py` | Propiedad del proyecto | No | `Assets/Relicbound/Art/Textures/T_BannerMasks.png` |
| 2026-10-27 | Efectos de interfaz: click_002, confirmation_002, error_006, tick_002, select_003 (Interface Sounds 1.0, Kenney) | https://kenney.nl/assets/interface-sounds | CC0 1.0 (License.txt del pack verificado) | No | `Assets/Relicbound/Audio/UI/` |
| 2026-10-27 | Efectos: bookOpen, bookClose, cloth1, handleCoins (RPG Audio, Kenney) | https://kenney.nl/assets/rpg-audio | CC0 1.0 (License.txt del pack verificado) | No | `Assets/Relicbound/Audio/UI/` |
| 2026-10-27 | Ambiente: park_ambience_birds.wav, park_ambience_wind.wav (Park ambiences, autor thimras) | https://opengameart.org/content/park-ambiences | CC0 (verificado en la página) | No | `Assets/Relicbound/Audio/Ambience/` (bucles editados) |
| 2026-10-27 | Música: «Medieval: Exploration» y «Medieval: Harvest Season» (autor RandomMind) | https://opengameart.org/content/medieval-exploration · https://opengameart.org/content/medieval-harvest-season | CC0 (verificado en la página) | No (se acredita igualmente) | `Assets/Relicbound/Audio/Music/` (normalizada a -20 LUFS) |

## Código de terceros
| Paquete | Licencia |
|---|---|
| Newtonsoft.Json 13 (vía `com.unity.nuget.newtonsoft-json`) | MIT |
| NUnit 3 (solo tests) | MIT |
