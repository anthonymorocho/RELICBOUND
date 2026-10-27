# Audio

## Mezclador (Unity AudioMixer)
`Master` → `Music`, `Ambience`, `Effects`, `Interface`, `Voice`. Coincide con `GameSettings` (implementado). Los volúmenes se guardan de 0 a 1 y se convierten a dB con `20·log10(v)` (−80 dB si v = 0).

## Normas de edición
Recortar, quitar ruido, normalizar a −1 dBTP de pico y loudness por grupo (efectos −16 LUFS de referencia, música −20, ambiente −24), fundidos de 5–20 ms, sin clipping, 3–5 variaciones por sonido repetido con pitch aleatorio de ±5 %. Formato fuente: WAV 48 kHz; importado como Vorbis (calidad 0,6) en Android y PCM o ADPCM en PC para sonidos cortos.

## Lista P1 (castillo)
Ambiente de viento suave, pájaros, herrería lejana en bucle, interfaz (tocar, abrir panel, cerrar, confirmar, error) y música ambiental discreta.

## Fuentes
Freesound (revisar la licencia **por archivo**: CC0 y CC-BY sí, con atribución en créditos; **no** CC-BY-NC), Pixabay (licencia propia, sin atribución), OpenGameArt (licencia por archivo) y el GDC bundle de Sonniss (libre de royalties). Cada archivo se anota en `19`.
