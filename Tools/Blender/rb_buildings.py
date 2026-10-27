"""Edificios de la fortaleza (P3): taller de asedio, almacén, aserradero y cantera, en 3 niveles.

Uso: blender -b --factory-startup --python Tools/Blender/rb_buildings.py -- <texturas> <salida_fbx> [preview.png]
Estilo: entramado de madera sobre zócalo de piedra, revoco de cal, tejado de tejas del color del jugador.
Cada pieza cabe en su huella de catálogo (Assets/Relicbound/Resources/catalog.json), con pivote en el centro.
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Vector

sys.path.append(os.path.dirname(__file__))
import rb_castle_kit_materials as K  # noqa: E402
import rb_geo as G  # noqa: E402

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
TEXTURES = os.path.abspath(args[0]) if args else os.path.abspath("Assets/Relicbound/Art/Textures")
OUT_DIR = os.path.abspath(args[1]) if len(args) > 1 else os.path.abspath("Assets/Relicbound/Art/Buildings")
PREVIEW = os.path.abspath(args[2]) if len(args) > 2 else None


def materials():
    return {
        "stone": K.textured_material("M_StoneWall", "StoneWall", TEXTURES, 0.85),
        "plaster": K.textured_material("M_Plaster", "Plaster", TEXTURES, 0.9),
        "wood": K.textured_material("M_Wood", "Wood", TEXTURES, 0.75),
        "wood_dark": K.textured_material("M_WoodDark", "Wood", TEXTURES, 0.8, tint="#8C7A68"),
        "roof": K.textured_material("M_RoofBlue", "RoofBlue", TEXTURES, 0.6),
        "iron": K.textured_material("M_Iron", "Iron", TEXTURES, 0.5, 0.7),
        "hay": K.textured_material("M_Hay", "Hay", TEXTURES, 0.9),
        "rock": K.textured_material("M_RockOutcrop", "StoneWall", TEXTURES, 0.9, tint="#8A857C"),
    }


def beam(name, start, end, size, mat):
    direction = Vector(end) - Vector(start)
    obj = G.box(name, (size, size, direction.length), (0, 0, 0), mat, bevel=0.015)
    obj.location = Vector(start)
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    bpy.context.view_layer.update()
    return obj


def prism(name, width, depth, height, location, mat):
    """Hastial: prisma triangular (sección en X-Z) a lo largo de Y."""
    bm = bmesh.new()
    hw, hd = width / 2, depth / 2
    front = [bm.verts.new((-hw, -hd, 0)), bm.verts.new((hw, -hd, 0)), bm.verts.new((0, -hd, height))]
    back = [bm.verts.new((-hw, hd, 0)), bm.verts.new((hw, hd, 0)), bm.verts.new((0, hd, height))]
    bm.faces.new(front)
    bm.faces.new(list(reversed(back)))
    for i in range(3):
        n = (i + 1) % 3
        bm.faces.new((front[i], back[i], back[n], front[n]))
    obj = G.mesh_object(name, bm, mat)
    obj.location = location
    return obj


def gable_roof(name, width, depth, height, top, m, overhang=0.4, thickness=0.16):
    """Faldones con grosor y alero, hastiales triangulares de revoco y cumbrera de madera."""
    half_w = width / 2 + overhang
    half_d = depth / 2
    slope = height / half_d
    eave_y = half_d + overhang
    eave_z = top - overhang * slope
    bm = bmesh.new()
    verts = [
        bm.verts.new((-half_w, -eave_y, eave_z)), bm.verts.new((half_w, -eave_y, eave_z)),
        bm.verts.new((half_w, 0, top + height)), bm.verts.new((-half_w, 0, top + height)),
        bm.verts.new((-half_w, eave_y, eave_z)), bm.verts.new((half_w, eave_y, eave_z)),
    ]
    bm.faces.new((verts[0], verts[1], verts[2], verts[3]))
    bm.faces.new((verts[3], verts[2], verts[5], verts[4]))
    roof = G.mesh_object(name, bm, m["roof"])
    solidify = roof.modifiers.new("Thickness", "SOLIDIFY")
    solidify.thickness = thickness
    solidify.offset = 1.0
    parts = [roof]
    bm = bmesh.new()
    hw = width / 2 - 0.06
    for x in (-hw, hw):
        tri = [bm.verts.new((x, -half_d + 0.06, top)), bm.verts.new((x, half_d - 0.06, top)), bm.verts.new((x, 0, top + height - 0.08))]
        if x < 0:
            bm.faces.new(tri)
        else:
            bm.faces.new(list(reversed(tri)))
    gable = G.mesh_object(f"{name}_gable", bm, m["plaster"])
    solidify = gable.modifiers.new("Thickness", "SOLIDIFY")
    solidify.thickness = 0.12
    parts.append(gable)
    parts.append(G.box(f"{name}_ridge", (width + overhang * 2 + 0.1, 0.24, 0.2), (0, 0, top + height + 0.02), m["wood_dark"], bevel=0.03))
    # Vigas de hastial (bargeboards) que rematan el borde del tejado.
    for x in (-half_w + 0.04, half_w - 0.04):
        for sy in (-1, 1):
            parts.append(beam(f"{name}_barge{x:.1f}{sy}", (x, sy * eave_y, eave_z + 0.14), (x, 0, top + height + 0.14), 0.14, m["wood_dark"]))
    return parts


def timber_hall(name, w, d, wall_h, roof_h, m, base_z=0.0, door_side=-1, windows=True, chimney=False, jetty=0.0):
    """Nave de entramado de w x d con tejado a dos aguas (cumbrera a lo largo de X)."""
    parts = []
    z0 = base_z
    if base_z == 0.0:
        parts.append(G.box(f"{name}_plinth", (w + 0.2, d + 0.2, 0.55), (0, 0, 0), m["stone"], bevel=0.04, taper=0.04))
        z0 = 0.55
    ww, dd = w + jetty * 2, d + jetty * 2
    parts.append(G.box(f"{name}_walls", (ww - 0.12, dd - 0.12, wall_h), (0, 0, z0), m["plaster"], bevel=0.0))
    post = 0.2
    hw, hd = ww / 2, dd / 2
    # Postes de esquina, carreras y soleras.
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(G.box(f"{name}_corner{sx}{sy}", (post, post, wall_h), (sx * (hw - post / 2), sy * (hd - post / 2), z0), m["wood_dark"], bevel=0.015))
    for z in (z0, z0 + wall_h * 0.52, z0 + wall_h - 0.16):
        for sy in (-1, 1):
            parts.append(G.box(f"{name}_rail_x{z:.1f}{sy}", (ww, 0.16, 0.16), (0, sy * (hd - 0.06), z), m["wood_dark"], bevel=0.015))
        for sx in (-1, 1):
            parts.append(G.box(f"{name}_rail_y{z:.1f}{sx}", (0.16, dd, 0.16), (sx * (hw - 0.06), 0, z), m["wood_dark"], bevel=0.015))
    # Montantes y tornapuntas por fachada.
    rng = random.Random(len(name))
    for sy in (-1, 1):
        count = max(2, int(ww / 1.3))
        for i in range(1, count):
            x = -hw + i * ww / count
            parts.append(G.box(f"{name}_stud{sy}{i}", (0.14, 0.14, wall_h), (x, sy * (hd - 0.05), z0), m["wood_dark"], bevel=0.01))
            if rng.random() < 0.45:
                x2 = x + ww / count * (1 if rng.random() < 0.5 else -1)
                parts.append(beam(f"{name}_brace{sy}{i}", (x, sy * (hd - 0.05), z0 + 0.1), (x2, sy * (hd - 0.05), z0 + wall_h * 0.5), 0.12, m["wood_dark"]))
    for sx in (-1, 1):
        count = max(2, int(dd / 1.3))
        for i in range(1, count):
            y = -hd + i * dd / count
            parts.append(G.box(f"{name}_studs{sx}{i}", (0.14, 0.14, wall_h), (sx * (hw - 0.05), y, z0), m["wood_dark"], bevel=0.01))
    # Puerta y ventanas con contraventanas.
    door_y = door_side * (hd + 0.02)
    if base_z == 0.0:
        parts.append(G.box(f"{name}_door", (1.1, 0.12, 1.9), (0, door_y, z0), m["wood"], bevel=0.02))
        parts.append(G.box(f"{name}_door_frame", (1.35, 0.1, 0.16), (0, door_y, z0 + 1.9), m["wood_dark"], bevel=0.01))
    if windows:
        for sx in (-1, 1):
            for sy in (-1, 1):
                x = sx * ww * 0.28
                y = sy * (hd + 0.03)
                parts.append(G.box(f"{name}_win{sx}{sy}", (0.6, 0.08, 0.7), (x, y, z0 + wall_h * 0.6), m["iron"], bevel=0.0))
                for side in (-1, 1):
                    parts.append(G.box(f"{name}_shutter{sx}{sy}{side}", (0.3, 0.06, 0.72), (x + side * 0.46, y + sy * 0.02, z0 + wall_h * 0.6 - 0.01), m["wood"], bevel=0.01))
    # Tejado a dos aguas con alero, hastiales y cumbrera (cumbrera a lo largo de X).
    top = z0 + wall_h
    parts += gable_roof(f"{name}_roof", ww, dd, roof_h, top, m)
    if chimney:
        parts.append(G.box(f"{name}_chimney", (0.6, 0.6, roof_h + 1.0), (ww * 0.28, hd * 0.35, top), m["stone"], bevel=0.04))
    return parts


def log_pile(name, location, count, m, length=2.2):
    parts = []
    rows = [count, max(0, count - 1), max(0, count - 3)]
    for r, n in enumerate(rows):
        for i in range(n):
            log = G.cylinder(f"{name}_log{r}{i}", 0.18, length, (0, 0, 0), m["wood"], segments=10, bevel=0.0)
            log.rotation_euler.y = math.pi / 2
            log.location = (location[0] - length / 2, location[1] + (i - n / 2 + 0.5) * 0.37, location[2] + 0.18 + r * 0.32)
            bpy.context.view_layer.update()
            parts.append(log)
    return parts


def block_stack(name, location, count, m):
    parts = []
    rng = random.Random(count)
    for i in range(count):
        layer = i // 4
        slot = i % 4
        parts.append(G.box(f"{name}_blk{i}", (0.9, 0.55, 0.45), (location[0] + (slot % 2) * 0.95 - 0.45, location[1] + (slot // 2) * 0.6 - 0.3, location[2] + layer * 0.46), m["stone"], bevel=0.05))
        parts[-1].rotation_euler.z = rng.uniform(-0.08, 0.08)
    return parts


def crane(name, location, height, m):
    """Grúa de pórtico en A con poleas de hierro y cuerda."""
    x, y, z = location
    parts = [
        beam(f"{name}_leg1", (x - 0.9, y, z), (x, y, z + height), 0.18, m["wood_dark"]),
        beam(f"{name}_leg2", (x + 0.9, y, z), (x, y, z + height), 0.18, m["wood_dark"]),
        beam(f"{name}_arm", (x, y, z + height - 0.2), (x, y - 2.0, z + height - 0.6), 0.16, m["wood_dark"]),
        G.cylinder(f"{name}_pulley", 0.16, 0.12, (x, y - 2.0, z + height - 0.8), m["iron"], segments=10, bevel=0.0),
        beam(f"{name}_rope", (x, y - 2.0, z + height - 0.85), (x, y - 2.0, z + 1.2), 0.03, m["wood"]),
    ]
    parts[3].rotation_euler.x = math.pi / 2
    return parts


# ---------------------------------------------------------------- edificios

def workshop(m, level):
    """Taller de asedio (6 x 6): nave, patio con grúa y piezas de máquinas; crece con el nivel."""
    parts = timber_hall("ws", 4.6, 3.2, 2.6 + (level - 1) * 0.6, 1.8, m, door_side=-1, chimney=level >= 2)
    for obj in parts:
        obj.location.y += 1.1
    bpy.context.view_layer.update()
    parts += crane("ws_crane", (-1.5, -1.3, 0), 3.6 + level * 0.4, m)
    wheel = G.cylinder("ws_wheel", 0.6, 0.14, (0.9, -2.5, 0.6), m["wood_dark"], segments=16, bevel=0.02)
    wheel.rotation_euler.x = math.pi / 2
    parts.append(wheel)
    parts += log_pile("ws_logs", (2.2, -1.2, 0), 2 + level, m, length=1.6)
    if level >= 3:
        parts.append(beam("ws_arm_frame", (-0.4, -2.2, 0.2), (0.6, -2.4, 1.8), 0.2, m["wood_dark"]))
    return parts


def storehouse(m, level):
    """Almacén (5 x 5): granero con portón; nivel 2 más alto, nivel 3 con anexo y montacargas."""
    parts = timber_hall("st", 4.4, 3.6 if level < 3 else 3.0, 2.8 + (level - 1) * 0.7, 2.0, m, door_side=-1)
    parts.append(G.box("st_gate", (1.8, 0.1, 2.2), (0, -1.86 if level < 3 else -1.56, 0.55), m["wood"], bevel=0.02))
    parts.append(G.box("st_crate1", (0.8, 0.8, 0.8), (1.7, -2.15, 0), m["wood"], bevel=0.03))
    parts.append(G.box("st_crate2", (0.7, 0.7, 0.7), (-1.8, -2.1, 0), m["wood"], bevel=0.03))
    if level >= 3:
        loft = G.box("st_loft_door", (0.8, 0.1, 0.9), (0, -1.56, 3.6), m["wood"], bevel=0.02)
        parts.append(loft)
        parts.append(beam("st_hoist", (0, -1.6, 5.0), (0, -2.3, 5.0), 0.16, m["wood_dark"]))
    return parts


def sawmill(m, level):
    """Aserradero (5 x 5): cobertizo abierto con banco de sierra y pilas de troncos y tablones."""
    parts = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(G.box(f"sm_post{sx}{sy}", (0.2, 0.2, 2.6), (sx * 1.9, sy * 1.2, 0), m["wood_dark"], bevel=0.015))
    parts += gable_roof("sm_roof", 4.0, 2.6, 1.2, 2.6, m)
    parts.append(G.box("sm_bench", (3.0, 0.7, 0.85), (0, 0, 0), m["wood_dark"], bevel=0.02))
    log = G.cylinder("sm_bench_log", 0.28, 2.8, (0, 0, 1.13), m["wood"], segments=12, bevel=0.0)
    log.rotation_euler.y = math.pi / 2
    parts.append(log)
    parts.append(G.box("sm_saw", (0.06, 0.9, 1.4), (0.2, 0, 0.85), m["iron"], bevel=0.0))
    parts += log_pile("sm_logs", (0.2, -2.0, 0), 3 + level, m, length=2.4)
    for i in range(level + 1):
        parts.append(G.box(f"sm_planks{i}", (2.0, 0.6, 0.12), (0, 2.0, i * 0.13), m["wood"], bevel=0.01))
    return parts


def quarry(m, level):
    """Cantera (5 x 5): afloramiento rocoso, sillares cortados apilados y grúa de rueda."""
    import rb_nature_shapes as N
    parts = []
    rock = N.blob("qr_outcrop", 1.7, (-1.0, 0.9, 0.2), 5, m["rock"], roughness=0.3, subdivisions=2, squash=0.55)
    parts.append(rock)
    parts += block_stack("qr_blocks", (1.2, -1.2, 0), 4 + level * 2, m)
    parts += crane("qr_crane", (1.4, 1.2, 0), 3.4 + level * 0.3, m)
    wheel = G.cylinder("qr_treadwheel", 1.1, 0.5, (-1.6, -1.6, 1.15), m["wood_dark"], segments=18, bevel=0.02)
    wheel.rotation_euler.x = math.pi / 2
    parts.append(wheel)
    parts.append(G.box("qr_cart", (1.2, 0.8, 0.45), (0.2, -0.2, 0.25), m["wood"], bevel=0.03))
    return parts


def export(obj):
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    location = obj.location.copy()
    obj.location = (0, 0, 0)
    triangulate = obj.modifiers.new("Triangulate", "TRIANGULATE")
    triangulate.keep_custom_normals = True
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT_DIR, f"{obj.name}.fbx"), use_selection=True,
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z",
                             axis_up="Y", mesh_smooth_type="FACE", use_tspace=True, path_mode="STRIP",
                             bake_space_transform=True, add_leaf_bones=False, use_mesh_modifiers=True)
    obj.modifiers.remove(triangulate)
    obj.location = location
    print(f"[rb] export {obj.name}: {sum(len(p.vertices) - 2 for p in obj.data.polygons)} tris")


def main():
    G.clear_scene()
    m = materials()
    exported = {}
    for level in (1, 2, 3):
        for key, builder in (("Workshop", workshop), ("Storehouse", storehouse), ("Sawmill", sawmill), ("Quarry", quarry)):
            parts = builder(m, level)
            for obj in parts:
                G.world_box_uv(obj)
            obj = G.join(parts, f"SM_{key}_L{level}")
            G.set_origin_to_ground(obj)
            bpy.ops.object.shade_auto_smooth(angle=math.radians(35))
            export(obj)
            obj.location = (level * 1000, 0, 0)
            exported[(key, level)] = obj
    if PREVIEW:
        import rb_preview as P
        P._ground()
        P._lighting()
        columns = {"Workshop": -12, "Storehouse": -4, "Sawmill": 4, "Quarry": 12}
        for (key, level), obj in exported.items():
            obj.location = (columns[key], (level - 2) * 8, 0)
        P._camera((0, 0, 1.5), 48, 38, -18, lens=45)
        P._render(PREVIEW, 1600, 900, 32)


main()
