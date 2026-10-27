"""Materiales con las texturas horneadas del juego (compartido por el kit, props e iconos)."""
import os

import bpy


def _hex_linear(value):
    value = value.lstrip("#")
    srgb = [int(value[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb]


def textured_material(name, family, textures, roughness=0.8, metallic=0.0, tint=None):
    existing = bpy.data.materials.get(name)
    if existing:
        return existing
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    bsdf = nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    albedo = nodes.new("ShaderNodeTexImage")
    albedo.image = bpy.data.images.load(os.path.join(textures, f"T_{family}_albedo.png"), check_existing=True)
    color_out = albedo.outputs["Color"]
    if tint:
        multiply = nodes.new("ShaderNodeMix")
        multiply.data_type = "RGBA"
        multiply.blend_type = "MULTIPLY"
        multiply.inputs["Factor"].default_value = 1.0
        links.new(color_out, multiply.inputs["A"])
        multiply.inputs["B"].default_value = (*_hex_linear(tint), 1)
        color_out = multiply.outputs["Result"]
    links.new(color_out, bsdf.inputs["Base Color"])
    normal_tex = nodes.new("ShaderNodeTexImage")
    normal_tex.image = bpy.data.images.load(os.path.join(textures, f"T_{family}_normal.png"), check_existing=True)
    normal_tex.image.colorspace_settings.name = "Non-Color"
    normal_map = nodes.new("ShaderNodeNormalMap")
    links.new(normal_tex.outputs["Color"], normal_map.inputs["Color"])
    links.new(normal_map.outputs["Normal"], bsdf.inputs["Normal"])
    return material
