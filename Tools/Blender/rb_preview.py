"""Renders de revisión del kit: castillo inicial montado con cámara de juego y ficha de cada pieza.

El terreno y el cielo de estos renders son provisionales (solo para revisar el kit); el terreno real se hace en Unity.
"""
import math
import os

import bpy
from mathutils import Vector

TOWER_OFFSET = 11.0
SECTION = 2.0


def _hex(value):
    value = value.lstrip("#")
    srgb = [int(value[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return (*[c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb], 1.0)


def _instance(source, location, rotation_z=0.0):
    obj = source.copy()
    obj.location = location
    obj.rotation_euler = (0, 0, rotation_z)
    obj.hide_render = False
    bpy.context.scene.collection.objects.link(obj)
    return obj


def _ground():
    bpy.ops.mesh.primitive_plane_add(size=160)
    ground = bpy.context.active_object
    material = bpy.data.materials.new("Preview_Grass")
    material.use_nodes = True
    nodes, links = material.node_tree.nodes, material.node_tree.links
    bsdf = nodes["Principled BSDF"]
    coord = nodes.new("ShaderNodeTexCoord")
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 0.08
    noise.inputs["Detail"].default_value = 8
    links.new(coord.outputs["Object"], noise.inputs["Vector"])
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = _hex("#5E7F34")
    ramp.color_ramp.elements[1].color = _hex("#9DB35A")
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])

    # Patio y camino de tierra (máscara por distancia al castillo y franja frente a la puerta).
    sep = nodes.new("ShaderNodeSeparateXYZ")
    links.new(coord.outputs["Object"], sep.inputs["Vector"])
    path_x = nodes.new("ShaderNodeMath")
    path_x.operation = "ABSOLUTE"
    links.new(sep.outputs["X"], path_x.inputs[0])
    path = nodes.new("ShaderNodeMath")
    path.operation = "LESS_THAN"
    path.inputs[1].default_value = 1.6
    links.new(path_x.outputs["Value"], path.inputs[0])
    south = nodes.new("ShaderNodeMath")
    south.operation = "LESS_THAN"
    south.inputs[1].default_value = -10.0
    links.new(sep.outputs["Y"], south.inputs[0])
    path_mask = nodes.new("ShaderNodeMath")
    path_mask.operation = "MULTIPLY"
    links.new(path.outputs["Value"], path_mask.inputs[0])
    links.new(south.outputs["Value"], path_mask.inputs[1])
    inside_x = nodes.new("ShaderNodeMath")
    inside_x.operation = "ABSOLUTE"
    links.new(sep.outputs["X"], inside_x.inputs[0])
    inside_y = nodes.new("ShaderNodeMath")
    inside_y.operation = "ABSOLUTE"
    links.new(sep.outputs["Y"], inside_y.inputs[0])
    inside = nodes.new("ShaderNodeMath")
    inside.operation = "MAXIMUM"
    links.new(inside_x.outputs["Value"], inside.inputs[0])
    links.new(inside_y.outputs["Value"], inside.inputs[1])
    yard = nodes.new("ShaderNodeMath")
    yard.operation = "LESS_THAN"
    yard.inputs[1].default_value = 10.0
    links.new(inside.outputs["Value"], yard.inputs[0])
    dirt_mask = nodes.new("ShaderNodeMath")
    dirt_mask.operation = "MAXIMUM"
    links.new(path_mask.outputs["Value"], dirt_mask.inputs[0])
    links.new(yard.outputs["Value"], dirt_mask.inputs[1])
    dirt = nodes.new("ShaderNodeMix")
    dirt.data_type = "RGBA"
    links.new(dirt_mask.outputs["Value"], dirt.inputs["Factor"])
    links.new(ramp.outputs["Color"], dirt.inputs["A"])
    dirt.inputs["B"].default_value = _hex("#9A7B55")
    links.new(dirt.outputs["Result"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.95
    ground.data.materials.append(material)
    return ground


def _lighting():
    world = bpy.data.worlds.new("Preview_Sky")
    bpy.context.scene.world = world
    world.use_nodes = True
    background = world.node_tree.nodes["Background"]
    background.inputs["Color"].default_value = _hex("#A9C6E8")
    background.inputs["Strength"].default_value = 0.9
    sun_data = bpy.data.lights.new("Sun", "SUN")
    sun_data.energy = 4.0
    sun_data.angle = math.radians(2.5)
    sun_data.color = (1.0, 0.93, 0.82)  # ~5200 K
    sun = bpy.data.objects.new("Sun", sun_data)
    sun.rotation_euler = (math.radians(48), 0, math.radians(35))
    bpy.context.scene.collection.objects.link(sun)


def _camera(target, distance, pitch_deg, yaw_deg, lens=50):
    data = bpy.data.cameras.new("Camera")
    data.lens = lens
    data.clip_end = 1000
    camera = bpy.data.objects.new("Camera", data)
    pitch, yaw = math.radians(pitch_deg), math.radians(yaw_deg)
    offset = Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch))) * distance
    camera.location = Vector(target) + offset
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.collection.objects.link(camera)
    bpy.context.scene.camera = camera
    return camera


