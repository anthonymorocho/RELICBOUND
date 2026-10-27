"""Vegetación y rocas estilizadas de RELICBOUND (Frontera Verde).

Uso: blender -b --factory-startup --python Tools/Blender/rb_nature.py -- <texturas> <salida_fbx> [preview.png]

Estilo (docs/02): siluetas redondeadas y legibles desde arriba, follaje facetado en grupos, color por vértice con
oclusión ambiental horneada (Cycles) y degradado de altura. En Unity se usa el shader Relicbound/Foliage.
"""
import colorsys
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Vector, noise

sys.path.append(os.path.dirname(__file__))
import rb_geo as G  # noqa: E402

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
TEXTURES = os.path.abspath(args[0]) if args else os.path.abspath("Assets/Relicbound/Art/Textures")
OUT_DIR = os.path.abspath(args[1]) if len(args) > 1 else os.path.abspath("Assets/Relicbound/Art/Nature")
PREVIEW = os.path.abspath(args[2]) if len(args) > 2 else None

PALETTE = {
    "bark": "#5A3E2B",
    "leaf": "#5B8A33",
    "leaf_warm": "#7A9A3A",
    "pine": "#2F5E3C",
    "rock": "#8E897E",
    "moss": "#5E7A34",
}


def rgb(hex_value):
    """Color lineal (los atributos de color de Blender y los vértices en Unity lineal trabajan en lineal)."""
    value = hex_value.lstrip("#")
    srgb = [int(value[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb]


def material(name):
    """Material blanco: el color va en los vértices (Unity: Relicbound/Foliage)."""
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes["Principled BSDF"]
    attribute = nodes.new("ShaderNodeVertexColor")
    attribute.layer_name = "Col"
    mat.node_tree.links.new(attribute.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.85
    return mat


def blob(name, radius, location, seed, mat, roughness=0.3, subdivisions=2, squash=0.85):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdivisions, radius=radius)
    offset = Vector((seed * 13.1, seed * 7.7, seed * 3.3))
    for v in bm.verts:
        direction = v.co.normalized()
        n = noise.noise(direction * 1.7 + offset) + 0.35 * noise.noise(direction * 4.1 + offset * 2)
        v.co = direction * radius * (1 + roughness * n)
        v.co.z *= squash
    obj = G.mesh_object(name, bm, mat)
    obj.location = location
    obj["tint"] = seed
    return obj


def trunk(name, height, radius, seed, mat, lean=0.12):
    rng = random.Random(seed)
    bm = bmesh.new()
    rings, segments = 6, 7
    lean_dir = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0)).normalized() * lean
    rows = []
    for r in range(rings + 1):
        t = r / rings
        ring_radius = radius * (1.3 - 0.6 * t) * (1.5 if r == 0 else 1.0)
        center = lean_dir * (t * t) * height + Vector((0, 0, t * height - 0.1))
        rows.append([bm.verts.new(center + Vector((math.cos(2 * math.pi * s / segments),
                                                   math.sin(2 * math.pi * s / segments), 0)) * ring_radius)
                     for s in range(segments)])
    for r in range(rings):
        for s in range(segments):
            n = (s + 1) % segments
            bm.faces.new((rows[r][s], rows[r][n], rows[r + 1][n], rows[r + 1][s]))
    bm.faces.new(list(reversed(rows[0])))
    bm.faces.new(rows[-1])
    obj = G.mesh_object(name, bm, mat)
    return obj, lean_dir * height + Vector((0, 0, height - 0.1))


def branch(name, start, end, radius, mat):
    direction = end - start
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=5, radius1=radius, radius2=radius * 0.5, depth=direction.length)
    for v in bm.verts:
        v.co.z += direction.length / 2
    obj = G.mesh_object(name, bm, mat)
    obj.location = start
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    bpy.context.view_layer.update()
    return obj


def broadleaf(name, seed, m):
    """Tronco con ramas y copa de 7-10 grupos facetados sobre una envolvente redondeada."""
    rng = random.Random(seed)
    height = rng.uniform(3.3, 4.2)
    trunk_obj, top = trunk(f"{name}_trunk", height, 0.24, seed, m["bark"])
    parts = [trunk_obj]
    crown = rng.uniform(1.9, 2.3)
    center = top + Vector((0, 0, crown * 0.45))
    count = rng.randint(7, 10)
    for i in range(count):
        # Distribución en espiral de Fibonacci sobre la envolvente (sin huecos ni montones).
        y = 1 - (i + 0.5) / count * 1.6
        r = math.sqrt(max(0.0, 1 - y * y))
        phi = i * 2.399963 + seed
        direction = Vector((math.cos(phi) * r, math.sin(phi) * r, y * 0.75))
        size = crown * rng.uniform(0.48, 0.62)
        position = center + direction * crown * 0.62
        parts.append(blob(f"{name}_c{i}", size, position, seed * 10 + i, m["leaf"], roughness=0.22))
        if i < 3:
            start = Vector(top) - Vector((0, 0, height * 0.28))
            parts.append(branch(f"{name}_b{i}", start, position - direction * size * 0.6, 0.09, m["bark"]))
    parts.append(blob(f"{name}_core", crown * 0.75, center, seed * 10 + 99, m["leaf"], roughness=0.15))
    return parts


