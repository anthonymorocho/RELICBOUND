"""
Convierte modelos glTF de Poly Haven (caché) a FBX para Unity y prepara sus texturas.

blender -b -P Tools/Blender/convert_gltf.py -- <cache_models_dir> <out_dir> [ids...]

Salida por modelo en <out_dir>/<id>/:
  <id>.fbx            geometría (escala 1 = 1 m, ejes Unity), materiales con nombre PH_<id>_<n>
  <id>_<n>_BaseColor.jpg / _Normal.jpg / _Mask.png (R=metálico, G=oclusión, A=suavidad)
  manifest.json       materiales, texturas, modo alfa y doble cara
"""
import json
import os
import shutil
import sys

import bpy
import numpy as np

argv = sys.argv[sys.argv.index("--") + 1:]
SRC, OUT = argv[0], argv[1]
ONLY = set(argv[2:])

# Límite de triángulos por modelo; los que lo superan se simplifican conservando las hojas (tarjetas alfa).
# Modelos de una sola pieza (miles de hojas sueltas: separarlas por piezas no tiene sentido).
NO_SPLIT = {"jacaranda_tree"}

MAX_TRIS = {"jacaranda_tree": 140000}


def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def image_to_array(path):
    img = bpy.data.images.load(path, check_existing=False)
    w, h = img.size
    px = np.empty(w * h * 4, np.float32)
    img.pixels.foreach_get(px)
    bpy.data.images.remove(img)
    return px.reshape(h, w, 4), w, h


def save_array(arr, w, h, path, fmt):
    img = bpy.data.images.new("tmp", w, h, alpha=True, float_buffer=False)
    img.colorspace_settings.name = "Non-Color"
    img.pixels.foreach_set(arr.reshape(-1))
    img.filepath_raw = path
    img.file_format = fmt
    img.save()
    bpy.data.images.remove(img)


def pack_mask(arm_path, out_path):
    a, w, h = image_to_array(arm_path)
    out = np.zeros_like(a)
    out[..., 0] = a[..., 2]
    out[..., 1] = a[..., 0]
    out[..., 3] = 1.0 - a[..., 1]
    save_array(out, w, h, out_path, "PNG")