def _render(path, width, height, samples):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_denoising = True
    scene.render.resolution_x = width
    scene.render.resolution_y = height
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print(f"[rb] render -> {path}")


def layout_castle(pieces):
    """Castillo inicial: torreón, recinto de 22 x 22 m con 4 torres y puerta al sur."""
    placed = []
    t = TOWER_OFFSET
    for sx in (-1, 1):
        for sy in (-1, 1):
            placed.append(_instance(pieces["tower"], (sx * t, sy * t, 0)))
    centers = [-9 + SECTION * i for i in range(10)]
    for x in centers:
        placed.append(_instance(pieces["wall"], (x, t, 0), math.pi))          # norte, exterior +Y
        if abs(x) > 4:
            placed.append(_instance(pieces["wall"], (x, -t, 0), 0.0))         # sur, exterior -Y
        placed.append(_instance(pieces["wall"], (t, x, 0), math.pi / 2))      # este, exterior +X
        placed.append(_instance(pieces["wall"], (-t, x, 0), -math.pi / 2))    # oeste, exterior -X
    placed.append(_instance(pieces["gate"], (0, -t, 0)))
    placed.append(_instance(pieces["keep"], (0, 2.5, 0)))
    return placed


def render_layout(pieces, materials, path):
    for obj in pieces.values():
        obj.hide_render = True
    _ground()
    _lighting()
    placed = layout_castle(pieces)
    base, ext = os.path.splitext(path)

    _camera((0, 0, 3), 78, 45, -35)
    _render(path, 1280, 720, 48)
    _camera((0, -4, 4), 42, 30, -20, lens=45)
    _render(f"{base}_close{ext}", 1280, 720, 48)

    # Fichas individuales: tres cuartos frontal y trasera, a escala de la pieza.
    sheet = {"wall": (6, 18), "tower": (7, 28), "gate": (5, 30), "keep": (8, 46)}
    for obj in placed:
        obj.hide_render = True
    for key, (height, distance) in sheet.items():
        pieces[key].hide_render = False
        pieces[key].location = (0, 60, 0)
        for label, yaw in (("front", -35), ("back", 145)):
            _camera((0, 60, height / 2), distance, 18, yaw, lens=50)
            _render(f"{base}_{key}_{label}{ext}", 640, 640, 32)
        pieces[key].hide_render = True


def render_levels(exported, path):
    """Ficha comparativa: filas = niveles 1-3, columnas = muralla (x3 tramos), torre, puerta, torreón."""
    _ground()
    _lighting()
    columns = {"wall": -18.0, "tower": -9.0, "gate": 2.0, "keep": 16.0}
    for (key, level), obj in exported.items():
        y = (level - 2) * 18.0
        obj.location = (columns[key], y, 0)
        obj.hide_render = False
        if key == "wall":
            for offset in (-2.0, 2.0):
                copy = obj.copy()
                copy.location = (columns[key] + offset, y, 0)
                bpy.context.scene.collection.objects.link(copy)
    _camera((0, 0, 4), 95, 38, -20, lens=45)
    _render(path, 1600, 900, 32)