def pine(name, seed, m):
    rng = random.Random(seed)
    height = rng.uniform(7.5, 10.0)
    trunk_obj, _ = trunk(f"{name}_trunk", height * 0.35, 0.24, seed, m["bark"], lean=0.03)
    parts = [trunk_obj]
    tiers = 5
    for i in range(tiers):
        t = i / tiers
        radius = (1 - t * 0.78) * height * 0.26
        tier_height = height * 0.32
        z = height * 0.16 + t * height * 0.62
        bm = bmesh.new()
        segments = 11
        base = []
        for s in range(segments):
            angle = 2 * math.pi * s / segments + rng.uniform(-0.12, 0.12)
            jag = radius * (1.0 if s % 2 == 0 else 0.78)
            base.append(bm.verts.new((math.cos(angle) * jag, math.sin(angle) * jag, rng.uniform(-0.3, 0.05))))
        inner = [bm.verts.new((v.co.x * 0.5, v.co.y * 0.5, 0.3)) for v in base]
        apex = bm.verts.new((0, 0, tier_height))
        for s in range(segments):
            n = (s + 1) % segments
            bm.faces.new((base[s], base[n], inner[n], inner[s]))
            bm.faces.new((inner[s], inner[n], apex))
        bm.faces.new(list(reversed(base)))
        tier = G.mesh_object(f"{name}_tier{i}", bm, m["pine"])
        tier.location = (0, 0, z)
        tier["tint"] = seed * 10 + i
        parts.append(tier)
    return parts


def bush(name, seed, m):
    rng = random.Random(seed)
    parts = []
    for i in range(rng.randint(3, 5)):
        angle = rng.uniform(0, 2 * math.pi)
        r = rng.uniform(0.0, 0.55)
        size = rng.uniform(0.45, 0.75)
        parts.append(blob(f"{name}_b{i}", size, (math.cos(angle) * r, math.sin(angle) * r, size * 0.55),
                          seed * 5 + i, m["leaf"], roughness=0.22))
    return parts


def rock(name, seed, m, size):
    rng = random.Random(seed)
    obj = blob(f"{name}_rock", size, (0, 0, size * 0.22), seed, m["rock"], roughness=0.32, subdivisions=3,
               squash=rng.uniform(0.5, 0.72))
    decimate = obj.modifiers.new("Facets", "DECIMATE")
    decimate.decimate_type = "COLLAPSE"
    decimate.ratio = 0.14
    obj.rotation_euler.z = rng.uniform(0, math.pi)
    return [obj]


# ---------------------------------------------------------------- color por vértice

def paint(obj, kind, seed):
    """Albedo por vértice: degradado de altura, variación por grupo y musgo en rocas; luego × AO horneada."""
    rng = random.Random(seed)
    mesh = obj.data
    if "Col" not in mesh.color_attributes:
        mesh.color_attributes.new("Col", "BYTE_COLOR", "CORNER")
    mesh.color_attributes.active_color = mesh.color_attributes["Col"]
    zs = [v.co.z for v in mesh.vertices]
    z_min, z_max = min(zs), max(zs)
    colors = mesh.color_attributes["Col"].data
    for poly in mesh.polygons:
        material_name = obj.material_slots[poly.material_index].material.name
        tint = poly.material_index
        for loop_index in poly.loop_indices:
            vertex = mesh.vertices[mesh.loops[loop_index].vertex_index]
            t = (vertex.co.z - z_min) / max(0.01, z_max - z_min)
            if material_name == "M_Bark":
                base = rgb(PALETTE["bark"])
                shade = 0.85 + 0.25 * t
            elif material_name == "M_Rock":
                base = rgb(PALETTE["rock"])
                up = max(0.0, poly.normal.z)
                moss = max(0.0, min(1.0, (up - 0.6) * 3.0)) * (0.5 + 0.5 * noise.noise(vertex.co * 1.3))
                base = [b * (1 - moss) + mm * moss for b, mm in zip(base, rgb(PALETTE["moss"]))]
                shade = 0.9 + 0.15 * t
            else:
                base = rgb(PALETTE["pine"] if kind == "pine" else PALETTE["leaf"])
                warm = rgb(PALETTE["leaf_warm"])
                mix = 0.35 * t + 0.25 * (noise.noise(vertex.co * 0.8 + Vector((seed, 0, 0))) + 0.5)
                if kind != "pine":
                    base = [b * (1 - mix) + w * mix for b, w in zip(base, warm)]
                shade = 0.72 + 0.45 * t
            colors[loop_index].color = (*[min(1.0, c * shade) for c in base], 1.0)
    del rng, tint


