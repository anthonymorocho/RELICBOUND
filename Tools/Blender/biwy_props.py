"""
Objetos de misión del episodio 01: carpeta azul de documentos, cargador de móvil con cable enrollado y bolsa
de tela con ropa. Cada uno con el origen en su base.

blender -b -P Tools/Blender/biwy_props.py -- <carpeta_salida>
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import biwy_geo as G  # noqa: E402

OUT = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "Assets/_Game/Art/Models/Props"


def folder():
    """Carpeta de anillas azul (A4), algo usada, con papeles asomando y etiqueta en el lomo."""
    b = G.Builder("Prop_BlueFolder")
    w, d, h = 0.235, 0.315, 0.045
    b.box(-w / 2, -d / 2, 0.0, w / 2, d / 2, 0.004, "M_FolderBlue")
    b.box(-w / 2, -d / 2, h - 0.004, w / 2, d / 2, h, "M_FolderBlue")
    b.box(-w / 2 - 0.004, -d / 2, 0.0, -w / 2, d / 2, h, "M_FolderBlue")
    b.box(-w / 2 + 0.01, -d / 2 + 0.008, 0.004, w / 2 - 0.004, d / 2 - 0.006, h - 0.006, "M_Paper")
    for k, (dx, dy) in enumerate(((0.006, 0.01), (0.012, -0.006), (0.004, 0.014))):
        z = 0.012 + k * 0.009
        b.box(-w / 2 + 0.03 + dx, -d / 2 + 0.01 + dy, z, w / 2 + 0.008 + dx, d / 2 - 0.02 + dy, z + 0.0015, "M_Paper")
    b.box(-w / 2 - 0.0045, -0.05, 0.012, -w / 2 - 0.004, 0.05, h - 0.012, "M_Label")
    return b.object()


def charger():
    """Cargador de pared blanco con cable USB enrollado."""
    b = G.Builder("Prop_Charger")
    b.box(-0.02, -0.022, 0.0, 0.02, 0.022, 0.026, "M_PlasticWhite")
    for x in (-0.006, 0.006):
        b.box(x - 0.0015, 0.022, 0.008, x + 0.0015, 0.04, 0.018, "M_Chrome")
    turns, r = 4, 0.045
    segs = 20
    for t in range(turns):
        rr = r - t * 0.004
        for i in range(segs):
            a0 = 2 * math.pi * i / segs
            a1 = 2 * math.pi * (i + 1) / segs
            cx, cy = 0.08, 0.0
            p0 = (cx + rr * math.cos(a0), cy + rr * math.sin(a0))
            p1 = (cx + rr * math.cos(a1), cy + rr * math.sin(a1))
            z = 0.002 + t * 0.003
            b.box(min(p0[0], p1[0]) - 0.0015, min(p0[1], p1[1]) - 0.0015, z, max(p0[0], p1[0]) + 0.0015, max(p0[1], p1[1]) + 0.0015, z + 0.003, "M_PlasticWhite")
    b.box(0.02, -0.002, 0.004, 0.04, 0.002, 0.008, "M_PlasticWhite")
    return b.object()


def bag():
    """Bolsa de tela con asas, llena (ropa doblada asomando)."""
    b = G.Builder("Prop_ClothesBag")
    w, d, h = 0.42, 0.18, 0.36
    b.prism([(-w / 2, -d / 2), (w / 2, -d / 2), (w / 2 + 0.02, d / 2), (-w / 2 - 0.02, d / 2)], h, "M_Canvas", z0=0.0, bottom=True)
    b.box(-w / 2 + 0.03, -d / 2 + 0.02, h - 0.02, w / 2 - 0.05, d / 2 - 0.03, h + 0.05, "M_ClothFolded")
    b.box(-0.05, -d / 2 + 0.03, h, w / 2 - 0.03, d / 2 - 0.02, h + 0.035, "M_ClothFolded2")
    for x in (-0.12, 0.12):
        for k in range(8):
            a0 = math.pi * k / 8
            a1 = math.pi * (k + 1) / 8
            r = 0.07
            b.box(x + min(r * math.cos(a0), r * math.cos(a1)) - 0.006, -d / 2 - 0.004, h + r * math.sin(min(a0, a1)) - 0.004,
                  x + max(r * math.cos(a0), r * math.cos(a1)) + 0.006, -d / 2 + 0.004, h + r * math.sin(max(a0, a1)) + 0.004, "M_Canvas")
    return b.object()


G.clear_scene()
for obj in (folder(), charger(), bag()):
    G.export_fbx([obj], os.path.join(OUT, f"SM_{obj.name}.fbx"))
