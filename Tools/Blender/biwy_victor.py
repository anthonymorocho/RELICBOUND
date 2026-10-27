"""
Víctor (MakeHuman / MPFB, assets CC0): hombre de unos 40 años, delgado, ropa gastada (chaqueta sobre camisa,
vaqueros, zapatillas sucias), pelo corto. Esqueleto game_engine (nombres tipo Unreal) para usar en Unity como
Humanoid y reutilizar las animaciones CC0 de Quaternius. Texturas de ropa envejecidas (más oscuras y sucias).

blender -b -P Tools/Blender/biwy_victor.py -- <assets_makehuman> <carpeta_salida>
Requiere la extensión MPFB 2 instalada en Blender.
"""
import os
import sys

import bpy
import numpy as np

argv = sys.argv[sys.argv.index("--") + 1:]
MH, OUT = argv[0], argv[1]
os.makedirs(OUT, exist_ok=True)

from bl_ext.user_default.mpfb.services.humanservice import HumanService  # noqa: E402
from bl_ext.user_default.mpfb.services.targetservice import TargetService  # noqa: E402

bpy.ops.wm.read_factory_settings(use_empty=True)

macro = TargetService.get_default_macro_info_dict()
macro["gender"] = 1.0
macro["age"] = 0.68          # algo más de 40 años
macro["muscle"] = 0.42
macro["weight"] = 0.38       # delgado: lleva semanas malviviendo
macro["height"] = 0.64
macro["proportions"] = 0.55
human = HumanService.create_human(macro_detail_dict=macro, scale=0.1)
# Rasgos: cara algo hundida, barba de varios días se sugiere con la textura.
for target, value in (("cheek-l-trans-down|incr", 0.3), ("cheek-r-trans-down|incr", 0.3)):
    try:
        TargetService.set_target_value(human, target, value)
    except Exception:
        pass

HumanService.add_builtin_rig(human, "game_engine")

def asset(path):
    return os.path.join(MH, path)

HumanService.set_character_skin(asset("skins/middleage_caucasian_male/middleage_caucasian_male.mhmat"), human, skin_type="MAKESKIN")
for kind, path in (("Eyes", "eyes/high-poly/high-poly.mhclo"), ("Eyebrows", "eyebrows/eyebrow004/eyebrow004.mhclo"),
                   ("Eyelashes", "eyelashes/eyelashes02/eyelashes02.mhclo"), ("Hair", "hair/short02/short02.mhclo"),
                   ("Clothes", "clothes/male_casualsuit05/male_casualsuit05.mhclo"), ("Clothes", "clothes/shoes02/shoes02.mhclo")):
    HumanService.add_mhclo_asset(asset(path), human, asset_type=kind, subdiv_levels=0, material_type="MAKESKIN")

# Un material por pieza, con nombre estable para crear los materiales URP en Unity.
by_suffix = {"eyebrow": "M_Victor_Brows", "eyelash": "M_Victor_Lashes", "high-poly": "M_Victor_Eyes", "casualsuit": "M_Victor_Clothes",
             "shoes": "M_Victor_Shoes", "short02": "M_Victor_Hair"}
for obj in [o for o in bpy.data.objects if o.type == "MESH"]:
    target = "M_Victor_Skin"
    for key, value in by_suffix.items():
        if key in obj.name:
            target = value
    first = obj.data.materials[0]
    first.name = target
    for i in range(len(obj.data.materials)):
        obj.data.materials[i] = first
    if target == "M_Victor_Skin":
        while len(obj.data.materials) > 1:
            obj.data.materials.pop()
        for poly in obj.data.polygons:
            poly.material_index = 0

def save_texture(img, name, darken=None):
    w, h = img.size
    px = np.empty(w * h * 4, np.float32)
    img.pixels.foreach_get(px)
    px = px.reshape(h, w, 4)
    if darken:
        # Ropa gastada: menos saturada, más oscura y con suciedad irregular abajo.
        rgb = px[..., :3]
        lum = rgb @ np.array([0.3, 0.59, 0.11], np.float32)
        rgb = rgb * (1 - darken["desat"]) + lum[..., None] * darken["desat"]
        px[..., :3] = rgb * darken["mul"]
    out = bpy.data.images.new(name, w, h, alpha=True)
    out.pixels.foreach_set(px.reshape(-1))
    out.filepath_raw = os.path.join(OUT, name + ".png")
    out.file_format = "PNG"
    out.save()

done = set()
for mat in bpy.data.materials:
    if not mat.use_nodes or not mat.name.startswith("M_Victor") or mat.name in done:
        continue
    done.add(mat.name)
    for node in mat.node_tree.nodes:
        if node.type != "TEX_IMAGE" or node.image is None:
            continue
        source = (node.image.filepath or node.image.name).lower()
        if "normal" in source:
            save_texture(node.image, f"{mat.name}_Normal")
        elif "diffuse" in source or "_eye" in source or "eyebrow" in source or "eyelash" in source:
            darken = {"desat": 0.35, "mul": 0.72} if mat.name in ("M_Victor_Clothes", "M_Victor_Shoes") else None
            save_texture(node.image, f"{mat.name}_BaseColor", darken)

# Exporta el personaje con su esqueleto (modificadores aplicados: ocultación de cuerpo bajo la ropa).
bpy.ops.object.select_all(action="DESELECT")
rig = next(o for o in bpy.data.objects if o.type == "ARMATURE")
rig.name = "Victor"
for o in bpy.data.objects:
    if o.type in ("MESH", "ARMATURE"):
        o.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "SK_Victor.fbx"), use_selection=True, apply_unit_scale=True,
                         apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE",
                         use_tspace=True, path_mode="STRIP", bake_space_transform=False, add_leaf_bones=False,
                         use_mesh_modifiers=True, use_armature_deform_only=True, bake_anim=False, object_types={"ARMATURE", "MESH"})
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
print("[biwy] victor", [(o.name, len(o.data.polygons), [m.name for m in o.data.materials]) for o in meshes])
print("[biwy] altura", max(o.dimensions.z for o in meshes))
