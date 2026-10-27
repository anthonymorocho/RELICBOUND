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
