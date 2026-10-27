"""
Sedán de Daniel a partir de un modelo CC BY 4.0 (TopoSphere, vía Sloyd). Escala a 4.75 m de largo,
ancho realista, origen en el suelo, morro hacia +y (norte en el juego) y textura repintada a gris azulado
oscuro (modificación permitida por CC BY; se documenta en REGISTRO_ASSETS.csv).

blender -b -P Tools/Blender/convert_sedan.py -- <car.glb> <carpeta_salida> <front_sign>
"""
import math
import os
import sys

import bpy
import numpy as np
from mathutils import Matrix

argv = sys.argv[sys.argv.index("--") + 1:]
SRC, OUT, FRONT = argv[0], argv[1], float(argv[2])

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SRC)
obj = [o for o in bpy.context.scene.objects if o.type == "MESH"][0]
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
dims = obj.dimensions
s = 4.75 / dims.y
mesh = obj.data
mesh.transform(Matrix.Diagonal((s * 0.93, s, s, 1.0)))
if FRONT < 0:
    mesh.transform(Matrix.Rotation(math.pi, 4, "Z"))
zs = [v.co.z for v in mesh.vertices]
xs = [v.co.x for v in mesh.vertices]
ys = [v.co.y for v in mesh.vertices]
mesh.transform(Matrix.Translation((-(min(xs) + max(xs)) / 2, -(min(ys) + max(ys)) / 2, -min(zs))))
obj.name = "Car_Daniel"
mesh.materials[0].name = "M_CarSedan"

# Repintado: tonos rojizos saturados (pintura) -> gris azulado oscuro; el resto (cromados, gomas, cristales) intacto.
img = bpy.data.images[0]
w, h = img.size
px = np.empty(w * h * 4, np.float32)
img.pixels.foreach_get(px)
px = px.reshape(h, w, 4)
r, g, b = px[..., 0], px[..., 1], px[..., 2]
mx = np.maximum(np.maximum(r, g), b)
mn = np.minimum(np.minimum(r, g), b)
sat = np.where(mx > 1e-4, (mx - mn) / np.maximum(mx, 1e-4), 0)
paint = np.clip((sat - 0.25) / 0.25, 0, 1) * (r >= g) * (r >= b)
lum = 0.2126 * r + 0.7152 * g + 0.0722 * b
shade = np.clip(mx, 0, 1)
target = np.stack([0.16 * shade + 0.02, 0.19 * shade + 0.025, 0.24 * shade + 0.03], -1)
for c in range(3):
    px[..., c] = px[..., c] * (1 - paint) + target[..., c] * paint
img.pixels.foreach_set(px.reshape(-1))
os.makedirs(OUT, exist_ok=True)
img.filepath_raw = os.path.join(OUT, "M_CarSedan_BaseColor.png")
img.file_format = "PNG"
img.save()

tri = obj.modifiers.new("Tri", "TRIANGULATE")
bpy.ops.object.select_all(action="DESELECT")
obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "SM_Car_Daniel.fbx"), use_selection=True, apply_unit_scale=True,
                         apply_scale_options="FBX_SCALE_UNITS", axis_forward="Z", axis_up="Y", mesh_smooth_type="FACE",
                         use_tspace=True, path_mode="STRIP", bake_space_transform=True, add_leaf_bones=False,
                         use_mesh_modifiers=True)
print("[biwy] sedan", tuple(round(d, 2) for d in obj.dimensions), len(mesh.polygons))
