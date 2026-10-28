"""
Render de comprobación del interior con la disposición real (Assets/_Game/Data/Layout_Interior.json),
texturas PBR y luces de techo, para revisar cada habitación antes de compilar en Unity.

blender -b -P Tools/Blender/preview_interior.py -- <salida.png> <x> <y> <z> <yaw> <pitch> <fov>   (coordenadas Unity)
"""
import json
import math
import os
import re
import sys

import bpy

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
argv = sys.argv[sys.argv.index("--") + 1:]
out = argv[0]
cx, cy, cz, yaw, pitch, fov = map(float, argv[1:7])
ART = os.path.join(ROOT, "Assets", "_Game", "Art")

bpy.ops.wm.read_factory_settings(use_empty=True)
for f in ("Models/Exterior/SM_Exterior.fbx", "Models/Interior/SM_Interior.fbx"):
    bpy.ops.import_scene.fbx(filepath=os.path.join(ART, f), axis_forward="Z", axis_up="Y")

# Materiales con textura (id, tamaño en metros, tinte) leídos de ExteriorMaterials.cs para no duplicar la tabla.
spec_src = open(os.path.join(ROOT, "Assets/_Game/Scripts/Editor/ExteriorMaterials.cs"), encoding="utf-8").read()
specs = {}
for m in re.finditer(r'S\("(M_\w+)", "(\w+)", ([\d.]+)f, C\(([\d.]+)f, ([\d.]+)f, ([\d.]+)f\)', spec_src):
    specs[m.group(1)] = (m.group(2), float(m.group(3)), (float(m.group(4)), float(m.group(5)), float(m.group(6))))
plain = {}
for m in re.finditer(r'Plain\("(M_\w+)", C\(([\d.]+)f, ([\d.]+)f, ([\d.]+)f\), ([\d.]+)f', spec_src):
    plain[m.group(1)] = ((float(m.group(2)), float(m.group(3)), float(m.group(4))), float(m.group(5)))


def textured(mat, tex_id, size, tint):
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    folder = os.path.join(ART, "Textures", tex_id)
    base = os.path.join(folder, f"{tex_id}_BaseColor.jpg")
    if not os.path.exists(base):
        return
    coord = nt.nodes.new("ShaderNodeTexCoord")
    mapping = nt.nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (1 / size, 1 / size, 1)
    nt.links.new(coord.outputs["UV"], mapping.inputs["Vector"])
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(base, check_existing=True)
    nt.links.new(mapping.outputs["Vector"], tex.inputs["Vector"])
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs["Factor"].default_value = 1.0
    nt.links.new(tex.outputs["Color"], mix.inputs[6])
    mix.inputs[7].default_value = (*tint, 1)
    nt.links.new(mix.outputs[2], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.6


for mat in bpy.data.materials:
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    name = mat.name.split(".")[0]
    if name in specs:
        textured(mat, *specs[name])
    elif name in plain and bsdf:
        col, smooth = plain[name]
        bsdf.inputs["Base Color"].default_value = (*col, 1)
        bsdf.inputs["Roughness"].default_value = 1 - smooth
    if name in ("M_Curtain", "M_CurtainLit") and bsdf:
        bsdf.inputs["Base Color"].default_value = (0.33, 0.3, 0.26, 1)
    if name in ("M_Glass", "M_GlassDoor") and bsdf:
        bsdf.inputs["Alpha"].default_value = 0.25
    if name == "M_LampShade" and bsdf:
        bsdf.inputs["Emission Color"].default_value = (1, 0.78, 0.52, 1)
        bsdf.inputs["Emission Strength"].default_value = 4.0

# Muebles CC0 según la disposición.
layout = json.load(open(os.path.join(ROOT, "Assets/_Game/Data/Layout_Interior.json")))
loaded = {}
for item in layout["items"]:
    model, variant = item["model"], item["variant"]
    folder = os.path.join(ART, "Models", "PolyHaven", model)
    if model not in loaded:
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=os.path.join(folder, model + ".fbx"), axis_forward="-Z", axis_up="Y")
        new = [o for o in bpy.data.objects if o not in before]
        manifest = json.load(open(os.path.join(folder, "manifest.json")))
        for mm in manifest["materials"]:
            mat = bpy.data.materials.get(mm["name"])
            if mat and mm.get("baseColor"):
                mat.use_nodes = True
                nt = mat.node_tree
                bsdf = nt.nodes.get("Principled BSDF")
                t = nt.nodes.new("ShaderNodeTexImage")
                t.image = bpy.data.images.load(os.path.join(folder, mm["baseColor"]), check_existing=True)
                nt.links.new(t.outputs["Color"], bsdf.inputs["Base Color"])
                if mm["alpha"] != "OPAQUE":
                    nt.links.new(t.outputs["Alpha"], bsdf.inputs["Alpha"])
        for o in new:
            o.hide_render = True
        loaded[model] = {o.name.split(".")[0]: o for o in new if o.type == "MESH"}
    src = loaded[model].get(variant) or next(iter(loaded[model].values()))
    inst = src.copy()
    inst.hide_render = False
    bpy.context.scene.collection.objects.link(inst)
    x, y, z = item["position"]
    from mathutils import Matrix
    s = item["scale"]
    inst.matrix_world = (Matrix.Translation((x, z, y)) @ Matrix.Rotation(math.radians(180 - item["yaw"]), 4, "Z")
                         @ Matrix.Scale(s, 4) @ src.matrix_world)

# Luces de techo encendidas (como con los interruptores), cálidas.
for (x, z) in ((7.1, 8.25), (12.1, 8.25), (9.1, 14.2), (6.5, 12.4), (6.5, 16.15), (12.1, 12.75), (10.8, 16.4), (13.2, 16.5)):
    lamp = bpy.data.objects.new("L", bpy.data.lights.new("L", "POINT"))
    lamp.data.energy = 110
    lamp.data.color = (1.0, 0.76, 0.52)
    lamp.data.shadow_soft_size = 0.15
    lamp.location = (x, z, 2.75)
    bpy.context.scene.collection.objects.link(lamp)

scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.device = "CPU"
scene.cycles.samples = 48
scene.cycles.use_denoising = True
scene.render.resolution_x, scene.render.resolution_y = 1280, 720
scene.view_settings.view_transform = "AgX"
world = bpy.data.worlds.new("W")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.02, 0.025, 0.04, 1)
scene.world = world
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
cam.data.sensor_fit = "VERTICAL"
cam.data.angle = math.radians(fov)
cam.data.clip_start = 0.05
cam.location = (cx, cz, cy)
cam.rotation_euler = (math.radians(90 - pitch), 0, math.radians(-yaw))
scene.collection.objects.link(cam)
scene.camera = cam
scene.render.filepath = out
bpy.ops.render.render(write_still=True)
