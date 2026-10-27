"""Iconos de recursos del HUD renderizados en 3D (fondo transparente), coherentes con el arte del juego.

Uso: blender -b --factory-startup --python Tools/Blender/rb_icons.py -- <salida> [tamaño]
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector

sys.path.append(os.path.dirname(__file__))
import rb_geo as G  # noqa: E402

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = os.path.abspath(args[0]) if args else os.path.abspath("Assets/Relicbound/UI/Icons")
SIZE = int(args[1]) if len(args) > 1 else 256


def mat(name, color, metallic=0.0, roughness=0.5, emission=0.0):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes["Principled BSDF"]
    value = color.lstrip("#")
    srgb = [int(value[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    linear = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb]
    bsdf.inputs["Base Color"].default_value = (*linear, 1)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*linear, 1)
        bsdf.inputs["Emission Strength"].default_value = emission
    return material


def setup_scene():
    G.clear_scene()
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 64
    scene.cycles.use_denoising = True
    scene.render.film_transparent = True
    scene.render.resolution_x = SIZE
    scene.render.resolution_y = SIZE
    scene.view_settings.view_transform = "Standard"
    world = bpy.data.worlds.new("IconWorld")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.35, 0.3, 0.25, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.5
    for name, energy, location, size in (("Key", 380, (3, -4, 6), 5), ("Rim", 450, (-4, 4, 3), 3),
                                         ("Fill", 120, (-4, -3, 1), 4)):
        data = bpy.data.lights.new(name, "AREA")
        data.energy = energy
        data.size = size
        light = bpy.data.objects.new(name, data)
        light.location = location
        light.rotation_euler = (Vector((0, 0, 0.5)) - Vector(location)).to_track_quat("-Z", "Y").to_euler()
        scene.collection.objects.link(light)
    camera_data = bpy.data.cameras.new("Cam")
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = 2.6
    camera = bpy.data.objects.new("Cam", camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    reset_camera()


def reset_camera():
    camera = bpy.context.scene.camera
    camera.location = (3.2, -3.2, 3.0)
    camera.rotation_euler = (Vector((0, 0, 0.45)) - camera.location).to_track_quat("-Z", "Y").to_euler()


def clear_meshes():
    for obj in list(bpy.data.objects):
        if obj.type == "MESH":
            bpy.data.objects.remove(obj, do_unlink=True)


def frame():
    """Centra el contenido y ajusta la escala ortográfica con un 12 % de margen."""
    bpy.context.view_layer.update()
    camera = bpy.context.scene.camera
    view = camera.matrix_world.inverted()
    points = []
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH":
            continue
        depsgraph = bpy.context.evaluated_depsgraph_get()
        evaluated = obj.evaluated_get(depsgraph)
        points += [obj.matrix_world @ v.co for v in evaluated.data.vertices]
    local = [view @ p for p in points]
    min_x, max_x = min(p.x for p in local), max(p.x for p in local)
    min_y, max_y = min(p.y for p in local), max(p.y for p in local)
    center = camera.matrix_world.to_3x3() @ Vector(((min_x + max_x) / 2, (min_y + max_y) / 2, 0))
    camera.location += center
    camera.data.ortho_scale = max(max_x - min_x, max_y - min_y) * 1.12


def render(name):
    frame()
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, f"ICO_{name}.png")
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print(f"[rb] icon {name} -> {path}")


def coin(location, tilt=0.0):
    gold = bpy.data.materials.get("Gold") or mat("Gold", "#D9A42E", 1.0, 0.38)
    body = G.cylinder("coin", 0.62, 0.16, location, gold, segments=40, bevel=0.03)
    rim = G.cylinder("coin_rim", 0.48, 0.03, (location[0], location[1], location[2] + 0.15), gold, segments=40,
                     bevel=0.01)
    for obj in (body, rim):
        obj.rotation_euler.x = tilt
    return [body, rim]


def gold_icon():
    for i in range(3):
        coin((0.15, 0.1, i * 0.17))
    coin((-0.45, -0.35, 0.35), tilt=1.2)


def wood_icon():
    bark = mat("Bark", "#6B4A2C", 0.0, 0.8)
    end = mat("EndGrain", "#D9B47A", 0.0, 0.7)
    rope = mat("Rope", "#B89A5E", 0.0, 0.9)
    for x, z in ((-0.35, 0.0), (0.35, 0.0), (0.0, 0.55)):
        log = G.cylinder("log", 0.36, 1.9, (x, 0.95, z + 0.36), bark, segments=16, bevel=0.03)
        log.rotation_euler.x = math.pi / 2
        cap = G.cylinder("cap", 0.3, 0.02, (x, -0.96, z + 0.36), end, segments=16, bevel=0.0)
        cap.rotation_euler.x = math.pi / 2
    for y in (-0.45, 0.45):
        bpy.ops.mesh.primitive_torus_add(major_radius=0.8, minor_radius=0.05, major_segments=32, minor_segments=8,
                                         location=(0, y, 0.55), rotation=(math.pi / 2, 0, 0))
        band = bpy.context.active_object
        band.data.materials.append(rope)


def stone_icon():
    """Sillar con la textura del juego y un fragmento: se lee como piedra de construcción."""
    stone = mat("Stone", "#A39C8E", 0.0, 0.85)
    textures = os.path.abspath("Assets/Relicbound/Art/Textures")
    albedo_path = os.path.join(textures, "T_StoneWall_albedo.png")
    if os.path.exists(albedo_path):
        nodes = stone.node_tree.nodes
        image = nodes.new("ShaderNodeTexImage")
        image.image = bpy.data.images.load(albedo_path)
        stone.node_tree.links.new(image.outputs["Color"], nodes["Principled BSDF"].inputs["Base Color"])
    dark = mat("StoneDark", "#7E786C", 0.0, 0.85)
    block = G.box("block", (1.5, 1.1, 0.85), (0, 0.1, 0), stone, bevel=0.1)
    G.world_box_uv(block, tile=2.0)
    chunk = G.box("chunk", (0.6, 0.5, 0.45), (-0.65, -0.65, 0), dark, bevel=0.08)
    chunk.rotation_euler.z = 0.5


def iron_icon():
    iron = mat("Iron", "#8D949C", 1.0, 0.35)
    for i, (x, y, z, rot) in enumerate(((0, 0.25, 0, 0), (0, -0.35, 0, 0), (0, -0.05, 0.42, 0.2))):
        ingot = G.box(f"ingot{i}", (1.5, 0.55, 0.42), (x, y, z), iron, bevel=0.06, taper=0.12)
        ingot.rotation_euler.z = rot


def gem_icon():
    gem = mat("Gem", "#B33CC9", 0.0, 0.08, emission=0.4)
    gem.node_tree.nodes["Principled BSDF"].inputs["Transmission Weight"].default_value = 0.35
    bm = bmesh.new()
    crown = [bm.verts.new((math.cos(a) * 0.55, math.sin(a) * 0.55, 0.55))
             for a in [i * math.pi / 4 for i in range(8)]]
    girdle = [bm.verts.new((math.cos(a + math.pi / 8) * 0.85, math.sin(a + math.pi / 8) * 0.85, 0.3))
              for a in [i * math.pi / 4 for i in range(8)]]
    tip = bm.verts.new((0, 0, -0.55))
    bm.faces.new(crown)
    for i in range(8):
        n = (i + 1) % 8
        bm.faces.new((crown[i], girdle[i], crown[n]))
        bm.faces.new((girdle[i], girdle[n], crown[n]))
        bm.faces.new((girdle[n], girdle[i], tip))
    obj = G.mesh_object("gem", bm, gem)
    obj.location = (0, 0, 0.55)
    obj.rotation_euler = (0.25, 0, 0.3)


def main():
    setup_scene()
    for name, builder in (("Gold", gold_icon), ("Wood", wood_icon), ("Stone", stone_icon), ("Iron", iron_icon),
                          ("Gems", gem_icon)):
        clear_meshes()
        reset_camera()
        builder()
        render(name)


main()