def tris(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def convert(asset_id):
    folder = os.path.join(SRC, asset_id)
    gltf_files = [f for f in os.listdir(folder) if f.endswith(".gltf")]
    if not gltf_files:
        return
    gltf_path = os.path.join(folder, gltf_files[0])
    with open(gltf_path) as fh:
        gltf = json.load(fh)
    clear()
    bpy.ops.import_scene.gltf(filepath=gltf_path)

    out = os.path.join(OUT, asset_id)
    os.makedirs(out, exist_ok=True)
    images = gltf.get("images", [])
    textures = gltf.get("textures", [])

    def tex_path(info):
        if not info:
            return None
        uri = images[textures[info["index"]]["source"]]["uri"]
        return os.path.join(folder, uri)

    manifest = {"id": asset_id, "materials": []}
    renamed = {}
    for i, mat in enumerate(gltf.get("materials", [])):
        name = f"PH_{asset_id}_{i}"
        renamed[mat.get("name", "")] = name
        pbr = mat.get("pbrMetallicRoughness", {})
        entry = {
            "name": name,
            "alpha": mat.get("alphaMode", "OPAQUE"),
            "cutoff": mat.get("alphaCutoff", 0.5),
            "doubleSided": mat.get("doubleSided", False),
            "baseColorFactor": pbr.get("baseColorFactor", [1, 1, 1, 1]),
        }
        base = tex_path(pbr.get("baseColorTexture"))
        if base:
            dst = f"{name}_BaseColor" + os.path.splitext(base)[1]
            shutil.copy(base, os.path.join(out, dst))
            entry["baseColor"] = dst
        normal = tex_path(mat.get("normalTexture"))
        if normal:
            dst = f"{name}_Normal" + os.path.splitext(normal)[1]
            shutil.copy(normal, os.path.join(out, dst))
            entry["normal"] = dst
        arm = tex_path(pbr.get("metallicRoughnessTexture"))
        if arm:
            dst = f"{name}_Mask.png"
            pack_mask(arm, os.path.join(out, dst))
            entry["mask"] = dst
        manifest["materials"].append(entry)

    for m in bpy.data.materials:
        if m.name in renamed:
            m.name = renamed[m.name]

    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    # Aplica transformaciones de la jerarquía glTF y une todo en un solo objeto.
    for o in meshes:
        o.parent = None if False else o.parent
    bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = asset_id
    for o in list(bpy.context.scene.objects):
        if o != obj:
            bpy.data.objects.remove(o, do_unlink=True)

    limit = MAX_TRIS.get(asset_id)
    before = tris(obj)
    if limit and before > limit:
        mod = obj.modifiers.new("Decimate", "DECIMATE")
        mod.decimate_type = "COLLAPSE"
        mod.ratio = limit / before
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    tri = obj.modifiers.new("Triangulate", "TRIANGULATE")
    bpy.ops.object.modifier_apply(modifier=tri.name)
    dims = list(obj.dimensions)
    manifest["triangles"] = tris(obj)
    manifest["trianglesOriginal"] = before
    manifest["size"] = [dims[0], dims[2], dims[1]]

    if asset_id in NO_SPLIT:
        bpy.context.scene.cursor.location = (0, 0, min((obj.matrix_world @ v.co).z for v in obj.data.vertices))
        bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
        obj.location = (0, 0, 0)
        variants = [obj]
    else:
        variants = split_variants(obj, asset_id)
    manifest["variants"] = []
    for v in variants:
        d = v.dimensions
        manifest["variants"].append({"name": v.name, "size": [d[0], d[2], d[1]], "triangles": tris(v)})
    bpy.ops.object.select_all(action="DESELECT")
    for v in variants:
        v.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(out, f"{asset_id}.fbx"), use_selection=True,
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z",
                             axis_up="Y", mesh_smooth_type="FACE", use_tspace=True, path_mode="STRIP",
                             bake_space_transform=True, add_leaf_bones=False, use_mesh_modifiers=True)
    with open(os.path.join(out, "manifest.json"), "w") as fh:
        json.dump(manifest, fh, indent=2)
    print(f"[biwy] {asset_id}: {before} -> {manifest['triangles']} tris, size {dims}")


def split_variants(obj, asset_id):
    """
    Los packs de Poly Haven traen varias variantes en fila (dos arbustos, dos contenedores...). Se separan por
    piezas sueltas, se agrupan por solape en X y cada grupo queda como objeto con el origen en su base.
    """
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.mesh.separate(type="LOOSE")
    parts = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    spans = []
    for o in parts:
        xs = [(o.matrix_world @ v.co).x for v in o.data.vertices]
        spans.append((min(xs), max(xs), o))
    spans.sort(key=lambda t: t[0])
    groups = []
    for lo, hi, o in spans:
        if groups and lo <= groups[-1][1] + 0.02:
            groups[-1][1] = max(groups[-1][1], hi)
            groups[-1][2].append(o)
        else:
            groups.append([lo, hi, [o]])
    result = []
    for index, (_, _, members) in enumerate(groups):
        bpy.ops.object.select_all(action="DESELECT")
        for o in members:
            o.select_set(True)
        bpy.context.view_layer.objects.active = members[0]
        if len(members) > 1:
            bpy.ops.object.join()
        v = bpy.context.view_layer.objects.active
        v.name = asset_id if len(groups) == 1 else f"{asset_id}_{chr(65 + index)}"
        coords = [v.matrix_world @ p.co for p in v.data.vertices]
        cx = (min(c.x for c in coords) + max(c.x for c in coords)) / 2
        cy = (min(c.y for c in coords) + max(c.y for c in coords)) / 2
        cz = min(c.z for c in coords)
        bpy.context.scene.cursor.location = (cx, cy, cz)
        bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
        v.location = (0, 0, 0)
        result.append(v)
    return result


for asset in sorted(os.listdir(SRC)):
    if ONLY and asset not in ONLY:
        continue
    convert(asset)
