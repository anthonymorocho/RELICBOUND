"""Texturas tileables de RELICBOUND horneadas desde materiales procedurales de Blender.

Uso: blender -b --factory-startup --python Tools/Blender/rb_textures.py -- <carpeta_salida> [resolucion]
Genera <nombre>_albedo.png y <nombre>_normal.png por cada familia (docs/02 §Técnica de materiales).
Cada textura representa 2 x 2 m de superficie (texel density = resolución / 2 por metro).
"""
import sys
import os
import bpy

sys.path.append(os.path.dirname(__file__))
import rb_materials as M  # noqa: E402

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
out_dir = os.path.abspath(args[0] if args else "Art/Textures")
res = int(args[1]) if len(args) > 1 else 1024
os.makedirs(out_dir, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.device = "CPU"
scene.cycles.samples = 4

# Plano de 2 x 2 m con UV 0..1: lo que se hornea es exactamente un tile.
bpy.ops.mesh.primitive_plane_add(size=2.0)
plane = bpy.context.active_object

for name, builder in M.FAMILIES.items():
    material = builder()
    plane.data.materials.clear()
    plane.data.materials.append(material)
    for kind in ("albedo", "normal"):
        image = bpy.data.images.new(f"{name}_{kind}", res, res, alpha=False, float_buffer=False)
        image.colorspace_settings.name = "sRGB" if kind == "albedo" else "Non-Color"
        nodes = material.node_tree.nodes
        target = nodes.new("ShaderNodeTexImage")
        target.image = image
        nodes.active = target
        bake = scene.render.bake
        bake.margin = 0
        if kind == "albedo":
            bake.use_pass_direct = False
            bake.use_pass_indirect = False
            bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"})
        else:
            bake.normal_space = "TANGENT"
            bpy.ops.object.bake(type="NORMAL")
        image.filepath_raw = os.path.join(out_dir, f"T_{name}_{kind}.png")
        image.file_format = "PNG"
        image.save()
        nodes.remove(target)
        print(f"[rb] {name} {kind} -> {image.filepath_raw}")
