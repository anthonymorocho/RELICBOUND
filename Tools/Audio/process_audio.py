"""Procesado de audio de RELICBOUND según docs/14 (normalización, fundidos, bucles sin salto).

Uso: python3 Tools/Audio/process_audio.py <carpeta_descargas>
La carpeta de descargas contiene los packs CC0 de Kenney descomprimidos (interface-sounds, rpg-audio)
y los archivos de OpenGameArt en oga/. Orígenes y licencias: docs/19_LICENCIAS_Y_ASSETS.md.
"""
import os
import subprocess
import sys

SRC = os.path.abspath(sys.argv[1]) if len(sys.argv) > 1 else "."
OUT = os.path.abspath("Assets/Relicbound/Audio")

# Efectos de interfaz: pico normalizado a -3 dBFS, fundidos de 4 ms (sin clics).
UI = {
    "UI_Click": "interface-sounds/Audio/click_002.ogg",
    "UI_PanelOpen": "rpg-audio/Audio/bookOpen.ogg",
    "UI_PanelClose": "rpg-audio/Audio/bookClose.ogg",
    "UI_Confirm": "interface-sounds/Audio/confirmation_002.ogg",
    "UI_Error": "interface-sounds/Audio/error_006.ogg",
    "UI_Tick": "interface-sounds/Audio/tick_002.ogg",
    "UI_Banner": "rpg-audio/Audio/cloth1.ogg",
    "UI_Coins": "rpg-audio/Audio/handleCoins.ogg",
    "UI_Select": "interface-sounds/Audio/select_003.ogg",
}

# Bucles de ambiente: (origen, inicio s, duración s) a -26 LUFS.
AMBIENCE = {
    "AMB_Birds": ("oga/park_ambience_birds.wav", 40, 90),
    "AMB_Wind": ("oga/park_ambience_wind.wav", 20, 90),
}

# Música del castillo: -20 LUFS integrados, techo -1 dBTP.
MUSIC = {
    "MUS_Castle_Exploration": "oga/Exploration.wav",
    "MUS_Castle_Harvest": "oga/harvestseason.wav",
}

CROSSFADE = 5.0


def run(args):
    subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", *args], check=True)


def peak_db(path):
    result = subprocess.run(["ffmpeg", "-hide_banner", "-i", path, "-af", "volumedetect", "-f", "null", "-"],
                            capture_output=True, text=True)
    for line in result.stderr.splitlines():
        if "max_volume" in line:
            return float(line.split("max_volume:")[1].split("dB")[0])
    return 0.0


def ui_sound(name, source):
    path = os.path.join(SRC, source)
    gain = -3.0 - peak_db(path)
    out = os.path.join(OUT, "UI", f"{name}.ogg")
    run(["-i", path, "-af", f"volume={gain}dB,afade=t=in:d=0.004,areverse,afade=t=in:d=0.004,areverse",
         "-ar", "48000", "-c:a", "libvorbis", "-q:a", "5", out])
    print(f"[audio] {name} ({gain:+.1f} dB)")


def ambience_loop(name, source, start, length):
    """Bucle sin salto: los últimos segundos se funden con el principio del fragmento."""
    path = os.path.join(SRC, source)
    out = os.path.join(OUT, "Ambience", f"{name}.ogg")
    body = length - CROSSFADE
    # Cuerpo [start+X, start+length] y cabeza [start, start+X]: el final del cuerpo se funde con la cabeza,
    # que acaba exactamente donde empieza el cuerpo, así el bucle no tiene salto.
    graph = f"[0:a][1:a]acrossfade=d={CROSSFADE}:c1=tri:c2=tri,loudnorm=I=-26:TP=-3:LRA=8[out]"
    run(["-ss", str(start + CROSSFADE), "-t", str(body), "-i", path,
         "-ss", str(start), "-t", str(CROSSFADE), "-i", path,
         "-filter_complex", graph, "-map", "[out]", "-ar", "48000", "-c:a", "libvorbis", "-q:a", "4", out])
    print(f"[audio] {name} bucle {length - CROSSFADE:.0f} s")


def music(name, source):
    path = os.path.join(SRC, source)
    out = os.path.join(OUT, "Music", f"{name}.ogg")
    run(["-i", path, "-af", "loudnorm=I=-20:TP=-1:LRA=11,afade=t=in:d=0.5",
         "-ar", "48000", "-c:a", "libvorbis", "-q:a", "5", out])
    print(f"[audio] {name}")


def main():
    for folder in ("UI", "Ambience", "Music"):
        os.makedirs(os.path.join(OUT, folder), exist_ok=True)
    for name, source in UI.items():
        ui_sound(name, source)
    for name, (source, start, length) in AMBIENCE.items():
        ambience_loop(name, source, start, length)
    for name, source in MUSIC.items():
        music(name, source)


main()
