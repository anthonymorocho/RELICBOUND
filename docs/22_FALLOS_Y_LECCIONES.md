# Fallos y lecciones

| ID | Fecha | Qué pasó | Lección / acción |
|---|---|---|---|
| L-001 | 2026-10-27 | El prompt maestro pedía auditar la prueba S0, pero el repositorio estaba vacío. El usuario confirmó que **el proyecto empieza desde cero**. | La auditoría P0 se cierra sin código previo. Las restricciones de S0 (paneles negros, esferas verdes, terreno plano) se convierten en reglas de calidad de `18` para que no aparezcan nunca. |
| L-002 | 2026-10-27 | El entorno de trabajo en la nube no trae Unity ni Blender. | Core se diseña para probarse con `dotnet test`. Hook de inicio de sesión que instala .NET y Blender. Unity se compila en CI (D-011) cuando haya licencia configurada. |
