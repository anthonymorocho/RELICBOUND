"""
Audio de BEFORE IT WAS YOU: recorta, normaliza y crea bucles sin salto a partir de descargas CC0 / CC BY
(orígenes y licencias en Docs/Proyecto/REGISTRO_ASSETS.csv). Lo propio (vibración del móvil, tono de
cocina del prólogo) se sintetiza aquí.

Uso: python3 Tools/Audio/process_audio.py <carpeta_descargas_oga>
"""
import glob
import os
import subprocess
import sys

SRC = os.path.abspath(sys.argv[1])
OUT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Audio"))


def ff(*args):
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", *args], check=True)


def oneshot(src, dst, peak_db=-4.0, start=None, dur=None, lowpass=None):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    filters = [f"loudnorm=I=-20:TP={peak_db}:LRA=11", "afade=t=in:d=0.004", "silenceremove=start_periods=1:start_threshold=-55dB"]
    if lowpass:
        filters.insert(0, f"lowpass=f={lowpass}")
    args = []
    if start is not None:
        args += ["-ss", str(start)]
    if dur is not None:
        args += ["-t", str(dur)]
    ff(*args, "-i", src, "-af", ",".join(filters), "-ac", "1", "-ar", "44100", "-c:a", "libvorbis", "-q:a", "5", dst)


def loop(src, dst, start, dur, lufs=-30, fade=2.0, lowpass=None):
    """Bucle con fundido cruzado: el final se mezcla con el principio para que no se note el salto."""
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    lp = f"lowpass=f={lowpass}," if lowpass else ""
    ff("-ss", str(start), "-t", str(dur + fade), "-i", src, "-ss", str(start), "-t", str(fade), "-i", src,
       "-filter_complex",
       f"[0:a]{lp}aformat=channel_layouts=stereo,afade=t=out:st={dur}:d={fade}[a];"
       f"[1:a]{lp}aformat=channel_layouts=stereo,afade=t=in:d={fade},adelay={int(dur * 1000)}|{int(dur * 1000)}[b];"
       f"[a][b]amix=inputs=2:normalize=0,atrim=start={fade}:end={dur + fade},asetpts=PTS-STARTPTS,loudnorm=I={lufs}:TP=-6[o]",
       "-map", "[o]", "-ar", "44100", "-c:a", "libvorbis", "-q:a", "5", dst)


def main():
    fs = os.path.join(SRC, "footsteps", "footsteps")
    for folder, name in (("boots", "Concrete"), ("grass", "Grass"), ("gravel", "Gravel"), ("tile", "Tile"), ("wood", "Wood"), ("metal", "Metal")):
        for i, f in enumerate(sorted(glob.glob(os.path.join(fs, folder, "*.ogg")))[:6]):
            oneshot(f, os.path.join(OUT, "Footsteps", f"STEP_{name}_{i}.ogg"), -6)
    oneshot(os.path.join(SRC, "door-lock-sounds/DoorLockSounds/DoorLockSounds/DoorOpen.wav"), os.path.join(OUT, "Doors", "DOOR_Open.ogg"))
    oneshot(os.path.join(SRC, "4-door-closes/door_close_-_starninjas/door.2.ogg"), os.path.join(OUT, "Doors", "DOOR_Close.ogg"))
    oneshot(os.path.join(SRC, "creaky-door-hinge-spooky/creaky_door_hinge.wav"), os.path.join(OUT, "Doors", "DOOR_Creak.ogg"), -8)
    oneshot(os.path.join(SRC, "door-lock-sounds/DoorLockSounds/DoorLockSounds/LockedDoorHandleJiggle.wav"), os.path.join(OUT, "Doors", "DOOR_Locked.ogg"))
    oneshot(os.path.join(SRC, "door-lock-sounds/DoorLockSounds/DoorLockSounds/UnlockDoor.wav"), os.path.join(OUT, "Doors", "DOOR_Unlock.ogg"))
    oneshot(os.path.join(SRC, "light-switch-turn-on-and-off-sfx-0/lightswitch_1/light_switch_turn_on.mp3"), os.path.join(OUT, "Props", "SWITCH_On.ogg"), -8)
    oneshot(os.path.join(SRC, "light-switch-turn-on-and-off-sfx-0/lightswitch_1/light_switch_turn_off.mp3"), os.path.join(OUT, "Props", "SWITCH_Off.ogg"), -8)
    loop(os.path.join(SRC, "crickets-ambient-noise-loopable/crickets-oneloop.mp3"), os.path.join(OUT, "Ambience", "AMB_Night_Crickets.ogg"), 0.5, 20.0, -32, 2.0, 7000)
    loop(os.path.join(SRC, "mild-wind-background-noise/wind%20background%20noise%202.wav"), os.path.join(OUT, "Ambience", "AMB_Night_Wind.ogg"), 2.0, 38.0, -34, 3.0, 2500)
    # Sintetizados (propios): vibración del móvil y tono de cocina del prólogo (nevera y sala).
    os.makedirs(os.path.join(OUT, "Props"), exist_ok=True)
    ff("-f", "lavfi", "-i", "sine=frequency=172:duration=0.9", "-f", "lavfi", "-i", "anoisesrc=color=brown:duration=0.9:amplitude=0.08",
       "-filter_complex", "[0][1]amix=inputs=2,volume='if(lt(mod(t,0.45),0.3),1,0)':eval=frame,tremolo=f=34:d=0.7,afade=t=out:st=0.8:d=0.1,loudnorm=I=-22:TP=-6",
       "-ac", "1", "-c:a", "libvorbis", "-q:a", "5", os.path.join(OUT, "Props", "PHONE_Vibrate.ogg"))
    ff("-f", "lavfi", "-i", "anoisesrc=color=brown:duration=40:amplitude=0.05", "-f", "lavfi", "-i", "sine=frequency=50:duration=40",
       "-f", "lavfi", "-i", "sine=frequency=100:duration=40",
       "-filter_complex", "[0]lowpass=f=900[n];[1]volume=0.03[h];[2]volume=0.012[h2];[n][h][h2]amix=inputs=3:normalize=0,loudnorm=I=-34:TP=-9",
       "-ac", "2", "-c:a", "libvorbis", "-q:a", "5", os.path.join(OUT, "Ambience", "AMB_Kitchen_RoomTone.ogg"))


if __name__ == "__main__":
    main()
    for f in sorted(glob.glob(os.path.join(OUT, "**", "*.ogg"), recursive=True)):
        print(os.path.relpath(f, OUT))
