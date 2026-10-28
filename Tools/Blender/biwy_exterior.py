"""
Exterior del episodio 01: casa de Elena (envolvente, molduras, ventanas, puerta, porche, tejado), parcela, calle,
vallas con portón, cobertizo y casas vecinas. Medidas de Docs/Proyecto/PLANO_TECNICO.md.

blender -b -P Tools/Blender/biwy_exterior.py -- <carpeta_salida>
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import biwy_geo as G  # noqa: E402

OUT = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "Assets/_Game/Art/Models/Exterior"

PITCH = math.radians(25.0)
T = math.tan(PITCH)
FLOOR = 0.45        # suelo interior
PLATE = 3.05        # coronación de muros (techo 2.60)
WALL = 0.20


# ---------------------------------------------------------------------------------------------------------------
# Ventanas y puertas
# ---------------------------------------------------------------------------------------------------------------

def window(b, axis, face, outward, a0, a1, z0, z1, frosted=False, lit=False):
    """Ventana de guillotina con marco, dos hojas, travesaños, cristal, cortina, jambas exteriores y alféizar."""
    def P(a, z, d):
        # d: profundidad desde la cara exterior hacia dentro (positiva = dentro).
        off = face - outward * d
        return (a, off, z) if axis == "x" else (off, a, z)

    def slab(a_lo, a_hi, z_lo, z_hi, d_lo, d_hi, mat):
        pts = [P(a_lo, z_lo, d_lo), P(a_hi, z_lo, d_lo), P(a_hi, z_hi, d_lo), P(a_lo, z_hi, d_lo),
               P(a_lo, z_lo, d_hi), P(a_hi, z_lo, d_hi), P(a_hi, z_hi, d_hi), P(a_lo, z_hi, d_hi)]
        xs = [p[0] for p in pts]
        ys = [p[1] for p in pts]
        zs = [p[2] for p in pts]
        b.box(min(xs), min(ys), min(zs), max(xs), max(ys), max(zs), mat)

    fr = 0.055   # marco
    d0, d1 = 0.05, 0.13
    # Marco perimetral.
    slab(a0, a1, z0, z0 + fr, d0, d1, "M_WindowFrame")
    slab(a0, a1, z1 - fr, z1, d0, d1, "M_WindowFrame")
    slab(a0, a0 + fr, z0, z1, d0, d1, "M_WindowFrame")
    slab(a1 - fr, a1, z0, z1, d0, d1, "M_WindowFrame")
    mid = (z0 + z1) / 2
    sash = 0.045
    ia0, ia1, iz0, iz1 = a0 + fr, a1 - fr, z0 + fr, z1 - fr
    # Hoja inferior (más adentro) y superior (más afuera), con travesaño de encuentro.
    for (s_z0, s_z1, s_d0, s_d1) in ((iz0, mid + 0.02, 0.085, 0.12), (mid - 0.02, iz1, 0.06, 0.095)):
        slab(ia0, ia1, s_z0, s_z0 + sash, s_d0, s_d1, "M_WindowFrame")
        slab(ia0, ia1, s_z1 - sash, s_z1, s_d0, s_d1, "M_WindowFrame")
        slab(ia0, ia0 + sash, s_z0, s_z1, s_d0, s_d1, "M_WindowFrame")
        slab(ia1 - sash, ia1, s_z0, s_z1, s_d0, s_d1, "M_WindowFrame")
        # Cuarterones 2 x 2.
        ga0, ga1, gz0, gz1 = ia0 + sash, ia1 - sash, s_z0 + sash, s_z1 - sash
        if not frosted:
            am = (ga0 + ga1) / 2
            zm = (gz0 + gz1) / 2
            slab(am - 0.011, am + 0.011, gz0, gz1, s_d0 + 0.008, s_d1 - 0.008, "M_WindowFrame")
            slab(ga0, ga1, zm - 0.011, zm + 0.011, s_d0 + 0.008, s_d1 - 0.008, "M_WindowFrame")
        glass_d = (s_d0 + s_d1) / 2
        quad = [P(ga0, gz0, glass_d), P(ga1, gz0, glass_d), P(ga1, gz1, glass_d), P(ga0, gz1, glass_d)]
        f = b.face(quad, "M_GlassFrosted" if frosted else "M_Glass")
        G.orient(f, axis, outward, b)
    # Cortina (o estor) por dentro: tela iluminada solo si la habitación tiene luz.
    if not frosted:
        curtains(b, P, axis, outward, ia0 - 0.08, ia1 + 0.08, iz0 - 0.04, iz1 + 0.08, 0.22, "M_CurtainLit" if lit else "M_Curtain")
    # Jambas exteriores (casing), dintel con vierteaguas y alféizar.
    cw, cp = 0.10, 0.025
    slab(a0 - cw, a0, z0, z1 + cw, -cp, 0.0, "M_Trim")
    slab(a1, a1 + cw, z0, z1 + cw, -cp, 0.0, "M_Trim")
    slab(a0 - cw - 0.02, a1 + cw + 0.02, z1 + cw, z1 + cw + 0.035, -cp - 0.015, 0.0, "M_Trim")
    slab(a0 - cw - 0.04, a1 + cw + 0.04, z0 - 0.055, z0, -0.07, 0.05, "M_Trim")
    slab(a0 - cw + 0.01, a1 + cw - 0.01, z0 - 0.11, z0 - 0.055, -0.03, 0.0, "M_Trim")


def curtains(b, P, axis, outward, a0, a1, z0, z1, depth, mat):
    """Dos paños corridos con pliegues (onda irregular) que se juntan en el centro, y barra por encima."""
    mid = (a0 + a1) / 2
    for (lo, hi, phase) in ((a0, mid + 0.02, 0.0), (mid - 0.02, a1, 1.3)):
        width = hi - lo
        folds = max(3, int(round(width / 0.11)))
        steps = folds * 4
        prev = None
        for k in range(steps + 1):
            a = lo + width * k / steps
            t = 2 * math.pi * folds * k / steps + phase
            # Amplitud mayor junto a los bordes recogidos; la tela cae algo más separada abajo.
            amp = 0.022 + 0.008 * math.sin(phase + k * 0.7)
            d_top = depth + 0.02 + amp * math.sin(t)
            d_bot = depth + 0.03 + amp * 1.25 * math.sin(t + 0.25)
            cur = (P(a, z0, d_bot), P(a, z1, d_top))
            if prev is not None:
                f = b.face([prev[0], cur[0], cur[1], prev[1]], mat, uv_mode="auto")
                if f is not None:
                    f.smooth = True
                    G.orient(f, axis, outward, b)
            prev = cur
    # Barra metálica y soportes.
    def slab(a_lo, a_hi, z_lo, z_hi, d_lo, d_hi, m):
        pts = [P(a_lo, z_lo, d_lo), P(a_hi, z_hi, d_hi)]
        b.box(min(p[0] for p in pts), min(p[1] for p in pts), min(p[2] for p in pts),
              max(p[0] for p in pts), max(p[1] for p in pts), max(p[2] for p in pts), m)
    slab(a0 - 0.06, a1 + 0.06, z1 + 0.01, z1 + 0.03, depth + 0.01, depth + 0.03, "M_CurtainRod")
    for a in (a0 - 0.04, a1 + 0.04):
        slab(a - 0.01, a + 0.01, z1 + 0.005, z1 + 0.035, 0.0, depth + 0.03, "M_CurtainRod")


def door_frame(b, axis, face, outward, a0, a1, z0, height=2.10):
    """Cerco de una puerta exterior (la hoja es un objeto aparte para poder abrirla)."""
    z1 = z0 + height
    cw = 0.11

    def box(a_lo, a_hi, d_lo, d_hi, zl, zh, mat):
        # d: profundidad hacia dentro desde la cara exterior (negativa = sobresale).
        f0, f1 = face - outward * d_lo, face - outward * d_hi
        if axis == "x":
            b.box(a_lo, min(f0, f1), zl, a_hi, max(f0, f1), zh, mat)
        else:
            b.box(min(f0, f1), a_lo, zl, max(f0, f1), a_hi, zh, mat)

    box(a0 - cw, a0, -0.025, 0.0, z0, z1 + cw, "M_Trim")
    box(a1, a1 + cw, -0.025, 0.0, z0, z1 + cw, "M_Trim")
    box(a0 - cw - 0.03, a1 + cw + 0.03, -0.045, 0.0, z1 + cw, z1 + cw + 0.05, "M_Trim")
    box(a0, a0 + 0.04, 0.0, WALL, z0, z1, "M_DoorFrame")
    box(a1 - 0.04, a1, 0.0, WALL, z0, z1, "M_DoorFrame")
    box(a0, a1, 0.0, WALL, z1 - 0.04, z1, "M_DoorFrame")
    box(a0, a1, -0.01, WALL, z0 - 0.01, z0 + 0.012, "M_MetalBrushed")


def porch_lantern(b, x, y_face, z):
    """Farol de pared junto a la puerta: placa, brazo, cuerpo con cristales iluminados y sombrerete."""
    y = y_face
    b.box(x - 0.06, y - 0.02, z - 0.13, x + 0.06, y, z + 0.13, "M_MetalDark")
    b.box(x - 0.015, y - 0.1, z - 0.02, x + 0.015, y - 0.02, z + 0.01, "M_MetalDark")
    cy = y - 0.17
    w = 0.08
    b.box(x - w, cy - w, z - 0.16, x + w, cy + w, z - 0.14, "M_MetalDark")
    for sx in (-1, 1):
        for sy in (-1, 1):
            b.box(x + sx * w - 0.008, cy + sy * w - 0.008, z - 0.14, x + sx * w + 0.008, cy + sy * w + 0.008, z + 0.12, "M_MetalDark")
    b.box(x - w + 0.004, cy - w + 0.004, z - 0.14, x + w - 0.004, cy + w - 0.004, z + 0.12, "M_LampGlass")
    b.box(x - w - 0.02, cy - w - 0.02, z + 0.12, x + w + 0.02, cy + w + 0.02, z + 0.15, "M_MetalDark")
    b.prism([(x - 0.05, cy - 0.05), (x + 0.05, cy - 0.05), (x + 0.05, cy + 0.05), (x - 0.05, cy + 0.05)], 0.05, "M_MetalDark", z0=z + 0.15)


def door_leaf(name, width, height, thickness, glass_rows=1, style="panel", mat="M_DoorPaint"):
    """Hoja de puerta con el origen en la bisagra (x = 0), cara exterior en y = 0, hacia +x y +z."""
    b = G.Builder(name)
    w, h, t = width, height, thickness
    rail, stile = 0.11, 0.11
    if style == "panel":
        # Bastidor y cuarterones rehundidos (4 paneles + 2 vidrios superiores).
        b.box(0, 0, 0, stile, t, h, mat)
        b.box(w - stile, 0, 0, w, t, h, mat)
        b.box(stile, 0, 0, w - stile, t, 0.22, mat)
        b.box(stile, 0, h - rail, w - stile, t, h, mat)
        b.box(stile, 0, 0.95, w - stile, t, 1.12, mat)
        b.box(stile, 0, 1.62, w - stile, t, 1.72, mat)
        mid = w / 2
        b.box(mid - 0.05, 0, 0.22, mid + 0.05, t, 0.95, mat)
        b.box(mid - 0.05, 0, 1.12, mid + 0.05, t, 1.62, mat)
        # Paneles (más finos) con moldura.
        for (a0, a1) in ((stile, mid - 0.05), (mid + 0.05, w - stile)):
            for (z0, z1) in ((0.22, 0.95), (1.12, 1.62)):
                b.box(a0, 0.012, z0, a1, t - 0.012, z1, mat)
                b.box(a0 + 0.035, 0.004, z0 + 0.035, a1 - 0.035, t - 0.004, z1 - 0.035, mat)
            # Vidrio superior.
            b.box(a0, 0.018, 1.72, a1, 0.022, h - rail, "M_GlassDoor")
            b.box(a0, t - 0.022, 1.72, a1, t - 0.018, h - rail, "M_GlassDoor")
    else:
        # Puerta de tablas (cobertizo, servicio): listones verticales con travesaños.
        n = max(4, int(w / 0.13))
        for i in range(n):
            a0 = i * w / n + 0.003
            a1 = (i + 1) * w / n - 0.003
            b.box(a0, 0, 0, a1, t, h, mat)
        for z in (0.18, h / 2, h - 0.25):
            b.box(0.06, t, z - 0.06, w - 0.06, t + 0.025, z + 0.06, mat)
    # Manilla de palanca y bocallave en ambas caras (fuera: y < 0; dentro: y > t).
    hx = w - 0.075
    for face_y, sign in ((0.0, -1), (t, 1)):
        y_plate = face_y + sign * 0.008
        b.box(hx - 0.024, min(face_y, y_plate), 0.95, hx + 0.024, max(face_y, y_plate), 1.13, "M_MetalBrushed")
        y_neck = y_plate + sign * 0.05
        b.box(hx - 0.009, min(y_plate, y_neck), 1.03, hx + 0.009, max(y_plate, y_neck), 1.05, "M_MetalBrushed")
        b.box(hx - 0.12, min(y_neck, y_neck - sign * 0.018), 1.03, hx + 0.009, max(y_neck, y_neck - sign * 0.018), 1.05, "M_MetalBrushed")
        b.box(hx - 0.02, min(face_y, y_plate + sign * 0.004), 1.2, hx + 0.02, max(face_y, y_plate + sign * 0.004), 1.25, "M_MetalBrushed")
    return b.object()


# ---------------------------------------------------------------------------------------------------------------
# Casa
# ---------------------------------------------------------------------------------------------------------------

def house(name, x0, y0, width, depth, openings, porch=None, chimney=None, siding="M_Siding", lit_rooms=(), extras=None):
    """
    Envolvente de una casa de una planta con tejado a dos aguas (cumbrera este-oeste).
    openings: dict lado -> lista de (a0, a1, z0, z1, tipo). Lados: 's','n','w','e'. Tipo: 'win','frosted','door','service'.
    """
    b = G.Builder(name)
    x1, y1 = x0 + width, y0 + depth
    yr = y0 + depth / 2
    ridge = PLATE + (depth / 2) * T
    over, rake = 0.45, 0.30

    # Zócalo de hormigón.
    for side, axis, face, outward, a0, a1 in (("s", "x", y0 - 0.025, -1, x0 - 0.025, x1 + 0.025), ("n", "x", y1 + 0.025, 1, x0 - 0.025, x1 + 0.025),
                                              ("w", "y", x0 - 0.025, -1, y0 - 0.025, y1 + 0.025), ("e", "y", x1 + 0.025, 1, y0 - 0.025, y1 + 0.025)):
        low = [(o[0], o[1], o[2], o[3]) for o in openings.get(side, []) if o[2] < FLOOR]
        G.wall_with_openings(b, axis, face, a0, a1, 0.0, FLOOR, low, 0.03, outward, "M_Foundation", inner=False)
    # Remate del zócalo: solo un borde alrededor del muro (no una tapa sobre toda la planta).
    cap0, cap1 = FLOOR - 0.04, FLOOR + 0.005
    b.box(x0 - 0.035, y0 - 0.035, cap0, x1 + 0.035, y0 + 0.01, cap1, "M_Trim", faces="nsewt")
    b.box(x0 - 0.035, y1 - 0.01, cap0, x1 + 0.035, y1 + 0.035, cap1, "M_Trim", faces="nsewt")
    b.box(x0 - 0.035, y0 + 0.01, cap0, x0 + 0.01, y1 - 0.01, cap1, "M_Trim", faces="ewt")
    b.box(x1 - 0.01, y0 + 0.01, cap0, x1 + 0.035, y1 - 0.01, cap1, "M_Trim", faces="ewt")

    def ops(side):
        return [(o[0], o[1], o[2], o[3]) for o in openings.get(side, [])]

    G.wall_with_openings(b, "x", y0, x0, x1, FLOOR, PLATE, ops("s"), WALL, -1, siding, "M_InteriorWall", "M_Trim")
    G.wall_with_openings(b, "x", y1, x0, x1, FLOOR, PLATE, ops("n"), WALL, 1, siding, "M_InteriorWall", "M_Trim")
    G.wall_with_openings(b, "y", x0, y0, y1, FLOOR, PLATE, ops("w"), WALL, -1, siding, "M_InteriorWall", "M_Trim")
    G.wall_with_openings(b, "y", x1, y0, y1, FLOOR, PLATE, ops("e"), WALL, 1, siding, "M_InteriorWall", "M_Trim")

    # Hastiales (triángulos de revestimiento) al oeste y al este.
    for xg, sign in ((x0, -1), (x1, 1)):
        tri = [(xg, y0, PLATE), (xg, y1, PLATE), (xg, yr, ridge - 0.02)]
        f = b.face(tri, siding)
        G.orient(f, "y", sign, b)
        f = b.face([(xg - sign * WALL, p[1], p[2]) for p in tri], "M_InteriorWall")
        G.orient(f, "y", -sign, b)
        # Rejilla de ventilación del hastial.
        vz = PLATE + (ridge - PLATE) * 0.42
        b.box(xg + (-0.03 if sign < 0 else 0), yr - 0.32, vz - 0.32, xg + (0 if sign < 0 else 0.03), yr + 0.32, vz + 0.32, "M_Trim")
        for k in range(7):
            z = vz - 0.26 + k * 0.085
            b.box(xg + (-0.06 if sign < 0 else 0.0), yr - 0.25, z, xg + (0.0 if sign < 0 else 0.06), yr + 0.25, z + 0.03, "M_TrimShadow")

    # Esquineros.
    for (cx, cy) in ((x0, y0), (x1, y0), (x0, y1), (x1, y1)):
        sx = -1 if cx == x0 else 1
        sy = -1 if cy == y0 else 1
        b.box(min(cx, cx + sx * 0.03), min(cy - sy * 0.11, cy + sy * 0.03), FLOOR, max(cx, cx + sx * 0.03), max(cy - sy * 0.11, cy + sy * 0.03), PLATE, "M_Trim")
        b.box(min(cx - sx * 0.11, cx + sx * 0.03), min(cy, cy + sy * 0.03), FLOOR, max(cx - sx * 0.11, cx + sx * 0.03), max(cy, cy + sy * 0.03), PLATE, "M_Trim")

    # Friso bajo el alero (sur y norte) y en los hastiales.
    b.box(x0, y0 - 0.025, PLATE - 0.2, x1, y0, PLATE, "M_Trim")
    b.box(x0, y1, PLATE - 0.2, x1, y1 + 0.025, PLATE, "M_Trim")

    # Ventanas y puertas.
    for side, items in openings.items():
        for (a0, a1, z0, z1, kind) in items:
            axis = "x" if side in "sn" else "y"
            face = {"s": y0, "n": y1, "w": x0, "e": x1}[side]
            outward = -1 if side in "sw" else 1
            if kind in ("win", "frosted"):
                window(b, axis, face, outward, a0, a1, z0, z1, frosted=(kind == "frosted"), lit=(side, a0) in lit_rooms)
            elif kind in ("door", "service"):
                door_frame(b, axis, face, outward, a0, a1, z0, z1 - z0)

    if extras:
        extras(b)

    # ---------- Tejado ----------
    porch_x = (porch["x0"] - 0.0, porch["x1"]) if porch else None
    roof_t = 0.16
    eave_z = PLATE - over * T

    def slope_z(y):
        return PLATE + (min(y - y0, y1 - y)) * T

    # Faldones (superficie de teja), con espesor visible en el borde.
    segments_south = [(x0 - rake, x1 + rake)]
    if porch_x:
        segments_south = [(x0 - rake, porch_x[0]), (porch_x[0], porch_x[1]), (porch_x[1], x1 + rake)]
    for (sa, sb) in segments_south:
        y_start = y0 - over
        if porch_x and sa == porch_x[0]:
            y_start = y0  # el porche cubre ese tramo
        z_start = slope_z(y_start) if y_start >= y0 else eave_z
        b.face([(sa, y_start, z_start + roof_t), (sb, y_start, z_start + roof_t), (sb, yr, ridge + roof_t), (sa, yr, ridge + roof_t)], "M_Roof")
        b.face([(sa, y_start, z_start), (sa, yr, ridge), (sb, yr, ridge), (sb, y_start, z_start)], "M_Soffit")
        if y_start < y0:
            # Imposta (fascia) y sofito horizontal.
            b.box(sa, y_start - 0.03, z_start - 0.05, sb, y_start, z_start + roof_t + 0.01, "M_Trim")
            b.face([(sa, y_start, eave_z - 0.05), (sb, y_start, eave_z - 0.05), (sb, y0, eave_z - 0.05), (sa, y0, eave_z - 0.05)], "M_Soffit", flip=True)
    b.face([(x1 + rake, y1 + over, eave_z + roof_t), (x0 - rake, y1 + over, eave_z + roof_t), (x0 - rake, yr, ridge + roof_t), (x1 + rake, yr, ridge + roof_t)], "M_Roof")
    b.face([(x1 + rake, y1 + over, eave_z), (x1 + rake, yr, ridge), (x0 - rake, yr, ridge), (x0 - rake, y1 + over, eave_z)], "M_Soffit")
    b.box(x0 - rake, y1 + over, eave_z - 0.05, x1 + rake, y1 + over + 0.03, eave_z + roof_t + 0.01, "M_Trim")
    b.face([(x0 - rake, y1, eave_z - 0.05), (x1 + rake, y1, eave_z - 0.05), (x1 + rake, y1 + over, eave_z - 0.05), (x0 - rake, y1 + over, eave_z - 0.05)], "M_Soffit", flip=True)
    # Caballete.
    b.box(x0 - rake, yr - 0.09, ridge + roof_t - 0.01, x1 + rake, yr + 0.09, ridge + roof_t + 0.05, "M_RoofRidge")
    # Tablas de remate en los hastiales (inclinadas).
    for xg in (x0 - rake, x1 + rake):
        for (ya, yb) in ((y0 - over, yr), (y1 + over, yr)):
            za = eave_z
            zb = ridge
            d = 0.035 if xg > x0 else -0.035
            quad = [(xg, ya, za - 0.06), (xg, yb, zb - 0.06), (xg, yb, zb + roof_t + 0.02), (xg, ya, za + roof_t + 0.02)]
            f = b.face(quad, "M_Trim")
            G.orient(f, "y", 1 if xg > x0 else -1, b)
            f = b.face([(xg - d, p[1], p[2]) for p in quad], "M_Trim")
            G.orient(f, "y", -1 if xg > x0 else 1, b)
            b.face([(xg - d, ya, za + roof_t + 0.02), (xg, ya, za + roof_t + 0.02), (xg, yb, zb + roof_t + 0.02), (xg - d, yb, zb + roof_t + 0.02)], "M_Trim")

    # Canalones y bajantes.
    gutters = [((x0 - rake, porch_x[0]) if porch_x else (x0 - rake, x1 + rake), y0 - over - 0.03, -1)]
    if porch_x:
        gutters.append(((porch_x[1], x1 + rake), y0 - over - 0.03, -1))
    gutters.append(((x0 - rake, x1 + rake), y1 + over + 0.03, 1))
    for (ga, gb), gy, s in gutters:
        y_in, y_out = (gy, gy + s * 0.12)
        b.box(ga, min(y_in, y_out), eave_z - 0.11, gb, max(y_in, y_out), eave_z + 0.02, "M_Gutter", faces="nsewb")
        b.box(ga, min(y_in, y_out) + 0.01, eave_z - 0.07, gb, max(y_in, y_out) - 0.01, eave_z + 0.0, "M_GutterInner", faces="t")
    for (dx, dy) in ((x0 + 0.12, y0 - 0.06), (x1 - 0.12, y0 - 0.06), (x0 + 0.12, y1 + 0.06), (x1 - 0.12, y1 + 0.06)):
        b.box(dx - 0.04, dy - 0.03, 0.08, dx + 0.04, dy + 0.03, eave_z - 0.1, "M_Gutter")
        sgn = -1 if dy < y0 else 1
        b.box(dx - 0.04, min(dy, dy + sgn * 0.28), 0.03, dx + 0.04, max(dy, dy + sgn * 0.28), 0.1, "M_Gutter")
        b.box(dx - 0.04, min(dy, dy + sgn * (over - 0.05)), eave_z - 0.18, dx + 0.04, max(dy, dy + sgn * (over - 0.05)), eave_z - 0.1, "M_Gutter")
        if dy < y0:
            b.box(dx - 0.16, dy + sgn * 0.3 - 0.2, 0.0, dx + 0.16, dy + sgn * 0.3 + 0.2, 0.025, "M_Foundation")

    # Chimenea de ladrillo.
    if chimney:
        cx0, cy0, cx1, cy1 = chimney
        top = ridge + 0.85
        base = slope_z(cy1) if cy1 > yr else slope_z(cy0)
        b.box(cx0, cy0, base - 0.2, cx1, cy1, top, "M_Brick", faces="nsew")
        b.box(cx0 - 0.05, cy0 - 0.05, top, cx1 + 0.05, cy1 + 0.05, top + 0.09, "M_Foundation")
        b.box(cx0 + 0.12, cy0 + 0.12, top + 0.09, cx1 - 0.12, cy1 - 0.12, top + 0.32, "M_MetalDark", faces="nsewt")
        b.box(cx0 - 0.02, cy0 - 0.02, base - 0.05, cx1 + 0.02, cy1 + 0.02, base + 0.12, "M_MetalDark", faces="nsew")

    # ---------- Porche ----------
    if porch:
        px0, px1, py0 = porch["x0"], porch["x1"], porch["y0"]
        slab_x0, slab_x1 = px0 + 0.2, px1 - 0.2
        b.box(slab_x0, py0 + 0.15, 0.0, slab_x1, y0, FLOOR, "M_PorchConcrete", faces="sewt")
        sw = porch.get("step_w", 1.6)
        sx = (porch["door_x"] - sw / 2, porch["door_x"] + sw / 2)
        for k, (zt, yd) in enumerate(((FLOOR - 0.15, 0.3), (FLOOR - 0.30, 0.6))):
            b.box(sx[0], py0 + 0.15 - yd, 0.0, sx[1], py0 + 0.15 - yd + 0.3, zt, "M_PorchConcrete", faces="sewt")
        # Pilares.
        for cxp in (slab_x0 + 0.12, slab_x1 - 0.12):
            cyp = py0 + 0.3
            b.box(cxp - 0.13, cyp - 0.13, FLOOR, cxp + 0.13, cyp + 0.13, FLOOR + 0.22, "M_Trim")
            b.box(cxp - 0.085, cyp - 0.085, FLOOR + 0.22, cxp + 0.085, cyp + 0.085, PLATE - 0.35, "M_Trim")
            b.box(cxp - 0.12, cyp - 0.12, PLATE - 0.35, cxp + 0.12, cyp + 0.12, PLATE - 0.28, "M_Trim")
        # Viga y techo del porche.
        b.box(px0 + 0.05, py0 + 0.15, PLATE - 0.28, px1 - 0.05, py0 + 0.45, PLATE, "M_Trim")
        b.box(px0 + 0.05, py0 + 0.15, PLATE - 0.28, px0 + 0.3, y0, PLATE, "M_Trim")
        b.box(px1 - 0.3, py0 + 0.15, PLATE - 0.28, px1 - 0.05, y0, PLATE, "M_Trim")
        b.face([(px0, py0, PLATE - 0.02), (px0, y0, PLATE - 0.02), (px1, y0, PLATE - 0.02), (px1, py0, PLATE - 0.02)], "M_PorchCeiling")
        # Tejado a dos aguas del porche (cumbrera norte-sur) que muere contra el faldón principal.
        pc = (px0 + px1) / 2
        half = (px1 - px0) / 2
        pr = PLATE + half * T
        for side in (-1, 1):
            xe = pc + side * half
            y_meet = y0 + half
            quad = [(xe, py0, PLATE + roof_t), (pc, py0, pr + roof_t), (pc, y_meet, pr + roof_t), (xe, y0, PLATE + roof_t)]
            f = b.face(quad if side < 0 else list(reversed(quad)), "M_Roof")
            if f is not None and f.normal.z < 0:
                f.normal_flip()
            quad_b = [(xe, py0, PLATE), (pc, py0, pr), (pc, y0, pr), (xe, y0, PLATE)]
            f = b.face(quad_b, "M_Soffit")
            if f is not None and f.normal.z > 0:
                f.normal_flip()
            # Imposta lateral y canalón.
            b.box(min(xe, xe + side * 0.03), py0, PLATE - 0.04, max(xe, xe + side * 0.03), y0, PLATE + roof_t + 0.01, "M_Trim")
        b.box(pc - 0.08, py0, pr + roof_t - 0.01, pc + 0.08, y0 + half, pr + roof_t + 0.05, "M_RoofRidge")
        # Frontón: triángulo con revestimiento y remates.
        tri = [(px0, py0 + 0.02, PLATE), (px1, py0 + 0.02, PLATE), (pc, py0 + 0.02, pr)]
        f = b.face(tri, siding)
        G.orient(f, "x", -1, b)
        for side in (-1, 1):
            xe = pc + side * half
            quad = [(xe, py0 - 0.01, PLATE - 0.05), (pc, py0 - 0.01, pr - 0.05), (pc, py0 - 0.01, pr + roof_t + 0.02), (xe, py0 - 0.01, PLATE + roof_t + 0.02)]
            f = b.face(quad, "M_Trim")
            G.orient(f, "x", -1, b)
        b.box(px0, py0 - 0.01, PLATE - 0.12, px1, py0 + 0.03, PLATE + 0.02, "M_Trim")
    return b.object()


# ---------------------------------------------------------------------------------------------------------------
# Parcela, calle y vallas
# ---------------------------------------------------------------------------------------------------------------

def site():
    b = G.Builder("Site")
    # Calzada y bordillo.
    b.face([(-40, -9.5, -0.15), (60, -9.5, -0.15), (60, -2.2, -0.15), (-40, -2.2, -0.15)], "M_Asphalt")
    b.face([(-40, -16, -0.15), (60, -16, -0.15), (60, -9.5, -0.15), (-40, -9.5, -0.15)], "M_Asphalt")
    curb_cuts = [(0.6, 3.8), (-14.6, -11.4), (21.0, 24.2)]
    xs = [-40] + [v for c in curb_cuts for v in c] + [60]
    for i in range(0, len(xs) - 1, 2):
        a, c = xs[i], xs[i + 1]
        b.box(a, -2.2, -0.15, c, -2.0, 0.0, "M_Curb", faces="nst")
    for (a, c) in curb_cuts:
        b.face([(a, -2.2, -0.15), (c, -2.2, -0.15), (c, -2.0, -0.02), (a, -2.0, -0.02)], "M_Curb")
    # Bordillo del lado opuesto.
    b.box(-40, -16.2, -0.15, 60, -16.0, 0.0, "M_Curb", faces="nst")
    b.face([(-40, -19, 0.0), (60, -19, 0.0), (60, -16.2, 0.0), (-40, -16.2, 0.0)], "M_Sidewalk")
    # Acera.
    b.face([(-40, -2.0, 0.0), (60, -2.0, 0.0), (60, 0.0, 0.0), (-40, 0.0, 0.0)], "M_Sidewalk")
    # Entradas de coches (la de Elena y las de los vecinos).
    for (a, c, ylen) in ((0.6, 3.8, 13.0), (-14.6, -11.4, 10.0), (21.0, 24.2, 10.0)):
        b.box(a, 0.0, -0.02, c, ylen, 0.02, "M_Driveway", faces="ewt")
        b.face([(a, -2.0, 0.001), (c, -2.0, 0.001), (c, 0.0, 0.001), (a, 0.0, 0.001)], "M_Driveway")
    # Sendero al porche.
    b.box(6.0, 0.0, -0.02, 7.1, 3.85, 0.025, "M_Path", faces="ewt")
    # Losa del patio trasero y pasos al cobertizo.
    b.box(11.6, 18.0, -0.02, 14.6, 20.4, 0.03, "M_PorchConcrete", faces="nsewt")
    for k in range(5):
        cx = 12.6 + k * 0.42
        cy = 20.9 + k * 0.42
        b.box(cx - 0.28, cy - 0.22, -0.02, cx + 0.28, cy + 0.22, 0.03, "M_Path", faces="nsewt")
    # Césped (por piezas que rodean lo pavimentado; el de la parcela de Elena algo más descuidado).
    b.face([(-40, 0.0, 0.0), (0.6, 0.0, 0.0), (0.6, 40, 0.0), (-40, 40, 0.0)], "M_LawnNeighbor")
    b.face([(3.8, 0.0, 0.0), (6.0, 0.0, 0.0), (6.0, 18.0, 0.0), (3.8, 18.0, 0.0)], "M_Lawn")
    b.face([(7.1, 0.0, 0.0), (18.0, 0.0, 0.0), (18.0, 4.9, 0.0), (7.1, 4.9, 0.0)], "M_Lawn")
    b.face([(6.0, 3.85, 0.0), (7.1, 3.85, 0.0), (7.1, 4.9, 0.0), (6.0, 4.9, 0.0)], "M_Lawn")
    b.face([(14.5, 4.9, 0.0), (18.0, 4.9, 0.0), (18.0, 18.0, 0.0), (14.5, 18.0, 0.0)], "M_Lawn")
    b.face([(0.6, 13.0, 0.0), (3.8, 13.0, 0.0), (3.8, 18.0, 0.0), (0.6, 18.0, 0.0)], "M_Lawn")
    b.face([(0.6, 18.0, 0.0), (18.0, 18.0, 0.0), (18.0, 26.0, 0.0), (0.6, 26.0, 0.0)], "M_Lawn")
    b.face([(18.0, 0.0, 0.0), (60, 0.0, 0.0), (60, 40, 0.0), (18.0, 40, 0.0)], "M_LawnNeighbor")
    b.face([(0.6, 26.0, 0.0), (18.0, 26.0, 0.0), (18.0, 40, 0.0), (0.6, 40, 0.0)], "M_LawnNeighbor")
    # Parterre con mantillo delante de la fachada.
    b.box(4.4, 4.9, -0.02, 6.0, 6.0, 0.04, "M_Mulch", faces="nsewt")
    b.box(7.1, 4.9, -0.02, 14.6, 6.0, 0.04, "M_Mulch", faces="nsewt")
    b.box(4.4, 4.9, 0.0, 14.6, 4.96, 0.07, "M_Edging", faces="nst")
    # Bajo la casa (por si se ve por algún hueco).
    b.face([(4.5, 6.0, 0.01), (14.5, 6.0, 0.01), (14.5, 18.0, 0.01), (4.5, 18.0, 0.01)], "M_Foundation")
    return b.object()


def fence_run(b, a0, a1, fixed, axis, height=1.8, gaps=(), mat="M_Fence"):
    """Valla de tablas verticales con postes y travesaños. gaps: huecos (a0, a1) para portones."""
    def P(a, d, z):
        return (a, fixed + d, z) if axis == "x" else (fixed + d, a, z)

    def box(a_lo, a_hi, d_lo, d_hi, z_lo, z_hi, m):
        p0 = P(a_lo, d_lo, z_lo)
        p1 = P(a_hi, d_hi, z_hi)
        b.box(min(p0[0], p1[0]), min(p0[1], p1[1]), p0[2], max(p0[0], p1[0]), max(p0[1], p1[1]), p1[2], m)

    segments = []
    start = a0
    for g0, g1 in sorted(gaps):
        segments.append((start, g0))
        start = g1
    segments.append((start, a1))
    for s0, s1 in segments:
        if s1 - s0 < 0.05:
            continue
        n_posts = max(2, int((s1 - s0) / 2.4) + 1)
        for k in range(n_posts):
            a = s0 + (s1 - s0) * k / (n_posts - 1)
            a = min(max(a, s0 + 0.045), s1 - 0.045)
            box(a - 0.045, a + 0.045, -0.045, 0.045, 0.0, height + 0.05, mat)
        for z in (0.25, height - 0.25):
            box(s0, s1, 0.045, 0.085, z - 0.045, z + 0.045, mat)
        boards = int((s1 - s0) / 0.145)
        for k in range(boards):
            a = s0 + k * (s1 - s0) / boards
            w = (s1 - s0) / boards - 0.012
            jitter = ((k * 37) % 5) * 0.008
            box(a, a + w, -0.065, -0.045, 0.04, height - jitter, mat)


def gate_leaf(width, height):
    """Portón de tablas con bastidor en Z; origen en la bisagra."""
    b = G.Builder("Gate_Side_Leaf")
    boards = int(width / 0.145)
    for k in range(boards):
        a = k * width / boards
        w = width / boards - 0.012
        b.box(a, -0.065, 0.06, a + w, -0.045, height, "M_Fence")
    for z in (0.3, height - 0.3):
        b.box(0.03, -0.045, z - 0.045, width - 0.03, -0.005, z + 0.045, "M_Fence")
    # Diagonal (del pie de la bisagra a la parte alta del otro lado).
    length = math.hypot(width - 0.1, height - 0.7)
    ang = math.atan2(height - 0.7, width - 0.1)
    import bmesh
    from mathutils import Matrix, Vector
    tmp = G.Builder("tmp")
    tmp.box(-length / 2, -0.045, -0.04, length / 2, -0.005, 0.04, "M_Fence")
    for v in tmp.bm.verts:
        v.co = Matrix.Rotation(-ang, 3, "Y") @ v.co
        v.co += Vector(((width) / 2, 0, height / 2))
    for f in tmp.bm.faces:
        verts = [b.bm.verts.new(v.co) for v in f.verts]
        nf = b.bm.faces.new(verts)
        nf.material_index = b.mat_index("M_Fence")
        b.apply_uv(nf, "auto", Vector((0, 0, 0)))
    tmp.bm.free()
    # Pestillo y tirador.
    b.box(width - 0.16, -0.08, 1.05, width - 0.04, -0.065, 1.1, "M_MetalDark")
    b.box(width - 0.12, -0.11, 0.95, width - 0.1, -0.065, 1.2, "M_MetalDark")
    for z in (0.3, height - 0.3):
        b.box(0.0, -0.075, z - 0.03, 0.32, -0.065, z + 0.03, "M_MetalDark")
    return b.object()


def fences():
    b = G.Builder("Fences")
    # Valla del portón (al fondo de la entrada de coches) con hueco para el portón.
    fence_run(b, 0.0, 4.5, 13.0, "x", gaps=[(1.6, 2.8)])
    # Lindero oeste (vecino), desde detrás de la casa hasta el fondo.
    fence_run(b, 8.0, 26.0, 0.0, "y")
    # Lindero este y su cierre frontal (sin salida).
    fence_run(b, 14.5, 18.0, 7.0, "x")
    fence_run(b, 7.0, 26.0, 18.0, "y")
    # Fondo.
    fence_run(b, 0.0, 18.0, 26.0, "x")
    # Postes del portón.
    for a in (1.6, 2.8):
        b.box(a - 0.06, 12.94, 0.0, a + 0.06, 13.06, 1.95, "M_Fence")
    return b.object()


def shed():
    """Cobertizo de tablas con tejado de chapa a un agua; puerta única al oeste."""
    b = G.Builder("Shed")
    x0, y0, x1, y1 = 14.2, 21.6, 17.2, 24.6
    h_front, h_back = 2.35, 2.05
    b.box(x0 - 0.05, y0 - 0.05, -0.02, x1 + 0.05, y1 + 0.05, 0.12, "M_PorchConcrete", faces="nsewt")
    door = (22.55, 23.5)
    # Paredes: tablas verticales (cara exterior) + cara interior.
    G.wall_with_openings(b, "y", x0, y0, y1, 0.12, h_front, [(door[0], door[1], 0.12, 2.0)], 0.06, -1, "M_ShedWall", "M_ShedWallIn", "M_ShedWall")
    G.wall_with_openings(b, "y", x1, y0, y1, 0.12, h_back, [], 0.06, 1, "M_ShedWall", "M_ShedWallIn")
    for (yy, s) in ((y0, -1), (y1, 1)):
        quad = [(x0, yy, 0.12), (x1, yy, 0.12), (x1, yy, h_back), (x0, yy, h_front)]
        f = b.face(quad, "M_ShedWall")
        G.orient(f, "x", s, b)
        f = b.face([(p[0], yy - s * 0.06, p[2]) for p in quad], "M_ShedWallIn")
        G.orient(f, "x", -s, b)
    # Tejado de chapa con vuelo.
    roof = [(x0 - 0.25, y0 - 0.2, h_front + 0.06), (x0 - 0.25, y1 + 0.2, h_front + 0.06), (x1 + 0.2, y1 + 0.2, h_back - 0.02), (x1 + 0.2, y0 - 0.2, h_back - 0.02)]
    f = b.face(roof, "M_ShedRoof")
    if f.normal.z < 0:
        f.normal_flip()
    f = b.face([(p[0], p[1], p[2] - 0.03) for p in roof], "M_ShedRoof")
    if f.normal.z > 0:
        f.normal_flip()
    for k in range(13):
        yy = y0 - 0.2 + k * (y1 - y0 + 0.4) / 12
        b.box(x0 - 0.25, yy - 0.012, h_front + 0.06, x1 + 0.2, yy + 0.012, h_front + 0.08, "M_ShedRoof")
    # Marco de la puerta y cantoneras.
    b.box(x0 - 0.03, door[0] - 0.08, 0.12, x0, door[0], 2.08, "M_ShedTrim")
    b.box(x0 - 0.03, door[1], 0.12, x0, door[1] + 0.08, 2.08, "M_ShedTrim")
    b.box(x0 - 0.03, door[0] - 0.08, 2.0, x0, door[1] + 0.08, 2.08, "M_ShedTrim")
    for (cx, cy) in ((x0, y0), (x1, y0), (x0, y1), (x1, y1)):
        b.box(cx - 0.05, cy - 0.05, 0.12, cx + 0.05, cy + 0.05, h_back if cx == x1 else h_front, "M_ShedTrim")
    # Ventanuco en el sur.
    b.box(15.2, y0 - 0.02, 1.4, 16.0, y0, 1.9, "M_GlassDirty")
    return b.object()


def mailbox():
    b = G.Builder("Mailbox")
    b.box(-0.045, -0.045, 0.0, 0.045, 0.045, 1.0, "M_Fence")
    b.box(-0.12, -0.25, 1.0, 0.12, 0.2, 1.03, "M_Fence")
    # Buzón metálico con techo redondeado (aproximado con segmentos).
    w, l, h = 0.17, 0.48, 0.16
    b.box(-w / 2, -0.26, 1.03, w / 2, -0.26 + l, 1.03 + h, "M_Mailbox", faces="nsewb")
    segs = 8
    for k in range(segs):
        a0 = math.pi * k / segs
        a1 = math.pi * (k + 1) / segs
        xa, za = math.cos(a0) * w / 2, math.sin(a0) * w / 2
        xb, zb = math.cos(a1) * w / 2, math.sin(a1) * w / 2
        quad = [(xa, -0.26, 1.03 + h + za), (xb, -0.26, 1.03 + h + zb), (xb, -0.26 + l, 1.03 + h + zb), (xa, -0.26 + l, 1.03 + h + za)]
        f = b.face(quad, "M_Mailbox")
        if f.normal.z < -0.01 or (abs(f.normal.z) < 0.01 and f.normal.x * (xa + xb) < 0):
            f.normal_flip()
        b.face([(xa, -0.26, 1.03 + h + za), (0, -0.26, 1.03 + h), (xb, -0.26, 1.03 + h + zb)], "M_Mailbox")
        b.face([(xa, -0.26 + l, 1.03 + h + za), (xb, -0.26 + l, 1.03 + h + zb), (0, -0.26 + l, 1.03 + h)], "M_Mailbox")
    b.box(w / 2, -0.05, 1.12, w / 2 + 0.01, 0.0, 1.32, "M_MailFlag")
    b.box(w / 2, 0.0, 1.27, w / 2 + 0.01, 0.07, 1.32, "M_MailFlag")
    return b.object()


def street_light():
    """Brazo y luminaria tipo cobra para el poste de madera (la luz real se pone en Unity)."""
    b = G.Builder("StreetLight")
    b.cylinder(0, 0, 0.0, 8.2, 0.14, "M_PoleWood", 12, r_top=0.11)
    b.box(-0.04, -0.04, 7.0, 0.04, 1.75, 7.08, "M_MetalGrey")
    b.box(-0.03, 1.6, 6.95, 0.03, 1.8, 7.05, "M_MetalGrey")
    # Cabeza de la luminaria.
    b.box(-0.17, 1.75, 6.88, 0.17, 2.45, 7.06, "M_MetalGrey")
    b.box(-0.13, 1.82, 6.86, 0.13, 2.38, 6.88, "M_LampLens")
    # Crucetas y aisladores en lo alto (sin cables: los añade Unity entre postes).
    b.box(-1.0, -0.06, 7.7, 1.0, 0.06, 7.82, "M_PoleWood")
    for xi in (-0.85, -0.3, 0.3, 0.85):
        b.cylinder(xi, 0, 7.82, 7.95, 0.035, "M_Insulator", 8)
    return b.object()


def neighbors():
    houses = []
    # Vecino oeste (más grande, revestimiento gris) y vecino este (estuco claro). Algunas ventanas con luz.
    houses.append(house("Neighbor_W", -16.5, 7.0, 11.0, 11.0, {
        "s": [(-14.2, -12.4, 1.35, 2.65, "win"), (-10.9, -10.0, FLOOR, FLOOR + 2.1, "door"), (-8.6, -7.0, 1.35, 2.65, "win")],
        "e": [(9.0, 10.3, 1.35, 2.65, "win"), (13.0, 14.3, 1.35, 2.65, "win")],
    }, siding="M_SidingNeighborA", lit_rooms={("s", -8.6), ("e", 13.0)}))
    houses.append(house("Neighbor_E", 25.0, 6.5, 11.5, 11.5, {
        "s": [(26.4, 28.0, 1.35, 2.65, "win"), (29.4, 30.3, FLOOR, FLOOR + 2.1, "door"), (32.2, 34.0, 1.35, 2.65, "win")],
        "w": [(8.5, 9.8, 1.35, 2.65, "win"), (12.5, 13.8, 1.35, 2.65, "win")],
    }, chimney=(33.0, 13.4, 33.7, 14.1), siding="M_SidingNeighborB", lit_rooms={("s", 26.4), ("w", 12.5)}))
    return houses


def main():
    G.clear_scene()
    elena = house("House_Elena", 4.5, 6.0, 10.0, 12.0, {
        "s": [(6.1, 7.0, FLOOR, FLOOR + 2.1, "door"), (8.35, 9.65, 1.35, 2.65, "win"), (10.7, 12.0, 1.35, 2.65, "win"), (12.7, 14.0, 1.35, 2.65, "win")],
        "w": [(7.6, 8.8, 1.35, 2.65, "win"), (11.8, 13.0, 1.35, 2.65, "win"), (15.6, 16.8, 1.35, 2.65, "win")],
        "e": [(7.6, 8.9, 1.35, 2.65, "win"), (12.2, 13.4, 1.5, 2.6, "win"), (16.2, 16.9, 1.9, 2.5, "frosted")],
        "n": [(5.9, 7.1, 1.35, 2.65, "win"), (10.4, 11.1, 1.9, 2.5, "frosted"), (12.7, 13.5, 0.27, 2.37, "service")],
    }, porch={"x0": 4.95, "x1": 8.15, "y0": 4.35, "door_x": 6.55, "step_w": 1.5}, chimney=(5.5, 13.2, 6.2, 13.9),
        extras=lambda b: (porch_lantern(b, 7.33, 6.0, FLOOR + 1.62),
                          b.box(12.35, 18.0, 0.0, 13.85, 18.65, 0.15, "M_PorchConcrete", faces="nsewt")))
    front_door = door_leaf("Door_Front_Leaf", 0.86, 2.06, 0.045)
    service_door = door_leaf("Door_Service_Leaf", 0.8, 2.0, 0.04, style="boards", mat="M_DoorServicePaint")
    shed_door = door_leaf("Door_Shed_Leaf", 0.88, 1.86, 0.035, style="boards", mat="M_ShedWall")
    gate = gate_leaf(1.16, 1.75)
    objs = [elena, site(), fences(), shed(), mailbox(), street_light()] + neighbors()
    G.export_fbx(objs, os.path.join(OUT, "SM_Exterior.fbx"))
    for leaf in (front_door, service_door, shed_door, gate):
        G.export_fbx([leaf], os.path.join(OUT, f"SM_{leaf.name}.fbx"))


main()