def bake_ambient_occlusion(obj):
    """Hornea AO de Cycles en un atributo temporal y la multiplica sobre el albedo por vértice."""
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 64
    for other in bpy.context.scene.objects:
        other.hide_render = other is not obj
    mesh = obj.data
    albedo = [tuple(c.color) for c in mesh.color_attributes["Col"].data]
    ao_layer = mesh.color_attributes.get("AO") or mesh.color_attributes.new("AO", "BYTE_COLOR", "CORNER")
    mesh.color_attributes.active_color = ao_layer
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    scene.render.bake.target = "VERTEX_COLORS"
    scene.world = scene.world or bpy.data.worlds.new("BakeWorld")
    scene.world.use_nodes = True
    bpy.ops.object.bake(type="AO")
    ao = [c.color[0] for c in ao_layer.data]
    out = mesh.color_attributes["Col"].data
    for i, (r, g, b, a) in enumerate(albedo):
        occlusion = 0.45 + 0.55 * ao[i]
        out[i].color = (r * occlusion, g * occlusion, b * occlusion, 1.0)
    mesh.color_attributes.remove(ao_layer)
    mesh.color_attributes.active_color = mesh.color_attributes["Col"]
    for other in bpy.context.scene.objects:
        other.hide_render = False


def finalize(parts, name, kind, seed, flat=True):
    obj = G.join(parts, name)
    G.set_origin_to_ground(obj)
    bpy.context.view_layer.objects.active = obj
    for modifier in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    for poly in obj.data.polygons:
        poly.use_smooth = not flat
    paint(obj, kind, seed)
    bake_ambient_occlusion(obj)
    return obj


def export(obj):
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    location = obj.location.copy()
    obj.location = (0, 0, 0)
    triangulate = obj.modifiers.new("Triangulate", "TRIANGULATE")
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT_DIR, f"{obj.name}.fbx"), use_selection=True,
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z",
                             axis_up="Y", mesh_smooth_type="FACE", path_mode="STRIP", bake_space_transform=True,
                             add_leaf_bones=False, use_mesh_modifiers=True, colors_type="LINEAR")
    obj.modifiers.remove(triangulate)
    obj.location = location
    print(f"[rb] export {obj.name}: {sum(len(p.vertices) - 2 for p in obj.data.polygons)} tris")


def main():
    G.clear_scene()
    m = {"bark": material("M_Bark"), "leaf": material("M_Leaf"), "pine": material("M_Pine"), "rock": material("M_Rock")}
    models = []
    for i in range(3):
        models.append(finalize(broadleaf(f"t{i}", 11 + i, m), f"SM_Tree_Broadleaf_{i + 1:02d}", "leaf", 11 + i))
    for i in range(2):
        models.append(finalize(pine(f"p{i}", 31 + i, m), f"SM_Tree_Pine_{i + 1:02d}", "pine", 31 + i))
    for i in range(2):
        models.append(finalize(bush(f"b{i}", 51 + i, m), f"SM_Bush_{i + 1:02d}", "leaf", 51 + i))
    for i, size in enumerate((0.7, 1.2, 2.0)):
        models.append(finalize(rock(f"r{i}", 71 + i, m, size), f"SM_Rock_{i + 1:02d}", "rock", 71 + i))
    for obj in models:
        export(obj)
    if PREVIEW:
        import rb_preview as P
        P._lighting()
        bpy.ops.mesh.primitive_plane_add(size=60)
        ground = bpy.context.active_object
        ground_mat = bpy.data.materials.new("Ground")
        ground_mat.use_nodes = True
        ground_mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.2, 0.32, 0.08, 1)
        ground.data.materials.append(ground_mat)
        for i, obj in enumerate(models):
            obj.location = ((i % 5) * 6.5 - 13, (i // 5) * 8 - 4, 0)
        P._camera((0, 0, 3), 40, 34, -20, lens=40)
        P._render(PREVIEW, 1280, 720, 48)


main()
