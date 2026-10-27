"""Kit modular del castillo inicial de RELICBOUND (Frontera Verde, nivel 1).

Uso:
  blender -b --factory-startup --python Tools/Blender/rb_castle_kit.py -- <texturas> <salida_fbx> [preview.png]

Escala (docs/02): 1 unidad = 1 m. Muralla 5 m de alto y 2 m de grosor, secciones de 2 m.
Exterior hacia -Y. Pivote de cada pieza en el centro de su huella, a nivel del suelo.
"""
import math
import os
import sys

import bpy

sys.path.append(os.path.dirname(__file__))
import rb_castle_kit_materials as K  # noqa: E402
import rb_geo as G  # noqa: E402

WALL_HEIGHT = 4.2          # altura del adarve
WALL_THICKNESS = 2.0
SECTION_LENGTH = 2.0
PARAPET_HEIGHT = 0.8
MERLON_HEIGHT = 0.8
MERLON_WIDTH = 0.6

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
TEXTURES = os.path.abspath(args[0]) if args else os.path.abspath("Assets/Relicbound/Art/Textures")
OUT_DIR = os.path.abspath(args[1]) if len(args) > 1 else os.path.abspath("Assets/Relicbound/Art/Castle")
PREVIEW = os.path.abspath(args[2]) if len(args) > 2 else None


# ---------------------------------------------------------------- materiales

def textured_material(name, family, roughness=0.8, metallic=0.0):
    return K.textured_material(name, family, TEXTURES, roughness, metallic)


def build_materials():
    return {
        "stone": textured_material("M_StoneWall", "StoneWall", 0.85),
        "roof": textured_material("M_RoofBlue", "RoofBlue", 0.6),
        "wood": textured_material("M_Wood", "Wood", 0.75),
        "iron": textured_material("M_Iron", "Iron", 0.5, 0.7),
    }


# ---------------------------------------------------------------- elementos comunes

def merlons_along(name, start, end, z, outer_y_offset, mat, depth=0.45, skip_ends=False):
    """Almenas a lo largo de un segmento en X (y fija). Devuelve lista de objetos."""
    length = end - start
    count = max(1, int(round(length / 1.0)))
    step = length / count
    parts = []
    for i in range(count):
        if skip_ends and (i == 0 or i == count - 1):
            continue
        cx = start + step * (i + 0.5)
        parts.append(G.box(f"{name}_m{i}", (MERLON_WIDTH, depth, MERLON_HEIGHT), (cx, outer_y_offset, z), mat,
                           bevel=0.05))
    return parts


def corbels_along(name, start, end, z, y, mat, spacing=0.5):
    parts = []
    count = int(round((end - start) / spacing))
    for i in range(count):
        cx = start + spacing * (i + 0.5)
        parts.append(G.box(f"{name}_c{i}", (0.22, 0.3, 0.35), (cx, y, z), mat, bevel=0.03, taper=-0.04))
    return parts


def oriented(obj, rotation_z):
    obj.rotation_euler.z = rotation_z
    return obj


# ---------------------------------------------------------------- piezas

def build_wall_straight(m):
    """Sección recta de muralla de 2 m. Exterior en -Y."""
    half_t = WALL_THICKNESS / 2
    parts = [
        G.box("plinth", (SECTION_LENGTH, WALL_THICKNESS + 0.5, 0.9), (0, 0, 0), m["stone"], bevel=0.06, taper=0.12),
        G.box("body", (SECTION_LENGTH, WALL_THICKNESS, WALL_HEIGHT), (0, 0, 0), m["stone"], bevel=0.0),
        # Parapeto exterior volado sobre ménsulas.
        G.box("parapet", (SECTION_LENGTH, 0.5, PARAPET_HEIGHT), (0, -half_t - 0.05, WALL_HEIGHT), m["stone"],
              bevel=0.04),
        G.box("parapet_lip", (SECTION_LENGTH, 0.62, 0.14), (0, -half_t - 0.05, WALL_HEIGHT - 0.12), m["stone"],
              bevel=0.03),
        # Pretil interior bajo.
        G.box("inner_rail", (SECTION_LENGTH, 0.3, 0.45), (0, half_t - 0.15, WALL_HEIGHT), m["stone"], bevel=0.04),
    ]
    parts += corbels_along("corbel", -SECTION_LENGTH / 2, SECTION_LENGTH / 2, WALL_HEIGHT - 0.45, -half_t - 0.12,
                           m["stone"])
    parts += merlons_along("merlon", -SECTION_LENGTH / 2, SECTION_LENGTH / 2, WALL_HEIGHT + PARAPET_HEIGHT,
                           -half_t - 0.05, m["stone"], depth=0.5)
    for obj in parts:
        G.world_box_uv(obj)
    return G.join(parts, "SM_Wall_Straight")


