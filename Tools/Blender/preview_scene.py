"""
Render de comprobación (Cycles CPU) de FBX exportados, con colores planos por material.
blender -b -P Tools/Blender/preview_scene.py -- salida.png cam_x cam_y cam_z yaw pitch fov fbx1 [fbx2 ...]
Coordenadas de cámara en el mundo de Unity (x este, y arriba, z norte); yaw en grados desde +z hacia +x.
"""
import math
import sys

import bpy

argv = sys.argv[sys.argv.index("--") + 1:]
out = argv[0]
cx, cy, cz, yaw, pitch, fov = map(float, argv[1:7])
files = argv[7:]

bpy.ops.wm.read_factory_settings(use_empty=True)
for f in files:
    bpy.ops.import_scene.fbx(filepath=f, axis_forward="Z", axis_up="Y")

COLORS = {
    "CarPaint": (0.18, 0.2, 0.24), "CarGlass": (0.02, 0.02, 0.025), "CarTire": (0.02, 0.02, 0.02), "CarRim": (0.6, 0.6, 0.62),
    "CarHeadlight": (0.9, 0.9, 0.85), "CarTaillight": (0.5, 0.02, 0.02), "CarGrille": (0.03, 0.03, 0.03), "CarPlate": (0.85, 0.85, 0.8),
    "CarTrim": (0.03, 0.03, 0.03), "CarSeam": (0.01, 0.01, 0.01), "CarChrome": (0.8, 0.8, 0.8), "CarInterior": (0.05, 0.05, 0.05),
    "FloorWood": (0.35, 0.22, 0.12), "FloorKitchen": (0.7, 0.7, 0.66), "FloorBath": (0.6, 0.62, 0.6), "Wall": (0.75, 0.7, 0.6),
    "Ceiling": (0.85, 0.85, 0.82), "Baseboard": (0.9, 0.9, 0.86), "Cabinet": (0.55, 0.38, 0.22), "Countertop": (0.3, 0.3, 0.3),
    "Fridge": (0.85, 0.85, 0.82), "Porcelain": (0.92, 0.92, 0.9), "Quilt": (0.45, 0.2, 0.2), "Mattress": (0.85, 0.85, 0.8),
    "Wardrobe": (0.5, 0.35, 0.22), "Appliance": (0.9, 0.9, 0.9), "Siding": (0.62, 0.55, 0.45), "Trim": (0.85, 0.84, 0.8), "Roof": (0.12, 0.12, 0.13), "Foundation": (0.4, 0.4, 0.4),
    "Glass": (0.05, 0.07, 0.09), "Curtain": (0.5, 0.45, 0.4), "Lawn": (0.15, 0.25, 0.08), "Asphalt": (0.08, 0.08, 0.08),
    "Sidewalk": (0.45, 0.45, 0.43), "Driveway": (0.5, 0.5, 0.48), "Path": (0.5, 0.48, 0.45), "Fence": (0.35, 0.27, 0.2),
    "Door": (0.25, 0.12, 0.08), "Brick": (0.45, 0.2, 0.15), "Mulch": (0.2, 0.12, 0.07), "Shed": (0.38, 0.3, 0.22),
    "Gutter": (0.8, 0.8, 0.78), "Metal": (0.3, 0.3, 0.3), "Soffit": (0.8, 0.8, 0.76), "Porch": (0.55, 0.54, 0.5),
}
for m in bpy.data.materials:
    col = (0.6, 0.6, 0.6)
    for key, value in COLORS.items():
        if key in m.name:
            col = value
            break
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*col, 1)
        bsdf.inputs["Roughness"].default_value = 0.7

scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.device = "CPU"
scene.cycles.samples = 24
scene.cycles.use_denoising = True
scene.render.resolution_x, scene.render.resolution_y = 1280, 720
world = bpy.data.worlds.new("W")
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.55, 0.6, 0.7, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.6
scene.world = world
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
sun.data.energy = 3.0
sun.rotation_euler = (math.radians(50), 0, math.radians(-35))
scene.collection.objects.link(sun)
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
cam.data.sensor_fit = "VERTICAL"
cam.data.angle = math.radians(fov)
cam.location = (cx, cz, cy)
cam.rotation_euler = (math.radians(90 - pitch), 0, math.radians(-yaw))
scene.collection.objects.link(cam)
scene.camera = cam
import os
if os.environ.get("PREVIEW_LAMP"):
    lamp = bpy.data.objects.new("Lamp", bpy.data.lights.new("Lamp", "POINT"))
    lamp.data.energy = float(os.environ["PREVIEW_LAMP"])
    lamp.data.shadow_soft_size = 0.3
    lamp.location = (cx, cz, cy + 0.6)
    scene.collection.objects.link(lamp)
scene.render.filepath = out
bpy.ops.render.render(write_still=True)
