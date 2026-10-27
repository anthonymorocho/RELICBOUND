"""Objetos de patio de RELICBOUND: dan vida a la fortaleza sin ocupar celdas de construcción.

Uso: blender -b --factory-startup --python Tools/Blender/rb_props.py -- <texturas> <salida_fbx> [preview.png]
"""
import math
import os
import sys

import bpy

sys.path.append(os.path.dirname(__file__))
import rb_geo as G  # noqa: E402

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
TEXTURES = os.path.abspath(args[0]) if args else os.path.abspath("Assets/Relicbound/Art/Textures")
OUT_DIR = os.path.abspath(args[1]) if len(args) > 1 else os.path.abspath("Assets/Relicbound/Art/Props")
PREVIEW = os.path.abspath(args[2]) if len(args) > 2 else None


def textured(name, family, roughness=0.8, metallic=0.0):
    import rb_castle_kit_materials as K
    return K.textured_material(name, family, TEXTURES, roughness, metallic)


def flat(name, color, roughness=0.85):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes["Principled BSDF"]
    value = color.lstrip("#")
    srgb = [int(value[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    bsdf.inputs["Base Color"].default_value = (*[c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
                                                 for c in srgb], 1)
    bsdf.inputs["Roughness"].default_value = roughness
    return material


def barrel(m):
    parts = []
    staves = G.cylinder("barrel_body", 0.42, 1.1, (0, 0, 0), m["wood"], segments=16, bevel=0.0)
    # Panza del barril: abombar los anillos centrales.
    for v in staves.data.vertices:
        t = v.co.z / 1.1
        bulge = 1 + 0.14 * math.sin(math.pi * t)
        v.co.x *= bulge
        v.co.y *= bulge
    G.radial_uv(staves)
    # Duelas verticales: la veta de la textura de tablones va en U, así que se giran las UV 90°.
    for loop_uv in staves.data.uv_layers.active.data:
        u, v = loop_uv.uv
        loop_uv.uv = (v, -u)
    parts.append(staves)
    for z in (0.12, 0.92):
        hoop = G.cylinder(f"hoop{z}", 0.47 + 0.02, 0.07, (0, 0, z), m["iron"], segments=16, bevel=0.0)
        G.world_box_uv(hoop)
        parts.append(hoop)
    lid = G.cylinder("lid", 0.36, 0.03, (0, 0, 1.08), m["wood"], segments=16, bevel=0.0)
    G.world_box_uv(lid)
    parts.append(lid)
    return parts


def crate(m, size=0.9):
    parts = [G.box("crate_body", (size, size, size), (0, 0, 0), m["wood"], bevel=0.03)]
    for axis in range(3):
        for side in (-1, 1):
            # Listones de refuerzo en los bordes de cada cara lateral.
            if axis == 2:
                continue
            for z in (0.06, size - 0.06):
                if axis == 0:
                    parts.append(G.box("slat", (size + 0.04, 0.06, 0.1), (0, side * (size / 2 + 0.01), z - 0.05), m["wood_dark"], bevel=0.01))
                else:
                    parts.append(G.box("slat", (0.06, size + 0.04, 0.1), (side * (size / 2 + 0.01), 0, z - 0.05), m["wood_dark"], bevel=0.01))
    for obj in parts:
        G.world_box_uv(obj)
    return parts


def hay_bale(m):
    bale = G.box("bale", (1.2, 0.7, 0.6), (0, 0, 0), m["hay"], bevel=0.12)
    G.world_box_uv(bale)
    parts = [bale]
    for x in (-0.3, 0.3):
        band = G.box("band", (0.05, 0.74, 0.64), (x, 0, -0.02), m["rope"], bevel=0.02)
        G.world_box_uv(band)
        parts.append(band)
    return parts


def cart(m):
    parts = []
    bed = G.box("bed", (2.4, 1.3, 0.12), (0, 0, 0.75), m["wood"], bevel=0.02)
    parts.append(bed)
    for side in (-1, 1):
        parts.append(G.box("side", (2.4, 0.08, 0.45), (0, side * 0.62, 0.87), m["wood"], bevel=0.02))
    parts.append(G.box("front", (0.08, 1.3, 0.45), (1.16, 0, 0.87), m["wood"], bevel=0.02))
    parts.append(G.box("axle", (0.12, 1.6, 0.12), (-0.2, 0, 0.42), m["wood_dark"], bevel=0.01))
    for side in (-1, 1):
        shaft = G.box("shaft", (1.6, 0.08, 0.08), (2.0, side * 0.45, 0.62), m["wood_dark"], bevel=0.01)
        parts.append(shaft)
    for obj in parts:
        G.world_box_uv(obj)
    for side in (-1, 1):
        wheel = G.cylinder("wheel", 0.45, 0.1, (-0.2, side * 0.78, 0.45), m["wood_dark"], segments=20, bevel=0.02)
        wheel.rotation_euler.x = math.pi / 2
        tire = G.cylinder("tire", 0.47, 0.06, (-0.2, side * 0.8, 0.45), m["iron"], segments=20, bevel=0.0)
        tire.rotation_euler.x = math.pi / 2
        bpy.context.view_layer.update()
        G.world_box_uv(wheel)
        G.world_box_uv(tire)
        parts += [wheel, tire]
    # Carga de heno abombada.
    import rb_nature_shapes as N
    parts.append(N.blob("hay_load", 0.75, (0.05, 0, 1.0), 7, m["hay"], roughness=0.12, subdivisions=2, squash=0.55,
                        scale=(1.45, 0.82, 1.0)))
    G.world_box_uv(parts[-1])
    return parts


def well(m):
    parts = []
    ring = G.cylinder("well_ring", 1.0, 0.9, (0, 0, 0), m["stone"], segments=20, bevel=0.03)
    hole = G.cylinder("well_hole", 0.72, 1.2, (0, 0, 0.15), None, segments=20, bevel=0.0)
    G.cut(ring, hole)
    G.radial_uv(ring)
    parts.append(ring)
    water = G.cylinder("water", 0.72, 0.02, (0, 0, 0.35), m["water"], segments=20, bevel=0.0)
    parts.append(water)
    for side in (-1, 1):
        post = G.box("post", (0.14, 0.14, 1.9), (side * 0.85, 0, 0.8), m["wood_dark"], bevel=0.02)
        parts.append(post)
    parts.append(G.box("beam", (2.0, 0.14, 0.14), (0, 0, 2.6), m["wood_dark"], bevel=0.02))
    for obj in parts[1:]:
        G.world_box_uv(obj)
    import rb_geo
    roof = rb_geo.hip_roof("well_roof", 2.4, 1.4, 0.7, (0, 0, 2.62), m["roof"])
    G.world_box_uv(roof)
    parts.append(roof)
    bucket = G.cylinder("bucket", 0.18, 0.28, (0, 0, 1.6), m["wood"], segments=12, top_radius=0.21, bevel=0.0)
    G.world_box_uv(bucket)
    parts.append(bucket)
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
    import rb_castle_kit_materials as K
    m = {
        "wood": K.textured_material("M_Wood", "Wood", TEXTURES, 0.75),
        "wood_dark": K.textured_material("M_WoodDark", "Wood", TEXTURES, 0.8, tint="#8C7A68"),
        "iron": K.textured_material("M_Iron", "Iron", TEXTURES, 0.5, 0.7),
        "stone": K.textured_material("M_StoneWall", "StoneWall", TEXTURES, 0.85),
        "roof": K.textured_material("M_RoofBlue", "RoofBlue", TEXTURES, 0.6),
        "hay": K.textured_material("M_Hay", "Hay", TEXTURES, 0.9),
        "rope": flat("M_Rope", "#8C6E3E", 0.9),
        "water": flat("M_Water", "#2E4A5C", 0.15),
    }
    builders = {"SM_Barrel": barrel, "SM_Crate": crate, "SM_HayBale": hay_bale, "SM_Cart": cart, "SM_Well": well}
    objects = []
    for name, builder in builders.items():
        parts = builder(m)
        obj = G.join(parts, name)
        G.set_origin_to_ground(obj)
        bpy.ops.object.shade_auto_smooth(angle=math.radians(35))
        export(obj)
        objects.append(obj)
    if PREVIEW:
        import rb_preview as P
        P._lighting()
        bpy.ops.mesh.primitive_plane_add(size=40)
        bpy.context.active_object.data.materials.append(flat("Ground", "#9A7B55", 0.95))
        for i, obj in enumerate(objects):
            obj.location = (i * 3.0 - 6.0, 0, 0)
        P._camera((0, 0, 0.8), 19, 30, -12, lens=45)
        P._render(PREVIEW, 1280, 720, 32)


main()