def round_tower_parts(m, radius, body_height, roof_height, center=(0.0, 0.0), prefix="tower", slits=True):
    cx, cy = center
    parts = []
    plinth = G.cylinder(f"{prefix}_plinth", radius + 0.3, 1.0, (cx, cy, 0), m["stone"], segments=32,
                        top_radius=radius + 0.05, bevel=0.0)
    body = G.cylinder(f"{prefix}_body", radius, body_height, (cx, cy, 0), m["stone"], segments=32,
                      top_radius=radius - 0.12, bevel=0.0)
    if slits:
        for i, angle_deg in enumerate((-90, -30, -150, 90)):
            angle = math.radians(angle_deg)
            for j, z in enumerate((2.6, body_height - 2.6)):
                slit = G.box(f"{prefix}_slit{i}{j}", (0.18, 1.6, 1.1),
                             (cx + math.cos(angle) * radius, cy + math.sin(angle) * radius, z), None, bevel=0.0)
                slit.rotation_euler.z = angle + math.pi / 2
                bpy.context.view_layer.update()
                G.cut(body, slit)
    top_r = radius - 0.12
    # Ménsulas y cadalso de piedra bajo el alero.
    for i in range(20):
        angle = 2 * math.pi * i / 20
        corbel = G.box(f"{prefix}_corbel{i}", (0.25, 0.35, 0.45),
                       (cx + math.cos(angle) * (top_r + 0.1), cy + math.sin(angle) * (top_r + 0.1), body_height - 0.45),
                       m["stone"], bevel=0.03, taper=-0.05)
        corbel.rotation_euler.z = angle + math.pi / 2
        parts.append(corbel)
    band = G.cylinder(f"{prefix}_band", top_r + 0.3, 1.0, (cx, cy, body_height), m["stone"], segments=32,
                      bevel=0.04)
    eave_z = body_height + 1.0
    roof_r = top_r + 0.75
    roof = G.cone(f"{prefix}_roof", roof_r, roof_height, (cx, cy, eave_z - 0.15), m["roof"], segments=32)
    finial = G.cylinder(f"{prefix}_finial", 0.06, 1.4, (cx, cy, eave_z - 0.15 + roof_height - 0.3), m["iron"],
                        segments=8, bevel=0.0)
    knob = G.cylinder(f"{prefix}_knob", 0.14, 0.22, (cx, cy, eave_z - 0.15 + roof_height + 0.3), m["iron"],
                      segments=8, bevel=0.02)
    for obj in (plinth, body, band):
        G.radial_uv(obj, center=center)
    G.cone_uv(roof, roof_height, roof_r, eave_z - 0.15, center=center)
    for obj in parts + [finial, knob]:
        G.world_box_uv(obj)
    return parts + [plinth, body, band, roof, finial, knob]


def build_tower_round(m):
    return G.join(round_tower_parts(m, 2.5, 8.0, 5.6), "SM_Tower_Round")


def arch_cutter(name, width, depth, spring_height, location):
    rect = G.box(f"{name}_rect", (width, depth, spring_height), location, None, bevel=0.0)
    arch = G.cylinder(f"{name}_arch", width / 2, depth, (location[0], location[1] - depth / 2,
                                                         location[2] + spring_height), None, segments=24, bevel=0.0)
    arch.rotation_euler.x = -math.pi / 2
    bpy.context.view_layer.update()
    # Unión booleana para obtener un único sólido cerrado: el solver exacto falla con cortadores solapados.
    union = rect.modifiers.new("Union", "BOOLEAN")
    union.operation = "UNION"
    union.solver = "EXACT"
    union.object = arch
    bpy.context.view_layer.objects.active = rect
    bpy.ops.object.modifier_apply(modifier=union.name)
    bpy.data.objects.remove(arch, do_unlink=True)
    rect.name = name
    return rect


