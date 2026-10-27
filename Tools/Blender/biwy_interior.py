"""
Interior de la casa de Elena (M02): suelos por habitación, techos, tabiques con huecos de puerta, rodapiés,
cercos, cocina (muebles bajos y altos, encimera, fregadero, nevera), baño (bañera, inodoro, lavabo),
lavadero (escalón de 0.18 m), armario empotrado, colchón y ropa de cama, interruptores y plafones.
Medidas de Docs/Proyecto/PLANO_TECNICO.md (mismas coordenadas que biwy_exterior.py).

blender -b -P Tools/Blender/biwy_interior.py -- <carpeta_salida>
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import biwy_geo as G  # noqa: E402

OUT = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "Assets/_Game/Art/Models/Interior"

FLOOR = 0.45
LAUNDRY = 0.27
CEIL = 3.05
T = 0.12          # tabique
DOOR_H = 2.05

# Habitaciones: nombre -> (x0, y0, x1, y1, suelo, material de suelo, material de pared)
ROOMS = {
    "Sala": (4.5, 6.0, 9.7, 10.5, FLOOR, "M_FloorWood", "M_WallSala"),
    "Comedor": (9.7, 6.0, 14.5, 10.5, FLOOR, "M_FloorWood", "M_WallSala"),
    "Pasillo": (8.5, 10.5, 9.7, 18.0, FLOOR, "M_FloorWood", "M_WallPasillo"),
    "Dormitorio2": (4.5, 10.5, 8.5, 14.3, FLOOR, "M_FloorWood", "M_WallDorm2"),
    "DormitorioElena": (4.5, 14.3, 8.5, 18.0, FLOOR, "M_FloorWood", "M_WallElena"),
    "Cocina": (9.7, 10.5, 14.5, 15.0, FLOOR, "M_FloorKitchen", "M_WallKitchen"),
    "Bano": (9.7, 15.0, 11.9, 18.0, FLOOR, "M_FloorBath", "M_WallBath"),
    "Lavadero": (11.9, 15.0, 14.5, 18.0, LAUNDRY, "M_FloorLaundry", "M_WallLaundry"),
}


def floors_and_ceilings():
    """Un objeto de suelo por tipo de superficie (para el sonido de pasos) y el techo común."""
    groups = {}
    for name, (x0, y0, x1, y1, z, mat, _) in ROOMS.items():
        b = groups.setdefault(mat, G.Builder("Floor_" + mat.replace("M_Floor", "")))
        b.face([(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)], mat)
    # Peldaño cocina -> lavadero (contrahuella de 0.18 en el hueco de puerta).
    lb = groups["M_FloorLaundry"]
    lb.face([(12.6, 15.0, LAUNDRY), (13.4, 15.0, LAUNDRY), (13.4, 15.0, FLOOR), (12.6, 15.0, FLOOR)], "M_StepEdge")
    lb.box(12.6, 14.94, FLOOR - 0.02, 13.4, 15.06, FLOOR + 0.005, "M_Threshold", faces="nst")
    ceiling = G.Builder("Ceiling")
    for name, (x0, y0, x1, y1, z, _, _) in ROOMS.items():
        f = ceiling.face([(x0, y0, CEIL), (x0, y1, CEIL), (x1, y1, CEIL), (x1, y0, CEIL)], "M_Ceiling")
        if f.normal.z > 0:
            f.normal_flip()
    return [b.object() for b in groups.values()] + [ceiling.object()]


def partition(b, axis, fixed, a0, a1, z0, openings, mat_a, mat_b, top=CEIL):
    """Tabique centrado en 'fixed' con huecos (a0, a1, z0, z1). mat_a: cara negativa, mat_b: cara positiva."""
    G.wall_with_openings(b, axis, fixed - T / 2, a0, a1, z0, top, openings, T, -1, mat_a, mat_b, "M_InteriorWall")


def walls():
    b = G.Builder("Partitions")
    door = lambda a0: (a0, a0 + 0.8, FLOOR, FLOOR + DOOR_H)
    # Sala | Dormitorio 2 (y = 10.5, x 4.5-8.5) y Comedor | Cocina con paso abierto.
    partition(b, "x", 10.5, 4.5, 8.44, FLOOR, [], "M_WallSala", "M_WallDorm2")
    partition(b, "x", 10.5, 9.76, 14.5, FLOOR, [(11.0, 11.9, FLOOR, FLOOR + 2.1)], "M_WallSala", "M_WallKitchen")
    # Dormitorios | Pasillo (x = 8.5).
    partition(b, "y", 8.5, 10.44, 18.0, FLOOR, [door(11.6), door(15.5)], "M_WallDorm2", "M_WallPasillo")
    # Dormitorio 2 | Elena (y = 14.3).
    partition(b, "x", 14.3, 4.5, 8.44, FLOOR, [], "M_WallDorm2", "M_WallElena")
    # Pasillo | Cocina y Baño (x = 9.7).
    partition(b, "y", 9.7, 10.44, 18.0, FLOOR, [door(11.2), door(16.1)], "M_WallPasillo", "M_WallKitchen")
    # Cocina | Baño y Lavadero (y = 15), puerta del lavadero con escalón.
    partition(b, "x", 15.0, 9.76, 14.5, LAUNDRY, [(12.6, 13.4, LAUNDRY, FLOOR + DOOR_H)], "M_WallKitchen", "M_WallBath")
    # Baño | Lavadero (x = 11.9).
    partition(b, "y", 11.9, 15.06, 18.0, LAUNDRY, [], "M_WallBath", "M_WallLaundry")
    # Sala | Comedor: arco amplio con machones.
    partition(b, "y", 9.7, 6.0, 10.44, FLOOR, [(6.45, 10.05, FLOOR, FLOOR + 2.25)], "M_WallSala", "M_WallSala")
    return b.object()


def trims():
    """Rodapiés en todas las habitaciones y cercos de las puertas interiores."""
    b = G.Builder("InteriorTrim")
    h, d = 0.09, 0.015
    gaps = {
        "Sala": [("s", 6.1, 7.0), ("e", 6.45, 10.05), ("n", 8.5, 9.7)],
        "Comedor": [("w", 6.45, 10.05), ("n", 11.0, 11.9)],
        "Pasillo": [("w", 11.6, 12.4), ("w", 15.5, 16.3), ("e", 11.2, 12.0), ("e", 16.1, 16.9), ("s", 8.5, 9.7)],
        "Dormitorio2": [("e", 11.6, 12.4)],
        "DormitorioElena": [("e", 15.5, 16.3)],
        "Cocina": [("w", 11.2, 12.0), ("s", 11.0, 11.9), ("n", 12.6, 13.4)],
        "Bano": [("w", 16.1, 16.9)],
        "Lavadero": [("s", 12.6, 13.4), ("n", 12.7, 13.5)],
    }
    inner = {"s": 0.0, "n": 0.0, "w": 0.0, "e": 0.0}
    for name, (x0, y0, x1, y1, z, _, _) in ROOMS.items():
        # Caras interiores: los muros exteriores tienen 0.20 hacia dentro; los tabiques 0.06 a cada lado.
        def off(side, value, exterior):
            return value + (0.2 if exterior else 0.06) * (1 if side in "sw" else -1)
        sx0 = off("w", x0, x0 == 4.5)
        sx1 = off("e", x1, x1 == 14.5)
        sy0 = off("s", y0, y0 == 6.0)
        sy1 = off("n", y1, y1 == 18.0)
        runs = {"s": ("x", sy0, sx0, sx1, 1), "n": ("x", sy1, sx0, sx1, -1), "w": ("y", sx0, sy0, sy1, 1), "e": ("y", sx1, sy0, sy1, -1)}
        for side, (axis, fixed, a0, a1, inward) in runs.items():
            cuts = sorted([(g0, g1) for (s, g0, g1) in gaps.get(name, []) if s == side])
            start = a0
            for g0, g1 in cuts + [(a1, a1)]:
                if g0 - start > 0.05:
                    if axis == "x":
                        b.box(start, min(fixed, fixed + inward * d), z, g0, max(fixed, fixed + inward * d), z + h, "M_Baseboard")
                    else:
                        b.box(min(fixed, fixed + inward * d), start, z, max(fixed, fixed + inward * d), g0, z + h, "M_Baseboard")
                start = max(start, g1)
    # Cercos de puertas interiores (ambas caras del tabique).
    doors = [("y", 8.5, 11.6), ("y", 8.5, 15.5), ("y", 9.7, 11.2), ("y", 9.7, 16.1), ("x", 15.0, 12.6)]
    for axis, fixed, a0 in doors:
        z0 = LAUNDRY if (axis == "x" and fixed == 15.0) else FLOOR
        for side in (-1, 1):
            face = fixed + side * T / 2
            for (p0, p1, q0, q1) in ((a0 - 0.07, a0, z0, FLOOR + DOOR_H + 0.07), (a0 + 0.8, a0 + 0.87, z0, FLOOR + DOOR_H + 0.07),
                                     (a0 - 0.07, a0 + 0.87, FLOOR + DOOR_H, FLOOR + DOOR_H + 0.07)):
                f0, f1 = sorted((face, face + side * 0.018))
                if axis == "y":
                    b.box(f0, p0, q0, f1, p1, q1, "M_Baseboard")
                else:
                    b.box(p0, f0, q0, p1, f1, q1, "M_Baseboard")
    return b.object()


def kitchen():
    b = G.Builder("Kitchen")
    # Muebles bajos a lo largo del muro este (x 13.9 - 14.3) y del norte junto a la nevera.
    x_wall = 14.3
    y0, y1 = 10.95, 14.88
    stove = (11.25, 11.77)
    sink = (12.3, 13.3)
    top = FLOOR + 0.9
    # Zócalo, cuerpos y puertas.
    b.box(x_wall - 0.55, y0, FLOOR, x_wall - 0.05, y1, FLOOR + 0.1, "M_CabinetDark")
    segments = [(y0, stove[0]), (stove[1], sink[0]), (sink[0], sink[1]), (sink[1], y1)]
    for a, c in segments:
        b.box(x_wall - 0.58, a, FLOOR + 0.1, x_wall, c, top - 0.04, "M_CabinetCarcass")
        n = max(1, int(round((c - a) / 0.45)))
        for k in range(n):
            d0 = a + (c - a) * k / n + 0.004
            d1 = a + (c - a) * (k + 1) / n - 0.004
            b.box(x_wall - 0.6, d0, FLOOR + 0.13, x_wall - 0.58, d1, top - 0.24, "M_CabinetDoor")
            b.box(x_wall - 0.6, d0, top - 0.22, x_wall - 0.58, d1, top - 0.06, "M_CabinetDoor")
            hy = d1 - 0.05 if k % 2 == 0 else d0 + 0.05
            b.box(x_wall - 0.625, hy - 0.006, top - 0.37, x_wall - 0.6, hy + 0.006, top - 0.29, "M_MetalBrushed")
    # Encimera con canto y hueco del fregadero.
    b.box(x_wall - 0.63, y0 - 0.02, top - 0.04, x_wall, sink[0], top, "M_Countertop")
    b.box(x_wall - 0.63, sink[1], top - 0.04, x_wall, y1, top, "M_Countertop")
    b.box(x_wall - 0.63, sink[0], top - 0.04, x_wall - 0.53, sink[1], top, "M_Countertop")
    b.box(x_wall - 0.1, sink[0], top - 0.04, x_wall, sink[1], top, "M_Countertop")
    # Fregadero de acero (cubeta) y grifo.
    b.box(x_wall - 0.53, sink[0] + 0.05, top - 0.2, x_wall - 0.1, sink[1] - 0.05, top - 0.185, "M_Steel")
    for (p, q) in ((sink[0] + 0.05, sink[0] + 0.06), (sink[1] - 0.06, sink[1] - 0.05)):
        b.box(x_wall - 0.53, p, top - 0.2, x_wall - 0.1, q, top - 0.04, "M_Steel")
    b.box(x_wall - 0.53, sink[0] + 0.05, top - 0.2, x_wall - 0.52, sink[1] - 0.05, top - 0.04, "M_Steel")
    b.box(x_wall - 0.11, sink[0] + 0.05, top - 0.2, x_wall - 0.1, sink[1] - 0.05, top - 0.04, "M_Steel")
    my = (sink[0] + sink[1]) / 2
    b.cylinder(x_wall - 0.06, my, top, top + 0.28, 0.015, "M_Chrome", 12)
    b.box(x_wall - 0.25, my - 0.012, top + 0.26, x_wall - 0.05, my + 0.012, top + 0.285, "M_Chrome")
    # Azulejo de pared sobre la encimera.
    f = b.face([(x_wall - 0.005, y0, top), (x_wall - 0.005, y1, top), (x_wall - 0.005, y1, top + 0.6), (x_wall - 0.005, y0, top + 0.6)], "M_Backsplash")
    G.orient(f, "y", -1, b)
    # Muebles altos (dejando libre la ventana y 12.2 - 13.4).
    for a, c in ((y0, 12.05), (13.55, y1)):
        b.box(x_wall - 0.34, a, FLOOR + 1.5, x_wall, c, FLOOR + 2.2, "M_CabinetCarcass")
        n = max(1, int(round((c - a) / 0.45)))
        for k in range(n):
            d0 = a + (c - a) * k / n + 0.004
            d1 = a + (c - a) * (k + 1) / n - 0.004
            b.box(x_wall - 0.36, d0, FLOOR + 1.52, x_wall - 0.34, d1, FLOOR + 2.18, "M_CabinetDoor")
            hy = d1 - 0.05 if k % 2 == 0 else d0 + 0.05
            b.box(x_wall - 0.385, hy - 0.006, FLOOR + 1.55, x_wall - 0.36, hy + 0.006, FLOOR + 1.63, "M_MetalBrushed")
    # Nevera junto a la puerta del lavadero (contra el tabique norte).
    fx0, fx1, fy0, fy1 = 9.86, 10.56, 14.22, 14.92
    b.box(fx0, fy0, FLOOR, fx1, fy1, FLOOR + 1.78, "M_Fridge")
    b.box(fx0 + 0.01, fy0 - 0.012, FLOOR + 0.05, fx1 - 0.01, fy0, FLOOR + 1.08, "M_Fridge")
    b.box(fx0 + 0.01, fy0 - 0.012, FLOOR + 1.1, fx1 - 0.01, fy0, FLOOR + 1.76, "M_Fridge")
    for z0, z1 in ((FLOOR + 0.7, FLOOR + 1.02), (FLOOR + 1.16, FLOOR + 1.42)):
        b.box(fx1 - 0.07, fy0 - 0.045, z0, fx1 - 0.05, fy0 - 0.012, z1, "M_Chrome")
    b.box(fx0, fy0 - 0.01, FLOOR, fx1, fy0, FLOOR + 0.05, "M_CabinetDark")
    return b.object()


def bathroom():
    b = G.Builder("Bathroom")
    # Bañera contra el muro norte.
    x0, x1, y0, y1 = 9.82, 11.84, 17.1, 17.8
    tz = FLOOR + 0.55
    b.box(x0, y0, FLOOR, x1, y0 + 0.06, tz, "M_Porcelain")
    b.box(x0, y1 - 0.06, FLOOR, x1, y1, tz, "M_Porcelain")
    b.box(x0, y0, FLOOR, x0 + 0.06, y1, tz, "M_Porcelain")
    b.box(x1 - 0.06, y0, FLOOR, x1, y1, tz, "M_Porcelain")
    b.box(x0 + 0.06, y0 + 0.06, FLOOR + 0.08, x1 - 0.06, y1 - 0.06, FLOOR + 0.1, "M_Porcelain")
    b.box(x0, y0 - 0.02, FLOOR, x1, y0, FLOOR + 0.06, "M_Grout")
    # Azulejo de pared alrededor de la bañera.
    for quad, axis, out in (
        ([(x0, 17.8, FLOOR), (x1, 17.8, FLOOR), (x1, 17.8, FLOOR + 1.8), (x0, 17.8, FLOOR + 1.8)], "x", -1),
    ):
        f = b.face([(p[0], p[1] - 0.003, p[2]) for p in quad], "M_BathTile")
        G.orient(f, axis, out, b)
    # Grifería.
    b.cylinder(11.6, 17.74, tz + 0.12, tz + 0.15, 0.02, "M_Chrome", 12)
    b.box(11.58, 17.6, tz + 0.12, 11.62, 17.78, tz + 0.15, "M_Chrome")
    # Inodoro contra el tabique del lavadero, mirando al oeste.
    cx, cy = 11.55, 16.05
    b.box(cx + 0.06, cy - 0.21, FLOOR + 0.35, cx + 0.26, cy + 0.21, FLOOR + 0.75, "M_Porcelain")
    b.box(cx + 0.04, cy - 0.23, FLOOR + 0.75, cx + 0.28, cy + 0.23, FLOOR + 0.78, "M_Porcelain")
    b.cylinder(cx - 0.12, cy, FLOOR, FLOOR + 0.38, 0.17, "M_Porcelain", 20, r_top=0.19)
    b.cylinder(cx - 0.12, cy, FLOOR + 0.38, FLOOR + 0.41, 0.2, "M_ToiletSeat", 20)
    # Lavabo con pedestal y espejo.
    lx, ly = 10.5, 15.28
    b.cylinder(lx, ly, FLOOR, FLOOR + 0.72, 0.08, "M_Porcelain", 16, r_top=0.11)
    b.box(lx - 0.28, ly - 0.2, FLOOR + 0.72, lx + 0.28, ly + 0.2, FLOOR + 0.86, "M_Porcelain")
    b.box(lx - 0.2, ly - 0.13, FLOOR + 0.84, lx + 0.2, ly + 0.11, FLOOR + 0.865, "M_Steel")
    b.cylinder(lx, ly - 0.15, FLOOR + 0.86, FLOOR + 1.02, 0.014, "M_Chrome", 12)
    b.box(lx - 0.3, 15.065, FLOOR + 1.15, lx + 0.3, 15.085, FLOOR + 1.85, "M_Mirror")
    b.box(lx - 0.32, 15.06, FLOOR + 1.13, lx + 0.32, 15.07, FLOOR + 1.87, "M_CabinetDark")
    # Azulejo hasta media altura en todo el baño.
    for (a, c, fixed, axis, out) in ((9.76, 11.84, 15.065, "x", 1), (15.06, 17.1, 9.765, "y", 1), (15.06, 17.1, 11.835, "y", -1)):
        if axis == "x":
            quad = [(a, fixed, FLOOR), (c, fixed, FLOOR), (c, fixed, FLOOR + 1.2), (a, fixed, FLOOR + 1.2)]
        else:
            quad = [(fixed, a, FLOOR), (fixed, c, FLOOR), (fixed, c, FLOOR + 1.2), (fixed, a, FLOOR + 1.2)]
        f = b.face(quad, "M_BathTile")
        G.orient(f, axis, out, b)
    return b.object()


def laundry():
    b = G.Builder("Laundry")
    # Lavadora de carga frontal contra el muro este.
    x0, x1, y0, y1 = 13.78, 14.28, 15.1, 15.72
    z0 = LAUNDRY
    b.box(x0, y0, z0, x1, y1, z0 + 0.85, "M_Appliance")
    b.cylinder(x0 - 0.005, (y0 + y1) / 2, z0 + 0.3, z0 + 0.3 + 0.001, 0.001, "M_Chrome", 6, caps=False)
    # Puerta de ojo de buey (anillos en la cara oeste).
    import bmesh  # noqa: F401
    from mathutils import Matrix, Vector
    tmp = G.Builder("tmp")
    tmp.cylinder(0, 0, 0.0, 0.03, 0.2, "M_Chrome", 32)
    tmp.cylinder(0, 0, 0.03, 0.04, 0.15, "M_WasherGlass", 32)
    rot = Matrix.Rotation(math.radians(-90), 3, "Y")
    for f in tmp.bm.faces:
        verts = [b.bm.verts.new(rot @ v.co + Vector((x0, (y0 + y1) / 2, z0 + 0.42))) for v in f.verts]
        nf = b.bm.faces.new(verts)
        nf.material_index = b.mat_index(tmp.mats[f.material_index])
        b.apply_uv(nf, "auto", Vector((0, 0, 0)))
    tmp.bm.free()
    b.box(x0 - 0.01, y0 + 0.05, z0 + 0.72, x0, y1 - 0.05, z0 + 0.82, "M_ApplianceDark")
    # Pila de lavar (fregadero de obra) junto a la lavadora.
    b.box(13.7, 15.8, z0, 14.28, 16.5, z0 + 0.82, "M_Concrete")
    b.box(13.74, 15.84, z0 + 0.6, 14.24, 16.46, z0 + 0.62, "M_ConcreteDark")
    # Estante de madera alto sobre la lavadora.
    b.box(13.95, 15.05, z0 + 1.55, 14.28, 16.6, z0 + 1.58, "M_ShelfWood")
    for yy in (15.15, 16.5):
        b.box(14.2, yy - 0.015, z0 + 1.4, 14.28, yy + 0.015, z0 + 1.55, "M_MetalDark")
    return b.object()


def bedroom():
    b = G.Builder("BedroomElena")
    # Colchón y ropa de cama sobre el somier de hierro (cama individual, cabecero al oeste).
    x0, x1, y0, y1 = 4.78, 6.72, 16.55, 17.43
    mz = FLOOR + 0.42
    b.box(x0, y0, mz, x1, y1, mz + 0.2, "M_Mattress")
    b.box(x0 + 0.45, y0 - 0.04, mz + 0.18, x1 + 0.03, y1 + 0.04, mz + 0.25, "M_Quilt")
    b.box(x0 + 0.45, y0 - 0.06, mz - 0.2, x0 + 0.47, y1 + 0.06, mz + 0.25, "M_Quilt")
    b.box(x0 + 0.45, y0 - 0.06, mz - 0.2, x1 + 0.05, y0 - 0.04, mz + 0.25, "M_Quilt")
    b.box(x0 + 0.45, y1 + 0.04, mz - 0.2, x1 + 0.05, y1 + 0.06, mz + 0.25, "M_Quilt")
    b.box(x0 + 0.06, y0 + 0.1, mz + 0.2, x0 + 0.42, y1 - 0.1, mz + 0.34, "M_Pillow")
    # Armario empotrado de dos puertas (ropa de Elena), contra el tabique del pasillo.
    ax0, ax1, ay0, ay1 = 7.82, 8.44, 16.55, 17.92
    b.box(ax0, ay0, FLOOR, ax1, ay1, FLOOR + 2.2, "M_WardrobeCarcass")
    for (d0, d1) in ((ay0 + 0.01, (ay0 + ay1) / 2 - 0.004), ((ay0 + ay1) / 2 + 0.004, ay1 - 0.01)):
        b.box(ax0 - 0.02, d0, FLOOR + 0.05, ax0, d1, FLOOR + 2.15, "M_WardrobeDoor")
        for (z0, z1) in ((FLOOR + 0.15, FLOOR + 1.0), (FLOOR + 1.1, FLOOR + 2.05)):
            b.box(ax0 - 0.028, d0 + 0.06, z0, ax0 - 0.02, d1 - 0.06, z1, "M_WardrobeDoor")
    my = (ay0 + ay1) / 2
    for hy in (my - 0.03, my + 0.03):
        b.box(ax0 - 0.05, hy - 0.006, FLOOR + 1.0, ax0 - 0.028, hy + 0.006, FLOOR + 1.15, "M_MetalBrushed")
    return b.object()


def fixtures():
    """Plafones (mismo diseño en todas las habitaciones salvo cocina) e interruptores junto a las puertas."""
    b = G.Builder("Fixtures")
    lamps = {"Sala": (7.1, 8.25), "Comedor": (12.1, 8.25), "Pasillo": (9.1, 14.2), "Dormitorio2": (6.5, 12.4),
             "DormitorioElena": (6.5, 16.15), "Bano": (10.8, 16.4), "Lavadero": (13.2, 16.5)}
    for name, (x, y) in lamps.items():
        b.cylinder(x, y, CEIL - 0.02, CEIL, 0.09, "M_Trim", 20)
        b.cylinder(x, y, CEIL - 0.11, CEIL - 0.02, 0.17, "M_LampShadeGlass", 24, r_top=0.12)
    return b.object()


SWITCHES = [
    # (id, x, y, z, normal: 'n','s','e','w')
    ("Switch_Sala", 5.95, 6.21, FLOOR + 1.1, "n"),
    ("Switch_Comedor", 11.95, 10.38, FLOOR + 1.1, "s"),
    ("Switch_Pasillo", 8.62, 10.75, FLOOR + 1.1, "e"),
    ("Switch_Dormitorio2", 8.38, 12.55, FLOOR + 1.1, "w"),
    ("Switch_DormitorioElena", 8.38, 15.35, FLOOR + 1.1, "w"),
    ("Switch_Cocina", 9.82, 12.15, FLOOR + 1.1, "e"),
    ("Switch_Bano", 9.82, 16.98, FLOOR + 1.1, "e"),
    ("Switch_Lavadero", 13.5, 15.12, FLOOR + 1.1, "n"),
]


def switch_plate(name):
    """Placa de interruptor con su tecla (objeto aparte para animarla); origen en el centro de la placa."""
    b = G.Builder(name)
    b.box(-0.04, -0.008, -0.04, 0.04, 0.0, 0.04, "M_SwitchPlate")
    b.box(-0.016, -0.016, -0.022, 0.016, -0.008, 0.022, "M_SwitchPlate")
    return b.object()


def interior_door():
    """Hoja de puerta interior lisa de cuarterones pintada; origen en la bisagra."""
    b = G.Builder("Door_Interior_Leaf")
    w, h, t = 0.78, 2.03, 0.04
    b.box(0, 0, 0, w, t, h, "M_DoorInterior")
    for (z0, z1) in ((0.15, 0.95), (1.1, 1.9)):
        for side in (-0.004, t):
            b.box(0.12, side, z0, w - 0.12, side + 0.004, z1, "M_DoorInterior")
    hx = w - 0.07
    for face, s in ((0.0, -1), (t, 1)):
        b.box(hx - 0.02, min(face, face + s * 0.008), 0.97, hx + 0.02, max(face, face + s * 0.008), 1.09, "M_MetalBrushed")
        b.box(hx - 0.11, min(face + s * 0.045, face + s * 0.06), 1.02, hx + 0.005, max(face + s * 0.045, face + s * 0.06), 1.04, "M_MetalBrushed")
        b.box(hx - 0.008, min(face + s * 0.008, face + s * 0.05), 1.02, hx + 0.008, max(face + s * 0.008, face + s * 0.05), 1.04, "M_MetalBrushed")
    return b.object()


def main():
    G.clear_scene()
    objs = floors_and_ceilings() + [walls(), trims(), kitchen(), bathroom(), laundry(), bedroom(), fixtures()]
    G.export_fbx(objs, os.path.join(OUT, "SM_Interior.fbx"))
    G.export_fbx([interior_door()], os.path.join(OUT, "SM_Door_Interior_Leaf.fbx"))
    G.export_fbx([switch_plate("Switch")], os.path.join(OUT, "SM_Switch.fbx"))


main()
