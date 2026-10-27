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


def backpack():
    """Mochila de Víctor: gastada, llena, con correas y bolsillo delantero."""
    b = G.Builder("Prop_Backpack")
    w, d, h = 0.32, 0.2, 0.46
    b.prism([(-w / 2, -d / 2), (w / 2, -d / 2), (w / 2 + 0.01, d / 2), (-w / 2 - 0.01, d / 2)], h, "M_BackpackFabric", z0=0.0, bottom=True)
    b.prism([(-w / 2 + 0.02, d / 2), (w / 2 - 0.02, d / 2), (w / 2 - 0.05, d / 2 + 0.035), (-w / 2 + 0.05, d / 2 + 0.035)], h * 0.82, "M_BackpackFabric", z0=h * 0.05)
    b.box(-w / 2 + 0.03, -d / 2 + 0.02, h, w / 2 - 0.03, d / 2 - 0.02, h + 0.045, "M_BackpackFabric")
    b.box(-w / 2 + 0.05, d / 2 + 0.03, h * 0.45, w / 2 - 0.05, d / 2 + 0.07, h * 0.75, "M_BackpackDark")
    for x in (-0.09, 0.09):
        b.box(x - 0.025, -d / 2 - 0.02, 0.04, x + 0.025, -d / 2, h - 0.02, "M_BackpackDark")
    b.box(-0.06, -0.01, h + 0.045, 0.06, 0.01, h + 0.08, "M_BackpackDark")
    b.box(-w / 2 + 0.02, d / 2 + 0.068, h * 0.6, w / 2 - 0.02, d / 2 + 0.072, h * 0.605, "M_Chrome")
    return b.object()


def refuge():
    """Cama improvisada: cartones extendidos, manta doblada encima y un abrigo hecho bola como almohada."""
    b = G.Builder("Prop_Refuge")
    b.box(-0.45, -0.95, 0.0, 0.42, 0.98, 0.012, "M_Cardboard")
    b.box(-0.4, -0.9, 0.012, 0.47, 0.6, 0.022, "M_Cardboard")
    b.box(-0.36, -0.7, 0.022, 0.38, 0.55, 0.07, "M_Blanket")
    b.box(-0.34, -0.68, 0.07, 0.36, -0.1, 0.1, "M_Blanket")
    b.box(-0.22, 0.6, 0.022, 0.24, 0.92, 0.14, "M_ClothFolded")
    return b.object()


def mug():
    b = G.Builder("Prop_Mug")
    b.cylinder(0, 0, 0.0, 0.095, 0.04, "M_Ceramic", 20)
    b.cylinder(0, 0, 0.088, 0.096, 0.034, "M_CoffeeDregs", 20)
    b.box(0.038, -0.006, 0.02, 0.062, 0.006, 0.075, "M_Ceramic")
    return b.object()


def open_can():
    b = G.Builder("Prop_OpenCan")
    b.cylinder(0, 0, 0.0, 0.11, 0.038, "M_Steel", 20)
    b.cylinder(0, 0, 0.1, 0.104, 0.035, "M_Lentils", 20)
    b.box(-0.03, 0.038, 0.11, 0.03, 0.04, 0.17, "M_Steel")
    b.box(-0.004, -0.02, 0.06, 0.004, 0.0, 0.2, "M_Chrome")
    b.box(-0.008, -0.03, 0.18, 0.008, 0.0, 0.2, "M_Chrome")
    return b.object()


def laundry_box():
    b = G.Builder("Prop_LaundryBox")
    b.box(-0.24, -0.17, 0.0, 0.24, 0.17, 0.3, "M_Cardboard")
    b.box(-0.245, -0.175, 0.28, 0.245, 0.175, 0.3, "M_CardboardTape")
    return b.object()


G.clear_scene()
for obj in (folder(), charger(), bag(), backpack(), refuge(), mug(), open_can(), laundry_box()):
    G.export_fbx([obj], os.path.join(OUT, f"SM_{obj.name}.fbx"))