def build_gatehouse(m):
    """Caseta de puerta: bloque central de 4,4 m, dos torretas flanqueantes y paso abovedado."""
    depth = 6.0
    height = 7.0
    parts = []
    block = G.box("gate_block", (4.6, depth, height), (0, 0, 0), m["stone"], bevel=0.0)
    passage = arch_cutter("gate_passage", 2.8, depth + 2, 2.6, (0, 0, -0.1))
    G.cut(block, passage)
    parts.append(block)
    parts.append(G.box("gate_plinth_l", (1.0, depth, 0.9), (-1.9, 0, 0), m["stone"], bevel=0.05, taper=0.08))
    parts.append(G.box("gate_plinth_r", (1.0, depth, 0.9), (1.9, 0, 0), m["stone"], bevel=0.05, taper=0.08))

    # Moldura del arco (dovelas sugeridas por un anillo saliente).
    ring = G.cylinder("gate_arch_ring", 1.75, 0.3, (0, -depth / 2 - 0.15, 2.5), m["stone"], segments=24, bevel=0.03)
    ring.rotation_euler.x = -math.pi / 2
    bpy.context.view_layer.update()
    ring_cut = arch_cutter("gate_ring_cut", 2.8, 2.0, 2.6, (0, -depth / 2 + 0.8, -0.1))
    G.cut(ring, ring_cut)
    lower = G.box("gate_ring_lower", (6, 2, 2.5), (0, -depth / 2, -0.05), None, bevel=0.0)
    G.cut(ring, lower)
    parts.append(ring)

    # Portones de madera dentro del paso, con herrajes.
    doors = G.box("gate_doors", (2.8, 0.18, 4.0), (0, -0.6, 0), m["wood"], bevel=0.0)
    door_shape = arch_cutter("gate_door_shape", 2.76, 0.4, 2.6, (0, -0.4, -0.05))
    keep = doors.modifiers.new("Shape", "BOOLEAN")
    keep.operation = "INTERSECT"
    keep.solver = "EXACT"
    keep.object = door_shape
    bpy.context.view_layer.objects.active = doors
    bpy.ops.object.modifier_apply(modifier=keep.name)
    bpy.data.objects.remove(door_shape, do_unlink=True)
    parts.append(doors)
    for z in (0.6, 1.6, 2.6):
        parts.append(G.box(f"gate_band{z}", (2.7, 0.08, 0.12), (0, -0.72, z), m["iron"], bevel=0.01))
    parts.append(G.box("gate_split", (0.06, 0.06, 3.9), (0, -0.72, 0.0), m["iron"], bevel=0.0))

    # Rastrillo levantado: sólo asoma la parte baja bajo el arco.
    for i in range(7):
        x = -1.2 + i * 0.4
        parts.append(G.box(f"portcullis_v{i}", (0.08, 0.08, 1.4), (x, -depth / 2 + 0.9, 2.75), m["iron"], bevel=0.0))
    for z in (2.85, 3.5):
        parts.append(G.box(f"portcullis_h{z}", (2.7, 0.08, 0.08), (0, -depth / 2 + 0.9, z), m["iron"], bevel=0.0))

    # Almenas del bloque central (frente y trasera).
    parts.append(G.box("gate_parapet_f", (4.6, 0.5, 0.8), (0, -depth / 2 - 0.05, height), m["stone"], bevel=0.04))
    parts.append(G.box("gate_parapet_b", (4.6, 0.5, 0.8), (0, depth / 2 - 0.25, height), m["stone"], bevel=0.04))
    parts += merlons_along("gate_mf", -2.3, 2.3, height + 0.8, -depth / 2 - 0.05, m["stone"], depth=0.5)
    parts += merlons_along("gate_mb", -2.3, 2.3, height + 0.8, depth / 2 - 0.25, m["stone"], depth=0.5)
    parts += corbels_along("gate_cf", -2.3, 2.3, height - 0.45, -depth / 2 - 0.12, m["stone"])

    for obj in parts:
        G.world_box_uv(obj)
    for side in (-1, 1):
        parts += round_tower_parts(m, 1.9, 8.2, 4.4, center=(side * 3.2, -1.4), prefix=f"gate_t{side}")
    return G.join(parts, "SM_Gatehouse")


