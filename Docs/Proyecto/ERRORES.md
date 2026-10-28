# Errores conocidos y lecciones

Lecciones heredadas (aplicadas desde el inicio):
- `EditorSceneManager.NewScene` descarga assets sin referencias: los assets compartidos (PanelSettings) se cargan **después** de crear la escena y se recargan desde disco tras `CreateAsset`.
- TerrainData/LightingData deben ser binarios en `.gitattributes`.
- USS con fuentes: `-unity-font: url(...)` + `-unity-font-definition: initial`.
- GameCI: el test runner necesita `checks: write`; el empaquetado de archivos generados usa `tar --ignore-failed-read`.
- En propiedades enum serializadas, `intValue` fija el valor subyacente (MSAA 4 = 4x), no el índice.
- Tests de entrada: no llamar `InputSystem.Update()` manualmente; dejar que el evento entre en la actualización normal del frame.

| Fecha | Error | Estado |
|---|---|---|
| 2026-10-28 | CI (run 40605214764): escenas generadas y compiladas, pero el primer `Camera.Render` de evidencias quedó bloqueado 105 min. Sospecha: skinning por GPU de Víctor (primera malla con esqueleto) en el renderizador por software. Mitigación: Víctor congelado con `BakeMesh` durante las capturas, skinning en CPU solo en la CI de verificación, registro con hora por captura y límite del paso a 40 min. | Pendiente de verificar |
| 2026-10-28 | `FSBTool ERROR: Failed decoding audio clip` en UI_Click/Confirm/PanelClose/Select: eran punteros Git LFS copiados de la rama archivada. Restaurado el audio real desde la caché LFS local. | Corregido |
