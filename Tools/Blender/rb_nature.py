"""Vegetación y rocas estilizadas de RELICBOUND (Frontera Verde).

Uso: blender -b --factory-startup --python Tools/Blender/rb_nature.py -- <texturas> <salida_fbx> [preview.png]
Estilo (docs/02): siluetas redondeadas y legibles desde arriba, volúmenes agrupados, sin hojas sueltas.
Cada modelo tiene variantes con semilla fija para que el resultado sea reproducible.
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Vector, noise

sys.path.append(os.path.dirname(__file__))
import rb_geo as G  # noqa: E402
from rb_nature_shapes import blob  # noqa: E402

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
TEXTURES = os.path.abspath(args[0]) if args else os.path.abspath("Assets/Relicbound/Art/Textures")
OUT_DIR = os.path.abspath(args[1]) if len(args) > 1 else os.path.abspath("Assets/Relicbound/Art/Nature")
PREVIEW = os.path.abspath(args[2]) if len(args) > 2 else None


def flat_material(name, color, roughness=0.8):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes["Principled BSDF"]
    value = color.lstrip("#")
    srgb = [int(value[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    bsdf.inputs["Base Color"].default_value = (*[c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
                                                 for c in srgb], 1)
    bsdf.inputs["Roughness"].default_value = roughness
    return material


def trunk(name, height, radius, location, seed, material, lean=0.15):
    rng = random.Random(seed)
    bm = bmesh.new()
    rings = 6
    segments = 8
    lean_dir = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0)).normalized() * lean
    verts = []
    for r in range(rings + 1):
        t = r / rings
        ring_radius = radius * (1.25 - 0.55 * t) * (1.35 if r == 0 else 1.0)
        center = lean_dir * (t * t) * height + Vector((0, 0, t * height))
        ring = []
        for s in range(segments):
            angle = 2 * math.pi * s / segments
            ring.append(bm.verts.new(center + Vector((math.cos(angle), math.sin(angle), 0)) * ring_radius))
        verts.append(ring)
    for r in range(rings):
        for s in range(segments):
            a, b = verts[r][s], verts[r][(s + 1) % segments]
            c, d = verts[r + 1][(s + 1) % segments], verts[r + 1][s]
            bm.faces.new((a, b, c, d))
    bm.faces.new(list(reversed(verts[0])))
    bm.faces.new(verts[-1])
    obj = G.mesh_object(name, bm, material)
    obj.location = location
    return obj, lean_dir * height + Vector(location) + Vector((0, 0, height))


def branch(name, start, end, radius, material):
    direction = end - start
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=6, radius1=radius, radius2=radius * 0.55,
                          depth=direction.length)
    for v in bm.verts:
        v.co.z += direction.length / 2
    obj = G.mesh_object(name, bm, material)
    obj.location = start
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    bpy.context.view_layer.update()
    return obj


def broadleaf_tree(name, seed, mats):
    """Tronco visible con dos ramas y copa de 3-4 masas redondeadas que se solapan (silueta de nube)."""
    rng = random.Random(seed)
    height = rng.uniform(3.4, 4.2)
    trunk_obj, top = trunk(f"{name}_trunk", height, 0.24, (0, 0, -0.1), seed, mats["bark"], lean=0.1)
    parts = [trunk_obj]
    crown = rng.uniform(1.6, 1.95)
    center = top + Vector((0, 0, crown * 0.6))
    parts.append(blob(f"{name}_crown", crown, center, seed, mats["leaf"], roughness=0.16, squash=0.9))
    count = rng.randint(3, 4)
    for i in range(count):
        angle = 2 * math.pi * i / count + rng.uniform(-0.4, 0.4)
        r = crown * rng.uniform(0.65, 0.85)
        size = crown * rng.uniform(0.62, 0.78)
        offset = Vector((math.cos(angle) * r, math.sin(angle) * r, rng.uniform(-0.45, 0.35) * crown))
        material = mats["leaf_light"] if offset.z > 0 else mats["leaf"]
        parts.append(blob(f"{name}_crown{i}", size, center + offset, seed + i + 1, material, roughness=0.16,
                          squash=0.9))
        if i < 2:
            start = Vector(top) - Vector((0, 0, height * 0.3))
            parts.append(branch(f"{name}_branch{i}", start, center + offset * 0.75 - Vector((0, 0, size * 0.6)),
                                0.1, mats["bark"]))
    return parts


def pine_tree(name, seed, mats):
    rng = random.Random(seed)
    height = rng.uniform(7.5, 10.5)
    trunk_obj, _ = trunk(f"{name}_trunk", height * 0.35, 0.25, (0, 0, -0.1), seed, mats["bark"], lean=0.03)
    parts = [trunk_obj]
    tiers = 4
    for i in range(tiers):
        t = i / tiers
        radius = (1 - t * 0.75) * height * 0.27
        tier_height = height * 0.36
        z = height * 0.18 + t * height * 0.58
        bm = bmesh.new()
        segments = 10
        base = []
        for s in range(segments):
            angle = 2 * math.pi * s / segments + rng.uniform(-0.1, 0.1)
            jag = radius * (1.0 if s % 2 == 0 else 0.8)
            base.append(bm.verts.new((math.cos(angle) * jag, math.sin(angle) * jag, rng.uniform(-0.25, 0.1))))
        inner = [bm.verts.new((v.co.x * 0.55, v.co.y * 0.55, 0.25)) for v in base]
        apex = bm.verts.new((0, 0, tier_height))
        for s in range(segments):
            n = (s + 1) % segments
            bm.faces.new((base[s], base[n], inner[n], inner[s]))
            bm.faces.new((inner[s], inner[n], apex))
        bm.faces.new(list(reversed(base)))
        tier = G.mesh_object(f"{name}_tier{i}", bm, mats["pine" if i % 2 == 0 else "pine_light"])
        tier.location = (0, 0, z)
        parts.append(tier)
    return parts


def bush(name, seed, mats):
    rng = random.Random(seed)
    parts = []
    for i in range(rng.randint(2, 4)):
        angle = rng.uniform(0, 2 * math.pi)
        r = rng.uniform(0.0, 0.6)
        size = rng.uniform(0.55, 0.9)
        parts.append(blob(f"{name}_b{i}", size, (math.cos(angle) * r, math.sin(angle) * r, size * 0.45),
                          seed * 5 + i, mats["leaf" if i % 2 else "leaf_light"], subdivisions=2))
    return parts


def rock(name, seed, mats, size):
    rng = random.Random(seed)
    obj = blob(f"{name}_rock", size, (0, 0, size * 0.25), seed, mats["rock"], roughness=0.3, subdivisions=3,
               squash=rng.uniform(0.5, 0.75))
    # Facetado estilizado: decimado planar y sombreado plano.
    decimate = obj.modifiers.new("Facets", "DECIMATE")
    decimate.decimate_type = "COLLAPSE"
    decimate.ratio = 0.12
    obj.rotation_euler.z = rng.uniform(0, math.pi)
    return [obj]


def finalize(parts, name):
    for p in parts:
        G.world_box_uv(p)
    obj = G.join(parts, name)
    G.set_origin_to_ground(obj)
    return obj


def export(obj):
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    location = obj.location.copy()
    obj.location = (0, 0, 0)
    triangulate = obj.modifiers.new("Triangulate", "TRIANGULATE")
    path = os.path.join(OUT_DIR, f"{obj.name}.fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
                             mesh_smooth_type="FACE", path_mode="STRIP", bake_space_transform=True,
                             add_leaf_bones=False, use_mesh_modifiers=True)
    obj.modifiers.remove(triangulate)
    obj.location = location
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    print(f"[rb] export {obj.name}: {tris} tris")


def main():
    G.clear_scene()
    mats = {
        "bark": flat_material("M_Bark", "#5A3E2B", 0.9),
        "leaf": flat_material("M_Leaf", "#557F30", 0.85),
        "leaf_light": flat_material("M_LeafLight", "#648E36", 0.85),
        "pine": flat_material("M_Pine", "#2F5A3A", 0.85),
        "pine_light": flat_material("M_PineLight", "#3D6E45", 0.85),
        "rock": flat_material("M_Rock", "#8C877C", 0.9),
    }
    models = []
    for i in range(3):
        models.append(finalize(broadleaf_tree(f"t{i}", 11 + i, mats), f"SM_Tree_Broadleaf_{i + 1:02d}"))
    for i in range(2):
        models.append(finalize(pine_tree(f"p{i}", 31 + i, mats), f"SM_Tree_Pine_{i + 1:02d}"))
    for i in range(2):
        models.append(finalize(bush(f"b{i}", 51 + i, mats), f"SM_Bush_{i + 1:02d}"))
    for i, size in enumerate((0.7, 1.2, 2.0)):
        models.append(finalize(rock(f"r{i}", 71 + i, mats, size), f"SM_Rock_{i + 1:02d}"))
    for obj in models:
        smooth = not obj.name.startswith("SM_Rock")
        for poly in obj.data.polygons:
            poly.use_smooth = smooth
        export(obj)
    if PREVIEW:
        preview(models)


def preview(models):
    import rb_preview as P
    P._lighting()
    bpy.ops.mesh.primitive_plane_add(size=60)
    ground = bpy.context.active_object
    ground.data.materials.append(flat_material("Preview_Ground", "#7E9C48", 0.95))
    for i, obj in enumerate(models):
        obj.location = ((i % 5) * 7 - 14, (i // 5) * 9 - 6, 0)
    P._camera((0, 0, 3), 42, 32, -25, lens=40)
    P._render(PREVIEW, 1280, 720, 32)


main()