def build_keep(m):
    """Torreón (castillo central nivel 1): 9 x 9 m, 12 m de cuerpo, garitas en las esquinas y tejado a cuatro aguas."""
    size = 9.0
    height = 12.0
    half = size / 2
    parts = []
    plinth = G.box("keep_plinth", (size + 0.8, size + 0.8, 1.4), (0, 0, 0), m["stone"], bevel=0.06, taper=0.3)
    body = G.box("keep_body", (size, size, height), (0, 0, 0), m["stone"], bevel=0.0, taper=0.15)
    # Ventanas en arco en tres alturas y puerta elevada al frente.
    for face in range(4):
        angle = face * math.pi / 2
        for i, (offset, z) in enumerate(((-2.2, 5.0), (2.2, 5.0), (0.0, 8.6))):
            nx, ny = math.sin(angle), -math.cos(angle)
            tx, ty = math.cos(angle), math.sin(angle)
            window = arch_cutter(f"keep_w{face}{i}", 0.7, 1.2, 1.0,
                                 (nx * half + tx * offset, ny * half + ty * offset, z))
            window.rotation_euler.z = angle
            bpy.context.view_layer.update()
            G.cut(body, window)
    door = arch_cutter("keep_door", 1.6, 1.4, 1.8, (0, -half + 0.2, 1.4))
    G.cut(body, door)
    parts += [plinth, body]
    parts.append(G.box("keep_door_wood", (1.6, 0.12, 2.6), (0, -half + 0.45, 1.4), m["wood"], bevel=0.0))
    # Escalinata de madera a la puerta elevada.
    for i in range(5):
        parts.append(G.box(f"keep_step{i}", (2.2, 0.45, 0.28 * (i + 1)), (0, -half - 2.2 + i * 0.45, 0), m["wood"],
                           bevel=0.02))

    # Cornisa, parapeto y almenas.
    top = height
    parts.append(G.box("keep_cornice", (size + 0.2, size + 0.2, 0.3), (0, 0, top - 0.3), m["stone"], bevel=0.05))
    for face in range(4):
        angle = face * math.pi / 2
        for obj in ([G.box(f"keep_parapet{face}", (size + 0.2, 0.5, 0.8), (0, -half - 0.05, top), m["stone"],
                           bevel=0.04)]
                    + merlons_along(f"keep_m{face}", -half + 1.0, half - 1.0, top + 0.8, -half - 0.05, m["stone"],
                                    depth=0.5)
                    + corbels_along(f"keep_c{face}", -half, half, top - 0.75, -half - 0.12, m["stone"])):
            obj.location = (obj.location.x * math.cos(angle) - obj.location.y * math.sin(angle),
                            obj.location.x * math.sin(angle) + obj.location.y * math.cos(angle), obj.location.z)
            obj.rotation_euler.z = angle
            parts.append(obj)
    bpy.context.view_layer.update()
    for obj in parts:
        G.world_box_uv(obj)

    roof_size = size - 1.4
    roof = G.pyramid("keep_roof", roof_size + 0.6, roof_size + 0.6, 6.0, (0, 0, top + 0.1), m["roof"])
    G.world_box_uv(roof)
    parts.append(roof)
    parts.append(G.cylinder("keep_finial", 0.07, 1.8, (0, 0, top + 5.6), m["iron"], segments=8, bevel=0.0))

    # Garitas en las esquinas, sobre ménsulas cónicas.
    for sx in (-1, 1):
        for sy in (-1, 1):
            c = (sx * (half - 0.15), sy * (half - 0.15))
            corbel = G.cylinder(f"keep_bartizan_base{sx}{sy}", 0.25, 1.2, (c[0], c[1], top - 1.6), m["stone"],
                                segments=16, top_radius=1.05, bevel=0.0)
            turret = G.cylinder(f"keep_bartizan{sx}{sy}", 1.05, 2.4, (c[0], c[1], top - 0.4), m["stone"],
                                segments=16, bevel=0.03)
            cap = G.cone(f"keep_bartizan_roof{sx}{sy}", 1.35, 2.6, (c[0], c[1], top + 1.95), m["roof"], segments=16)
            for obj in (corbel, turret):
                G.radial_uv(obj, center=c)
            G.cone_uv(cap, 2.6, 1.35, top + 1.95, center=c)
            parts += [corbel, turret, cap]
    return G.join(parts, "SM_Keep_L1")


# ---------------------------------------------------------------- exportación y vista previa

def export_piece(obj):
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    location = obj.location.copy()
    obj.location = (0, 0, 0)
    # Triangular para que el FBX lleve tangentes (normal maps correctos en Unity).
    triangulate = obj.modifiers.new("Triangulate", "TRIANGULATE")
    triangulate.keep_custom_normals = True
    path = os.path.join(OUT_DIR, f"{obj.name}.fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE", use_tspace=True,
                             path_mode="STRIP", bake_space_transform=True, add_leaf_bones=False,
                             use_mesh_modifiers=True)
    obj.modifiers.remove(triangulate)
    obj.location = location
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    print(f"[rb] export {obj.name}: {tris} tris -> {path}")


def main():
    G.clear_scene()
    m = build_materials()
    pieces = {
        "wall": build_wall_straight(m),
        "tower": build_tower_round(m),
        "gate": build_gatehouse(m),
        "keep": build_keep(m),
    }
    for obj in pieces.values():
        G.set_origin_to_ground(obj)
        bpy.ops.object.shade_auto_smooth(angle=math.radians(35))
        export_piece(obj)
    if PREVIEW:
        import rb_preview
        rb_preview.render_layout(pieces, m, PREVIEW)


main()
