# Fallos y lecciones

| ID | Fecha | Qué pasó | Lección / acción |
|---|---|---|---|
| L-001 | 2026-10-27 | El prompt maestro pedía auditar la prueba S0, pero el repositorio estaba vacío. El usuario confirmó que **el proyecto empieza desde cero**. | La auditoría P0 se cierra sin código previo. Las restricciones de S0 (paneles negros, esferas verdes, terreno plano) se convierten en reglas de calidad de `18` para que no aparezcan nunca. |
| L-002 | 2026-10-27 | El entorno de trabajo en la nube no trae Unity ni Blender. | Core se diseña para probarse con `dotnet test`. Hook de inicio de sesión que instala .NET y Blender. Unity se compila en CI (D-011) cuando haya licencia configurada. |
| L-003 | 2026-10-27 | La splash salía vacía: `EditorSceneManager.NewScene` descarga los assets sin referencias e invalidaba el `PanelSettings` cargado antes de crear la escena. | Cargar assets compartidos **después** de crear la escena en los constructores. Añadido diagnóstico en el test del recorrido. |
| L-004 | 2026-10-27 | `*.asset text` en `.gitattributes` corrompía el `TerrainData`, que es binario. | Los assets binarios se marcan como `binary`; comprobar con `cmp` tras añadirlos. |
| L-005 | 2026-10-27 | Colores de vértice lavados en la vegetación: se escribían valores sRGB en atributos lineales. | Convertir a lineal antes de escribir y exportar el FBX con color lineal. |
| L-006 | 2026-10-27 | El usuario juzgó los gráficos «simples». | Pasada de calidad: shader de desgaste, SSAO, luz baja, pradera instanciada, vegetación facetada con AO, más detalle en el kit. Seguir comparando contra las referencias de `03` en cada hito. |

